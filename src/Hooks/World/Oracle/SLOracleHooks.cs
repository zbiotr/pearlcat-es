﻿using UnityEngine;
using Random = UnityEngine.Random;

namespace Pearlcat;

// adapted from NoirCatto: https://github.com/NoirCatto/NoirCatto/blob/master/Oracles.cs
public static class Hooks
{
    public static void ApplyHooks_SLOracle()
    {
        On.SLOracleBehaviorHasMark.MoonConversation.AddEvents += MoonConversation_AddEvents;
        On.SLOracleBehaviorHasMark.NameForPlayer += SLOracleBehaviorHasMark_NameForPlayer;

        On.SLOracleBehaviorHasMark.ThirdAndUpGreeting += SLOracleBehaviorHasMark_ThirdAndUpGreeting;
    }


    // SL Dialog Helpers
    private static void Dialog(this SLOracleBehaviorHasMark self, string text) => self.dialogBox.NewMessage(self.Translate(text), 10);
    private static void Dialog_Start(this SLOracleBehaviorHasMark self, string text) => self.dialogBox.Interrupt(self.Translate(text), 10);

    // SL Conversation Dialog Helpers
    private static void Dialog(this SLOracleBehaviorHasMark.MoonConversation self, string text, int initialWait, int textLinger) => self.events.Add(new Conversation.TextEvent(self, initialWait, self.Translate(text), textLinger));
    private static void Dialog_NoLinger(this SLOracleBehaviorHasMark.MoonConversation self, string text) => self.events.Add(new Conversation.TextEvent(self, 0, t.Translate(text), 0));
    private static void Dialog_Wait(this SLOracleBehaviorHasMark.MoonConversation self, int initialWait) => self.events.Add(new Conversation.WaitEvent(self, initialWait));



    private static void SLOracleBehaviorHasMark_ThirdAndUpGreeting(On.SLOracleBehaviorHasMark.orig_ThirdAndUpGreeting orig, SLOracleBehaviorHasMark self)
    {
        if (!self.oracle.room.game.IsPearlcatStory() || !self.IsMoon())
        {
            orig(self);
            return;
        }

        var save = self.oracle.room.game.GetMiscWorld();
        var miscProg = Utils.GetMiscProgression();

        var t = self;



        if (save?.HasPearlpupWithPlayer == true && miscProg.IsPearlpupSick && self.State.GetOpinion != SLOrcacleState.PlayerOpinion.Dislikes && !self.DamagedMode)
        {
            self.oracle.room.game.GetStorySession.TryDream(Enums.Dreams.Dream_Pearlcat_Moon_Sick);

            if (save.MoonSickPupMeetCount == 0)
            {
                self.Dialog_Start("Oh! It is good to see you two again!");

                self.Dialog("Is my memory so bad to forget how your little one looks? They seem paler...");

                self.Dialog("Oh... oh no...");

                self.Dialog("They are unwell, <PlayerName>, very unwell indeed.");

                self.Dialog("I... wish there was more I could do... but even... nevermind in my current state.");

                self.Dialog("I am so sorry.");


                self.Dialog(". . .");

                self.Dialog("My neighbour, Five Pebbles, is a little temperamental, but means well.");

                self.Dialog("He is much better equipped than me at present - I would recommend paying him a visit, if you can.<LINE>Although he was not designed as a medical facility, he may be able to aid you, in some way.");

                self.Dialog("In any case, you are welcome to stay as long as you like. Anything to ease the pain.");

                save.MoonSickPupMeetCount++;
            }
            else if (save.MoonSickPupMeetCount == 1)
            {
                self.Dialog_Start("Welcome back, <PlayerName>, and your little one too.");

                self.Dialog("I hope the cycles have been treating you well... it must be hard to take care of eachother out there.");

                self.Dialog(". . .");

                self.Dialog("...I am not sure if this is comforting, but...");

                self.Dialog("Death is not the end... even death that seems permanent.");

                self.Dialog("I know that quite well.");

                save.MoonSickPupMeetCount++;
            }
            else
            {
                self.Dialog_Start("Welcome back, you two!");

                self.Dialog("I hope you are staying safe out there...");
            }
        }
        else if (miscProg.HasTrueEnding && self.State.GetOpinion != SLOrcacleState.PlayerOpinion.Dislikes && !self.DamagedMode)
        {
            self.Dialog_Start("Welcome back, <PlayerName>!");

            self.Dialog("Ah, you didn't happen to find a way to tell me about your travels, did you?");

            self.Dialog("I'm kidding, of course! My imagination will suffice, though the curiosity does burn me up inside...");

            self.Dialog("For now, my only lens into the outside is those pearls you carry.");

            self.Dialog("So please, bring me more, as long as it isn't too dangerous for you... these visits really are the highlight of my days here...");
        }

        orig(self);
    }

    private static string SLOracleBehaviorHasMark_NameForPlayer(On.SLOracleBehaviorHasMark.orig_NameForPlayer orig, SLOracleBehaviorHasMark self, bool capitalized)
    {
        if (!self.oracle.room.game.IsPearlcatStory() || !self.IsMoon())
        {
            return orig(self, capitalized);
        }

        var t = Utils.Translator;

        var prefix = t.Translate("strange");
        var name = t.Translate("scholar");

        if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Dislikes)
        {
            prefix = t.Translate("acursed");
            name = t.Translate("thing");
        }

        var damagedSpeech = self.DamagedMode && Random.value < 0.5f;

        if (capitalized)
        {
            prefix = string.Concat(prefix[0].ToString().ToUpper(), prefix.Substring(1));
        }

        return prefix + (damagedSpeech ? t.Translate("... ") : " ") + name;
    }



    private static void MoonConversation_AddEvents(On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, SLOracleBehaviorHasMark.MoonConversation self)
    {
        var save = self.myBehavior.oracle.room.game.GetMiscWorld();
        var miscProg = Utils.GetMiscProgression();

        if (self.currentSaveFile != Enums.Pearlcat || self.myBehavior.oracle.IsMoon() || save == null)
        {
            orig(self);
            return;
        }

        var metMoon = self.myBehavior.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.EverMetMoon;

        if (miscProg.HasTrueEnding)
        {
            // Adult Pearlpup
            if (Moon_TrueEndDialog(orig, self, metMoon, save)) return;
        }
        else
        {
            // Pearlcat
            if (Moon_Dialog(orig, self, save, miscProg)) return;
        }


        orig(self);
    }

    private static bool Moon_Dialog(On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, SLOracleBehaviorHasMark.MoonConversation self, SaveMiscWorld save, SaveMiscProgression miscProg)
    {
        if (self.id == Conversation.ID.MoonFirstPostMarkConversation)
        {
            switch (Mathf.Clamp(self.State.neuronsLeft, 0, 5))
            {
                case 2:
                    self.Dialog("Get... get away... strange... thing.", 30, 10);
                    self.Dialog("Please... this all I have left... you have so much... ...why take mine?", 0, 10);
                    return true;

                case 3:
                    self.Dialog("YOU!", 30, 10);
                    self.Dialog("...You ate... me. Please go away. I won't speak... to you.<LINE>I... CAN'T speak to you... because... you ate...me... ...and why?", 60, 0);
                    return true;

                case 5:
                    self.Dialog_NoLinger("Oh, hello! Hello!");

                    self.Dialog_NoLinger("You gave me quite the fright! I do not get visitors often these days;<LINE>much less visitors quite like you, who I can talk to...");


                    self.Dialog_NoLinger("...you have the gift of communication!<LINE>Perhaps you paid a visit to my neighbour, Five Pebbles...?");

                    self.Dialog_NoLinger("But... the mark you possess, it is very unusual.<LINE>Not something anyone in the local group could bestow...");


                    self.Dialog_NoLinger("You must be very brave to have made it all the way here. Very brave indeed...");

                    self.Dialog_Wait(10);

                    if (save?.HasPearlpupWithPlayer == true)
                    {
                        if (miscProg.IsPearlpupSick)
                        {
                            self.Dialog_NoLinger("And who is your little friend? They are...");

                            self.Dialog_NoLinger("Oh... oh no...");

                            self.Dialog_NoLinger("They are unwell, <PlayerName>, very unwell indeed.");

                            self.Dialog_NoLinger("I... wish there was more I could do... but even... nevermind in my current state.");

                            self.Dialog_NoLinger("I am so sorry.");

                            self.Dialog_NoLinger("You are welcome to stay as long as you like. Anything to ease the pain.");

                            save.MoonSickPupMeetCount++;
                            return true;
                        }
                        else
                        {
                            self.Dialog_NoLinger("And who is your little friend? They are quite adorable!");

                            self.Dialog_NoLinger("Ah, I hope you both stay safe out there...");
                        }
                    }
                    else
                    {
                        self.Dialog_NoLinger("The power you possess, it is fascinating. I have never seen anything quite like it from your kind.");

                        self.Dialog_NoLinger("Your biology... it is both familiar and foreign to me. How curious.");

                        self.Dialog("If only I had my memories, I might have a clue...", 0, 5);

                        self.Dialog("But it's best not to dwell on the past, don't you agree <PlayerName>?", 0, 5);
                    }

                    self.Dialog_Wait(10);

                    self.Dialog("Given your unique abilities, if you are curious as to the contents of those pearls -", 0, 5);

                    self.Dialog("I think I may still have some use left after all!~", 0, 5);
                    return true;

                default:
                    orig(self);
                    return true;
            }
        }

        if (save?.HasPearlpupWithPlayer == true && miscProg.IsPearlpupSick && self.State.GetOpinion != SLOrcacleState.PlayerOpinion.Dislikes && self.myBehavior is SLOracleBehaviorHasMark mark && !mark.DamagedMode)
        {
            self.myBehavior.oracle.room.game.GetStorySession.TryDream(Enums.Dreams.Dream_Pearlcat_Moon_Sick);

            if (save.MoonSickPupMeetCount == 0)
            {
                self.Dialog_NoLinger("Oh! It is good to see you two again!");

                self.Dialog_NoLinger("Is my memory so bad to forget how your little one looks? They seem paler...");

                self.Dialog_NoLinger("Oh... oh no...");

                self.Dialog_NoLinger("They are unwell, <PlayerName>, very unwell indeed.");

                self.Dialog_NoLinger("I... wish there was more I could do... but even... nevermind in my current state.");

                self.Dialog_NoLinger("I am so sorry.");


                self.Dialog_NoLinger(". . .");

                self.Dialog_NoLinger("My neighbour, Five Pebbles, is a little temperamental, but means well.");

                self.Dialog_NoLinger("He is much better equipped than me at present - I would recommend paying him a visit, if you can.<LINE>Although he was not designed as a medical facility, he may be able to aid you, in some way.");

                self.Dialog_NoLinger("In any case, you are welcome to stay as long as you like. Anything to ease the pain.");

                save.MoonSickPupMeetCount++;
                return true;
            }
            else if (save.MoonSickPupMeetCount == 1)
            {
                self.Dialog_NoLinger("Welcome back, <PlayerName>, and your little one too.");

                self.Dialog_NoLinger("I hope the cycles have been treating you well... it must be hard to take care of eachother out there.");

                self.Dialog_NoLinger(". . .");

                self.Dialog_NoLinger("...I am not sure if this is comforting, but...");

                self.Dialog_NoLinger("Death is not the end... even death that seems permanent.");

                self.Dialog_NoLinger("I know that quite well.");

                save.MoonSickPupMeetCount++;
                return true;
            }
            else
            {
                self.Dialog_NoLinger("Welcome back, you two!");

                self.Dialog_NoLinger("I hope you are staying safe out there...");
            }
        }


        if (self.id == Conversation.ID.MoonSecondPostMarkConversation)
        {
            switch (Mathf.Clamp(self.State.neuronsLeft, 0, 5))
            {
                case 4:
                    if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Likes)
                    {
                        self.Dialog("Hello! I remember you! I remember...", 30, 0);

                        self.Dialog_NoLinger("You would be a hard one to forget after all, <PlayerName>!");

                        self.Dialog_NoLinger("I'm still more than happy to read any pearls you bring me;<LINE>especially given your unique abilities.");

                        self.Dialog_NoLinger("I do not have much else to occupy my time with these days...");
                        return true;
                    }

                    if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Dislikes)
                    {

                        self.Dialog("You. I didn't forget...", 30, 0);

                        self.Dialog_NoLinger("What do you want? To take away more of my life...?");

                        self.Dialog_NoLinger("Nevermind... this anger is useless...");

                        self.Dialog_NoLinger("There is nothing I can do to stop you, so here is my request:<LINE>Leave me be, or end it quickly.");
                        return true;
                    }

                    self.Dialog("You... I didn't forget...", 30, 0);

                    self.Dialog("I can only hope you have not come... to hurt me more.", 30, 0);
                    return true;

                case 5:
                    if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Dislikes)
                    {
                        self.Dialog("You. Again. I still remember what you did...", 0, 10);
                        return true;
                    }

                    if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Likes)
                    {
                        self.Dialog("Hello again, <PlayerName>!", 0, 10);

                        self.Dialog_NoLinger("So curious... I wonder what it is you're searching for?");

                        self.Dialog_NoLinger("I have nothing for you, I'm afraid... but I hope you find the answers you seek.");

                        if (ModManager.MSC && self.myBehavior.CheckSlugpupsInRoom())
                        {
                            self.Dialog("I very much enjoy the company though... you and your little friend are always welcome here.", 0, 5);

                            self.Dialog("I would guess they will grow up into quite the scholar themself, someday...", 0, 5);
                            return true;
                        }

                        if (ModManager.MMF && self.myBehavior.CheckStrayCreatureInRoom() != CreatureTemplate.Type.StandardGroundCreature)
                        {
                            self.Dialog("I do enjoy the company of you and your friend though, <PlayerName>.", 0, 5);

                            self.Dialog("You're welcome to stay a while... your ability is fascinating.", 0, 5);
                            return true;
                        }

                        self.Dialog("I do enjoy the company though... it gets lonely out here.", 0, 5);

                        self.Dialog_NoLinger("If you happen to have a moment to spare, I'd be more than happy to read those pearls...<LINE>There is not much else to do to pass the time.");
                        return true;
                    }

                    self.Dialog("Oh, hello <PlayerName>!", 0, 10);
                    return true;

                default:
                    orig(self);
                    return true;
            }
        }

        return false;
    }

    private static bool Moon_TrueEndDialog(On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, SLOracleBehaviorHasMark.MoonConversation self, bool metMoon, SaveMiscWorld save)
    {
        // Moon never met Pearlcat
        if (!metMoon)
        {
            if (self.id == Conversation.ID.MoonFirstPostMarkConversation)
            {
                switch (Mathf.Clamp(self.State.neuronsLeft, 0, 5))
                {
                    case 2:
                        self.Dialog("...hello?", 30, 10);
                        self.Dialog("...help... please...", 0, 10);
                        return true;

                    case 3:
                        self.Dialog("Who... who is there?", 30, 10);
                        self.Dialog("I have so little... please don't take... this... this is all I have...", 0, 10);
                        return true;

                    case 5:
                        self.Dialog_NoLinger("Oh...? Hello! Hello...");

                        self.Dialog_Wait(10);

                        self.Dialog_NoLinger("...are you alright, little one? The modifications to your body are quite... extreme.");

                        self.Dialog_Wait(5);

                        self.Dialog_NoLinger("Sorry, I shouldn't be so intrusive, I am in quite the state myself after all.");

                        self.Dialog("You are just a lot to take in for little old me! A fascinating power, the gift of communication;", 0, 5);

                        self.Dialog("And a nasty scar, poor thing... I can't imagine where you got that, or even where you came from...?", 0, 5);

                        self.Dialog("You seem to be doing well for yourself nonetheless, your tenacity from whatever occurred is admirable!", 0, 5);

                        self.Dialog_Wait(5);

                        self.Dialog("...well, given your unique set of skills... if you ever need something read...", 0, 5);

                        self.Dialog("I'll be self.Waiting right here - not like I can go far, anyways~", 0, 5);
                        return true;

                    default:
                        orig(self);
                        return true;
                }
            }

            if (self.id == Conversation.ID.MoonSecondPostMarkConversation)
            {
                switch (Mathf.Clamp(self.State.neuronsLeft, 0, 5))
                {
                    case 4:
                        if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Likes)
                        {
                            self.Dialog("Hello! I remember you! I remember...", 30, 0);

                            self.Dialog_NoLinger("You would be a hard one to forget after all, strange scholar!");

                            self.Dialog_NoLinger("I'm still more than happy to read any pearls you bring me;<LINE>especially given your unique abilities.");

                            self.Dialog_NoLinger("I do not have much else to occupy my time with these days...");
                            return true;
                        }

                        if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Dislikes)
                        {

                            self.Dialog("You. I didn't forget...", 30, 0);

                            self.Dialog_NoLinger("What do you want? To take away more of my life...?");

                            self.Dialog_NoLinger("Look at yourself! You know how it feels, to be within an inch of death...");

                            self.Dialog_NoLinger("There is nothing I can do to stop you, so here is my request:<LINE>Leave me be, or end it quickly.");
                            return true;
                        }

                        self.Dialog("Hello again, strange scholar...", 30, 0);

                        self.Dialog("Please try not to push me too hard; with the number of neurons I have... let's just say my mind gets a little foggy.", 30, 0);
                        return true;

                    case 5:
                        if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Dislikes)
                        {
                            self.Dialog("You... I still remember what you did.", 0, 10);

                            self.Dialog("It would be pointless to ask why...", 0, 10);

                            self.Dialog("I've accepted my fate - and one day, you will meet an end too...", 0, 10);
                            return true;
                        }

                        if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Likes)
                        {
                            self.Dialog("Hello again, strange scholar!", 0, 10);

                            self.Dialog_NoLinger("So curious... I wonder what it is you're searching for?");

                            self.Dialog_NoLinger("I have nothing for you, I'm afraid... but I hope you find the answers you seek.");

                            if (ModManager.MSC && self.myBehavior.CheckSlugpupsInRoom())
                            {
                                self.Dialog("I very much enjoy the company though... you and your family are always welcome here.", 0, 5);
                                return true;
                            }

                            if (ModManager.MMF && self.myBehavior.CheckStrayCreatureInRoom() != CreatureTemplate.Type.StandardGroundCreature)
                            {
                                self.Dialog("I do enjoy the company of you and your friend though, strange scholar.", 0, 5);

                                self.Dialog("You're welcome to stay a while... your ability is fascinating.", 0, 5);
                                return true;
                            }

                            self.Dialog("I do enjoy the company though... it gets lonely out here.", 0, 5);

                            self.Dialog_NoLinger("If you happen to have a moment to spare, I'd be more than happy to read those pearls...<LINE>There is not much else to do to pass the time.");
                            return true;
                        }

                        self.Dialog("Oh, hello strange scholar!", 0, 10);
                        return true;

                    default:
                        orig(self);
                        return true;
                }
            }
        }

        // Moon met Pearlcat but never met sick Pearlpup
        else if (save.MoonSickPupMeetCount == 0)
        {
            if (self.id == Conversation.ID.MoonFirstPostMarkConversation)
            {
                switch (Mathf.Clamp(self.State.neuronsLeft, 0, 5))
                {
                    case 2:
                        self.Dialog("...hello?", 30, 10);

                        self.Dialog("...familiar ...help... please...", 0, 10);
                        return true;

                    case 3:
                        self.Dialog("Who... who is there?", 30, 10);

                        self.Dialog("I have so little... please don't take... this... this is all I have...", 0, 10);

                        self.Dialog_Wait(10);

                        self.Dialog("You are... so familiar...", 0, 10);
                        return true;

                    case 5:
                        self.Dialog_NoLinger("Oh, hello there!");

                        self.Dialog_Wait(10);

                        self.Dialog_NoLinger("...are you alright, little one? The modifications to your body are quite... unusual.");

                        self.Dialog_Wait(5);

                        self.Dialog_NoLinger("Sorry, I shouldn't be so intrusive, I am in quite the state myself after all.");

                        self.Dialog_Wait(5);

                        self.Dialog("Ah... you seem, familiar...? ...but in such a strange way...", 0, 5);

                        self.Dialog("A scholar with the ability to manipulate pearls...? Oh... yes! I remember!", 0, 5);

                        self.Dialog("...but, you are not like I remember them? Am I misremembering so badly?", 0, 5);

                        self.Dialog_Wait(10);

                        self.Dialog("...did they have a child? ...could you be?", 0, 5);

                        self.Dialog_Wait(10);

                        self.Dialog("Well, if my memory is serving me correctly, it is nice to see you again... strange scholar.", 0, 5);

                        self.Dialog("Has it really been so long...? And considering your current state, you've been through a lot...", 0, 5);

                        self.Dialog_NoLinger("Ah, I won't lie... I am curious as to how you ended up back here, and what happened on your travels...");

                        self.Dialog_NoLinger("...and what happened to your parent...");

                        self.Dialog_Wait(5);

                        self.Dialog("Ah, I'm sorry... it's wrong to be so invasive, and I shouldn't be so sentimental anyways.", 0, 5);

                        self.Dialog("I won't be going anywhere any time soon... if you need company, you know who to visit!", 0, 5);
                        return true;

                    default:
                        orig(self);
                        return true;
                }
            }

            if (self.id == Conversation.ID.MoonSecondPostMarkConversation)
            {
                switch (Mathf.Clamp(self.State.neuronsLeft, 0, 5))
                {
                    case 4:
                        if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Likes)
                        {
                            self.Dialog("Hello! Strange scholar, it is good to see you!", 30, 0);

                            self.Dialog_NoLinger("Was it really so long ago that you were only half my height...?");

                            self.Dialog_NoLinger("I'm still more than happy to read any pearls you bring me;<LINE>especially given your unique abilities.");

                            self.Dialog_NoLinger("Sorry if I seem a little slow by the way, we are designed to operate with millions of neurons, as opposed to, well...");
                            return true;
                        }

                        if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Dislikes)
                        {
                            self.Dialog("You...", 30, 0);

                            self.Dialog_NoLinger("What do you want? To take away more of my life...?");

                            self.Dialog_NoLinger("Look at yourself! You know how it feels, to be within an inch of death...");

                            self.Dialog_NoLinger("I... I have nothing more to say...");
                            return true;
                        }

                        self.Dialog("Hello again, strange scholar...", 30, 0);

                        self.Dialog("Please try not to push me too hard; with the number of neurons I have... let's just say my mind gets a little foggy.", 30, 0);
                        return true;

                    case 5:
                        if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Dislikes)
                        {
                            self.Dialog("You... I still remember what you did.", 0, 10);

                            self.Dialog("It would be pointless to ask why...", 0, 10);

                            self.Dialog("I've accepted my fate - and one day, you will meet an end too...", 0, 10);
                            return true;
                        }

                        if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Likes)
                        {
                            self.Dialog("Hello again, strange scholar!", 0, 10);

                            self.Dialog_NoLinger("I do so wish you could tell me what happened on your travels... I hardly remember the last time I saw beyond this chamber...");

                            self.Dialog_NoLinger("Oh... the memories can hurt a little... but I shouldn't well on them.");

                            if (ModManager.MSC && self.myBehavior.CheckSlugpupsInRoom())
                            {
                                self.Dialog("You and your family are always welcome here - please visit often!", 0, 5);
                                return true;
                            }

                            else if (ModManager.MMF && self.myBehavior.CheckStrayCreatureInRoom() != CreatureTemplate.Type.StandardGroundCreature)
                            {
                                self.Dialog("The company of you and your friend makes my day, strange scholar.", 0, 5);

                                self.Dialog("You're more than welcome to stay a while... your ability will always be a miracle to me...", 0, 5);
                                return true;
                            }

                            self.Dialog("I'll always enjoy your company... it gets lonely out here.", 0, 5);

                            self.Dialog_NoLinger("If you happen to have a moment to spare, I'd be more than happy to read those pearls...<LINE>There is not much else to do to pass the time.");
                            return true;
                        }

                        self.Dialog("Oh, hello strange scholar!", 0, 10);

                        self.Dialog_NoLinger("You really do remind me so much of your mother...");
                        return true;

                    default:
                        orig(self);
                        return true;
                }
            }
        }

        // Moon met Pearlcat & sick Pearlpup
        else
        {
            if (self.id == Conversation.ID.MoonFirstPostMarkConversation)
            {
                switch (Mathf.Clamp(self.State.neuronsLeft, 0, 5))
                {
                    case 2:
                        self.Dialog("...hello?", 30, 10);

                        self.Dialog("...familiar ...help... please...", 0, 10);
                        return true;

                    case 3:
                        self.Dialog("Who... who is there?", 30, 10);

                        self.Dialog("I have so little... please don't take... this... this is all I have...", 0, 10);

                        self.Dialog_Wait(10);

                        self.Dialog("You are... so familiar...", 0, 10);

                        self.Dialog("...scholar?", 0, 10);
                        return true;

                    case 5:
                        self.Dialog_NoLinger("Oh, hello! Hello...");

                        self.Dialog_Wait(10);

                        self.Dialog_NoLinger("...are you alright, little one? The modifications to your body are quite... extreme.");

                        self.Dialog_Wait(5);

                        self.Dialog_NoLinger("Sorry, I shouldn't be so intrusive, I am in quite the state myself after all.");

                        self.Dialog_Wait(5);

                        self.Dialog("Ah... you seem, familiar...? ...but in such a strange way...", 0, 5);

                        self.Dialog("A scholar with the ability to manipulate pearls...? Oh... yes! I remember!", 0, 5);

                        self.Dialog("...but, you are not like I remember them? Am I misremembering so badly?", 0, 5);

                        self.Dialog_Wait(10);

                        self.Dialog("...did they have a child? ...could you be?", 0, 5);

                        self.Dialog_Wait(10);

                        self.Dialog("Is that you, little scholar? I'm speechless...", 0, 5);

                        self.Dialog("Has it really been so long...? And considering your current state, you've been through a lot...", 0, 5);

                        self.Dialog_NoLinger("I won't lie, I'm so curious as to how you... survived, and how you ended up back here...?");

                        self.Dialog_NoLinger("...and what happened to your parent...");

                        self.Dialog_Wait(5);

                        self.Dialog("I'm sorry... it's wrong to be so invasive, it's none of my business really.", 0, 5);

                        self.Dialog("I'm just so glad you're safe, too many things have come into my memory, only to be washed away...", 0, 5);

                        self.Dialog("Please, stay as long as you'd like, I can't begin to imagine what you've been through.", 0, 5);

                        self.Dialog("I still don't have much to offer, sadly... aside from my pearl readings, and company of course! ~", 0, 5);
                        return true;

                    default:
                        orig(self);
                        return true;
                }
            }

            if (self.id == Conversation.ID.MoonSecondPostMarkConversation)
            {
                switch (Mathf.Clamp(self.State.neuronsLeft, 0, 5))
                {
                    case 4:
                        if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Likes)
                        {
                            self.Dialog("Hello! Strange scholar, it is good to see you!", 30, 0);

                            self.Dialog_NoLinger("Was it really so long ago that you were only half my height...?");

                            self.Dialog_NoLinger("I'm still more than happy to read any pearls you bring me;<LINE>especially given your unique abilities.");

                            self.Dialog_NoLinger("Sorry if I seem a little slow by the way, we are designed to operate with millions of neurons, as opposed to, well...");
                            return true;
                        }

                        if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Dislikes)
                        {
                            self.Dialog("You...", 30, 0);

                            self.Dialog_NoLinger("What do you want? To take away more of my life...?");

                            self.Dialog_NoLinger("Look at yourself! You know how it feels, to be within an inch of death...");

                            self.Dialog_NoLinger("I... I have nothing more to say...");
                            return true;
                        }

                        self.Dialog("Hello again, strange scholar...", 30, 0);

                        self.Dialog("Please try not to push me too hard; with the number of neurons I have... let's just say my mind gets a little foggy.", 30, 0);
                        return true;

                    case 5:
                        if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Dislikes)
                        {
                            self.Dialog("You... I still remember what you did.", 0, 10);

                            self.Dialog("It would be pointless to ask why...", 0, 10);

                            self.Dialog("I've accepted my fate - and one day, you will meet an end too...", 0, 10);
                            return true;
                        }

                        if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Likes)
                        {
                            self.Dialog("Hello again, strange scholar!", 0, 10);

                            self.Dialog_NoLinger("I do so wish you could tell me what happened on your travels... I hardly remember the last time I saw beyond this chamber...");

                            self.Dialog_NoLinger("Oh... the memories can hurt a little... but I shouldn't well on them.");

                            if (ModManager.MSC && self.myBehavior.CheckSlugpupsInRoom())
                            {
                                self.Dialog("You and your family are always welcome here - please visit often!", 0, 5);
                                return true;
                            }

                            else if (ModManager.MMF && self.myBehavior.CheckStrayCreatureInRoom() != CreatureTemplate.Type.StandardGroundCreature)
                            {
                                self.Dialog("The company of you and your friend makes my day, strange scholar.", 0, 5);

                                self.Dialog("You're more than welcome to stay a while... your ability will always be a little miracle to me...", 0, 5);
                                return true;
                            }

                            self.Dialog("I'll always enjoy your company... it gets lonely out here.", 0, 5);

                            self.Dialog_NoLinger("If you happen to have a moment to spare, I'd be more than happy to read those pearls...<LINE>There is not much else to do to pass the time.");
                            return true;
                        }

                        self.Dialog("Oh, hello strange scholar!", 0, 10);

                        self.Dialog_NoLinger("You really do remind me so much of your mother...");
                        return true;

                    default:
                        orig(self);
                        return true;
                }
            }
        }

        return false;
    }
}