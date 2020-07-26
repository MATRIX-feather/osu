﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Input;
using osu.Game.Configuration;
using osu.Game.Graphics.UserInterface;

namespace osu.Game.Overlays.Settings.Sections.Input
{
    public class MouseSettings : SettingsSubsection
    {
        protected override string Header => "鼠标";

        private readonly BindableBool rawInputToggle = new BindableBool();
        private Bindable<double> sensitivityBindable = new BindableDouble();
        private Bindable<string> ignoredInputHandler;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager osuConfig, FrameworkConfigManager config)
        {
            var configSensitivity = config.GetBindable<double>(FrameworkSetting.CursorSensitivity);

            // use local bindable to avoid changing enabled state of game host's bindable.
            sensitivityBindable = configSensitivity.GetUnboundCopy();
            configSensitivity.BindValueChanged(val => sensitivityBindable.Value = val.NewValue);
            sensitivityBindable.BindValueChanged(val => configSensitivity.Value = val.NewValue);

            Children = new Drawable[]
            {
                new SettingsCheckbox
                {
                    LabelText = "绝对输入",
                    Bindable = rawInputToggle
                },
                new SensitivitySetting
                {
                    LabelText = "鼠标灵敏度",
                    Bindable =sensitivityBindable
                },
                new SettingsCheckbox
                {
                    LabelText = "将光标绝对映射至窗口中",
                    Bindable = config.GetBindable<bool>(FrameworkSetting.MapAbsoluteInputToWindow)
                },
                new SettingsEnumDropdown<ConfineMouseMode>
                {
                    LabelText = "光标边界",
                    Bindable = config.GetBindable<ConfineMouseMode>(FrameworkSetting.ConfineMouseMode),
                },
                new SettingsCheckbox
                {
                    LabelText = "在游玩时禁用鼠标滚轮",
                    Bindable = osuConfig.GetBindable<bool>(OsuSetting.MouseDisableWheel)
                },
                new SettingsCheckbox
                {
                    LabelText = "在游玩时禁用鼠标按键",
                    Bindable = osuConfig.GetBindable<bool>(OsuSetting.MouseDisableButtons)
                },
            };

            if (RuntimeInfo.OS != RuntimeInfo.Platform.Windows)
            {
                rawInputToggle.Disabled = true;
                sensitivityBindable.Disabled = true;
            }
            else
            {
                rawInputToggle.ValueChanged += enabled =>
                {
                    // this is temporary until we support per-handler settings.
                    const string raw_mouse_handler = @"OsuTKRawMouseHandler";
                    const string standard_mouse_handler = @"OsuTKMouseHandler";

                    ignoredInputHandler.Value = enabled.NewValue ? standard_mouse_handler : raw_mouse_handler;
                };

                ignoredInputHandler = config.GetBindable<string>(FrameworkSetting.IgnoredInputHandlers);
                ignoredInputHandler.ValueChanged += handler =>
                {
                    bool raw = !handler.NewValue.Contains("Raw");
                    rawInputToggle.Value = raw;
                    sensitivityBindable.Disabled = !raw;
                };

                ignoredInputHandler.TriggerChange();
            }
        }

        private class SensitivitySetting : SettingsSlider<double, SensitivitySlider>
        {
            public SensitivitySetting()
            {
                KeyboardStep = 0.01f;
                TransferValueOnCommit = true;
            }
        }

        private class SensitivitySlider : OsuSliderBar<double>
        {
            public override string TooltipText => Current.Disabled ? "开启绝对输入以调整灵敏度" : $"{base.TooltipText}x";
        }
    }
}
