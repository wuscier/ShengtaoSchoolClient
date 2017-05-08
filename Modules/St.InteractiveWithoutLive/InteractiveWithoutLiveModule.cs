using Prism.Modularity;
using Prism.Regions;
using St.Common;
using autofac= Autofac;
using St.InteractiveWithouLive;


namespace St.InteractiveWithoutLive
{
    public class InteractiveWithoutLiveModule : autofac.Module, IModule
    {
        private readonly IRegionManager _regionManager;

        public InteractiveWithoutLiveModule()
        {
        }

        public InteractiveWithoutLiveModule(IRegionManager regionManager)
        {
            _regionManager = regionManager;

        }

        protected override void Load(autofac.ContainerBuilder builder)
        {
        }

        public void Initialize()
        {
            //if (GlobalData.Instance.ActiveModules.Contains(LessonType.InteractiveWithoutLive.ToString()))
            //{
                _regionManager.RegisterViewWithRegion(RegionNames.ContentRegion,
                    typeof(InteractiveWithouLiveContentView));
                _regionManager.RegisterViewWithRegion(RegionNames.NavRegion, typeof(InteractiveWithouLiveNavView));
            //}
        }
    }
}
