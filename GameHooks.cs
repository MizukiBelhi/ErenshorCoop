using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.AI;
using ErenshorCoop.Shared;
using ErenshorCoop.Client;
using ErenshorCoop.Server;
using ErenshorCoop.Shared.Packets;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace ErenshorCoop
{
	public class GameHooks
	{
		public static void CreateHooks(Harmony harm)
		{
			ErenshorCoopMod.CreatePrefixHook(typeof(Animator),                   "SetBool",                 typeof(GameHooks), "SetBool_Prefix",         new[] { typeof(string), typeof(bool) } );
			ErenshorCoopMod.CreatePrefixHook(typeof(Animator),                   "SetFloat",                typeof(GameHooks), "SetFloat_Prefix",        new[] { typeof(string), typeof(float) });
			ErenshorCoopMod.CreatePrefixHook(typeof(Animator),                   "SetInteger",              typeof(GameHooks), "SetInteger_Prefix",      new[] { typeof(string), typeof(int) });
			ErenshorCoopMod.CreatePrefixHook(typeof(Animator),                   "SetTrigger",              typeof(GameHooks), "SetTrigger_Prefix",      new[] { typeof(string) });
			ErenshorCoopMod.CreatePrefixHook(typeof(Animator),                   "ResetTrigger",            typeof(GameHooks), "ResetTrigger_Prefix",    new[] { typeof(string) });
			ErenshorCoopMod.CreatePrefixHook(typeof(AnimatorOverrideController), "set_Item",                typeof(GameHooks), "AnimOverrideSet_Prefix", new[] { typeof(string), typeof(AnimationClip) });
			ErenshorCoopMod.CreatePrefixHook(typeof(Zoneline),                   "OnTriggerEnter",          typeof(GameHooks), "ZoneLineOnTriggerEnter_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(NPC),                        "Update",                  typeof(GameHooks), "NPCUpdate_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(NPC),                        "Combat",                  typeof(GameHooks), "NPCCombat_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(NPC),                        "ForceAggroOn",            typeof(GameHooks), "ForceAggroOn_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(TypeText),                   "CheckInput",              typeof(GameHooks), "CheckInput_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(Stats),                      "ReduceHP",                typeof(GameHooks), "StatsReduceHP_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(Stats),                      "MitigatePhysical",        typeof(GameHooks), "StatsMitigatePhysical_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(Character),                  "DamageMe",                typeof(GameHooks), "CharacterDamageMe_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(Character),                  "MagicDamageMe",           typeof(GameHooks), "MagicDamageMe_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(Character),                  "CheckVsMR",               typeof(GameHooks), "CheckVsMR_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(SimPlayerGrouping),          "InviteToGroup",           typeof(GameHooks), "InviteToGroup_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(SimPlayerGrouping),          "DismissMember1",          typeof(GameHooks), "DismissMember1_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(SimPlayerGrouping),          "DismissMember2",          typeof(GameHooks), "DismissMember2_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(SimPlayerGrouping),          "DismissMember3",          typeof(GameHooks), "DismissMember3_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(SimPlayerTracking),          "SpawnMeInGame",           typeof(GameHooks), "SpawnMeInGame_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(SpellVessel),                "CreateSpellChargeEffect", typeof(GameHooks), "ChargeEffect_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(SpellVessel),                "EndSpell",                typeof(GameHooks), "EndSpell_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(SpellVessel),                "EndSpellNoCD",            typeof(GameHooks), "EndSpell_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(SpellVessel),                "ResolveSpell",            typeof(GameHooks), "ResolveSpell_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(SimPlayerMngr),              "BringPlayerGroupToZone",  typeof(GameHooks), "BringGroup_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(GameData),                   "AddExperience",           typeof(GameHooks), "AddExperience_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(Stats),                      "AddStatusEffect",         typeof(GameHooks), "AddStatusEffectType1_Prefix", new[] { typeof(Spell), typeof(bool), typeof(int) });
			ErenshorCoopMod.CreatePrefixHook(typeof(Stats),                      "AddStatusEffect",         typeof(GameHooks), "AddStatusEffectType2_Prefix", new[] { typeof(Spell), typeof(bool), typeof(int), typeof(Character) });
			ErenshorCoopMod.CreatePrefixHook(typeof(Stats),                      "AddStatusEffect",         typeof(GameHooks), "AddStatusEffectType3_Prefix", new[] { typeof(Spell), typeof(bool), typeof(int), typeof(Character), typeof(float) });
			ErenshorCoopMod.CreatePrefixHook(typeof(Stats),                      "RemoveStatusEffect",      typeof(GameHooks), "RemoveStatusEffect_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(Stats),                      "RemoveAllStatusEffects",  typeof(GameHooks), "RemoveAllStatusEffects_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(Stats),                      "RemoveBreakableEffects",  typeof(GameHooks), "RemoveBreakableEffects_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(MalarothFeed),               "CheckForGamepiece",       typeof(GameHooks), "MCheckForGamepiece_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(MalarothFeed),               "SpawnPiece",              typeof(GameHooks), "MSpawnPiece_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(Chessboard),                 "CheckForGamepiece",       typeof(GameHooks), "CCheckForGamepiece_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(Chessboard),                 "SpawnPiece",              typeof(GameHooks), "CSpawnPiece_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(SiraetheEvent),              "Update",                  typeof(GameHooks), "SUpdate_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(NPCFightEvent),              "FixedUpdate",             typeof(GameHooks), "FightFixedUpdate_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(CharmedNPC),                 "GoAway",                  typeof(GameHooks), "CharmedGoAway_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(SimPlayerMngr),              "CollectActiveSimData",    typeof(GameHooks), "CollectActiveSimData_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(SceneChange),                "ChangeScene",             typeof(GameHooks), "ChangeScene_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(LootWindow),                 "CloseWindow",             typeof(GameHooks), "LootWindowClose_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(PlayerControl),              "LeftClick",               typeof(GameHooks), "PlayerLeftClick_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(ItemIcon),                   "InformGroupOfLoot",       typeof(GameHooks), "InformGroupOfLoot_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(AtmosphereColors),           "Update",                  typeof(WeatherHandler), "AtmosphereUpdate_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(DayNight),                   "FixedUpdate",             typeof(WeatherHandler), "DayNightFixedUpd_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(SimPlayer),                  "FollowPlayer",            typeof(GameHooks), "FollowPlayer_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(SimPlayer),                  "LoadSimData",             typeof(GameHooks), "LoadSimData_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(PlayerCombat),               "DoWandAttack",            typeof(GameHooks), "PlayerDoWandAttack_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(NPC),                        "DoWandAttack",            typeof(GameHooks), "NPCDoWandAttack_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(SimPlayerMngr),              "BringPlayerGroupToZone",  typeof(GameHooks), "BringPlayerGroupToZone_pre");
			ErenshorCoopMod.CreatePrefixHook(typeof(SimItemDisplay),             "OnPointerDown",           typeof(GameHooks), "SimItemOnPointerDown_Prefix");

			ErenshorCoopMod.CreatePostHook(typeof(SpawnPoint),        "SpawnNPC",           typeof(GameHooks), "SpawnPointSpawnNPC_Post");
			ErenshorCoopMod.CreatePostHook(typeof(Inventory),         "Update",             typeof(GameHooks), "InventoryUpdate_Postfix");
			ErenshorCoopMod.CreatePostHook(typeof(Character),         "DamageMe",           typeof(GameHooks), "CharacterDamageMe_Postfix");
			ErenshorCoopMod.CreatePostHook(typeof(Character),         "MagicDamageMe",      typeof(GameHooks), "MagicDamageMe_Postfix");
			ErenshorCoopMod.CreatePostHook(typeof(Respawn),           "RespawnPlayer",      typeof(GameHooks), "RespawnPlayer_Postfix");

			ErenshorCoopMod.CreatePostHook(typeof(GameManager),       "OpenEscMenu",        typeof(GameHooks), "OpenEscMenu_Postfix");
			ErenshorCoopMod.CreatePostHook(typeof(GameManager),       "CloseEscMenu",       typeof(GameHooks), "CloseEscMenu_Postfix");
			ErenshorCoopMod.CreatePostHook(typeof(GameManager),       "ToggleEscapeMenu",   typeof(GameHooks), "ToggleEscMenu_Postfix");
			ErenshorCoopMod.CreatePostHook(typeof(MalarothFeed),      "SpawnPiece",         typeof(GameHooks), "MSpawnPiece_Postfix");
			ErenshorCoopMod.CreatePostHook(typeof(Chessboard),        "SpawnPiece",         typeof(GameHooks), "CSpawnPiece_Postfix");
			ErenshorCoopMod.CreatePostHook(typeof(SiraetheEvent),     "Update",             typeof(GameHooks), "SUpdate_Postfix");
			ErenshorCoopMod.CreatePostHook(typeof(TreasureChestEvent),"SpawnGuardiansCont", typeof(GameHooks), "TreSpawn_Postfix");
			ErenshorCoopMod.CreatePostHook(typeof(SimPlayerTracking), "SpawnMeInGame",      typeof(GameHooks), "SimSpawn_Postfix");
			ErenshorCoopMod.CreatePostHook(typeof(Stats),             "HealMe",             typeof(GameHooks), "StatsHealMe_Postfix", new[] { typeof(Spell), typeof(int), typeof(bool), typeof(bool), typeof(Character) });


			
			//ErenshorCoopMod.CreatePrefixHook(typeof(Misc), "GenPopup", typeof(GameHooks), "MiscGenPopup_Prefix");


			var type = typeof(ModularParts);
			hideHair = type.GetField("hideHair", BindingFlags.NonPublic | BindingFlags.Instance);
			hideHead = type.GetField("hideHead", BindingFlags.NonPublic | BindingFlags.Instance);
			GetTransformNames = type.GetMethod("GetTransformNames", BindingFlags.NonPublic | BindingFlags.Instance);

			type = typeof(TypeText);
			lastTyped = type.GetField("lastTyped", BindingFlags.NonPublic | BindingFlags.Instance);

			type = typeof(NPC);
			animatorController = type.GetField("AnimOverride", BindingFlags.NonPublic | BindingFlags.Instance);
			spawnPoint = type.GetField("MySpawnPoint",         BindingFlags.NonPublic | BindingFlags.Instance);
			rotTimer = type.GetField("rotTimer",         BindingFlags.NonPublic | BindingFlags.Instance);
			startMethod = type.GetMethod("Start",           BindingFlags.NonPublic | BindingFlags.Instance);
			handleNameTag = type.GetMethod("HandleNameTag", BindingFlags.NonPublic | BindingFlags.Instance);
			leashing = type.GetField("spawnCD", BindingFlags.NonPublic | BindingFlags.Instance);

			type = typeof(SpellVessel);
			targ = type.GetField("targ",               BindingFlags.NonPublic | BindingFlags.Instance);
			SpellSource = type.GetField("SpellSource", BindingFlags.NonPublic | BindingFlags.Instance);

			type = typeof(Zoneline);
			thisZoning = type.GetField("thisZoning", BindingFlags.NonPublic | BindingFlags.Instance);

			type = typeof(Stats);
			xpBonus = type.GetField("XPBonus", BindingFlags.NonPublic | BindingFlags.Instance);

			type = typeof(MalarothFeed);
			_malarothSpawn = type.GetField("Spawned", BindingFlags.NonPublic | BindingFlags.Instance);

			type = typeof(Chessboard);
			_chessSpawn = type.GetField("Spawned", BindingFlags.NonPublic | BindingFlags.Instance);

			type = typeof(NPCFightEvent);
			_actualSpawn = type.GetField("actualSpawn", BindingFlags.NonPublic | BindingFlags.Instance);

			type = typeof(LootWindow);
			downCD = type.GetField("downCD", BindingFlags.NonPublic | BindingFlags.Instance);

			type = typeof(SimPlayer);
			followPlayer = type.GetMethod("FollowPlayer", BindingFlags.NonPublic | BindingFlags.Instance);
		}



		public static bool SimItemOnPointerDown_Prefix(PointerEventData eventData)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;

			if (eventData.button == PointerEventData.InputButton.Left)
			{
				if (GameData.InspectSim.Who != null && GameData.InspectSim.Who.GetComponent<Entity>() != null)
				{
					var ent = GameData.InspectSim.Who.GetComponent<Entity>();
					if (ent is NetworkedPlayer || ent is NetworkedSim) return false;
				}
			}
			return true;
		}

		//Sadly we need to modify this to not try to spawn players, that'd be bad
		public static bool BringPlayerGroupToZone_pre(SimPlayerMngr __instance)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;

			if (GameData.GroupMember1 != null && GameData.GroupMember1.simIndex >= 0)
			{
			//	Logging.Log($"spawn gp1 {GameData.GroupMember1.simIndex} {GameData.GroupMember1.SimName} ");
				__instance.ActiveSimInstances.Add(GameData.GroupMember1.SpawnMeInGame(GameData.PlayerControl.transform.position + new Vector3(Random.Range(-1, 1), 0f, Random.Range(-1, 1))));
				GameData.GroupMember1.MyAvatar.InGroup = true;
				GameData.GroupMember1.isPuller = false;
				GameData.GroupMember1.Caution = false;
				GameData.GroupMember1.CurScene = SceneManager.GetActiveScene().name;
				__instance.SimsInZones[GameData.GroupMember1.simIndex] = SceneManager.GetActiveScene().name;
			}
			if (GameData.GroupMember2 != null && GameData.GroupMember2.simIndex >= 0)
			{
			//	Logging.Log($"spawn gp2 {GameData.GroupMember2.simIndex} {GameData.GroupMember2.SimName} ");
				__instance.ActiveSimInstances.Add(GameData.GroupMember2.SpawnMeInGame(GameData.PlayerControl.transform.position + new Vector3(Random.Range(-1, 1), 0f, Random.Range(-1, 1))));
				GameData.GroupMember2.MyAvatar.InGroup = true;
				GameData.GroupMember2.isPuller = false;
				GameData.GroupMember2.Caution = false;
				GameData.GroupMember2.CurScene = SceneManager.GetActiveScene().name;
				__instance.SimsInZones[GameData.GroupMember2.simIndex] = SceneManager.GetActiveScene().name;
			}
			if (GameData.GroupMember3 != null && GameData.GroupMember1.simIndex >= 0)
			{
			//	Logging.Log($"spawn gp3 {GameData.GroupMember3.simIndex} {GameData.GroupMember3.SimName} ");
				__instance.ActiveSimInstances.Add(GameData.GroupMember3.SpawnMeInGame(GameData.PlayerControl.transform.position + new Vector3(Random.Range(-1, 1), 0f, Random.Range(-1, 1))));
				GameData.GroupMember3.MyAvatar.InGroup = true;
				GameData.GroupMember3.isPuller = false;
				GameData.GroupMember3.Caution = false;
				GameData.GroupMember3.CurScene = SceneManager.GetActiveScene().name;
				__instance.SimsInZones[GameData.GroupMember3.simIndex] = SceneManager.GetActiveScene().name;
			}
			return false;
		}

		public static MethodInfo followPlayer;
		public static bool CollectActiveSimData_Prefix(SimPlayerMngr __instance)
		{
			return true;
		}

		public static bool ChangeScene_Prefix(SceneChange __instance, string _dest, Vector3 _landing, bool _useSun, float yRot)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;

			if (ClientConnectionManager.Instance.LocalPlayer.zone == _dest)
			{
				GameData.SimPlayerGrouping.GroupTargets.Clear();
				GameData.SimPlayerGrouping.isPulling = false;
				GameData.AttackingPlayer.Clear();
				GameData.InCombat = false;
				GameData.GroupMatesInCombat.Clear();
				GameData.PlayerControl.GetComponent<Fishing>().resetFishing();
				GameData.PlayerControl.HuntingMe.Clear();

				__instance.Player.GetComponent<CharacterController>().enabled = false;

				GameData.CharmedNPC = null;
				__instance.SetLandingPos(_landing);
				__instance.Player.transform.position = _landing;
				__instance.Player.GetComponent<CharacterController>().enabled = true;
				__instance.Player.GetComponent<PlayerControl>().enabled = true;
				GameData.usingSun = _useSun;
				__instance.Player.transform.eulerAngles = new Vector3(0f, yRot, 0f);

				InZoneEvents.ClearNodes();
				GameData.Zoning = false;
				return false;
			}

			return true;
		}



		public static NetworkedPlayer CreatePlayer(int playerID, Vector3 pos, Quaternion rot)
		{
			var sims = GameData.SimMngr.ActualSims;
			if (sims.Count == 0)
			{
				Logging.LogError($"Could not create player for {playerID}. No Sim instances.");
				return null;
			}
			var sim = sims[0];
			if (sim == null)
			{
				Logging.LogError($"Could not create player for {playerID}. No Sim.");
				return null;
			}

			//Logging.Log($"Sim Name: {sim.name}");

			var nMeshAgent = sim.gameObject.GetComponent<NavMeshAgent>();
			if (nMeshAgent != null) nMeshAgent.enabled = false;


			GameObject _player = UnityEngine.Object.Instantiate(sim.gameObject, new Vector3(999, 999, 999), rot);
			_player.SetActive(false); //immediately hide this player, could be optimized by only creating the player if they're in the same zone.
			var pCon = _player.GetComponent<SimPlayer>();
			if(pCon == null)
			{
				Logging.LogError($"Could not create player for {playerID}.");
				return null;
			}
			pCon.enabled = false;
			_player.transform.position = pos;

			NetworkedPlayer player = _player.AddComponent<NetworkedPlayer>();
			player.sim = pCon;
			player.pos = pos;
			player.rot = rot;

			if (nMeshAgent != null) nMeshAgent.enabled = true;

			return player;
		}


		public static NetworkedSim CreateSim(int playerID, Vector3 pos, Quaternion rot)
		{
			var sims = GameData.SimMngr.ActualSims;
			if (sims.Count == 0)
			{
				Logging.LogError($"Could not create player for {playerID}. No Sim instances.");
				return null;
			}
			var sim = sims[0];
			if (sim == null)
			{
				Logging.LogError($"Could not create player for {playerID}. No Sim.");
				return null;
			}

			//Logging.Log($"Sim Name: {sim.name}");
			var nMeshAgent = sim.gameObject.GetComponent<NavMeshAgent>();
			if (nMeshAgent != null) nMeshAgent.enabled = false;

			GameObject _player = UnityEngine.Object.Instantiate(sim.gameObject, new Vector3(999, 999, 999), rot);
			
			var pCon = _player.GetComponent<SimPlayer>();
			if (pCon == null)
			{
				Logging.LogError($"Could not create player for {playerID}.");
				return null;
			}
			pCon.enabled = false;
			
			_player.transform.position = pos;

			NetworkedSim player = _player.AddComponent<NetworkedSim>();
			player.sim = pCon;
			player.pos = pos;
			player.rot = rot;

			if (nMeshAgent != null) nMeshAgent.enabled = true;

			return player;
		}


		public static void StatsHealMe_Postfix(Stats __instance, int __result, Spell _spell, int _amt, bool _isCrit, bool _isMana, Character _source)
		{
			SyncHealing(_spell, __instance, __result, _isCrit, _source, _isMana);
		}

		public static void PlayerDoWandAttack_Prefix(Character _target)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return;

			Inventory playerInv = GameData.PlayerInv;
			Item item;
			if (playerInv == null) return;

			ItemIcon mh = playerInv.MH;
			item = ((mh != null) ? mh.MyItem : null);

			if (item == null) return;

			var (_,_,targID) = GetEntityIDByCharacter(_target);
			if (targID == -1) return;

			WandAttackData wandAttackData = new()
			{
				targetID = targID,
				itemID = item.Id
			};

			ClientConnectionManager.Instance.LocalPlayer.SendWand(wandAttackData);
		}


		public static void NPCDoWandAttack_Prefix(NPC __instance, Character _target)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return;

			Entity ent = GetEntityByCharacter(__instance.ThisSim.MyStats.Myself);
			if (ent == null) return;
			//make sure its a local sim
			if (ent.entityID == -1) return;
			if (!(ent is SimSync)) return;

			Item item;

			if (!__instance.SimPlayer) return; //Seems to only be a thing on sims

			SimInvSlot simMH = __instance.ThisSim.MyStats.MyInv.SimMH;
			item = ((simMH != null) ? simMH.MyItem : null);

			if (item == null) return;

			var (_, _, targID) = GetEntityIDByCharacter(_target);
			if (targID == -1) return;

			WandAttackData wandAttackData = new()
			{
				targetID = targID,
				itemID = item.Id
			};

			((SimSync)ent).SendWand(wandAttackData);
		}

		public static bool LoadSimData_Prefix(SimPlayer __instance)
		{
			if (__instance.transform.position == new Vector3(999, 999, 999)) return false;
			return true;
		}


		public static bool ForceAggroOn_Prefix(NPC __instance, Character tar)
		{
			__instance.CurrentAggroTarget = tar;

			if (!GameData.SimPlayerGrouping.IsSimInPlayerGroup(__instance.ThisSim)) return false;
			return true;
		}

		public static bool FollowPlayer_Prefix(SimPlayer __instance)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;

			if (!SharedNPCSyncManager.Instance.simToSync.TryGetValue(__instance, out var sync)) return false; //Prevent others

			if (sync.npc.CurrentAggroTarget == null && !sync.character.MySpells.isCasting())
			{
				Transform follow = sync.target == null ? GameData.PlayerControl.transform : sync.target;

				sync.sim.TimeOnTask -= 60f * Time.deltaTime;
				if (Vector3.Distance(sync.transform.position, follow.position + sync.randomizeOffset) >= 7f)
				{
					sync.nav.speed = sync.stats.actualRunSpeed;
					sync.nav.isStopped = false;
					sync.animator.SetBool("Walking", true);
					sync.animator.SetBool("Patrol", false);
					if (sync.npc.NeedsNavUpdate(follow.position + sync.randomizeOffset))
					{
						sync.npc.HighPriorityNavUpdate(GameData.GetSafeNavMeshPoint(follow.position, 2f, 0.25f, 4f, 0.5f) + sync.randomizeOffset);
					}
				}
				if (Vector3.Distance(sync.transform.position, sync.nav.destination) < 5f)
				{
					if (Vector3.Distance(sync.transform.position, sync.nav.destination) >= 1f)
					{
						sync.nav.speed = 3f;
						sync.nav.isStopped = false;
						sync.animator.SetBool("Walking", false);
						sync.animator.SetBool("Patrol", true);
					}
					else
					{
						sync.nav.speed = 0f;
						sync.nav.velocity = Vector3.zero;
						sync.nav.isStopped = true;
						sync.animator.SetBool("Walking", false);
						sync.animator.SetBool("Patrol", false);
					}
					if (sync.npc.NeedsNavUpdate(follow.position + sync.randomizeOffset))
					{
						sync.npc.HighPriorityNavUpdate(GameData.GetSafeNavMeshPoint(follow.position, 2f, 0.25f, 4f, 0.5f) + sync.randomizeOffset);
					}
				}
			}
			return false;
		}

		public static void SimSpawn_Postfix(SimPlayerTracking __instance, ref SimPlayer __result, Vector3 pos)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return;
			if (__result == null) return;
			var ent = __result.GetComponent<SimSync>();

			//If it exists already that probably means this sim zoned
			if(ent != null)
			{
				UnityEngine.Object.Destroy(ent);
			}

			ent = __result.gameObject.AddComponent<SimSync>();
			ent.type = EntityType.SIM;
			//ent.entityID = SharedNPCSyncManager.Instance.GetFreeId();
			
		}


		private static List<GameObject> _prevTreGuards = new();
		public static int GetChestID(List<GameObject> guardList)
		{
			if (guardList.SequenceEqual(GameData.Misc.TreasureChest0_10.GetComponent<TreasureChestEvent>().Guardians))
				return 0;
			if (guardList.SequenceEqual(GameData.Misc.TreasureChest10_20.GetComponent<TreasureChestEvent>().Guardians))
				return 1;
			if (guardList.SequenceEqual(GameData.Misc.TreasureChest20_30.GetComponent<TreasureChestEvent>().Guardians))
				return 2;
			if (guardList.SequenceEqual(GameData.Misc.TreasureChest30_35.GetComponent<TreasureChestEvent>().Guardians))
				return 3;
			return -1;
		}

		public static GameObject GetChestGuardPrefab(int treasureID, int guardID)
		{
			if (treasureID == 0 && GameData.Misc.TreasureChest0_10.GetComponent<TreasureChestEvent>().Guardians.Count > guardID)
				return GameData.Misc.TreasureChest0_10.GetComponent<TreasureChestEvent>().Guardians[guardID];
			if (treasureID == 1 && GameData.Misc.TreasureChest10_20.GetComponent<TreasureChestEvent>().Guardians.Count > guardID)
				return GameData.Misc.TreasureChest10_20.GetComponent<TreasureChestEvent>().Guardians[guardID];
			if (treasureID == 2 && GameData.Misc.TreasureChest20_30.GetComponent<TreasureChestEvent>().Guardians.Count > guardID)
				return GameData.Misc.TreasureChest20_30.GetComponent<TreasureChestEvent>().Guardians[guardID];
			if (treasureID == 3 && GameData.Misc.TreasureChest30_35.GetComponent<TreasureChestEvent>().Guardians.Count > guardID)
				return GameData.Misc.TreasureChest30_35.GetComponent<TreasureChestEvent>().Guardians[guardID];
			return null;
		}

		public static void TreSpawn_Postfix(TreasureChestEvent __instance, int _attackerLevel)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return;

			//if(!_prevTreGuards.SequenceEqual(__instance.LiveGuardians))
			{
				foreach(var go in __instance.LiveGuardians)
				{
					var net = go.GetOrAddComponent<NPCSync>();
					net.treasureChestID = GetChestID(__instance.Guardians);
					var gid = 0;
					foreach(var grd in __instance.Guardians)
					{
						if (grd.GetComponent<NPC>().NPCName.Contains(go.GetComponent<NPC>().NPCName))
							break;
						gid++;
					}

					net.guardianId = gid;//__instance.Guardians.IndexOf(go);
					net.isGuardian = true;
					net.RequestID();
				}
			}
		}


		public static bool InformGroupOfLoot_Prefix(ItemIcon __instance, Item _item)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;

			if (GameData.GroupMember1 != null && GameData.GroupMember1.simIndex >= 0 && GameData.GroupMember1.MyAvatar.IsThatAnUpgrade(_item))
			{
				string str = GameData.GroupMember1.MyAvatar.MyDialog.GetLootReq().Replace("II", _item.ItemName);
				GameData.SimPlayerGrouping.AddStringForDisplay(GameData.SimPlayerGrouping.PlayerOneName.text + " tells the group: " + str, "#00B2B7");
				GameData.GroupMember1.OpinionOfPlayer -= 0.3f;
			}
			if (GameData.GroupMember2 != null && GameData.GroupMember2.simIndex >= 0 && GameData.GroupMember2.MyAvatar.IsThatAnUpgrade(_item))
			{
				string str2 = GameData.GroupMember2.MyAvatar.MyDialog.GetLootReq().Replace("II", _item.ItemName);
				GameData.SimPlayerGrouping.AddStringForDisplay(GameData.SimPlayerGrouping.PlayerTwoName.text + " tells the group: " + str2, "#00B2B7");
				GameData.GroupMember2.OpinionOfPlayer -= 0.3f;
			}
			if (GameData.GroupMember3 != null && GameData.GroupMember3.simIndex >= 0 && GameData.GroupMember3.MyAvatar.IsThatAnUpgrade(_item))
			{
				string str3 = GameData.GroupMember3.MyAvatar.MyDialog.GetLootReq().Replace("II", _item.ItemName);
				GameData.SimPlayerGrouping.AddStringForDisplay(GameData.SimPlayerGrouping.PlayerThreeName.text + " tells the group: " + str3, "#00B2B7");
				GameData.GroupMember3.OpinionOfPlayer -= 0.3f;
			}

			if (Variables.lastDroppedItem != null && Variables.lastDroppedItem.item.Id == _item.Id)
			{
				Variables.lastDroppedItem.RemoveSingleItem();
			}

			return false;
		}

		public static bool PlayerLeftClick_Prefix(PlayerControl __instance)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;
			if (GameData.MouseSlot.MyItem == GameData.PlayerInv.Empty) return true;

			var ray = __instance.camera.ScreenPointToRay(Input.mousePosition);
			//bool flag = EventSystem.current.IsPointerOverGameObject();
			if (Physics.Raycast(ray, out var raycastHit))
			{
				if (raycastHit.transform.GetComponent<NetworkedPlayer>() != null)
					return false;
				if (raycastHit.transform.GetComponent<NetworkedSim>() != null)
					return false;
				if (raycastHit.transform.GetComponent<Character>() != null)
					return true;
				if (raycastHit.transform.GetComponent<Door>() != null)
					return true;
				if (raycastHit.transform.tag == "Bind")
					return true;
				if (raycastHit.transform.tag == "Forge")
					return true;
				if (raycastHit.transform.tag == "Treasure")
					return true;

				List<RaycastResult> list = new();
				PointerEventData pointerEventData = new(EventSystem.current)
				{
					position = Input.mousePosition
				};
				EventSystem.current.RaycastAll(pointerEventData, list);
				if (list.Count > 0)
				{
					foreach (RaycastResult raycastResult in list)
					{
						if (raycastResult.gameObject.layer == LayerMask.NameToLayer("UI"))
						{
							return true;
						}
					}
				}

				if (GameData.MouseSlot.MyItem != GameData.PlayerInv.Empty && !GameData.Trading)
				{
					ClientConnectionManager.Instance.DropItem(GameData.MouseSlot.MyItem, GameData.MouseSlot.Quantity);
					return false;
				}
			}

			return true;
		}


		public static FieldInfo downCD;

		public static bool LootWindowClose_Prefix()
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;

			if (Variables.lastDroppedItem == null) return true;

			if (!GameData.LootWindow.WindowParent.activeSelf)
			{
				return true;
			}
			GameData.PlayerControl.GetComponent<Animator>().SetTrigger("EndLoot");
			GameData.PlayerAud.PlayOneShot(GameData.Misc.CloseWindow, GameData.SFXVol * 0.05f);
			if (GameData.LootWindow.WindowParent.activeSelf && !GameData.InCharSelect)
			{
				GameData.GM.SaveGameData(true);
				int num = 0;
				List<Item> list = new();
				foreach (ItemIcon itemIcon in GameData.LootWindow.LootSlots)
				{
					if (itemIcon.MyItem != GameData.PlayerInv.Empty)
					{
						list.Add(itemIcon.MyItem);
						num++;
					}
					
					itemIcon.MyItem = GameData.PlayerInv.Empty;
					itemIcon.UpdateSlotImage();
					GameData.PlayerInv.ForceCloseInv();
				}

				
				GameData.LootWindow.WindowParent.SetActive(false);

				if ((num <= 0 && Variables.lastDroppedItem.quality <= 0 && Variables.lastDroppedItem.item.RequiredSlot == Item.SlotType.General) 
					|| (num <= 0 && Variables.lastDroppedItem.item.RequiredSlot != Item.SlotType.General))
				{
					ClientConnectionManager.Instance.SendItemLooted(Variables.lastDroppedItem.id);
					Object.Destroy(Variables.lastDroppedItem.gameObject);
				}
				else
				{
					Variables.lastDroppedItem.ReturnLoot(list);
				}
			}

			return false;
		}

		public static void InventoryUpdate_Postfix(Inventory __instance)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return;

			if (__instance.isPlayer && Input.GetKeyDown(InputManager.Loot) && !__instance.InvWindow.activeSelf && !GameData.PlayerTyping)
			{
				DroppedItem nearest = null;
				float dis = float.PositiveInfinity;
				var allDropped = Object.FindObjectsOfType<DroppedItem>();
				foreach (var drop in allDropped)
				{
					var dd = Vector3.Distance(drop.transform.position, __instance.transform.position);
					if (dd < dis)
					{
						nearest = drop;
						dis = dd;
					}
				}
				
				if (nearest != null && dis < 1.5f)
				{
					GameData.PlayerCombat.ForceAttackOff();
					nearest.LoadLootTable();
					GameData.PlayerControl.Myself.GetComponent<Animator>().SetTrigger("StartLoot");
				}
			}
		}


#region CHARMEDNPC


		public static void CharmedGoAway_Prefix(CharmedNPC __instance)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return;

			if (GameData.PlayerControl.Myself.MyCharmedNPC.SummonedByPlayer)
			{
				ClientConnectionManager.Instance.LocalPlayer.DespawnSummon();
			}
		}


#endregion




#region NPCFIGHTEVENT

		private static FieldInfo _actualSpawn;
		private static List<GameObject> fightSpawnList = new();
		private static List<string> fightSpawnListID = new();

		public static bool FightFixedUpdate_Prefix(NPCFightEvent __instance)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;
			if (!ClientZoneOwnership.isZoneOwner) return false;

			bool runSpawn = false;

			

			if (__instance.MyNPC.CurrentAggroTarget != null && __instance.MyStats.Myself.Alive)
			{
				if (__instance.PercentagesToSpawn.Count > 0 && (float)__instance.MyStats.CurrentHP / (float)__instance.MyStats.CurrentMaxHP * 100f < __instance.PercentagesToSpawn[0])
				{
					UpdateSocialLog.LogAdd(__instance.PromptOnSpawn, "orange");
					__instance.PercentagesToSpawn.RemoveAt(0);
					int idx = 0;
					foreach (GameObject original in __instance.SpawnAdds)
					{
						var obj = UnityEngine.Object.Instantiate(original, __instance.transform.position + new Vector3((float)Random.Range(-11, 11), 0f, (float)Random.Range(-11, 11)), __instance.transform.rotation);
						fightSpawnList.Add(obj);
						fightSpawnListID.Add($"1,{idx}");
						idx++;
					}

					runSpawn = true;
				}
				if (__instance.SpawnAddsEveryXSeconds > 0f)
				{
					float curVal = (float)_actualSpawn.GetValue(__instance);
					if (curVal > 0f)
					{
						_actualSpawn.SetValue(__instance, curVal - 1f);
						//__instance.actualSpawn -= 1f;
					}
					else
					{
						UpdateSocialLog.LogAdd(__instance.PromptOnSpawn, "orange");
						int idx = 0;
						foreach (GameObject original2 in __instance.SpawnAdds)
						{
							var obj = UnityEngine.Object.Instantiate(original2, __instance.transform.position + new Vector3((float)Random.Range(-11, 11), 0f, (float)Random.Range(-11, 11)), __instance.transform.rotation);
							fightSpawnList.Add(obj);
							fightSpawnListID.Add($"2,{idx}");
							idx++;
						}

						_actualSpawn.SetValue(__instance, __instance.SpawnAddsEveryXSeconds * 60f);
						//__instance.actualSpawn = __instance.SpawnAddsEveryXSeconds * 60f;
						runSpawn = true;
					}
				}
			}

			if (__instance.SpawnOnDeath.Count > 0 && !__instance.MyStats.Myself.Alive)
			{
				int idx = 0;
				foreach (GameObject original3 in __instance.SpawnOnDeath)
				{
					var obj = UnityEngine.Object.Instantiate(original3, __instance.transform.position + new Vector3(Random.Range(-__instance.transform.localScale.magnitude, __instance.transform.localScale.magnitude), 0f, Random.Range(-__instance.transform.localScale.magnitude, __instance.transform.localScale.magnitude)), __instance.transform.rotation);
					fightSpawnList.Add(obj);
					fightSpawnListID.Add($"3,{idx}");
					idx++;
				}
				__instance.SpawnOnDeath.Clear();
				UpdateSocialLog.LogAdd(__instance.PromptOnSpawn, "orange");
				if (__instance.InstantDespawn)
				{
					__instance.MyStats.Myself.MyNPC.ExpediteRot();
				}

				runSpawn = true;
			}

			if (fightSpawnList.Count > 0)
			{
				SharedNPCSyncManager.Instance.ServerSpawnMobs(fightSpawnList,fightSpawnListID, (int)CustomSpawnID.ADDS, __instance.MyNPC);
				fightSpawnList.Clear();
				fightSpawnListID.Clear();
			}

			return !runSpawn;

		}



#endregion


#region MALAROTHFEED + CHESS + SIRAETHE

		private static FieldInfo _malarothSpawn;
		private static FieldInfo _chessSpawn;
		private static bool isMalaroth = false;
		private static byte chessSpawn = 0;
		public static bool MCheckForGamepiece_Prefix()
		{
			if (ClientConnectionManager.Instance.IsRunning)
			{
				if (!ClientZoneOwnership.isZoneOwner) return false;
			}
			return true;
		}
		public static bool CCheckForGamepiece_Prefix()
		{
			if (ClientConnectionManager.Instance.IsRunning)
			{
				if (!ClientZoneOwnership.isZoneOwner) return false;
			}

			return true;
		}
		public static bool SUpdate_Prefix()
		{
			if (ClientConnectionManager.Instance.IsRunning)
			{
				if (!ClientZoneOwnership.isZoneOwner)
				{
					hasWardOne = false;
					hasWardTwo = false;
					hasWardThree = false;
					return false;
				}
			}
			return true;
		}


		public static void MSpawnPiece_Prefix(MalarothFeed __instance, GameObject _npc)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return;

			isMalaroth = _npc == __instance.Malaroth;
		}
		public static void CSpawnPiece_Prefix(Chessboard __instance, GameObject _npc)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return;

			if (_npc == __instance.PeonNPC) chessSpawn = 1;
			if (_npc == __instance.EmberNPC) chessSpawn = 2;
			if (_npc == __instance.BlazeNPC) chessSpawn = 3;
			if (_npc == __instance.MonarchNPC) chessSpawn = 4;
			if (_npc == __instance.KingsmanNPC) chessSpawn = 5;
			if (_npc == __instance.CandlekeeperNPC) chessSpawn = 6;
			if (_npc == __instance.FacelessDuel) chessSpawn = 7;
			if (_npc == __instance.FacelessArc) chessSpawn = 8;
			if (_npc == __instance.FacelessPal) chessSpawn = 9;
			if (_npc == __instance.FacelessDru) chessSpawn = 10;
		}


		public static void MSpawnPiece_Postfix(MalarothFeed __instance, GameObject _npc)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return;

			var mal = _malarothSpawn.GetValue(__instance) as Character;
			if (mal != null)
			{
				SharedNPCSyncManager.Instance.ServerSpawnMob(mal.gameObject, (int)CustomSpawnID.MALAROTH, "0", isMalaroth, mal.transform.position, mal.transform.rotation);
			}
		}
		public static void CSpawnPiece_Postfix(Chessboard __instance, GameObject _npc)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return;
			
			var ch = _chessSpawn.GetValue(__instance) as Character;
			if (ch != null)
			{
				SharedNPCSyncManager.Instance.ServerSpawnMob(ch.gameObject, (int)CustomSpawnID.CHESS, chessSpawn.ToString(), false, ch.transform.position, ch.transform.rotation);
			}
		}

		private static bool hasWardOne = false;
		private static bool hasWardTwo = false;
		private static bool hasWardThree = false;
		public static void SUpdate_Postfix(SiraetheEvent __instance)
		{
			if (!ClientConnectionManager.Instance.IsRunning || !ClientZoneOwnership.isZoneOwner) return;

			if (__instance.WardOne != null && !hasWardOne)
			{
				SharedNPCSyncManager.Instance.ServerSpawnMob(__instance.WardOne, (int)CustomSpawnID.SIRAETHE, "1", false, __instance.WardOne.transform.position, __instance.WardOne.transform.rotation);
				hasWardOne = true;
			}else if (__instance.WardOne == null && hasWardOne)
			{
				hasWardOne = false;
			}
			if (__instance.WardTwo != null && !hasWardTwo)
			{
				SharedNPCSyncManager.Instance.ServerSpawnMob(__instance.WardTwo, (int)CustomSpawnID.SIRAETHE, "2", false, __instance.WardTwo.transform.position, __instance.WardTwo.transform.rotation);
				hasWardTwo = true;
			}else if (__instance.WardTwo == null && hasWardTwo)
			{
				hasWardTwo = false;
			}
			if (__instance.WardThree != null && !hasWardThree)
			{
				SharedNPCSyncManager.Instance.ServerSpawnMob(__instance.WardThree, (int)CustomSpawnID.SIRAETHE, "3", false, __instance.WardThree.transform.position, __instance.WardThree.transform.rotation);
				hasWardThree = true;
			}else if (__instance.WardThree == null && hasWardThree)
			{
				hasWardThree = false;
			}

		}


#endregion


		public static void SyncHealing(Spell spell, Stats target, int amount, bool isCrit, Character caster, bool isMP)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return;

			(bool IsPlayer, bool IsSim, short entityID) = GetEntityIDByCharacter(caster);
			if (entityID == -1)	return;

			if ( ( !IsSim && !ClientZoneOwnership.isZoneOwner ) )
			{
				Logging.Log($"er {IsPlayer} {IsSim} {entityID}");
				return;
			}

			if (IsPlayer && entityID != ClientConnectionManager.Instance.LocalPlayerID)
			{
				Logging.Log($"er NoLocalPlayer");
				return;
			}

			(bool targetIsPlayer, bool targetIsSim, short targetEntityID) = GetEntityIDByCharacter(target.Myself);
			if (targetEntityID == -1)
			{
				Logging.Log($"no target {targetIsPlayer} {targetIsSim} {targetEntityID} {target.name}");
				return;
			}

			var hd = new HealingData
			{
				amount = amount,
				isCrit = isCrit,
				isMP = isMP,
				targetID = targetEntityID,
				targetIsNPC = !targetIsPlayer && !targetIsSim,
				targetIsSim = targetIsSim,
			};

			//Logging.Log($"[{caster.name}] healed [{target.name}] for {amount} {(isMP?"MP":"HP")}. Text = {text}");
			if (!IsPlayer)
			{
				if (IsSim)
				{
					var ent = SharedNPCSyncManager.Instance.GetEntityFromID(entityID, true);
					if(ent != null) //make sure its local
						((SimSync)ent).SendHeal(hd);
				}
				else
					((NPCSync)SharedNPCSyncManager.Instance.GetEntityFromID(entityID, false))?.SendHeal(hd);
			}
			else
			{
				ClientConnectionManager.Instance.LocalPlayer.SendHeal(hd);
			}
		}



#region GAMEMANAGER


		public static void OpenEscMenu_Postfix()
		{
			UI.Main.isGameMenuOpen = true;
			if (UI.Main.connectUI != null)
				UI.Main.connectUI.SetActive(true);
			//Logging.Log("open esc");
		}

		public static void CloseEscMenu_Postfix()
		{
			UI.Main.isGameMenuOpen = false;
			if(UI.Main.connectUI != null)
				UI.Main.connectUI.SetActive(false);
			//Logging.Log("close esc");
		}

		public static void ToggleEscMenu_Postfix(GameManager __instance)
		{
			UI.Main.isGameMenuOpen = __instance.EscapeMenu.activeSelf;
			if (UI.Main.connectUI != null)
				UI.Main.connectUI.SetActive(UI.Main.isGameMenuOpen);
			//Logging.Log("toggle esc");
		}


#endregion


#region GAMEDATA

		public static bool AddExperience_Prefix(int xp, bool useMod)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;
			if (Grouping.currentGroup.groupList == null || Grouping.currentGroup.groupList.Count <= 1) return true;

			if (Grouping.currentGroup.groupList.Count > 1)
			{
				if (Grouping.IsLocalLeader())
				{
					//Logging.Log($"is leader");
					var XPBonus = (float)xpBonus.GetValue(GameData.PlayerStats);
					//Send xp
					if (!ServerConnectionManager.Instance.IsRunning)
					{
						//Logging.Log($"doing non-host xp");
						PacketManager.GetOrCreatePacket<GroupPacket>(ClientConnectionManager.Instance.LocalPlayerID, PacketType.GROUP)
							.AddPacketData(GroupDataType.EXPERIENCE, "xp", xp)
							.SetData("xpBonus", XPBonus)
							.SetData("useMod",  useMod);
					}
					else
					{
						//Logging.Log($"doing host xp");
						Grouping.ServerHandleXP(ClientConnectionManager.Instance.LocalPlayerID, xp, useMod, XPBonus);
					}


					return false;
				}
				else
				{
					//Logging.Log($"not leader");
				}

				return false;
			}

			return true;
		}

#endregion



#region SIMPLAYERMNGR

		public static bool BringGroup_Prefix(SimPlayerMngr __instance)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;

			//If we aren't the host... we are outta here, we don't handle sims
			if (!ServerConnectionManager.Instance.IsRunning) return false;

			if (GameData.GroupMember1 != null && GameData.GroupMember1.simIndex >= 0)
			{
				__instance.ActiveSimInstances.Add(GameData.GroupMember1.SpawnMeInGame(GameData.PlayerControl.transform.position + new Vector3((float)Random.Range(-1, 1), 0f, (float)Random.Range(-1, 1))));
				GameData.GroupMember1.MyAvatar.InGroup = true;
				GameData.GroupMember1.isPuller = false;
				GameData.GroupMember1.Caution = false;
				GameData.GroupMember1.CurScene = SceneManager.GetActiveScene().name;
				__instance.SimsInZones[GameData.GroupMember1.simIndex] = SceneManager.GetActiveScene().name;
			}
			if (GameData.GroupMember2 != null && GameData.GroupMember2.simIndex >= 0)
			{
				__instance.ActiveSimInstances.Add(GameData.GroupMember2.SpawnMeInGame(GameData.PlayerControl.transform.position + new Vector3((float)Random.Range(-1, 1), 0f, (float)Random.Range(-1, 1))));
				GameData.GroupMember2.MyAvatar.InGroup = true;
				GameData.GroupMember2.isPuller = false;
				GameData.GroupMember2.Caution = false;
				GameData.GroupMember2.CurScene = SceneManager.GetActiveScene().name;
				__instance.SimsInZones[GameData.GroupMember2.simIndex] = SceneManager.GetActiveScene().name;
			}
			if (GameData.GroupMember3 != null && GameData.GroupMember3.simIndex >= 0)
			{
				__instance.ActiveSimInstances.Add(GameData.GroupMember3.SpawnMeInGame(GameData.PlayerControl.transform.position + new Vector3((float)Random.Range(-1, 1), 0f, (float)Random.Range(-1, 1))));
				GameData.GroupMember3.MyAvatar.InGroup = true;
				GameData.GroupMember3.isPuller = false;
				GameData.GroupMember3.Caution = false;
				GameData.GroupMember3.CurScene = SceneManager.GetActiveScene().name;
				__instance.SimsInZones[GameData.GroupMember3.simIndex] = SceneManager.GetActiveScene().name;
			}

			return false;
		}

#endregion

#region SPELLVESSEL

		public static FieldInfo targ;
		public static FieldInfo SpellSource;

		public static bool ResolveSpell_Prefix(SpellVessel __instance)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;

			var chara = ((CastSpell)SpellSource.GetValue(__instance)).MyChar;
			var ent = GetEntityByCharacter(chara);
			if (ent == null) return true;


			var target = (Stats)targ.GetValue(__instance);
			if (target == null) return true;
			var targEnt = GetEntityByStats(target);
			if (targEnt == null) return true;

			if (ent is PlayerSync || ent is SimSync)
			{
				var pack = PacketManager.GetOrCreatePacket<PlayerActionPacket>(ent.entityID, PacketType.PLAYER_ACTION);

				pack.dataTypes.Add(ActionType.SPELL_EFFECT);
				pack.spellID = __instance.spell.Id;
				pack.targetID = targEnt.entityID;
				pack.targetIsNPC = targEnt.type == EntityType.ENEMY;
				pack.targetIsSim = targEnt.type == EntityType.SIM;
				pack.isSim = ent is SimSync;
			}
			else if(ent is NPCSync)
			{
				var pack = PacketManager.GetOrCreatePacket<EntityActionPacket>(ent.entityID, PacketType.ENTITY_ACTION);
				pack.dataTypes.Add(ActionType.SPELL_EFFECT);
				pack.spellID = __instance.spell.Id;
				pack.targetID = targEnt.entityID;
				pack.targetIsNPC = targEnt.type == EntityType.ENEMY;
				pack.targetIsSim = targEnt.type == EntityType.SIM;
				pack.targetPlayerIDs = SharedNPCSyncManager.Instance.GetPlayerSendList();
				pack.entityType = ent.type;
			}
			return true;
		}

		public static void EndSpell_Prefix(SpellVessel __instance)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return;

			var chara = ((CastSpell)SpellSource.GetValue(__instance)).MyChar;
			var ent = GetEntityByCharacter(chara);
			if (ent == null) return;

			if (ent is PlayerSync || ent is SimSync)
			{
				PlayerActionPacket pack;

				pack = PacketManager.GetOrCreatePacket<PlayerActionPacket>(ent.entityID, PacketType.PLAYER_ACTION);
				pack.dataTypes.Add(ActionType.SPELL_END);

				if (__instance.spell.Type == Spell.SpellType.Pet)
				{
					if (chara.MyCharmedNPC != null)
					{
						if (ent is PlayerSync)
							ClientConnectionManager.Instance.LocalPlayer.CreateSummon(__instance.spell, chara.MyCharmedNPC.gameObject);
						else
						{
							ent.CreateSummon(__instance.spell, chara.MyCharmedNPC.gameObject);
						}
					}
				}
				pack.isSim = ent is SimSync;
			}
			else if(ent is NPCSync)
			{
				var pack = PacketManager.GetOrCreatePacket<EntityActionPacket>(ent.entityID, PacketType.ENTITY_ACTION);
				pack.dataTypes.Add(ActionType.SPELL_END);
				pack.targetPlayerIDs = SharedNPCSyncManager.Instance.GetPlayerSendList();

				if (__instance.spell.Type == Spell.SpellType.Pet)
				{
					if (chara.MyCharmedNPC != null)
					{
						var en = GetEntityByCharacter(chara);
						en.CreateSummon(__instance.spell, chara.MyCharmedNPC.gameObject);
					}
				}
				pack.entityType = ent.type;
			}
		}

		public static void ChargeEffect_Prefix(SpellVessel __instance, Spell _spell, Transform _caster, Stats _target, CastSpell _source, float _castTime, bool _useMana, bool _resonate)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return;

			var chara = _source.MyChar;
			var ent = GetEntityByCharacter(chara);
			if (ent == null) return;

			if (ent is PlayerSync || ent is SimSync)
			{
				var pack = PacketManager.GetOrCreatePacket<PlayerActionPacket>(ent.entityID, PacketType.PLAYER_ACTION);
				pack.dataTypes.Add(ActionType.SPELL_CHARGE);
				pack.SpellChargeFXIndex = _spell.SpellChargeFXIndex;
				pack.isSim = ent is SimSync;
			}
			else if (ent is NPCSync)
			{
				var pack = PacketManager.GetOrCreatePacket<EntityActionPacket>(ent.entityID, PacketType.ENTITY_ACTION);
				pack.dataTypes.Add(ActionType.SPELL_CHARGE);
				pack.SpellChargeFXIndex = _spell.SpellChargeFXIndex;
				pack.targetPlayerIDs = SharedNPCSyncManager.Instance.GetPlayerSendList();
				pack.entityType = ent.type;
			}
		}

#endregion



#region SIMPLAYERTRACKING


		public static bool SpawnMeInGame_Prefix(SimPlayerTracking __instance)
		{
			if (__instance.simIndex < 0) return false;
			return true;
		}


#endregion



#region SIMPLAYERGROUPING

		//Yes it's annoying to have to do each individually, the game should really be using a list to address group members.
		public static bool DismissMember1_Prefix(SimPlayerGrouping __instance)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;

			if (GameData.GroupMember1 != null && GameData.GroupMember1.MyAvatar != null && GameData.GroupMember1.simIndex < 0)
			{
				int pid = Math.Abs(GameData.GroupMember1.simIndex) - 1;
				var player = ClientConnectionManager.Instance.GetPlayerFromID((short)pid);
				if (player != null)
				{
					//we also need to check something else here, because the simindex can be the same as a player id
					if (( (NetworkedPlayer)player ).sim.MyStats == GameData.GroupMember1.MyStats)
					{
						Grouping.RemoveFromGroup((short)pid);
						return false;
					}
				}
			}
			

			//if (ServerConnectionManager.Instance.IsRunning)
			{

				if (GameData.GroupMember1 != null && GameData.GroupMember1.MyAvatar != null)
				{
					Entity npcsync = GameData.GroupMember1.MyAvatar.GetComponent<SimSync>();
					if (npcsync == null)
						npcsync = GameData.GroupMember1.MyAvatar.GetComponent<NetworkedSim>();
					if (npcsync != null)
					{
						//SharedNPCSyncManager.Instance.ServerRemoveSim(npcsync.entityID);
						Grouping.RemoveFromGroup(npcsync.entityID);
						return false;
					}
				}
			}

			return true;
		}

		public static bool DismissMember2_Prefix(SimPlayerGrouping __instance)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;

			if (GameData.GroupMember2 != null && GameData.GroupMember2.MyAvatar != null && GameData.GroupMember2.simIndex < 0)
			{
				int pid = Math.Abs(GameData.GroupMember2.simIndex) - 1;
				var player = ClientConnectionManager.Instance.GetPlayerFromID((short)pid);
				if (player != null)
				{
					//we also need to check something else here, because the simindex can be the same as a player id
					if (( (NetworkedPlayer)player ).sim.MyStats == GameData.GroupMember2.MyStats)
					{
						Grouping.RemoveFromGroup((short)pid);
						return false;
					}
				}
			}
			

			//if (ServerConnectionManager.Instance.IsRunning)
			{
				if (GameData.GroupMember2 != null && GameData.GroupMember2.MyAvatar != null)
				{
					Entity npcsync = GameData.GroupMember2.MyAvatar.GetComponent<SimSync>();
					if (npcsync == null)
						npcsync = GameData.GroupMember2.MyAvatar.GetComponent<NetworkedSim>();
					if (npcsync != null)
					{
						//SharedNPCSyncManager.Instance.ServerRemoveSim(npcsync.entityID);
						Grouping.RemoveFromGroup(npcsync.entityID);
						return false;
					}
				}
			}

			return true;
		}
		public static bool DismissMember3_Prefix(SimPlayerGrouping __instance)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;

			
			if (GameData.GroupMember3 != null && GameData.GroupMember3.MyAvatar != null && GameData.GroupMember3.simIndex < 0)
			{
				int pid = Math.Abs(GameData.GroupMember3.simIndex) - 1;
				var player = ClientConnectionManager.Instance.GetPlayerFromID((short)pid);
				if (player != null)
				{
					//we also need to check something else here, because the simindex can be the same as a player id
					if (( (NetworkedPlayer)player ).sim.MyStats == GameData.GroupMember3.MyStats)
					{
						Grouping.RemoveFromGroup((short)pid);
						return false;
					}
				}
			}
			

			//if (ServerConnectionManager.Instance.IsRunning)
			{
				if (GameData.GroupMember3 != null && GameData.GroupMember3.MyAvatar != null)
				{
					Entity npcsync = GameData.GroupMember3.MyAvatar.GetComponent<SimSync>();
					if(npcsync == null)
						npcsync = GameData.GroupMember3.MyAvatar.GetComponent<NetworkedSim>();
					if (npcsync != null)
					{
						//SharedNPCSyncManager.Instance.ServerRemoveSim(npcsync.entityID);
						Grouping.RemoveFromGroup(npcsync.entityID);
						return false;
					}
				}
			}

			return true;
		}

		/*private static bool noInvitePostFix = false;
		//At this point we know we haven't invited a player
		public static void InviteToGroup_Postfix(SimPlayerGrouping __instance)
		{
			return;

			if (!noInvitePostFix)
			{
				Logging.Log($"pf run");
				//We know if it's a sim if the index is geater or equal to 0 because we're writing negatives for players
				if (GameData.GroupMember1 != null && GameData.GroupMember1.simIndex >= 0)
				{
					Entity npcsync = GameData.GroupMember1.MyAvatar.GetComponent<SimSync>();
					if (npcsync == null)
						npcsync = GameData.GroupMember1.MyAvatar.GetComponent<NetworkedSim>();
					if(npcsync != null && !Grouping.IsPlayerInGroup(npcsync.entityID, true))
						Grouping.InvitePlayer(npcsync);
				}

				if (GameData.GroupMember2 != null && GameData.GroupMember2.simIndex >= 0)
				{
					Entity npcsync = GameData.GroupMember2.MyAvatar.GetComponent<SimSync>();
					if (npcsync == null)
						npcsync = GameData.GroupMember2.MyAvatar.GetComponent<NetworkedSim>();
					if (npcsync != null && !Grouping.IsPlayerInGroup(npcsync.entityID, true))
						Grouping.InvitePlayer(npcsync);
				}

				if (GameData.GroupMember3 != null && GameData.GroupMember3.simIndex >= 0)
				{
					Entity npcsync = GameData.GroupMember3.MyAvatar.GetComponent<SimSync>();
					if (npcsync == null)
						npcsync = GameData.GroupMember3.MyAvatar.GetComponent<NetworkedSim>();
					if (npcsync != null && !Grouping.IsPlayerInGroup(npcsync.entityID, true))
						Grouping.InvitePlayer(npcsync);
				}
			}

			noInvitePostFix = false;

		}*/

		public static bool InviteToGroup_Prefix(SimPlayerGrouping __instance)
		{
			if (ClientConnectionManager.Instance.IsRunning || ServerConnectionManager.Instance.IsRunning)
			{
				Character currentTarget = GameData.PlayerControl.CurrentTarget;
				if (currentTarget == null) return true;

				Entity targ = currentTarget.GetComponent<Entity>();

				if (targ != null && (targ is SimSync || targ is NetworkedSim || targ is NetworkedPlayer))
				{
					if (!GameData.PlayerControl.Myself.Alive) return true;
					Grouping.InvitePlayer(targ);

					return false;
				}
			}
			return true;
		}

#endregion



#region REVIVE

		public static void RespawnPlayer_Postfix()
		{
			if (ClientConnectionManager.Instance.IsRunning)
			{
				var pack = PacketManager.GetOrCreatePacket<PlayerActionPacket>(ClientConnectionManager.Instance.LocalPlayerID, PacketType.PLAYER_ACTION);
				pack.dataTypes.Add(ActionType.REVIVE);
			}
		}

#endregion


#region MISC

		public static bool MiscGenPopup_Prefix(int _dmg, bool _crit, GameData.DamageType _type, Transform _tar)
		{
			if (ClientConnectionManager.Instance.IsRunning)
			{
				Debug.Log($"{_dmg} {_crit} {_type} {_tar.position}");
			}
			return true;
		}

#endregion


#region STATS

		public static FieldInfo xpBonus;

		private static (bool,bool,short) GetEntityIDByCharacter(Character _char)
		{
			foreach (var mob in ClientNPCSyncManager.Instance.NetworkedMobs)
			{
				if (mob.Value.character == _char)
				{
					return (false,false,mob.Value.entityID);
				}
			}

			foreach (var mob in ClientNPCSyncManager.Instance.NetworkedSims)
			{
				if (mob.Value.character == _char)
				{
					return (false, true, mob.Value.entityID);
				}
			}

			foreach (var player in ClientConnectionManager.Instance.Players)
			{
				if (player.Value.character == _char)
				{
					return (true, false, player.Value.entityID);
				}
			}

			foreach (var mob in SharedNPCSyncManager.Instance.mobs)
			{
				if (mob.Value.character == _char)
				{
					return (false, true, mob.Value.entityID);
				}
			}

			//if (ServerConnectionManager.Instance.IsRunning)
			{
				foreach (var mob in SharedNPCSyncManager.Instance.sims)
				{
					if (mob.Value.character == _char)
					{
						return (false, true, mob.Value.entityID);
					}
				}
			}

			if (ClientConnectionManager.Instance.LocalPlayer.character == _char)
				return ( true, false, ClientConnectionManager.Instance.LocalPlayerID );

			return ( false, false, -1 );
		}

		public static Entity GetEntityByCharacter(Character _char)
		{
			foreach (var mob in ClientNPCSyncManager.Instance.NetworkedMobs)
			{
				if (mob.Value.character == _char)
				{
					return mob.Value;
				}
			}

			foreach (var mob in ClientNPCSyncManager.Instance.NetworkedSims)
			{
				if (mob.Value.character == _char)
				{
					return mob.Value;
				}
			}

			foreach (var player in ClientConnectionManager.Instance.Players)
			{
				if (player.Value.character == _char)
				{
					return player.Value;
				}
			}

			//if (ServerConnectionManager.Instance.IsRunning)
			{
				foreach (var mob in SharedNPCSyncManager.Instance.sims)
				{
					if (mob.Value.character == _char)
					{
						return mob.Value;
					}
				}
			}

			if (ClientConnectionManager.Instance.LocalPlayer.character == _char)
				return ClientConnectionManager.Instance.LocalPlayer;

			return null;
		}

		private static (bool, bool, short) GetEntityIDByStats(Stats _char)
		{
			foreach (var mob in ClientNPCSyncManager.Instance.NetworkedMobs)
			{
				if (mob.Value.character.MyStats == _char)
				{
					return (false, false, mob.Value.entityID);
				}
			}

			foreach (var mob in ClientNPCSyncManager.Instance.NetworkedSims)
			{
				if (mob.Value.character.MyStats == _char)
				{
					return (false, true, mob.Value.entityID);
				}
			}

			foreach (var player in ClientConnectionManager.Instance.Players)
			{
				if (player.Value.character.MyStats == _char)
				{
					return (true, false, player.Value.entityID);
				}
			}

			if (ClientConnectionManager.Instance.LocalPlayer.character.MyStats == _char)
				return (true, false, ClientConnectionManager.Instance.LocalPlayerID);

			return (false, false, -1);
		}
		private static Entity GetEntityByStats(Stats _char)
		{
			if (!ClientZoneOwnership.isZoneOwner)
			{
				foreach (var mob in ClientNPCSyncManager.Instance.NetworkedMobs)
				{
					if (mob.Value.character.MyStats == _char)
					{
						return mob.Value;
					}
				}

				foreach (var mob in ClientNPCSyncManager.Instance.NetworkedSims)
				{
					if (mob.Value.character.MyStats == _char)
					{
						return mob.Value;
					}
				}
			}
			else
			{
				foreach (var mob in SharedNPCSyncManager.Instance.mobs)
				{
					if (mob.Value.character.MyStats == _char)
					{
						return mob.Value;
					}
				}

				
			}

			foreach (var mob in SharedNPCSyncManager.Instance.sims)
			{
				if (mob.Value.character.MyStats == _char)
				{
					return mob.Value;
				}
			}

			foreach (var player in ClientConnectionManager.Instance.Players)
			{
				if (player.Value.character.MyStats == _char)
				{
					return player.Value;
				}
			}

			if (ClientConnectionManager.Instance.LocalPlayer.character.MyStats == _char)
				return ClientConnectionManager.Instance.LocalPlayer;

			return null;
		}

		public static void AddStatusEffectType1_Prefix(Stats __instance, Spell spell, bool _fromPlayer, int _dmgBonus)
		{
			if (ClientConnectionManager.Instance.IsRunning)
			{
				if (ClientConnectionManager.Instance.LocalPlayer.stats == __instance)
				{
					if (Variables.DontCheckEffectCharacters.Contains(ClientConnectionManager.Instance.LocalPlayer)) return;

					string spellID = spell.Id;
					var pack = PacketManager.GetOrCreatePacket<PlayerActionPacket>(ClientConnectionManager.Instance.LocalPlayerID, PacketType.PLAYER_ACTION);
					pack.dataTypes.Add(ActionType.STATUS_EFFECT_APPLY);
					pack.effectData = new StatusEffectData()
					{
						spellID =  spellID,
						damageBonus = _dmgBonus,
						targetID =  -1,
						duration = -1,
					};

				}
			}
		}

		public static void AddStatusEffectType2_Prefix(Stats __instance, Spell spell, bool _fromPlayer, int _dmgBonus, Character _specificCaster)
		{
			if (ClientConnectionManager.Instance.IsRunning)
			{
				Entity target = GetEntityByStats(__instance);
				Entity caster = GetEntityByCharacter(_specificCaster);

				if (target == null) return;
				if(caster == null) return;


				if (Variables.DontCheckEffectCharacters.Contains(target)) return;

				//If we cast something on ourselves
				if (target == ClientConnectionManager.Instance.LocalPlayer && caster == ClientConnectionManager.Instance.LocalPlayer)
				{
					string spellID = spell.Id;
					var pack = PacketManager.GetOrCreatePacket<PlayerActionPacket>(ClientConnectionManager.Instance.LocalPlayerID, PacketType.PLAYER_ACTION);
					pack.dataTypes.Add(ActionType.STATUS_EFFECT_APPLY);
					pack.effectData = new StatusEffectData()
					{
						spellID = spellID,
						damageBonus = _dmgBonus,
						casterType = EntityType.PLAYER,
						targetID = -2,
						duration = -1,
					};
				}
				else
				{
					//if we are the caster
					if (caster == ClientConnectionManager.Instance.LocalPlayer)
					{
						string spellID = spell.Id;
						var pack = PacketManager.GetOrCreatePacket<PlayerActionPacket>(ClientConnectionManager.Instance.LocalPlayerID, PacketType.PLAYER_ACTION);
						pack.dataTypes.Add(ActionType.STATUS_EFFECT_APPLY);
						pack.effectData = new StatusEffectData()
						{
							spellID = spellID,
							damageBonus = _dmgBonus,
							casterType = EntityType.PLAYER,
							casterID = caster.entityID,
							duration = -1,
							targetID = target.entityID,
							targetType = target.type,
						};
						if (target is NetworkedPlayer)
							pack.effectData.targetType = EntityType.PLAYER;

					}
					else //We're not the caster
					{
						if (ClientZoneOwnership.isZoneOwner || caster is SimSync)
						{
							if (caster is SimSync)
							{
								string spellID = spell.Id;
								var pack = PacketManager.GetOrCreatePacket<PlayerActionPacket>(caster.entityID, PacketType.PLAYER_ACTION);
								pack.dataTypes.Add(ActionType.STATUS_EFFECT_APPLY);
								pack.effectData = new StatusEffectData()
								{
									spellID = spellID,
									damageBonus = _dmgBonus,
									casterType = caster.type,
									casterID = caster.entityID,
									duration = -1,
									targetID = target.entityID,
									targetType = target.type,
								};
								if (target is NetworkedPlayer)
									pack.effectData.targetType = EntityType.PLAYER;
								pack.targetPlayerIDs = SharedNPCSyncManager.Instance.GetPlayerSendList();
								pack.isSim = true;
							}
							else
							{
								string spellID = spell.Id;
								var pack = PacketManager.GetOrCreatePacket<EntityActionPacket>(caster.entityID, PacketType.ENTITY_ACTION);
								pack.dataTypes.Add(ActionType.STATUS_EFFECT_APPLY);
								pack.effectData = new StatusEffectData()
								{
									spellID = spellID,
									damageBonus = _dmgBonus,
									casterType = caster.type,
									casterID = caster.entityID,
									duration = -1,
									targetID = target.entityID,
									targetType = target.type,
								};
								if (target is NetworkedPlayer)
									pack.effectData.targetType = EntityType.PLAYER;
								pack.targetPlayerIDs = SharedNPCSyncManager.Instance.GetPlayerSendList();
							}
						}
					}
				}
			}
		}

		public static void AddStatusEffectType3_Prefix(Stats __instance, Spell spell, bool _fromPlayer, int _dmgBonus, Character _specificCaster, float _duration)
		{
			if (ClientConnectionManager.Instance.IsRunning)
			{
				Entity target = GetEntityByStats(__instance);
				Entity caster = GetEntityByCharacter(_specificCaster);

				if (target == null) return;
				if (caster == null) return;


				if (Variables.DontCheckEffectCharacters.Contains(target)) return;

				//If we cast something on ourselves
				if (target == ClientConnectionManager.Instance.LocalPlayer && caster == ClientConnectionManager.Instance.LocalPlayer)
				{
					string spellID = spell.Id;
					var pack = PacketManager.GetOrCreatePacket<PlayerActionPacket>(ClientConnectionManager.Instance.LocalPlayerID, PacketType.PLAYER_ACTION);
					pack.dataTypes.Add(ActionType.STATUS_EFFECT_APPLY);
					pack.effectData = new StatusEffectData()
					{
						spellID = spellID,
						damageBonus = _dmgBonus,
						casterType = EntityType.PLAYER,
						targetID = -3,
						duration = _duration,
					};

				}
				else
				{
					//if we are the caster
					if (caster == ClientConnectionManager.Instance.LocalPlayer)
					{
						string spellID = spell.Id;
						var pack = PacketManager.GetOrCreatePacket<PlayerActionPacket>(ClientConnectionManager.Instance.LocalPlayerID, PacketType.PLAYER_ACTION);
						pack.dataTypes.Add(ActionType.STATUS_EFFECT_APPLY);
						pack.effectData = new StatusEffectData()
						{
							spellID = spellID,
							damageBonus = _dmgBonus,
							casterType = EntityType.PLAYER,
							casterID = caster.entityID,
							duration = _duration,
							targetID = target.entityID,
							targetType = target.type,
						};
						if (target is NetworkedPlayer)
							pack.effectData.targetType = EntityType.PLAYER;
					}
					else //We're not the caster
					{
						if (ClientZoneOwnership.isZoneOwner || caster is SimSync)
						{
							if (caster is SimSync)
							{
								string spellID = spell.Id;
								var pack = PacketManager.GetOrCreatePacket<PlayerActionPacket>(caster.entityID, PacketType.PLAYER_ACTION);
								pack.dataTypes.Add(ActionType.STATUS_EFFECT_APPLY);
								pack.effectData = new StatusEffectData()
								{
									spellID = spellID,
									damageBonus = _dmgBonus,
									casterType = caster.type,
									casterID = caster.entityID,
									duration = _duration,
									targetID = target.entityID,
									targetType = target.type,
								};
								if (target is NetworkedPlayer)
									pack.effectData.targetType = EntityType.PLAYER;
								pack.targetPlayerIDs = SharedNPCSyncManager.Instance.GetPlayerSendList();
								pack.isSim = true;
							}
							else
							{
								string spellID = spell.Id;
								var pack = PacketManager.GetOrCreatePacket<EntityActionPacket>(caster.entityID, PacketType.ENTITY_ACTION);
								pack.dataTypes.Add(ActionType.STATUS_EFFECT_APPLY);
								pack.effectData = new StatusEffectData()
								{
									spellID = spellID,
									damageBonus = _dmgBonus,
									casterType = caster.type,
									casterID = caster.entityID,
									duration = _duration,
									targetID = target.entityID,
									targetType = target.type,
								};
								if (target is NetworkedPlayer)
									pack.effectData.targetType = EntityType.PLAYER;
								pack.targetPlayerIDs = SharedNPCSyncManager.Instance.GetPlayerSendList();
							}
						}
					}
				}
			}
		}

		public static void RemoveStatusEffect_Prefix(Stats __instance, int index)
		{
			if (ClientConnectionManager.Instance.IsRunning)
			{
				if (ClientConnectionManager.Instance.LocalPlayer.stats == __instance)
				{
					var pack = PacketManager.GetOrCreatePacket<PlayerActionPacket>(ClientConnectionManager.Instance.LocalPlayerID, PacketType.PLAYER_ACTION);
					pack.dataTypes.Add(ActionType.STATUS_EFFECT_REMOVE);
					pack.statusID = index;
				}
				else
				{
					Entity target = GetEntityByStats(__instance);
					if (ClientZoneOwnership.isZoneOwner || target is SimSync)
					{
						if (target == null) return;
						if (target is SimSync)
						{
							var pack = PacketManager.GetOrCreatePacket<PlayerActionPacket>(target.entityID, PacketType.PLAYER_ACTION);
							pack.dataTypes.Add(ActionType.STATUS_EFFECT_REMOVE);
							pack.statusID = index;
							pack.targetPlayerIDs = SharedNPCSyncManager.Instance.GetPlayerSendList();
							pack.isSim = true;
						}
						else
						{
							var pack = PacketManager.GetOrCreatePacket<EntityActionPacket>(target.entityID, PacketType.ENTITY_ACTION);
							pack.dataTypes.Add(ActionType.STATUS_EFFECT_REMOVE);
							pack.statusID = index;
							pack.targetPlayerIDs = SharedNPCSyncManager.Instance.GetPlayerSendList();
						}
					}
				}
			}
		}

		public static void RemoveAllStatusEffects_Prefix(Stats __instance)
		{
			if (ClientConnectionManager.Instance.IsRunning)
			{
				if (ClientConnectionManager.Instance.LocalPlayer.stats == __instance)
				{
					var pack = PacketManager.GetOrCreatePacket<PlayerActionPacket>(ClientConnectionManager.Instance.LocalPlayerID, PacketType.PLAYER_ACTION);
					pack.dataTypes.Add(ActionType.STATUS_EFFECT_REMOVE);
					pack.RemoveAllStatus = true;
				}
				else
				{
					Entity target = GetEntityByStats(__instance);
					if (ClientZoneOwnership.isZoneOwner || target is SimSync)
					{
						if (target == null) return;
						if (target is SimSync)
						{
							var pack = PacketManager.GetOrCreatePacket<PlayerActionPacket>(target.entityID, PacketType.PLAYER_ACTION);
							pack.dataTypes.Add(ActionType.STATUS_EFFECT_REMOVE);
							pack.RemoveAllStatus = true;
							pack.targetPlayerIDs = SharedNPCSyncManager.Instance.GetPlayerSendList();
							pack.isSim = true;
						}
						else
						{
							var pack = PacketManager.GetOrCreatePacket<EntityActionPacket>(target.entityID, PacketType.ENTITY_ACTION);
							pack.dataTypes.Add(ActionType.STATUS_EFFECT_REMOVE);
							pack.RemoveAllStatus = true;
							pack.targetPlayerIDs = SharedNPCSyncManager.Instance.GetPlayerSendList();
						}
						
					}
				}
			}
		}

		public static void RemoveBreakableEffects_Prefix(Stats __instance)
		{
			if (ClientConnectionManager.Instance.IsRunning)
			{
				if (ClientConnectionManager.Instance.LocalPlayer.stats == __instance)
				{
					var pack = PacketManager.GetOrCreatePacket<PlayerActionPacket>(ClientConnectionManager.Instance.LocalPlayerID, PacketType.PLAYER_ACTION);
					pack.dataTypes.Add(ActionType.STATUS_EFFECT_REMOVE);
					pack.RemoveBreakable = true;
				}
				else
				{
					Entity target = GetEntityByStats(__instance);
					if (ClientZoneOwnership.isZoneOwner || target is SimSync)
					{
						if (target == null) return;
						if (target is SimSync)
						{
							var pack = PacketManager.GetOrCreatePacket<PlayerActionPacket>(target.entityID, PacketType.PLAYER_ACTION);
							pack.dataTypes.Add(ActionType.STATUS_EFFECT_REMOVE);
							pack.RemoveBreakable = true;
							pack.targetPlayerIDs = SharedNPCSyncManager.Instance.GetPlayerSendList();
							pack.isSim = true;
						}
						else
						{
							var pack = PacketManager.GetOrCreatePacket<EntityActionPacket>(target.entityID, PacketType.ENTITY_ACTION);
							pack.dataTypes.Add(ActionType.STATUS_EFFECT_REMOVE);
							pack.RemoveBreakable = true;
							pack.targetPlayerIDs = SharedNPCSyncManager.Instance.GetPlayerSendList();
						}
					}
				}
			}
		}

		public static bool StatsMitigatePhysical_Prefix(Stats __instance, ref int __result, int _incomingDmg)
		{
			var _char = __instance.gameObject.GetComponent<Character>();
			if (_char == null) return true;

			if (Variables.DontCalculateDamageMitigationCharacters.Contains(_char))
			{
				//Debug.Log($"Setting Res to {_incomingDmg}");
				__result = _incomingDmg;
				return false;
			}

			return true;
		}
		public static bool StatsReduceHP_Prefix(Stats __instance, ref bool __result, int _dmg, GameData.DamageType _dmgType)
		{
			if (!ClientZoneOwnership.isZoneOwner && ClientConnectionManager.Instance.IsRunning)
			{
				foreach (var mob in ClientNPCSyncManager.Instance.NetworkedMobs)
				{
					if (mob.Value.character.MyStats == __instance)
					{
						//Show the damage popup but dont reduce health
						GameData.Misc.GenPopup(_dmg, false, _dmgType, __instance.transform);
						__result = __instance.CurrentHP <= 0;
						return false;
					}
				}

				foreach (var mob in ClientNPCSyncManager.Instance.NetworkedSims)
				{
					if (mob.Value.character.MyStats == __instance)
					{
						//Show the damage popup but dont reduce health
						GameData.Misc.GenPopup(_dmg, false, _dmgType, __instance.transform);
						__result = __instance.CurrentHP <= 0;
						return false;
					}
				}

				foreach (var player in ClientConnectionManager.Instance.Players)
				{
					if (player.Value.character.MyStats == __instance)
					{
						//Show the damage popup but dont reduce health
						GameData.Misc.GenPopup(_dmg, false, _dmgType, __instance.transform);
						__result = __instance.CurrentHP <= 0;
						return false;
					}
				}
			}
			return true;
		}


#endregion




#region CHARACTER

		public static bool CheckVsMR_Prefix(Character __instance, ref float __result)
		{
			if (Variables.DontCalculateDamageMitigationCharacters.Contains(__instance))
			{
				__result = 0f;
				return false;
			}

			return true;
		}

		private static void SyncDamage(Character attackedChar, int __result, GameData.DamageType _dmgType, Character _attacker, bool _animEffect, float resistMod, bool isCrit)
		{

			//See if this is a sync NPC
			var npcSync = attackedChar.GetComponent<NPCSync>();
			//See if this is the localPlayer
			var playerSync = attackedChar.GetComponent<PlayerSync>();
			//See if this is a networked Player
			var networkedSync = attackedChar.GetComponent<NetworkedPlayer>();
			//See if this is a network entity
			var networkedEntity = attackedChar.GetComponent<NetworkedNPC>();
			//See if this is a sim entity
			var networkedSim = attackedChar.GetComponent<NetworkedSim>();

			//how?
			if (_attacker == null)
			{
				//Logging.LogError("no attacker");
				return;

			}

			//See if the ATTACKER is a sync NPC or SIM
			var attackerNpcSync = _attacker.GetComponent<NPCSync>();
			var attackerSimSync = _attacker.GetComponent<SimSync>();


			bool characterIsNPC = npcSync != null;
			bool characterIsLocalPlayer = playerSync != null;
			bool characterIsNetworked = networkedSync != null;
			bool characterIsSim = networkedSim != null;
			bool characterIsOutwardNPC = networkedEntity != null;

			bool attackerIsNPC = attackerNpcSync != null;
			bool attackerIsSIM = attackerSimSync != null;

			//If none of those are networked we dont care

			if (!attackerIsNPC && !attackerIsSIM) return;
			if (!characterIsNPC && !characterIsLocalPlayer && !characterIsNetworked && !characterIsOutwardNPC) return;


			if (attackerIsNPC)
			{
				if (characterIsNPC || characterIsSim)
				{
					attackerNpcSync.SendAttack(__result, npcSync.entityID, true, _dmgType, _animEffect, resistMod, isCrit);
				}
				else if (characterIsNetworked)
				{
					attackerNpcSync.SendAttack(__result, networkedSync.playerID, false, _dmgType, _animEffect, resistMod, isCrit);
					if (ServerConnectionManager.Instance.IsRunning)
					{
						if (GameData.GroupMember1 != null && GameData.GroupMember1.simIndex >= 0)
							attackerNpcSync.npc.ManageAggro(1, GameData.GroupMember1.MyStats.Myself);
						if (GameData.GroupMember2 != null && GameData.GroupMember2.simIndex >= 0)
							attackerNpcSync.npc.ManageAggro(1, GameData.GroupMember2.MyStats.Myself);
						if (GameData.GroupMember3 != null && GameData.GroupMember3.simIndex >= 0)
							attackerNpcSync.npc.ManageAggro(1, GameData.GroupMember3.MyStats.Myself);
					}
				}
				else if (characterIsLocalPlayer)
				{
					attackerNpcSync.SendAttack(__result, ClientConnectionManager.Instance.LocalPlayerID, false, _dmgType, _animEffect, resistMod, isCrit);
				}
				else if (characterIsOutwardNPC)
				{
					attackerNpcSync.SendAttack(__result, networkedEntity.entityID, true, _dmgType, _animEffect, resistMod, isCrit);
				}
			} else if (attackerIsSIM)
			{
				var e = attackedChar.GetComponent<Entity>();
				if (e == null) Logging.Log("Cant get ent with unity wtf");

				attackerSimSync.SendDamageAttack(__result, e.entityID, e.type == EntityType.ENEMY, _dmgType, _animEffect, resistMod, isCrit);
			}
			
		}

		private static void SyncDamageClient(Character attackedChar, int damage, GameData.DamageType _dmgType, Character _attacker, bool _animEffect, float resistMod, bool isCrit)
		{
			//how?
			if (_attacker == null) return;

			//See if the ATTACKED is a networked NPC
			var attackedNpcSync = attackedChar.GetComponent<NetworkedNPC>();
			//See if the ATTACKED is a networked Player (PVP support!)
			var attackedNetworked = attackedChar.GetComponent<NetworkedPlayer>();
			//For Host
			var attackedSynced = attackedChar.GetComponent<NPCSync>();
			

			bool attackedIsNPC = attackedNpcSync != null;
			bool attackedIsNetworked = attackedNetworked != null;
			bool attackedIsSynced = attackedSynced != null;

			if (!attackedIsNPC && !attackedIsNetworked && !attackedIsSynced) return;

			short attackedID = -1;
			var attackedNPC = false;

			if (attackedIsNPC)
			{
				foreach (var npc in ClientNPCSyncManager.Instance.NetworkedMobs)
				{
					if (npc.Value == attackedNpcSync)
					{
						attackedID = npc.Key;
						attackedNPC = true;
						break;
					}
				}
			}
			else if (attackedIsNetworked)
			{
				foreach (var player in ClientConnectionManager.Instance.Players)
				{
					if (player.Value == attackedNetworked)
					{
						attackedID = player.Key;
						break;
					}
				}
			}else if (attackedIsSynced)
			{
				foreach (var npc in SharedNPCSyncManager.Instance.mobs)
				{
					if (npc.Value == attackedSynced)
					{
						attackedID = npc.Key;
						attackedNPC = true;
						break;
					}
				}
			}

			if (attackedID == -1) return;

			//Logging.Log($"doing player attack  {attackedID} {attackedNPC} {damage}");
			ClientConnectionManager.Instance.LocalPlayer.SendDamageAttack(damage, attackedID, attackedNPC, _dmgType, _animEffect, resistMod, isCrit);
			//__result = 0;
		}

		public static void CharacterDamageMe_Postfix(Character __instance, ref int __result, int _incdmg, bool _fromPlayer, GameData.DamageType _dmgType, Character _attacker, bool _animEffect, bool _criticalHit)
		{
			if(ClientZoneOwnership.isZoneOwner)
				SyncDamage(__instance, __result, _dmgType, _attacker, _animEffect, 0, _criticalHit);
			if (_attacker != null && _attacker.GetComponent<SimSync>() != null)
				SyncDamage(__instance, __result, _dmgType, _attacker, _animEffect, 0, _criticalHit);
			if (ClientConnectionManager.Instance.IsRunning && _attacker != null && _attacker == ClientConnectionManager.Instance.LocalPlayer.character)
				SyncDamageClient(__instance, __result, _dmgType, _attacker, _animEffect, 0, _criticalHit);

			if (ClientConnectionManager.Instance.LocalPlayer.MySummon != null)
			{
				if (ClientConnectionManager.Instance.LocalPlayer.MySummon.character == _attacker)
					SyncDamage(__instance, __result, _dmgType, _attacker, _animEffect, 0, _criticalHit);
			}
		}

		public static void MagicDamageMe_Postfix(Character __instance, ref int __result, int _dmg, bool _fromPlayer, GameData.DamageType _dmgType, Character _attacker, float resistMod)
		{
			if(ClientZoneOwnership.isZoneOwner)
				SyncDamage(__instance, __result, _dmgType, _attacker, false, resistMod, false);
			if(_attacker.GetComponent<SimSync>() != null)
				SyncDamage(__instance, __result, _dmgType, _attacker, false, resistMod, false);
			if (ClientConnectionManager.Instance.IsRunning && _attacker != null && _attacker == ClientConnectionManager.Instance.LocalPlayer.character)
				SyncDamageClient(__instance, __result, _dmgType, _attacker, false, resistMod, false);
			if (ClientConnectionManager.Instance.LocalPlayer.MySummon != null)
			{
				if (ClientConnectionManager.Instance.LocalPlayer.MySummon.character == _attacker)
					SyncDamage(__instance, __result, _dmgType, _attacker, false, resistMod, false);
			}
		}

		public static bool CharacterDamageMe_Prefix(Character __instance, ref int __result, int _incdmg, bool _fromPlayer, GameData.DamageType _dmgType, Character _attacker, bool _animEffect, bool _criticalHit)
		{

			return true;
		}

		public static bool MagicDamageMe_Prefix(Character __instance, ref int __result, int _dmg, bool _fromPlayer, GameData.DamageType _dmgType, Character _attacker, float resistMod)
		{

			return true;
		}

#endregion



#region TYPETEXT
		public static FieldInfo lastTyped;
		public static bool CheckInput_Prefix(TypeText __instance)
		{
			var text = __instance.typed.text;

			if (text.Length > 0)
			{
				var target = "";
				var message = "";
				MessageType messageType = MessageType.SAY;

				if (text.Contains("/group "))
				{
					string grpText = "";
					for (int j = 7; j < text.Length; j++)
					{
						grpText += text[j];
					}

					message = grpText;
					messageType = MessageType.GROUP;
				}
				else if (text.Contains("/shout "))
				{
					string shoutText = "";
					for (int k = 7; k < text.Length; k++)
					{
						shoutText += text[k];
					}

					message = shoutText;
					messageType = MessageType.SHOUT;
				}
				else if (text.Contains("/whisper "))
				{
					string mes = "";
					int num3 = 7;
					for (int l = 9; l < text.Length; l++)
					{
						num3++;
						if (text[l] == ' ')
						{
							break;
						}

						target += text[l];
					}

					for (int m = num3 + 2; m < text.Length; m++)
					{
						mes += text[m];
					}
					message = mes;
					messageType = MessageType.WHISPER;

					bool exists = false;
					foreach(var p in ClientConnectionManager.Instance.Players.Values)
					{
						if(p.playerName == target)
						{
							exists = true;
							break;
						}
					}

					if(!exists)
					{
						//UpdateSocialLog.LogAdd($"The player with the name {target} does not exists.", "#FB09FF");
						//UpdateSocialLog.LocalLogAdd($"The player with the name {target} does not exists.", "#FB09FF");
						return true;
					}
					else
					{
						UpdateSocialLog.LogAdd($"[WHISPER TO] {target}: {mes}", "#FB09FF");
						UpdateSocialLog.LocalLogAdd($"[WHISPER TO] {target}: {mes}", "#FB09FF");
					}
				}
				else
				{
					if(text[0] != '/')
						message = text;
				}

				if (string.IsNullOrEmpty(message)) return true;


				var packet = PacketManager.GetOrCreatePacket<PlayerMessagePacket>(ClientConnectionManager.Instance.LocalPlayerID, PacketType.PLAYER_MESSAGE);
				packet.message = message;
				packet.messageType = messageType;
				packet.target = target;
				

				if (messageType == MessageType.WHISPER)
				{
					lastTyped.SetValue(__instance, text);
					__instance.typed.text = "";
					__instance.CDFrames = 10f;
					__instance.InputBox.SetActive(value: false);
					GameData.PlayerTyping = false;

					return false;
				}
				else
				{
					return true;
				}
			}

			return true;
		}
#endregion




#region SPAWNPOINT
		public static void SpawnPointSpawnNPC_Post(SpawnPoint __instance)
		{
			//if (!ServerConnectionManager.Instance.IsRunning) return;
			if (!ClientZoneOwnership.isZoneOwner) return;

			int spawnID = Extensions.GenerateHash(__instance.transform.position, SceneManager.GetActiveScene().name);

			if (!Variables.spawnData.ContainsKey(spawnID))
			{
				//For some reason this spawn didn't exist
				Variables.AddSpawn(spawnID, __instance);
			}

			var npc = __instance.SpawnedNPC;
			
			if(npc != null)
			{
				var spawnData = Variables.spawnData[spawnID];
				var spawnMobData = spawnData.GetMobData(npc.gameObject);
				if (spawnMobData == null) return;

				SharedNPCSyncManager.Instance.ServerSpawnMob(npc.gameObject, spawnID, spawnMobData.mobID.ToString(), spawnMobData.isRare, npc.transform.position, npc.transform.rotation);
			}
		}
#endregion




#region NPC

		public static FieldInfo spawnPoint;
		public static MethodInfo startMethod;
		public static MethodInfo handleNameTag;
		public static FieldInfo rotTimer;
		public static FieldInfo leashing;

		public static bool NPCUpdate_Prefix(NPC __instance)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;

			var isNetworked = false;
			var isPlayer = false;


			foreach(var player in ClientConnectionManager.Instance.Players.Values)
			{
				if (player.npc == null || player.npc != __instance) continue;
				isNetworked = true;
				isPlayer = true;
				break;
			}
			/*foreach (var mob in ClientNPCSyncManager.Instance.NetworkedSims)
			{
				if (mob.Value.npc == null || mob.Value.npc != __instance) continue;
				isNetworked = true;
				break;
			}*/
			if (!ClientZoneOwnership.isZoneOwner)
			{
				foreach (var mob in ClientNPCSyncManager.Instance.NetworkedMobs)
				{
					if (mob.Value.npc == null || mob.Value.npc != __instance) continue;
					isNetworked = true;
					break;
				}
			}


			if (isNetworked)
			{
				if (!__instance.CheckLiving() && !isPlayer)
				{
					
					//__instance.rotTimer -= 60f * Time.deltaTime;
					var val = (float)rotTimer.GetValue(__instance);
					rotTimer.SetValue(__instance,val-( 60f * Time.deltaTime));
					if (val <= 0f && !__instance.SimPlayer)
					{
						UnityEngine.Object.Destroy(__instance.gameObject);
					}
				}

				handleNameTag?.Invoke(__instance, null);
			}


			return !isNetworked;
		}

		public static bool NPCCombat_Prefix(NPC __instance)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;

			foreach (var player in ClientConnectionManager.Instance.Players.Values)
			{
				if (player.npc != null && player.npc == __instance)
					return false;
			}
			foreach (var mob in ClientNPCSyncManager.Instance.NetworkedSims)
			{
				if (mob.Value.npc == __instance)
					return false;
			}
			if (!ClientZoneOwnership.isZoneOwner)
			{
				foreach (var mob in ClientNPCSyncManager.Instance.NetworkedMobs)
				{
					if (mob.Value.npc == __instance)
						return false;
				}
			}
			
			return true;
		}

#endregion




#region ZONELINE

		private static FieldInfo thisZoning;
		public static bool ZoneLineOnTriggerEnter_Prefix(Zoneline __instance, Collider other)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;

			if (GameData.Zoning) return false;

			var ent = other.GetComponent<Entity>();
			if (ent == null) return true;

			if (ent.type != EntityType.PLAYER && ent.type != EntityType.SIM) return false;
			
			if (ent.type == EntityType.PLAYER && ent.entityID != ClientConnectionManager.Instance.LocalPlayerID)
			{
				if (!Grouping.IsPlayerInGroup(ent.entityID, false)) return false;
			}
			if(ent.type == EntityType.SIM)
			{
				var simInGrp = Grouping.IsPlayerInGroup(ent.entityID, true);
				if (!simInGrp && ent is SimSync) return true;
				if (!simInGrp && ent is NetworkedSim) return false;
			}


			if (Grouping.IsLocalLeader())
			{
				//Check for other ppls sims in grp and remove them
				if (GameData.GroupMember1 != null && GameData.GroupMember1.MyAvatar != null)
				{
					Entity npcsync1 = GameData.GroupMember1.MyAvatar.GetComponent<Entity>();
					if (npcsync1 != null)
					{
						if (npcsync1 is NetworkedSim)
						{
							Grouping.GroupListCallback += GroupListCB;
							hasGRPCB = true;
							zl = __instance;
							Grouping.RemoveFromGroup(npcsync1.entityID);
						}
					}

				}
				if (GameData.GroupMember2 != null && GameData.GroupMember2.MyAvatar != null)
				{
					Entity npcsync2 = GameData.GroupMember2.MyAvatar.GetComponent<Entity>();
					if (npcsync2 != null)
					{
						if (npcsync2 is NetworkedSim)
						{
							if (!hasGRPCB)
							{
								Grouping.GroupListCallback += GroupListCB;
								hasGRPCB = true;
								zl = __instance;
							}
							Grouping.RemoveFromGroup(npcsync2.entityID);
						}
					}
				}
				if (GameData.GroupMember3 != null && GameData.GroupMember3.MyAvatar != null)
				{
					Entity npcsync = GameData.GroupMember3.MyAvatar.GetComponent<Entity>();
					if (npcsync != null)
					{
						if (npcsync is NetworkedSim)
						{
							if (!hasGRPCB)
							{
								Grouping.GroupListCallback += GroupListCB;
								hasGRPCB = true;
								zl = __instance;
							}
							Grouping.RemoveFromGroup(npcsync.entityID);
						}
					}
				}
			}

			if(!hasGRPCB)
				__instance.CallZoning();
			//ClientConnectionManager.Instance.LocalPlayer.StartZoneTransfer(__instance);

			return false;
		}

		public static bool hasGRPCB = false;
		public static Zoneline zl;

		public static void GroupListCB()
		{
			zl.CallZoning();
			zl = null;
			hasGRPCB = false;
			Grouping.GroupListCallback -= GroupListCB;
		}

#endregion




#region MODULARPARTS
		public static FieldInfo hideHair;
		public static FieldInfo hideHead;
		public static MethodInfo GetTransformNames;
#endregion




#region ANIMATOR

		public static FieldInfo animatorController;

		//I think we can do it like this, even if the param changes multiple times a frame we should keep a record of it and send it
		private static void SendAnimData(string param, object value, AnimatorSyncType syncType, bool isSim=false, short simEntId=-1)
		{
			var pack = PacketManager.GetOrCreatePacket<PlayerDataPacket>(isSim?simEntId:ClientConnectionManager.Instance.LocalPlayerID, PacketType.PLAYER_DATA);
			pack.AddType(PlayerDataType.ANIM);
			pack.isSim = isSim;
			AnimationData animData = new()
			{
				param = param,
				value = value,
				syncType = syncType
			};
			pack.animData.Add(animData);
		}

		private static void SendMobAnimData(short mobID, string param, object value, AnimatorSyncType syncType, bool issim = false)
		{
			if (issim && !SharedNPCSyncManager.Instance.sims.ContainsKey(mobID)) return;
			if (!issim && !SharedNPCSyncManager.Instance.mobs.ContainsKey(mobID)) return;
			if (issim && !SharedNPCSyncManager.Instance.sims[mobID].isCloseToPlayer) return;
			if (!issim && !SharedNPCSyncManager.Instance.mobs[mobID].isCloseToPlayer) return;

			var pack = PacketManager.GetOrCreatePacket<EntityDataPacket>(mobID, PacketType.ENTITY_DATA);
			pack.AddType(EntityDataType.ANIM);
			pack.SetData("targetPlayerIDs", SharedNPCSyncManager.Instance.GetPlayerSendList());
			pack.entityType = issim?SharedNPCSyncManager.Instance.sims[mobID].type:SharedNPCSyncManager.Instance.mobs[mobID].type;

			AnimationData animData = new()
			{
				param = param,
				value = value,
				syncType = syncType
			};
			pack.animData.Add(animData);
		}


		public static void AnimOverrideSet_Prefix(AnimatorOverrideController __instance, string name, AnimationClip value)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return;
			if (__instance[name] == value)
				return;

			if (value == null) return; //???

			if (ClientConnectionManager.Instance.LocalPlayer != null && ClientConnectionManager.Instance.LocalPlayer.AnimOverride != null && ClientConnectionManager.Instance.LocalPlayer.AnimOverride == __instance)
			{
				SendAnimData(name, value.name, AnimatorSyncType.OVERRIDE);
			}

			if (ClientZoneOwnership.isZoneOwner && SharedNPCSyncManager.Instance.overrideToMobID.TryGetValue(__instance, out short mobID))
			{
				SendMobAnimData(mobID, name, value.name, AnimatorSyncType.OVERRIDE);
			}
			if (ClientConnectionManager.Instance.LocalPlayer.MySummon != null)
			{
				if (ClientConnectionManager.Instance.LocalPlayer.MySummon.GetComponent<NPCSync>().anim.runtimeAnimatorController == __instance)
					SendMobAnimData(ClientConnectionManager.Instance.LocalPlayer.MySummon.entityID, name, value.name, AnimatorSyncType.OVERRIDE);
			}

			//if (ClientConnectionManager.Instance.LocalPlayer != null && ClientConnectionManager.Instance.LocalPlayer.AnimOverride != null && ClientConnectionManager.Instance.LocalPlayer.AnimOverride == __instance)
			{
				bool hasSim = false;
				short simentid = -1;
				foreach (var sim in SharedNPCSyncManager.Instance.sims)
				{
					if (sim.Value.animator.runtimeAnimatorController == __instance)
					{
						hasSim = true;
						simentid = sim.Key;
						break;
					}
				}
				if (hasSim)
					SendAnimData(name, value.name, AnimatorSyncType.OVERRIDE, true, simentid);
			}

		}
		public static void SetBool_Prefix(Animator __instance, string name, bool value)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return;
			if (__instance.GetBool(name) == value)
				return;

			if (ClientConnectionManager.Instance.LocalPlayer != null && ClientConnectionManager.Instance.LocalPlayer.animator != null && ClientConnectionManager.Instance.LocalPlayer.animator == __instance)
			{
				SendAnimData(name, value, AnimatorSyncType.BOOL);
			}

			if (ClientZoneOwnership.isZoneOwner && SharedNPCSyncManager.Instance.animatorToMobID.TryGetValue(__instance, out short mobID))
			{
				SendMobAnimData(mobID, name, value, AnimatorSyncType.BOOL);
			}
			if (ClientConnectionManager.Instance.LocalPlayer.MySummon != null)
			{
				if (ClientConnectionManager.Instance.LocalPlayer.MySummon.GetComponent<NPCSync>().anim == __instance)
					SendMobAnimData(ClientConnectionManager.Instance.LocalPlayer.MySummon.entityID, name, value, AnimatorSyncType.BOOL);
			}

			//if (ServerConnectionManager.Instance.IsRunning)
			{
				var hasSim = false;
				short simentid = 0;
				foreach (var sim in SharedNPCSyncManager.Instance.sims)
				{
					if (sim.Value.animator == __instance)
					{
						hasSim = true;
						simentid = sim.Key;
						break;
					}
				}
				if (hasSim)
					SendAnimData(name, value, AnimatorSyncType.BOOL, true, simentid);
			}
		}

		public static void SetFloat_Prefix(Animator __instance, string name, float value)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return;
			if (Math.Abs(__instance.GetFloat(name) - value) < 0.01f)
				return;

			if (ClientConnectionManager.Instance.LocalPlayer != null && ClientConnectionManager.Instance.LocalPlayer.animator != null && ClientConnectionManager.Instance.LocalPlayer.animator == __instance)
			{
				SendAnimData(name, value, AnimatorSyncType.FLOAT);
			}

			if (ClientZoneOwnership.isZoneOwner && SharedNPCSyncManager.Instance.animatorToMobID.TryGetValue(__instance, out short mobID))
			{
				SendMobAnimData(mobID, name, value, AnimatorSyncType.FLOAT);
			}
			if (ClientConnectionManager.Instance.LocalPlayer.MySummon != null)
			{
				if (ClientConnectionManager.Instance.LocalPlayer.MySummon.GetComponent<NPCSync>().anim == __instance)
					SendMobAnimData(ClientConnectionManager.Instance.LocalPlayer.MySummon.entityID, name, value, AnimatorSyncType.FLOAT);
			}

			//if (ClientZoneOwnership.isZoneOwner)
			{
				bool hasSim = false;
				short simentid = 0;
				foreach (var sim in SharedNPCSyncManager.Instance.sims)
				{
					if (sim.Value.animator == __instance)
					{
						hasSim = true;
						simentid = sim.Key;
						break;
					}
				}
				if (hasSim)
					SendAnimData(name, value, AnimatorSyncType.FLOAT, true, simentid);
			}
		}

		public static void SetInteger_Prefix(Animator __instance, string name, int value)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return;

			if (__instance.GetInteger(name) == value)
				return;

			if (ClientConnectionManager.Instance.LocalPlayer != null && ClientConnectionManager.Instance.LocalPlayer.animator != null && ClientConnectionManager.Instance.LocalPlayer.animator == __instance)
			{
				SendAnimData(name, value, AnimatorSyncType.INT);
			}

			if (ClientZoneOwnership.isZoneOwner && SharedNPCSyncManager.Instance.animatorToMobID.TryGetValue(__instance, out short mobID))
			{
				SendMobAnimData(mobID, name, value, AnimatorSyncType.INT);
			}
			if (ClientConnectionManager.Instance.LocalPlayer.MySummon != null)
			{
				if (ClientConnectionManager.Instance.LocalPlayer.MySummon.GetComponent<NPCSync>().anim == __instance)
					SendMobAnimData(ClientConnectionManager.Instance.LocalPlayer.MySummon.entityID, name, value, AnimatorSyncType.INT);
			}

			//if (ClientZoneOwnership.isZoneOwner)
			{
				bool hasSim = false;
				short simentid = 0;
				foreach (var sim in SharedNPCSyncManager.Instance.sims)
				{
					if (sim.Value.animator == __instance)
					{
						hasSim = true;
						simentid = sim.Key;
						break;
					}
				}
				if (hasSim)
					SendAnimData(name, value, AnimatorSyncType.INT, true, simentid);
			}
		}

		public static void SetTrigger_Prefix(Animator __instance, string name)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return;
			if (ClientConnectionManager.Instance.LocalPlayer != null && ClientConnectionManager.Instance.LocalPlayer.animator != null && ClientConnectionManager.Instance.LocalPlayer.animator == __instance)
			{
				SendAnimData(name, true, AnimatorSyncType.TRIG);
			}

			if (ClientZoneOwnership.isZoneOwner && SharedNPCSyncManager.Instance.animatorToMobID.TryGetValue(__instance, out short mobID))
			{
				SendMobAnimData(mobID, name, true, AnimatorSyncType.TRIG);
			}
			if (ClientConnectionManager.Instance.LocalPlayer.MySummon != null)
			{
				if (ClientConnectionManager.Instance.LocalPlayer.MySummon.GetComponent<NPCSync>().anim == __instance)
					SendMobAnimData(ClientConnectionManager.Instance.LocalPlayer.MySummon.entityID, name, true, AnimatorSyncType.TRIG);
			}

			//if (ClientZoneOwnership.isZoneOwner)
			{
				bool hasSim = false;
				short simentid = 0;
				foreach (var sim in SharedNPCSyncManager.Instance.sims)
				{
					if (sim.Value.animator == __instance)
					{
						hasSim = true;
						simentid = sim.Key;
						break;
					}
				}
				if (hasSim)
					SendAnimData(name, true, AnimatorSyncType.TRIG, true, simentid);
			}
		}

		public static void ResetTrigger_Prefix(Animator __instance, string name)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return;
			if (ClientConnectionManager.Instance.LocalPlayer != null && ClientConnectionManager.Instance.LocalPlayer.animator != null && ClientConnectionManager.Instance.LocalPlayer.animator == __instance)
			{
				SendAnimData(name, false, AnimatorSyncType.RSTTRIG);
			}

			if (ClientZoneOwnership.isZoneOwner && SharedNPCSyncManager.Instance.animatorToMobID.TryGetValue(__instance, out short mobID))
			{
				SendMobAnimData(mobID, name, false, AnimatorSyncType.RSTTRIG);
			}
			if (ClientConnectionManager.Instance.LocalPlayer.MySummon != null)
			{
				if (ClientConnectionManager.Instance.LocalPlayer.MySummon.GetComponent<NPCSync>().anim == __instance)
					SendMobAnimData(ClientConnectionManager.Instance.LocalPlayer.MySummon.entityID, name, false, AnimatorSyncType.RSTTRIG);
			}

			//if (ClientZoneOwnership.isZoneOwner)
			{
				bool hasSim = false;
				short simentid = 0;
				foreach (var sim in SharedNPCSyncManager.Instance.sims)
				{
					if (sim.Value.animator == __instance)
					{
						hasSim = true;
						simentid = sim.Key;
						break;
					}
				}
				if (hasSim)
					SendAnimData(name, false, AnimatorSyncType.RSTTRIG, true, simentid);
			}
		}
#endregion
	}
}
