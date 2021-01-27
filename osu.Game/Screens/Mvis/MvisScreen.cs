// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Audio;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Screens;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.UserInterface;
using osu.Game.Input;
using osu.Game.Input.Bindings;
using osu.Game.Overlays;
using osu.Game.Overlays.Settings;
using osu.Game.Overlays.Settings.Sections.Mf;
using osu.Game.Rulesets.Mods;
using osu.Game.Screens.Mvis.BottomBar;
using osu.Game.Screens.Mvis.BottomBar.Buttons;
using osu.Game.Screens.Mvis.Collections;
using osu.Game.Screens.Mvis.Collections.Interface;
using osu.Game.Screens.Mvis.Objects;
using osu.Game.Screens.Mvis.SideBar;
using osu.Game.Screens.Mvis.Skinning;
using osu.Game.Screens.Mvis.Storyboard;
using osu.Game.Screens.Play;
using osu.Game.Screens.Select;
using osu.Game.Skinning;
using osu.Game.Users;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;
using Sidebar = osu.Game.Screens.Mvis.SideBar.Sidebar;
using SongProgressBar = osu.Game.Screens.Mvis.BottomBar.SongProgressBar;

namespace osu.Game.Screens.Mvis
{
    ///<summary>
    /// 音乐播放器
    ///</summary>
    public class MvisScreen : ScreenWithBeatmapBackground, IKeyBindingHandler<GlobalAction>
    {
        public override bool HideOverlaysOnEnter => true;
        private bool allowCursor;
        public override bool AllowBackButton => false;
        public override bool CursorVisible => allowCursor;
        public override bool AllowRateAdjustments => true;

        private bool canReallyHide =>
            // don't hide if the user is hovering one of the panes, unless they are idle.
            (IsHovered || isIdle.Value)
            // don't hide if the user is dragging a slider or otherwise.
            && inputManager?.DraggedDrawable == null
            // don't hide if a focused overlay is visible, like settings.
            && inputManager?.FocusedDrawable == null;

        private bool isPushed;

        #region 依赖

        [Resolved(CanBeNull = true)]
        private OsuGame game { get; set; }

        [Resolved]
        private MusicController musicController { get; set; }

        private CollectionHelper collectionHelper;
        private CustomColourProvider colourProvider;
        private DependencyContainer dependencies;

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent) =>
            dependencies = new DependencyContainer(base.CreateChildDependencies(parent));

        #endregion

        #region 底栏

        private BottomBarContainer bottomBar;
        private FillFlowContainer bottomFillFlow;
        private Container buttonsContainer;
        private SongProgressBar progressBar;
        private BottomBarButton soloButton;
        private BottomBarButton prevButton;
        private BottomBarButton nextButton;
        private BottomBarButton sidebarToggleButton;
        private BottomBarSwitchButton loopToggleButton;
        private BottomBarOverlayLockSwitchButton lockButton;
        private BottomBarSwitchButton songProgressButton;
        private BottomBarButton collectionButton;

        #endregion

        #region 背景和前景

        private InputManager inputManager { get; set; }
        private NightcoreBeatContainer nightcoreBeatContainer;

        private Container background;
        private BackgroundStoryBoardLoader sbLoader;
        private BgTrianglesContainer bgTriangles;
        private Container particles;
        private FullScreenSkinnableComponent skinnableBbBackground;

        private Container foreground;
        private BeatmapLogo beatmapLogo;
        private FullScreenSkinnableComponent skinnableForeground;

        #endregion

        #region overlay

        private LoadingSpinner loadingSpinner;
        private Sidebar sidebar;

        #endregion

        #region 侧边栏

        private CollectionSelectPanel collectionPanel;
        private SidebarSettingsScrollContainer settingsScroll;

        #endregion

        #region 设置

        private readonly BindableBool trackRunning = new BindableBool();
        private readonly BindableBool showParticles = new BindableBool();
        private readonly BindableFloat bgBlur = new BindableFloat();
        private readonly BindableFloat idleBgDim = new BindableFloat();
        private readonly BindableFloat contentAlpha = new BindableFloat();
        private readonly BindableDouble musicSpeed = new BindableDouble();
        private readonly BindableBool adjustFreq = new BindableBool();
        private readonly BindableBool nightcoreBeat = new BindableBool();
        private readonly BindableBool playFromCollection = new BindableBool();
        private readonly BindableBool allowProxy = new BindableBool();

        #endregion

        #region 故事版proxy

        private readonly Container proxyContainer = new Container
        {
            RelativeSizeAxes = Axes.Both
        };

        private Drawable prevProxy;

        #endregion

        #region 杂项

        private const float duration = 750;

        private bool overlaysHidden;
        private readonly BindableBool lockChanges = new BindableBool();
        private readonly IBindable<bool> isIdle = new BindableBool();

        private readonly Bindable<UserActivity> activity = new Bindable<UserActivity>();

        private DrawableTrack track => musicController.CurrentTrack;

        private WorkingBeatmap prevBeatmap;

        #endregion

        public MvisScreen()
        {
            Padding = new MarginPadding { Horizontal = -HORIZONTAL_OVERFLOW_PADDING };

            Activity.BindTo(activity);
        }

        [BackgroundDependencyLoader]
        private void load(MConfigManager config, IdleTracker idleTracker)
        {
            var iR = config.Get<float>(MSetting.MvisInterfaceRed);
            var iG = config.Get<float>(MSetting.MvisInterfaceGreen);
            var iB = config.Get<float>(MSetting.MvisInterfaceBlue);
            dependencies.Cache(colourProvider = new CustomColourProvider(iR, iG, iB));
            dependencies.Cache(collectionHelper = new CollectionHelper());

            InternalChildren = new Drawable[]
            {
                colourProvider,
                collectionHelper,
                nightcoreBeatContainer = new NightcoreBeatContainer
                {
                    Alpha = 0
                },
                new Container
                {
                    Name = "Content Container",
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Horizontal = 50 },
                    Masking = true,
                    Children = new Drawable[]
                    {
                        background = new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Name = "Gameplay Background Elements Container",
                            Children = new Drawable[]
                            {
                                bgTriangles = new BgTrianglesContainer()
                            }
                        },
                        foreground = new Container
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Name = "Gameplay Foreground Elements Container",
                            RelativeSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                particles = new Container
                                {
                                    RelativeSizeAxes = Axes.Both
                                },
                                new ParallaxContainer
                                {
                                    ParallaxAmount = -0.0025f,
                                    Child = beatmapLogo = new BeatmapLogo
                                    {
                                        Anchor = Anchor.Centre,
                                    }
                                }
                            }
                        }
                    }
                },
                skinnableForeground = new FullScreenSkinnableComponent("MPlayer-foreground", confineMode: ConfineMode.ScaleToFill, defaultImplementation: _ => new PlaceHolder())
                {
                    Name = "前景图",
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Alpha = 0,
                    OverrideChildAnchor = true
                },
                new Container
                {
                    Name = "Overlay Container",
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        loadingSpinner = new LoadingSpinner(true, true)
                        {
                            Anchor = Anchor.BottomCentre,
                            Origin = Anchor.BottomCentre,
                            Margin = new MarginPadding(115)
                        },
                        sidebar = new Sidebar
                        {
                            Name = "Sidebar Container",
                            Padding = new MarginPadding { Right = HORIZONTAL_OVERFLOW_PADDING }
                        },
                        skinnableBbBackground = new FullScreenSkinnableComponent("MBottomBar-background",
                            confineMode: ConfineMode.ScaleToFill,
                            masking: true,
                            defaultImplementation: _ => new PlaceHolder())
                        {
                            Name = "底栏背景图",
                            Anchor = Anchor.BottomCentre,
                            Origin = Anchor.BottomCentre,
                            RelativeSizeAxes = Axes.X,
                            Height = 100,
                            ChildAnchor = Anchor.BottomCentre,
                            ChildOrigin = Anchor.BottomCentre,
                            Alpha = 0,
                            CentreComponent = false,
                            OverrideChildAnchor = true
                        },
                        bottomFillFlow = new FillFlowContainer
                        {
                            Name = "Bottom FillFlow",
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Anchor = Anchor.BottomCentre,
                            Origin = Anchor.BottomCentre,
                            Direction = FillDirection.Vertical,
                            LayoutDuration = duration,
                            LayoutEasing = Easing.OutQuint,
                            Children = new Drawable[]
                            {
                                bottomBar = new BottomBarContainer
                                {
                                    Children = new Drawable[]
                                    {
                                        new Container
                                        {
                                            Name = "Base Container",
                                            Padding = new MarginPadding { Horizontal = HORIZONTAL_OVERFLOW_PADDING },
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            RelativeSizeAxes = Axes.Both,
                                            Children = new Drawable[]
                                            {
                                                buttonsContainer = new Container
                                                {
                                                    Name = "Buttons Container",
                                                    Anchor = Anchor.Centre,
                                                    Origin = Anchor.Centre,
                                                    RelativeSizeAxes = Axes.Both,
                                                    Children = new Drawable[]
                                                    {
                                                        new FillFlowContainer
                                                        {
                                                            Name = "Left Buttons FillFlow",
                                                            Anchor = Anchor.CentreLeft,
                                                            Origin = Anchor.CentreLeft,
                                                            AutoSizeAxes = Axes.Both,
                                                            Spacing = new Vector2(5),
                                                            Margin = new MarginPadding { Left = 5 },
                                                            Children = new Drawable[]
                                                            {
                                                                new BottomBarButton
                                                                {
                                                                    ButtonIcon = FontAwesome.Solid.ArrowLeft,
                                                                    Action = this.Exit,
                                                                    TooltipText = "generic.exit",
                                                                },
                                                                new BottomBarButton
                                                                {
                                                                    ButtonIcon = FontAwesome.Regular.QuestionCircle,
                                                                    Action = () => game?.OpenUrlExternally("https://matrix-feather.github.io/%E6%97%A5%E5%B8%B8/mfosu_mp_manual/"),
                                                                    TooltipText = "screen.mvis.main.openManualInBrowser"
                                                                },
                                                            }
                                                        },
                                                        new FillFlowContainer
                                                        {
                                                            Name = "Centre Button FillFlow",
                                                            Anchor = Anchor.Centre,
                                                            Origin = Anchor.Centre,
                                                            AutoSizeAxes = Axes.Both,
                                                            Spacing = new Vector2(5),
                                                            Children = new Drawable[]
                                                            {
                                                                prevButton = new NextPrevButton
                                                                {
                                                                    Size = new Vector2(50, 30),
                                                                    Anchor = Anchor.Centre,
                                                                    Origin = Anchor.Centre,
                                                                    ButtonIcon = FontAwesome.Solid.StepBackward,
                                                                    Action = prevTrack,
                                                                    TooltipText = "screen.mvis.main.prevOrRestart",
                                                                },
                                                                songProgressButton = new SongProgressButton
                                                                {
                                                                    TooltipText = "screen.mvis.main.togglePause",
                                                                    Action = togglePause,
                                                                    Anchor = Anchor.Centre,
                                                                    Origin = Anchor.Centre
                                                                },
                                                                nextButton = new NextPrevButton
                                                                {
                                                                    Size = new Vector2(50, 30),
                                                                    Anchor = Anchor.Centre,
                                                                    Origin = Anchor.Centre,
                                                                    ButtonIcon = FontAwesome.Solid.StepForward,
                                                                    Action = nextTrack,
                                                                    TooltipText = "screen.mvis.main.next",
                                                                },
                                                            }
                                                        },
                                                        new FillFlowContainer
                                                        {
                                                            Name = "Right Buttons FillFlow",
                                                            Anchor = Anchor.CentreRight,
                                                            Origin = Anchor.CentreRight,
                                                            AutoSizeAxes = Axes.Both,
                                                            Spacing = new Vector2(5),
                                                            Margin = new MarginPadding { Right = 5 },
                                                            Children = new Drawable[]
                                                            {
                                                                collectionButton = new BottomBarButton
                                                                {
                                                                    ButtonIcon = FontAwesome.Solid.List,
                                                                    TooltipText = "screen.mvis.main.openCollection",
                                                                    Action = () => updateSidebarState(collectionPanel)
                                                                },
                                                                new BottomBarButton
                                                                {
                                                                    ButtonIcon = FontAwesome.Solid.Desktop,
                                                                    Action = () =>
                                                                    {
                                                                        //隐藏界面，锁定更改并隐藏锁定按钮
                                                                        lockChanges.Value = false;
                                                                        hideOverlays(true);

                                                                        updateSidebarState(null);

                                                                        //防止手机端无法恢复界面
                                                                        lockChanges.Value = RuntimeInfo.IsDesktop;
                                                                        lockButton.ToggleableValue.Value = !RuntimeInfo.IsDesktop;
                                                                    },
                                                                    TooltipText = "screen.mvis.main.lockChanges"
                                                                },
                                                                loopToggleButton = new ToggleLoopButton
                                                                {
                                                                    ButtonIcon = FontAwesome.Solid.Undo,
                                                                    Action = () => track.Looping = loopToggleButton.ToggleableValue.Value,
                                                                    TooltipText = "screen.mvis.main.loop",
                                                                },
                                                                soloButton = new BottomBarButton
                                                                {
                                                                    ButtonIcon = FontAwesome.Solid.User,
                                                                    Action = presentBeatmap,
                                                                    TooltipText = "screen.mvis.main.presentBeatmap",
                                                                },
                                                                sidebarToggleButton = new BottomBarButton
                                                                {
                                                                    ButtonIcon = FontAwesome.Solid.Cog,
                                                                    Action = () => updateSidebarState(settingsScroll),
                                                                    TooltipText = "screen.mvis.main.openSettings",
                                                                }
                                                            }
                                                        },
                                                    }
                                                },
                                                progressBar = new SongProgressBar
                                                {
                                                    RelativeSizeAxes = Axes.Both,
                                                },
                                            }
                                        },
                                    }
                                },
                                new Container
                                {
                                    Name = "Lock Button Container",
                                    AutoSizeAxes = Axes.Both,
                                    Anchor = Anchor.BottomCentre,
                                    Origin = Anchor.BottomCentre,
                                    Margin = new MarginPadding { Bottom = 5 },
                                    Child = lockButton = new BottomBarOverlayLockSwitchButton
                                    {
                                        TooltipText = "screen.mvis.main.toggleLock",
                                        Action = updateLockButtonVisuals,
                                        Anchor = Anchor.BottomCentre,
                                        Origin = Anchor.BottomCentre,
                                    }
                                }
                            }
                        }
                    }
                },
            };

            sidebar.Add(settingsScroll = new SidebarSettingsScrollContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Y,
                    RelativeSizeAxes = Axes.X,
                    Spacing = new Vector2(20),
                    Padding = new MarginPadding { Top = 10, Left = 5, Right = 5 },
                    Margin = new MarginPadding { Bottom = 10 },
                    Direction = FillDirection.Vertical,
                    Children = new Drawable[]
                    {
                        new MfMvisSection
                        {
                            Margin = new MarginPadding { Top = -15 },
                            Padding = new MarginPadding(0)
                        },
                        new SettingsButton
                        {
                            Text = "screen.mvis.main.songSelect",
                            Action = () => this.Push(new MvisSongSelect())
                        }
                    }
                },
            });
            sidebar.Add(collectionPanel = new CollectionSelectPanel());

            isIdle.BindTo(idleTracker.IsIdle);
            config.BindWith(MSetting.MvisBgBlur, bgBlur);
            config.BindWith(MSetting.MvisIdleBgDim, idleBgDim);
            config.BindWith(MSetting.MvisContentAlpha, contentAlpha);
            config.BindWith(MSetting.MvisShowParticles, showParticles);
            config.BindWith(MSetting.MvisMusicSpeed, musicSpeed);
            config.BindWith(MSetting.MvisAdjustMusicWithFreq, adjustFreq);
            config.BindWith(MSetting.MvisEnableNightcoreBeat, nightcoreBeat);
            config.BindWith(MSetting.MvisPlayFromCollection, playFromCollection);
            config.BindWith(MSetting.MvisStoryboardProxy, allowProxy);
        }

        protected override void LoadComplete()
        {
            bgBlur.BindValueChanged(v => updateBackground(Beatmap.Value));
            contentAlpha.BindValueChanged(_ => updateIdleVisuals());
            idleBgDim.BindValueChanged(_ => updateIdleVisuals());
            lockChanges.BindValueChanged(v =>
            {
                lockButton.FadeColour(v.NewValue
                    ? Color4.Gray.Opacity(0.6f)
                    : Color4.White, 300);
            });

            Beatmap.BindValueChanged(OnBeatmapChanged, true);

            musicSpeed.BindValueChanged(_ => applyTrackAdjustments());
            adjustFreq.BindValueChanged(_ => applyTrackAdjustments());
            nightcoreBeat.BindValueChanged(_ => applyTrackAdjustments());
            playFromCollection.BindValueChanged(v =>
            {
                //确保Beatmap.Value.Track.Completed中collectionHelper.PlayNextBeatmap只会触发一次
                Beatmap.Value.Track.Completed -= collectionHelper.PlayNextBeatmap;

                if (v.NewValue)
                {
                    Beatmap.Value.Track.Completed += collectionHelper.PlayNextBeatmap;
                    musicController.TrackAdjustTakenOver = true;
                }
                else
                {
                    musicController.TrackAdjustTakenOver = false;
                }
            }, true);

            isIdle.BindValueChanged(v =>
            {
                if (v.NewValue) hideOverlays(false);
            });
            showParticles.BindValueChanged(v =>
            {
                switch (v.NewValue)
                {
                    case true:
                        particles.Child = new SpaceParticlesContainer();
                        break;

                    case false:
                        particles.Clear();
                        break;
                }
            }, true);

            inputManager = GetContainingInputManager();

            progressBar.OnSeek = seekTo;

            songProgressButton.ToggleableValue.BindTo(trackRunning);

            allowProxy.BindValueChanged(v =>
            {
                //如果允许proxy显示
                if (v.NewValue)
                {
                    background.Remove(proxyContainer);
                    foreground.Add(proxyContainer);
                }
                else
                {
                    foreground.Remove(proxyContainer);
                    background.Add(proxyContainer);
                }
            }, true);

            showOverlays(true);

            base.LoadComplete();
        }

        ///<summary>
        ///更新bgTriangles是否可见
        ///</summary>
        private void updateBgTriangles(ValueChangedEvent<bool> value)
        {
            switch (value.NewValue)
            {
                case true:
                    bgTriangles.Hide();
                    break;

                case false:
                    bgTriangles.Show();
                    break;
            }
        }

        private void updateSidebarState(Drawable d)
        {
            if (d == null) sidebar.Hide();
            if (!(d is ISidebarContent)) return;

            var sc = (ISidebarContent)d;

            //如果sc是上一个显示(mvis)或sc是侧边栏的当前显示并且侧边栏未隐藏
            if (sc == sidebar.CurrentDisplay.Value && !sidebar.Hiding)
            {
                sidebar.Hide();
                return;
            }

            sidebar.ShowComponent(d);
        }

        private void seekTo(double position)
        {
            musicController.SeekTo(position);
            sbLoader?.Seek(position);
        }

        protected override void Update()
        {
            base.Update();

            trackRunning.Value = track.IsRunning;
            progressBar.CurrentTime = track.CurrentTime;
            progressBar.EndTime = track.Length;
        }

        public override void OnEntering(IScreen last)
        {
            base.OnEntering(last);

            //各种背景层的动画
            background.FadeOut().Then().Delay(250).FadeIn(500);

            //非背景层的动画
            foreground.ScaleTo(0f).Then().ScaleTo(1f, duration, Easing.OutQuint);
            bottomFillFlow.MoveToY(bottomBar.Height + 30).Then().MoveToY(0, duration, Easing.OutQuint);
            skinnableForeground.FadeIn(duration, Easing.OutQuint);

            playFromCollection.TriggerChange();

            isPushed = true;
        }

        public override bool OnExiting(IScreen next)
        {
            //重置Track
            track.ResetSpeedAdjustments();
            track.Looping = false;
            musicController.TrackAdjustTakenOver = false;

            //停止beatmapLogo，取消故事版家在任务以及锁定变更
            beatmapLogo.StopResponseOnBeatmapChanges();
            lockChanges.Value = true;

            //非背景层的动画
            foreground.ScaleTo(0, duration, Easing.OutQuint);
            bottomFillFlow.MoveToY(bottomBar.Height + 30, duration, Easing.OutQuint);

            this.FadeOut(500, Easing.OutQuint);
            return base.OnExiting(next);
        }

        public override void OnSuspending(IScreen next)
        {
            track.ResetSpeedAdjustments();
            playFromCollection.TriggerChange();

            //背景层的动画
            applyBackgroundBrightness(false, 1);

            //非背景层的动画
            foreground.MoveToX(-DrawWidth, duration, Easing.OutQuint);
            bottomFillFlow.MoveToY(bottomBar.Height + 30, duration, Easing.OutQuint);

            this.FadeOut(duration, Easing.OutQuint);
            beatmapLogo.StopResponseOnBeatmapChanges();
            Beatmap.UnbindEvents();

            base.OnSuspending(next);
        }

        public override void OnResuming(IScreen last)
        {
            base.OnResuming(last);

            musicController.TrackAdjustTakenOver = true;
            collectionHelper.UpdateBeatmaps();
            collectionPanel.RefreshCollectionList();

            this.FadeIn(duration);
            track.ResetSpeedAdjustments();
            applyTrackAdjustments();
            updateBackground(Beatmap.Value);

            Beatmap.BindValueChanged(OnBeatmapChanged, true);
            beatmapLogo.ResponseOnBeatmapChanges();

            //背景层的动画
            background.FadeOut().Then().Delay(250).FadeIn(500);

            //非背景层的动画
            foreground.MoveToX(0, duration, Easing.OutQuint);
            bottomFillFlow.MoveToY(bottomBar.Height + 30).Then().MoveToY(0, duration, Easing.OutQuint);
        }

        public bool OnPressed(GlobalAction action)
        {
            switch (action)
            {
                case GlobalAction.MvisMusicPrev:
                    prevButton.Click();
                    return true;

                case GlobalAction.MvisMusicNext:
                    nextButton.Click();
                    return true;

                case GlobalAction.MvisTogglePause:
                    songProgressButton.Click();
                    return true;

                case GlobalAction.MvisTogglePlayList:
                    sidebarToggleButton.Click();
                    return true;

                case GlobalAction.MvisOpenInSongSelect:
                    soloButton.Click();
                    return true;

                case GlobalAction.MvisToggleOverlayLock:
                    lockButton.Click();
                    return true;

                case GlobalAction.MvisToggleTrackLoop:
                    loopToggleButton.Click();
                    return true;

                case GlobalAction.MvisForceLockOverlayChanges:
                    lockChanges.Toggle();
                    return true;

                case GlobalAction.MvisSelectCollection:
                    collectionButton.Click();
                    return true;
            }

            return false;
        }

        public void OnReleased(GlobalAction action)
        {
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    if (sidebar.IsPresent && !sidebar.Hiding)
                    {
                        sidebar.Hide();
                        return true;
                    }

                    if (!overlaysHidden)
                        this.Exit();

                    return true;
            }

            return base.OnKeyDown(e);
        }

        protected override bool Handle(UIEvent e)
        {
            switch (e)
            {
                case MouseMoveEvent _:
                    showOverlays(false);
                    return base.Handle(e);

                default:
                    return base.Handle(e);
            }
        }

        private void presentBeatmap() =>
            game?.PresentBeatmap(Beatmap.Value.BeatmapSetInfo);

        //当有弹窗或游戏失去焦点时要进行的动作
        protected override void OnHoverLost(HoverLostEvent e)
        {
            if (lockButton.ToggleableValue.Value && overlaysHidden)
                lockButton.Toggle();

            showOverlays(false);
            base.OnHoverLost(e);
        }

        private void updateLockButtonVisuals() =>
            lockButton.FadeIn(500, Easing.OutQuint).Then().Delay(2000).FadeOut(500, Easing.OutQuint);

        private void hideOverlays(bool force)
        {
            if (!force && (!canReallyHide || !isIdle.Value || !IsHovered
                           || bottomBar.Hovered.Value || lockButton.ToggleableValue.Value
                           || lockChanges.Value))
                return;

            skinnableBbBackground.MoveToY(bottomBar.Height, duration, Easing.OutQuint)
                                 .FadeOut(duration, Easing.OutQuint);
            buttonsContainer.MoveToY(bottomBar.Height, duration, Easing.OutQuint);
            progressBar.MoveToY(5, duration, Easing.OutQuint);
            bottomBar.FadeOut(duration, Easing.OutQuint);

            allowCursor = false;
            overlaysHidden = true;
            updateIdleVisuals();
        }

        private void showOverlays(bool force)
        {
            //在有锁并且悬浮界面已隐藏或悬浮界面可见的情况下显示悬浮锁
            if (!force && (lockButton.ToggleableValue.Value && overlaysHidden || !overlaysHidden || lockChanges.Value))
            {
                lockButton.FadeIn(500, Easing.OutQuint).Then().Delay(2500).FadeOut(500, Easing.OutQuint);
                return;
            }

            foreground.FadeTo(1, duration, Easing.OutQuint);

            skinnableBbBackground.MoveToY(0, duration, Easing.OutQuint)
                                 .FadeIn(duration, Easing.OutQuint);
            buttonsContainer.MoveToY(0, duration, Easing.OutQuint);
            progressBar.MoveToY(0, duration, Easing.OutQuint);
            bottomBar.FadeIn(duration, Easing.OutQuint);

            allowCursor = true;
            overlaysHidden = false;

            applyBackgroundBrightness();
        }

        private void togglePause()
        {
            if (track.IsRunning)
                musicController.Stop();
            else
                musicController.Play();
        }

        private void prevTrack()
        {
            if (playFromCollection.Value)
                collectionHelper.PrevTrack();
            else
                musicController.PreviousTrack();
        }

        private void nextTrack()
        {
            if (playFromCollection.Value)
                collectionHelper.NextTrack();
            else
                musicController.NextTrack();
        }

        private void updateIdleVisuals()
        {
            if (!overlaysHidden)
                return;

            applyBackgroundBrightness(true, idleBgDim.Value);
            foreground.FadeTo(contentAlpha.Value, duration, Easing.OutQuint);
        }

        private void applyTrackAdjustments()
        {
            track.ResetSpeedAdjustments();
            track.Looping = loopToggleButton.ToggleableValue.Value;
            track.RestartPoint = 0;
            track.AddAdjustment(adjustFreq.Value ? AdjustableProperty.Frequency : AdjustableProperty.Tempo, musicSpeed);

            if (nightcoreBeat.Value)
                nightcoreBeatContainer.Show();
            else
                nightcoreBeatContainer.Hide();
        }

        private void updateBackground(WorkingBeatmap beatmap)
        {
            if (!isPushed) return;

            ApplyToBackground(bsb =>
            {
                bsb.BlurAmount.Value = bgBlur.Value * 100;
                bsb.Beatmap = beatmap;
            });

            applyBackgroundBrightness();
        }

        /// <summary>
        /// 将屏幕暗化应用到背景层
        /// </summary>
        /// <param name="auto">是否根据情况自动调整.</param>
        /// <param name="brightness">要调整的亮度.</param>
        private void applyBackgroundBrightness(bool auto = true, float brightness = 0)
        {
            if (!this.IsCurrentScreen() || !isPushed) return;

            ApplyToBackground(b =>
            {
                if (auto)
                {
                    b.FadeColour((sbLoader?.StoryboardReplacesBackground.Value ?? false) ? Color4.Black : OsuColour.Gray(overlaysHidden ? idleBgDim.Value : 0.6f),
                        duration,
                        Easing.OutQuint);
                }
                else
                    b.FadeColour(OsuColour.Gray(brightness), duration, Easing.OutQuint);
            });

            sbLoader?.FadeColour(OsuColour.Gray(auto ? (overlaysHidden ? idleBgDim.Value : 0.6f) : brightness), duration, Easing.OutQuint);
        }

        private void OnBeatmapChanged(ValueChangedEvent<WorkingBeatmap> v)
        {
            var beatmap = v.NewValue;
            playFromCollection.TriggerChange();

            Schedule(() =>
            {
                applyTrackAdjustments();
                updateBackground(beatmap);
            });

            if (beatmap != prevBeatmap)
            {
                sbLoader?.FadeOut(BackgroundStoryBoardLoader.STORYBOARD_FADEOUT_DURATION, Easing.OutQuint).Expire();
                sbLoader = null;
                background.Add(sbLoader = new BackgroundStoryBoardLoader(beatmap)
                {
                    OnNewStoryboardLoaded = () =>
                    {
                        if (prevProxy != null)
                        {
                            proxyContainer.Remove(prevProxy);
                            prevProxy.Expire();
                        }

                        prevProxy = sbLoader.StoryboardProxy;

                        if (prevProxy != null) proxyContainer.Add(prevProxy);
                        prevProxy?.Show();
                    }
                });
                reBind();
            }

            activity.Value = new UserActivity.InMvis(beatmap.BeatmapInfo);
            prevBeatmap = beatmap;
        }

        private void reBind()
        {
            sbLoader.NeedToHideTriangles.BindValueChanged(updateBgTriangles, true);
            sbLoader.State.BindValueChanged(v =>
            {
                switch (v.NewValue)
                {
                    case StoryboardState.Success:
                    case StoryboardState.NotLoaded:
                        loadingSpinner.Hide();
                        break;

                    case StoryboardState.Loading:
                        loadingSpinner.Show();
                        loadingSpinner.FadeColour(Color4.White, 300);
                        break;
                }
            }, true);

            sbLoader.StoryboardReplacesBackground.BindValueChanged(_ => applyBackgroundBrightness());
        }
    }
}
