using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace osu.Game.Overlays.OnlinePicture
{
    public class OnlinePictureContentContainer : Container
    {
        public Func<float> GetTopBarHeight;
        public Func<float> GetBottomContainerHeight;

        public OnlinePictureContentContainer()
        {
            Name = "Content Container";
            RelativeSizeAxes = Axes.Both;
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            Padding = new MarginPadding { Bottom = GetBottomContainerHeight?.Invoke() ?? 0, Top = GetTopBarHeight?.Invoke() ?? 0 };
        }
    }
}