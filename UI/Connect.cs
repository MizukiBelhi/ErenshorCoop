using System;
using System.Collections.Generic;
using System.Linq;
using ErenshorCoop.Client;
using ErenshorCoop.Server;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace ErenshorCoop.UI
{
	public class Connect
	{
		private static GameObject mainPanel;

		private static IPanel directConnectPanel;
		private static IPanel lobbyPanel;
		private static IPanel playerPanel;
		private static IPanel settingsPanel;
		private static IPanel createLobbyPanel;
		private static GameObject playerListPanel;
		public static GameObject createLobbyContainer;

		public static GameObject spinnyIcon;
		public static ToggleGroup toggleGroup;

		private const float width = 600f;
		private const float height = 750f;

		public static TabManager tabManager;

		public static void Cleanup()
		{
			if (tabManager != null)
				tabManager.Cleanup();

			tabManager = null;
		}

		public static void Update()
		{
			LobbyPanel.joinLobbyButton?.SetActive(LobbyPanel.selectedLobby != CSteamID.Nil && !Steam.Lobby.isInLobby && !ClientConnectionManager.Instance.IsRunning);
			LobbyPanel.leaveLobbyButton?.SetActive(Steam.Lobby.isInLobby);
			LobbyPanel.createLobbyButton?.SetActive(!Steam.Lobby.isInLobby);
			if(tabManager != null)
			{
				if (Steam.Lobby.isInLobby)
					tabManager.ShowTab("Players");
				else
				{
					tabManager.HideTab("Players");
					if (tabManager.currentTab == "Players")
						tabManager.SetActiveTab(0);
				}
			}


			ConnectPanel.connectButton?.gameObject.SetActive(!Steam.Lobby.isInLobby && !ClientConnectionManager.Instance.IsRunning);
			ConnectPanel.hostButton?.gameObject.SetActive(!Steam.Lobby.isInLobby && !ClientConnectionManager.Instance.IsRunning);
			ConnectPanel.disconnectButton?.gameObject.SetActive(!Steam.Lobby.isInLobby && ClientConnectionManager.Instance.IsRunning);

			PlayerPanel.inviteFriendsButton?.gameObject.SetActive(Steam.Lobby.isInLobby && Steam.Lobby.isLobbyHost);
			

			PlayerPanel.RefreshPlayerInfo();
		}

		public static GameObject CreateConnectUi(Canvas canvas)
		{

			tabManager = new();

			mainPanel = new GameObject("LobbyPanel", typeof(RectTransform), typeof(DetectMainPanel));
			mainPanel.transform.SetParent(canvas.transform);
			var panelRt = mainPanel.GetComponent<RectTransform>();
			panelRt.anchorMin = new Vector2(0.5f,     0.5f);
			panelRt.anchorMax = new Vector2(0.5f,     0.5f);
			panelRt.pivot = new Vector2(0.5f,         0.5f);
			panelRt.anchoredPosition = new Vector2(0, -3.7f);
			panelRt.sizeDelta = new Vector2(width,    height);

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


			var vL = Base.AddLabel(mainPanel.transform, new Vector2((width/2)-25, -(height-25f)), new Vector2(width, 30), "v." + ErenshorCoopMod.version.ToString(), 12f);


			toggleGroup = mainPanel.AddComponent<ToggleGroup>();
			//toggleGroup.allowSwitchOff = true;

			var cont = CreateTab("Lobbies","Lobbies", () => { LobbyPanel.OnLobbieRefresh(); });

			lobbyPanel = new LobbyPanel();
			lobbyPanel.CreatePanel(cont.transform, width, height, canvas);


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


			var directContainer = CreateTab("Direct","Direct Connection");

			directConnectPanel = new ConnectPanel();
			directConnectPanel.CreatePanel(directContainer.transform, width, height);


			var settingsContainer = CreateTab("Settings");
			settingsPanel = new SettingsPanel();
			settingsPanel.CreatePanel(settingsContainer.transform, width, height);



			playerListPanel = CreateTab("Players");


			playerPanel = new PlayerPanel();
			playerPanel.CreatePanel(playerListPanel.transform, width, height, canvas);



			createLobbyContainer = CreateContainer("Create Lobby");
			createLobbyPanel = new LobbyCreatePanel();
			createLobbyPanel.CreatePanel(createLobbyContainer.transform, width, height, canvas);
			createLobbyContainer.SetActive(false);

			tabManager.CreateTabbedUI(canvas.transform);

			return mainPanel;
		}



		public static void HidePanels()
		{
			LobbyPanel.filterPanel?.SetActive(false);
			LobbyPanel.sortPanel?.SetActive(false);
		}



		private static GameObject CreateTab(string name,string longName="", Action cb=null)
		{
			var obj = new GameObject(name+"Container", typeof(RectTransform));
			//lobbyContainer = cont.transform;
			obj.transform.SetParent(mainPanel.transform, false);
			var panelRt = obj.GetComponent<RectTransform>();
			panelRt.anchorMin = new Vector2(0.5f, 0.5f);
			panelRt.anchorMax = new Vector2(0.5f, 0.5f);
			panelRt.pivot = new Vector2(0.5f, 0.5f);
			panelRt.anchoredPosition = new Vector2(0, 0f);
			panelRt.sizeDelta = new Vector2(width, height);

			tabManager.AddTab(name, obj, cb!=null?cb:() => { spinnyIcon.SetActive(false); HidePanels(); Connect.createLobbyContainer.SetActive(false); });


			var im = Base.AddImage(obj.transform, new Vector2(5, -35f), new Vector2(width - 10, 2), null);
			im.color = new Color(0, 0, 0, 0.25f);

			Base.AddLabel(obj.transform, new Vector2(0, -5), new Vector2(width, 30), string.IsNullOrEmpty(longName)?name:longName, 24f);

			return obj;
		}

		private static GameObject CreateContainer(string name, string longName = "", Action cb = null)
		{
			var obj = new GameObject(name + "Container", typeof(RectTransform));
			//lobbyContainer = cont.transform;
			obj.transform.SetParent(mainPanel.transform, false);
			var panelRt = obj.GetComponent<RectTransform>();
			panelRt.anchorMin = new Vector2(0.5f, 0.5f);
			panelRt.anchorMax = new Vector2(0.5f, 0.5f);
			panelRt.pivot = new Vector2(0.5f, 0.5f);
			panelRt.anchoredPosition = new Vector2(0, 0f);
			panelRt.sizeDelta = new Vector2(width, height);

			var im = Base.AddImage(obj.transform, new Vector2(5, -35f), new Vector2(width - 10, 2), null);
			im.color = new Color(0, 0, 0, 0.25f);

			Base.AddLabel(obj.transform, new Vector2(0, -5), new Vector2(width, 30), string.IsNullOrEmpty(longName) ? name : longName, 24f);

			return obj;
		}
	}

}


public class DetectMainPanel : MonoBehaviour
{
	public void OnEnable()
	{
		GameData.PlayerTyping = true;
	}
	public void OnDisable()
	{
		GameData.PlayerTyping = false;
	}
	public void OnDestroy()
	{
		GameData.PlayerTyping = false;
	}
}