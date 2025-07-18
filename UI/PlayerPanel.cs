using ErenshorCoop.Client;
using ErenshorCoop.Server;
using ErenshorCoop.Shared;
using ErenshorCoop.Shared.Packets;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ErenshorCoop.UI
{
	public class PlayerPanel : IPanel
	{
		public static float yPos;

		private static float width;
		private static float height;

		public static GameObject playerListPanel;
		public static GameObject inviteFriendsButton;
		private static Canvas canvas;
		public GameObject CreatePanel(Transform parent, float _width, float _height, Canvas _canvas = null)
		{
			canvas = _canvas;
			playerListPanel = parent.gameObject;
			yPos = LobbyPanel.yPos;
			width = _width;
			height = _height;


			inviteFriendsButton = Base.AddButton(parent, new Vector2((width / 2) - 50, -height + 35f), new Vector2(100, 30), "Invite Friends", InviteFriends).gameObject;

			return null;
		}

		public class PlayerInfoObject
		{
			public GameObject go;
			public TMP_Text text;
			public Image pingIcon;
			public Image modIcon;
			public Text tooltip;
		}

		static private Dictionary<string, PlayerInfoObject> playerLabels = new();
		//FIXME: Needs to be somewhere else, really. Doesn't really matter either way, all the calls are protected host-side
		private static bool areWeMod = false;

		public static void ClearPlayerInfo()
		{
			Steam.Networking.lastPlayerData.Clear();
			foreach (var kp in playerLabels)
			{
				UnityEngine.Object.Destroy(kp.Value.go);
			}
			playerLabels.Clear();
		}
		public static void RefreshPlayerInfo(bool force=false)
		{
			if (contextMenu != null && (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)))
			{
				Vector2 mousePos = Input.mousePosition;
				if (!RectTransformUtility.RectangleContainsScreenPoint(contextMenuRT, mousePos, canvas.worldCamera))
				{
					UnityEngine.Object.Destroy(contextMenu);
					contextMenu = null;
				}
			}

			//if (playerListPanel == null) return;
			//if (!playerListPanel.activeSelf) return;
			var collected = false;
			if (!force)
			{
				if (!Steam.Lobby.isInLobby) return;

				
				if (Steam.Lobby.isInLobby && Steam.Lobby.isLobbyHost)
				{
					collected = Steam.Networking.CollectPlayerData();
				}
				else if (Steam.Lobby.isInLobby)
				{
					if (Steam.Networking.lastPlayerData != null)
						collected = true;
				}
			}
			else
			{
				collected = true;
			}

			if (collected && playerListPanel != null && playerListPanel.activeSelf)
			{
				var keysToRemove = new List<string>();
				var newPlayers = Steam.Networking.lastPlayerData;

				foreach (var existing in playerLabels)
				{
					bool stillExists = newPlayers.Any(p => p.name == existing.Key);
					if (!stillExists)
						keysToRemove.Add(existing.Key);
				}

				foreach (var key in keysToRemove)
				{
					if (playerLabels.TryGetValue(key, out var label))
						UnityEngine.Object.Destroy(label.go);

					playerLabels.Remove(key);
				}

				areWeMod = false;
				foreach (var pl in newPlayers)
				{
					if (pl.isMod && pl.playerID == ClientConnectionManager.Instance.LocalPlayerID)
						areWeMod = true;

					if (Steam.Lobby.isLobbyHost || (pl.isHost && pl.playerID == ClientConnectionManager.Instance.LocalPlayerID))
						areWeMod = true;

					if (string.IsNullOrEmpty(pl.name)) continue;

					if (!playerLabels.ContainsKey(pl.name))
					{
						float y = yPos;

						var colorB = new ColorBlock
						{
							normalColor = Color.white,
							selectedColor = new Color(0.9608f, 0.9608f, 0.9608f, 0.5f),
							disabledColor = new Color(0.7843f, 0.7843f, 0.7843f, 0.502f),
							pressedColor = new Color(0.8833f, 1f, 0f, 1f),
							highlightedColor = new Color(0.1114f, 1f, 0f, 1f),
							fadeDuration = 0.1f,
							colorMultiplier = 1f
						};


						var g = new GameObject("plLabel");
						g.transform.SetParent(playerListPanel.transform, false);
						RectTransform rt = g.AddComponent<RectTransform>();
						rt.sizeDelta = new Vector2(width, 30);
						rt.anchorMin = new Vector2(0, 1);
						rt.anchorMax = new Vector2(0, 1);
						rt.pivot = new Vector2(0, 1);
						rt.anchoredPosition = new Vector2(0, y - 20);



						Image im = Base.AddImage(g.transform, new Vector2(5, 0), new Vector2(width - 10, 30), null);
						im.color = new Color(1, 1, 1, 0.25f);



						Toggle but = g.AddComponent<Toggle>();
						but.image = im;
						but.isOn = false;

						but.colors = colorB;



						var trigger = im.gameObject.AddComponent<EventTrigger>();

						EventTrigger.Entry entry = new()
						{
							eventID = EventTriggerType.PointerClick
						};
						entry.callback.AddListener((eventData) =>
						{
							if (!areWeMod && !Steam.Lobby.isLobbyHost) return;
							if (pl.playerID == ClientConnectionManager.Instance.LocalPlayerID) return;

							var ped = (PointerEventData)eventData;
							if (ped.button == PointerEventData.InputButton.Right)
							{
								ShowContextMenu(ped.position, pl.playerID);
							}
						});

						trigger.triggers.Add(entry);


						var img = Base.AddImage(g.transform, new Vector2(width - 25, -7), new Vector2(16, 16), Base.pingIcon);
						img.raycastTarget = false;

						if (pl.ping > 100 && pl.ping < 200)
						{
							img.color = Color.yellow;
						}
						else if (pl.ping < 100)
						{
							img.color = Color.green;
						}
						else
						{
							img.color = Color.red;
						}

						var f = Base.AddLabel(g.transform, new Vector2(10, 0), new Vector2(width - 60, 30), $"{pl.zone}", 20f);
						f.raycastTarget = false;
						f.alignment = TextAlignmentOptions.Left;

						Sprite imgSprite = null;
						if (pl.isMod)
							imgSprite = Base.starIcon;
						if (pl.isHost)
							imgSprite = Base.crownIcon;
						if (pl.isDev)
							imgSprite = Base.bugIcon;

						Image modIcon = null;

						Text tooltipText = null;

						if (imgSprite != null)
						{
							modIcon = Base.AddImage(g.transform, new Vector2(200 - 16, -7), new Vector2(16, 16), imgSprite);
							modIcon.raycastTarget = true;
							modIcon.color = Color.white;

							var tooltipGO = new GameObject("Tooltip");
							tooltipGO.transform.SetParent(modIcon.transform, false);

							tooltipText = tooltipGO.AddComponent<Text>();
							tooltipText.text = (pl.isDev ? "Mod Developer" : pl.isHost ? "Host" : "Moderator");
							tooltipText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
							tooltipText.fontSize = 16;
							tooltipText.color = Color.white;
							tooltipText.alignment = TextAnchor.MiddleCenter;
							tooltipText.raycastTarget = false;


							rt = tooltipGO.GetComponent<RectTransform>();
							rt.sizeDelta = new Vector2(150, 20);
							rt.anchoredPosition = new Vector2(0, 20);

							tooltipGO.SetActive(false);

							var trig = modIcon.gameObject.AddComponent<EventTrigger>();

							var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
							enter.callback.AddListener((e) => { tooltipGO.SetActive(true); });
							trig.triggers.Add(enter);

							var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
							exit.callback.AddListener((e) => { tooltipGO.SetActive(false); });
							trig.triggers.Add(exit);

						}

						f = Base.AddLabel(g.transform, new Vector2(10 + 200, 0), new Vector2(width - 60, 30), $"{pl.name}", 20f);
						f.raycastTarget = false;
						f.alignment = TextAlignmentOptions.Left;

						var z = Base.AddLabel(g.transform, new Vector2(30, 0), new Vector2(width - 60, 30), $"{pl.ping}", 20f);
						z.raycastTarget = false;
						z.alignment = TextAlignmentOptions.Right;

						playerLabels.Add(pl.name, new() { go = g, text = z, pingIcon = img, modIcon = modIcon, tooltip = tooltipText });
						y -= 35;
					}
					else
					{
						if (pl.ping > 100 && pl.ping < 200)
						{
							playerLabels[pl.name].pingIcon.color = Color.yellow;
						}
						else if (pl.ping < 100)
						{
							playerLabels[pl.name].pingIcon.color = Color.green;
						}
						else
						{
							playerLabels[pl.name].pingIcon.color = Color.red;
						}
						playerLabels[pl.name].text.text = $"{pl.ping}";

						Sprite imgSprite = null;
						if (pl.isMod)
							imgSprite = Base.starIcon;
						if (pl.isHost)
							imgSprite = Base.crownIcon;
						if (pl.isDev)
							imgSprite = Base.bugIcon;

							
						if (imgSprite != null && playerLabels[pl.name].modIcon != null)
						{
							playerLabels[pl.name].modIcon.sprite = imgSprite;
							playerLabels[pl.name].tooltip.text = (pl.isDev ? "Mod Developer" : pl.isHost ? "Host" : "Moderator");

						}
						else if(imgSprite != null && playerLabels[pl.name].modIcon == null)
						{
							playerLabels[pl.name].modIcon = Base.AddImage(playerLabels[pl.name].go.transform, new Vector2(200 - 16, -7), new Vector2(16, 16), imgSprite);
							playerLabels[pl.name].modIcon.raycastTarget = true;
							playerLabels[pl.name].modIcon.color = Color.white;

							var tooltipGO = new GameObject("Tooltip");
							tooltipGO.transform.SetParent(playerLabels[pl.name].modIcon.transform, false);

							var tooltipText = tooltipGO.AddComponent<Text>();
							tooltipText.text = (pl.isDev ? "Mod Developer" : pl.isHost ? "Host" : "Moderator");
							tooltipText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
							tooltipText.fontSize = 16;
							tooltipText.color = Color.white;
							tooltipText.alignment = TextAnchor.MiddleCenter;
							tooltipText.raycastTarget = false;

							playerLabels[pl.name].tooltip = tooltipText;
							var rt = tooltipGO.GetComponent<RectTransform>();
							rt.sizeDelta = new Vector2(150, 20);
							rt.anchoredPosition = new Vector2(0, 20);

							tooltipGO.SetActive(false);

							var trig = playerLabels[pl.name].modIcon.gameObject.AddComponent<EventTrigger>();

							var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
							enter.callback.AddListener((e) => { tooltipGO.SetActive(true); });
							trig.triggers.Add(enter);

							var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
							exit.callback.AddListener((e) => { tooltipGO.SetActive(false); });
							trig.triggers.Add(exit);
						}
						else if (imgSprite == null && playerLabels[pl.name].modIcon != null)
						{
							UnityEngine.Object.Destroy(playerLabels[pl.name].modIcon.gameObject);
							playerLabels[pl.name].modIcon = null;
						}
					}

				}

				float y2 = yPos;
				foreach (var kvp in playerLabels)
				{
					var rt = kvp.Value.go.GetComponent<RectTransform>();
					if (rt != null)
						rt.anchoredPosition = new Vector2(0, y2 - 20);
					y2 -= 35;
				}
			}

		}

		private static GameObject contextMenu;
		private static RectTransform contextMenuRT;
		private static void ShowContextMenu(Vector2 screenPosition, short playerID)
		{
			if(contextMenu != null)
			{
				UnityEngine.Object.Destroy(contextMenu);
			}

			var menu = new GameObject("ContextMenu", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			menu.transform.SetParent(canvas.transform, false);

			contextMenu = menu;

			var rt = menu.GetComponent<RectTransform>();
			rt.sizeDelta = new Vector2(100, Steam.Lobby.isLobbyHost?78:52);
			rt.pivot = new Vector2(0, 1);
			rt.position = screenPosition;

			contextMenuRT = rt;

			var bg = menu.GetComponent<Image>();
			bg.color = new Color(0, 0, 0, 0.8f);

			var ent = ClientConnectionManager.Instance.GetPlayerFromID(playerID);
			if(ent == null || playerID == ClientConnectionManager.Instance.LocalPlayerID)
				UnityEngine.Object.Destroy(contextMenu);


			var b = CreateButton("Kick", new Vector2(0, 0), menu.transform, () =>
			{
				Main.EnablePrompt($"Are you sure you want to kick {ent.entityName}?", () => { Logging.Log($"Kicked {ent.entityName}"); SendKickBan(true, ent.entityName.ToLower()); }, () => { });
				UnityEngine.Object.Destroy(menu);
			});

			b = CreateButton("Ban", new Vector2(0, -26), menu.transform, () =>
			{
				Main.EnablePrompt($"Are you sure you want to ban {ent.entityName}?", () => { Logging.Log($"Banned {ent.entityName}"); SendKickBan(false, ent.entityName.ToLower()); }, () => { });
				UnityEngine.Object.Destroy(menu);
			});

			if (Steam.Lobby.isLobbyHost)
			{
				if (!ServerConfig.ModeratorList.Contains(ent.steamID.m_SteamID))
				{
					b = CreateButton("Make Mod", new Vector2(0, -52), menu.transform, () =>
					{
						Main.EnablePrompt($"Are you sure you want to promote {ent.entityName}?", () => { Logging.Log($"Promoted {ent.entityName}"); AddModerator(ent.steamID); }, () => { });
						UnityEngine.Object.Destroy(menu);
					});
				}
				else
				{
					b = CreateButton("Remove Mod", new Vector2(0, -52), menu.transform, () =>
					{
						Main.EnablePrompt($"Are you sure you want to demote {ent.entityName}?", () => { Logging.Log($"Demoted {ent.entityName}"); RemoveModerator(ent.steamID); }, () => { });
						UnityEngine.Object.Destroy(menu);
					});
				}

			}
		}

		private static void SendKickBan(bool kick, string pln)
		{
			if (ServerConnectionManager.Instance.IsRunning)
			{
				ClientConnectionManager.Instance.HandleModCommand((byte)(kick ? 0 : 1), pln);
			}
			else
			{
				//Send packet
				var pa = PacketManager.GetOrCreatePacket<PlayerRequestPacket>(ClientConnectionManager.Instance.LocalPlayerID, PacketType.PLAYER_REQUEST);
				pa.dataTypes.Add(Request.MOD_COMMAND);
				pa.playerName = pln;
				pa.commandType = (byte)(kick ? 0 : 1);
			}
		}

		private static GameObject CreateButton(string label, Vector2 localPosition, Transform parent, UnityEngine.Events.UnityAction onClick)
		{
			var buttonGO = new GameObject($"{label}Button");
			buttonGO.transform.SetParent(parent, false);

			var rect = buttonGO.AddComponent<RectTransform>();
			rect.sizeDelta = new Vector2(100f, 25f);
			rect.anchoredPosition = localPosition;
			rect.anchorMin = new Vector2(0, 1);
			rect.anchorMax = new Vector2(0, 1);
			rect.pivot = new Vector2(0, 1);


			var colorB = new ColorBlock
			{
				normalColor = Color.white,
				selectedColor = new Color(0.9608f, 0.9608f, 0.9608f, 0.5f),
				disabledColor = new Color(0.7843f, 0.7843f, 0.7843f, 0.502f),
				pressedColor = new Color(0.8833f, 1f, 0f, 1f),
				highlightedColor = new Color(0.1114f, 1f, 0f, 1f),
				fadeDuration = 0.1f,
				colorMultiplier = 1f
			};

			var button = buttonGO.AddComponent<Button>();
			var buttonImage = buttonGO.AddComponent<Image>();
			buttonImage.material = new Material(Base.materials["UI_OUTLINE CHAT"]);
			buttonImage.sprite = Base.bgSprite;
			button.colors = colorB;
			buttonGO.AddComponent<ButtonColorText>();

			var outline = buttonGO.AddComponent<Outline>();
			outline.effectColor = new Color(0.6f, 0.6f, 0.6f, 0.65f);
			outline.effectDistance = new Vector2(1f, -1f);

			button.image = buttonImage;

			var textGO = new GameObject("Text");
			textGO.transform.SetParent(buttonGO.transform, false);
			var textRect = textGO.AddComponent<RectTransform>();
			textRect.anchorMin = Vector2.zero;
			textRect.anchorMax = Vector2.one;
			textRect.offsetMin = Vector2.zero;
			textRect.offsetMax = Vector2.zero;

			var buttonText = textGO.AddComponent<TextMeshProUGUI>();
			buttonText.text = label;
			buttonText.color = Color.white;
			buttonText.alignment = TextAlignmentOptions.Center;
			buttonText.fontSize = 14;

			button.onClick.AddListener(onClick);

			return buttonGO;
		}

		public static void AddModerator(CSteamID steamid)
		{
			if (!ServerConnectionManager.Instance.IsRunning) return;

			var newList = ServerConfig.ModeratorList.Append(steamid.m_SteamID);
			ServerConfig.ModeratorListRaw.Value = string.Join(",", newList);
			ServerConfig.ModeratorListRaw.ConfigFile.Save();
		}

		public static void RemoveModerator(CSteamID steamid)
		{
			if (!ServerConnectionManager.Instance.IsRunning) return;

			var newList = ServerConfig.ModeratorList.Except(new[] { steamid.m_SteamID });
			ServerConfig.ModeratorListRaw.Value = string.Join(",", newList);
			ServerConfig.ModeratorListRaw.ConfigFile.Save();
		}


		public static void InviteFriends()
		{
			Steam.Lobby.InviteFriends();
		}
	}
}
