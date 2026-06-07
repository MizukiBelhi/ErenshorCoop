using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace ErenshorCoop.Shared.Packets
{
	public class EntityGuildData : EntityBasePacket
	{
		public string guildName;

		public EntityGuildData() : base(DeliveryMethod.ReliableOrdered) { }

		public override void Write(NetDataWriter writer)
		{
			writer.Put((byte)PacketType.ENTITY_GUILD);
			writer.Put(entityID);
			writer.Put(isSim);
			
			writer.Put(guildName);
		}

		public override void Read(NetDataReader reader)
		{
			entityID = reader.GetShort();
			isSim = reader.GetBool();

			guildName = reader.GetString().Sanitize();
		}
	}
}
