// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osuTK;
using osu.Game.Graphics;
using osu.Framework.Bindables;
using osu.Game.Beatmaps;
using osu.Framework.Localisation;

namespace osu.Game.Screens.Menu
{
    public class SongTicker : Container
    {
        private const int fade_duration = 800;

        public bool AllowUpdates { get; set; } = true;

        [Resolved]
        private Bindable<WorkingBeatmap> beatmap { get; set; }

        private readonly OsuSpriteText title, artist;

        public SongTicker()
        {
            AutoSizeAxes = Axes.Both;
            Child = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 3),
                Children = new Drawable[]
                {
                    title = new OsuSpriteText
                    {
                        Anchor = Anchor.TopRight,
                        Origin = Anchor.TopRight,
                        Font = OsuFont.GetFont(size: 24, weight: FontWeight.Light, italics: true)
                    },
                    artist = new OsuSpriteText
                    {
                        Anchor = Anchor.TopRight,
                        Origin = Anchor.TopRight,
                        Font = OsuFont.GetFont(size: 16)
                    }
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            beatmap.BindValueChanged(onBeatmapChanged);
        }

        private void onBeatmapChanged(ValueChangedEvent<WorkingBeatmap> working)
        {
            if (!AllowUpdates)
                return;

            var metadata = working.NewValue.Metadata;

            title.Text = new LocalisedString((metadata.TitleUnicode, metadata.Title));
            artist.Text = new LocalisedString((metadata.ArtistUnicode, metadata.Artist));

            this.FadeIn(fade_duration, Easing.OutQuint).Delay(4000).Then().FadeOut(fade_duration, Easing.OutQuint);
        }
    }
}