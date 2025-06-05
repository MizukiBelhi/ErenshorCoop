using System;
using System.Collections.Generic;
using ErenshorCoop.Shared;
using UnityEngine;
using UnityEngine.LowLevel;

namespace ErenshorCoop.Client
{
	public class DroppedItem : MonoBehaviour
	{

		public Item item;
		public int quality = 0;
		public string zone;
		public bool owner = false;
		public string id = "";

		private List<Item> _drops = new();

		public void Init(bool noRegister)
		{
			Instantiate(GameData.GM.SpecialLootBeam, transform.position + Vector3.up, transform.rotation).transform.SetParent(transform);

			if (!noRegister)
			{
				Variables.ItemDropData data = new()
				{
					item = item,
					quantity = quality,
					pos = transform.position,
					id = id
				};
				
				if (Variables.droppedItems.ContainsKey(zone))
					Variables.droppedItems[zone].Add(data);
				else
				{
					Variables.droppedItems[zone] = new() { data };
				}
			}


			if (item.RequiredSlot == Item.SlotType.General)
			{
				var max = Math.Min(8, quality);
				for (int i = 0; i < max; i++)
				{
					_drops.Add(item);
					quality--;
					if (i == 7) break;
				}
			}
			else
			{
				_drops.Add(item);
			}
		}

		public void OnDestroy()
		{
			ClientConnectionManager.Instance.SendItemLooted(id);
		}

		public void LoadLootTable()
		{

			Variables.lastDroppedItem = this;
			
			GameData.LootWindow.CloseWindow();
			LoadWindow(_drops);
		}

		public void LoadWindow(List<Item> LootItems)
		{
			GameHooks.downCD.SetValue(GameData.LootWindow, 5f);
			foreach (ItemIcon itemIcon in GameData.LootWindow.LootSlots)
			{
				itemIcon.Quantity = 1;
			}

			GameData.PlayerControl.LootDelay = 25f;

			GameData.LootWindow.LootSource.text = "Dropped Loot";
			GameData.LootWindow.WindowParent.SetActive(true);
			//GameData.LootWindow.parent = _incoming;
			GameData.PlayerInv.ForceOpenInv();
			GameData.GM.CloseAscensionWindow();
			GameData.LootWindow.LootButtonTxt.text = InputManager.Loot.ToString() + "  - Loot All";


			for (int i = 0; i < 8; i++)
			{
				if (i < LootItems.Count && LootItems[i] != null)
				{
					GameData.LootWindow.LootSlots[i].MyItem = LootItems[i];
					GameData.LootWindow.LootSlots[i].Quantity = quality;
				}
				else
				{
					GameData.LootWindow.LootSlots[i].MyItem = GameData.PlayerInv.Empty;
					GameData.LootWindow.LootSlots[i].Quantity = 1;

				}
				GameData.LootWindow.LootSlots[i].UpdateSlotImage();
			}
			GameData.PlayerControl.GetComponent<Animator>().ResetTrigger("EndLoot");
		}

		public void UpdateQuantity(int quant)
		{
			if (Variables.lastDroppedItem == this)
			{
				GameData.LootWindow.CloseWindow();
			}
			_drops.Clear();

			quality = quant;

			if (item.RequiredSlot == Item.SlotType.General && quality > 0)
			{
				var max = Math.Min(8, quality);
				for (int i = 0; i < max; i++)
				{
					_drops.Add(item);
				}
				quality -= max;
			}
		}

		public void ReturnLoot(List<Item> list)
		{
			_drops.Clear();
			_drops.AddRange(list);

			var sendQual = quality;
			if (item.RequiredSlot == Item.SlotType.General && _drops.Count < 8 && quality > 0)
			{
				var max = Math.Min(8 - _drops.Count, quality);
				for (int i = 0; i < max; i++)
				{
					_drops.Add(item);
				}
				quality -= max;
			}

			ClientConnectionManager.Instance.SendItemQuantityUpdate(id, sendQual);
		}
	}
}
