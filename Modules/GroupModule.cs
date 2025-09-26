using ErenshorCoop.Client.Grouping;
using ErenshorCoop.Server.Grouping;
using ErenshorCoop.Shared;
using ErenshorCoop.Shared.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErenshorCoop.Modules
{

	[Module("Groups")]
	public class GroupModule : Module
	{

		public override void OnLoad()
		{
			RegisterPacket<ServerGroupPacket>(PacketType.SERVER_GROUP, false, 9);
			RegisterPacket<GroupPacket>(PacketType.GROUP, true, 9);
		}
		public override (T, PacketType) OnReceiveClientPacket<T>(T packet, PacketType packetType)
		{
			if (packetType == PacketType.SERVER_GROUP)
			{
				ClientGroup.HandlePacket((ServerGroupPacket)(object)packet);
				return (null, PacketType.DONT_RESEND);
			}
			return (packet, packetType);
		}

		public override (T, PacketType) OnReceiveServerPacket<T>(T packet, PacketType packetType)
		{
			if (packetType == PacketType.GROUP)
			{
				ServerGroup.HandlePacket((GroupPacket)(object)packet);
				return (null, PacketType.DONT_RESEND);
			}
			return (packet, packetType);
		}

	}
}
