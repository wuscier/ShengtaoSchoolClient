using Newtonsoft.Json;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;
using ICSharpCode.SharpZipLib.Zip;
using Squirrel;

namespace St.Common
{
    public static class UpdateManager
    {
        private static VersionInfo _versionInfo;

        private static string _versionFile;
        public static string VersionFolder;
        private static string _releaseFolder;
        private static string _releaseZipFile;

        private const string VersionFileName = "ssc_version.json";
        private const string VersionFolderName = "ShengtaoSchoolClient_Version";
        private const string ReleaseFolderName = "Releases";

        public static void InitializePaths()
        {
            VersionFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                VersionFolderName);

            _versionFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                VersionFolderName,
                VersionFileName);

            _releaseFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                VersionFolderName,
                ReleaseFolderName);

            if (!Directory.Exists(VersionFolder))
            {
                Log.Logger.Debug($"【create folder begins】：{VersionFolder}");
                Directory.CreateDirectory(VersionFolder);
            }
        }

        public static VersionInfo GetVersionInfoFromServer()
        {
            if (string.IsNullOrEmpty(GlobalData.Instance.AggregatedConfig.ServerVersionInfo))
            {
                Log.Logger.Debug($"【update address is empty】");
                return null;
            }

            try
            {
                if (File.Exists(_versionFile))
                {
                    Log.Logger.Debug($"【delete version file begins】：{_versionFile}");
                    File.Delete(_versionFile);
                }

                using (WebClient webClient = new WebClient())
                {
                    Log.Logger.Debug(
                        $"【download version file begins】：download {GlobalData.Instance.AggregatedConfig.ServerVersionInfo} to {_versionFile}");

                    webClient.DownloadFile(
                        new Uri(GlobalData.Instance.AggregatedConfig.ServerVersionInfo),
                        _versionFile);

                    if (File.Exists(_versionFile))
                    {
                        string fileVersionJson = File.ReadAllText(_versionFile, Encoding.UTF8);
                        VersionInfo versionInfo = JsonConvert.DeserializeObject<VersionInfo>(fileVersionJson);

                        _versionInfo = versionInfo;
                        return versionInfo;
                    }

                    return null;
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"【get version info from server exception】：{ex}");
                return null;
            }
        }

        public  static async void UpdateApp()
        {
            string releaseZipFile = Path.GetFileName(_versionInfo?.DownloadReleaseUrl);
            if (string.IsNullOrEmpty(releaseZipFile))
            {
                Log.Logger.Error($"【wrong download url】：{_versionInfo?.DownloadReleaseUrl}");
                return;
            }

            _releaseZipFile = Path.Combine(VersionFolder, releaseZipFile);

            try
            {
                using (WebClient webClient = new WebClient())
                {
                    Log.Logger.Debug(
                        $"【download release zip file begins】：download {_versionInfo.DownloadReleaseUrl} to {_releaseZipFile}");

                    webClient.DownloadFile(new Uri(_versionInfo.DownloadReleaseUrl), _releaseZipFile);
                }

                if (File.Exists(_releaseZipFile))
                {
                    if (Directory.Exists(_releaseFolder))
                    {
                        Log.Logger.Debug($"【delete folder begins】：{_releaseFolder}");
                        DirectoryInfo releaseDirectoryInfo = new DirectoryInfo(_releaseFolder);
                        releaseDirectoryInfo.Delete(true);
                    }

                    Log.Logger.Debug($"【create folder begins】：{_releaseFolder}");
                    Directory.CreateDirectory(_releaseFolder);

                    using (ZipInputStream zipInputStream = new ZipInputStream(File.OpenRead(_releaseZipFile)))
                    {
                        ZipEntry entry;

                        while ((entry = zipInputStream.GetNextEntry()) != null)
                        {
                            string fileName = Path.GetFileName(entry.Name);

                            if (!string.IsNullOrEmpty(fileName))
                            {
                                string fileItemName = Path.Combine(_releaseFolder, fileName);

                                Log.Logger.Debug($"【unzip file】：upzip {entry.Name} to {fileItemName}");

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

                    using (
                        var updateManager =
                            new Squirrel.UpdateManager(_releaseFolder))
                    {
                        Log.Logger.Debug(
                            $"【update begins】：...............................................................");
                        await updateManager.UpdateApp();

                        Log.Logger.Debug(
                            $"【update ends】：...............................................................");

                        await Application.Current.Dispatcher.BeginInvoke(
                            new Action(() =>
                            {
                                GlobalData.Instance.UpdatingDialog.TbMsg.Text = "升级成功！正在启动新版本。";
                                Application.Current.Shutdown();
                                Process newProcess = new Process
                                {
                                    StartInfo =
                                    {
                                        UseShellExecute = true,
                                        FileName =
                                            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                                                "ShengtaoSchoolClient.lnk")
                                    }
                                };
                                newProcess.Start();
                            }));
                    }
                }
                else
                {
                    Log.Logger.Error("【failed to download file】：");
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"【download，unzip，update exception】：{ex}");
                await Application.Current.Dispatcher.BeginInvoke(
                    new Action(() => { GlobalData.Instance.UpdatingDialog.TbMsg.Text = $"升级失败！\r\n{ex.Message}"; }));
            }
        }

        public static void WriteConfigToVersionFolder(string configJson)
        {
            string sharedConfig = Path.Combine(VersionFolder, GlobalResources.ConfigPath);
            File.WriteAllText(sharedConfig, configJson, Encoding.UTF8);
        }
    }

}
