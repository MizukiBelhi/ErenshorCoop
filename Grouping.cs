using System;
using System.Collections.Generic;
using ErenshorCoop.Client;
using ErenshorCoop.Server;
using ErenshorCoop.Shared;
using ErenshorCoop.Shared.Packets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace ErenshorCoop
{
	public class Grouping
	{
		public static Dictionary<short, Group> groups = new();
		public static Dictionary<short, PendingInvite> pendingInvites = new();

		public static Group currentGroup = new();
		public static short ClientInviteID = -1;

		private static short serverGroupID = -1;
		private static short curInviteID = -1;

		public struct Group
		{
			public List<Member> groupList;
			public short leaderID;
			public List<Entity> internalList;
		}

		public struct Member : IEquatable<Member>
		{
			public short entityID;
			public bool isSim;

			public bool Equals(Member other)
			{
				return entityID == other.entityID && isSim == other.isSim;
			}

			public override bool Equals(object obj)
			{
				return obj is Member other && Equals(other);
			}

			public override int GetHashCode()
			{
				unchecked { return ( entityID.GetHashCode() * 397 ) ^ isSim.GetHashCode(); }
			}

			public override string ToString()
			{
				return $"Member(entityID: {entityID}, isSim: {isSim})";
			}
		}

		public struct PendingInvite
		{
			public short leaderID;
			public short inviteeID;
			public short groupID;
		}

		private static PendingInvite? GetPendingInvite(short inviteID)
		{
			foreach (var inv in pendingInvites)
			{
				if(inv.Key == inviteID)
					return pendingInvites[inviteID];
			}

			return null;
		}

		private static short GetGroupIDFromPlayer(short playerID)
		{
			foreach (var group in groups)
			{
				foreach(Member p in group.Value.groupList)
					if(p.entityID == playerID)
						return group.Key;
			}

			return -1;
		}

		private static Member? GetMemberFromPlayer(short playerID, bool isSim = false)
		{
			foreach (var group in groups)
			{
				foreach (Member p in group.Value.groupList)
					if (p.entityID == playerID && p.isSim == isSim)
						return p;
			}

			return null;
		}

		public static bool IsPlayerInGroup(short playerID, bool isSim)
		{
			if (currentGroup.groupList == null || currentGroup.groupList.Count == 0)
				return false; //Not in a group

			foreach (var members in currentGroup.groupList)
			{
				if(members.entityID == playerID && members.isSim == isSim)
					return true;
			}
			return false;
		}

		public static bool HasGroup => currentGroup.groupList is {Count: > 0 };

		public static bool IsLocalLeader()
		{
			return currentGroup.leaderID == ClientConnectionManager.Instance.LocalPlayerID;
		}

		public static void Cleanup()
		{
			groups.Clear();
			pendingInvites.Clear();
			currentGroup = new();
			currentGroup.leaderID = -1;
			serverGroupID = -1;
			curInviteID = -1;
			ClientInviteID = -1;
			ForceClearGroup();
		}

		public static void ServerInvitePlayer(short leader, short player, bool isLeaderLocalPlayer, bool isPlayerLocalPlayer, bool isSim)
		{
			short groupID = GetGroupIDFromPlayer(leader);
			short groupIDPlayer = GetGroupIDFromPlayer(player);

			Entity netPlayer = null;
			if(!isSim)
				netPlayer = ClientConnectionManager.Instance.GetPlayerFromID(player);
			else
			{
				netPlayer = ClientNPCSyncManager.Instance.NetworkedSims.ContainsKey(player) ? ClientNPCSyncManager.Instance.NetworkedSims[player] : null;
				if (netPlayer == null)
					netPlayer = SharedNPCSyncManager.Instance.sims.ContainsKey(player) ? SharedNPCSyncManager.Instance.sims[player] : null;
			}

			if(netPlayer == null)
			{
				Logging.LogError($"There was an issue inviting a {(isSim ? "sim" : "player")} ID: {player}");
				return;
			}

			var leaderPlayer = ClientConnectionManager.Instance.GetPlayerFromID(leader);


			if (groupIDPlayer != -1)
			{

				PacketManager.GetOrCreatePacket<PlayerMessagePacket>(leaderPlayer.entityID, PacketType.PLAYER_MESSAGE)
					.SetTarget(leaderPlayer)
					.SetData("message",     $"Player {netPlayer.entityName} is already in a group.")
					.SetData("messageType", MessageType.INFO);
				return;
			}

			if (!isSim)
			{
				var pInvite = new PendingInvite();
				pInvite.groupID = groupID;
				pInvite.inviteeID = player;
				pInvite.leaderID = leader;

				pendingInvites.Add(++curInviteID, pInvite);

				PacketManager.GetOrCreatePacket<ServerGroupPacket>(netPlayer.entityID, PacketType.SERVER_GROUP)
					.SetTarget(netPlayer)
					.AddPacketData(GroupDataType.INVITE, "leaderID", leader)
					.SetData("inviteID", curInviteID);
			}
			else
			{
				ServerDoInvite(groupID, leaderPlayer, netPlayer);
			}
		}

		/*public static void ServerInviteSim(short leaderID, int simIndex)
		{
			short groupID = GetGroupIDFromPlayer(leaderID);

			var leaderPlayer = ClientConnectionManager.Instance.GetPlayerFromID(leaderID);

			//Logging.Log($"Adding sim with {simIndex}");

			var s = GameData.SimMngr.Sims[simIndex];
			if (s.MyAvatar.GetComponent<NPCSync>() == null)
			{
				//SharedNPCSyncManager.Instance.ServerSpawnSim(s.MyAvatar.gameObject, simIndex);
			}

			short simEntId = s.MyAvatar.GetComponent<NPCSync>().entityID;

			Group group;

			if (groupID == -1)
			{

				group = new Group
				{
					leaderID = leaderID,
					groupList = new()
					{
						new Member(){entityID = leaderID,isSim = false},
						new Member(){entityID = simEntId,isSim = true, simIndex = (short)simIndex}
					},
					internalList = new()
					{
						leaderPlayer,
					}
				};
				groups.Add(++serverGroupID, group);
			}
			else
			{
				group = groups[groupID];
				group.groupList.Add(new Member() { entityID = simEntId, isSim = true, simIndex = (short)simIndex });
			}

			//actual group leader
			var gLeader = ClientConnectionManager.Instance.GetPlayerFromID(group.leaderID);

			foreach (var player in group.internalList)
			{
				PacketManager.GetOrCreatePacket<ServerGroupPacket>(player.entityID, PacketType.SERVER_GROUP)
					.SetTarget(player.peer)
					.AddPacketData(GroupDataType.MEMBER_LIST, "groupList", group.groupList)
					.SetData("leaderID", group.leaderID);

				if(gLeader.entityID != player.entityID)
					PacketManager.GetOrCreatePacket<PlayerMessagePacket>(player.entityID, PacketType.PLAYER_MESSAGE)
					.SetTarget(player.peer)
					.SetData("message",     $"Sim {s.SimName} has joined your group.")
					.SetData("messageType", MessageType.INFO);
			}

			SharedNPCSyncManager.Instance.StartCoroutine(SharedNPCSyncManager.Instance.DelayedCheckSim());
		}*/

		private static void ServerDoInvite(short groupID, Entity leader, Entity invPlayer)
		{
			Group group;

			if (groupID == -1)
			{
				group = new Group
				{
					leaderID = leader.entityID,
					groupList = new()
					{
						new Member(){entityID = leader.entityID,isSim = false},
						new Member(){entityID = invPlayer.entityID,isSim = invPlayer.type == EntityType.SIM}
					},
					internalList = new()
					{
						leader
					}
				};
				if (invPlayer.type != EntityType.SIM)
					group.internalList.Add(invPlayer);
				groups.Add(++serverGroupID, group);

				foreach (var m in group.groupList)
					Logging.Log($"{m.ToString()}");
			}
			else
			{
				group = groups[groupID];
				group.groupList.Add(new Member() { entityID = invPlayer.entityID, isSim = invPlayer.type == EntityType.SIM });
				if(invPlayer.type != EntityType.SIM)
					group.internalList.Add(invPlayer);
			}

			//actual group leader
			var gLeader = ClientConnectionManager.Instance.GetPlayerFromID(group.leaderID);

			foreach (var player in group.internalList)
			{
				PacketManager.GetOrCreatePacket<ServerGroupPacket>(player.entityID, PacketType.SERVER_GROUP)
					.SetTarget(player)
					.AddPacketData(GroupDataType.MEMBER_LIST, "groupList", group.groupList)
					.SetData("leaderID", group.leaderID);

				PacketManager.GetOrCreatePacket<PlayerMessagePacket>(player.entityID, PacketType.PLAYER_MESSAGE)
					.SetTarget(player)
					.SetData("message", player.entityID == invPlayer.entityID ? $"You have joined {gLeader.entityName}'s group." : $"{(invPlayer.type == EntityType.SIM?"Sim":"Player")} {invPlayer.entityName} has joined your group.")
					.SetData("messageType", MessageType.INFO);
			}

		}
		public static void ServerOnAcceptInvite(short inviteID)
		{
			var pendingInvite = GetPendingInvite(inviteID);

			if (!pendingInvite.HasValue) return;

			short groupID = pendingInvite.Value.groupID;
			short leaderID = pendingInvite.Value.leaderID;
			short playerID = pendingInvite.Value.inviteeID;
			var networkedPlayer = ClientConnectionManager.Instance.GetPlayerFromID(playerID);
			var networkedLeader = ClientConnectionManager.Instance.GetPlayerFromID(leaderID);
			ServerDoInvite(groupID, networkedLeader, networkedPlayer);
			pendingInvites.Remove(inviteID);
		}

		private static string MakeSimString(Stats st, int xpEarned, float xpBonus)
		{
			var str = "";
			if (st.Level < 35)
			{
				str = string.Concat(new string[]
				{
						st.transform.name,
						" receives ",
						xpEarned.ToString(),
						" +(",
						( xpEarned + xpBonus ).ToString(),
						" XP bonus) xp - (",
						(st.CurrentExperience).ToString(),
						" / ",
						st.ExperienceToLevelUp.ToString(),
						")"
				});
			}
			else
			{
				str = string.Concat(new string[]
				{
						st.transform.name,
						" receives ",
						xpEarned.ToString(),
						" +(",
						( xpEarned + xpBonus ).ToString(),
						" XP bonus) ASCENSION xp - (",
						(st.CurrentAscensionXP).ToString(),
						" / ",
						st.AscensionXPtoLevelUp.ToString(),
						")"
				});
			}

			return str;
		}

		public static void ServerHandleXP(short playerID, int xp, bool useMod, float xpBonus)
		{
			short groupID = GetGroupIDFromPlayer(playerID);
			if (groupID == -1) return; //no group??
			var group = groups[groupID];
			if (group.leaderID != playerID) return; //Not leader

			var mod = 1f;

			int idx = 0;
			if (useMod)
			{
				foreach (var member in group.groupList)
				{
					if (member.entityID == playerID && !member.isSim) continue;

					switch (idx)
					{
						case 0:
							if (member.isSim && GameData.GroupMember1.MyAvatar.GetComponent<SimSync>() != null)
							{
								GameData.SimMngr.Sims[GameData.GroupMember1.simIndex].OpinionOfPlayer += 0.01f;
							}

							mod -= 0.3f;
							break;
						case 1:
							if (member.isSim && GameData.GroupMember2.MyAvatar.GetComponent<SimSync>() != null)
						{
								GameData.SimMngr.Sims[GameData.GroupMember2.simIndex].OpinionOfPlayer += 0.01f;
							}

							mod -= 0.2f;
							break;
						case 2:
							if (member.isSim && GameData.GroupMember3.MyAvatar.GetComponent<SimSync>() != null)
							{
								GameData.SimMngr.Sims[GameData.GroupMember3.simIndex].OpinionOfPlayer += 0.01f;
							}

							mod -= 0.1f;
							break;
					}

					++idx;
				}
			}

			int xpEarned = Mathf.RoundToInt((float)xp * mod);

			var simMessage = "";


			

			idx = 0;
			foreach (var member in group.groupList)
			{
				if (member.entityID == playerID && !member.isSim) continue;

				switch (idx)
				{
					case 0:
						if (member.isSim && GameData.GroupMember1.MyAvatar.GetComponent<SimSync>() != null)
						{
							var st = GameData.GroupMember1.MyStats;
							HandleXPGain(st, xpEarned, xpBonus);
							

							simMessage += MakeSimString(st, xpEarned, xpBonus);
						}
						break;
					case 1:
						if (member.isSim && GameData.GroupMember2.MyAvatar.GetComponent<SimSync>() != null)
						{
							var st = GameData.GroupMember2.MyStats;
							HandleXPGain(st, xpEarned, xpBonus);


							simMessage += simMessage.Length>1?"\r\n":""+MakeSimString(st, xpEarned, xpBonus);
						}
						break;
					case 2:
						if (member.isSim && GameData.GroupMember3.MyAvatar.GetComponent<SimSync>() != null)
						{
							var st = GameData.GroupMember3.MyStats;
							HandleXPGain(st, xpEarned, xpBonus);


							simMessage += simMessage.Length>1?"\r\n":""+MakeSimString(st, xpEarned, xpBonus);
						}
						break;
				}
				++idx;
			}

			foreach (var member in group.internalList)
			{
				if (member.entityID != playerID)
				{
					if (simMessage.Length > 1)
					{
						PacketManager.GetOrCreatePacket<PlayerMessagePacket>(member.entityID, PacketType.PLAYER_MESSAGE)
							.SetTarget(member)
							.SetData("message",     simMessage)
							.SetData("messageType", MessageType.INFO);
					}
				}

				PacketManager.GetOrCreatePacket<ServerGroupPacket>(member.entityID, PacketType.SERVER_GROUP)
					.SetTarget(member)
					.AddPacketData(GroupDataType.EXPERIENCE, "earnedXP", xpEarned)
					.SetData("xpBonus", xpBonus);
			}
		}

		public static void HandleXPGain(Stats stats, int earnedXP, float XPBonus)
		{

			int num = Mathf.RoundToInt((float)earnedXP * XPBonus);
			earnedXP += num;
			if (stats.Level < 35)
			{
				if (!stats.Myself.isNPC)
				{
					if (num == 0)
					{
						UpdateSocialLog.LogAdd("You've gained " + earnedXP.ToString() + " experience!", "yellow");
					}
					else
					{
						UpdateSocialLog.LogAdd("You've gained " + (earnedXP + num).ToString() + " experience!", "yellow");
					}
				}
				stats.CurrentExperience += earnedXP;
				if (stats.Myself.MyNPC != null && stats.Myself.MyNPC.SimPlayer && stats.Myself.MyNPC.ThisSim.InGroup)
				{
					UpdateSocialLog.LogAdd(string.Concat(new string[]
					{
						stats.transform.name,
						" receives ",
						earnedXP.ToString(),
						" +(",
						num.ToString(),
						" XP bonus) xp - (",
						stats.CurrentExperience.ToString(),
						" / ",
						stats.ExperienceToLevelUp.ToString(),
						")"
					}), "yellow");
				}
				if (stats.CurrentExperience >= stats.ExperienceToLevelUp && stats.Level < 35)
				{
					stats.DoLevelUp();
					return;
				}
				if (stats.Level == 35)
				{
					stats.CurrentExperience = stats.ExperienceToLevelUp;
					return;
				}
			}
			else
			{
				stats.CurrentAscensionXP += earnedXP;
				if (!stats.Myself.isNPC)
				{
					if (num == 0)
					{
						UpdateSocialLog.LogAdd("You've gained " + earnedXP.ToString() + " ASCENSION experience!", "yellow");
					}
					else
					{
						UpdateSocialLog.LogAdd("You've gained " + (earnedXP + num).ToString() + " ASCENSION experience!", "yellow");
					}
				}
				else
				{
					if (stats.Myself.MyNPC != null && stats.Myself.MyNPC.SimPlayer && stats.Myself.MyNPC.ThisSim.InGroup)
					{
						UpdateSocialLog.LogAdd(string.Concat(new string[]
						{
							stats.transform.name,
							" receives ",
							earnedXP.ToString(),
							" +(",
							num.ToString(),
							" XP bonus) ASCENSION xp - (",
							stats.CurrentAscensionXP.ToString(),
							" / ",
							stats.AscensionXPtoLevelUp.ToString(),
							")"
						}), "yellow");
					}
					if (stats.Myself.MySkills.AscensionPoints > 0)
					{
						stats.SimPlayerChooseAscension();
					}
				}
				if (stats.CurrentAscensionXP >= stats.AscensionXPtoLevelUp)
				{
					stats.Myself.MySkills.AscensionPoints++;
					stats.CurrentAscensionXP = 0;
					if (!stats.Myself.isNPC)
					{
						UpdateSocialLog.LogAdd("You've gained an ASCENSION POINT!", "yellow");
						SetAchievement.Unlock("ASCENSION");
					}
					if (stats.Myself.isNPC && stats.Myself.MyNPC.SimPlayer)
					{
						stats.SimPlayerChooseAscension();
					}
				}
			}
		}

		public static void HandleClientPacket(GroupPacket packet)
		{
			if (packet.dataTypes.Contains(GroupDataType.INVITE))
			{
				short leaderID = packet.entityID;
				short playerID = packet.playerID;

				Logging.Log($"try inv {leaderID}->{playerID}");

				ServerInvitePlayer(leaderID, playerID, leaderID == ClientConnectionManager.Instance.LocalPlayerID, playerID == ClientConnectionManager.Instance.LocalPlayerID, packet.isSim);
			}
			if (packet.dataTypes.Contains(GroupDataType.EXPERIENCE))
			{
				ServerHandleXP(packet.entityID, packet.xp, packet.useMod, packet.xpBonus);
			}
			if (packet.dataTypes.Contains(GroupDataType.INVITE_SIM))
			{
				//ServerInviteSim(packet.entityID, packet.playerID);
			}
			if (packet.dataTypes.Contains(GroupDataType.ACCEPT_DECLINE))
			{
				bool hasAcceptedInvite = packet.inviteAccept;
				short inviteID = packet.inviteID;

				if(hasAcceptedInvite) ServerOnAcceptInvite(inviteID);
				else ServerOnDeclineInvite(inviteID);
			}
			if (packet.dataTypes.Contains(GroupDataType.REMOVE))
			{
				//short player = packet.entityID;
				var reason = packet.reason;
				short playerID = packet.playerID;
				short groupID = GetGroupIDFromPlayer(playerID);

				Entity playerSc = null;
				if(!packet.isSim)
					playerSc = ClientConnectionManager.Instance.GetPlayerFromID(playerID);
				else
				{
					playerSc = ClientNPCSyncManager.Instance.GetEntityFromID(playerID, true);
					if (playerSc == null)
						playerSc = SharedNPCSyncManager.Instance.GetEntityFromID(playerID, true);
				}

				


				if (!packet.isSim && playerSc != null && playerSc.peer != null) //Happens when they disconnect
				{
					PacketManager.GetOrCreatePacket<PlayerMessagePacket>(playerSc.entityID, PacketType.PLAYER_MESSAGE)
						.SetTarget(playerSc)
						.SetData("message", reason == GroupLeaveReason.LEFT ? $"You've left the group." : $"You've been removed from the group.")
						.SetData("messageType", MessageType.INFO);


					var l = new List<Member>();

					PacketManager.GetOrCreatePacket<ServerGroupPacket>(playerSc.entityID, PacketType.SERVER_GROUP)
						.SetTarget(playerSc)
						.AddPacketData(GroupDataType.MEMBER_LIST, "groupList", l)
						.SetData("leaderID", (short)-1);
				}

				

				if (groupID != -1)
				{
					Member? t = GetMemberFromPlayer(playerID, packet.isSim);
					Member m;
					if (t.HasValue)
						m = t.Value;
					else return;

					var group = groups[groupID];

					group.groupList.Remove(m);
					if(!packet.isSim)
						group.internalList.Remove(playerSc);

					var mes = "";
					if (playerSc != null && playerSc.peer != null)
						mes = reason == GroupLeaveReason.LEFT ? $"Player {playerSc.entityName} has left your group." : $"Player {playerSc.entityName} removed from group.";
					else
						mes = "A Player has left your group.";

					if (m.isSim)
						mes = $"Sim {playerSc.name} has been removed from the group.";

					foreach (var nPlayer in group.internalList)
					{
						PacketManager.GetOrCreatePacket<ServerGroupPacket>(nPlayer.entityID, PacketType.SERVER_GROUP)
						.SetTarget(nPlayer)
						.AddPacketData(GroupDataType.MEMBER_LIST, "groupList", group.groupList)
						.SetData("leaderID", group.leaderID);

						
						PacketManager.GetOrCreatePacket<PlayerMessagePacket>(nPlayer.entityID, PacketType.PLAYER_MESSAGE)
							.SetTarget(nPlayer)
							.SetData("message",     mes)
							.SetData("messageType", MessageType.INFO);

					}

					if (group.groupList.Count <= 1)
					{
						//group basically disbanded
						groups.Remove(groupID);

					}
					
				}
			}
		}

		public static void HandleServerPacket(ServerGroupPacket packet)
		{
			if (packet.dataTypes.Contains(GroupDataType.INVITE))
			{
				short leaderID = packet.leaderID;
				var leader = ClientConnectionManager.Instance.GetPlayerFromID(leaderID);
				ClientInviteID = packet.inviteID;

				
				UI.Main.EnablePrompt($"{leader.entityName} has invited you to join their group.", () => {AcceptInvite(ClientInviteID);}, () => {DeclineInvite(ClientInviteID);});
			}
			if (packet.dataTypes.Contains(GroupDataType.EXPERIENCE))
			{
				string simMessage = "";
				int idx = 0;
				foreach (var member in currentGroup.groupList)
				{
					if (!member.isSim) continue;
					
					switch (idx)
					{
						case 0:
							if (member.isSim && GameData.GroupMember1.MyAvatar.GetComponent<SimSync>() != null)
							{
								GameData.SimMngr.Sims[GameData.GroupMember1.simIndex].OpinionOfPlayer += 0.01f;
								var st = GameData.GroupMember1.MyStats;
								HandleXPGain(st, packet.earnedXP, packet.xpBonus);
								simMessage += MakeSimString(st, packet.earnedXP, packet.xpBonus);
							}
							break;
						case 1:
							if (member.isSim && GameData.GroupMember2.MyAvatar.GetComponent<SimSync>() != null)
							{
								GameData.SimMngr.Sims[GameData.GroupMember2.simIndex].OpinionOfPlayer += 0.01f;
								var st = GameData.GroupMember2.MyStats;
								HandleXPGain(st, packet.earnedXP, packet.xpBonus);
								simMessage += simMessage.Length > 1 ? "\r\n" : "" + MakeSimString(st, packet.earnedXP, packet.xpBonus);
							}
							break;
						case 2:
							if (member.isSim && GameData.GroupMember3.MyAvatar.GetComponent<SimSync>() != null)
							{
								GameData.SimMngr.Sims[GameData.GroupMember3.simIndex].OpinionOfPlayer += 0.01f;
								var st = GameData.GroupMember3.MyStats;
								HandleXPGain(st, packet.earnedXP, packet.xpBonus);
								simMessage += simMessage.Length > 1 ? "\r\n" : "" + MakeSimString(st, packet.earnedXP, packet.xpBonus);
							}
							break;
					}

					++idx;
				}
				if(!string.IsNullOrEmpty(simMessage))
					UpdateSocialLog.LogAdd(simMessage, "yellow");
				HandleXPGain(GameData.PlayerStats, packet.earnedXP, packet.xpBonus);
			}
			if (packet.dataTypes.Contains(GroupDataType.MEMBER_LIST))
			{
				List<GameObject> idk = new()
				{
					GameData.SimPlayerGrouping.D1,
					GameData.SimPlayerGrouping.D2,
					GameData.SimPlayerGrouping.D3
				};

				List<TextMeshProUGUI> texts = new()
				{
					GameData.SimPlayerGrouping.PlayerOneName,
					GameData.SimPlayerGrouping.PlayerTwoName,
					GameData.SimPlayerGrouping.PlayerThreeName
				};

				if (packet.groupList.Count <= 1) //Group disbanded
				{
					if (GameData.GroupMember1 != null)
					{
						GameData.GroupMember1.Grouped = false;
						if (GameData.GroupMember1.MyAvatar != null)
						{
							GameData.GroupMember1.MyAvatar.InGroup = false;
							GameData.GroupMember1.MyStats.Myself.MyNPC.InGroup = false;
						}
					}
					if (GameData.GroupMember2 != null)
					{
						GameData.GroupMember2.Grouped = false;
						if (GameData.GroupMember2.MyAvatar != null)
						{
							GameData.GroupMember2.MyAvatar.InGroup = false;
							GameData.GroupMember2.MyStats.Myself.MyNPC.InGroup = false;
						}
					}
					if (GameData.GroupMember3 != null)
					{
						GameData.GroupMember3.Grouped = false;
						if (GameData.GroupMember3.MyAvatar != null)
						{
							GameData.GroupMember3.MyAvatar.InGroup = false;
							GameData.GroupMember3.MyStats.Myself.MyNPC.InGroup = false;
						}
					}
					GameData.GroupMember1 = null;
					GameData.GroupMember2 = null;
					GameData.GroupMember3 = null;
					idk[0].SetActive(false);
					idk[1].SetActive(false);
					idk[2].SetActive(false);
					currentGroup.groupList = null;
					currentGroup.leaderID = -1;
					return;
				}

				if (currentGroup.groupList != null)
				{
					if (currentGroup.groupList.Count > 0)
					{
						var pidx = 0;
						for (var i = 0; i < currentGroup.groupList.Count; i++)
						{
							short playerID = currentGroup.groupList[i].entityID;
							bool isSim = currentGroup.groupList[i].isSim;
							if (playerID != ClientConnectionManager.Instance.LocalPlayerID)
							{
								switch (pidx)
								{
									case 0:

										if (GameData.GroupMember1 != null)
										{
											GameData.GroupMember1.Grouped = false;
											if (GameData.GroupMember1.MyAvatar != null)
											{
												GameData.GroupMember1.MyAvatar.InGroup = false;
												GameData.GroupMember1.MyStats.Myself.MyNPC.InGroup = false;
											}
										}

										GameData.GroupMember1 = null;
										break;
									case 1:

										if (GameData.GroupMember2 != null)
										{
											GameData.GroupMember2.Grouped = false;
											if (GameData.GroupMember2.MyAvatar != null)
											{
												GameData.GroupMember2.MyAvatar.InGroup = false;
												GameData.GroupMember2.MyStats.Myself.MyNPC.InGroup = false;
											}
										}
										
										GameData.GroupMember2 = null;
										break;
									case 2:
										if (GameData.GroupMember3 != null)
										{
											GameData.GroupMember3.Grouped = false;
											if (GameData.GroupMember3.MyAvatar != null)
											{
												GameData.GroupMember3.MyAvatar.InGroup = false;
												GameData.GroupMember3.MyStats.Myself.MyNPC.InGroup = false;
											}
										}
										
										GameData.GroupMember3 = null; 
										break;
								}

								texts[pidx].text = "Empty";
								texts[pidx].color = Color.grey;

								idk[pidx].SetActive(false);
								pidx++;
							}
						}
					}
				}

				if(currentGroup.groupList  != null)
					Logging.Log($"recv group list prev: {currentGroup.groupList.Count}");

				currentGroup = new()
				{
					groupList = packet.groupList,
					leaderID = packet.leaderID
				};

				if (currentGroup.groupList != null)
					Logging.Log($"recv group list n: {currentGroup.groupList.Count}");

				var plidx = 0;
				for (var i = 0;i < currentGroup.groupList.Count;i++)
				{
					short playerID = currentGroup.groupList[i].entityID;
					bool isSim = currentGroup.groupList[i].isSim;
					if (playerID != ClientConnectionManager.Instance.LocalPlayerID)
					{
						bool isLocalSim = false;
						Entity player = null;
						if (!isSim)
							player = ClientConnectionManager.Instance.GetPlayerFromID(playerID);
						else
						{
							player = ClientNPCSyncManager.Instance.GetEntityFromID(playerID, true);
							if(player == null)
							{
								player = SharedNPCSyncManager.Instance.GetEntityFromID(playerID, true);
								if (player != null)
									isLocalSim = true;
							}
						}

						if(player == null)
						{
							Logging.LogError($"Error adding {(isSim?"Sim":"Player")} with id {playerID}");
							continue;
						}

						SimPlayerTracking n;

						if (!isSim)
						{
							n = new SimPlayerTracking(player.entityName, 0f, player.zone, -(playerID+1));
							n.MyAvatar = ( (NetworkedPlayer)player ).sim;
							n.MyAvatar.InGroup = true;
							( (NetworkedPlayer)player ).npc.InGroup = true;
							n.MyStats = n.MyAvatar.MyStats;
							n.Grouped = true;
						}
						else
						{
							if (!isLocalSim)
							{
								n = new SimPlayerTracking(player.entityName, 0f, player.zone, -(playerID + 1));
								//bool isSpawned = n.CurScene == SceneManager.GetActiveScene().name && n.MyAvatar != null;
								//Make 100% sure the sim isn't already in the scene
								//var _sim = GameObject.Find(n.SimName);
								//if (_sim != null)
								//	isSpawned = true;

								n.MyAvatar = ((NetworkedSim)player).sim;
								n.MyAvatar.InGroup = true;
								((NetworkedSim)player).npc.InGroup = true;
								n.MyStats = n.MyAvatar.MyStats;
								n.Grouped = true;
							}
							else
							{
								var ss = (SimSync)player;
								n = GameData.SimMngr.Sims[ss.simIndex];
								n.MyAvatar.InGroup = true;
								ss.npc.InGroup = true;
								//n.MyStats = n.MyAvatar.MyStats;
								n.Grouped = true;
								
							}
							//if (!isSpawned) //Spawn the sim, everything else should be handled by the sim spawn packet
							//	n.SpawnMeInGame(ClientConnectionManager.Instance.LocalPlayer.transform.position);



						}

						switch (plidx)
						{
							case 0: GameData.GroupMember1 = n; break;
							case 1: GameData.GroupMember2 = n; break;
							case 2: GameData.GroupMember3 = n; break;
						}

						texts[plidx].text = isSim ? n.SimName : player.entityName;
						texts[plidx].color = Color.white;

						if(packet.leaderID == ClientConnectionManager.Instance.LocalPlayerID)
							idk[plidx].SetActive(true);
						plidx++;
					}
				}

				GameData.SimPlayerGrouping.ChangeToGroup = true;
				GameData.SimPlayerGrouping.SetRoles();
				GameData.SimPlayerGrouping.UpdateGroupNames();

			}
		}

		public static void ServerOnDeclineInvite(short inviteID)
		{
			var pendingInvite = GetPendingInvite(inviteID);

			if (!pendingInvite.HasValue) { return;}

			var player = ClientConnectionManager.Instance.GetPlayerFromID(pendingInvite.Value.inviteeID);
			var leader = ClientConnectionManager.Instance.GetPlayerFromID(pendingInvite.Value.leaderID);

			PacketManager.GetOrCreatePacket<PlayerMessagePacket>(ClientConnectionManager.Instance.LocalPlayerID, PacketType.PLAYER_MESSAGE)
				.SetTarget(leader)
				.SetData("message",     $"Player {player.entityName} has declined your group invite.")
				.SetData("messageType", MessageType.INFO);

			pendingInvites.Remove(inviteID);
		}

		public static void AcceptInvite(short inviteID)
		{
			if (ServerConnectionManager.Instance.IsRunning)
			{
				var packet = new GroupPacket();
				packet.dataTypes.Add(GroupDataType.ACCEPT_DECLINE);
				packet.inviteID = inviteID;
				packet.inviteAccept = true;
				HandleClientPacket(packet);
				return;
			}
			PacketManager.GetOrCreatePacket<GroupPacket>(ClientConnectionManager.Instance.LocalPlayerID, PacketType.GROUP)
				.AddPacketData(GroupDataType.ACCEPT_DECLINE, "inviteAccept", true)
				.SetData("inviteID", inviteID);
		}

		public static void DeclineInvite(short inviteID)
		{
			if (ServerConnectionManager.Instance.IsRunning)
			{
				var packet = new GroupPacket();
				packet.dataTypes.Add(GroupDataType.ACCEPT_DECLINE);
				packet.inviteID = inviteID;
				packet.inviteAccept = false;
				HandleClientPacket(packet);
				return;
			}
			PacketManager.GetOrCreatePacket<GroupPacket>(ClientConnectionManager.Instance.LocalPlayerID, PacketType.GROUP)
				.AddPacketData(GroupDataType.ACCEPT_DECLINE, "inviteAccept", false)
				.SetData("inviteID", inviteID);
		}

		public static void LeaveGroup()
		{
			PacketManager.GetOrCreatePacket<GroupPacket>(ClientConnectionManager.Instance.LocalPlayerID, PacketType.GROUP)
				.AddPacketData(GroupDataType.REMOVE, "reason", GroupLeaveReason.LEFT)
				.SetData("playerID", ClientConnectionManager.Instance.LocalPlayerID);

			//currentGroup = new();
		}


		public static void ForceRemoveFromGroup(short playerID)
		{
			if (ServerConnectionManager.Instance.IsRunning)
			{
				var packet = new GroupPacket();
				packet.dataTypes.Add(GroupDataType.REMOVE);
				packet.playerID = playerID;
				packet.reason = GroupLeaveReason.KICKED;
				HandleClientPacket(packet);
			}
		}
		public static void RemoveFromGroup(short playerID)
		{
			if (currentGroup.leaderID != -1 && currentGroup.leaderID != ClientConnectionManager.Instance.LocalPlayerID)
			{
				Logging.WriteInfoMessage("You are not the group leader.");
				return;
			}

			Member? m = GetMemberFromPlayer(playerID, false);
			if(!m.HasValue)
				m = GetMemberFromPlayer(playerID, true);

			if(!m.HasValue)
			{
				Logging.LogError($"There was an error trying to remove ID {playerID}");
				return;
			}

			if (ServerConnectionManager.Instance.IsRunning)
			{
				var packet = new GroupPacket();
				packet.dataTypes.Add(GroupDataType.REMOVE);
				packet.playerID = playerID;
				packet.reason = GroupLeaveReason.KICKED;
				packet.isSim = m.Value.isSim;
				HandleClientPacket(packet);
				return;
			}


			PacketManager.GetOrCreatePacket<GroupPacket>(ClientConnectionManager.Instance.LocalPlayerID, PacketType.GROUP)
				.AddPacketData(GroupDataType.REMOVE, "reason", GroupLeaveReason.KICKED)
				.SetData("playerID", playerID)
				.SetData("isSim", m.Value.isSim);
		}

		public static void InvitePlayer(Entity simPlayer)
		{
			if (currentGroup.leaderID != -1 && currentGroup.leaderID != ClientConnectionManager.Instance.LocalPlayerID)
			{
				Logging.WriteInfoMessage("You are not the group leader.");
				return;
			}

			Logging.Log($"Try inv {simPlayer.name} {simPlayer.entityID}");

			if (ServerConnectionManager.Instance.IsRunning)
			{
				ServerInvitePlayer(ClientConnectionManager.Instance.LocalPlayerID, simPlayer.entityID, true, false, simPlayer.type == EntityType.SIM);
				return;
			}
			PacketManager.GetOrCreatePacket<GroupPacket>(ClientConnectionManager.Instance.LocalPlayerID, PacketType.GROUP)
				.AddPacketData(GroupDataType.INVITE, "playerID", simPlayer.entityID).SetData("isSim", simPlayer.type == EntityType.SIM);
		}

		public static void ForceClearGroup(bool shutdown=false)
		{
			if (!ClientConnectionManager.Instance.IsRunning && !shutdown) return;

			bool hasChanged = false;
			if (GameData.GroupMember1 != null)
			{
				GameData.GroupMember1.Grouped = false;
				if (GameData.GroupMember1.MyAvatar != null)
					GameData.GroupMember1.MyAvatar.InGroup = false;
				GameData.GroupMember1 = null;
				hasChanged = true;
			}
			if (GameData.GroupMember2 != null)
			{
				GameData.GroupMember2.Grouped = false;
				if (GameData.GroupMember2.MyAvatar != null)
					GameData.GroupMember2.MyAvatar.InGroup = false;
				GameData.GroupMember2 = null;
				hasChanged = true;
			}
			if (GameData.GroupMember3 != null)
			{
				GameData.GroupMember3.Grouped = false;
				if (GameData.GroupMember3.MyAvatar != null)
					GameData.GroupMember3.MyAvatar.InGroup = false;
				GameData.GroupMember3 = null;
				hasChanged = true;
			}

			if (GameData.SimPlayerGrouping != null)
			{
				GameData.SimPlayerGrouping.D1.SetActive(false);
				GameData.SimPlayerGrouping.D2.SetActive(false);
				GameData.SimPlayerGrouping.D3.SetActive(false);
			}
			if(hasChanged)
				Logging.WriteInfoMessage("Your group has been disbanded.");
		}

		//Assumes the sim is already in the players group
		/*public static void InviteSim(SimPlayerTracking simPlayer)
		{
			*//*if (currentGroup.leaderID != -1 && currentGroup.leaderID != ClientConnectionManager.Instance.LocalPlayerID)
			{
				Logging.WriteInfoMessage("You are not the group leader.");
				//forcefully remove the sim from the group.. no sims for you!
				if (GameData.GroupMember1 != null && GameData.GroupMember1 == simPlayer)
				{
					GameData.GroupMember1.Grouped = false;
					GameData.GroupMember1.MyAvatar.InGroup = false;
					GameData.GroupMember1 = null;
				}
				if (GameData.GroupMember2 != null && GameData.GroupMember1 == simPlayer)
				{
					GameData.GroupMember2.Grouped = false;
					GameData.GroupMember2.MyAvatar.InGroup = false;
					GameData.GroupMember2 = null;
				}
				if (GameData.GroupMember3 != null && GameData.GroupMember1 == simPlayer)
				{
					GameData.GroupMember3.Grouped = false;
					GameData.GroupMember3.MyAvatar.InGroup = false;
					GameData.GroupMember3 = null;
				}
				return;
			}*//*

			//Check if this sim is already in our group
			if (currentGroup.groupList != null)
			{
				foreach (var member in currentGroup.groupList)
				{
					if (member.isSim && member.simIndex == simPlayer.simIndex)
						return;
				}
			}


			var entID = simPlayer.MyAvatar.GetComponent<SimSync>().entityID;

			ServerInviteSim(ClientConnectionManager.Instance.LocalPlayerID, entID);

		}*/

		public static void RemoveSim(SimPlayerTracking simPlayer)
		{
			//short player = packet.entityID;
			short groupID = GetGroupIDFromPlayer(ClientConnectionManager.Instance.LocalPlayerID);

			var l = new List<Member>();

			if (groupID != -1)
			{
				var group = groups[groupID];

				group.groupList.Remove(GetMemberFromPlayer((short)simPlayer.simIndex, true).Value);
				//group.internalList.Remove(playerSc);
				//bool isHostInGroup = false;
				foreach (var nPlayer in group.internalList)
				{
					//if (nPlayer.entityID == ClientConnectionManager.Instance.LocalPlayerID)
					//	isHostInGroup = true;
					PacketManager.GetOrCreatePacket<ServerGroupPacket>(nPlayer.entityID, PacketType.SERVER_GROUP)
					.SetTarget(nPlayer)
					.AddPacketData(GroupDataType.MEMBER_LIST, "groupList", group.groupList)
					.SetData("leaderID", group.leaderID);


					PacketManager.GetOrCreatePacket<PlayerMessagePacket>(nPlayer.entityID, PacketType.PLAYER_MESSAGE)
						.SetTarget(nPlayer)
						.SetData("message", $"Sim {simPlayer.SimName} removed from group.")
						.SetData("messageType", MessageType.INFO);

				}

				if (group.groupList.Count <= 1)
				{
					//group basically disbanded
					groups.Remove(groupID);
					//if (isHostInGroup)
					//	SharedNPCSyncManager.Instance.StartCoroutine(SharedNPCSyncManager.Instance.DelayedCheckSim());
				}

			}
		}
	}
}
