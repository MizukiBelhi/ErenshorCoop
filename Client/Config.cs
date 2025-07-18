using BepInEx.Configuration;
using System;
using UnityEngine;

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
		public static ConfigEntry<bool> DisplayCompassMarker;
		public static ConfigEntry<bool> DisplayOffScreenMarker;
		public static ConfigEntry<bool> MarkersOnlyGroup;
		public static ConfigEntry<Color> OffScreenMarkerColor;

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

			DisplayCompassMarker = config.Bind(
				"Client Settings",
				"!!!!!Display Compass Markers",
				true,
				"Displays a marker on the compass pointing towards players"
			);
			DisplayOffScreenMarker = config.Bind(
				"Client Settings",
				"!!!!!Display Off-Screen Markers",
				true,
				"Displays a marker on the screen if a player is off screen"
			);
			MarkersOnlyGroup = config.Bind(
				"Client Settings",
				"!!!!!Off-Screen Markers group only",
				true,
				"When enabled, off-screen markers are only displayed for group"
			);
			OffScreenMarkerColor = config.Bind(
				"Client Settings",
				"!!!!!Off-Screen Marker Color",
				Color.green,
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
