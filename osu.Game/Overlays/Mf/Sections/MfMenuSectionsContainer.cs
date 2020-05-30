using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics.Containers;
using osuTK;

namespace osu.Game.Overlays.MfMenu.Sections
{
    public class MfMenuSectionsContainer : SectionsContainer<MfMenuSection>
    {
        public MfMenuSectionsContainer()
        {
            RelativeSizeAxes = Axes.Both;
        }

        protected override OsuScrollContainer CreateScrollContainer() => new OverlayScrollContainer();

        protected override FlowContainer<MfMenuSection> CreateScrollContentContainer() => new FillFlowContainer<MfMenuSection>
        {
            Direction = FillDirection.Vertical,
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Spacing = new Vector2(0, 40),
            Margin = new MarginPadding{Bottom = 50},
        };
    }
}