using ErenshorCoop.Client;
using ErenshorCoop.Client.Grouping;
using ErenshorCoop.Shared;
using ErenshorCoop.Shared.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ErenshorCoop.Server.Grouping
{
	public class ServerGroup
	{
		public static Dictionary<short, SharedGroup.Group> groups = new();
		
		private static short serverGroupID = 0;


		public static void AddGroup(SharedGroup.Group group)
		{
			groups.Add(++serverGroupID, group);
		}

		public static void RemoveGroup(short groupID)
		{
			groups.Remove(groupID);
		}

		public static SharedGroup.Group GetGroup(short groupID)
		{
			if (groups.TryGetValue(groupID, out var group))
				return group;
			return null;
		}


		public static short GetGroupIDFromPlayer(short playerID)
		{
			foreach (var group in groups)
			{
				foreach (SharedGroup.Member p in group.Value.groupList)
					if (p.entityID == playerID)
						return group.Key;
			}

			return -1;
		}

		internal static SharedGroup.Member GetMemberFromPlayer(short playerID, bool isSim = false)
		{
			foreach (var group in groups)
			{
				foreach (SharedGroup.Member p in group.Value.groupList)
					if (p.entityID == playerID && p.isSim == isSim)
						return p;
			}

			return null;
		}

		public static void Cleanup()
		{
			groups.Clear();
			serverGroupID = 0;
		}



		public static void HandleXP(short playerID, int xp, bool useMod, float xpBonus)
		{
			short groupID = GetGroupIDFromPlayer(playerID);
			if (groupID == -1) return; //no group??
			var group = groups[groupID];
			if (group.leaderID != playerID) return; //Not leader

			var mod = 1f;

			if (useMod)
			{
				switch (group.groupList.Count)
				{
					case 2:
						mod -= 0.25f;
						break;
					case 3:
						mod -= 0.45f;
						break;
					case 4:
						mod -= 0.55f;
						break;
					case 5:
						mod -= 0.65f;
						break;
					default:
					break;
				}
			}

			int xpEarned = Mathf.RoundToInt((float)xp * mod);

			foreach (var member in group.internalList)
			{
				if (SharedGroup.IsPlayerHost(member.entityID))
				{
					var p = new ServerGroupPacket();
					p.entityID = member.entityID;
					p.dataTypes.Add(GroupDataType.EXPERIENCE);
					p.earnedXP = xpEarned;
					p.xpBonus = xpBonus;
					ClientGroup.HandlePacket(p); //Hosts need to send packets to themselves
				}
				else
				{
					PacketManager.GetOrCreatePacket<ServerGroupPacket>(member.entityID, PacketType.SERVER_GROUP)
						.SetTarget(member)
						.AddPacketData(GroupDataType.EXPERIENCE, "earnedXP", xpEarned)
						.SetData("xpBonus", xpBonus);
				}
			}
		}

		public static void HandlePacket(GroupPacket packet)
		{
			foreach(var dt in packet.dataTypes)
			{
				switch(dt)
				{
					case GroupDataType.INVITE:
					//case GroupDataType.INVITE_SIM:
					case GroupDataType.ACCEPT_DECLINE:
					case GroupDataType.REMOVE:
						Invites.HandlePacket(dt, packet);
						break;
					case GroupDataType.EXPERIENCE:
						HandleXP(packet.entityID, packet.xp, packet.useMod, packet.xpBonus);
						break;
				}
			}

		}

		public static void ForceRemoveFromGroup(short playerID, bool isSim = false)
		{
			if (ServerConnectionManager.Instance.IsRunning)
			{
				var packet = new GroupPacket();
				packet.dataTypes.Add(GroupDataType.REMOVE);
				packet.playerID = playerID;
				packet.reason = GroupLeaveReason.KICKED;
				packet.entityID = ClientConnectionManager.Instance.LocalPlayerID;
				packet.isSim = isSim;
				HandlePacket(packet); //Host send to self
			}
		}


	}
}
