using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text.RegularExpressions;
using ErenshorCoop.Client;
using ErenshorCoop.Server;
using ErenshorCoop.Shared;
using ErenshorCoop.Shared.Packets;
using ErenshorCoop.UI;
using LiteNetLib;
using LiteNetLib.Utils;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ErenshorCoop.Steam
{
	public static class Networking
	{

		private static HSteamListenSocket _listenSocket = HSteamListenSocket.Invalid;
		private static HSteamNetConnection _connection = HSteamNetConnection.Invalid;


		private static Callback<SteamNetConnectionStatusChangedCallback_t> _connectionStatusChanged;

		private static Dictionary<CSteamID, HSteamNetConnection> _steamIdToConnection = new Dictionary<CSteamID, HSteamNetConnection>();
		private static Dictionary<HSteamNetConnection, CSteamID> _connectionToSteamId = new Dictionary<HSteamNetConnection, CSteamID>();


		private static bool isUsingDirectConnect = false;

		public static bool isHosting => _listenSocket != HSteamListenSocket.Invalid;

		public static void StartHost(int port, bool directConnect=false)
		{
			SteamNetworkingUtils.InitRelayNetworkAccess();

			if (_listenSocket != HSteamListenSocket.Invalid)
			{
				Logging.Log("Steam Socket host already started.");
				return;
			}

			SteamNetworkingIPAddr address = new SteamNetworkingIPAddr();
			address.Clear();

			address.m_port = (ushort)port;

			if(directConnect)
				_listenSocket = SteamNetworkingSockets.CreateListenSocketIP(ref address, 0, null);
			else
				_listenSocket = SteamNetworkingSockets.CreateListenSocketP2P(port, 0, null);

			if (_listenSocket == HSteamListenSocket.Invalid)
			{
				Logging.LogError($"Failed to create Steam Socket.");
				Lobby.LeaveLobby();
				return;
			}

			isUsingDirectConnect = directConnect;

			ClientConnectionManager.Instance.LocalPlayer.steamID = Lobby.playerSteamID;
			ClientConnectionManager.Instance.LocalPlayer.entityID = 0;
			ClientConnectionManager.Instance.LocalPlayerID = 0;
			Logging.Log($"Steam Socket host started. Listening on: {_listenSocket.m_HSteamListenSocket}:{port}.");
			ClientConnectionManager.Instance.OnConnect?.Invoke();
			WeatherHandler.Init();
			lastPlayerUpdate = 0;
			CollectPlayerData(true);
			PlayerPanel.RefreshPlayerInfo(true);

			_connectionStatusChanged = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnSteamNetConnectionStatusChanged);

			Logging.LogGameMessage($"Hosting.");
		}

		public static bool ConnectToPeer(CSteamID peerSteamID, int port, string ip="", bool directConnect=false)
		{
			SteamNetworkingUtils.InitRelayNetworkAccess();

			if (_connection != HSteamNetConnection.Invalid)
			{
				Logging.Log("Already connected or trying to connect to a peer.");
				return false;
			}

			lastPlayerUpdate = 0;
			_connectionStatusChanged = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnSteamNetConnectionStatusChanged);

			SteamNetworkingIdentity identity = new();
			identity.SetSteamID(peerSteamID);

			if (directConnect)
			{
				//convert localhost to 127.0.0.1
				if (ip.Equals("localhost"))
					ip = "127.0.0.1";

				IPAddress ipAddress = IPAddress.Parse(ip);
				uint ipUintNetworkOrder = BitConverter.ToUInt32(ipAddress.GetAddressBytes(), 0);
				uint ipUintHostOrder = (uint)IPAddress.NetworkToHostOrder((int)ipUintNetworkOrder);

				SteamNetworkingIPAddr ipaddr = new();
				ipaddr.SetIPv4(ipUintHostOrder, (ushort)port);

				_connection = SteamNetworkingSockets.ConnectByIPAddress(ref ipaddr, 0, null);
			}
			else
				_connection = SteamNetworkingSockets.ConnectP2P(ref identity, port, 0, null);


			if (_connection == HSteamNetConnection.Invalid)
			{
				ESteamNetworkingConnectionState state;
				SteamNetConnectionInfo_t info;
				SteamNetworkingSockets.GetConnectionInfo(_connection, out info);
				state = info.m_eState;

				Logging.Log($"Connection failed, state: {state}, reason: {info.m_eEndReason}, debug: {info.m_szEndDebug}");

				//Logging.LogError($"Failed to create Steam Sockets P2P connection to {peerSteamID}.");
				if (_connectionStatusChanged != null)
				{
					_connectionStatusChanged.Unregister();
					_connectionStatusChanged.Dispose();
					_connectionStatusChanged = null;
				}
				return false;
			}

			if(!directConnect)
				Logging.Log($"Attempting to connect to peer: {peerSteamID} on connection: {_connection.m_HSteamNetConnection}.");
			else
				Logging.Log($"Attempting to connect to peer: {_connection.m_HSteamNetConnection}.");

			isUsingDirectConnect = directConnect;

			

			return true;
		}

		public static void Cleanup()
		{
			foreach (var conn in _steamIdToConnection)
			{
				SteamNetworkingSockets.CloseConnection(conn.Value, 0, "Shutting down", false);
			}

			if (_listenSocket != HSteamListenSocket.Invalid)
			{
				SteamNetworkingSockets.CloseListenSocket(_listenSocket);
				Logging.Log("Steam listen socket closed.");
			}

			_steamIdToConnection.Clear();
			_connectionToSteamId.Clear();


			if (_connection != HSteamNetConnection.Invalid)
			{
				SteamNetworkingSockets.CloseConnection(_connection, 0, "Client disconnecting", false);
				_connection = HSteamNetConnection.Invalid;
			}


			if (_connectionStatusChanged != null)
			{
				_connectionStatusChanged.Unregister();
				_connectionStatusChanged.Dispose();
				_connectionStatusChanged = null;
			}

			_listenSocket = HSteamListenSocket.Invalid;
			_connection = HSteamNetConnection.Invalid;

			ServerConnectionManager.Instance?.Disconnect();
			ClientConnectionManager.Instance?.Disconnect();

			if(ClientConnectionManager.Instance.LocalPlayer != null)
				ClientConnectionManager.Instance.LocalPlayer.hasSentConnect = false;

			Lobby.LeaveLobby();
		}

		public static int ConvertDeliveryMethod(DeliveryMethod deliveryMethod)
		{
			if (deliveryMethod == DeliveryMethod.Unreliable)
				return Constants.k_nSteamNetworkingSend_Unreliable;
			return Constants.k_nSteamNetworkingSend_Reliable;
		}

		public static DeliveryMethod ConvertDeliveryMethod(int sendFlags)
		{
			if (sendFlags == Constants.k_nSteamNetworkingSend_Unreliable)
				return DeliveryMethod.Unreliable;
			return DeliveryMethod.ReliableOrdered;
		}


		public static void SendPacket(HSteamNetConnection connection, byte[] data, int channel, int sendFlags)
		{

			IntPtr pData = Marshal.AllocHGlobal(data.Length);
			try
			{
				Marshal.Copy(data, 0, pData, data.Length);

				long messageNumber;

				EResult res = SteamNetworkingSockets.SendMessageToConnection(
					connection,
					pData,
					(uint)data.Length,
					(int)sendFlags,
					out messageNumber
				);

				if (res != EResult.k_EResultOK)
				{
					Logging.LogError($"Failed to send packet to connection {connection.m_HSteamNetConnection}, Result: {res}");
				}
			}
			finally
			{
				Marshal.FreeHGlobal(pData);
			}
		}

		public static bool SendPacket(CSteamID receiverSteamID, byte[] data, int channel, int sendFlags)
		{
			if (_steamIdToConnection.TryGetValue(receiverSteamID, out HSteamNetConnection connection))
			{
				SendPacket(connection, data, channel, sendFlags);
				return true;
			}
			else
			{
				Logging.Log($"Attempted to send packet to unknown SteamID: {receiverSteamID}. Not connected?");
				return false;
			}
		}


		public static void Update()
		{
			//if (!Lobby.isInLobby) return;

			SteamAPI.RunCallbacks();

			try
			{
				if (_listenSocket != HSteamListenSocket.Invalid)
				{
					foreach (var c in _steamIdToConnection)
					{
						IntPtr[] pOutMessages = new IntPtr[32];
						int numMessages = SteamNetworkingSockets.ReceiveMessagesOnConnection(c.Value, pOutMessages, 32);

						for (int i = 0; i < numMessages; i++)
						{
							SteamNetworkingMessage_t msg = (SteamNetworkingMessage_t)Marshal.PtrToStructure(pOutMessages[i], typeof(SteamNetworkingMessage_t));
							ProcessIncomingMessage(msg);
							SteamNetworkingMessage_t.Release(pOutMessages[i]);
						}
					}
				}

				if (_connection != HSteamNetConnection.Invalid)
				{
					IntPtr[] pOutMessages = new IntPtr[32];
					int numMessages = SteamNetworkingSockets.ReceiveMessagesOnConnection(_connection, pOutMessages, 32);

					for (int i = 0; i < numMessages; i++)
					{
						SteamNetworkingMessage_t msg = (SteamNetworkingMessage_t)Marshal.PtrToStructure(pOutMessages[i], typeof(SteamNetworkingMessage_t));
						ProcessIncomingMessage(msg);
						SteamNetworkingMessage_t.Release(pOutMessages[i]);
					}
				}
			}
			catch{ }
		}

		private static void ProcessIncomingMessage(SteamNetworkingMessage_t msg)
		{
			try
			{
				byte[] data = new byte[msg.m_cbSize];
				Marshal.Copy(msg.m_pData, data, 0, msg.m_cbSize);

				NetDataReader reader = new NetDataReader();
				reader.SetSource(data);

				//SteamNetworkingIdentity identity = new SteamNetworkingIdentity();
				SteamNetworkingSockets.GetConnectionInfo(msg.m_conn, out SteamNetConnectionInfo_t info);
				CSteamID senderSteamID = info.m_identityRemote.GetSteamID();

				if (senderSteamID.IsValid())
				{
					ClientConnectionManager.Instance._lastSteamID = senderSteamID;

					(var packet, var packetType) = ClientConnectionManager.Instance.OnNetworkHandle(reader, (byte)msg.m_nChannel, ConvertDeliveryMethod(msg.m_nFlags), true);


					if (ServerConnectionManager.Instance.IsRunning)
					{
						if (packet != null)
						{
							// Ensure the sender is excluded from the relay
							packet.steamExclusions.Add(senderSteamID);

							// Make sure we only send the packet to whoever should actually receive it
							if (packet.targetPlayerIDs != null && packet.targetPlayerIDs.Count > 0)
							{
								foreach (var p in ClientConnectionManager.Instance.Players)
								{
									if (!packet.targetPlayerIDs.Contains(p.Key))
										packet.steamExclusions.Add(p.Value.steamID);
								}
							}
							PacketManager.ServerAddPacket(packetType, packet);
						}
					}
				}
				else
				{
					Logging.Log("Received message from invalid SteamID. This should not happen frequently.");
				}
			}
			catch (Exception ex)
			{
				Logging.LogError($"Error processing incoming Steam message: {ex.Message}\n{ex.StackTrace}");
			}
		}


		private static void OnSteamNetConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t pCallback)
		{
			HSteamNetConnection hConn = pCallback.m_hConn;
			ESteamNetworkingConnectionState oldState = pCallback.m_eOldState;
			SteamNetConnectionInfo_t info = pCallback.m_info;

			string debugInfo = $"Connection {hConn.m_HSteamNetConnection} status changed from {oldState} to {info.m_eState}";

			CSteamID remoteSteamID = info.m_identityRemote.GetSteamID();
			if (remoteSteamID.IsValid())
			{
				debugInfo += $" (Remote SteamID: {remoteSteamID})";
			}
			else
			{
				Logging.LogError($"{debugInfo}");
				return;
			}

			

			switch (info.m_eState)
			{
				case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_None:
				// This state is not usually seen in callbacks
				break;

				case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting:
					if (_listenSocket != HSteamListenSocket.Invalid)
					{
						SteamNetworkingSockets.GetConnectionInfo(hConn, out SteamNetConnectionInfo_t liveInfo);
						if(Lobby.isInLobby && Lobby.GetLobbyMembersSteamID().Contains(remoteSteamID) && liveInfo.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting)
						{
							if(ServerConfig.BanList.Contains(remoteSteamID.m_SteamID))
							{
								SteamNetworkingSockets.CloseConnection(hConn, 0, "Banned from lobby", false);
								break;
							}
							
							EResult res = SteamNetworkingSockets.AcceptConnection(hConn);
							if (res == EResult.k_EResultOK)
							{
								_steamIdToConnection[remoteSteamID] = hConn;
								_connectionToSteamId[hConn] = remoteSteamID;
								Logging.Log($"Accepted new connection from {remoteSteamID}.");

								ServerConnectionManager.Instance?.OnPeerConnected(remoteSteamID);
							}
							else
							{
								Logging.LogError($"Failed to accept connection from {remoteSteamID}. Result: {res}");
								SteamNetworkingSockets.CloseConnection(hConn, 0, "Failed to accept connection", false);
							}
						}
						else
						{
							SteamNetworkingSockets.CloseConnection(hConn, 0, "Not in lobby.", false);
						}
					}
					break;

				case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
					Logging.Log($"Connected: {debugInfo}");

					if (_connection != HSteamNetConnection.Invalid)
					{
						if (hConn == _connection)
						{
							_steamIdToConnection[remoteSteamID] = hConn;
							_connectionToSteamId[hConn] = remoteSteamID;

						Logging.Log($"Successfully connected to host: {remoteSteamID}.");
						}
					}
					if (_listenSocket != HSteamListenSocket.Invalid)
					{
							
						_steamIdToConnection[remoteSteamID] = hConn;
						_connectionToSteamId[hConn] = remoteSteamID;

						Logging.Log($"CLIENT: {remoteSteamID} - {Lobby.playerSteamID}.");
					}
					break;

				case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
				case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
					Logging.Log($"Disconnected: {debugInfo}. Reason: {info.m_szEndDebug}, OldState: {oldState}, RemoteState: {info.m_eState}");

					//if (oldState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected)
					{
						if (_listenSocket != HSteamListenSocket.Invalid)
						{
							_steamIdToConnection.Remove(remoteSteamID);
							_connectionToSteamId.Remove(hConn);
							Logging.Log($"Client {remoteSteamID} disconnected.");

							ServerConnectionManager.Instance?.OnPeerDisconnected(remoteSteamID, info.m_szEndDebug);
						}
						else if (hConn == _connection)
						{
							_connection = HSteamNetConnection.Invalid;

							Logging.Log($"Disconnected from host: {remoteSteamID}.");
								
							SteamNetworkingSockets.CloseConnection(hConn, 0, "Connection ended", false);
							ClientConnectionManager.Instance?.Disconnect();
							UI.ConnectPanel._feedbackText.text = $"{info.m_eState}";
							UI.ConnectPanel.EnableButtons();
							Cleanup();
							Logging.LogGameMessage($"Disconnected. Reason: {info.m_szEndDebug}.");
						}
					}
					SteamNetworkingSockets.CloseConnection(hConn, 0, "Connection ended", false);


				break;
			}
		}

		public static CSteamID GetSteamIDFromConnection(HSteamNetConnection connection)
		{
			if (_connectionToSteamId.TryGetValue(connection, out CSteamID steamId))
			{
				return steamId;
			}
			return CSteamID.Nil;
		}

		public static HSteamNetConnection GetConnectionFromSteamID(CSteamID steamId)
		{
			if (_steamIdToConnection.TryGetValue(steamId, out HSteamNetConnection connection))
			{
				return connection;
			}
			return HSteamNetConnection.Invalid;
		}

		public static bool KickPlayer(CSteamID steamid)
		{
			HSteamNetConnection con;
			if ((con = GetConnectionFromSteamID(steamid)) != HSteamNetConnection.Invalid)
			{
				SteamNetworkingSockets.CloseConnection(con, 0, "Kicked from lobby", false);
				_steamIdToConnection.Remove(steamid);
				_connectionToSteamId.Remove(con);

				ServerConnectionManager.Instance?.OnPeerDisconnected(steamid, "Kicked from lobby");

				return true;
			}
			return false;
		}

		public static bool BanPlayer(CSteamID steamid)
		{
			HSteamNetConnection con;
			if ((con = GetConnectionFromSteamID(steamid)) != HSteamNetConnection.Invalid)
			{
				SteamNetworkingSockets.CloseConnection(con, 0, "Banned from lobby", false);
				var newList = ServerConfig.BanList.Append(steamid.m_SteamID);
				ServerConfig.BanListRaw.Value = string.Join(",", newList);
				ServerConfig.BanListRaw.ConfigFile.Save();

				SteamNetworkingSockets.CloseConnection(con, 0, "Kicked from lobby", false);
				_steamIdToConnection.Remove(steamid);
				_connectionToSteamId.Remove(con);

				ServerConnectionManager.Instance?.OnPeerDisconnected(steamid, "Kicked from lobby");
				return true;
			}
			return false;
		}

		private static float lastUpdate = 0;
		private static SteamConnectionStats _lastStats = new();
		public static SteamConnectionStats GetConnectionInfo()
		{
			if (lastUpdate + 1 > Time.time) return _lastStats;

			lastUpdate = Time.time;
			if (_connection != HSteamNetConnection.Invalid)
			{
				int result = SteamNetworkingSockets.GetDetailedConnectionStatus(_connection, out string status, 2048);

				if (result != -1 && !string.IsNullOrEmpty(status))
				{
					_lastStats = SteamConnectionStats.FromDetailedStatus(status);
					return _lastStats;
				}
			}
			if(_listenSocket != HSteamListenSocket.Invalid)
			{
				SteamConnectionStats stats = new();
				stats.PingMs = 0;
				if (_steamIdToConnection.Count > 0)
				{
					foreach (var usr in _steamIdToConnection)
					{
						SteamNetConnectionRealTimeStatus_t _status = new();
						SteamNetConnectionRealTimeLaneStatus_t laneStatus = new();
						var res = SteamNetworkingSockets.GetConnectionRealTimeStatus(usr.Value, ref _status, 0, ref laneStatus);

						stats.SentKBps += _status.m_flOutBytesPerSec / 1024f;
						stats.RecvKBps += _status.m_flInBytesPerSec / 1024f;
						stats.DroppedPacketPercent += (1.0f - _status.m_flConnectionQualityRemote) * 100.0f;
					}
					stats.DroppedPacketPercent /= _steamIdToConnection.Count;
				}

				_lastStats = stats;
				return _lastStats;
			}

			return new();
		}

		public struct PlayerData
		{
			public string name;
			public int ping;
			public string zone;
			public short playerID;
			public bool isMod;
			public bool isHost;
			public bool isDev;
		}

		private static float lastPlayerUpdate = 0;
		public static List<PlayerData> lastPlayerData = new();
		public static bool CollectPlayerData(bool force = false)
		{
			if (!force)
			{
				if (!ClientConnectionManager.Instance.IsRunning) return false;
				if (!ServerConnectionManager.Instance.IsRunning) return false;
				if (!Lobby.isInLobby) return false;
				if (!Lobby.isLobbyHost) return false;

				if (lastPlayerUpdate + 5 > Time.time) return false;
			}
			lastPlayerUpdate = Time.time;

			lastPlayerData.Clear();

			var isDev = SteamUser.GetSteamID().m_SteamID == 76561198852628904;
			lastPlayerData.Add(new() { name = GameData.CurrentCharacterSlot.CharName, ping = 0, zone = SceneManager.GetActiveScene().name, playerID = ClientConnectionManager.Instance.LocalPlayerID, isMod = true, isDev = isDev, isHost = true });

			foreach (var usr in _steamIdToConnection)
			{
				var ent = ClientConnectionManager.Instance.GetPlayerFromSteam(usr.Key);
				if (ent == null) continue;

				SteamNetConnectionRealTimeStatus_t _status = new();
				SteamNetConnectionRealTimeLaneStatus_t laneStatus = new();
				var res = SteamNetworkingSockets.GetConnectionRealTimeStatus(usr.Value, ref _status, 0, ref laneStatus);

				isDev = usr.Key.m_SteamID == 76561198852628904;

				lastPlayerData.Add(new() { name = ent.entityName, ping = _status.m_nPing, zone = ent.zone, playerID = ent.entityID, isMod = ServerConfig.ModeratorList.Contains(ent.steamID.m_SteamID), isDev = isDev });
			}

			var p = PacketManager.GetOrCreatePacket<ServerInfoPacket>(0, PacketType.SERVER_INFO);

			p.dataTypes.Add(ServerInfoType.PLAYER_LIST);
			p.playerInfoList = lastPlayerData;
#if DEBUG
			//lastPlayerData.Add(new() { name = "Scrubby", ping = UnityEngine.Random.Range(200,500), zone = SceneManager.GetActiveScene().name, playerID = 1, isHost = true });
			//lastPlayerData.Add(new() { name = "Cyndara", ping = UnityEngine.Random.Range(35, 65), zone = SceneManager.GetActiveScene().name, playerID = 2, isMod = true });
			//lastPlayerData.Add(new() { name = "Behox", ping = UnityEngine.Random.Range(101, 200), zone = SceneManager.GetActiveScene().name, playerID = 0 });
			//lastPlayerData.Add(new() { name = "Turk", ping = UnityEngine.Random.Range(1, 5), zone = SceneManager.GetActiveScene().name, playerID = 0 });
			//lastPlayerData.Add(new() { name = "Brian", ping = UnityEngine.Random.Range(998, 999), zone = SceneManager.GetActiveScene().name, playerID = 0 });
#endif
			ErenshorCoopMod.ModMain.HandleOnConnect();
			return true;
		}
	}


	public class SteamConnectionStats
	{
		public float PingMs = 0;
		public float DroppedPacketPercent = 0;
		public float SentKBps = 0;
		public float RecvKBps = 0;

		public static SteamConnectionStats FromDetailedStatus(string status)
		{
			var stats = new SteamConnectionStats();

			var pingMatch = Regex.Match(status, @"Ping:(\d+(?:\.\d+)?)ms");
			if (pingMatch.Success && float.TryParse(pingMatch.Groups[1].Value, out float ping))
				stats.PingMs = ping;

			var droppedMatch = Regex.Match(status, @"Dropped:(\d+(?:\.\d+)?)%");
			if (droppedMatch.Success && float.TryParse(droppedMatch.Groups[1].Value, out float dropped))
				stats.DroppedPacketPercent = dropped;

			var sentMatch = Regex.Match(status, @"Sent:\s*[\d\.]+ pkts/sec\s+([\d\.]+) K/sec");
			if (sentMatch.Success && float.TryParse(sentMatch.Groups[1].Value, out float sent))
				stats.SentKBps = sent;

			var recvMatch = Regex.Match(status, @"Recv:\s*[\d\.]+ pkts/sec\s+([\d\.]+) K/sec");
			if (recvMatch.Success && float.TryParse(recvMatch.Groups[1].Value, out float recv))
				stats.RecvKBps = recv;

			return stats;
		}
	}
}
