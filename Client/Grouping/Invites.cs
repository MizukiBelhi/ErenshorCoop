using ErenshorCoop.Server;
using ErenshorCoop.Server.Grouping;
using ErenshorCoop.Shared;
using ErenshorCoop.Shared.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErenshorCoop.Client.Grouping
{
	public class Invites
	{

		public static short ClientInviteID = -1;

		public static void Cleanup()
		{
			ClientInviteID = -1;
		}

		public static bool InvitePlayer(Entity simPlayer)
		{
			if (ClientGroup.currentGroup.leaderID != -1 && ClientGroup.currentGroup.leaderID != ClientConnectionManager.Instance.LocalPlayerID)
			{
				Logging.WriteInfoMessage("You are not the group leader.");
				return false;
			}

			Logging.Log($"Try inv {simPlayer.name} {simPlayer.entityID}");

			if (ServerConnectionManager.Instance.IsRunning)
			{
				//Host send to self
				Server.Grouping.Invites.InvitePlayer(ClientConnectionManager.Instance.LocalPlayerID, simPlayer.entityID, true, false, simPlayer.type == EntityType.SIM);
				return true;
			}
			PacketManager.GetOrCreatePacket<GroupPacket>(ClientConnectionManager.Instance.LocalPlayerID, PacketType.GROUP)
				.AddPacketData(GroupDataType.INVITE, "playerID", simPlayer.entityID).SetData("isSim", simPlayer.type == EntityType.SIM);
			return true;
		}


		public static void AcceptInvite(short inviteID)
		{
			if (ServerConnectionManager.Instance.IsRunning)
			{
				var packet = new GroupPacket();
				packet.dataTypes.Add(GroupDataType.ACCEPT_DECLINE);
				packet.inviteID = inviteID;
				packet.inviteAccept = true;
				packet.entityID = ClientConnectionManager.Instance.LocalPlayerID;
				ServerGroup.HandlePacket(packet); //Host send to self
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
				packet.entityID = ClientConnectionManager.Instance.LocalPlayerID;
				ServerGroup.HandlePacket(packet); //Host send to self
				return;
			}

			PacketManager.GetOrCreatePacket<GroupPacket>(ClientConnectionManager.Instance.LocalPlayerID, PacketType.GROUP)
				.AddPacketData(GroupDataType.ACCEPT_DECLINE, "inviteAccept", false)
				.SetData("inviteID", inviteID);
		}

		public static void LeaveGroup()
		{
			if (ServerConnectionManager.Instance.IsRunning)
			{
				var p = new GroupPacket();
				p.entityID = ClientConnectionManager.Instance.LocalPlayerID;
				p.dataTypes.Add(GroupDataType.REMOVE);
				p.playerID = ClientConnectionManager.Instance.LocalPlayerID;
				p.reason = GroupLeaveReason.LEFT;
				ServerGroup.HandlePacket(p); //Host send to self
			}
			else
			{

				PacketManager.GetOrCreatePacket<GroupPacket>(ClientConnectionManager.Instance.LocalPlayerID, PacketType.GROUP)
				.AddPacketData(GroupDataType.REMOVE, "reason", GroupLeaveReason.LEFT)
				.SetData("playerID", ClientConnectionManager.Instance.LocalPlayerID);
			}

			//currentGroup = new();
		}


		public static void HandlePacket(GroupDataType groupDataType, ServerGroupPacket packet)
		{
			switch (groupDataType)
			{
				case GroupDataType.INVITE:
					short leaderID = packet.leaderID;
					var leader = ClientConnectionManager.Instance.GetPlayerFromID(leaderID);
					ClientInviteID = packet.inviteID;


					UI.Main.EnablePrompt($"{leader.entityName} has invited you to join their group.", () => { AcceptInvite(ClientInviteID); }, () => { DeclineInvite(ClientInviteID); });
					break;
			}
		}
	}
}
