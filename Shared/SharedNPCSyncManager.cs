using ErenshorCoop.Shared;
using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ErenshorCoop.Client;
using ErenshorCoop.Shared.Packets;
using ErenshorCoop.Server;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ErenshorCoop.Shared
{
	public class SharedNPCSyncManager : MonoBehaviour
	{
		public Dictionary<short, NPCSync> mobs = new();
		public Dictionary<short, SimSync> sims = new();
		public Dictionary<Animator, short> animatorToMobID = new();
		public Dictionary<AnimatorOverrideController, short> overrideToMobID = new();

		//public short currentMobID = -1;
		public byte currentSpawnID = 0;

		public static SharedNPCSyncManager Instance;

		private bool CanRun => ClientConnectionManager.Instance.IsRunning;

		

		private string curZone = "";
		

		//TODO: Do this properly
		public short sharedPetId = -1;
		public short serverLastId = -1;
		
		

		public void Awake()
		{
			if (Instance != null) Destroy(gameObject);
			Instance = this;


			ClientConnectionManager.Instance.OnConnect += ClientZoneOwnership.OnConnect;
			ClientConnectionManager.Instance.OnDisconnect += ClientZoneOwnership.OnDisconnect;
			ClientConnectionManager.Instance.OnConnect += ServerZoneOwnership.OnConnect;
			ClientConnectionManager.Instance.OnDisconnect += ServerZoneOwnership.OnDisconnect;


			ClientConnectionManager.Instance.OnConnect += CollectSpawnData;
			ClientConnectionManager.Instance.OnDisconnect += OnDisconnect;

			
			ErenshorCoopMod.OnGameMapLoad += OnZoneChange;

			DontDestroyOnLoad(gameObject);
			mobs.Clear();
			animatorToMobID.Clear();

			curZone = SceneManager.GetActiveScene().name;


			Logging.Log($"SharedNPCSyncManager Created.");
		}

		private void OnZoneChange(Scene scene)
		{
			if (scene.name != curZone)
			{
				Logging.Log($"{curZone} != {scene.name}");
				Cleanup();
				CollectSpawnData();
				//if (ServerConnectionManager.Instance.IsRunning)
				//	ServerRemoveSims();
				if(ClientConnectionManager.Instance.IsRunning)
					StartCoroutine(DelayedCheckSim());

			}
			curZone = scene.name;

			if (ServerConnectionManager.Instance.IsRunning)
			{
				//OnClientChangeZone(ClientConnectionManager.Instance.LocalPlayerID, scene.name);
			//	ServerRemoveSims();
				//We assume our sims got destroyed..
				//sims.Clear();
				//We delay this just in case the sims aren't spawned in yet
				//StartCoroutine(DelayedCheckSim());
			}
		}


		public void ConvertOwnedToUnowned()
		{
			Logging.Log($"Converting {mobs.Count}");
			foreach (var m in mobs)
			{
				var npc = m.Value;
				short id = m.Key;

				if (!npc.character.Alive) continue;

				var sync = npc.gameObject.AddComponent<NetworkedNPC>();
				var _npc = npc.gameObject.GetComponent<NPC>();

				ClientNPCSyncManager.Instance.NetworkedMobs.Add(id, sync);
				sync.entityID = id;
				sync.SetPosition(npc.transform.position);
				sync.SetRotation(npc.transform.rotation);
				Destroy(npc);
				NPCTable.LiveNPCs.Add(_npc);
			}
			Logging.Log($"Got {ClientNPCSyncManager.Instance.NetworkedMobs.Count}");

			Cleanup(true);
		}

		public void TakeOwnership(string zone)
		{
			Logging.Log($"Taking ownership of zone");

			if (ServerConnectionManager.Instance.IsRunning && ServerZoneOwnership.hostIsTakingOver)
			{

				Cleanup();
				//Delete all mobs
				ClientNPCSyncManager.Instance.LoadAndDestroySpawns();
				Logging.Log($"Removed Spawns");
				//Spawn all the spawns we've gotten so far
				if (ServerZoneOwnership.zoneEntities.TryGetValue(zone, out var spawnData))
				{
					foreach (var spawn in spawnData)
					{
						ClientNPCSyncManager.Instance.ReadEntitySpawn(spawn.Value, true);
					}

					Logging.Log($"Loaded Spawns {spawnData.Count} @{zone}");
				}

				ServerZoneOwnership.zoneEntities[zone] = new();
			}


			//Cleanup();
			//CollectSpawnData();

			Logging.Log($"Converting {ClientNPCSyncManager.Instance.NetworkedMobs.Count}");
			short lastHighMobID = GetFreeId();
			foreach (var m in ClientNPCSyncManager.Instance.NetworkedMobs)
			{
				var npc = m.Value;
				short id = m.Key;

				if (!npc.character.Alive) continue;

				var sync = npc.gameObject.AddComponent<NPCSync>();
				var _npc = npc.gameObject.GetComponent<NPC>();

				int spawnerID = npc.associatedSpawner;

				if (Variables.spawnPoints.TryGetValue(spawnerID, out var point))
				{
					GameHooks.spawnPoint.SetValue(_npc, point);
					point.SpawnedNPC = _npc;
					point.gameObject.SetActive(true);
				}

				//GameHooks.startMethod.Invoke(_npc, null);

				GameHooks.animatorController.SetValue(_npc, (AnimatorOverrideController)npc.MyAnim.runtimeAnimatorController);

				Destroy(npc);
				mobs.Add(id, sync);
				sync.entityID = id;
				//if(id > lastHighMobID) lastHighMobID = id;

				NPCTable.LiveNPCs.Add(_npc);	
			}

			Logging.Log($"Activating spawns");
			var spawns = FindObjectsOfType<SpawnPoint>(true);
			foreach (var spawner in spawns)
				spawner.gameObject.SetActive(true);

			Logging.Log($"clearing networked mobs");
			ClientNPCSyncManager.Instance.NetworkedMobs.Clear();

			//SendMobData(0, true);
			if (!ServerZoneOwnership.hostIsTakingOver)
			{
				Logging.Log($"Sending spawns");
				StartCoroutine(DelayedSendMobData());
			}
			else
			{
				CollectSpawnData();
			}

			ServerZoneOwnership.hostIsTakingOver = false;
		}

		public void OnDestroy()
		{
			Cleanup();

			ClientConnectionManager.Instance.OnConnect -= CollectSpawnData;
			ClientConnectionManager.Instance.OnDisconnect -= OnDisconnect;
			ErenshorCoopMod.OnGameMapLoad -= OnZoneChange;

			ClientConnectionManager.Instance.OnConnect -= ClientZoneOwnership.OnConnect;
			ClientConnectionManager.Instance.OnDisconnect -= ClientZoneOwnership.OnDisconnect;
			ClientConnectionManager.Instance.OnConnect -= ServerZoneOwnership.OnConnect;
			ClientConnectionManager.Instance.OnDisconnect -= ServerZoneOwnership.OnDisconnect;
		}


		private void OnDisconnect()
		{

			Cleanup();
		}
		private void Cleanup(bool noActivateSpawn=false)
		{
			foreach (var mob in mobs)
				Destroy(mob.Value);

			if (!noActivateSpawn)
			{
				var spawns = FindObjectsOfType<SpawnPoint>(true);
				foreach (var spawn in spawns)
				{
					spawn.gameObject.SetActive(true);
				}
			}

			foreach (var s in sims)
				Destroy(s.Value);

			sims.Clear();
			mobs.Clear();
			animatorToMobID.Clear();
			overrideToMobID.Clear();
			Variables.spawnData.Clear();
			Variables.spawnPoints.Clear();

			Logging.Log($"SharedNPCSyncManager Cleaned up.");
		}

		public void CollectSims()
		{
			var _sims = GameData.SimMngr.ActiveSimInstances;
			foreach(var sim  in _sims)
			{
				var s = sim.gameObject.GetOrAddComponent<SimSync>();
				s.type = EntityType.SIM;
			}
		}

		public void CollectSpawnData()
		{
			Logging.Log("collecting");

			var spawns = FindObjectsOfType<SpawnPoint>(true);

			foreach (var spawn in spawns)
			{
				int spawnID = Extensions.GenerateHash(spawn.transform.position, SceneManager.GetActiveScene().name);

				if (!Variables.spawnData.ContainsKey(spawnID))
				{
					Variables.AddSpawn(spawnID, spawn);
				}


				//Add our sync to the mob, if there is one
				var spawnedNPC = spawn.SpawnedNPC;
				if (spawnedNPC != null)
				{
					if (!spawnedNPC.GetComponent<Character>().Alive) continue;

					var s = spawnedNPC.gameObject.GetComponent<NPCSync>();
					if (s == null)
					{
						s = spawnedNPC.gameObject.AddComponent<NPCSync>();
						s.entityID = GetFreeId();
						mobs.Add(s.entityID, s);
					}
					else
					{
						mobs[s.entityID] = s;
					}

					var anim = spawnedNPC.gameObject.GetComponent<Animator>();
					animatorToMobID[anim] = s.entityID;

					EntitySpawnData entSpawn = new()
					{
						entityID = s.entityID,
						npcID = "",
						spawnerID = spawnID,
						isRare = false,
						position = s.transform.position,
						rotation = s.transform.rotation,
					};
					s.spawnData = entSpawn;
					//var npc = spawnedNPC.gameObject.GetComponent<NPC>();
					try
					{
						overrideToMobID.Add((AnimatorOverrideController)anim.runtimeAnimatorController, s.entityID);
					}
					catch{}
				}

			}
		}

		public IEnumerator DelayedCheckSim(short playerID=-1)
		{
			yield return new WaitForSeconds(2f);
			CollectSims();
		}

		public IEnumerator DelayedSendMobData(short playerID = -1)
		{
			yield return new WaitForSeconds(2f);
			if(playerID == -1)
				SendMobData(0, true);
			else
				SendMobData(playerID);
		}
		public void ServerCheckSims(short toPlayer=-1)
		{
			var currentGroup = Grouping.currentGroup;

			if (currentGroup.groupList == null) return;
			if (currentGroup.groupList.Count <= 0) return;

			List<EntitySpawnData> spawnData = new();
			var pidx = 0;
			for (var i = 0; i < currentGroup.groupList.Count; i++)
			{
				short playerID = currentGroup.groupList[i].entityID;
				bool isSim = currentGroup.groupList[i].isSim;
				if (playerID == ClientConnectionManager.Instance.LocalPlayerID) continue;

				switch (pidx)
				{
					case 0:
						if (isSim)
						{
							if (GameData.GroupMember1 != null)
							{
								//spawnData.Add(ServerSpawnSim(GameData.GroupMember1.MyAvatar.gameObject, GameData.GroupMember1.simIndex));
								//GameData.GroupMember1.MyAvatar.GetComponent<NPCSync>().OnClientConnect(-1,"",""); //Force pet spawn
							}
						}
						break;
					case 1:
						if (isSim)
						{
							if (GameData.GroupMember2 != null)
							{ 
								//spawnData.Add(ServerSpawnSim(GameData.GroupMember2.MyAvatar.gameObject, GameData.GroupMember2.simIndex)); 
								//GameData.GroupMember2.MyAvatar.GetComponent<NPCSync>().OnClientConnect(-1, "", "");
							}
						}
						break;
					case 2:
						if (isSim)
						{
							if (GameData.GroupMember3 != null)
							{
								//spawnData.Add(ServerSpawnSim(GameData.GroupMember3.MyAvatar.gameObject, GameData.GroupMember3.simIndex));
								//GameData.GroupMember3.MyAvatar.GetComponent<NPCSync>().OnClientConnect(-1, "", "");
							}
						}
						break;
				}
				pidx++;
			}
			SendEntitySpawnPacket(spawnData, EntityType.SIM, toPlayer);
		}

		/// <summary>
		/// Tells clients to "free" the sims if we zone or something
		/// </summary>
		public void ServerRemoveSims()
		{
			List<short> playerIDs = ClientConnectionManager.Instance.Players.Keys.ToList();
			foreach (var si in sims)
			{
				var p = PacketManager.GetOrCreatePacket<EntityDataPacket>(si.Key, PacketType.ENTITY_DATA);
				p.dataTypes.Add(EntityDataType.SIM_REMOVE);
				p.entityType = EntityType.SIM;
				p.SetData("targetPlayerIDs", playerIDs);
			}
		}

		/// <summary>
		/// Tells clients to "free" a sim when we remove them from the party
		/// </summary>
		public void ServerRemoveSim(short entityID)
		{
			List<short> playerIDs = ClientConnectionManager.Instance.Players.Keys.ToList();

			var p = PacketManager.GetOrCreatePacket<EntityDataPacket>(entityID, PacketType.ENTITY_DATA);
			p.dataTypes.Add(EntityDataType.SIM_REMOVE);
			p.entityType = EntityType.SIM;
			p.SetData("targetPlayerIDs", playerIDs);
			
		}


		public List<short> GetPlayerSendList() => ClientZoneOwnership._zonePlayers.Keys.ToList();

		private IEnumerator MobGetAnimOver(NPC npc, short entityID)
		{
			yield return new WaitForSeconds(1f);
			var ff = (AnimatorOverrideController)GameHooks.animatorController.GetValue(npc);
			overrideToMobID[ff] = entityID;
		}


		public Entity GetEntityFromID(short entityID, bool isSim)
		{
			if (isSim)
			{
				if (sims.TryGetValue(entityID, out var sim))
					return sim;
			}
			else
				return mobs.TryGetValue(entityID, out var mob) ? mob : null;

			return null;
		}

		public void OnMobDestroyed(short id, EntityType type)
		{
			if (!CanRun) return;

			if (type == EntityType.SIM)
			{
				SendEntityDestroyPacket(id, type);
				//sims.Remove(id);
				return;
			}

			if (type == EntityType.PET)
			{
				SendEntityDestroyPacket(id, type);
			}

			if (mobs.ContainsKey(id))
			{
				animatorToMobID.Remove(mobs[id].anim);
				overrideToMobID.Remove((AnimatorOverrideController)GameHooks.animatorController.GetValue(mobs[id].npc));
				mobs.Remove(id);
			}
		}

		public short GetFreeId()
		{
			//serverLastId = -1;
			foreach(var f in mobs)
				if(f.Key >  serverLastId)
					serverLastId = f.Key;
			foreach (var f in sims)
				if (f.Key > serverLastId)
					serverLastId = f.Key;
			foreach (var f in ClientNPCSyncManager.Instance.NetworkedMobs)
				if (f.Key > serverLastId)
					serverLastId = f.Key;
			foreach (var f in ClientNPCSyncManager.Instance.NetworkedSims)
				if (f.Key > serverLastId)
					serverLastId = f.Key;

			if (serverLastId == -1)
				serverLastId++;

			return ++serverLastId;
		}





		/// <summary>
		/// Sends mob data to a specific player or all players in the zone.
		/// </summary>
		public void SendMobData(short playerID, bool sendToAll = false)
		{
			Logging.LogError($"Trying to send mob data.....");
			if (!CanRun || !ClientZoneOwnership.isZoneOwner) return;

			Logging.LogError($"Trying to send mob data. {Variables.spawnData.Count}");

			var spawns = FindObjectsOfType<SpawnPoint>(true);
			List<EntitySpawnData> spawnData = new();

			foreach (var spawn in spawns)
			{
				int spawnID = Extensions.GenerateHash(spawn.transform.position, SceneManager.GetActiveScene().name);
				var currentMob = spawn.SpawnedNPC;

				if (currentMob != null && Variables.spawnData.ContainsKey(spawnID))
				{
					if (!currentMob.GetComponent<Character>().Alive)
						continue;

					NPCSync mobSync = currentMob.GetComponent<NPCSync>() ?? currentMob.gameObject.GetOrAddComponent<NPCSync>();
					if (mobSync.entityID == 0)
					{
						mobSync.entityID = GetFreeId();
						mobs[mobSync.entityID] = mobSync;
						var anim = currentMob.GetComponent<Animator>();
						animatorToMobID[anim] = mobSync.entityID;
						overrideToMobID[(AnimatorOverrideController)anim.runtimeAnimatorController] = mobSync.entityID;
					}

					(int spawnMobID, bool isRare) = Variables.spawnData[spawnID].GetMob(currentMob.gameObject);
					if (spawnMobID == -1)
					{
						Logging.Log($"Error getting mob for {spawnID} {currentMob.name}.");
						continue;
					}

					EntitySpawnData entSpawn = CreateEntitySpawnData(
						mobSync.entityID,
						spawnMobID.ToString(),
						spawnID,
						isRare,
						currentMob.transform.position,
						currentMob.transform.rotation,
						EntityType.ENEMY
					);

					mobSync.spawnData = entSpawn;
					mobSync.type = EntityType.ENEMY;
					mobSync.zone = SceneManager.GetActiveScene().name;
					spawnData.Add(entSpawn);
				}
			}

			var targetPlayers = sendToAll ? ClientZoneOwnership._zonePlayers.Keys.ToList() : new List<short> { playerID };

			currentSpawnID++;
			if (currentSpawnID == 0) currentSpawnID++;

			var packet = PacketManager.GetOrCreatePacket<EntitySpawnPacket>(currentSpawnID, PacketType.ENTITY_SPAWN) as EntitySpawnPacket;
			packet.SetData("spawnData", spawnData).SetData("targetPlayerIDs", targetPlayers);
			packet.zone = SceneManager.GetActiveScene().name;

			Logging.Log($"Sending {spawnData.Count} mobs.");
		}

		/// <summary>
		/// Spawns a single mob.
		/// </summary>
		public void ServerSpawnMob(GameObject spawnedNPC, int spawnID, string spawnMobID, bool isRare, Vector3 pos, Quaternion rot)
		{
			if (!CanRun || !ClientZoneOwnership.isZoneOwner) return;

			var s = spawnedNPC.GetOrAddComponent<NPCSync>();
			s.type = EntityType.ENEMY;
			if(s.entityID == -1)
				s.entityID = GetFreeId();
			s.zone = SceneManager.GetActiveScene().name;

			mobs[s.entityID] = s;
			animatorToMobID[spawnedNPC.GetComponent<Animator>()] = s.entityID;
			StartCoroutine(MobGetAnimOver(spawnedNPC.GetComponent<NPC>(), s.entityID));

			List<EntitySpawnData> spawnData = new()
			{
				CreateEntitySpawnData(
					s.entityID,
					spawnMobID,
					spawnID,
					isRare,
					pos,
					rot,
					EntityType.ENEMY,
					0,
					s.isGuardian?s:null
				)
			};

			SendEntitySpawnPacket(spawnData, EntityType.ENEMY);
		}

		/// <summary>
		/// Spawns a group of mobs based on a parent NPC source.
		/// </summary>
		public void ServerSpawnMobs(List<GameObject> fmobs, List<string> fids, int spawnID, NPC from)
		{
			if (!CanRun || !ClientZoneOwnership.isZoneOwner) return;

			List<EntitySpawnData> spawnData = new();
			int fromID = mobs.FirstOrDefault(f => f.Value.npc == from).Key;

			if (fromID == 0)
			{
				Logging.LogError($"Unknown spawn from {from.name}");
				return;
			}

			for (int i = 0; i < fmobs.Count; i++)
			{
				var mob = fmobs[i];
				var s = mob.GetOrAddComponent<NPCSync>();
				s.entityID = GetFreeId();
				s.zone = SceneManager.GetActiveScene().name;
				mobs[s.entityID] = s;
				animatorToMobID[mob.GetComponent<Animator>()] = s.entityID;
				StartCoroutine(MobGetAnimOver(mob.GetComponent<NPC>(), s.entityID));

				spawnData.Add(CreateEntitySpawnData(
					s.entityID,
					$"{fromID},{fids[i]}",
					spawnID,
					false,
					mob.transform.position,
					mob.transform.rotation,
					EntityType.ENEMY
				));
			}
			
			SendEntitySpawnPacket(spawnData, EntityType.ENEMY);
		}

		/// <summary>
		/// Spawns pet
		/// </summary>
		public void ServerSpawnPet(GameObject spawnedNPC, short owner, short mobID, string spellID)
		{
			var s = spawnedNPC.GetOrAddComponent<NPCSync>();
			s.entityID = mobID;
			s.type = EntityType.PET;
			s.zone = SceneManager.GetActiveScene().name;
			mobs[mobID] = s;
			animatorToMobID[spawnedNPC.GetComponent<Animator>()] = mobID;
			StartCoroutine(MobGetAnimOver(spawnedNPC.GetComponent<NPC>(), mobID));

			List<EntitySpawnData> spawnData = new()
			{
				CreateEntitySpawnData(
					mobID,
					spellID,
					0,
					false,
					spawnedNPC.transform.position,
					spawnedNPC.transform.rotation,
					EntityType.PET,
					owner
				)
			};

			//Logging.Log("spawning pet");

			SendEntitySpawnPacket(spawnData, EntityType.PET);
		}

		/// <summary>
		/// Creates SpawnData for Sim
		/// </summary>
		/*public EntitySpawnData ServerSpawnSim(GameObject sim, int simIndex)
		{
			if (!CanRun) return new();

			var s = sim.GetOrAddComponent<NPCSync>();
			if (!sims.Values.Contains(s))
			{
				s.type = EntityType.SIM;
				s.entityID = GetFreeId();
				sims[s.entityID] = s;
			}
			s.zone = SceneManager.GetActiveScene().name;
			return CreateEntitySpawnData(
				s.entityID,
				simIndex.ToString(),
				-1,
				false,
				sim.transform.position,
				sim.transform.rotation,
				EntityType.SIM
			);
		}*/


		/// <summary>
		/// Sends Spawn Packet
		/// </summary>
		private void SendEntitySpawnPacket(List<EntitySpawnData> spawnData, EntityType type, short playerID = -1)
		{
			currentSpawnID++;
			if (currentSpawnID == 0) currentSpawnID++;

			List<short> playerIDs = ClientZoneOwnership._zonePlayers.Keys.ToList();
			if (playerID != -1)
			{
				playerIDs = new List<short> { playerID };
			}

			var pack = PacketManager.GetOrCreatePacket<EntitySpawnPacket>(currentSpawnID, PacketType.ENTITY_SPAWN);
			pack.SetData("spawnData", spawnData);
			if (pack.spawnData == null || pack.spawnData.Count == 0)
			{
				pack.spawnData = new();
			}
			pack.spawnData.AddRange(spawnData);

			pack.SetData("targetPlayerIDs", playerIDs);
			if (playerID != -1)
				pack.SetTarget(ClientConnectionManager.Instance.GetPlayerFromID(playerID));
			pack.zone = SceneManager.GetActiveScene().name;
			pack.entityType = type;

			foreach (var p in spawnData)
			{
				NPCSync ent = null;
				if(mobs.ContainsKey(p.entityID))
					ent = mobs[p.entityID];
				//if (ent == null && sims.ContainsKey(p.entityID))
				//	ent = sims[p.entityID];
				if(ent == null) continue;

				//Force pet spawn
				if(ent.type != EntityType.PET)
					ent.OnClientConnect(-1, "", "");
			}
		}

		/// <summary>
		/// Sends Destroy Packet
		/// </summary>
		private void SendEntityDestroyPacket(short entityID, EntityType type, short playerID = -1)
		{
			currentSpawnID++;
			if (currentSpawnID == 0) currentSpawnID++;

			List<short> playerIDs = ClientZoneOwnership._zonePlayers.Keys.ToList();
			if (playerID != -1)
			{
				playerIDs = new List<short> { playerID };
			}

			var pack = PacketManager.GetOrCreatePacket<EntityDataPacket>(entityID, PacketType.ENTITY_DATA);
			pack.dataTypes.Add(EntityDataType.ENTITY_REMOVE);
			pack.SetData("targetPlayerIDs", playerIDs);
			if (playerID != -1)
				pack.SetTarget(ClientConnectionManager.Instance.GetPlayerFromID(playerID));
			pack.zone = SceneManager.GetActiveScene().name;
			pack.entityType = type;

		}

		/// <summary>
		/// Helper method to create an EntitySpawnData object.
		/// </summary>
		private EntitySpawnData CreateEntitySpawnData(short entityID, string npcID, int spawnerID, bool isRare, Vector3 position, Quaternion rotation, EntityType type, short ownerID = 0, NPCSync sync = null)
		{
			var es = new EntitySpawnData
			{
				entityID = entityID,
				npcID = npcID,
				spawnerID = spawnerID,
				isRare = isRare,
				position = position,
				rotation = rotation,
				entityType = type,
				ownerID = ownerID
			};

			if(sync != null && sync.isGuardian)
			{

				es.syncStats = true;
				es.level = sync.character.MyStats.Level;
				es.baseAC = sync.character.MyStats.BaseAC;
				es.baseER = sync.character.MyStats.BaseER;
				es.baseHP = sync.character.MyStats.BaseHP;
				es.baseMR = sync.character.MyStats.BaseMR;
				es.basePR = sync.character.MyStats.BasePR;
				es.baseVR = sync.character.MyStats.BaseVR;
				es.baseDMG = sync.npc.BaseAtkDmg;
				es.mhatkDelay = sync.character.MyStats.BaseMHAtkDelay;

				Debug.Log($@"
						ID: {entityID}\n
						Level:        {es.level}\n
						BaseAC:       {es.baseAC}\n
						BaseER:       {es.baseER}\n
						BaseHP:       {es.baseHP}\n
						BaseMR:       {es.baseMR}\n
						BasePR:       {es.basePR}\n
						BaseVR:       {es.baseVR}\n
						MHAtkDelay:   {es.mhatkDelay}\n
						BaseDMG:      {es.baseDMG}
						");
			}

			return es;
		}

	}
}
