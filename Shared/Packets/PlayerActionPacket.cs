using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ErenshorCoop.Client;
using LiteNetLib;
using LiteNetLib.Utils;

namespace ErenshorCoop.Shared.Packets
{
	public class PlayerActionPacket : EntityBasePacket
	{
		public HashSet<ActionType> dataTypes = new();


		public List<DamageTakenData> damageTakenData;
		public int SpellChargeFXIndex;
		public string spellID;
		public short targetID;
		public bool targetIsNPC;
		public bool targetIsSim;


		public List<HealingData> healingData;
		public List<WandAttackData> wandData;
		
		public PlayerActionPacket() : base(DeliveryMethod.ReliableOrdered) { }

		public override void Write(NetDataWriter writer)
		{
			writer.Put((byte)PacketType.PLAYER_ACTION);
			writer.Put(entityID);

			writer.Put(isSim);

			ushort flag = Extensions.GetSubTypeFlag(dataTypes);
			writer.Put(flag);

			if (dataTypes.Contains(ActionType.DAMAGE_TAKEN))
			{
				writer.Put(damageTakenData.Count);
				foreach (var damageTakenData in damageTakenData)
				{
					writer.Put(damageTakenData.attackerID);
					writer.Put(damageTakenData.attackerIsNPC);
					writer.Put(damageTakenData.damage);
					writer.Put((byte)damageTakenData.damageType);
					writer.Put(damageTakenData.effect);
					writer.Put(damageTakenData.resistMod);
					writer.Put(damageTakenData.isCrit);
					writer.Put(damageTakenData.baseDmg);
				}
			}
			if (dataTypes.Contains(ActionType.SPELL_CHARGE))
			{
				writer.Put(SpellChargeFXIndex);
			}
			if (dataTypes.Contains(ActionType.SPELL_EFFECT))
			{
				writer.Put(spellID);
				writer.Put(targetID);
				writer.Put(targetIsNPC);
				writer.Put(targetIsSim);
			}
			
			if (dataTypes.Contains(ActionType.HEAL))
			{
				writer.Put(healingData.Count);
				foreach (var h in healingData)
				{
					writer.Put(h.targetID);
					writer.Put(h.spellID);
					writer.Put(h.amount);
					writer.Put(h.isCrit);
					writer.Put(h.isMP);
				}
			}
			if(dataTypes.Contains(ActionType.WAND_ATTACK))
			{
				writer.Put(wandData.Count);
				foreach(var w in wandData)
				{
					writer.Put(w.targetID);
					writer.Put(w.itemID);
					writer.Put(w.isBowAttack);
					writer.Put(w.attackType);
					writer.Put(w.arrowIndex);
					writer.Put(w.dmgMod);
					writer.Put(w.interrupt);
				}
			}
			
		}

		public override void Read(NetDataReader reader)
		{
			entityID = reader.GetShort();
			isSim = reader.GetBool();
			dataTypes = Extensions.ReadSubTypeFlag<ActionType>(reader.GetUShort());

			if (dataTypes.Contains(ActionType.DAMAGE_TAKEN))
			{
				var cnt = reader.GetInt();
				damageTakenData = new List<DamageTakenData>(cnt);
				for (int i = 0; i < cnt; i++)
				{
					DamageTakenData d = new()
					{
						attackerID = reader.GetShort(),
						attackerIsNPC = reader.GetBool(),
						damage = reader.GetInt(),
						damageType = (GameData.DamageType)reader.GetByte(),
						effect = reader.GetBool(),
						resistMod = reader.GetFloat(),
						isCrit = reader.GetBool(),
						baseDmg = reader.GetInt()
					};
					damageTakenData.Add(d);
				}
			}
			if (dataTypes.Contains(ActionType.SPELL_CHARGE))
			{
				SpellChargeFXIndex = reader.GetInt();
			}
			if (dataTypes.Contains(ActionType.SPELL_EFFECT))
			{
				spellID = reader.GetString().Sanitize();
				targetID = reader.GetShort();
				targetIsNPC = reader.GetBool();
				targetIsSim = reader.GetBool();
			}
			
			if (dataTypes.Contains(ActionType.HEAL))
			{
				var cnt = reader.GetInt();
				healingData = new();
				for (int i = 0;i < cnt;i++)
				{
					HealingData h = new()
					{
						targetID = reader.GetShort(),
						spellID = reader.GetString(),
						amount = reader.GetInt(),
						isCrit = reader.GetBool(),
						isMP = reader.GetBool()
					};
					healingData.Add(h);
				}
			}
			if (dataTypes.Contains(ActionType.WAND_ATTACK))
			{
				var cnt = reader.GetInt();
				wandData = new();
				for (int i = 0; i < cnt; i++)
				{
					WandAttackData w = new()
					{
						targetID = reader.GetShort(),
						itemID = reader.GetString().Sanitize(),
						isBowAttack = reader.GetBool(),
						attackType = reader.GetShort(),
						arrowIndex = reader.GetInt(),
						dmgMod = reader.GetInt(),
						interrupt = reader.GetBool()
					};
					wandData.Add(w);
				}
			}
			
		}
	}

	public struct PlayerAttackData
	{
		public short attackedID;
		public bool attackedIsNPC;
		public int damage;
		public GameData.DamageType damageType;
		public bool effect;
		public float resistMod;
		public bool isCrit;
		public int baseDmg;
	}

	public struct DamageTakenData
	{
		public short attackerID;
		public bool attackerIsNPC;
		public int damage;
		public GameData.DamageType damageType;
		public bool effect;
		public float resistMod;
		public bool isCrit;
		public int baseDmg;
	}

	public struct StatusEffectData
	{
		public string spellID;
		public int damageBonus;
		public EntityType casterType;
		public short casterID;
		public float duration;
		public short targetID;
		public EntityType targetType;
	}

	public struct HealingData
	{
		public short targetID;
		public string spellID;
		public int amount;
		public bool isMP;
		public bool isCrit;
	}

	public struct WandAttackData
	{
		public short targetID;
		public string itemID;
		public bool isBowAttack;
		public short attackType;
		public int arrowIndex;
		public int dmgMod;
		public bool interrupt;
	}
}
