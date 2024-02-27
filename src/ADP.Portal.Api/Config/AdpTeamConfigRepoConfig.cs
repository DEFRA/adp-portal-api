namespace ADP.Portal.Api.Config
{
    public class AdpTeamConfigRepoConfig
    {
        public required string RepoUrl { get; set; }

        public required string BranchName { get; set; }

        public required string LocalPath { get; set; }

        public required string UserName { get; set; }

        public required string UserEmail { get; set; }
    }
}
