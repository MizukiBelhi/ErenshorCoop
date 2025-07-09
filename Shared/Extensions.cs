using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ErenshorCoop.Client;
using ErenshorCoop.Server;
using UnityEngine;
using System.Text.RegularExpressions;

namespace ErenshorCoop.Shared
{
	public static class Extensions
	{

		public static void Put(this NetDataWriter writer, Vector3 vec)
		{
			writer.Put(vec.x);
			writer.Put(vec.y);
			writer.Put(vec.z);
		}
		public static void Put(this NetDataWriter writer, Quaternion rot)
		{
			writer.Put(rot.x);
			writer.Put(rot.y);
			writer.Put(rot.z);
			writer.Put(rot.w);
		}
		public static void Put(this NetDataWriter writer, Color col, bool useAlpha = false)
		{
			writer.Put(col.r);
			writer.Put(col.g);
			writer.Put(col.b);
			if(useAlpha)
				writer.Put(col.a);
		}

		public static Vector3 GetVector3(this NetDataReader reader)
		{
			return new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
		}
		public static Quaternion GetRotation(this NetDataReader reader)
		{
			return new Quaternion(reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
		}
		public static Color GetColor(this NetDataReader reader, bool useAlpha = false)
		{
			var col = new Color(reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), 255);
			if(useAlpha)
				col.a = reader.GetFloat();
			return col;
		}

		public static void WriteObject(NetDataWriter writer, object data)
		{
			var type = data.GetType();

			if (type == typeof(int))
				writer.Put((int)data);
			else if (type == typeof(float))
				writer.Put((float)data);
			else if (type == typeof(bool))
				writer.Put((bool)data);
			else if (type == typeof(string))
				writer.Put((string)data);
			else if (type == typeof(byte))
				writer.Put((byte)data);
		}

		public static ushort GetSubTypeFlag<T>(HashSet<T> dataTypes) where T: Enum
		{
			ushort val = 1;
			ushort subTypeFlag = 0;
			foreach (T enumVal in Enum.GetValues(typeof(T)))
			{
				if (dataTypes.Contains(enumVal))
					subTypeFlag |= val;
				val *= 2;
			}
			return subTypeFlag;
		}

		public static HashSet<T> ReadSubTypeFlag<T>(ushort flag) where T : Enum
		{
			var dataTypes = new HashSet<T>();
			ushort val = 1;
			foreach (T enumVal in Enum.GetValues(typeof(T)))
			{
				if ((flag & val) != 0)
					dataTypes.Add(enumVal);
					
				val *= 2;
			}
			return dataTypes;
		}

		public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
		{
			var component = gameObject.GetComponent<T>();
			if (component == null)
				component = gameObject.AddComponent<T>();
			return component;
		}

		public static (bool isPlayer, Character character) GetCharacterFromID(bool isNPC, short entityID, bool isSim)
		{
			Character character = null;
			bool isPlayer = false;
			//if (isNPC || isSim)
			{
				var ent = ClientNPCSyncManager.Instance.GetEntityFromID(entityID, isSim);
				character = ent != null ? ent.character : null;

				if (character == null)
				{
					ent = SharedNPCSyncManager.Instance.GetEntityFromID(entityID, isSim);
					character = ent != null ? ent.character : null;
				}
				
				if (ClientConnectionManager.Instance.LocalPlayer.MySummon != null)
				{
					if (ClientConnectionManager.Instance.LocalPlayer.MySummon.entityID == entityID)
						character = ClientConnectionManager.Instance.LocalPlayer.MySummon.character;
				}
			}
			if(!isNPC && !isSim)
			{
				if (entityID == ClientConnectionManager.Instance.LocalPlayerID)
					character = ClientConnectionManager.Instance.LocalPlayer.character;
				else
				{
					var ent = ClientConnectionManager.Instance.GetPlayerFromID(entityID);
					character = ent != null ? ent.character : null;
				}

				isPlayer = true;
			}

			return ( isPlayer, character );
		}

		public static (bool isPlayer, Entity entity) GetEntityFromID(bool isNPC, short entityID, bool isSim)
		{
			Entity character = null;
			bool isPlayer = false;
			//if (isNPC || isSim)
			{
				//if (!ClientZoneOwnership.isZoneOwner)
				{
					var ent = ClientNPCSyncManager.Instance.GetEntityFromID(entityID, isSim);
					character = ent != null ? ent : null;

				}
				//else
				{
					var ent = SharedNPCSyncManager.Instance.GetEntityFromID(entityID, isSim);
					character = ent != null ? ent : null;
				}
				if (ClientConnectionManager.Instance.LocalPlayer.MySummon != null)
				{
					if (ClientConnectionManager.Instance.LocalPlayer.MySummon.entityID == entityID)
						character = ClientConnectionManager.Instance.LocalPlayer.MySummon;
				}
			}
			if(!isNPC && !isSim)
			{
				if (entityID == ClientConnectionManager.Instance.LocalPlayerID)
					character = ClientConnectionManager.Instance.LocalPlayer;
				else
				{
					var ent = ClientConnectionManager.Instance.GetPlayerFromID(entityID);
					character = ent != null ? ent : null;
				}

				isPlayer = true;
			}

			return (isPlayer, character);
		}

		public static Entity GetEntityByID(short entityID)
		{
			var ent = ClientConnectionManager.Instance.GetPlayerFromID(entityID);
			if (ent == null)
				ent = SharedNPCSyncManager.Instance.GetEntityFromID(entityID, false);
			if (ent == null)
				ent = SharedNPCSyncManager.Instance.GetEntityFromID(entityID, true);
			if (ent == null)
				ent = ClientNPCSyncManager.Instance.GetEntityFromID(entityID, false);
			if (ent == null)
				ent = ClientNPCSyncManager.Instance.GetEntityFromID(entityID, true);
			if (ent == null) return null;
			return ent;
		}

		public static Entity GetPlayerOrSimEntityByID(short entityID)
		{
			var ent = ClientConnectionManager.Instance.GetPlayerFromID(entityID);
			if (ent == null)
				ent = SharedNPCSyncManager.Instance.GetEntityFromID(entityID, true);
			if (ent == null)
				ent = ClientNPCSyncManager.Instance.GetEntityFromID(entityID, true);
			if (ent == null) return null;
			return ent;
		}

		public static Dictionary<string, AnimationClip> clipLookup = new();
		public static void BuildClipLookup(NPC npc)
		{
			void AddClip(AnimationClip clip)
			{
				if (clip != null && !clipLookup.ContainsKey(clip.name))
					clipLookup.Add(clip.name, clip);
			}

			AddClip(npc.TwoHandSwordIdle);
			AddClip(npc.TwoHandStaffIdle);
			AddClip(npc.TwoHandStaffWalk);
			AddClip(npc.TwoHandStaffRun);
			AddClip(npc.TwoHandSwordWalk);
			AddClip(npc.TwoHandSwordRun);
			AddClip(npc.ArmedIdle);
			AddClip(npc.RelaxedIdle);
			AddClip(npc.Run);
			AddClip(npc.Walk);
		}

		public static void BuildPlayerClipLookup(PlayerControl npc, PlayerCombat pc)
		{
			void AddClip(AnimationClip clip)
			{
				if (clip != null && !clipLookup.ContainsKey(clip.name))
					clipLookup.Add(clip.name, clip);
			}

			AddClip(npc.SwimAhead);
			AddClip(npc.SwimIdle);
			AddClip(npc.RelaxedIdle);
			AddClip(npc.Sprint);
			AddClip(npc.Jog);
			AddClip(npc.JogArmed);
			AddClip(npc.RelaxedIdle);
			AddClip(pc.ArmedIdle);
			AddClip(pc.TwoHandSwordIdle);
			AddClip(pc.TwoHandStaffIdle);
			AddClip(pc.RelaxedIdle);
		}


		public static int StableHash(string text)
		{
			unchecked
			{
				const int fnvPrime = 16777619;
				var hash = (int)2166136261;

				foreach (char c in text)
				{
					hash ^= c;
					hash *= fnvPrime;
				}

				return hash;
			}
		}
		public static int GenerateHash(Vector3 pos, string name)
		{
			int x = Mathf.RoundToInt((pos.x * 1000f)/1000f);
			int y = Mathf.RoundToInt((pos.y * 1000f)/1000f);
			int z = Mathf.RoundToInt((pos.z * 1000f)/1000f);
			int nameHash = StableHash(name);

			unchecked
			{
				var hash = 17;
				hash = hash * 23 + x;
				hash = hash * 23 + y;
				hash = hash * 23 + z;
				hash = hash * 23 + nameHash;
				return hash & 0x7FFFFFFF;
			}
		}

		private static Regex richText = new(@"<.*?>", RegexOptions.Compiled);

		public static string Sanitize(this string input, int maxLength = 256)
		{
			if (string.IsNullOrEmpty(input))
				return string.Empty;


			string clean = input.Replace("<", "&lt;").Replace(">", "&gt;");
			//Hardcore removal of tags
			//string clean = richText.Replace(input, "");
			clean = clean.Where(c => !char.IsControl(c)).Aggregate("", (s, c) => s + c);
			return clean.Length > maxLength ? clean.Substring(0, maxLength) : clean;
		}
	}
}
