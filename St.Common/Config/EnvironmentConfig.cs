namespace St.Common
{
    public class EnvironmentConfig
    {
        public EnvironmentConfig()
        {
            InternalEnvironment = new EnvironmentItem()
            {
                Environment = RunEnv.InternalEnv,
        };
            ExternalEnvironment = new EnvironmentItem()
            {
                Environment = RunEnv.ExternalEnv,
            };
        }

        public EnvironmentItem InternalEnvironment { get; set; }
        public EnvironmentItem ExternalEnvironment { get; set; }
    }
}
