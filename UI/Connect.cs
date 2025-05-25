using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using ErenshorCoop.Client;
using ErenshorCoop.Server;
using ErenshorCoop.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

		private const float TextIndentWidth = 10f;
		public static GameObject CreateConnectUi(Canvas canvas)
		{
			var x = 300f;
			var y = 100f;

			const float width = 130f;


			var panel = new GameObject("ConnectGroup", typeof(RectTransform));
			panel.transform.SetParent(canvas.transform);
			var panelRt = panel.GetComponent<RectTransform>();
			panelRt.anchorMin = new Vector2(0.5f,     0.5f);
			panelRt.anchorMax = new Vector2(0.5f,     0.5f);
			panelRt.pivot = new Vector2(0.5f,         0.5f);
			panelRt.anchoredPosition = new Vector2(x, y);
			panelRt.sizeDelta = new Vector2(width,    200f);

			var panelImage = panel.AddComponent<Image>();
			panelImage.color = new Color(1, 1, 1, 1f);
			panelImage.raycastTarget = false;
			panelImage.material = Base.materials["UI_OUTLINE CHAT"];
			panelImage.sprite = Base.sprites["cosmetics_and_essentials_carv 1"];

			var outline = panel.AddComponent<Outline>();
			outline.effectColor = new Color(0.6f, 0.6f, 0.6f, 0.65f);
			outline.effectDistance = new Vector2(1f, -1f);


			x = 0;
			y = 0;
			Base.AddLabel(panel.transform, new Vector2(x, y + 6), new Vector2(width, 30), "CO-OP");
			var vL = Base.AddLabel(panel.transform, new Vector2(x + 30, y - 5), new Vector2(width, 30), "v."+ErenshorCoopMod.version.ToString());
			vL.fontSize = 8f;

			x = 0;
			y = -30;
			var l = Base.AddLabel(panel.transform, new Vector2(x+5, y+6), new Vector2(width, 30), "IP");
			l.alignment = TextAlignmentOptions.Left;
			y -= 18f;
			_addressInput = Base.AddInputField(panel.transform, new Vector2(x+5, y),new Vector2(width-10,16), "IP Address...");
			_addressInput.text = ClientConfig.SavedIP.Value;
			_addressInput.characterValidation = InputField.CharacterValidation.None;
			_addressInput.onValidateInput += (_, _, addedChar) => {
				var black = new List<char> {' ', '\n', '\t', '\v', '\f', '\b', '\r'};
				return black.Contains(addedChar) ? '\0' : addedChar;
			};
			y -= 22f;

			l= Base.AddLabel(panel.transform, new Vector2(x+5, y), new Vector2(width, 30), "Port");
			l.alignment = TextAlignmentOptions.Left;
			y -= 24f;
			_portInput = Base.AddInputField(panel.transform, new Vector2(x+5, y), new Vector2(width-10, 16), "Port...");
			_portInput.text = ClientConfig.SavedPort.Value;
			_portInput.characterValidation = InputField.CharacterValidation.None;
			_portInput.onValidateInput += (_, _, addedChar) => {
				var all = new List<char> {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9'};
				return !all.Contains(addedChar) ? '\0' : addedChar;
			};

			y -= 10;

			_feedbackText = Base.AddLabel(panel.transform, new Vector2(x, y), new Vector2(width, 30), "");
			_feedbackText.alignment = TextAlignmentOptions.Center;

			y -= 25f;

			connectButton = Base.AddButton(panel.transform, new Vector2(x+15,   y), new Vector2(width-30,   30), "Connect", OnConnectButtonPressed);
			disconnectButton = Base.AddButton(panel.transform, new Vector2(x + 15, y), new Vector2(width - 30, 30), "Disconnect", OnDisconnectButtonPressed);
			disconnectButton.gameObject.SetActive(false);
			y -= 30f + 8f;

			hostButton = Base.AddButton(panel.transform, new Vector2(x+15, y), new Vector2(width-30, 30), "Start Hosting", OnStartButtonPressed);
			y -= 30f + 8f;

			//AddButton(panel.transform, new Vector2(x, y), "Settings", () => Debug.Log("Open Settings"));
			//y -= 30f + 8f;

			
			//_feedbackText.gameObject.SetActive(false);

			return panel;
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


			ClientConnectionManager.Instance.Connect(ipString, portNumber);
			hostButton.gameObject.SetActive(false);
			connectButton.gameObject.SetActive(false);
			disconnectButton.gameObject.SetActive(true);
		}

		public static void EnableButtons()
		{
			hostButton.gameObject.SetActive(true);
			connectButton.gameObject.SetActive(true);
			disconnectButton.gameObject.SetActive(false);
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

		
	}

}
