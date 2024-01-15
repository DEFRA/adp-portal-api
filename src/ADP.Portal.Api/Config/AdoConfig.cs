namespace ADP.Portal.Api.Config
{
    public class AdoConfig
    {
        public string OrganizationUrl { get; set; }
        public AzureAd AzureAd { get; set; }
    }

    public class AzureAd
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string TenantId { get; set; }
    }
}
