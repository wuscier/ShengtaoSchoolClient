using System.Collections.Generic;
using System.Threading.Tasks;

namespace St.Common
{
    public interface IRecord
    {
        int RecordId { get; }
        string RecordDirectory { get; }
        RecordParam RecordParam { get; }

        void ResetStatus();

        bool GetRecordParam();

        Task<LocalRecordResult> StartRecord(List<LiveVideoStreamInfo> liveVideoStreamInfos);
        Task<AsynCallResult> StopRecord();
        Task<AsynCallResult> RefreshLiveStream(List<LiveVideoStreamInfo> liveVideoStreamInfos);
    }
}
