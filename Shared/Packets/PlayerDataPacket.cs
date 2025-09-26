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
	public class PlayerDataPacket : EntityBasePacket
	{
		public HashSet<PlayerDataType> dataTypes = new ();

		public Vector3 position;
		public Quaternion rotation;
		public Class _class;
		public int health;
		public int mp;
		public int maxHealth;
		public int maxMP;
		public int level;
		public string scene;
		public string name;
		public bool alive;
		public LookData lookData;
		public List<GearData> gearData;
		public List<AnimationData> animData = new();
		public short targetID;
		public EntityType targetType;
		public StatData stats;
		public string rename;

		public PlayerDataPacket() : base(DeliveryMethod.ReliableOrdered) {}

		public override void Write(NetDataWriter writer)
		{
			writer.Put((byte)PacketType.PLAYER_DATA);
			writer.Put(entityID);
			writer.Put(isSim);
			writer.Put(Extensions.GetSubTypeFlag(dataTypes));

			if (dataTypes.Contains(PlayerDataType.POSITION))
				writer.Put(position);
			if (dataTypes.Contains(PlayerDataType.ROTATION))
				writer.Put(rotation);
			if (dataTypes.Contains(PlayerDataType.HEALTH))
				writer.Put(health);
			if(dataTypes.Contains(PlayerDataType.MP))
				writer.Put(mp);
			if (dataTypes.Contains(PlayerDataType.CLASS))
				writer.Put(ErenshorCoopMod.Class2ClassID(_class));
			if (dataTypes.Contains(PlayerDataType.LEVEL))
				writer.Put((byte)level);
			if (dataTypes.Contains(PlayerDataType.NAME))
				writer.Put(name);
			if (dataTypes.Contains(PlayerDataType.CURTARGET))
			{
				writer.Put(targetID);
				writer.Put((byte)targetType);
			}
			if (dataTypes.Contains(PlayerDataType.SCENE))
				writer.Put(scene);
			if (dataTypes.Contains(PlayerDataType.ANIM))
			{
				writer.Put(animData.Count);
				foreach (var a in animData)
				{
					writer.Put((byte)a.syncType);
					writer.Put(a.param);
					Extensions.WriteObject(writer, a.value);
				}
			}
			if (dataTypes.Contains(PlayerDataType.GEAR))
			{
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
			if(dataTypes.Contains(PlayerDataType.PERIODIC_UPDATE))
			{
				writer.Put(health);
				writer.Put(maxHealth);
				writer.Put(mp);
				writer.Put(maxMP);
				writer.Put(scene);
				writer.Put(zone);
				writer.Put(alive);
			}
			if(dataTypes.Contains(PlayerDataType.STATS))
			{
				writer.Put(stats.str);
				writer.Put(stats.dex);
				writer.Put(stats._int);
				writer.Put(stats.wis);
				writer.Put(stats.agi);
				writer.Put(stats.end);
				writer.Put(stats.cha);
			}
			if(dataTypes.Contains(PlayerDataType.RENAME))
				writer.Put(rename);
		}

		public override void Read(NetDataReader reader)
		{
			entityID = reader.GetShort();
			isSim = reader.GetBool();
			dataTypes = Extensions.ReadSubTypeFlag<PlayerDataType>(reader.GetUShort());

			if (dataTypes.Contains(PlayerDataType.POSITION))
				position = reader.GetVector3();
			if (dataTypes.Contains(PlayerDataType.ROTATION))
				rotation = reader.GetRotation();
			if (dataTypes.Contains(PlayerDataType.HEALTH))
				health = reader.GetInt();
			if(dataTypes.Contains(PlayerDataType.MP))
				mp = reader.GetInt();
			if (dataTypes.Contains(PlayerDataType.CLASS))
				_class = ErenshorCoopMod.ClassID2Class(reader.GetByte());
			if(dataTypes.Contains(PlayerDataType.LEVEL))
				level = reader.GetByte();
			if(dataTypes.Contains(PlayerDataType.NAME))
				name = reader.GetString().Sanitize();
			if (dataTypes.Contains(PlayerDataType.CURTARGET))
			{
				targetID = reader.GetShort();
				targetType = (EntityType)reader.GetByte();
			}
			if(dataTypes.Contains(PlayerDataType.SCENE))
				scene = reader.GetString().Sanitize();
			if (dataTypes.Contains(PlayerDataType.ANIM))
			{
				int animCount = reader.GetInt();
				animData = new();
				for(var i=0;i<animCount;i++)
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
						AnimatorSyncType.OVERRIDE                                                  => reader.GetString(),
						_                                                                          => _anim.value
					};
					animData.Add(_anim);
				}
			}
			if (dataTypes.Contains(PlayerDataType.GEAR))
			{
				lookData = new()
				{
					isMale = reader.GetBool(),
					hairName = reader.GetString().Sanitize(),
					hairColor = reader.GetColor(),
					skinColor = reader.GetColor(),
				};
				gearData = new();
				int count = reader.GetInt();
				for (var i = 0; i < count;i++)
				{
					GearData gd = new()
					{
						slotType = (Item.SlotType)reader.GetByte(),
						itemID = reader.GetString().Sanitize(),
						quality = reader.GetByte()
					};
					gearData.Add(gd);
				}
			}
			if (dataTypes.Contains(PlayerDataType.PERIODIC_UPDATE))
			{
				health = reader.GetInt();
				maxHealth = reader.GetInt();
				mp = reader.GetInt();
				maxMP = reader.GetInt();
				scene = reader.GetString();
				zone = reader.GetString();
				alive = reader.GetBool();
			}
			if(dataTypes.Contains(PlayerDataType.STATS))
			{
				stats = new StatData()
				{
					str = reader.GetInt(),
					dex = reader.GetInt(),
					_int = reader.GetInt(),
					wis = reader.GetInt(),
					agi = reader.GetInt(),
					end = reader.GetInt(),
					cha = reader.GetInt()
				};
			}
			if(dataTypes.Contains(PlayerDataType.RENAME))
				rename = reader.GetString().Sanitize();
		}
	}

	public struct AnimationData
	{
		public AnimatorSyncType syncType;
		public string param;
		public object value;
	}

	public struct LookData
	{
		public bool isMale;
		public string hairName;
		public Color hairColor;
		public Color skinColor;
	}

	public struct GearData
	{
		public Item.SlotType slotType;
		public string itemID;
		public int quality;
	}

	public struct StatData
	{
		public int str;
		public int dex;
		public int _int;
		public int wis;
		public int agi;
		public int end;
		public int cha;
	}
}
