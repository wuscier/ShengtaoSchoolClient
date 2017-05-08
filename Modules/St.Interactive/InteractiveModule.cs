using Prism.Modularity;
using Prism.Regions;
using St.Common;
using autofac = Autofac;

namespace St.Interactive
{
    public class InteractiveModule : autofac.Module, IModule
    {
        private readonly IRegionManager _regionManager;

        public InteractiveModule()
        {

        }

        public InteractiveModule(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }

        protected override void Load(autofac.ContainerBuilder builder)
        {
        }
        
        public void Initialize()
        {
            //if (GlobalData.Instance.ActiveModules.Contains(LessonType.Interactive.ToString()))
            //{
                _regionManager.RegisterViewWithRegion(RegionNames.NavRegion, typeof(InteractiveNavView));
                _regionManager.RegisterViewWithRegion(RegionNames.ContentRegion, typeof(InteractiveContentView));
            //}
        }
    }
}
