using Autofac;
using St.Common;
using Prism.Modularity;


namespace St.Meeting
{
    public class MeetingModule : Module, IModule
    {
        public MeetingModule()
        {
            
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<MeetingService>().As<IMeeting>();
            builder.RegisterType<LocalPushLiveService>().Named<IPushLive>(GlobalResources.LocalPushLive).SingleInstance();
            builder.RegisterType<ServerPushLiveService>().Named<IPushLive>(GlobalResources.RemotePushLive).SingleInstance();
            builder.RegisterType<LocalRecordService>().As<IRecord>().SingleInstance();
        }

        public void Initialize()
        {
        }
    }
}
