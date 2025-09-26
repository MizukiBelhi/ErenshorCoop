using ErenshorCoop.Client;
using ErenshorCoop.Server;
using ErenshorCoop.Shared.Packets;
using LiteNetLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using Steamworks;
using System.Collections.Generic;
using System.Collections;
using ErenshorCoop.Client.Grouping;
using TMPro;
using System;

namespace ErenshorCoop.Shared
{
	public class Entity : MonoBehaviour
	{
		public string entityName = "";
		public NetPeer peer;
		public CSteamID steamID;
		public short entityID = -1;
		public Character character;
		public EntityType type = EntityType.ENEMY;
		public string zone = "";
		protected Entity aggroTarget;

		public bool isGuardian = false;
		public int guardianId = 0;
		public int treasureChestID = 0;

		//Summon
		public Entity MySummon;
		public Entity owner;
		public EntityType ownerType;
		public string spellID;

		private GameObject spellEffect;

		internal float timeSinceLastPeriodicUpdate = 0;

		public CustomSpawnID spawnID;

		public void Start()
		{
			if (type == EntityType.PET && entityID == -1 && (GetType() == typeof(NPCSync)))
			{
				RequestID();
			}
			if (type == EntityType.SIM && entityID == -1 && (GetType() == typeof(SimSync)))
				RequestID();
		}

		public void RequestID()
		{
			if (type == EntityType.SIM)
			{
				var sim = GetComponent<SimPlayer>();
				if (Variables.savedZoneSimID.ContainsKey(sim.myIndex))
				{
					entityID = Variables.savedZoneSimID[sim.myIndex];
					if(SharedNPCSyncManager.Instance.sims.ContainsKey(entityID))
					{
						var e = SharedNPCSyncManager.Instance.sims[entityID];
						if (e != null)
						{
							//Logging.Log($"[{entityName}] We already have a sim with id {entityID}: {e.entityName}");
							DestroyImmediate(e.gameObject);
							//SharedNPCSyncManager.Instance.sims.Remove(entityID);
						}
					}

					SharedNPCSyncManager.Instance.sims.Add(entityID, ((SimSync)this));
					StartCoroutine(DelayedSendConnect());
					return;
				}
			}
			//ask server for an id
			if (!ServerConnectionManager.Instance.IsRunning)
			{
				ClientConnectionManager.Instance.requestReceivers.Add(this);

				var pa = PacketManager.GetOrCreatePacket<PlayerRequestPacket>(ClientConnectionManager.Instance.LocalPlayerID, PacketType.PLAYER_REQUEST);
				if (pa.requestEntityType == null)
					pa.requestEntityType = new();
				pa.dataTypes.Add(Request.ENTITY_ID);
				pa.requestEntityType.Add(type);
				//pa.CanSend();
				//Logging.Log($"Sending entityID request");
			}
			else
			{
				entityID = SharedNPCSyncManager.Instance.GetFreeId();
				ReceiveRequestID(entityID, true);
			}
		}

		public void FixedUpdate()
		{
			if(entityID != -1 && ClientConnectionManager.Instance.IsRunning && (GetType() == typeof(NPCSync) || GetType() == typeof(SimSync) || GetType() == typeof(PlayerSync)))
			{
				if (timeSinceLastPeriodicUpdate + 5 < Time.time)
				{
					if (GetType() != typeof(NPCSync))
					{
						var p = PacketManager.GetOrCreatePacket<PlayerDataPacket>(entityID, PacketType.PLAYER_DATA);
						p.AddPacketData(PlayerDataType.PERIODIC_UPDATE, "health", character.MyStats.CurrentHP);
						p.mp = character.MyStats.CurrentMana;
						p.maxHealth = character.MyStats.CurrentMaxHP;
						p.maxMP = character.MyStats.GetCurrentMaxMana();
						p.scene = SceneManager.GetActiveScene().name;
						p.zone = SceneManager.GetActiveScene().name;
						p.alive = character.Alive;

						p.isSim = GetType() == typeof(SimSync);
					}
					else if(GetType() == typeof(NPCSync))
					{
						var p = PacketManager.GetOrCreatePacket<EntityDataPacket>(entityID, PacketType.ENTITY_DATA);
						p.AddPacketData(EntityDataType.PERIODIC_UPDATE, "mp", character.MyStats.CurrentMana);
						p.health = character.MyStats.CurrentHP;
						p.maxHealth = character.MyStats.CurrentMaxHP;
						p.maxMP = character.MyStats.GetCurrentMaxMana();
						p.SetData("targetPlayerIDs", SharedNPCSyncManager.Instance.GetPlayerSendList());
						p.entityType = type;
						p.zone = SceneManager.GetActiveScene().name;
					}

					timeSinceLastPeriodicUpdate = Time.time;
				}
				if (type == EntityType.PLAYER)
					ClientGroup.PeriodicGroupCheck();
			}
		}

		public void HandleLevelUp()
		{
			var p = PacketManager.GetOrCreatePacket<PlayerDataPacket>(entityID, PacketType.PLAYER_DATA).AddPacketData(PlayerDataType.LEVEL, "level", character.MyStats.Level);
			p.isSim = type == EntityType.SIM;
		}

		public void CheckSummon()
		{
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

		public IEnumerator DelayedSendConnect()
		{
			yield return new WaitForSeconds(3f);
			((SimSync)this).SendConnectData();
			CheckSummon();
		}

		public void ReceiveRequestID(short id, bool isHost=false)
		{
			//Logging.Log($"received entityID request {id} for {name}");
			if (GetType() != typeof(NPCSync) && GetType() != typeof(SimSync))
				return;
			if (id == -1) return;

			entityID = id;

			if (type == EntityType.PET)
				SharedNPCSyncManager.Instance.ServerSpawnPet(gameObject, owner.entityID, id, spellID);
			else
			{
				if(isGuardian)
					SharedNPCSyncManager.Instance.ServerSpawnMob(gameObject, (int)CustomSpawnID.TREASURE_GUARD, $"{treasureChestID},{guardianId}", false, transform.position, transform.rotation);
				if (spawnID == CustomSpawnID.ASTRA)
					SharedNPCSyncManager.Instance.ServerSpawnMob(gameObject, (int)spawnID, "", false, transform.position, transform.rotation);
				if (spawnID == CustomSpawnID.WAVE_EVENT || spawnID == CustomSpawnID.SPAWN_TRIGGER
					|| spawnID == CustomSpawnID.FERNALLA_WARD || spawnID == CustomSpawnID.FERNALLA_PORTAL || spawnID == CustomSpawnID.PRE_SYNCED)
					SharedNPCSyncManager.Instance.ServerSpawnMob(gameObject, (int)spawnID, $"{treasureChestID},{guardianId}", guardianId == 99, transform.position, transform.rotation);
			}
			if(type == EntityType.SIM)
			{
				SharedNPCSyncManager.Instance.sims.Add(entityID, ((SimSync)this));
				((SimSync)this).SendConnectData();
				Variables.savedZoneSimID.Add(((SimSync)this).simIndex, entityID);
				CheckSummon();
			}
		}

		public void CreateSummon(Spell spell, GameObject o)
		{
			MySummon = character.MyCharmedNPC.gameObject.GetComponent<NPCSync>();
			if (MySummon == null)
			{
				MySummon = character.MyCharmedNPC.gameObject.AddComponent<NPCSync>();
				MySummon.entityID = -1;
			}
			else
			{
				MySummon.entityID = -1;
				MySummon.Start(); //Need to ask for a new ID
			}

			MySummon.type = EntityType.PET;
			MySummon.owner = this;
			MySummon.ownerType = type;
			MySummon.spellID = spell.Id;
			MySummon.zone = zone;
		}

		public void DespawnSummon()
		{
			if (MySummon != null)
			{
				var entID = MySummon.entityID;
				Destroy(MySummon);
			}
		}

		public void HandleTargetChange(short targetID, EntityType targetType)
		{
			if(targetID == -1 && character != null && character.MyNPC != null)
			{
				character.MyNPC.CurrentAggroTarget = null;
				return;
			}
			if (character == null || character.MyNPC == null) return;

			(bool isPlayer, var target) = Extensions.GetEntityFromID(targetType==EntityType.ENEMY, targetID, targetType==EntityType.SIM);
			if (target == null)
			{
				character.MyNPC.CurrentAggroTarget = null;
				return;
			}

			if(type != EntityType.ENEMY)
			{
				if (targetType != EntityType.ENEMY)
					return;
			}

			if(type == EntityType.ENEMY)
			{
				if (targetType == EntityType.ENEMY)
					return;
			}

			try
			{

				character.MyNPC.CurrentAggroTarget = target.character;
				aggroTarget = target;
			}
			catch{}
		}

		public void SendAttack(int damage, short attackerID, bool attackerNPC, GameData.DamageType dmgType, bool animEffect, float resistMod, bool isCrit, int baseDmg)
		{
			var p = PacketManager.GetOrCreatePacket<EntityActionPacket>(entityID, PacketType.ENTITY_ACTION);
			p.AddPacketData(ActionType.ATTACK, "attackData",
				new EntityAttackData()
				{
					attackedID = attackerID,
					attackedIsNPC = attackerNPC,
					damage = damage,
					damageType = dmgType,
					effect = animEffect,
					resistMod = resistMod,
					isCrit = isCrit,
					baseDmg = baseDmg
				});
			p.SetData("targetPlayerIDs", SharedNPCSyncManager.Instance.GetPlayerSendList());
			p.entityType = type;
			p.zone = SceneManager.GetActiveScene().name;
		}


		public void SendWand(WandAttackData wa)
		{
			var pack = PacketManager.GetOrCreatePacket<PlayerActionPacket>(entityID, PacketType.PLAYER_ACTION);
			var wandData = pack.wandData ?? new();
			wandData.Add(wa);
			pack.dataTypes.Add(ActionType.WAND_ATTACK);
			pack.wandData = wandData;
			pack.isSim = type == EntityType.SIM;
		}

		public void HandleWand(WandAttackData wandAttackData)
		{
			//Get item
			Item item = GameData.ItemDB.GetItemByID(wandAttackData.itemID);
			if (item == null) return;

			var ent = Extensions.GetEntityByID(wandAttackData.targetID);
			if(ent == null) return;

			if (!wandAttackData.isBowAttack)
			{
				var go = Instantiate(GameData.Misc.WandBoltSimple, transform.position + Vector3.up + transform.forward, transform.rotation);
				var bolt = go.GetComponent<WandBolt>();
				var nbolt = go.AddComponent<SyncedWandBolt>();
				nbolt.MyAud = bolt.MyAud;
				nbolt.MyParticle = bolt.MyParticle;

				nbolt.LoadWandBolt(item.WeaponDmg, ent.character, character, item.WandBoltSpeed, GameData.DamageType.Magic, item.WandBoltColor, item.WandAttackSound);
				DestroyImmediate(bolt);
			}
			else
			{
				if(type == EntityType.PLAYER)
					((NetworkedPlayer)this).MyAnim.SetTrigger("FireBow");
				else if(type == EntityType.SIM)
					((NetworkedSim)this).MyAnim.SetTrigger("FireBow");


				var go = Instantiate(GameData.Misc.ArcheryArrows[wandAttackData.arrowIndex], transform.position + Vector3.up + transform.forward, transform.rotation);
				var bolt = go.GetComponent<WandBolt>();
				var nbolt = go.AddComponent<SyncedWandBolt>();
				nbolt.MyAud = bolt.MyAud;
				nbolt.MyParticle = bolt.MyParticle;

				var dmgMod = wandAttackData.dmgMod != 0 ? wandAttackData.dmgMod : 1;


				nbolt.LoadArrow(item.WeaponDmg * dmgMod, null, ent.character, character, item.BowArrowSpeed, GameData.DamageType.Physical, item.BowAttackSound, false, wandAttackData.interrupt);

				DestroyImmediate(bolt);
			}
		}

		public void HandleHeal(List<HealingData> healing)
		{
			foreach (var healingData in healing)
			{

				var target = Extensions.GetEntityByID(healingData.targetID);
				if (target == null) continue;
				var spell = GameData.SpellDatabase.GetSpellByID(healingData.spellID);
				if (spell == null) continue;

				if (!healingData.isMP)
				{

					if (Vector3.Distance(transform.position, GameData.PlayerControl.transform.position) <= 15 && healingData.amount > 0)
					{
						bool targetIsLocal = healingData.targetID == ClientConnectionManager.Instance.LocalPlayerID;
						UpdateSocialLog.LogAdd($"{name}'s {(healingData.isCrit ? "CRITICAL " : "")}healing spell restores {healingData.amount} of {(targetIsLocal ? "your" : target.name + "'s")} life!", "green");
					}

					target.character.MyStats.HealMe(healingData.amount);
				}
				else
				{
					target.character.MyStats.CurrentMana += healingData.amount;
					//having the bar extend for a frame kinda sucks so we handle it ourselves
					if (target.character.MyStats.CurrentMana > target.character.MyStats.GetCurrentMaxMana())
						target.character.MyStats.CurrentMana = target.character.MyStats.GetCurrentMaxMana();
				}

				if ((target.type == EntityType.SIM || target.type == EntityType.ENEMY || target is NetworkedPlayer) && Vector3.Distance(transform.position, target.transform.position) < 10f && healingData.amount > 0)
				{
					UpdateSocialLog.LogAdd(target.name + " " + spell.StatusEffectMessageOnNPC, "lightblue");
				}
				else if (target is PlayerSync && healingData.amount > 0)
				{
					UpdateSocialLog.LogAdd("You " + spell.StatusEffectMessageOnPlayer, "lightblue");
				}
			}
		}


		public void HandleSpellCharge(int SpellChargeFXIndex)
		{
			if (zone != SceneManager.GetActiveScene().name) return;

			if (spellEffect != null)
				DestroyImmediate(spellEffect);

			spellEffect = new GameObject();
			spellEffect.transform.position = transform.position + transform.forward + Vector3.up * 1.5f;
			spellEffect.transform.SetParent(transform);

			var ChargeFX = Instantiate(GameData.EffectDB.SpellEffects[SpellChargeFXIndex], spellEffect.transform.position, spellEffect.transform.rotation);
			ChargeFX.transform.SetParent(spellEffect.transform);
			//spellEffect.AddComponent<DestroyObjectTimer>().TimeToDestroy = 600f;
		}

		public void HandleSpellEffect(string spellID, short targetID, bool targetIsNPC, bool isSim)
		{
			if (spellEffect == null) return;

			Spell spell = GameData.SpellDatabase.GetSpellByID(spellID);

			if (Vector3.Distance(spellEffect.transform.position, GameData.PlayerControl.transform.position) < 30f && spell.ShakeDur > 0f)
			{
				GameData.CamControl.ShakeScreen(spell.ShakeAmp, spell.ShakeDur);
			}

			Entity targ = null;
			if (!targetIsNPC)
			{
				targ = ClientConnectionManager.Instance.GetPlayerFromID(targetID);
			}
			else
			{
				if (targ == null)
					targ = SharedNPCSyncManager.Instance.GetEntityFromID(targetID, isSim);
				if (targ == null)
					targ = ClientNPCSyncManager.Instance.GetEntityFromID(targetID, isSim);
			}

			if (targ == null) return;

			//Should do for everything
			if (targ.character.isNPC && ClientZoneOwnership.isZoneOwner)
			{
				targ.character.MyNPC.ManageAggro(spell.Aggro, character);
			}

			switch (spell.Type)
			{
				case Spell.SpellType.Damage:
				case Spell.SpellType.StatusEffect:
				case Spell.SpellType.Beneficial:
				case Spell.SpellType.PBAE:
				case Spell.SpellType.Heal:
					Instantiate(GameData.EffectDB.SpellEffects[spell.SpellResolveFXIndex], targ.transform.position, Quaternion.identity);
					//.AddComponent<DestroyObjectTimer>().TimeToDestroy = 600f;
					break;
				case Spell.SpellType.Pet:
					Instantiate(GameData.EffectDB.SpellEffects[spell.SpellResolveFXIndex], targ.transform.position, Quaternion.identity);
					//.AddComponent<DestroyObjectTimer>().TimeToDestroy = 600f;
					UpdateSocialLog.LogAdd($"{entityName} summoned a companion!", "lightblue");
					break;
			}
		}

		public void HandleEndSpell()
		{
			if (spellEffect != null)
			{
				//this.ChargeFX.GetComponent<ParticleSystem>().Stop();
				Destroy(spellEffect);
			}
		}

		public void HandleStatusEffectRefresh(List<StatusEffectData> wornEffects)
		{
			foreach (StatusEffectData effect in wornEffects)
			{
				Spell spell = GameData.SpellDatabase.GetSpellByID(effect.spellID);
				if (spell == null) return;

				if (!character.MyStats.CheckForStatus(spell) && !character.MyStats.CheckForHigherLevelSE(spell))
				{
					character.MyStats.AddStatusEffect(spell, false, 0, character);
				}
				else if (character.MyStats.CheckForStatus(spell))
				{
					character.MyStats.RefreshWornSE(spell);
				}
			}
		}

		public void HandleActiveStatusEffects(List<StatusEffectData> activeEffects)
		{
			foreach (StatusEffectData effect in activeEffects)
			{
				Spell spell = GameData.SpellDatabase.GetSpellByID(effect.spellID);
				if (spell == null) continue;

				character.MyStats.AddStatusEffect(spell, false, 0, character, effect.duration);
			}
		}

		private void SendActiveEffects()
		{
			var p = PacketManager.GetOrCreatePacket<PlayerActionPacket>(entityID, PacketType.PLAYER_ACTION);
			p.dataTypes.Add(ActionType.ACTIVE_STATUS_EFFECTS);
			var activeEffects = p.activeEffects ?? new();
			p.isSim = type == EntityType.SIM;

			var s = character.MyStats;

			for (int i = 0; i <= s.StatusEffects.Length - 1; i++)
			{
				var eff = s.StatusEffects[i];
				if (eff != null && eff.Effect != null)
				{
					activeEffects.Add(new() { spellID = eff.Effect.Id, duration = eff.Duration });
				}
			}
			p.activeEffects = activeEffects;
		}

		public IEnumerator DelayedSendEffects()
		{
			yield return new WaitForSeconds(2f);
			SendActiveEffects();
			//Also send stats
			OnStatChange();
		}

		public void OnStatChange()
		{
			if (type != EntityType.PLAYER && type != EntityType.SIM) return;
			var p = PacketManager.GetOrCreatePacket<PlayerDataPacket>(entityID, PacketType.PLAYER_DATA);
			StatData sd = new()
			{
				agi = character.MyStats.AgiScaleSpent,
				str = character.MyStats.StrScaleSpent,
				end = character.MyStats.EndScaleSpent,
				_int = character.MyStats.IntScaleSpent,
				dex = character.MyStats.DexScaleSpent,
				cha = character.MyStats.ChaScaleSpent,
				wis = character.MyStats.WisScaleSpent
			};
			p.AddPacketData(PlayerDataType.STATS, "stats", sd);
			p.isSim = type == EntityType.SIM;
		}

		public void HandleStatChange(StatData sd)
		{
			character.MyStats.AgiScaleSpent = sd.agi;
			character.MyStats.StrScaleSpent = sd.str;
			character.MyStats.EndScaleSpent = sd.end;
			character.MyStats.IntScaleSpent = sd._int;
			character.MyStats.DexScaleSpent = sd.dex;
			character.MyStats.ChaScaleSpent = sd.cha;
			character.MyStats.WisScaleSpent = sd.wis;
		}

		public void HandleRename(string newName)
		{
			var npc = character.MyNPC;
			npc.NPCName = newName;
			npc.transform.name = newName;
			character.MyStats.MyName = newName;
			npc.NamePlate.GetComponent<TextMeshPro>().text = newName;
			GameData.SimPlayerGrouping.UpdateGroupNamesAfterRename();
			entityName = newName;
		}

		public void SendRename(string newName)
		{
			if (type != EntityType.SIM) return;
			var p = PacketManager.GetOrCreatePacket<PlayerDataPacket>(entityID, PacketType.PLAYER_DATA);
			p.AddPacketData(PlayerDataType.RENAME, "rename", newName);
			p.isSim = true;
		}
	}

	public enum EntityType
	{
		ENEMY,
		SIM,
		PET,
		PLAYER,
		LOCAL_PLAYER,
	}

}
