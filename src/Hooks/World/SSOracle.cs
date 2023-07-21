﻿using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using static Conversation;
using static SSOracleBehavior;
using Random = UnityEngine.Random;
using Action = SSOracleBehavior.Action;
using static AbstractPhysicalObject;
using UnityEngine;
using MonoMod.RuntimeDetour;
using System.Reflection;
using MoreSlugcats;

namespace Pearlcat;

// the base was referenced from Dronemaster: https://github.com/HarvieSorroway/TheDroneMaster
public static partial class Hooks
{
    public static void ApplySSOracleHooks()
    {
        On.SSOracleBehavior.SeePlayer += SSOracleBehavior_SeePlayer;
        On.SSOracleBehavior.NewAction += SSOracleBehavior_NewAction;

        On.SSOracleBehavior.PebblesConversation.AddEvents += PebblesConversation_AddEvents;

        On.SSOracleBehavior.UpdateStoryPearlCollection += SSOracleBehavior_UpdateStoryPearlCollection;
        On.PebblesPearl.Update += PebblesPearl_Update;

        try
        {
            IL.SSOracleBehavior.Update += SSOracleBehavior_Update;
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError("Oracle Hooks Error:\n" + e);
        }

        new Hook(
            typeof(PebblesPearl).GetProperty(nameof(PebblesPearl.NotCarried), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
            typeof(Hooks).GetMethod(nameof(GetPebblesPearlNotCarried), BindingFlags.Static | BindingFlags.Public)
        );
    }

    public delegate bool orig_PebblesPearlNotCarried(PebblesPearl self);
    public static bool GetPebblesPearlNotCarried(orig_PebblesPearlNotCarried orig, PebblesPearl self)
    {
        var result = orig(self);

        if (self.room.game.IsPearlcatStory())
            return false;

        return result;
    }

    private static void PebblesPearl_Update(On.PebblesPearl.orig_Update orig, PebblesPearl self, bool eu)
    {
        orig(self, eu);

        if (self.oracle == null) return;

        if (self.room.gravity == 1.0f) return;

        if (!self.oracle.room.game.IsPearlcatStory()) return;

        if (self.grabbedBy.Count > 0) return;

        var origin = new Vector2(225.0f, 570.0f);
        var targetPos = origin + Vector2.down * 12.5f * self.marbleIndex;

        if (Custom.Dist(self.firstChunk.pos, targetPos) > 10.0f)
            self.AbstractedEffect();
    
        self.firstChunk.HardSetPosition(targetPos);
    }

    private static void SSOracleBehavior_Update(ILContext il)
    {
        var c = new ILCursor(il);

        c.GotoNext(MoveType.After,
            x => x.MatchLdstr("Yes, help yourself. They are not edible."));

        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate<Func<string, SSOracleBehavior, string>>((origText, self) =>
        {
            if (self.oracle.room.game.IsPearlcatStory())
                return self.Translate("...oh? Take them, the data they contain is worthless to me. I suppose they'd be far more useful to you...");

            return origText;
        });
    }


    private static void SSOracleBehavior_UpdateStoryPearlCollection(On.SSOracleBehavior.orig_UpdateStoryPearlCollection orig, SSOracleBehavior self)
    {
        if (self.oracle.room.game.IsPearlcatStory()) return;

        orig(self); // HACK
    }

    public static ConditionalWeakTable<SSOracleBehavior, SSOracleModule> SSOracleData { get; } = new();
    public static SSOracleModule GetModule(this SSOracleBehavior oracle) => SSOracleData.GetValue(oracle, x => new SSOracleModule());

    private static void PebblesConversation_AddEvents(On.SSOracleBehavior.PebblesConversation.orig_AddEvents orig, PebblesConversation self)
    {
        if (!self.owner.oracle.room.game.IsPearlcatStory())
        {
            orig(self);
            return;
        }

        var module = self.owner.GetModule();
        var l = self.owner.rainWorld.inGameTranslator.currentLanguage == InGameTranslator.LanguageID.Chinese ? 1 : 0;

        var id = self.id;
        var e = self.events;

        var rand = module.Rand;

        if (id == Enums.SSOracle.Pearlcat_SSConvoFirstMeet)
        {
            e.Add(new WaitEvent(self, 160));

            e.Add(new TextEvent(self, 0,
                self.Translate(".  .  ."), 0));

            e.Add(new TextEvent(self, 0,
                self.Translate("And just who might you be?"), l * 80));

            e.Add(new TextEvent(self, 0,
                self.Translate("My overseers have made no peace over your arrival - I was under the impression the transit system was mostly inoperable."), l * 80));

            e.Add(new WaitEvent(self, 80));
            

            e.Add(new TextEvent(self, 0,
                self.Translate("You come from quite a distance? You can communicate with us, but the mark you possess is foreign to me..."), l * 80));

            e.Add(new TextEvent(self, 0,
                self.Translate("...you can manipulate our storage medium in unforseen ways..."), l * 80));

            e.Add(new WaitEvent(self, 40));


            e.Add(new TextEvent(self, 0,
                self.Translate("Clearly an artifical creation. But from where? Sent by who?"), l * 80));

            e.Add(new TextEvent(self, 0,
                self.Translate("And why... to me?"), l * 80));


            e.Add(new WaitEvent(self, 80));

            e.Add(new TextEvent(self, 0,
                self.Translate("I do not know what to make of this."), l * 80));
        }

        else if (id == Enums.SSOracle.Pearlcat_SSConvoFirstLeave)
        {
            e.Add(new WaitEvent(self, 120));

            e.Add(new TextEvent(self, 0,
                self.Translate(".  .  ."), 0));

            e.Add(new TextEvent(self, 0,
                self.Translate("You have a knack for finding these pearls, yes?"), l * 80));

            e.Add(new TextEvent(self, 0,
                self.Translate("I suppose, seeing as you cannot read the information stored on them, I can read them for you, if you wish."), l * 80));

            e.Add(new TextEvent(self, 0,
                self.Translate("Think of this mutually, I can 'see' a little further outside this can..."), l * 80));


            e.Add(new TextEvent(self, 0,
                self.Translate("...and you can gather more of that data you so clearly desire."), l * 80));


            e.Add(new WaitEvent(self, 120));

            e.Add(new TextEvent(self, 0,
                self.Translate("I must resume my work. I will be waiting here, as always."), l * 80));

            e.Add(new WaitEvent(self, 40));
        }
        
        else if (id == Enums.SSOracle.Pearlcat_SSConvoRMPearlInspect)
        {
            module.TakeRMTimer = 120;
            module.GiveSSTimer = 60;

            e.Add(new WaitEvent(self, 120));

            e.Add(new TextEvent(self, 0,
                self.Translate("...that pearl you carry, the purple one, where did you find it?"), l * 80));

            e.Add(new TextEvent(self, 0,
                self.Translate(".  .  ."), 0));

            e.Add(new WaitEvent(self, 80));



            e.Add(new TextEvent(self, 0,
                self.Translate("It appears to contain a hymn that once meant a lot to the inhabitants of my city..."), l * 80));

            e.Add(new TextEvent(self, 0,
                self.Translate("...one which still means a lot to me."), l * 80));


            e.Add(new TextEvent(self, 0,
                self.Translate("I have tried to reconstruct it from memory many times, fruitlessly."), l * 80));

            e.Add(new WaitEvent(self, 80));



            e.Add(new TextEvent(self, 0,
                self.Translate("And now here you are, with a perfect copy."), l * 80));

            e.Add(new WaitEvent(self, 80));


            e.Add(new TextEvent(self, 0,
                self.Translate("I suppose the only thing you value in these pearls is how pure their lattice is? I will substitute you with one much more refined than this."), l * 80));

            e.Add(new TextEvent(self, 0,
                self.Translate("I assure you it will be a more than suitable replacement for your primitive needs."), l * 80));

            e.Add(new WaitEvent(self, 80));
        }

        else if (id == Enums.SSOracle.Pearlcat_SSConvoTakeRMPearl)
        {
            e.Add(new WaitEvent(self, 200));

            e.Add(new TextEvent(self, 0,
                self.Translate("...there."), l * 80));
        }

        #region Vanilla Pearls

        else if (self.id == ID.Moon_Pearl_Misc || self.id == ID.Moon_Pearl_Misc2)
        {
            self.PebblesPearlIntro();
            self.LoadEventsFromFile(38, Enums.SSOracle.PearlcatPebbles, true, rand);
        }

        else if (self.id == ID.Moon_Pebbles_Pearl)
        {
            self.PebblesPearlIntro();
            self.LoadEventsFromFile(40, Enums.SSOracle.PearlcatPebbles, true, rand);
        }
        else if (self.id == ID.Moon_Pearl_CC)
        {
            self.PebblesPearlIntro();
            self.LoadEventsFromFile(7, Enums.SSOracle.PearlcatPebbles, false, rand);
        }
        else if (self.id == ID.Moon_Pearl_LF_west)
        {
            self.PebblesPearlIntro();
            self.LoadEventsFromFile(10, Enums.SSOracle.PearlcatPebbles, false, rand);
        }
        else if (self.id == ID.Moon_Pearl_LF_bottom)
        {
            self.PebblesPearlIntro();
            self.LoadEventsFromFile(11, Enums.SSOracle.PearlcatPebbles, false, rand);
        }
        else if (self.id == ID.Moon_Pearl_HI)
        {
            self.PebblesPearlIntro();
            self.LoadEventsFromFile(12, Enums.SSOracle.PearlcatPebbles, false, rand);
        }
        else if (self.id == ID.Moon_Pearl_SH)
        {
            self.PebblesPearlIntro();
            self.LoadEventsFromFile(13, Enums.SSOracle.PearlcatPebbles, false, rand);
        }
        else if (self.id == ID.Moon_Pearl_DS)
        {
            self.PebblesPearlIntro();
            self.LoadEventsFromFile(14, Enums.SSOracle.PearlcatPebbles, false, rand);
        }
        else if (self.id == ID.Moon_Pearl_SB_filtration)
        {
            self.PebblesPearlIntro();
            self.LoadEventsFromFile(15, Enums.SSOracle.PearlcatPebbles, false, rand);
        }
        else if (self.id == ID.Moon_Pearl_GW)
        {
            self.PebblesPearlIntro();
            self.LoadEventsFromFile(16, Enums.SSOracle.PearlcatPebbles, false, rand);
        }
        else if (self.id == ID.Moon_Pearl_SL_bridge)
        {
            self.PebblesPearlIntro();
            self.LoadEventsFromFile(17, Enums.SSOracle.PearlcatPebbles, false, rand);
        }
        else if (self.id == ID.Moon_Pearl_SL_moon)
        {
            self.PebblesPearlIntro();
            self.LoadEventsFromFile(18, Enums.SSOracle.PearlcatPebbles, false, rand);
        }
        else if (self.id == ID.Moon_Pearl_SU)
        {
            self.PebblesPearlIntro();
            self.LoadEventsFromFile(41, Enums.SSOracle.PearlcatPebbles, false, rand);
        }
        else if (self.id == ID.Moon_Pearl_UW)
        {
            self.PebblesPearlIntro();
            self.LoadEventsFromFile(42, Enums.SSOracle.PearlcatPebbles, false, rand);
        }
        else if (self.id == ID.Moon_Pearl_SB_ravine)
        {
            self.PebblesPearlIntro();
            self.LoadEventsFromFile(43, Enums.SSOracle.PearlcatPebbles, false, rand);
        }
        else if (self.id == ID.Moon_Pearl_SL_chimney)
        {
            self.PebblesPearlIntro();
            self.LoadEventsFromFile(54, Enums.SSOracle.PearlcatPebbles, false, rand);
        }
        else if (self.id == ID.Moon_Pearl_Red_stomach)
        {
            self.PebblesPearlIntro();
            self.LoadEventsFromFile(51, Enums.SSOracle.PearlcatPebbles, false, rand);
        }
        #endregion

        #region MSC Pearls
        else if (self.id == ID.Moon_Pearl_SI_west)
        {
            self.PebblesPearlIntro();
            self.LoadEventsFromFile(20, Enums.SSOracle.PearlcatPebbles, false, rand);
        }
        else if (self.id == ID.Moon_Pearl_SI_top)
        {
            self.PebblesPearlIntro();
            self.LoadEventsFromFile(21, Enums.SSOracle.PearlcatPebbles, false, rand);
        }
        else if (self.id == MoreSlugcatsEnums.ConversationID.Moon_Pearl_SI_chat3)
        {
            self.PebblesPearlIntro();
            self.LoadEventsFromFile(22, Enums.SSOracle.PearlcatPebbles, false, rand);
        }
        else if (self.id == MoreSlugcatsEnums.ConversationID.Moon_Pearl_SI_chat4)
        {
            self.PebblesPearlIntro();
            self.LoadEventsFromFile(23, Enums.SSOracle.PearlcatPebbles, false, rand);
        }
        else if (self.id == MoreSlugcatsEnums.ConversationID.Moon_Pearl_SI_chat5)
        {
            self.PebblesPearlIntro();
            self.LoadEventsFromFile(24, Enums.SSOracle.PearlcatPebbles, false, rand);
        }
        else if (self.id == MoreSlugcatsEnums.ConversationID.Moon_Pearl_SU_filt)
        {
            self.PebblesPearlIntro();
            self.LoadEventsFromFile(101, Enums.SSOracle.PearlcatPebbles, false, rand);
        }
        else if (self.id == MoreSlugcatsEnums.ConversationID.Moon_Pearl_DM)
        {
            self.PebblesPearlIntro();
            self.LoadEventsFromFile(102, Enums.SSOracle.PearlcatPebbles, false, rand);
        }
        else if (self.id == MoreSlugcatsEnums.ConversationID.Moon_Pearl_LC)
        {
            self.PebblesPearlIntro();
            self.LoadEventsFromFile(103, Enums.SSOracle.PearlcatPebbles, false, rand);
        }
        else if (self.id == MoreSlugcatsEnums.ConversationID.Moon_Pearl_OE)
        {
            self.PebblesPearlIntro();
            self.LoadEventsFromFile(104, Enums.SSOracle.PearlcatPebbles, false, rand);
        }
        else if (self.id == MoreSlugcatsEnums.ConversationID.Moon_Pearl_MS)
        {
            self.PebblesPearlIntro();
            self.LoadEventsFromFile(105, Enums.SSOracle.PearlcatPebbles, false, rand);
        }
        else if (self.id == MoreSlugcatsEnums.ConversationID.Moon_Pearl_Rivulet_stomach)
        {
            self.PebblesPearlIntro();
            self.LoadEventsFromFile(119, Enums.SSOracle.PearlcatPebbles, false, rand);
        }
        else if (self.id == MoreSlugcatsEnums.ConversationID.Moon_Pearl_LC_second)
        {
            self.PebblesPearlIntro();
            self.LoadEventsFromFile(121, Enums.SSOracle.PearlcatPebbles, false, rand);
        }
        #endregion

        else if (self.id == Enums.SSOracle.Pearlcat_SSConvoRMPearl)
        {
            self.PebblesPearlIntro();
            self.LoadEventsFromFile(1001, Enums.SSOracle.PearlcatPebbles, false, rand);
        }
        else if (self.id == Enums.SSOracle.Pearlcat_SSConvoSSPearl)
        {
            self.PebblesPearlIntro();
            self.LoadEventsFromFile(1002, Enums.SSOracle.PearlcatPebbles, false, rand);
        }
        else if (self.id == Enums.SSOracle.Pearlcat_SSConvoASPearlBlue)
        {
            self.PebblesPearlIntro();
            self.LoadEventsFromFile(1003, Enums.SSOracle.PearlcatPebbles, false, rand);
        }
        else if (self.id == Enums.SSOracle.Pearlcat_SSConvoASPearlRed)
        {
            self.PebblesPearlIntro();
            self.LoadEventsFromFile(1004, Enums.SSOracle.PearlcatPebbles, false, rand);
        }
        else if (self.id == Enums.SSOracle.Pearlcat_SSConvoASPearlYellow)
        {
            self.PebblesPearlIntro();
            self.LoadEventsFromFile(1005, Enums.SSOracle.PearlcatPebbles, false, rand);
        }
        else if (self.id == Enums.SSOracle.Pearlcat_SSConvoASPearlGreen)
        {
            self.PebblesPearlIntro();
            self.LoadEventsFromFile(1006, Enums.SSOracle.PearlcatPebbles, false, rand);
        }
        else if (self.id == Enums.SSOracle.Pearlcat_SSConvoASPearlBlack)
        {
            self.PebblesPearlIntro();
            self.LoadEventsFromFile(1007, Enums.SSOracle.PearlcatPebbles, false, rand);
        }

        else
        {
            orig(self); // HACK
        }
    }


    private static void PebblesPearlIntro(this PebblesConversation self)
    {
        var module = self.owner.GetModule();
        var save = self.owner.oracle.room.game.GetMiscWorld();

        if (save == null) return;
        

        if (module.WasPearlAlreadyRead)
        {
            switch (Random.Range(0, 4))
            {
                case 0:
                    self.events.Add(new TextEvent(self, 0, self.owner.Translate("I already read this one. I can read it again, I suppose."), 10));
                    break;

                case 1:
                    self.events.Add(new TextEvent(self, 0, self.owner.Translate("This one again? I have better things to do... but..."), 10));
                    break;

                case 2:
                    self.events.Add(new TextEvent(self, 0, self.owner.Translate("Let us see what- oh, again?"), 10));
                    break;

                default:
                    self.events.Add(new TextEvent(self, 0, self.owner.Translate("I remember this one... don't you? Well..."), 10));
                    break;
            }
        }
        else
        {
            switch (save.UniquePearlsBroughtToPebbles)
            {
                case 1:
                    self.events.Add(new TextEvent(self, 0, self.owner.Translate("Something new to read... it has been too long..."), 10));
                    break;

                case 2:
                    self.events.Add(new TextEvent(self, 0, self.owner.Translate("Another? And I shall read this one to you as well..."), 10));
                    break;

                case 3:
                    self.events.Add(new TextEvent(self, 0, self.owner.Translate("And a third? So it is..."), 10));
                    break;

                case 4:
                    self.events.Add(new TextEvent(self, 0, self.owner.Translate("Four! Well, if you insist..."), 10));
                    break;

                case 5:
                    self.events.Add(new TextEvent(self, 0, self.owner.Translate("So curious..."), 10));
                    break;

                default:
                    switch (Random.Range(0, 4))
                    {
                        case 0:
                            self.events.Add(new TextEvent(self, 0, self.owner.Translate("Another one? I shouldn't be surprised. Let's see..."), 10));
                            break;

                        case 1:
                            self.events.Add(new TextEvent(self, 0, self.owner.Translate("Ah, yet another one? You are even better at finding these than I imagined..."), 10));
                            break;

                        case 2:
                            self.events.Add(new TextEvent(self, 0, self.owner.Translate("Read this one too? Yes, yes, here it is..."), 10));
                            break;

                        default:
                            self.events.Add(new TextEvent(self, 0, self.owner.Translate("Something else new? Allow me to see..."), 10));
                            break;
                    }
                    break;
            }
        }
    }

    private static void SSOracleBehavior_SeePlayer(On.SSOracleBehavior.orig_SeePlayer orig, SSOracleBehavior self)
    {
        if (self.oracle.room.game.IsPearlcatStory() && self.action != Enums.SSOracle.Pearlcat_SSActionGeneral)
        {
            if (self.timeSinceSeenPlayer < 0)
                self.timeSinceSeenPlayer = 0;
            
            self.SlugcatEnterRoomReaction();
            self.NewAction(Enums.SSOracle.Pearlcat_SSActionGeneral);
            return;
        }

        orig(self); // HACK
    }

    private static void SSOracleBehavior_NewAction(On.SSOracleBehavior.orig_NewAction orig, SSOracleBehavior self, Action nextAction)
    {
        if (self.oracle.room.game.IsPearlcatStory() && self.action != Enums.SSOracle.Pearlcat_SSActionGeneral && self.action != SSOracleBehavior.Action.ThrowOut_KillOnSight)
        {
            if (self.currSubBehavior.ID == Enums.SSOracle.Pearlcat_SSSubBehavGeneral) return;

            self.inActionCounter = 0;
            self.action = nextAction;

            var subBehavior = self.allSubBehaviors.FirstOrDefault(x => x.ID == Enums.SSOracle.Pearlcat_SSSubBehavGeneral);
            
            if (subBehavior == null)
                self.allSubBehaviors.Add(subBehavior = new SSOracleMeetPearlcat(self));

            self.currSubBehavior.Deactivate();

            subBehavior.Activate(self.action, nextAction);
            self.currSubBehavior = subBehavior;
            return;
        }
        
        orig(self, nextAction); // HACK
    }


    public class SSOracleMeetPearlcat : ConversationBehavior
    {
        public int ConvoCount { get; set; }

        public SSOracleMeetPearlcat(SSOracleBehavior owner) : base(owner, Enums.SSOracle.Pearlcat_SSSubBehavGeneral, Enums.SSOracle.Pearlcat_SSConvoFirstMeet)
        {
            var save = oracle.room.game.GetMiscWorld();

            if (save == null) return;

            Plugin.Logger.LogWarning("PEARLCAT PEBBLES MEETING: " + save.PebblesMeetCount);

            switch (save.PebblesMeetCount)
            {
                case 0:
                    break;

                case 1:
                    dialogBox.NewMessage(
                        Translate("Ah. So you've returned."), 0);

                    dialogBox.NewMessage(
                        Translate("Brought me something to read? Or just wasting my time, as per usual?"), 0);
                    break;

                case 2:
                    dialogBox.NewMessage(
                            Translate("Back again? I hope you have brought me something this time."), 0);
                    break;

                case 3:
                    dialogBox.NewMessage(
                            Translate("Back again? You do visit often..."), 0);
                    break;

                default:
                    switch (Random.Range(0, 4))
                    {
                        case 0:
                            dialogBox.NewMessage(
                                Translate("Welcome back, I suppose."), 0);
                            break;

                        case 1:
                            dialogBox.NewMessage(
                                Translate("I see you've returned, yet again."), 0);
                            break;

                        case 2:
                            dialogBox.NewMessage(
                                Translate("Hello again."), 0);
                            break;

                        default:
                            dialogBox.NewMessage(
                                Translate("Hello, again. You are very curious. Very curious indeed."), 0);
                            break;
                    }
                    break;
            }

            save.PebblesMeetCount++;
        }

        public override void Update()
        {
            if (player == null) return;

            var module = owner.GetModule();
            var save = oracle.room.game.GetMiscWorld();

            if (save == null) return;

            var meetCount = save.PebblesMeetCount;

            owner.movementBehavior = MovementBehavior.KeepDistance;

            ReadPearlUpdate(module);

            if (module.PearlToReturn != null)
            {
                if (module.PearlToReturn.grabbedBy.Count > 0 || module.PlayerToReturnTo?.TryGetTarget(out var player) != true)
                {
                    module.PearlToReturn = null;
                }
                else
                {
                    var freeHand = player.FreeHand();

                    if (freeHand == -1)
                    {
                        module.PearlToReturn = null;
                    }
                    else
                    {
                        var pearlDir = Custom.DirVec(module.PearlToReturn.firstChunk.pos, player.firstChunk.pos);
                        var pearlDist = Custom.Dist(module.PearlToReturn.firstChunk.pos, player.firstChunk.pos);

                        module.PearlToReturn.firstChunk.vel = pearlDir * Custom.LerpMap(pearlDist, 200.0f, 10.0f, 15.0f, 4.0f);

                        if (pearlDist < 10.0f)
                            player.SlugcatGrab(module.PearlToReturn, freeHand);
                    }
                }
            }

            if (owner.conversation?.id != null && owner.conversation.id == Enums.SSOracle.Pearlcat_SSConvoTakeRMPearl)
            {
                if (module.TakeRMTimer > 0)
                {
                    module.TakeRMTimer--;
                }
                else if (module.TakeRMTimer == 0)
                {
                    TakeRMPearl(oracle);
                    module.TakeRMTimer = -1;
                }

                if (module.TakeRMTimer == -1)
                {
                    if (module.GiveSSTimer > 0)
                    {
                        module.GiveSSTimer--;
                    }
                    else if (module.GiveSSTimer == 0)
                    {
                        GiveSSPearl(oracle);
                        module.GiveSSTimer = -1;
                    }
                }
            }

            if (owner.conversation != null && !owner.conversation.slatedForDeletion) return;


            if (module.PearlBeingRead != null)
                module.PearlBeingRead.gravity = 0.0f;

            module.PearlToReturn ??= module.PearlBeingRead;
            module.PearlBeingRead = null;


            if (meetCount == 1)
            {
                if (ConvoCount == 0)
                {
                    owner.LockShortcuts();
                    owner.InitateConversation(Enums.SSOracle.Pearlcat_SSConvoFirstMeet, this);
                    ConvoCount++;
                }
                else if (ConvoCount == 1)
                {
                    if (HasRMPearl(oracle))
                        owner.InitateConversation(Enums.SSOracle.Pearlcat_SSConvoRMPearlInspect, this);

                    ConvoCount++;
                }
                else if (ConvoCount == 2)
                {
                    if (HasRMPearl(oracle))
                        owner.InitateConversation(Enums.SSOracle.Pearlcat_SSConvoTakeRMPearl, this);

                    ConvoCount++;
                }
                else if (ConvoCount == 3)
                {
                    owner.InitateConversation(Enums.SSOracle.Pearlcat_SSConvoFirstLeave, this);
                    ConvoCount++;
                }
                else
                {
                    owner.UnlockShortcuts();
                    owner.getToWorking = 1.0f;
                    owner.movementBehavior = MovementBehavior.Meditate;
                }
            }
            else
            {
                if (ConvoCount == 1)
                {
                    owner.InitateConversation(Enums.SSOracle.Pearlcat_SSConvoTakeRMPearl, this);
                    ConvoCount++;
                }
                else
                {
                    owner.UnlockShortcuts();
                    owner.getToWorking = 1.0f;
                    owner.movementBehavior = MovementBehavior.Meditate;

                    if (HasRMPearl(oracle))
                    {
                        owner.LockShortcuts();
                        owner.getToWorking = 0.0f;

                        owner.InitateConversation(Enums.SSOracle.Pearlcat_SSConvoRMPearlInspect, this);
                        ConvoCount++;
                    }
                }
            }
        }

        private void ReadPearlUpdate(SSOracleModule module)
        {
            var graphics = oracle.graphicsModule as OracleGraphics;
            var handPos = graphics?.hands?.FirstOrDefault()?.pos ?? oracle.firstChunk.pos;

            if (module.PearlBeingRead != null)
            {
                module.PearlBeingRead.AllGraspsLetGoOfThisObject(true);
                module.PearlBeingRead.gravity = 0.0f;

                module.PearlBeingRead.firstChunk.HardSetPosition(handPos);
                module.PearlBeingRead.firstChunk.vel = Vector2.zero;
            }

            if (owner.getToWorking != 1.0f) return;

            if (module.PearlToRead != null)
            {
                owner.LockShortcuts();
                owner.getToWorking = 0.0f;
                owner.movementBehavior = MovementBehavior.Talk;

                module.PearlToRead.AllGraspsLetGoOfThisObject(true);

                var oraclePearlDir = Custom.DirVec(module.PearlToRead.firstChunk.pos, handPos);
                var oraclePearlDist = Custom.Dist(handPos, module.PearlToRead.firstChunk.pos);

                module.PearlToRead.firstChunk.vel = oraclePearlDir * Custom.LerpMap(oraclePearlDist, 200.0f, 10.0f, 15.0f, 4.0f);


                if (oraclePearlDist < 10.0f)
                {
                    StartItemConversation(module.PearlToRead, module);

                    module.PearlBeingRead = module.PearlToRead;
                    module.PearlToRead = null;
                }
            }
            else // Look for pearl to read
            {
                var roomObjects = oracle.room.physicalObjects;

                for (int i = 0; i < roomObjects.Length; i++)
                {
                    for (int j = 0; j < roomObjects[i].Count; j++)
                    {
                        var physicalObject = roomObjects[i][j];

                        if (physicalObject.grabbedBy.Count > 0) continue;

                        if (physicalObject is not DataPearl dataPearl) continue;

                        if (physicalObject.abstractPhysicalObject.IsPlayerObject()) continue;

                        if (!module.PearlsHeldByPlayer.TryGetValue(dataPearl, out var player)) continue;

                        if (dataPearl is PebblesPearl) continue;

                        module.PearlToRead = dataPearl;
                        module.PlayerToReturnTo = new(player);
                    }
                }
            }

            foreach (var roomObject in oracle.room.physicalObjects)
            {
                foreach (var physicalObject in roomObject)
                {
                    if (physicalObject is not DataPearl dataPearl) continue;

                    var wasGrabbed = module.PearlsHeldByPlayer.TryGetValue(dataPearl, out _);

                    if (physicalObject.grabbedBy.FirstOrDefault(x => x.grabber is Player)?.grabber is Player playerGrabber)
                    {
                        if (!wasGrabbed)
                            module.PearlsHeldByPlayer.Add(dataPearl, playerGrabber);
                    }
                    else
                    {
                        if (wasGrabbed)
                            module.PearlsHeldByPlayer.Remove(dataPearl);
                    }
                }
            }
        }

        public void StartItemConversation(DataPearl pearl, SSOracleModule module)
        {
            var save = pearl.room.game.GetMiscWorld();

            if (save == null) return;

            var pearlID = pearl.abstractPhysicalObject.ID.number;

            module.WasPearlAlreadyRead = save.PearlIDsBroughtToPebbles.ContainsKey(pearlID);

            var rand = Random.Range(0, 100000);

            if (!module.WasPearlAlreadyRead)
                save.PearlIDsBroughtToPebbles.Add(pearlID, rand);

            else
                rand = save.PearlIDsBroughtToPebbles[pearlID];

            module.Rand = rand;
            var type = pearl.AbstractPearl.dataPearlType;

            if (type == DataPearl.AbstractDataPearl.DataPearlType.Misc || type.Index == -1)
                owner.InitateConversation(Conversation.ID.Moon_Pearl_Misc, this);

            else if (type == DataPearl.AbstractDataPearl.DataPearlType.Misc2)
                owner.InitateConversation(Conversation.ID.Moon_Pearl_Misc2, this);

            else if (ModManager.MSC && type == MoreSlugcatsEnums.DataPearlType.BroadcastMisc)
                owner.InitateConversation(MoreSlugcatsEnums.ConversationID.Moon_Pearl_BroadcastMisc, this);

            else if (type == DataPearl.AbstractDataPearl.DataPearlType.PebblesPearl)
                owner.InitateConversation(Conversation.ID.Moon_Pebbles_Pearl, this);


            else if (type == Enums.Pearls.RM_Pearlcat || type == MoreSlugcatsEnums.DataPearlType.RM)
                owner.InitateConversation(Enums.SSOracle.Pearlcat_SSConvoRMPearl, this);

            else if (type == Enums.Pearls.SS_Pearlcat)
                owner.InitateConversation(Enums.SSOracle.Pearlcat_SSConvoSSPearl, this);

            else if (type == Enums.Pearls.AS_PearlBlue)
                owner.InitateConversation(Enums.SSOracle.Pearlcat_SSConvoASPearlBlue, this);

            else if (type == Enums.Pearls.AS_PearlGreen)
                owner.InitateConversation(Enums.SSOracle.Pearlcat_SSConvoASPearlGreen, this);
            
            else if (type == Enums.Pearls.AS_PearlRed)
                owner.InitateConversation(Enums.SSOracle.Pearlcat_SSConvoASPearlRed, this);
            
            else if (type == Enums.Pearls.AS_PearlYellow)
                owner.InitateConversation(Enums.SSOracle.Pearlcat_SSConvoASPearlYellow, this);
            
            else if (type == Enums.Pearls.AS_PearlBlack)
                owner.InitateConversation(Enums.SSOracle.Pearlcat_SSConvoASPearlBlack, this);


            else
                owner.InitateConversation(DataPearlToConversation(type), this);
        }

        public bool HasRMPearl(Oracle oracle)
        {
            foreach (var roomObject in oracle.room.physicalObjects)
            {
                foreach (var physicalObject in roomObject)
                {
                    if (physicalObject is not DataPearl dataPearl) continue;

                    var type = dataPearl.AbstractPearl.dataPearlType;

                    if (type == Enums.Pearls.RM_Pearlcat || type == MoreSlugcatsEnums.DataPearlType.RM)
                        return true;
                }
            }

            return false;
        }

        public void TakeRMPearl(Oracle oracle)
        {
            for (int roomObjIndex = oracle.room.physicalObjects.Length - 1; roomObjIndex >= 0; roomObjIndex--)
            {
                var roomObject = oracle.room.physicalObjects[roomObjIndex];
               
                for (int physObjInded = roomObject.Count - 1; physObjInded >= 0; physObjInded--)
                {
                    var physicalObject = roomObject[physObjInded];
                
                    if (physicalObject is Player player)
                    {
                        if (!player.TryGetPearlcatModule(out var playerModule)) continue;

                        for (int i = playerModule.Inventory.Count - 1; i >= 0; i--)
                        {
                            var item = playerModule.Inventory[i];
                            
                            if (item is not DataPearl.AbstractDataPearl itemDataPearl) continue;

                            var itemType = itemDataPearl.dataPearlType;

                            if (itemType != Enums.Pearls.RM_Pearlcat && itemType != MoreSlugcatsEnums.DataPearlType.RM) continue;

                            if (item.realizedObject != null)
                            {
                                item.realizedObject.AbstractedEffect();
                                oracle.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, player.firstChunk.pos);
                            }

                            player.RemoveFromInventory(item);
                            //item.tracker.UninitializeTracker();

                            item.destroyOnAbstraction = true;
                            item.Abstractize(item.pos);

                        }
                        continue;
                    }
                    
                    if (physicalObject.abstractPhysicalObject.IsPlayerObject()) continue;

                    if (physicalObject is not DataPearl dataPearl) continue;

                    var type = dataPearl.AbstractPearl.dataPearlType;

                    if (type != Enums.Pearls.RM_Pearlcat && type != MoreSlugcatsEnums.DataPearlType.RM) continue;


                    physicalObject.AbstractedEffect();
                    oracle.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, physicalObject.firstChunk, false, 1.0f, 1.0f);

                    //physicalObject.abstractPhysicalObject.tracker.UninitializeTracker();

                    physicalObject.abstractPhysicalObject.destroyOnAbstraction = true;
                    physicalObject.abstractPhysicalObject.Abstractize(physicalObject.abstractPhysicalObject.pos);
                }
            }
        }

        public void GiveSSPearl(Oracle oracle)
        {
            foreach (var roomObject in oracle.room.physicalObjects)
            {
                foreach (var physicalObject in roomObject)
                {
                    if (physicalObject is not Player player) continue;

                    if (!player.TryGetPearlcatModule(out var playerModule)) continue;


                    var abstractPearl = new DataPearl.AbstractDataPearl(oracle.room.world, AbstractObjectType.DataPearl, null, player.abstractCreature.pos, oracle.room.game.GetNewID(), -1, -1, null, Enums.Pearls.SS_Pearlcat);

                    if (playerModule.Inventory.Count >= ModOptions.MaxPearlCount.Value)
                    {
                        abstractPearl.pos = player.abstractCreature.pos;
                        oracle.room.abstractRoom.AddEntity(abstractPearl);
                        abstractPearl.RealizeInRoom();

                        var freeHand = player.FreeHand();

                        if (freeHand > -1)
                           player.SlugcatGrab(abstractPearl.realizedObject, freeHand);
                    }
                    else
                    {
                        player.StoreObject(abstractPearl);
                    }
                }
            }
        }
    }
}