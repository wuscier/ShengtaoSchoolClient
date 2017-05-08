using Autofac;
using Prism.Modularity;
using St.Common;
using Prism.Regions;

namespace St.Core
{
    public class CoreModule : Module,IModule
    {


        private readonly IRegionManager _regionManager;

        public CoreModule(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }

        public CoreModule()
        {
            
        }

        public void Initialize()
        {
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<BmsService>().As<IBms>().SingleInstance();
            builder.RegisterType<SdkService>().As<ISdk>().SingleInstance();
            builder.RegisterType<ViewLayoutService>().As<IViewLayout>().SingleInstance();
            builder.RegisterType<GroupManager>().As<IGroupManager>().SingleInstance();
        }
    }
}