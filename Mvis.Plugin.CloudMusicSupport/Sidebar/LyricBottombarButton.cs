using osu.Framework.Graphics.Sprites;
using osu.Game.Screens.Mvis.Plugins;

namespace Mvis.Plugin.CloudMusicSupport.Sidebar
{
    public class LyricBottombarButton : PluginBottomBarButton
    {
        public LyricBottombarButton(PluginSidebarPage page)
            : base(page)
        {
            ButtonIcon = FontAwesome.Solid.Music;
            TooltipText = "打开歌词面板";
        }
    }
}
