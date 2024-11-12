using System.Linq;
using MoreSlugcats;

namespace Pearlcat;

public static partial class Utils
{
    public static bool IsPearlcatStory(this RainWorldGame? game)
    {
        return game?.StoryCharacter == Enums.Pearlcat;
    }
    public static bool IsSingleplayer(this Player player)
    {
        return player.abstractCreature.world.game.Players.Count == 1;
    }


    // Pearl ID
    public static bool IsHeartPearl(this AbstractPhysicalObject? obj) => obj is DataPearl.AbstractDataPearl dataPearl && dataPearl.IsHeartPearl();
    public static bool IsHeartPearl(this DataPearl dataPearl) => dataPearl.AbstractPearl.IsHeartPearl();
    public static bool IsHeartPearl(this DataPearl.AbstractDataPearl dataPearl) => dataPearl.dataPearlType == Enums.Pearls.Heart_Pearlpup;

    public static bool IsHalcyonPearl(this DataPearl dataPearl) => dataPearl.AbstractPearl.IsHalcyonPearl();
    public static bool IsHalcyonPearl(this DataPearl.AbstractDataPearl dataPearl) => dataPearl.dataPearlType == Enums.Pearls.RM_Pearlcat || dataPearl.dataPearlType == MoreSlugcatsEnums.DataPearlType.RM;


    // Oracle ID
    public static bool IsPebbles(this SSOracleBehavior behavior) => behavior?.oracle?.IsPebbles() ?? false;
    public static bool IsMoon(this SLOracleBehavior behavior) => behavior?.oracle?.IsMoon() ?? false;

    public static bool IsPebbles(this Oracle oracle) => oracle?.ID == Oracle.OracleID.SS;
    public static bool IsMoon(this Oracle oracle) => oracle?.ID == Oracle.OracleID.SL;


    // Room Shortcut Management
    public static void LockAndHideShortcuts(this Room room)
    {
        room.LockShortcuts();
        room.HideShortcuts();
    }
    public static void UnlockAndShowShortcuts(this Room room)
    {
        room.UnlockShortcuts();
        room.ShowShortcuts();

        room.game.cameras.First().hud.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom);
    }


    public static void LockShortcuts(this Room room)
    {
        foreach (var shortcut in room.shortcutsIndex)
            if (!room.lockedShortcuts.Contains(shortcut))
                room.lockedShortcuts.Add(shortcut);
    }
    public static void UnlockShortcuts(this Room room)
    {
        room.lockedShortcuts.Clear();
    }


    public static void HideShortcuts(this Room room)
    {
        var rCam = room.game.cameras.First();

        if (rCam.room != room) return;

        var shortcutGraphics = rCam.shortcutGraphics;

        for (int i = 0; i < room.shortcuts.Length; i++)
            if (shortcutGraphics.entranceSprites.Length > i && shortcutGraphics.entranceSprites[i, 0] != null)
                shortcutGraphics.entranceSprites[i, 0].isVisible = false;
    }
    public static void ShowShortcuts(this Room room)
    {
        var rCam = room.game.cameras.First();

        if (rCam.room != room) return;

        var shortcutGraphics = rCam.shortcutGraphics;

        for (int i = 0; i < room.shortcuts.Length; i++)
            if (shortcutGraphics.entranceSprites[i, 0] != null)
                shortcutGraphics.entranceSprites[i, 0].isVisible = true;
    }


    // Misc
    public static void TryDream(this StoryGameSession storyGame, DreamsState.DreamID dreamId, bool isRecurringDream = false)
    {
        var miscWorld = storyGame.saveState.miscWorldSaveData.GetMiscWorld();

        if (miscWorld == null) return;

        var strId = dreamId.value;

        if (miscWorld.PreviousDreams.Contains(strId) && !isRecurringDream) return;

        miscWorld.CurrentDream = strId;
        SlugBase.Assets.CustomDreams.QueueDream(storyGame, dreamId);
    }

    public static int GetFirstPearlcatIndex(this RainWorldGame? game)
    {
        if (game == null)
        {
            return -1;
        }

        for (int i = 0; i < game.Players.Count; i++)
        {
            AbstractCreature? abstractCreature = game.Players[i];
            if (abstractCreature.realizedCreature is not Player player) continue;

            if (player.IsPearlcat())
                return i;
        }

        return -1;
    }

    public static void AddTextPrompt(this RainWorldGame game, string text, int wait, int time, bool darken = false, bool? hideHud = null)
    {
        hideHud ??= ModManager.MMF;

        game.cameras.First().hud.textPrompt.AddMessage(Translator.Translate(text), wait, time, darken, (bool)hideHud);
    }


    // Save Presets
    public static void GiveTrueEnding(this SaveState saveState)
    {
        if (saveState.saveStateNumber != Enums.Pearlcat) return;

        var miscProg = GetMiscProgression();
        var miscWorld = saveState.miscWorldSaveData.GetMiscWorld();

        if (miscWorld == null) return;


        miscProg.HasTrueEnding = true;
        miscProg.IsPearlpupSick = false;

        miscWorld.PebblesMeetCount = 0;

        SlugBase.Assets.CustomScene.SetSelectMenuScene(saveState, Enums.Scenes.Slugcat_Pearlcat);

        // So the tutorial scripts can be added again
        foreach (var regionState in saveState.regionStates)
        {
            regionState?.roomsVisited?.RemoveAll(x => x?.StartsWith("T1_") == true);
        }
    }

    public static void StartFromMira(this SaveState saveState)
    {
        if (saveState.saveStateNumber != Enums.Pearlcat) return;

        var miscProg = GetMiscProgression();
        var miscWorld = saveState.miscWorldSaveData.GetMiscWorld();
        var baseMiscWorld = saveState.miscWorldSaveData;

        if (miscWorld == null) return;


        miscProg.IsPearlpupSick = true;
        miscProg.HasOEEnding = true;
        miscProg.DidHavePearlpup = true;

        miscWorld.ShownFullInventoryTutorial = true;
        miscWorld.ShownSpearCreationTutorial = true;

        miscWorld.PebblesMeetCount = 3;
        miscWorld.MoonSickPupMeetCount = 1;
        miscWorld.PebblesMetSickPup = true;


        baseMiscWorld.SLOracleState.playerEncountersWithMark = 0;
        baseMiscWorld.SLOracleState.playerEncounters = 1;

        miscWorld.JustMiraSkipped = true;

        SlugBase.Assets.CustomScene.SetSelectMenuScene(saveState, Enums.Scenes.Slugcat_Pearlcat_Sick);
    }
}