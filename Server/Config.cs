using BepInEx.Configuration;
using System;
using ErenshorCoop.Client;
using ErenshorCoop.Shared;
using ErenshorCoop.Shared.Packets;
using System.Collections.Generic;
using System.Linq;
using Steamworks;

namespace ErenshorCoop.Server
{
	public static class ServerConfig
	{
		public static ConfigEntry<float> EntitySyncDistance;
		public static ConfigEntry<bool> IsEntitySyncDistance;
		public static ConfigEntry<bool> IsPVPEnabled;
		public static ConfigEntry<bool> EnableZoneTransfership;
		public static ConfigEntry<bool> EnableModWhitelist;
		public static ConfigEntry<string> ModeratorListRaw;
		public static List<ulong> ModeratorList = new();
		public static ConfigEntry<string> BanListRaw;
		public static List<ulong> BanList = new();

		public static bool clientIsPvpEnabled = false;

		public static void Load(ConfigFile config)
		{
			EnableZoneTransfership = config.Bind(
				"Host Settings",
				"!Enable Zone Transfership",
				true,
				"Lets other players take control of a zone even if the host isn't in it. The host still takes over the zone if they enter."
			);
			EntitySyncDistance = config.Bind(
				"Host Settings",
				"!!Entity Sync Distance",
				40f,
				"Minimum distance to another player to display animations."
			);
			IsEntitySyncDistance = config.Bind(
				"Host Settings",
				"!!!Enable Entity Sync Distance",
				false,
				"If you enable this it can cause visual issues but give lower bandwidth usage."
			);
			IsPVPEnabled = config.Bind(
				"Host Settings",
				"!!!!Enable PVP",
				false,
				"Enables PVP"
			);
			IsPVPEnabled.SettingChanged += OnPVPSettingChanged;

			/*EnableModWhitelist = config.Bind(
				"Host Settings",
				"!!!!!Enable Mod Whitelist",
				false,
				"When this is enabled, only players with the same mods can join you."
			);*/

			ModeratorListRaw = config.Bind(
				"Host Settings",
				"!!!!!!Moderator SteamIDs",
				"",
				"Comma separated list of moderator SteamIDs."
			);
			ModeratorListRaw.SettingChanged += OnModListChanged;

			ModeratorList = ModeratorListRaw.Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
							.Select(s => { return ulong.TryParse(s.Trim(), out var id) ? id : 0UL; })
							.Where(id => id != 0UL)
							.ToList();

			BanListRaw = config.Bind(
				"Host Settings",
				"!!!!!!SteamID Ban List",
				"",
				"Comma separated list of banned SteamIDs."
			);
			BanListRaw.SettingChanged += OnBanListChanged;

			BanList = BanListRaw.Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
							.Select(s => { return ulong.TryParse(s.Trim(), out var id) ? id : 0UL; })
							.Where(id => id != 0UL)
							.ToList();
		}

		private static void OnPVPSettingChanged(object sender, EventArgs e)
		{
			if (!ServerConnectionManager.Instance.IsRunning) return;

			foreach (var player in ClientConnectionManager.Instance.Players)
			{
				player.Value.character.MyFaction = IsPVPEnabled.Value ? Character.Faction.PC : Character.Faction.Player;
				player.Value.character.BaseFaction = player.Value.character.MyFaction;
			}

			PacketManager.GetOrCreatePacket<ServerInfoPacket>(0, PacketType.SERVER_INFO).AddPacketData(ServerInfoType.PVP_MODE, "pvpMode", IsPVPEnabled.Value);
		}

		private static void OnModListChanged(object sender, EventArgs e)
		{
			ModeratorList = ModeratorListRaw.Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
							.Select(s => { return ulong.TryParse(s.Trim(), out var id) ? id : 0UL; })
							.Where(id => id != 0UL)
							.ToList();
		}

		private static void OnBanListChanged(object sender, EventArgs e)
		{
			BanList = BanListRaw.Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
							.Select(s => { return ulong.TryParse(s.Trim(), out var id) ? id : 0UL; })
							.Where(id => id != 0UL)
							.ToList();
		}
	}
}
