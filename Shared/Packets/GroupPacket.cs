using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;

namespace ErenshorCoop.Shared.Packets
{
	public class GroupPacket : EntityBasePacket
	{
		public HashSet<GroupDataType> dataTypes = new();
		public GroupPacket() : base(DeliveryMethod.ReliableOrdered) { }

		
		public short inviteID;
		public bool inviteAccept;
		public short groupID;
		public short playerID;
		public GroupLeaveReason reason;

		public int xp;
		public bool useMod;
		public float xpBonus;

		public override void Write(NetDataWriter writer)
		{
			writer.Put((byte)PacketType.GROUP);
			writer.Put(entityID);

			writer.Put(Extensions.GetSubTypeFlag(dataTypes));


			if (dataTypes.Contains(GroupDataType.ACCEPT_DECLINE))
			{
				writer.Put(inviteAccept);
				writer.Put(inviteID);
			}
			if (dataTypes.Contains(GroupDataType.INVITE))
			{
				writer.Put(playerID);
				writer.Put(isSim);
			}
			if (dataTypes.Contains(GroupDataType.REMOVE))
			{
				writer.Put((byte)reason);
				writer.Put(playerID);
				writer.Put(isSim);
			}
			if (dataTypes.Contains(GroupDataType.EXPERIENCE))
			{
				writer.Put(xp);
				writer.Put(useMod);
				writer.Put(xpBonus);
			}

		}

		public override void Read(NetDataReader reader)
		{
			entityID = reader.GetShort();

			dataTypes = Extensions.ReadSubTypeFlag<GroupDataType>(reader.GetUShort());

			if (dataTypes.Contains(GroupDataType.ACCEPT_DECLINE))
			{
				inviteAccept = reader.GetBool();
				inviteID = reader.GetShort();
			}
			if (dataTypes.Contains(GroupDataType.INVITE))
			{
				playerID = reader.GetShort();
				isSim = reader.GetBool();
			}
			if (dataTypes.Contains(GroupDataType.INVITE_RESPONSE))
				groupID = reader.GetShort();
			if (dataTypes.Contains(GroupDataType.REMOVE))
			{
				reason = (GroupLeaveReason)reader.GetByte();
				playerID = reader.GetShort();
				isSim = reader.GetBool();
			}
			if (dataTypes.Contains(GroupDataType.EXPERIENCE))
			{
				xp = reader.GetInt();
				useMod = reader.GetBool();
				xpBonus = reader.GetFloat();
			}
		}
	}
}
