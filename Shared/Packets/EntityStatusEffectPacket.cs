using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErenshorCoop.Shared.Packets
{
	public class EntityStatusEffectPacket : EntityBasePacket
	{
		public HashSet<ActionType> dataTypes = new();

		public StatusEffectData effectData;
		public bool RemoveAllStatus = false;
		public bool RemoveBreakable = false;
		public int statusID;
		public List<StatusEffectData> activeEffects;
		public List<StatusEffectData> wornEffects;

		public EntityStatusEffectPacket() : base(DeliveryMethod.ReliableOrdered) { }

		public override void Write(NetDataWriter writer)
		{
			writer.Put((byte)PacketType.ENTITY_STATUS);
			writer.Put(entityID);
			writer.Put((byte)entityType);

			ushort flag = Extensions.GetSubTypeFlag(dataTypes);
			writer.Put(flag);

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
			if (dataTypes.Contains(ActionType.WORN_EFFECT_REFRESH))
			{
				writer.Put(wornEffects.Count);
				foreach (var h in wornEffects)
				{
					writer.Put(h.spellID);
				}
			}
			if (dataTypes.Contains(ActionType.ACTIVE_STATUS_EFFECTS))
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
			entityType = (EntityType)reader.GetByte();

			dataTypes = Extensions.ReadSubTypeFlag<ActionType>(reader.GetUShort());

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
}
