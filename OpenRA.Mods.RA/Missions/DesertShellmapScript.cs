﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Missions
{
	class DesertShellmapScriptInfo : TraitInfo<DesertShellmapScript>, Requires<SpawnMapActorsInfo> { }

	class DesertShellmapScript : ITick, IWorldLoaded
	{
		World world;
		Player allies;
		Player soviets;

		List<int2> viewportTargets = new List<int2>();
		int2 viewportTarget;
		int viewportTargetNumber;
		int2 viewportOrigin;
		float mul;
		float div = 400;
		int waitTicks = 0;

		Actor attackLocation;
		Actor coastRP1;
		Actor coastRP2;
		int coastUnitsLeft;
		static readonly string[] CoastUnits = { "e1", "e1", "e2", "e3", "e4" };

		Actor paradropLocation;
		static readonly string[] ParadropUnits = { "e1", "e1", "e1", "e2", "e2" };

		public void Tick(Actor self)
		{
			MissionUtils.CapOre(soviets);
			if (world.FrameNumber % 20 == 0 && coastUnitsLeft-- > 0)
			{
				var u = world.CreateActor(CoastUnits.Random(world.SharedRandom), soviets, coastRP1.Location, null);
				u.QueueActivity(new Move.Move(coastRP2.Location, 0));
				u.QueueActivity(new AttackMove.AttackMoveActivity(u, new Move.Move(attackLocation.Location, 0)));
			}

			if (world.FrameNumber % 25 == 0)
				foreach (var actor in world.Actors.Where(a => a.IsInWorld && a.Owner == soviets && a.IsIdle && !a.IsDead()
					&& a.HasTrait<AttackBase>() && a.HasTrait<Mobile>()))
					actor.QueueActivity(new AttackMove.AttackMoveActivity(actor, new Move.Move(attackLocation.Location)));

			if (--waitTicks <= 0)
			{
				if (++mul <= div)
					Game.MoveViewport(float2.Lerp(viewportOrigin, viewportTarget, mul / div));
				else
				{
					mul = 0;
					viewportOrigin = viewportTarget;
					viewportTarget = viewportTargets[(viewportTargetNumber = (viewportTargetNumber + 1) % viewportTargets.Count)];
					waitTicks = 100;

					if (viewportTargetNumber == 0)
						coastUnitsLeft = 15;
					if (viewportTargetNumber == 2)
						MissionUtils.Paradrop(world, soviets, ParadropUnits, world.ChooseRandomEdgeCell(), paradropLocation.Location);
				}
			}
		}

		public void WorldLoaded(World w)
		{
			world = w;

			allies = w.Players.Single(p => p.InternalName == "Allies");
			soviets = w.Players.Single(p => p.InternalName == "Soviets");

			var actors = w.WorldActor.Trait<SpawnMapActors>().Actors;

			attackLocation = actors["AttackLocation"];
			coastRP1 = actors["CoastRP1"];
			coastRP2 = actors["CoastRP2"];
			paradropLocation = actors["ParadropLocation"];

			var t1 = actors["ViewportTarget1"];
			var t2 = actors["ViewportTarget2"];
			var t3 = actors["ViewportTarget3"];
			var t4 = actors["ViewportTarget4"];
			var t5 = actors["ViewportTarget5"];
			viewportTargets = new[] { t1, t2, t3, t4, t5 }.Select(t => t.Location.ToInt2()).ToList();

			foreach (var actor in actors.Values.Where(a => a.Owner == allies || a.HasTrait<Bridge>()))
			{
				if (actor.Owner == allies && actor.HasTrait<AutoTarget>())
					actor.Trait<AutoTarget>().stance = UnitStance.Defend;
				actor.AddTrait(new Invulnerable());
			}

			viewportOrigin = viewportTargets[0];
			viewportTargetNumber = 1;
			viewportTarget = viewportTargets[1];
			Game.viewport.Center(viewportOrigin);
			Sound.SoundVolumeModifier = 0.25f;

			world.RenderedPlayer = allies;
			world.RenderedShroud.Jank();
		}
	}
}
