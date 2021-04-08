// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Audio.Track;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Audio;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Rulesets.Edit;
using osuTK;

namespace osu.Game.Screens.Edit.Compose.Components.Timeline
{
    [Cached(typeof(IPositionSnapProvider))]
    [Cached]
    public class Timeline : ZoomableScrollContainer, IPositionSnapProvider
    {
        public readonly Bindable<bool> WaveformVisible = new Bindable<bool>();

        public readonly Bindable<bool> ControlPointsVisible = new Bindable<bool>();

        public readonly Bindable<bool> TicksVisible = new Bindable<bool>();

        public readonly IBindable<WorkingBeatmap> Beatmap = new Bindable<WorkingBeatmap>();

        [Resolved]
        private EditorClock editorClock { get; set; }

        /// <summary>
        /// The timeline's scroll position in the last frame.
        /// </summary>
        private float lastScrollPosition;

        /// <summary>
        /// The track time in the last frame.
        /// </summary>
        private double lastTrackTime;

        /// <summary>
        /// Whether the user is currently dragging the timeline.
        /// </summary>
        private bool handlingDragInput;

        /// <summary>
        /// Whether the track was playing before a user drag event.
        /// </summary>
        private bool trackWasPlaying;

        private Track track;

        public Timeline()
        {
            ZoomDuration = 200;
            ZoomEasing = Easing.OutQuint;
            ScrollbarVisible = false;
        }

        private WaveformGraph waveform;

        private TimelineTickDisplay ticks;

        private TimelineControlPointDisplay controlPoints;

        private Bindable<float> waveformOpacity;

        [BackgroundDependencyLoader]
        private void load(IBindable<WorkingBeatmap> beatmap, OsuColour colours, OsuConfigManager config)
        {
            AddRange(new Drawable[]
            {
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Depth = float.MaxValue,
                    Children = new Drawable[]
                    {
                        waveform = new WaveformGraph
                        {
                            RelativeSizeAxes = Axes.Both,
                            BaseColour = colours.Blue.Opacity(0.2f),
                            LowColour = colours.BlueLighter,
                            MidColour = colours.BlueDark,
                            HighColour = colours.BlueDarker,
                        },
                        ticks = new TimelineTickDisplay(),
                        controlPoints = new TimelineControlPointDisplay(),
                        new Box
                        {
                            Name = "zero marker",
                            RelativeSizeAxes = Axes.Y,
                            Width = 2,
                            Origin = Anchor.TopCentre,
                            Colour = colours.YellowDarker,
                        },
                    }
                },
            });

            // We don't want the centre marker to scroll
            AddInternal(new CentreMarker { Depth = float.MaxValue });

            waveformOpacity = config.GetBindable<float>(OsuSetting.EditorWaveformOpacity);
            waveformOpacity.BindValueChanged(_ => updateWaveformOpacity(), true);

            WaveformVisible.ValueChanged += _ => updateWaveformOpacity();
            ControlPointsVisible.ValueChanged += visible => controlPoints.FadeTo(visible.NewValue ? 1 : 0, 200, Easing.OutQuint);
            TicksVisible.ValueChanged += visible => ticks.FadeTo(visible.NewValue ? 1 : 0, 200, Easing.OutQuint);

            Beatmap.BindTo(beatmap);
            Beatmap.BindValueChanged(b =>
            {
                waveform.Waveform = b.NewValue.Waveform;
                track = b.NewValue.Track;

                // todo: i don't think this is safe, the track may not be loaded yet.
                if (track.Length > 0)
                {
                    MaxZoom = getZoomLevelForVisibleMilliseconds(500);
                    MinZoom = getZoomLevelForVisibleMilliseconds(10000);
                    Zoom = getZoomLevelForVisibleMilliseconds(2000);
                }
            }, true);
        }

        private void updateWaveformOpacity() =>
            waveform.FadeTo(WaveformVisible.Value ? waveformOpacity.Value : 0, 200, Easing.OutQuint);

        private float getZoomLevelForVisibleMilliseconds(double milliseconds) => Math.Max(1, (float)(track.Length / milliseconds));

        protected override void Update()
        {
            base.Update();

            // The extrema of track time should be positioned at the centre of the container when scrolled to the start or end
            Content.Margin = new MarginPadding { Horizontal = DrawWidth / 2 };

            // This needs to happen after transforms are updated, but before the scroll position is updated in base.UpdateAfterChildren
            if (editorClock.IsRunning)
                scrollToTrackTime();
        }

        protected override bool OnScroll(ScrollEvent e)
        {
            // if this is not a precision scroll event, let the editor handle the seek itself (for snapping support)
            if (!e.AltPressed && !e.IsPrecise)
                return false;

            return base.OnScroll(e);
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            if (handlingDragInput)
                seekTrackToCurrent();
            else if (!editorClock.IsRunning)
            {
                // The track isn't running. There are three cases we have to be wary of:
                // 1) The user flick-drags on this timeline and we are applying an interpolated seek on the clock, until interrupted by 2 or 3.
                // 2) The user changes the track time through some other means (scrolling in the editor or overview timeline; clicking a hitobject etc.). We want the timeline to track the clock's time.
                // 3) An ongoing seek transform is running from an external seek. We want the timeline to track the clock's time.

                // The simplest way to cover the first two cases is by checking whether the scroll position has changed and the audio hasn't been changed externally
                // Checking IsSeeking covers the third case, where the transform may not have been applied yet.
                if (Current != lastScrollPosition && editorClock.CurrentTime == lastTrackTime && !editorClock.IsSeeking)
                    seekTrackToCurrent();
                else
                    scrollToTrackTime();
            }

            lastScrollPosition = Current;
            lastTrackTime = editorClock.CurrentTime;
        }

        private void seekTrackToCurrent()
        {
            if (!track.IsLoaded)
                return;

            double target = Current / Content.DrawWidth * track.Length;
            editorClock.Seek(Math.Min(track.Length, target));
        }

        private void scrollToTrackTime()
        {
            if (!track.IsLoaded || track.Length == 0)
                return;

            // covers the case where the user starts playback after a drag is in progress.
            // we want to ensure the clock is always stopped during drags to avoid weird audio playback.
            if (handlingDragInput)
                editorClock.Stop();

            ScrollTo((float)(editorClock.CurrentTime / track.Length) * Content.DrawWidth, false);
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            if (base.OnMouseDown(e))
            {
                beginUserDrag();
                return true;
            }

            return false;
        }

        protected override void OnMouseUp(MouseUpEvent e)
        {
            endUserDrag();
            base.OnMouseUp(e);
        }

        private void beginUserDrag()
        {
            handlingDragInput = true;
            trackWasPlaying = editorClock.IsRunning;
            editorClock.Stop();
        }

        private void endUserDrag()
        {
            handlingDragInput = false;
            if (trackWasPlaying)
                editorClock.Start();
        }

        [Resolved]
        private EditorBeatmap beatmap { get; set; }

        [Resolved]
        private IBeatSnapProvider beatSnapProvider { get; set; }

        /// <summary>
        /// The total amount of time visible on the timeline.
        /// </summary>
        public double VisibleRange => track.Length / Zoom;

        public SnapResult SnapScreenSpacePositionToValidPosition(Vector2 screenSpacePosition) =>
            new SnapResult(screenSpacePosition, null);

        public SnapResult SnapScreenSpacePositionToValidTime(Vector2 screenSpacePosition) =>
            new SnapResult(screenSpacePosition, beatSnapProvider.SnapTime(getTimeFromPosition(Content.ToLocalSpace(screenSpacePosition))));

        private double getTimeFromPosition(Vector2 localPosition) =>
            (localPosition.X / Content.DrawWidth) * track.Length;

        public float GetBeatSnapDistanceAt(double referenceTime) => throw new NotImplementedException();

        public float DurationToDistance(double referenceTime, double duration) => throw new NotImplementedException();

        public double DistanceToDuration(double referenceTime, float distance) => throw new NotImplementedException();

        public double GetSnappedDurationFromDistance(double referenceTime, float distance) => throw new NotImplementedException();

        public float GetSnappedDistanceFromDistance(double referenceTime, float distance) => throw new NotImplementedException();
    }
}