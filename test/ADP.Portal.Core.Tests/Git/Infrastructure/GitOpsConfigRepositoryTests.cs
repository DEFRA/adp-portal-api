﻿using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Infrastructure;
using AutoFixture;
using Mapster;
using NSubstitute;
using NUnit.Framework;
using Octokit;
using System.Reflection;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ADP.Portal.Core.Tests.Git.Infrastructure
{
    [TestFixture]
    public class GitOpsConfigRepositoryTests
    {
        private readonly IGitHubClient gitHubClientMock;
        private readonly GitOpsConfigRepository repository;
        private readonly IDeserializer deserializer;
        private readonly ISerializer serializer;
        

        public GitOpsConfigRepositoryTests()
        {
            gitHubClientMock = Substitute.For<IGitHubClient>();
            serializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
            deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
            repository = new GitOpsConfigRepository(gitHubClientMock, deserializer, serializer);
        }

        [Test]
        public async Task GetConfigAsync_WhenCalledWithStringType_ReturnsStringContent()
        {
            // Arrange
            var gitRepo = new GitRepo("repo", "branch", "org") ;


            var contentFile = CreateRepositoryContent("fileContent");

            gitHubClientMock.Repository.Content.GetAllContentsByRef(gitRepo.Organisation, gitRepo.Name, "fileName", gitRepo.BranchName)
                .Returns(new List<RepositoryContent> { contentFile });

            // Act
            var result = await repository.GetConfigAsync<string>("fileName", gitRepo);

            // Assert
            Assert.That(result, Is.EqualTo("fileContent"));
        }

        [Test]
        public async Task GetConfigAsync_WhenCalledWithNonStringType_DeserializesContent()
        {
            var gitRepo = new GitRepo("repo", "branch", "org");
            var yamlContent = "property:\n - name: \"test\"";
            var contentFile = CreateRepositoryContent(yamlContent);
            gitHubClientMock.Repository.Content.GetAllContentsByRef(gitRepo.Organisation, gitRepo.Name, "fileName", gitRepo.BranchName)
                .Returns([contentFile]);

            var result = await repository.GetConfigAsync<TestType>("fileName", gitRepo);

            Assert.That(result, Is.Not.Null);
            if (result != null)
            {
                Assert.That(result.Property[0].Name, Is.EqualTo("test"));
            }
        }

        private static RepositoryContent CreateRepositoryContent(string content)
        {
            var contentBytes = Encoding.UTF8.GetBytes(content);
            var encodedContent = Convert.ToBase64String(contentBytes);
            return new RepositoryContent("name", "path", "sha", 0, Octokit.ContentType.File, "downloadUrl", "url", "gitUrl", "htmlUrl", "base64", encodedContent, "target", "sub");
        }

        public class TestType
        {
            public required List<TestPropertyObject> Property { get; set; }
        }

        public class TestPropertyObject
        {
            public required string Name { get; set; }
        }

    }
}
