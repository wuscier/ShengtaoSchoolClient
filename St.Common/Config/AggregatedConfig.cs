namespace St.Common
{

    public class AggregatedConfig
    {
        /// <summary>
        /// ExternalEnv|InternalEnv
        /// </summary>
        public RunEnv RunEnvironment { get; set; }

        public string UserCenterAddress { get; set; }
        public string RtServerAddress { get; set; }
        public string ServerVersionInfo { get; set; }
        //public int DeviceAutoLogin { get; set; }
        public string DeviceNo { get; set; }
        public string DeviceKey { get; set; }
        public LoginConfig AccountAutoLogin { get; set; }
        public VideoConfig MainCamera { get; set; }
        public VideoConfig SecondaryCamera { get; set; }
        public AudioConfig AudioConfig { get; set; }
        public LiveConfig LocalLiveConfig { get; set; }
        public LiveConfig RemoteLiveConfig { get; set; }
        public RecordConfig RecordConfig { get; set; }

        public AggregatedConfig()
        {
            RunEnvironment = RunEnv.ExternalEnv;
            UserCenterAddress = string.Empty;
            RtServerAddress = string.Empty;
            ServerVersionInfo = string.Empty;

            DeviceNo = string.Empty;
            DeviceKey = string.Empty;

            AccountAutoLogin = new LoginConfig();

            MainCamera = new VideoConfig()
            {
                Type = "主摄像头",
            };

            SecondaryCamera = new VideoConfig()
            {
                Type = "辅摄像头"
            };
            AudioConfig = new AudioConfig();
            LocalLiveConfig = new LiveConfig()
            {
                Description = "本地推流",
                IsEnabled = true
            };
            RemoteLiveConfig = new LiveConfig()
            {
                Description = "服务器推流",
                IsEnabled = true
            };
            RecordConfig = new RecordConfig()
            {
                Description = "录制"
            };
        }

        public void CloneConfig(AggregatedConfig newConfig)
        {
            RunEnvironment = newConfig.RunEnvironment;
            UserCenterAddress = newConfig.UserCenterAddress;
            RtServerAddress = newConfig.RtServerAddress;
            ServerVersionInfo = newConfig.ServerVersionInfo;
            DeviceNo = newConfig.DeviceNo;
            DeviceKey = newConfig.DeviceKey;

            AccountAutoLogin.IsAutoLogin = newConfig.AccountAutoLogin.IsAutoLogin;
            AccountAutoLogin.UserName = newConfig.AccountAutoLogin.UserName;
            AccountAutoLogin.Password = newConfig.AccountAutoLogin.Password;

            MainCamera.CodeRate = newConfig.MainCamera.CodeRate;
            MainCamera.Name = newConfig.MainCamera.Name;
            MainCamera.Resolution = newConfig.MainCamera.Resolution;

            SecondaryCamera.CodeRate = newConfig.SecondaryCamera.CodeRate;
            SecondaryCamera.Name = newConfig.SecondaryCamera.Name;
            SecondaryCamera.Resolution = newConfig.SecondaryCamera.Resolution;

            AudioConfig.CodeRate = newConfig.AudioConfig.CodeRate;
            AudioConfig.MainMicrophone = newConfig.AudioConfig.MainMicrophone;
            AudioConfig.SampleRate = newConfig.AudioConfig.SampleRate;
            AudioConfig.SecondaryMicrophone = newConfig.AudioConfig.SecondaryMicrophone;
            AudioConfig.Speaker = newConfig.AudioConfig.Speaker;

            LocalLiveConfig.CodeRate = newConfig.LocalLiveConfig.CodeRate;
            LocalLiveConfig.Resolution = newConfig.LocalLiveConfig.Resolution;
            LocalLiveConfig.PushLiveStreamUrl = newConfig.LocalLiveConfig.PushLiveStreamUrl;
            LocalLiveConfig.IsEnabled = newConfig.LocalLiveConfig.IsEnabled;
            RemoteLiveConfig.CodeRate = newConfig.RemoteLiveConfig.CodeRate;
            RemoteLiveConfig.Resolution = newConfig.RemoteLiveConfig.Resolution;
            RemoteLiveConfig.IsEnabled = newConfig.RemoteLiveConfig.IsEnabled;
            RecordConfig.CodeRate = newConfig.RecordConfig.CodeRate;
            RecordConfig.Resolution = newConfig.RecordConfig.Resolution;
            RecordConfig.RecordPath = newConfig.RecordConfig.RecordPath;
        }
    }
}
