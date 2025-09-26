using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ErenshorCoop.Client;
using LiteNetLib;
using LiteNetLib.Utils;

namespace ErenshorCoop.Shared.Packets
{
	public class PlayerActionPacket : EntityBasePacket
	{
		public HashSet<ActionType> dataTypes = new();

		public PlayerAttackData attackData;
		public DamageTakenData damageTakenData;
		public int SpellChargeFXIndex;
		public string spellID;
		public short targetID;
		public bool targetIsNPC;
		public bool targetIsSim;


		public StatusEffectData effectData;

		public bool RemoveAllStatus = false;
		public bool RemoveBreakable = false;
		public int statusID;

		public List<HealingData> healingData;
		public List<WandAttackData> wandData;
		public List<StatusEffectData> activeEffects;
		public List<StatusEffectData> wornEffects;
		public PlayerActionPacket() : base(DeliveryMethod.ReliableOrdered) { }

		public override void Write(NetDataWriter writer)
		{
			writer.Put((byte)PacketType.PLAYER_ACTION);
			writer.Put(entityID);

			writer.Put(isSim);

			ushort flag = Extensions.GetSubTypeFlag(dataTypes);
			writer.Put(flag);

			if (dataTypes.Contains(ActionType.ATTACK))
			{
				writer.Put(attackData.attackedID);
				writer.Put(attackData.attackedIsNPC);
				writer.Put(attackData.damage);
				writer.Put((byte)attackData.damageType);
				writer.Put(attackData.effect);
				writer.Put(attackData.resistMod);
				writer.Put(attackData.isCrit);
				writer.Put(attackData.baseDmg);
			}
			if (dataTypes.Contains(ActionType.DAMAGE_TAKEN))
			{
				writer.Put(damageTakenData.attackerID);
				writer.Put(damageTakenData.attackerIsNPC);
				writer.Put(damageTakenData.damage);
				writer.Put((byte)damageTakenData.damageType);
				writer.Put(damageTakenData.effect);
				writer.Put(damageTakenData.resistMod);
				writer.Put(damageTakenData.isCrit);
				writer.Put(damageTakenData.baseDmg);
			}
			if (dataTypes.Contains(ActionType.SPELL_CHARGE))
			{
				writer.Put(SpellChargeFXIndex);
			}
			if (dataTypes.Contains(ActionType.SPELL_EFFECT))
			{
				writer.Put(spellID);
				writer.Put(targetID);
				writer.Put(targetIsNPC);
				writer.Put(targetIsSim);
			}
			if (dataTypes.Contains(ActionType.STATUS_EFFECT_APPLY))
			{
				writer.Put(effectData.spellID);
				writer.Put(effectData.damageBonus);
				writer.Put(effectData.casterID);
				writer.Put((byte)effectData.casterType);
				writer.Put(effectData.duration);
				writer.Put(effectData.targetID);
				writer.Put((byte)effectData.targetType);
			}
			if (dataTypes.Contains(ActionType.STATUS_EFFECT_REMOVE))
			{
				writer.Put(statusID);
				writer.Put(RemoveAllStatus);
				writer.Put(RemoveBreakable);
			}
			if (dataTypes.Contains(ActionType.HEAL))
			{
				writer.Put(healingData.Count);
				foreach (var h in healingData)
				{
					writer.Put(h.targetID);
					writer.Put(h.spellID);
					writer.Put(h.amount);
					writer.Put(h.isCrit);
					writer.Put(h.isMP);
				}
			}
			if(dataTypes.Contains(ActionType.WAND_ATTACK))
			{
				writer.Put(wandData.Count);
				foreach(var w in wandData)
				{
					writer.Put(w.targetID);
					writer.Put(w.itemID);
					writer.Put(w.isBowAttack);
					writer.Put(w.attackType);
					writer.Put(w.arrowIndex);
					writer.Put(w.dmgMod);
					writer.Put(w.interrupt);
				}
			}
			if (dataTypes.Contains(ActionType.WORN_EFFECT_REFRESH))
			{
				writer.Put(wornEffects.Count);
				foreach (var h in wornEffects)
				{
					writer.Put(h.spellID);
				}
			}
			if(dataTypes.Contains(ActionType.ACTIVE_STATUS_EFFECTS))
			{
				writer.Put(activeEffects.Count);
				foreach (var h in activeEffects)
				{
					writer.Put(h.spellID);
					writer.Put(h.duration);
				}
			}
		}

		public override void Read(NetDataReader reader)
		{
			entityID = reader.GetShort();
			isSim = reader.GetBool();
			dataTypes = Extensions.ReadSubTypeFlag<ActionType>(reader.GetUShort());

			if (dataTypes.Contains(ActionType.ATTACK))
			{
				attackData = new()
				{
					attackedID = reader.GetShort(),
					attackedIsNPC = reader.GetBool(),
					damage = reader.GetInt(),
					damageType = (GameData.DamageType)reader.GetByte(),
					effect = reader.GetBool(),
					resistMod = reader.GetFloat(),
					isCrit = reader.GetBool(),
					baseDmg = reader.GetInt()
				};
			}
			if (dataTypes.Contains(ActionType.DAMAGE_TAKEN))
			{
				damageTakenData = new()
				{
					attackerID = reader.GetShort(),
					attackerIsNPC = reader.GetBool(),
					damage = reader.GetInt(),
					damageType = (GameData.DamageType)reader.GetByte(),
					effect = reader.GetBool(),
					resistMod = reader.GetFloat(),
					isCrit = reader.GetBool(),
					baseDmg = reader.GetInt()
				};
			}
			if (dataTypes.Contains(ActionType.SPELL_CHARGE))
			{
				SpellChargeFXIndex = reader.GetInt();
			}
			if (dataTypes.Contains(ActionType.SPELL_EFFECT))
			{
				spellID = reader.GetString().Sanitize();
				targetID = reader.GetShort();
				targetIsNPC = reader.GetBool();
				targetIsSim = reader.GetBool();
			}
			if (dataTypes.Contains(ActionType.STATUS_EFFECT_APPLY))
			{
				effectData = new()
				{
					spellID = reader.GetString(),
					damageBonus = reader.GetInt(),
					casterID = reader.GetShort(),
					casterType = (EntityType)reader.GetByte(),
					duration = reader.GetFloat(),
					targetID = reader.GetShort(),
					targetType = (EntityType)reader.GetByte()
				};
			}
			if (dataTypes.Contains(ActionType.STATUS_EFFECT_REMOVE))
			{
				statusID = reader.GetInt();
				RemoveAllStatus = reader.GetBool();
				RemoveBreakable = reader.GetBool();
			}
			if (dataTypes.Contains(ActionType.HEAL))
			{
				var cnt = reader.GetInt();
				healingData = new();
				for (int i = 0;i < cnt;i++)
				{
					HealingData h = new()
					{
						targetID = reader.GetShort(),
						spellID = reader.GetString(),
						amount = reader.GetInt(),
						isCrit = reader.GetBool(),
						isMP = reader.GetBool()
					};
					healingData.Add(h);
				}
			}
			if (dataTypes.Contains(ActionType.WAND_ATTACK))
			{
				var cnt = reader.GetInt();
				wandData = new();
				for (int i = 0; i < cnt; i++)
				{
					WandAttackData w = new()
					{
						targetID = reader.GetShort(),
						itemID = reader.GetString().Sanitize(),
						isBowAttack = reader.GetBool(),
						attackType = reader.GetShort(),
						arrowIndex = reader.GetInt(),
						dmgMod = reader.GetInt(),
						interrupt = reader.GetBool()
					};
					wandData.Add(w);
				}
			}
			if (dataTypes.Contains(ActionType.WORN_EFFECT_REFRESH))
			{
				var cnt = reader.GetInt();
				wornEffects = new();
				for (int i = 0; i < cnt; i++)
				{
					StatusEffectData d = new()
					{
						spellID = reader.GetString()
					};
					wornEffects.Add(d);
				}
			}
			if (dataTypes.Contains(ActionType.ACTIVE_STATUS_EFFECTS))
			{
				var cnt = reader.GetInt();
				activeEffects = new();
				for (int i = 0; i < cnt; i++)
				{
					StatusEffectData d = new()
					{
						spellID = reader.GetString(),
						duration = reader.GetFloat(),
					};
					activeEffects.Add(d);
				}
			}
		}
	}

	public struct PlayerAttackData
	{
		public short attackedID;
		public bool attackedIsNPC;
		public int damage;
		public GameData.DamageType damageType;
		public bool effect;
		public float resistMod;
		public bool isCrit;
		public int baseDmg;
	}

	public struct DamageTakenData
	{
		public short attackerID;
		public bool attackerIsNPC;
		public int damage;
		public GameData.DamageType damageType;
		public bool effect;
		public float resistMod;
		public bool isCrit;
		public int baseDmg;
	}

	public struct StatusEffectData
	{
		public string spellID;
		public int damageBonus;
		public EntityType casterType;
		public short casterID;
		public float duration;
		public short targetID;
		public EntityType targetType;
	}

	public struct HealingData
	{
		public short targetID;
		public string spellID;
		public int amount;
		public bool isMP;
		public bool isCrit;
	}

	public struct WandAttackData
	{
		public short targetID;
		public string itemID;
		public bool isBowAttack;
		public short attackType;
		public int arrowIndex;
		public int dmgMod;
		public bool interrupt;
	}
}
