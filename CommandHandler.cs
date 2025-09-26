using ErenshorCoop.Client;
using ErenshorCoop.Server;
using ErenshorCoop.Shared;
using ErenshorCoop.Shared.Packets;
using HarmonyLib;
using System.Net;
using System.Text.RegularExpressions;
using UnityEngine;


namespace ErenshorCoop
{
	public class CommandHandler
	{
		public static void CreateHooks(Harmony harm)
		{
			ErenshorCoopMod.CreatePrefixHook(typeof(TypeText), "CheckCommands", typeof(CommandHandler), "CheckCommands_Prefix");
		}

		public static bool CheckCommands_Prefix(TypeText __instance)
		{
			string txt = __instance.typed.text;

			if (string.IsNullOrEmpty(txt)) return true;

			if (txt.StartsWith("/"))
			{
				//Split
				string[] spl = txt.Substring(1).Split(' ');
				string command = spl[0].ToLower();

				//FIXME: Improve
				switch (command)
				{
#if DEBUG
					case "warpto":
						foreach(var p in ClientConnectionManager.Instance.Players)
						{
							if(p.Value.name == "Valk")
							{
								GameData.PlayerControl.transform.GetComponent<CharacterController>().enabled = false;
								ClientConnectionManager.Instance.LocalPlayer.transform.position = p.Value.transform.position;
									Logging.Log($"dbg warped");
								GameData.PlayerControl.transform.GetComponent<CharacterController>().enabled = true;
								break;
							}
						}
						break;
#endif
					case "kick":
					case "ban":
						if (!ClientConnectionManager.Instance.IsRunning)
						{
							return true;//Not connected
						}
						if (!Steam.Lobby.isInLobby)
						{
							//Only supported on steam
							Logging.WriteInfoMessage("Moderator commands are only supported using steam lobbies.");

							return false;
						}
						if (spl.Length < 2 || spl.Length > 2)
						{
							Logging.WriteInfoMessage($"Usage: /{command} <name>");
							return false;
						}
						var pln = spl[1].ToLower();
						if (pln == GameData.CurrentCharacterSlot.CharName.ToLower())
						{
							//cant kick/ban self...
							Logging.WriteInfoMessage($"Cannot {command} self!");
							return false;
						}
						if(ServerConnectionManager.Instance.IsRunning)
						{
							ClientConnectionManager.Instance.HandleModCommand((byte)(command == "kick" ? 0 : 1), pln);
						}
						else
						{
							//Send packet
							var pa = PacketManager.GetOrCreatePacket<PlayerRequestPacket>(ClientConnectionManager.Instance.LocalPlayerID, PacketType.PLAYER_REQUEST);
							pa.dataTypes.Add(Request.MOD_COMMAND);
							pa.playerName = pln;
							pa.commandType = (byte)(command == "kick" ? 0 : 1);
						}
						return false;
				}
			}
			return true;
		}
	}
}
