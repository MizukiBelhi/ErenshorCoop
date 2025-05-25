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
	public class EntitySpawnPacket : EntityBasePacket
	{
		public List<EntitySpawnData> spawnData;

		public EntitySpawnPacket() : base(DeliveryMethod.ReliableOrdered) { }
		public List<short> targetPlayerIDs;

		public override void Write(NetDataWriter writer)
		{
			writer.Put((byte)PacketType.ENTITY_SPAWN);
			writer.Put(targetPlayerIDs.Count);
			foreach(var p in targetPlayerIDs)
				writer.Put(p);

			writer.Put(zone);
			writer.Put((byte)entityType);
			writer.Put(spawnData.Count);
			foreach (var s in spawnData)
			{
				writer.Put(s.entityID);
				writer.Put(s.npcID);
				writer.Put(s.spawnerID);
				writer.Put(s.isRare);
				writer.Put(s.position);
				writer.Put(s.rotation);
				writer.Put((byte)s.entityType);
				if (s.entityType == EntityType.PET)
				{
					writer.Put(s.ownerID);
				}
			}
		}

		public override void Read(NetPacketReader reader)
		{
			
			int c = reader.GetInt();
			targetPlayerIDs = new();
			for (var i = 0;i < c;i++)
			{
				targetPlayerIDs.Add(reader.GetShort());
			}

			zone = reader.GetString();
			entityType = (EntityType)reader.GetByte();

			int count = reader.GetInt();
			spawnData = new List<EntitySpawnData>();
			for (var i = 0;i < count;i++)
			{
				EntitySpawnData s = new()
				{
					entityID = reader.GetShort(),
					npcID = reader.GetString(),
					spawnerID = reader.GetInt(),
					isRare = reader.GetBool(),
					position = reader.GetVector3(),
					rotation = reader.GetRotation(),
					entityType = (EntityType)reader.GetByte()
				};
				s.ownerID = s.entityType == EntityType.PET ? reader.GetShort() : (short)-1;
				spawnData.Add(s);
			}
		}
	}

	public class EntitySpawnData
	{
		public short entityID;
		public string npcID;
		public int spawnerID;
		public bool isRare;
		public Vector3 position;
		public Quaternion rotation;
		public EntityType entityType;
		public short ownerID;
	}
}
