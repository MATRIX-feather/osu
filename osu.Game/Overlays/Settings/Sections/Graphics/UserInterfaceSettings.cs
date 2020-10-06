﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Game.Configuration;
using osu.Game.Graphics.UserInterface;

namespace osu.Game.Overlays.Settings.Sections.Graphics
{
    public class UserInterfaceSettings : SettingsSubsection
    {
        protected override string Header => "用户界面";

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            Children = new Drawable[]
            {
                new SettingsCheckbox
                {
                    LabelText = "在拖拽时旋转光标",
                    Current = config.GetBindable<bool>(OsuSetting.CursorRotation)
                },
                new SettingsCheckbox
                {
                    LabelText = "视差效果",
                    Current = config.GetBindable<bool>(OsuSetting.MenuParallax)
                },
                new SettingsSlider<float, TimeSlider>
                {
                    LabelText = "\"按压以确认\" 激活时间",
                    Current = config.GetBindable<float>(OsuSetting.UIHoldActivationDelay),
                    KeyboardStep = 50
                },
            };
        }

        private class TimeSlider : OsuSliderBar<float>
        {
            public override string TooltipText => Current.Value.ToString("N0") + "毫秒";
        }
    }
}
