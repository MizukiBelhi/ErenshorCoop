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
		public Entity previousTarget = null;


		private int previousHealth = 0;
		private int previousMP = 0;

		

		public void Awake()
		{
			anim = GetComponent<Animator>();
			character = GetComponent<Character>();
			npc = GetComponent<NPC>();
			sim = GetComponent<SimPlayer>();
			ClientConnectionManager.Instance.OnClientConnect += OnClientConnect;

			type = EntityType.ENEMY;
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
				if (character.MyCharmedNPC != null)
				{
					Destroy(character.MyCharmedNPC.gameObject);
				}
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


			if (type == EntityType.PET) return;

			var curTar = npc?.GetCurrentTarget();
			if (curTar != null)
			{
				var curTarEnt = curTar?.GetComponent<Entity>();
				if (curTarEnt != null)
				{
					if (previousTarget != curTarEnt)
					{
						var p = PacketManager.GetOrCreatePacket<EntityDataPacket>(entityID, PacketType.ENTITY_DATA);
						p.AddPacketData(EntityDataType.CURTARGET, "targetID", curTarEnt.entityID);
						p.targetType = curTarEnt.type;
						if (curTarEnt is PlayerSync || curTarEnt is NetworkedPlayer)
							p.targetType = EntityType.PLAYER;
						p.SetData("targetPlayerIDs", SharedNPCSyncManager.Instance.GetPlayerSendList());
						p.entityType = type;
						p.zone = SceneManager.GetActiveScene().name;
						previousTarget = curTarEnt;
					}
				}
				else
				{
					if (previousTarget != null)
					{
						var p = PacketManager.GetOrCreatePacket<EntityDataPacket>(entityID, PacketType.ENTITY_DATA);
						p.AddPacketData(EntityDataType.CURTARGET, "targetID", (short)-1);
						p.targetType = EntityType.LOCAL_PLAYER;
						p.SetData("targetPlayerIDs", SharedNPCSyncManager.Instance.GetPlayerSendList());
						p.targetType = EntityType.LOCAL_PLAYER;
						p.zone = SceneManager.GetActiveScene().name;
						previousTarget = null;
					}
				}
			}



			//check everyone in aggro table
			if (npc.AggroTable != null && npc.AggroTable.Count > 0)
			{
				for (int i = 0; i < npc.AggroTable.Count; i++)
				{
					var p = npc.AggroTable[i];
					if (p == null || p.Player == null) //cleanup
					{
						npc.AggroTable.Remove(p);
						continue;
					}
					var ent = p.Player.GetComponent<Entity>(); //Remove if they are in a different zone
					if (ent != null && (ent is NetworkedPlayer || ent is NetworkedSim) && ent.zone != SceneManager.GetActiveScene().name)
					{
						if (npc.CurrentAggroTarget == p.Player)
							npc.CurrentAggroTarget = null;
						npc.AggroTable.Remove(p);
					}
				}
			}

		}

		public void LateUpdate()
		{
			if (type == EntityType.PET) return;

			var curTar = npc?.GetCurrentTarget();
			if (curTar != null)
			{
				var curTarEnt = curTar?.GetComponent<Entity>();
				if (character != null && npc.CurrentAggroTarget != null && character.Alive && curTarEnt != null)
				{
					//GameData.GroupMatesInCombat.Add(npc);
					//Logging.Log($" {curTarEnt.ToString()} {character.ToString()} {GameData.SimPlayerGrouping.GroupTargets.Contains(character)} ");
					if (npc.CurrentAggroTarget != null && npc.CurrentAggroTarget.MyNPC != null && npc.CurrentAggroTarget.MyNPC.ThisSim != null && !GameData.AttackingPlayer.Contains(npc) && curTarEnt != null && npc.CurrentAggroTarget.MyNPC.ThisSim.InGroup)
					{

						GameData.AttackingPlayer.Add(npc);
						if (!GameData.SimPlayerGrouping.GroupTargets.Contains(character))
							GameData.SimPlayerGrouping.GroupTargets.Add(character);


						//Logging.Log($"Add {character.name} grouptarget");
						if (!GameData.GroupMatesInCombat.Contains(npc.CurrentAggroTarget.MyNPC))
							GameData.GroupMatesInCombat.Add(npc.CurrentAggroTarget.MyNPC);
						//GameData.SimPlayerGrouping.GroupTargets.Add(character);
					}
				}
			}
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

		public void SendWand(WandAttackData wa)
		{
			var pack = PacketManager.GetOrCreatePacket<EntityActionPacket>(entityID, PacketType.ENTITY_ACTION);
			var wandData = pack.wandData ?? new();
			wandData.Add(wa);
			pack.dataTypes.Add(ActionType.WAND_ATTACK);
			pack.wandData = wandData;
			pack.entityType = type;
			pack.zone = SceneManager.GetActiveScene().name;
		}
	}
}
