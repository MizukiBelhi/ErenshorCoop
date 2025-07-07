using ErenshorCoop.Client;
using ErenshorCoop.Server;
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
		public short entityID = -1;
		public Character character;
		public string currentScene = "";
		public EntityType type = EntityType.ENEMY;
		public string zone = "";
		protected Entity aggroTarget;

		public bool isGuardian = false;
		public int guardianId = 0;
		public int treasureChestID = 0;

		//Summon
		public Entity MySummon;
		public Entity owner;
		public EntityType ownerType;
		public string spellID;


		public void Start()
		{
			if (type == EntityType.PET && entityID == -1 && (GetType() == typeof(NPCSync)))
			{
				RequestID();

			}
		}

		public void RequestID()
		{
			
			//ask server for an id
			if (!ServerConnectionManager.Instance.IsRunning)
			{
				ClientConnectionManager.Instance.requestReceivers.Add(this);

				var pa = PacketManager.GetOrCreatePacket<PlayerRequestPacket>(ClientConnectionManager.Instance.LocalPlayerID, PacketType.PLAYER_REQUEST);
				if (pa.requestEntityType == null)
					pa.requestEntityType = new();
				pa.dataTypes.Add(Request.ENTITY_ID);
				pa.requestEntityType.Add(type);
				//pa.CanSend();
				Logging.Log($"Sending entityID request");
			}
			else
			{
				entityID = SharedNPCSyncManager.Instance.GetFreeId();
				if (type == EntityType.PET)
					SharedNPCSyncManager.Instance.ServerSpawnPet(gameObject, owner.entityID, entityID, spellID);
				else
				{
					if(isGuardian)
						SharedNPCSyncManager.Instance.ServerSpawnMob(gameObject, (int)CustomSpawnID.TREASURE_GUARD, $"{treasureChestID},{guardianId}", false, transform.position, transform.rotation);
				}
			}
		}

		public void ReceiveRequestID(short id)
		{
			Logging.Log($"received entityID request {id}");
			if (GetType() != typeof(NPCSync))
				return;
			if (id == -1) return;

			entityID = id;

			if (type == EntityType.PET)
				SharedNPCSyncManager.Instance.ServerSpawnPet(gameObject, owner.entityID, id, spellID);
			else
			{
				if(isGuardian)
					SharedNPCSyncManager.Instance.ServerSpawnMob(gameObject, (int)CustomSpawnID.TREASURE_GUARD, $"{treasureChestID},{guardianId}", false, transform.position, transform.rotation);
			}
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

		public void HandleTargetChange(short targetID, EntityType targetType)
		{
			if(targetID == -1)
			{
				character.MyNPC.CurrentAggroTarget = null;
				return;
			}
			(bool isPlayer, var target) = Extensions.GetEntityFromID(targetType==EntityType.ENEMY, targetID, targetType==EntityType.SIM);
			if (target == null)
			{
				character.MyNPC.CurrentAggroTarget = null;
				return;
			}

			if(type != EntityType.ENEMY)
			{
				if (targetType != EntityType.ENEMY)
					return;
			}

			if(type == EntityType.ENEMY)
			{
				if (targetType == EntityType.ENEMY)
					return;
			}
			
			character.MyNPC.CurrentAggroTarget = target.character;
			aggroTarget = target;
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
