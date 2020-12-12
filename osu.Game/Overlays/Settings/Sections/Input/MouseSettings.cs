﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Game.Configuration;
using osu.Game.Graphics.UserInterface;
using osu.Game.Input;

namespace osu.Game.Overlays.Settings.Sections.Input
{
    public class MouseSettings : SettingsSubsection
    {
        protected override string Header => "鼠标";

        private readonly BindableBool rawInputToggle = new BindableBool();
        private Bindable<double> sensitivityBindable = new BindableDouble();
        private Bindable<string> ignoredInputHandlers;

        private Bindable<WindowMode> windowMode;
        private SettingsEnumDropdown<OsuConfineMouseMode> confineMouseModeSetting;

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
                    Current = rawInputToggle
                },
                new SensitivitySetting
                {
                    LabelText = "光标灵敏度",
                    Current = sensitivityBindable
                },
                new SettingsCheckbox
                {
                    LabelText = "将光标绝对映射至窗口中",
                    Current = config.GetBindable<bool>(FrameworkSetting.MapAbsoluteInputToWindow)
                },
                confineMouseModeSetting = new SettingsEnumDropdown<OsuConfineMouseMode>
                {
                    LabelText = "光标边界( 将光标限制在窗口中 )",
                    Current = osuConfig.GetBindable<OsuConfineMouseMode>(OsuSetting.ConfineMouseMode)
                },
                new SettingsCheckbox
                {
                    LabelText = "在游玩时禁用鼠标滚轮",
                    Current = osuConfig.GetBindable<bool>(OsuSetting.MouseDisableWheel)
                },
                new SettingsCheckbox
                {
                    LabelText = "在游玩时禁用鼠标按键",
                    Current = osuConfig.GetBindable<bool>(OsuSetting.MouseDisableButtons)
                },
            };

            windowMode = config.GetBindable<WindowMode>(FrameworkSetting.WindowMode);
            windowMode.BindValueChanged(mode => confineMouseModeSetting.Alpha = mode.NewValue == WindowMode.Fullscreen ? 0 : 1, true);

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
                    const string standard_mouse_handlers = @"OsuTKMouseHandler MouseHandler";

                    ignoredInputHandlers.Value = enabled.NewValue ? standard_mouse_handlers : raw_mouse_handler;
                };

                ignoredInputHandlers = config.GetBindable<string>(FrameworkSetting.IgnoredInputHandlers);
                ignoredInputHandlers.ValueChanged += handler =>
                {
                    bool raw = !handler.NewValue.Contains("Raw");
                    rawInputToggle.Value = raw;
                    sensitivityBindable.Disabled = !raw;
                };

                ignoredInputHandlers.TriggerChange();
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
