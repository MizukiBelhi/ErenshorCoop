using UnityEngine;
using Steamworks;
using System.Collections.Generic;
using System.Text;
using System;
using ErenshorCoop.Client;
using ErenshorCoop.Server;


namespace ErenshorCoop.Steam
{
	public static class Lobby
	{
		private static Callback<LobbyCreated_t> lobbyCreatedCallback;
		private static Callback<GameLobbyJoinRequested_t> lobbyJoinRequested;
		private static Callback<LobbyChatMsg_t> lobbyChatMsg;
		private static Callback<LobbyMatchList_t> lobbyMatchList;
		private static Callback<GameRichPresenceJoinRequested_t> richJoinRequested;
		private static Callback<LobbyEnter_t> OnLobbyJoined;

		private static string lobbyName = "";
		private static string lobbyPassword = "";

		public static CSteamID playerSteamID;
		public static CSteamID hostSteamID;

		private static CSteamID lobbyID;
		public static bool isInLobby = false;
		public static bool isInit = false;
		public static bool isLobbyHost = false;

		public static void Init()
		{
			if (isInit) return;

			lobbyMatchList = Callback<LobbyMatchList_t>.Create(OnLobbyMatchList);
			lobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnLobbyJoinRequested);
			lobbyChatMsg = Callback<LobbyChatMsg_t>.Create(OnLobbyChatMsg);
			richJoinRequested = Callback<GameRichPresenceJoinRequested_t>.Create(OnRichJoinRequested);
			OnLobbyJoined = Callback<LobbyEnter_t>.Create(OnLobbyJoin);
			isInit = true;
			playerSteamID = SteamUser.GetSteamID();
		}

		public static void CreateLobby(string name, string password, ELobbyType lobbyType, int maxPlayers)
		{
			ClientConnectionManager.Instance?.Disconnect();
			ServerConnectionManager.Instance?.Disconnect();

			if (isInLobby)
				LeaveLobby();

			lobbyCreatedCallback = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
			SteamMatchmaking.CreateLobby(lobbyType, maxPlayers);
			lobbyName = name;
			lobbyPassword = password;
		}

		public static void JoinLobby(CSteamID _lobbyID)
		{
			ClientConnectionManager.Instance?.Disconnect();
			ServerConnectionManager.Instance?.Disconnect();

			if (isInLobby)
				LeaveLobby();

			lobbyID = _lobbyID;

			var hasPass = SteamMatchmaking.GetLobbyData(lobbyID, "hasPassword") == "true";

			//if (!hasPass)
			{
				SteamMatchmaking.JoinLobby(lobbyID);
			}
		}

		public static void OnLobbyJoin(LobbyEnter_t callback)
		{
			if (isLobbyHost) return;

			string mes = "/password ";
			SteamMatchmaking.SendLobbyChatMsg(lobbyID, Encoding.UTF8.GetBytes(mes), mes.Length + 1);
		}

		public static void LeaveLobby()
		{

			if (!isInLobby) return;

			Logging.Log($"LEAVING LOBBY");
			SteamMatchmaking.LeaveLobby(lobbyID);
			isInLobby = false;
			isLobbyHost = false;
			UI.Connect.EnableButtons();
		}



		public static void CheckForGameStart()
		{
			string[] args = Environment.GetCommandLineArgs();

			foreach (string arg in args)
			{
				if (arg.StartsWith("+connect_lobby"))
				{
					string argStr = arg.Substring("+connect_lobby".Length + 1);
					CSteamID lobbyID = new CSteamID(ulong.Parse(argStr));

					JoinLobby(lobbyID);
					break;
				}
			}
		}


		public struct FriendData
		{
			public string name;
			public CSteamID steamID;
			public bool isHost;
		}

		public struct LobbyInfo
		{
			public string name;
			public bool hasPassword;
			public CSteamID lobbyID;
			public int currentPlayers;
			public int maxPlayers;
		}

		public static List<FriendData> GetFriendsList()
		{
			List<FriendData> friends = new();
			int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
			for (int i = 0; i < friendCount; i++)
			{
				CSteamID friendID = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
				string friendName = SteamFriends.GetFriendPersonaName(friendID);
				friends.Add(new() { name=friendName, steamID = friendID });
			}

			return friends;
		}

		public static void InviteFriend(CSteamID lobbyID, CSteamID friendSteamID)
		{
			SteamMatchmaking.InviteUserToLobby(lobbyID, friendSteamID);
		}

		public static void Cleanup()
		{
			Logging.Log($"Cleaning up lobby...");
			if (lobbyCreatedCallback != null)
			{
				lobbyCreatedCallback.Unregister();
				lobbyCreatedCallback.Dispose();
				lobbyCreatedCallback = null;
			}
			if(lobbyJoinRequested != null)
			{
				lobbyJoinRequested.Unregister();
				lobbyJoinRequested.Dispose();
				lobbyJoinRequested = null;
			}
			if(lobbyChatMsg != null)
			{
				lobbyChatMsg.Unregister();
				lobbyChatMsg.Dispose();
				lobbyChatMsg = null;
			}
			if(lobbyMatchList != null)
			{
				lobbyMatchList.Unregister();
				lobbyMatchList.Dispose();
				lobbyMatchList = null;
			}
			if(richJoinRequested != null)
			{
				richJoinRequested.Unregister();
				richJoinRequested.Dispose();
				richJoinRequested = null;
			}
			if (OnLobbyJoined != null)
			{
				OnLobbyJoined.Unregister();
				OnLobbyJoined.Dispose();
				OnLobbyJoined = null;
			}

			LeaveLobby();
			isInit = false;
		}

		public static void OnLobbyCreated(LobbyCreated_t result)
		{
			if (result.m_eResult == EResult.k_EResultOK)
			{
				Logging.Log("Lobby created successfully");
				lobbyID = new(result.m_ulSteamIDLobby);
				isInLobby = true;
				isLobbyHost = true;
				SteamMatchmaking.SetLobbyData(lobbyID, "name", lobbyName);
				SteamMatchmaking.SetLobbyData(lobbyID, "hasPassword", string.IsNullOrEmpty(lobbyPassword) ? "false" : "true");
				SteamMatchmaking.SetLobbyData(lobbyID, "port", "7777");

				Networking.StartHost(7777);
			}
			else
			{
				Logging.Log($"Failed to create lobby: {result.m_eResult}");
				Logging.LogGameMessage($"Failed to create lobby: {result.m_eResult}");
			}
			if (lobbyCreatedCallback != null)
			{
				lobbyCreatedCallback.Unregister();
				lobbyCreatedCallback.Dispose();
				lobbyCreatedCallback = null;
			}
		}

		private static void OnLobbyJoinRequested(GameLobbyJoinRequested_t callback)
		{
			CSteamID lobbyID = callback.m_steamIDLobby;
			Logging.Log($"Received lobby join request {lobbyID}");
			JoinLobby(lobbyID);
		}

		private static void OnRichJoinRequested(GameRichPresenceJoinRequested_t callback)
		{
			if (ulong.TryParse(callback.m_rgchConnect, out ulong lobbyId))
			{
				CSteamID lobbyID = new CSteamID(lobbyId);
				JoinLobby(lobbyID);
			}
		}


		private static void OnLobbyChatMsg(LobbyChatMsg_t callback)
		{
			CSteamID lobbyID = new CSteamID(callback.m_ulSteamIDLobby);
			CSteamID senderID = new CSteamID(callback.m_ulSteamIDUser);
			var lobbyOwnerID = SteamMatchmaking.GetLobbyOwner(lobbyID);

			EChatEntryType entryType;

			byte[] data = new byte[4096];
			int size = SteamMatchmaking.GetLobbyChatEntry(lobbyID, (int)callback.m_iChatID, out senderID, data, data.Length, out entryType);

			//var f = EChatEntryType.

			string message = Encoding.UTF8.GetString(data, 0, size);


			if (lobbyOwnerID == playerSteamID && senderID.m_SteamID != playerSteamID.m_SteamID)
			{
				if (!string.IsNullOrEmpty(lobbyPassword) && message.StartsWith("/password "))
				{
					string providedPassword = message.Substring(10).Trim();
					if (providedPassword != lobbyPassword)
					{
						Logging.Log($"sendin kick");
						string mes = $"{senderID.m_SteamID}:kick";
						SteamMatchmaking.SendLobbyChatMsg(lobbyID, Encoding.UTF8.GetBytes(mes), mes.Length);
					}
					else
					{
						Logging.Log($"sendin ok");
						string mes = $"{senderID.m_SteamID}:canJoin";
						SteamMatchmaking.SendLobbyChatMsg(lobbyID, Encoding.UTF8.GetBytes(mes), mes.Length);
					}
				}else if(string.IsNullOrEmpty(lobbyPassword))
				{
					Logging.Log($"sendin ok");
					string mes = $"{senderID.m_SteamID}:canJoin";
					SteamMatchmaking.SendLobbyChatMsg(lobbyID, Encoding.UTF8.GetBytes(mes), mes.Length);
				}
				return;
			}

			if(senderID == lobbyOwnerID)
			{
				var msSpl = message.Split(':');
				if (!ulong.TryParse(msSpl[0], out ulong stmId))
					return; //malformed

				var cmd = msSpl[1].Trim();

				if (stmId == playerSteamID.m_SteamID)
				{
					Logging.Log($"recv cmd \"{cmd}\"");
					switch (cmd)
					{
						case "kick":
							LeaveLobby();
							break;
						case "canJoin":
							SuccessJoinLobby();
							break;
					}
				}
			}
		}


		public static List<FriendData> GetLobbyMembers()
		{
			List<FriendData> members = new();
			int numMembers = SteamMatchmaking.GetNumLobbyMembers(lobbyID);
			for (int i = 0; i < numMembers; i++)
			{
				CSteamID steamID = SteamMatchmaking.GetLobbyMemberByIndex(lobbyID, i);

				if (steamID.IsValid())
				{
					var name = SteamFriends.GetFriendPersonaName(steamID);
					members.Add(new() { steamID = steamID, isHost = steamID == hostSteamID, name = name });
				}
			}
			return members;
		}

		public static List<CSteamID> GetLobbyMembersSteamID()
		{
			List<CSteamID> members = new();
			int numMembers = SteamMatchmaking.GetNumLobbyMembers(lobbyID);
			for (int i = 0; i < numMembers; i++)
			{
				CSteamID steamID = SteamMatchmaking.GetLobbyMemberByIndex(lobbyID, i);
				if (steamID.IsValid())
				{
					members.Add(steamID);
				}
			}
			return members;
		}

		private static void SuccessJoinLobby()
		{
			hostSteamID = SteamMatchmaking.GetLobbyOwner(lobbyID);
			isInLobby = Networking.ConnectToPeer(hostSteamID, 7777);
		}


		private static Action<uint, List<LobbyInfo>> _lobbyCallback;
		private static void OnLobbyMatchList(LobbyMatchList_t callback)
		{
			uint lobbyCount = callback.m_nLobbiesMatching;

			Logging.Log($"Received {lobbyCount} lobbies.");

			List<LobbyInfo> lobbies = new();

			for (uint i = 0; i < lobbyCount; i++)
			{
				CSteamID lobbyID = SteamMatchmaking.GetLobbyByIndex((int)i);

				var hasPass = SteamMatchmaking.GetLobbyData(lobbyID, "hasPassword")=="true"?true:false;
				var lobbyName = SteamMatchmaking.GetLobbyData(lobbyID, "name");

				var maxPlayer = SteamMatchmaking.GetLobbyMemberLimit(lobbyID);
				var curPlayer = SteamMatchmaking.GetNumLobbyMembers(lobbyID);

				lobbies.Add(new() { lobbyID = lobbyID, name = lobbyName, hasPassword = hasPass, maxPlayers = maxPlayer, currentPlayers = curPlayer });
			}

			string[] dummyNames =
{
    "Chill Vibes Only",
    "Hardcore Gamers",
    "Noobs Welcome",
    "Tryhard Tuesdays",
    "Catgirls Unite",
    "Speedrun Central",
    "Dad Gaming Club",
    "Modded Mayhem",
    "Casual Chaos",
    "Sweaty Saturdays",
    "Goblin Den",
    "Late Night Legends",
    "Coffee & Carnage",
    "Friendly Fire ON",
    "Waifu Warriors",
    "Dungeon Crawlers Anonymous",
    "AimBot Not Included",
    "Only Clutch Moments",
    "Nerf This!",
    "The Lagging Dead",
    "Broken Keyboard Club",
    "Thicc Damage Dealers",
    "Alt+F4 Survivors",
    "Pepega Party",
    "Microphone Screechers",
    "Smurf Patrol",
    "No Scope No Hope",
    "High Ping Heroes",
    "Zero Strategy Squad",
    "We're Not Sweating (We Are)"
};

			for (uint i = 0; i < 19; i++)
			{
				CSteamID lobbyID = new CSteamID((ulong)10000000000000000 + i);
				bool hasPass = UnityEngine.Random.Range(0,10) <= 4;
				string lobbyName = dummyNames[UnityEngine.Random.Range(0, 1000) % dummyNames.Length];

				lobbies.Add(new LobbyInfo
				{
					lobbyID = lobbyID,
					name = lobbyName,
					hasPassword = hasPass,
					maxPlayers = 100,
					currentPlayers = UnityEngine.Random.Range(1, 101)
				});
			}

			_lobbyCallback?.Invoke(lobbyCount, lobbies);
		}

		public static void RequestLobbyList(SteamFilter filter, Action<uint, List<LobbyInfo>> callback)
		{
			_lobbyCallback = callback;

			switch(filter)
			{
				case SteamFilter.DISTANCE_CLOSE:
					SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterClose);
					break;
				case SteamFilter.DISTANCE_FAR:
					SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterFar);
					break;
				case SteamFilter.DISTANCE_WORLD:
					SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
					break;
				case SteamFilter.DISTANCE_DEFAULT:
					SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterDefault);
					break;
				case SteamFilter.FRIENDS_ONLY:
					SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
					break;
			}

			if (filter != SteamFilter.FRIENDS_ONLY)
			{
				SteamMatchmaking.AddRequestLobbyListResultCountFilter(19);
				SteamMatchmaking.RequestLobbyList();
			}
			else
			{
				var frCnt = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
				List<LobbyInfo> lobbies = new();

				for (int i=0;i<frCnt;i++)
				{
					CSteamID steamIDFriend = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
					FriendGameInfo_t fI;
					if (SteamFriends.GetFriendGamePlayed(steamIDFriend, out fI) && fI.m_steamIDLobby.IsValid())
					{

						CSteamID lobbyID = fI.m_steamIDLobby;

						var hasPass = SteamMatchmaking.GetLobbyData(lobbyID, "hasPassword") == "true" ? true : false;
						var lobbyName = SteamMatchmaking.GetLobbyData(lobbyID, "name");

						var maxPlayer = SteamMatchmaking.GetLobbyMemberLimit(lobbyID);
						var curPlayer = SteamMatchmaking.GetNumLobbyMembers(lobbyID);

						lobbies.Add(new() { lobbyID = lobbyID, name = lobbyName, hasPassword = hasPass, maxPlayers = maxPlayer, currentPlayers = curPlayer });
						
					}
				}

				_lobbyCallback?.Invoke((uint)lobbies.Count, lobbies);
			}
		}
	}
	
	public enum SteamFilter
	{
		DISTANCE_CLOSE,
		DISTANCE_FAR,
		DISTANCE_DEFAULT,
		DISTANCE_WORLD,
		FRIENDS_ONLY,

	}
}
