﻿using ADP.Portal.Core.Git.Entities;
using Octokit;

namespace ADP.Portal.Core.Git.Infrastructure
{
    public interface IGitHubRepository
    {
        Task<T?> GetConfigAsync<T>(string fileName, GitRepo gitRepo);

        Task<string> CreateConfigAsync(GitRepo gitRepo, string fileName, string content);

        Task<string> UpdateConfigAsync(GitRepo gitRepo, string fileName, string content);

        Task<IEnumerable<KeyValuePair<string, Dictionary<object, object>>>> GetAllFilesAsync(GitRepo gitRepo, string path);

        Task<Reference?> GetBranchAsync(GitRepo gitRepo, string branchName);

        Task<Reference> CreateBranchAsync(GitRepo gitRepo, string branchName, string sha);

        Task<Reference> UpdateBranchAsync(GitRepo gitRepo, string branchName, string sha);

        Task<Commit?> CreateCommitAsync(GitRepo gitRepo, Dictionary<string, Dictionary<object, object>> generatedFiles, string message, string? branchName = null);

        Task<bool> CreatePullRequestAsync(GitRepo gitRepo, string branchName, string message);
    }
}