using System.Collections;
using System.Collections.Generic;
using ErenshorCoop.Client;
using ErenshorCoop.Shared;
using ErenshorCoop.Shared.Packets;
using LiteNetLib;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ErenshorCoop.Server
{
	public static class ServerZoneOwnership
	{
		private static readonly Dictionary<string, List<short>> _zoneMembers = new();
		private static readonly Dictionary<string, short> _zoneOwners = new();
		private static Queue<(short peerPlayer, short newOwnerID, string zone, Entity ent)> packetQueue = new();

		private static bool isInUse = false;


		private static string hostPreviousScene = "";
		public static bool hostIsTakingOver = false;

		public static void OnConnect()
		{
			_zoneOwners.Clear();
			_zoneMembers.Clear();
			packetQueue.Clear();
			if (ServerConnectionManager.Instance.IsRunning)
			{
				ClientConnectionManager.Instance.OnClientConnect += OnClientChangeZone;
				ClientConnectionManager.Instance.OnClientSwapZone += OnClientChangeZone;
				ClientConnectionManager.Instance.OnPlayerDisconnect += OnPlayerDisconnect;
				ClientConnectionManager.Instance.OnConnect += OnHostConnect;
				ErenshorCoopMod.OnGameMapLoad += OnGameMapLoad;
				isInUse = true;

				ServerConnectionManager.Instance.StartCoroutine(ProcessQueue());

				OnClientChangeZone(ClientConnectionManager.Instance.LocalPlayerID, SceneManager.GetActiveScene().name, null);
			}
		}

		private static void OnHostConnect()
		{
			//if (isInUse)
			{
				var scene = SceneManager.GetActiveScene();
				hostPreviousScene = scene.name;
			}
		}

		public static void OnDisconnect()
		{
			_zoneOwners.Clear();
			_zoneMembers.Clear();
			packetQueue.Clear();
			if (isInUse)
			{
				ServerConnectionManager.Instance.StopCoroutine(ProcessQueue());
				ClientConnectionManager.Instance.OnClientConnect -= OnClientChangeZone;
				ClientConnectionManager.Instance.OnClientSwapZone -= OnClientChangeZone;
				ClientConnectionManager.Instance.OnPlayerDisconnect -= OnPlayerDisconnect;
				ClientConnectionManager.Instance.OnConnect -= OnHostConnect;
				ErenshorCoopMod.OnGameMapLoad -= OnGameMapLoad;
				isInUse = false;
			}
		}

		private static void OnGameMapLoad(Scene scene)
		{
			if (isInUse)
			{
				if (hostPreviousScene != scene.name)
				{
					OnClientChangeZone(ClientConnectionManager.Instance.LocalPlayerID, scene.name, hostPreviousScene);
					hostPreviousScene = scene.name;
				}
			}
		}

		public static void OnClientChangeZone(short playerID, string newZone, string previousZone)
		{
			if (newZone == previousZone) return;

			if (!_zoneMembers.TryGetValue(newZone, out var newList))
			{
				newList = new List<short>();
				_zoneMembers[newZone] = newList;
			}

			//I tried fixing this like a normal person but oh well hackity hack hack
			if (playerID == ClientConnectionManager.Instance.LocalPlayerID)
				hostPreviousScene = newZone;

			Logging.Log($" {newZone} += {playerID}");
			_zoneMembers[newZone].Add(playerID);


			//Send our sims to this player
			//if (newZone == SceneManager.GetActiveScene().name)
			//	SharedNPCSyncManager.Instance.StartCoroutine(SharedNPCSyncManager.Instance.DelayedCheckSim(playerID));

			if (!string.IsNullOrEmpty(previousZone) && _zoneMembers.TryGetValue(previousZone, out var pList))
			{
				Logging.Log($" {previousZone} -= {playerID}");

				pList.Remove(playerID);
				_zoneMembers[previousZone] = pList;

				if (ServerConfig.EnableZoneTransfership.Value && pList.Count >= 1 && _zoneOwners[previousZone] != pList[0])
				{
					_zoneOwners[previousZone] = pList[0];
					Logging.Log($" {previousZone}.O = {pList[0]}");
					SendZoneOwnershipPacket(pList[0],pList[0], previousZone, ClientConnectionManager.Instance.GetPlayerFromID(pList[0]));
				}
			}
			

			if (_zoneMembers[newZone].Count == 1)
			{
				_zoneOwners[newZone] = playerID;
				Logging.Log($" {newZone}.O = {playerID}");
			}

			if (!ServerConfig.EnableZoneTransfership.Value)
			{
				_zoneOwners[newZone] = ClientConnectionManager.Instance.LocalPlayerID;
				Logging.Log($" {newZone}.O = 0.1");
			}

			if (playerID == ClientConnectionManager.Instance.LocalPlayerID && _zoneOwners[newZone] != ClientConnectionManager.Instance.LocalPlayerID)
			{
				//Send previous owner that we're the new owner
				SendZoneOwnershipPacket(_zoneOwners[newZone], ClientConnectionManager.Instance.LocalPlayerID, newZone, ClientConnectionManager.Instance.GetPlayerFromID(_zoneOwners[newZone]));

				_zoneOwners[newZone] = ClientConnectionManager.Instance.LocalPlayerID;
				Logging.Log($" {newZone}.O = 0.2");
				hostIsTakingOver = true;
			}


			SendZoneOwnershipPacket(playerID, _zoneOwners[newZone], newZone, ClientConnectionManager.Instance.GetPlayerFromID(playerID));
		}


		public static void OnPlayerDisconnect(short playerID)
		{
			Logging.Log($"Cleaning up zone data for disconnected player {playerID}");

			var zone = "";
			foreach (var z in _zoneMembers)
			{
				if (!z.Value.Contains(playerID)) continue;
				zone = z.Key;
				break;
			}

			if (string.IsNullOrEmpty(zone) || !_zoneMembers.TryGetValue(zone, out var members))
			{
				return;
			}

			members.Remove(playerID);
			if (_zoneOwners[zone] == playerID && _zoneMembers[zone].Count >= 1)
			{
				_zoneOwners[zone] = _zoneMembers[zone][0];
				SendZoneOwnershipPacket(_zoneOwners[zone], _zoneOwners[zone], zone, ClientConnectionManager.Instance.Players[_zoneOwners[zone]]);
			}

			if (_zoneOwners[zone] == playerID)
				_zoneOwners.Remove(zone);

			if (_zoneMembers[zone].Count == 0)
				_zoneMembers.Remove(zone);
		}


		private static void SendZoneOwnershipPacket(short peerPlayer, short newOwnerID, string zone, Entity ent)
		{
			if(ent != null && ent == ClientConnectionManager.Instance.LocalPlayer)
			{
				ClientZoneOwnership.OnZoneOwnerChange(newOwnerID, zone, _zoneMembers[zone]);
				return;
			}
			packetQueue.Enqueue((peerPlayer, newOwnerID, zone, ent));
		}


		private static IEnumerator ProcessQueue()
		{
			while (true)
			{
				while (packetQueue.Count <= 0) yield return new WaitForSeconds(0.5f);

				( short peerPlayer, short newOwnerID, string zone, Entity ent ) = packetQueue.Dequeue();

				Logging.Log($"assigning {newOwnerID} as owner of {zone}");

				
				var packet = PacketManager.GetOrCreatePacket<ServerInfoPacket>(peerPlayer, PacketType.SERVER_INFO);
				packet.dataTypes.Add(ServerInfoType.ZONE_OWNERSHIP);
				packet.SetData("zone",      zone);
				packet.SetData("zoneOwner", newOwnerID);
				packet.playerList = _zoneMembers[zone];
				if (ent != null)
				{
					if (!Steam.Lobby.isInLobby)
						packet.SetPeerTarget(ent.peer);
					else
						packet.SetSteamTarget(ent.steamID);
				}

				yield return new WaitForSeconds(2f);
			}
		}

		public static Dictionary<string, Dictionary<short, EntitySpawnData>> zoneEntities = new();

		public static void HandleEntityPacket(EntityBasePacket packet)
		{
			if (packet.entityType == EntityType.SIM) return;
			if (packet is EntityActionPacket) return;

			if (packet is EntitySpawnPacket entitySpawnPacket)
			{
				
				foreach (var spawn in entitySpawnPacket.spawnData)
				{
					//Get spawn zone
					if(!zoneEntities.ContainsKey(entitySpawnPacket.zone))
						zoneEntities.Add(entitySpawnPacket.zone, new Dictionary<short, EntitySpawnData>());

					//Logging.Log($"Recevied data for @{zone}");
					zoneEntities[entitySpawnPacket.zone][spawn.entityID] = spawn;
					SharedNPCSyncManager.Instance.serverLastId = (short)(spawn.entityID+1);
				}
				return;
			}

			Dictionary<short, EntitySpawnData> zoneEnts = new();
			if (zoneEntities.TryGetValue(packet.zone, out var _zoneEnts))
				zoneEnts = _zoneEnts;

			//TODO: Make better
			if (packet is EntityTransformPacket entityTransformPacket)
			{
				if (zoneEnts.ContainsKey(entityTransformPacket.entityID))
				{
					if (entityTransformPacket.dataTypes.Contains(EntityDataType.POSITION))
						zoneEnts[entityTransformPacket.entityID].position = entityTransformPacket.position;

					if (entityTransformPacket.dataTypes.Contains(EntityDataType.ROTATION))
						zoneEnts[entityTransformPacket.entityID].rotation = entityTransformPacket.rotation;
				}
			}

			if (packet is EntityDataPacket entityData)
			{
				if (zoneEnts.ContainsKey(entityData.entityID))
				{
					if (entityData.dataTypes.Contains(EntityDataType.HEALTH))
					{
						if (entityData.health <= 0)
						{
							zoneEnts.Remove(entityData.entityID);
						}
					}
				}
			}
		}
	}
}
