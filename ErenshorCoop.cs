using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using ErenshorCoop.Client;
using ErenshorCoop.Server;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using ErenshorCoop.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;
using Steamworks;
using System.Collections;

namespace ErenshorCoop
{
	[BepInPlugin("mizuki.coop", "Erenshor Coop", "2.0.0")]
	public class ErenshorCoopMod : BaseUnityPlugin
	{
		public static Action<Scene> OnGameMapLoad;
		public static Action<Scene> OnGameMenuLoad;

		public static SceneChange sceneChange;

		public static ManualLogSource logger;

		private static Harmony harm;

		public static UI.Main ModMain;

		public static List<PluginData> loadedPlugins = new();

		public static System.Version version;

		private GameObject mainGO;
		private GameObject ui;

		public static ConfigFile config;

		public void Awake()
		{
			version = Info.Metadata.Version;
			logger = Logger;
			config = Config;


			if (!SteamAPI.Init())
			{
				Logging.LogError("SteamAPI Init failed.");
			}

			SceneManager.sceneLoaded += OnSceneLoaded;
			OnGameMapLoad += OnGameLoad;
			OnGameMenuLoad += OnMenuLoad;

			mainGO = new("Erenshor Coop");
			mainGO.transform.SetParent(transform);

			ClientConfig.Load(Config);
			ServerConfig.Load(Config);

			mainGO.AddComponent<ClientConnectionManager>();
			mainGO.AddComponent<ServerConnectionManager>();
			mainGO.AddComponent<ClientNPCSyncManager>();
			mainGO.AddComponent<SharedNPCSyncManager>();

			ui = new("CoopUI");
			ModMain = ui.AddComponent<UI.Main>();
			ui.transform.SetParent(mainGO.transform);

			EnableHooks();

			GetLoadedPlugins();

			
			StartCoroutine(DelayedSteamworksInit());

			//For ScriptEngine
			var scene = SceneManager.GetActiveScene();
			//if (scene.name != "LoadScene" && scene.name != "Menu")
			{
				if (sceneChange == null)
					sceneChange = FindObjectOfType<SceneChange>();

				if (sceneChange == null) return;

				if (sceneChange.Player != null && ClientConnectionManager.Instance.LocalPlayer != null)
				{
					var hasPlayer = sceneChange.Player.GetComponent<PlayerSync>();
					if (hasPlayer != null) DestroyImmediate(hasPlayer);


					//Networking.localPlayer = sceneChange.Player.gameObject.AddComponent<PlayerSync>();
				}
				else if (ClientConnectionManager.Instance.LocalPlayer == null)
				{
					ClientConnectionManager.Instance.LocalPlayer = sceneChange.Player.gameObject.AddComponent<PlayerSync>();
					//Logging.LogError("Could not find Player object. Try changing scenes?");
				}
			}

		}

		public IEnumerator DelayedSteamworksInit()
		{
			yield return new WaitForSeconds(2);
			Steamworks.SteamAPI.Init();
			Steam.Lobby.Init();
		}

		public void OnApplicationQuit()
		{

		}

		private void OnDestroy()
		{
			Steam.Lobby.Cleanup();
			Steam.Networking.Cleanup();

			OnGameMapLoad -= OnGameLoad;
			OnGameMenuLoad -= OnMenuLoad;

			SceneManager.sceneLoaded -= OnSceneLoaded;
			if (ClientConnectionManager.Instance != null)
				ClientConnectionManager.Instance.Disconnect();
			if(ServerConnectionManager.Instance != null)
				ServerConnectionManager.Instance.Disconnect();

			var p = FindObjectOfType<PlayerSync>();
			if (p != null)
				Destroy(p);

			Destroy(ui);
			Destroy(mainGO);


			harm.UnpatchSelf();

			//Make absolutely sure there's no more syncs
			var s = FindObjectsOfType<NPCSync>();
			foreach (var n in s)
				Destroy(n);
			var sn = FindObjectsOfType<SimSync>();
			foreach (var n in sn)
				Destroy(n);
		}


		private void GetLoadedPlugins()
		{
			loadedPlugins.Clear();
			foreach (var plugin in Chainloader.PluginInfos.Values)
			{
				PluginData pluginData = new()
				{
					name = plugin.Metadata.Name,
					version = plugin.Metadata.Version
				};
				loadedPlugins.Add(pluginData);
			}
		}

		private void OnMenuLoad(Scene scene)
		{
			if (ClientConnectionManager.Instance == null) return;

			if (ClientConnectionManager.Instance.LocalPlayer == null || sceneChange.Player == null) return;

			Destroy(ClientConnectionManager.Instance.LocalPlayer);
			ClientConnectionManager.Instance.LocalPlayer = null;
			Steam.Networking.Cleanup();
		}

		private void OnGameLoad(Scene scene)
		{
			if(sceneChange == null)
				sceneChange = FindObjectOfType<SceneChange>();

			if (sceneChange == null) return;

			if(sceneChange.Player != null && ClientConnectionManager.Instance.LocalPlayer == null)
			{
				ClientConnectionManager.Instance.LocalPlayer = sceneChange.Player.gameObject.AddComponent<PlayerSync>();
			}else if(ClientConnectionManager.Instance.LocalPlayer == null){
				Logging.LogError("Could not find Player object. Try changing scenes?");
			}

			StartCoroutine(SteamCheckStart());
		}

		public IEnumerator SteamCheckStart()
		{
			yield return new WaitForSeconds(3);
			Steam.Lobby.CheckForGameStart();
		}

		public void Update()
		{
			PacketManager.ExtremePoolNoodleAction();
			Steam.Networking.Update();
		}

		public void EnableHooks()
		{
			harm = new Harmony("mizuki.coop");
			GameHooks.CreateHooks(harm);
			CommandHandler.CreateHooks(harm);
		}


		private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			if (scene.name == "LoadScene" || scene.name == "Menu")
			{
				OnGameMenuLoad?.Invoke(scene);
			}
			else
			{
				OnGameMapLoad?.Invoke(scene);
			}
		}

		public static void CreatePrefixHook(Type origType, string origName, Type newType, string newName)
		{
			try
			{
				var patch = new HarmonyMethod(AccessTools.Method(newType, newName));
				var orig = AccessTools.Method(origType, origName);
				harm.Patch(orig, patch);
			} catch {} //something will tell us anyway
		}
		public static void CreatePostHook(Type origType, string origName, Type newType, string newName)
		{
			try
			{
				var patch = new HarmonyMethod(AccessTools.Method(newType, newName));
				var orig = AccessTools.Method(origType, origName);
				harm.Patch(orig, null, patch);
			} catch {}
		}

		public static void CreatePrefixHook(Type origType, string origName, Type newType, string newName, Type[] paramTypes)
		{
			try
			{
				var patch = new HarmonyMethod(AccessTools.Method(newType, newName));
				var orig = AccessTools.Method(origType, origName, paramTypes);
				harm.Patch(orig, patch);
			} catch {}
		}

		public static void CreatePostHook(Type origType, string origName, Type newType, string newName, Type[] paramTypes)
		{
			try
			{
				var patch = new HarmonyMethod(AccessTools.Method(newType, newName));
				var orig = AccessTools.Method(origType, origName, paramTypes);
				harm.Patch(orig, null, patch);
			}
			catch { }
		}
		public static void UnPatchTranspiler(Type origType, string origName, Type newType, string newName)
		{
			try
			{
				MethodInfo orig = AccessTools.Method(origType, origName);
				MethodInfo trans = AccessTools.Method(newType, newName);

				harm.Unpatch(orig, trans);
			}catch{}
		}

		public static void CreateTranspilerHook(Type origType, string origName, Type newType, string newName, Type[] paramTypes)
		{
			try
			{
				var orig = AccessTools.Method(origType,  origName, paramTypes);
				var trans = new HarmonyMethod(AccessTools.Method(newType, newName));
				harm.Patch(orig, transpiler: trans);
			}
			catch{}
		}

		public static void CreateTranspilerHook(Type origType, string origName, Type newType, string newName)
		{
			try
			{
				var orig = AccessTools.Method(origType, origName);
				var trans = new HarmonyMethod(AccessTools.Method(newType, newName));
				harm.Patch(orig, transpiler: trans);
			}
			catch { }
		}

		public static Class ClassID2Class(byte _class)
		{
			switch(_class)
			{
				case 0:	return GameData.ClassDB.Warrior;
				case 1:	return GameData.ClassDB.Druid;
				case 2: return GameData.ClassDB.Duelist;
				case 3: return GameData.ClassDB.Arcanist;
			}

			return null;
		}

		public static byte Class2ClassID(Class _class)
		{
			if(_class == GameData.ClassDB.Warrior) return 0;
			if(_class == GameData.ClassDB.Druid) return 1;
			if(_class == GameData.ClassDB.Duelist) return 2;
			if(_class == GameData.ClassDB.Arcanist) return 3;
			return byte.MaxValue;
		}

		public struct PluginData
		{
			public string name;
			public System.Version version;
			public int diff;
			public System.Version other;
		}
	}
}