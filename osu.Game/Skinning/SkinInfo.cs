﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.IO.Stores;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Extensions;
using osu.Game.IO;

namespace osu.Game.Skinning
{
    public class SkinInfo : IHasFiles<SkinFileInfo>, IEquatable<SkinInfo>, IHasPrimaryKey, ISoftDelete
    {
        internal const int DEFAULT_SKIN = 0;
        internal const int CLASSIC_SKIN = -1;
        internal const int RANDOM_SKIN = -2;

        public int ID { get; set; }

        public string Name { get; set; }

        public string Hash { get; set; }

        public string Creator { get; set; }

        public string InstantiationInfo { get; set; }

        public virtual Skin CreateInstance(IResourceStore<byte[]> legacyDefaultResources, IStorageResourceProvider resources)
        {
            var type = string.IsNullOrEmpty(InstantiationInfo)
                // handle the case of skins imported before InstantiationInfo was added.
                ? typeof(LegacySkin)
                : Type.GetType(InstantiationInfo).AsNonNull();

            if (type == typeof(DefaultLegacySkin))
                return (Skin)Activator.CreateInstance(type, this, legacyDefaultResources, resources);

            return (Skin)Activator.CreateInstance(type, this, resources);
        }

        public List<SkinFileInfo> Files { get; set; } = new List<SkinFileInfo>();

        public List<DatabasedSetting> Settings { get; set; }

        public bool DeletePending { get; set; }

        public static SkinInfo Default { get; } = new SkinInfo
        {
            ID = DEFAULT_SKIN,
            Name = "osu!lazer",
            Creator = "team osu!",
            InstantiationInfo = typeof(DefaultSkin).GetInvariantInstantiationInfo()
        };

        public bool Equals(SkinInfo other) => other != null && ID == other.ID;

        public override string ToString()
        {
            string author = Creator == null ? string.Empty : $"({Creator})";
            return $"{Name} {author}".Trim();
        }
    }
}
