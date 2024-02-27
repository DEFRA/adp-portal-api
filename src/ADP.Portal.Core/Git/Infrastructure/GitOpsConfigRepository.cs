using LibGit2Sharp;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;


namespace ADP.Portal.Core.Git.Infrastructure
{
    public class GitOpsConfigRepository : IGitOpsConfigRepository
    {
        private readonly IRepository repository;

        public GitOpsConfigRepository(IRepository repository)
        {
            this.repository = repository;
        }

        public bool IsConfigExists(string fileName)
        {
            var latestCommit = repository.Head.Tip;
            var file = latestCommit[fileName];

            return file != null;
        }

        public T? ReadYamlFromRepo<T>(string fileName)
        {
            var latestCommit = repository.Head.Tip;

            var file = latestCommit[fileName];

            if (file != null)
            {
                var blob = (Blob)repository.Lookup(file.Target.Id);
                if (blob != null)
                {
                    var yamlContent = blob.GetContentText();

                    var deserializer = new DeserializerBuilder()
                            .WithNamingConvention(CamelCaseNamingConvention.Instance)
                            .Build();

                    var result = deserializer.Deserialize<T>(yamlContent);
                    return result;
                }
            }

            return default;
        }
    }
}
