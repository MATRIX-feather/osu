﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Game.Overlays.Settings;
using osu.Game.Rulesets.Tau.Configuration;

namespace osu.Game.Rulesets.Tau.UI
{
    public class TauSettingsSubsection : RulesetSettingsSubsection
    {
        protected override string Header => "tau";

        public TauSettingsSubsection(Ruleset ruleset)
            : base(ruleset)
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            var config = (TauRulesetConfigManager)Config;

            if (config == null)
                return;

            Children = new Drawable[]
            {
                new SettingsCheckbox
                {
                    LabelText = "显示歌曲高潮效果",
                    Bindable = config.GetBindable<bool>(TauRulesetSettings.ShowVisualizer)
                },
                new SettingsSlider<float>
                {
                    LabelText = "盘面背景暗化",
                    Bindable = config.GetBindable<float>(TauRulesetSettings.PlayfieldDim),
                    KeyboardStep = 0.01f,
                    DisplayAsPercentage = true
                },
                new SettingsSlider<float>
                {
                    LabelText = "方块大小",
                    Bindable = config.GetBindable<float>(TauRulesetSettings.BeatSize),
                    KeyboardStep = 1f
                }
            };
        }
    }
}
