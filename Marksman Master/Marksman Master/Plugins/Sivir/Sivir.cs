﻿#region Licensing
// ---------------------------------------------------------------------
// <copyright file="Sivir.cs" company="EloBuddy">
// 
// Marksman Master
// Copyright (C) 2016 by gero
// All rights reserved
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see http://www.gnu.org/licenses/. 
// </copyright>
// <summary>
// 
// Email: geroelobuddy@gmail.com
// PayPal: geroelobuddy@gmail.com
// </summary>
// ---------------------------------------------------------------------
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Constants;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
using EloBuddy.SDK.Rendering;
using Marksman_Master.Utils;

namespace Marksman_Master.Plugins.Sivir
{
    internal class Sivir : ChampionPlugin
    {
        protected static Spell.Skillshot Q { get; }
        protected static Spell.Active W { get; }
        protected static Spell.Active E { get; }
        protected static Spell.Active R { get; }

        internal static Menu ComboMenu { get; set; }
        internal static Menu HarassMenu { get; set; }
        internal static Menu LaneClearMenu { get; set; }
        internal static Menu DrawingsMenu { get; set; }
        internal static Menu SpellBlockerMenu { get; set; }

        private static readonly ColorPicker[] ColorPicker;
        private static bool _changingRangeScan;

        protected static bool IsPostAttack { get; private set; }

        static Sivir()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 1200, SkillShotType.Linear, 250, 1350, 90)
            {
                AllowedCollisionCount = int.MaxValue
            };
            W = new Spell.Active(SpellSlot.W);
            E = new Spell.Active(SpellSlot.E);
            R = new Spell.Active(SpellSlot.R);

            ColorPicker = new ColorPicker[1];

            ColorPicker[0] = new ColorPicker("SivirQ", new ColorBGRA(243, 109, 160, 255));

            ChampionTracker.Initialize(ChampionTrackerFlags.LongCastTimeTracker);

            BlockableSpells.Initialize();
            BlockableSpells.OnBlockableSpell += BlockableSpells_OnBlockableSpell;
            Game.OnPostTick += args => IsPostAttack = false;
            Orbwalker.OnPostAttack += (s, a) => IsPostAttack = true;

            ChampionTracker.OnLongSpellCast += ChampionTracker_OnLongSpellCast;
        }

        private static void ChampionTracker_OnLongSpellCast(object sender, OnLongSpellCastEventArgs e)
        {
            if (e.IsTeleport)
                return;

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && Q.IsReady() && Settings.Combo.UseQ && (Player.Instance.Mana - 60 > (R.IsReady() ? 100 : 0)))
            {
                Q.CastMinimumHitchance(e.Sender, 65);
            }
        }

        private static void BlockableSpells_OnBlockableSpell(AIHeroClient sender,
            BlockableSpells.OnBlockableSpellEventArgs args)
        {
            if (!args.Enabled || !E.IsReady())
                return;

            E.Cast();
        }

        protected override void OnDraw()
        {
            if (_changingRangeScan)
                Circle.Draw(Color.White,
                    LaneClearMenu["Plugins.Sivir.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue, Player.Instance);

            if (Settings.Drawings.DrawQ && (!Settings.Drawings.DrawSpellRangesWhenReady || Q.IsReady()))
                Circle.Draw(ColorPicker[0].Color, Q.Range, Player.Instance);
        }
        protected override void OnInterruptible(AIHeroClient sender, InterrupterEventArgs args)
        {
        }

        protected override void OnGapcloser(AIHeroClient sender, GapCloserEventArgs args)
        {
        }

        protected override void CreateMenu()
        {
            ComboMenu = MenuManager.Menu.AddSubMenu("Combo");
            ComboMenu.AddGroupLabel("Combo mode settings for Sivir addon");

            ComboMenu.AddLabel("Boomerang Blade (Q) settings :");
            ComboMenu.Add("Plugins.Sivir.ComboMenu.UseQ", new CheckBox("Use Q"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Ricochet (W) settings :");
            ComboMenu.Add("Plugins.Sivir.ComboMenu.UseW", new CheckBox("Use W"));
            ComboMenu.AddSeparator(5);

            HarassMenu = MenuManager.Menu.AddSubMenu("Harass");
            HarassMenu.AddGroupLabel("Harass mode settings for Sivir addon");

            HarassMenu.AddLabel("Boomerang Blade (Q) settings :");
            HarassMenu.Add("Plugins.Sivir.HarassMenu.UseQ", new CheckBox("Use Q"));
            HarassMenu.Add("Plugins.Sivir.HarassMenu.AutoHarass", new CheckBox("Auto harass immobile targets"));
            HarassMenu.Add("Plugins.Sivir.HarassMenu.MinManaQ", new Slider("Min mana percentage ({0}%) to use Q", 80, 1));
            HarassMenu.AddSeparator(5);

            HarassMenu.AddLabel("Ricochet (W) settings :");
            HarassMenu.Add("Plugins.Sivir.HarassMenu.UseW", new CheckBox("Use W"));
            HarassMenu.Add("Plugins.Sivir.HarassMenu.MinManaW", new Slider("Min mana percentage ({0}%) to use W", 80, 1));
            HarassMenu.AddSeparator(5);

            LaneClearMenu = MenuManager.Menu.AddSubMenu("Clear");
            LaneClearMenu.AddGroupLabel("Clear mode settings for Sivir addon");

            LaneClearMenu.AddLabel("Basic settings :");
            LaneClearMenu.Add("Plugins.Sivir.LaneClearMenu.EnableLCIfNoEn", new CheckBox("Enable lane clear only if no enemies nearby"));
            var scanRange = LaneClearMenu.Add("Plugins.Sivir.LaneClearMenu.ScanRange", new Slider("Range to scan for enemies", 1500, 300, 2500));
            scanRange.OnValueChange += (a, b) =>
            {
                _changingRangeScan = true;
                Core.DelayAction(() =>
                {
                    if (!scanRange.IsLeftMouseDown && !scanRange.IsMouseInside)
                    {
                        _changingRangeScan = false;
                    }
                }, 2000);
            };
            LaneClearMenu.Add("Plugins.Sivir.LaneClearMenu.AllowedEnemies", new Slider("Allowed enemies amount", 1, 0, 5));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Boomerang Blade (Q) settings :");
            LaneClearMenu.Add("Plugins.Sivir.LaneClearMenu.UseQInLaneClear", new CheckBox("Use Q in Lane Clear"));
            LaneClearMenu.Add("Plugins.Sivir.LaneClearMenu.UseQInJungleClear", new CheckBox("Use Q in Jungle Clear"));
            LaneClearMenu.Add("Plugins.Sivir.LaneClearMenu.MinManaQ", new Slider("Min mana percentage ({0}%) to use Q", 80, 1));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Ricochet (W) settings :");
            LaneClearMenu.Add("Plugins.Sivir.LaneClearMenu.UseWInLaneClear", new CheckBox("Use W in Lane Clear"));
            LaneClearMenu.Add("Plugins.Sivir.LaneClearMenu.UseWInJungleClear", new CheckBox("Use W in Jungle Clear"));
            LaneClearMenu.Add("Plugins.Sivir.LaneClearMenu.MinManaW", new Slider("Min mana percentage ({0}%) to use W", 80, 1));

            BlockableSpells.BuildMenu();

            DrawingsMenu = MenuManager.Menu.AddSubMenu("Drawings");
            DrawingsMenu.AddGroupLabel("Drawings settings for Sivir addon");

            DrawingsMenu.AddLabel("Basic settings :");
            DrawingsMenu.Add("Plugins.Sivir.DrawingsMenu.DrawSpellRangesWhenReady",
                new CheckBox("Draw spell ranges only when they are ready"));
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Boomerang Blade (Q) settings :");
            DrawingsMenu.Add("Plugins.Sivir.DrawingsMenu.DrawQ", new CheckBox("Draw Q range", false));
            DrawingsMenu.Add("Plugins.Twitch.DrawingsMenu.DrawQColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[0].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
        }

        protected override void PermaActive()
        {
            Modes.PermaActive.Execute();
        }

        protected override void ComboMode()
        {
            Modes.Combo.Execute();
        }

        protected override void HarassMode()
        {
            Modes.Harass.Execute();
        }

        protected override void LaneClear()
        {
            Modes.LaneClear.Execute();
        }

        protected override void JungleClear()
        {
            Modes.JungleClear.Execute();
        }

        protected override void LastHit()
        {
            Modes.LastHit.Execute();
        }

        protected override void Flee()
        {
            Modes.Flee.Execute();
        }

        protected static class Settings
        {
            internal static class Combo
            {
                public static bool UseQ => MenuManager.MenuValues["Plugins.Sivir.ComboMenu.UseQ"];

                public static bool UseW => MenuManager.MenuValues["Plugins.Sivir.ComboMenu.UseW"];
            }

            internal static class Harass
            {
                public static bool UseQ => MenuManager.MenuValues["Plugins.Sivir.HarassMenu.UseQ"];

                public static bool AutoHarass => MenuManager.MenuValues["Plugins.Sivir.HarassMenu.AutoHarass"];

                public static int MinManaQ => MenuManager.MenuValues["Plugins.Sivir.HarassMenu.MinManaQ", true];

                public static bool UseW => MenuManager.MenuValues["Plugins.Sivir.HarassMenu.UseW"];

                public static int MinManaW => MenuManager.MenuValues["Plugins.Sivir.HarassMenu.MinManaW", true];
            }

            internal static class LaneClear
            {
                public static bool EnableIfNoEnemies => MenuManager.MenuValues["Plugins.Sivir.LaneClearMenu.EnableLCIfNoEn"];

                public static int ScanRange => MenuManager.MenuValues["Plugins.Sivir.LaneClearMenu.ScanRange", true];

                public static int AllowedEnemies => MenuManager.MenuValues["Plugins.Sivir.LaneClearMenu.AllowedEnemies", true];

                public static bool UseQInLaneClear => MenuManager.MenuValues["Plugins.Sivir.LaneClearMenu.UseQInLaneClear"];

                public static bool UseQInJungleClear => MenuManager.MenuValues["Plugins.Sivir.LaneClearMenu.UseQInJungleClear"];

                public static int MinManaQ => MenuManager.MenuValues["Plugins.Sivir.LaneClearMenu.MinManaQ", true];

                public static bool UseWInLaneClear => MenuManager.MenuValues["Plugins.Sivir.LaneClearMenu.UseWInLaneClear"];

                public static bool UseWInJungleClear => MenuManager.MenuValues["Plugins.Sivir.LaneClearMenu.UseWInJungleClear"];

                public static int WMinMana => MenuManager.MenuValues["Plugins.Sivir.LaneClearMenu.MinManaW", true];
            }

            internal static class Drawings
            {
                public static bool DrawSpellRangesWhenReady => MenuManager.MenuValues["Plugins.Sivir.DrawingsMenu.DrawSpellRangesWhenReady"];
                
                public static bool DrawQ => MenuManager.MenuValues["Plugins.Sivir.DrawingsMenu.DrawQ"];
            }
        }

        protected static class BlockableSpells
        {
            private static readonly HashSet<BlockableSpellData> BlockableSpellsHashSet = new HashSet<BlockableSpellData>
            {
                new BlockableSpellData(Champion.Alistar, "[Q] Headbutt", SpellSlot.W),
                new BlockableSpellData(Champion.Amumu, "[R] Curse of the Sad Mummy", SpellSlot.R) {NeedsAdditionalLogics = true},
                new BlockableSpellData(Champion.Anivia, "[E] Frostbite", SpellSlot.E),
                new BlockableSpellData(Champion.Akali, "[Q] Mark of the Assassin", SpellSlot.Q),
                new BlockableSpellData(Champion.Akali, "[E] Crescent Slash", SpellSlot.E) {NeedsAdditionalLogics = true},
                new BlockableSpellData(Champion.Akali, "[R] Shadow Dance", SpellSlot.R),
                new BlockableSpellData(Champion.Annie, "[Q] Disintegrate", SpellSlot.Q),
                new BlockableSpellData(Champion.Azir, "[R] Emperor's Divide", SpellSlot.R) {NeedsAdditionalLogics = true},
                new BlockableSpellData(Champion.Bard, "[R] Tempered Fate", SpellSlot.R) {NeedsAdditionalLogics = true},
                new BlockableSpellData(Champion.Blitzcrank, "[R] Power Fist", SpellSlot.E)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "powerfistattack"
                },
                new BlockableSpellData(Champion.Brand, "[R] Pyroclasm", SpellSlot.R),
                new BlockableSpellData(Champion.Braum, "[Passive] Concussive Blows", SpellSlot.Unknown) {NeedsAdditionalLogics = true, AdditionalBuffName = "braumbasicattackpassiveoverride"},
                new BlockableSpellData(Champion.Caitlyn, "[R] Ace in the Hole", SpellSlot.R) {NeedsAdditionalLogics = true},
                new BlockableSpellData(Champion.Chogath, "[R] Feast", SpellSlot.R),
                new BlockableSpellData(Champion.Darius, "[R] Noxian Guillotine", SpellSlot.R),
                new BlockableSpellData(Champion.Diana, "[E] Moonfall", SpellSlot.E) {NeedsAdditionalLogics = true},
                new BlockableSpellData(Champion.Diana, "[R] Lunar Rush", SpellSlot.R),
                new BlockableSpellData(Champion.FiddleSticks, "[Q] Terrify", SpellSlot.Q),
                new BlockableSpellData(Champion.Fiora, "[R] Grand Challenge", SpellSlot.R),
                new BlockableSpellData(Champion.Galio, "[R] Idol of Durand", SpellSlot.R),
                new BlockableSpellData(Champion.Gangplank, "[Q] Parrrley", SpellSlot.Q),
                new BlockableSpellData(Champion.Garen, "[Q] Decisive Strike", SpellSlot.Q)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "garenqattack"
                },
                new BlockableSpellData(Champion.Garen, "[R] Demacian Justice", SpellSlot.R),
                new BlockableSpellData(Champion.Gragas, "[W] Drunken Rage", SpellSlot.W)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "drunkenrage"
                },
                new BlockableSpellData(Champion.Hecarim, "[E] Devastating Charge", SpellSlot.E)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "hecarimrampattack"
                },
                //new BlockableSpellData(Champion.Illaoi, "", SpellSlot.W),
                new BlockableSpellData(Champion.Irelia, "[E] Equilibrium Strike", SpellSlot.E),
                new BlockableSpellData(Champion.Janna, "[W] Zephyr", SpellSlot.W),
                new BlockableSpellData(Champion.JarvanIV, "[R] Cataclysm", SpellSlot.R),
                new BlockableSpellData(Champion.Jayce, "[E] Thundering Blow", SpellSlot.E),
                new BlockableSpellData(Champion.Jhin, "[Q] Dancing Grenade", SpellSlot.Q),
                new BlockableSpellData(Champion.Kalista, "[E] Rend", SpellSlot.E) {NeedsAdditionalLogics = true},
                //new BlockableSpellData(Champion.Karma, "", SpellSlot.W)
                //{
                    //NeedsAdditionalLogics = true,
                    //AdditionalDelay = 1800
                //},
                new BlockableSpellData(Champion.Karthus, "[R] Requiem", SpellSlot.R)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalDelay = 2800
                },
                new BlockableSpellData(Champion.Kassadin, "[Q] Null Sphere", SpellSlot.Q),
                new BlockableSpellData(Champion.Katarina, "[Q] Bouncing Blades", SpellSlot.Q),
                new BlockableSpellData(Champion.Kayle, "[Q] Reckoning", SpellSlot.Q),
                new BlockableSpellData(Champion.Kennen, "[W] Electrical Surge", SpellSlot.W),
                new BlockableSpellData(Champion.Khazix, "[Q] Taste Their Fear", SpellSlot.Q),
                new BlockableSpellData(Champion.Kindred, "[E] Mounting Dread", SpellSlot.E),
                new BlockableSpellData(Champion.KogMaw, "[Passive] Icathian Surprise", SpellSlot.Unknown)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalDelay = 3800
                },
                new BlockableSpellData(Champion.Leblanc, "[R] Mimic", SpellSlot.R),
                new BlockableSpellData(Champion.LeeSin, "[R] Dragon's Rage", SpellSlot.R),
                new BlockableSpellData(Champion.Leona, "[Q] Shield of Daybreak", SpellSlot.Q)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "leonashieldofdaybreakattack"
                },
                new BlockableSpellData(Champion.Lucian, "[Q] Piercing Light", SpellSlot.Q),
                new BlockableSpellData(Champion.Malphite, "[Q] Seismic Shard", SpellSlot.Q),
                new BlockableSpellData(Champion.Malzahar, "[R] Malefic Visions", SpellSlot.E),
                new BlockableSpellData(Champion.Maokai, "[W] Twisted Advance", SpellSlot.W),
                new BlockableSpellData(Champion.MasterYi, "[Q] Alpha Strike", SpellSlot.Q),
                new BlockableSpellData(Champion.MissFortune, "[Q] Double Up", SpellSlot.Q),
                new BlockableSpellData(Champion.Mordekaiser, "[R] Children of the Grave", SpellSlot.R),
                new BlockableSpellData(Champion.Morgana, "[R] Soul Shackles", SpellSlot.R),
                new BlockableSpellData(Champion.Nautilus, "[R] Depth Charge", SpellSlot.R) {NeedsAdditionalLogics = true},
                new BlockableSpellData(Champion.Nasus, "[Q] Siphoning Strike", SpellSlot.Q)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "nasusqattack"
                },
                new BlockableSpellData(Champion.Nasus, "[W] Wither", SpellSlot.W),
                new BlockableSpellData(Champion.Nidalee, "[Q] Takedown", SpellSlot.Q)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "nidaleetakedownattack"
                },
                new BlockableSpellData(Champion.Nunu, "[E] Ice Blast", SpellSlot.E),
                new BlockableSpellData(Champion.Pantheon, "[W] Aegis of Zeonia", SpellSlot.W),
                new BlockableSpellData(Champion.Poppy, "[E] Heroic Charge", SpellSlot.E),
                new BlockableSpellData(Champion.Quinn, "[E] Vault", SpellSlot.E),
                new BlockableSpellData(Champion.Rammus, "[E] Puncturing Taunt", SpellSlot.E),
                new BlockableSpellData(Champion.Renekton, "[W] Cull the Meek", SpellSlot.W)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "renektonexecute"
                },
                new BlockableSpellData(Champion.Renekton, "[Empowered W] Cull the Meek", SpellSlot.W)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "renektonsuperexecute"
                },
                new BlockableSpellData(Champion.Ryze, "[W] Rune Prison", SpellSlot.W),
                new BlockableSpellData(Champion.Sejuani, "[E] Flail of the Northern Winds", SpellSlot.E),
                new BlockableSpellData(Champion.Shaco, "[E] Two-Shiv Poison", SpellSlot.E),
                new BlockableSpellData(Champion.Shyvana, "[Q] Twin Bite", SpellSlot.Q)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "shyvanadoubleattackhit"
                },
                new BlockableSpellData(Champion.Singed, "[E] Fling", SpellSlot.E),
                new BlockableSpellData(Champion.Skarner, "[R] Impale", SpellSlot.R),
                new BlockableSpellData(Champion.Swain, "[E] Torment", SpellSlot.E),
                new BlockableSpellData(Champion.Syndra, "[R] Unleashed Power", SpellSlot.R),
                new BlockableSpellData(Champion.TahmKench, "[W] Devour", SpellSlot.W),
                new BlockableSpellData(Champion.Teemo, "[Q] Blinding Dart", SpellSlot.Q),
                new BlockableSpellData(Champion.Tristana, "[E] Explosive Charge", SpellSlot.E)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalDelay = 3800
                },
                new BlockableSpellData(Champion.Tristana, "[R] Buster Shot", SpellSlot.R),
                new BlockableSpellData(Champion.Trundle, "[R] Subjugate", SpellSlot.R),
                new BlockableSpellData(Champion.TwistedFate, "[W] Pick A Card", SpellSlot.W)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "goldcardpreattack"
                },
                new BlockableSpellData(Champion.Twitch, "[E] Contaminate", SpellSlot.E),
                new BlockableSpellData(Champion.Udyr, "[E] Bear Stance", SpellSlot.E)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "udyrbearattack"
                },
                new BlockableSpellData(Champion.Urgot, "[R] Hyper-Kinetic Position Reverser", SpellSlot.R),
                new BlockableSpellData(Champion.Vayne, "[E] Condemn", SpellSlot.E),
                new BlockableSpellData(Champion.Veigar, "[R] Primordial Burst", SpellSlot.R),
                new BlockableSpellData(Champion.Vi, "[R] Assault and Battery", SpellSlot.R) {NeedsAdditionalLogics = true},
                new BlockableSpellData(Champion.Vladimir, "[Q] Transfusion", SpellSlot.Q),
                new BlockableSpellData(Champion.Volibear, "[Q] Rolling Thunder", SpellSlot.Q)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "volibearqattack"
                },
                new BlockableSpellData(Champion.Volibear, "[W] Frenzy", SpellSlot.W),
                new BlockableSpellData(Champion.XinZhao, "[Q] Three Talon Strike", SpellSlot.Q)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "xenzhaothrust3"
                },
                new BlockableSpellData(Champion.XinZhao, "[R] Crescent Sweep", SpellSlot.R),
                new BlockableSpellData(Champion.Zed, "[R] Death Mark", SpellSlot.R) {NeedsAdditionalLogics = true, AdditionalDelay = 200}
            };

            public delegate void OnBlockableSpellEvent(AIHeroClient sender, OnBlockableSpellEventArgs args);

            public static event OnBlockableSpellEvent OnBlockableSpell;

            public static void Initialize()
            {
                BlockableSpellsHashSet.RemoveWhere(x => EntityManager.Heroes.Enemies.All(k => k.Hero != x.ChampionName));

                if (BlockableSpellsHashSet.Count > 0)
                {
                    Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
                    Game.OnTick += Game_OnTick;
                    Obj_AI_Base.OnBasicAttack += Obj_AI_Base_OnBasicAttack;
                }
            }

            public static void BuildMenu()
            {
                if (BlockableSpellsHashSet.Count < 1)
                    return;

                SpellBlockerMenu = MenuManager.Menu.AddSubMenu("Spell blocker");
                SpellBlockerMenu.AddGroupLabel("Spell blocker settings for Sivir addon");
                SpellBlockerMenu.Add("Plugins.Sivir.SpellBlockerMenu.Enabled", new CheckBox("Enable Spell blocker"));

                SpellBlockerMenu.AddLabel("Spell blocker enabled for :");
                SpellBlockerMenu.AddSeparator(5);

                foreach (var enemy in EntityManager.Heroes.Enemies.Where(x=> BlockableSpellsHashSet.Any(k=>k.ChampionName == x.Hero)))
                {
                    SpellBlockerMenu.AddLabel(enemy.ChampionName + " :");

                    foreach (var spell in BlockableSpellsHashSet.Where(x => x.ChampionName == enemy.Hero))
                    {
                        SpellBlockerMenu.Add(
                            "Plugins.Sivir.SpellBlockerMenu.Enabled." + spell.ChampionName + "." + spell.SpellSlot,
                            new CheckBox(spell.ChampionName + " | " + spell.SpellName));
                    }
                    SpellBlockerMenu.AddSeparator(2);
                }
            }

            public static bool IsEnabledFor(AIHeroClient unit, SpellSlot slot)
                => MenuManager.MenuValues["Plugins.Sivir.SpellBlockerMenu.Enabled"] &&
                   MenuManager.MenuValues["Plugins.Sivir.SpellBlockerMenu.Enabled." + unit.ChampionName + "." + slot];
            
            private static void Obj_AI_Base_OnBasicAttack(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
            {
                var enemy = sender as AIHeroClient;

                if (enemy == null || args.Target == null || !args.Target.IsMe)
                    return;

                if (enemy.Hero == Champion.Tristana)
                {
                    var trist = EntityManager.Heroes.Enemies.Find(x => x.Hero == Champion.Tristana);
                    var buff = Player.Instance.Buffs.FirstOrDefault(x => x.Name.ToLowerInvariant() == "tristanaecharge");
                    if (buff != null && buff.Count >= 3)
                    {
                        OnBlockableSpell?.Invoke(trist,
                            new OnBlockableSpellEventArgs(trist.Hero, SpellSlot.E, IsEnabledFor(trist, SpellSlot.E),
                                false, 0));
                    }
                }

                foreach (var blockableSpellData in BlockableSpellsHashSet.Where(x => x.ChampionName == enemy.Hero && !string.IsNullOrWhiteSpace(x.AdditionalBuffName) && x.AdditionalBuffName == args.SData.Name.ToLowerInvariant()))
                {

                    OnBlockableSpell?.Invoke(enemy,
                        new OnBlockableSpellEventArgs(enemy.Hero, args.Slot, IsEnabledFor(enemy, blockableSpellData.SpellSlot), true,
                            blockableSpellData.AdditionalDelay));
                }
            }

            private static void Game_OnTick(EventArgs args)
            {
                if (EntityManager.Heroes.Enemies.Any(x => x.Hero == Champion.KogMaw))
                {
                    var enemy = EntityManager.Heroes.Enemies.Find(x => x.Hero == Champion.KogMaw);
                    var buff = enemy.Buffs.FirstOrDefault(x => x.Name.ToLowerInvariant() == "kogmawicathiansurprise");
                    if (buff != null && (buff.EndTime - Game.Time) * 1000 < 300 && enemy.Distance(Player.Instance) < 370)
                    {
                        OnBlockableSpell?.Invoke(enemy,
                        new OnBlockableSpellEventArgs(enemy.Hero, SpellSlot.Unknown, IsEnabledFor(enemy, SpellSlot.Unknown), false, 0));
                    }
                }
                if (EntityManager.Heroes.Enemies.Any(x => x.Hero == Champion.Karthus))
                {
                    var enemy = EntityManager.Heroes.Enemies.Find(x => x.Hero == Champion.Karthus);
                    var buff = Player.Instance.Buffs.FirstOrDefault(x => x.Name.ToLowerInvariant() == "karthusfallenonetarget");
                    if (buff != null && (buff.EndTime - Game.Time) * 1000 < 300)
                    {
                        OnBlockableSpell?.Invoke(enemy,
                            new OnBlockableSpellEventArgs(enemy.Hero, SpellSlot.R, IsEnabledFor(enemy, SpellSlot.R), false, 0));
                    }
                }
                if (EntityManager.Heroes.Enemies.Any(x => x.Hero == Champion.Tristana))
                {
                    var enemy = EntityManager.Heroes.Enemies.Find(x => x.Hero == Champion.Tristana);
                    var buff = Player.Instance.Buffs.FirstOrDefault(x => x.Name.ToLowerInvariant() == "tristanaecharge");
                    if (buff != null && (buff.EndTime - Game.Time) * 1000 < 300)
                    {
                        OnBlockableSpell?.Invoke(enemy,
                            new OnBlockableSpellEventArgs(enemy.Hero, SpellSlot.E, IsEnabledFor(enemy, SpellSlot.E), false, 0));
                    }
                }
            }

            private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
            {
                if (BlockableSpellsHashSet.Count == 0)
                {
                    Console.WriteLine("[DEBUG] Not found any spells that can be blocked ...");
                    return;
                }

                var enemy = sender as AIHeroClient;

                if (enemy == null)
                    return;

                foreach (var blockableSpellData in BlockableSpellsHashSet.Where(data => data.ChampionName == enemy.Hero))
                {
                    if (blockableSpellData.NeedsAdditionalLogics == false && args.Target != null && args.Target.IsMe && args.Slot == blockableSpellData.SpellSlot)
                    {
                        OnBlockableSpell?.Invoke(enemy, new OnBlockableSpellEventArgs(enemy.Hero, args.Slot, IsEnabledFor(enemy, blockableSpellData.SpellSlot), false, 0));
                    }
                    else if (blockableSpellData.NeedsAdditionalLogics)
                    {
                        if (args.SData.IsAutoAttack() && args.Target != null && args.Target.IsMe && !string.IsNullOrWhiteSpace(blockableSpellData.AdditionalBuffName) &&
                            blockableSpellData.AdditionalBuffName == args.SData.Name.ToLowerInvariant())
                        {
                            OnBlockableSpell?.Invoke(enemy, new OnBlockableSpellEventArgs(enemy.Hero, args.Slot, IsEnabledFor(enemy, blockableSpellData.SpellSlot), true, blockableSpellData.AdditionalDelay)); // better db check
                        }
                        else if (enemy.Hero == Champion.Azir && args.Slot == blockableSpellData.SpellSlot)
                        {
                            if (enemy.Distance(Player.Instance) < 300)
                            {
                                OnBlockableSpell?.Invoke(enemy, new OnBlockableSpellEventArgs(enemy.Hero, args.Slot, IsEnabledFor(enemy, blockableSpellData.SpellSlot), false, 0));
                            }
                        }
                        else if (enemy.Hero == Champion.Amumu && args.Slot == blockableSpellData.SpellSlot)
                        {
                            if (enemy.Distance(Player.Instance) < 1100)
                            {
                                OnBlockableSpell?.Invoke(enemy, new OnBlockableSpellEventArgs(enemy.Hero, args.Slot, IsEnabledFor(enemy, blockableSpellData.SpellSlot), false, 0));
                            }
                        }
                        else if (enemy.Hero == Champion.Akali && args.Slot == blockableSpellData.SpellSlot && enemy.Distance(Player.Instance) < 325)
                        {
                            OnBlockableSpell?.Invoke(enemy,
                                new OnBlockableSpellEventArgs(enemy.Hero, args.Slot,
                                    IsEnabledFor(enemy, blockableSpellData.SpellSlot), false, 0));
                        }
                        else if (enemy.Hero == Champion.Bard && args.Slot == blockableSpellData.SpellSlot && new Geometry.Polygon.Circle(args.End, 325).IsInside(Player.Instance))
                        {
                            Core.DelayAction(() => OnBlockableSpell?.Invoke(enemy, new OnBlockableSpellEventArgs(enemy.Hero, args.Slot, IsEnabledFor(enemy, blockableSpellData.SpellSlot), false, 0)), (int)Math.Max(enemy.Distance(Player.Instance) / 2000 * 1000 - 300, 0));
                        }
                        else if (enemy.Hero == Champion.Diana && args.Slot == blockableSpellData.SpellSlot && new Geometry.Polygon.Circle(args.End, 225).IsInside(Player.Instance))
                        {
                            OnBlockableSpell?.Invoke(enemy, new OnBlockableSpellEventArgs(enemy.Hero, args.Slot, IsEnabledFor(enemy, blockableSpellData.SpellSlot), false, 0));
                        }
                        else if (enemy.Hero == Champion.Caitlyn && args.Slot == blockableSpellData.SpellSlot && args.Target != null && args.Target.IsMe)
                        {
                            Core.DelayAction(() => OnBlockableSpell?.Invoke(enemy, new OnBlockableSpellEventArgs(enemy.Hero, args.Slot, IsEnabledFor(enemy, blockableSpellData.SpellSlot), false, 0)), (int)Math.Max(enemy.Distance(Player.Instance) / args.SData.MissileSpeed * 1000 + 500, 0));
                        }
                        else if (enemy.Hero == Champion.Kalista && args.Slot == blockableSpellData.SpellSlot && args.Target != null && args.Target.IsMe)
                        {
                            OnBlockableSpell?.Invoke(enemy, new OnBlockableSpellEventArgs(enemy.Hero, args.Slot, IsEnabledFor(enemy, blockableSpellData.SpellSlot), false, 0));
                        }
                        else if (enemy.Hero == Champion.Karma && args.Slot == blockableSpellData.SpellSlot)
                        {

                        }
                        else if (enemy.Hero == Champion.Nautilus && args.Slot == blockableSpellData.SpellSlot &&
                                 args.Target != null && args.Target.IsMe)
                        {
                            Core.DelayAction(
                                () =>
                                    OnBlockableSpell?.Invoke(enemy,
                                        new OnBlockableSpellEventArgs(enemy.Hero, args.Slot, IsEnabledFor(enemy, blockableSpellData.SpellSlot), false, 0)),
                                (int)
                                    Math.Max(
                                        enemy.Distance(Player.Instance) / args.SData.MissileSpeed * 1000 - 300, 0));
                        }
                        else if (enemy.Hero == Champion.Vi && args.Slot == blockableSpellData.SpellSlot &&
                                 args.Target != null && args.Target.IsMe)
                        {
                            Core.DelayAction(() =>
                            {
                                if (enemy.Distance(Player.Instance) < 350)
                                    OnBlockableSpell?.Invoke(enemy,
                                        new OnBlockableSpellEventArgs(enemy.Hero, args.Slot, IsEnabledFor(enemy, blockableSpellData.SpellSlot), false, 0));

                            },
                                (int)
                                    Math.Max(
                                        enemy.Distance(Player.Instance) / args.SData.MissileSpeed * 1000 - 400,
                                        0));
                        }
                        else if (enemy.Hero == Champion.Zed && args.Slot == blockableSpellData.SpellSlot &&
                                 enemy.Distance(Player.Instance) < 300)
                        {
                            Core.DelayAction(
                                () =>
                                    OnBlockableSpell?.Invoke(enemy,
                                        new OnBlockableSpellEventArgs(enemy.Hero, args.Slot, IsEnabledFor(enemy, blockableSpellData.SpellSlot), false, 0)),
                                300);
                        }
                    }
                }
            }

            public class OnBlockableSpellEventArgs : EventArgs
            {
                public Champion ChampionName { get; private set; }
                public bool IsAutoAttack { get; }
                public SpellSlot SpellSlot { get; }
                public float AdditionalDelay { get; private set; }
                public bool Enabled { get; }

                public OnBlockableSpellEventArgs(Champion championName, SpellSlot spellSlot, bool enabled, bool isAutoAttack, float additionalDelay)
                {
                    ChampionName = championName;
                    SpellSlot = spellSlot;
                    Enabled = enabled;
                    IsAutoAttack = isAutoAttack;
                    AdditionalDelay = additionalDelay;
                }
            }

            private class BlockableSpellData
            {
                public Champion ChampionName { get; }
                public bool NeedsAdditionalLogics { get; set; }
                public string AdditionalBuffName { get; set; }
                public SpellSlot SpellSlot { get; }
                public string SpellName { get; }
                public float AdditionalDelay { get; set; }

                public BlockableSpellData(Champion championName, string spellName, SpellSlot spellSlot)
                {
                    ChampionName = championName;
                    SpellSlot = spellSlot;
                    SpellName = spellName;
                }
            }
        }
    }
}