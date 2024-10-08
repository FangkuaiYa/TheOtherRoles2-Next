using HarmonyLib;
using System;
using System.Collections.Generic;
using TheOtherRoles.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using static UnityEngine.UI.Button;
using Object = UnityEngine.Object;

namespace TheOtherRoles.Patches
{
	[HarmonyPatch]
	public static class ClientOptionsPatch
	{
		private static List<SelectionBehaviour> AllOptions = new();
		private static void setAllOptions()
		{
			AllOptions = new() {
			new (new ("ClientOptions", 1), () => MapOptionsTor.ghostsSeeInformation  = TheOtherRolesPlugin.GhostsSeeInformation.Value = !TheOtherRolesPlugin.GhostsSeeInformation.Value, TheOtherRolesPlugin.GhostsSeeInformation.Value, 1),
			new (new ("ClientOptions", 2), () => MapOptionsTor.ghostsSeeVotes = TheOtherRolesPlugin.GhostsSeeVotes.Value = !TheOtherRolesPlugin.GhostsSeeVotes.Value, TheOtherRolesPlugin.GhostsSeeVotes.Value, 2),
			new (new ("ClientOptions", 3), () => MapOptionsTor.ghostsSeeRoles = TheOtherRolesPlugin.GhostsSeeRoles.Value = !TheOtherRolesPlugin.GhostsSeeRoles.Value, TheOtherRolesPlugin.GhostsSeeRoles.Value, 3),
			new (new ("ClientOptions", 4), () => MapOptionsTor.ghostsSeeModifier = TheOtherRolesPlugin.GhostsSeeModifier.Value = !TheOtherRolesPlugin.GhostsSeeModifier.Value, TheOtherRolesPlugin.GhostsSeeModifier.Value, 4),
			new (new ("ClientOptions", 5), () => MapOptionsTor.showRoleSummary = TheOtherRolesPlugin.ShowRoleSummary.Value = !TheOtherRolesPlugin.ShowRoleSummary.Value, TheOtherRolesPlugin.ShowRoleSummary.Value, 5),
			new (new ("ClientOptions", 6), () => MapOptionsTor.showLighterDarker = TheOtherRolesPlugin.ShowLighterDarker.Value = !TheOtherRolesPlugin.ShowLighterDarker.Value, TheOtherRolesPlugin.ShowLighterDarker.Value, 6),
			new (new ("ClientOptions", 7), () => MapOptionsTor.toggleCursor = TheOtherRolesPlugin.ToggleCursor.Value = !TheOtherRolesPlugin.ToggleCursor.Value, TheOtherRolesPlugin.ToggleCursor.Value, 7),
            // new ("Hide Kill Animation", () => MapOptionsTor.showKillAnimation = TheOtherRolesPlugin.showKillAnimation.Value = !TheOtherRolesPlugin.showKillAnimation.Value, TheOtherRolesPlugin.showKillAnimation.Value, 8),
            new (new ("ClientOptions", 8), () => MapOptionsTor.enableSoundEffects = TheOtherRolesPlugin.EnableSoundEffects.Value = !TheOtherRolesPlugin.EnableSoundEffects.Value, TheOtherRolesPlugin.EnableSoundEffects.Value, 9),
			new (new ("ClientOptions", 9), () => MapOptionsTor.ShowVentsOnMap = TheOtherRolesPlugin.ShowVentsOnMap.Value = !TheOtherRolesPlugin.ShowVentsOnMap.Value, TheOtherRolesPlugin.ShowVentsOnMap.Value, 10),
			new(new ("ClientOptions", 10), () => MapOptionsTor.ShowChatNotifications = TheOtherRolesPlugin.ShowChatNotifications.Value = !TheOtherRolesPlugin.ShowChatNotifications.Value, TheOtherRolesPlugin.ShowChatNotifications.Value, 11),
			new(new ("ClientOptions", 11), () => MapOptionsTor.showFPS = TheOtherRolesPlugin.ShowFPS.Value = !TheOtherRolesPlugin.ShowFPS.Value, TheOtherRolesPlugin.ShowFPS.Value, 12),
		};
		}

		private static GameObject popUp;
		private static TextMeshPro titleText;
		private static ToggleButtonBehaviour moreOptions = null;
		private static TextMeshPro titleTextTitle;
		private static ToggleButtonBehaviour buttonPrefab;
		private static Vector3? _origin;
		private static int page = 1;
		private static List<ToggleButtonBehaviour> modButtons = new();
		[HarmonyPostfix]
		[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
		public static void MainMenuManager_StartPostfix(MainMenuManager __instance)
		{
			// Prefab for the title
			var go = new GameObject("TitleTextTOR");
			var tmp = go.AddComponent<TextMeshPro>();
			tmp.fontSize = 4;
			tmp.alignment = TextAlignmentOptions.Center;
			tmp.transform.localPosition += Vector3.left * 0.2f;
			titleText = Object.Instantiate(tmp);
			//Object.Destroy(titleText.GetComponent<TextTranslatorTMP>());
			titleText.gameObject.SetActive(false);
			Object.DontDestroyOnLoad(titleText);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Start))]
		public static void OptionsMenuBehaviour_StartPostfix(OptionsMenuBehaviour __instance)
		{
			if (!__instance.CensorChatButton) return;

			if (!popUp)
			{
				CreateCustom(__instance);
			}

			if (!buttonPrefab)
			{
				buttonPrefab = Object.Instantiate(__instance.CensorChatButton);
				Object.DontDestroyOnLoad(buttonPrefab);
				buttonPrefab.name = "CensorChatPrefab";
				buttonPrefab.gameObject.SetActive(false);
			}

			SetUpOptions();
			InitializeMoreButton(__instance);
		}

		private static void CreateCustom(OptionsMenuBehaviour prefab)
		{
			popUp = Object.Instantiate(prefab.gameObject);
			Object.DontDestroyOnLoad(popUp);
			var transform = popUp.transform;
			var pos = transform.localPosition;
			pos.z = -810f;
			transform.localPosition = pos;

			Object.Destroy(popUp.GetComponent<OptionsMenuBehaviour>());
			foreach (var gObj in popUp.gameObject.GetAllChilds())
			{
				if (gObj.name != "Background" && gObj.name != "CloseButton")
					Object.Destroy(gObj);
			}

			popUp.SetActive(false);
		}

		private static void InitializeMoreButton(OptionsMenuBehaviour __instance)
		{
			var moreOptions = Object.Instantiate(buttonPrefab, __instance.CensorChatButton.transform.parent);
			var transform = __instance.CensorChatButton.transform;
			__instance.CensorChatButton.Text.transform.localScale = new Vector3(1 / 0.66f, 1, 1);
			_origin ??= transform.localPosition;

			transform.localPosition = _origin.Value + Vector3.left * 0.45f;
			transform.localScale = new Vector3(0.66f, 1, 1);
			__instance.EnableFriendInvitesButton.transform.localScale = new Vector3(0.66f, 1, 1);
			__instance.EnableFriendInvitesButton.transform.localPosition += Vector3.right * 0.5f;
			__instance.EnableFriendInvitesButton.Text.transform.localScale = new Vector3(1.2f, 1, 1);

			moreOptions.transform.localPosition = _origin.Value + Vector3.right * 4f / 3f;
			moreOptions.transform.localScale = new Vector3(0.66f, 1, 1);

			moreOptions.gameObject.SetActive(true);
			moreOptions.Text.text = ModTranslation.GetString("ClientOptions", 12);
			moreOptions.Text.transform.localScale = new Vector3(1 / 0.66f, 1, 1);
			var moreOptionsButton = moreOptions.GetComponent<PassiveButton>();
			moreOptionsButton.OnClick = new ButtonClickedEvent();
			moreOptionsButton.OnClick.AddListener((Action)(() =>
			{
				bool closeUnderlying = false;
				if (!popUp) return;

				if (__instance.transform.parent && __instance.transform.parent == FastDestroyableSingleton<HudManager>.Instance.transform)
				{
					popUp.transform.SetParent(FastDestroyableSingleton<HudManager>.Instance.transform);
					popUp.transform.localPosition = new Vector3(0, 0, -800f);
					closeUnderlying = true;
				}
				else
				{
					popUp.transform.SetParent(null);
					Object.DontDestroyOnLoad(popUp);
				}

				CheckSetTitle();
				RefreshOpen();
				if (closeUnderlying)
					__instance.Close();
			}));
		}

		private static void RefreshOpen()
		{
			popUp.gameObject.SetActive(false);
			popUp.gameObject.SetActive(true);
			SetUpOptions();
		}

		private static void CheckSetTitle()
		{
			if (!popUp || popUp.GetComponentInChildren<TextMeshPro>() || !titleText) return;

			var title = titleTextTitle = Object.Instantiate(titleText, popUp.transform);
			title.GetComponent<RectTransform>().localPosition = Vector3.up * 2.3f;
			title.gameObject.SetActive(true);
			title.text = titleTextTitle.text = ModTranslation.GetString("ClientOptions", 13);
			title.name = "TitleText";
		}

		private static void SetUpOptions()
		{
			//if (popUp.transform.GetComponentInChildren<ToggleButtonBehaviour>()) return;
			setAllOptions();

			foreach (var button in modButtons)
			{
				if (button != null) GameObject.Destroy(button.gameObject);
			}

			modButtons = new List<ToggleButtonBehaviour>();
			int length = (page * 10) < AllOptions.Count ? page * 10 : AllOptions.Count;

			for (var i = 0; i + ((page - 1) * 10) < length; i++)
			{
				var info = AllOptions[i + ((page - 1) * 10)];

				var button = Object.Instantiate(buttonPrefab, popUp.transform);
				var pos = new Vector3(i % 2 == 0 ? -1.17f : 1.17f, 1.3f - i / 2 * 0.8f, -.5f);

				var transform = button.transform;
				transform.localPosition = pos;

				button.onState = info.DefaultValue;
				button.Background.color = button.onState ? Color.green : Palette.ImpostorRed;

				button.Text.text = info.Title.GetString();
				button.Text.fontSizeMin = button.Text.fontSizeMax = 1.9f;
				button.Text.font = Object.Instantiate(titleText.font);
				button.Text.GetComponent<RectTransform>().sizeDelta = new Vector2(2, 2);

				button.name = info.Title.GetString().Replace(" ", "") + "Toggle";
				button.gameObject.SetActive(true);

				var passiveButton = button.GetComponent<PassiveButton>();
				var colliderButton = button.GetComponent<BoxCollider2D>();

				colliderButton.size = new Vector2(2.2f, .7f);

				passiveButton.OnClick = new ButtonClickedEvent();
				passiveButton.OnMouseOut = new UnityEvent();
				passiveButton.OnMouseOver = new UnityEvent();

				passiveButton.OnClick.AddListener((Action)(() =>
				{
					if (info.Number == 7)
					{
						Helpers.enableCursor(false);
					}
					button.onState = info.OnClick();
					button.Background.color = button.onState ? Color.green : Palette.ImpostorRed;
				}));

				passiveButton.OnMouseOver.AddListener((Action)(() => button.Background.color = new Color32(34, 139, 34, byte.MaxValue)));
				passiveButton.OnMouseOut.AddListener((Action)(() => button.Background.color = button.onState ? Color.green : Palette.ImpostorRed));

				foreach (var spr in button.gameObject.GetComponentsInChildren<SpriteRenderer>())
					spr.size = new Vector2(2.2f, .7f);

				modButtons.Add(button);
			}
			if (page * 10 < AllOptions.Count)
			{
				var button = Object.Instantiate(buttonPrefab, popUp.transform);
				var pos = new Vector3(1.2f, -2.5f, -0.5f);
				var transform = button.transform;
				transform.localPosition = pos;
				button.Text.text = ModTranslation.GetString("ClientOptions", 14);
				button.Text.fontSizeMin = button.Text.fontSizeMax = 2.2f;
				button.Text.font = Object.Instantiate(titleText.font);
				button.Text.GetComponent<RectTransform>().sizeDelta = new Vector2(2, 2);
				button.gameObject.SetActive(true);
				var passiveButton = button.GetComponent<PassiveButton>();
				var colliderButton = button.GetComponent<BoxCollider2D>();
				colliderButton.size = new Vector2(2.2f, .7f);
				passiveButton.OnClick = new ButtonClickedEvent();
				passiveButton.OnMouseOut = new UnityEvent();
				passiveButton.OnMouseOver = new UnityEvent();
				passiveButton.OnClick.AddListener((Action)(() =>
				{
					page += 1;
					SetUpOptions();
				}));
				modButtons.Add(button);
			}
			if (page > 1)
			{
				var button = Object.Instantiate(buttonPrefab, popUp.transform);
				var pos = new Vector3(-1.2f, -2.5f, -0.5f);
				var transform = button.transform;
				transform.localPosition = pos;
				button.Text.text = ModTranslation.GetString("ClientOptions", 15);
				button.Text.fontSizeMin = button.Text.fontSizeMax = 2.2f;
				button.Text.font = Object.Instantiate(titleText.font);
				button.Text.GetComponent<RectTransform>().sizeDelta = new Vector2(2, 2);
				button.gameObject.SetActive(true);
				var passiveButton = button.GetComponent<PassiveButton>();
				var colliderButton = button.GetComponent<BoxCollider2D>();
				colliderButton.size = new Vector2(2.2f, .7f);
				passiveButton.OnClick = new ButtonClickedEvent();
				passiveButton.OnMouseOut = new UnityEvent();
				passiveButton.OnMouseOver = new UnityEvent();
				passiveButton.OnClick.AddListener((Action)(() =>
				{
					page -= 1;
					SetUpOptions();
				}));
				modButtons.Add(button);
			}
		}

		private static IEnumerable<GameObject> GetAllChilds(this GameObject Go)
		{
			for (var i = 0; i < Go.transform.childCount; i++)
			{
				yield return Go.transform.GetChild(i).gameObject;
			}
		}
		public static void UpdateTranslations()
		{
			if (titleTextTitle)
				titleTextTitle.text = ModTranslation.GetString("ClientOptions", 13);

			if (moreOptions)
				moreOptions.Text.text = ModTranslation.GetString("ClientOptions", 12);

			for (int i = 0; i < AllOptions.Count; i++)
			{
				if (i >= modButtons.Count) break;
				modButtons[i].Text.text = AllOptions[i].Title.GetString();
			}
		}
		public class SelectionBehaviour
		{
			public TranslationInfo Title;
			public Func<bool> OnClick;
			public bool DefaultValue;
			public int Number;

			public SelectionBehaviour(TranslationInfo title, Func<bool> onClick, bool defaultValue, int number)
			{
				Title = title;
				OnClick = onClick;
				DefaultValue = defaultValue;
				Number = number;
			}
		}
	}
}
