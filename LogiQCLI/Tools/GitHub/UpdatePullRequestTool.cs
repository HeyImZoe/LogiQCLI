using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Octokit;

using LogiQCLI.Tools.Core.Objects;
using LogiQCLI.Infrastructure.ApiClients.GitHub;
using LogiQCLI.Tools.GitHub.Objects;
using LogiQCLI.Tools.Core.Interfaces;
using LogiQCLI.Infrastructure.ApiClients.GitHub.Objects;

namespace LogiQCLI.Tools.GitHub
{
    [ToolMetadata("GitHub", Tags = new[] { "github", "update" })]
    public class UpdatePullRequestTool : ITool
    {
        private readonly GitHubClientWrapper _gitHubClient;

        public override List<string> RequiredServices => new List<string> { "GitHubClientWrapper" };

        public UpdatePullRequestTool(GitHubClientWrapper gitHubClient)
        {
            _gitHubClient = gitHubClient;
        }

        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "update_github_pull_request",
                Description = "Updates an existing GitHub pull request's title, body, or state. " +
                              "Requires GitHub authentication token to be configured. " +
                              "Use this tool to modify pull request details or close pull requests.",
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new
                    {
                        owner = new
                        {
                            type = "string",
                            description = "Repository owner (username or organization name). " +
                                         "Required unless default owner is configured."
                        },
                        repo = new
                        {
                            type = "string",
                            description = "Repository name. " +
                                         "Required unless default repo is configured."
                        },
                        pullRequestNumber = new
                        {
                            type = "integer",
                            description = "Pull request number to update. " +
                                         "Must be an existing pull request number in the repository. " +
                                         "Example: 42"
                        },
                        title = new
                        {
                            type = "string",
                            description = "New title for the pull request. " +
                                         "Leave empty to keep current title unchanged."
                        },
                        body = new
                        {
                            type = "string",
                            description = "New body content for the pull request. Supports GitHub markdown. " +
                                         "Leave empty to keep current body unchanged."
                        },
                        state = new
                        {
                            type = "string",
                            description = "New state for the pull request. Options: 'open', 'closed'. " +
                                         "Leave empty to keep current state unchanged."
                        }
                    },
                    Required = new[] { "pullRequestNumber" }
                }
            };
        }

        public override async Task<string> Execute(string args)
        {
            try
            {
                var arguments = JsonSerializer.Deserialize<UpdatePullRequestArguments>(args);
                if (arguments == null || arguments.PullRequestNumber <= 0)
                {
                    return "Error: Invalid arguments. Pull request number is required.";
                }

                if (string.IsNullOrEmpty(arguments.Owner) || string.IsNullOrEmpty(arguments.Repo))
                {
                    return "Error: Owner and repo are required. Configure default values or provide them explicitly.";
                }

                var pullRequestUpdate = new PullRequestUpdate();
                bool hasUpdates = false;

                if (!string.IsNullOrEmpty(arguments.Title))
                {
                    pullRequestUpdate.Title = arguments.Title;
                    hasUpdates = true;
                }

                if (!string.IsNullOrEmpty(arguments.Body))
                {
                    pullRequestUpdate.Body = arguments.Body;
                    hasUpdates = true;
                }

                if (!string.IsNullOrEmpty(arguments.State))
                {
                    if (Enum.TryParse<ItemState>(arguments.State, true, out var itemState))
                    {
                        pullRequestUpdate.State = itemState;
                        hasUpdates = true;
                    }
                    else
                    {
                        return $"Error: Invalid state '{arguments.State}'. Must be 'open' or 'closed'.";
                    }
                }

                if (!hasUpdates)
                {
                    return "Error: No updates specified. Provide at least one field to update (title, body, or state).";
                }

                var pullRequest = await _gitHubClient.UpdatePullRequestAsync(arguments.Owner, arguments.Repo, arguments.PullRequestNumber, pullRequestUpdate);

                var result = $"Successfully updated pull request #{pullRequest.Number}: {pullRequest.Title}\n";
                result += $"State: {pullRequest.State}\n";
                result += $"Draft: {pullRequest.Draft}\n";
                result += $"Updated: {pullRequest.UpdatedAt:yyyy-MM-dd HH:mm:ss} UTC\n";
                result += $"URL: {pullRequest.HtmlUrl}\n";

                if (pullRequest.Labels.Count > 0)
                {
                    result += $"Labels: {string.Join(", ", pullRequest.Labels.Select(l => l.Name))}\n";
                }

                if (pullRequest.Assignees.Count > 0)
                {
                    result += $"Assignees: {string.Join(", ", pullRequest.Assignees.Select(a => a.Login))}\n";
                }

                return result.TrimEnd();
            }
            catch (GitHubClientException ex)
            {
                return $"GitHub API Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error updating GitHub pull request: {ex.Message}";
            }
        }
    }
}
