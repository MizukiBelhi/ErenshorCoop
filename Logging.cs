using BepInEx.Logging;
using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ErenshorCoop.Shared;
using ErenshorCoop.Shared.Packets;
using UnityEngine;
using ErenshorCoop.Client;
using ErenshorCoop.Client.Grouping;

namespace ErenshorCoop
{
	public static class Logging
	{
		static ManualLogSource logger;

		public static void Log(string message)
		{
			logger ??= ErenshorCoopMod.logger;

			logger.LogMessage(message);

			//if(GameData.ChatLog != null)
			//	UpdateSocialLog.CombatLogAdd($"[COOP] {message}");
		}

		public static void LogError(string message)
		{
			logger ??= ErenshorCoopMod.logger;

			logger.LogError(message);

			//if (GameData.ChatLog != null)
			//	UpdateSocialLog.CombatLogAdd($"[COOP] {message}", "red");
		}

		public static void LogGameMessage(string message, bool error=false)
		{
			if (GameData.ChatLog != null)
				UpdateSocialLog.CombatLogAdd($"[COOP] {message}", error?"red":GameData.ReadableBlue);
		}

		public static void HandleMessage(Entity from, PlayerMessagePacket packet, Entity battleFrom=null)
		{
			MessageType messageType = packet.messageType;
			string mes = packet.message;
			string target = "";
			if (messageType == MessageType.WHISPER)
				target = packet.target;

			switch(messageType)
			{
				case MessageType.SAY:
					UpdateSocialLog.LogAdd($"{from.entityName} says: {mes}");
					UpdateSocialLog.LocalLogAdd($"{from.entityName} says: {mes}");
					break;
				case MessageType.GROUP:
					if(ClientGroup.HasGroup && ClientGroup.IsPlayerInGroup(from.entityID, false))
						UpdateSocialLog.LogAdd($"{from.entityName} tells the group: {mes}", "#00B2B7");
					break;
				case MessageType.SHOUT:
					UpdateSocialLog.LogAdd($"{from.entityName} shouts: {mes}", "orange");
					break;
				case MessageType.WHISPER:
					//if (Networking.localPlayer == null) return;
					if(target == GameData.CurrentCharacterSlot.CharName)
					{
						UpdateSocialLog.LogAdd($"[WHISPER FROM] {from.entityName}: {mes}", "#FB09FF");
						UpdateSocialLog.LocalLogAdd($"[WHISPER FROM] {from.entityName}: {mes}", "#FB09FF");
						GameData.TextInput.LastPlayerMsg = from.entityName;
						GameData.PlayerAud.PlayOneShot(GameData.Misc.ReceiveTell, GameData.PlayerAud.volume * 0.5f * GameData.SFXVol);
					}
					break;
				case MessageType.INFO:
					WriteInfoMessage(mes);
					break;
				case MessageType.BATTLE_LOG:
					if (from == null || from != ClientConnectionManager.Instance.LocalPlayer) break;
					Variables.DontResendMessageEnts.Add(battleFrom);
					var isAppend = packet.append;
					var hasColor = !string.IsNullOrEmpty(packet.color);
					if (packet.isCombatLog)
					{
						if (hasColor)
							UpdateSocialLog.CombatLogAdd(mes, packet.color);
						else
							UpdateSocialLog.CombatLogAdd(mes);
					}
					else
					{
						if (hasColor && isAppend)
							UpdateSocialLog.LogAdd(mes, packet.color, packet.append);
						else if (hasColor && !isAppend)
							UpdateSocialLog.LogAdd(mes, packet.color);
						else if (!hasColor && isAppend)
							UpdateSocialLog.LogAdd(mes, isAppend);
						else
							UpdateSocialLog.LogAdd(mes);
					}
					Variables.DontResendMessageEnts.Remove(from);
					break;
			}
		}

		public static void WriteInfoMessage(string message)
		{
			if (GameData.ChatLog != null)
				UpdateSocialLog.LogAdd(message);
		}
	}
}
