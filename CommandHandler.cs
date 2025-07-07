using ErenshorCoop.Client;
using ErenshorCoop.Server;
using HarmonyLib;
using System.Net;


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
				var port = 0;


				//FIXME: Improve
				switch (command)
				{

					case "host" when spl.Length < 2:
						Logging.LogError($"Not enough arguments for \"host\". Usage: /host port");
						return false;
					case "host":
						if(!int.TryParse(spl[1], out port))
						{
							Logging.LogError($"Could not parse port. ({spl[2]})");
						}
						else
						{
							if(ServerConnectionManager.Instance.StartHost(port))
								ClientConnectionManager.Instance.Connect("localhost", port);
						}
						return false;
					case "connect" when spl.Length == 1:
						ClientConnectionManager.Instance.Connect("localhost", 7777);
						return false;
					case "connect" when spl.Length < 3:
						Logging.LogError($"Not enough arguments for \"connect\". Usage: /connect ip port");
						return false;
					case "connect":
						{
						string ip = spl[1];
						if(ip != "localhost" && !IPAddress.TryParse(ip, out _))
						{
							Logging.LogError($"{ip} is not a valid IP Address.");
							return false;
						}

						if (!int.TryParse(spl[2], out port))
						{
							Logging.LogError($"Could not parse port. ({spl[2]})");
						}
						else
						{
							ClientConnectionManager.Instance.Connect(ip, port);
						}
						return false;
						}
					case "disconnect":
						ClientConnectionManager.Instance?.Disconnect();
						ServerConnectionManager.Instance?.Disconnect();
						return false;
				}
			}
			return true;
		}
	}
}
