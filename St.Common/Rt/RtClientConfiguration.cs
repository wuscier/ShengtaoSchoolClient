using St.Common.Contract;

namespace St.Common
{
    public class RtClientConfiguration : IRtClientConfiguration
    {
        public string RtServer => GlobalData.Instance.AggregatedConfig.RtServerAddress;
    }
}



