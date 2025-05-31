using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Timers;
using UnityEngine;
using UnityEngine.SceneManagement;
using ErenshorCoop.Shared;
using ErenshorCoop.Server;
using ErenshorCoop.Shared.Packets;

namespace ErenshorCoop.Client
{
	public class ClientConnectionManager : MonoBehaviour, INetEventListener
	{
		private NetManager netManager;
		public NetPeer Server;
		private NetPeer peer;


		public PlayerSync LocalPlayer;
		public short LocalPlayerID = -1;
		public Dictionary<short, NetworkedPlayer> Players = new();


		public Action<short, string, string> OnClientConnect;
		public Action<short, string, string> OnClientSwapZone;
		public Action<short, string, List<short>> OnZoneOwnerChange;
		public Action<short> OnPlayerDisconnect;
		public Action OnConnect;
		public Action OnDisconnect;

		public static ClientConnectionManager Instance;

		public bool IsRunning => netManager.IsRunning && Server is {ConnectionState: ConnectionState.Connected };
		private bool IsConnected => netManager.IsRunning && peer is {ConnectionState: ConnectionState.Connected };

		public NetStatistics GetStatistics() => Server.Statistics;

		public Entity requestReceiver;

		public void Awake()
		{
			if (Instance != null) Destroy(gameObject);

			ErenshorCoopMod.OnGameMenuLoad += OnGameMenuLoad;

			Instance = this;

			netManager = new(this)
			{
				EnableStatistics = true,
				ChannelsCount = Variables.MaxChannelCount
			};
		}

		public void OnGameMenuLoad(Scene _)
		{
			Disconnect();
		}

		public void Connect(string ip, int port)
		{
			Disconnect();

			netManager.Start();
			peer = netManager.Connect(ip, port, "");
			if (peer != null)
			{
				Logging.Log($"Connected to {ip}:{port}");
				if (LocalPlayer != null)
					LocalPlayer.peer = peer;
			}
			else
			{
				Logging.LogError($"Could not connect.");
				netManager.Stop();
				peer = null;
			}
		}

		public void Disconnect()
		{
			Logging.LogError($"Disconnected.");
			if(IsConnected && IsRunning)
				Logging.LogGameMessage($"Disconnected.");

			//Destroy connected player characters
			foreach (var player in Players.Values)
			{
				if (player != null && player.gameObject != null)
					Destroy(player.gameObject);
			}

			Players.Clear();
			netManager.Stop();

			peer = null;
			Server = null;
			if (LocalPlayer != null)
				LocalPlayer.peer = null;

			//Clear packet mngr q
			PacketManager.ClearQueue();

			OnDisconnect?.Invoke();
			Grouping.Cleanup();
		}


		public void Update()
		{
			netManager.PollEvents();
			if (!IsConnected) return;

			foreach (var player in Players)
			{
				if (player.Value.sceneChanged)
				{
					player.Value.gameObject.SetActive(player.Value.currentScene == SceneManager.GetActiveScene().name);
					player.Value.sceneChanged = false;

					Logging.LogError($"[Client] Player changed scene");
				}
			}
		}


		public void OnDestroy()
		{
			Disconnect();
			ErenshorCoopMod.OnGameMenuLoad -= OnGameMenuLoad;
		}



		//We're delaying this because it takes a second to start the connection
		public void OnConnectDelayed(float delayInSeconds, short playerID, string scene)
		{
			Timer timer = new(delayInSeconds * 1000);
			timer.Elapsed += (_, _) =>
			{
				OnClientConnect?.Invoke(playerID, scene, null);
			};
			timer.AutoReset = false;
			timer.Start();
		}



		private void OnPlayerConnect(short playerID, PlayerConnectionPacket packet, NetPeer peer)
		{
			if (Players.ContainsKey(playerID))
			{
				//Logging.Log($"Already in list?");
				return;
			}

			string scene = packet.scene;
			string charName = packet.name;
			var pos = packet.position;
			var rot = packet.rotation;

			var pl = GameHooks.CreatePlayer(playerID, pos, rot);
			if (pl != null)
			{
				Players.Add(playerID, pl);
				pl.Init(pos, rot, charName, scene, playerID, peer);
				pl.HandleConnectPacket(packet);



				Logging.Log($"Player {charName} connected @{scene}");

				OnConnectDelayed(0.25f, playerID, scene);
			}
			else
			{
				Logging.LogError($"Unknown Error: Failed to Create Player.");
			}
		}

		public void PlayerDisconnect(short playerID)
		{
			
			if (Players.TryGetValue(playerID, out var p))
			{
				Logging.Log($"{p.name} Disconnected");
				Destroy(p.gameObject);
				Players.Remove(playerID);
				OnPlayerDisconnect?.Invoke(playerID);
			}
		}


		public void OnPeerConnected(NetPeer peer)
		{
			Server ??= peer;
			Logging.Log($"Connected to: {peer.Id} {peer.Address}");
		}
		public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
		{
			Logging.Log($"Server Disconnected: {peer.Id} {peer.Address} {disconnectInfo.Reason}");
			if(Server == null)
				Logging.LogGameMessage($"Could not connect to server: {disconnectInfo.Reason}");
			else
				Logging.LogGameMessage($"Disconnected: {disconnectInfo.Reason}");
			Disconnect();

			UI.Connect._feedbackText.text = $"{disconnectInfo.Reason}";
			UI.Connect.EnableButtons();
		}

		public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
		{
			if (socketError == SocketError.Success)
			{
				UI.Connect._feedbackText.text = $"Connected!";
				return;
			}

			Logging.LogGameMessage($"Error: {socketError}");

			Disconnect();
			UI.Connect._feedbackText.text = $"{socketError}";
			UI.Connect.EnableButtons();
		}

		public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
		{
			var packetType = (PacketType)reader.GetByte();

			var isPlayerPacket = true;

			BasePacket packet;
			switch (packetType)
			{
				case PacketType.PLAYER_DATA:
					packet = new PlayerDataPacket();
					break;
				case PacketType.PLAYER_TRANSFORM:
					packet = new PlayerTransformPacket();
					break;
				case PacketType.PLAYER_ACTION:
					packet = new PlayerActionPacket();
					break;
				case PacketType.ENTITY_SPAWN:
					isPlayerPacket = false;
					packet = new EntitySpawnPacket();
					break;
				case PacketType.ENTITY_ACTION:
					isPlayerPacket = false;
					packet = new EntityActionPacket();
					break;
				case PacketType.ENTITY_DATA:
					isPlayerPacket = false;
					packet = new EntityDataPacket();
					break;
				case PacketType.ENTITY_TRANSFORM:
					isPlayerPacket = false;
					packet = new EntityTransformPacket();
					break;
				case PacketType.SERVER_CONNECT:
					isPlayerPacket = false;
					packet = new ServerConnectPacket();
					break;
				case PacketType.PLAYER_CONNECT:
					packet = new PlayerConnectionPacket();
					break;
				case PacketType.DISCONNECT:
					isPlayerPacket = false;
					packet = new ServerDisonnectPacket();
					break;
				case PacketType.PLAYER_MESSAGE:
					packet = new PlayerMessagePacket();
					break;
				case PacketType.SERVER_INFO:
					isPlayerPacket = false;
					packet = new ServerInfoPacket();
					break;
				case PacketType.GROUP:
					packet = new GroupPacket();
					isPlayerPacket = false;
					break;
				case PacketType.SERVER_GROUP:
					packet = new ServerGroupPacket();
					break;
				case PacketType.PLAYER_REQUEST:
					isPlayerPacket = false;
					packet = new PlayerRequestPacket();
					break;
				case PacketType.SERVER_REQUEST:
					isPlayerPacket = false;
					packet = new ServerRequestPacket();
					break;
				default: packet = new BasePacket(DeliveryMethod.Unreliable); Logging.LogError($"Unhandled PacketType: {packetType}."); isPlayerPacket = false; break;
			}

			


			packet.Read(reader);
			reader.Recycle();

			if (packet.GetType() == typeof(BasePacket))
			{
				return; //Unknown packet
			}


			

			if (isPlayerPacket)
			{
				if (packetType == PacketType.SERVER_GROUP)
				{
					Grouping.HandleServerPacket((ServerGroupPacket)packet);
					return;
				}
				if (packet is PlayerMessagePacket messagePacket)
				{
					Logging.HandleMessage(GetPlayerFromID(packet.entityID),messagePacket);
					if (messagePacket.messageType == MessageType.INFO)
						return; //don't resend
				}
				//Logging.Log($"{packet.GetType()}");
				short playerID = packet.entityID;
				//if (playerID == LocalPlayerID) return;

				if (playerID != LocalPlayerID)
				{
					if (packet is PlayerConnectionPacket)
					{
						OnPlayerConnect(playerID, (PlayerConnectionPacket)packet, peer);
					}
					else
					{
						if (packet is PlayerDataPacket)
						{
							if (((PlayerDataPacket)packet).dataTypes.Contains(PlayerDataType.SCENE))
							{
								if (Players.TryGetValue(playerID, out var pl))
								{
									if (pl.currentScene != ((PlayerDataPacket)packet).scene)
									{
										OnClientSwapZone?.Invoke(playerID, ((PlayerDataPacket)packet).scene, pl.currentScene);
									}
								}
							}
						}
						if (Players.ContainsKey(playerID))
							Players[playerID].OnPlayerDataReceive(packet);
					}
				}
			}
			else if (packetType == PacketType.GROUP)
			{
				Grouping.HandleClientPacket((GroupPacket)packet);
				return;
			}
			else if (packetType == PacketType.SERVER_INFO)
			{
				Logging.Log($"{packet.GetType()}");
				if (((ServerInfoPacket)packet).dataTypes.Contains(ServerInfoType.PVP_MODE))
				{
					foreach (var player in Players)
					{
						player.Value.character.MyFaction = ((ServerInfoPacket)packet).pvpMode ? Character.Faction.PC : Character.Faction.Player;
						player.Value.character.BaseFaction = player.Value.character.MyFaction;
					}

					ServerConfig.clientIsPvpEnabled = ((ServerInfoPacket)packet).pvpMode;
				}
				if (((ServerInfoPacket)packet).dataTypes.Contains(ServerInfoType.ZONE_OWNERSHIP))
				{
					OnZoneOwnerChange?.Invoke(((ServerInfoPacket)packet).zoneOwner, ((ServerInfoPacket)packet).zone, ((ServerInfoPacket)packet).playerList);
					return;
				}
			}
			else if (packetType == PacketType.SERVER_CONNECT)
			{
				LocalPlayerID = ((ServerConnectPacket)packet).entityID;
				LocalPlayer.playerID = LocalPlayerID;
				LocalPlayer.entityID = LocalPlayerID;
				OnConnect?.Invoke();

				UI.Connect._feedbackText.text = "Connected!";

				Grouping.ForceClearGroup();
			}
			else if (packetType == PacketType.PLAYER_REQUEST)
			{
				var p = (PlayerRequestPacket)packet;
				if (p.dataTypes.Contains(Request.ENTITY_ID))
				{
					short reqId = SharedNPCSyncManager.Instance.GetFreeId();
					var pa = PacketManager.GetOrCreatePacket<ServerRequestPacket>(p.entityID, PacketType.SERVER_REQUEST);
					pa.AddPacketData(Request.ENTITY_ID, "reqID", reqId);
					pa.SetTarget(peer);
					pa.exclusions.Add(LocalPlayer.peer);
				}
				return;
			}
			else if (packetType == PacketType.SERVER_REQUEST)
			{
				if (packet.entityID == LocalPlayerID)
				{
					var p = (ServerRequestPacket)packet;
					if (p.dataTypes.Contains(Request.ENTITY_ID))
					{
						requestReceiver?.ReceiveRequestID(p.reqID);
					}
				}

				return;
			}
			else if ( packetType == PacketType.DISCONNECT)
			{
				PlayerDisconnect(packet.entityID);
			}
			else
			{
				var bp = (EntityBasePacket)packet;
				if (!ClientZoneOwnership.isZoneOwner || bp.entityType == EntityType.PET)
				{
					ClientNPCSyncManager.Instance.OnEntityDataReceive(packet);
				}

				if (ServerConnectionManager.Instance.IsRunning)
				{
					ServerZoneOwnership.HandleEntityPacket(bp);
				}
			}

			if (ServerConnectionManager.Instance.IsRunning)
			{
				//Logging.Log($"resending {packet.GetType()}");
				packet.exclusions.Add(peer);
				PacketManager.ServerAddPacket(packetType, packet);
			}
		}

		public Entity GetPlayerFromID(short playerID)
		{
			if (playerID == LocalPlayerID) return LocalPlayer;
			return Players.TryGetValue(playerID, out var play) ? play : null;
		}

		public void OnConnectionRequest(ConnectionRequest request) { request.Accept(); }
		public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }
		public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
	}
}
