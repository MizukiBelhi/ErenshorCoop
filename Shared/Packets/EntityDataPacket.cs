﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace ErenshorCoop.Shared.Packets
{
	public class EntityDataPacket : EntityBasePacket
	{
		public HashSet<EntityDataType> dataTypes = new();

		public List<AnimationData> animData = new();
		public int health;
		public int mp;
		public short targetID;
		public EntityType targetType;

		public EntityDataPacket() : base(DeliveryMethod.ReliableOrdered) { }

		public override void Write(NetDataWriter writer)
		{
			writer.Put((byte)PacketType.ENTITY_DATA);

			writer.Put(targetPlayerIDs.Count);
			foreach (var p in targetPlayerIDs)
				writer.Put(p);

			writer.Put(entityID);
			writer.Put((byte)entityType);
			writer.Put(zone);
			writer.Put(Extensions.GetSubTypeFlag(dataTypes));
			

			if(dataTypes.Contains(EntityDataType.HEALTH))
				writer.Put(health);
			if(dataTypes.Contains(EntityDataType.MP))
				writer.Put(mp);
			if (dataTypes.Contains(EntityDataType.ANIM))
			{
				writer.Put(animData.Count);
				foreach (var a in animData)
				{
					writer.Put((byte)a.syncType);
					writer.Put(a.param);
					Extensions.WriteObject(writer, a.value);
				}
			}
			if (dataTypes.Contains(EntityDataType.CURTARGET))
			{
				writer.Put(targetID);
				writer.Put((byte)targetType);
			}
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
			entityType = (EntityType)reader.GetByte();
			zone = reader.GetString().Sanitize();

			dataTypes = Extensions.ReadSubTypeFlag<EntityDataType>(reader.GetUShort());
			

			if (dataTypes.Contains(EntityDataType.HEALTH))
				health = reader.GetInt();
			if(dataTypes.Contains(EntityDataType.MP))
				mp = reader.GetInt();
			if (dataTypes.Contains(EntityDataType.ANIM))
			{
				int animCount = reader.GetInt();
				animData = new();
				for (var i = 0; i < animCount; i++)
				{
					AnimationData _anim = new()
					{
						syncType = (AnimatorSyncType)reader.GetByte(),
						param = reader.GetString()
					};
					_anim.value = _anim.syncType switch
					{
						AnimatorSyncType.BOOL or AnimatorSyncType.TRIG or AnimatorSyncType.RSTTRIG => reader.GetBool(),
						AnimatorSyncType.FLOAT                                                     => reader.GetFloat(),
						AnimatorSyncType.INT                                                       => reader.GetInt(),
						AnimatorSyncType.OVERRIDE                                                  => reader.GetString().Sanitize(),
						_                                                                          => _anim.value
					};
					animData.Add(_anim);
				}
			}
			if (dataTypes.Contains(EntityDataType.CURTARGET))
			{
				targetID = reader.GetShort();
				targetType = (EntityType)reader.GetByte();
			}
		}
	}
}
