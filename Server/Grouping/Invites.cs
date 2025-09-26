using ErenshorCoop.Client;
using ErenshorCoop.Client.Grouping;
using ErenshorCoop.Shared;
using ErenshorCoop.Shared.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErenshorCoop.Server.Grouping
{
	public class Invites
	{
		public static Dictionary<short, PendingInvite> pendingInvites = new();
		private static short curInviteID = 0;

		public class PendingInvite
		{
			public short leaderID;
			public short inviteeID;
			public short groupID;
		}
		private static PendingInvite GetPendingInvite(short inviteID)
		{
			if (pendingInvites.TryGetValue(inviteID, out var invite))
				return invite;
			return null;
		}

		public static void Cleanup()
		{
			pendingInvites.Clear();
			curInviteID = 0;
		}

		public static void InvitePlayer(short leader, short player, bool isLeaderLocalPlayer, bool isPlayerLocalPlayer, bool isSim)
		{
			short groupID = ServerGroup.GetGroupIDFromPlayer(leader);
			short groupIDPlayer = ServerGroup.GetGroupIDFromPlayer(player);

			Entity netPlayer = null;
			if (!isSim)
				netPlayer = ClientConnectionManager.Instance.GetPlayerFromID(player);
			else
			{
				netPlayer = ClientNPCSyncManager.Instance.NetworkedSims.ContainsKey(player) ? ClientNPCSyncManager.Instance.NetworkedSims[player] : null;
				if (netPlayer == null)
					netPlayer = SharedNPCSyncManager.Instance.sims.ContainsKey(player) ? SharedNPCSyncManager.Instance.sims[player] : null;
			}

			if (netPlayer == null)
			{
				Logging.LogError($"There was an issue inviting a {(isSim ? "sim" : "player")} ID: {player}");
				return;
			}

			var leaderPlayer = ClientConnectionManager.Instance.GetPlayerFromID(leader);


			if (groupIDPlayer != -1)
			{
				if (SharedGroup.IsPlayerHost(leaderPlayer.entityID))
				{
					Logging.WriteInfoMessage($"Player {netPlayer.entityName} is already in a group.");
				}
				else
				{
					PacketManager.GetOrCreatePacket<PlayerMessagePacket>(leaderPlayer.entityID, PacketType.PLAYER_MESSAGE)
						.SetTarget(leaderPlayer)
						.SetData("message", $"Player {netPlayer.entityName} is already in a group.")
						.SetData("messageType", MessageType.INFO);
				}
				return;
			}

			if (!isSim)
			{
				var pInvite = new PendingInvite();
				pInvite.groupID = groupID;
				pInvite.inviteeID = player;
				pInvite.leaderID = leader;

				pendingInvites.Add(++curInviteID, pInvite);

				if (SharedGroup.IsPlayerHost(netPlayer.entityID))
				{
					var p = new ServerGroupPacket();
					p.dataTypes.Add(GroupDataType.INVITE);
					p.leaderID = leader;
					p.inviteID = curInviteID;
					p.entityID = netPlayer.entityID;
					ClientGroup.HandlePacket(p); //Hosts need to send packets to themselves
				}
				else
				{

					PacketManager.GetOrCreatePacket<ServerGroupPacket>(netPlayer.entityID, PacketType.SERVER_GROUP)
						.SetTarget(netPlayer)
						.AddPacketData(GroupDataType.INVITE, "leaderID", leader)
						.SetData("inviteID", curInviteID);
				}
			}
			else
			{
				DoInvite(groupID, leaderPlayer, netPlayer);
			}
		}

		private static void DoInvite(short groupID, Entity leader, Entity invPlayer)
		{
			SharedGroup.Group group;

			if (groupID == -1)
			{
				group = new SharedGroup.Group
				{
					leaderID = leader.entityID,
					groupList = new()
					{
						new SharedGroup.Member(0, leader.entityID, false),
						new SharedGroup.Member(1, invPlayer.entityID, invPlayer.type == EntityType.SIM)
					},
					internalList = new()
					{
						leader
					}
				};
				if (invPlayer.type != EntityType.SIM)
					group.internalList.Add(invPlayer);
				ServerGroup.AddGroup(group);

				foreach (var m in group.groupList)
					Logging.Log($"{m.ToString()}");
			}
			else
			{
				group = ServerGroup.GetGroup(groupID);
				if(group == null)
				{
					Logging.LogError($"There was an issue adding player {invPlayer.entityName} to group ID {groupID}");
					return;
				}
				var lastSlot = group.GetOpenSlot();
				group.groupList.Add(new SharedGroup.Member(lastSlot, invPlayer.entityID, invPlayer.type == EntityType.SIM));
				if (invPlayer.type != EntityType.SIM)
					group.internalList.Add(invPlayer);
			}

			//actual group leader
			var gLeader = ClientConnectionManager.Instance.GetPlayerFromID(group.leaderID);

			//Send a message to the owner of the sim to set the follow target
			if (invPlayer.type == EntityType.SIM)
			{
				if (invPlayer is SimSync)
				{
					var p = new ServerGroupPacket();
					p.dataTypes.Add(GroupDataType.SIM_FOLLOW);
					p.entityID = ClientConnectionManager.Instance.LocalPlayerID;
					p.followTargetID = group.leaderID;
					p.simID = invPlayer.entityID;
					ClientGroup.HandlePacket(p); //Hosts need to send packets to themselves
				}
				else
				{
					NetworkedSim s = ((NetworkedSim)invPlayer);

					if (s != null && s.ownerID != -1)
					{
						Entity owner = ClientConnectionManager.Instance.GetPlayerFromID(s.ownerID);
						if (owner != null)
						{
							PacketManager.GetOrCreatePacket<ServerGroupPacket>(owner.entityID, PacketType.SERVER_GROUP)
							.SetTarget(owner)
							.AddPacketData(GroupDataType.SIM_FOLLOW, "followTargetID", group.leaderID)
							.SetData("simID", invPlayer.entityID);
						}
					}
				}
			}

			foreach (var player in group.internalList)
			{
				var message = player.entityID == invPlayer.entityID ? $"You have joined {gLeader.entityName}'s group." : $"{(invPlayer.type == EntityType.SIM ? "Sim" : "Player")} {invPlayer.entityName} has joined your group.";
				if (SharedGroup.IsPlayerHost(player.entityID))
				{
					var p = new ServerGroupPacket();
					p.dataTypes.Add(GroupDataType.MEMBER_LIST);
					p.groupList = group.groupList;
					p.leaderID = group.leaderID;
					p.entityID = player.entityID;
					ClientGroup.HandlePacket(p); //Hosts need to send packets to themselves
					Logging.WriteInfoMessage(message);
				}
				else
				{
					PacketManager.GetOrCreatePacket<ServerGroupPacket>(player.entityID, PacketType.SERVER_GROUP)
						.SetTarget(player)
						.AddPacketData(GroupDataType.MEMBER_LIST, "groupList", group.groupList)
						.SetData("leaderID", group.leaderID);

					PacketManager.GetOrCreatePacket<PlayerMessagePacket>(player.entityID, PacketType.PLAYER_MESSAGE)
						.SetTarget(player)
						.SetData("message", message)
						.SetData("messageType", MessageType.INFO);
				}
			}

		}
		public static void OnAcceptInvite(short inviteID)
		{
			var pendingInvite = GetPendingInvite(inviteID);

			if (pendingInvite == null) return;

			short groupID = pendingInvite.groupID;
			short leaderID = pendingInvite.leaderID;
			short playerID = pendingInvite.inviteeID;
			var networkedPlayer = ClientConnectionManager.Instance.GetPlayerFromID(playerID);
			var networkedLeader = ClientConnectionManager.Instance.GetPlayerFromID(leaderID);
			DoInvite(groupID, networkedLeader, networkedPlayer);
			pendingInvites.Remove(inviteID);
		}

		public static void OnDeclineInvite(short inviteID)
		{
			var pendingInvite = GetPendingInvite(inviteID);

			if (pendingInvite == null) { return; }

			var player = ClientConnectionManager.Instance.GetPlayerFromID(pendingInvite.inviteeID);
			var leader = ClientConnectionManager.Instance.GetPlayerFromID(pendingInvite.leaderID);


			if (SharedGroup.IsPlayerHost(ClientConnectionManager.Instance.LocalPlayerID))
			{
				Logging.WriteInfoMessage($"Player {player.entityName} has declined your group invite.");
			}
			else
			{

				PacketManager.GetOrCreatePacket<PlayerMessagePacket>(ClientConnectionManager.Instance.LocalPlayerID, PacketType.PLAYER_MESSAGE)
					.SetTarget(leader)
					.SetData("message", $"Player {player.entityName} has declined your group invite.")
					.SetData("messageType", MessageType.INFO);
			}

			pendingInvites.Remove(inviteID);
		}

		private static void RemovePlayer(short playerID, short groupID, GroupLeaveReason reason, bool isSim)
		{
			bool isLocalSim = true;

			Entity playerSc = null;
			if (!isSim)
				playerSc = ClientConnectionManager.Instance.GetPlayerFromID(playerID);
			else
			{
				playerSc = ClientNPCSyncManager.Instance.GetEntityFromID(playerID, true);
				if (playerSc == null)
				{
					playerSc = SharedNPCSyncManager.Instance.GetEntityFromID(playerID, true);
					isLocalSim = false;
				}
			}




			if (!isSim && playerSc != null && (playerSc.peer != null || playerSc.steamID != null)) //Happens when they disconnect
			{
				PacketManager.GetOrCreatePacket<PlayerMessagePacket>(playerSc.entityID, PacketType.PLAYER_MESSAGE)
					.SetTarget(playerSc)
					.SetData("message", reason == GroupLeaveReason.LEFT ? $"You've left the group." : $"You've been removed from the group.")
					.SetData("messageType", MessageType.INFO);


				var l = new List<SharedGroup.Member>();

				PacketManager.GetOrCreatePacket<ServerGroupPacket>(playerSc.entityID, PacketType.SERVER_GROUP)
					.SetTarget(playerSc)
					.AddPacketData(GroupDataType.MEMBER_LIST, "groupList", l)
					.SetData("leaderID", (short)-1);
			}



			if (groupID != -1)
			{
				SharedGroup.Member m = ServerGroup.GetMemberFromPlayer(playerID, isSim);

				if (m == null) return;

				var group = ServerGroup.GetGroup(groupID);
				if(group == null) //Should never happen
				{
					Logging.LogError($"There was an issue removing player ID {playerID} from group ID {groupID}");
					return;
				}

				group.groupList.Remove(m);
				if (!isSim)
				{
					if (!group.internalList.Remove(playerSc))
					{
						//hardcore remove everything null or if it has the playerid
						for (int i = group.internalList.Count - 1; i >= 0; i--)
						{
							var gm = group.internalList[i];
							if (gm == null || gm.entityID == playerID)
								group.internalList.RemoveAt(i);
						}
					}
				}

				var mes = "";
				if (playerSc != null && playerSc.peer != null)
					mes = reason == GroupLeaveReason.LEFT ? $"Player {playerSc.entityName} has left your group." : $"Player {playerSc.entityName} removed from group.";
				else
					mes = "A Player has left your group.";

				if (m.isSim)
					mes = $"Sim {playerSc.name} has been removed from the group.";


				//Send a message to the owner of the sim to set the follow target
				if (m.isSim)
				{
					if (isLocalSim)
					{
						NetworkedSim s = ((NetworkedSim)playerSc);

						if (s != null && s.ownerID != -1)
						{
							Entity owner = ClientConnectionManager.Instance.GetPlayerFromID(s.ownerID);
							if (owner != null)
							{
								PacketManager.GetOrCreatePacket<ServerGroupPacket>(owner.entityID, PacketType.SERVER_GROUP)
								.SetTarget(owner)
								.AddPacketData(GroupDataType.SIM_FOLLOW, "followTargetID", (short)-1)
								.SetData("simID", playerSc.entityID);
							}
						}
					}
					else
					{
						if (playerSc != null)
						{
							var p = new ServerGroupPacket();
							p.dataTypes.Add(GroupDataType.SIM_FOLLOW);
							p.entityID = ClientConnectionManager.Instance.LocalPlayerID;
							p.followTargetID = -1;
							p.simID = playerSc.entityID;
							ClientGroup.HandlePacket(p); //Hosts need to send packets to themselves
						}
					}
				}

				foreach (var nPlayer in group.internalList)
				{
					if (SharedGroup.IsPlayerHost(nPlayer.entityID))
					{
						//Logging.Log($"sending host {group.groupList.Count}");
						var p = new ServerGroupPacket();
						p.dataTypes.Add(GroupDataType.MEMBER_LIST);
						p.groupList = group.groupList;
						p.leaderID = group.leaderID;
						p.entityID = nPlayer.entityID;
						ClientGroup.HandlePacket(p); //Hosts need to send packets to themselves
						Logging.WriteInfoMessage(mes);
					}
					else
					{
						//Logging.Log($"sending pl {group.groupList.Count}");
						PacketManager.GetOrCreatePacket<ServerGroupPacket>(nPlayer.entityID, PacketType.SERVER_GROUP)
						.SetTarget(nPlayer)
						.AddPacketData(GroupDataType.MEMBER_LIST, "groupList", group.groupList)
						.SetData("leaderID", group.leaderID);


						PacketManager.GetOrCreatePacket<PlayerMessagePacket>(nPlayer.entityID, PacketType.PLAYER_MESSAGE)
							.SetTarget(nPlayer)
							.SetData("message", mes)
							.SetData("messageType", MessageType.INFO);
					}

				}

				if (group.groupList.Count <= 1)
				{
					//group basically disbanded
					ServerGroup.RemoveGroup(groupID);
				}

			}
		}

		public static void HandlePacket(GroupDataType dt, GroupPacket packet)
		{
			short leaderID = packet.entityID;
			short playerID = packet.playerID;

			switch (dt)
			{
				case GroupDataType.INVITE:
					InvitePlayer(leaderID, playerID, leaderID == ClientConnectionManager.Instance.LocalPlayerID, playerID == ClientConnectionManager.Instance.LocalPlayerID, packet.isSim);
					break;
				case GroupDataType.ACCEPT_DECLINE:
					bool hasAcceptedInvite = packet.inviteAccept;
					short inviteID = packet.inviteID;

					if (hasAcceptedInvite) OnAcceptInvite(inviteID);
					else OnDeclineInvite(inviteID);
					break;
				case GroupDataType.REMOVE:
					RemovePlayer(playerID, ServerGroup.GetGroupIDFromPlayer(playerID), packet.reason, packet.isSim);
					break;

			}
		}
	}
}
