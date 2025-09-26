using System.Collections;
using System.Collections.Generic;
using LiteNetLib;
using UnityEngine;
using ErenshorCoop.Shared;
using ErenshorCoop.Client;
using ErenshorCoop.Server;
using ErenshorCoop.Shared.Packets;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using System;
using TMPro;
using System.Runtime.Remoting.Messaging;
using ErenshorCoop.Client.Grouping;

namespace ErenshorCoop
{
	public class NetworkedNPC : Entity
	{
		public Vector3 pos;
		public Quaternion rot;

		public NPC npc;
		public Animator MyAnim;
		public SimPlayer sim;
		public AnimatorOverrideController AnimOverride;

		public EntitySpawnData associatedSpawner;

		//Saving stats here
		public EntitySpawnData spData;

		public void Awake()
		{
			npc = GetComponent<NPC>();
			MyAnim = GetComponent<Animator>();
			character = GetComponent<Character>();
			sim = GetComponent<SimPlayer>();

			AnimOverride = new AnimatorOverrideController(MyAnim.runtimeAnimatorController);
			MyAnim.runtimeAnimatorController = AnimOverride;

			Extensions.BuildClipLookup(npc);

			//Forces behaviour update to stop
			GameHooks.leashing.SetValue(npc, 100f);
		}


		public void SetPosition(Vector3 pos)
		{
			this.pos = pos;
		}

		public void SetRotation(Quaternion rot)
		{
			this.rot = rot;
		}

		private void OnDestroy()
		{
			HandleEndSpell();
			GameHooks.leashing.SetValue(npc, 0f);
			ClientNPCSyncManager.Instance.OnClientMobDestroyed(entityID);
		}

		public void Update()
		{
			transform.position = pos;
			//Vector3 velocity = (pos - transform.position) / Time.deltaTime;
			//transform.position = Vector3.MoveTowards(transform.position, pos, velocity.magnitude * Time.deltaTime);
			
			if (rot != transform.rotation)
				transform.rotation = rot;

			//Not strictly necessary but can be used for the future, if there will ever be stat changes during fights
			if (isGuardian && (
				character.MyStats.Level != spData.level ||
				character.MyStats.BaseAC != spData.baseAC ||
				character.MyStats.BaseER != spData.baseER ||
				character.MyStats.BaseMR != spData.baseMR ||
				character.MyStats.BasePR != spData.basePR ||
				character.MyStats.BaseVR != spData.baseVR ||
				character.MyStats.BaseMHAtkDelay != spData.mhatkDelay ||
				npc.BaseAtkDmg != spData.baseDMG))
			{
				character.MyStats.Level = spData.level;
				character.MyStats.BaseAC = spData.baseAC;
				character.MyStats.BaseER = spData.baseER;
				character.MyStats.BaseMR = spData.baseMR;
				character.MyStats.BasePR = spData.basePR;
				character.MyStats.BaseVR = spData.baseVR;
				character.MyStats.BaseMHAtkDelay = spData.mhatkDelay;
				npc.BaseAtkDmg = spData.baseDMG;
				if(character.MyStats.CurrentMaxHP != spData.baseHP)
				{

					if (character.MyStats.CurrentHP == character.MyStats.CurrentMaxHP)
						character.MyStats.CurrentHP = spData.baseHP;
					character.MyStats.CurrentMaxHP = spData.baseHP;
				}
			}
		}

		public void UpdateAnimState(AnimationData data)
		{
			if (MyAnim == null) return;

			var syncType = data.syncType;
			string param = data.param;
			object value = data.value;

			switch (syncType)
			{
				case AnimatorSyncType.FLOAT:
					MyAnim.SetFloat(param, (float)value);
					break;
				case AnimatorSyncType.INT:
					MyAnim.SetInteger(param, (int)value);
					break;
				case AnimatorSyncType.BOOL:
					MyAnim.SetBool(param, (bool)value);
					break;
				case AnimatorSyncType.TRIG:
					MyAnim.SetTrigger(param);
					if(type == EntityType.SIM && param == "Revive")
						HandleRespawn();
					break;
				case AnimatorSyncType.RSTTRIG:
					MyAnim.ResetTrigger(param);
					break;
				case AnimatorSyncType.OVERRIDE:
					if (MyAnim.runtimeAnimatorController != AnimOverride) //I guess this still gets overwritten somewhere
					{
						AnimOverride = new AnimatorOverrideController(MyAnim.runtimeAnimatorController);
						MyAnim.runtimeAnimatorController = AnimOverride;
					}
					//Logging.Log($"Recv: {param} = '{(string)value}'");
					AnimOverride[param] = Extensions.clipLookup[(string)value];
					break;
			}
		}

		public void HandleAttack(EntityAttackData data)
		{
			var ent = Extensions.GetEntityByID(data.attackedID);

			if (ent == null)
			{
				if (string.IsNullOrEmpty(entityName))
					entityName = name;
				Logging.Log($"[Entity Attack ({entityName})] But something was null? {ent == null} {data.attackedIsNPC} {data.attackedID}");
				return;
			}
			var attacked = ent.character;
			if (ent.character == null) return;
			if (character == null) return;
			//Logging.Log($"{name} attacked {attacked.name} {data.damage}");

			if (ClientGroup.HasGroup)
			{
				//Set fromPlayer to if this player is in our group and we're the leader
				if ((ent is NetworkedPlayer || ent is PlayerSync || ent is NetworkedSim || ent is SimSync) && ClientGroup.IsPlayerInGroup(data.attackedID, ent.type == EntityType.SIM)) //If the target isn't a player and the player is in our group
				{
					//Logging.Log($"beep boop aggro {attacked.name} vs {name} to us");
					npc.ManageAggro(1, ClientConnectionManager.Instance.LocalPlayer.character);
					//if (ServerConnectionManager.Instance.IsRunning)
					{
						foreach(var mem in GameData.GroupMembers)
						{
							if (mem != null && mem.simIndex >= 0)
								npc.ManageAggro(1, mem.MyStats.Myself);
						}

					}
				}
			}

			//Add attacked character to a list and remove it again after this call
			//This way we can skip the mitigation calculation

			Variables.DontCalculateDamageMitigationCharacters.Add(attacked);
			int ret = 999;

			if (ent is NetworkedPlayer || ent is PlayerSync)
				attacked.MyFaction = Character.Faction.PC;

			if (data.damageType == GameData.DamageType.Physical)
				ret = attacked.DamageMe(data.damage, (ent is NetworkedPlayer || ent is PlayerSync), data.damageType, character, data.effect, data.isCrit);
			else
				ret = attacked.MagicDamageMe(data.damage, (ent is NetworkedPlayer || ent is PlayerSync), data.damageType, character, data.resistMod, data.baseDmg);

			if (ent is NetworkedPlayer || ent is PlayerSync)
				attacked.MyFaction = attacked.BaseFaction;

			Variables.DontCalculateDamageMitigationCharacters.Remove(attacked);
		}

		public void HandleStatusEffectApply(StatusEffectData effectData)
		{
			Spell spell = GameData.SpellDatabase.GetSpellByID(effectData.spellID);
			if (spell == null) return;

			Entity target = null;
			if (effectData.targetID < 0)
				target = this;
			else
			{
				Entity t = null;
				if (ClientZoneOwnership.isZoneOwner)
				{
					t = SharedNPCSyncManager.Instance.GetEntityFromID(effectData.targetID, effectData.targetType == EntityType.SIM);
				}
				else
				{
					t = ClientNPCSyncManager.Instance.GetEntityFromID(effectData.targetID, effectData.targetType == EntityType.SIM);
				}

				if (t == null) return;
				target = t;
			}

			if (target == null) return;

			Variables.DontCheckEffectCharacters.Add(target);
			var targetChar = target.character;

			if (effectData.targetID >= 0)
			{
				if (effectData.duration >= 0)
					targetChar.MyStats.AddStatusEffect(spell, true, effectData.damageBonus, character, effectData.duration);
				else if (effectData.duration <= 0)
					targetChar.MyStats.AddStatusEffect(spell, true, effectData.damageBonus, character);
			}

			Variables.DontCheckEffectCharacters.Remove(target);
		}

		public void HandleStatusRemoval(bool RemoveAllStatus, bool RemoveBreak, int spellID)
		{
			if (RemoveAllStatus)
			{
				character.MyStats.RemoveAllStatusEffects();
			}
			if (RemoveBreak)
			{
				character.MyStats.RemoveBreakableEffects();
			}
			if (!RemoveAllStatus && !RemoveBreak)
			{
				character.MyStats.RemoveStatusEffect(spellID);
			}
		}

		private void HandleRespawn()
		{
			for (int i = 0; i <= 9; i++)
			{
				if (character.MyStats.StatusEffects[i].Effect != null)
				{
					character.MyStats.RemoveStatusEffect(i);
				}
			}

			character.MyStats.CurrentHP = 10;
			character.Alive = true;

			MyAnim.SetBool("Dead", false);
			MyAnim.SetTrigger("Revive");


			TextMeshPro component = npc.NamePlate.GetComponent<TextMeshPro>();
			component.text = component.text.Replace("'s corpse", "");
		}

	}
}
