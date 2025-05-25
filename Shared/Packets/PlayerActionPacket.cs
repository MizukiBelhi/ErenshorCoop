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

		public PlayerActionPacket() : base(DeliveryMethod.ReliableOrdered) { }

		public override void Write(NetDataWriter writer)
		{
			writer.Put((byte)PacketType.PLAYER_ACTION);
			writer.Put(entityID);


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
			}
			if (dataTypes.Contains(ActionType.DAMAGE_TAKEN))
			{
				writer.Put(damageTakenData.attackerID);
				writer.Put(damageTakenData.attackerIsNPC);
				writer.Put(damageTakenData.damage);
				writer.Put((byte)damageTakenData.damageType);
				writer.Put(damageTakenData.effect);
				writer.Put(damageTakenData.resistMod);
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
				writer.Put(effectData.attackerID);
				writer.Put(effectData.attackerIsPlayer);
				writer.Put(effectData.attackerIsSim);
				writer.Put(effectData.playerIsCaster);
				writer.Put(effectData.duration);
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
					writer.Put(h.targetIsNPC);
					writer.Put(h.targetIsSim);
					writer.Put(h.amount);
					writer.Put(h.isCrit);
					writer.Put(h.isMP);
				}
			}
		}

		public override void Read(NetPacketReader reader)
		{
			entityID = reader.GetShort();
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
					resistMod = reader.GetFloat()
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
					resistMod = reader.GetFloat()
				};
			}
			if (dataTypes.Contains(ActionType.SPELL_CHARGE))
			{
				SpellChargeFXIndex = reader.GetInt();
			}
			if (dataTypes.Contains(ActionType.SPELL_EFFECT))
			{
				spellID = reader.GetString();
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
					attackerID = reader.GetShort(),
					attackerIsPlayer = reader.GetBool(),
					attackerIsSim = reader.GetBool(),
					playerIsCaster = reader.GetBool(),
					duration = reader.GetFloat()
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
						targetIsNPC = reader.GetBool(),
						targetIsSim = reader.GetBool(),
						amount = reader.GetInt(),
						isCrit = reader.GetBool(),
						isMP = reader.GetBool()
					};
					healingData.Add(h);
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
	}

	public struct DamageTakenData
	{
		public short attackerID;
		public bool attackerIsNPC;
		public int damage;
		public GameData.DamageType damageType;
		public bool effect;
		public float resistMod;
	}

	public struct StatusEffectData
	{
		public string spellID;
		public int damageBonus;
		public bool attackerIsPlayer;
		public bool attackerIsSim;
		public short attackerID;
		public float duration;
		public bool playerIsCaster;
	}

	public struct HealingData
	{
		public short targetID;
		public bool targetIsNPC;
		public bool targetIsSim;
		public int amount;
		public bool isMP;
		public bool isCrit;
	}
}
