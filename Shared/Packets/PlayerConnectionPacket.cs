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
	public class PlayerConnectionPacket : EntityBasePacket
	{
		public Vector3 position;
		public Quaternion rotation;
		public Class _class;
		public int health;
		public int mp;
		public int level;
		public string scene;
		public string name;
		public LookData lookData;
		public List<GearData> gearData;
		public PlayerConnectionPacket() : base(DeliveryMethod.ReliableOrdered) { }

		public override void Write(NetDataWriter writer)
		{
			writer.Put((byte)PacketType.PLAYER_CONNECT);
			writer.Put(entityID);
			writer.Put(isSim);
			writer.Put(position);
			writer.Put(rotation);
			writer.Put(health);
			writer.Put(mp);
			writer.Put(ErenshorCoopMod.Class2ClassID(_class));
			writer.Put((byte)level);
			writer.Put(name);
			writer.Put(scene);


			writer.Put(lookData.isMale);
			writer.Put(lookData.hairName);
			writer.Put(lookData.hairColor);
			writer.Put(lookData.skinColor);
			writer.Put(gearData.Count);
			foreach (var g in gearData)
			{
				writer.Put((byte)g.slotType);
				writer.Put(g.itemID);
				writer.Put((byte)g.quality);
			}
			
		}

		public override void Read(NetDataReader reader)
		{
			entityID = reader.GetShort();
			isSim = reader.GetBool();
			position = reader.GetVector3();
			rotation = reader.GetRotation();
			health = reader.GetInt();
			mp = reader.GetInt();
			_class = ErenshorCoopMod.ClassID2Class(reader.GetByte());
			level = reader.GetByte();
			name = reader.GetString().Sanitize();
			scene = reader.GetString().Sanitize();
			
			lookData = new()
			{
				isMale = reader.GetBool(),
				hairName = reader.GetString(),
				hairColor = reader.GetColor(),
				skinColor = reader.GetColor(),
			};

			gearData = new();
			int count = reader.GetInt();
			for (var i = 0; i < count; i++)
			{
				GearData gd = new()
				{
					slotType = (Item.SlotType)reader.GetByte(),
					itemID = reader.GetString(),
					quality = reader.GetByte()
				};
				gearData.Add(gd);
			}
			
		}
	}
}
