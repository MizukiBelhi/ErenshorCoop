using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErenshorCoop.Shared.Packets
{
	public class EntityAttackPacket : EntityBasePacket
	{
		public List<EntityAttackData> attackData = new();
		public bool isMob = false;
		public EntityAttackPacket() : base(DeliveryMethod.ReliableOrdered) { }

		public override void Write(NetDataWriter writer)
		{
			writer.Put((byte)PacketType.ENTITY_ATTACK);
			writer.Put(entityID);
			writer.Put((byte)entityType);

			writer.Put(isSim);
			writer.Put(isMob);

			writer.Put(attackData.Count);

			foreach(var attackData in attackData)
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
			
		}
		public override void Read(NetDataReader reader)
		{
			entityID = reader.GetShort();
			entityType = (EntityType)reader.GetByte();
			isSim = reader.GetBool();
			isMob = reader.GetBool();

			var cnt = reader.GetInt();
			attackData = new List<EntityAttackData>(cnt);
			for(var i = 0; i < cnt; i++)
			{
				EntityAttackData _attackData = new()
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
				attackData.Add(_attackData);
			}
			
		}
	}
}
