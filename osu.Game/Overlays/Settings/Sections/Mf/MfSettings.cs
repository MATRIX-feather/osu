// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Game.Configuration;

namespace osu.Game.Overlays.Settings.Sections.Mf
{
    public class MfSettings : SettingsSubsection
    {
        protected override string Header => "Mf-osu";

        [BackgroundDependencyLoader]
        private void load(MfConfigManager config, OsuConfigManager osuConfig)
        {
            Children = new Drawable[]
            {
                new SettingsCheckbox
                {
                    LabelText = "启用Mf自定义UI",
                    TooltipText = "启用以获得mfosu提供的默认界面体验, "
                                  + "禁用以获得接近原版lazer提供的界面体验",
                    Current = config.GetBindable<bool>(MfSetting.OptUI)
                },
                new SettingsCheckbox
                {
                    LabelText = "启用三角形粒子动画",
                    Current = config.GetBindable<bool>(MfSetting.TrianglesEnabled)
                },
                new SettingsCheckbox
                {
                    LabelText = "启用Sayobot功能",
                    TooltipText = "这将影响所有谱面预览、封面、和下图的功能, 但不会影响已完成或正在进行中的请求",
                    Current = config.GetBindable<bool>(MfSetting.UseSayobot)
                },
                new SettingsSlider<float>
                {
                    LabelText = "立体音效增益",
                    Current = config.GetBindable<float>(MfSetting.SamplePlaybackGain),
                    DisplayAsPercentage = true,
                    KeyboardStep = 0.01f,
                },
                new SettingsSlider<float>
                {
                    LabelText = "歌曲选择界面背景模糊",
                    Current = config.GetBindable<float>(MfSetting.SongSelectBgBlur),
                    DisplayAsPercentage = true,
                    KeyboardStep = 0.01f,
                },
                new SettingsCheckbox
                {
                    LabelText = "启动后直接进入选歌界面",
                    TooltipText = "仅在开场样式为\"略过开场\"时生效",
                    Current = config.GetBindable<bool>(MfSetting.IntroLoadDirectToSongSelect)
                },
            };
        }
    }
}
