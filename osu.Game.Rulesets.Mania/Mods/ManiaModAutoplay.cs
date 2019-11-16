﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mania.Beatmaps;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Mania.Replays;
using osu.Game.Rulesets.Mods;
using osu.Game.Scoring;
using osu.Game.Users;

namespace osu.Game.Rulesets.Mania.Mods
{
    public class ManiaModAutoplay : ModAutoplay<ManiaHitObject>
    {
        public override string Description => @"欣赏精彩绝伦的自动表演";
        public override Score CreateReplayScore(IBeatmap beatmap) => new Score
        {
            ScoreInfo = new ScoreInfo { User = new User { Username = "osu!topus!" } },
            Replay = new ManiaAutoGenerator((ManiaBeatmap)beatmap).Generate(),
        };
    }
}
