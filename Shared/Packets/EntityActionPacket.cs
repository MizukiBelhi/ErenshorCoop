using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;

namespace ErenshorCoop.Shared.Packets
{
	public class EntityActionPacket : EntityBasePacket
	{
		
		public HashSet<ActionType> dataTypes = new();

		public EntityAttackData attackData;
		public int SpellChargeFXIndex;
		public string spellID;
		public short targetID;
		public bool targetIsNPC;
		public bool targetIsSim;
		public List<HealingData> healingData;
		public StatusEffectData effectData;
		public bool RemoveAllStatus = false;
		public bool RemoveBreakable = false;
		public int statusID;
		public List<WandAttackData> wandData;
		public EntityActionPacket() : base(DeliveryMethod.ReliableOrdered) { }

		public override void Write(NetDataWriter writer)
		{
			writer.Put((byte)PacketType.ENTITY_ACTION);

			writer.Put(targetPlayerIDs.Count);
			foreach (var p in targetPlayerIDs)
				writer.Put(p);


			writer.Put(entityID);
			writer.Put((byte)entityType);
			writer.Put(zone);


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
			if (dataTypes.Contains(ActionType.WAND_ATTACK))
			{
				writer.Put(wandData.Count);
				foreach (var w in wandData)
				{
					writer.Put(w.targetID);
					writer.Put(w.itemID);
				}
			}
		}

		public override void Read(NetDataReader reader)
		{
			int c = reader.GetInt();
			targetPlayerIDs = new();
			for (var i = 0; i < c; i++)
			{
				targetPlayerIDs.Add(reader.GetShort());
			}

			entityID = reader.GetShort();
			entityType = (EntityType)reader.GetByte();
			zone = reader.GetString().Sanitize();

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
					isCrit = reader.GetBool()
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
			if (dataTypes.Contains(ActionType.HEAL))
			{
				var cnt = reader.GetInt();
				healingData = new();
				for (int i = 0; i < cnt; i++)
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
			if (dataTypes.Contains(ActionType.STATUS_EFFECT_APPLY))
			{
				effectData = new()
				{
					spellID = reader.GetString().Sanitize(),
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
					};
					wandData.Add(w);
				}
			}
		}

	}

	public struct EntityAttackData
	{
		public short attackedID;
		public bool attackedIsNPC;
		public int damage;
		public GameData.DamageType damageType;
		public bool effect;
		public float resistMod;
		public bool isCrit;
	}
}
