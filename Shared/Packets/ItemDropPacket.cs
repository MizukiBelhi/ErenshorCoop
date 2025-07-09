using System.Collections.Generic;
using System.Runtime.InteropServices;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace ErenshorCoop.Shared.Packets
{
	public class ItemDropPacket : BasePacket
	{
		public HashSet<ItemDropType> dataTypes = new();

		public string itemID = "";
		public int quality = 0;
		public Vector3 location = Vector3.zero;
		public string zone;
		public string id;
		public short senderID;

		public ItemDropPacket() : base(DeliveryMethod.ReliableOrdered) { }

		public override void Write(NetDataWriter writer)
		{
			writer.Put((byte)PacketType.ITEM_DROP);

			writer.Put(senderID);

			ushort flag = Extensions.GetSubTypeFlag(dataTypes);
			writer.Put(flag);

			if (dataTypes.Contains(ItemDropType.DROP))
			{
				writer.Put(itemID);
				writer.Put((byte)quality);
				writer.Put(location);
				writer.Put(zone);
				writer.Put(id);
			}
			if (dataTypes.Contains(ItemDropType.DESTROY))
				writer.Put(id);
			if (dataTypes.Contains(ItemDropType.NEW_QUANTITY))
			{
				writer.Put(id);
				writer.Put((byte)quality);
			}
		}

		public override void Read(NetDataReader reader)
		{
			senderID = reader.GetShort();
			dataTypes = Extensions.ReadSubTypeFlag<ItemDropType>(reader.GetUShort());
			if (dataTypes.Contains(ItemDropType.DROP))
			{
				itemID = reader.GetString().Sanitize();
				quality = reader.GetByte();
				location = reader.GetVector3();
				zone = reader.GetString().Sanitize();
				id = reader.GetString().Sanitize();
			}

			if (dataTypes.Contains(ItemDropType.DESTROY))
				id = reader.GetString().Sanitize();
			if (dataTypes.Contains(ItemDropType.NEW_QUANTITY))
			{
				id = reader.GetString().Sanitize();
				quality = reader.GetByte();
			}
		}

	}
}
