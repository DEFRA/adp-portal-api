namespace ADP.Portal.Api.Config
{
    public class AdoConfig(string organizationUrl, bool usePatToken, string patToken, string patTokenSecretName)
    {
        public string OrganizationUrl { get; set; } = organizationUrl;
        public bool UsePatToken { get; set; } = usePatToken;
        public string PatToken { get; set; } = patToken;
        public string PatTokenSecretName { get; set; } = patTokenSecretName;
    }
}
