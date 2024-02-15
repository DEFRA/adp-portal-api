namespace ADP.Portal.Api.Models
{
    public class ADConfig
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string TenantId { get; set; }
        public string Instance { get; set; }
        public string GraphResource { get; set; }
        public string GraphResourceEndPoint { get; set; }
        public string GroupId { get; set; }
    }
}
