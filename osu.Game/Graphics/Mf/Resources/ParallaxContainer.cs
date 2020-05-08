﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics;
using osu.Framework.Input;
using osuTK;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Game.Configuration;
using osu.Framework.Utils;

namespace osu.Game.Graphics.Mf.Resources
{
    public class ParallaxContainer : Container, IRequireHighFrequencyMousePosition
    {
        public const float DEFAULT_PARALLAX_AMOUNT = 0.02f;

        /// <summary>
        /// The amount of parallax movement. Negative values will reverse the direction of parallax relative to user input.
        /// </summary>
        public float ParallaxAmount = DEFAULT_PARALLAX_AMOUNT;

        private Bindable<bool> OptUIEnabled;
        private Bindable<bool> parallaxEnabled;

        public ParallaxContainer()
        {
            RelativeSizeAxes = Axes.Both;
            AddInternal(content = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre
            });
        }

        private readonly Container content;
        private InputManager input;

        protected override Container<Drawable> Content => content;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config, MfConfigManager mfConfig)
        {
            OptUIEnabled = mfConfig.GetBindable<bool>(MfSetting.OptUI);
            parallaxEnabled = config.GetBindable<bool>(OsuSetting.MenuParallax);

            OptUIEnabled.ValueChanged += delegate
            {
                if (!OptUIEnabled.Value)
                {
                    content.MoveTo(Vector2.Zero, firstUpdate ? 0 : 1000, Easing.OutQuint);
                    content.Scale = new Vector2(1 + Math.Abs(ParallaxAmount));
                }
                UpdateVisualEffect();
            };
            parallaxEnabled.ValueChanged += delegate
            {
                if (!parallaxEnabled.Value)
                {
                    content.MoveTo(Vector2.Zero, firstUpdate ? 0 : 1000, Easing.OutQuint);
                    content.Scale = new Vector2(1 + Math.Abs(ParallaxAmount));
                }
                UpdateVisualEffect();
            };
        }

        private void UpdateVisualEffect()
        {
            if (parallaxEnabled.Value && OptUIEnabled.Value)
            {
                Vector2 offset = (input.CurrentState.Mouse == null ? Vector2.Zero : ToLocalSpace(input.CurrentState.Mouse.Position) - DrawSize / 2) * ParallaxAmount;

                const float parallax_duration = 100;

                double elapsed = Math.Clamp(Clock.ElapsedFrameTime, 0, parallax_duration);

                content.Position = Interpolation.ValueAt(elapsed, content.Position, offset, 0, parallax_duration, Easing.OutQuint);
                content.Scale = Interpolation.ValueAt(elapsed, content.Scale, new Vector2(1 + Math.Abs(ParallaxAmount)), 0, 1000, Easing.OutQuint);
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            input = GetContainingInputManager();
        }

        private bool firstUpdate = true;

        protected override void Update()
        {
            base.Update();

            UpdateVisualEffect();

            firstUpdate = false;
        }
    }
}
