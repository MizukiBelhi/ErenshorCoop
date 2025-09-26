using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;

namespace ErenshorCoop.Shared.Packets
{
	public class PlayerRequestPacket : BasePacket
	{
		public HashSet<Request> dataTypes = new();
		public List<EntityType> requestEntityType;

		public PlayerRequestPacket() :base(DeliveryMethod.ReliableOrdered) { }

		public byte commandType;
		public string playerName;
		public short entityReqID;
		public short ownerID;

		public override void Write(NetDataWriter writer)
		{
			writer.Put((byte)PacketType.PLAYER_REQUEST);
			writer.Put(entityID);

			writer.Put(Extensions.GetSubTypeFlag(dataTypes));

			if (dataTypes.Contains(Request.ENTITY_ID))
			{
				writer.Put(requestEntityType.Count);
				foreach (var entityType in requestEntityType)
					writer.Put((byte)entityType);
			}
			if (dataTypes.Contains(Request.MOD_COMMAND))
			{
				writer.Put(commandType);
				writer.Put(playerName);
			}
			if(dataTypes.Contains(Request.ENTITY_SPAWN))
			{
				writer.Put(entityReqID);
				writer.Put(ownerID);
			}

		}

		public override void Read(NetDataReader reader)
		{
			entityID = reader.GetShort();
			dataTypes = Extensions.ReadSubTypeFlag<Request>(reader.GetUShort());

			if (dataTypes.Contains(Request.ENTITY_ID))
			{
				requestEntityType = new();
				var c = reader.GetInt();
				for(int i = 0; i < c; i++)
					requestEntityType.Add((EntityType)reader.GetByte());
			}
			if (dataTypes.Contains(Request.MOD_COMMAND))
			{
				commandType = reader.GetByte();
				playerName = reader.GetString().Sanitize();
			}
			if (dataTypes.Contains(Request.ENTITY_SPAWN))
			{
				entityReqID = reader.GetShort();
				ownerID = reader.GetShort();
			}
		}
	}
}
