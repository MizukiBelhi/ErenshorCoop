using ErenshorCoop.Client;
using ErenshorCoop.Server;
using ErenshorCoop.Shared;
using ErenshorCoop.Shared.Packets;


namespace ErenshorCoop.Modules
{

	[Module("Guilds")]
	public class GuildModule : Module
	{

		public override void OnLoad()
		{
			RegisterPacket<EntityGuildData>(PacketType.ENTITY_GUILD, false, 9);
		}

		public override (T, PacketType) OnReceiveClientPacket<T>(T packet, PacketType packetType)
		{
			if (packetType == PacketType.ENTITY_GUILD)
			{
				HandleGuildPacket((EntityGuildData)(object)packet);
				//only resend if we are host
				if (!ServerConnectionManager.Instance.IsRunning)
					return (null, PacketType.DONT_RESEND);
			}
			return (packet, packetType);
		}

		/*public override (T, PacketType) OnReceiveServerPacket<T>(T packet, PacketType packetType)
		{
			return (packet, packetType);
		}*/


		private void HandleGuildPacket(EntityGuildData p)
		{
			Entity entity;
			if(!p.isSim)
				entity = ClientConnectionManager.Instance.GetPlayerFromID(p.entityID);
			else
				entity = ClientNPCSyncManager.Instance.GetEntityFromID(p.entityID, true);

			if (!(bool)entity) return;

			if (entity == ClientConnectionManager.Instance.LocalPlayer) return;
			entity.UpdateGuildName(p.guildName);
		}

	}
}
