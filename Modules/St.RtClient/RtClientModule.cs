using Autofac;
using Prism.Modularity;
using Prism.Regions;
using St.Common.Contract;

namespace St.RtClient
{
    public class RtClientModule : Module, IModule
    {
        private readonly IRegionManager _regionManager;

        public RtClientModule()
        {
        }

        public RtClientModule(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }
        public void Initialize()
        {
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<RtClientService>().As<IRtClientService>().SingleInstance();
        }
    }
}
