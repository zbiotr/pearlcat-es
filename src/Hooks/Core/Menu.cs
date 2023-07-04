﻿using Menu;
using RWCustom;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
using static DataPearl.AbstractDataPearl;

namespace Pearlcat;

public static partial class Hooks
{
    public static void ApplyMenuHooks()
    {
        On.Menu.SlugcatSelectMenu.SlugcatPage.ctor += SlugcatPage_ctor;

        On.Menu.MenuScene.ctor += MenuScene_ctor;
        On.Menu.MenuScene.Update += MenuScene_Update;

        On.Menu.SlugcatSelectMenu.Update += SlugcatSelectMenu_Update;

        On.HUD.HUD.InitSinglePlayerHud += HUD_InitSinglePlayerHud;
    }

    private static void HUD_InitSinglePlayerHud(On.HUD.HUD.orig_InitSinglePlayerHud orig, HUD.HUD self, RoomCamera cam)
    {
        orig(self, cam);

        self.AddPart(new InventoryHUD(self, self.fContainers[1]));
    }


    public static readonly ConditionalWeakTable<MenuScene, MenuSceneModule> MenuSceneData = new();

    public static readonly ConditionalWeakTable<MenuDepthIllustration, MenuPearlModule> MenuIllustrationData = new();

    private static void SlugcatPage_ctor(On.Menu.SlugcatSelectMenu.SlugcatPage.orig_ctor orig, SlugcatSelectMenu.SlugcatPage self, Menu.Menu menu, MenuObject owner, int pageIndex, SlugcatStats.Name slugcatNumber)
    {
        orig(self, menu, owner, pageIndex, slugcatNumber);

        if (slugcatNumber != Enums.General.Pearlcat) return;

        self.effectColor = ItemSymbol.ColorForItem(AbstractPhysicalObject.AbstractObjectType.DataPearl, DataPearlType.HI.index);
    }

    public static void MenuScene_Update(On.Menu.MenuScene.orig_Update orig, MenuScene self)
    {
        orig(self);

        foreach (var illustration in self.depthIllustrations)
        {
            if (!MenuSceneData.TryGetValue(self, out var menuSceneModule)) continue;

            if (!MenuIllustrationData.TryGetValue(illustration, out var pearlModule)) continue;

            if (pearlModule.Index == -1)
            {
                if (menuSceneModule.ActivePearlType == null)
                {
                    illustration.visible = false;
                    continue;
                }
               
                var activePearlColor = DataPearl.UniquePearlMainColor(menuSceneModule.ActivePearlType);
                
                illustration.visible = true;
                illustration.color = MenuPearlColorFilter(activePearlColor);
                illustration.sprite.scale = 0.3f;


                var pos = illustration.pos;
                var spritePos = illustration.sprite.GetPosition();
                var mousePos = self.menu.mousePosition;
                var mouseVel = (self.menu.mousePosition - self.menu.lastMousePos).magnitude;
                // Custom.LerpMap(mouseVel, 0.0f, 100.0f, 1.0f, 6.0f);

                if (Custom.Dist(spritePos, mousePos) < 30.0f && Custom.Dist(pos, pearlModule.InitialPos) < 120.0f)
                    pearlModule.Vel += (spritePos - mousePos).normalized * 2.0f;


                var dir = (pearlModule.InitialPos - pos).normalized;
                var dist = Custom.Dist(pearlModule.InitialPos, pos);
                var speed = Custom.LerpMap(dist, 0.0f, 5.0f, 0.1f, 1.0f);

                pearlModule.Vel *= Custom.LerpMap(pearlModule.Vel.magnitude, 2.0f, 0.5f, 0.97f, 0.5f);
                pearlModule.Vel += dir * speed;

                illustration.pos += pearlModule.Vel;
                continue;
            }

            var pearlTypes = menuSceneModule.PearlTypes;

            var count = pearlTypes.Count;
            var i = pearlModule.Index;

            if (i >= pearlTypes.Count)
            {
                illustration.visible = false;
                continue;
            }

            illustration.visible = true;

            var angleFrameAddition = 0.00075f;
            var radius = 120.0f;
            var origin = new Vector2(680, 400);

            var angle = (i * Mathf.PI * 2.0f / count) + angleFrameAddition * MenuPearlAnimStacker;

            Vector2 targetPos = new(origin.x + Mathf.Cos(angle) * radius, origin.y + Mathf.Sin(angle) * radius * 1.25f);
            illustration.pos = targetPos;

            illustration.sprite.scale = Custom.LerpMap(Mathf.Cos(angle), 0.0f, 1.0f, 0.2f, 0.35f);
            illustration.color = MenuPearlColorFilter(DataPearl.UniquePearlMainColor(pearlTypes[i]));
        }

        MenuPearlAnimStacker++;
    }

    public static Color MenuPearlColorFilter(Color color) => color; 

    public static int MenuPearlAnimStacker = 0;

    public static void MenuScene_ctor(On.Menu.MenuScene.orig_ctor orig, MenuScene self, Menu.Menu menu, MenuObject owner, MenuScene.SceneID sceneID)
    {
        orig(self, menu, owner, sceneID);

        if (sceneID.value != "Slugcat_Pearlcat") return;


        var save = menu.manager.rainWorld.GetMiscProgression();

        if (save.IsNewSave)
        {
            List <DataPearlType> types = new()
            {
                Enums.Pearls.AS_PearlBlue,
                Enums.Pearls.AS_PearlYellow,
                Enums.Pearls.AS_PearlRed,
                Enums.Pearls.AS_PearlGreen,
                Enums.Pearls.AS_PearlBlack,
            };

            MenuSceneData.Add(self, new(types, MoreSlugcats.MoreSlugcatsEnums.DataPearlType.RM));
        }
        else
        {
            MenuSceneData.Add(self, new(save.StoredPearlTypes, save.ActivePearlType));
        }

        MenuPearlAnimStacker = 0;
        
        foreach (var illustration in self.depthIllustrations)
        {
            var fileName = Path.GetFileNameWithoutExtension(illustration.fileName);

            if (fileName.Contains("pearl"))
            {
                var indexString = fileName.Replace("pearl", "");

                if (!int.TryParse(indexString, out var index))
                {
                    if (fileName == "pearlactive")
                        index = -1;

                    else
                        continue;
                }

                MenuIllustrationData.Add(illustration, new(illustration, index));
            }
        }
    }

    public static void SlugcatSelectMenu_Update(On.Menu.SlugcatSelectMenu.orig_Update orig, SlugcatSelectMenu self)
    {
        orig(self);

        //Music.MusicPlayer musicPlayer = self.manager.musicPlayer;

        //if (self.slugcatPages[self.slugcatPageIndex].slugcatNumber.ToString() == Plugin.SLUGCAT_ID)
        //{
        //    musicPlayer.RequestHalcyonSong("NA_19 - Halcyon Memories");

        //    MoreSlugcats.HalcyonSong? halcyonSong = null;
        //    if (musicPlayer.song is MoreSlugcats.HalcyonSong song && musicPlayer.song != null) halcyonSong = song;
        //    if (musicPlayer.nextSong is MoreSlugcats.HalcyonSong nextSong && musicPlayer.nextSong != null) halcyonSong = nextSong;

        //    if (halcyonSong != null)
        //    {
        //        halcyonSong.volume = 0.5f;
        //        halcyonSong.baseVolume = 0.5f;
        //        halcyonSong.setVolume = 0.5f;
        //        halcyonSong.droneVolume = 0.5f;
        //    }
        //}
        //else
        //{
        //    musicPlayer.RequestIntroRollMusic();

        //    Music.IntroRollMusic? introRollSong = null;
        //    if (musicPlayer.song is Music.IntroRollMusic song && musicPlayer.song != null) introRollSong = song;
        //    if (musicPlayer.nextSong is Music.IntroRollMusic nextSong && musicPlayer.nextSong != null) introRollSong = nextSong;

        //    if (introRollSong != null)
        //    {
        //        introRollSong.StartPlaying();
        //        introRollSong.StartMusic();
        //    }
        //}
    }
}