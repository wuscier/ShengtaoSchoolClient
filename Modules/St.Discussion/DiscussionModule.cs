using autofac = Autofac;
using Prism.Modularity;
using Prism.Regions;
using St.Common;

namespace St.Discussion
{
    public class DiscussionModule:autofac.Module,IModule
    {
        private readonly IRegionManager _regionManager;

        public DiscussionModule()
        {

        }
        public DiscussionModule(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }


        protected override void Load(autofac.ContainerBuilder builder)
        {
        }

        public void Initialize()
        {
            //if (GlobalData.Instance.ActiveModules.Contains(Common.LessonType.Discussion.ToString()))
            //{
                _regionManager.RegisterViewWithRegion(RegionNames.NavRegion, typeof(DiscussionNavView));
                _regionManager.RegisterViewWithRegion(RegionNames.ContentRegion, typeof(DiscussionContentView));
            //}
        }

    }
}