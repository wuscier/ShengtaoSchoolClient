using Autofac;
using Prism.Modularity;
using Prism.Regions;
using St.Common;

namespace St.InstantMeeting
{
    public class InstantMeetingModule : Module, IModule
    {
        private readonly IRegionManager _regionManager;

        public InstantMeetingModule()
        {
            
        }
        public InstantMeetingModule(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }


        protected override void Load(ContainerBuilder builder)
        {
        }

        public void Initialize()
        {
            _regionManager.RegisterViewWithRegion(RegionNames.NavRegion, typeof(InstantMeetingNavView));
            _regionManager.RegisterViewWithRegion(RegionNames.ContentRegion, typeof(InstantMeetingContentView));
        }
    }
}
