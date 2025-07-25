﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;

namespace ErenshorCoop.Shared.Packets
{
	public class ServerGroupPacket : EntityBasePacket
	{
		public HashSet<GroupDataType> dataTypes = new();
		public ServerGroupPacket() : base(DeliveryMethod.ReliableOrdered) { }

		public List<Grouping.Member> groupList;
		public short groupID;
		public short inviteID;
		public short leaderID;
		public GroupLeaveReason reason;

		public int earnedXP;
		public float xpBonus;

		public short followTargetID;
		public short simID;

		public override void Write(NetDataWriter writer)
		{
			writer.Put((byte)PacketType.SERVER_GROUP);
			writer.Put(entityID);

			writer.Put(Extensions.GetSubTypeFlag(dataTypes));

			if (dataTypes.Contains(GroupDataType.INVITE))
			{
				writer.Put(leaderID);
				writer.Put(inviteID);
			}
			if (dataTypes.Contains(GroupDataType.MEMBER_LIST))
			{
				writer.Put(leaderID);
				writer.Put((byte)groupList.Count);
				foreach (Grouping.Member member in groupList)
				{
					writer.Put(member.entityID);
					writer.Put(member.isSim);
					//writer.Put(member.simIndex);
				}
			}
			if(dataTypes.Contains(GroupDataType.SIM_FOLLOW))
			{
				writer.Put(followTargetID);
				writer.Put(simID);
			}
			if (dataTypes.Contains(GroupDataType.EXPERIENCE))
			{
				writer.Put(earnedXP);
				writer.Put(xpBonus);
			}
		}

		public override void Read(NetDataReader reader)
		{
			entityID = reader.GetShort();

			dataTypes = Extensions.ReadSubTypeFlag<GroupDataType>(reader.GetUShort());

			if (dataTypes.Contains(GroupDataType.INVITE))
			{
				leaderID = reader.GetShort();
				inviteID = reader.GetShort();
			}
			if (dataTypes.Contains(GroupDataType.MEMBER_LIST))
			{
				leaderID = reader.GetShort();
				byte count = reader.GetByte();
				groupList = new();
				for (var i = 0; i < count; i++)
				{
					var m = new Grouping.Member
					{
						entityID = reader.GetShort(),
						isSim = reader.GetBool(),
						//simIndex = reader.GetShort(),
					};
					groupList.Add(m);
				}
			}
			if(dataTypes.Contains(GroupDataType.SIM_FOLLOW))
			{
				followTargetID = reader.GetShort();
				simID = reader.GetShort();
			}
			if (dataTypes.Contains(GroupDataType.EXPERIENCE))
			{
				earnedXP = reader.GetInt();
				xpBonus = reader.GetFloat();
			}
		}
	}
}
