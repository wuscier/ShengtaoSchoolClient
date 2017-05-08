using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using Autofac;
using Prism.Autofac;
using Prism.Modularity;
using St.Common;
using St.Host.ViewModels;
using St.Host.Views;
using St.Common.Contract;

namespace St.Host
{
    public class StBootstrapper : AutofacBootstrapper
    {
        protected override DependencyObject CreateShell()
        {
            //var sharedMainView = new MainView(Container.Resolve<IRegionManager>(), Container.Resolve<IModuleManager>());
            //RegisterInstance(sharedMainView, typeof(MainView), "", true);

            return Container.Resolve<MainView>();
        }

        protected override void InitializeShell()
        {
            var loginView = Container.Resolve<LoginView>();
            loginView.ShowDialog();

            var loginViewModel = loginView.DataContext as LoginViewModel;

            if ((loginViewModel != null) && loginViewModel.IsLoginSucceeded)
            {
                var mainView = Shell as MainView;
                mainView?.Show();
            }
            else
            {
                Application.Current.Shutdown();
            }
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
            var modulePath = Path.Combine(Environment.CurrentDirectory, Common.GlobalResources.ModulesPath);
            var assemblies =
                Directory.GetFiles(modulePath, "*.dll").Select(Assembly.LoadFile).ToArray();

            builder.RegisterAssemblyModules(assemblies);
        }

        protected override IModuleCatalog CreateModuleCatalog()
        {
            var path = Path.Combine(Environment.CurrentDirectory, Common.GlobalResources.ModulesPath);

            return new DirectoryModuleCatalog { ModulePath = path };
        }

        protected override void ConfigureModuleCatalog()
        {
            var directoryCatalog = (DirectoryModuleCatalog)ModuleCatalog;
            directoryCatalog.Initialize();
        }
    }
}