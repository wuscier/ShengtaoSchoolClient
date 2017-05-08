using Autofac;
using Prism.Modularity;
using Prism.Regions;
using St.Common;

namespace St.Profile
{
    public class ProfileModule : Module, IModule
    {
        private readonly IRegionManager _regionManager;

        public ProfileModule()
        {
        }

        public ProfileModule(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }


        protected override void Load(ContainerBuilder builder)
        {
        }
        public void Initialize()
        {
            _regionManager.RegisterViewWithRegion(RegionNames.NavRegion, typeof(ProfileNavView));
            _regionManager.RegisterViewWithRegion(RegionNames.ContentRegion, typeof(ProfileContentView));
        }
    }
}
