using ErenshorCoop.Steam;
using LiteNetLib;
using LiteNetLib.Utils;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ErenshorCoop.Shared.Packets
{

	public class EntityBasePacket : BasePacket
	{
		public EntityType entityType;
		public string zone;
		
		public EntityBasePacket(DeliveryMethod deliveryMethod) : base(deliveryMethod) {}
	}
	public class BasePacket : IPacket
	{
		public bool canSend = false;
		public bool hasSend = false;
		public DeliveryMethod deliveryMethod;
		public NetPeer peer;
		public CSteamID steamPeer;
		public bool singleTarget = false;
		public List<NetPeer> exclusions = new();
		public List<CSteamID> steamExclusions = new();
		public short entityID;
		public List<short> targetPlayerIDs;
		public bool isSim = false;

		public BasePacket(DeliveryMethod deliveryMethod) { this.deliveryMethod = deliveryMethod; }

		public virtual void Write(NetDataWriter writer)
		{
			writer.Put(entityID);
		}

		public virtual void Read(NetDataReader reader)
		{
			entityID = reader.GetShort();
		}

		public void CanSend()
		{
			canSend = true;
		}

		public BasePacket SetTarget(Entity peer)
		{
			if (Lobby.isInLobby)
				this.steamPeer = peer.steamID;
			else
				this.peer = peer.peer;
			singleTarget = true;
			return this;
		}

		public BasePacket SetSteamTarget(CSteamID peer)
		{
			this.steamPeer = peer;
			singleTarget = true;
			return this;
		}
		public BasePacket SetPeerTarget(NetPeer peer)
		{
			this.peer = peer;
			singleTarget = true;
			return this;
		}

		public BasePacket AddPacketData<T>(T type, string fieldName, object value) where T : Enum
		{
			var targetField = GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (targetField == null)
			{
				Logging.LogError($"Field '{fieldName}' not found.");
				return this;
			}

			targetField.SetValue(this, value);

			// Find the matching dataTypes field
			var allFields = GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (var field in allFields)
			{
				var fieldType = field.FieldType;
				if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(HashSet<>))
				{
					var genericArg = fieldType.GetGenericArguments()[0];
					if (genericArg == typeof(T))
					{
						object dataTypesSet = field.GetValue(this);
						var addMethod = fieldType.GetMethod("Add");
						var containsMethod = fieldType.GetMethod("Contains");
						var doesContain = (bool)containsMethod?.Invoke(dataTypesSet, new object[] { type })!;
						if (!doesContain)
							addMethod?.Invoke(dataTypesSet, new object[] { type });
						addMethod?.Invoke(dataTypesSet, new object[] { type });
						return this;
					}
				}
			}

			Logging.LogError($"No HashSet<{typeof(T).Name}> field found in {GetType().Name}");
			return this;
		}

		public BasePacket AddType<T>(T type)
		{
			var allFields = GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (var field in allFields)
			{
				var fieldType = field.FieldType;
				if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(HashSet<>))
				{
					var genericArg = fieldType.GetGenericArguments()[0];
					if (genericArg == typeof(T))
					{
						object dataTypesSet = field.GetValue(this);
						var addMethod = fieldType.GetMethod("Add");
						var containsMethod = fieldType.GetMethod("Contains");
						var doesContain = (bool)containsMethod?.Invoke(dataTypesSet, new object[] { type })!;
						if(!doesContain)
							addMethod?.Invoke(dataTypesSet, new object[] { type });
						return this;
					}
				}
			}

			return this;
		}

		public BasePacket SetData(string fieldName, object value)
		{
			var targetField = GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (targetField == null)
			{
				Logging.LogError($"Field '{fieldName}' not found.");
				return this;
			}

			targetField.SetValue(this, value);
			return this;
		}
	}
}
