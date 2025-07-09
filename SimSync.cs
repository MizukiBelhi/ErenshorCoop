using ErenshorCoop.Client;
using ErenshorCoop.Shared;
using ErenshorCoop.Shared.Packets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace ErenshorCoop
{
	public class SimSync : Entity
	{
		public Vector3 previousPosition = Vector3.zero;
		public Quaternion previousRotation = Quaternion.identity;
		public int previousHealth = 0;
		public int previousLevel = 0;
		public int previousMP = 0;
		public Entity previousTarget = null;
		//public short entityID = -1;

		public Animator animator;
		public Inventory inventory;
		public Stats stats;
		//public AnimatorOverrideController AnimOverride;
		public NPC npc;
		public SimPlayer sim;
		public NavMeshAgent nav;

		public ModularParts mod;
		public ModularPar modPar;

		public int simIndex = -1;

		public bool hasSentConnect = false;

		public bool isCloseToPlayer = true;

		public Vector3 randomizeOffset = Vector3.zero;

		public Transform target;

		public void Awake()
		{
			ErenshorCoopMod.OnGameMenuLoad += OnGameMenuLoad;
			ErenshorCoopMod.OnGameMapLoad += OnGameMapLoad;
			ClientConnectionManager.Instance.OnClientConnect += OnClientConnect;
			ClientConnectionManager.Instance.OnConnect += OnGameConnect;

			animator = GetComponent<Animator>();
			inventory = GetComponent<Inventory>();
			stats = GetComponent<Stats>();
			character = GetComponent<Character>();
			npc = GetComponent<NPC>();
			sim = GetComponent<SimPlayer>();
			modPar = GetComponentInChildren<ModularPar>();
			nav = GetComponent<NavMeshAgent>();

			simIndex = sim.myIndex;

			Extensions.BuildClipLookup(npc);

			previousLevel = stats.Level;
			entityName = npc.NPCName + $" [{GameData.CurrentCharacterSlot.CharName}]";
			zone = SceneManager.GetActiveScene().name;

			type = EntityType.SIM;


			do
			{
				randomizeOffset = new Vector3(Random.Range(-3, 3), 0f, Random.Range(-3, 3));
			}
			while (randomizeOffset.magnitude < 2f);

			SharedNPCSyncManager.Instance.simToSync.Add(sim, this);
		}

		private void OnClientConnect(short __, string ___, string ____)
		{
			var sceneIDX = SceneManager.GetActiveScene().name;

			//Not sure why putting this in a try-catch fixes it failing to send the packet, but here we are
			try
			{
				(_, LookData l, List<GearData> gear) = GetLookData(true);

				if (stats == null) //Just in case
				{
					animator = GetComponent<Animator>();
					inventory = GetComponent<Inventory>();
					stats = GetComponent<Stats>();
					character = GetComponent<Character>();
					npc = GetComponent<NPC>();
					sim = GetComponent<SimPlayer>();
					modPar = GetComponentInChildren<ModularPar>();
					Extensions.BuildClipLookup(npc);
					simIndex = sim.myIndex;
					previousLevel = stats.Level;
					entityName = npc.NPCName + $" [{GameData.CurrentCharacterSlot.CharName}]";
					zone = SceneManager.GetActiveScene().name;
				}

				var packet = PacketManager.GetOrCreatePacket<PlayerConnectionPacket>(entityID, PacketType.PLAYER_CONNECT, true)
					.SetData("scene", sceneIDX)
					.SetData("name", entityName)
					.SetData("position", transform.position)
					.SetData("rotation", transform.rotation)
					.SetData("_class", stats.CharacterClass)
					.SetData("lookData", l)
					.SetData("gearData", gear)
					.SetData("level", stats.Level)
					.SetData("health", stats.CurrentHP)
					.SetData("mp", stats.CurrentMana)
					.SetData("ownerID", ClientConnectionManager.Instance.LocalPlayerID);
				packet.isSim = true;
				packet.CanSend();

				//Logging.Log($"sending sim client con {entityName}");

				//Do we have a summon?
				if (MySummon != null)
				{
					//Needs to be delayed in case the joining player isn't in the zone list yet
					StartCoroutine(DelayedSendPet());
				}
			}
			catch (Exception ex)
			{
				Logging.LogError($"{ex.Message} \r\n {ex.StackTrace}");
			}

			if (MySummon == null || MySummon.character.MyNPC != character.MyCharmedNPC)
			{
				if (character.MyCharmedNPC != null)
				{
					Destroy(character.MyCharmedNPC.gameObject);
				}
			}
			if (MySummon != null)
			{
				MySummon.ReceiveRequestID(MySummon.entityID);
			}
		}


		public IEnumerator DelayedSendPet()
		{
			yield return new WaitForSeconds(2f);
			MySummon.ReceiveRequestID(MySummon.entityID);
		}

		private void OnGameMapLoad(Scene scene)
		{
			zone = scene.name;

			if (!ClientConnectionManager.Instance.IsRunning) return;
			if (!hasSentConnect) return;

			var p = PacketManager.GetOrCreatePacket<PlayerDataPacket>(entityID, PacketType.PLAYER_DATA).AddPacketData(PlayerDataType.SCENE, "scene", zone);
			p.isSim = true;

			//Do we have a summon?
			if (MySummon != null)
			{
				CreateSummon(GameData.SpellDatabase.GetSpellByID(MySummon.spellID), MySummon.gameObject);
			}

		}

		private void OnGameConnect()
		{
			//SendConnectData();
		}

		public void SendConnectData()
		{
			//Tells others to create our player with this data, if they don't already have us
			var sceneIDX = SceneManager.GetActiveScene().name;
			(_, LookData l, List<GearData> gear) = GetLookData(true);

			var packet = PacketManager.GetOrCreatePacket<PlayerConnectionPacket>(entityID, PacketType.PLAYER_CONNECT, true)
				.SetData("scene", sceneIDX)
				.SetData("name", entityName)
				.SetData("position", transform.position)
				.SetData("rotation", transform.rotation)
				.SetData("_class", stats.CharacterClass)
				.SetData("lookData", l)
				.SetData("gearData", gear)
				.SetData("level", stats.Level)
				.SetData("health", stats.CurrentHP)
				.SetData("mp", stats.CurrentMana)
				.SetData("ownerID", ClientConnectionManager.Instance.LocalPlayerID);
			packet.isSim = true;
			packet.CanSend();
			hasSentConnect = true;
		}

		private void OnGameMenuLoad(Scene scene)
		{
			//ClientConnectionManager.Instance.Disconnect();
			Destroy(this);
		}

		public void SendLevelUpdate()
		{
			var p = PacketManager.GetOrCreatePacket<PlayerDataPacket>(entityID, PacketType.PLAYER_DATA).AddPacketData(PlayerDataType.LEVEL, "level", GameData.PlayerStats.Level);
			p.isSim = true;
		}

		private void OnDestroy()
		{
			//ClientConnectionManager.Instance.Disconnect();
			ErenshorCoopMod.OnGameMenuLoad -= OnGameMenuLoad;
			ErenshorCoopMod.OnGameMapLoad -= OnGameMapLoad;
			ClientConnectionManager.Instance.OnClientConnect -= OnClientConnect;
			ClientConnectionManager.Instance.OnConnect -= OnGameConnect;

			var p = PacketManager.GetOrCreatePacket<PlayerDataPacket>(entityID, PacketType.PLAYER_DATA);
			p.dataTypes.Add(PlayerDataType.DESTR_SIM);
			p.isSim = true;

			if (SharedNPCSyncManager.Instance != null)
			{
				if(SharedNPCSyncManager.Instance.sims.ContainsKey(entityID))
					SharedNPCSyncManager.Instance.sims.Remove(entityID);
				if (SharedNPCSyncManager.Instance.simToSync.ContainsKey(sim))
					SharedNPCSyncManager.Instance.simToSync.Remove(sim);
			}



			//Logging.LogError($"{name} Destroyed.");
		}
		public void Update()
		{
			if (!ClientConnectionManager.Instance.IsRunning) return;
			if (!hasSentConnect) return;


			//if (Vector3.Distance(transform.position,previousPosition) > 0.1f && lastInterpTime >= interpCall)
			if (transform.position != previousPosition)
			{
				var p = PacketManager.GetOrCreatePacket<PlayerTransformPacket>(entityID, PacketType.PLAYER_TRANSFORM).AddPacketData(PlayerDataType.POSITION, "position", transform.position);
				p.isSim = true;
				previousPosition = transform.position;
			}
			if (previousRotation != transform.rotation)
			{
				var p = PacketManager.GetOrCreatePacket<PlayerTransformPacket>(entityID, PacketType.PLAYER_TRANSFORM).AddPacketData(PlayerDataType.ROTATION, "rotation", transform.rotation);
				p.isSim = true;
				previousRotation = transform.rotation;
			}

			if (previousHealth != stats.CurrentHP)
			{
				var p = PacketManager.GetOrCreatePacket<PlayerDataPacket>(entityID, PacketType.PLAYER_DATA).AddPacketData(PlayerDataType.HEALTH, "health", stats.CurrentHP);
				p.isSim = true;
				previousHealth = stats.CurrentHP;
			}

			if (previousLevel != GameData.PlayerStats.Level)
			{
				SendLevelUpdate();
				previousLevel = GameData.PlayerStats.Level;
			}

			if (previousMP != stats.CurrentMana)
			{
				var p = PacketManager.GetOrCreatePacket<PlayerDataPacket>(entityID, PacketType.PLAYER_DATA).AddPacketData(PlayerDataType.MP, "mp", stats.CurrentMana);
				p.isSim = true;
				previousMP = stats.CurrentMana;
			}


			var curTar = npc.GetCurrentTarget();
			Entity curTarEnt = null;
			if (curTar != null)
				curTarEnt = curTar.GetComponent<Entity>();

			if (curTarEnt != null)
			{
				//Logging.Log($"{name} target set {curTarEnt.name}");
				if (previousTarget != curTarEnt)
				{
					var p = PacketManager.GetOrCreatePacket<PlayerDataPacket>(entityID, PacketType.PLAYER_DATA);
					p.AddPacketData(PlayerDataType.CURTARGET, "targetID", curTarEnt.entityID);
					p.targetType = curTarEnt.type;
					p.isSim = true;
					if (curTarEnt is PlayerSync || curTarEnt is NetworkedPlayer)
						p.targetType = EntityType.PLAYER;

					previousTarget = curTarEnt;
				}
			}
			else
			{
				if (previousTarget != null)
				{
					var p = PacketManager.GetOrCreatePacket<PlayerDataPacket>(entityID, PacketType.PLAYER_DATA);
					p.AddPacketData(PlayerDataType.CURTARGET, "targetID", (short)-1);
					p.isSim = true;
					p.targetType = EntityType.LOCAL_PLAYER;
					previousTarget = null;
				}
				else
				{
					//Logging.Log($"{name} targetres invalid {curTar}");
				}
			}

			(bool hasChanged, LookData lookData, List<GearData> gearData) = GetLookData();
			if (hasChanged)
			{
				var p = PacketManager.GetOrCreatePacket<PlayerDataPacket>(entityID, PacketType.PLAYER_DATA)
					.AddPacketData(PlayerDataType.GEAR, "lookData", lookData)
					.SetData("gearData", gearData);
				p.isSim = true;
			}
			//Happens when zoning, we need to set the follow again
			if (Grouping.IsLocalLeader() && Grouping.IsPlayerInGroup(entityID, true) && target == null)
				target = ClientConnectionManager.Instance.LocalPlayer.transform;
			if(target != null && GameHooks.followPlayer != null)
			{
				sim.InGroup = true;
				GameHooks.followPlayer.Invoke(sim, null);
			}
			if(target == null)
				sim.InGroup = false;
		}

		public void SendDamageAttack(int damage, short attackedID, bool attackedIsNPC, GameData.DamageType dmgType, bool effect, float resistMod, bool isCrit)
		{
			if (!hasSentConnect) return;

			var p = PacketManager.GetOrCreatePacket<PlayerActionPacket>(entityID, PacketType.PLAYER_ACTION).AddPacketData(ActionType.ATTACK, "attackData",
				new PlayerAttackData()
				{
					attackedID = attackedID,
					attackedIsNPC = attackedIsNPC,
					damage = damage,
					damageType = dmgType,
					effect = effect,
					resistMod = resistMod,
					isCrit = isCrit
				});
			p.isSim = true;
		}


		private readonly Dictionary<string, gear_check_data> previousGear = new();

		public struct gear_check_data
		{
			public string Id;
			public int quant;
		}

		public (bool, LookData, List<GearData>) GetLookData(bool force = false)
		{
			var numSlots = 0;

			Dictionary<string, gear_check_data> newGear = new();
			Dictionary<string, gear_check_data> weaponGear = new();

			HashSet<string> includedBracerIDs = new();
			HashSet<string> includedRingIDs = new();

			var bracerCount = 0;
			var ringCount = 0;

			//Loop through equip
			for (var i = 0; i < sim.MyEquipment.Count; i++)
			{
				var slot = sim.MyEquipment[i];
				if (slot == null) continue;
				var slotType = slot.ThisSlotType;
				string itemID = slot.MyItem.Id;
				var slotKey = $"{slotType}_{i}";

				gear_check_data gc = new() { Id = itemID, quant = slot.Quant };

				switch (slotType)
				{
					case Item.SlotType.Primary:
						weaponGear["Primary_0"] = gc;
						break;
					case Item.SlotType.Secondary:
						weaponGear["Secondary_0"] = gc;
						break;
					case Item.SlotType.Bracer:
						if (bracerCount < 2 && includedBracerIDs.Add(slotKey))
						{
							newGear[slotKey] = gc;
							bracerCount++;
						}
						break;
					case Item.SlotType.Ring:
						if (ringCount < 2 && includedRingIDs.Add(slotKey))
						{
							newGear[slotKey] = gc;
							ringCount++;
						}
						break;
					default:
						newGear[slotKey] = gc;
					break;
				}
			}

			//Check if the gear changed
			bool anyChanges = force;

			foreach (var pair in newGear.Concat(weaponGear))
			{
				if (previousGear.TryGetValue(pair.Key, out gear_check_data oldID) && (oldID.Id == pair.Value.Id && oldID.quant == pair.Value.quant)) continue;
				anyChanges = true;
				break;
			}

			if (!anyChanges) return (false, new(), new());

			List<GearData> gearData = new();

			foreach (var pair in newGear)
			{
				string typeStr = pair.Key.Split('_')[0];
				GearData gd = new()
				{
					itemID = pair.Value.Id,
					slotType = (Item.SlotType)Enum.Parse(typeof(Item.SlotType), typeStr),
					quality = pair.Value.quant
				};
				gearData.Add(gd);
				previousGear[pair.Key] = pair.Value;
				numSlots++;
			}

			foreach (var pair in weaponGear)
			{
				string typeStr = pair.Key.Split('_')[0];
				GearData gd = new()
				{
					itemID = pair.Value.Id,
					slotType = (Item.SlotType)Enum.Parse(typeof(Item.SlotType), typeStr),
					quality = pair.Value.quant
				};
				gearData.Add(gd);
				previousGear[pair.Key] = pair.Value;
				numSlots++;
			}

			bool isMale = modPar.Female.enabled? false: true;
			ModularParts activePar = isMale ? modPar.Female : modPar.Male;
			//It's super expensive to always attach these, but this isn't done very often
			var lookData = new LookData
			{
				hairName = activePar.HairName,
				isMale = isMale,
				hairColor = activePar.HairCol,
				skinColor = activePar.SkinCol,
			};

			return (true, lookData, gearData);
		}

		//Sends packet when we're using a healing spell (mp or hp)
		public void SendHeal(HealingData hd)
		{
			var pack = PacketManager.GetOrCreatePacket<PlayerActionPacket>(entityID, PacketType.PLAYER_ACTION);
			var healingData = pack.healingData ?? new();
			healingData.Add(hd);
			pack.dataTypes.Add(ActionType.HEAL);
			pack.healingData = healingData;
			pack.isSim = true;
		}

		public void SendWand(WandAttackData wa)
		{
			var pack = PacketManager.GetOrCreatePacket<PlayerActionPacket>(entityID, PacketType.PLAYER_ACTION);
			var wandData = pack.wandData ?? new();
			wandData.Add(wa);
			pack.dataTypes.Add(ActionType.WAND_ATTACK);
			pack.wandData = wandData;
			pack.isSim = true;
		}

		public new void SendAttack(int damage, short attackerID, bool attackerNPC, GameData.DamageType dmgType, bool animEffect, float resistMod, bool isCrit)
		{
			var p = PacketManager.GetOrCreatePacket<EntityActionPacket>(entityID, PacketType.PLAYER_ACTION);
			p.AddPacketData(ActionType.ATTACK, "attackData",
				new EntityAttackData()
				{
					attackedID = attackerID,
					attackedIsNPC = attackerNPC,
					damage = damage,
					damageType = dmgType,
					effect = animEffect,
					resistMod = resistMod,
					isCrit = isCrit
				});
			p.SetData("targetPlayerIDs", SharedNPCSyncManager.Instance.GetPlayerSendList());
			p.entityType = type;
			p.zone = SceneManager.GetActiveScene().name;
			p.isSim = true;
		}
	}
}
