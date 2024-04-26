﻿using ADP.Portal.Core.Git.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Services.Common;
using Octokit;
using YamlDotNet.Serialization;

namespace ADP.Portal.Core.Git.Infrastructure
{
    public class GitOpsConfigRepository : IGitOpsConfigRepository
    {
        private readonly IGitHubClient gitHubClient;
        private readonly IDeserializer deserializer;
        private readonly ISerializer serializer;
        private readonly ILogger<GitOpsConfigRepository> logger;

        public GitOpsConfigRepository(IGitHubClient gitHubClient, IDeserializer deserializer, ISerializer serializer, ILogger<GitOpsConfigRepository> logger)
        {
            this.gitHubClient = gitHubClient;
            this.deserializer = deserializer;
            this.serializer = serializer;
            this.logger = logger;
        }

        public async Task<T?> GetConfigAsync<T>(string fileName, GitRepo gitRepo)
        {
            var file = await GetRepositoryFiles(gitRepo, fileName);
            if (typeof(T) == typeof(string))
            {
                return (T)Convert.ChangeType(file[0].Content, typeof(T));
            }

            var result = deserializer.Deserialize<T>(file[0].Content);
            return result;
        }

        public async Task<string> CreateConfigAsync(GitRepo gitRepo, string fileName, string content)
        {
            var response = await gitHubClient.Repository.Content.CreateFile(gitRepo.Organisation, gitRepo.Name, fileName, new CreateFileRequest($"Create config file: {fileName}", content, gitRepo.BranchName));
            return response.Commit.Sha;
        }

        public async Task<string> UpdateConfigAsync(GitRepo gitRepo, string fileName, string content)
        {
            var existingFile = await GetRepositoryFiles(gitRepo, fileName);

            if (existingFile.Any())
            {
                var response = await gitHubClient.Repository.Content.UpdateFile(gitRepo.Organisation, gitRepo.Name, fileName,
                    new UpdateFileRequest($"Update config file: {fileName}", content, existingFile[0].Sha, gitRepo.BranchName));
                return response.Commit.Sha;
            }
            return string.Empty;
        }

        public async Task<IEnumerable<KeyValuePair<string, Dictionary<object, object>>>> GetAllFilesAsync(GitRepo gitRepo, string path)
        {
            return await GetAllFilesContentsAsync(gitRepo, path);
        }

        public async Task PushFilesToRepository(GitRepo gitRepoFluxServices, string branchName, Dictionary<string, Dictionary<object, object>> generatedFiles)
        {
            var branchRef = await GetBranchAsync(gitRepoFluxServices, branchName);
            var message = branchRef == null ? $"{branchName.ToUpper()} Config": "Update config";
            
            logger.LogInformation("Creating commit for the branch:'{BranchName}'.", branchName);
            var commitRef = await CreateCommitAsync(gitRepoFluxServices, generatedFiles, message, branchRef == null ? null : branchName);
            if (commitRef != null)
            {
                if (branchRef == null)
                {
                    logger.LogInformation("Creating branch:'{BranchName}'.", branchName);
                    await CreateBranchAsync(gitRepoFluxServices, branchName, commitRef.Sha);

                    logger.LogInformation("Creating pull request for the branch:'{BranchName}'.", branchName);
                    await CreatePullRequestAsync(gitRepoFluxServices, branchName, message);
                }
                else
                {
                    logger.LogInformation("Updating branch:'{BranchName}' with the changes.", branchName);
                    await UpdateBranchAsync(gitRepoFluxServices, branchName, commitRef.Sha);
                }
            }
            else
            {
                logger.LogInformation("No changes found in the files for the branch:'{BranchName}'.", branchName);
            }
        }

        public async Task<bool> CreatePullRequestAsync(GitRepo gitRepo, string branchName, string message)
        {
            var repository = await gitHubClient.Repository.Get(gitRepo.Organisation, gitRepo.Name);

            var pullRequest = new NewPullRequest(message, branchName, gitRepo.BranchName);
            var createdPullRequest = await gitHubClient.PullRequest.Create(repository.Owner.Login, repository.Name, pullRequest);

            return createdPullRequest != null;
        }

        private async Task<IEnumerable<KeyValuePair<string, Dictionary<object, object>>>> GetAllFilesContentsAsync(GitRepo gitRepo, string path)
        {
            var repositoryItems = await GetRepositoryFiles(gitRepo, path);

            var fileTasks = repositoryItems
                .Where(item => item.Type == ContentType.File)
                .Select(async item =>
                {
                    var file = await GetRepositoryFiles(gitRepo, item.Path);
                    var result = deserializer.Deserialize<Dictionary<object, object>>(file[0].Content);
                    var list = new List<KeyValuePair<string, Dictionary<object, object>>>() { (new KeyValuePair<string, Dictionary<object, object>>(item.Path, result)) };
                    return list.AsEnumerable();
                });

            var dirTasks = repositoryItems
                .Where(item => item.Type == ContentType.Dir)
                .Select(async item =>
                {
                    return await GetAllFilesContentsAsync(gitRepo, item.Path);
                });

            var allTasks = fileTasks.Concat(dirTasks);
            var allResults = await Task.WhenAll(allTasks);

            return allResults.SelectMany(x => x);
        }

        private async Task<Reference?> GetBranchAsync(GitRepo gitRepo, string branchName)
        {
            try
            {
                return await gitHubClient.Git.Reference.Get(gitRepo.Organisation, gitRepo.Name, branchName);
            }
            catch (NotFoundException)
            {
                return default;
            }
        }

        private async Task<Reference> CreateBranchAsync(GitRepo gitRepo, string branchName, string sha)
        {
            return await gitHubClient.Git.Reference.Create(gitRepo.Organisation, gitRepo.Name, new NewReference(branchName, sha));
        }

        private async Task<Reference> UpdateBranchAsync(GitRepo gitRepo, string branchName, string sha)
        {
            return await gitHubClient.Git.Reference.Update(gitRepo.Organisation, gitRepo.Name, branchName, new ReferenceUpdate(sha));
        }

        private async Task<Commit?> CreateCommitAsync(GitRepo gitRepo, Dictionary<string, Dictionary<object, object>> generatedFiles, string message, string? branchName = null)
        {
            var branch = branchName ?? $"heads/{gitRepo.BranchName}";

            var repository = await gitHubClient.Repository.Get(gitRepo.Organisation, gitRepo.Name);

            var branchRef = await gitHubClient.Git.Reference.Get(repository.Owner.Login, repository.Name, branch);

            var latestCommit = await gitHubClient.Git.Commit.Get(repository.Owner.Login, repository.Name, branchRef.Object.Sha);

            var featureBranchTree = await CreateTree(gitHubClient, repository, generatedFiles, latestCommit.Sha);
            if (featureBranchTree != null)
            {
                var featureBranchCommit = await CreateCommit(gitHubClient, repository, message, featureBranchTree.Sha, branchRef.Object.Sha);
                return featureBranchCommit;
            }
            return default;
        }

        private async Task<TreeResponse?> CreateTree(IGitHubClient client, Repository repository, Dictionary<string, Dictionary<object, object>> treeContents, string parentSha)
        {
            var newTree = new NewTree() { BaseTree = parentSha };

            var existingTree = await client.Git.Tree.GetRecursive(repository.Owner.Login, repository.Name, parentSha);

            var tasks = treeContents.Select(async treeContent =>
            {
                var baselineBlob = new NewBlob
                {
                    Content = serializer.Serialize(treeContent.Value).Replace(FluxConstants.TEMPLATE_IMAGEPOLICY_KEY, FluxConstants.TEMPLATE_IMAGEPOLICY_KEY_VALUE),
                    Encoding = EncodingType.Utf8
                };

                var baselineBlobResult = await client.Git.Blob.Create(repository.Owner.Login, repository.Name, baselineBlob);

                return new NewTreeItem
                {
                    Type = TreeType.Blob,
                    Mode = Octokit.FileMode.File,
                    Path = treeContent.Key,
                    Sha = baselineBlobResult.Sha
                };
            });

            var newTreeItems = await Task.WhenAll(tasks);

            newTree.Tree.AddRange(newTreeItems.Where(newItem => !existingTree.Tree.Any(existingItem => existingItem.Path == newItem.Path && existingItem.Sha == newItem.Sha)));

            if (newTree.Tree.Count > 0)
            {
                return await client.Git.Tree.Create(repository.Owner.Login, repository.Name, newTree);
            }
            return default;
        }

        private static async Task<Commit> CreateCommit(IGitHubClient client, Repository repository, string message, string sha, string parent)
        {
            var newCommit = new NewCommit(message, sha, parent);
            return await client.Git.Commit.Create(repository.Owner.Login, repository.Name, newCommit);
        }


        private async Task<IReadOnlyList<RepositoryContent>> GetRepositoryFiles(GitRepo gitRepo, string filePathOrName)
        {
            return await gitHubClient.Repository.Content.GetAllContentsByRef(gitRepo.Organisation, gitRepo.Name, filePathOrName, gitRepo.BranchName);
        }
    }
}