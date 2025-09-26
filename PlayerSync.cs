using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Linq;
using ErenshorCoop.Client;
using ErenshorCoop.Shared;
using ErenshorCoop.Shared.Packets;
using ErenshorCoop.Server;

namespace ErenshorCoop
{
	//Local player
	public class PlayerSync : Entity
	{
		public Vector3 previousPosition = Vector3.zero;
		public Quaternion previousRotation = Quaternion.identity;
		public int previousHealth = 0;
		public int previousMP = 0;
		public Entity previousTarget = null;

		public Animator animator;
		public Inventory inventory;
		public Stats stats;
		public AnimatorOverrideController AnimOverride;

		public bool hasSentConnect = false;

		private static PlayerSync _instance;
		public void Awake()
		{
			if(_instance != null) Destroy(this);

			_instance = this;
			ErenshorCoopMod.OnGameMenuLoad += OnGameMenuLoad;
			ErenshorCoopMod.OnGameMapLoad += OnGameMapLoad;
			ClientConnectionManager.Instance.OnClientConnect += OnClientConnect;
			ClientConnectionManager.Instance.OnConnect += OnGameConnect;

			animator = GetComponent<Animator>();
			inventory = GetComponent<Inventory>();
			stats = GetComponent<Stats>();
			character = GetComponent<Character>();
			var pc = GetComponent<PlayerControl>();
			var com = GetComponent<PlayerCombat>();
			AnimOverride = GetComponent<PlayerControl>().AnimOverride;

			Extensions.BuildPlayerClipLookup(pc, com);

			entityName = GameData.CurrentCharacterSlot.CharName;
			zone = SceneManager.GetActiveScene().name;

			type = EntityType.PLAYER;

			
		}

		private void OnClientConnect(short __, string ___, string ____)
		{
			//Logging.Log("hurrdurr");
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
					var pc = GetComponent<PlayerControl>();
					var com = GetComponent<PlayerCombat>();
					AnimOverride = pc.AnimOverride;
					Extensions.BuildPlayerClipLookup(pc, com);
					entityName = GameData.CurrentCharacterSlot.CharName;
					zone = SceneManager.GetActiveScene().name;
				}

				var packet = PacketManager.GetOrCreatePacket<PlayerConnectionPacket>(ClientConnectionManager.Instance.LocalPlayerID, PacketType.PLAYER_CONNECT, true)
					.SetData("scene",    sceneIDX)
					.SetData("name",     GameData.CurrentCharacterSlot.CharName)
					.SetData("position", transform.position)
					.SetData("rotation", transform.rotation)
					.SetData("_class",   GameData.PlayerStats.CharacterClass)
					.SetData("lookData", l)
					.SetData("gearData", gear)
					.SetData("level",    GameData.PlayerStats.Level)
					.SetData("health",   stats.CurrentHP)
					.SetData("mp",       stats.CurrentMana);
				packet.isSim = false;
				packet.CanSend();
				timeSinceLastPeriodicUpdate = Time.time;

				StartCoroutine(DelayedSendEffects());

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

		public void StartZoneTransfer(Zoneline zoneline)
		{
			StartCoroutine(DelayedZoneTransfer(zoneline));
		}
		public IEnumerator DelayedZoneTransfer(Zoneline zoneline)
		{
			yield return new WaitForSeconds(2f);
			zoneline.CallZoning();
		}

		private void OnGameMapLoad(Scene scene)
		{
			zone = scene.name;

			if (!ClientConnectionManager.Instance.IsRunning) return;
			if (!hasSentConnect) return;

			PacketManager.GetOrCreatePacket<PlayerDataPacket>(entityID, PacketType.PLAYER_DATA).AddPacketData(PlayerDataType.SCENE, "scene", zone);

			//Check if other players are in same scene, this is kinda hacky but it absolutely makes sure they are visible or not
			foreach (var p in ClientConnectionManager.Instance.Players)
			{
				if (p.Value.zone == zone || p.Value.zone == zone)
				{
					p.Value.gameObject.SetActive(true);
					if(p.Value.MySummon != null && p.Value.MySummon.gameObject != null)
						p.Value.MySummon.gameObject.SetActive(true);
				}
				else
				{
					p.Value.gameObject.SetActive(false);
					if (p.Value.MySummon != null && p.Value.MySummon.gameObject != null)
						p.Value.MySummon.gameObject.SetActive(false);
				}
			}

			//Do we have a summon?
			if (MySummon != null)
			{
				CreateSummon(GameData.SpellDatabase.GetSpellByID(MySummon.spellID), MySummon.gameObject);
			}

			if (Variables.droppedItems.TryGetValue(zone, out var items))
			{
				foreach (var itm in items)
				{
					ClientConnectionManager.Instance.SpawnItem(itm.item, itm.quantity, itm.pos, zone, itm.id,true);
				}
			}
		}

		private void OnGameConnect()
		{
			SendConnectData();
		}

		public void SendConnectData()
		{
			Logging.Log("Sending player connect data...");
			//Tells others to create our player with this data, if they don't already have us
			var sceneIDX = SceneManager.GetActiveScene().name;
			(_, LookData l, List<GearData> gear) = GetLookData(true);

			var packet = PacketManager.GetOrCreatePacket<PlayerConnectionPacket>(ClientConnectionManager.Instance.LocalPlayerID, PacketType.PLAYER_CONNECT, true)
				.SetData("scene",    sceneIDX)
				.SetData("name",     GameData.CurrentCharacterSlot.CharName)
				.SetData("position", transform.position)
				.SetData("rotation", transform.rotation)
				.SetData("_class",   GameData.PlayerStats.CharacterClass)
				.SetData("lookData", l)
				.SetData("gearData", gear)
				.SetData("level",    GameData.PlayerStats.Level)
				.SetData("health",   stats.CurrentHP)
				.SetData("mp",   stats.CurrentMana);
			packet.isSim = false;
			packet.CanSend();
			hasSentConnect = true;
			timeSinceLastPeriodicUpdate = Time.time;
			StartCoroutine(DelayedSendEffects());
		}

		private void OnGameMenuLoad(Scene scene)
		{
			ClientConnectionManager.Instance.Disconnect();
			Destroy(this);
		}


		private void OnDestroy()
		{
			ClientConnectionManager.Instance.Disconnect();
			ErenshorCoopMod.OnGameMenuLoad -= OnGameMenuLoad;
			ErenshorCoopMod.OnGameMapLoad -= OnGameMapLoad;
			ClientConnectionManager.Instance.OnClientConnect -= OnClientConnect;
			ClientConnectionManager.Instance.OnConnect -= OnGameConnect;
			//Logging.LogError("Destroyed.");
		}
		public void Update()
		{
			if (ServerConnectionManager.Instance != null && ServerConnectionManager.Instance.IsRunning)
				hasSentConnect = true;

			if (!ClientConnectionManager.Instance.IsRunning) return;
			if (!hasSentConnect) return;

			//if (Vector3.Distance(transform.position,previousPosition) > 0.1f && lastInterpTime >= interpCall)
			if(transform.position != previousPosition)
			{
				PacketManager.GetOrCreatePacket<PlayerTransformPacket>(entityID, PacketType.PLAYER_TRANSFORM).AddPacketData(PlayerDataType.POSITION, "position", transform.position);
				previousPosition = transform.position;
			}
			if (previousRotation != transform.rotation)
			{
				PacketManager.GetOrCreatePacket<PlayerTransformPacket>(entityID, PacketType.PLAYER_TRANSFORM).AddPacketData(PlayerDataType.ROTATION, "rotation", transform.rotation);
				previousRotation = transform.rotation;
			}

			if (previousHealth != stats.CurrentHP)
			{
				PacketManager.GetOrCreatePacket<PlayerDataPacket>(entityID, PacketType.PLAYER_DATA).AddPacketData(PlayerDataType.HEALTH, "health", stats.CurrentHP);
				previousHealth = stats.CurrentHP;
			}

			if (previousMP != stats.CurrentMana)
			{
				PacketManager.GetOrCreatePacket<PlayerDataPacket>(entityID, PacketType.PLAYER_DATA).AddPacketData(PlayerDataType.MP, "mp", stats.CurrentMana);
				previousMP = stats.CurrentMana;
			}
			

			var curTar = GameData.PlayerControl.CurrentTarget;
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
					p.targetType = EntityType.LOCAL_PLAYER;
					previousTarget = null;
				}
				else
				{
					//Logging.Log($"{name} targetres invalid {curTar}");
				}
			}
			
			(bool hasChanged, LookData lookData, List<GearData> gearData ) = GetLookData();
			if (hasChanged)
			{
				PacketManager.GetOrCreatePacket<PlayerDataPacket>(entityID, PacketType.PLAYER_DATA)
					.AddPacketData(PlayerDataType.GEAR, "lookData", lookData)
					.SetData("gearData", gearData);
			}
		}

		public void SendDamageAttack(int damage, short attackedID, bool attackedIsNPC, GameData.DamageType dmgType, bool effect, float resistMod, bool isCrit, int baseDmg)
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
					isCrit = isCrit,
					baseDmg = baseDmg
				});
			p.isSim = false;
		}


		private readonly Dictionary<string, gear_check_data> previousGear = new();

		public struct gear_check_data
		{
			public string Id;
			public int quant;
		}
		private string charm_slot_id = "";
		private string aura_slot_id = "";
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
			for (var i = 0; i < inventory.EquipmentSlots.Count; i++)
			{
				var slot = inventory.EquipmentSlots[i];
				var slotType = slot.ThisSlotType;
				string itemID = slot.MyItem.Id;
				var slotKey = $"{slotType}_{i}";

				gear_check_data gc = new() { Id = itemID, quant = slot.Quantity };

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
					case Item.SlotType.Aura: break; //skip
					case Item.SlotType.Charm: break;
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

			if (inventory.AuraSlot.MyItem.Id != aura_slot_id)
			{
				anyChanges = true;
			}
			if (inventory.CharmSlot.MyItem.Id != charm_slot_id)
			{
				anyChanges = true;
			}

			//Logging.Log($"Changes {anyChanges}");

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

			//Aura
			GearData _gd = new()
			{
				itemID = inventory.AuraSlot.MyItem.Id,
				slotType = Item.SlotType.Aura,
				quality = inventory.AuraSlot.Quantity
			};
			gearData.Add(_gd);

			aura_slot_id = inventory.AuraSlot.MyItem.Id;
			numSlots++;

			//Charm
			_gd = new()
			{
				itemID = inventory.CharmSlot.MyItem.Id,
				slotType = Item.SlotType.Charm,
				quality = inventory.CharmSlot.Quantity
			};
			gearData.Add(_gd);

			charm_slot_id = inventory.CharmSlot.MyItem.Id;
			numSlots++;


			//It's super expensive to always attach these, but this isn't done very often
			var lookData = new LookData
			{
				hairName = GameData.CurrentCharacterSlot.HairName,
				isMale = GameData.CurrentCharacterSlot.isMale,
				hairColor = GameData.CurrentCharacterSlot.HairColor,
				skinColor = GameData.CurrentCharacterSlot.SkinColor,
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
		}

		
	}
}
