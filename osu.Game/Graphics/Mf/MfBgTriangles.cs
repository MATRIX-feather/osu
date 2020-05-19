// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Configuration;
using osu.Game.Graphics.Backgrounds;

namespace osu.Game.Graphics
{
    public class MfBgTriangles : Container
    {
        private readonly Bindable<bool> Optui = new Bindable<bool>();
        private BackgroundTriangles BackgroundTriangle;
        public bool EnableBeatSync { get; set; }

        [BackgroundDependencyLoader]
        private void load(MfConfigManager config, OsuColour colour)
        {
            config.BindWith(MfSetting.OptUI, Optui);

            Optui.ValueChanged += _ => UpdateIcons();
            UpdateIcons();
        }

        public MfBgTriangles(float alpha = 0.65f, bool highLight = false, float triangleScale = 2f, bool sync = false)
        {
            this.Alpha = alpha;
            RelativeSizeAxes = Axes.Both;
            Anchor = Anchor.BottomCentre;
            Origin = Anchor.BottomCentre;
            Children = new Drawable[]
            {
                BackgroundTriangle = new BackgroundTriangles(highLight, triangleScale)
                {
                    beatSync = sync,
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                },
            };
        }

        private class BackgroundTriangles : Container
        {
            private Triangles triangles;
            public bool beatSync;
            public bool highLight;
            public float scale;

            public BackgroundTriangles(bool highLight = false, float triangleScaleValue = 2f)
            {
                RelativeSizeAxes = Axes.Both;
                Masking = true;
                this.highLight = highLight;
                this.scale = triangleScaleValue;
            }

            protected override void LoadComplete()
            {
                this.Add(
                    triangles = new Triangles
                    {
                        EnableBeatSync = beatSync,
                        Anchor = Anchor.BottomCentre,
                        Origin = Anchor.BottomCentre,
                        RelativeSizeAxes = Axes.Both,
                        TriangleScale = scale,
                        Colour = highLight ? Color4Extensions.FromHex(@"88b300") : OsuColour.Gray(0.2f),
                    });
            }
        }

        private void UpdateIcons()
        {
            switch (Optui.Value)
            {
                case true:
                    BackgroundTriangle.FadeIn(250);
                    break;

                case false:
                    BackgroundTriangle.FadeOut(250);
                    break;
            }
        }
    }
}
