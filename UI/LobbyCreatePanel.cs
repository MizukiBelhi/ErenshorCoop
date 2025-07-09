using ErenshorCoop.Shared;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
	public class LobbyCreatePanel : IPanel
	{
		private static Transform container;

		private static float width = 0;
		private static float height = 0;

		private static InputField lobbyNameInput;
		private static InputField passwordInput;
		private static Dropdown lobbyTypeDropdown;
		private static Slider lobbyPlayerCountSlider;

		private TMP_Text barText;
		private TMP_Text infoText;

		public GameObject CreatePanel(Transform parent, float _width, float _height, Canvas canvas = null)
		{
			width = _width;
			height = _height;
			container = parent;

			float y = -50;
			/*var l = Base.AddLabel(parent, new Vector2(25, y-20), new Vector2(200, 14), "Direct Connection");
			l.alignment = TMPro.TextAlignmentOptions.Left;
			var tog = Base.AddToggle(parent, new Vector2(250,y -20), new Vector2(16, 16), (bool _) => {  });
			tog.isOn = false;
			tog.interactable = false;
			y -= 20;*/

			var l = Base.AddLabel(parent, new Vector2(25, y-20), new Vector2(100, 14), "Lobby Name: ");
			l.alignment = TMPro.TextAlignmentOptions.Left;
			l.raycastTarget = false;
			lobbyNameInput = Base.AddInputField(parent, new Vector2(150, y - 20), new Vector2(200, 16), "Lobby Name");
			lobbyNameInput.characterLimit = 50;
			lobbyNameInput.characterValidation = InputField.CharacterValidation.None;
			lobbyNameInput.text = $"{GameData.CurrentCharacterSlot.CharName}'s Lobby";
			lobbyNameInput.onValidateInput += (_, _, addedChar) => {
				var black = new List<char> { '\n', '\t', '\v', '\f', '\b', '\r' };
				return black.Contains(addedChar) ? '\0' : addedChar;
			};

			y -= 20;
			l = Base.AddLabel(parent, new Vector2(25, y - 20), new Vector2(100, 14), "Password: ");
			l.raycastTarget = false;
			l.alignment = TMPro.TextAlignmentOptions.Left;
			passwordInput = Base.AddInputField(parent, new Vector2(150, y - 20), new Vector2(200, 16), "Leave Empty For NONE");
			passwordInput.characterValidation = InputField.CharacterValidation.None;
			passwordInput.onValidateInput += (_, _, addedChar) => {
				var black = new List<char> { '\n', '\t', '\v', '\f', '\b', '\r' };
				return black.Contains(addedChar) ? '\0' : addedChar;
			};
			y -= 20;
			l = Base.AddLabel(parent, new Vector2(25, y - 20), new Vector2(100, 14), "Lobby Type: ");
			l.alignment = TMPro.TextAlignmentOptions.Left;
			l.raycastTarget = false;
			lobbyTypeDropdown = Base.AddDropdown(parent, new Vector2(150, y - 20), new Vector2(200, 16), new(){"Public", "Invite Only", "Friends Only"}, (int _) => { });
			y -= 20;

			barText = Base.AddLabel(parent, new Vector2(25, y - 20), new Vector2(130, 14), "Max Players (4): ");
			barText.alignment = TMPro.TextAlignmentOptions.Left;
			barText.raycastTarget = false;
			lobbyPlayerCountSlider = Base.AddSlider(parent, new Vector2(150, y - 20), new Vector2(200, 16), (float _) => { barText.text = $"Max Players ({(int)_}): "; var c = (lobbyPlayerCountSlider.value * 150f) / 1024f; infoText.text = $"Warning: With the current selected Max Players amount,\nYou will need approx. more than {c.ToString("F2", CultureInfo.InvariantCulture)} MBps upload speed."; }, 2, 100, 4);
			y -= 20;
			var c = (lobbyPlayerCountSlider.value * 150f) / 1024f;
			infoText = Base.AddLabel(parent, new Vector2(25, y - 35), new Vector2(width, 30), $"Warning: With the current selected Max Players amount,\nYou will need approx. more than {c.ToString("F2", CultureInfo.InvariantCulture)} MBps upload speed.");
			infoText.alignment = TMPro.TextAlignmentOptions.Left;
			infoText.raycastTarget = false;
			Base.AddButton(parent, new Vector2(50, y -70), new Vector2(100, 30), "Create", OnLobbieCreate);
			Base.AddButton(parent, new Vector2(175, y-70), new Vector2(100, 30), "Cancel", OnCancel);

			return null;
		}

		public static void OnCancel()
		{
			Connect.createLobbyContainer.SetActive(false);
			LobbyPanel.lobbyContainer.gameObject.SetActive(true);
		}
		public static void OnLobbieCreate()
		{
			if (string.IsNullOrEmpty(lobbyNameInput.text)) return;

			ELobbyType lobbyType = ELobbyType.k_ELobbyTypePublic;
			if (lobbyTypeDropdown.value == 1)
				lobbyType = ELobbyType.k_ELobbyTypePrivate;
			if (lobbyTypeDropdown.value == 2)
				lobbyType = ELobbyType.k_ELobbyTypeFriendsOnly;

			var mPl = Math.Max(2, (int)lobbyPlayerCountSlider.value);
			mPl = Math.Min(100, mPl);

			Steam.Lobby.CreateLobby(lobbyNameInput.text.Sanitize(), passwordInput.text, lobbyType, mPl);

			Connect.createLobbyContainer.SetActive(false);
		}
	}
}
