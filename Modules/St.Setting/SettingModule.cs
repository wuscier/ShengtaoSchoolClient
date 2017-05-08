using Autofac;
using Prism.Modularity;
using Prism.Regions;
using St.Common;

namespace St.Setting
{
    public class SettingModule :Module,  IModule
    {
        private readonly IRegionManager _regionManager;
        public SettingModule()
        { }

        public SettingModule(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }


        public void Initialize()
        {
            _regionManager.RegisterViewWithRegion(RegionNames.NavRegion, typeof(SettingNavView));
            _regionManager.RegisterViewWithRegion(RegionNames.ContentRegion, typeof(SettingContentView));
        }

        protected override void Load(ContainerBuilder builder)
        {
        }
    }
}
