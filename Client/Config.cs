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

		private static ConfigFile config;
		public static void Load(ConfigFile configFile)
		{
			config = configFile;

			SavedIP = config.Bind(
				"Client Settings",
				"IP",
				"",
				""
			);
			SavedPort = config.Bind(
				"Client Settings",
				"Port",
				"",
				""
			);
		}

		public static void Save()
		{
			config?.Save();
		}
	}
}
