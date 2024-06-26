﻿using ADP.Portal.Core.Git.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace ADP.Portal.Core.Git.Services;

public class GitHubService : IGitHubService
{
    private readonly IGitHubClient client;
    private readonly IOptions<GitHubOptions> options;
    private readonly ILogger<GitHubService> logger;

    public GitHubService(IGitHubClient client, IOptions<GitHubOptions> options, ILogger<GitHubService> logger)
    {
        this.client = client;
        this.options = options;
        this.logger = logger;
    }

    public async Task<GithubTeamDetails?> SyncTeamAsync(GithubTeamUpdate team, CancellationToken cancellationToken)
    {
        logger.LogInformation("Setting team details for team {TeamId}", team.Id);
        var currentTeam = await GetTeamDetails(team.Id);

        if (currentTeam is null)
        {
            logger.LogInformation("Team {TeamId} does not exist, it will be created.", team.Id);
            return await CreateTeamAsync(team);
        }
        else
        {
            logger.LogInformation("Team {TeamId} already exists ({TeamName}), it will be updated.", currentTeam.Id, team.Name);
            return await UpdateTeamAsync(currentTeam, team);
        }
    }

    private async Task<GithubTeamDetails?> UpdateTeamAsync(GithubTeamDetails currentTeam, GithubTeamUpdate team)
    {
        if (TryBuildUpdate(currentTeam, team, out var update))
        {
            logger.LogInformation("Updating details for team {TeamId} ({TeamName}).", currentTeam.Id, currentTeam.Name);
            var updatedTeam = await TryUpdateTeam(currentTeam.Id, update);
            if (updatedTeam is null)
                return null;
            logger.LogInformation("Team {TeamId} ({TeamName}).", currentTeam.Id, team.Name);

            currentTeam = currentTeam with
            {
                Description = updatedTeam.Description,
                IsPublic = updatedTeam.Privacy.Value is TeamPrivacy.Closed,
                Id = updatedTeam.Id,
                Name = updatedTeam.Name,
                Slug = updatedTeam.Slug
            };
        }

        await SyncTeamMembers(
            currentTeam.Id,
            await GetOrgTeamMembers(),
            BuildTeamRoleDictionary(currentTeam.Maintainers, currentTeam.Members),
            BuildTeamRoleDictionary(team.Maintainers, team.Members));

        return currentTeam with
        {
            Maintainers = team.Maintainers ?? currentTeam.Maintainers,
            Members = team.Members ?? currentTeam.Members
        };
    }

    private static bool TryBuildUpdate(GithubTeamDetails currentTeam, GithubTeamUpdate team, [NotNullWhen(true)] out UpdateTeam? update)
    {
        if (IsUnchanged(currentTeam.Name, team.Name)
            && IsUnchanged(currentTeam.Description, team.Description)
            && IsUnchanged(currentTeam.IsPublic, team.IsPublic)
            && IsUnchanged(currentTeam.Name, team.Name))
        {
            update = null;
            return false;
        }

        update = new(team.Name)
        {
            Description = team.Description ?? currentTeam.Description,
            Privacy = (team.IsPublic ?? currentTeam.IsPublic) ? TeamPrivacy.Closed : TeamPrivacy.Secret
        };
        return true;
    }

    private static bool IsUnchanged<T>(T current, T? update)
    {
        return update is null || Equals(current, update);
    }

    private static Dictionary<string, TeamRole> BuildTeamRoleDictionary(IEnumerable<string>? maintainers, IEnumerable<string>? members)
    {
        maintainers ??= [];
        members ??= [];

        members = members.Except(maintainers, StringComparer.OrdinalIgnoreCase);

        return Enumerable.Concat(
            maintainers.Select(m => KeyValuePair.Create(m, TeamRole.Maintainer)),
            members.Select(m => KeyValuePair.Create(m, TeamRole.Member)))
            .ToDictionary(StringComparer.OrdinalIgnoreCase);
    }

    private async Task<GithubTeamDetails?> CreateTeamAsync(GithubTeamUpdate team)
    {
        var allowedMembers = await GetOrgTeamMembers();
        var request = new NewTeam(team.Name)
        {
            Description = team.Description,
            Privacy = (team.IsPublic ?? true) ? TeamPrivacy.Closed : TeamPrivacy.Secret,
        };

        foreach (var member in team.Maintainers ?? [])
        {
            if (allowedMembers.Contains(member))
                request.Maintainers.Add(member);
            else
                logger.LogInformation("Skipping {Member} membership of {TeamName} as they are not already a member of the organisation", member, team.Name);
        }

        logger.LogInformation("Creating team {TeamName}.", team.Name);
        var newTeam = await TryCreateTeam(request);
        if (newTeam is null)
            return await TryAdoptTeamAsync(team);

        logger.LogInformation("Team {TeamId} ({TeamName}) has been created, syncing members.", newTeam.Id, newTeam.Name);
        await SyncTeamMembers(newTeam.Id, allowedMembers, [], BuildTeamRoleDictionary(null, team.Members));

        return new()
        {
            Id = newTeam.Id,
            Name = newTeam.Name,
            Slug = newTeam.Slug,
            Description = newTeam.Description,
            IsPublic = newTeam.Privacy.Value == TeamPrivacy.Closed,
            Maintainers = request.Maintainers,
            Members = team.Members ?? []
        };
    }

    private async Task<GithubTeamDetails?> TryAdoptTeamAsync(GithubTeamUpdate team)
    {
        if (options.Value.TeamDenyList.Contains(team.Name, StringComparer.OrdinalIgnoreCase))
        {
            logger.LogError("Cannot adopt team {TeamName} ({TeamId}) as it has been explicitly added to the ADP team denylist.", team.Name, team.Id);
            return null;
        }

        var toAdopt = await GetTeamDetails(team.Name);
        if (toAdopt is null || !toAdopt.Members.Concat(toAdopt.Maintainers).Contains(options.Value.AdminLogin))
        {
            logger.LogError("Cannot adopt team {TeamName} ({TeamId}) as it does not have {AdminLogin} as a member.", team.Name, team.Id, options.Value.AdminLogin);
            return null;
        }

        return await UpdateTeamAsync(toAdopt, team);
    }

    private async Task SyncTeamMembers(int teamId, IReadOnlyCollection<string> allowedMembers, Dictionary<string, TeamRole> currentMembers, Dictionary<string, TeamRole> targetMembers)
    {
        targetMembers[options.Value.AdminLogin] = TeamRole.Maintainer;
        var setMembers = targetMembers
            .Where(kvp => !currentMembers.TryGetValue(kvp.Key, out var currentRole) || currentRole < kvp.Value)
            .Select(kvp => SetMemberRole(kvp.Key, kvp.Value));
        var removeMembers = currentMembers
            .Where(kvp => !targetMembers.ContainsKey(kvp.Key))
            .Select(kvp => RemoveMember(kvp.Key));
        var mutations = setMembers.Concat(removeMembers);

        // If we want to run the changes in parallel, change to `await Task.WhenAll(mutations)`
        foreach (var task in mutations)
            await task;

        async Task SetMemberRole(string member, TeamRole role)
        {
            if (!allowedMembers.Contains(member))
            {
                logger.LogInformation("Skipping {Member} membership of {TeamId} as they are not already a member of the organisation", member, teamId);
                return;
            }

            logger.LogInformation("Setting {Member} membership of {TeamId} to {Role}", member, teamId, role);
            try
            {
                await client.Organization.Team.AddOrEditMembership(teamId, member, new(role));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while setting {Member} membership of {TeamId} to {Role}.", member, teamId, role);
            }
        }

        async Task RemoveMember(string member)
        {
            logger.LogInformation("Removing {Member} from {TeamId}.", member, teamId);
            try
            {
                await client.Organization.Team.RemoveMembership(teamId, member);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while removing {Member} from {TeamId}.", member, teamId);
            }
        }
    }

    private async Task<GithubTeamDetails?> GetTeamDetails(int? teamId)
    {
        if (teamId is not int id)
            return null;

        logger.LogInformation("Getting details of {TeamId}.", teamId);
        var team = await TryGetTeam(id);
        return await ResolveTeamDetails(team);
    }

    private async Task<GithubTeamDetails?> GetTeamDetails(string? teamName)
    {
        if (teamName is not string name)
            return null;

        logger.LogInformation("Getting details of {TeamName}.", name);
        var team = await TryGetTeam(name);
        return await ResolveTeamDetails(team);
    }

    private async Task<GithubTeamDetails?> ResolveTeamDetails(Team? team)
    {
        if (team is null || !StringComparer.OrdinalIgnoreCase.Equals(team.Organization.Login, options.Value.Organisation))
        {
            return null;
        }

        logger.LogInformation("Getting members and maintainers of {TeamId}.", team.Id);
        var members = await client.Organization.Team.GetAllMembers(team.Id, new TeamMembersRequest(TeamRoleFilter.Member));
        var maintainers = await client.Organization.Team.GetAllMembers(team.Id, new TeamMembersRequest(TeamRoleFilter.Maintainer));

        return new()
        {
            Id = team.Id,
            Name = team.Name,
            Description = team.Description,
            IsPublic = team.Privacy.Value is not TeamPrivacy.Secret,
            Maintainers = maintainers.Select(u => u.Login).ToArray(),
            Members = members.Select(u => u.Login).ToArray(),
            Slug = team.Slug
        };
    }

    private async Task<Team?> TryCreateTeam(NewTeam request)
    {
        try
        {
            return await client.Organization.Team.Create(options.Value.Organisation, request);
        }
        catch (ApiValidationException)
        {
            return null;
        }
    }

    private async Task<Team?> TryUpdateTeam(int teamId, UpdateTeam request)
    {
        try
        {
            return await client.Organization.Team.Update(teamId, request);
        }
        catch (ApiValidationException)
        {
            return null;
        }
    }

    private async Task<Team?> TryGetTeam(int teamId)
    {
        try
        {
            return await client.Organization.Team.Get(teamId);
        }
        catch (NotFoundException)
        {
            return default;
        }
    }

    private async Task<Team?> TryGetTeam(string teamName)
    {
        try
        {
            return await client.Organization.Team.GetByName(options.Value.Organisation, teamName);
        }
        catch (NotFoundException)
        {
            return default;
        }
    }

    private async Task<ImmutableHashSet<string>> GetOrgTeamMembers()
    {
        try
        {
            var members = await client.Organization.Member.GetAll(options.Value.Organisation);
            return members.Select(m => m.Login).ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
        }
        catch (ApiException)
        {
            return [];
        }
    }
}