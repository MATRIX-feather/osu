using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Game.Beatmaps;
using osu.Game.Collections;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osuTK;

namespace osu.Game.Screens.Mvis.Modules.v2
{
    public class CollectionPanel : Container
    {
        ///<summary>
        ///判断该panel所显示的BeatmapCollection
        ///</summary>
        public readonly BeatmapCollection collection;

        ///<summary>
        ///用于触发<see cref="CollectionSelectPanel"/>的SelectedCollection变更
        ///</summary>
        public Bindable<BeatmapCollection> SelectedCollection = new Bindable<BeatmapCollection>();

        private List<BeatmapSetInfo> beatmapSets = new List<BeatmapSetInfo>();

        [Resolved]
        private BeatmapManager beatmaps { get; set; }

        private OsuSpriteText collectionName;
        private OsuSpriteText collectionBeatmapCount;
        private OsuScrollContainer thumbnailScroll;
        private Action doubleClick;

        public Bindable<ActiveState> state = new Bindable<ActiveState>();
        private Box stateBox;

        /// <summary>
        /// ...
        /// </summary>
        /// <param name="c">要设定的<see cref="BeatmapCollection"/></param>
        /// <param name="doubleClickAction">双击(再次选中)时执行的动作</param>
        public CollectionPanel(BeatmapCollection c, Action doubleClickAction)
        {
            BorderColour = Colour4.White;
            RelativeSizeAxes = Axes.X;
            Height = 150;
            Masking = true;
            CornerRadius = 12.5f;

            collection = c;
            doubleClick = doubleClickAction;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = ColourInfo.GradientVertical(
                        Color4Extensions.FromHex("#111").Opacity(0),
                        Color4Extensions.FromHex("#111")
                    ),
                },
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4Extensions.FromHex("#111").Opacity(0.6f),
                },
                new Container
                {
                    Name = "标题容器",
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Padding = new MarginPadding(15),
                    Children = new Drawable[]
                    {
                        new CircularContainer
                        {
                            RelativeSizeAxes = Axes.Y,
                            Width = 5,
                            Anchor = Anchor.TopLeft,
                            Origin = Anchor.TopLeft,
                            Masking = true,
                            Child = stateBox = new Box
                            {
                                RelativeSizeAxes = Axes.Both
                            }
                        },
                        new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(4),
                            Padding = new MarginPadding{Left = 15},
                            Children = new Drawable[]
                            {
                                collectionName = new OsuSpriteText
                                {
                                    Font = OsuFont.GetFont(size: 30),
                                    Text = "???"
                                },
                                collectionBeatmapCount = new OsuSpriteText
                                {
                                    Text = "???"
                                }
                            }
                        }
                    }
                },
                thumbnailScroll = new OsuScrollContainer(Direction.Horizontal)
                {
                    Name = "谱面图标容器",
                    Padding = new MarginPadding(15),
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Width = 0.98f,
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    ScrollbarVisible = false,
                    Child = new BeatmapThumbnailFlow(beatmapSets)
                }
            };

            thumbnailScroll.ScrollContent.RelativeSizeAxes = Axes.None;
            thumbnailScroll.ScrollContent.AutoSizeAxes = Axes.Both;

            SortBeatmapCollection();

            if (beatmapSets.Count > 0)
            {
                Add(new BeatmapCover(beatmaps.GetWorkingBeatmap(beatmapSets.ElementAt(0).Beatmaps.First()))
                {
                    Depth = float.MaxValue
                });
                state.Value = ActiveState.Idle;
            }
            else
                state.Value = ActiveState.Disabled;

            collectionName.Text = collection.Name.Value;
            collectionBeatmapCount.Text = $"{beatmapSets.Count}首歌曲, {collection.Beatmaps.Count}个谱面";

            state.BindValueChanged(OnStateChanged, true);
        }

        private void OnStateChanged(ValueChangedEvent<ActiveState> v)
        {
            switch( v.NewValue )
            {
                case ActiveState.Active:
                    BorderThickness = 3f;
                    BorderColour = Color4Extensions.FromHex(@"88b300");
                    stateBox.FadeColour(Color4Extensions.FromHex("#88b300"), 300, Easing.OutQuint);
                    break;

                case ActiveState.Disabled:
                    Height = 1;
                    AutoSizeAxes = Axes.Y;
                    BorderThickness = 0f;
                    Colour = Color4Extensions.FromHex("#555");
                    stateBox.FadeColour(Colour4.Gray, 300, Easing.OutQuint);
                    thumbnailScroll.Hide();
                    break;
                
                case ActiveState.Selected:
                    BorderThickness = 3f;
                    BorderColour = Colour4.White;
                    break;

                default:
                case ActiveState.Idle:
                    BorderThickness = 0f;
                    BorderColour = Colour4.White;
                    stateBox.FadeColour(Colour4.White, 300, Easing.OutQuint);
                    break;
            }
        }

        private void SortBeatmapCollection()
        {
            //From CollectionHelper.cs
            foreach (var item in collection.Beatmaps)
            {
                //获取当前BeatmapSet
                var currentSet = item.BeatmapSet;

                //进行比对，如果beatmapList中不存在，则添加。
                if (!beatmapSets.Contains(currentSet))
                    beatmapSets.Add(currentSet);
            }
        }

        protected override bool OnHover(HoverEvent e)
        {
            if ( state.Value == ActiveState.Idle )
                BorderThickness = 1.5f;

            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            if ( state.Value == ActiveState.Idle )
                BorderThickness = 0f;

            base.OnHoverLost(e);
        }

        protected override bool OnClick(ClickEvent e)
        {
            if ( state.Value == ActiveState.Disabled )
                return base.OnClick(e);

            //如果已经被选中了，则触发双击
            if ( state.Value == ActiveState.Selected )
            {
                doubleClick?.Invoke();
                state.Value = ActiveState.Active;

                return base.OnClick(e);
            }
            
            //使SelectedCollection的值变为collection, 从而出触发CollectionSelectPanel的UpdateSelection
            SelectedCollection.Value = collection;

            if ( state.Value != ActiveState.Active )
                state.Value = ActiveState.Selected;

            return base.OnClick(e);
        }

        /// <summary>
        /// 重置状态
        /// </summary>
        public void Reset(bool makeInactive = false)
        {
            if ( state.Value != ActiveState.Disabled && state.Value != ActiveState.Active ||
                 state.Value != ActiveState.Disabled && makeInactive )
            {
                state.Value = ActiveState.Idle;
            }
        }

        private class BeatmapThumbnailFlow : FillFlowContainer
        {

            [Resolved]
            private BeatmapManager beatmaps { get; set; }

            private List<BeatmapSetInfo> beatmapSetList;

            public BeatmapThumbnailFlow(List<BeatmapSetInfo> list)
            {
                beatmapSetList = list;
                Direction = FillDirection.Horizontal;
                Spacing = new Vector2(5);
                AutoSizeAxes = Axes.Both;
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                AddBeatmapThumbnails();
            }

            private void AddBeatmapThumbnails()
            {
                foreach (var c in beatmapSetList)
                {
                    var b = beatmaps.GetWorkingBeatmap(c.Beatmaps.First());

                    Add(new TooltipContainer
                    {
                        Size = new Vector2(40),
                        Masking = true,
                        CornerRadius = 7.25f,
                        TooltipText = b.BeatmapInfo.Metadata.TitleUnicode ?? b.BeatmapInfo.Metadata.Title ?? "亲, 您这歌没有标题",
                        Child = new BeatmapCover(b)
                    });
                };
            }
        }
    }

    public enum ActiveState
    {
        Disabled,
        Idle,
        Selected,
        Active,
    }
}