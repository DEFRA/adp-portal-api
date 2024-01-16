﻿namespace ADP.Portal.Api.Config
{
    public class AdoConfig
    {
        public string OrganizationUrl { get; set; }
        public bool UsePatToken { get; set; }
        public string PatToken { get; set; }
        public string PatTokenSecretName { get; set; }
    }
}
