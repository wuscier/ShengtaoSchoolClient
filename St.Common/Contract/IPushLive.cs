using System.Collections.Generic;
using System.Threading.Tasks;

namespace St.Common
{
    public interface IPushLive
    {
        void ResetStatus();

        bool HasPushLiveSuccessfully { get; set; }

        int LiveId { get; }

        LiveParam LiveParam { get; }

        LiveParam GetLiveParam();

        Task<StartLiveStreamResult> StartPushLiveStream(List<LiveVideoStreamInfo> liveVideoStreamInfos,
            string pushLiveUrl = "");

        Task<AsynCallResult> RefreshLiveStream(List<LiveVideoStreamInfo> liveVideoStreamInfos);
        Task<AsynCallResult> StopPushLiveStream();
    }
}
