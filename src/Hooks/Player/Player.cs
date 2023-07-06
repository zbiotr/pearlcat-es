﻿using RWCustom;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static AbstractPhysicalObject;
using static DataPearl.AbstractDataPearl;

namespace Pearlcat;

public static partial class Hooks
{
    public static void ApplyPlayerHooks()
    {
        On.Player.Update += Player_Update;
        On.Player.checkInput += Player_checkInput;

        On.Player.GrabUpdate += Player_GrabUpdate;
        On.Player.Grabability += Player_Grabability;

        On.Player.Die += Player_Die;
        On.Creature.SuckedIntoShortCut += Creature_SuckedIntoShortCut;
    }
    

    private static void Player_checkInput(On.Player.orig_checkInput orig, Player self)
    {
        orig(self);
        
        if (!self.TryGetPearlcatModule(out var playerModule)) return;
        
        var input = self.input[0];
        playerModule.UnblockedInput = input;

        if (playerModule.BlockInput)
        {
            input.x = 0;
            input.y = 0;
            input.analogueDir *= 0f;

            input.jmp = false;
            input.thrw = false;
            input.pckp = false;
        }

        self.input[0] = input;
    }

    private static void Player_Die(On.Player.orig_Die orig, Player self)
    {
        orig(self);

        if (!self.TryGetPearlcatModule(out var playerModule)) return;


        for (int i = playerModule.Inventory.Count - 1; i >= 0; i--)
        {
            AbstractPhysicalObject abstractObject = playerModule.Inventory[i];

            DeathEffect(abstractObject.realizedObject);
            RemoveFromInventory(self, abstractObject);

            playerModule.PostDeathInventory.Add(abstractObject);
        }
    }

    private static void Player_GrabUpdate(On.Player.orig_GrabUpdate orig, Player self, bool eu)
    {
        orig(self, eu);

        //StoreObjectUpdate(self);

        //TransferObjectUpdate(self);
    }

    private static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
    {
        var result = orig(self, obj);

        if (obj.abstractPhysicalObject.IsPlayerObject())
            return Player.ObjectGrabability.CantGrab;

        return result;
    }

    private static void Creature_SuckedIntoShortCut(On.Creature.orig_SuckedIntoShortCut orig, Creature self, IntVector2 entrancePos, bool carriedByOther)
    {
        if (self is Player player && player.TryGetPearlcatModule(out _))
            player.AbstractizeInventory();

        orig(self, entrancePos, carriedByOther);
    }



    public static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);

        if (!self.TryGetPearlcatModule(out var playerModule)) return;
        
        playerModule.BaseStats = self.Malnourished ? playerModule.MalnourishedStats : playerModule.NormalStats;

        var unblockedInput = playerModule.UnblockedInput;

        bool swapLeftInput = self.IsSwapLeftInput();
        bool swapRightInput = self.IsSwapRightInput();

        bool swapInput = self.IsSwapKeybindPressed();
        bool storeInput = self.IsStoreKeybindPressed(playerModule);
        bool abilityInput = self.IsAbilityKeybindPressed(playerModule);

        int numPressed = self.IsFirstPearlcat() ? self.GetNumberPressed() : -1;

        playerModule.BlockInput = false;

        if (numPressed >= 0)
            self.ActivateObjectInStorage(numPressed - 1);

        // Should probably clean this up sometime
        if (SwapRepeatInterval.TryGet(self, out var swapInterval))
        {
            // || playerModule.swapIntervalStacker > swapInterval
            if (Mathf.Abs(unblockedInput.x) <= 0.5f)
            {
                playerModule.WasSwapped = false;
                playerModule.SwapIntervalTimer = 0;
            }

            if (swapInput)
            {
                playerModule.BlockInput = true;

                if (playerModule.SwapIntervalTimer <= swapInterval)
                    playerModule.SwapIntervalTimer++;
            }
            else
            {
                playerModule.SwapIntervalTimer = 0;
            }
        }

        if (swapLeftInput && !playerModule.WasSwapLeftInput)
        {
            self.SelectPreviousObject();
        }
        else if (swapRightInput && !playerModule.WasSwapRightInput)
        {
            self.SelectNextObject();
        }
        else if (swapInput && !playerModule.WasSwapped)
        {
            if (unblockedInput.x < -0.5f)
            {
                self.SelectPreviousObject();
                playerModule.WasSwapped = true;
            }
            else if (unblockedInput.x > 0.5f)
            {
                self.SelectNextObject();
                playerModule.WasSwapped = true;
            }
        }

        UpdateAll(self, playerModule);

        playerModule.WasSwapLeftInput = swapLeftInput;
        playerModule.WasSwapRightInput = swapRightInput;
        playerModule.WasStoreInput = storeInput;
        playerModule.WasAbilityInput = abilityInput;
    }

    private static void UpdateAll(Player self, PlayerModule playerModule)
    {
        if (self.onBack != null)
            self.AbstractizeInventory();

        // Warp Fix
        if (self.room != null && JustWarpedData.TryGetValue(self.room.game, out var justWarped) && justWarped.Value)
        {
            justWarped.Value = false;
            playerModule.LoadSaveData(self);

            Plugin.Logger.LogWarning("WARP LOADED");
            Plugin.Logger.LogWarning(playerModule.Inventory.Count);
        }

        self.TryRealizeInventory();

        UpdatePlayerOA(self, playerModule);
        UpdatePlayerDaze(self, playerModule);
        UpdatePostDeathInventory(self, playerModule);

        UpdateCombinedPOEffect(self, playerModule);
        ApplyCombinedPOEffect(self, playerModule);

        UpdateHUD(self, playerModule);
        UpdateSFX(self, playerModule);

        UpdateStoreRetrieveObject(self, playerModule);
    }

    private static void UpdateStoreRetrieveObject(Player self, PlayerModule playerModule)
    {

        if (!StoreObjectDelay.TryGet(self, out var storeObjectDelay)) return;

        var storeInput = self.IsStoreKeybindPressed(playerModule);
        var toStore = self.grasps[0]?.grabbed;
        var isStoring = self.grasps[0]?.grabbed.abstractPhysicalObject.IsStorable() ?? false;

        if (isStoring && toStore == null) return;

        if (!isStoring && self.FreeHand() == -1) return;

        if (!isStoring && playerModule.ActiveObject == null) return;


        if (playerModule.StoreObjectTimer > storeObjectDelay)
        {
            if (isStoring && toStore != null)
            {
                self.room.PlaySound(Enums.Sounds.Pearlcat_PearlStore, toStore.abstractPhysicalObject.realizedObject.firstChunk);
                self.ReleaseGrasp(0);
                self.StoreObject(toStore.abstractPhysicalObject);
            }
            else if (playerModule.ActiveObject != null)
            {
                self.room.PlaySound(Enums.Sounds.Pearlcat_PearlRetrieve, playerModule.ActiveObject.realizedObject.firstChunk);
                self.RetrieveActiveObject();
            }

            playerModule.StoreObjectTimer = -1;
        }


        if (storeInput)
        {
            if (playerModule.StoreObjectTimer >= 0)
            {
                playerModule.BlockInput = true;
                playerModule.StoreObjectTimer++;

                self.Blink(5);

                //var pGraphics = (PlayerGraphics)self.graphicsModule;
                //pGraphics.hands[self.FreeHand()].absoluteHuntPos = self.firstChunk.pos + new Vector2(50.0f, 0.0f);

                // every 5 frames
                if (playerModule.StoreObjectTimer % 5 == 0)
                {
                    if (isStoring)
                    {
                        var activeObjPos = self.GetActiveObjectPos();
                        toStore?.ConnectEffect(activeObjPos);                
                    }
                    else
                    {
                        var activeObj = playerModule.ActiveObject?.realizedObject;
                        activeObj.ConnectEffect(self.firstChunk.pos);
                    }
                }
            }
        }
        else
        {
            playerModule.StoreObjectTimer = 0;
        }
    }

    private static void UpdateSFX(Player self, PlayerModule playerModule)
    {
        playerModule.MenuCrackleLoop.Update();
        playerModule.MenuCrackleLoop.Volume = playerModule.HudFade;
    }

    private static void UpdateHUD(Player self, PlayerModule playerModule)
    {
        if (playerModule.HudFadeTimer > 0)
        {
            playerModule.HudFadeTimer--;
            playerModule.HudFade = Mathf.Lerp(playerModule.HudFade, 1.0f, 0.1f);
        }
        else
        {
            playerModule.HudFadeTimer = 0;
            playerModule.HudFade = Mathf.Lerp(playerModule.HudFade, 0.0f, 0.05f);
        }
    }

    private static void UpdatePostDeathInventory(Player self, PlayerModule playerModule)
    {
        if (!self.dead && playerModule.PostDeathInventory.Count > 0)
        {
            for (int i = playerModule.PostDeathInventory.Count - 1; i >= 0; i--)
            {
                AbstractPhysicalObject? item = playerModule.PostDeathInventory[i];
                playerModule.PostDeathInventory.RemoveAt(i);

                if (item.realizedObject == null) continue;

                if (item.realizedObject.room != self.room) continue;

                if (item.realizedObject.grabbedBy.Count > 0) continue;


                if (ObjectAddon.ObjectsWithAddon.TryGetValue(item, out var _))
                    ObjectAddon.ObjectsWithAddon.Remove(item);

                self.StoreObject(item);
            }
        }
    }

    private static void UpdatePlayerDaze(Player self, PlayerModule playerModule)
    {
        if (!DazeDuration.TryGet(self, out var dazeDuration)) return;

        if (self.dead || self.bodyMode == Player.BodyModeIndex.Stunned || self.Sleeping)
            playerModule.DazeTimer = dazeDuration;

        if (playerModule.DazeTimer > 0)
            playerModule.DazeTimer--;
    }

    private static void UpdatePlayerOA(Player self, PlayerModule playerModule)
    {
        if (self.bodyMode == Player.BodyModeIndex.Stunned || self.bodyMode == Player.BodyModeIndex.Dead)
        {
            playerModule.CurrentObjectAnimation = new FreeFallOA(self);
        }
        else if (self.Sleeping || self.sleepCurlUp > 0.0f)
        {
            playerModule.CurrentObjectAnimation = new SleepOA(self);
        }
        else if (playerModule.CurrentObjectAnimation is SleepOA or FreeFallOA)
        {
            foreach (var abstractObject in playerModule.Inventory)
                abstractObject.realizedObject.ConnectEffect(((PlayerGraphics)self.graphicsModule).head.pos);

            playerModule.PickObjectAnimation(self);
        }

        if (playerModule.ObjectAnimationTimer > playerModule.ObjectAnimationDuration)
            playerModule.PickObjectAnimation(self);

        playerModule.CurrentObjectAnimation?.Update(self);
        playerModule.ObjectAnimationTimer++;


        if (self.room == null) return;

        // HACK
        var save = self.room.game.GetMiscWorld();

        if (save.IsNewGame && !playerModule.GivenPearls)
        {
            playerModule.GivenPearls = true;

            for (int i = 0; i < 6; i++)
            {
                var types = new List<DataPearlType>()
                {
                    MoreSlugcats.MoreSlugcatsEnums.DataPearlType.RM,
                    Enums.Pearls.AS_PearlBlack,
                    Enums.Pearls.AS_PearlGreen,
                    Enums.Pearls.AS_PearlYellow,
                    Enums.Pearls.AS_PearlRed,
                    Enums.Pearls.AS_PearlBlue,
                    DataPearlType.LF_bottom,
                    DataPearlType.SL_chimney,
                    DataPearlType.SL_bridge,
                    DataPearlType.HI,
                    DataPearlType.Misc,
                };

                var type = i switch
                {
                    0 => types[0],
                    1 => types[1],
                    2 => types[2],
                    3 => types[3],
                    4 => types[4],
                    5 => types[5],
                    6 => types[6],
                    7 => types[7],
                    8 => types[8],
                    9 => types[9],
                    10 => types[10],
                    _ => types[Random.Range(0, types.Count)],
                };

                var pearl = new DataPearl.AbstractDataPearl(self.room.world, AbstractObjectType.DataPearl, null, self.abstractPhysicalObject.pos, self.room.game.GetNewID(), -1, -1, null, type);
                self.StoreObject(pearl);
            }
        }
    }



    // Revivify moment
    public static void Revive(this Player self)
    {
        self.stun = 20;
        self.airInLungs = 0.1f;
        self.exhausted = true;
        self.aerobicLevel = 1;
         
        self.playerState.alive = true;
        self.playerState.permaDead = false;
        self.dead = false;
        self.killTag = null;
        self.killTagCounter = 0;
        self.abstractCreature.abstractAI?.SetDestination(self.abstractCreature.pos);

        if (!self.TryGetPearlcatModule(out var playerModule)) return;

        playerModule.PickObjectAnimation(self);
    }

    public static int GraspsHasType(this Player self, AbstractPhysicalObject.AbstractObjectType type)
    {
        for (int i = 0; i < self.grasps.Length; i++)
        {
            Creature.Grasp? grasp = self.grasps[i];
            
            if (grasp == null) continue;

            if (grasp.grabbed.abstractPhysicalObject.type == type)
                return i;
        }

        return -1;
    }
}
