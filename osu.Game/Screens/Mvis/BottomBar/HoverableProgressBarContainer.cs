using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Events;
using osu.Game.Graphics.UserInterface;
using osu.Game.Screens.Mvis.Modules;

namespace osu.Game.Screens.Mvis.UI
{
    public class HoverableProgressBarContainer : Container
    {
        protected const float idle_alpha = 0.5f;
        public ProgressBar progressBar;

        [Resolved]
        private CustomColourProvider colourProvider { get; set; }

        [BackgroundDependencyLoader]
        private void load()
        {
            Children = new Drawable[]
            {
                progressBar = new HoverableProgressBar
                {
                    Origin = Anchor.BottomCentre,
                    Anchor = Anchor.BottomCentre,
                    RelativeSizeAxes = Axes.Both,
                    Height = 0.1f,
                    FillColour = colourProvider.Highlight1,
                    BackgroundColour = colourProvider.Background3.Opacity(0.5f),
                    Alpha = idle_alpha,
                },
            };
        }

        private class HoverableProgressBar : ProgressBar
        {
            protected override bool OnHover(HoverEvent e)
            {
                this.ResizeHeightTo(0.15f, 500, Easing.OutQuint).FadeTo(1, 500, Easing.OutQuint);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                this.ResizeHeightTo(0.2f / 2, 500, Easing.OutQuint).FadeTo(idle_alpha, 500, Easing.OutQuint);
                base.OnHoverLost(e);
            }
        }
    }
}