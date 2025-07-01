using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ErenshorCoop.Client;
using ErenshorCoop.Server;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static ErenshorCoop.Steam.Lobby;

namespace ErenshorCoop.UI
{
	public class Connect
	{

		private static InputField _addressInput;
		private static InputField _portInput;
		public static TextMeshProUGUI _feedbackText;

		private static Button connectButton;
		private static Button hostButton;
		private static Button disconnectButton;

		private static GameObject mainPanel;
		private static Transform lobbyContainer;
		private static GameObject directConnectPanel;
		private static GameObject createLobbyPanel;
		private static GameObject joinLobbyPanel;
		private static GameObject inviteFriendsPanel;
		private static GameObject playerListPanel;


		private static GameObject joinLobbyButton;
		private static GameObject createLobbyButton;
		private static GameObject leaveLobbyButton;

		private static GameObject filterPanel;
		private static GameObject sortPanel;

		public static GameObject spinnyIcon;

		private static ToggleGroup toggleGroup;

		private static float yPos = 0;

		private const float width = 600f;

		public static TabManager tabManager;

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

		public static void Cleanup()
		{
			if (tabManager != null)
				tabManager.Cleanup();

			tabManager = null;
		}

		public static void Update()
		{
			joinLobbyButton?.SetActive(selectedLobby != CSteamID.Nil);
			leaveLobbyButton?.SetActive(Steam.Lobby.isInLobby);
			createLobbyButton?.SetActive(!Steam.Lobby.isInLobby);
			if(tabManager != null)
			{
				if (Steam.Lobby.isInLobby)
					tabManager.ShowTab("Players");
				else
					tabManager.HideTab("Players");
			}

			connectButton?.gameObject.SetActive(!Steam.Lobby.isInLobby && !ClientConnectionManager.Instance.IsRunning);
			hostButton?.gameObject.SetActive(!Steam.Lobby.isInLobby && !ClientConnectionManager.Instance.IsRunning);
			disconnectButton?.gameObject.SetActive(!Steam.Lobby.isInLobby && ClientConnectionManager.Instance.IsRunning);

			RefreshPlayerInfo();
		}

		public static GameObject CreateConnectUi(Canvas canvas)
		{

			tabManager = new();

			mainPanel = new GameObject("LobbyPanel", typeof(RectTransform));
			mainPanel.transform.SetParent(canvas.transform);
			var panelRt = mainPanel.GetComponent<RectTransform>();
			panelRt.anchorMin = new Vector2(0.5f,     0.5f);
			panelRt.anchorMax = new Vector2(0.5f,     0.5f);
			panelRt.pivot = new Vector2(0.5f,         0.5f);
			panelRt.anchoredPosition = new Vector2(0, -3.7f);
			panelRt.sizeDelta = new Vector2(width,    750f);

			var panelImage = mainPanel.AddComponent<Image>();
			panelImage.color = new Color(1, 1, 1, 1f);
			panelImage.material = new Material(Base.materials["UI_OUTLINE CHAT"]);
			panelImage.sprite = Base.bgSprite;
			panelImage.raycastTarget = true;
			panelImage.type = Image.Type.Sliced;
			panelImage.fillCenter = true;

			var outline = mainPanel.AddComponent<Outline>();
			outline.effectColor = new Color(0.6f, 0.6f, 0.6f, 0.65f);
			outline.effectDistance = new Vector2(1f, -1f);


			var vL = Base.AddLabel(mainPanel.transform, new Vector2((width/2)-25, -(750f-25f)), new Vector2(width, 30), "v." + ErenshorCoopMod.version.ToString(), 12f);


			toggleGroup = mainPanel.AddComponent<ToggleGroup>();
			//toggleGroup.allowSwitchOff = true;

			var cont = new GameObject("LobbyContainer", typeof(RectTransform));
			lobbyContainer = cont.transform;
			lobbyContainer.SetParent(mainPanel.transform, false);
			panelRt = lobbyContainer.GetComponent<RectTransform>();
			panelRt.anchorMin = new Vector2(0.5f, 0.5f);
			panelRt.anchorMax = new Vector2(0.5f, 0.5f);
			panelRt.pivot = new Vector2(0.5f, 0.5f);
			panelRt.anchoredPosition = new Vector2(0, 0f);
			panelRt.sizeDelta = new Vector2(width, 750);


			spinnyIcon = new GameObject("spinner", typeof(RectTransform));
			spinnyIcon.transform.SetParent(mainPanel.transform, false);
			panelRt = spinnyIcon.GetComponent<RectTransform>();
			panelRt.anchorMin = new Vector2(0.5f, 0.5f);
			panelRt.anchorMax = new Vector2(0.5f, 0.5f);
			panelRt.pivot = new Vector2(0.5f, 0.5f);
			panelRt.anchoredPosition = new Vector2(0, 0f);
			panelRt.sizeDelta = new Vector2(64, 64);
			Base.AddImage(spinnyIcon.transform, Vector2.zero, panelRt.sizeDelta, Base.refreshIcon);
			spinnyIcon.SetActive(false);

			var x = 0f;
			var y = 0f;
			Base.AddLabel(lobbyContainer, new Vector2(x, y - 5), new Vector2(width, 30), "Lobbies", 24f);
			Base.AddButton(lobbyContainer, new Vector2(x + 16, y - 13), new Vector2(16, 16), "", OnLobbieRefresh, Base.refreshIcon);
			Base.AddButton(lobbyContainer, new Vector2(x + 40, y - 13), new Vector2(16, 16), "", OnLobbySort, Base.filterIcon);
			joinLobbyButton = Base.AddButton(lobbyContainer, new Vector2(x+100, -750f+35f), new Vector2(100, 30), "Join", OnLobbyJoin).gameObject;
			createLobbyButton = Base.AddButton(lobbyContainer, new Vector2((width/2) - 50, -750f + 35f), new Vector2(100, 30), "Create", OnLobbieCreate).gameObject;
			leaveLobbyButton = Base.AddButton(lobbyContainer, new Vector2(width -200, -750f + 35f), new Vector2(100, 30), "Leave", OnLobbieLeave).gameObject;
			var im = Base.AddImage(lobbyContainer, new Vector2(5, -35f), new Vector2(width - 10, 2), null);
			im.color = new Color(0, 0, 0, 0.25f);
			y = y - 20;
			yPos = y;

			tabManager.AddTab("Lobbies", cont, () => { OnLobbieRefresh(); });


			joinLobbyButton.SetActive(false);
			//createLobbyButton.SetActive(false);
			leaveLobbyButton.SetActive(false);


			var directContainer = new GameObject("DirectContainer", typeof(RectTransform));
			//lobbyContainer = cont.transform;
			directContainer.transform.SetParent(mainPanel.transform, false);
			panelRt = directContainer.GetComponent<RectTransform>();
			panelRt.anchorMin = new Vector2(0.5f, 0.5f);
			panelRt.anchorMax = new Vector2(0.5f, 0.5f);
			panelRt.pivot = new Vector2(0.5f, 0.5f);
			panelRt.anchoredPosition = new Vector2(0, 0f);
			panelRt.sizeDelta = new Vector2(width, 750);

			tabManager.AddTab("Direct", directContainer, () => { spinnyIcon.SetActive(false); HidePanels(); });


			var settingsContainer = new GameObject("SettingsContainer", typeof(RectTransform));
			//lobbyContainer = cont.transform;
			settingsContainer.transform.SetParent(mainPanel.transform, false);
			panelRt = settingsContainer.GetComponent<RectTransform>();
			panelRt.anchorMin = new Vector2(0.5f, 0.5f);
			panelRt.anchorMax = new Vector2(0.5f, 0.5f);
			panelRt.pivot = new Vector2(0.5f, 0.5f);
			panelRt.anchoredPosition = new Vector2(0, 0f);
			panelRt.sizeDelta = new Vector2(width, 750);

			tabManager.AddTab("Settings", settingsContainer, () => { spinnyIcon.SetActive(false); HidePanels(); });


			im = Base.AddImage(settingsContainer.transform, new Vector2(5, -35f), new Vector2(width - 10, 2), null);
			im.color = new Color(0, 0, 0, 0.25f);


			x = 0f;
			y = 0f;
			Base.AddLabel(settingsContainer.transform, new Vector2(x, y - 5), new Vector2(width, 30), "Settings", 24f);





			playerListPanel = new GameObject("playersContainer", typeof(RectTransform));
			//lobbyContainer = cont.transform;
			playerListPanel.transform.SetParent(mainPanel.transform, false);
			panelRt = playerListPanel.GetComponent<RectTransform>();
			panelRt.anchorMin = new Vector2(0.5f, 0.5f);
			panelRt.anchorMax = new Vector2(0.5f, 0.5f);
			panelRt.pivot = new Vector2(0.5f, 0.5f);
			panelRt.anchoredPosition = new Vector2(0, 0f);
			panelRt.sizeDelta = new Vector2(width, 750);

			tabManager.AddTab("Players", playerListPanel, () => { spinnyIcon.SetActive(false); HidePanels(); });


			im = Base.AddImage(playerListPanel.transform, new Vector2(5, -35f), new Vector2(width - 10, 2), null);
			im.color = new Color(0, 0, 0, 0.25f);


			x = 0f;
			y = 0f;
			Base.AddLabel(playerListPanel.transform, new Vector2(x, y - 5), new Vector2(width, 30), "Players", 24f);


			filterPanel = new GameObject("SortPanel", typeof(RectTransform));
			filterPanel.transform.SetParent(canvas.transform);
			panelRt = filterPanel.GetComponent<RectTransform>();
			panelRt.anchorMin = new Vector2(0.5f, 0.5f);
			panelRt.anchorMax = new Vector2(0.5f, 0.5f);
			panelRt.pivot = new Vector2(0f, 0f);
			panelRt.anchoredPosition = new Vector2(-270, 270);
			panelRt.sizeDelta = new Vector2(150, (16*4f)+5);

			panelImage = filterPanel.AddComponent<Image>();
			panelImage.color = new Color(1, 1, 1, 1f);
			panelImage.material = new Material(Base.materials["UI_OUTLINE CHAT"]);
			panelImage.sprite = Base.sprites["cosmetics_and_essentials_carv 1"];
			panelImage.raycastTarget = true;

			var colorB = new ColorBlock
			{
				normalColor = new Color(0,0,0,0),
				selectedColor = new Color(0.9608f, 0.9608f, 0.9608f, 0.0f),
				disabledColor = new Color(0.7843f, 0.7843f, 0.7843f, 0.502f),
				pressedColor = new Color(0.8833f, 1f, 0f, 0.5f),
				highlightedColor = new Color(0.1114f, 1f, 0f, 0.5f),
				fadeDuration = 0.1f,
				colorMultiplier = 1f
			};

			outline = filterPanel.AddComponent<Outline>();
			outline.effectColor = new Color(0.6f, 0.6f, 0.6f, 0.65f);
			outline.effectDistance = new Vector2(1f, -1f);
			var lb = Base.AddButton(filterPanel.transform, new Vector2(x+5, 0), new Vector2(150-7, 16), "Filter >", () =>{ EventSystem.current.SetSelectedGameObject(null); sortPanel.SetActive(!sortPanel.activeSelf); }, null, true);
			lb.colors = colorB;
			lb.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
			lb = Base.AddButton(filterPanel.transform, new Vector2(x + 5, -5- 16), new Vector2(150-7, 16), "Sort by Name", () => { EventSystem.current.SetSelectedGameObject(null); OnSortSelect(SortMode.NAME); }, null, true);
			lb.colors = colorB;
			lb.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
			lb = Base.AddButton(filterPanel.transform, new Vector2(x + 5, - 5 -(16*2)), new Vector2(150-7, 16), "Sort by Players", () => { EventSystem.current.SetSelectedGameObject(null); OnSortSelect(SortMode.PLAYER); }, null, true);
			lb.colors = colorB;
			lb.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
			lb = Base.AddButton(filterPanel.transform, new Vector2(x + 5, - 5- (16 * 3)), new Vector2(150-7, 16), "Sort by Password", () => { EventSystem.current.SetSelectedGameObject(null); OnSortSelect(SortMode.PASSWORD); }, null, true);
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
			lb = Base.AddButton(sortPanel.transform, new Vector2(x + 5, - 16), new Vector2(150 - 7, 16), "Close Distance", () => { EventSystem.current.SetSelectedGameObject(null); OnFilterSelect(Steam.SteamFilter.DISTANCE_CLOSE); }, null, true);
			lb.colors = colorB;
			lb.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
			lb = Base.AddButton(sortPanel.transform, new Vector2(x + 5, - (16 * 2)), new Vector2(150 - 7, 16), "Far Distance", () => { EventSystem.current.SetSelectedGameObject(null); OnFilterSelect(Steam.SteamFilter.DISTANCE_FAR); }, null, true);
			lb.colors = colorB;
			lb.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
			lb = Base.AddButton(sortPanel.transform, new Vector2(x + 5, - (16 * 3)), new Vector2(150 - 7, 16), "Worldwide", () => { EventSystem.current.SetSelectedGameObject(null); OnFilterSelect(Steam.SteamFilter.DISTANCE_WORLD); }, null, true);
			lb.colors = colorB;
			lb.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
			lb = Base.AddButton(sortPanel.transform, new Vector2(x + 5, - (16 * 4)), new Vector2(150 - 7, 16), "Friends Only", () => { EventSystem.current.SetSelectedGameObject(null); OnFilterSelect(Steam.SteamFilter.FRIENDS_ONLY); }, null, true);
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
				var img = Base.AddImage(sortPanel.transform, new Vector2(150 - 20,  - (16 * 1)), new Vector2(12, 12), Base.checkmarkIcon);
				img.color = Color.green;
				img.raycastTarget = false;
				_filterChecks.Add(img.gameObject);
			}
			if (filterMode == Steam.SteamFilter.DISTANCE_FAR)
			{
				var img = Base.AddImage(sortPanel.transform, new Vector2(150 - 20,  - (16 * 2)), new Vector2(12, 12), Base.checkmarkIcon);
				img.color = Color.green;
				img.raycastTarget = false;
				_filterChecks.Add(img.gameObject);
			}
			if (filterMode == Steam.SteamFilter.DISTANCE_WORLD)
			{
				var img = Base.AddImage(sortPanel.transform, new Vector2(150 - 20,  - (16 * 3)), new Vector2(12, 12), Base.checkmarkIcon);
				img.color = Color.green;
				img.raycastTarget = false;
				_filterChecks.Add(img.gameObject);
			}
			if (filterMode == Steam.SteamFilter.FRIENDS_ONLY)
			{
				var img = Base.AddImage(sortPanel.transform, new Vector2(150 - 20,  - (16 * 4)), new Vector2(12, 12), Base.checkmarkIcon);
				img.color = Color.green;
				img.raycastTarget = false;
				_filterChecks.Add(img.gameObject);
			}
			if (filterOptionRemovePasswords)
			{
				var img = Base.AddImage(sortPanel.transform, new Vector2(150 - 20,  - (16 * 5)), new Vector2(12, 12), Base.checkmarkIcon);
				img.color = Color.green;
				img.raycastTarget = false;
				_filterChecks.Add(img.gameObject);
			}


			directConnectPanel = new GameObject("ConnectPanel", typeof(RectTransform));
			directConnectPanel.transform.SetParent(directContainer.transform);
			panelRt = directConnectPanel.GetComponent<RectTransform>();
			panelRt.anchorMin = new Vector2(0.5f, 0.5f);
			panelRt.anchorMax = new Vector2(0.5f, 0.5f);
			panelRt.pivot = new Vector2(0.5f, 0.5f);
			panelRt.anchoredPosition = new Vector2(0, -0.0f);
			panelRt.sizeDelta = new Vector2(width, 750f);

			im = Base.AddImage(directConnectPanel.transform, new Vector2(5, -35f), new Vector2(width - 10, 2), null);
			im.color = new Color(0, 0, 0, 0.25f);


			x = 0f;
			y = 0f;
			Base.AddLabel(directConnectPanel.transform, new Vector2(x, y - 5), new Vector2(width, 30), "Direct Connection", 24f);
			

			x = 10;
			y = -40;
			var l = Base.AddLabel(directConnectPanel.transform, new Vector2(x+5, y+6), new Vector2(200, 30), "IP");
			l.alignment = TextAlignmentOptions.Left;
			y -= 18f;
			_addressInput = Base.AddInputField(directConnectPanel.transform, new Vector2(x+5, y),new Vector2(200-10,16), "IP Address...");
			_addressInput.text = ClientConfig.SavedIP.Value;
			_addressInput.characterValidation = InputField.CharacterValidation.None;
			_addressInput.onValidateInput += (_, _, addedChar) => {
				var black = new List<char> {' ', '\n', '\t', '\v', '\f', '\b', '\r'};
				return black.Contains(addedChar) ? '\0' : addedChar;
			};
			y -= 22f;

			l= Base.AddLabel(directConnectPanel.transform, new Vector2(x+5, y), new Vector2(200, 30), "Port");
			l.alignment = TextAlignmentOptions.Left;
			y -= 24f;
			_portInput = Base.AddInputField(directConnectPanel.transform, new Vector2(x+5, y), new Vector2(200-10, 16), "Port...");
			_portInput.text = ClientConfig.SavedPort.Value;
			_portInput.characterValidation = InputField.CharacterValidation.None;
			_portInput.onValidateInput += (_, _, addedChar) => {
				var all = new List<char> {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9'};
				return !all.Contains(addedChar) ? '\0' : addedChar;
			};

			y -= 10;

			_feedbackText = Base.AddLabel(directConnectPanel.transform, new Vector2(x, y), new Vector2(width, 30), "");
			_feedbackText.alignment = TextAlignmentOptions.Center;

			y -= 25f;

			connectButton = Base.AddButton(directConnectPanel.transform, new Vector2(x+15,   y), new Vector2(200-30,   30), "Connect", OnConnectButtonPressed);
			disconnectButton = Base.AddButton(directConnectPanel.transform, new Vector2(x + 15, y), new Vector2(200 - 30, 30), "Disconnect", OnDisconnectButtonPressed);
			disconnectButton.gameObject.SetActive(false);
			y -= 30f + 8f;

			hostButton = Base.AddButton(directConnectPanel.transform, new Vector2(x+15, y), new Vector2(200-30, 30), "Start Hosting", OnStartButtonPressed);
			y -= 30f + 8f;

			//AddButton(panel.transform, new Vector2(x, y), "Settings", () => Debug.Log("Open Settings"));
			//y -= 30f + 8f;


			//_feedbackText.gameObject.SetActive(false);

			//directConnectPanel.SetActive(false);

			tabManager.CreateTabbedUI(canvas.transform);

			return mainPanel;
		}

		private static void OnConnectButtonPressed()
		{
			ClientConfig.SavedIP.Value = _addressInput.text;
			ClientConfig.SavedPort.Value = _portInput.text;
			ClientConfig.Save();
			
			_feedbackText.text = "Connecting...";

			var ipString = _addressInput.text;
			if (string.IsNullOrEmpty(ipString))
			{
				_feedbackText.text = "No IP!";
				return;
			}

			bool isValidIP = false;
			if (!IPAddress.TryParse(ipString, out var parsedIpAddress))
			{
				try
				{
					// Dns.GetHostAddresses will throw an exception if the hostname is invalid or cannot be resolved
					IPAddress[] addresses = Dns.GetHostAddresses(ipString);
					// If we reached here, it was a valid hostname that resolved to at least one address
					if (addresses.Length >= 1)
					{
						isValidIP = true;
					}
				}catch{}
			}
			else
			{
				isValidIP = true;
			}

			if (!isValidIP)
			{
				_feedbackText.text = "Invalid IP!";
				return;
			}

			string portString = _portInput.text;
			if (!int.TryParse(portString, out int portNumber))
			{
				_feedbackText.text = "Invalid Port!";
				return;
			}

			if (portNumber < 1 || portNumber > 65535)
			{
				_feedbackText.text = "Port must be between 1 and 65535.";
				return;
			}

			//FIXME: Later use
			//Steam.Networking.ConnectToPeer(Steamworks.CSteamID.Nil, portNumber, ipString, true);
			ClientConnectionManager.Instance.Connect(ipString, portNumber);
			hostButton.gameObject.SetActive(false);
			connectButton.gameObject.SetActive(false);
			disconnectButton.gameObject.SetActive(true);
		}

		public static void EnableButtons()
		{
			hostButton?.gameObject.SetActive(true);
			hostButton?.gameObject.SetActive(true);
			hostButton?.gameObject.SetActive(false);
		}

		private static void OnStartButtonPressed()
		{
			ClientConfig.SavedIP.Value = _addressInput.text;
			ClientConfig.SavedPort.Value = _portInput.text;
			ClientConfig.Save();

			_feedbackText.text = "Connecting...";

			string portString = _portInput.text;
			if (!int.TryParse(portString, out int portNumber))
			{
				_feedbackText.text = "Invalid Port!";
				return;
			}

			if (portNumber < 1 || portNumber > 65535)
			{
				_feedbackText.text = "Port must be between 1 and 65535.";
				return;
			}

			if (ServerConnectionManager.Instance.StartHost(portNumber))
			{
				ClientConnectionManager.Instance.Connect("localhost", portNumber);
				hostButton.gameObject.SetActive(false);
				connectButton.gameObject.SetActive(false);
				disconnectButton.gameObject.SetActive(true);
			}
		}

		private static void OnDisconnectButtonPressed()
		{
			_feedbackText.text = "";
			ClientConfig.SavedIP.Value = _addressInput.text;
			ClientConfig.SavedPort.Value = _portInput.text;
			ClientConfig.Save();

			ClientConnectionManager.Instance?.Disconnect();
			ServerConnectionManager.Instance?.Disconnect();
			EnableButtons();
		}




		static private List<GameObject> lobbyLabels = new();
		public static void OnLobbieRefresh()
		{
			toggleGroup.allowSwitchOff = true;

			foreach (var go in lobbyLabels)
				UnityEngine.Object.Destroy(go);

			lobbyLabels.Clear();

			spinnyIcon.SetActive(true);

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
					rt.anchoredPosition = new Vector2(0,y-20);

					

					Image im = Base.AddImage(g.transform, new Vector2(5, 0), new Vector2(width-10, 30), null);
					im.color = new Color(1, 1, 1, 0.25f);

					var lobbyID = lob.lobbyID;

					Toggle but = g.AddComponent<Toggle>();
					but.image = im;
					but.onValueChanged.AddListener((selected) => { OnLobbySelect(selected, lobbyID); });
					but.group = toggleGroup;
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
				spinnyIcon.SetActive(false);
				Main._instance.StartCoroutine(EndSuppressNextFrame());
				selectedLobby = CSteamID.Nil;
			});
		}


		public struct PlayerInfoObject
		{
			public GameObject go;
			public TMP_Text text;
			public Image pingIcon;
		}

		static private Dictionary<string, PlayerInfoObject> playerLabels = new();

		public static void ClearPlayerInfo()
		{
			Steam.Networking.lastPlayerData.Clear();
			foreach(var kp in playerLabels)
			{
				UnityEngine.Object.Destroy(kp.Value.go);
			}
			playerLabels.Clear();
		}
		public static void RefreshPlayerInfo()
		{
			if (playerListPanel == null) return;
			if (!playerListPanel.activeSelf) return;
			if (!Steam.Lobby.isInLobby) return;

			var collected = false;
			if (Steam.Lobby.isInLobby && Steam.Lobby.isLobbyHost)
			{
				collected = Steam.Networking.CollectPlayerData();
			}

			if (collected)
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

				foreach (var pl in newPlayers)
				{
					if (!playerLabels.ContainsKey(pl.name))
					{
						float y = yPos;
						//var pl = Steam.Networking.lastPlayerData.Last();
						//add new one
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
						//but.onValueChanged.AddListener((selected) => { OnLobbySelect(selected, lobbyID); });
						//but.group = toggleGroup;
						but.isOn = false;

						but.colors = colorB;

						var img = Base.AddImage(g.transform, new Vector2(width-25, -7), new Vector2(16, 16), Base.pingIcon);
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

						f = Base.AddLabel(g.transform, new Vector2(10+200, 0), new Vector2(width - 60, 30), $"{pl.name}", 20f);
						f.raycastTarget = false;
						f.alignment = TextAlignmentOptions.Left;

						var z = Base.AddLabel(g.transform, new Vector2(30, 0), new Vector2(width - 60, 30), $"{pl.ping}", 20f);
						z.raycastTarget = false;
						z.alignment = TextAlignmentOptions.Right;

						playerLabels.Add(pl.name, new() { go = g, text = z, pingIcon = img });
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


		private static IEnumerator EndSuppressNextFrame()
		{
			yield return null;
			toggleGroup.allowSwitchOff = false;
		}

		public static void OnLobbySort()
		{
			filterPanel.SetActive(!filterPanel.activeSelf);
			sortPanel.SetActive(false);
		}

		public static void HidePanels()
		{
			filterPanel.SetActive(false);
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
				var img = Base.AddImage(filterPanel.transform, new Vector2(150 - 20, - 7 - 16), new Vector2(12, 12), Base.checkmarkIcon);
				img.color = Color.green;
				img.raycastTarget = false;
				_sortChecks.Add(img.gameObject);
			}

			if (sortMode == SortMode.PLAYER)
			{
				var img = Base.AddImage(filterPanel.transform, new Vector2(150 - 20,- 7 - (16 * 2)), new Vector2(12, 12), Base.checkmarkIcon);
				img.color = Color.green;
				img.raycastTarget = false;
				_sortChecks.Add(img.gameObject);
			}

			if (sortMode == SortMode.PASSWORD)
			{
				var img = Base.AddImage(filterPanel.transform, new Vector2(150 -20, - 7 - (16 * 3)), new Vector2(12, 12), Base.checkmarkIcon);
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
				var img = Base.AddImage(sortPanel.transform, new Vector2(150 - 20, - (16 * 1)), new Vector2(12, 12), Base.checkmarkIcon);
				img.color = Color.green;
				img.raycastTarget = false;
				_filterChecks.Add(img.gameObject);
			}
			if (filterMode == Steam.SteamFilter.DISTANCE_FAR)
			{
				var img = Base.AddImage(sortPanel.transform, new Vector2(150 - 20, - (16 * 2)), new Vector2(12, 12), Base.checkmarkIcon);
				img.color = Color.green;
				img.raycastTarget = false;
				_filterChecks.Add(img.gameObject);
			}
			if (filterMode == Steam.SteamFilter.DISTANCE_WORLD)
			{
				var img = Base.AddImage(sortPanel.transform, new Vector2(150 - 20, - (16 * 3)), new Vector2(12, 12), Base.checkmarkIcon);
				img.color = Color.green;
				img.raycastTarget = false;
				_filterChecks.Add(img.gameObject);
			}
			if (filterMode == Steam.SteamFilter.FRIENDS_ONLY)
			{
				var img = Base.AddImage(sortPanel.transform, new Vector2(150 - 20, - (16 * 4)), new Vector2(12, 12), Base.checkmarkIcon);
				img.color = Color.green;
				img.raycastTarget = false;
				_filterChecks.Add(img.gameObject);
			}
			if (filterOptionRemovePasswords)
			{
				var img = Base.AddImage(sortPanel.transform, new Vector2(150 - 20, - (16 * 5)), new Vector2(12, 12), Base.checkmarkIcon);
				img.color = Color.green;
				img.raycastTarget = false;
				_filterChecks.Add(img.gameObject);
			}

			OnLobbieRefresh();
		}

		public static CSteamID selectedLobby;
		public static void OnLobbySelect(bool sel, CSteamID lobbyID)
		{
			if (!sel)
				selectedLobby = CSteamID.Nil;
			else
			{
				Logging.Log($"selected Lobby: {lobbyID}");
				selectedLobby = lobbyID;
			}
		}

		public static void OnLobbyJoin()
		{
			if(selectedLobby != CSteamID.Nil)
				Steam.Lobby.JoinLobby(selectedLobby);
		}

		public static void OnLobbieCreate()
		{
			Steam.Lobby.CreateLobby("I'm in love with pain", "aaa", Steamworks.ELobbyType.k_ELobbyTypePublic, 100);
			//OnLobbieRefresh();
		}

		public static void OnLobbieLeave()
		{
			
			Steam.Networking.Cleanup();
			//OnLobbieRefresh();
			//Steam.Lobby.LeaveLobby();
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

	}

}
