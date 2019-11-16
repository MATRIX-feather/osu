﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Taiko.Mods
{
    public class TaikoModHidden : ModHidden
    {
        public override string Description => @"音符会在你击打前隐身!";
        public override double ScoreMultiplier => 1.06;
        public override bool HasImplementation => false;
    }
}
