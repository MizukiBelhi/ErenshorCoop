using LiteNetLib;
using LiteNetLib.Utils;

namespace ErenshorCoop.Shared.Packets
{
	public class PlayerMessagePacket : EntityBasePacket
	{

		public MessageType messageType;
		public string message;
		public string target;
		public string color;
		public bool append;
		public bool isCombatLog;
		public short sender;
		public PlayerMessagePacket() : base(DeliveryMethod.ReliableOrdered) { }

		public override void Write(NetDataWriter writer)
		{
			writer.Put((byte)PacketType.PLAYER_MESSAGE);
			writer.Put(entityID);
			writer.Put((byte)messageType);
			writer.Put(message);
			if (messageType == MessageType.WHISPER)
				writer.Put(target);
			if(messageType == MessageType.BATTLE_LOG)
			{
				writer.Put(sender);
				writer.Put(color);
				writer.Put(append);
				writer.Put(isCombatLog);
			}
		}

		public override void Read(NetDataReader reader)
		{
			entityID = reader.GetShort();
			messageType = (MessageType)reader.GetByte();
			message = reader.GetString().Sanitize();
			if(messageType == MessageType.WHISPER)
				target = reader.GetString().Sanitize();
			if (messageType == MessageType.BATTLE_LOG)
			{
				sender = reader.GetShort();
				color = reader.GetString().Sanitize();
				append = reader.GetBool();
				isCombatLog = reader.GetBool();
			}
		}
	}
}
