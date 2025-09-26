using ErenshorCoop.Client;
using ErenshorCoop.Client.Grouping;
using ErenshorCoop.Server;
using ErenshorCoop.Shared;
using ErenshorCoop.Shared.Packets;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

namespace ErenshorCoop.Modules
{
	[Module("GenericPacketModule")]
	public class GenericPacketModule : Module
	{
		public override void OnLoad()
		{
			/*packetType == PacketType.GROUP ||
								packetType == PacketType.PLAYER_CONNECT ||
								packetType == PacketType.PLAYER_DATA ||
								packetType == PacketType.PLAYER_ACTION ||
								packetType == PacketType.PLAYER_TRANSFORM ||
								packetType == PacketType.PLAYER_MESSAGE ||
								packetType == PacketType.PLAYER_REQUEST ||
								packetType == PacketType.ITEM_DROP ||
								packetType == PacketType.WEATHER_DATA;*/

			RegisterPacket<PlayerConnectionPacket>(PacketType.PLAYER_CONNECT, false, 3);
			RegisterPacket<PlayerDataPacket>(PacketType.PLAYER_DATA, false, 3);
			RegisterPacket<PlayerTransformPacket>(PacketType.PLAYER_TRANSFORM, false, 2);
			RegisterPacket<PlayerActionPacket>(PacketType.PLAYER_ACTION, false, 3);
			RegisterPacket<PlayerMessagePacket>(PacketType.PLAYER_MESSAGE, false, 8);
			RegisterPacket<PlayerRequestPacket>(PacketType.PLAYER_REQUEST, false, 8);
			RegisterPacket<ItemDropPacket>(PacketType.ITEM_DROP, false, 8);
			RegisterPacket<WeatherPacket>(PacketType.WEATHER_DATA, false, 8);

			RegisterPacket<EntitySpawnPacket>(PacketType.ENTITY_SPAWN, true, 5);
			RegisterPacket<EntityActionPacket>(PacketType.ENTITY_ACTION, true, 5);
			RegisterPacket<EntityDataPacket>(PacketType.ENTITY_DATA, true, 5);
			RegisterPacket<EntityTransformPacket>(PacketType.ENTITY_TRANSFORM, true, 4);
			RegisterPacket<ServerConnectPacket>(PacketType.SERVER_CONNECT, true, 3);
			RegisterPacket<ServerDisonnectPacket>(PacketType.DISCONNECT, true, 0);
			RegisterPacket<ServerInfoPacket>(PacketType.SERVER_INFO, true, 0);
			RegisterPacket<ServerRequestPacket>(PacketType.SERVER_REQUEST, true, 0);
		}
		public override (T, PacketType) OnReceiveClientPacket<T>(T packet, PacketType packetType)
		{
			return HandleIncomingPacket(packet, packetType, fromServer: false);
		}
		public override (T, PacketType) OnReceiveServerPacket<T>(T packet, PacketType packetType)
		{
			return HandleIncomingPacket(packet, packetType, fromServer: true);
		}

		private (T, PacketType) HandleIncomingPacket<T>(T packet, PacketType packetType, bool fromServer) where T : BasePacket
		{
			//Logging.Log($"Received packet {packetType} from {(fromServer ? "server" : "client")}");
			switch (packetType)
			{
				case PacketType.SERVER_INFO when packet is ServerInfoPacket serverInfo:
					HandleServerInfo(serverInfo);
					return (null, PacketType.DONT_RESEND);

				case PacketType.SERVER_CONNECT when packet is ServerConnectPacket serverConnect:
					HandleServerConnect(serverConnect);
					return (null, PacketType.DONT_RESEND);

				case PacketType.ITEM_DROP when packet is ItemDropPacket itemDrop:
					HandleItemDrop(itemDrop);
					break;

				case PacketType.WEATHER_DATA when packet is WeatherPacket weather:
					HandleWeatherPacket(weather);
					break;

				case PacketType.PLAYER_REQUEST when packet is PlayerRequestPacket preq:
				if (HandlePlayerRequest(preq) == false)
					return (null, PacketType.DONT_RESEND);
				break;

				case PacketType.SERVER_REQUEST when packet is ServerRequestPacket sreq:
					HandleServerRequest(sreq);
					return (null, PacketType.DONT_RESEND);

				default:
					if (packet is PlayerMessagePacket msg)
					{
						HandlePlayerMessage(msg);
						if (msg.messageType == MessageType.INFO)
							return (null, PacketType.DONT_RESEND); // don't resend
					}
					else if (packetType == PacketType.DISCONNECT)
					{
						ClientConnectionManager.Instance.PlayerDisconnect(packet.entityID);
					}
					else
					{
						if ((packetType == PacketType.PLAYER_CONNECT || packetType == PacketType.PLAYER_ACTION || packetType == PacketType.PLAYER_DATA || packetType == PacketType.PLAYER_TRANSFORM) && (packet.entityID != ClientConnectionManager.Instance.LocalPlayerID || packet.isSim))
						{
							HandlePlayerRelated(packet);
						}
						else
						{
							//Logging.Log($"Received ent packet {packetType}");
							var pb = packet as EntityBasePacket;
							if (!ClientZoneOwnership.isZoneOwner || pb.entityType == EntityType.PET)
								ClientNPCSyncManager.Instance.OnEntityDataReceive(packet);
							if (ServerConnectionManager.Instance.IsRunning)
								ServerZoneOwnership.HandleEntityPacket(pb);
						}

					}
					
				break;
			}

			return (packet, packetType);
		}

		private void HandleWeatherPacket(WeatherPacket weather)
		{
			if (weather.targetPlayerIDs.Contains(ClientConnectionManager.Instance.LocalPlayerID))
				WeatherHandler.ReceiveWeatherData(weather.weatherData);
		}

		private void HandleServerInfo(ServerInfoPacket p)
		{
			// PVP mode
			if (p.dataTypes.Contains(ServerInfoType.PVP_MODE))
			{
				foreach (var player in ClientConnectionManager.Instance.Players)
				{
					player.Value.character.MyFaction = p.pvpMode ? Character.Faction.PC : Character.Faction.Player;
					player.Value.character.BaseFaction = player.Value.character.MyFaction;
				}
				ServerConfig.clientIsPvpEnabled = p.pvpMode;
			}

			// Server settings (apply only if we haven't saved settings yet)
			if (p.dataTypes.Contains(ServerInfoType.SERVER_SETTINGS))
			{
				if (ClientConnectionManager.Instance.savedSettings == null)
				{
					ClientConnectionManager.Instance.savedSettings = new()
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

			// Player list
			if (p.dataTypes.Contains(ServerInfoType.PLAYER_LIST))
			{
				for (int i = 0; i < p.playerInfoList.Count; i++)
				{
					var pl = p.playerInfoList[i];
					var ent = ClientConnectionManager.Instance.GetPlayerFromID(pl.playerID);
					if (ent == null) continue;

					pl.zone = ent.zone;
					pl.name = ent.name;
					if (pl.playerID == ClientConnectionManager.Instance.LocalPlayerID)
					{
						pl.name = GameData.CurrentCharacterSlot.CharName;
						pl.zone = SceneManager.GetActiveScene().name;
					}
					p.playerInfoList[i] = pl;
				}

				Steam.Networking.lastPlayerData = p.playerInfoList;
				ErenshorCoopMod.ModMain.HandleOnConnect();
			}

			// Host mods / plugin diffs
			if (p.dataTypes.Contains(ServerInfoType.HOST_MODS))
			{
				Logging.LogGameMessage($"Connected!");
				var differentPlugins = new List<ErenshorCoopMod.PluginData>();
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
								if (plugin.name == "Erenshor Coop") coopDiff = true;
							}
						}
					}
					if (!found) isMissingPlugin = true;
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

				if (differentPlugins.Count > 0)
				{
					foreach (var plugin in differentPlugins)
					{
						if (plugin.other != null && plugin.version != null)
							Logging.LogGameMessage($"\"{plugin.name}\" You: v{plugin.version} - Host: v{plugin.other}", true);
						else if (plugin.other == null && plugin.version != null)
							Logging.LogGameMessage($"\"{plugin.name}\" You: v{plugin.version} - Host: None", true);
						else if (plugin.other != null && plugin.version == null)
							Logging.LogGameMessage($"\"{plugin.name}\" You: None - Host: v{plugin.other}", true);
					}
				}

				if (coopDiff)
					Logging.LogGameMessage($"Your COOP version differs from the hosts, this will cause major issues!", true);
				else if (isMissingPlugin)
					Logging.LogGameMessage($"Your mods differ from the hosts, this could cause issues.", true);
			}

			// Zone ownership
			if (p.dataTypes.Contains(ServerInfoType.ZONE_OWNERSHIP))
			{
				ClientConnectionManager.Instance.OnZoneOwnerChange?.Invoke(p.zoneOwner, p.zone, p.playerList);
			}
		}

		private void HandleServerConnect(ServerConnectPacket p)
		{
			ClientConnectionManager.Instance.LocalPlayerID = p.entityID;
			ClientConnectionManager.Instance.LocalPlayer.entityID = ClientConnectionManager.Instance.LocalPlayerID;
			ClientConnectionManager.Instance.LocalPlayer.steamID = Steam.Lobby.playerSteamID;
			ClientConnectionManager.Instance.OnConnect?.Invoke();
			WeatherHandler.Init();

			UI.ConnectPanel._feedbackText.text = "Connected!";

			ClientGroup.ForceClearGroup();
		}

		private void HandleItemDrop(ItemDropPacket drop)
		{
			if (drop.senderID == ClientConnectionManager.Instance.LocalPlayerID) return;

			if (drop.dataTypes.Contains(ItemDropType.DROP))
			{
				var item = GameData.ItemDB.GetItemByID(drop.itemID);
				if (item != GameData.PlayerInv.Empty)
					ClientConnectionManager.Instance.SpawnItem(item, drop.quality, drop.location, drop.zone, drop.id);
			}

			if (drop.dataTypes.Contains(ItemDropType.DESTROY))
				ClientConnectionManager.Instance.ClearDroppedItem(drop.id);

			if (drop.dataTypes.Contains(ItemDropType.NEW_QUANTITY))
				ClientConnectionManager.Instance.UpdateQuantity(drop.id, drop.quality);
		}

		private bool HandlePlayerRequest(PlayerRequestPacket p)
		{
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
				pa.SetTarget((!ClientConnectionManager.Instance.useSteam) ? ClientConnectionManager.Instance.GetPlayerFromPeer(ClientConnectionManager.Instance._lastPeer) : ClientConnectionManager.Instance.GetPlayerFromSteam(ClientConnectionManager.Instance._lastSteamID));
				pa.exclusions.Add(ClientConnectionManager.Instance.LocalPlayer.peer);
			}

			if (p.dataTypes.Contains(Request.MOD_COMMAND))
			{
				if (!ServerConnectionManager.Instance.IsRunning) return false;

				string retMes = "";
				if (!Steam.Lobby.isInLobby)
				{
					// Only supported on steam
					retMes = "[Host] Commands are only supported using steam lobbies.";
					PacketManager.GetOrCreatePacket<PlayerMessagePacket>(p.entityID, PacketType.PLAYER_MESSAGE)
						.SetTarget((!ClientConnectionManager.Instance.useSteam) ? ClientConnectionManager.Instance.GetPlayerFromPeer(ClientConnectionManager.Instance._lastPeer) : ClientConnectionManager.Instance.GetPlayerFromSteam(ClientConnectionManager.Instance._lastSteamID))
						.SetData("message", retMes)
						.SetData("messageType", MessageType.INFO);
					return false;
				}

				// permission check
				if (ClientConnectionManager.Instance._lastSteamID == null || !ServerConfig.ModeratorList.Contains(ClientConnectionManager.Instance._lastSteamID.m_SteamID))
				{
					PacketManager.GetOrCreatePacket<PlayerMessagePacket>(p.entityID, PacketType.PLAYER_MESSAGE)
						.SetTarget((!ClientConnectionManager.Instance.useSteam) ? ClientConnectionManager.Instance.GetPlayerFromPeer(ClientConnectionManager.Instance._lastPeer) : ClientConnectionManager.Instance.GetPlayerFromSteam(ClientConnectionManager.Instance._lastSteamID))
						.SetData("message", "[Host] Insufficient Permission.")
						.SetData("messageType", MessageType.INFO);
					return false;
				}

				bool sendtoall = false;
				if (p.commandType > 1 || p.commandType < 0)
				{
					retMes = "[Host] Unknown command.";
				}
				else if (p.commandType == 0) // kick
				{
					retMes = TryKickPlayer(p, out sendtoall);
				}
				else if (p.commandType == 1) // ban
				{
					retMes = TryBanPlayer(p, out sendtoall);
				}

				if (!sendtoall)
				{
					PacketManager.GetOrCreatePacket<PlayerMessagePacket>(p.entityID, PacketType.PLAYER_MESSAGE)
						.SetTarget((!ClientConnectionManager.Instance.useSteam) ? ClientConnectionManager.Instance.GetPlayerFromPeer(ClientConnectionManager.Instance._lastPeer) : ClientConnectionManager.Instance.GetPlayerFromSteam(ClientConnectionManager.Instance._lastSteamID))
						.SetData("message", retMes)
						.SetData("messageType", MessageType.INFO);
				}
				else
				{
					var pa = PacketManager.GetOrCreatePacket<PlayerMessagePacket>(0, PacketType.PLAYER_MESSAGE)
						.SetData("message", retMes)
						.SetData("messageType", MessageType.INFO);

					// Host message
					Logging.HandleMessage(ClientConnectionManager.Instance.GetPlayerFromID(p.entityID), (PlayerMessagePacket)pa);
				}
			}

			if (p.dataTypes.Contains(Request.ENTITY_SPAWN))
			{
				if (p.ownerID == ClientConnectionManager.Instance.LocalPlayerID && SharedNPCSyncManager.Instance.sims.ContainsKey(p.entityReqID))
				{
					SharedNPCSyncManager.Instance.sims[p.entityReqID].SendConnectData();
				}
			}

			return true;
		}

		private void HandleServerRequest(ServerRequestPacket p)
		{
			if (p.entityID != ClientConnectionManager.Instance.LocalPlayerID) return;

			if (p.dataTypes.Contains(Request.ENTITY_ID))
			{
				var recvs = new List<Entity>(ClientConnectionManager.Instance.requestReceivers);
				ClientConnectionManager.Instance.requestReceivers.Clear();
				var idx = 0;
				foreach (var r in recvs)
				{
					if (r != null)
					{
						if (idx >= p.reqID.Count)
						{
							r.RequestID(); // request new id
						}
						else
						{
							r.ReceiveRequestID(p.reqID[idx]);
							idx++;
						}
					}
				}
			}
		}

		private void HandlePlayerMessage(PlayerMessagePacket message)
		{
			var sender = ClientConnectionManager.Instance.GetPlayerFromID(message.sender);
			var entity = ClientConnectionManager.Instance.GetPlayerFromID(message.entityID);

			if (message.messageType == MessageType.BATTLE_LOG)
				Logging.HandleMessage(entity, message, sender);
			else
				Logging.HandleMessage(entity, message);
		}

		private void HandlePlayerRelated(BasePacket packet)
		{
			short playerID = packet.entityID;

			if (packet is PlayerConnectionPacket connPacket)
			{
				if (!packet.isSim)
				{
					if (!ClientConnectionManager.Instance.useSteam)
					{
						ClientConnectionManager.Instance.OnPlayerConnect(playerID, connPacket, ClientConnectionManager.Instance._lastPeer);
					}
					else
					{
						ClientConnectionManager.Instance.OnPlayerConnect(playerID, connPacket, ClientConnectionManager.Instance._lastSteamID);
					}
				}
				else
				{
					ClientConnectionManager.Instance.OnSimSpawn(playerID, connPacket);
				}
			}
			else
			{

				if (packet is PlayerDataPacket dataPacket)
				{
					if (dataPacket.dataTypes.Contains(PlayerDataType.SCENE) && !packet.isSim)
					{
						if (ClientConnectionManager.Instance.Players.TryGetValue(playerID, out var pl))
						{
							if (pl.zone != dataPacket.scene)
							{
								ClientConnectionManager.Instance.OnClientSwapZone?.Invoke(playerID, ((PlayerDataPacket)packet).scene, pl.zone);
							}
						}
					}
				}
				if (ClientConnectionManager.Instance.Players.ContainsKey(playerID) && !packet.isSim)
					ClientConnectionManager.Instance.Players[playerID].OnPlayerDataReceive(packet);
				else if (packet.isSim)
				{
					if (ClientNPCSyncManager.Instance.NetworkedSims.ContainsKey(playerID))
						ClientNPCSyncManager.Instance.NetworkedSims[playerID].OnSimDataReceive(packet);
					else
					{
						if (packet is PlayerTransformPacket ptp)
						{
							//We are receiving packets for a sim that we don't have, request connection packet
							var pa = PacketManager.GetOrCreatePacket<PlayerRequestPacket>(ClientConnectionManager.Instance.LocalPlayerID, PacketType.PLAYER_REQUEST);
							pa.dataTypes.Add(Request.ENTITY_SPAWN);
							pa.ownerID = ptp.ownerID;
							pa.entityReqID = playerID;
						}
					}
				}
			}
		}

		private string TryKickPlayer(PlayerRequestPacket p, out bool sendToAll)
		{
			sendToAll = false;
			string retMes;
			Entity selected = null;
			foreach (var pl in ClientConnectionManager.Instance.Players)
			{
				if (pl.Value.playerName.ToLower() == p.playerName && pl.Value != ClientConnectionManager.Instance.LocalPlayer)
				{
					selected = pl.Value;
					break;
				}
			}

			if (selected != null)
			{
				if (ServerConfig.ModeratorList.Contains(selected.steamID.m_SteamID))
				{
					retMes = "[Host] Insufficient Permission.";
					return retMes;
				}

				var res = Steam.Networking.KickPlayer(selected.steamID);
				if (res)
					retMes = $"[Host] Player {selected.entityName} has been kicked.";
				else
					retMes = $"[Host] Could not kick {selected.entityName}.";

				sendToAll = res;
			}
			else
			{
				retMes = $"[Host] Player not found.";
			}

			return retMes;
		}

		private string TryBanPlayer(PlayerRequestPacket p, out bool sendToAll)
		{
			sendToAll = false;
			string retMes;
			Entity selected = null;
			foreach (var pl in ClientConnectionManager.Instance.Players)
			{
				if (pl.Value.playerName.ToLower() == p.playerName && pl.Value != ClientConnectionManager.Instance.LocalPlayer)
				{
					selected = pl.Value;
					break;
				}
			}

			if (selected != null)
			{
				if (ServerConfig.ModeratorList.Contains(selected.steamID.m_SteamID))
				{
					retMes = "[Host] Insufficient Permission.";
					return retMes;
				}

				var res = Steam.Networking.BanPlayer(selected.steamID);
				if (res)
					retMes = $"[Host] Player {selected.entityName} has been banned.";
				else
					retMes = $"[Host] Could not ban {selected.entityName}.";

				sendToAll = res;
			}
			else
			{
				retMes = $"[Host] Player not found.";
			}

			return retMes;
		}

	}
}
