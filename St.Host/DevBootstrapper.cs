using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using Autofac;
using Prism.Autofac;
using Prism.Modularity;
using Serilog;
using St.CollaborativeInfo;
using St.Common;
using St.Common.Contract;
using St.Core;
using St.Discussion;
using St.Host.ViewModels;
using St.Host.Views;
using St.Interactive;
using St.InteractiveWithoutLive;
using St.Meeting;
using St.Profile;
using St.RtClient;
using St.Setting;
using System.Threading.Tasks;

namespace St.Host
{
    public class DevBootstrapper : AutofacBootstrapper
    {
        protected override DependencyObject CreateShell()
        {            
            return Container.Resolve<MainView>();
        }

        protected override async void InitializeShell()
        {
            RegisterSignoutHandler();

            GetSerialNo();

            bool hasDeviceInfo = await GetDeviceInfoFromServer();

            if (!hasDeviceInfo)
            {
                Application.Current.Shutdown();
                return;
            }

            if (GlobalData.Instance.Device.EnableLogin)
            {
                Log.Logger.Debug("【device startup】");
                var deviceLoginView = Container.Resolve<DeviceLoginView>();
                deviceLoginView.ShowDialog();

                var deviceLoginViewModel = deviceLoginView.DataContext as DeviceLoginViewModel;

                if (deviceLoginViewModel != null && !deviceLoginViewModel.IsLoginSucceeded)
                {
                    Application.Current.Shutdown();
                    return;
                }
            }
            else
            {
                Log.Logger.Debug("【account startup】");
                var loginView = Container.Resolve<LoginView>();
                loginView.ShowDialog();

                var loginViewModel = loginView.DataContext as LoginViewModel;

                if ((loginViewModel != null) && !loginViewModel.IsLoginSucceeded)
                {
                    Application.Current.Shutdown();
                    return;
                }
            }

            var mainView = Shell as MainView;
            mainView?.Show();
        }

        private async Task<bool> GetDeviceInfoFromServer()
        {
            IBms bmsService = Container.Resolve<IBms>();

            ResponseResult getDeviceResult =
                await
                    bmsService.GetDeviceInfo(GlobalData.Instance.SerialNo,
                        GlobalData.Instance.AggregatedConfig.DeviceKey);

            if (getDeviceResult.Status != "0")
            {
                string msg = $"{getDeviceResult.Message}\r\n本机设备号：{GlobalData.Instance.SerialNo}";

                SscDialog dialog = new SscDialog(msg);
                dialog.ShowDialog();
                return false;
            }

            Device device = getDeviceResult.Data as Device;
            if (device != null)
            {
                GlobalData.Instance.Device = device;
                GlobalData.Instance.AggregatedConfig.DeviceNo = device.Id;

                if (device.IsExpired)
                {
                    string msg = $"{Messages.WarningDeviceExpires}\r\n本机设备号：{GlobalData.Instance.SerialNo}";

                    SscDialog dialog = new SscDialog(msg);
                    dialog.ShowDialog();
                    return false;
                }
                if (device.Locked)
                {
                    string msg = $"{Messages.WarningLockedDevice}\r\n本机设备号：{GlobalData.Instance.SerialNo}";

                    SscDialog dialog = new SscDialog(msg);
                    dialog.ShowDialog();
                    return false;
                }

                return true;
            }

            string emptyDeviceMsg = $"{Messages.WarningEmptyDevice}\r\n本机设备号：{GlobalData.Instance.SerialNo}";
            SscDialog emptyDeviceDialog = new SscDialog(emptyDeviceMsg);
            emptyDeviceDialog.ShowDialog();
            return false;
        }

        protected override IContainer CreateContainer(ContainerBuilder containerBuilder)
        {
            var container = base.CreateContainer(containerBuilder);
            DependencyResolver.SetContainer(container);

            return container;
        }

        protected override void ConfigureContainerBuilder(ContainerBuilder builder)
        {
            base.ConfigureContainerBuilder(builder);

            builder.RegisterInstance(new LessonInfo()).SingleInstance();
            builder.RegisterInstance(new UserInfo()).SingleInstance();
            builder.RegisterInstance(new LessonDetail()).SingleInstance();
            builder.RegisterInstance(new List<UserInfo>()).SingleInstance();
            builder.RegisterType<MainView>().AsSelf().SingleInstance();

            builder.RegisterInstance(new RtClientConfiguration())
                .AsSelf()
                .As<IRtClientConfiguration>()
                .SingleInstance();

            builder.RegisterType<VisualizeShellService>().As<IVisualizeShell>().SingleInstance();

            builder.RegisterType<MainViewModel>().AsSelf().As<ISignOutHandler>().SingleInstance();

            RegisterAutofacModules(builder);
        }

        private void RegisterAutofacModules(ContainerBuilder builder)
        {
            var moduleCatalog = (ModuleCatalog)ModuleCatalog;
            var assemblies = new List<Assembly>();
            foreach (var module in moduleCatalog.Items)
            {
                var type = Type.GetType(((ModuleInfo)module).ModuleType);
                if (type != null) assemblies.Add(type.Assembly);
            }
            builder.RegisterAssemblyModules(assemblies.ToArray());
        }

        protected override void ConfigureModuleCatalog()
        {
            ModuleCatalog catalog = (ModuleCatalog) ModuleCatalog;

            catalog.AddModule(typeof(CoreModule));
            catalog.AddModule(typeof(MeetingModule));

            catalog.AddModule(typeof(CollaborativeInfoModule));
            catalog.AddModule(typeof(InteractiveModule));
            catalog.AddModule(typeof(InteractiveWithoutLiveModule));
            catalog.AddModule(typeof(DiscussionModule));
            //catalog.AddModule(typeof(InstantMeetingModule));
            catalog.AddModule(typeof(SettingModule));
            catalog.AddModule(typeof(ProfileModule));
            catalog.AddModule(typeof(RtClientModule));
        }


        private void RegisterSignoutHandler()
        {
            // 注册上线冲突处理程序
            RtServerHandler.Instance.SignOutHandler = () =>
            {
                try
                {
                    // 注销时理清资源，初始化单例注册的UI组件（如MainView）
                    Application.Current.Dispatcher.BeginInvoke(new Action((() =>
                    {
                        var handler = DependencyResolver.Current.GetService<ISignOutHandler>();
                        handler.SignOut();
                    })));
                }
                catch (Exception ex)
                {
                    Log.Logger.Error($"【sign out exception】：{ex}");
                }
            };
        }

        private void GetSerialNo()
        {
            ISdk sdkService = DependencyResolver.Current.GetService<ISdk>();
            GlobalData.Instance.SerialNo = sdkService.GetSerialNo();

            Log.Logger.Debug($"【device no.】：{GlobalData.Instance.SerialNo}");
        }
    }
}