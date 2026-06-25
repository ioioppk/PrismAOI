using System.Windows;
using Prism.Ioc;
using Prism.DryIoc;
using VisionInspect.Views;

namespace VisionInspect
{
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<ShellWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            Bootstrapper.AppBootstrapper.Register(containerRegistry);
        }
    }
}