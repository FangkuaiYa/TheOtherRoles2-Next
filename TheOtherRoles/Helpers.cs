using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using Hazel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TheOtherRoles.CustomGameModes;
using TheOtherRoles.Modules;
using TheOtherRoles.Objects;
using TheOtherRoles.Patches;
using TheOtherRoles.Players;
using TheOtherRoles.Utilities;
using TMPro;
using UnityEngine;
using static TheOtherRoles.TheOtherRoles;
using static UnityEngine.GraphicsBuffer;

namespace TheOtherRoles
{

	public enum MurderAttemptResult
	{
		ReverseKill,
		BothKill,
		PerformKill,
		SuppressKill,
		BlankKill,
		BodyGuardKill,
		DelayVampireKill
	}

	public enum CustomGamemodes
	{
		Classic,
		Guesser,
		HideNSeek,
		PropHunt
	}
	public enum RoleTeam
	{
		Crewmate,
		Impostor,
		Neutral,
		Modifier
	}

	public enum SabatageTypes
	{
		Comms,
		O2,
		Reactor,
		OxyMask,
		Lights,
		None
	}

	public static class Helpers
	{

		public static Dictionary<string, Sprite> CachedSprites = new();
		//public static Sprite teamJackalChat = null;
		//public static Sprite teamLoverChat = null;

		//public static Sprite getTeamJackalChatButtonSprite()
		//{
		//    if (teamJackalChat) return teamJackalChat;
		//    teamJackalChat = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.TeamJackalChat.png", 115f);
		//    return teamJackalChat;
		//}

		//public static Sprite getLoversChatButtonSprite()
		//{
		//    if (teamLoverChat) return teamLoverChat;
		//    teamLoverChat = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.LoversChat.png", 115f);
		//    return teamLoverChat;
		//}


		public static void enableCursor(bool initalSetCursor)
		{
			if (initalSetCursor)
			{
				Sprite sprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.Cursor.png", 115f);
				Cursor.SetCursor(sprite.texture, Vector2.zero, CursorMode.Auto);
				return;
			}
			if (TheOtherRolesPlugin.ToggleCursor.Value)
			{
				Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
			}
			else
			{
				Sprite sprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.Cursor.png", 115f);
				Cursor.SetCursor(sprite.texture, Vector2.zero, CursorMode.Auto);
			}
		}


		public static int flipBitwise(int bit)
		{
			return -(Math.Abs(bit - 1));
		}


		public static bool roleCanSabotage(this PlayerControl player)
		{
			bool roleCouldUse = false;
			if (Jackal.canSabotage)
			{
				if (player == Jackal.jackal || player == Sidekick.sidekick || Jackal.formerJackals.Contains(player))
				{
					roleCouldUse = true;
				}
			}
			if (player.Data?.Role != null && player.Data.Role.IsImpostor)
				roleCouldUse = true;
			return roleCouldUse;
		}

		public static bool isRoleAlive(PlayerControl role)
		{
			if (Mimic.mimic != null)
			{
				if (role == Mimic.mimic) return false;
			}
			return (role != null && isAlive(role));
		}

		public static bool killingCrewAlive()
		{
			// This functions blocks the game from ending if specified crewmate roles are alive
			if (!CustomOptionHolder.blockGameEnd.getBool()) return false;
			bool powerCrewAlive = false;

			if (isRoleAlive(Sheriff.sheriff)) powerCrewAlive = true;
			if (isRoleAlive(Veteran.veteran)) powerCrewAlive = true;
			if (isRoleAlive(Mayor.mayor)) powerCrewAlive = true;
			if (isRoleAlive(Swapper.swapper)) powerCrewAlive = true;

			return powerCrewAlive;
		}
		public static bool isNeutral(PlayerControl player)
		{
			RoleInfo roleInfo = RoleInfo.getRoleInfoForPlayer(player, false).FirstOrDefault();
			if (roleInfo != null)
				return roleInfo.isNeutral;
			return false;
		}

		public static SabatageTypes getActiveSabo()
		{
			foreach (PlayerTask task in CachedPlayer.LocalPlayer.PlayerControl.myTasks.GetFastEnumerator())
			{
				if (task.TaskType == TaskTypes.FixLights)
				{
					return SabatageTypes.Lights;
				}
				else if (task.TaskType == TaskTypes.RestoreOxy)
				{
					return SabatageTypes.O2;
				}
				else if (task.TaskType == TaskTypes.ResetReactor || task.TaskType == TaskTypes.StopCharles || task.TaskType == TaskTypes.StopCharles)
				{
					return SabatageTypes.Reactor;
				}
				else if (task.TaskType == TaskTypes.FixComms)
				{
					return SabatageTypes.Comms;
				}
				else if (SubmergedCompatibility.IsSubmerged && task.TaskType == SubmergedCompatibility.RetrieveOxygenMask)
				{
					return SabatageTypes.OxyMask;
				}
			}
			return SabatageTypes.None;
		}

		public static void resetKill(byte playerId)
		{
			PlayerControl player = playerById(playerId);
			player.killTimer = GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown;
			if (player == Cleaner.cleaner)
				Cleaner.cleaner.killTimer = HudManagerStartPatch.cleanerCleanButton.Timer = HudManagerStartPatch.cleanerCleanButton.MaxTimer;
			else if (player == Warlock.warlock)
				Warlock.warlock.killTimer = HudManagerStartPatch.warlockCurseButton.Timer = HudManagerStartPatch.warlockCurseButton.MaxTimer;
			else if (player == Mini.mini && Mini.mini.Data.Role.IsImpostor)
				Mini.mini.SetKillTimer(GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown * (Mini.isGrownUp() ? 0.66f : 2f));
			else if (player == Witch.witch)
				Witch.witch.killTimer = HudManagerStartPatch.witchSpellButton.Timer = HudManagerStartPatch.witchSpellButton.MaxTimer;
			else if (player == Ninja.ninja)
				Ninja.ninja.killTimer = HudManagerStartPatch.ninjaButton.Timer = HudManagerStartPatch.ninjaButton.MaxTimer;
			else if (player == Sheriff.sheriff)
				Sheriff.sheriff.killTimer = HudManagerStartPatch.sheriffKillButton.Timer = HudManagerStartPatch.sheriffKillButton.MaxTimer;
			else if (player == Vampire.vampire)
				Vampire.vampire.killTimer = HudManagerStartPatch.vampireKillButton.Timer = HudManagerStartPatch.vampireKillButton.MaxTimer;
			else if (player == Jackal.jackal)
				Jackal.jackal.killTimer = HudManagerStartPatch.jackalKillButton.Timer = HudManagerStartPatch.jackalKillButton.MaxTimer;
			else if (player == Sidekick.sidekick)
				Sidekick.sidekick.killTimer = HudManagerStartPatch.sidekickKillButton.Timer = HudManagerStartPatch.sidekickKillButton.MaxTimer;
			else if (player == Swooper.swooper)
				Swooper.swooper.killTimer = HudManagerStartPatch.swooperKillButton.Timer = HudManagerStartPatch.swooperKillButton.MaxTimer;

		}

		public static bool isTeamJackal(PlayerControl player)
		{
			if (Jackal.jackal == player) return true;
			if (Sidekick.sidekick == player) return true;
			return false;
		}

		public static bool isPlayerLover(PlayerControl player)
		{
			return !(player == null) && (player == Lovers.lover1 || player == Lovers.lover2);
		}

		public static bool isSaboActive()
		{
			return (Helpers.getActiveSabo() != SabatageTypes.None);
		}

		public static bool isReactorActive()
		{
			return (Helpers.getActiveSabo() == SabatageTypes.Reactor);
		}

		public static bool isLightsActive()
		{
			return (Helpers.getActiveSabo() == SabatageTypes.Lights);
		}

		public static bool isO2Active()
		{
			return (Helpers.getActiveSabo() == SabatageTypes.O2);
		}

		public static bool isO2MaskActive()
		{
			return (Helpers.getActiveSabo() == SabatageTypes.OxyMask);
		}

		public static bool isCommsActive()
		{
			return (Helpers.getActiveSabo() == SabatageTypes.Comms);
		}


		public static bool isCamoComms()
		{
			return (isCommsActive() && MapOptionsTor.camoComms);
		}


		public static IEnumerator BlackmailShhh()
		{
			//Helpers.showFlash(new Color32(49, 28, 69, byte.MinValue), 3f, "Blackmail", false, 0.75f);
			yield return HudManager.Instance.CoFadeFullScreen(Color.clear, new Color(0f, 0f, 0f, 0.98f));
			var TempPosition = HudManager.Instance.shhhEmblem.transform.localPosition;
			var TempDuration = HudManager.Instance.shhhEmblem.HoldDuration;
			HudManager.Instance.shhhEmblem.transform.localPosition = new Vector3(
				HudManager.Instance.shhhEmblem.transform.localPosition.x,
				HudManager.Instance.shhhEmblem.transform.localPosition.y,
				HudManager.Instance.FullScreen.transform.position.z + 1f);
			HudManager.Instance.shhhEmblem.TextImage.text = ModTranslation.GetString("Text", 43);
			HudManager.Instance.shhhEmblem.HoldDuration = 2.5f;
			yield return HudManager.Instance.ShowEmblem(true);
			HudManager.Instance.shhhEmblem.transform.localPosition = TempPosition;
			HudManager.Instance.shhhEmblem.HoldDuration = TempDuration;
			yield return HudManager.Instance.CoFadeFullScreen(new Color(0f, 0f, 0f, 0.98f), Color.clear);
			yield return null;
		}

		public static void Log(string e)
		{
			TheOtherRolesPlugin.Logger.LogMessage(e);
		}

		public static int getAvailableId()
		{
			var id = 0;
			while (true)
			{
				if (ShipStatus.Instance.AllVents.All(v => v.Id != id)) return id;
				id++;
			}
		}

		public static bool isActiveCamoComms()
		{
			return (isCamoComms() && Camouflager.camoComms);
		}

		public static bool wasActiveCamoComms()
		{
			return (!isCamoComms() && Camouflager.camoComms);
		}

		public static void camoReset()
		{
			Camouflager.resetCamouflage();
			if (Morphling.morphTimer > 0f && Morphling.morphling != null && Morphling.morphTarget != null)
			{
				PlayerControl target = Morphling.morphTarget;
				Morphling.morphling.setLook(target.Data.PlayerName, target.Data.DefaultOutfit.ColorId, target.Data.DefaultOutfit.HatId, target.Data.DefaultOutfit.VisorId, target.Data.DefaultOutfit.SkinId, target.Data.DefaultOutfit.PetId);
			}
		}

		public static bool canAlwaysBeGuessed(RoleId roleId)
		{
			bool guessable = false;
			if (roleId == RoleId.Cursed) guessable = true;
			return guessable;
		}

		public static void turnToCrewmate(PlayerControl player)
		{

			MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.TurnToCrewmate, Hazel.SendOption.Reliable, -1);
			writer.Write(player.PlayerId);
			AmongUsClient.Instance.FinishRpcImmediately(writer);
			RPCProcedure.turnToCrewmate(player.PlayerId);
			foreach (var player2 in PlayerControl.AllPlayerControls)
			{
				if (player2.Data.Role.IsImpostor && CachedPlayer.LocalPlayer.PlayerControl.Data.Role.IsImpostor)
				{
					player.cosmetics.nameText.color = Palette.White;
				}
			}

		}

		public static void turnToCrewmate(List<PlayerControl> players, PlayerControl player)
		{
			foreach (PlayerControl p in players)
			{
				if (p == player) continue;
				turnToCrewmate(p);
			}
		}
		public static void turnToImpostorRPC(PlayerControl player)
		{
			MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.TurnToImpostor, Hazel.SendOption.Reliable, -1);
			writer.Write(player.PlayerId);
			AmongUsClient.Instance.FinishRpcImmediately(writer);
			RPCProcedure.turnToImpostor(player.PlayerId);
		}

		public static void turnToImpostor(PlayerControl player)
		{
			player.Data.Role.TeamType = RoleTeamTypes.Impostor;
			RoleManager.Instance.SetRole(player, RoleTypes.Impostor);
			player.SetKillTimer(GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown);

			System.Console.WriteLine("PROOF I AM IMP VANILLA ROLE: " + player.Data.Role.IsImpostor);

			foreach (var player2 in PlayerControl.AllPlayerControls)
			{
				if (player2.Data.Role.IsImpostor && CachedPlayer.LocalPlayer.PlayerControl.Data.Role.IsImpostor)
				{
					player.cosmetics.nameText.color = Palette.ImpostorRed;
				}
			}
		}

		public static bool ShowButtons =>
			!(MapBehaviour.Instance && MapBehaviour.Instance.IsOpen) &&
			!MeetingHud.Instance &&
			!ExileController.Instance;

		public static void showTargetNameOnButton(PlayerControl target, CustomButton button, string defaultText)
		{
			if (CustomOptionHolder.showButtonTarget.getBool())
			{ // Should the button show the target name option
				var text = "";
				if (Camouflager.camouflageTimer >= 0.1f || isActiveCamoComms()) text = defaultText; // set text to default if camo is on
				else if (Helpers.isLightsActive()) text = defaultText; // set to default if lights are out
				else if (Trickster.trickster != null && Trickster.lightsOutTimer > 0f) text = defaultText; // set to default if trickster ability is active
				else if (Morphling.morphling != null && Morphling.morphTarget != null && target == Morphling.morphling && Morphling.morphTimer > 0) text = Morphling.morphTarget.Data.PlayerName;  // set to morphed player
				else if (target == Swooper.swooper && Swooper.isInvisable) text = defaultText;
				else if (target == null) text = defaultText; // Set text to defaultText if no target
				else text = target.Data.PlayerName; // Set text to playername
				showTargetNameOnButtonExplicit(null, button, text);
			}
		}


		public static void showTargetNameOnButtonExplicit(PlayerControl target, CustomButton button, string defaultText)
		{
			var text = defaultText;
			if (target == null) text = defaultText; // Set text to defaultText if no target
			else text = target.Data.PlayerName; // Set text to playername
			button.actionButton.OverrideText(text);
			button.showButtonText = true;
		}
		public static Sprite loadSpriteFromResources(Texture2D texture, float pixelsPerUnit, Rect textureRect)
		{
			return Sprite.Create(texture, textureRect, new Vector2(0.5f, 0.5f), pixelsPerUnit);
		}
		public static Sprite loadSpriteFromResources(string path, float pixelsPerUnit, bool cache = true)
		{
			try
			{
				if (cache && CachedSprites.TryGetValue(path + pixelsPerUnit, out var sprite)) return sprite;
				Texture2D texture = loadTextureFromResources(path);
				sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
				if (cache) sprite.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;
				if (!cache) return sprite;
				return CachedSprites[path + pixelsPerUnit] = sprite;
			}
			catch
			{
				System.Console.WriteLine("Error loading sprite from path: " + path);
			}
			return null;
		}

		public static unsafe Texture2D loadTextureFromResources(string path)
		{
			try
			{
				Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, true);
				Assembly assembly = Assembly.GetExecutingAssembly();
				Stream stream = assembly.GetManifestResourceStream(path);
				var length = stream.Length;
				var byteTexture = new Il2CppStructArray<byte>(length);
				stream.Read(new Span<byte>(IntPtr.Add(byteTexture.Pointer, IntPtr.Size * 4).ToPointer(), (int)length));
				//if (path.Contains("HorseHats"))
				//{
				//    byteTexture = new Il2CppStructArray<byte>(byteTexture.Reverse().ToArray());
				//}
				ImageConversion.LoadImage(texture, byteTexture, false);
				return texture;
			}
			catch
			{
				TheOtherRolesPlugin.Logger.LogError("Error loading texture from resources: " + path);
			}
			return null;
		}

		public static Texture2D loadTextureFromDisk(string path)
		{
			try
			{
				if (File.Exists(path))
				{
					Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, true);
					var byteTexture = Il2CppSystem.IO.File.ReadAllBytes(path);
					ImageConversion.LoadImage(texture, byteTexture, false);
					return texture;
				}
			}
			catch
			{
				System.Console.WriteLine("Error loading texture from disk: " + path);
			}
			return null;
		}

		public static AudioClip loadAudioClipFromResources(string path, string clipName = "UNNAMED_TOR_AUDIO_CLIP")
		{
			// must be "raw (headerless) 2-channel signed 32 bit pcm (le)" (can e.g. use Audacity?to export)
			try
			{
				Assembly assembly = Assembly.GetExecutingAssembly();
				Stream stream = assembly.GetManifestResourceStream(path);
				var byteAudio = new byte[stream.Length];
				_ = stream.Read(byteAudio, 0, (int)stream.Length);
				float[] samples = new float[byteAudio.Length / 4]; // 4 bytes per sample
				int offset;
				for (int i = 0; i < samples.Length; i++)
				{
					offset = i * 4;
					samples[i] = (float)BitConverter.ToInt32(byteAudio, offset) / Int32.MaxValue;
				}
				int channels = 2;
				int sampleRate = 48000;
				AudioClip audioClip = AudioClip.Create(clipName, samples.Length / 2, channels, sampleRate, false);
				audioClip.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;
				audioClip.SetData(samples, 0);
				return audioClip;
			}
			catch
			{
				System.Console.WriteLine("Error loading AudioClip from resources: " + path);
			}
			return null;

			/* Usage example:
            AudioClip exampleClip = Helpers.loadAudioClipFromResources("TheOtherRoles.Resources.exampleClip.raw");
            if (Constants.ShouldPlaySfx()) SoundManager.Instance.PlaySound(exampleClip, false, 0.8f);
            */
		}
		public static string readTextFromResources(string path)
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			Stream stream = assembly.GetManifestResourceStream(path);
			StreamReader textStreamReader = new StreamReader(stream);
			return textStreamReader.ReadToEnd();
		}
		public static string readTextFromFile(string path)
		{
			Stream stream = File.OpenRead(path);
			StreamReader textStreamReader = new StreamReader(stream);
			return textStreamReader.ReadToEnd();
		}


		public static PlayerControl playerById(byte id)
		{
			foreach (PlayerControl player in CachedPlayer.AllPlayers)
				if (player.PlayerId == id)
					return player;
			return null;
		}

		public static Dictionary<byte, PlayerControl> allPlayersById()
		{
			Dictionary<byte, PlayerControl> res = new Dictionary<byte, PlayerControl>();
			foreach (PlayerControl player in CachedPlayer.AllPlayers)
				res.Add(player.PlayerId, player);
			return res;
		}

		public static void handleVampireBiteOnBodyReport()
		{
			// Murder the bitten player and reset bitten (regardless whether the kill was successful or not)
			Helpers.checkMurderAttemptAndKill(Vampire.vampire, Vampire.bitten, true, false);
			MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.VampireSetBitten, Hazel.SendOption.Reliable, -1);
			writer.Write(byte.MaxValue);
			writer.Write(byte.MaxValue);
			AmongUsClient.Instance.FinishRpcImmediately(writer);
			RPCProcedure.vampireSetBitten(byte.MaxValue, byte.MaxValue);
		}

		public static void handleBomber2ExplodeOnBodyReport()
		{
			// Murder the bitten player and reset bitten (regardless whether the kill was successful or not)
			Helpers.checkMuderAttemptAndKill(Bomber2.bomber, Bomber2.hasBomb, true, false);
			MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.GiveBomb, Hazel.SendOption.Reliable, -1);
			writer.Write(byte.MaxValue);
			AmongUsClient.Instance.FinishRpcImmediately(writer);
			RPCProcedure.giveBomb(byte.MaxValue);
		}

		public static void refreshRoleDescription(PlayerControl player)
		{
			List<RoleInfo> infos = RoleInfo.getRoleInfoForPlayer(player);
			List<string> taskTexts = new(infos.Count);

			foreach (var roleInfo in infos)
			{
				taskTexts.Add(getRoleString(roleInfo));
			}

			var toRemove = new List<PlayerTask>();
			foreach (PlayerTask t in player.myTasks.GetFastEnumerator())
			{
				var textTask = t.TryCast<ImportantTextTask>();
				if (textTask == null) continue;

				var currentText = textTask.Text;

				if (taskTexts.Contains(currentText)) taskTexts.Remove(currentText); // TextTask for this RoleInfo does not have to be added, as it already exists
				else toRemove.Add(t); // TextTask does not have a corresponding RoleInfo and will hence be deleted
			}

			foreach (PlayerTask t in toRemove)
			{
				t.OnRemove();
				player.myTasks.Remove(t);
				UnityEngine.Object.Destroy(t.gameObject);
			}

			// Add TextTask for remaining RoleInfos
			foreach (string title in taskTexts)
			{
				var task = new GameObject("RoleTask").AddComponent<ImportantTextTask>();
				task.transform.SetParent(player.transform, false);
				task.Text = title;
				player.myTasks.Insert(0, task);
			}
		}

		internal static string getRoleString(RoleInfo roleInfo)
		{
            if (RoleInfo.getRoleInfoForPlayer(CachedPlayer.LocalPlayer.PlayerControl).Any(info => info.roleId == RoleId.Jackal))
			{
				var getSidekickText = Jackal.canCreateSidekick ? $" {ModTranslation.GetString("Text", 44)}" : "";
				return cs(roleInfo.color, $"{roleInfo.name}: {ModTranslation.GetString("Text", 45)} {getSidekickText}");
			}

            if (RoleInfo.getRoleInfoForPlayer(CachedPlayer.LocalPlayer.PlayerControl).Any(info => info.roleId == RoleId.Invert))
			{
				return cs(roleInfo.color, $"{roleInfo.name}: {roleInfo.shortDescription} ({Invert.meetings})");
			}

            if (CachedPlayer.LocalPlayer.PlayerControl == Lawyer.lawyer && Lawyer.isProsecutor)
            {
                var lawyerTarget = Lawyer.target != null ? string.Format(ModTranslation.GetString("Text", 4), Lawyer.target?.Data?.PlayerName ?? "") + " " : ModTranslation.GetString("Role-ShortDesc", 3);
                return cs(roleInfo.color, $"{roleInfo.name}: {roleInfo.shortDescription}\n{lawyerTarget}");
            }

            return cs(roleInfo.color, $"{roleInfo.name}: {roleInfo.shortDescription}");
		}


		public static bool isD(byte playerId)
		{
			return playerId % 2 == 0;
		}

		public static bool isLighterColor(PlayerControl target)
		{
			return isD(target.PlayerId);
		}
		public static bool isLighterColor(int colorId)
		{
			return CustomColors.lighterColors.Contains(colorId);
		}

		public static bool isCustomServer()
		{
			if (FastDestroyableSingleton<ServerManager>.Instance == null) return false;
			StringNames n = FastDestroyableSingleton<ServerManager>.Instance.CurrentRegion.TranslateName;
			return n != StringNames.ServerNA && n != StringNames.ServerEU && n != StringNames.ServerAS;
		}

		public static bool isDead(this PlayerControl player)
		{
			return player == null || player?.Data?.IsDead == true || player?.Data?.Disconnected == true;
		}

		public static void setInvisable(PlayerControl player)
		{
			MessageWriter invisibleWriter = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.SetInvisibleGen, Hazel.SendOption.Reliable, -1);
			invisibleWriter.Write(player.PlayerId);
			invisibleWriter.Write(byte.MinValue);
			AmongUsClient.Instance.FinishRpcImmediately(invisibleWriter);
			RPCProcedure.setInvisibleGen(player.PlayerId, byte.MinValue);
		}

		public static bool isAlive(this PlayerControl player)
		{
			return !isDead(player);
		}
		public static bool CanMultipleShots(PlayerControl dyingTarget)
		{
			if (dyingTarget == CachedPlayer.LocalPlayer.PlayerControl)
				return false;

			if (HandleGuesser.isGuesser(CachedPlayer.LocalPlayer.PlayerId)
				&& HandleGuesser.remainingShots(CachedPlayer.LocalPlayer.PlayerId) > 1
				&& HandleGuesser.hasMultipleShotsPerMeeting)
				return true;

			return CachedPlayer.LocalPlayer.PlayerControl == Doomsayer.doomsayer && Doomsayer.hasMultipleShotsPerMeeting &&
				   Doomsayer.CanShoot;
		}
		public static List<RoleInfo> onlineRoleInfos()
		{
			//if (CachedPlayer.AllPlayers.Count < Doomsayer.formationNum + 2) return allRoleInfos();
			var role = new List<RoleInfo>();
			role.AddRange(CachedPlayer.AllPlayers.Select(n => RoleInfo.getRoleInfoForPlayer(n, false)).SelectMany(n => n));
			return role;
		}

		public static bool hasFakeTasks(this PlayerControl player)
		{
			return player == Werewolf.werewolf ||
				   player == Jester.jester ||
				   player == Amnisiac.amnisiac ||
				   player == Swooper.swooper ||
				   player == Jackal.jackal ||
				   player == Sidekick.sidekick ||
				   player == Arsonist.arsonist ||
				   player == Vulture.vulture ||
				   player == Doomsayer.doomsayer ||
				   player == Juggernaut.juggernaut ||
				   player == PlagueDoctor.plagueDoctor ||
				   player == Cupid.cupid ||
				   player == Lawyer.lawyer ||
				   player == Pursuer.pursuer ||

                   Survivor.survivor.Contains(player) ||
				   //Jackal.formerJackals.Any(x => x == player);
				   Jackal.formerJackals.Contains(player);
		}

		public static bool canBeErased(this PlayerControl player)
		{
			return player != Jackal.jackal &&
				player != Sidekick.sidekick &&
				Jackal.formerJackals.All(x => x != player) &&
				player != Swooper.swooper &&
				player != Werewolf.werewolf &&
				player != Juggernaut.juggernaut;
		}
		public static bool shouldShowGhostInfo()
		{
			return CachedPlayer.LocalPlayer.PlayerControl != null && CachedPlayer.LocalPlayer.PlayerControl.Data.IsDead && MapOptionsTor.ghostsSeeInformation || AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Ended;
		}

		public static void clearAllTasks(this PlayerControl player)
		{
			if (player == null) return;
			foreach (var playerTask in player.myTasks.GetFastEnumerator())
			{
				playerTask.OnRemove();
				UnityEngine.Object.Destroy(playerTask.gameObject);
			}
			player.myTasks.Clear();

			if (player.Data != null && player.Data.Tasks != null)
				player.Data.Tasks.Clear();
		}
		public static void MurderPlayer(this PlayerControl player, PlayerControl target)
		{
			player.MurderPlayer(target, MurderResultFlags.Succeeded);
		}

		public static void RpcRepairSystem(this ShipStatus shipStatus, SystemTypes systemType, byte amount)
		{
			shipStatus.RpcUpdateSystem(systemType, amount);
		}

		public static bool isMira()
		{
			return GameOptionsManager.Instance.CurrentGameOptions.MapId == 1;
		}

		public static bool isAirship()
		{
			return GameOptionsManager.Instance.CurrentGameOptions.MapId == 4;
		}
		public static bool isSkeld()
		{
			return GameOptionsManager.Instance.CurrentGameOptions.MapId == 0;
		}
		public static bool isPolus()
		{
			return GameOptionsManager.Instance.CurrentGameOptions.MapId == 2;
		}

		public static bool isFungle()
		{
			return GameOptionsManager.Instance.CurrentGameOptions.MapId == 5;
		}

		public static bool MushroomSabotageActive()
		{
			return CachedPlayer.LocalPlayer.PlayerControl.myTasks.ToArray().Any((x) => x.TaskType == TaskTypes.MushroomMixupSabotage);
		}
		public static bool sabotageActive()
		{
			var sabSystem = ShipStatus.Instance.Systems[SystemTypes.Sabotage].CastFast<SabotageSystemType>();
			return sabSystem.AnyActive;
		}

		public static float sabotageTimer()
		{
			var sabSystem = ShipStatus.Instance.Systems[SystemTypes.Sabotage].CastFast<SabotageSystemType>();
			return sabSystem.Timer;
		}
		public static bool canUseSabotage()
		{
			var sabSystem = ShipStatus.Instance.Systems[SystemTypes.Sabotage].CastFast<SabotageSystemType>();
			ISystemType systemType;
			IActivatable doors = null;
			if (ShipStatus.Instance.Systems.TryGetValue(SystemTypes.Doors, out systemType))
			{
				doors = systemType.CastFast<IActivatable>();
			}
			return GameManager.Instance.SabotagesEnabled() && sabSystem.Timer <= 0f && !sabSystem.AnyActive && !(doors != null && doors.IsActive);
		}
		public static void setSemiTransparent(this PoolablePlayer player, bool value, float alpha = 0.25f)
		{
			alpha = value ? alpha : 1f;
			foreach (SpriteRenderer r in player.gameObject.GetComponentsInChildren<SpriteRenderer>())
				r.color = new Color(r.color.r, r.color.g, r.color.b, alpha);
			player.cosmetics.nameText.color = new Color(player.cosmetics.nameText.color.r, player.cosmetics.nameText.color.g, player.cosmetics.nameText.color.b, alpha);
		}

		public static string GetString(this TranslationController t, StringNames key, params Il2CppSystem.Object[] parts)
		{
			return t.GetString(key, parts);
		}
		public static bool isChinese()
		{
			try
			{
				var name = CultureInfo.CurrentUICulture.Name;
				if (name.StartsWith("zh")) return true;
				return false;
			}
			catch
			{
				return false;
			}
		}
		public static string cs(Color c, string s)
		{
			return string.Format("<color=#{0:X2}{1:X2}{2:X2}{3:X2}>{4}</color>", ToByte(c.r), ToByte(c.g), ToByte(c.b), ToByte(c.a), s);
		}
		public static int lineCount(string text)
		{
			return text.Count(c => c == '\n');
		}

		private static byte ToByte(float f)
		{
			f = Mathf.Clamp01(f);
			return (byte)(f * 255);
		}

		public static KeyValuePair<byte, int> MaxPair(this Dictionary<byte, int> self, out bool tie)
		{
			tie = true;
			KeyValuePair<byte, int> result = new KeyValuePair<byte, int>(byte.MaxValue, int.MinValue);
			foreach (KeyValuePair<byte, int> keyValuePair in self)
			{
				if (keyValuePair.Value > result.Value)
				{
					result = keyValuePair;
					tie = false;
				}
				else if (keyValuePair.Value == result.Value)
				{
					tie = true;
				}
			}
			return result;
		}

		public static bool hidePlayerName(PlayerControl source, PlayerControl target)
		{
			if (Camouflager.camouflageTimer > 0f || Helpers.MushroomSabotageActive()) return true; // No names are visible
			if (isActiveCamoComms()) return true;
			if (SurveillanceMinigamePatch.nightVisionIsActive) return true;
			if (Ninja.isInvisble && Ninja.ninja == target) return true;
			if (Swooper.isInvisable && Swooper.swooper == target) return true;
			if (!MapOptionsTor.hidePlayerNames || source.Data.IsDead) return false; // All names are visible
			if (source == null || target == null) return true;
			if (source == target) return false; // Player sees his own name
			if (source.Data.Role.IsImpostor && (target.Data.Role.IsImpostor || target == Spy.spy || target == Sidekick.sidekick && Sidekick.wasTeamRed || target == Jackal.jackal && Jackal.wasTeamRed)) return false; // Members of team Impostors see the names of Impostors/Spies
			if ((source == Lovers.lover1 || source == Lovers.lover2) && (target == Lovers.lover1 || target == Lovers.lover2)) return false; // Members of team Lovers see the names of each other
			if ((source == Jackal.jackal || source == Sidekick.sidekick) && (target == Jackal.jackal || target == Sidekick.sidekick || target == Jackal.fakeSidekick)) return false; // Members of team Jackal see the names of each other
			if ((source == Lawyer.lawyer) && (target == Lawyer.target) && Lawyer.isProsecutor) return false; // Prosecutor can always see target name
			return !Deputy.knowsSheriff || (source != Sheriff.sheriff && source != Deputy.deputy) || (target != Sheriff.sheriff && target != Deputy.deputy); // Sheriff & Deputy see the names of each other
		}

		public static void setDefaultLook(this PlayerControl target, bool enforceNightVisionUpdate = true)
		{
			if (Helpers.MushroomSabotageActive())
			{
				var instance = ShipStatus.Instance.CastFast<FungleShipStatus>().specialSabotage;
				MushroomMixupSabotageSystem.CondensedOutfit condensedOutfit = instance.currentMixups[target.PlayerId];
				NetworkedPlayerInfo.PlayerOutfit playerOutfit = instance.ConvertToPlayerOutfit(condensedOutfit);
				target.MixUpOutfit(playerOutfit);
			}
			else
				target.setLook(target.Data.PlayerName, target.Data.DefaultOutfit.ColorId, target.Data.DefaultOutfit.HatId, target.Data.DefaultOutfit.VisorId, target.Data.DefaultOutfit.SkinId, target.Data.DefaultOutfit.PetId, enforceNightVisionUpdate);
		}

		public static void setLook(this PlayerControl target, String playerName, int colorId, string hatId, string visorId, string skinId, string petId, bool enforceNightVisionUpdate = true)
		{
			target.RawSetColor(colorId);
			target.RawSetVisor(visorId, colorId);
			target.RawSetHat(hatId, colorId);
			target.RawSetName(hidePlayerName(CachedPlayer.LocalPlayer.PlayerControl, target) ? "" : playerName);

			SkinViewData nextSkin = null;
			try { nextSkin = ShipStatus.Instance.CosmeticsCache.GetSkin(skinId); } catch { return; };

			PlayerPhysics playerPhysics = target.MyPhysics;
			AnimationClip clip = null;
			var spriteAnim = playerPhysics.myPlayer.cosmetics.skin.animator;
			var currentPhysicsAnim = playerPhysics.Animations.Animator.GetCurrentAnimation();


			if (currentPhysicsAnim == playerPhysics.Animations.group.RunAnim) clip = nextSkin.RunAnim;
			else if (currentPhysicsAnim == playerPhysics.Animations.group.SpawnAnim) clip = nextSkin.SpawnAnim;
			else if (currentPhysicsAnim == playerPhysics.Animations.group.EnterVentAnim) clip = nextSkin.EnterVentAnim;
			else if (currentPhysicsAnim == playerPhysics.Animations.group.ExitVentAnim) clip = nextSkin.ExitVentAnim;
			else if (currentPhysicsAnim == playerPhysics.Animations.group.IdleAnim) clip = nextSkin.IdleAnim;
			else clip = nextSkin.IdleAnim;
			float progress = playerPhysics.Animations.Animator.m_animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
			playerPhysics.myPlayer.cosmetics.skin.skin = nextSkin;
			playerPhysics.myPlayer.cosmetics.skin.UpdateMaterial();
			spriteAnim.Play(clip, 1f);
			spriteAnim.m_animator.Play("a", 0, progress % 1);
			spriteAnim.m_animator.Update(0f);

			target.RawSetPet(petId, colorId);

			if (enforceNightVisionUpdate) Patches.SurveillanceMinigamePatch.enforceNightVision(target);
			Chameleon.update();  // so that morphling and camo wont make the chameleons visible
		}

		public static void showFlash(Color color, float duration = 1f, string message = "", bool fade = true, float opacity = 100f)
		{
			if (FastDestroyableSingleton<HudManager>.Instance == null || FastDestroyableSingleton<HudManager>.Instance.FullScreen == null) return;
			FastDestroyableSingleton<HudManager>.Instance.FullScreen.gameObject.SetActive(true);
			FastDestroyableSingleton<HudManager>.Instance.FullScreen.enabled = true;
			// Message Text
			TMPro.TextMeshPro messageText = GameObject.Instantiate(FastDestroyableSingleton<HudManager>.Instance.KillButton.cooldownTimerText, FastDestroyableSingleton<HudManager>.Instance.transform);
			messageText.text = message;
			messageText.enableWordWrapping = false;
			messageText.transform.localScale = Vector3.one * 0.5f;
			messageText.transform.localPosition += new Vector3(0f, 2f, -69f);
			messageText.gameObject.SetActive(true);
			FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(duration, new Action<float>((p) =>
			{
				var renderer = FastDestroyableSingleton<HudManager>.Instance.FullScreen;
				if (!fade)
				{
					if (renderer != null)
						renderer.color = new Color(color.r, color.g, color.b, opacity);
				}
				else
				{
					if (p < 0.5)
					{
						if (renderer != null)
							renderer.color = new Color(color.r, color.g, color.b, Mathf.Clamp01(p * 2 * 0.75f));
					}
					else
					{
						if (renderer != null)
							renderer.color = new Color(color.r, color.g, color.b, Mathf.Clamp01((1 - p) * 2 * 0.75f));
					}
				}
				if (p == 1f && renderer != null) renderer.enabled = false;
				if (p == 1f) messageText.gameObject.Destroy();
			})));
		}

		public static void checkWatchFlash(PlayerControl target)
		{
			if (CachedPlayer.LocalPlayer.PlayerControl == PrivateInvestigator.watching)
			{
				MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.PrivateInvestigatorWatchFlash, Hazel.SendOption.Reliable, -1);
				writer.Write(target.PlayerId);
				AmongUsClient.Instance.FinishRpcImmediately(writer);
				RPCProcedure.privateInvestigatorWatchFlash(target.PlayerId);
			}
		}

		public static bool roleCanUseVents(this PlayerControl player)
		{
			bool roleCouldUse = false;
			if (Engineer.engineer != null && Engineer.engineer == player)
				roleCouldUse = true;
			else if (Swooper.swooper != null && Swooper.swooper == player)
				roleCouldUse = true;
			else if (Werewolf.werewolf != null && Werewolf.werewolf == player)
				roleCouldUse = true;
			else if (Jackal.canUseVents && Jackal.jackal != null && Jackal.jackal == player)
				roleCouldUse = true;
			else if (Sidekick.canUseVents && Sidekick.sidekick != null && Sidekick.sidekick == player)
				roleCouldUse = true;
			else if (Spy.canEnterVents && Spy.spy != null && Spy.spy == player)
				roleCouldUse = true;
			else if (Vulture.canUseVents && Vulture.vulture != null && Vulture.vulture == player)
				roleCouldUse = true;
			else if (Undertaker.deadBodyDraged != null && !Undertaker.canDragAndVent && Undertaker.undertaker == player)
				roleCouldUse = false;
			else if (Thief.canUseVents && Thief.thief != null && Thief.thief == player)
				roleCouldUse = true;
			else if (Juggernaut.juggernaut != null && Juggernaut.juggernaut == player && Juggernaut.canVent)
				roleCouldUse = true;
			else if (player.Data?.Role != null && player.Data.Role.CanVent)
			{
				if (Janitor.janitor != null && Janitor.janitor == CachedPlayer.LocalPlayer.PlayerControl)
					roleCouldUse = false;
				else if (Mafioso.mafioso != null && Mafioso.mafioso == CachedPlayer.LocalPlayer.PlayerControl &&
						 Godfather.godfather != null && !Godfather.godfather.Data.IsDead)
					roleCouldUse = false;
				else
					roleCouldUse = true;
			}
			else if (Jester.jester != null && Jester.jester == player && Jester.canVent)
				roleCouldUse = true;

			else if (Tunneler.tunneler != null && Tunneler.tunneler == player)
			{
				var (playerCompleted, playerTotal) = TasksHandler.taskInfo(Tunneler.tunneler.Data);
				int numberOfTasks = playerTotal - playerCompleted;
				if (numberOfTasks == 0) roleCouldUse = true;
			}
			return roleCouldUse;
		}

		public static MurderAttemptResult checkMuderAttempt(PlayerControl killer, PlayerControl target, bool blockRewind = false, bool ignoreBlank = false, bool ignoreIfKillerIsDead = false, bool ignoreMedic = false)
		{
			var targetRole = RoleInfo.getRoleInfoForPlayer(target, false).FirstOrDefault();

			// Modified vanilla checks
			if (AmongUsClient.Instance.IsGameOver) return MurderAttemptResult.SuppressKill;
			if (killer == null || killer.Data == null || (killer.Data.IsDead && !ignoreIfKillerIsDead) || killer.Data.Disconnected) return MurderAttemptResult.SuppressKill; // Allow non Impostor kills compared to vanilla code
			if (target == null || target.Data == null || target.Data.IsDead || target.Data.Disconnected) return MurderAttemptResult.SuppressKill; // Allow killing players in vents compared to vanilla code
			if (GameOptionsManager.Instance.currentGameOptions.GameMode == GameModes.HideNSeek || PropHunt.isPropHuntGM) return MurderAttemptResult.PerformKill;

			// Handle first kill attempt
			if (MapOptionsTor.shieldFirstKill && MapOptionsTor.firstKillPlayer == target) return MurderAttemptResult.SuppressKill;

			// Handle blank shot
			if (!ignoreBlank && Pursuer.blankedList.Any(x => x.PlayerId == killer.PlayerId))
			{
				MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.SetBlanked, Hazel.SendOption.Reliable, -1);
				writer.Write(killer.PlayerId);
				writer.Write((byte)0);
				AmongUsClient.Instance.FinishRpcImmediately(writer);
				RPCProcedure.setBlanked(killer.PlayerId, 0);

				return MurderAttemptResult.BlankKill;
			}


			// Kill the killer if the Veteran is on alert
			else if (Veteran.veteran != null && target == Veteran.veteran && Veteran.alertActive)
			{
				if (!ignoreMedic && Medic.shielded != null && Medic.shielded == target)
				{
					MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)CustomRPC.ShieldedMurderAttempt, Hazel.SendOption.Reliable, -1);
					writer.Write(target.PlayerId);
					AmongUsClient.Instance.FinishRpcImmediately(writer);
					RPCProcedure.shieldedMurderAttempt(killer.PlayerId);
				}
				return MurderAttemptResult.ReverseKill;
			}            // Kill the killer if the Veteran is on alert

			// Kill the Body Guard and the killer if the target is guarded
			else if (BodyGuard.bodyguard != null && target == BodyGuard.guarded && isAlive(BodyGuard.bodyguard))
			{
				if (Medic.shielded != null && Medic.shielded == target)
				{
					MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)CustomRPC.ShieldedMurderAttempt, Hazel.SendOption.Reliable, -1);
					writer.Write(target.PlayerId);
					AmongUsClient.Instance.FinishRpcImmediately(writer);
					RPCProcedure.shieldedMurderAttempt(killer.PlayerId);
				}
				return MurderAttemptResult.BodyGuardKill;
			}

			// Block impostor shielded kill
			if (Medic.shielded != null && Medic.shielded == target)
			{
				MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)CustomRPC.ShieldedMurderAttempt, Hazel.SendOption.Reliable, -1);
				writer.Write(killer.PlayerId);
				AmongUsClient.Instance.FinishRpcImmediately(writer);
				RPCProcedure.shieldedMurderAttempt(killer.PlayerId);
				if (MapOptionsTor.enableSoundEffects) SoundManager.Instance.PlaySound(CustomMain.customZips.fail, false, 1f);
				return MurderAttemptResult.SuppressKill;
			}

			// Block impostor not fully grown mini kill
			else if (Mini.mini != null && target == Mini.mini && !Mini.isGrownUp())
			{
				return MurderAttemptResult.SuppressKill;
			}

			// Block Time Master with time shield kill
			else if (TimeMaster.shieldActive && TimeMaster.timeMaster != null && TimeMaster.timeMaster == target)
			{
				if (!blockRewind)
				{ // Only rewind the attempt was not called because a meeting startet 
					MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)CustomRPC.TimeMasterRewindTime, Hazel.SendOption.Reliable, -1);
					AmongUsClient.Instance.FinishRpcImmediately(writer);
					RPCProcedure.timeMasterRewindTime();
				}
				MessageWriter write = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.SetBlanked, Hazel.SendOption.Reliable, -1);
				write.Write(killer.PlayerId);
				write.Write((byte)0);
				AmongUsClient.Instance.FinishRpcImmediately(write);
				RPCProcedure.setBlanked(killer.PlayerId, 0);

				return MurderAttemptResult.BlankKill;
			}

			else if (Cupid.cupid != null && Cupid.shielded == target && !Cupid.cupid.Data.IsDead)
			{
				MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.CupidSuicide, Hazel.SendOption.Reliable, -1);
				writer.Write(Cupid.cupid.PlayerId);
				writer.Write(true);
				AmongUsClient.Instance.FinishRpcImmediately(writer);
				RPCProcedure.cupidSuicide(Cupid.cupid.PlayerId, true);
				return MurderAttemptResult.BlankKill;
			}

			else if (Survivor.survivor != null && Survivor.survivor.Contains(target) && Survivor.vestActive)
			{
				CustomButton.resetKillButton(killer, Survivor.vestResetCooldown);
				SoundManager.Instance.PlaySound(CustomMain.customZips.fail, false, 0.8f);
				return MurderAttemptResult.SuppressKill;
			}

			else if (Cursed.cursed != null && Cursed.cursed == target && killer.Data.Role.IsImpostor)
			{
				MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.SetBlanked, Hazel.SendOption.Reliable, -1);
				writer.Write(killer.PlayerId);
				writer.Write((byte)0);
				AmongUsClient.Instance.FinishRpcImmediately(writer);
				RPCProcedure.setBlanked(killer.PlayerId, 0);

				turnToImpostorRPC(target);

				return MurderAttemptResult.BlankKill;
			}

			// Thief if hit crew only kill if setting says so, but also kill the thief.
			else if (Thief.isFailedThiefKill(target, killer, targetRole))
			{
				Thief.suicideFlag = true;
				return MurderAttemptResult.SuppressKill;
			}

			// Block hunted with time shield kill
			else if (Hunted.timeshieldActive.Contains(target.PlayerId))
			{
				MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)CustomRPC.HuntedRewindTime, Hazel.SendOption.Reliable, -1);
				writer.Write(target.PlayerId);
				AmongUsClient.Instance.FinishRpcImmediately(writer);
				RPCProcedure.huntedRewindTime(target.PlayerId);

				return MurderAttemptResult.SuppressKill;
			}
			if (TransportationToolPatches.isUsingTransportation(target) && !blockRewind && killer == Vampire.vampire)
			{
				return MurderAttemptResult.DelayVampireKill;
			}
			else if (TransportationToolPatches.isUsingTransportation(target))
				return MurderAttemptResult.SuppressKill;

			return MurderAttemptResult.PerformKill;
		}

		public static MurderAttemptResult checkMuderAttemptAndKill(PlayerControl killer, PlayerControl target, bool isMeetingStart = false, bool showAnimation = true)
		{
			return checkMurderAttemptAndKill(killer, target, isMeetingStart, showAnimation);
		}
		public static void MurderPlayer(PlayerControl killer, PlayerControl target, bool showAnimation)
		{
			MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.UncheckedMurderPlayer, Hazel.SendOption.Reliable, -1);
			writer.Write(killer.PlayerId);
			writer.Write(target.PlayerId);
			writer.Write(showAnimation ? Byte.MaxValue : 0);
			AmongUsClient.Instance.FinishRpcImmediately(writer);
			RPCProcedure.uncheckedMurderPlayer(killer.PlayerId, target.PlayerId, showAnimation ? Byte.MaxValue : (byte)0);
		}

		public static MurderAttemptResult checkMurderAttemptAndKill(PlayerControl killer, PlayerControl target, bool isMeetingStart = false, bool showAnimation = true, bool ignoreBlank = false, bool ignoreIfKillerIsDead = false)
		{
			// The local player checks for the validity of the kill and performs it afterwards (different to vanilla, where the host performs all the checks)
			// The kill attempt will be shared using a custom RPC, hence combining modded and unmodded versions is impossible

			MurderAttemptResult murder = checkMuderAttempt(killer, target, isMeetingStart, ignoreBlank, ignoreIfKillerIsDead);

			if (murder == MurderAttemptResult.PerformKill)
			{
				if (killer == Poucher.poucher) Poucher.killed.Add(target);

				if (Mimic.mimic != null && killer == Mimic.mimic && !Mimic.hasMimic)
				{
					MessageWriter writerMimic = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.MimicMimicRole, Hazel.SendOption.Reliable, -1);
					writerMimic.Write(target.PlayerId);
					AmongUsClient.Instance.FinishRpcImmediately(writerMimic);
					RPCProcedure.mimicMimicRole(target.PlayerId);
				}
				MurderPlayer(killer, target, showAnimation);
			}
			else if (murder == MurderAttemptResult.DelayVampireKill)
			{
				HudManager.Instance.StartCoroutine(Effects.Lerp(10f, new Action<float>((p) =>
				{
					if (!TransportationToolPatches.isUsingTransportation(target) && Vampire.bitten != null)
					{
						MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.VampireSetBitten, Hazel.SendOption.Reliable, -1);
						writer.Write(byte.MaxValue);
						writer.Write(byte.MaxValue);
						AmongUsClient.Instance.FinishRpcImmediately(writer);
						RPCProcedure.vampireSetBitten(byte.MaxValue, byte.MaxValue);
						MurderPlayer(killer, target, showAnimation);
					}
				})));
			}

			if (murder == MurderAttemptResult.BodyGuardKill)
			{
				// Kill the Killer
				MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.UncheckedMurderPlayer, Hazel.SendOption.Reliable, -1);
				writer.Write(killer.PlayerId);
				writer.Write(killer.PlayerId);
				writer.Write(showAnimation ? Byte.MaxValue : 0);
				AmongUsClient.Instance.FinishRpcImmediately(writer);
				RPCProcedure.uncheckedMurderPlayer(BodyGuard.bodyguard.PlayerId, killer.PlayerId, (byte)0);

				// Kill the BodyGuard
				MessageWriter writer2 = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.UncheckedMurderPlayer, Hazel.SendOption.Reliable, -1);
				writer2.Write(BodyGuard.bodyguard.PlayerId);
				writer2.Write(BodyGuard.bodyguard.PlayerId);
				writer2.Write(showAnimation ? Byte.MaxValue : 0);
				AmongUsClient.Instance.FinishRpcImmediately(writer2);
				RPCProcedure.uncheckedMurderPlayer(BodyGuard.bodyguard.PlayerId, BodyGuard.bodyguard.PlayerId, (byte)0);


				MessageWriter writer3 = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.ShowBodyGuardFlash, Hazel.SendOption.Reliable, -1);
				AmongUsClient.Instance.FinishRpcImmediately(writer3);
				RPCProcedure.showBodyGuardFlash();
			}

			if (murder == MurderAttemptResult.ReverseKill)
			{
				checkMuderAttemptAndKill(target, killer, isMeetingStart);
			}

			return murder;
		}


		public static bool checkAndDoVetKill(PlayerControl target)
		{
			bool shouldVetKill = (Veteran.veteran == target && Veteran.alertActive);
			if (shouldVetKill)
			{
				MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.VeteranKill, Hazel.SendOption.Reliable, -1);
				writer.Write(CachedPlayer.LocalPlayer.PlayerControl.PlayerId);
				AmongUsClient.Instance.FinishRpcImmediately(writer);
				RPCProcedure.veteranKill(CachedPlayer.LocalPlayer.PlayerControl.PlayerId);
			}
			return shouldVetKill;
		}

		public static void shareGameVersion()
		{
			MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.VersionHandshake, Hazel.SendOption.Reliable, -1);
			writer.Write((byte)TheOtherRolesPlugin.Version.Major);
			writer.Write((byte)TheOtherRolesPlugin.Version.Minor);
			writer.Write((byte)TheOtherRolesPlugin.Version.Build);
			writer.Write(AmongUsClient.Instance.AmHost ? Patches.GameStartManagerPatch.timer : -1f);
			writer.WritePacked(AmongUsClient.Instance.ClientId);
			writer.Write((byte)(TheOtherRolesPlugin.Version.Revision < 0 ? 0xFF : TheOtherRolesPlugin.Version.Revision));
			writer.Write(Assembly.GetExecutingAssembly().ManifestModule.ModuleVersionId.ToByteArray());
			AmongUsClient.Instance.FinishRpcImmediately(writer);
			RPCProcedure.versionHandshake(TheOtherRolesPlugin.Version.Major, TheOtherRolesPlugin.Version.Minor, TheOtherRolesPlugin.Version.Build, TheOtherRolesPlugin.Version.Revision, Assembly.GetExecutingAssembly().ManifestModule.ModuleVersionId, AmongUsClient.Instance.ClientId);
		}

		public static List<PlayerControl> getKillerTeamMembers(PlayerControl player)
		{
			List<PlayerControl> team = new List<PlayerControl>();
			foreach (PlayerControl p in CachedPlayer.AllPlayers)
			{
				if (player.Data.Role.IsImpostor && p.Data.Role.IsImpostor && player.PlayerId != p.PlayerId && team.All(x => x.PlayerId != p.PlayerId)) team.Add(p);
				else if (player == Jackal.jackal && p == Sidekick.sidekick) team.Add(p);
				else if (player == Sidekick.sidekick && p == Jackal.jackal) team.Add(p);
			}

			return team;
		}

		public static bool isKiller(PlayerControl player)
		{
			return player.Data.Role.IsImpostor ||
				(isNeutral(player) &&
				player != Jester.jester &&
				player != Arsonist.arsonist &&
				player != Vulture.vulture &&
				player != Lawyer.lawyer &&
				player != PlagueDoctor.plagueDoctor &&
				player != Pursuer.pursuer &&
				player != Cupid.cupid);

		}

		public static bool isEvil(PlayerControl player)
		{
			return player.Data.Role.IsImpostor ||
				   isNeutral(player);
		}

		public static bool zoomOutStatus = false;
		public static void toggleZoom(bool reset = false)
		{
			float orthographicSize = reset || zoomOutStatus ? 3f : 12f;

			zoomOutStatus = !zoomOutStatus && !reset;
			Camera.main.orthographicSize = orthographicSize;
			foreach (var cam in Camera.allCameras)
			{
				if (cam != null && cam.gameObject.name == "UI Camera") cam.orthographicSize = orthographicSize;  // The UI is scaled too, else we cant click the buttons. Downside: map is super small.
			}

			var tzGO = GameObject.Find("TOGGLEZOOMBUTTON");
			if (tzGO != null)
			{
				var rend = tzGO.transform.Find("Inactive").GetComponent<SpriteRenderer>();
				var rendActive = tzGO.transform.Find("Active").GetComponent<SpriteRenderer>();
				rend.sprite = zoomOutStatus ? Helpers.loadSpriteFromResources("TheOtherRoles.Resources.Plus_Button.png", 100f) : Helpers.loadSpriteFromResources("TheOtherRoles.Resources.Minus_Button.png", 100f);
				rendActive.sprite = zoomOutStatus ? Helpers.loadSpriteFromResources("TheOtherRoles.Resources.Plus_ButtonActive.png", 100f) : Helpers.loadSpriteFromResources("TheOtherRoles.Resources.Minus_ButtonActive.png", 100f);
				tzGO.transform.localScale = new Vector3(1.2f, 1.2f, 1f) * (zoomOutStatus ? 4 : 1);
			}
			ResolutionManager.ResolutionChanged.Invoke((float)Screen.width / Screen.height, Screen.width, Screen.height, Screen.fullScreen); // This will move button positions to the correct position.
		}
		private static long GetBuiltInTicks()
		{
			var assembly = Assembly.GetExecutingAssembly();
			var builtin = assembly.GetType("Builtin");
			if (builtin == null) return 0;
			var field = builtin.GetField("CompileTime");
			if (field == null) return 0;
			var value = field.GetValue(null);
			if (value == null) return 0;
			return (long)value;
		}

		public static async Task checkBeta()
		{
			if (TheOtherRolesPlugin.betaDays > 0)
			{
				TheOtherRolesPlugin.Logger.LogMessage($"Beta check");
				var ticks = GetBuiltInTicks();
				var compileTime = new DateTime(ticks, DateTimeKind.Utc);  // This may show as an error, but it is not, compilation will work!
				TheOtherRolesPlugin.Logger.LogMessage($"Compiled at {compileTime.ToString(CultureInfo.InvariantCulture)}");
				DateTime? now;
				// Get time from the internet, so no-one can cheat it (so easily).
				try
				{
					var client = new System.Net.Http.HttpClient();
					using var response = await client.GetAsync("http://www.google.com/");
					if (response.IsSuccessStatusCode)
						now = response.Headers.Date?.UtcDateTime;
					else
					{
						TheOtherRolesPlugin.Logger.LogMessage($"Could not get time from server: {response.StatusCode}");
						now = DateTime.UtcNow; //In case something goes wrong. 
					}
				}
				catch (System.Net.Http.HttpRequestException)
				{
					now = DateTime.UtcNow;
				}
				if ((now - compileTime)?.TotalDays > TheOtherRolesPlugin.betaDays)
				{
					TheOtherRolesPlugin.Logger.LogMessage($"Beta expired!");
					BepInExUpdater.MessageBoxTimeout(BepInExUpdater.GetForegroundWindow(), "BETA is expired. You cannot play this version anymore.", "The Other Roles Beta", 0, 0, 10000);
					Application.Quit();

				}
				else TheOtherRolesPlugin.Logger.LogMessage($"Beta will remain runnable for {TheOtherRolesPlugin.betaDays - (now - compileTime)?.TotalDays} days!");
			}
		}

		public static bool hasImpVision(NetworkedPlayerInfo player)
		{
			return player.Role.IsImpostor
				|| ((Jackal.jackal != null && Jackal.jackal.PlayerId == player.PlayerId || Jackal.formerJackals.Any(x => x.PlayerId == player.PlayerId)) && Jackal.hasImpostorVision)
				|| (Sidekick.sidekick != null && Sidekick.sidekick.PlayerId == player.PlayerId && Sidekick.hasImpostorVision)
				|| (Spy.spy != null && Spy.spy.PlayerId == player.PlayerId && Spy.hasImpostorVision)
				|| (Jester.jester != null && Jester.jester.PlayerId == player.PlayerId && Jester.hasImpostorVision)
				|| (Thief.thief != null && Thief.thief.PlayerId == player.PlayerId && Thief.hasImpostorVision)
				|| (Werewolf.werewolf != null && Werewolf.werewolf.PlayerId == player.PlayerId)
				|| (Juggernaut.juggernaut != null && Juggernaut.juggernaut.PlayerId == player.PlayerId)
				|| (Swooper.swooper != null && Swooper.swooper.PlayerId == player.PlayerId && Swooper.hasImpVision);

		}

		public static object TryCast(this Il2CppObjectBase self, Type type)
		{
			return AccessTools.Method(self.GetType(), nameof(Il2CppObjectBase.TryCast)).MakeGenericMethod(type).Invoke(self, Array.Empty<object>());
		}
		public static List<RoleInfo> allRoleInfos()
		{
			var allRoleInfo = new List<RoleInfo>();
			foreach (var role in RoleInfo.allRoleInfos)
			{
				if (role.isModifier) continue;
				allRoleInfo.Add(role);
			}
			return allRoleInfo;
		}
		public interface Image
		{
			internal UnityEngine.Sprite GetSprite();
		}

		//Use to Del Resctor.dll
		/*public static void Destroy(this UnityEngine.Object obj)
		{
			UnityEngine.Object.Destroy(obj);
		}*/
		public static GameObject CreateObject(string objName, Transform parent, Vector3 localPosition, int? layer = null)
		{
			var obj = new GameObject(objName);
			obj.transform.SetParent(parent);
			obj.transform.localPosition = localPosition;
			obj.transform.localScale = new Vector3(1f, 1f, 1f);
			if (layer.HasValue) obj.layer = layer.Value;
			else if (parent != null) obj.layer = parent.gameObject.layer;
			return obj;
		}
		public static T CreateObject<T>(string objName, Transform parent, Vector3 localPosition, int? layer = null) where T : Component
		{
			return CreateObject(objName, parent, localPosition, layer).AddComponent<T>();
		}
		public interface ITextureLoader
		{
			Texture2D GetTexture();
		}
		public static Sprite ToSprite(this Texture2D texture, Rect rect, float pixelsPerUnit)
		{
			return Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), pixelsPerUnit);
		}
		public static Sprite ToSprite(this Texture2D texture, float pixelsPerUnit) => ToSprite(texture, new Rect(0, 0, texture.width, texture.height), pixelsPerUnit);
		public class ResourceTextureLoader : ITextureLoader
		{
			string address;
			Texture2D texture = null;

			public ResourceTextureLoader(string address)
			{
				this.address = address;
			}

			public Texture2D GetTexture()
			{
				if (!texture) texture = Helpers.loadTextureFromResources(address);
				return texture!;
			}
		}
		public static void ForEach<T>(this Il2CppArrayBase<T> list, Action<T> func)
		{
			foreach (T obj in list) func(obj);
		}
		public static Color getTeamColor(RoleTeam team)
		{
			return team switch
			{
				RoleTeam.Crewmate => Palette.CrewmateBlue,
				RoleTeam.Impostor => Palette.ImpostorRed,
				RoleTeam.Neutral => Color.gray,
				RoleTeam.Modifier => Color.yellow,
				_ => Color.white
			};
		}
		public class SpriteLoader : Image
		{
			Sprite sprite = null!;
			float pixelsPerUnit;
			ITextureLoader textureLoader;
			public SpriteLoader(ITextureLoader textureLoader, float pixelsPerUnit)
			{
				this.textureLoader = textureLoader;
				this.pixelsPerUnit = pixelsPerUnit;
			}

			public Sprite GetSprite()
			{
				if (!sprite) sprite = textureLoader.GetTexture().ToSprite(pixelsPerUnit);
				sprite.hideFlags = textureLoader.GetTexture().hideFlags;
				return sprite;
			}
		}
		public static DeadBody[] AllDeadBodies()
		{
			var bodies = GameObject.FindGameObjectsWithTag("DeadBody");
			DeadBody[] deadBodies = new DeadBody[bodies.Count];
			for (int i = 0; i < bodies.Count; i++) if (bodies[i].gameObject.active) deadBodies[i] = bodies[i].GetComponent<DeadBody>();
			return deadBodies;
		}

		public static string RandomString(int length)
		{
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
			return new string(Enumerable.Repeat(chars, length)
				.Select(s => s[random.Next(s.Length)]).ToArray());
		}
		public static readonly System.Random random = new System.Random((int)DateTime.Now.Ticks);

		public static float getKillerTimerMultiplier()
		{
			float multiplier = 1f;
			return multiplier * getTimerMultiplier();
		}

		public static float getTimerMultiplier()
		{
			float multiplier = 1f;
			return multiplier;
		}

		public static TMPro.TextMeshPro getFirst(this TMPro.TextMeshPro[] text)
		{
			if (text == null) return null;
			foreach (var self in text)
				if (self.text == "") return self;
			return text[0];
		}

		private static string ColorToHex(Color color)
		{
			Color32 color32 = (Color32)color;
			return $"{color32.r:X2}{color32.g:X2}{color32.b:X2}{color32.a:X2}";
		}

		public static Color HexToColor(string hex)
		{
			Color color = new();
			ColorUtility.TryParseHtmlString("#" + hex, out color);
			return color;
		}
		public static void SetActive(this TextMeshPro tf, bool b) => tf.gameObject.SetActive(b);

		public static string GradientColorText(string startColorHex, string endColorHex, string text)
		{
			if (startColorHex.Length != 6 || endColorHex.Length != 6)
			{
				TheOtherRolesPlugin.Logger.LogError("GradientColorText : Invalid Color Hex Code, Hex code should be 6 characters long (without #) (e.g., FFFFFF).");
				return text;
			}

			Color startColor = HexToColor(startColorHex);
			Color endColor = HexToColor(endColorHex);

			int textLength = text.Length;
			float stepR = (endColor.r - startColor.r) / (float)textLength;
			float stepG = (endColor.g - startColor.g) / (float)textLength;
			float stepB = (endColor.b - startColor.b) / (float)textLength;
			float stepA = (endColor.a - startColor.a) / (float)textLength;

			string gradientText = "";

			for (int i = 0; i < textLength; i++)
			{
				float r = startColor.r + (stepR * i);
				float g = startColor.g + (stepG * i);
				float b = startColor.b + (stepB * i);
				float a = startColor.a + (stepA * i);


				string colorhex = ColorToHex(new Color(r, g, b, a));
				gradientText += $"<color=#{colorhex}>{text[i]}</color>";

			}
			return gradientText;
		}
        public static class Coroutines
        {
            [RegisterInIl2Cpp]
            internal sealed class Component : MonoBehaviour
            {
                internal static Component Instance { get; set; }

                public Component(IntPtr ptr) : base(ptr)
                {
                }

                private void Awake()
                {
                    Instance = this;
                }

                [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "OnDestroy is an unity event and can't be static")]
                private void OnDestroy()
                {
                    Instance = null;
                }
            }

            private static readonly ConditionalWeakTable<IEnumerator, Coroutine> _ourCoroutineStore = new();

            [return: NotNullIfNotNull("coroutine")]
            public static IEnumerator Start(IEnumerator coroutine)
            {
                if (coroutine != null)
                {
                    _ourCoroutineStore.AddOrUpdate(coroutine, Component.Instance!.StartCoroutine(coroutine));
                }

                return coroutine;
            }
            public static void Stop(IEnumerator coroutine)
            {
                if (coroutine != null && _ourCoroutineStore.TryGetValue(coroutine, out var routine))
                {
                    Component.Instance!.StopCoroutine(routine);
                }
            }
        }
    }
}