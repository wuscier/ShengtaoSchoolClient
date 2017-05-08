using System;
using System.IO;
using System.Threading.Tasks;
using St.Common;
using System.Collections.Generic;
using Serilog;

namespace St.Meeting
{
    public class ServerPushLiveService : IPushLive
    {
        private static readonly string ConfigFile = Path.Combine(Environment.CurrentDirectory, GlobalResources.ConfigPath);
        private readonly ISdk _sdkService;

        public ServerPushLiveService()
        {
            _sdkService = DependencyResolver.Current.GetService<ISdk>();
        }

        public bool HasPushLiveSuccessfully { get; set; }

        public int LiveId { get; set; }

        public LiveParam LiveParam { get; private set; }

        public void ResetStatus()
        {
            LiveId = 0;
            HasPushLiveSuccessfully = false;
        }

        public LiveParam GetLiveParam()
        {
            if (!File.Exists(ConfigFile))
            {
                LiveParam = new LiveParam();
                return new LiveParam();
            }

            try
            {
                LiveParam liveParam = new LiveParam()
                {
                    m_nAudioBitrate = 64,
                    m_nBitsPerSample = 16,
                    m_nChannels = 1,
                    m_nIsLive = 1,
                    m_nIsRecord = 0,
                    m_nSampleRate = 8000,
                    m_sRecordFilePath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),

                    m_nVideoBitrate = int.Parse(GlobalData.Instance.AggregatedConfig.RemoteLiveConfig.CodeRate)
                };

                string[] resolutionStrings = GlobalData.Instance.AggregatedConfig.RemoteLiveConfig.Resolution.Split(new[] {'*'},
                    StringSplitOptions.RemoveEmptyEntries);

                liveParam.m_nWidth = int.Parse(resolutionStrings[0]);
                liveParam.m_nHeight = int.Parse(resolutionStrings[1]);

                LiveParam = liveParam;
                return liveParam;

            }
            catch (Exception ex)
            {
                Log.Logger.Error($"【get server push live param exception】：{ex}");
                LiveParam = new LiveParam();
                return new LiveParam();
            }
        }

        public async Task<StartLiveStreamResult> StartPushLiveStream(List<LiveVideoStreamInfo> liveVideoStreamInfos,
            string pushLiveUrl)
        {
            Log.Logger.Debug($"【push live url】：{pushLiveUrl}");

            if (string.IsNullOrEmpty(pushLiveUrl))
            {
                return new StartLiveStreamResult();
            }

            Common.LiveParam liveParam = LiveParam;
            liveParam.m_url1 = pushLiveUrl;

            if (liveParam.m_nWidth == 0 || liveParam.m_nHeight == 0 || liveParam.m_nVideoBitrate == 0)
            {
                return new StartLiveStreamResult()
                {
                    m_result = new AsynCallResult()
                    {
                        m_rc = -1,
                        m_message = Messages.WarningLiveResolutionNotSet
                    }
                };
            }

            Log.Logger.Debug(
                $"【server push live begins】：width={liveParam.m_nWidth}, height={liveParam.m_nHeight}, bitrate={liveParam.m_nVideoBitrate}, url={liveParam.m_url1}, videos={liveVideoStreamInfos.Count}");

            for (int i = 0; i < liveVideoStreamInfos.Count; i++)
            {
                Log.Logger.Debug(
                    $"video{i + 1}：x={liveVideoStreamInfos[i].XLocation}, y={liveVideoStreamInfos[i].YLocation}, width={liveVideoStreamInfos[i].Width}, height={liveVideoStreamInfos[i].Height}");
            }

            StartLiveStreamResult startLiveStreamResult =
                await _sdkService.StartLiveStream(liveParam, liveVideoStreamInfos.ToArray(), liveVideoStreamInfos.Count);

            LiveId = startLiveStreamResult.m_liveId;

            if (startLiveStreamResult.m_result.m_rc == 0)
            {
                HasPushLiveSuccessfully = true;
                Log.Logger.Debug($"【server push live succeeded】：liveId={startLiveStreamResult.m_liveId}");
            }
            else
            {
                HasPushLiveSuccessfully = false;
                Log.Logger.Error($"【server push live failed】：{startLiveStreamResult.m_result.m_message}");
            }

            return startLiveStreamResult;
        }

        public async Task<AsynCallResult> RefreshLiveStream(List<LiveVideoStreamInfo> openedStreamInfos)
        {
            if (LiveId != 0)
            {
                Log.Logger.Debug($"【server refresh live begins】：liveId={LiveId}, videos={openedStreamInfos.Count}");
                for (int i = 0; i < openedStreamInfos.Count; i++)
                {
                    Log.Logger.Debug(
                        $"video{i + 1}：x={openedStreamInfos[i].XLocation}, y={openedStreamInfos[i].YLocation}, width={openedStreamInfos[i].Width}, height={openedStreamInfos[i].Height}");
                }

                AsynCallResult updateAsynCallResult =
                    await
                        _sdkService.UpdateLiveVideoStreams(LiveId, openedStreamInfos.ToArray(), openedStreamInfos.Count);
                Log.Logger.Debug(
                    $"【server refresh live result】：result={updateAsynCallResult.m_rc}, msg={updateAsynCallResult.m_message}");
                return updateAsynCallResult;
            }
            return new AsynCallResult() {m_rc = -1, m_message = Messages.WarningNoLiveToRefresh};
        }

        public async Task<AsynCallResult> StopPushLiveStream()
        {
            if (LiveId != 0)
            {
                Log.Logger.Debug($"【server push live stop begins】：liveId={LiveId}");
                AsynCallResult stopAsynCallResult = await _sdkService.StopLiveStream(LiveId);
                LiveId = 0;

                Log.Logger.Debug($"【server push live stop result】：result={stopAsynCallResult.m_rc}, msg={stopAsynCallResult.m_message}");

                return stopAsynCallResult;
            }
            return new AsynCallResult() {m_rc = -1, m_message = Messages.WarningNoLiveToStop};
        }
    }
}
