
using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ErenshorCoop.Shared
{

	public interface IPacket
	{
		void Write(NetDataWriter writer);
		void Read(NetPacketReader reader);
	}


	public enum PacketType : byte
	{
		SERVER_CONNECT,
		SERVER_INFO,
		DISCONNECT,
		PLAYER_CONNECT,
		PLAYER_DATA, // ReliableOrdered (Used mainly for gear/level/health updates)
		PLAYER_TRANSFORM, // Sequenced
		PLAYER_ACTION, // ReliableOrdered
		ENTITY_DATA, // ReliableOrdered
		ENTITY_SPAWN, // ReliableOrdered
		ENTITY_TRANSFORM, // Sequenced
		ENTITY_ACTION, // ReliableOrdered
		PLAYER_MESSAGE,// ReliableOrdered
		PLAYER_REQUEST,
		SERVER_REQUEST,
		
		GROUP,
		SERVER_GROUP,
		ITEM_DROP
	}

	public enum PacketEntityType : byte
	{
		PLAYER,
		MOB,
		NPC,
		SIM,
		MAX
	}

	public enum ItemDropType : byte
	{
		DROP,
		DESTROY,
		NEW_QUANTITY
	}

	public enum GroupDataType : byte
	{
		INVITE,
		INVITE_RESPONSE,
		ACCEPT_DECLINE,
		MEMBER_LIST,
		REMOVE,
		INVITE_SIM,
		EXPERIENCE
	}

	public enum GroupLeaveReason : byte
	{
		LEFT,
		KICKED,
	}

	public enum ConnectType : byte
	{
		SERVER_OK,
		PLAYER_SEND,
	}

	public enum MessageType : byte
	{
		SAY,
		GROUP,
		SHOUT,
		WHISPER,
		INFO,
	}

	public enum PlayerDataType : byte
	{
		POSITION,
		ROTATION,
		GEAR,
		HEALTH,
		SCENE,
		NAME,
		ANIM,
		LEVEL,
		CLASS,
		MP,
	}

	public enum EntityDataType : byte
	{
		POSITION,
		ROTATION,
		ANIM,
		HEALTH,
		SIM_REMOVE,
		ENTITY_REMOVE,
		MP
	}

	public enum ActionType : byte
	{
		ATTACK,
		DAMAGE_TAKEN,
		SPELL_CHARGE,
		SPELL_EFFECT,
		SPELL_END,
		REVIVE,
		STATUS_EFFECT_APPLY,
		STATUS_EFFECT_REMOVE,
		HEAL
	}
	public enum AnimatorSyncType : byte
	{
		BOOL,
		FLOAT,
		INT,
		TRIG,
		RSTTRIG,
		OVERRIDE
	}

	public enum ServerInfoType : byte
	{
		WEATHER,
		PVP_MODE,
		ZONE_OWNERSHIP,
	}

	public enum CustomSpawnID
	{
		MALAROTH = -1,
		CHESS = -2,
		SIRAETHE = -3,
		ADDS = -4,
	}

	public enum Request : byte
	{
		ENTITY_ID,
	}
}
