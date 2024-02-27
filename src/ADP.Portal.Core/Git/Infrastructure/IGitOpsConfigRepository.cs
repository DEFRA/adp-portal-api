namespace ADP.Portal.Core.Git.Infrastructure
{
    public interface IGitOpsConfigRepository
    {
        bool IsConfigExists(string fileName);
        T? ReadYamlFromRepo<T>(string fileName);
    }
}