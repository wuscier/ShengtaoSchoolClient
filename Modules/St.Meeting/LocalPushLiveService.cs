using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Serilog;
using St.Common;

namespace St.Meeting
{
    public class LocalPushLiveService : IPushLive
    {
        private static readonly string ConfigFile = Path.Combine(Environment.CurrentDirectory,
            GlobalResources.ConfigPath);

        private readonly ISdk _sdkService;

        public LocalPushLiveService()
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
            // get live configuration from local xml file
            if (!File.Exists(ConfigFile))
            {
                LiveParam = new LiveParam();
                return new LiveParam();
            }
            try
            {
                LiveParam liveParam = new LiveParam
                {
                    m_nAudioBitrate = 64,
                    m_nBitsPerSample = 16,
                    m_nChannels = 1,
                    m_nIsLive = 1,
                    m_nIsRecord = 0,
                    m_nSampleRate = 8000,
                    m_sRecordFilePath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),

                    m_url1 = GlobalData.Instance.AggregatedConfig.LocalLiveConfig.PushLiveStreamUrl,
                    m_nVideoBitrate = int.Parse(GlobalData.Instance.AggregatedConfig.LocalLiveConfig.CodeRate)
                };

                string[] resolutionStrings =
                    GlobalData.Instance.AggregatedConfig.LocalLiveConfig.Resolution.Split(new[] {'*'},
                        StringSplitOptions.RemoveEmptyEntries);

                liveParam.m_nWidth = int.Parse(resolutionStrings[0]);
                liveParam.m_nHeight = int.Parse(resolutionStrings[1]);


                LiveParam = liveParam;
                return liveParam;
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"【get local push live param exception】：{ex}");
                LiveParam = new LiveParam();
                return new LiveParam();
            }
        }

        public async Task<AsynCallResult> RefreshLiveStream(List<LiveVideoStreamInfo> openedStreamInfos)
        {
            if (LiveId != 0)
            {
                Log.Logger.Debug($"【local push live refresh begins】：liveId={LiveId}, videos={openedStreamInfos.Count}");
                for (int i = 0; i < openedStreamInfos.Count; i++)
                {
                    Log.Logger.Debug(
                        $"video{i + 1}：x={openedStreamInfos[i].XLocation}, y={openedStreamInfos[i].YLocation}, width={openedStreamInfos[i].Width}, height={openedStreamInfos[i].Height}");
                }

                AsynCallResult updateAsynCallResult =
                    await
                        _sdkService.UpdateLiveVideoStreams(LiveId, openedStreamInfos.ToArray(), openedStreamInfos.Count);
                Log.Logger.Debug(
                    $"【local push live refresh result】：result={updateAsynCallResult.m_rc}, msg={updateAsynCallResult.m_message}");
                return updateAsynCallResult;
            }

            return new AsynCallResult() {m_rc = -1, m_message = Messages.WarningNoLiveToRefresh};
        }

        public async Task<StartLiveStreamResult> StartPushLiveStream(List<LiveVideoStreamInfo> liveVideoStreamInfos,
            string pushLiveUrl)
        {
            if (string.IsNullOrEmpty(LiveParam.m_url1))
            {
                return new StartLiveStreamResult()
                {
                    m_result = new AsynCallResult()
                    {
                        m_rc = -1,
                        m_message = Messages.WarningLivePushLiveUrlNotSet
                    }
                };
            }

            if (LiveParam.m_nWidth == 0 || LiveParam.m_nHeight == 0 || LiveParam.m_nVideoBitrate == 0)
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
                $"【local push live begins】：width={LiveParam.m_nWidth}, height={LiveParam.m_nHeight}, bitrate={LiveParam.m_nVideoBitrate}, url={LiveParam.m_url1}, videos={liveVideoStreamInfos.Count}");

            for (int i = 0; i < liveVideoStreamInfos.Count; i++)
            {
                Log.Logger.Debug(
                    $"video{i + 1}：x={liveVideoStreamInfos[i].XLocation}, y={liveVideoStreamInfos[i].YLocation}, width={liveVideoStreamInfos[i].Width}, height={liveVideoStreamInfos[i].Height}");
            }


            StartLiveStreamResult startLiveStreamResult =
                await _sdkService.StartLiveStream(LiveParam, liveVideoStreamInfos.ToArray(), liveVideoStreamInfos.Count);

            LiveId = startLiveStreamResult.m_liveId;

            if (startLiveStreamResult.m_result.m_rc == 0)
            {
                HasPushLiveSuccessfully = true;
                Log.Logger.Debug($"【local push live succeeded】：liveId={startLiveStreamResult.m_liveId}");
            }
            else
            {
                Log.Logger.Error($"【local push live failed】：{startLiveStreamResult.m_result.m_message}");
            }

            return startLiveStreamResult;
        }

        public async Task<AsynCallResult> StopPushLiveStream()
        {
            if (LiveId != 0)
            {
                Log.Logger.Debug($"【local push live stop begins】：liveId={LiveId}");
                AsynCallResult stopAsynCallResult = await _sdkService.StopLiveStream(LiveId);
                LiveId = 0;

                Log.Logger.Debug(
                    $"【local push live stop result】：result={stopAsynCallResult}, msg={stopAsynCallResult.m_message}");
                return stopAsynCallResult;
            }
            return new AsynCallResult() {m_rc = -1, m_message = Messages.WarningNoLiveToStop};
        }
    }
}
