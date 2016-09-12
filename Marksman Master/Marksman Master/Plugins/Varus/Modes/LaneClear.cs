﻿#region Licensing
// ---------------------------------------------------------------------
// <copyright file="LaneClear.cs" company="EloBuddy">
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

namespace Marksman_Master.Plugins.Varus.Modes
{
    internal class LaneClear : Varus
    {
        public static bool CanILaneClear()
        {
            return !Settings.LaneClear.EnableIfNoEnemies ||
                   Player.Instance.CountEnemiesInRange(Settings.LaneClear.ScanRange) <=
                   Settings.LaneClear.AllowedEnemies;
        }

        public static void Execute()
        {
            var laneMinions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy,
                Player.Instance.Position, 1500).ToList();

            if (!laneMinions.Any())
                return;

            if (Q.IsReady() && Settings.LaneClear.UseQInLaneClear && Player.Instance.ManaPercent >= Settings.LaneClear.MinManaQ && laneMinions.Count >= Settings.LaneClear.MinMinionsHitQ)
            {
                if (!Q.IsCharging && !Player.Instance.IsUnderTurret() && !IsPreAttack && EntityManager.MinionsAndMonsters.GetLineFarmLocation(laneMinions, Q.Width, 1550).HitNumber >= Settings.LaneClear.MinMinionsHitQ && CanILaneClear())
                {
                    Q.StartCharging();
                } else if (Q.IsCharging && Q.IsFullyCharged)
                {
                    Q.CastOnBestFarmPosition(Settings.LaneClear.MinMinionsHitQ);
                }
            }

            if (E.IsReady() && !Player.Instance.IsUnderTurret() && Settings.LaneClear.UseEInLaneClear && Player.Instance.ManaPercent >= Settings.LaneClear.MinManaE && laneMinions.Count >= Settings.LaneClear.MinMinionsHitE)
            {
                E.CastOnBestFarmPosition(Settings.LaneClear.MinMinionsHitE);
            }
        }
    }
}