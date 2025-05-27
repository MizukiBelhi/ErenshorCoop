using ErenshorCoop.Client;
using UnityEngine;
using ErenshorCoop.Shared;
using ErenshorCoop.Server;
using ErenshorCoop.Shared.Packets;
using UnityEngine.SceneManagement;

namespace ErenshorCoop
{
	public class NPCSync : Entity
	{
		public Animator anim;
		//public Character character;
		public NPC npc;
		public SimPlayer sim;

		//public short entityID;
		public bool isCloseToPlayer = false;

		public EntitySpawnData spawnData;


		public Vector3 previousPosition = Vector3.zero;
		public Quaternion previousRotation = Quaternion.identity;

		private int previousHealth = 0;
		private int previousMP = 0;

		

		public void Awake()
		{
			anim = GetComponent<Animator>();
			character = GetComponent<Character>();
			npc = GetComponent<NPC>();
			sim = GetComponent<SimPlayer>();
			ClientConnectionManager.Instance.OnClientConnect += OnClientConnect;
		}

		public void OnDestroy()
		{
			ClientConnectionManager.Instance.OnClientConnect -= OnClientConnect;
			SharedNPCSyncManager.Instance.OnMobDestroyed(entityID, type);
		}

		public void OnClientConnect(short __, string ___, string ____)
		{
			//Do we have a summon?
			//Destroy summon if it isn't our summon..
			if (MySummon == null || MySummon.character.MyNPC != character.MyCharmedNPC)
			{
				if(character.MyCharmedNPC != null)
					Destroy(character.MyCharmedNPC.gameObject);
			}
			if (MySummon != null)
			{
				MySummon.ReceiveRequestID(MySummon.entityID);
			}
		}

		public void Update()
		{
			if (!ClientConnectionManager.Instance.IsRunning) return; //??

			if (type == EntityType.PET && entityID == -1) return;
			if (type != EntityType.PET)
			{
				if (type != EntityType.SIM && !ClientZoneOwnership.isZoneOwner) return;
				if (type == EntityType.SIM && !ServerConnectionManager.Instance.IsRunning) return;
			}

			if (previousHealth != character.MyStats.CurrentHP)
			{
				var p = PacketManager.GetOrCreatePacket<EntityDataPacket>(entityID, PacketType.ENTITY_DATA);
				p.AddPacketData(EntityDataType.HEALTH, "health", character.MyStats.CurrentHP);
				p.SetData("targetPlayerIDs", SharedNPCSyncManager.Instance.GetPlayerSendList());
				p.entityType = type;
				p.zone = SceneManager.GetActiveScene().name;

				previousHealth = character.MyStats.CurrentHP;
			}

			if (previousMP != character.MyStats.CurrentMana)
			{
				var p = PacketManager.GetOrCreatePacket<EntityDataPacket>(entityID, PacketType.ENTITY_DATA);
				p.AddPacketData(EntityDataType.MP, "mp", character.MyStats.CurrentMana);
				p.SetData("targetPlayerIDs", SharedNPCSyncManager.Instance.GetPlayerSendList());
				p.entityType = type;
				p.zone = SceneManager.GetActiveScene().name;

				previousMP = character.MyStats.CurrentMana;
			}

			if (Vector3.Distance(transform.position, previousPosition) > 0.1f)
			{
				var p = PacketManager.GetOrCreatePacket<EntityTransformPacket>(entityID, PacketType.ENTITY_TRANSFORM);
				p.AddPacketData(EntityDataType.POSITION, "position", transform.position);
				p.SetData("targetPlayerIDs", SharedNPCSyncManager.Instance.GetPlayerSendList());
				p.entityType = type;
				p.zone = SceneManager.GetActiveScene().name;

				previousPosition = transform.position;
			}
			if (previousRotation != transform.rotation)
			{
				var p = PacketManager.GetOrCreatePacket<EntityTransformPacket>(entityID, PacketType.ENTITY_TRANSFORM);
				p.AddPacketData(EntityDataType.ROTATION, "rotation", transform.rotation);
				p.SetData("targetPlayerIDs", SharedNPCSyncManager.Instance.GetPlayerSendList());
				p.entityType = type;
				p.zone = SceneManager.GetActiveScene().name;
				previousRotation = transform.rotation;
			}

			isCloseToPlayer = true;
		}

		


		//Sends packet when this entity is using a healing spell (mp or hp)
		public void SendHeal(HealingData hd)
		{
			var p = PacketManager.GetOrCreatePacket<EntityActionPacket>(entityID, PacketType.ENTITY_ACTION);
			var healingData = p.healingData ?? new();
			healingData.Add(hd);
			p.dataTypes.Add(ActionType.HEAL);
			p.healingData = healingData;
			p.entityType = type;
			p.zone = SceneManager.GetActiveScene().name;
		}
	}
}
