using ErenshorCoop.Shared;
using System.Collections;
using System.Collections.Generic;
using ErenshorCoop.Shared.Packets;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ErenshorCoop.Client
{
	public class ClientNPCSyncManager : MonoBehaviour
	{
		public Dictionary<short, NetworkedNPC> NetworkedMobs = new();
		public Dictionary<short, NetworkedNPC> NetworkedSims = new();

		private readonly Queue<(int spawnID, string spawnMobID, bool isRare, short mobID, Vector3 pos, Quaternion rot)> spawnQueue = new();
		private Coroutine spawnRoutine;

		public static ClientNPCSyncManager Instance;


		private bool CanRun => ClientConnectionManager.Instance.IsRunning;

		public void Awake()
		{
			if (Instance != null) Destroy(gameObject);
			Instance = this;

			//ClientConnectionManager.Instance.OnConnect += LoadAndDestroySpawns;
			ClientConnectionManager.Instance.OnDisconnect += Cleanup;
			//ErenshorCoopMod.OnGameMapLoad += _OnMapSceneLoad;

			DontDestroyOnLoad(gameObject);

			Logging.Log($"ClientNPCSyncManager Created.");

			NetworkedMobs.Clear();

			spawnRoutine = StartCoroutine(SpawnMobsRoutine());
		}

		private void OnDestroy()
		{
			Cleanup();
			if (ClientConnectionManager.Instance.IsRunning)
				Grouping.ForceClearGroup();
			//ClientConnectionManager.Instance.OnConnect -= LoadAndDestroySpawns;
			//ErenshorCoopMod.OnGameMapLoad -= _OnMapSceneLoad;
			ClientConnectionManager.Instance.OnDisconnect -= Cleanup;
		}


		public void Cleanup()
		{
			foreach (var mob in NetworkedMobs)
				Destroy(mob.Value.gameObject);

			var spawns = FindObjectsOfType<SpawnPoint>(true);
			foreach (var spawn in spawns)
			{
				spawn.gameObject.SetActive(true);
			}

			NetworkedMobs.Clear();
			spawnQueue.Clear();

			foreach (var s in NetworkedSims)
			{
				if(s.Value == null) continue;

				s.Value.sim.enabled = true;
				Destroy(s.Value);
			}

			NetworkedSims.Clear();

			Logging.Log($"ClientNPCSyncManager Cleaned up.");
		}

		public void _OnMapSceneLoad(Scene scene) => LoadAndDestroySpawns();



		public void LoadSpawns()
		{
			if (!CanRun) return;

			//Cleanup here, because we're setting networkedNPC to not destroy, in case we're respawning
			//Cleanup();


			var spawns = FindObjectsOfType<SpawnPoint>(true);
			foreach (var spawn in spawns)
			{
				int spawnID = Extensions.GenerateHash(spawn.transform.position, SceneManager.GetActiveScene().name);

				if (!Variables.spawnData.ContainsKey(spawnID))
				{
					Variables.AddSpawn(spawnID, spawn);
					Logging.Log($"Spawn added {spawnID}");
					spawn.gameObject.SetActive(false);
				}
				else
				{
					spawn.gameObject.SetActive(false);
				}

			}
		}
		public void LoadAndDestroySpawns()
		{
			if (!CanRun) return;

			//Cleanup here, because we're setting networkedNPC to not destroy, in case we're respawning
			//Cleanup();


			var spawns = FindObjectsOfType<SpawnPoint>(true);
			foreach (var spawn in spawns)
			{
				int spawnID = Extensions.GenerateHash(spawn.transform.position, SceneManager.GetActiveScene().name);

				var spawnedNPC = spawn.SpawnedNPC;

				if (!Variables.spawnData.ContainsKey(spawnID))
				{
					Variables.AddSpawn(spawnID, spawn);

					//Logging.Log($"Deleted: {spawnID}");
					//Delete spawned mob+spawner
					if (spawnedNPC != null)
					{
						Destroy(spawnedNPC.gameObject);
						//spawn.ResetSpawnPoint();
						spawn.actualSpawnDelay = spawn.SpawnDelay;
						spawn.recentlyKilled = spawn.SpawnDelay;
						
						if (NPCTable.LiveNPCs.Contains(spawnedNPC))
							NPCTable.LiveNPCs.Remove(spawnedNPC);
					}
					spawn.gameObject.SetActive(false);
				}
				else
				{
					//Logging.Log($"Spawn already added: {spawnID}");
					//the spawn is already added but we still need to destroy it

					//Logging.Log($"Deleted: {spawnID}");
					//Delete spawned mob+spawner
					if (spawnedNPC != null)
					{
						Destroy(spawnedNPC.gameObject);
						//spawn.ResetSpawnPoint();
						spawn.actualSpawnDelay = spawn.SpawnDelay;
						spawn.recentlyKilled = spawn.SpawnDelay;

						if (NPCTable.LiveNPCs.Contains(spawnedNPC))
							NPCTable.LiveNPCs.Remove(spawnedNPC);
					}
					spawn.gameObject.SetActive(false);
				}

			}
		}

		public void ReadEntitySpawn(EntitySpawnData data, bool spawnInstant=false)
		{
			if (!CanRun) return;


			if (data.entityType == EntityType.SIM)
			{
				//if (NetworkedSims.ContainsKey(data.entityID)) return;
				//Logging.Log($"====> Spawning sim");
				SpawnSim(data.npcID, data.entityID, data.position, data.rotation);
				return;
			}

			if (data.entityType == EntityType.PET)
			{
				//Logging.Log($"====> Spawning pet");
				SpawnPet(data);
				return;
			}

			if (NetworkedMobs.ContainsKey(data.entityID)) return;

			if (!spawnInstant)
				spawnQueue.Enqueue(( data.spawnerID, data.npcID, data.isRare, data.entityID, data.position, data.rotation ));
			else
				SpawnMob(data.spawnerID, data.npcID, data.isRare, data.entityID, data.position, data.rotation);
		}

		private bool SpawnPet(EntitySpawnData data)
		{
			//Logging.Log($"bee");
			if (!CanRun) return true;
			
			//Get spell
			var spell = GameData.SpellDatabase.GetSpellByID(data.npcID);
			if (spell == null)
			{
				Logging.Log($"spell no exist");
				return false;
			}

			//Get owner
			Character owner = null;
			if (NetworkedMobs.TryGetValue(data.ownerID, out var own))
				owner = own.character;
			if (NetworkedSims.TryGetValue(data.ownerID, out var npc))
				owner = npc.character;
			if (ClientConnectionManager.Instance.Players.TryGetValue(data.ownerID, out var pl))
				owner = pl.character;
			if (owner == null)
			{
				Logging.Log($"owner no exist");
				return false;
			}

			
			if(owner.MyCharmedNPC != null)
				DestroyImmediate(owner.MyCharmedNPC.gameObject);

			var pet = Instantiate(spell.PetToSummon, data.position, data.rotation);
			var np = pet.AddComponent<NetworkedNPC>();
			np.entityID = data.entityID;
			np.type = EntityType.PET;
			pet.GetComponent<Character>().Master = owner;

			var ownEnt = owner.GetComponent<Entity>();
			ownEnt.MySummon = np;
			owner.MyCharmedNPC = pet.GetComponent<NPC>();
			owner.MyCharmedNPC.SummonedByPlayer = true;
			np.zone = ownEnt.zone;
			//Logging.Log($"pet zone {np.zone}");
			if(np.zone != SceneManager.GetActiveScene().name)
				np.gameObject.SetActive(false);

			if (!np.npc.NPCName.Contains("Pocket") && !np.npc.NPCName.Contains("Summoned"))
			{
				np.name = $"{owner.name}'s pet";
				np.npc.NPCName = np.name;
			}

			np.SetPosition(data.position);
			np.SetRotation(data.rotation);

			NetworkedMobs[np.entityID] = np;
			//DontDestroyOnLoad(component.gameObject);
			return true;
		}

		private IEnumerator SpawnMobsRoutine()
		{
			while (true)
			{
				if (spawnQueue.Count > 0)
				{
					(int spawnID, string spawnMobID, bool isRare, short entityID, var pos, var rot) = spawnQueue.Dequeue();
					if (!SpawnMob(spawnID, spawnMobID, isRare, entityID, pos, rot))
					{
						//Put it back on the queue, in case we're not on the same scene as the host
						//spawnQueue.Enqueue((spawnID, spawnMobID, isRare, entityID, pos, rot));
					}
				}
				yield return new WaitForSeconds(0.01f); //one spawn per second
			}
		}

		private bool SpawnMob(int spawnID, string spawnMobID, bool isRare, short entityID, Vector3 pos, Quaternion rot)
		{
			if (!CanRun) return true;
			if (NetworkedMobs.ContainsKey(entityID)) return true;

			GameObject prefab = null;
			//Special cases
			bool isSpecial = false;
			if (spawnID < 0)
			{
				var tSpawnMobID = 0;

				if ((CustomSpawnID)spawnID == CustomSpawnID.ADDS || (CustomSpawnID)spawnID == CustomSpawnID.TREASURE_GUARD)
					tSpawnMobID = int.Parse(spawnMobID.Split(',')[0]);
				else
					tSpawnMobID = int.Parse(spawnMobID);
				
				switch ((CustomSpawnID)spawnID)
				{
					case CustomSpawnID.MALAROTH:
						var malFeed = FindObjectOfType<MalarothFeed>(); //Just assume we're in the correct zone
						if (malFeed != null)
						{
							prefab = isRare ? malFeed.Malaroth : malFeed.Demented;
						}
						break;
					case CustomSpawnID.CHESS:
						var chessBoard = FindObjectOfType<Chessboard>();
						if (chessBoard != null)
						{
							prefab = tSpawnMobID switch
							{
								1  => chessBoard.PeonNPC,
								2  => chessBoard.EmberNPC,
								3  => chessBoard.BlazeNPC,
								4  => chessBoard.MonarchNPC,
								5  => chessBoard.KingsmanNPC,
								6  => chessBoard.CandlekeeperNPC,
								7  => chessBoard.FacelessDuel,
								8  => chessBoard.FacelessArc,
								9  => chessBoard.FacelessPal,
								10 => chessBoard.FacelessDru,
								_  => null
							};
						}
						break;
					case CustomSpawnID.SIRAETHE:
						var sira = FindObjectOfType<SiraetheEvent>();
						if (sira != null)
						{
							prefab = sira.WardSpawnable;
						}
						break;
					case CustomSpawnID.ADDS:
						var spawnedEnt = GetEntityFromID((short)tSpawnMobID, false);
						if (spawnedEnt != null)
						{
							var spawnedFightEvent = spawnedEnt.GetComponent<NPCFightEvent>();
							if (spawnedFightEvent != null)
							{
								var mID = int.Parse(spawnMobID.Split(',')[1]);
								var vID = int.Parse(spawnMobID.Split(',')[2]);
								prefab = mID switch
								{
									1 => spawnedFightEvent.SpawnAdds[vID],
									2 => spawnedFightEvent.SpawnAdds[vID],
									3 => spawnedFightEvent.SpawnOnDeath[vID],
									_ => null
								};
							}
						}
						break;
					case CustomSpawnID.TREASURE_GUARD:
						var guardID = int.Parse(spawnMobID.Split(',')[1]);
						var guard = GameHooks.GetChestGuardPrefab(tSpawnMobID, guardID);
						if (guard != null)
						{
							prefab = guard;
						}
						break;
				}

				isSpecial = true;
			}

			
			if (!isSpecial) //No checks if it is
			{
				if (!Variables.spawnData.ContainsKey(spawnID))
				{
					//Just incase, we'll try to find spawns again.
					bool fspawn = false;
					var spawns = FindObjectsOfType<SpawnPoint>(true);
					foreach (var spawn in spawns)
					{
						int spawnerID = Extensions.GenerateHash(spawn.transform.position, SceneManager.GetActiveScene().name);

						if (!Variables.spawnData.ContainsKey(spawnerID))
						{
							Variables.AddSpawn(spawnerID, spawn);
							spawn.gameObject.SetActive(false);
						}

						if (spawnerID == spawnID)
							fspawn = true;
					}

					if (!fspawn)
					{
						Logging.LogError($"Could not create mob {spawnMobID} for {spawnID}. No spawn data.");
						return false;
					}
				}

				var tSpawnMobID = int.Parse(spawnMobID);

				var gameData = Variables.spawnData[spawnID].GetMob(isRare, tSpawnMobID);
				if (gameData == null)
				{
					Logging.LogError($"Could not create mob {spawnMobID} for {spawnID}. No mob found.");
					return false;
				}

				prefab = gameData.prefab;
				
			}
			//Logging.Log($"Spawning {spawnID} {spawnMobID} {isRare} {entityID} {prefab.name} ({gameData.name}).");

			if (prefab == null)
			{
				Logging.LogError($"Could not create mob {spawnMobID} for {spawnID}. No prefab for mob.");
				return false;
			}

			var component = Instantiate(prefab, pos, rot).GetComponent<NPC>();

			if(!isSpecial)
				component.GetComponent<Stats>().Level += Variables.spawnData[spawnID].levelMod;


			//SpawnedNPC = component;
			NPCTable.LiveNPCs.Add(component);


			//GameObject mob = Instantiate(prefab, pos, rot);
			var s = component.gameObject.AddComponent<NetworkedNPC>();
			s.entityID = entityID;
			s.pos = pos;
			s.rot = rot;
			s.associatedSpawner = spawnID;
			s.type = EntityType.ENEMY;
			s.zone = SceneManager.GetActiveScene().name;
			NetworkedMobs.Add(entityID, s);
			//DontDestroyOnLoad(component.gameObject);
			return true;
		}


		private bool SpawnSim(string npcID, short entityID, Vector3 pos, Quaternion rot)
		{
			if (!CanRun) return true;
			//if (NetworkedSims.ContainsKey(entityID)) return true;

			if (!int.TryParse(npcID, out int simIndex))
			{
				Logging.LogError($"Could not parse simIndex {npcID}.");
				return false;
			}

			if (GameData.SimMngr.Sims.Count <= simIndex || simIndex < 0)
			{
				Logging.LogError($"SimIndex {npcID} out of bounds (does not exist in game?).");
				return false;
			}

			//Get sim
			var sim = GameData.SimMngr.Sims[simIndex];

			//is the sim on this map?
			NetworkedNPC network;

			bool isSpawned = sim.CurScene == SceneManager.GetActiveScene().name && sim.MyAvatar != null;
			//Make 100% sure the sim isn't already in the scene
			var _sim = GameObject.Find(sim.SimName);
			if (_sim != null)
				isSpawned = true;

			if (isSpawned)
			{
				if (sim.MyAvatar == null)
					sim.MyAvatar = _sim.GetComponent<SimPlayer>();
				//No need to do anything but add our sync and disable npc/sim
				//Logging.Log($"not spawning sim");
				network = sim.MyAvatar.gameObject.GetOrAddComponent<NetworkedNPC>();
				sim.MyAvatar.enabled = false;
				sim.CurScene = SceneManager.GetActiveScene().name;
			}
			else
			{
				//Spawn the sim
				//Logging.Log($"spawning sim");
				var actualSim = sim.SpawnMeInGame(pos);
				sim.CurScene = SceneManager.GetActiveScene().name;
				network = actualSim.gameObject.GetOrAddComponent<NetworkedNPC>();
				actualSim.enabled = false;
			}

			//Fuck it, override
			GameData.SimMngr.Sims[simIndex] = sim;

			network.entityID = entityID;
			network.pos = pos;
			network.rot = rot;
			network.type = EntityType.SIM;
			network.zone = SceneManager.GetActiveScene().name;

			//Remove the sims summon
			if (network.GetComponent<Character>().MyCharmedNPC != null)
				Destroy(network.GetComponent<Character>().MyCharmedNPC.gameObject);

			NetworkedSims[entityID] = network;
			//DontDestroyOnLoad(component.gameObject);
			return true;
		}

		public void OnEntityDataReceive<T>(T packet) where T : BasePacket
		{
			if (!CanRun) return;

			if (packet is EntitySpawnPacket entitySpawnPacket)
			{
				//Logging.Log("recv spawn packet");
				if (!entitySpawnPacket.targetPlayerIDs.Contains(ClientConnectionManager.Instance.LocalPlayerID) && entitySpawnPacket.entityType != EntityType.SIM)
				{
					//Logging.Log("but we arent a receiving player");
					return;
				}

				foreach (var spawn in entitySpawnPacket.spawnData)
				{
					if(entitySpawnPacket.entityType == EntityType.SIM && entitySpawnPacket.zone == SceneManager.GetActiveScene().name)
						ReadEntitySpawn(spawn);
					else if(entitySpawnPacket.entityType != EntityType.SIM)
						ReadEntitySpawn(spawn);
				}

				return;
			}

			//TODO: Make better
			if (packet is EntityTransformPacket entityTransformPacket)
			{
				
				if (NetworkedMobs.ContainsKey(entityTransformPacket.entityID) && entityTransformPacket.entityType != EntityType.SIM)
				{
					if (!entityTransformPacket.targetPlayerIDs.Contains(ClientConnectionManager.Instance.LocalPlayerID))
						return;

					if (entityTransformPacket.dataTypes.Contains(EntityDataType.POSITION))
						NetworkedMobs[entityTransformPacket.entityID].SetPosition(entityTransformPacket.position);
					if (entityTransformPacket.dataTypes.Contains(EntityDataType.ROTATION))
						NetworkedMobs[entityTransformPacket.entityID].SetRotation(entityTransformPacket.rotation);
				}
				else if(NetworkedSims.ContainsKey(entityTransformPacket.entityID) && entityTransformPacket.entityType == EntityType.SIM)
				{
					if (entityTransformPacket.dataTypes.Contains(EntityDataType.POSITION))
						NetworkedSims[entityTransformPacket.entityID].SetPosition(entityTransformPacket.position);
					if (entityTransformPacket.dataTypes.Contains(EntityDataType.ROTATION))
						NetworkedSims[entityTransformPacket.entityID].SetRotation(entityTransformPacket.rotation);
				}
				
			}

			if (packet is EntityDataPacket entityData)
			{
				if (NetworkedMobs.ContainsKey(entityData.entityID) && entityData.entityType != EntityType.SIM)
				{
					if (!entityData.targetPlayerIDs.Contains(ClientConnectionManager.Instance.LocalPlayerID))
						return;

					if (entityData.dataTypes.Contains(EntityDataType.ANIM))
					{
						foreach (var a in entityData.animData)
							NetworkedMobs[entityData.entityID].UpdateAnimState(a);
					}
					if (entityData.dataTypes.Contains(EntityDataType.HEALTH))
						NetworkedMobs[entityData.entityID].character.MyStats.CurrentHP = entityData.health;
					if (entityData.dataTypes.Contains(EntityDataType.ENTITY_REMOVE))
					{
						
						if (entityData.entityType == EntityType.PET)
						{
							//NetworkedMobs[entityData.entityID].owner.MySummon = null;
						}
						Logging.Log($"ent rem {NetworkedMobs[entityData.entityID].name}");
						Destroy(NetworkedMobs[entityData.entityID].gameObject);
					}
					if(entityData.dataTypes.Contains(EntityDataType.CURTARGET))
					{
						NetworkedMobs[entityData.entityID].HandleTargetChange(entityData.targetID, entityData.targetType);
					}
				}
				else if(NetworkedSims.ContainsKey(entityData.entityID) && entityData.entityType == EntityType.SIM)
				{
					if (entityData.dataTypes.Contains(EntityDataType.ANIM))
					{
						foreach (var a in entityData.animData)
							NetworkedSims[entityData.entityID].UpdateAnimState(a);
					}
					if (entityData.dataTypes.Contains(EntityDataType.HEALTH))
						NetworkedSims[entityData.entityID].character.MyStats.CurrentHP = entityData.health;
					if (entityData.dataTypes.Contains(EntityDataType.SIM_REMOVE))
					{
						NetworkedSims[entityData.entityID].sim.enabled = true;
						Destroy(NetworkedSims[entityData.entityID]);
						NetworkedSims.Remove(entityData.entityID);
					}
					if (entityData.dataTypes.Contains(EntityDataType.CURTARGET))
					{
						NetworkedSims[entityData.entityID].HandleTargetChange(entityData.targetID, entityData.targetType);
					}
				}
			}

			if (packet is EntityActionPacket entityAction)
			{
				if (NetworkedMobs.ContainsKey(entityAction.entityID) && entityAction.entityType != EntityType.SIM)
				{
					if (!entityAction.targetPlayerIDs.Contains(ClientConnectionManager.Instance.LocalPlayerID))
						return;

					if (entityAction.dataTypes.Contains(ActionType.ATTACK))
						NetworkedMobs[entityAction.entityID].HandleAttack(entityAction.attackData);
					if (entityAction.dataTypes.Contains(ActionType.SPELL_CHARGE))
						NetworkedMobs[entityAction.entityID].HandleSpellCharge(entityAction.SpellChargeFXIndex);
					if (entityAction.dataTypes.Contains(ActionType.SPELL_EFFECT))
						NetworkedMobs[entityAction.entityID].HandleSpellEffect(entityAction.spellID, entityAction.targetID, entityAction.targetIsNPC, entityAction.targetIsSim);
					if (entityAction.dataTypes.Contains(ActionType.SPELL_END))
						NetworkedMobs[entityAction.entityID].HandleEndSpell();
					if (entityAction.dataTypes.Contains(ActionType.HEAL))
						NetworkedMobs[entityAction.entityID].HandleHeal(entityAction.healingData);
					if(entityAction.dataTypes.Contains(ActionType.STATUS_EFFECT_APPLY))
						NetworkedMobs[entityAction.entityID].HandleStatusEffectApply(entityAction.effectData);
					if(entityAction.dataTypes.Contains(ActionType.STATUS_EFFECT_REMOVE))
						NetworkedMobs[entityAction.entityID].HandleStatusRemoval(entityAction.RemoveAllStatus, entityAction.RemoveBreakable, entityAction.statusID);
				}
				else if(NetworkedSims.ContainsKey(entityAction.entityID) && entityAction.entityType == EntityType.SIM)
				{
					if (entityAction.dataTypes.Contains(ActionType.ATTACK))
						NetworkedSims[entityAction.entityID].HandleAttack(entityAction.attackData);
					if (entityAction.dataTypes.Contains(ActionType.SPELL_CHARGE))
						NetworkedSims[entityAction.entityID].HandleSpellCharge(entityAction.SpellChargeFXIndex);
					if (entityAction.dataTypes.Contains(ActionType.SPELL_EFFECT))
						NetworkedSims[entityAction.entityID].HandleSpellEffect(entityAction.spellID, entityAction.targetID, entityAction.targetIsNPC, true);
					if (entityAction.dataTypes.Contains(ActionType.SPELL_END))
						NetworkedSims[entityAction.entityID].HandleEndSpell();
					if (entityAction.dataTypes.Contains(ActionType.HEAL))
						NetworkedSims[entityAction.entityID].HandleHeal(entityAction.healingData);
					if (entityAction.dataTypes.Contains(ActionType.STATUS_EFFECT_APPLY))
						NetworkedSims[entityAction.entityID].HandleStatusEffectApply(entityAction.effectData);
					if (entityAction.dataTypes.Contains(ActionType.STATUS_EFFECT_REMOVE))
						NetworkedSims[entityAction.entityID].HandleStatusRemoval(entityAction.RemoveAllStatus, entityAction.RemoveBreakable, entityAction.statusID);
				}
			}
		}

		public Entity GetEntityFromID(short entityID, bool isSim)
		{
			if(isSim)
				foreach (var p in NetworkedSims)
					if (p.Key == entityID)
						return p.Value;
			foreach (var p in NetworkedMobs)
				if (p.Key == entityID)
					return p.Value;
			return null;
		}


		public void OnClientMobDestroyed(short id)
		{
			if (!CanRun) return;

			NetworkedMobs.Remove(id);
		}
	}
}
