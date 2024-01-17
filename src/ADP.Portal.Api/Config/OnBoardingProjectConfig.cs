namespace ADP.Portal.Api.Config
{
    public class OnBoardingProjectConfig
    {
        public List<EnvironmentConfig> Environments { get; set; }
    }

    public class EnvironmentConfig
    {
        public string Name { get; set; }
    }
}
