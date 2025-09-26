using ErenshorCoop.Client;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace ErenshorCoop.Shared
{
	public static class SyncedSpawnPoint
	{
		public static void Start(SpawnPoint __instance)
		{
			var d = Variables.syncedSpawns.GetOrCreateValue(__instance);
			var uniqueID = Extensions.GenerateHash(__instance.transform.position, SceneManager.GetActiveScene().name);
			d.id = uniqueID;
			Variables.syncedSpawnPoints[uniqueID] = new(__instance);

			if(ClientConnectionManager.Instance.IsRunning)
				__instance.gameObject.SetActive(false);
		}

		public static GameObject GetPrefab(int id, int index, bool isRare)
		{
		//	Logging.Log($"trying to get prefab for {id}:{index}:{isRare}");
			if (Variables.syncedSpawnPoints.TryGetValue(id, out var _spawnPoint) && _spawnPoint.TryGetTarget(out var spawnPoint))
			{
				if (isRare)
					return spawnPoint.RareSpawns[index];
				else
					return spawnPoint.CommonSpawns[index];
			}
			return null;
		}

		public static bool SpawnNPC(SpawnPoint __instance, bool _asCorpse)
		{

			if (ClientConnectionManager.Instance.IsRunning && !ClientZoneOwnership.isZoneOwner) return false;

			NPC npc = null;
			GameObject NextSpawn = null;
			int index = -1;
			bool isRare = false;


			if (__instance.RareSpawns.Count > 0)
			{
				if (Random.Range(0, 100) < __instance.RareNPCChance)
				{
					index = Random.Range(0, __instance.RareSpawns.Count);
					NextSpawn = __instance.RareSpawns[index];
					isRare = true;
				}
				else
				{
					index = Random.Range(0, __instance.CommonSpawns.Count);
					NextSpawn = __instance.CommonSpawns[index];
				}
			}
			else if (__instance.CommonSpawns.Count > 0)
			{
				index = Random.Range(0, __instance.CommonSpawns.Count);
				NextSpawn = __instance.CommonSpawns[index];
			}
			if (__instance.PatrolPoints.Count <= 0 || __instance.SpawnIteration > 0)
			{
				npc = UnityEngine.Object.Instantiate(NextSpawn, __instance.transform.position, __instance.transform.rotation).GetComponent<NPC>();
			}
			else if (__instance.PatrolPoints.Count > 0 && __instance.SpawnIteration <= 0)
			{
				npc = UnityEngine.Object.Instantiate(NextSpawn, __instance.PatrolPoints[Random.Range(0, __instance.PatrolPoints.Count)].transform.position, __instance.transform.rotation).GetComponent<NPC>();
			}
			if (npc != null)
			{
				npc.GetComponent<Stats>().Level += __instance.levelMod;
				if (__instance.Protector != null && npc.GetComponent<NPCInvuln>() != null)
				{
					npc.GetComponent<NPCInvuln>().Protector = __instance.Protector;
				}
				__instance.SpawnedNPC = npc;
				NPCTable.LiveNPCs.Add(npc);
				__instance.NPCCurrentlySpawned = true;
				__instance.MyNPCAlive = true;
				if (__instance.PatrolPoints.Count > 0)
				{
					npc.InitNewNPC(__instance, __instance.PatrolPoints);
				}
				else
				{
					npc.InitNewNPC(__instance, __instance.RandomWanderRange);
				}
				if (Variables.syncedSpawns.TryGetValue(__instance, out var val))
				{

					val.isRare = isRare;
					val.index = index;
					//Logging.Log($"set prefab for {val.id}:{index}:{isRare}");
					if(ClientConnectionManager.Instance.IsRunning && ClientZoneOwnership.isZoneOwner)
						SharedNPCSyncManager.Instance.ServerSpawnMob(npc.gameObject, val.id, index.ToString(), isRare, npc.transform.position, npc.transform.rotation);
				}
			}
			__instance.SpawnIteration++;

			return false;
		}
	}
}
