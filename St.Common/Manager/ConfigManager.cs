using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Serilog;

namespace St.Common
{
    public static class ConfigManager
    {
        public static readonly string ConfigFileName = Path.Combine(Environment.CurrentDirectory,GlobalResources.ConfigPath);

        public static BaseResult ReadConfig()
        {
            if (!File.Exists(ConfigFileName))
            {
                Log.Logger.Debug($"【read config】：config file does not exist");
                return new BaseResult()
                {
                    Status = "-1",
                    Message = Messages.ErrorConfigFileLost
                };
            }

            string configJson = File.ReadAllText(ConfigFileName, Encoding.UTF8);

            AggregatedConfig aggregatedConfig;
            try
            {
                aggregatedConfig = JsonConvert.DeserializeObject<AggregatedConfig>(configJson);
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"【read config exception】：{ex}");
                return new BaseResult()
                {
                    Status = "-1",
                    Message = Messages.ErrorReadConfigFailed
                };
            }

            if (aggregatedConfig == null)
            {
                Log.Logger.Error($"【read config】：empty config");
                return new BaseResult()
                {
                    Status = "-1",
                    Message = Messages.ErrorReadEmptyConfig
                };
            }

            GlobalData.Instance.AggregatedConfig = aggregatedConfig;
            return new BaseResult()
            {
                Status = "0",
            };
        }

        public static BaseResult WriteConfig()
        {
            try
            {
                if (GlobalData.Instance.AggregatedConfig == null)
                {
                    return new BaseResult()
                    {
                        Status = "-1",
                        Message = Messages.ErrorWriteEmptyConfig
                    };
                }
                string configJson = JsonConvert.SerializeObject(GlobalData.Instance.AggregatedConfig);

                File.WriteAllText(ConfigFileName, configJson, Encoding.UTF8);

                return new BaseResult() {Status = "0"};
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"【write config exception】：{ex}");
                return new BaseResult()
                {
                    Status = "-1",
                    Message = Messages.ErrorWriteConfigFailed
                };
            }
        }
    }
}
