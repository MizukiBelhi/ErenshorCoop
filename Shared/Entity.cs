using ErenshorCoop.Client;
using ErenshorCoop.Server;
using ErenshorCoop.Shared.Packets;
using LiteNetLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using Steamworks;
using System.Collections.Generic;
using System.Collections;

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
				if (type == EntityType.PET)
					SharedNPCSyncManager.Instance.ServerSpawnPet(gameObject, owner.entityID, entityID, spellID);
				else
				{
					if(isGuardian)
						SharedNPCSyncManager.Instance.ServerSpawnMob(gameObject, (int)CustomSpawnID.TREASURE_GUARD, $"{treasureChestID},{guardianId}", false, transform.position, transform.rotation);
				}
				if (type == EntityType.SIM)
				{
					SharedNPCSyncManager.Instance.sims.Add(entityID, ((SimSync)this));
					((SimSync)this).SendConnectData();
					Variables.savedZoneSimID.Add(((SimSync)this).simIndex, entityID);
				}
			}
		}

		public IEnumerator DelayedSendConnect()
		{
			yield return new WaitForSeconds(3f);
			((SimSync)this).SendConnectData();
		}

		public void ReceiveRequestID(short id)
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
			}
			if(type == EntityType.SIM)
			{
				SharedNPCSyncManager.Instance.sims.Add(entityID, ((SimSync)this));
				((SimSync)this).SendConnectData();
				Variables.savedZoneSimID.Add(((SimSync)this).simIndex, entityID);
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

		public void SendAttack(int damage, short attackerID, bool attackerNPC, GameData.DamageType dmgType, bool animEffect, float resistMod, bool isCrit)
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
					isCrit = isCrit
				});
			p.SetData("targetPlayerIDs", SharedNPCSyncManager.Instance.GetPlayerSendList());
			p.entityType = type;
			p.zone = SceneManager.GetActiveScene().name;
		}

		public void HandleWand(WandAttackData wandAttackData)
		{
			//Get item
			Item item = GameData.ItemDB.GetItemByID(wandAttackData.itemID);
			if (item == null) return;

			var ent = Extensions.GetEntityByID(wandAttackData.targetID);
			if(ent == null) return;

			var go = Instantiate(GameData.Misc.WandBoltSimple, transform.position + Vector3.up + transform.forward, transform.rotation);
			var bolt = go.GetComponent<WandBolt>();
			var nbolt = go.AddComponent<SyncedWandBolt>();
			nbolt.MyAud = bolt.MyAud;
			nbolt.MyParticle = bolt.MyParticle;

			nbolt.LoadWandBolt(item.WeaponDmg, ent.character, character, item.WandBoltSpeed, GameData.DamageType.Magic, item.WandBoltColor, item.WandAttackSound);
			DestroyImmediate(bolt);
		}

		public void HandleHeal(List<HealingData> healing)
		{
			foreach (var healingData in healing)
			{

				(bool isPlayer, var target) = Extensions.GetCharacterFromID(healingData.targetIsNPC, healingData.targetID, healingData.targetIsSim);
				if (target == null) continue;

				if (!healingData.isMP)
				{

					if (Vector3.Distance(transform.position, GameData.PlayerControl.transform.position) <= 15)
					{
						bool targetIsLocal = healingData.targetID == ClientConnectionManager.Instance.LocalPlayerID;
						UpdateSocialLog.LogAdd($"{name}'s {(healingData.isCrit ? "CRITICAL " : "")}healing spell restores {healingData.amount} of {(targetIsLocal ? "your" : target.name + "'s")} life!", "green");
					}

					target.MyStats.HealMe(healingData.amount);
				}
				else
				{
					target.MyStats.CurrentMana += healingData.amount;
					//having the bar extend for a frame kinda sucks so we handle it ourselves
					if (target.MyStats.CurrentMana > target.MyStats.GetCurrentMaxMana())
						target.MyStats.CurrentMana = target.MyStats.GetCurrentMaxMana();
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

			//Logging.Log($"{targetIsNPC}");

			switch (spell.Type)
			{
				case Spell.SpellType.Damage:

					if (targ.character.isNPC && ClientZoneOwnership.isZoneOwner)
					{
						targ.character.MyNPC.ManageAggro(spell.Aggro, character);
					}
					Instantiate(GameData.EffectDB.SpellEffects[spell.SpellResolveFXIndex], targ.transform.position, Quaternion.identity);

					break;
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

	}

	public enum EntityType
	{
		ENEMY,
		SIM,
		PET,
		PLAYER,
		LOCAL_PLAYER,
	}


	public class SyncedWandBolt : MonoBehaviour
	{
		private void Start()
		{
			MyAud.volume = GameData.SFXVol * MyAud.volume;
		}

		private void Update()
		{
			if (TargetChar == null)
			{
				Destroy(gameObject);
				return;
			}
			if (moveDel <= 0f)
			{
				Vector3 normalized = (TargetChar.transform.position + Vector3.up - transform.position).normalized;
				if (Vector3.Distance(transform.position, TargetChar.transform.position + Vector3.up) > 2f)
				{
					transform.position += normalized * MoveSpeed * Time.deltaTime;
				}
				else
				{
					DeliverDamage();
				}
				MoveSpeed += 10f * Time.deltaTime;
				return;
			}
			moveDel -= 60f * Time.deltaTime;
			if (moveDel < 15f && !didSFX)
			{
				if (AtkSound != null && SourceChar != null)
				{
					SourceChar.MyAudio.PlayOneShot(AtkSound, SourceChar.MyAudio.volume * GameData.SFXVol);
				}
				didSFX = true;
			}
			if (moveDel <= 0f)
			{
				transform.position = SourceChar.transform.position + transform.forward + Vector3.up;
				if (Vector3.Distance(transform.position, TargetChar.transform.position + Vector3.up) > 5f)
				{
					MyAud.Play();
				}
			}
		}
		private void DeliverDamage()
		{
			Destroy(gameObject);
		}
		public void LoadWandBolt(int _dmg, Character _tar, Character _caster, float _speed, GameData.DamageType _dmgType, Color _boltCol, AudioClip _atkSound)
		{
			AtkSound = _atkSound;
			TargetChar = _tar;
			SourceChar = _caster;
			MoveSpeed = _speed;
			DmgType = _dmgType;
			var main = MyParticle.main;
			main.startColor = _boltCol;
		}


		public Character SourceChar;
		public Character TargetChar;
		public float MoveSpeed;
		public GameData.DamageType DmgType = GameData.DamageType.Magic;
		public ParticleSystem MyParticle;
		public AudioSource MyAud;
		private AudioClip AtkSound;
		private float moveDel = 40f;
		private bool didSFX;
	}

}
