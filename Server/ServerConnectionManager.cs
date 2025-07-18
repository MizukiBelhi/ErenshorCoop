using ErenshorCoop.Shared;
using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using ErenshorCoop.Client;
using ErenshorCoop.Shared.Packets;
using UnityEngine;
using UnityEngine.SceneManagement;
using Steamworks;

namespace ErenshorCoop.Server
{
	public class ServerConnectionManager : MonoBehaviour, INetEventListener
	{
		private NetManager netManager;

		protected short PlayerIDNum = 0;



		//Callbacks
		public Action<short, NetPeer> OnClientConnect;
		public Action<short, NetPeer> OnClientSwapZone;
		public Action OnCloseServer;
		public Action OnHost;

		private readonly Dictionary<PacketType, List<Delegate>> Callbacks = new();

		public static ServerConnectionManager Instance;

		public bool IsRunning => netManager.IsRunning || (Steam.Lobby.isLobbyHost && Steam.Networking.isHosting);

		public NetStatistics GetStatistics() => netManager.Statistics;

		public void Awake()
		{
			if (Instance != null) Destroy(gameObject);

			ErenshorCoopMod.OnGameMenuLoad += OnGameMenuLoad;

			Instance = this;
			netManager = new(this)
			{
				NatPunchEnabled = true,
				ChannelsCount = Variables.MaxChannelCount,
				EnableStatistics = true
			};
		}

		public bool StartHost(int port)
		{
			Disconnect();

			if (netManager.Start(port))
			{
				Logging.Log($"Hosting on {GetLocalIpAddress()}:{port}");
				Logging.LogGameMessage($"[Server] Hosting.");
				OnHost?.Invoke();
				return true;
			}
			else
			{
				//Logging.LogGameMessage($"[Server] There was an issue starting the server.");
				//Logging.LogError($"[Server] There was an issue starting the server.");
				Disconnect();
				return false;
			}
		}


		public void OnGameMenuLoad(Scene _)
		{
			Disconnect();
		}

		public void Update()
		{
			netManager?.PollEvents();

		}

		public void OnDestroy()
		{
			ErenshorCoopMod.OnGameMenuLoad -= OnGameMenuLoad;
			Disconnect();
		}

		public void Disconnect()
		{
			Logging.Log($"[Server] Closing Server.");
			if (IsRunning)
				Logging.LogGameMessage($"[Server] Closing Server.");

			netManager.DisconnectAll();
			netManager.Stop();
			//Entities.Clear();
			OnCloseServer?.Invoke();
			PlayerIDNum = 0;
		}

		private string GetLocalIpAddress()
		{
			var localIp = string.Empty;

			var host = Dns.GetHostEntry(Dns.GetHostName());
			var i = 0;
			foreach (var ip in host.AddressList)
			{
				if (ip.AddressFamily != AddressFamily.InterNetwork) continue;

				localIp += (i>0?",":"")+ip;
				i++;
			}

			return localIp;
		}




		public void OnPeerConnected(NetPeer peer)
		{
			Logging.Log($"Peer Connected: {peer.Id} {peer.Address}");

			++PlayerIDNum;
			while (ClientConnectionManager.Instance.Players.ContainsKey(PlayerIDNum) && PlayerIDNum != 0)
			{
				++PlayerIDNum;
			}

			var p = PacketManager.GetOrCreatePacket<ServerInfoPacket>(0, PacketType.SERVER_INFO);
			p.AddPacketData(ServerInfoType.PVP_MODE, "pvpMode", ServerConfig.IsPVPEnabled.Value);
			p.SetPeerTarget(peer);
			p.dataTypes.Add(ServerInfoType.SERVER_SETTINGS);
			p.serverSettings = new()
			{
				xpMod = GameData.ServerXPMod,
				dmgMod = GameData.ServerDMGMod,
				hpMod = GameData.ServerHPMod,
				lootMod = GameData.ServerLootRate
			};
			p.dataTypes.Add(ServerInfoType.HOST_MODS);
			p.plugins = ErenshorCoopMod.loadedPlugins;
			PacketManager.GetOrCreatePacket<ServerConnectPacket>(PlayerIDNum, PacketType.SERVER_CONNECT).SetPeerTarget(peer);
		}

		public void OnPeerConnected(CSteamID steamID)
		{
			++PlayerIDNum;
			while (ClientConnectionManager.Instance.Players.ContainsKey(PlayerIDNum) && PlayerIDNum != 0)
			{
				++PlayerIDNum;
			}

			var p = PacketManager.GetOrCreatePacket<ServerInfoPacket>(0, PacketType.SERVER_INFO);
			p.AddPacketData(ServerInfoType.PVP_MODE, "pvpMode", ServerConfig.IsPVPEnabled.Value);
			p.SetSteamTarget(steamID);
			p.dataTypes.Add(ServerInfoType.SERVER_SETTINGS);
			p.serverSettings = new()
			{
				xpMod = GameData.ServerXPMod,
				dmgMod = GameData.ServerDMGMod,
				hpMod = GameData.ServerHPMod,
				lootMod = GameData.ServerLootRate
			};
			p.dataTypes.Add(ServerInfoType.HOST_MODS);
			p.plugins = ErenshorCoopMod.loadedPlugins;
			p.dataTypes.Add(ServerInfoType.PLAYER_LIST);
			p.playerInfoList = Steam.Networking.lastPlayerData;
			PacketManager.GetOrCreatePacket<ServerConnectPacket>(PlayerIDNum, PacketType.SERVER_CONNECT).SetSteamTarget(steamID);
		}

		public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
		{
			Logging.Log($"Client Disconnected: {peer.Id} {peer.Address}");
			short playerID = -1;

			foreach(var p in ClientConnectionManager.Instance.Players)
				if (Equals(p.Value.peer, peer))
					playerID = p.Value.playerID;

			if (playerID == -1) return;


			//Also remove player on host, because we're not receiving the packet
			ClientConnectionManager.Instance.PlayerDisconnect(playerID);
			
			PacketManager.GetOrCreatePacket<ServerDisonnectPacket>(playerID, PacketType.DISCONNECT);
		}

		public void OnPeerDisconnected(CSteamID peer, string disconnectInfo)
		{
			Logging.Log($"Client Disconnected: {peer.m_SteamID} Reason: {disconnectInfo}");
			short playerID = -1;

			foreach (var p in ClientConnectionManager.Instance.Players)
				if (Equals(p.Value.steamID, peer))
					playerID = p.Value.playerID;

			if (playerID == -1) return;


			//Also remove player on host, because we're not receiving the packet
			ClientConnectionManager.Instance.PlayerDisconnect(playerID);

			PacketManager.GetOrCreatePacket<ServerDisonnectPacket>(playerID, PacketType.DISCONNECT);
		}

		public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
		{
			if (socketError == SocketError.Success) return;

			Logging.LogGameMessage($"[Server] Error: {socketError}");

			Disconnect();
		}

		

		public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
		{
			//Let the host client handle this..
			ClientConnectionManager.Instance.OnNetworkReceive(peer, reader, channelNumber, deliveryMethod);
		}


		public void OnConnectionRequest(ConnectionRequest request) { request.Accept(); }
		public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }
		public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
	}
}
