using System;
using System.Collections.Generic;
using ErenshorCoop.Server;
using ErenshorCoop.Shared;
using UnityEngine.SceneManagement;

namespace ErenshorCoop.Client
{
	public static class ClientZoneOwnership
	{
		public static readonly Dictionary<short, Entity> _zonePlayers = new();

		public static bool isZoneOwner = false;
		private static string _lastOwnershipZone = "";


		public static void OnConnect()
		{
			_zonePlayers.Clear();
			isZoneOwner = false;
			_lastOwnershipZone = null;
			ClientConnectionManager.Instance.OnClientConnect += OnClientChangeZone;
			ClientConnectionManager.Instance.OnClientSwapZone += OnClientChangeZone;
			ClientConnectionManager.Instance.OnPlayerDisconnect += OnPlayerDisconnect;
			ClientConnectionManager.Instance.OnZoneOwnerChange += OnZoneOwnerChange;
			ErenshorCoopMod.OnGameMapLoad += OnGameMapLoad;
		}

		public static void OnDisconnect()
		{
			_zonePlayers.Clear();
			isZoneOwner = false;
			_lastOwnershipZone = null;
			ClientConnectionManager.Instance.OnClientConnect -= OnClientChangeZone;
			ClientConnectionManager.Instance.OnClientSwapZone -= OnClientChangeZone;
			ClientConnectionManager.Instance.OnPlayerDisconnect -= OnPlayerDisconnect;
			ClientConnectionManager.Instance.OnZoneOwnerChange -= OnZoneOwnerChange;
			ErenshorCoopMod.OnGameMapLoad -= OnGameMapLoad;
		}

		private static void OnGameMapLoad(Scene scene)
		{
			_zonePlayers.Clear();
			//SharedNPCSyncManager.Instance.StartCoroutine(SharedNPCSyncManager.Instance.DelayedCheckSim());
			if (scene.name != "FernallaPortal")
			{
				GameHooks.fernallaSpawn1 = false;
				GameHooks.fernallaSpawn2 = false;
				GameHooks.fernallaStats = null;
			}
		}

		public static void OnClientChangeZone(short playerID, string newZone, string prevZone)
		{
			bool isLocalZone = newZone == SceneManager.GetActiveScene().name;
			var player = ClientConnectionManager.Instance.GetPlayerFromID(playerID);

			if (isLocalZone)
			{
				Logging.Log($"add {playerID} to zone");
				_zonePlayers[playerID] = player;
				if (isZoneOwner)
				{
					SharedNPCSyncManager.Instance.StartCoroutine(SharedNPCSyncManager.Instance.DelayedSendMobData(playerID));
				}
				//if(ServerConnectionManager.Instance.IsRunning)
				SharedNPCSyncManager.Instance.StartCoroutine(SharedNPCSyncManager.Instance.DelayedCheckSim());
			}
			else
			{
				Logging.Log($"rem {playerID} from zone");
				_zonePlayers.Remove(playerID);
			}
		}

		public static void OnPlayerDisconnect(short playerID)
		{
			_zonePlayers.Remove(playerID);
		}

		public static void OnZoneOwnerChange(short playerID, string zone, List<short> zonePlayers)
		{
			Logging.Log($"got new zone owner {playerID} {zone}");
			bool previousOwnership = isZoneOwner;

			_zonePlayers.Clear();
			foreach (short p in zonePlayers)
			{
				_zonePlayers[p] = ClientConnectionManager.Instance.GetPlayerFromID(p);
			}

			if (zone == SceneManager.GetActiveScene().name)
			{
				isZoneOwner = playerID == ClientConnectionManager.Instance.LocalPlayerID;
			}

			if (zone == SceneManager.GetActiveScene().name && isZoneOwner && _lastOwnershipZone != zone)
			{
				SharedNPCSyncManager.Instance.StartCoroutine(SharedNPCSyncManager.Instance.TakeOwnershipDelayed(zone));
				_lastOwnershipZone = zone;
				
			}

			bool ownershipWasRemoved = previousOwnership && !isZoneOwner && _lastOwnershipZone == zone;

			if (zone == SceneManager.GetActiveScene().name && !isZoneOwner && !ownershipWasRemoved)
			{
				Logging.Log("Not Owner!");
				ClientNPCSyncManager.Instance.LoadAndDestroySpawns(false);
				_lastOwnershipZone = null;

				foreach(var obj in Variables.entitiesToDisable)
				{
					if(obj != null)
						obj?.SetActive(false);
				}
				Variables.entitiesToDisable.Clear();
			}

			if (zone == SceneManager.GetActiveScene().name && ownershipWasRemoved)
			{
				Logging.Log("Ownership Removed!");
				ClientNPCSyncManager.Instance.LoadSpawns();
				SharedNPCSyncManager.Instance.ConvertOwnedToUnowned();
				_lastOwnershipZone = null;

				foreach (var obj in Variables.entitiesToDisable)
				{
					if (obj != null)
						obj?.SetActive(false);
				}
				Variables.entitiesToDisable.Clear();
			}
		}

		public static void HandlePreSyncs(bool isOwner)
		{
			//var preSyncs = UnityEngine.Object.FindObjectsOfType<GameHooks.PreSyncedEntity>(true);
			foreach (var sync in GameHooks.preSyncList)
			{
				if(isOwner && sync != null && sync.gameObject != null && sync.gameObject.GetComponent<Entity>() == null && sync.id != -1)
				{
					GameHooks.SetNetworked(sync.gameObject, sync.id, 0, CustomSpawnID.PRE_SYNCED);
				}
			}
		}

	}
}
