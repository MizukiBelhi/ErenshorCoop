using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ErenshorCoop.Client;
using UnityEngine;

namespace ErenshorCoop.Shared
{
	public static class Variables
	{
		private   const byte RequiredEntityChannels = 3;
		private   const byte MaxEntityType = (byte)PacketEntityType.MAX;
		public const byte MaxChannelCount = (RequiredEntityChannels * MaxEntityType)+2; //Two extra channel for messaging and grouping

		public static List<Character> DontCalculateDamageMitigationCharacters = new();
		public static List<Entity> DontCheckEffectCharacters = new();
		public static DroppedItem lastDroppedItem = new();

		public static Dictionary<int, SpawnGroup> spawnData = new();
		public static Dictionary<int, SpawnPoint> spawnPoints = new();

		public static Dictionary<string, List<ItemDropData>> droppedItems = new();

		public struct ItemDropData
		{
			public Item item;
			public int quantity;
			public Vector3 pos;
			public string id;
		}

		public class SpawnMobData
		{
			public string name = "";
			public int mobID = -1;
			public bool isRare = false;
			public GameObject prefab = null;
		}

		public static void AddSpawn(int spawnID, SpawnPoint spawn)
		{
			//string spawnID = spawn.ID;

			var commonSpawns = spawn.CommonSpawns;
			var rareSpawns = spawn.RareSpawns;

			var grp = new SpawnGroup();
			for (var i = 0; i < commonSpawns.Count; i++)
			{
				SpawnMobData spawnMobData = new()
				{
					isRare = false,
					prefab = commonSpawns[i],
					name = commonSpawns[i].GetComponent<NPC>().NPCName,
					mobID = i
				};
				grp.mobData.Add(spawnMobData);
			}
			for (var i = 0; i < rareSpawns.Count; i++)
			{
				SpawnMobData spawnMobData = new()
				{
					isRare = true,
					prefab = rareSpawns[i],
					name = rareSpawns[i].GetComponent<NPC>().NPCName,
					mobID = i
				};
				grp.mobData.Add(spawnMobData);
			}

			foreach (var eventNode in spawn.EventNodes)
			{
				eventNode.GetComponent<MeshRenderer>().enabled = false;
			}

			spawnData.Add(spawnID, grp);
			spawnPoints.Add(spawnID, spawn);
		}

		public class SpawnGroup
		{
			public List<SpawnMobData> mobData = new();

			public int levelMod = 0;

			public SpawnMobData GetMob(bool isRare, int mobID)
			{
				foreach (var other in mobData)
					if (other.mobID == mobID && isRare == other.isRare)
						return other;
				return null;
			}

			public SpawnMobData GetMobData(GameObject mob)
			{
				foreach (var other in mobData)
				{
					string otherMobName = other.name;
					string mobName = mob.GetComponent<NPC>().NPCName;

					if (mobName.Contains(otherMobName))
						return other;
				}

				return null;
			}

			public (int, bool) GetMob(GameObject mob)
			{
				foreach (var other in mobData)
				{
					string otherMobName = other.name;
					string mobName = mob.GetComponent<NPC>().NPCName;

					if (mobName.Contains(otherMobName))
						return (other.mobID, other.isRare);
				}

				return (-1, false);
			}
		}
	}
}
