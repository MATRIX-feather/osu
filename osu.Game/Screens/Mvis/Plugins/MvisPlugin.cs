using System.Threading;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Screens.Mvis.SideBar;

namespace osu.Game.Screens.Mvis.Plugins
{
    public abstract class MvisPlugin : Container
    {
        protected abstract Drawable CreateContent();
        protected abstract bool OnContentLoaded(Drawable content);
        protected abstract bool PostInit();
        public virtual PluginSidebarPage SidebarPage => null;

        public string Description = "插件描述";
        public string Author = "插件作者";
        public int Version = 0;

        [CanBeNull]
        [Resolved(CanBeNull = true)]
        private MvisScreen mvisScreen { get; set; }

        [Resolved]
        private Sidebar sidebar { get; set; }

        #region 异步加载任务

        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        #endregion

        #region 杂项

        protected bool ContentLoaded;

        public BindableBool Disabled = new BindableBool
        {
            Default = true,
            Value = true
        };

        #endregion

        private void createLoadTask()
        {
            ContentLoaded = false;

            //加载内容
            LoadComponentAsync(CreateContent(), content =>
            {
                ContentLoaded = true;

                //添加内容
                Add(content);

                //调用OnContentLoaded进行善后
                OnContentLoaded(content);

                mvisScreen?.RemovePluginFromLoadList(this);
            }, cancellationTokenSource.Token);
        }

        public void Cancel()
        {
            cancellationTokenSource.Cancel();
            CreateContent()?.Dispose();

            mvisScreen?.RemovePluginFromLoadList(this);
        }

        public virtual void Load()
        {
            if (Disabled.Value) return;

            //向加载列表添加这个plugin
            mvisScreen?.AddPluginToLoadList(this);

            //调用PostInit在加载内容前初始化
            if (!PostInit())
            {
                mvisScreen?.RemovePluginFromLoadList(this);
                return;
            }

            createLoadTask();
        }

        public virtual bool Enable()
        {
            Disabled.Value = false;

            if (!ContentLoaded)
                Load();

            return true;
        }

        public virtual void UnLoad()
        {
            Expire();
        }

        public virtual bool Disable()
        {
            Disabled.Value = true;
            return true;
        }
    }
}