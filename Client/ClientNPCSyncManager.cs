using ErenshorCoop.Shared;
using System.Collections;
using System.Collections.Generic;
using ErenshorCoop.Shared.Packets;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Runtime.InteropServices;
using ErenshorCoop.Client.Grouping;

namespace ErenshorCoop.Client
{
	public class ClientNPCSyncManager : MonoBehaviour
	{
		public Dictionary<short, NetworkedNPC> NetworkedMobs = new();
		public Dictionary<short, NetworkedSim> NetworkedSims = new();

		private readonly Queue<(int spawnID, string spawnMobID, bool isRare, short mobID, Vector3 pos, Quaternion rot, EntitySpawnData data)> spawnQueue = new();
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
				ClientGroup.ForceClearGroup();
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

				//s.Value.sim.enabled = true;
				Destroy(s.Value.gameObject);
			}

			NetworkedSims.Clear();

			Logging.Log($"ClientNPCSyncManager Cleaned up.");
		}

		//public void _OnMapSceneLoad(Scene scene) => LoadAndDestroySpawns();



		public void LoadSpawns()
		{
			if (!CanRun) return;

			//Cleanup here, because we're setting networkedNPC to not destroy, in case we're respawning
			//Cleanup();


			/*foreach (var _spawn in Variables.syncedSpawnPoints)
			{
				if (_spawn.Value.TryGetTarget(out var spawn))
				{
					if (spawn == null || spawn.gameObject == null) continue;
					spawn.gameObject.SetActive(false);
				}
			}*/
		}
		public void LoadAndDestroySpawns(bool isTakeOver)
		{
			if (!CanRun) return;

			var cnt = 0;
			var spcnt = 0;
			foreach (var _spawn in Variables.syncedSpawnPoints)
			{
				if (!_spawn.Value.TryGetTarget(out var spawn)) continue;
				if (spawn == null || spawn.gameObject == null) continue;
				int spawnID = _spawn.Key;

				var spawnedNPC = spawn.SpawnedNPC;
				spawn.gameObject.SetActive(false);
				spcnt++;

				if (spawnedNPC != null)
				{
					spawn.actualSpawnDelay = spawn.SpawnDelay;
					spawn.recentlyKilled = spawn.SpawnDelay;

					if (NPCTable.LiveNPCs.Contains(spawnedNPC))
						NPCTable.LiveNPCs.Remove(spawnedNPC);

					//FIXME: it hasn't even been that long and i already forgot why i put this here
					if (!isTakeOver && spawnedNPC.GetComponent<NetworkedNPC>() != null)
					{
						spawn.SpawnedNPC = null;
						continue;
					}
					Destroy(spawnedNPC.gameObject);
					cnt++;
				}
			}
			Logging.Log($"Destroyed {cnt} mobs with {spcnt} spawns");
		}

		public void ReadEntitySpawn(EntitySpawnData data, bool spawnInstant=false)
		{
			if (!CanRun) return;


			if (data.entityType == EntityType.SIM)
			{
				//if (NetworkedSims.ContainsKey(data.entityID)) return;
				//Logging.Log($"====> Spawning sim");
				//SpawnSim(data.npcID, data.entityID, data.position, data.rotation);
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
				spawnQueue.Enqueue(( data.spawnerID, data.npcID, data.isRare, data.entityID, data.position, data.rotation, data ));
			else
				SpawnMob(data.spawnerID, data.npcID, data.isRare, data.entityID, data.position, data.rotation, data);
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
				//Logging.Log($"owner no exist");
				return false;
			}

			
			if(owner.MyCharmedNPC != null)
				DestroyImmediate(owner.MyCharmedNPC.gameObject);

			var pet = Instantiate(spell.PetToSummon, data.position, data.rotation);
			var np = pet.AddComponent<NetworkedNPC>();
			np.entityID = data.entityID;
			np.type = EntityType.PET;
			pet.GetComponent<Character>().Master = owner;
			pet.GetComponent<Stats>().CurrentMaxHP = data.maxHP;
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
					(int spawnID, string spawnMobID, bool isRare, short entityID, var pos, var rot, var data) = spawnQueue.Dequeue();
					if (data.zone == SceneManager.GetActiveScene().name) //Make sure we're in the correct scene
					{
						if (!SpawnMob(spawnID, spawnMobID, isRare, entityID, pos, rot, data))
						{
							//Put it back on the queue, in case we're not on the same scene as the host
							spawnQueue.Enqueue((spawnID, spawnMobID, isRare, entityID, pos, rot, data));
						}
					}
				}
				yield return new WaitForSeconds(0.01f); //one spawn per second
			}
		}

		private bool SpawnMob(int spawnID, string spawnMobID, bool isRare, short entityID, Vector3 pos, Quaternion rot, EntitySpawnData data)
		{
			if (!CanRun) return true;
			if (NetworkedMobs.ContainsKey(entityID)) return true;

			GameObject prefab = null;
			bool dontInstantiate = false;
			int vID = -1;
			//Special cases
			bool isSpecial = false;
			NPC component = null;

			if (spawnID < 0)
			{
				var tSpawnMobID = 0;

				if ((CustomSpawnID)spawnID == CustomSpawnID.ADDS || (CustomSpawnID)spawnID == CustomSpawnID.TREASURE_GUARD || 
					(CustomSpawnID)spawnID == CustomSpawnID.WAVE_EVENT || (CustomSpawnID)spawnID == CustomSpawnID.SPAWN_TRIGGER ||
					(CustomSpawnID)spawnID == CustomSpawnID.FERNALLA_PORTAL || (CustomSpawnID)spawnID == CustomSpawnID.FERNALLA_WARD ||
					(CustomSpawnID)spawnID == CustomSpawnID.PRE_SYNCED)
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
								vID = int.Parse(spawnMobID.Split(',')[2]);
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
					case CustomSpawnID.ASTRA:
						var astr = FindObjectOfType<AstraListener>();
						if (astr != null)
						{
							prefab = astr.Dragon;
						}
						break;
					case CustomSpawnID.WAVE_EVENT:
						var we = FindObjectOfType<WaveEvent>();
						if (we != null)
						{
							var mID = tSpawnMobID;
							vID = int.Parse(spawnMobID.Split(',')[1]);

							if(isRare)
							{
								prefab = we.BossMob;
								break;
							}

							List<GameObject> prefList = null;
							prefList = mID switch
							{
								1 => we.WeakWave,
								2 => we.StrongWave,
								3 => we.StrongestWave,
								_ => null
							};
							if(prefList != null)
							{
								prefab = prefList[vID];
							}
						}
						break;
					case CustomSpawnID.SPAWN_TRIGGER:
						var trig = FindObjectsOfType<SpawnPointTrigger>(true);
						vID = int.Parse(spawnMobID.Split(',')[1]);
						foreach (var t in trig)
						{
							if(Variables.triggerIDs.TryGetValue(t, out var d) && d.id == tSpawnMobID)
							{
								GameData.PlayerAud.PlayOneShot(t.Trigger, GameData.SFXVol);
								if (isRare)
								{
									prefab = t.Alt;
									break;
								}
								prefab = t.Spawnables[vID];
								break;
							}
						}
						break;
					case CustomSpawnID.FERNALLA_WARD:
						if(isRare)
						{
							prefab = GameHooks.fernallaBoss;
							break;
						}
						vID = tSpawnMobID;
						dontInstantiate = true;
						var vts = FindObjectOfType<FernallaPortalBoss>(true);
						if (vts != null)
						{
							prefab = vID switch
							{
								1 => vts.Ward1,
								2 => vts.Ward2,
								3 => vts.Ward3,
								_ => null
							};
							prefab.SetActive(true);
							component = prefab.GetComponent<NPC>();
						}
						break;
					case CustomSpawnID.FERNALLA_PORTAL:
						vID = int.Parse(spawnMobID.Split(',')[1]);
						var prtls = FindObjectsOfType<GameHooks.SyncedFernallaPortalEvent>();
						foreach(var prtl in prtls)
						{
							if(Variables.portalIDs.TryGetValue(prtl, out var d) && d.id == tSpawnMobID)
							{
								prefab = vID switch
								{
									1 => prtl.Knight,
									2 => prtl.Arcanist,
									3 => prtl.Hound,
									4 => prtl.Invader,
									_ => null
								};
							}
						}
						break;
					case CustomSpawnID.PRE_SYNCED:
						var syncs = FindObjectsOfType<GameHooks.PreSyncedEntity>(true);
						dontInstantiate = true;
						foreach (var t in syncs)
						{
							if (Variables.presyncedEntities.TryGetValue(t, out var d) && d.id == tSpawnMobID)
							{
								component = d.go.GetComponent<NPC>();
								Logging.Log($"syncing {d.go.name}");
								break;
							}
						}
						if(component == null)
						{
							Logging.LogError($"Could not create mob {spawnMobID} for {spawnID}. No prefab for mob.");
							return false;
						}
					break;
				}

				isSpecial = true;
			}

			
			if (!isSpecial) //No checks if it is
			{

				var tSpawnMobID = int.Parse(spawnMobID);
				
				prefab = SyncedSpawnPoint.GetPrefab(spawnID, tSpawnMobID, isRare);
				
			}
			//Logging.Log($"Spawning {spawnID} {spawnMobID} {isRare} {entityID} {prefab.name} ({gameData.name}).");

			if (prefab == null && !dontInstantiate)
			{
				//Logging.LogError($"Could not create mob {spawnMobID} for {spawnID}. No prefab for mob.");
				return false;
			}

			if(!dontInstantiate)
				component = Instantiate(prefab, pos, rot).GetComponent<NPC>();

			if (!isSpecial)
			{
				if(Variables.syncedSpawnPoints.TryGetValue(spawnID, out var _spwn) && _spwn.TryGetTarget(out var dt))
					component.GetComponent<Stats>().Level += dt.levelMod;
			}


			//SpawnedNPC = component;
			NPCTable.LiveNPCs.Add(component);

			component.gameObject.SetActive(true);
			//GameObject mob = Instantiate(prefab, pos, rot);
			var s = component.gameObject.AddComponent<NetworkedNPC>();
			s.entityID = entityID;
			s.pos = pos;
			s.rot = rot;
			s.associatedSpawner = data;
			s.type = EntityType.ENEMY;
			s.zone = SceneManager.GetActiveScene().name;
			NetworkedMobs.Add(entityID, s);
			s.GetComponent<Stats>().CurrentMaxHP = data.maxHP;
			//DontDestroyOnLoad(component.gameObject);
			if(data.syncStats)
			{
				s.character.MyStats.Level = data.level;
				s.character.MyStats.BaseAC = data.baseAC;
				s.character.MyStats.BaseER = data.baseER;
				s.character.MyStats.BaseHP = data.baseHP;
				s.character.MyStats.BaseMR = data.baseMR;
				s.character.MyStats.BasePR = data.basePR;
				s.character.MyStats.BaseVR = data.baseVR;
				s.character.MyStats.BaseMHAtkDelay = data.mhatkDelay;
				s.npc.BaseAtkDmg = data.baseDMG;
				s.character.MyStats.CurrentMaxHP = data.baseHP;
				s.character.MyStats.CurrentHP = data.baseHP;

				s.spData = data;
			}
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
						Destroy(NetworkedMobs[entityData.entityID].gameObject);
					}
					if(entityData.dataTypes.Contains(EntityDataType.CURTARGET))
					{
						NetworkedMobs[entityData.entityID].HandleTargetChange(entityData.targetID, entityData.targetType);
					}
					if (entityData.dataTypes.Contains(EntityDataType.PERIODIC_UPDATE))
					{
						var m = NetworkedMobs[entityData.entityID];
						m.character.MyStats.CurrentMaxHP = entityData.maxHealth;
						m.character.MyStats.CurrentHP = entityData.health;
						GameHooks.maxMP.SetValue(m.character.MyStats, entityData.maxMP);
						m.character.MyStats.CurrentMana = entityData.mp;
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
					if (entityAction.dataTypes.Contains(ActionType.WAND_ATTACK))
					{
						foreach (var wd in entityAction.wandData)
							NetworkedMobs[entityAction.entityID].HandleWand(wd);
					}
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
