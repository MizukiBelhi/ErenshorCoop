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
using Random = UnityEngine.Random;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using Steamworks;
using LiteNetLib.Utils;

namespace ErenshorCoop.Client
{
	public class ClientConnectionManager : MonoBehaviour, INetEventListener
	{
		private NetManager netManager;
		public NetPeer Server;
		private NetPeer peer;


		public PlayerSync LocalPlayer;
		public short LocalPlayerID = 0;
		public Dictionary<short, NetworkedPlayer> Players = new();


		public Action<short, string, string> OnClientConnect;
		public Action<short, string, string> OnClientSwapZone;
		public Action<short, string, List<short>> OnZoneOwnerChange;
		public Action<short> OnPlayerDisconnect;
		public Action OnConnect;
		public Action OnDisconnect;

		public static ClientConnectionManager Instance;

		public bool IsRunning => (netManager.IsRunning && Server is {ConnectionState: ConnectionState.Connected }) || Steam.Lobby.isInLobby;
		private bool IsConnected => (netManager.IsRunning && peer is {ConnectionState: ConnectionState.Connected }) || Steam.Lobby.isInLobby;

		public NetStatistics GetStatistics() => Server.Statistics;

		public List<Entity> requestReceivers = new();

		private ServerSettings savedSettings;

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

			Grouping.ForceClearGroup(true);

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
			WeatherHandler.Stop();

			ClearDroppedItems();

			if(savedSettings != null)
			{
				GameData.ServerXPMod = savedSettings.xpMod;
				GameData.ServerDMGMod = savedSettings.dmgMod;
				GameData.ServerHPMod = savedSettings.hpMod;
				GameData.ServerLootRate = savedSettings.lootMod;
			}
			savedSettings = null;
		}


		public void Update()
		{
			foreach (var player in Players)
			{
				if (player.Value.sceneChanged)
				{
					player.Value.gameObject.SetActive(player.Value.zone == SceneManager.GetActiveScene().name);
					player.Value.sceneChanged = false;

					Logging.LogError($"[Client] Player changed scene");
				}
			}

			if (Steam.Lobby.isInLobby)
			{
				//Steam.Networking.Update();
				return;
			}
			netManager.PollEvents();
			if (!IsConnected) return;

			
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

		private void OnPlayerConnect(short playerID, PlayerConnectionPacket packet, CSteamID peer)
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



				Logging.Log($"Player {charName} connected @{scene} id:{playerID} steam:{peer}");

				OnConnectDelayed(0.25f, playerID, scene);
			}
			else
			{
				Logging.LogError($"Unknown Error: Failed to Create Player.");
			}
		}

		private void OnSimSpawn(short playerID, PlayerConnectionPacket packet)
		{
			if (ClientNPCSyncManager.Instance.NetworkedSims.ContainsKey(playerID))
			{
				Logging.Log($"Already in list? {packet.name}");
				return;
			}

			string scene = packet.scene;
			string charName = packet.name;
			var pos = packet.position;
			var rot = packet.rotation;

			var pl = GameHooks.CreateSim(playerID, pos, rot);
			if (pl != null)
			{
				ClientNPCSyncManager.Instance.NetworkedSims.Add(playerID, pl);
				pl.Init(pos, rot, charName, scene, playerID);
				pl.HandleConnectPacket(packet);

				Logging.Log($"SIM {charName} connected @{scene}");
			}
			else
			{
				Logging.LogError($"Unknown Error: Failed to Create Sim");
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


		private NetPeer _lastPeer = null;
		public CSteamID _lastSteamID;

		public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
		{
			_lastPeer = peer;
			(var packet, var packetType) = OnNetworkHandle(reader, channelNumber, deliveryMethod);
			reader.Recycle();
			if (packet == null) return;

			if (ServerConnectionManager.Instance.IsRunning)
			{
				packet.exclusions.Add(peer);
				//Make sure we only send the packet to whoever should actually receive it
				if (packet.targetPlayerIDs != null && packet.targetPlayerIDs.Count > 0)
				{
					foreach (var p in Players)
					{
						if (!packet.targetPlayerIDs.Contains(p.Key))
							packet.exclusions.Add(p.Value.peer);
					}
				}
				PacketManager.ServerAddPacket(packetType, packet);
			}
		}


		public (BasePacket, PacketType) OnNetworkHandle(NetDataReader reader, byte channelNumber, DeliveryMethod deliveryMethod, bool useSteam=false)
		{
			var psize = reader.RawDataSize;
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
				case PacketType.ITEM_DROP:
					packet = new ItemDropPacket();
					break;
				case PacketType.WEATHER_DATA:
					packet = new WeatherPacket();
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
			//Logging.Log($"Received {packetType} of size {psize}");

			if (packet.GetType() == typeof(BasePacket))
			{
				return (null, 0); //Unknown packet
			}

			if (isPlayerPacket)
			{
				if (packetType == PacketType.ITEM_DROP)
				{
					var pack = (ItemDropPacket)packet;
					if (pack.senderID != LocalPlayerID)
					{
						if (pack.dataTypes.Contains(ItemDropType.DROP))
						{
							var itm = GameData.ItemDB.GetItemByID(pack.itemID);
							if (itm != GameData.PlayerInv.Empty)
								SpawnItem(itm, pack.quality, pack.location, pack.zone, pack.id);
						}

						if (pack.dataTypes.Contains(ItemDropType.DESTROY))
							ClearDroppedItem(pack.id);
						if (pack.dataTypes.Contains(ItemDropType.NEW_QUANTITY))
						{
							UpdateQuantity(pack.id, pack.quality);
						}
					}
				}

				if (packetType == PacketType.WEATHER_DATA)
				{
					var pack = (WeatherPacket)packet;
					if (pack.targetPlayerIDs.Contains(LocalPlayerID))
					{
						WeatherHandler.ReceiveWeatherData(pack.weatherData);
					}
				}
				if (packetType == PacketType.SERVER_GROUP)
				{
					Grouping.HandleServerPacket((ServerGroupPacket)packet);
					return (null, 0);
				}
				if (packet is PlayerMessagePacket messagePacket)
				{
					Logging.HandleMessage(GetPlayerFromID(packet.entityID), messagePacket);
					if (messagePacket.messageType == MessageType.INFO)
						return (null, 0); //don't resend
				}
				//Logging.Log($"{packet.GetType()}");
				short playerID = packet.entityID;
				//if (playerID == LocalPlayerID) return;

				if (playerID != LocalPlayerID || packet.isSim)
				{
					if (packet is PlayerConnectionPacket)
					{
						if (!packet.isSim)
						{
							if(!useSteam)
								OnPlayerConnect(playerID, (PlayerConnectionPacket)packet, _lastPeer);
							else
								OnPlayerConnect(playerID, (PlayerConnectionPacket)packet, _lastSteamID);
						}
						else
						{
							OnSimSpawn(playerID, (PlayerConnectionPacket)packet);
						}
					}
					else
					{

						if (packet is PlayerDataPacket)
						{
							if (((PlayerDataPacket)packet).dataTypes.Contains(PlayerDataType.SCENE) && !packet.isSim)
							{
								if (Players.TryGetValue(playerID, out var pl))
								{
									if (pl.zone != ((PlayerDataPacket)packet).scene)
									{
										OnClientSwapZone?.Invoke(playerID, ((PlayerDataPacket)packet).scene, pl.zone);
									}
								}
							}
						}
						if (Players.ContainsKey(playerID) && !packet.isSim)
							Players[playerID].OnPlayerDataReceive(packet);
						else if (packet.isSim && ClientNPCSyncManager.Instance.NetworkedSims.ContainsKey(playerID))
							ClientNPCSyncManager.Instance.NetworkedSims[playerID].OnSimDataReceive(packet);
					}
				}
			}
			else if (packetType == PacketType.GROUP)
			{
				Grouping.HandleClientPacket((GroupPacket)packet);
				return (null, 0);
			}
			else if (packetType == PacketType.SERVER_INFO)
			{
				//Logging.Log($"{packet.GetType()}");
				if (((ServerInfoPacket)packet).dataTypes.Contains(ServerInfoType.PVP_MODE))
				{
					foreach (var player in Players)
					{
						player.Value.character.MyFaction = ((ServerInfoPacket)packet).pvpMode ? Character.Faction.PC : Character.Faction.Player;
						player.Value.character.BaseFaction = player.Value.character.MyFaction;
					}

					ServerConfig.clientIsPvpEnabled = ((ServerInfoPacket)packet).pvpMode;
				}
				if (((ServerInfoPacket)packet).dataTypes.Contains(ServerInfoType.SERVER_SETTINGS))
				{
					var p = (ServerInfoPacket)packet;
					if (savedSettings == null) //we dont have saved settings, we can apply
					{
						savedSettings = new()
						{
							xpMod = GameData.ServerXPMod,
							dmgMod = GameData.ServerDMGMod,
							hpMod = GameData.ServerHPMod,
							lootMod = GameData.ServerLootRate
						};

						GameData.ServerXPMod = p.serverSettings.xpMod;
						GameData.ServerDMGMod = p.serverSettings.dmgMod;
						GameData.ServerHPMod = p.serverSettings.hpMod;
						GameData.ServerLootRate = p.serverSettings.lootMod;
					}
				}
				if (((ServerInfoPacket)packet).dataTypes.Contains(ServerInfoType.HOST_MODS))
				{
					Logging.Log($"Received {packetType} of size {psize}");
					//We just put it here
					Logging.LogGameMessage($"Connected!");

					var p = (ServerInfoPacket)packet;
					List<ErenshorCoopMod.PluginData> differentPlugins = new();

					//ErenshorCoopMod.PluginData coopMod = new();

					bool isMissingPlugin = false;
					bool coopDiff = false;
					foreach (var plugin in p.plugins)
					{
						var found = false;
						foreach (var ownPlugin in ErenshorCoopMod.loadedPlugins)
						{
							if (plugin.name == ownPlugin.name)
							{
								found = true;
								var cmp = plugin.version.CompareTo(ownPlugin.version);
								if (cmp != 0)
								{
									differentPlugins.Add(new()
									{
										name = plugin.name,
										version = ownPlugin.version,
										other = plugin.version,
										diff = cmp
									});

									if (plugin.name == "Erenshor Coop")
									{
										coopDiff = true;
									}
								}
							}
						}
						if (!found)
							isMissingPlugin = true;
					}



					if (p.plugins.Count != ErenshorCoopMod.loadedPlugins.Count)
					{
						isMissingPlugin = true;

						foreach (var ownPlugin in ErenshorCoopMod.loadedPlugins)
						{
							if (!p.plugins.Any(x => x.name == ownPlugin.name))
							{
								differentPlugins.Add(new()
								{
									name = ownPlugin.name,
									version = ownPlugin.version,
									other = null
								});
							}
						}

						foreach (var plugin in p.plugins)
						{
							if (!ErenshorCoopMod.loadedPlugins.Any(x => x.name == plugin.name))
							{
								differentPlugins.Add(new()
								{
									name = plugin.name,
									version = null,
									other = plugin.version
								});
							}
						}
					}

					if (coopDiff)
					{
						Logging.LogGameMessage($"Your COOP version differs from the hosts, this will cause major issues!", true);
					}
					else if (isMissingPlugin)
					{
						Logging.LogGameMessage($"Your mods differ from the hosts, this could cause issues.", true);
					}
					if (differentPlugins.Count > 0)
					{
						foreach (var plugin in differentPlugins)
						{
							if (plugin.other != null && plugin.version != null)
								Logging.LogGameMessage($"\"{plugin.name}\" You: v{plugin.version.ToString()} - Host: v{plugin.other.ToString()}", true);
							if (plugin.other == null && plugin.version != null)
								Logging.LogGameMessage($"\"{plugin.name}\" You: v{plugin.version.ToString()} - Host: None", true);
							if (plugin.other != null && plugin.version == null)
								Logging.LogGameMessage($"\"{plugin.name}\" You: None - Host: v{plugin.other.ToString()}", true);
						}
					}
				}
				if (((ServerInfoPacket)packet).dataTypes.Contains(ServerInfoType.ZONE_OWNERSHIP))
				{
					OnZoneOwnerChange?.Invoke(((ServerInfoPacket)packet).zoneOwner, ((ServerInfoPacket)packet).zone, ((ServerInfoPacket)packet).playerList);
					return (null, 0);
				}
			}
			else if (packetType == PacketType.SERVER_CONNECT)
			{
				LocalPlayerID = ((ServerConnectPacket)packet).entityID;
				LocalPlayer.playerID = LocalPlayerID;
				LocalPlayer.entityID = LocalPlayerID;
				LocalPlayer.steamID = Steam.Lobby.playerSteamID;
				OnConnect?.Invoke();
				WeatherHandler.Init();

				UI.Connect._feedbackText.text = "Connected!";

				Grouping.ForceClearGroup();
			}
			else if (packetType == PacketType.PLAYER_REQUEST)
			{
				
				var p = (PlayerRequestPacket)packet;

				if (p.dataTypes.Contains(Request.ENTITY_ID))
				{
					var idL = new List<short>();
					for (int i = 0; i < p.requestEntityType.Count; i++)
					{
						var fid = SharedNPCSyncManager.Instance.GetFreeId();
						idL.Add(fid);
					}
					var pa = PacketManager.GetOrCreatePacket<ServerRequestPacket>(p.entityID, PacketType.SERVER_REQUEST);
					pa.AddPacketData(Request.ENTITY_ID, "reqID", idL);
					pa.SetTarget((!useSteam)?GetPlayerFromPeer(_lastPeer):GetPlayerFromSteam(_lastSteamID));
					pa.exclusions.Add(LocalPlayer.peer);
				}
				if (p.dataTypes.Contains(Request.MOD_COMMAND))
				{
					if (!ServerConnectionManager.Instance.IsRunning) return (null, 0);

					string retMes = "";
					if (!Steam.Lobby.isInLobby)
					{
						//Only supported on steam
						retMes = "[Host] Commands are only supported using steam lobbies.";
						PacketManager.GetOrCreatePacket<PlayerMessagePacket>(p.entityID, PacketType.PLAYER_MESSAGE)
						.SetTarget((!useSteam) ? GetPlayerFromPeer(_lastPeer) : GetPlayerFromSteam(_lastSteamID))
						.SetData("message", retMes)
						.SetData("messageType", MessageType.INFO);
						return (null, 0);
					}

					//Make sure sender is a mod
					if (_lastSteamID == null || !ServerConfig.ModeratorList.Contains(_lastSteamID.m_SteamID))
					{
						PacketManager.GetOrCreatePacket<PlayerMessagePacket>(p.entityID, PacketType.PLAYER_MESSAGE)
									.SetTarget((!useSteam) ? GetPlayerFromPeer(_lastPeer) : GetPlayerFromSteam(_lastSteamID))
									.SetData("message", "[Host] Insufficent Permission.")
									.SetData("messageType", MessageType.INFO);
						return (null, 0);
					}

					bool sendtoall = false;
					if (p.commandType > 1 || p.commandType < 0)
					{
						retMes = "[Host] Unknown command.";
					}
					else if (p.commandType == 0) // kick
					{
						Entity selected = null;
						foreach (var pl in Players)
						{
							if (pl.Value.playerName.ToLower() == p.playerName && pl.Value != LocalPlayer) //dont remember if localplayer is in the list but just incase...
							{
								selected = pl.Value;
								break;
							}
						}


						if (selected != null)
						{
							//Make sure it's not a mod
							if (ServerConfig.ModeratorList.Contains(selected.steamID.m_SteamID))
							{
								PacketManager.GetOrCreatePacket<PlayerMessagePacket>(p.entityID, PacketType.PLAYER_MESSAGE)
									.SetTarget((!useSteam) ? GetPlayerFromPeer(_lastPeer) : GetPlayerFromSteam(_lastSteamID))
									.SetData("message", "[Host] Insufficent Permission.")
									.SetData("messageType", MessageType.INFO);
								return (null, 0);
							}

							var res = Steam.Networking.KickPlayer(selected.steamID);
							if (res)
								retMes = $"[Host] Player {selected.entityName} has been kicked.";
							else
								retMes = $"[Host] Could not kick {selected.entityName}.";

							sendtoall = res;
						}
						else
						{
							retMes = $"[Host] Player not found.";
						}
					}
					else if (p.commandType == 1) // ban
					{
						Entity selected = null;
						foreach (var pl in Players)
						{
							if (pl.Value.playerName.ToLower() == p.playerName && pl.Value != LocalPlayer) //dont remember if localplayer is in the list but just incase...
							{
								selected = pl.Value;
								break;
							}
						}


						if (selected != null)
						{
							//Make sure it's not a mod
							if (ServerConfig.ModeratorList.Contains(selected.steamID.m_SteamID))
							{
								PacketManager.GetOrCreatePacket<PlayerMessagePacket>(p.entityID, PacketType.PLAYER_MESSAGE)
									.SetTarget((!useSteam) ? GetPlayerFromPeer(_lastPeer) : GetPlayerFromSteam(_lastSteamID))
									.SetData("message", "[Host] Insufficent Permission.")
									.SetData("messageType", MessageType.INFO);
								return (null, 0);
							}

							var res = Steam.Networking.BanPlayer(selected.steamID);
							if (res)
								retMes = $"[Host] Player {selected.entityName} has been banned.";
							else
								retMes = $"[Host] Could not ban {selected.entityName}.";

							sendtoall = res;
						}
						else
						{
							retMes = $"[Host] Player not found.";
						}
					}

					if (!sendtoall)
					{
						PacketManager.GetOrCreatePacket<PlayerMessagePacket>(p.entityID, PacketType.PLAYER_MESSAGE)
							.SetTarget((!useSteam) ? GetPlayerFromPeer(_lastPeer) : GetPlayerFromSteam(_lastSteamID))
							.SetData("message", retMes)
							.SetData("messageType", MessageType.INFO);
					}
					else
					{

						var pa = PacketManager.GetOrCreatePacket<PlayerMessagePacket>(0, PacketType.PLAYER_MESSAGE)
							.SetData("message", retMes)
							.SetData("messageType", MessageType.INFO);

						//Host message
						Logging.HandleMessage(GetPlayerFromID(packet.entityID), (PlayerMessagePacket)pa);
					}
				}
				return (null, 0);
			}
			else if (packetType == PacketType.SERVER_REQUEST)
			{
				//Logging.Log($"Received {packetType} of size {psize}");
				if (packet.entityID == LocalPlayerID)
				{
					var p = (ServerRequestPacket)packet;
					if (p.dataTypes.Contains(Request.ENTITY_ID))
					{

						var recvs = new List<Entity>(requestReceivers);
						requestReceivers.Clear();
						var idx = 0;
						foreach (var r in recvs)
						{
							if (r != null)
							{
								if (idx >= p.reqID.Count)
								{
									//Logging.LogError($"Error receiving entityID, request failed, not enough IDs.");
									//break;
									r.RequestID(); //request new id
								}
								else
								{
									r.ReceiveRequestID(p.reqID[idx]);
									idx++;
								}
							}
						}

						//requestReceiver?.ReceiveRequestID(p.reqID);
					}
					
				}

				return (null, 0);
			}
			else if (packetType == PacketType.DISCONNECT)
			{
				PlayerDisconnect(packet.entityID);
			}
			else
			{

				var bp = (EntityBasePacket)packet;
				if (!ClientZoneOwnership.isZoneOwner || bp.entityType == EntityType.PET)
				{
					if (packetType == PacketType.ENTITY_SPAWN)
						Logging.Log($"Received {packetType} of size {psize}");

					ClientNPCSyncManager.Instance.OnEntityDataReceive(packet);
				}

				if (ServerConnectionManager.Instance.IsRunning)
				{
					ServerZoneOwnership.HandleEntityPacket(bp);
				}
			}

			return (packet, packetType);
		}


		private ItemIcon savedIcon;
		public void DropItem(Item item, int quantity)
		{
			if (ClientConfig.ItemDropConfirm.Value)
			{
				savedIcon = GameData.MouseSlot;
				GameData.MouseSlot.dragging = false;
				//GameData.MouseSlot.MyItem = GameData.PlayerInv.Empty;
				//GameData.MouseSlot.dragging = false;
				//GameData.MouseSlot.UpdateSlotImage();

				var hasqual = item.RequiredSlot == Item.SlotType.General;
				UI.Main.EnablePrompt($"Are you sure you want to drop {(hasqual?quantity:"")} {item.ItemName}.", () => { ConfirmDrop(item, quantity); }, () => { GameData.MouseSlot.dragging = true;});
			}
			else
			{
				GameData.MouseSlot.SendToTrade();
				_DropItem(item, quantity);
			}
		}

		public static void ConfirmDrop(Item item, int quantity)
		{
			Instance.savedIcon.SendToTrade();
			Instance._DropItem(item, quantity);
		}


		private void _DropItem(Item item, int quantity)
		{
			var pack = PacketManager.GetOrCreatePacket<ItemDropPacket>(LocalPlayerID, PacketType.ITEM_DROP);
			pack.zone = SceneManager.GetActiveScene().name;
			pack.itemID = item.Id;
			pack.quality = quantity;
			pack.id = GameData.CurrentCharacterSlot.CharName + (Random.Range(0, 9999) + Random.Range(0, 9999)).ToString();
			pack.location = LocalPlayer.transform.position + (LocalPlayer.transform.forward * 2f);
			pack.dataTypes.Add(ItemDropType.DROP);
			pack.senderID = LocalPlayerID;

			//Spawn item for self
			SpawnItem(item, quantity, pack.location, pack.zone, pack.id);
		}

		public void SpawnItem(Item item, int quantity, Vector3 location, string zone, string id, bool noRegister=false)
		{
			var go = new GameObject();
			go.transform.position = location + new Vector3(0,0.25f,0); //Small height offset
			var dpitem = go.AddComponent<DroppedItem>();
			dpitem.zone = zone;
			dpitem.item = item;
			dpitem.quality = quantity;
			dpitem.id = id;
			dpitem.Init(noRegister);

		}

		public void SendItemLooted(string id)
		{
			var pack = PacketManager.GetOrCreatePacket<ItemDropPacket>(LocalPlayerID, PacketType.ITEM_DROP);
			pack.dataTypes.Add(ItemDropType.DESTROY);
			pack.id = id;
			pack.senderID = LocalPlayerID;
		}

		public void SendItemQuantityUpdate(string id, int quant)
		{
			var pack = PacketManager.GetOrCreatePacket<ItemDropPacket>(LocalPlayerID, PacketType.ITEM_DROP);
			pack.dataTypes.Add(ItemDropType.NEW_QUANTITY);
			pack.id = id;
			pack.quality = quant;
			pack.senderID = LocalPlayerID;
		}

		public void UpdateQuantity(string id, int quant)
		{
			var itms = FindObjectsOfType<DroppedItem>();
			foreach (var item in itms)
			{
				if (item.id != id) continue;
				item.UpdateQuantity(quant);
				break;
			}

			int idx = -1;
			string keyToRemoveFrom = null;

			foreach (var kvp in Variables.droppedItems)
			{
				foreach (var item in kvp.Value)
				{
					if (item.id != id) continue;
					idx = kvp.Value.IndexOf(item);
					keyToRemoveFrom = kvp.Key;
					break;
				}
				if (idx != -1)
					break;
			}

			if (idx != -1 && keyToRemoveFrom != null)
			{
				var f = Variables.droppedItems[keyToRemoveFrom][idx];
				f.quantity = quant;
				Variables.droppedItems[keyToRemoveFrom][idx] = f;
			}
		}

		public void ClearDroppedItem(string id)
		{
			var itms = FindObjectsOfType<DroppedItem>();
			foreach (var item in itms)
			{
				if (item.id != id) continue;
				if (Variables.lastDroppedItem == item)
				{
					GameData.LootWindow.CloseWindow();
				}
				Destroy(item.gameObject);
				break;
			}

			Variables.ItemDropData? toRemove = null;
			string keyToRemoveFrom = null;

			foreach (var kvp in Variables.droppedItems)
			{
				foreach (var item in kvp.Value)
				{
					if (item.id != id) continue;
					toRemove = item;
					keyToRemoveFrom = kvp.Key;
					break;
				}
				if (toRemove.HasValue)
					break;
			}

			if (toRemove.HasValue && keyToRemoveFrom != null)
			{
				Variables.droppedItems[keyToRemoveFrom].Remove(toRemove.Value);
			}
		}

		public void ClearDroppedItems()
		{
			var itms = FindObjectsOfType<DroppedItem>();
			foreach (var item in itms)
			{
				Destroy(item.gameObject);
			}

			Variables.droppedItems.Clear();
		}

		public Entity GetPlayerFromID(short playerID)
		{
			if (playerID == LocalPlayerID) return LocalPlayer;
			return Players.TryGetValue(playerID, out var play) ? play : null;
		}

		public Entity GetPlayerFromPeer(NetPeer peer)
		{
			if(LocalPlayer.peer == peer) return LocalPlayer;
			foreach (var p in Players)
				if (p.Value.peer == peer)
					return p.Value;

			return null;
		}

		public Entity GetPlayerFromSteam(CSteamID peer)
		{
			if (LocalPlayer.steamID == peer) return LocalPlayer;
			foreach (var p in Players)
				if (p.Value.steamID == peer)
					return p.Value;

			return null;
		}

		public void HandleModCommand(byte cmdType, string plName)
		{
			if (!ServerConnectionManager.Instance.IsRunning) return;

			string retMes = "";
			if (!Steam.Lobby.isInLobby)
			{
				//Only supported on steam
				PlayerMessagePacket pa = new();
				pa.message = "[Host] Commands are only supported using steam lobbies.";
				pa.messageType = MessageType.INFO;

				Logging.HandleMessage(LocalPlayer, pa);
				return;
			}

			bool sendtoall = false;
			if (cmdType > 1 || cmdType < 0)
			{
				retMes = "[Host] Unknown command.";
			}
			else if (cmdType == 0) // kick
			{
				Entity selected = null;
				foreach (var pl in Players)
				{
					if (pl.Value.playerName.ToLower() == plName && pl.Value != LocalPlayer) //dont remember if localplayer is in the list but just incase...
					{
						selected = pl.Value;
						break;
					}
				}


				if (selected != null)
				{
					var res = Steam.Networking.KickPlayer(selected.steamID);
					if (res)
						retMes = $"[Host] Player {selected.entityName} has been kicked.";
					else
						retMes = $"[Host] Could not kick {selected.entityName}.";

					sendtoall = res;
				}
				else
				{
					retMes = $"[Host] Player not found.";
				}
			}
			else if (cmdType == 1) // ban
			{
				Entity selected = null;
				foreach (var pl in Players)
				{
					if (pl.Value.playerName.ToLower() == plName && pl.Value != LocalPlayer) //dont remember if localplayer is in the list but just incase...
					{
						selected = pl.Value;
						break;
					}
				}


				if (selected != null)
				{
					var res = Steam.Networking.BanPlayer(selected.steamID);
					if (res)
						retMes = $"[Host] Player {selected.entityName} has been banned.";
					else
						retMes = $"[Host] Could not ban {selected.entityName}.";

					sendtoall = res;
				}
				else
				{
					retMes = $"[Host] Player not found.";
				}
			}

			if (!sendtoall)
			{
				PlayerMessagePacket pa = new();
				pa.message = retMes;
				pa.messageType = MessageType.INFO;

				Logging.HandleMessage(LocalPlayer, pa);
			}
			else
			{

				var pa = PacketManager.GetOrCreatePacket<PlayerMessagePacket>(0, PacketType.PLAYER_MESSAGE)
					.SetData("message", retMes)
					.SetData("messageType", MessageType.INFO);

				//Host message
				Logging.HandleMessage(LocalPlayer, (PlayerMessagePacket)pa);
			}
		}

		public void OnConnectionRequest(ConnectionRequest request) { request.Accept(); }
		public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }
		public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
	}
}
