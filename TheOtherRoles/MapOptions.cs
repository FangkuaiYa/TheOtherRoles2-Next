using System.Collections.Generic;
using TheOtherRoles.Players;
using UnityEngine;

namespace TheOtherRoles
{
	static class MapOptionsTor
	{
		// Set values
		public static int maxNumberOfMeetings = 10;
		public static bool blockSkippingInEmergencyMeetings = false;
		public static bool noVoteIsSelfVote = false;
		public static bool hidePlayerNames = false;
		public static bool ghostsSeeRoles = true;
		public static bool ghostsSeeModifier = true;
		public static bool ghostsSeeInformation = true;
		public static bool ghostsSeeVotes = true;
		public static bool showRoleSummary = true;
		public static bool disableMedscanWalking = false;
		public static bool ShowChatNotifications = true;
		public static bool showFPS = true;

		public static bool allowParallelMedBayScans = false;
		public static bool showLighterDarker = true;
		public static bool toggleCursor = true;
		public static bool showKillAnimation = true;
		public static bool camoComms = false;

		public static int restrictDevices = 0;
		public static float restrictAdminTime = 600f;
		public static float restrictAdminTimeMax = 600f;
		public static float restrictCamerasTime = 600f;
		public static float restrictCamerasTimeMax = 600f;
		public static float restrictVitalsTime = 600f;
		public static float restrictVitalsTimeMax = 600f;
		public static bool enableSoundEffects = true;
		public static bool disableCamsRoundOne = false;
		public static bool isRoundOne = true;


		public static bool enableHorseMode = false;
		public static bool shieldFirstKill = false;
		public static bool ShowVentsOnMap = true;
		public static CustomGamemodes gameMode = CustomGamemodes.Classic;

		// Updating values
		public static int meetingsCount = 0;
		public static List<SurvCamera> camerasToAdd = new List<SurvCamera>();
		public static List<Vent> ventsToSeal = new List<Vent>();
		public static Dictionary<byte, PoolablePlayer> playerIcons = new Dictionary<byte, PoolablePlayer>();
		public static string firstKillName;
		public static PlayerControl firstKillPlayer;

		public static void clearAndReloadMapOptions()
		{
			meetingsCount = 0;
			camerasToAdd = new List<SurvCamera>();
			ventsToSeal = new List<Vent>();
			playerIcons = new Dictionary<byte, PoolablePlayer>(); ;

			maxNumberOfMeetings = Mathf.RoundToInt(CustomOptionHolder.maxNumberOfMeetings.getSelection());
			blockSkippingInEmergencyMeetings = CustomOptionHolder.blockSkippingInEmergencyMeetings.getBool();
			noVoteIsSelfVote = CustomOptionHolder.noVoteIsSelfVote.getBool();
			hidePlayerNames = CustomOptionHolder.hidePlayerNames.getBool();
			allowParallelMedBayScans = CustomOptionHolder.allowParallelMedBayScans.getBool();
			shieldFirstKill = CustomOptionHolder.shieldFirstKill.getBool();
			disableCamsRoundOne = CustomOptionHolder.disableCamsRound1.getBool();
			disableMedscanWalking = CustomOptionHolder.disableMedbayWalk.getBool();
			isRoundOne = true;
			firstKillPlayer = null;
			restrictDevices = CustomOptionHolder.restrictDevices.getSelection();
			restrictAdminTime = restrictAdminTimeMax = CustomOptionHolder.restrictAdmin.getFloat();
			restrictCamerasTime = restrictCamerasTimeMax = CustomOptionHolder.restrictCameras.getFloat();
			restrictVitalsTime = restrictVitalsTimeMax = CustomOptionHolder.restrictVents.getFloat();
			camoComms = CustomOptionHolder.enableCamoComms.getBool();

		}

		public static void reloadPluginOptions()
		{
			ghostsSeeRoles = TheOtherRolesPlugin.GhostsSeeRoles.Value;
			ghostsSeeModifier = TheOtherRolesPlugin.GhostsSeeModifier.Value;
			ghostsSeeInformation = TheOtherRolesPlugin.GhostsSeeInformation.Value;
			ghostsSeeVotes = TheOtherRolesPlugin.GhostsSeeVotes.Value;
			showRoleSummary = TheOtherRolesPlugin.ShowRoleSummary.Value;
			showLighterDarker = TheOtherRolesPlugin.ShowLighterDarker.Value;
			toggleCursor = TheOtherRolesPlugin.ToggleCursor.Value;
			showKillAnimation = TheOtherRolesPlugin.showKillAnimation.Value;

			enableSoundEffects = TheOtherRolesPlugin.EnableSoundEffects.Value;
			enableHorseMode = TheOtherRolesPlugin.EnableHorseMode.Value;
			ShowVentsOnMap = TheOtherRolesPlugin.ShowVentsOnMap.Value;
			ShowChatNotifications = TheOtherRolesPlugin.ShowChatNotifications.Value;
			showFPS = TheOtherRolesPlugin.ShowFPS.Value;
			//Patches.ShouldAlwaysHorseAround.isHorseMode = TheOtherRolesPlugin.EnableHorseMode.Value;
		}

		public static void resetDeviceTimes()
		{
			restrictAdminTime = restrictAdminTimeMax;
			restrictCamerasTime = restrictCamerasTimeMax;
			restrictVitalsTime = restrictVitalsTimeMax;
		}

		public static bool canUseAdmin => restrictDevices == 0 || restrictAdminTime > 0f || CachedPlayer.LocalPlayer.PlayerControl == Hacker.hacker || CachedPlayer.LocalPlayer.Data.IsDead;

		public static bool couldUseAdmin => restrictDevices == 0 || restrictAdminTimeMax > 0f || CachedPlayer.LocalPlayer.PlayerControl == Hacker.hacker || CachedPlayer.LocalPlayer.Data.IsDead;

		public static bool canUseCameras => restrictDevices == 0 || restrictCamerasTime > 0f || CachedPlayer.LocalPlayer.PlayerControl == Hacker.hacker || CachedPlayer.LocalPlayer.Data.IsDead;

		public static bool couldUseCameras => restrictDevices == 0 || restrictCamerasTimeMax > 0f || CachedPlayer.LocalPlayer.PlayerControl == Hacker.hacker || CachedPlayer.LocalPlayer.Data.IsDead;

		public static bool canUseVitals => restrictDevices == 0 || restrictVitalsTime > 0f || CachedPlayer.LocalPlayer.PlayerControl == Hacker.hacker || CachedPlayer.LocalPlayer.Data.IsDead;

		public static bool couldUseVitals => restrictDevices == 0 || restrictVitalsTimeMax > 0f || CachedPlayer.LocalPlayer.PlayerControl == Hacker.hacker || CachedPlayer.LocalPlayer.Data.IsDead;
	}
}
