using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using System.Text.RegularExpressions;
using ErenshorCoop.Client.Grouping;
using ErenshorCoop.Server.Grouping;

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
			//ErenshorCoopMod.CreatePrefixHook(typeof(Character),                  "DamageMe",                typeof(GameHooks), "CharacterDamageMe_Prefix");
			//ErenshorCoopMod.CreatePrefixHook(typeof(Character),                  "MagicDamageMe",           typeof(GameHooks), "MagicDamageMe_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(Character),                  "CheckResistAmount",       typeof(GameHooks), "CheckVsMR_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(SimPlayerGrouping),          "InviteToGroup",           typeof(GameHooks), "InviteToGroup_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(SimPlayerGrouping),          "DismissMember1",          typeof(GameHooks), "DismissMember1_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(SimPlayerGrouping),          "DismissMember2",          typeof(GameHooks), "DismissMember2_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(SimPlayerGrouping),          "DismissMember3",          typeof(GameHooks), "DismissMember3_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(SimPlayerGrouping),          "DismissMember4",          typeof(GameHooks), "DismissMember4_Prefix");
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
			ErenshorCoopMod.CreatePrefixHook(typeof(PlayerCombat),               "DoBowAttack",             typeof(GameHooks), "PlayerDoBowAttackA_Prefix", new[] { typeof(Character), typeof(int) });
			ErenshorCoopMod.CreatePrefixHook(typeof(PlayerCombat),               "DoBowAttack",             typeof(GameHooks), "PlayerDoBowAttackB_Prefix", new[] { typeof(Character), typeof(int), typeof(bool), typeof(Spell) });
			ErenshorCoopMod.CreatePrefixHook(typeof(PlayerCombat),               "DoBowAttack",             typeof(GameHooks), "PlayerDoBowAttackC_Prefix", new[] { typeof(Character), typeof(int), typeof(int) });
			ErenshorCoopMod.CreatePrefixHook(typeof(PlayerCombat),               "DoBowAttack",             typeof(GameHooks), "PlayerDoBowAttackD_Prefix", new[] { typeof(Character), typeof(Spell), typeof(int), typeof(bool) });
			ErenshorCoopMod.CreatePrefixHook(typeof(NPC),                        "DoWandAttack",            typeof(GameHooks), "NPCDoWandAttack_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(NPC),                        "DoBowAttack",             typeof(GameHooks), "NPCDoBowAttackA_Prefix", new[] { typeof(Character), typeof(int) });
			ErenshorCoopMod.CreatePrefixHook(typeof(NPC),                        "DoBowAttack",             typeof(GameHooks), "NPCDoBowAttackB_Prefix", new[] { typeof(Character), typeof(int), typeof(bool), typeof(Spell) });
			ErenshorCoopMod.CreatePrefixHook(typeof(NPC),                        "DoBowAttack",             typeof(GameHooks), "NPCDoBowAttackC_Prefix", new[] { typeof(Character), typeof(Spell), typeof(int), typeof(bool) });
			ErenshorCoopMod.CreatePrefixHook(typeof(NPC),                        "DoBowAttack",             typeof(GameHooks), "NPCDoBowAttackD_Prefix", new[] { typeof(Character), typeof(int), typeof(int) });
			ErenshorCoopMod.CreatePrefixHook(typeof(NPC),                        "UpdateNav",               typeof(GameHooks), "NPCUpdateNav_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(SimPlayerMngr),              "BringPlayerGroupToZone",  typeof(GameHooks), "BringPlayerGroupToZone_pre");
			ErenshorCoopMod.CreatePrefixHook(typeof(SimPlayerMngr),              "SpawnSimsInZone",         typeof(GameHooks), "SpawnSimsInZone_pre");
			ErenshorCoopMod.CreatePrefixHook(typeof(SimItemDisplay),             "OnPointerDown",           typeof(GameHooks), "SimItemOnPointerDown_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(SimPlayer),                  "SaveSim",                 typeof(GameHooks), "SaveSim_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(NPC),                        "CheckAggro",              typeof(GameHooks), "CheckAggro_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(Character),                  "GroupMemberAlive",        typeof(GameHooks), "GroupMemberAlive_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(Stats),                      "RegenEffects",            typeof(GameHooks), "RegenEffects_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(Stats),                      "RefreshWornSE",           typeof(GameHooks), "RefreshWornSE_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(AstraListener),              "Update",                  typeof(GameHooks), "AstraUpdate_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(AstraBreathScriot),          "Update",                  typeof(GameHooks), "AstraBreathUpdate_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(WaveEvent),                  "Update",                  typeof(GameHooks), "WaveUpdate_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(WaveEvent),                  "DoIntro",                 typeof(GameHooks), "WaveDoIntro_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(WaveEvent),                  "SpawnNewWave",            typeof(GameHooks), "WaveSpawnNewWave_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(WaveEvent),                  "DoEnd",                   typeof(GameHooks), "WaveDoEnd_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(SpawnPointTrigger),          "OnTriggerEnter",          typeof(GameHooks), "SpawnTriggerOnTriggerEnter_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(SpawnPointTrigger),          "Start",                   typeof(GameHooks), "SpawnTriggerStart_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(FernallaPortalBoss),         "Update",                  typeof(GameHooks), "FernallaPortalBossUpdate_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(FernallaPortalEvent),        "Update",                  typeof(GameHooks), "FernallaPortalEventUpdate_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(FernallaPortalShouts),       "Update",                  typeof(GameHooks), "FernallaShoutsUpdate_Prefix");
			ErenshorCoopMod.CreatePrefixHook(typeof(SpawnPoint),                 "SpawnNPC",                typeof(SyncedSpawnPoint), "SpawnNPC");
			//ErenshorCoopMod.CreatePrefixHook(typeof(TypeText),                   "RenameSimPlayer",         typeof(GameHooks), "RenameSimPlayer_Prefix");


			ErenshorCoopMod.CreatePostHook(typeof(SpawnPoint),        "Start",              typeof(SyncedSpawnPoint), "Start");
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
			ErenshorCoopMod.CreatePostHook(typeof(Stats),             "DoLevelUp",          typeof(GameHooks), "DoLevelUp_Postfix");
			ErenshorCoopMod.CreatePostHook(typeof(UpdateSocialLog),   "CombatLogAdd",       typeof(GameHooks), "CombatLogAdd_Postfix", new[] { typeof(string) });
			ErenshorCoopMod.CreatePostHook(typeof(UpdateSocialLog),   "LogAdd",             typeof(GameHooks), "LogAdd_Postfix", new[] { typeof(string) });
			ErenshorCoopMod.CreatePostHook(typeof(UpdateSocialLog),   "CombatLogAdd",       typeof(GameHooks), "CombatLogAdd2_Postfix", new[] { typeof(string), typeof(string) });
			ErenshorCoopMod.CreatePostHook(typeof(UpdateSocialLog),   "LogAdd",             typeof(GameHooks), "LogAdd2_Postfix", new[] { typeof(string), typeof(string) });
			ErenshorCoopMod.CreatePostHook(typeof(UpdateSocialLog),   "LogAdd",             typeof(GameHooks), "LogAdd3_Postfix", new[] { typeof(string), typeof(string), typeof(bool) });
			ErenshorCoopMod.CreatePostHook(typeof(UpdateSocialLog),   "LogAdd",             typeof(GameHooks), "LogAdd4_Postfix", new[] { typeof(string), typeof(bool) });
			//ErenshorCoopMod.CreatePostHook(typeof(SimInspect),        "SpendPoint",         typeof(GameHooks), "SimSpendPoint");
			ErenshorCoopMod.CreatePostHook(typeof(Stats), "CalcStats", typeof(GameHooks), "OnCalcStat");
			//ErenshorCoopMod.CreatePrefixHook(typeof(Misc), "GenPopup", typeof(GameHooks), "MiscGenPopup_Prefix");
			//ErenshorCoopMod.CreatePostHook(typeof(TypeText),          "RenameSimPlayer",    typeof(GameHooks), "RenameSimPlayer_Postfix");



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
			wander = type.GetField("wander", BindingFlags.NonPublic | BindingFlags.Instance);

			type = typeof(SpellVessel);
			targ = type.GetField("targ",               BindingFlags.NonPublic | BindingFlags.Instance);
			SpellSource = type.GetField("SpellSource", BindingFlags.NonPublic | BindingFlags.Instance);

			type = typeof(Zoneline);
			thisZoning = type.GetField("thisZoning", BindingFlags.NonPublic | BindingFlags.Instance);

			type = typeof(Stats);
			xpBonus = type.GetField("XPBonus", BindingFlags.NonPublic | BindingFlags.Instance);
			maxMP = type.GetField("CurrentMaxMana", BindingFlags.NonPublic | BindingFlags.Instance);

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

			type = typeof(WaveEvent);
			actSpawned = type.GetField("actualSpawned", BindingFlags.NonPublic | BindingFlags.Instance);
			actBoss = type.GetField("ActualBoss", BindingFlags.NonPublic | BindingFlags.Instance);

			type = typeof(SpawnPointTrigger);
			spTrigCD = type.GetField("cooldown", BindingFlags.NonPublic | BindingFlags.Instance);
		}

		public static void RenameSimPlayer_Postfix(Character _currentTarget, string _allTxt)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return;
			Entity ent = null;
			if (_currentTarget != null && (ent = _currentTarget.GetComponent<Entity>()) != null && ent is SimSync)
			{
				ent.SendRename(_allTxt);
			}
		}
		public static bool RenameSimPlayer_Prefix(Character _currentTarget, string _allTxt)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;
			Entity ent = null;
			if(_currentTarget != null && (ent = _currentTarget.GetComponent<Entity>()) != null && ent is NetworkedSim)
			{
				UpdateSocialLog.LogAdd("You can only rename your own SimPlayers!", "yellow");
				return false;
			}
			return true;
		}
		public static void OnCalcStat(Stats __instance)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return;
			if(__instance.Myself == null || __instance.Myself.MySpells == null) return;
			//check if player or sim
			if (!__instance.Myself.MySpells.isPlayer && !__instance.Myself.MySpells.isSimPlayer) return;
			var ent = __instance.GetComponent<Entity>();
			if (ent == null || ent is NetworkedPlayer || ent is NetworkedSim) return;
			ent.OnStatChange();
		}

		private static float fernTimer = 1000f;
		public static bool FernallaShoutsUpdate_Prefix(FernallaPortalShouts __instance)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;
			if (!ClientZoneOwnership.isZoneOwner) return false;

			if (fernTimer > 0f)
			{
				fernTimer -= 60f * Time.deltaTime;
			}
			if (fernTimer <= 0f)
			{
				fernTimer = Random.Range(1000, 3000);
				if (__instance.StopIfDead != null && __instance.StopIfDead.Alive)
				{
					var mes = __instance.Name + " shouts: " + __instance.Shouts[Random.Range(0, __instance.Shouts.Count)];
					UpdateSocialLog.LogAdd(mes, "#FF9000");

					SendMessageToPlayers(mes, "#FF9000", false, false);

					return false;
				}
				var mes2 = __instance.Name + " shouts: My... you are resilient. You'll proceed no further here. Do not return.";
				UpdateSocialLog.LogAdd(mes2, "#FF9000");

				SendMessageToPlayers(mes2, "#FF9000", false, false);

				fernTimer = 999999f;
			}

			return false;
		}


		public static List<PreSyncedEntity> preSyncList = new();
		public class PreSyncedEntity : MonoBehaviour
		{
			public int id = -1;
			public void Awake()
			{
				var sp = transform.Find("EVENTSPAWNER");
				//Only sync fernalla rn
				if ((!name.Contains("Nightmar") || GetComponent<Entity>() != null) && sp == null)
					DestroyImmediate(this);
				else
				{
					var d = Variables.presyncedEntities.GetOrCreateValue(this);
					id = Extensions.GenerateHash(transform.position, SceneManager.GetActiveScene().name);
					d.id = id;
					d.go = gameObject;
					preSyncList.Add(this);

					Logging.Log($"{name} {id}");
				}
			}

			public void OnDestroy()
			{
				if (id != -1)
				{
					if (GetComponent<Entity>() != null)
						Destroy(GetComponent<Entity>());
					preSyncList.Remove(this);
				}
			}
		}
		public class SyncedFernallaPortalEvent : MonoBehaviour
		{
			private int id = -1;
			public List<Transform> Patrol;
			public GameObject Arcanist;
			public GameObject Knight;
			public GameObject Hound;
			public GameObject Invader;
			public float SpawnWaveDel;
			private float counter;
			public bool KeepSpawning;
			private int waveNum;
			private List<Character> MySpawns = new();
			public int MaxSpawns = 12;
			public void Awake()
			{
				if (id == -1)
				{
					var d = Variables.portalIDs.GetOrCreateValue(this);
					id = Extensions.GenerateHash(transform.position, SceneManager.GetActiveScene().name);
					d.id = id;
					counter = 500f;

					if(name == "EVENTSPAWNER")
					{
						transform.parent.gameObject.AddComponent<PreSyncedEntity>();
					}
				}
			}
			public void Update()
			{
				if (!ClientConnectionManager.Instance.IsRunning) return;
				if (!ClientZoneOwnership.isZoneOwner) return;


				if (KeepSpawning)
				{
					if (counter > 0f)
					{
						counter -= 60f * Time.deltaTime;
					}
					if (counter <= 0f)
					{
						for (int i = MySpawns.Count - 1; i >= 0; i--)
						{
							if (MySpawns[i] == null || !MySpawns[i].Alive)
							{
								MySpawns.RemoveAt(i);
							}
						}
						if (MySpawns.Count < MaxSpawns)
						{
							if (waveNum < 3)
							{
								GameObject gameObject = Instantiate(Knight, transform.position, transform.rotation);
								gameObject.GetComponent<NPC>().InitNewNPC(Patrol);
								MySpawns.Add(gameObject.GetComponent<Character>());

								SetNetworked(gameObject, id, 1, CustomSpawnID.FERNALLA_PORTAL);

								waveNum++;
								
							}
							if (waveNum >= 3 && waveNum < 6)
							{
								GameObject gameObject2 = Instantiate(Knight, transform.position, transform.rotation);
								gameObject2.GetComponent<NPC>().InitNewNPC(Patrol);
								MySpawns.Add(gameObject2.GetComponent<Character>());

								SetNetworked(gameObject2, id, 1, CustomSpawnID.FERNALLA_PORTAL);
								
								gameObject2 = Instantiate(Arcanist, transform.position, transform.rotation);
								gameObject2.GetComponent<NPC>().InitNewNPC(Patrol);
								MySpawns.Add(gameObject2.GetComponent<Character>());

								SetNetworked(gameObject2, id, 2, CustomSpawnID.FERNALLA_PORTAL);

								waveNum++;
							}
							if (waveNum >= 6 && waveNum < 10)
							{
								GameObject gameObject3 = Instantiate(Knight, transform.position, transform.rotation);
								gameObject3.GetComponent<NPC>().InitNewNPC(Patrol);
								MySpawns.Add(gameObject3.GetComponent<Character>());

								SetNetworked(gameObject3, id, 1, CustomSpawnID.FERNALLA_PORTAL);

								gameObject3 = Instantiate(Arcanist, transform.position, transform.rotation);
								gameObject3.GetComponent<NPC>().InitNewNPC(Patrol);
								MySpawns.Add(gameObject3.GetComponent<Character>());

								SetNetworked(gameObject3, id, 2, CustomSpawnID.FERNALLA_PORTAL);

								gameObject3 = Instantiate(Hound, transform.position, transform.rotation);
								gameObject3.GetComponent<NPC>().InitNewNPC(Patrol);
								MySpawns.Add(gameObject3.GetComponent<Character>());

								SetNetworked(gameObject3, id, 3, CustomSpawnID.FERNALLA_PORTAL);

								waveNum++;
							}
						}
						counter = SpawnWaveDel;
					}
				}
			}
		}
		
		public static bool FernallaPortalEventUpdate_Prefix(FernallaPortalEvent __instance)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;

			var sync = __instance.gameObject.GetOrAddComponent<SyncedFernallaPortalEvent>();
			sync.Awake();
			sync.Patrol = __instance.Patrol;
			sync.Arcanist = __instance.Arcanist;
			sync.Knight = __instance.Knight;
			sync.Hound = __instance.Hound;
			sync.Invader = __instance.Invader;
			sync.SpawnWaveDel = __instance.SpawnWaveDel;
			sync.KeepSpawning = __instance.KeepSpawning;
			sync.MaxSpawns = __instance.MaxSpawns;

			UnityEngine.Object.Destroy(__instance);

			return false;
		}


		public static bool fernallaSpawn1 = false;
		public static bool fernallaSpawn2 = false;
		public static Stats fernallaStats = null;
		public static GameObject fernallaBoss;
		public static bool FernallaPortalBossUpdate_Prefix(FernallaPortalBoss __instance)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;

			if (!ClientZoneOwnership.isZoneOwner)
			{
				if (__instance.GetComponent<Entity>() == null)
				{
					fernallaBoss = __instance.gameObject;

					//Activate all the wards
					FernallaPortalEventUpdate_Prefix(__instance.Ward1.GetComponent<FernallaPortalEvent>());
					FernallaPortalEventUpdate_Prefix(__instance.Ward2.GetComponent<FernallaPortalEvent>());
					FernallaPortalEventUpdate_Prefix(__instance.Ward3.GetComponent<FernallaPortalEvent>());
					//Disable this object?
					if (!Variables.entitiesToDisable.Contains(__instance.gameObject))
						Variables.entitiesToDisable.Add(__instance.gameObject);
				}
				//UnityEngine.Object.Destroy(__instance);
				__instance.enabled = false;
				return false;
			}

			if (fernallaStats == null)
			{
				fernallaStats = __instance.GetComponent<Stats>();
				SetNetworked(__instance.gameObject, 0, 99, CustomSpawnID.FERNALLA_WARD);

				//Activate all the wards
				FernallaPortalEventUpdate_Prefix(__instance.Ward1.GetComponent<FernallaPortalEvent>());
				FernallaPortalEventUpdate_Prefix(__instance.Ward2.GetComponent<FernallaPortalEvent>());
				FernallaPortalEventUpdate_Prefix(__instance.Ward3.GetComponent<FernallaPortalEvent>());
			}

			if (fernallaStats.CurrentHP < Mathf.RoundToInt(12000f * GameData.HPScale) && !fernallaSpawn1)
			{
				fernallaSpawn1 = true;
				__instance.Ward1.SetActive(true);

				SetNetworked(__instance.Ward1, 1, -1, CustomSpawnID.FERNALLA_WARD);
			}
			if (fernallaStats.CurrentHP < Mathf.RoundToInt(7000f * GameData.HPScale) && !fernallaSpawn2)
			{
				fernallaSpawn2 = true;
				__instance.Ward2.SetActive(true);
				SetNetworked(__instance.Ward2, 2, -1, CustomSpawnID.FERNALLA_WARD);

				__instance.Ward3.SetActive(true);
				SetNetworked(__instance.Ward3, 3, -1, CustomSpawnID.FERNALLA_WARD);
			}
			return false;
		}



		public static void SetNetworked(GameObject obj, int tID, int gID, CustomSpawnID spawnID)
		{
			var net = obj.GetComponent<NPCSync>();
			if (net != null) return;
			else net = obj.AddComponent<NPCSync>();
			net.treasureChestID = tID;
			net.guardianId = gID;
			net.spawnID = spawnID;
			net.RequestID();
		}



		private static FieldInfo spTrigCD;

		public static void SpawnTriggerStart_Prefix(SpawnPointTrigger __instance)
		{
			var d = Variables.triggerIDs.GetOrCreateValue(__instance);
			d.id = Extensions.GenerateHash(__instance.transform.position, SceneManager.GetActiveScene().name);
		}
		public static bool SpawnTriggerOnTriggerEnter_Prefix(SpawnPointTrigger __instance, Collider other)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;
			if (!ClientZoneOwnership.isZoneOwner) return false;

			if (other.transform.name == "Player" && (float)spTrigCD.GetValue(__instance) <= 0f)
			{
				var d = Variables.triggerIDs.GetOrCreateValue(__instance);

				GameData.PlayerAud.PlayOneShot(__instance.Trigger, GameData.SFXVol);
				foreach (GameObject gameObject in __instance.SpawnSpots)
				{
					GameObject go = null;
					int n = 99;
					if (__instance.Alt == null || Random.Range(0, 10000) > 0)
					{
						n = Random.Range(0, __instance.Spawnables.Count);
						go = Object.Instantiate(__instance.Spawnables[n], gameObject.transform.position, gameObject.transform.rotation);
					}
					else
					{
						go = Object.Instantiate(__instance.Alt, gameObject.transform.position, gameObject.transform.rotation);
					}

					var net = go.GetOrAddComponent<NPCSync>();
					net.treasureChestID = d.id;
					net.guardianId = n;
					net.spawnID = CustomSpawnID.SPAWN_TRIGGER;
					net.RequestID();
				}
				spTrigCD.SetValue(__instance, 10000f);
			}

			return false;
		}


		private static FieldInfo actSpawned;
		private static FieldInfo actBoss;
		public static bool WaveDoEnd_Prefix(WaveEvent __instance)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;
			if (!ClientZoneOwnership.isZoneOwner) return false;


			var mes = __instance.ShouterName + " shouts: " + __instance.End;

			UpdateSocialLog.LogAdd(mes, "#FF9000");

			__instance.Done = true;
			SendMessageToPlayers(mes, "#FF9000", false, false);
			return false;
		}
		public static bool WaveSpawnNewWave_Prefix(WaveEvent __instance)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;
			if (!ClientZoneOwnership.isZoneOwner) return false;

			var s = __instance.ShoutBetweenWaves[Random.Range(0, __instance.ShoutBetweenWaves.Count)];
			UpdateSocialLog.LogAdd(__instance.ShouterName + " shouts: " + s, "#FF9000");
			SendMessageToPlayers(__instance.ShouterName + " shouts: " + s, "#FF9000", false, false);
			int num = 0;
			if (__instance.CurWave < 3)
			{
				num = 0;
			}
			if (__instance.CurWave >= 3 && __instance.CurWave <= 5)
			{
				num = 1;
			}
			if (__instance.CurWave > 5)
			{
				num = 2;
			}
			if (__instance.CurWave >= __instance.TotalWaves)
			{
				num = 3;
			}
			if (__instance.CurWave < __instance.TotalWaves)
			{
				int num2 = Random.Range((int)__instance.SmallWaveCountRange.x, (int)__instance.SmallWaveCountRange.y);
				for (int i = 0; i <= num2; i++)
				{
					GameObject go = null;
					var lNum = -1;
					var n = -1;
					if (num == 0)
					{
						n = Random.Range(0, __instance.WeakWave.Count);
						go = Object.Instantiate(__instance.WeakWave[n], __instance.SpawnLocations[Random.Range(0, __instance.SpawnLocations.Count)].transform.position, __instance.transform.rotation);
						lNum = 1;
					}
					if (num == 1)
					{
						n = Random.Range(0, __instance.StrongWave.Count);
						go = Object.Instantiate(__instance.StrongWave[n], __instance.SpawnLocations[Random.Range(0, __instance.SpawnLocations.Count)].transform.position, __instance.transform.rotation);
						lNum = 2;
					}
					if (num == 2)
					{
						n = Random.Range(0, __instance.StrongestWave.Count);
						go = Object.Instantiate(__instance.StrongestWave[n], __instance.SpawnLocations[Random.Range(0, __instance.SpawnLocations.Count)].transform.position, __instance.transform.rotation);
						lNum = 3;
					}
					if (num == 3)
					{
						n = Random.Range(0, __instance.StrongestWave.Count);
						go = Object.Instantiate(__instance.StrongestWave[n], __instance.SpawnLocations[Random.Range(0, __instance.SpawnLocations.Count)].transform.position, __instance.transform.rotation);
						lNum = 3;
					}
					var net = go.GetOrAddComponent<NPCSync>();
					net.treasureChestID = lNum;
					net.guardianId = n;
					net.spawnID = CustomSpawnID.WAVE_EVENT;
					net.RequestID();
				}
				return false;
			}
			if (__instance.CurWave == __instance.TotalWaves)
			{

				var b = Object.Instantiate(__instance.BossMob, __instance.SpawnLocations[Random.Range(0, __instance.SpawnLocations.Count)].transform.position, __instance.transform.rotation).GetComponent<Character>();
				actBoss.SetValue(__instance, b);
				UpdateSocialLog.LogAdd(__instance.ShouterName + " shouts: " + __instance.BossAlert, "#FF9000");

				SendMessageToPlayers(__instance.ShouterName + " shouts: " + __instance.BossAlert, "#FF9000", false, false);

				__instance.CurWave++;
				actSpawned.SetValue(__instance, true);

				var net = b.gameObject.GetOrAddComponent<NPCSync>();
				net.treasureChestID = 99;
				net.guardianId = 99;
				net.spawnID = CustomSpawnID.WAVE_EVENT;
				net.RequestID();
			}

			return false;
		}
		public static bool WaveDoIntro_Prefix(WaveEvent __instance)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;
			if (!ClientZoneOwnership.isZoneOwner) return false;

			if(!__instance.IntroDone)
			{
				var mes = __instance.ShouterName + " shouts: " + __instance.IntroText;
				SendMessageToPlayers(mes, "#FF9000", false, false);
			}
			return true;
		}
		public static bool WaveUpdate_Prefix(WaveEvent __instance)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;
			if (!ClientZoneOwnership.isZoneOwner) return false;
			return true;
		}

		public static void SendMessageToPlayers(string mes, string col, bool append, bool isCombat)
		{
			foreach (var p in ClientConnectionManager.Instance.Players)
			{
				if (Variables.DontResendMessageEnts.Contains((Entity)p.Value)) continue;
				if (p.Value.zone != SceneManager.GetActiveScene().name) continue;
				//if (Vector3.Distance(p.Value.transform.position, GameData.PlayerControl.transform.position) > 15) continue;

				var player = p.Value;

				PacketManager.GetOrCreatePacket<PlayerMessagePacket>(player.entityID, PacketType.PLAYER_MESSAGE)
						.SetTarget(player)
						.SetData("message", mes)
						.SetData("messageType", MessageType.BATTLE_LOG)
						.SetData("color", col)
						.SetData("append", append)
						.SetData("isCombatLog", isCombat)
						.SetData("sender", ClientConnectionManager.Instance.LocalPlayerID);
			}
		}

		public static void CombatLogAdd_Postfix(string _string)
		{
			ProcessLogMessage(_string, "", false, true);
		}
		public static void LogAdd_Postfix(string _string)
		{
			ProcessLogMessage(_string, "", false, false);
		}
		public static void CombatLogAdd2_Postfix(string _string, string _colorAsString)
		{
			ProcessLogMessage(_string, _colorAsString, false, true);
		}
		public static void LogAdd2_Postfix(string _string, string _colorAsString)
		{
			ProcessLogMessage(_string, _colorAsString, false, false);
		}
		public static void LogAdd3_Postfix(string _string, string _colorAsString, bool _append)
		{
			ProcessLogMessage(_string, _colorAsString, _append, false);
		}
		public static void LogAdd4_Postfix(string _string, bool _append)
		{
			ProcessLogMessage(_string, "", _append, false);
		}

		public static string ProcessLogForSync(string rawLogEntry)
		{
			if (string.IsNullOrWhiteSpace(rawLogEntry))
				return null;

			string original = rawLogEntry.Trim();
			string logLower = original.ToLowerInvariant();

			bool isSyncCandidate = logLower.Contains("take") ||
						   logLower.Contains("caught in") ||
						   logLower.Contains("attack") ||
						   logLower.Contains("absorbed") ||
						   logLower.Contains("inhale") ||
						   logLower.Contains("hit") ||
						   logLower.Contains("damage");

			if (rawLogEntry.Contains("can't hit your target from here") || rawLogEntry.Contains("can't hit yourself") || rawLogEntry.Contains("tells the group"))
				return null;

			if (!ClientZoneOwnership.isZoneOwner)
			{
				bool mentionsYou = logLower.Contains("you") || logLower.Contains("your");

				if (!mentionsYou || !isSyncCandidate)
					return null;
			}

			if (!(logLower.Contains("take") || logLower.Contains("caught in") || logLower.Contains("attack") ||
				  logLower.Contains("absorbed") || logLower.Contains("inhale") || logLower.Contains("hit")))
				return null;

			string result = original;

			var CurrentPlayerName = GameData.CurrentCharacterSlot.CharName;

			result = Regex.Replace(result, @"\bYou hit\b", $"{CurrentPlayerName} hits", RegexOptions.IgnoreCase);

			result = ReplaceWholeWord(result, "YOU", CurrentPlayerName);
			result = ReplaceWholeWord(result, "You", CurrentPlayerName);

			result = ReplaceWholeWord(result, "YOUR", CurrentPlayerName + "'s");
			result = ReplaceWholeWord(result, "Your", CurrentPlayerName + "'s");

			result = Regex.Replace(result, $@"\b{CurrentPlayerName} (are|take|try|have|do|absorb)\b", match =>
			{
				string verb = match.Groups[1].Value.ToLowerInvariant();
				return $"{CurrentPlayerName} {ConjugateToThirdPerson(verb)}";
			});

			return result;
		}
		private static string ConjugateToThirdPerson(string verb)
		{
			switch (verb.ToLowerInvariant())
			{
				case "take": return "takes";
				case "attack": return "attacks";
				case "are": return "is";
				case "have": return "has";
				case "do": return "does";
				case "try": return "tries";
				case "absorb": return "absorbs";
				default: return verb;
			}
		}
		private static string ReplaceWholeWord(string input, string word, string replacement)
		{
			return Regex.Replace(input, $@"\b{Regex.Escape(word)}\b", replacement);
		}

		public static void ProcessLogMessage(string message, string color, bool append, bool isCombatLog)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return;

			string personalizedMessage = ProcessLogForSync(message);
			if (personalizedMessage == null) return;

			foreach (var p in ClientConnectionManager.Instance.Players)
			{
				if (Variables.DontResendMessageEnts.Contains((Entity)p.Value)) continue;
				if (p.Value.zone != SceneManager.GetActiveScene().name) continue;
				if (Vector3.Distance(p.Value.transform.position, GameData.PlayerControl.transform.position) > 15) continue;

				var player = p.Value;

				if (personalizedMessage.Contains(player.entityName))
				{
					personalizedMessage = ReplaceWholeWord(personalizedMessage, player.entityName, "YOU");
					personalizedMessage = Regex.Replace(personalizedMessage, $@"\b{player.entityName}'s\b", "YOUR");
					personalizedMessage = Regex.Replace(personalizedMessage, @"\bYOU is\b", "YOU are", RegexOptions.IgnoreCase);
				}

				PacketManager.GetOrCreatePacket<PlayerMessagePacket>(player.entityID, PacketType.PLAYER_MESSAGE)
						.SetTarget(player)
						.SetData("message", personalizedMessage)
						.SetData("messageType", MessageType.BATTLE_LOG)
						.SetData("color", color)
						.SetData("append", append)
						.SetData("isCombatLog", isCombatLog)
						.SetData("sender", ClientConnectionManager.Instance.LocalPlayerID);
				
			}
		}


		public static FieldInfo maxMP;
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

		public static bool GroupMemberAlive_Prefix(Character __instance, ref bool __result)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;

			var e = GetEntityByCharacter(__instance);
			if (e == null) return true;
			if (e is SimSync) return true;

			//var groupMembers = new[] { GameData.GroupMember1, GameData.GroupMember2, GameData.GroupMember3 };
			string currentZone = SceneManager.GetActiveScene().name;

			foreach (var member in GameData.GroupMembers)
			{
				var avatar = member?.MyAvatar;
				if (avatar?.MyStats?.Myself?.Alive == true)
				{
					var entity = avatar.GetComponent<Entity>();
					if (entity == null || entity.zone == currentZone)
					{
						__result = true;
						return false;
					}
				}
			}

			__result = false;
			return false;
		}

		public static bool RegenEffects_Prefix(Stats __instance)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;

			if(__instance.GetComponent<Entity>() != null)
			{
				var e = __instance.GetComponent<Entity>();
				if (e is NetworkedPlayer || e is NetworkedSim || e is NetworkedNPC)
					return false;
			}
			return true;
		}

		public static void CheckAggro_Prefix(NPC __instance)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return;
			
		}

		public static void SpawnSimsInZone_pre()
		{
			//For some reason pois arent correctly cleared and leave null in here
			//Could be a game bug, could be a coop bug, who knows
			for (int i=0;i<POI.POIs.Count;i++)
			{
				var p = POI.POIs[i];
				if (p == null || p.gameObject == null)
					POI.POIs.Remove(p);
			}
		}

		//Sadly we need to modify this to not try to spawn players, that'd be bad
		public static bool BringPlayerGroupToZone_pre(SimPlayerMngr __instance)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;


			foreach(var mem in GameData.GroupMembers)
			{
				if (mem != null && mem.simIndex >= 0 && mem.MyAvatar == null)
				{
					//	Logging.Log($"spawn gp1 {GameData.GroupMember1.simIndex} {GameData.GroupMember1.SimName} ");
					__instance.ActiveSimInstances.Add(mem.SpawnMeInGame(GameData.PlayerControl.transform.position + new Vector3(Random.Range(-1, 1), 0f, Random.Range(-1, 1))));
					mem.MyAvatar.InGroup = true;
					mem.isPuller = false;
					mem.Caution = false;
					mem.CurScene = SceneManager.GetActiveScene().name;
					__instance.SimsInZones[mem.simIndex] = SceneManager.GetActiveScene().name;
				}
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

			NetworkedSim player = _player.GetOrAddComponent<NetworkedSim>();
			player.sim = pCon;
			player.pos = pos;
			player.rot = rot;

			if (nMeshAgent != null) nMeshAgent.enabled = true;

			return player;
		}


		public static void StatsHealMe_Postfix(Stats __instance, int __result, Spell _spell, int _amt, bool _isCrit, bool _isMana, Character _source)
		{
			SyncHealing(_spell, __instance, _isMana?_amt:__result, _isCrit, _source, _isMana);
		}

		private static (bool, string, short) WandCheckPlayer(Character _target)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return (false, "", -1);

			Inventory playerInv = GameData.PlayerInv;
			Item item;
			if (playerInv == null) return (false, "", -1);

			ItemIcon mh = playerInv.MH;
			item = ((mh != null) ? mh.MyItem : null);

			if (item == null) return (false, "", -1);

			var (_, _, targID) = GetEntityIDByCharacter(_target);
			if (targID == -1) return (false, "", -1);

			return (true, item.Id, targID);
		}

		public static void PlayerDoWandAttack_Prefix(Character _target)
		{
			(var c, string itemID, short targID) = WandCheckPlayer(_target);
			if (!c) return;

			WandAttackData wandAttackData = new()
			{
				targetID = targID,
				itemID = itemID,
				isBowAttack = false
			};

			ClientConnectionManager.Instance.LocalPlayer.SendWand(wandAttackData);
		}


		public static void PlayerDoBowAttackA_Prefix(Character _target, int _arrowIndex)
		{
			(var c, string itemID, short targID) = WandCheckPlayer(_target);
			if (!c) return;

			WandAttackData wandAttackData = new()
			{
				targetID = targID,
				itemID = itemID,
				isBowAttack = true,
				attackType = 0,
				arrowIndex = _arrowIndex,
			};

			ClientConnectionManager.Instance.LocalPlayer.SendWand(wandAttackData);
		}

		public static void PlayerDoBowAttackB_Prefix(Character _target, int _arrowIndex, bool _interrupt, Spell _force)
		{
			(var c, string itemID, short targID) = WandCheckPlayer(_target);
			if (!c) return;

			WandAttackData wandAttackData = new()
			{
				targetID = targID,
				itemID = itemID,
				isBowAttack = true,
				attackType = 1,
				arrowIndex = _arrowIndex,
				interrupt = _interrupt
			};

			ClientConnectionManager.Instance.LocalPlayer.SendWand(wandAttackData);
		}

		public static void PlayerDoBowAttackC_Prefix(Character _target, int _dmgMod, int _arrowIndex)
		{
			(var c, string itemID, short targID) = WandCheckPlayer(_target);
			if (!c) return;

			WandAttackData wandAttackData = new()
			{
				targetID = targID,
				itemID = itemID,
				isBowAttack = true,
				attackType = 2,
				arrowIndex = _arrowIndex,
				dmgMod = _dmgMod
			};

			ClientConnectionManager.Instance.LocalPlayer.SendWand(wandAttackData);
		}

		public static void PlayerDoBowAttackD_Prefix(Character _target, Spell _forceProc, int _arrowIndex, bool _noCheckEffect)
		{
			(var c, string itemID, short targID) = WandCheckPlayer(_target);
			if (!c) return;

			WandAttackData wandAttackData = new()
			{
				targetID = targID,
				itemID = itemID,
				isBowAttack = true,
				attackType = 3,
				arrowIndex = _arrowIndex,
			};

			ClientConnectionManager.Instance.LocalPlayer.SendWand(wandAttackData);
		}

		private static (bool,string,short,Entity) WandCheck(NPC __instance, Character _target)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return (false,"",-1,null);

			Entity ent = GetEntityByCharacter(__instance.ThisSim.MyStats.Myself);
			if (ent == null) return (false, "", -1, null);
			//make sure its a local sim
			if (ent.entityID == -1) return (false, "", -1, null);
			if (!(ent is SimSync)) return (false, "", -1, null);

			Item item;

			if (!__instance.SimPlayer) return (false, "", -1, null); //Seems to only be a thing on sims

			SimInvSlot simMH = __instance.ThisSim.MyStats.MyInv.SimMH;
			item = ((simMH != null) ? simMH.MyItem : null);

			if (item == null) return (false, "", -1, null);

			var (_, _, targID) = GetEntityIDByCharacter(_target);
			if (targID == -1) return (false, "", -1, null);

			return (true, item.Id, targID, ent);
		}
		public static void NPCDoWandAttack_Prefix(NPC __instance, Character _target)
		{
			(var c, string itemID, short targID, Entity ent) = WandCheck(__instance, _target);
			if (!c) return;

			WandAttackData wandAttackData = new()
			{
				targetID = targID,
				itemID = itemID,
				isBowAttack = false,
				attackType = -1,
			};

			ent.SendWand(wandAttackData);
		}


		public static void NPCDoBowAttackA_Prefix(NPC __instance, Character _target, int _arrowIndex)
		{
			(var c, string itemID, short targID, Entity ent) = WandCheck(__instance, _target);
			if (!c) return;

			WandAttackData wandAttackData = new()
			{
				targetID = targID,
				itemID = itemID,
				isBowAttack = true,
				attackType = 0,
				arrowIndex = _arrowIndex,
			};

			ent.SendWand(wandAttackData);
		}

		public static void NPCDoBowAttackB_Prefix(NPC __instance, Character _target, int _arrowIndex, bool _interrupt, Spell _force)
		{
			(var c, string itemID, short targID, Entity ent) = WandCheck(__instance, _target);
			if (!c) return;

			WandAttackData wandAttackData = new()
			{
				targetID = targID,
				itemID = itemID,
				isBowAttack = true,
				attackType = 1,
				arrowIndex = _arrowIndex,
				interrupt = _interrupt
			};

			ent.SendWand(wandAttackData);
		}

		public static void NPCDoBowAttackC_Prefix(NPC __instance, Character _target, Spell _force, int _arrowIndex, bool _noCheckEffect)
		{
			(var c, string itemID, short targID, Entity ent) = WandCheck(__instance, _target);
			if (!c) return;

			WandAttackData wandAttackData = new()
			{
				targetID = targID,
				itemID = itemID,
				isBowAttack = true,
				attackType = 2,
				arrowIndex = _arrowIndex,
			};

			ent.SendWand(wandAttackData);
		}

		public static void NPCDoBowAttackD_Prefix(NPC __instance, Character _target, int _dmgMod, int _arrowIndex)
		{
			(var c, string itemID, short targID, Entity ent) = WandCheck(__instance, _target);
			if (!c) return;

			WandAttackData wandAttackData = new()
			{
				targetID = targID,
				itemID = itemID,
				isBowAttack = true,
				attackType = 3,
				arrowIndex = _arrowIndex,
				dmgMod = _dmgMod
			};

			ent.SendWand(wandAttackData);
		}



		public static bool SaveSim_Prefix(SimPlayer __instance)
		{
			return PreventSimSaves(__instance);
		}
		public static bool LoadSimData_Prefix(SimPlayer __instance)
		{
			return PreventSimSaves(__instance);
		}

		private static bool PreventSimSaves(SimPlayer __instance)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;

			if (__instance.transform.position == new Vector3(999, 999, 999)) return false;
			if (__instance.GetComponent<Entity>() != null)
			{
				var e = __instance.GetComponent<Entity>();
				if (e is NetworkedSim || e is NetworkedPlayer) return false;
			}
			return true;
		}


		public static bool ForceAggroOn_Prefix(NPC __instance, Character tar)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;

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


		private static bool _astraSpawnSent = false;
		public static bool AstraUpdate_Prefix(AstraListener __instance)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;
			if (!ClientZoneOwnership.isZoneOwner) return false;

			if (__instance.Astra != null && !__instance.AstraCanSpawn && !_astraSpawnSent)
			{
				_astraSpawnSent = true;
				var net = __instance.Astra.GetOrAddComponent<NPCSync>();
				net.spawnID = CustomSpawnID.ASTRA;
				net.RequestID();
			}
			if (__instance.Astra == null && _astraSpawnSent && __instance.AstraCanSpawn)
				_astraSpawnSent = false;

			return false;
		}

		public static bool AstraBreathUpdate_Prefix(AstraBreathScriot __instance)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;
			if (!ClientZoneOwnership.isZoneOwner) return false;

			return true;
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

			foreach(var mem in GameData.GroupMembers)
			{
				if (mem != null && mem.simIndex >= 0 && mem.MyAvatar.IsThatAnUpgrade(_item))
				{
					string str = mem.MyAvatar.MyDialog.GetLootReq().Replace("II", _item.ItemName);
					GameData.SimPlayerGrouping.AddStringForDisplay(GameData.SimPlayerGrouping.PlayerOneName.text + " tells the group: " + str, "#00B2B7");
					mem.OpinionOfPlayer -= 0.3f;
				}
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
			if (UI.Main.promptPanel != null && UI.Main.promptPanel.activeSelf) return false;


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
					var id = Variables.lastDroppedItem.id;

					Object.Destroy(Variables.lastDroppedItem.gameObject);

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

			var casterEnt = GetEntityByCharacter(caster);
			if (casterEnt == null) return;
			var targetEnt = GetEntityByCharacter(target.Myself);
			if (targetEnt == null) return;


			if ((!(casterEnt is PlayerSync)) && (!(casterEnt is SimSync)) && (!(casterEnt is NPCSync))) return;

			var hd = new HealingData
			{
				amount = amount,
				isCrit = isCrit,
				isMP = isMP,
				targetID = targetEnt.entityID,
				spellID = spell.Id
			};

			//Logging.Log($"[{caster.name}] healed [{target.name}] for {amount} {(isMP?"MP":"HP")}. Text = {text}");
			if (casterEnt is NPCSync)
			{
				((NPCSync)casterEnt).SendHeal(hd);
			}
			else if(casterEnt is SimSync)
			{
				((SimSync)casterEnt).SendHeal(hd);
			}
			else if(casterEnt is PlayerSync)
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
			if (ClientGroup.currentGroup.groupList == null || ClientGroup.currentGroup.groupList.Count <= 1) return true;

			if (ClientGroup.currentGroup.groupList.Count > 1)
			{
				if (ClientGroup.IsLocalLeader())
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
						ServerGroup.HandleXP(ClientConnectionManager.Instance.LocalPlayerID, xp, useMod, XPBonus);
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
			//if (!ServerConnectionManager.Instance.IsRunning) return false;

			foreach (var mem in GameData.GroupMembers)
			{
				if (mem != null && mem.simIndex >= 0)
				{
					__instance.ActiveSimInstances.Add(mem.SpawnMeInGame(GameData.PlayerControl.transform.position + new Vector3((float)Random.Range(-1, 1), 0f, (float)Random.Range(-1, 1))));
					mem.MyAvatar.InGroup = true;
					mem.isPuller = false;
					mem.Caution = false;
					mem.CurScene = SceneManager.GetActiveScene().name;
					__instance.SimsInZones[mem.simIndex] = SceneManager.GetActiveScene().name;
				}
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
			//Logging.Log($"spawning {__instance.simIndex} {__instance.SimName}\n {System.Environment.StackTrace}");
			return true;
		}


#endregion



#region SIMPLAYERGROUPING

		//Yes it's annoying to have to do each individually, the game should really be using a list to address group members.
		public static bool DismissMember1_Prefix(SimPlayerGrouping __instance)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;

			if (GameData.GroupMembers[0] != null && GameData.GroupMembers[0].MyAvatar != null && GameData.GroupMembers[0].simIndex < 0)
			{
				int pid = Math.Abs(GameData.GroupMembers[0].simIndex) - 1;
				var player = ClientConnectionManager.Instance.GetPlayerFromID((short)pid);
				if (player != null)
				{
					//we also need to check something else here, because the simindex can be the same as a player id
					if (( (NetworkedPlayer)player ).sim.MyStats == GameData.GroupMembers[0].MyStats)
					{
						ClientGroup.RemoveFromGroup((short)pid);
						return false;
					}
				}
			}
			

			//if (ServerConnectionManager.Instance.IsRunning)
			{

				if (GameData.GroupMembers[0] != null && GameData.GroupMembers[0].MyAvatar != null)
				{
					Entity npcsync = GameData.GroupMembers[0].MyAvatar.GetComponent<SimSync>();
					if (npcsync == null)
						npcsync = GameData.GroupMembers[0].MyAvatar.GetComponent<NetworkedSim>();
					if (npcsync != null)
					{
						//SharedNPCSyncManager.Instance.ServerRemoveSim(npcsync.entityID);
						ClientGroup.RemoveFromGroup(npcsync.entityID);
						return false;
					}
				}
			}

			return true;
		}

		public static bool DismissMember2_Prefix(SimPlayerGrouping __instance)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;

			if (GameData.GroupMembers[1] != null && GameData.GroupMembers[1].MyAvatar != null && GameData.GroupMembers[1].simIndex < 0)
			{
				int pid = Math.Abs(GameData.GroupMembers[1].simIndex) - 1;
				var player = ClientConnectionManager.Instance.GetPlayerFromID((short)pid);
				if (player != null)
				{
					//we also need to check something else here, because the simindex can be the same as a player id
					if (( (NetworkedPlayer)player ).sim.MyStats == GameData.GroupMembers[1].MyStats)
					{
						ClientGroup.RemoveFromGroup((short)pid);
						return false;
					}
				}
			}
			

			//if (ServerConnectionManager.Instance.IsRunning)
			{
				if (GameData.GroupMembers[1] != null && GameData.GroupMembers[1].MyAvatar != null)
				{
					Entity npcsync = GameData.GroupMembers[1].MyAvatar.GetComponent<SimSync>();
					if (npcsync == null)
						npcsync = GameData.GroupMembers[1].MyAvatar.GetComponent<NetworkedSim>();
					if (npcsync != null)
					{
						//SharedNPCSyncManager.Instance.ServerRemoveSim(npcsync.entityID);
						ClientGroup.RemoveFromGroup(npcsync.entityID);
						return false;
					}
				}
			}

			return true;
		}
		public static bool DismissMember3_Prefix(SimPlayerGrouping __instance)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;

			
			if (GameData.GroupMembers[2] != null && GameData.GroupMembers[2].MyAvatar != null && GameData.GroupMembers[2].simIndex < 0)
			{
				int pid = Math.Abs(GameData.GroupMembers[2].simIndex) - 1;
				var player = ClientConnectionManager.Instance.GetPlayerFromID((short)pid);
				if (player != null)
				{
					//we also need to check something else here, because the simindex can be the same as a player id
					if (( (NetworkedPlayer)player ).sim.MyStats == GameData.GroupMembers[2].MyStats)
					{
						ClientGroup.RemoveFromGroup((short)pid);
						return false;
					}
				}
			}
			

			//if (ServerConnectionManager.Instance.IsRunning)
			{
				if (GameData.GroupMembers[2] != null && GameData.GroupMembers[2].MyAvatar != null)
				{
					Entity npcsync = GameData.GroupMembers[2].MyAvatar.GetComponent<SimSync>();
					if(npcsync == null)
						npcsync = GameData.GroupMembers[2].MyAvatar.GetComponent<NetworkedSim>();
					if (npcsync != null)
					{
						//SharedNPCSyncManager.Instance.ServerRemoveSim(npcsync.entityID);
						ClientGroup.RemoveFromGroup(npcsync.entityID);
						return false;
					}
				}
			}

			return true;
		}

		public static bool DismissMember4_Prefix(SimPlayerGrouping __instance)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;


			if (GameData.GroupMembers[3] != null && GameData.GroupMembers[3].MyAvatar != null && GameData.GroupMembers[3].simIndex < 0)
			{
				int pid = Math.Abs(GameData.GroupMembers[3].simIndex) - 1;
				var player = ClientConnectionManager.Instance.GetPlayerFromID((short)pid);
				if (player != null)
				{
					//we also need to check something else here, because the simindex can be the same as a player id
					if (((NetworkedPlayer)player).sim.MyStats == GameData.GroupMembers[3].MyStats)
					{
						ClientGroup.RemoveFromGroup((short)pid);
						return false;
					}
				}
			}


			//if (ServerConnectionManager.Instance.IsRunning)
			{
				if (GameData.GroupMembers[3] != null && GameData.GroupMembers[3].MyAvatar != null)
				{
					Entity npcsync = GameData.GroupMembers[3].MyAvatar.GetComponent<SimSync>();
					if (npcsync == null)
						npcsync = GameData.GroupMembers[3].MyAvatar.GetComponent<NetworkedSim>();
					if (npcsync != null)
					{
						//SharedNPCSyncManager.Instance.ServerRemoveSim(npcsync.entityID);
						ClientGroup.RemoveFromGroup(npcsync.entityID);
						return false;
					}
				}
			}

			return true;
		}

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
					if (Mathf.Abs(GameData.PlayerStats.Level - targ.character.MyStats.Level) > 3)
					{
						UpdateSocialLog.LogAdd("This player is outside of your level range.", "yellow");
						return false;
					}
					Client.Grouping.Invites.InvitePlayer(targ);

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
				if (mob.Value != null && mob.Value.character != null && mob.Value.character == _char)
				{
					return (false,false,mob.Value.entityID);
				}
			}

			foreach (var mob in ClientNPCSyncManager.Instance.NetworkedSims)
			{
				if (mob.Value != null && mob.Value.character != null && mob.Value.character == _char)
				{
					return (false, true, mob.Value.entityID);
				}
			}

			foreach (var player in ClientConnectionManager.Instance.Players)
			{
				if (player.Value != null && player.Value.character != null && player.Value.character == _char)
				{
					return (true, false, player.Value.entityID);
				}
			}

			foreach (var mob in SharedNPCSyncManager.Instance.mobs)
			{
				if(mob.Value != null && mob.Value.character != null && mob.Value.character == _char)
				{
					return (false, true, mob.Value.entityID);
				}
			}

			//if (ServerConnectionManager.Instance.IsRunning)
			{
				foreach (var mob in SharedNPCSyncManager.Instance.sims)
				{
					if (mob.Value != null && mob.Value.character != null && mob.Value.character == _char)
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
				if (mob.Value != null && mob.Value.character != null && mob.Value.character == _char)
				{
					return mob.Value;
				}
			}

			foreach (var mob in ClientNPCSyncManager.Instance.NetworkedSims)
			{
				if (mob.Value != null && mob.Value.character != null && mob.Value.character == _char)
				{
					return mob.Value;
				}
			}

			foreach (var player in ClientConnectionManager.Instance.Players)
			{
				if (player.Value != null && player.Value.character != null && player.Value.character == _char)
				{
					return player.Value;
				}
			}

			//if (ServerConnectionManager.Instance.IsRunning)
			{
				foreach (var mob in SharedNPCSyncManager.Instance.sims)
				{
					if (mob.Value != null && mob.Value.character != null && mob.Value.character == _char)
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
					if (mob.Value != null && mob.Value.character != null && mob.Value.character.MyStats == _char)
					{
						return mob.Value;
					}
				}

				foreach (var mob in ClientNPCSyncManager.Instance.NetworkedSims)
				{
					if (mob.Value != null && mob.Value.character != null && mob.Value.character.MyStats == _char)
					{
						return mob.Value;
					}
				}
			}
			else
			{
				foreach (var mob in SharedNPCSyncManager.Instance.mobs)
				{
					if (mob.Value != null && mob.Value.character != null && mob.Value.character.MyStats == _char)
					{
						return mob.Value;
					}
				}

				
			}

			foreach (var mob in SharedNPCSyncManager.Instance.sims)
			{
				if (mob.Value != null && mob.Value.character != null && mob.Value.character.MyStats == _char)
				{
					return mob.Value;
				}
			}

			foreach (var player in ClientConnectionManager.Instance.Players)
			{
				if (player.Value != null && player.Value.character != null && player.Value.character.MyStats == _char)
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
				if (__instance == null) return;
				if (_specificCaster == null) return;

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
				if (__instance == null) return;
				if (_specificCaster == null) return;

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
					if (__instance == null) return;
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
					if (__instance == null) return;
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
					if (__instance == null) return;
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
			if (!ClientConnectionManager.Instance.IsRunning) return true;

			if (__instance == null || __instance.gameObject == null) return true;
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
					if (mob.Value.character != null && mob.Value.character.MyStats == __instance)
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

		public static void RefreshWornSE_Prefix(Stats __instance, Spell _spell)
		{
			if (ClientConnectionManager.Instance.IsRunning)
			{
				if (ClientConnectionManager.Instance.LocalPlayer.stats == __instance)
				{
					var pack = PacketManager.GetOrCreatePacket<PlayerActionPacket>(ClientConnectionManager.Instance.LocalPlayerID, PacketType.PLAYER_ACTION);
					pack.dataTypes.Add(ActionType.WORN_EFFECT_REFRESH);
					var activeEffects = pack.wornEffects ?? new();
					activeEffects.Add(new() { spellID = _spell.Id });
					pack.wornEffects = activeEffects;
				}
				else
				{
					if (__instance == null) return;
					Entity target = GetEntityByStats(__instance);
					if (ClientZoneOwnership.isZoneOwner || target is SimSync)
					{
						if (target == null) return;
						if (target is SimSync)
						{
							var pack = PacketManager.GetOrCreatePacket<PlayerActionPacket>(target.entityID, PacketType.PLAYER_ACTION);
							pack.dataTypes.Add(ActionType.WORN_EFFECT_REFRESH);
							var activeEffects = pack.wornEffects ?? new();
							activeEffects.Add(new() { spellID = _spell.Id });
							pack.wornEffects = activeEffects;
							pack.targetPlayerIDs = SharedNPCSyncManager.Instance.GetPlayerSendList();
							pack.isSim = true;
						}
						else
						{
							var pack = PacketManager.GetOrCreatePacket<EntityActionPacket>(target.entityID, PacketType.ENTITY_ACTION);
							pack.dataTypes.Add(ActionType.WORN_EFFECT_REFRESH);
							var activeEffects = pack.wornEffects ?? new();
							activeEffects.Add(new() { spellID = _spell.Id });
							pack.wornEffects = activeEffects;
							pack.targetPlayerIDs = SharedNPCSyncManager.Instance.GetPlayerSendList();
						}

					}
				}
			}
		}

		public static void DoLevelUp_Postfix(Stats __instance)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return;

			if(__instance.Level < 35)
			{
				var ent = __instance.GetComponent<Entity>();
				if(ent != null && (ent is SimSync || ent is PlayerSync))
				{
					ent.HandleLevelUp();
				}
			}
		}


#endregion




#region CHARACTER

		public static bool CheckVsMR_Prefix(Character __instance, ref float __result)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;

			if (Variables.DontCalculateDamageMitigationCharacters.Contains(__instance))
			{
				__result = 0f;
				return false;
			}

			return true;
		}

		private static void SyncDamage(Character attackedChar, int __result, GameData.DamageType _dmgType, Character _attacker, bool _animEffect, float resistMod, bool isCrit, int baseDmg)
		{

			//See if this is a sync NPC
			var attackedEnt = attackedChar.GetComponent<Entity>();

			if (attackedEnt == null)
			{
				//Logging.LogError("no attacked");
				return;
			}
			if (_attacker == null)
			{
				//Logging.LogError("no attacker");
				return;
			}

			var attackerEnt = _attacker.GetComponent<Entity>();

			if (attackerEnt is not SimSync && attackerEnt is not NPCSync) return;

			if (attackerEnt is NPCSync)
			{
				attackerEnt.SendAttack(__result, attackedEnt.entityID, attackedEnt.type == EntityType.ENEMY, _dmgType, _animEffect, resistMod, isCrit, baseDmg);
				if (attackedEnt is NetworkedPlayer || attackedEnt is NetworkedSim)
				{
					if (ClientGroup.IsPlayerInGroup(attackedEnt.entityID, attackedEnt is NetworkedSim))
					{
						foreach(var mem in GameData.GroupMembers)
						{
							if (mem != null && mem.simIndex >= 0)
								attackerEnt.character.MyNPC.ManageAggro(1, mem.MyStats.Myself);
						}
					}
				}
			}
			else if (attackerEnt is SimSync)
			{
				((SimSync)attackerEnt).SendDamageAttack(__result, attackedEnt.entityID, attackedEnt.type == EntityType.ENEMY, _dmgType, _animEffect, resistMod, isCrit, baseDmg);
			}
			
		}

		private static void SyncDamageClient(Character attackedChar, int damage, GameData.DamageType _dmgType, Character _attacker, bool _animEffect, float resistMod, bool isCrit, int baseDmg)
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
			ClientConnectionManager.Instance.LocalPlayer.SendDamageAttack(damage, attackedID, attackedNPC, _dmgType, _animEffect, resistMod, isCrit, baseDmg);
			//__result = 0;
		}

		public static void CharacterDamageMe_Postfix(Character __instance, ref int __result, int _incdmg, bool _fromPlayer, GameData.DamageType _dmgType, Character _attacker, bool _animEffect, bool _criticalHit)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return;

			if (ClientZoneOwnership.isZoneOwner)
				SyncDamage(__instance, __result, _dmgType, _attacker, _animEffect, 0, _criticalHit, __result);
			if (_attacker != null && _attacker.GetComponent<SimSync>() != null)
				SyncDamage(__instance, __result, _dmgType, _attacker, _animEffect, 0, _criticalHit, __result);
			if (ClientConnectionManager.Instance.IsRunning && _attacker != null && _attacker == ClientConnectionManager.Instance.LocalPlayer.character)
				SyncDamageClient(__instance, __result, _dmgType, _attacker, _animEffect, 0, _criticalHit, __result);

			if (ClientConnectionManager.Instance.LocalPlayer.MySummon != null)
			{
				if (ClientConnectionManager.Instance.LocalPlayer.MySummon.character == _attacker)
					SyncDamage(__instance, __result, _dmgType, _attacker, _animEffect, 0, _criticalHit, __result);
			}
		}

		public static void MagicDamageMe_Postfix(Character __instance, ref int __result, int _dmg, bool _fromPlayer, GameData.DamageType _dmgType, Character _attacker, float resistMod, int _baseDmg)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return;

			if (ClientZoneOwnership.isZoneOwner)
				SyncDamage(__instance, __result, _dmgType, _attacker, false, resistMod, false, _baseDmg);
			if(_attacker.GetComponent<SimSync>() != null)
				SyncDamage(__instance, __result, _dmgType, _attacker, false, resistMod, false, _baseDmg);
			if (ClientConnectionManager.Instance.IsRunning && _attacker != null && _attacker == ClientConnectionManager.Instance.LocalPlayer.character)
				SyncDamageClient(__instance, __result, _dmgType, _attacker, false, resistMod, false, _baseDmg);
			if (ClientConnectionManager.Instance.LocalPlayer.MySummon != null)
			{
				if (ClientConnectionManager.Instance.LocalPlayer.MySummon.character == _attacker)
					SyncDamage(__instance, __result, _dmgType, _attacker, false, resistMod, false, _baseDmg);
			}
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
		/*public static void SpawnPointSpawnNPC_Post(SpawnPoint __instance)
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
		}*/
#endregion




#region NPC

		public static FieldInfo spawnPoint;
		public static MethodInfo startMethod;
		public static MethodInfo handleNameTag;
		public static FieldInfo rotTimer;
		public static FieldInfo leashing;
		public static FieldInfo wander;


		public static bool NPCUpdateNav_Prefix(NPC __instance)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return true;
			var e = __instance.GetComponent<Entity>();
			if (e != null && (e is NetworkedSim || e is NetworkedPlayer || e is NetworkedNPC))
				return false;
			return true;
		}

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
				if (!ClientGroup.IsPlayerInGroup(ent.entityID, false)) return false;
			}
			if(ent.type == EntityType.SIM)
			{
				var simInGrp = ClientGroup.IsPlayerInGroup(ent.entityID, true);
				if (!simInGrp && ent is SimSync) return true;
				if (!simInGrp && ent is NetworkedSim) return false;
			}


			if (ClientGroup.IsLocalLeader())
			{
				//Check for other ppls sims in grp and remove them
				foreach (var mem in GameData.GroupMembers)
				{
					if (mem != null && mem.MyAvatar != null)
					{
						Entity npcsync1 = mem.MyAvatar.GetComponent<Entity>();
						if (npcsync1 != null)
						{
							if (npcsync1 is NetworkedSim)
							{
								ClientGroup.GroupListCallback += GroupListCB;
								hasGRPCB = true;
								zl = __instance;
								ClientGroup.RemoveFromGroup(npcsync1.entityID);
							}
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
			ClientGroup.GroupListCallback -= GroupListCB;
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
			//if (!issim && !SharedNPCSyncManager.Instance.mobs[mobID].isCloseToPlayer) return;

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
				return;
			}

			if (ClientZoneOwnership.isZoneOwner && SharedNPCSyncManager.Instance.overrideToMobID.TryGetValue(__instance, out short mobID))
			{
				SendMobAnimData(mobID, name, value.name, AnimatorSyncType.OVERRIDE);
				return;
			}
			if (ClientConnectionManager.Instance.LocalPlayer.MySummon != null)
			{
				if (ClientConnectionManager.Instance.LocalPlayer.MySummon.GetComponent<NPCSync>().anim.runtimeAnimatorController == __instance)
				{
					SendMobAnimData(ClientConnectionManager.Instance.LocalPlayer.MySummon.entityID, name, value.name, AnimatorSyncType.OVERRIDE);
					return;
				}
			}

			//if (ClientConnectionManager.Instance.LocalPlayer != null && ClientConnectionManager.Instance.LocalPlayer.AnimOverride != null && ClientConnectionManager.Instance.LocalPlayer.AnimOverride == __instance)
			{
				bool hasSim = false;
				short simentid = -1;
				foreach (var sim in SharedNPCSyncManager.Instance.sims)
				{
					if (sim.Value != null && sim.Value.animator != null && sim.Value.animator.runtimeAnimatorController != null && sim.Value.animator.runtimeAnimatorController == __instance)
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
				return;
			}

			if (ClientZoneOwnership.isZoneOwner && SharedNPCSyncManager.Instance.animatorToMobID.TryGetValue(__instance, out short mobID))
			{
				SendMobAnimData(mobID, name, value, AnimatorSyncType.BOOL);
				return;
			}
			if (ClientConnectionManager.Instance.LocalPlayer.MySummon != null)
			{
				if (ClientConnectionManager.Instance.LocalPlayer.MySummon.GetComponent<NPCSync>().anim == __instance)
				{
					SendMobAnimData(ClientConnectionManager.Instance.LocalPlayer.MySummon.entityID, name, value, AnimatorSyncType.BOOL);
					return;
				}
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
				return;
			}

			if (ClientZoneOwnership.isZoneOwner && SharedNPCSyncManager.Instance.animatorToMobID.TryGetValue(__instance, out short mobID))
			{
				SendMobAnimData(mobID, name, value, AnimatorSyncType.FLOAT);
				return;
			}
			if (ClientConnectionManager.Instance.LocalPlayer.MySummon != null)
			{
				if (ClientConnectionManager.Instance.LocalPlayer.MySummon.GetComponent<NPCSync>().anim == __instance)
				{
					SendMobAnimData(ClientConnectionManager.Instance.LocalPlayer.MySummon.entityID, name, value, AnimatorSyncType.FLOAT);
					return;
				}
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
				return;
			}

			if (ClientZoneOwnership.isZoneOwner && SharedNPCSyncManager.Instance.animatorToMobID.TryGetValue(__instance, out short mobID))
			{
				SendMobAnimData(mobID, name, value, AnimatorSyncType.INT);
				return;
			}
			if (ClientConnectionManager.Instance.LocalPlayer.MySummon != null)
			{
				if (ClientConnectionManager.Instance.LocalPlayer.MySummon.GetComponent<NPCSync>().anim == __instance)
				{
					SendMobAnimData(ClientConnectionManager.Instance.LocalPlayer.MySummon.entityID, name, value, AnimatorSyncType.INT);
					return;
				}
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
				return;
			}

			if (ClientZoneOwnership.isZoneOwner && SharedNPCSyncManager.Instance.animatorToMobID.TryGetValue(__instance, out short mobID))
			{
				SendMobAnimData(mobID, name, true, AnimatorSyncType.TRIG);
				return;
			}
			if (ClientConnectionManager.Instance.LocalPlayer.MySummon != null)
			{
				if (ClientConnectionManager.Instance.LocalPlayer.MySummon.GetComponent<NPCSync>().anim == __instance)
				{
					SendMobAnimData(ClientConnectionManager.Instance.LocalPlayer.MySummon.entityID, name, true, AnimatorSyncType.TRIG);
					return;
				}
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
				return;
			}

			if (ClientZoneOwnership.isZoneOwner && SharedNPCSyncManager.Instance.animatorToMobID.TryGetValue(__instance, out short mobID))
			{
				SendMobAnimData(mobID, name, false, AnimatorSyncType.RSTTRIG);
				return;
			}
			if (ClientConnectionManager.Instance.LocalPlayer.MySummon != null)
			{
				if (ClientConnectionManager.Instance.LocalPlayer.MySummon.GetComponent<NPCSync>().anim == __instance)
				{
					SendMobAnimData(ClientConnectionManager.Instance.LocalPlayer.MySummon.entityID, name, false, AnimatorSyncType.RSTTRIG);
					return;
				}
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
