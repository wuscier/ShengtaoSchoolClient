using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using St.Common;
using System.IO;
using Serilog;

namespace St.Meeting
{
    public class LocalRecordService : IRecord
    {
        private static readonly string ConfigFile = Path.Combine(Environment.CurrentDirectory, GlobalResources.ConfigPath);
        private readonly ISdk _sdkService;

        public LocalRecordService()
        {
            _sdkService = DependencyResolver.Current.GetService<ISdk>();
        }

        public string RecordDirectory { get; private set; }

        public int RecordId { get; set; }

        public RecordParam RecordParam { get; private set; }

        public void ResetStatus()
        {
            RecordDirectory = string.Empty;
            RecordId = 0;
        }

        public bool GetRecordParam()
        {
            if (!File.Exists(ConfigFile))
            {
                return false;
            }

            try
            {
                Common.RecordParam recordParam = new RecordParam()
                {
                    AudioBitrate = 64,
                    BitsPerSample = 16,
                    Channels = 1,
                    SampleRate = 8000,
                    VideoBitrate = int.Parse(GlobalData.Instance.AggregatedConfig.RecordConfig.CodeRate)
                };

                string[] resolutionStrings =
                    GlobalData.Instance.AggregatedConfig.RecordConfig.Resolution.Split(new[] {'*'},
                        StringSplitOptions.RemoveEmptyEntries);

                recordParam.Width = int.Parse(resolutionStrings[0]);
                recordParam.Height = int.Parse(resolutionStrings[1]);

                RecordParam = recordParam;
                RecordDirectory = GlobalData.Instance.AggregatedConfig.RecordConfig.RecordPath;

                return true;
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"【get record param exception】：{ex}");
                return false;
            }
        }

        public async Task<AsynCallResult> RefreshLiveStream(List<LiveVideoStreamInfo> openedStreamInfos)
        {
            if (RecordId != 0)
            {
                Log.Logger.Debug(
                    $"【local record live refresh begins】：liveId={RecordId}, videos={openedStreamInfos.Count}");
                for (int i = 0; i < openedStreamInfos.Count; i++)
                {
                    Log.Logger.Debug(
                        $"video{i + 1}：x={openedStreamInfos[i].XLocation}, y={openedStreamInfos[i].YLocation}, width={openedStreamInfos[i].Width}, height={openedStreamInfos[i].Height}");
                }

                AsynCallResult updateAsynCallResult =
                    await
                        _sdkService.UpdateLiveVideoStreams(RecordId, openedStreamInfos.ToArray(),
                            openedStreamInfos.Count);
                Log.Logger.Debug(
                    $"【local record live refresh result】：result={updateAsynCallResult.m_rc}, msg={updateAsynCallResult.m_message}");
                return updateAsynCallResult;
            }

            return new AsynCallResult() {m_rc = -1, m_message = Messages.WarningNoLiveToRefresh};
        }

        public async Task<LocalRecordResult> StartRecord(List<LiveVideoStreamInfo> liveVideoStreamInfos)
        {
            if (string.IsNullOrEmpty(RecordDirectory))
            {
                return new LocalRecordResult()
                {
                    m_result = new AsynCallResult()
                    {
                        m_rc = -1,
                        m_message = Messages.WarningRecordDirectoryNotSet
                    }
                };
            }


            if (RecordParam.Width == 0 || RecordParam.Height == 0 || RecordParam.VideoBitrate == 0)
            {
                return new LocalRecordResult()
                {
                    m_result = new AsynCallResult()
                    {
                        m_rc = -1,
                        m_message = Messages.WarningLiveResolutionNotSet
                    }
                };
            }

            int result = await _sdkService.SetRecordDirectory(RecordDirectory);
            AsynCallResult setRecordParamResult = await _sdkService.SetRecordParam(RecordParam);

            string recordFileName = $"{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.mp4";

            Log.Logger.Debug(
                $"【local record live begins】：width={RecordParam.Width}, height={RecordParam.Height}, bitrate={RecordParam.VideoBitrate}, path={Path.Combine(RecordDirectory, recordFileName)}, videos={liveVideoStreamInfos.Count}");

            for (int i = 0; i < liveVideoStreamInfos.Count; i++)
            {
                Log.Logger.Debug(
                    $"video{i + 1}：x={liveVideoStreamInfos[i].XLocation}, y={liveVideoStreamInfos[i].YLocation}, width={liveVideoStreamInfos[i].Width}, height={liveVideoStreamInfos[i].Height}");
            }

            LocalRecordResult localRecordResult =
                await
                    _sdkService.StartRecord(recordFileName, liveVideoStreamInfos.ToArray(), liveVideoStreamInfos.Count);

            RecordId = localRecordResult.m_liveId;

            if (localRecordResult.m_result.m_rc == 0)
            {
                Log.Logger.Debug($"【local record live succeeded】：liveId={localRecordResult.m_liveId}");
            }
            else
            {
                Log.Logger.Error($"【local record live failed】：{localRecordResult.m_result.m_message}");
            }

            return localRecordResult;
        }

        public async Task<AsynCallResult> StopRecord()
        {
            if (RecordId != 0)
            {
                Log.Logger.Debug($"【local record live stop begins】：liveId={RecordId}");
                AsynCallResult stopAsynCallResult = await _sdkService.StopRecord();
                RecordId = 0;

                Log.Logger.Debug(
                    $"【local record live stop result】：result={stopAsynCallResult.m_rc}, msg={stopAsynCallResult.m_message}");
                return stopAsynCallResult;
            }

            return new AsynCallResult() {m_rc = -1, m_message = Messages.WarningNoLiveToStop};
        }
    }
}
