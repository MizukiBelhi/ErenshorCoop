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
				writer.Put(s.syncStats);
				if (s.entityType == EntityType.PET)
				{
					writer.Put(s.ownerID);
				}
				if(s.syncStats)
				{
					writer.Put(s.level);
					writer.Put(s.baseAC);
					writer.Put(s.baseHP);
					writer.Put(s.baseMR);
					writer.Put(s.basePR);
					writer.Put(s.baseVR);
					writer.Put(s.baseER);
					writer.Put(s.baseDMG);
					writer.Put(s.mhatkDelay);
				}
			}
		}

		public override void Read(NetDataReader reader)
		{
			
			int c = reader.GetInt();
			targetPlayerIDs = new();
			for (var i = 0;i < c;i++)
			{
				targetPlayerIDs.Add(reader.GetShort());
			}

			zone = reader.GetString().Sanitize();
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
					entityType = (EntityType)reader.GetByte(),
					syncStats = reader.GetBool()
				};
				s.ownerID = s.entityType == EntityType.PET ? reader.GetShort() : (short)-1;
				if (s.syncStats)
				{
					s.level = reader.GetInt();
					s.baseAC = reader.GetInt();
					s.baseHP = reader.GetInt();
					s.baseMR = reader.GetInt();
					s.basePR = reader.GetInt();
					s.baseVR = reader.GetInt();
					s.baseER = reader.GetInt();
					s.baseDMG = reader.GetInt();
					s.mhatkDelay = reader.GetFloat();
				}

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
		public bool syncStats = false;
		public int level;
		public int baseHP;
		public int baseAC;
		public int baseMR;
		public int baseER;
		public int baseVR;
		public int basePR;
		public int baseDMG;
		public float mhatkDelay;
	}
}
