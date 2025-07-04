﻿using System.Collections;
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

namespace ErenshorCoop
{
	public class NetworkedNPC : Entity
	{
		//public short entityID = 0;
		public Vector3 pos;
		public Quaternion rot;

		public NPC npc;
		public Animator MyAnim;
		//public Character character;
		public SimPlayer sim;
		public AnimatorOverrideController AnimOverride;

		private GameObject spellEffect;


		//private string currentScene = "";

		public int associatedSpawner = -1;

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
			//if(npc.navDo != null)
			//	StopCoroutine(npc.navDo);
			//if(npc.behDo != null)
			//	StopCoroutine(npc.behDo);
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
			//Logging.LogError($"{name} Destroyed.");
			//if (npc.navDo != null)
			//	StartCoroutine(npc.navDo);
			//if (npc.behDo != null)
			//	StartCoroutine(npc.behDo);
			//	GameHooks.leashing.SetValue(npc, false);
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
			(bool isPlayer, var attacked) = Extensions.GetCharacterFromID(data.attackedIsNPC, data.attackedID, false);

			//Logging.Log($"{name} attacked {attacked.name} {data.damage}");

			if (attacked == null)
			{
				//Logging.Log($"But something was null? {attacked == null} {data.attackedIsNPC} {data.attackedID}");
				return;
			}


			if (Grouping.HasGroup)
			{
				//Set fromPlayer to if this player is in our group and we're the leader
				//fromPlayer = Grouping.IsLocalLeader() && Grouping.IsPlayerInGroup(entityID, false);
				//FIXME: attacked could be a sim
				if (isPlayer && Grouping.IsPlayerInGroup(data.attackedID, false)) //If the target isn't a player and the player is in our group
				{
					//We add ourselves to the aggro list, this way everyone in the group is automatically in the aggro list
					//if (data.damage > 0) //Make sure we actually did damage
					{
						//Logging.Log($"beep boop aggro {attacked.name} vs {name} to us");
						npc.ManageAggro(1, ClientConnectionManager.Instance.LocalPlayer.character);
						if (ServerConnectionManager.Instance.IsRunning)
						{
							if (GameData.GroupMember1 != null && GameData.GroupMember1.simIndex >= 0)
								npc.ManageAggro(1, GameData.GroupMember1.MyStats.Myself);
							if (GameData.GroupMember2 != null && GameData.GroupMember2.simIndex >= 0)
								npc.ManageAggro(1, GameData.GroupMember2.MyStats.Myself);
							if (GameData.GroupMember3 != null && GameData.GroupMember3.simIndex >= 0)
								npc.ManageAggro(1, GameData.GroupMember3.MyStats.Myself);
						}
						//GameData.GroupMatesInCombat.Add(npc);
					}
				}
			}

			//Add attacked character to a list and remove it again after this call
			//This way we can skip the mitigation calculation

			Variables.DontCalculateDamageMitigationCharacters.Add(attacked);
			int ret = 999;

			if (isPlayer)
				attacked.MyFaction = Character.Faction.PC;

			if (data.damageType == GameData.DamageType.Physical)
				ret = attacked.DamageMe(data.damage, isPlayer, data.damageType, character, data.effect);
			else
				ret = attacked.MagicDamageMe(data.damage, isPlayer, data.damageType, character, data.resistMod);

			if (isPlayer)
				attacked.MyFaction = attacked.BaseFaction;

			//Logging.Log($"{ret}");

			Variables.DontCalculateDamageMitigationCharacters.Remove(attacked);
		}

		public void HandleSpellCharge(int SpellChargeFXIndex)
		{
			//if (currentScene != SceneManager.GetActiveScene().name) return;
			if (transform == null) return;

			spellEffect = new GameObject();
			spellEffect.transform.position = transform.position + transform.forward + Vector3.up * 1.5f;
			spellEffect.transform.SetParent(transform);

			var ChargeFX = Instantiate(GameData.EffectDB.SpellEffects[SpellChargeFXIndex], spellEffect.transform.position, spellEffect.transform.rotation);
			ChargeFX.transform.SetParent(spellEffect.transform);
		}

		public void HandleSpellEffect(string spellID, short targetID, bool targetIsNPC, bool isSim)
		{
			if (spellEffect == null) return;

			Spell spell = GameData.SpellDatabase.GetSpellByID(spellID);

			if (Vector3.Distance(spellEffect.transform.position, GameData.PlayerControl.transform.position) < 30f && spell.ShakeDur > 0f)
			{
				GameData.CamControl.ShakeScreen(spell.ShakeAmp, spell.ShakeDur);
			}

			GameObject targ = null;
			if (!targetIsNPC)
			{
				targ = ClientConnectionManager.Instance.GetPlayerFromID(targetID).gameObject;
			}
			else
			{
				Entity f = ClientNPCSyncManager.Instance.GetEntityFromID(targetID, isSim);
				
				if(f == null)
					f = SharedNPCSyncManager.Instance.GetEntityFromID(targetID, isSim);

				if (f != null)
					targ = f.gameObject;
			}

			if (targ == null)
			{
				Instantiate(GameData.EffectDB.SpellEffects[spell.SpellResolveFXIndex], transform.position, Quaternion.identity);
				return;
			}

			switch (spell.Type)
			{
				case Spell.SpellType.Damage:
				case Spell.SpellType.StatusEffect:
				case Spell.SpellType.Beneficial:
				case Spell.SpellType.PBAE:
				case Spell.SpellType.Heal:
					Instantiate(GameData.EffectDB.SpellEffects[spell.SpellResolveFXIndex], targ.transform.position, Quaternion.identity);
					break;
				case Spell.SpellType.Pet:
					//Vector3? vector = null;
					//if (NavMesh.SamplePosition(transform.position, out var navMeshHit, 5f, -1))
					//{
					//	vector = navMeshHit.position;
					//}
					//vector ??= transform.position;
					//pet = Instantiate(spell.PetToSummon, vector.Value, transform.rotation);
					//pet.AddComponent<NetworkedNPC>();
					Instantiate(GameData.EffectDB.SpellEffects[spell.SpellResolveFXIndex], targ.transform.position, Quaternion.identity);
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

		public void HandleHeal(List<HealingData> healing)
		{
			foreach (var healingData in healing)
			{

				(bool isPlayer, var target) = Extensions.GetCharacterFromID(healingData.targetIsNPC, healingData.targetID, healingData.targetIsSim);
				if (target == null) continue;

				if (!healingData.isMP)
				{
					//Note: even if each player sends their updated health, this might actually be faster
					if (type != EntityType.ENEMY) //!We don't want mobs to spam the chat with their healing
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
				//Check if we are in a group, and its a sim in our group
				if (Grouping.HasGroup && Grouping.IsPlayerInGroup(entityID, type == EntityType.SIM))
				{
					if (target.type == EntityType.ENEMY) //If the target is an enemy
					{
						//We add ourselves to the aggro list, this way everyone in the group is automatically in the aggro list
						targetChar.MyNPC.ManageAggro(1, ClientConnectionManager.Instance.LocalPlayer.character);
					}
				}

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
