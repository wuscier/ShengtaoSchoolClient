using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ICSharpCode.SharpZipLib.Zip;
using St.Common;
using Serilog;
using St.Host.Properties;

namespace St.Host
{
    /// <summary>
    ///     App.xaml 的交互逻辑
    /// </summary>
    public partial class App
    {
        private bool isNewInstance = true;
        public static Mutex Mutex1 { get; private set; }

        protected override async void OnStartup(StartupEventArgs e)
        {
            Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            CheckAppInstance();

            LogManager.CreateLogFile();

            LogManager.DeleteLogFiles();

            GetLocalVersion();

            UpdateManager.InitializePaths();

            await UnzipExeFilesAndCopyConfigAsync();

            ReadConfig();

            CheckUpdateBackground();

            Current.ShutdownMode = ShutdownMode.OnMainWindowClose;

            //var bootstrapper = new StBootstrapper();
            var bootstrapper = new DevBootstrapper();

            bootstrapper.Run();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            Log.Logger.Error("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            Log.Logger.Error($"【unhandled exception】：{exception}");
        }

        private void Current_DispatcherUnhandledException(object sender,
            System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Logger.Error("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            Log.Logger.Error($"【unhandled exception】：{e.Exception}");
            MessageBox.Show("当前应用程序遇到一些问题，将要退出！", "意外的操作", MessageBoxButton.OK, MessageBoxImage.Information);
            e.Handled = true;
        }

        private void CheckAppInstance()
        {
            Mutex1 = new Mutex(true, "ShengtaoSchoolClient", out isNewInstance);

            if (!isNewInstance)
            {
                SscDialog dialog = new SscDialog("程序已经在运行中！");
                dialog.ShowDialog();
                Current.Shutdown();
            }
        }

        private void GetLocalVersion()
        {
            GlobalData.Instance.Version = Assembly.GetExecutingAssembly().GetName().Version;
            GlobalResources.VersionInfo =
                $"互联网校际协作客户端 V{GlobalData.Instance.Version.Major}.{GlobalData.Instance.Version.Minor}.{GlobalData.Instance.Version.Build}";
            Log.Logger.Debug($"【current version info】：{GlobalData.Instance.Version}");
        }

        private async Task UnzipExeFilesAndCopyConfigAsync()
        {
            string sdkexesPath = Path.Combine(Environment.CurrentDirectory, "sdkexes.zip");
            if (File.Exists(sdkexesPath))
            {
                Log.Logger.Debug($"【unzip {sdkexesPath}】：assume it's first startup after installation");
                await Task.Run(() =>
                {
                    try
                    {
                        using (ZipInputStream zipInputStream = new ZipInputStream(File.OpenRead(sdkexesPath)))
                        {
                            ZipEntry entry;

                            while ((entry = zipInputStream.GetNextEntry()) != null)
                            {
                                string fileName = Path.GetFileName(entry.Name);

                                if (!string.IsNullOrEmpty(fileName))
                                {
                                    string fileItemName = Path.Combine(Environment.CurrentDirectory, fileName);

                                    Log.Logger.Debug($"【unzip file】：unzip {entry.Name} to {fileItemName}");

                                    using (FileStream streamWriter = File.Create(fileItemName))
                                    {
                                        byte[] data = new byte[2048];

                                        while (true)
                                        {
                                            var size = zipInputStream.Read(data, 0, data.Length);
                                            if (size > 0)
                                            {
                                                streamWriter.Write(data, 0, size);
                                            }
                                            else
                                            {
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        Log.Logger.Debug($"【delete {sdkexesPath} begins】");
                        File.Delete(sdkexesPath);

                        string sharedConfigFile = Path.Combine(UpdateManager.VersionFolder, GlobalResources.ConfigPath);
                        string curConfigFile = Path.Combine(Environment.CurrentDirectory, GlobalResources.ConfigPath);

                        if (File.Exists(sharedConfigFile) && Settings.Default.UseUserConfig)
                        {
                            Log.Logger.Debug($"【copy config file】：copy {sharedConfigFile} to {curConfigFile}");

                            File.Copy(sharedConfigFile, curConfigFile, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error($"【unzip sdkexes.zip or copy config exception】：{ex}");
                        Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            SscDialog dialog = new SscDialog(Messages.ErrorUnzipExesFailed);
                            dialog.ShowDialog();
                            Current.Shutdown();
                        }));
                    }
                });
            }
        }

        private void ReadConfig()
        {
            BaseResult result = ConfigManager.ReadConfig();

            if (result.Status == "-1")
            {
                SscDialog dialog = new SscDialog(result.Message);
                dialog.ShowDialog();
                Current.Shutdown();
            }
        }

        private void CheckUpdateBackground()
        {
            VersionInfo versionInfo = UpdateManager.GetVersionInfoFromServer();
            if (versionInfo != null)
            {
                try
                {
                    string[] versions = versionInfo.VersionNumber.Split(new[] {'.'},
                        StringSplitOptions.RemoveEmptyEntries);

                    int major = int.Parse(versions[0]);
                    int minor = int.Parse(versions[1]);
                    int build = int.Parse(versions[2]);

                    string localVersion =
                        $"{GlobalData.Instance.Version.Major}.{GlobalData.Instance.Version.Minor}.{GlobalData.Instance.Version.Build}";
                    string serverVersion = $"{major}.{minor}.{build}";

                    Log.Logger.Debug(
                        $"【check update】：local version={localVersion}, server version={serverVersion}");

                    bool hasNewerVersion = false;
                    if (major > GlobalData.Instance.Version.Major)
                    {
                        hasNewerVersion = true;
                    }
                    else if (major == GlobalData.Instance.Version.Major)
                    {
                        if (minor > GlobalData.Instance.Version.Minor)
                        {
                            hasNewerVersion = true;
                        }
                        else if (minor == GlobalData.Instance.Version.Minor)
                        {
                            if (build > GlobalData.Instance.Version.Build)
                            {
                                hasNewerVersion = true;
                            }
                        }
                    }

                    if (hasNewerVersion)
                    {
                        string updateMsg =
                            $"当前版本为{localVersion}，检测到有新版本{serverVersion}，是否升级？";


                        UpdateConfirmView updateConfirmView = new UpdateConfirmView(updateMsg);
                        bool? update = updateConfirmView.ShowDialog();

                        if (update.HasValue && update.Value)
                        {
                            Log.Logger.Debug($"【agree to update】");

                            Task.Run(() => { UpdateManager.UpdateApp(); });

                            GlobalData.Instance.UpdatingDialog =
                                new SscDialogWithoutButton("请勿关闭程序，正在升级中......\r\n升级完成后会自动启动新版本。");
                            GlobalData.Instance.UpdatingDialog.ShowDialog();
                        }
                        else
                        {
                            Log.Logger.Debug($"【refuse to update】");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger.Error($"【version comparison exception】：{ex}");
                }
            }
            else
            {
                Log.Logger.Error("【get version info from server error】：returned version info is null");
            }
        }

        //protected override void OnExit(ExitEventArgs e)
        //{
        //    base.OnExit(e);

        //    if (isNewInstance)
        //    {
        //        Log.Logger.Debug(
        //            "【exit app】：#######################################################################################");
        //        IGroupManager groupManager = DependencyResolver.Current.GetService<IGroupManager>();
        //        groupManager.LeaveGroup();

        //        IRtClientService rtClientService = DependencyResolver.Current.GetService<IRtClientService>();
        //        Log.Logger.Debug($"【rt server connected】：{rtClientService.IsConnected()}");
        //        Log.Logger.Debug($"【stop rt server begins】：");
        //        rtClientService.Stop();
        //        Log.Logger.Debug($"【rt server connected】：{rtClientService.IsConnected()}");
        //    }
        //}
    }
}