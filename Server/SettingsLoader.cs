using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErenshorCoop.Server
{
	static class SettingsLoader
	{
		static Dictionary<string, CoopSetting> schema = new Dictionary<string, CoopSetting>()
		{
			{ "lobbyName",     new CoopSetting("lobbyName",     typeof(string), "My Lobby", "Name of the hosted lobby") },
			{ "lobbyPassword", new CoopSetting("lobbyPassword", typeof(string), "",         "Lobby password (empty = none)") },
			{ "lobbyType",     new CoopSetting("lobbyType",     typeof(string), "Public",   "Lobby visibility: Public / Invite Only / Friends Only / Private") },
			{ "maxPlayers",    new CoopSetting("maxPlayers",    typeof(int),    4,          "Max players (1-100)") },
		};

		static Dictionary<string, object> _values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

		public static void Load()
		{
			string exeDir = AppDomain.CurrentDomain.BaseDirectory;
			string path = Path.Combine(exeDir, "coop_settings.txt");

			foreach (var kv in schema)
				_values[kv.Key] = kv.Value.DefaultValue;

			if (!File.Exists(path))
			{
				WriteDefaultFile(path);
				return;
			}

			foreach (var rawLine in File.ReadAllLines(path))
			{
				string line = rawLine.Trim();
				if (string.IsNullOrWhiteSpace(line)) continue;
				if (line.StartsWith("//")) continue;
				if (line.StartsWith("#")) continue;

				var parts = line.Split('=');
				if (parts.Length != 2) continue;

				string key = parts[0].Trim();
				string val = parts[1].Trim();

				if (!schema.TryGetValue(key, out var setting))
					continue;

				if (setting.ValueType == typeof(int))
				{
					if (int.TryParse(val, out int v))
					{
						if (setting.Key == "maxPlayers")
							v = Clamp(v, 1, 100);

						_values[key] = v;
					}
				}
				else if (setting.ValueType == typeof(bool))
				{
					_values[key] = ParseBool(val);
				}
				else
				{
					_values[key] = val;
				}
			}
		}

		private static int Clamp(int value, int min, int max)
		{
			if (value < min) return min;
			if (value > max) return max;
			return value;
		}
		private static bool ParseBool(string v)
		{
			v = v.ToLower();
			if (v == "true" || v == "1" || v == "yes") return true;
			if (v == "false" || v == "0" || v == "no") return false;
			return false;
		}

		private static void WriteDefaultFile(string path)
		{
			using (var sw = new StreamWriter(path))
			{
				sw.WriteLine("# Coop headless configuration file");

				foreach (var kv in schema)
				{
					var s = kv.Value;
					sw.WriteLine($"// {s.Description}");
					sw.WriteLine($"{s.Key} = {s.DefaultValue}");
				}
			}
		}

		public static string GetString(string key) => (string)_values[key];
		public static int GetInt(string key) => (int)_values[key];
		public static bool GetBool(string key) => (bool)_values[key];

		public static ELobbyType GetLobbyType()
		{
			string type = GetString("lobbyType").ToLower();

			return type switch
			{
				"invite only" => ELobbyType.k_ELobbyTypePrivate,
				"private" => ELobbyType.k_ELobbyTypePrivate,
				"friends only" => ELobbyType.k_ELobbyTypeFriendsOnly,
				"public" or _ => ELobbyType.k_ELobbyTypePublic,
			};
		}
	}

	public class CoopSetting
	{
		public string Key;
		public Type ValueType;
		public object DefaultValue;
		public string Description;

		public CoopSetting(string key, Type type, object defaultValue, string desc)
		{
			Key = key;
			ValueType = type;
			DefaultValue = defaultValue;
			Description = desc;
		}
	}
}
