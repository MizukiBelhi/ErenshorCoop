using BepInEx.Configuration;
using System;
using ErenshorCoop.Client;
using ErenshorCoop.Shared;
using ErenshorCoop.Shared.Packets;

namespace ErenshorCoop.Client
{
	public static class ClientConfig
	{
		public static ConfigEntry<string> SavedIP;
		public static ConfigEntry<string> SavedPort;
		public static ConfigEntry<bool> WeatherSync;
		public static ConfigEntry<bool> ItemDropConfirm;
		public static ConfigEntry<bool> DisplayMetrics;
		public static ConfigEntry<bool> CloseMenu;

		private static ConfigFile config;
		public static void Load(ConfigFile configFile)
		{
			config = configFile;

			WeatherSync = config.Bind(
				"Client Settings",
				"!Sync Weather",
				true,
				""
			);

			ItemDropConfirm = config.Bind(
				"Client Settings",
				"!!Drop Confirmation",
				true,
				""
			);
			DisplayMetrics = config.Bind(
				"Client Settings",
				"!!!Show Metrics (ping, etc)",
				false,
				""
			);
			CloseMenu = config.Bind(
				"Client Settings",
				"!!!!Close Menu On Connect",
				false,
				""
			);

			SavedIP = config.Bind(
				"Client Settings",
				"IP",
				"",
				""
			);
			SavedPort = config.Bind(
				"Client Settings",
				"Port",
				"7777",
				""
			);
			DisplayMetrics.SettingChanged += OnMetricsSettingChanged;

		}

		public static void Save()
		{
			config?.Save();
		}

		private static void OnMetricsSettingChanged(object sender, EventArgs e)
		{
			if (!ClientConnectionManager.Instance.IsRunning) return;

			UI.Main.statsPanel?.SetActive(DisplayMetrics.Value);
		}
	}
}
