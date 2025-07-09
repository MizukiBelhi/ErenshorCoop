using System;
using System.Collections.Generic;
using ErenshorCoop.Client;
using ErenshorCoop.Server;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ErenshorCoop.UI
{
	public class Main : MonoBehaviour
	{
		public static Main _instance;

		private Text statisticsText;
		private long lastBReceived = 0;
		private long lastBSent = 0;
		public static GameObject statsPanel;

		private  const float StatsUpdateTime = 1f;
		private float statsTimer = 0f;

		private static GameObject mainMenuButton;
		private static TextMeshProUGUI promptText;
		private static GameObject promptPanel;
		private static Action acceptCB;
		private static Action cancelCB;
		public static InputField promptInput;

		public static GameObject connectUI;

		public static bool isGameMenuOpen = false;

		public static CSteamID selectedLobby;

		private bool hasDoneConnect = false;

		public void UpdateStatistics()
		{
			statsTimer += Time.deltaTime;

			if (!( statsTimer >= StatsUpdateTime )) return;

			statsTimer = 0f;

			if (statsPanel == null) return;
			if (!statsPanel.activeSelf) return;
			if (!ClientConnectionManager.Instance.IsRunning) return;

			if (!Steam.Lobby.isInLobby)
			{
				var statistics = ClientConnectionManager.Instance.GetStatistics();
				int ping = ClientConnectionManager.Instance.Server.Ping;

				if (ServerConnectionManager.Instance.IsRunning)
				{
					statistics = ServerConnectionManager.Instance.GetStatistics();
					ping = 0;
				}

				long bReceived = statistics.BytesReceived;
				long bSent = statistics.BytesSent;



				float rec = (bReceived - lastBReceived) / 1024f;
				float sen = (bSent - lastBSent) / 1024f;

				lastBReceived = bReceived;
				lastBSent = bSent;

				var inout = $"In/Out: {rec:F2}|{sen:F2} KB/s  Packet Loss: {statistics.PacketLossPercent}%";

				statisticsText.text = $"Ping: {ping}ms {inout}";
			}
			else
			{
				var stats = Steam.Networking.GetConnectionInfo();

				var inout = $"In/Out: {stats.RecvKBps:F2}|{stats.SentKBps:F2} KB/s  Packet Loss: {stats.DroppedPacketPercent}%";

				statisticsText.text = $"Ping: {stats.PingMs}ms {inout}";
			}

		}

		public void Update()
		{
			UpdateStatistics();


			if (GameData.GM != null && GameData.GM.EscapeMenu != null && GameData.GM.EscapeMenu.activeSelf != isGameMenuOpen)
			{
				isGameMenuOpen = GameData.GM.EscapeMenu.activeSelf;
			}

			if(connectUI != null && Connect.tabManager != null)
			{

				
				Connect.Update();
				Connect.tabManager.tabButtonsContainer.SetActive(connectUI.activeSelf);
				selectedLobby = LobbyPanel.selectedLobby;
				if(Connect.spinnyIcon != null)
				{
					Connect.spinnyIcon.transform.localRotation = Quaternion.Euler(0, 0, Connect.spinnyIcon.transform.rotation.eulerAngles.z - 1f);
				}
			}
			
		}

		public void OnDestroy()
		{
			if (connectUI != null)
			{
				GameData.Misc.UIWindows.Remove(connectUI);
			}
			Connect.Cleanup();
			ClientConnectionManager.Instance.OnConnect -= OnConnect;
			ClientConnectionManager.Instance.OnDisconnect -= OnDisconnect;
			ErenshorCoopMod.OnGameMapLoad -= OnMapLoad;

			if (mainMenuButton != null)
				Destroy(mainMenuButton);

			Logging.Log($"UI Destroyed");
		}


		private void OnCoopMenuOpen()
		{
			
			if (connectUI != null)
			{
				connectUI.SetActive(!connectUI.activeSelf);

				if (Connect.tabManager != null)
				{
					if (Connect.tabManager.currentActiveTab == 0)
						LobbyPanel.OnLobbieRefresh();
				}
			}
		}

		public void OnMapLoad(Scene scene)
		{
			if (scene.name == "Menu" || scene.name == "LoadScene") return;

			LoadMenus();
		}

		public void Awake()
		{
			if (_instance != null) Destroy(gameObject);

			_instance = this;
			DontDestroyOnLoad(this);

			if (EventSystem.current == null)
			{
				new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
			}

			ClientConnectionManager.Instance.OnConnect += OnConnect;
			ClientConnectionManager.Instance.OnDisconnect += OnDisconnect;
			ErenshorCoopMod.OnGameMapLoad += OnMapLoad;
			OnMapLoad(SceneManager.GetActiveScene());

			//My Sanity
			if (GameData.Misc != null && GameData.Misc.UIWindows != null)
			{
				var wins = GameData.Misc.UIWindows;
				for (int i = wins.Count - 1; i >= 0; i--)
				{
					if (wins[i] == null)
						wins.RemoveAt(i);
				}
			}
		}

		public void LoadMenus()
		{
			var canvasObject = this.gameObject;

			var canvas = canvasObject.AddComponent<Canvas>();
			//HACK: UI Should be deleted when going to menu and rebuilt when entering the game
			if (canvas != null)
			{
				Base.LoadSpritesAndMaterials();

				canvas.renderMode = RenderMode.ScreenSpaceOverlay;
				canvasObject.AddComponent<GraphicRaycaster>();
				canvas.overrideSorting = true;
				canvas.sortingOrder = 999;

				

				var group = canvasObject.AddComponent<CanvasGroup>();
				group.blocksRaycasts = true;
				group.interactable = true;

				var scaler = canvasObject.AddComponent<CanvasScaler>();
				scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
				scaler.referenceResolution = new Vector2(1920, 1200);
				scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
				scaler.matchWidthOrHeight = 0.5f;

				statsPanel = new GameObject("Panel");
				statsPanel.transform.SetParent(canvas.transform);
				var panelRect = statsPanel.AddComponent<RectTransform>();
				panelRect.sizeDelta = new Vector2(410, 16);
				panelRect.anchorMin = new Vector2(0, 1);
				panelRect.anchorMax = new Vector2(0, 1);
				panelRect.pivot = new Vector2(0, 1);
				panelRect.anchoredPosition = new Vector2(10, -10);


				var panelImage = statsPanel.AddComponent<Image>();
				panelImage.color = new Color(0, 0, 0, 0.5f);
				panelImage.raycastTarget = false;

				var sentTextObject = new GameObject("SentDataText");
				sentTextObject.transform.SetParent(statsPanel.transform);
				statisticsText = sentTextObject.AddComponent<Text>();
				statisticsText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
				statisticsText.color = Color.white;
				statisticsText.fontSize = 14;
				statisticsText.text = "Ping: 999 In/Out: 0|0 KB/s";
				var textRect = statisticsText.rectTransform;
				textRect.anchorMin = new Vector2(0, 0.5f);
				textRect.anchorMax = new Vector2(0, 0.5f);
				textRect.pivot = new Vector2(0, 0.5f);
				textRect.anchoredPosition = new Vector2(5, 0);
				textRect.sizeDelta = new Vector2(400, 16);
				statisticsText.raycastTarget = false;

				statsPanel.SetActive(false);

				(promptPanel, promptText, promptInput) = CreatePrompt(canvasObject);
				promptPanel.SetActive(false);

				if (connectUI == null)
					connectUI = Connect.CreateConnectUi(canvas);


				if (GameData.GM != null && GameData.GM.EscapeMenu != null)
					isGameMenuOpen = GameData.GM.EscapeMenu.activeSelf;

				
				
			}
			if(!GameData.Misc.UIWindows.Contains(connectUI))
				GameData.Misc.UIWindows.Add(connectUI);
			//Add a button to the main menu
			var escMenu = GameData.GM.EscapeMenu.transform;
			if (mainMenuButton == null)
				mainMenuButton = Base.AddButton(escMenu, new Vector2(0f, -254.0358f), new Vector2(175, 35), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Base.sprites["dd_form 1"], "CO-OP", () => { OnCoopMenuOpen(); }).gameObject;


			connectUI.SetActive(false);
		}

		public static void EnablePrompt(string text, Action cbAccept, Action cbDecline, bool hasInput=false)
		{
			acceptCB = cbAccept;
			cancelCB = cbDecline;
			promptText.text = text;
			promptPanel.SetActive(true);
			promptInput.gameObject.SetActive(hasInput);
			promptInput.text = "";
			promptPanel.transform.SetAsLastSibling();
		}

		private (GameObject, TextMeshProUGUI, InputField) CreatePrompt(GameObject canvasGO)
		{
			// Background panel
			var panelGO = new GameObject("PromptPanel", typeof(Image));
			panelGO.transform.SetParent(canvasGO.transform, false);
			var panel = panelGO.GetComponent<Image>();
			//panel.color = new Color(0, 0, 0, 0.7f);
			panel.sprite = Base.sprites["cosmetics_and_essentials_carv 1"];
			var panelRT = panel.GetComponent<RectTransform>();
			panelRT.sizeDelta = new Vector2(300, 140);
			panelRT.anchoredPosition = Vector2.zero;
			panel.raycastTarget = true;

			// TMP Text
			var textGO = new GameObject("PromptText", typeof(TextMeshProUGUI));
			textGO.transform.SetParent(panelGO.transform, false);
			var tmp = textGO.GetComponent<TextMeshProUGUI>();
			tmp.text = "";
			tmp.fontSize = 14;
			tmp.alignment = TextAlignmentOptions.Center;
			var tmpRT = tmp.GetComponent<RectTransform>();
			tmpRT.sizeDelta = new Vector2(280, 50);
			tmpRT.anchoredPosition = new Vector2(0, 30);


			var acceptGO = CreateButton("Accept", new Vector2(-60, -35), panelGO.transform);
			acceptGO.GetComponent<Button>().onClick.AddListener(() => {
				try { acceptCB?.Invoke(); }
				catch (Exception e)
				{
					Logging.LogError(e.Message + "\r\n" + e.StackTrace);
				}
				acceptCB = null; promptPanel.SetActive(false); });

			var declineGO = CreateButton("Decline", new Vector2(60, -35), panelGO.transform);
			declineGO.GetComponent<Button>().onClick.AddListener(() =>
			{
				try { cancelCB?.Invoke(); } catch (Exception e)
				{
					Logging.LogError(e.Message + "\r\n"+e.StackTrace);
				}

				cancelCB = null; promptPanel.SetActive(false);
			});

			var i = Base.AddInputField(panelGO.transform, new Vector2(50,-65), new Vector2(200, 16), "");
			i.characterValidation = InputField.CharacterValidation.None;
			i.onValidateInput += (_, _, addedChar) => {
				var black = new List<char> { '\n', '\t', '\v', '\f', '\b', '\r' };
				return black.Contains(addedChar) ? '\0' : addedChar;
			};
			i.gameObject.SetActive(false);

			return (panelGO, tmp, i);
		}

		private GameObject CreateButton(string label, Vector2 pos, Transform parent)
		{
			var btnGO = new GameObject(label + "Button", typeof(Button), typeof(Image));
			btnGO.transform.SetParent(parent, false);
			var rt = btnGO.GetComponent<RectTransform>();
			rt.sizeDelta = new Vector2(100, 36);
			rt.anchoredPosition = pos;
			var btn = btnGO.GetComponent<Button>();
			btn.interactable = true;
			btn.enabled = true;
			

			//I'LL TRY ANYTHING!
			var b = btnGO.AddComponent<ActivateButton>();

			var img = btnGO.GetComponent<Image>();
			img.transform.SetParent(btnGO.transform, false);
			img.raycastTarget = true;
			img.sprite = Base.sprites["heading_texture"];
			btn.targetGraphic = img;

			var textGO = new GameObject("Text", typeof(TextMeshProUGUI));
			textGO.transform.SetParent(btnGO.transform, false);
			var tmp = textGO.GetComponent<TextMeshProUGUI>();
			tmp.text = label;
			tmp.alignment = TextAlignmentOptions.Center;
			tmp.fontSize = 16;
			tmp.raycastTarget = false;
			var tmpRT = tmp.GetComponent<RectTransform>();
			tmpRT.anchorMin = Vector2.zero;
			tmpRT.anchorMax = Vector2.one;
			tmpRT.offsetMin = Vector2.zero;
			tmpRT.offsetMax = Vector2.zero;

			return btnGO;
		}

		private void OnDisconnect()
		{
			statsPanel?.SetActive(false);
			ConnectPanel.EnableButtons();
			hasDoneConnect = false;
		}

		private void OnConnect()
		{
			statsPanel?.SetActive(ClientConfig.DisplayMetrics.Value);
		}

		//Only ran once when connecting
		public void HandleOnConnect()
		{
			if (hasDoneConnect) return;
			hasDoneConnect = true;
			if (ClientConfig.CloseMenu.Value)
			{
				if (connectUI != null)
				{
					connectUI.SetActive(false);
					GameData.GM.EscapeMenu.SetActive(false);
				}
			}
			else
			{
				//Swap to player tab
				Connect.tabManager.SetActiveTab(3);
			}
		}
	}
}

