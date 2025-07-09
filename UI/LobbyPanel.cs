using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static ErenshorCoop.Steam.Lobby;

namespace ErenshorCoop.UI
{
	public class LobbyPanel : IPanel
	{
		public static float yPos = 0;
		public static GameObject joinLobbyButton;
		public static GameObject createLobbyButton;
		public static GameObject leaveLobbyButton;


		public static GameObject filterPanel;
		public static GameObject sortPanel;


		public enum SortMode
		{
			NONE,
			NAME,
			PASSWORD,
			PLAYER
		}

		private static SortMode sortMode = SortMode.PLAYER;

		private static Steam.SteamFilter filterMode = Steam.SteamFilter.DISTANCE_WORLD;

		private static bool filterOptionRemovePasswords = false;

		public static Transform lobbyContainer;

		private static float width = 0;
		private static float height = 0;

		public GameObject CreatePanel(Transform parent, float _width, float _height, Canvas canvas = null)
		{
			lobbyContainer = parent;
			
			width = _width;
			height = _height;

			var x = 0f;
			var y = 0f;
			//Base.AddLabel(parent, new Vector2(x, y - 5), new Vector2(width, 30), "Lobbies", 24f);
			Base.AddButton(parent, new Vector2(x + 16, y - 13), new Vector2(16, 16), "", OnLobbieRefresh, Base.refreshIcon);
			Base.AddButton(parent, new Vector2(x + 40, y - 13), new Vector2(16, 16), "", OnLobbySort, Base.filterIcon);
			joinLobbyButton = Base.AddButton(parent, new Vector2(x + 100, -height + 35f), new Vector2(100, 30), "Join", OnLobbyJoin).gameObject;
			createLobbyButton = Base.AddButton(parent, new Vector2((width / 2) - 50, -height + 35f), new Vector2(100, 30), "Create", OnLobbieCreate).gameObject;
			leaveLobbyButton = Base.AddButton(parent, new Vector2(width - 200, -height + 35f), new Vector2(100, 30), "Leave", OnLobbieLeave).gameObject;
			//var im = Base.AddImage(parent, new Vector2(5, -35f), new Vector2(width - 10, 2), null);
			//im.color = new Color(0, 0, 0, 0.25f);
			y = y - 20;
			yPos = y;


			filterPanel = new GameObject("SortPanel", typeof(RectTransform));
			filterPanel.transform.SetParent(canvas.transform);
			var panelRt = filterPanel.GetComponent<RectTransform>();
			panelRt.anchorMin = new Vector2(0.5f, 0.5f);
			panelRt.anchorMax = new Vector2(0.5f, 0.5f);
			panelRt.pivot = new Vector2(0f, 0f);
			panelRt.anchoredPosition = new Vector2(-270, 270);
			panelRt.sizeDelta = new Vector2(150, (16 * 4f) + 5);

			var panelImage = filterPanel.AddComponent<Image>();
			panelImage.color = new Color(1, 1, 1, 1f);
			panelImage.material = new Material(Base.materials["UI_OUTLINE CHAT"]);
			panelImage.sprite = Base.sprites["cosmetics_and_essentials_carv 1"];
			panelImage.raycastTarget = true;

			var colorB = new ColorBlock
			{
				normalColor = new Color(0, 0, 0, 0),
				selectedColor = new Color(0.9608f, 0.9608f, 0.9608f, 0.0f),
				disabledColor = new Color(0.7843f, 0.7843f, 0.7843f, 0.502f),
				pressedColor = new Color(0.8833f, 1f, 0f, 0.5f),
				highlightedColor = new Color(0.1114f, 1f, 0f, 0.5f),
				fadeDuration = 0.1f,
				colorMultiplier = 1f
			};

			var outline = filterPanel.AddComponent<Outline>();
			outline.effectColor = new Color(0.6f, 0.6f, 0.6f, 0.65f);
			outline.effectDistance = new Vector2(1f, -1f);
			var lb = Base.AddButton(filterPanel.transform, new Vector2(x + 5, 0), new Vector2(150 - 7, 16), "Filter >", () => { EventSystem.current.SetSelectedGameObject(null); sortPanel.SetActive(!sortPanel.activeSelf); }, null, true);
			lb.colors = colorB;
			lb.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
			lb = Base.AddButton(filterPanel.transform, new Vector2(x + 5, -5 - 16), new Vector2(150 - 7, 16), "Sort by Name", () => { EventSystem.current.SetSelectedGameObject(null); OnSortSelect(SortMode.NAME); }, null, true);
			lb.colors = colorB;
			lb.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
			lb = Base.AddButton(filterPanel.transform, new Vector2(x + 5, -5 - (16 * 2)), new Vector2(150 - 7, 16), "Sort by Players", () => { EventSystem.current.SetSelectedGameObject(null); OnSortSelect(SortMode.PLAYER); }, null, true);
			lb.colors = colorB;
			lb.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
			lb = Base.AddButton(filterPanel.transform, new Vector2(x + 5, -5 - (16 * 3)), new Vector2(150 - 7, 16), "Sort by Password", () => { EventSystem.current.SetSelectedGameObject(null); OnSortSelect(SortMode.PASSWORD); }, null, true);
			lb.colors = colorB;
			lb.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
			filterPanel.SetActive(false);

			if (sortMode == SortMode.NAME)
			{
				var img = Base.AddImage(filterPanel.transform, new Vector2(150 - 20, -7 - 16), new Vector2(12, 12), Base.checkmarkIcon);
				img.color = Color.green;
				img.raycastTarget = false;
				_sortChecks.Add(img.gameObject);
			}
			if (sortMode == SortMode.PLAYER)
			{
				var img = Base.AddImage(filterPanel.transform, new Vector2(150 - 20, -7 - (16 * 2)), new Vector2(12, 12), Base.checkmarkIcon);
				img.color = Color.green;
				img.raycastTarget = false;
				_sortChecks.Add(img.gameObject);
			}
			if (sortMode == SortMode.PASSWORD)
			{
				var img = Base.AddImage(filterPanel.transform, new Vector2(150 - 20, -7 - (16 * 3)), new Vector2(12, 12), Base.checkmarkIcon);
				img.color = Color.green;
				img.raycastTarget = false;
				_sortChecks.Add(img.gameObject);
			}




			sortPanel = new GameObject("FilterPanel", typeof(RectTransform));
			sortPanel.transform.SetParent(canvas.transform);
			panelRt = sortPanel.GetComponent<RectTransform>();
			panelRt.anchorMin = new Vector2(0.5f, 0.5f);
			panelRt.anchorMax = new Vector2(0.5f, 0.5f);
			panelRt.pivot = new Vector2(0f, 0f);
			panelRt.anchoredPosition = new Vector2(-120, 245);
			panelRt.sizeDelta = new Vector2(150, (16 * 6f));

			panelImage = sortPanel.AddComponent<Image>();
			panelImage.color = new Color(1, 1, 1, 1f);
			panelImage.material = new Material(Base.materials["UI_OUTLINE CHAT"]);
			panelImage.sprite = Base.sprites["cosmetics_and_essentials_carv 1"];
			panelImage.raycastTarget = true;

			outline = sortPanel.AddComponent<Outline>();
			outline.effectColor = new Color(0.6f, 0.6f, 0.6f, 0.65f);
			outline.effectDistance = new Vector2(1f, -1f);

			lb = Base.AddButton(sortPanel.transform, new Vector2(x + 5, 0), new Vector2(150 - 7, 16), "Default", () => { EventSystem.current.SetSelectedGameObject(null); OnFilterSelect(Steam.SteamFilter.DISTANCE_DEFAULT); }, null, true);
			lb.colors = colorB;
			lb.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
			lb = Base.AddButton(sortPanel.transform, new Vector2(x + 5, -16), new Vector2(150 - 7, 16), "Close Distance", () => { EventSystem.current.SetSelectedGameObject(null); OnFilterSelect(Steam.SteamFilter.DISTANCE_CLOSE); }, null, true);
			lb.colors = colorB;
			lb.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
			lb = Base.AddButton(sortPanel.transform, new Vector2(x + 5, -(16 * 2)), new Vector2(150 - 7, 16), "Far Distance", () => { EventSystem.current.SetSelectedGameObject(null); OnFilterSelect(Steam.SteamFilter.DISTANCE_FAR); }, null, true);
			lb.colors = colorB;
			lb.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
			lb = Base.AddButton(sortPanel.transform, new Vector2(x + 5, -(16 * 3)), new Vector2(150 - 7, 16), "Worldwide", () => { EventSystem.current.SetSelectedGameObject(null); OnFilterSelect(Steam.SteamFilter.DISTANCE_WORLD); }, null, true);
			lb.colors = colorB;
			lb.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
			lb = Base.AddButton(sortPanel.transform, new Vector2(x + 5, -(16 * 4)), new Vector2(150 - 7, 16), "Friends Only", () => { EventSystem.current.SetSelectedGameObject(null); OnFilterSelect(Steam.SteamFilter.FRIENDS_ONLY); }, null, true);
			lb.colors = colorB;
			lb.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
			lb = Base.AddButton(sortPanel.transform, new Vector2(x + 5, -(16 * 5)), new Vector2(150 - 7, 16), "Hide Protected", () => { EventSystem.current.SetSelectedGameObject(null); filterOptionRemovePasswords = !filterOptionRemovePasswords; OnFilterSelect(filterMode); }, null, true);
			lb.colors = colorB;
			lb.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
			sortPanel.SetActive(false);



			if (filterMode == Steam.SteamFilter.DISTANCE_DEFAULT)
			{
				var img = Base.AddImage(sortPanel.transform, new Vector2(150 - 20, 0), new Vector2(12, 12), Base.checkmarkIcon);
				img.color = Color.green;
				img.raycastTarget = false;
				_filterChecks.Add(img.gameObject);
			}
			if (filterMode == Steam.SteamFilter.DISTANCE_CLOSE)
			{
				var img = Base.AddImage(sortPanel.transform, new Vector2(150 - 20, -(16 * 1)), new Vector2(12, 12), Base.checkmarkIcon);
				img.color = Color.green;
				img.raycastTarget = false;
				_filterChecks.Add(img.gameObject);
			}
			if (filterMode == Steam.SteamFilter.DISTANCE_FAR)
			{
				var img = Base.AddImage(sortPanel.transform, new Vector2(150 - 20, -(16 * 2)), new Vector2(12, 12), Base.checkmarkIcon);
				img.color = Color.green;
				img.raycastTarget = false;
				_filterChecks.Add(img.gameObject);
			}
			if (filterMode == Steam.SteamFilter.DISTANCE_WORLD)
			{
				var img = Base.AddImage(sortPanel.transform, new Vector2(150 - 20, -(16 * 3)), new Vector2(12, 12), Base.checkmarkIcon);
				img.color = Color.green;
				img.raycastTarget = false;
				_filterChecks.Add(img.gameObject);
			}
			if (filterMode == Steam.SteamFilter.FRIENDS_ONLY)
			{
				var img = Base.AddImage(sortPanel.transform, new Vector2(150 - 20, -(16 * 4)), new Vector2(12, 12), Base.checkmarkIcon);
				img.color = Color.green;
				img.raycastTarget = false;
				_filterChecks.Add(img.gameObject);
			}
			if (filterOptionRemovePasswords)
			{
				var img = Base.AddImage(sortPanel.transform, new Vector2(150 - 20, -(16 * 5)), new Vector2(12, 12), Base.checkmarkIcon);
				img.color = Color.green;
				img.raycastTarget = false;
				_filterChecks.Add(img.gameObject);
			}

			joinLobbyButton.SetActive(false);
			//createLobbyButton.SetActive(false);
			leaveLobbyButton.SetActive(false);


			return null;
		}



		private static IEnumerator EndSuppressNextFrame()
		{
			yield return null;
			Connect.toggleGroup.allowSwitchOff = false;
		}

		public static void OnLobbySort()
		{
			filterPanel.SetActive(!filterPanel.activeSelf);
			sortPanel.SetActive(false);
		}

		


		private static List<GameObject> _sortChecks = new();
		public static void OnSortSelect(SortMode sort)
		{
			filterPanel.SetActive(false);
			sortPanel.SetActive(false);
			sortMode = sort;

			foreach (var f in _sortChecks)
				UnityEngine.Object.Destroy(f);

			_sortChecks.Clear();

			if (sortMode == SortMode.NAME)
			{
				var img = Base.AddImage(filterPanel.transform, new Vector2(150 - 20, -7 - 16), new Vector2(12, 12), Base.checkmarkIcon);
				img.color = Color.green;
				img.raycastTarget = false;
				_sortChecks.Add(img.gameObject);
			}

			if (sortMode == SortMode.PLAYER)
			{
				var img = Base.AddImage(filterPanel.transform, new Vector2(150 - 20, -7 - (16 * 2)), new Vector2(12, 12), Base.checkmarkIcon);
				img.color = Color.green;
				img.raycastTarget = false;
				_sortChecks.Add(img.gameObject);
			}

			if (sortMode == SortMode.PASSWORD)
			{
				var img = Base.AddImage(filterPanel.transform, new Vector2(150 - 20, -7 - (16 * 3)), new Vector2(12, 12), Base.checkmarkIcon);
				img.color = Color.green;
				img.raycastTarget = false;
				_sortChecks.Add(img.gameObject);
			}

			OnLobbieRefresh();
		}

		private static List<GameObject> _filterChecks = new();
		public static void OnFilterSelect(Steam.SteamFilter sort)
		{
			filterPanel.SetActive(false);
			sortPanel.SetActive(false);
			filterMode = sort;

			foreach (var f in _filterChecks)
				UnityEngine.Object.Destroy(f);

			_filterChecks.Clear();

			if (filterMode == Steam.SteamFilter.DISTANCE_DEFAULT)
			{
				var img = Base.AddImage(sortPanel.transform, new Vector2(150 - 20, 0), new Vector2(12, 12), Base.checkmarkIcon);
				img.color = Color.green;
				img.raycastTarget = false;
				_filterChecks.Add(img.gameObject);
			}
			if (filterMode == Steam.SteamFilter.DISTANCE_CLOSE)
			{
				var img = Base.AddImage(sortPanel.transform, new Vector2(150 - 20, -(16 * 1)), new Vector2(12, 12), Base.checkmarkIcon);
				img.color = Color.green;
				img.raycastTarget = false;
				_filterChecks.Add(img.gameObject);
			}
			if (filterMode == Steam.SteamFilter.DISTANCE_FAR)
			{
				var img = Base.AddImage(sortPanel.transform, new Vector2(150 - 20, -(16 * 2)), new Vector2(12, 12), Base.checkmarkIcon);
				img.color = Color.green;
				img.raycastTarget = false;
				_filterChecks.Add(img.gameObject);
			}
			if (filterMode == Steam.SteamFilter.DISTANCE_WORLD)
			{
				var img = Base.AddImage(sortPanel.transform, new Vector2(150 - 20, -(16 * 3)), new Vector2(12, 12), Base.checkmarkIcon);
				img.color = Color.green;
				img.raycastTarget = false;
				_filterChecks.Add(img.gameObject);
			}
			if (filterMode == Steam.SteamFilter.FRIENDS_ONLY)
			{
				var img = Base.AddImage(sortPanel.transform, new Vector2(150 - 20, -(16 * 4)), new Vector2(12, 12), Base.checkmarkIcon);
				img.color = Color.green;
				img.raycastTarget = false;
				_filterChecks.Add(img.gameObject);
			}
			if (filterOptionRemovePasswords)
			{
				var img = Base.AddImage(sortPanel.transform, new Vector2(150 - 20, -(16 * 5)), new Vector2(12, 12), Base.checkmarkIcon);
				img.color = Color.green;
				img.raycastTarget = false;
				_filterChecks.Add(img.gameObject);
			}

			OnLobbieRefresh();
		}

		public static CSteamID selectedLobby;
		public static void OnLobbySelect(CSteamID lobbyID)
		{
			Logging.Log($"selected Lobby: {lobbyID}");
			selectedLobby = lobbyID;
		}

		public static void OnLobbyJoin()
		{
			if (selectedLobby != CSteamID.Nil)
			{
				LobbyInfo info = null;
				foreach (var i in _lastLobbies)
				{
					if (i.lobbyID == selectedLobby)
					{ info = i; break; }
				}
				if(info != null)
				{
					if(info.hasPassword)
					{
						Main.EnablePrompt("To join you need to provide the correct password.", () => { Steam.Lobby.JoinLobby(selectedLobby, Main.promptInput.text); }, () => { }, true);
					}
					else
					{
						Steam.Lobby.JoinLobby(selectedLobby);
					}
				}
				
			}
		}

		public static void OnLobbieCreate()
		{
			Connect.spinnyIcon.SetActive(false);
			Connect.createLobbyContainer.SetActive(true);
			lobbyContainer.gameObject.SetActive(false);
			//Steam.Lobby.CreateLobby("I'm in love with pain", "", Steamworks.ELobbyType.k_ELobbyTypePublic, 100);
			//OnLobbieRefresh();
		}

		public static void OnLobbieLeave()
		{

			Steam.Networking.Cleanup();
			//OnLobbieRefresh();
			//Steam.Lobby.LeaveLobby();
		}



		static private List<GameObject> lobbyLabels = new();
		static private List<LobbyInfo> _lastLobbies = new();
		public static void OnLobbieRefresh()
		{
			Connect.toggleGroup.allowSwitchOff = true;

			foreach (var go in lobbyLabels)
				UnityEngine.Object.Destroy(go);

			lobbyLabels.Clear();

			Connect.spinnyIcon.SetActive(true);

			Steam.Lobby.RequestLobbyList(filterMode, (uint lobbyCount, List<LobbyInfo> lobbies) =>
			{


				if (filterOptionRemovePasswords)
					lobbies.RemoveAll(lobby => lobby.hasPassword);

				switch (sortMode)
				{
					case SortMode.NAME:
						lobbies.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
						break;
					case SortMode.PASSWORD:
						lobbies.Sort((a, b) => a.hasPassword.CompareTo(b.hasPassword));
						break;
					case SortMode.PLAYER:
						lobbies.Sort((a, b) => b.currentPlayers.CompareTo(a.currentPlayers));
						break;
					case SortMode.NONE:
					default: break;
				}

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

				_lastLobbies = lobbies;

				float y = yPos;
				foreach (var lob in lobbies)
				{
					var g = new GameObject("lobLabel");
					g.transform.SetParent(lobbyContainer, false);
					RectTransform rt = g.AddComponent<RectTransform>();
					rt.sizeDelta = new Vector2(width, 30);
					rt.anchorMin = new Vector2(0, 1);
					rt.anchorMax = new Vector2(0, 1);
					rt.pivot = new Vector2(0, 1);
					rt.anchoredPosition = new Vector2(0, y - 20);



					Image im = Base.AddImage(g.transform, new Vector2(5, 0), new Vector2(width - 10, 30), null);
					im.color = new Color(1, 1, 1, 0.25f);

					var lobbyID = lob.lobbyID;

					Toggle but = g.AddComponent<Toggle>();
					but.image = im;
					//but.onValueChanged.AddListener((selected) => { OnLobbySelect(selected, lobbyID); });
					var tog = g.AddComponent<LobbyClickHandler>();
					tog.lobbyID = lobbyID;
					but.group = Connect.toggleGroup;
					but.isOn = false;

					but.colors = colorB;

					if (lob.hasPassword)
					{
						var img = Base.AddImage(g.transform, new Vector2(16, -7), new Vector2(16, 16), Base.lockClosedIcon);
						img.color = Color.red;
						img.raycastTarget = false;
					}
					else
					{
						var img = Base.AddImage(g.transform, new Vector2(16, -7), new Vector2(16, 16), Base.lockOpenIcon);
						img.color = Color.green;
						img.raycastTarget = false;
					}
					var f = Base.AddLabel(g.transform, new Vector2(50, 0), new Vector2(width - 60, 30), $"{lob.name}", 20f);
					f.raycastTarget = false;
					f.alignment = TextAlignmentOptions.Left;

					var z = Base.AddLabel(g.transform, new Vector2(50, 0), new Vector2(width - 60, 30), $"{lob.currentPlayers}/{lob.maxPlayers}", 20f);
					z.raycastTarget = false;
					z.alignment = TextAlignmentOptions.Right;
					lobbyLabels.Add(g);
					y -= 35;
				}
				if(Connect.spinnyIcon != null)
					Connect.spinnyIcon?.SetActive(false);
				Main._instance.StartCoroutine(EndSuppressNextFrame());
				selectedLobby = CSteamID.Nil;
			});
		}


	}


	public class LobbyClickHandler : MonoBehaviour, IPointerClickHandler
	{
		public CSteamID lobbyID;

		public void OnPointerClick(PointerEventData eventData)
		{
			LobbyPanel.OnLobbySelect(lobbyID);
			if (eventData.clickCount >= 2)
				LobbyPanel.OnLobbyJoin();	
		}
	}
}
