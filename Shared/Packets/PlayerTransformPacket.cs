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
	public class PlayerTransformPacket : EntityBasePacket
	{
		public HashSet<PlayerDataType> dataTypes = new();

		public Vector3 position;
		public Quaternion rotation;

		public PlayerTransformPacket() : base(DeliveryMethod.Sequenced) { }

		public override void Write(NetDataWriter writer)
		{
			writer.Put((byte)PacketType.PLAYER_TRANSFORM);
			writer.Put(entityID);
			writer.Put(Extensions.GetSubTypeFlag(dataTypes));

			if (dataTypes.Contains(PlayerDataType.POSITION))
				writer.Put(position);
			if (dataTypes.Contains(PlayerDataType.ROTATION))
				writer.Put(rotation);
		}

		public override void Read(NetPacketReader reader)
		{
			entityID = reader.GetShort();

			dataTypes = Extensions.ReadSubTypeFlag<PlayerDataType>(reader.GetUShort());

			if (dataTypes.Contains(PlayerDataType.POSITION))
				position = reader.GetVector3();
			if (dataTypes.Contains(PlayerDataType.ROTATION))
				rotation = reader.GetRotation();
		}
	}
}
