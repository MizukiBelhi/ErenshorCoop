using LiteNetLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using ErenshorCoop.Client;
using ErenshorCoop.Server;
using UnityEngine;
using ErenshorCoop.Shared;
using ErenshorCoop.Shared.Packets;
using TMPro;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace ErenshorCoop
{
	//Other peers
	public class NetworkedPlayer : Entity
	{
		public bool sceneChanged = false;
		public string playerName = "";

		private bool requiresSimUpdate = false;
		private bool isNextFrame = false; //Try delaying it by another frame? only needed on the first received packet
		private bool isFirstCreated = false;

		public short playerID;

		public Quaternion rot;
		public Vector3 pos;

		public NPC npc;
		private Animator MyAnim;
		private Inventory inventory;
		public SimPlayer sim;
		public ModularParts mod;
		public ModularPar modPar;
		public AnimatorOverrideController AnimOverride;


		SimInvSlot MH = new(Item.SlotType.Primary) { Quant = 1 };
		SimInvSlot OH = new(Item.SlotType.Secondary) { Quant = 1 };


		//Interpolation
		private bool firstInterpUpdate;
		private const float interpDuration = 0.1f;
		private Coroutine interpRoutine;

		private GameObject spellEffect;

		public void Init(Vector3 position, Quaternion rotation, string pName, string scene, short playerID, NetPeer peer)
		{
			this.peer = peer;
			rot = rotation;
			playerName = pName;
			currentScene = scene;
			sceneChanged = true;
			this.playerID = playerID;

			transform.position = position;
			transform.rotation = rotation;

			npc = GetComponent<NPC>();
			npc.NPCName = playerName;
			npc.GuildName = "";
			npc.SimPlayer = false;
			name = playerName;
			entityName = playerName;
			entityID = playerID;

			MyAnim = GetComponent<Animator>();
			inventory = GetComponent<Inventory>();
			character = GetComponent<Character>();
			modPar = GetComponentInChildren<ModularPar>();
			//Create new one
			AnimOverride = new AnimatorOverrideController(MyAnim.runtimeAnimatorController);
			MyAnim.runtimeAnimatorController = AnimOverride;

			character.ShoutOnDeath.Clear(); //Clear the on-death messages
			character.DestroyOnDeath = false; //we don't want this
			character.ShrinkColliderOnDeath = false;


			Extensions.BuildClipLookup(npc);

			sim.AllHeldItems.Clear();
			sim.MyStats.MyAura = GameData.PlayerInv.Empty.Aura;
			sim.MyStats.OverrideHPforNPC = false;
			DontDestroyOnLoad(gameObject);

			if(ServerConnectionManager.Instance.IsRunning)
				character.MyFaction = ServerConfig.IsPVPEnabled.Value?Character.Faction.PC:Character.Faction.Player;
			else
				character.MyFaction = ServerConfig.clientIsPvpEnabled ? Character.Faction.PC : Character.Faction.Player;
			character.isNPC = false;

			Logging.LogGameMessage($"{name} Connected.");

			//Forces behaviour update stop
			//character.Alive = false;
			//GameHooks.leashing.SetValue(npc, true);
			GameHooks.leashing.SetValue(npc, 100f);
		}

		//Hack: Change the sims models to the player ones
		private void HackUpdateModels()
		{
			var playerMod = GameData.PlayerInv.GetComponentInChildren<ModularPar>();
			var playerFem = playerMod.FemaleBase;
			var playerMal = playerMod.MaleBase;
			var simFem = modPar.FemaleBase;
			var simMal = modPar.MaleBase;

			//Get all components
			SkinnedMeshRenderer[] pM = playerMal.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
			SkinnedMeshRenderer[] pF = playerFem.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);

			SkinnedMeshRenderer[] sM = simMal.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
			SkinnedMeshRenderer[] sF = simFem.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);

			string GetTransformPath(Transform t)
			{
				return t.parent.name + "/" + t.name;
			}

			foreach (SkinnedMeshRenderer pRen in pM)
			{
				string pPath = GetTransformPath(pRen.transform);

				foreach (SkinnedMeshRenderer sRen in sM)
				{
					string sPath = GetTransformPath(sRen.transform);

					if (sPath == pPath && sRen.sharedMesh != pRen.sharedMesh)
					{
						sRen.sharedMesh = pRen.sharedMesh;
					}
				}
			}

			int foundCount = 0;
			foreach (SkinnedMeshRenderer pRen in pF)
			{
				string pPath = GetTransformPath(pRen.transform);
				foreach (SkinnedMeshRenderer sRen in sF)
				{
					string sPath = GetTransformPath(sRen.transform);


					if (sPath == pPath && sRen.sharedMesh != pRen.sharedMesh)
					{
						//Debug.Log($"found {sPath} == {pPath}");
						sRen.sharedMesh = pRen.sharedMesh;
						foundCount++;
					}
				}
			}

			if (pF.Length < foundCount)
			{
				Debug.Log($"Didn't find {pF.Length - foundCount} renderers.");
			}
		}

		public void SetPosition(Vector3 pos)
		{
			//transform.position = pos;
			//return;
			this.pos = pos;
			/*if(firstInterpUpdate)
			{
				transform.position = pos;
				firstInterpUpdate = false;
				return;
			}

			if(interpRoutine != null)
				StopCoroutine(interpRoutine);

			interpRoutine = StartCoroutine(InterpPos(pos, interpDuration));*/
		}

		public void SetRotation(Quaternion rot)
		{
			this.rot = rot;
		}

		private void OnDestroy()
		{
			//GameHooks.leashing.SetValue(npc, false);
			//character.Alive = true;
			GameHooks.leashing.SetValue(npc, 0f);
			Logging.LogError($"{name} Destroyed.");
			Logging.LogGameMessage($"{name} Disconnected.");

			if (ServerConnectionManager.Instance.IsRunning)
			{
				Grouping.ForceRemoveFromGroup(entityID);
			}
		}

		private IEnumerator InterpPos(Vector3 targ, float dur)
		{
			float time = 0f;
			Vector3 start = transform.position;
			while(time < dur)
			{
				transform.position = Vector3.Lerp(start, targ, time/dur);
				time += Time.deltaTime;
				yield return null;
			}
			transform.position = targ;
		}

		public void Update()
		{
			transform.position = pos;

			if(rot != transform.rotation)
				transform.rotation = rot;
		}

		public void LateUpdate()
		{
			if (requiresSimUpdate && isNextFrame)
			{

				GameHooks.GetTransformNames.Invoke(mod, null);
				mod.UpdateSimPlayerVisuals(sim.MyEquipment, MH, OH);

				//sim.AuditInventory();
				requiresSimUpdate = false;
				isNextFrame = false;
				sim.MyStats.MyAura = sim.MyEquipment[equipSlotIndices["Aura_0"]].MyItem.Aura;
				sim.MyStats.OverrideHPforNPC = false; //Make sure this is always set false before calculating stats
				inventory.UpdateInvStats();
				sim.MyStats.CalcSimStats();
				sim.MyStats.CalcStats();

				HackUpdateModels();
				//Set health to full
				sim.MyStats.CurrentHP = sim.MyStats.CurrentMaxHP;

				
			}
			if (requiresSimUpdate) isNextFrame = true;
		}


		private void HandleRespawn()
		{
			HandleStatusRemoval(true,false,-1);

			character.MyStats.CurrentHP = character.MyStats.CurrentMaxHP;
			character.Alive = true;
			

			MyAnim.SetBool("Dead", false);
			MyAnim.SetTrigger("Revive");

			npc.NPCName = playerName;
			npc.GuildName = "";
			name = playerName;
			//Here go 2 hours
			gameObject.layer = 9;

			var component = npc.NamePlate.GetComponent<TextMeshPro>();
			component.text = component.text.Replace("'s corpse", "");
		}


		private void HandlePlayerAttack(PlayerAttackData data)
		{
			//TODO: attacked could potentially be a sim with PVP mod
			(bool isPlayer, var attacked) = Extensions.GetCharacterFromID(data.attackedIsNPC, data.attackedID, false);

			

			if (attacked == null)
			{
				Logging.Log($"But something was null? {attacked == null} {data.attackedIsNPC} {data.attackedID}");
				return;
			}

			bool fromPlayer = false; //This should be false by default, otherwise every player that receives this will be
									//Adding player damage to the enemy, which would cause everyone to get xp
			//Check if we are in a group
			if (Grouping.HasGroup)
			{
				//Set fromPlayer to if this player is in our group and we're the leader
				fromPlayer =  Grouping.IsLocalLeader() && Grouping.IsPlayerInGroup(entityID, false);
				if (!isPlayer) //If the target isn't a player
				{
					//We add ourselves to the aggro list, this way everyone in the group is automatically in the aggro list
					attacked.MyNPC.ManageAggro(1, ClientConnectionManager.Instance.LocalPlayer.character);
				}
			}

			//Add attacked character to a list and remove it again after this call
			//This way we can skip the mitigation calculation
			
			Variables.DontCalculateDamageMitigationCharacters.Add(attacked);

			if (data.damageType == GameData.DamageType.Physical)
				attacked.DamageMe(data.damage, fromPlayer, data.damageType, character, data.effect);
			else
				attacked.MagicDamageMe(data.damage, fromPlayer, data.damageType, character, data.resistMod);

			Variables.DontCalculateDamageMitigationCharacters.Remove(attacked);
		}


		private void HandleDamageTaken(DamageTakenData data)
		{
			//TODO: attacker could potentially be a sim with PVP mod
			(bool isPlayer, var attacker) = Extensions.GetCharacterFromID(data.attackerIsNPC, data.attackerID, false);

			//Logging.Log($"{name} damage taken");

			if (attacker == null)
			{
				Logging.Log($"But something was null? {attacker == null} {data.attackerIsNPC} {data.attackerID}");
				return;
			}

			//Add attacked character to a list and remove it again after this call
			//This way we can skip the mitigation calculation

			Variables.DontCalculateDamageMitigationCharacters.Add(character);

			if (data.damageType == GameData.DamageType.Physical)
				character.DamageMe(data.damage, false, data.damageType, attacker, data.effect);
			else
				character.MagicDamageMe(data.damage, false, data.damageType, attacker, data.resistMod);

			Variables.DontCalculateDamageMitigationCharacters.Remove(character);
		}



		public void HandleConnectPacket(PlayerConnectionPacket packet)
		{
			sim.MyStats.Level = packet.level;
			sim.MyStats.CharacterClass = packet._class;
			zone = packet.scene;
			currentScene = packet.scene;
			UpdateLooks(packet.lookData, packet.gearData);

			sim.MyStats.CurrentHP = packet.health;
			sim.MyStats.CurrentMana = packet.mp;

			if (ServerConnectionManager.Instance.IsRunning)
				ServerConnectionManager.Instance.OnClientConnect?.Invoke(playerID, peer);
		}

		public void OnPlayerDataReceive<T>(T packet) where T : BasePacket
		{
			if (packet is PlayerTransformPacket playerTransformPacket)
			{
				if (playerTransformPacket.dataTypes.Contains(PlayerDataType.POSITION))
					SetPosition(playerTransformPacket.position);
				if (playerTransformPacket.dataTypes.Contains(PlayerDataType.ROTATION))
					SetRotation(playerTransformPacket.rotation);
			}
			if(packet is PlayerDataPacket playerDataPacket)
			{
				if (playerDataPacket.dataTypes.Contains(PlayerDataType.POSITION))
					SetPosition(playerDataPacket.position);
				if (playerDataPacket.dataTypes.Contains(PlayerDataType.ROTATION))
					SetRotation(playerDataPacket.rotation);
				if (playerDataPacket.dataTypes.Contains(PlayerDataType.HEALTH))
					sim.MyStats.CurrentHP = playerDataPacket.health;
				if (playerDataPacket.dataTypes.Contains(PlayerDataType.MP))
					sim.MyStats.CurrentMana = playerDataPacket.mp;
				if (playerDataPacket.dataTypes.Contains(PlayerDataType.CLASS))
					sim.MyStats.CharacterClass = playerDataPacket._class;
				if (playerDataPacket.dataTypes.Contains(PlayerDataType.LEVEL))
					sim.MyStats.Level = playerDataPacket.level;
				if (playerDataPacket.dataTypes.Contains(PlayerDataType.NAME))
				{
					playerName = playerDataPacket.name;
					name = playerName;
					npc.NPCName = playerName;
				}
				if (playerDataPacket.dataTypes.Contains(PlayerDataType.SCENE))
				{
					if (currentScene != playerDataPacket.scene)
					{
						sceneChanged = true;
						if(ServerConnectionManager.Instance.IsRunning)
							ServerConnectionManager.Instance.OnClientSwapZone?.Invoke(playerID, peer);
					}
				
					currentScene =	playerDataPacket.scene;
					zone = currentScene;
				}
				if (playerDataPacket.dataTypes.Contains(PlayerDataType.ANIM))
				{
					foreach (var an in playerDataPacket.animData)
					{
						UpdateAnimState(an);
					}
				}
				if (playerDataPacket.dataTypes.Contains(PlayerDataType.GEAR))
				{
					UpdateLooks(playerDataPacket.lookData, playerDataPacket.gearData);
				}
			}

			if (packet is PlayerActionPacket playerActionPacket)
			{
				if (playerActionPacket.dataTypes.Contains(ActionType.ATTACK))
				{
					HandlePlayerAttack(playerActionPacket.attackData);
				}
				if(playerActionPacket.dataTypes.Contains(ActionType.REVIVE))
					HandleRespawn();
				if (playerActionPacket.dataTypes.Contains(ActionType.DAMAGE_TAKEN))
				{
					HandleDamageTaken(playerActionPacket.damageTakenData);
				}
				if (playerActionPacket.dataTypes.Contains(ActionType.SPELL_CHARGE))
				{
					HandleSpellCharge(playerActionPacket.SpellChargeFXIndex);
				}
				if(playerActionPacket.dataTypes.Contains(ActionType.SPELL_EFFECT))
					HandleSpellEffect(playerActionPacket.spellID, playerActionPacket.targetID, playerActionPacket.targetIsNPC, playerActionPacket.targetIsSim);
				if(playerActionPacket.dataTypes.Contains(ActionType.SPELL_END))
					HandleEndSpell();
				if(playerActionPacket.dataTypes.Contains(ActionType.STATUS_EFFECT_APPLY))
					HandleStatusEffectApply(playerActionPacket.effectData);
				if(playerActionPacket.dataTypes.Contains(ActionType.STATUS_EFFECT_REMOVE))
					HandleStatusRemoval(playerActionPacket.RemoveAllStatus, playerActionPacket.RemoveBreakable, playerActionPacket.statusID);
				if (playerActionPacket.dataTypes.Contains(ActionType.HEAL))
					HandleHeal(playerActionPacket.healingData);
			}

		}

		

		static Dictionary<string, int> equipSlotIndices;
		private bool hasClearedEquip = false;


		public void UpdateLooks(LookData data, List<GearData> gearData)
		{
			inventory.WornEffects.Clear();

			if (!hasClearedEquip)
			{
				sim.MyEquipment.Clear();
				inventory.EquippedItems.Clear();

				sim.MyEquipment.Add(new SimInvSlot(Item.SlotType.Primary) { MyItem = GameData.PlayerInv.Empty, Quant = 1 });
				sim.MyEquipment.Add(new SimInvSlot(Item.SlotType.Secondary) { MyItem = GameData.PlayerInv.Empty, Quant = 1 });
				inventory.EquippedItems.Add(GameData.PlayerInv.Empty);
				inventory.EquippedItems.Add(GameData.PlayerInv.Empty);

				foreach (var slotType in (Item.SlotType[])Enum.GetValues(typeof(Item.SlotType)))
				{
					if (slotType == Item.SlotType.Primary || slotType == Item.SlotType.Secondary) continue;

					var simInvSlot = new SimInvSlot(slotType)
					{
						MyItem = GameData.PlayerInv.Empty,
						Quant = 1
					};
					sim.MyEquipment.Add(simInvSlot);
					inventory.EquippedItems.Add(GameData.PlayerInv.Empty);

					if (slotType == Item.SlotType.Bracer || slotType == Item.SlotType.Ring)
					{
						sim.MyEquipment.Add(new SimInvSlot(slotType) //need to add two of these
						{
							MyItem = GameData.PlayerInv.Empty,
							Quant = 1
						});
						inventory.EquippedItems.Add(GameData.PlayerInv.Empty);
					}
				}
				hasClearedEquip = true;
			}


			// Build index map if not built yet
			if (equipSlotIndices == null)
			{
				equipSlotIndices = new();
				Dictionary<Item.SlotType, int> slotCount = new();

				for (int i = 0; i < sim.MyEquipment.Count; i++)
				{
					var slotType = sim.MyEquipment[i].ThisSlotType;

					if (!slotCount.ContainsKey(slotType))
						slotCount[slotType] = 0;

					string slotKey = $"{slotType}_{slotCount[slotType]}";
					equipSlotIndices[slotKey] = i;
					slotCount[slotType]++;
				}
			}

			Dictionary<Item.SlotType, int> currentCounts = new();

			foreach (var gd in gearData)
			{
				var item = GameData.ItemDB.GetItemByID(gd.itemID);

				if (!currentCounts.ContainsKey(gd.slotType))
					currentCounts[gd.slotType] = 0;

				int index = currentCounts[gd.slotType];
				currentCounts[gd.slotType]++;

				string slotKey = $"{gd.slotType}_{index}";

				if (equipSlotIndices.TryGetValue(slotKey, out var id))
				{
					if (gd.slotType == Item.SlotType.Primary)
					{
						MH.MyItem = item;
						MH.Quant = gd.quality;
					}
					if (gd.slotType == Item.SlotType.Secondary)
					{
						OH.MyItem = item;
						OH.Quant = gd.quality;
					}

					sim.MyEquipment[id].MyItem = item;

					sim.MyEquipment[id].Quant = gd.quality;
					inventory.EquippedItems[id] = item;
					//Logging.Log($"{id} - {gd.slotType} {slotKey} {gd.quality}");
				}
			}

			ModularParts toRemoveSlots = modPar.Female.enabled ? modPar.Female : modPar.Male;

			//Logging.Log($"Removing items player {name}");
			ResetPlayerVisuals(toRemoveSlots);

			inventory.isMale = data.isMale;

			modPar.FemaleBase.SetActive(false);
			modPar.MaleBase.SetActive(false);
			modPar.Female.enabled = false;
			modPar.Male.enabled = false;

			if (!data.isMale)
			{
				modPar.FemaleBase.SetActive(true);
				mod = modPar.Female;
				modPar.Female.enabled = true;
				//Logging.Log($"Setting player {name} to female");
			}
			else
			{
				modPar.MaleBase.SetActive(true);
				mod = modPar.Male;
				modPar.Male.enabled = true;
				//Logging.Log($"Setting player {name} to male");
			}

			inventory.Modulars = mod;
			mod.HairName = data.hairName;
			mod.HairCol = data.hairColor;
			mod.SkinCol = data.skinColor;


			//Logging.Log($"Updating visuals player {name}");

			//FIXME: can be done better, we should only request an update after 2 frames if this is the first gear packet received.
			//Otherwise we see the player flash for 2 frames
			if (isFirstCreated)
			{
				GameHooks.GetTransformNames.Invoke(mod, null);
				mod.UpdateSimPlayerVisuals(sim.MyEquipment, MH, OH);
				sim.MyStats.MyAura = sim.MyEquipment[equipSlotIndices["Aura_0"]].MyItem.Aura;
				inventory.UpdateInvStats();
				sim.MyStats.CalcSimStats();
				sim.MyStats.CalcStats();
				mod.UpdateHair(data.hairName, data.hairColor);
			}
			else
			{
				requiresSimUpdate = true;
				isFirstCreated = true;
			}
		}

		private void UpdateAnimState(AnimationData data)
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
					if (param == "Revive")
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
					AnimOverride[param] = Extensions.clipLookup[(string)value];
					break;
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
					t = ClientNPCSyncManager.Instance.GetEntityFromID(effectData.targetID,effectData.targetType == EntityType.SIM);
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
				else if (effectData.duration < 0)
					targetChar.MyStats.AddStatusEffect(spell, true, effectData.damageBonus, character);

				//Check if we are in a group, and its a sim in our group
				if (Grouping.HasGroup && Grouping.IsPlayerInGroup(entityID, false))
				{
					if (target.type == EntityType.ENEMY) //If the target is an enemy
					{
						//We add ourselves to the aggro list, this way everyone in the group is automatically in the aggro list
						targetChar.MyNPC.ManageAggro(1, ClientConnectionManager.Instance.LocalPlayer.character);
					}
				}
			}
			else
			{
				if(effectData.targetID == -1)
					targetChar.MyStats.AddStatusEffect(spell, true, effectData.damageBonus);
				if(effectData.targetID == -2)
					targetChar.MyStats.AddStatusEffect(spell, true, effectData.damageBonus, character);
				if(effectData.targetID == -3)
					targetChar.MyStats.AddStatusEffect(spell, true, effectData.damageBonus, character, effectData.duration);
			}

			Variables.DontCheckEffectCharacters.Remove(target);
		}

		public void HandleStatusRemoval(bool RemoveAllStatus, bool RemoveBreak, int spellID)
		{
			if (RemoveAllStatus)
			{
				sim.MyStats.RemoveAllStatusEffects();
			}
			if (RemoveBreak)
			{
				sim.MyStats.RemoveBreakableEffects();
			}
			if (!RemoveAllStatus && !RemoveBreak)
			{
				sim.MyStats.RemoveStatusEffect(spellID);
			}
		}

		public void HandleSpellCharge(int SpellChargeFXIndex)
		{
			if (currentScene != SceneManager.GetActiveScene().name) return;

			if(spellEffect != null)
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

			GameObject targ = null;
			if (!targetIsNPC)
			{
				targ = ClientConnectionManager.Instance.GetPlayerFromID(targetID).gameObject;
			}
			else
			{
				if (!ClientZoneOwnership.isZoneOwner)
					targ = ClientNPCSyncManager.Instance.GetEntityFromID(targetID, isSim).gameObject;
				else
					targ = SharedNPCSyncManager.Instance.GetEntityFromID(targetID, isSim).gameObject;
			}

			//Logging.Log($"{targetIsNPC}");

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
					UpdateSocialLog.LogAdd($"{playerName} summoned a companion!", "lightblue");

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


		private void HandleHeal(List<HealingData> healing)
		{
			foreach (var healingData in healing)
			{
	
				( bool isPlayer, var target ) = Extensions.GetCharacterFromID(healingData.targetIsNPC, healingData.targetID, healingData.targetIsSim);
				if (target == null) continue;

				if (!healingData.isMP)
				{
					//Note: even if each player sends their updated health, this might actually be faster
					bool targetIsLocal = healingData.targetID == ClientConnectionManager.Instance.LocalPlayerID;
					UpdateSocialLog.LogAdd($"{playerName}'s {( healingData.isCrit ? "CRITICAL " : "" )}healing spell restores {healingData.amount} of {( targetIsLocal ? "your" : target.name + "'s" )} life!", "green");

					target.MyStats.HealMe(healingData.amount);
				}
				else
				{
					target.MyStats.CurrentMana += healingData.amount;
					//having the bar extend for a frame kinda sucks so we handle it ourselves
					if(target.MyStats.CurrentMana > target.MyStats.GetCurrentMaxMana())
						target.MyStats.CurrentMana = target.MyStats.GetCurrentMaxMana();
				}
			}
		}

		public void ResetPlayerVisuals(ModularParts mod)
		{
			mod.LoadSparkles();
			GameHooks.hideHead.SetValue(mod, false);
			GameHooks.hideHair.SetValue(mod, false);
			mod.UpdateSlot(mod.WeaponR, GameData.PlayerInv.Empty, 0, Item.SlotType.Primary);
			mod.UpdateSlot(mod.WeaponL, GameData.PlayerInv.Empty, 0, Item.SlotType.Secondary);
			mod.UpdateHair(sim.HairName, sim.HairColor);
			mod.UpdateSlot(mod.HeadCover, GameData.PlayerInv.Empty, 0, Item.SlotType.Head);
			mod.UpdateSlot(mod.FullHeadHelm, GameData.PlayerInv.Empty, 0, Item.SlotType.Leg);
			mod.UpdateSlot(mod.HatWithHair, GameData.PlayerInv.Empty, 0, Item.SlotType.Leg);
			mod.UpdateSlot(mod.Cloak, GameData.PlayerInv.Empty, 0, Item.SlotType.Back);
			mod.UpdateSlot(mod.Chest, GameData.PlayerInv.Empty, 0, Item.SlotType.Chest);
			mod.UpdateTrimSlot(mod.ShoulderL, GameData.PlayerInv.Empty, "");
			mod.UpdateTrimSlot(mod.ShoulderR, GameData.PlayerInv.Empty, "");
			mod.UpdateSlot(mod.ArmR, GameData.PlayerInv.Empty, 0, Item.SlotType.Arm);
			mod.UpdateSlot(mod.ArmL, GameData.PlayerInv.Empty, 0, Item.SlotType.Arm);
			mod.UpdateSlot(mod.BracerL, GameData.PlayerInv.Empty, 0, Item.SlotType.Bracer);
			mod.UpdateSlot(mod.BracerR, GameData.PlayerInv.Empty, 0, Item.SlotType.Bracer);
			mod.UpdateTrimSlot(mod.ElbowR, GameData.PlayerInv.Empty, "");
			mod.UpdateTrimSlot(mod.ElbowL, GameData.PlayerInv.Empty, "");
			mod.UpdateSlot(mod.GloveR, GameData.PlayerInv.Empty, 0, Item.SlotType.Hand);
			mod.UpdateSlot(mod.GloveL, GameData.PlayerInv.Empty, 0, Item.SlotType.Hand);
			mod.UpdateSlot(mod.Pants, GameData.PlayerInv.Empty, 0, Item.SlotType.Leg);
			mod.UpdateTrimSlot(mod.KneeR, GameData.PlayerInv.Empty, "");
			mod.UpdateTrimSlot(mod.KneeL, GameData.PlayerInv.Empty, "");
			mod.UpdateSlot(mod.BootsL, GameData.PlayerInv.Empty, 0, Item.SlotType.Foot);
			mod.UpdateSlot(mod.BootsR, GameData.PlayerInv.Empty, 0, Item.SlotType.Foot);
		}

		

	}
}
