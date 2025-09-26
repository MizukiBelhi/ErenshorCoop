using ErenshorCoop.Server;
using ErenshorCoop.Server.Grouping;
using ErenshorCoop.Shared;
using ErenshorCoop.Shared.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace ErenshorCoop.Client.Grouping
{
	public class ClientGroup
	{
		public static SharedGroup.Group currentGroup = new();
		public static Action GroupListCallback = null;

		public static SharedGroup.Member ClientGetMemberFromPlayer(short playerID, bool isSim = false)
		{

			foreach (SharedGroup.Member p in currentGroup.groupList)
				if (p.entityID == playerID && p.isSim == isSim)
					return p;

			return null;
		}

		public static bool IsPlayerInGroup(short playerID, bool isSim)
		{
			if (currentGroup.groupList == null || currentGroup.groupList.Count == 0)
				return false; //Not in a group

			foreach (var members in currentGroup.groupList)
			{
				if (members.entityID == playerID && members.isSim == isSim)
					return true;
			}
			return false;
		}

		public static bool HasGroup => currentGroup.groupList is { Count: > 0 };

		public static bool IsLocalLeader()
		{
			return currentGroup.leaderID == ClientConnectionManager.Instance.LocalPlayerID;
		}

		private static void HandleExperience(ServerGroupPacket packet)
		{
			var processedSimIndexes = new HashSet<short>();

			foreach (var member in currentGroup.groupList)
			{
				if (!member.isSim)
				{
					if (member.entityID != ClientConnectionManager.Instance.LocalPlayerID)
					{
						//Get player entity + stats
						var player = ClientConnectionManager.Instance.GetPlayerFromID(member.entityID);
						if (player == null) continue;
						if (player is NetworkedPlayer nt && processedSimIndexes.Add(member.entityID))
						{
							HandleXPGain(nt.sim.MyStats, packet.earnedXP, packet.xpBonus, true);
						}
					}
				}
				else
				{
					foreach (var mem in GameData.GroupMembers)
					{
						SimSync ssync = null;
						if (mem != null && mem.MyAvatar != null && (ssync = mem.MyAvatar.GetComponent<SimSync>()) != null && mem.simIndex == ssync.simIndex && processedSimIndexes.Add(member.entityID))
						{
							GameData.SimMngr.Sims[mem.simIndex].OpinionOfPlayer += 0.01f;
							var st = mem.MyStats;
							HandleXPGain(st, packet.earnedXP, packet.xpBonus, false);
							break;
						}
						else if(mem != null && mem.MyAvatar != null && mem.MyAvatar.GetComponent<NetworkedSim>() != null && processedSimIndexes.Add(member.entityID))
						{
							HandleXPGain(mem.MyStats, packet.earnedXP, packet.xpBonus, false);
							break;
						}
					}
				}
			}

			HandleXPGain(GameData.PlayerStats, packet.earnedXP, packet.xpBonus, true);
		}

		private static void HandleSimFollow(ServerGroupPacket packet)
		{
			Entity sim = SharedNPCSyncManager.Instance.GetEntityFromID(packet.simID, true);
			if (sim != null)
			{
				if (packet.followTargetID != -1)
				{
					Entity followTarget = ClientConnectionManager.Instance.GetPlayerFromID(packet.followTargetID);
					if (followTarget != null)
					{
						((SimSync)sim).target = followTarget.transform;
					}
				}
				else
				{
					((SimSync)sim).target = null;
				}
			}
		}

		private static void HandleMemberList(ServerGroupPacket packet)
		{
			List<GameObject> idk = new()
			{
				GameData.SimPlayerGrouping.D1,
				GameData.SimPlayerGrouping.D2,
				GameData.SimPlayerGrouping.D3,
				GameData.SimPlayerGrouping.D4,
			};

			List<TextMeshProUGUI> texts = new()
			{
				GameData.SimPlayerGrouping.PlayerOneName,
				GameData.SimPlayerGrouping.PlayerTwoName,
				GameData.SimPlayerGrouping.PlayerThreeName,
				GameData.SimPlayerGrouping.PlayerFourName,
			};

			List<SimPlayerTracking> _saved = new();

			if (packet.groupList.Count <= 1) //Group disbanded
			{
				for (int i = 0; i < GameData.GroupMembers.Length; i++)
				{
					var mem = GameData.GroupMembers[i];
					if (mem != null)
					{
						_saved.Add(mem);
						mem.Grouped = false;
						if (mem.MyAvatar != null)
						{
							mem.MyAvatar.InGroup = false;
							mem.MyStats.Myself.MyNPC.InGroup = false;
						}
					}
					GameData.GroupMembers[i] = null;
				}

				idk[0].SetActive(false);
				idk[1].SetActive(false);
				idk[2].SetActive(false);
				idk[3].SetActive(false);
				currentGroup.groupList = new();
				currentGroup.leaderID = -1;
				return;
			}


			for (int i = 0; i < GameData.GroupMembers.Length; i++)
			{
				var mem = GameData.GroupMembers[i];
				if (mem != null)
				{
					mem.Grouped = false;
					if (mem.MyAvatar != null)
						mem.MyAvatar.InGroup = false;
					GameData.GroupMembers[i] = null;
				}
			}


			if (GameData.SimPlayerGrouping != null)
			{
				GameData.SimPlayerGrouping.D1.SetActive(false);
				GameData.SimPlayerGrouping.D2.SetActive(false);
				GameData.SimPlayerGrouping.D3.SetActive(false);
				GameData.SimPlayerGrouping.D4.SetActive(false);
			}

			texts[0].text = "Empty";
			texts[0].color = Color.grey;
			texts[1].text = "Empty";
			texts[1].color = Color.grey;
			texts[2].text = "Empty";
			texts[2].color = Color.grey;
			texts[3].text = "Empty";
			texts[3].color = Color.grey;


			currentGroup = new()
			{
				groupList = packet.groupList,
				leaderID = packet.leaderID
			};


			var grp = new SharedGroup.Group();
			grp.groupList = new();
			grp.leaderID = currentGroup.leaderID;

			var savedList = new List<SharedGroup.Member>(currentGroup.groupList);

			var localPlayer = savedList.FirstOrDefault(m => m.entityID == ClientConnectionManager.Instance.LocalPlayerID);
			bool localIsLeader = localPlayer != null && localPlayer.entityID == currentGroup.leaderID;

			//We throw the local player into slot -1 so we can properly display the group
			if (localPlayer != null)
			{
				savedList.Remove(localPlayer);
				localPlayer.slot = 255; // -1 equivalent
				grp.groupList.Add(localPlayer);
			}

			if (!localIsLeader)
			{
				var leader = savedList.FirstOrDefault(m => m.entityID == currentGroup.leaderID);
				if (leader != null)
				{
					savedList.Remove(leader);
					leader.slot = 0;
					grp.groupList.Add(leader);
				}
			}
			byte nextSlot = (byte)(localIsLeader ? 0 : 1);
			foreach (var member in savedList)
			{
				member.slot = nextSlot++;
				grp.groupList.Add(member);
			}

			currentGroup = grp;


			foreach (var mem in currentGroup.groupList)
			{
				short playerID = mem.entityID;
				bool isSim = mem.isSim;
				byte slot = mem.slot;

				if (playerID != ClientConnectionManager.Instance.LocalPlayerID)
				{
					Entity player = Extensions.GetPlayerOrSimEntityByID(playerID);

					if (player == null)
					{
						Logging.LogError($"Error adding {(isSim ? "Sim" : "Player")} with id {playerID}");
						continue;
					}

					SimPlayerTracking n;

					if (!isSim)
					{
						n = new SimPlayerTracking(player.entityName, 0f, player.zone, -(playerID + 1));
						n.MyAvatar = ((NetworkedPlayer)player).sim;
						n.MyAvatar.InGroup = true;
						((NetworkedPlayer)player).npc.InGroup = true;
						n.MyStats = n.MyAvatar.MyStats;
						n.Grouped = true;
					}
					else
					{
						if (player is NetworkedSim)
						{
							n = new SimPlayerTracking(player.entityName, 0f, player.zone, -(playerID + 1));
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
					}

					GameData.GroupMembers[slot] = n;

					texts[slot].text = isSim ? n.SimName : player.entityName;
					texts[slot].color = Color.white;

					if (packet.leaderID == ClientConnectionManager.Instance.LocalPlayerID)
						idk[slot].SetActive(true);
				}
			}

			foreach (var s in _saved)
			{
				if (s != null && GameData.SimMngr.Sims.Contains(s))
				{
					if (s.MyAvatar != null && s.MyAvatar.GetComponent<Entity>() != null)
					{
						var e = s.MyAvatar.GetComponent<Entity>();
						if (e.type == EntityType.SIM && !IsPlayerInGroup(e.entityID, true))
						{
							if (e is SimSync)
								((SimSync)e).npc.InGroup = false;
							s.Grouped = false;
							s.MyAvatar.InGroup = false;
						}
					}
				}
			}

			GameData.SimPlayerGrouping.ChangeToGroup = true;
			GameData.SimPlayerGrouping.SetRoles();
			GameData.SimPlayerGrouping.UpdateGroupNames();
			GroupListCallback?.Invoke();
		}



		public static void HandlePacket(ServerGroupPacket packet)
		{
			foreach(var dt in packet.dataTypes)
			{
				switch (dt)
				{
					case GroupDataType.INVITE:
					case GroupDataType.ACCEPT_DECLINE:
					case GroupDataType.REMOVE:
						Invites.HandlePacket(dt, packet);
						break;
					
					case GroupDataType.EXPERIENCE:
						HandleExperience(packet);
						break;
					case GroupDataType.SIM_FOLLOW:
						HandleSimFollow(packet);
						break;
					case GroupDataType.MEMBER_LIST:
						HandleMemberList(packet);
						break;
				}
			}

		}


		public static void Cleanup()
		{
			currentGroup = new();
			currentGroup.leaderID = -1;
			ForceClearGroup();
		}

		public static void RemoveFromGroup(short playerID)
		{
			if (currentGroup.leaderID != -1 && currentGroup.leaderID != ClientConnectionManager.Instance.LocalPlayerID)
			{
				Logging.WriteInfoMessage("You are not the group leader.");
				return;
			}

			SharedGroup.Member m = ClientGetMemberFromPlayer(playerID, false);
			if (m == null)
				m = ClientGetMemberFromPlayer(playerID, true);

			if (m == null)
			{
				Logging.LogError($"There was an error trying to remove ID {playerID}");
				return;
			}

			Logging.Log($"Removing {m.ToString()}");


			if (ServerConnectionManager.Instance.IsRunning)
			{
				var packet = new GroupPacket();
				packet.dataTypes.Add(GroupDataType.REMOVE);
				packet.playerID = playerID;
				packet.reason = GroupLeaveReason.KICKED;
				packet.isSim = m.isSim;
				packet.entityID = ClientConnectionManager.Instance.LocalPlayerID;
				ServerGroup.HandlePacket(packet); //Host send to self
				return;
			}


			PacketManager.GetOrCreatePacket<GroupPacket>(ClientConnectionManager.Instance.LocalPlayerID, PacketType.GROUP)
				.AddPacketData(GroupDataType.REMOVE, "reason", GroupLeaveReason.KICKED)
				.SetData("playerID", playerID)
				.SetData("isSim", m.isSim);
		}

		

		public static void ForceClearGroup(bool shutdown = false)
		{
			if (!ClientConnectionManager.Instance.IsRunning && !shutdown) return;

			bool hasChanged = false;
			for (int i = 0; i < GameData.GroupMembers.Length; i++)
			{
				var mem = GameData.GroupMembers[i];
				if (mem != null)
				{
					mem.Grouped = false;
					if (mem.MyAvatar != null)
						mem.MyAvatar.InGroup = false;

					hasChanged = true;

					GameData.GroupMembers[i] = null;
				}
			}

			if (GameData.SimPlayerGrouping != null)
			{
				GameData.SimPlayerGrouping.D1.SetActive(false);
				GameData.SimPlayerGrouping.D2.SetActive(false);
				GameData.SimPlayerGrouping.D3.SetActive(false);
				GameData.SimPlayerGrouping.D4.SetActive(false);
			}
			if (hasChanged)
				Logging.WriteInfoMessage("Your group has been disbanded.");
		}


		public static void HandleXPGain(Stats stats, int earnedXP, float XPBonus, bool isPlayer)
		{
			int num = Mathf.RoundToInt((float)earnedXP * XPBonus);
			earnedXP += num;

			bool isLocal = ClientConnectionManager.Instance.LocalPlayer.stats == stats;

			void LogGain(string type)
			{
				string xpType = type == "ASCENSION" ? "ASCENSION " : "";

				if (isLocal)
				{
					if (num == 0) 
					{ 
						UpdateSocialLog.LogAdd("You've gained " + earnedXP.ToString() + $" {xpType}experience!", "yellow"); }
					else
					{
						UpdateSocialLog.LogAdd("You've gained " + (earnedXP + num).ToString() + $" {xpType}experience!", "yellow");
					}

				}
				else
				{
					UpdateSocialLog.LogAdd(
						$"{stats.transform.name} receives {earnedXP} +({num} XP bonus) {xpType}xp - ({(type == "ASCENSION" ? stats.CurrentAscensionXP : stats.CurrentExperience)} / {(type == "ASCENSION" ? stats.AscensionXPtoLevelUp : stats.ExperienceToLevelUp)})",
						"yellow"
					);
					if(!isPlayer)
					{
						var zoneAnnounceData = GameData.ZoneAnnounceData;
						int mobsKilled = zoneAnnounceData != null ? zoneAnnounceData.MobsKilledByPlayerParty : 0;
						if (mobsKilled >= 10)
						{
							var simMngr = GameData.SimMngr;
							int simIndex = stats.Myself.MyNPC.ThisSim.myIndex;
							var simEntry = simMngr?.Sims != null && simIndex >= 0 && simIndex < simMngr.Sims.Count
										   ? simMngr.Sims[simIndex]
										   : null;

							if (simEntry?.MyCurrentMemory != null)
							{
								// same formula as decompiled: add baseXP + num
								simEntry.MyCurrentMemory.XPGain += earnedXP;
								simEntry.MyCurrentMemory.ZoneName = GameData.ZoneAnnounceData.ZoneName;
								simEntry.MyCurrentMemory.PlayedDay = DateTime.Now.DayOfYear;
								simEntry.MyCurrentMemory.PlayedYear = DateTime.Now.Year;
								simEntry.MyCurrentMemory.GroupedLastYear = DateTime.Now.Year;
								simEntry.MyCurrentMemory.GroupedLastDay = DateTime.Now.DayOfYear;
							}
							else
							{
								Debug.Log("Sim Memory not intialized");
							}
						}
					}
				}
			}

			if (stats.Level < 35)
			{
				stats.CurrentExperience += earnedXP;
				LogGain("NORMAL");

				if (stats.CurrentExperience >= stats.ExperienceToLevelUp && (isLocal || stats.GetComponent<SimSync>() != null))
				{
					stats.DoLevelUp();
					stats.CurrentExperience = Mathf.Min(stats.CurrentExperience, stats.ExperienceToLevelUp);
				}
			}
			else
			{
				stats.CurrentAscensionXP += earnedXP;
				LogGain("ASCENSION");

				if (stats.CurrentAscensionXP >= stats.AscensionXPtoLevelUp)
				{
					stats.CurrentAscensionXP = 0;
					stats.Myself.MySkills.AscensionPoints++;

					if (isLocal)
					{
						UpdateSocialLog.LogAdd("You've gained an ASCENSION POINT!", "yellow");
						SetAchievement.Unlock("ASCENSION");
					}
					else if (!isPlayer && stats.GetComponent<SimSync>() != null)
					{
						stats.SimPlayerChooseAscension();
					}
					else if (isPlayer)
					{
						UpdateSocialLog.LogAdd($"{stats.transform.name} gained an ASCENSION POINT!", "yellow");
					}
				}

				if (!isLocal && !isPlayer && stats.GetComponent<SimSync>() != null && stats.Myself.MySkills.AscensionPoints > 0)
				{
					stats.SimPlayerChooseAscension();
				}
			}
		}



		public static void HandleXPGain2(Stats stats, int earnedXP, float XPBonus)
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



		public static void PeriodicGroupCheck()
		{
			if (currentGroup == null || currentGroup.groupList == null) return;

			List<GameObject> idk = new()
			{
				GameData.SimPlayerGrouping.D1,
				GameData.SimPlayerGrouping.D2,
				GameData.SimPlayerGrouping.D3,
				GameData.SimPlayerGrouping.D4,
			};

			List<TextMeshProUGUI> texts = new()
			{
				GameData.SimPlayerGrouping.PlayerOneName,
				GameData.SimPlayerGrouping.PlayerTwoName,
				GameData.SimPlayerGrouping.PlayerThreeName,
				GameData.SimPlayerGrouping.PlayerFourName,
			};

			
			var plidx = 0;
			for (var i = 0; i < currentGroup.groupList.Count; i++)
			{
				short playerID = currentGroup.groupList[i].entityID;
				bool isSim = currentGroup.groupList[i].isSim;
				if (playerID != ClientConnectionManager.Instance.LocalPlayerID)
				{
					Entity player = Extensions.GetPlayerOrSimEntityByID(playerID);


					if (player == null)
					{
						//Logging.LogError($"Error adding {(isSim ? "Sim" : "Player")} with id {playerID}");
						continue;
					}

					SimPlayerTracking n;

					if (!isSim)
					{
						n = new SimPlayerTracking(player.entityName, 0f, player.zone, -(playerID + 1));
						n.MyAvatar = ((NetworkedPlayer)player).sim;
						n.MyAvatar.InGroup = true;
						((NetworkedPlayer)player).npc.InGroup = true;
						n.MyStats = n.MyAvatar.MyStats;
						n.Grouped = true;
					}
					else
					{
						if (player is NetworkedSim)
						{
							n = new SimPlayerTracking(player.entityName, 0f, player.zone, -(playerID + 1));
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
							n.Grouped = true;
						}
					}

					GameData.GroupMembers[plidx] = n;


					texts[plidx].text = isSim ? n.SimName : player.entityName;
					texts[plidx].color = Color.white;

					if (currentGroup.leaderID == ClientConnectionManager.Instance.LocalPlayerID)
						idk[plidx].SetActive(true);
					else
						idk[plidx].SetActive(false);
					plidx++;
				}
			}
		}
	}
}
