﻿#region Licensing
// ---------------------------------------------------------------------
// <copyright file="PermaActive.cs" company="EloBuddy">
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
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Spells;
using Marksman_Master.Utils;

namespace Marksman_Master.Plugins.Ashe.Modes
{
    internal class PermaActive : Ashe
    {
        public static void Execute()
        {
            if (R.IsReady() && Settings.Combo.UseR && EntityManager.Heroes.Enemies.Any(x => x.IsValidTarget(Settings.Combo.RMaximumRange) && x.HealthPercent < 50 && !x.HasSpellShield() && !x.HasUndyingBuffA()))
            {
                foreach (var target in EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(Settings.Combo.RMaximumRange)).OrderBy(TargetSelector.GetPriority))
                {
                    var incomingDamage = IncomingDamage.GetIncomingDamage(target);

                    var damage = incomingDamage + Player.Instance.GetSpellDamage(target, SpellSlot.R) - 25;

                    if (target.Hero == Champion.Blitzcrank && !target.HasBuff("BlitzcrankManaBarrierCD") && !target.HasBuff("ManaBarrier"))
                    {
                        damage -= target.Mana / 2;
                    }

                    if (target.Distance(Player.Instance) > Player.Instance.GetAutoAttackRange() + 200 &&
                        target.TotalHealthWithShields(true) < damage)
                    {
                        var rPrediction = Prediction.Manager.GetPrediction(new Prediction.Manager.PredictionInput
                        {
                            CollisionTypes = new HashSet<CollisionType> { CollisionType.ObjAiMinion },
                            Delay = 250,
                            From = Player.Instance.Position,
                            Radius = 120,
                            Range = Settings.Combo.RMaximumRange,
                            RangeCheckFrom = Player.Instance.Position,
                            Speed = R.Speed,
                            Target = target,
                            Type = SkillShotType.Linear
                        });

                        if (rPrediction.HitChance >= HitChance.High)
                        {/*
                            if (rPrediction.HitChance == HitChance.Collision)
                            {
                                var polygon = new Geometry.Polygon.Rectangle(Player.Instance.Position,
                                    rPrediction.CastPosition, 120);
                                
                                if (!EntityManager.Heroes.Enemies.Any(x => polygon.IsInside(x)))
                                {
                                    Console.WriteLine("[DEBUG] Casting R on : {0} to killsteal ! v 1", target.Hero);
                                    R.Cast(rPrediction.CastPosition);
                                }
                            }
                            else
                            {*/
                            Console.WriteLine("[DEBUG] Casting R on : {0} to killsteal ! v 1", target.Hero);
                            R.Cast(rPrediction.CastPosition);
                        }
                    }
                }
            }

            if (W.IsReady() && Settings.Combo.UseW)
            {
                foreach (var source in EntityManager.Heroes.Enemies.Where(x=>x.IsValidTarget(W.Range) && !x.HasUndyingBuffA() && !x.HasSpellShield() && x.TotalHealthWithShields() < Player.Instance.GetSpellDamage(x, SpellSlot.W)))
                {
                    var wPrediction = GetWPrediction(source);

                    if (wPrediction != null && wPrediction.HitChance >= HitChance.Medium)
                    {
                        W.Cast(wPrediction.CastPosition);
                        break;
                    }
                }
            }
        }
    }
}