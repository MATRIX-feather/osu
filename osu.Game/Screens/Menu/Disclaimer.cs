﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Platform;
using osu.Framework.Screens;
using osu.Framework.Utils;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Online.API;
using osuTK;
using osuTK.Graphics;
using osu.Game.Users;

namespace osu.Game.Screens.Menu
{
    public class Disclaimer : StartupScreen
    {
        private SpriteIcon icon;
        private Color4 iconColour;
        private LinkFlowContainer textFlow;
        private LinkFlowContainer supportFlow;

        private Drawable heart;

        private const float icon_y = -85;
        private const float icon_size = 30;

        private readonly OsuScreen nextScreen;
        private readonly bool showDisclaimer;

        private readonly Bindable<User> currentUser = new Bindable<User>();
        private FillFlowContainer fill;

        [CanBeNull]
        private Sprite avatarSprite;

        public Disclaimer(OsuScreen nextScreen = null, bool showDisclaimer = false)
        {
            this.showDisclaimer = showDisclaimer;
            this.nextScreen = nextScreen;
            ValidForResume = false;
        }

        [Resolved]
        private IAPIProvider api { get; set; }

        [Resolved]
        private Storage storage { get; set; }

        [Resolved(CanBeNull = true)]
        private OsuGame game { get; set; }

        [Resolved]
        private GameHost host { get; set; }

        [Resolved]
        private MConfigManager mConfig { get; set; }

        private bool enableAvatarSprite;

        [BackgroundDependencyLoader]
        private void load(OsuColour colours, TextureStore textures, Storage storage, OsuGame game, CustomStore customStorage)
        {
            textures.AddStore(new TextureLoaderStore(customStorage));
            enableAvatarSprite = mConfig.Get<bool>(MSetting.UseCustomGreetingPicture);

            InternalChildren = new Drawable[]
            {
                icon = new SpriteIcon
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Icon = FontAwesome.Solid.Flask,
                    Size = new Vector2(icon_size),
                    Y = icon_y,
                },
                fill = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Y = icon_y,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.TopCentre,
                    Children = new Drawable[]
                    {
                        textFlow = new LinkFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            TextAnchor = Anchor.TopCentre,
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Spacing = new Vector2(0, 2),
                            LayoutDuration = 2000,
                            LayoutEasing = Easing.OutQuint
                        },
                        supportFlow = new LinkFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            TextAnchor = Anchor.TopCentre,
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Alpha = 0,
                            Spacing = new Vector2(0, 2),
                        },
                    }
                }
            };

            if (enableAvatarSprite)
                AddInternal(avatarSprite = new Sprite
                {
                    Size = new Vector2(400),
                    FillMode = FillMode.Fill,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Texture = textures.Get("avatarlogo"),
                    Alpha = 0,
                    Depth = float.MaxValue
                });

            game.SetWindowIcon(mConfig.Get<string>(MSetting.CustomWindowIconPath));

            textFlow.NewParagraph();
            textFlow.NewParagraph();
            textFlow.AddText("注意, 这是一个", t => t.Font = t.Font.With(Typeface.Torus, 30, FontWeight.Light));
            textFlow.AddText("分支版本", t => t.Font = t.Font.With(Typeface.Torus, 30, FontWeight.SemiBold));

            textFlow.AddParagraph("一些功能可能不会像预期或最新版的那样工作", t => t.Font = t.Font.With(size: 25));
            textFlow.NewParagraph();

            static void format(SpriteText t) => t.Font = OsuFont.GetFont(size: 20, weight: FontWeight.SemiBold);

            textFlow.AddParagraph(getRandomTip(), t => t.Font = t.Font.With(Typeface.Torus, 20, FontWeight.SemiBold));
            textFlow.NewParagraph();

            textFlow.NewParagraph();

            iconColour = colours.Yellow;

            // manually transfer the user once, but only do the final bind in LoadComplete to avoid thread woes (API scheduler could run while this screen is still loading).
            // the manual transfer is here to ensure all text content is loaded ahead of time as this is very early in the game load process and we want to avoid stutters.
            currentUser.Value = api.LocalUser.Value;
            currentUser.BindValueChanged(e =>
            {
                supportFlow.Children.ForEach(d => d.FadeOut().Expire());

                if (e.NewValue.IsSupporter)
                {
                    supportFlow.AddText("感谢支持osu!", format);
                }
                else
                {
                    supportFlow.AddText("您也可以考虑成为一名", format);
                    supportFlow.AddLink("osu!supporter", "https://osu.ppy.sh/home/support", creationParameters: format);
                    supportFlow.AddText("来支持游戏的开发", format);
                }

                heart = supportFlow.AddIcon(FontAwesome.Solid.Heart, t =>
                {
                    t.Padding = new MarginPadding { Left = 5, Top = 3 };
                    t.Font = t.Font.With(size: 12);
                    t.Origin = Anchor.Centre;
                    t.Colour = colours.Pink;
                }).First();

                if (IsLoaded)
                    animateHeart();

                if (supportFlow.IsPresent)
                    supportFlow.FadeInFromZero(500);
            }, true);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            if (nextScreen != null)
                LoadComponentAsync(nextScreen);

            ((IBindable<User>)currentUser).BindTo(api.LocalUser);
        }

        public override void OnEntering(IScreen last)
        {
            base.OnEntering(last);
            game?.TransformWindowOpacity(1, 300);

            icon.RotateTo(10);
            icon.FadeOut();
            icon.ScaleTo(0.5f);

            fill.FadeOut();

            var displayDelay = enableAvatarSprite ? 1000 : 0;
            bool fadeInWindowOnEnter = mConfig.Get<bool>(MSetting.FadeInWindowWhenEntering) && host.Window is SDL2DesktopWindow;
            avatarSprite?.FadeIn(fadeInWindowOnEnter ? 0 : 500);

            if (showDisclaimer) //显示Disclaimer时要提供的动画过程
            {
                this.Delay(displayDelay).Schedule(() =>
                {
                    avatarSprite?.FadeColour(Color4.Gray.Opacity(0.15f), 500);
                    icon.Delay(1000 - displayDelay).FadeIn(500).ScaleTo(1, 500, Easing.OutQuint);
                    fill.Delay(1000 - displayDelay).FadeIn(500);

                    using (BeginDelayedSequence(3000 - displayDelay, true))
                    {
                        icon.FadeColour(iconColour, 200, Easing.OutQuint);
                        icon.MoveToY(icon_y * 1.3f, 500, Easing.OutCirc)
                            .RotateTo(-360, 520, Easing.OutQuint)
                            .Then()
                            .MoveToY(icon_y, 160, Easing.InQuart)
                            .FadeColour(Color4.White, 160);

                        fill.Delay(520 + 160).MoveToOffset(new Vector2(0, 15), 160, Easing.OutQuart);
                    }

                    supportFlow.FadeOut().Delay(2000 - displayDelay).FadeIn(500);
                    double delay = 1000 - displayDelay;
                    foreach (var c in textFlow.Children)
                        c.FadeTo(0.001f).Delay(delay += 20).FadeIn(500);

                    animateHeart();
                });

                this
                    .FadeInFromZero(fadeInWindowOnEnter ? 0 : 500)
                    .Then(5500)
                    .FadeOut(250)
                    .ScaleTo(0.9f, 250, Easing.InQuint)
                    .Then(1000)
                    .Finally(d =>
                    {
                        if (nextScreen != null)
                            this.Push(nextScreen);
                    });
            }
            else //不显示时
            {
                if (enableAvatarSprite)
                    this
                        .FadeInFromZero(fadeInWindowOnEnter ? 0 : 500)
                        .Then(2000)
                        .FadeOut(250)
                        .ScaleTo(0.9f, 250, Easing.InQuint)
                        .Then(1000)
                        .Finally(d =>
                        {
                            if (nextScreen != null)
                                this.Push(nextScreen);
                        });
                else if (nextScreen != null)
                    this.Push(nextScreen);
            }
        }

        private string getRandomTip()
        {
            string[] tips =
            {
                "您可以在游戏中的任何位置按Ctrl+T来切换顶栏!",
                "您可以在游戏中的任何位置按Ctrl+O来访问设置!",
                "所有设置都是动态的，并实时生效。试试在游戏时时更改皮肤!",
                "每一次更新都会携带全新的功能。确保您的游戏为最新版本!",
                "如果您发现UI太大或太小，那么试试更改设置中的界面缩放!",
                "试着调整“屏幕缩放”模式，即使在全屏模式下也可以更改游戏或UI区域！",
                "目前，osu!direct对所有使用lazer的用户可用。您可以使用Ctrl+D在任何地方访问它！",
                "看到回放界面下面的时间条没？拖动他试试！",
                "多线程模式允许您即使在低帧数的情况下也能拥有准确的判定！",
                "在mod选择面板中向下滚动可以找到一堆有趣的新mod！",
                "大部分web内容(玩家资料,在线排名等)在游戏内已有原生支持！点点看顶栏上的图标！",
                "右键一个谱面可以选择查看在线信息，隐藏该谱面甚至删除单个难度！",
                "所有删除操作在退出游戏前都是临时的！您可以在“维护”设置中选择恢复被意外删除的内容！",
                "看看多人游戏中的“时移”玩法，他具备房间排行榜和游玩列表的功能！",
                "您可以在游戏中按Ctrl+F11来切换高级fps显示功能！",
                "并使用Ctrl+F2来查看详细性能记录！",
                "看看\"游玩列表\"系统, 他允许用户创建自己的自定义排行榜和永久排行榜!",
                "owo"
            };

            return tips[RNG.Next(0, tips.Length)];
        }

        private void animateHeart()
        {
            heart.FlashColour(Color4.White, 750, Easing.OutQuint).Loop();
        }
    }
}
