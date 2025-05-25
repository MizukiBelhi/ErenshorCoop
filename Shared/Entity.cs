using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ErenshorCoop.Client;
using ErenshorCoop.Server;
using ErenshorCoop.Shared;
using ErenshorCoop.Shared.Packets;
using LiteNetLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ErenshorCoop.Shared
{
	public class Entity : MonoBehaviour
	{
		public string entityName = "";
		public NetPeer peer;
		public short entityID;
		public Character character;
		public string currentScene = "";
		public EntityType type = EntityType.ENEMY;
		public string zone = "";

		//Summon
		public Entity MySummon;
		public Entity owner;
		public EntityType ownerType;
		public string spellID;


		public void Start()
		{
			if (type == EntityType.PET && entityID == -1 && (GetType() == typeof(NPCSync)))
			{
				ClientConnectionManager.Instance.requestReceiver = this;
				//ask server for an id
				if (!ServerConnectionManager.Instance.IsRunning)
				{
					var pa = PacketManager.GetOrCreatePacket<PlayerRequestPacket>(ClientConnectionManager.Instance.LocalPlayerID, PacketType.PLAYER_REQUEST, true)
						.AddPacketData(Request.ENTITY_ID, "requestEntityType", EntityType.PET);
					pa.CanSend();
					//Logging.Log($"Sending entityID request");
				}
				else
				{
					entityID = SharedNPCSyncManager.Instance.GetFreeId();
					SharedNPCSyncManager.Instance.ServerSpawnPet(gameObject, owner.entityID, entityID, spellID);
				}

			}

		}

		public void ReceiveRequestID(short id)
		{
			//Logging.Log($"{name} Received entityID request {id}");
			if (GetType() != typeof(NPCSync))
				return;
			if (id == -1) return;
			//Logging.Log($"{name} Received entityID request {id}");
			entityID = id;

			
			SharedNPCSyncManager.Instance.ServerSpawnPet(gameObject, owner.entityID, id, spellID);
		}

		public void CreateSummon(Spell spell, GameObject o)
		{
			MySummon = character.MyCharmedNPC.gameObject.GetComponent<NPCSync>();
			if (MySummon == null)
			{
				MySummon = character.MyCharmedNPC.gameObject.AddComponent<NPCSync>();
				MySummon.entityID = -1;
			}
			else
			{
				MySummon.entityID = -1;
				MySummon.Start(); //Need to ask for a new ID
			}

			MySummon.type = EntityType.PET;
			MySummon.owner = this;
			MySummon.ownerType = type;
			MySummon.spellID = spell.Id;
			MySummon.zone = zone;
		}

		public void DespawnSummon()
		{
			if (MySummon != null)
			{
				var entID = MySummon.entityID;
				Destroy(MySummon);
			}
		}

		public void SendAttack(int damage, short attackerID, bool attackerNPC, GameData.DamageType dmgType, bool animEffect, float resistMod)
		{
			var p = PacketManager.GetOrCreatePacket<EntityActionPacket>(entityID, PacketType.ENTITY_ACTION);
			p.AddPacketData(ActionType.ATTACK, "attackData",
				new EntityAttackData()
				{
					attackedID = attackerID,
					attackedIsNPC = attackerNPC,
					damage = damage,
					damageType = dmgType,
					effect = animEffect,
					resistMod = resistMod
				});
			p.SetData("targetPlayerIDs", SharedNPCSyncManager.Instance.GetPlayerSendList());
			p.entityType = type;
			p.zone = SceneManager.GetActiveScene().name;
		}
	}

	public enum EntityType
	{
		ENEMY,
		SIM,
		PET,
		PLAYER,
		LOCAL_PLAYER,
	}
}
