using System;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Timing;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Logging;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Storyboards.Drawables;

namespace osu.Game.Screens.Mvis.Storyboard
{
    public class BackgroundStoryBoardLoader : Container
    {
        private const float DURATION = 750;
        private CancellationTokenSource ChangeSB;
        private BindableBool EnableSB = new BindableBool();
        ///<summary>
        ///用于内部确定故事版是否已加载
        ///</summary>
        private BindableBool SBLoaded = new BindableBool();

        ///<summary>
        ///用于对外提供该BindableBool用于检测故事版功能是否已经准备好了
        ///</summary>
        public readonly BindableBool IsReady = new BindableBool();
        public readonly BindableBool NeedToHideTriangles = new BindableBool();
        public readonly BindableBool storyboardReplacesBackground = new BindableBool();

        /// <summary>
        /// This will log which beatmap's storyboard we are loading
        /// </summary>
        private Task LogTask;

        /// <summary>
        /// This will invoke LoadSBTask and run asyncly
        /// </summary>
        private Task LoadSBAsyncTask;

        /// <summary>
        /// This will be invoked by LoadSBAsyncTask and loads the current beatmap's storyboard
        /// </summary>
        private Task LoadSBTask;

        /// <summary>
        /// 当准备的故事版加载完毕时要调用的Action
        /// </summary>
        private Action OnComplete;

        private DrawableStoryboard storyboard;
        private StoryboardClock StoryboardClock = new StoryboardClock();
        private Container ClockContainer;

        public Drawable GetOverlayProxy()
        {
            var proxy = storyboard.OverlayLayer.CreateProxy();
            return proxy;
        }

        [Resolved]
        private IBindable<WorkingBeatmap> b { get; set; }

        public BackgroundStoryBoardLoader()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load(MfConfigManager config)
        {
            config.BindWith(MfSetting.MvisEnableStoryboard, EnableSB);
        }

        protected override void LoadComplete()
        {
            EnableSB.BindValueChanged(_ => UpdateVisuals());
        }

        public void UpdateVisuals()
        {
            if ( EnableSB.Value )
            {
                if ( !SBLoaded.Value )
                    UpdateStoryBoardAsync();
                else
                {
                    storyboardReplacesBackground.Value = b.Value.Storyboard.ReplacesBackground && b.Value.Storyboard.HasDrawable;
                    NeedToHideTriangles.Value = b.Value.Storyboard.HasDrawable;
                }

                ClockContainer?.FadeIn(DURATION, Easing.OutQuint);
            }
            else
            {
                ClockContainer?.FadeOut(DURATION / 2, Easing.OutQuint);
                storyboardReplacesBackground.Value = false;
                NeedToHideTriangles.Value = false;
                IsReady.Value = true;
                CancelAllTasks();
            }
        }

        public bool UpdateComponent(WorkingBeatmap beatmap)
        {
            try
            {
                StoryboardClock.Stop();

                if ( ClockContainer != null )
                {
                    ClockContainer.Clock = new ThrottledFrameClock();

                    if ( storyboard != null )
                        storyboard.Clock = StoryboardClock;

                    ClockContainer.FadeOut(DURATION, Easing.OutQuint);
                    ClockContainer.Expire();
                    ClockContainer = null;
                }
    
                storyboard = null;

                LoadSBTask = LoadComponentAsync(new Container
                {
                    Name = "Storyboard Container",
                    RelativeSizeAxes = Axes.Both,
                    Alpha = 0,
                    Clock = StoryboardClock = new StoryboardClock(),
                    Child = storyboard = beatmap.Storyboard.CreateDrawable()
                }, newClockContainer =>
                {
                    StoryboardClock.ChangeSource(beatmap.Track);

                    this.Add(newClockContainer);
                    ClockContainer = newClockContainer;

                    SBLoaded.Value = true;
                    IsReady.Value = true;
                    NeedToHideTriangles.Value = beatmap.Storyboard.HasDrawable;

                    UpdateVisuals();
                    OnComplete?.Invoke();

                    Logger.Log($"Load Storyboard for Beatmap \"{beatmap.BeatmapSetInfo}\" complete!");
                }, (ChangeSB = new CancellationTokenSource()).Token);
            }
            catch (Exception e)
            {
                Logger.Error(e, "加载Storyboard时出现错误! 请检查你的谱面!");
                return false;
            }

            return true;
        }

        public void CancelAllTasks()
        {
            ChangeSB?.Cancel();

            LoadSBTask = null;
            LoadSBAsyncTask = null;
            LogTask = null;
        }

        public void UpdateStoryBoardAsync( Action OnComplete = null )
        {
            if ( b == null )
                return;

            CancelAllTasks();
            IsReady.Value = false;
            SBLoaded.Value = false;
            NeedToHideTriangles.Value = false;

            if ( !EnableSB.Value )
            {
                IsReady.Value = true;
                return;
            }

            Schedule(() =>
            {
                this.OnComplete = OnComplete;
                LoadSBAsyncTask = Task.Run( async () =>
                {
                    Logger.Log($"Loading Storyboard for Beatmap \"{b.Value.BeatmapSetInfo}\"...");

                    storyboardReplacesBackground.Value = false;

                    LogTask = Task.Run( () => 
                    {
                        UpdateComponent(b.Value);
                    });

                    await LogTask;
                });
            });
        }
    }
}