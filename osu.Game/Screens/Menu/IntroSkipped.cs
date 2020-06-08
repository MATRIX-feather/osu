﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Screens;
using osu.Framework.Graphics;
using osu.Framework.Allocation;
using osu.Game.Configuration;
using osu.Framework.Bindables;

namespace osu.Game.Screens.Menu
{
    public class IntroSkipped : IntroScreen
    {
        protected override string BeatmapHash => "3c8b1fcc9434dbb29e2fb613d3b9eada9d7bb6c125ceb32396c3b53437280c83";

        protected override string BeatmapFile => "circles.osz";

        protected IBindable<bool> LoadDirectToSongSelect { get; private set; }

        [Resolved(CanBeNull = true)]
        private OsuGame game { get; set; }

        [BackgroundDependencyLoader]
        private void load(MfConfigManager config)
        {
            LoadDirectToSongSelect = config.GetBindable<bool>(MfSetting.IntroLoadDirectToSongSelect);
        }

        protected override void LogoArriving(OsuLogo logo, bool resuming)
        {
            base.LogoArriving(logo, resuming);

            if (!resuming)
            {
                Scheduler.AddDelayed(delegate
                {
                    StartTrack();

                    PrepareMenuLoad();

                    Scheduler.AddDelayed(LoadMenu, 0);
                }, 0);

                logo.ScaleTo(0).FadeOut();
                if ( !LoadDirectToSongSelect.Value )
                {
                    logo.ScaleTo(1, 300, Easing.OutQuint);
                    logo.FadeIn(300);
                }
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (!MenuMusic.Value && setInfo != null && LoadDirectToSongSelect.Value )
            {
                game?.PresentBeatmap(setInfo);
            }
        }

        public override void OnSuspending(IScreen next)
        {
            this.FadeOut(300);
            base.OnSuspending(next);
        }
    }
}
