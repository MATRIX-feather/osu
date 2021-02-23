// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Replays;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Configuration;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Replays.Movers;
using osu.Game.Rulesets.Osu.UI;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays
{
    public class OsuDanceAutoGenerator : OsuAutoGeneratorBase
    {
        public static DanceMover GetMover(OsuDanceMover mover) =>
            mover switch
            {
                OsuDanceMover.HalfCircle => new HalfCircleMover(),
                OsuDanceMover.Flower => new FlowerMover(),
                OsuDanceMover.Momentum => new MomentumMover(),
                _ => new MomentumMover()
            };

        public new OsuBeatmap Beatmap => (OsuBeatmap)base.Beatmap;
        private readonly bool sliderDance;
        private readonly bool[] objectsDuring;
        private readonly DanceMover mover;
        private readonly float spinRadiusStart;
        private readonly float spinRadiusEnd;
        private readonly OsuRulesetConfigManager config;
        private readonly double frameDelay;
        private int buttonIndex;

        private OsuAction getAction(OsuHitObject h, OsuHitObject last)
        {
            double timeDifference = ApplyModsToTimeDelta(last.StartTime, h.StartTime);

            if (timeDifference > 0 && timeDifference >= 300)
            {
                buttonIndex = 0;
            }
            else
            {
                buttonIndex++;
            }

            return buttonIndex % 2 == 0 ? OsuAction.LeftButton : OsuAction.RightButton;
        }

        public OsuDanceAutoGenerator(IBeatmap beatmap, IReadOnlyList<Mod> mods)
            : base(beatmap, mods)
        {
            config = OsuRulesetConfigManager.Instance;
            frameDelay = 1000.0 / config.Get<float>(OsuRulesetSetting.ReplayFramerate);
            spinRadiusStart = config.Get<float>(OsuRulesetSetting.SpinnerRadiusStart);
            sliderDance = config.Get<bool>(OsuRulesetSetting.SliderDance);
            spinRadiusEnd = config.Get<float>(OsuRulesetSetting.SpinnerRadiusEnd);
            mover = GetMover(config.Get<OsuDanceMover>(OsuRulesetSetting.DanceMover));

            mover.Beatmap = Beatmap;

            var moverObjectsDuring = new bool[Beatmap.HitObjects.Count];

            for (int i = 0; i < Beatmap.HitObjects.Count - 1; ++i)
            {
                var e = Beatmap.HitObjects[i].GetEndTime();
                moverObjectsDuring[i] = false;

                for (int j = i + 1; j < Beatmap.HitObjects.Count; ++j)
                {
                    if (Beatmap.HitObjects[j].StartTime + 1 > e) continue;

                    moverObjectsDuring[i] = true;
                    break;
                }
            }

            objectsDuring = moverObjectsDuring;
            mover.ObjectsDuring = moverObjectsDuring;
        }

        private void moveToHitObject(OsuHitObject h, int idx, bool sliderDance)
        {
            OsuHitObject last = Beatmap.HitObjects[idx == 0 ? idx : idx - 1];
            OsuAction action = getAction(h, last);
            Vector2 startPosition = h.StackedPosition;
            Vector2 difference = startPosition - SPINNER_CENTRE;
            float radius = difference.Length;
            float angle = radius == 0 ? 0 : MathF.Atan2(difference.Y, difference.X);

            switch (h)
            {
                case Slider slider:
                    AddFrameToReplay(new OsuReplayFrame(slider.StartTime, slider.StackedPosition, action));

                    if (objectsDuring[idx]) break;

                    double speed = slider.Distance / slider.Duration;

                    for (double j = GetFrameDelay(slider.StartTime); j < slider.Duration; j += GetFrameDelay(slider.StartTime + j))
                    {
                        bool canDance = sliderDance && slider.Duration > 350;
                        Vector2 pos = slider.StackedPositionAt(j / slider.Duration);
                        Vector2 pos2 = pos + CirclePosition(ApplyModsToTimeDelta(last.GetEndTime(), h.StartTime) / 12 * speed + angle, 20);
                        AddFrameToReplay(new OsuReplayFrame((int)h.StartTime + j, canDance ? pos2 : pos, action));
                    }

                    break;

                case Spinner spinner:
                    if (spinner.SpinsRequired == 0) return;

                    double r = spinner.SpinsRequired > 3 ? spinRadiusStart : spinRadiusEnd;
                    double r1;
                    double rEndTime = spinner.StartTime + spinner.Duration * 0.6;
                    double previousFrame = h.StartTime;

                    for (double nextFrame = h.StartTime + GetFrameDelay(h.StartTime); nextFrame < spinner.EndTime; nextFrame += GetFrameDelay(nextFrame))
                    {
                        var t = ApplyModsToTimeDelta(previousFrame, nextFrame) * -1;
                        angle += (float)t / 20;
                        r1 = nextFrame > rEndTime ? spinRadiusEnd : Interpolation.ValueAt(nextFrame, r, spinRadiusEnd, spinner.StartTime, rEndTime, Easing.In);
                        Vector2 pos = SPINNER_CENTRE + CirclePosition(angle, r1);
                        AddFrameToReplay(new OsuReplayFrame((int)nextFrame, new Vector2(pos.X, pos.Y), action));

                        previousFrame = nextFrame;
                    }

                    break;

                case HitCircle circle:
                    AddFrameToReplay(new OsuReplayFrame(circle.StartTime, circle.StackedPosition, action));
                    break;

                default: return;
            }
        }

        public Replay GenerateReplay(bool sliderDance)
        {
            OsuHitObject hitObject = Beatmap.HitObjects[0];
            AddFrameToReplay(new OsuReplayFrame(-10000, hitObject.StackedPosition));
            AddFrameToReplay(new OsuReplayFrame(Beatmap.HitObjects[0].StartTime, hitObject.StackedPosition));

            Vector2 baseSize = OsuPlayfield.BASE_SIZE;

            float xf = baseSize.X / 0.8f * (4f / 3f);
            float x0 = (baseSize.X - xf) / 2f;
            float x1 = xf + x0;

            float yf = baseSize.Y / 0.8f;
            float y0 = (baseSize.Y - yf) / 2f;
            float y1 = yf + y0;

            for (int i = 0; i < Beatmap.HitObjects.Count - 1; i++)
            {
                OsuReplayFrame lastFrame = (OsuReplayFrame)Frames[^1];
                hitObject = Beatmap.HitObjects[i];
                moveToHitObject(hitObject, i, sliderDance);

                mover.ObjectIndex = i;
                mover.OnObjChange();

                for (double time = (objectsDuring[i] ? hitObject.StartTime : hitObject.GetEndTime()) + frameDelay; time < mover.End.StartTime; time += frameDelay)
                {
                    Start:
                    var timeToNext = mover.End.StartTime - time;

                    if (timeToNext > 3000 && lastFrame.Time < mover.End.StartTime - timeToNext * 0.6)
                    {
                        AddFrameToReplay(new OsuReplayFrame(hitObject.GetEndTime(), hitObject.StackedEndPosition));
                        AddFrameToReplay(new OsuReplayFrame(mover.End.StartTime - timeToNext * 0.8, hitObject.StackedEndPosition));
                        time = mover.End.StartTime - timeToNext * 0.6;
                        goto Start;
                    }

                    Vector2 currentPosition = mover.Update(time);

                    if (config.Get<bool>(OsuRulesetSetting.BorderBounce))
                    {
                        if (currentPosition.X < x0) currentPosition.X = x0 - (currentPosition.X - x0);
                        if (currentPosition.Y < y0) currentPosition.Y = y0 - (currentPosition.Y - y0);

                        if (currentPosition.X > x1)
                        {
                            float x = currentPosition.X - x0;
                            int m = (int)(x / xf);
                            x %= xf;
                            x = m % 2 == 0 ? x : xf - x;
                            currentPosition.X = x + x0;
                        }

                        if (currentPosition.Y > y1)
                        {
                            float y = currentPosition.Y - y0;
                            float m = (int)(y / yf);
                            y %= yf;
                            y = m % 2 == 0 ? y : yf - y;
                            currentPosition.Y = y + y0;
                        }
                    }

                    AddFrameToReplay(new OsuReplayFrame(time, currentPosition));
                }
            }

            moveToHitObject(Beatmap.HitObjects[^1], Beatmap.HitObjects.Count - 1, sliderDance);

            AddFrameToReplay(new OsuReplayFrame(Beatmap.HitObjects[^1].GetEndTime(), Beatmap.HitObjects[^1].StackedEndPosition));

            return Replay;
        }

        public override Replay Generate()
        {
            return GenerateReplay(sliderDance);
        }
    }

    public abstract class DanceMover
    {
        protected double StartTime => Start.GetEndTime();
        protected double EndTime => End.StartTime;
        protected double Duration => EndTime - StartTime;

        protected Vector2 StartPos => ObjectsDuring[ObjectIndex] ? Start.StackedPosition : Start.StackedEndPosition;
        protected Vector2 EndPos => End.StackedPosition;
        protected float StartX => StartPos.X;
        protected float StartY => StartPos.Y;
        protected float EndX => EndPos.X;
        protected float EndY => EndPos.Y;

        protected float T(double time) => (float)((time - StartTime) / Duration);

        public bool[] ObjectsDuring { set; protected get; }

        public int ObjectIndex { set; protected get; }
        public OsuBeatmap Beatmap { set; protected get; }

        public OsuHitObject Start
        {
            get
            {
                if (Beatmap.HitObjects[ObjectIndex] is Spinner spinner && spinner.SpinsRequired == 0) return Beatmap.HitObjects[ObjectIndex == 0 ? ObjectIndex : ObjectIndex - 1];

                return Beatmap.HitObjects[ObjectIndex];
            }
        }

        public OsuHitObject End
        {
            get
            {
                if (Beatmap.HitObjects[ObjectIndex + 1] is Spinner spinner && spinner.SpinsRequired == 0) return Beatmap.HitObjects[ObjectIndex == Beatmap.HitObjects.Count ? ObjectIndex - 1 : ObjectIndex];

                return Beatmap.HitObjects[ObjectIndex + 1];
            }
        }

        public virtual void OnObjChange() { }
        public abstract Vector2 Update(double time);
    }
}
