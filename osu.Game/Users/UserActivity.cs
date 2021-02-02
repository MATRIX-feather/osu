﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Online.Rooms;
using osu.Game.Rulesets;
using osuTK.Graphics;

namespace osu.Game.Users
{
    public abstract class UserActivity
    {
        public abstract string Status { get; }
        public virtual Color4 GetAppropriateColour(OsuColour colours) => colours.GreenDarker;

        public class Modding : UserActivity
        {
            public override string Status => "正在摸图";
            public override Color4 GetAppropriateColour(OsuColour colours) => colours.PurpleDark;
        }

        public class ChoosingBeatmap : UserActivity
        {
            public override string Status => "正在选图";
        }

        public class MultiplayerGame : UserActivity
        {
            public override string Status => "正在多人联机";
        }

        public class InMvis : UserActivity
        {
            public BeatmapInfo Beatmap { get; }
            public bool Unicode { get; }

            public InMvis(BeatmapInfo info, bool useUnicode)
            {
                Beatmap = info;
                Unicode = useUnicode;
            }

            public override string Status => $@"正在听 {(Unicode ? Beatmap.BeatmapSet.Metadata.TitleUnicode : Beatmap.BeatmapSet.Metadata.Title)} - {(Unicode ? Beatmap.BeatmapSet.Metadata.ArtistUnicode : Beatmap.BeatmapSet.Metadata.Artist)}";
        }

        public class Editing : UserActivity
        {
            public BeatmapInfo Beatmap { get; }

            public Editing(BeatmapInfo info)
            {
                Beatmap = info;
            }

            public override string Status => @"正在编辑谱面";
        }

        public class SoloGame : UserActivity
        {
            public BeatmapInfo Beatmap { get; }

            public RulesetInfo Ruleset { get; }

            public SoloGame(BeatmapInfo info, RulesetInfo ruleset)
            {
                Beatmap = info;
                Ruleset = ruleset;
            }

            public override string Status => Ruleset.CreateInstance().PlayingVerb;
        }

        public class Spectating : UserActivity
        {
            public override string Status => @"正在旁观别人";
        }

        public class SearchingForLobby : UserActivity
        {
            public override string Status => @"正在寻找多人游戏房间";
        }

        public class InLobby : UserActivity
        {
            public override string Status => @"正在多人游戏房间中";

            public readonly Room Room;

            public InLobby(Room room)
            {
                Room = room;
            }
        }
    }
}
