using System.ComponentModel;

namespace St.Common
{
    public enum RunEnv
    {
        [Description("公网环境")]
        ExternalEnv,
        [Description("内网环境")]
        InternalEnv
    }
}
