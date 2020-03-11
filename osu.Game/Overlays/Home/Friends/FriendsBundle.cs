// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel;

namespace osu.Game.Overlays.Home.Friends
{
    public class FriendsBundle
    {
        public FriendsOnlineStatus Status { get; }

        public int Count { get; }

        public FriendsBundle(FriendsOnlineStatus status, int count)
        {
            Status = status;
            Count = count;
        }
    }

    public enum FriendsOnlineStatus
    {
        [Description("所有")]
        All,
        [Description("在线")]
        Online,
        [Description("离线")]
        Offline
    }
}