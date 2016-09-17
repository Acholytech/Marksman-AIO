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

using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using Marksman_Master.Utils;

namespace Marksman_Master.Plugins.Lucian.Modes
{
    internal class PermaActive : Lucian
    {
        public static void Execute()
        {
            if (Q.IsReady() && Settings.Harass.UseQ && !Player.Instance.IsRecalling() && !Player.Instance.Position.IsVectorUnderEnemyTower() && Player.Instance.ManaPercent >= Settings.Harass.MinManaQ && !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass) && !Player.Instance.IsDashing())
            {
                foreach (
                    var enemy in
                        EntityManager.Heroes.Enemies.Where(
                            x => x.IsValidTarget(1100) && Settings.Harass.IsAutoHarassEnabledFor(x))
                            .OrderByDescending(x => Player.Instance.GetSpellDamage(x, SpellSlot.Q)))
                {
                    if (enemy.IsValidTarget(Q.Range))
                    {
                        Q.Cast(enemy);
                        return;
                    }

                    if (!enemy.IsValidTarget(1100) || !Settings.Combo.ExtendQOnMinions)
                        break;

                    foreach (
                        var entity in
                            from entity in
                                EntityManager.MinionsAndMonsters.CombinedAttackable.Where(
                                    x => x.IsValidTarget(Q.Range))
                            let pos =
                                Player.Instance.Position.Extend(entity, Player.Instance.Distance(entity) > 1025 ? 1025 - Player.Instance.Distance(entity) : 1025)
                            let targetpos = Prediction.Position.PredictUnitPosition(enemy, 250)
                            let rect = new Geometry.Polygon.Rectangle(entity.Position.To2D(), pos, 20)
                            where
                                new Geometry.Polygon.Circle(targetpos, enemy.BoundingRadius).Points.Any(
                                    rect.IsInside)
                            select entity)
                    {
                        Q.Cast(entity);
                        return;
                    }
                }
            }

            if (!R.IsReady() || !Settings.Combo.UseR ||
                Player.Instance.Spellbook.GetSpell(SpellSlot.R).Name != "LucianR")
                return;

            var target = TargetSelector.GetTarget(R.Range, DamageType.Physical);

            if (target == null || !Settings.Combo.RKeybind)
                return;

            var rPrediciton = R.GetPrediction(target);
            if (rPrediciton.HitChance >= HitChance.Medium)
            {
                R.Cast(rPrediciton.CastPosition);
            }
        }
    }
}