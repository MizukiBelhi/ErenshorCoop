using ErenshorCoop.Client;
using ErenshorCoop.Server;
using System;
using System.Collections.Generic;

namespace ErenshorCoop.Shared
{
	public class SharedGroup
	{

		public static bool IsPlayerHost(short playerID)
		{
			if (ServerConnectionManager.Instance.IsRunning && ClientConnectionManager.Instance.IsRunning && ClientConnectionManager.Instance.LocalPlayerID == playerID)
				return true;
			return false;
		}

		public class Member : IEquatable<Member>
		{
			public byte slot;
			public short entityID;
			public bool isSim;

			public Member(byte slot, short entityID, bool isSim)
			{
				this.slot = slot;
				this.entityID = entityID;
				this.isSim = isSim;
			}

			public bool Equals(Member other)
			{
				return entityID == other.entityID && isSim == other.isSim && slot == other.slot;
			}

			public override bool Equals(object obj)
			{
				return obj is Member other && Equals(other);
			}

			public override int GetHashCode()
			{
				unchecked { return (entityID.GetHashCode() * 397) ^ isSim.GetHashCode() * (slot.GetHashCode()*397); }
			}

			public override string ToString()
			{
				return $"Member(slot: {slot}, entityID: {entityID}, isSim: {isSim})";
			}
		}

		public class Group
		{
			public List<Member> groupList;
			public short leaderID;
			public List<Entity> internalList;

			public byte GetOpenSlot()
			{
				for (byte i = 0; i < GameData.GroupMembers.Length; i++)
				{
					bool found = false;
					foreach (Member member in groupList)
					{
						if (member.slot == i)
						{
							found = true;
							break;
						}
					}
					if (!found)
						return i;
				}
				return 255;
			}
		}

	}
}
