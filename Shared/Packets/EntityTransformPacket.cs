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
	public class EntityTransformPacket : EntityBasePacket
	{
		public HashSet<EntityDataType> dataTypes = new();

		public Vector3 position;
		public Quaternion rotation;

		public EntityTransformPacket() : base(DeliveryMethod.Unreliable) { }

		public override void Write(NetDataWriter writer)
		{
			writer.Put((byte)PacketType.ENTITY_TRANSFORM);

			writer.Put(targetPlayerIDs.Count);
			foreach (var p in targetPlayerIDs)
				writer.Put(p);

			writer.Put(entityID);
			writer.Put(zone);
			writer.Put(Extensions.GetSubTypeFlag(dataTypes));
			writer.Put((byte)entityType);

			if (dataTypes.Contains(EntityDataType.POSITION))
				writer.Put(position);
			if (dataTypes.Contains(EntityDataType.ROTATION))
				writer.Put(rotation);
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
			zone = reader.GetString().Sanitize();

			dataTypes = Extensions.ReadSubTypeFlag<EntityDataType>(reader.GetUShort());
			entityType = (EntityType)reader.GetByte();

			if (dataTypes.Contains(EntityDataType.POSITION))
				position = reader.GetVector3();
			if (dataTypes.Contains(EntityDataType.ROTATION))
				rotation = reader.GetRotation();
		}
	}
}
