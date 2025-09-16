using System.Text.Json;
using System.Text.Json.Nodes;
using Seq.Apps;
using Seq.Apps.LogEvents;
using System.Text;

namespace Seq.App.Redmine;

[SeqApp("Redmine Issue Creator", Description = "Creates Redmine issues from Seq events")]
public class RedmineApp : SeqApp, Seq.Apps.ISubscribeToAsync<LogEventData>
{
    [SeqAppSetting(
        DisplayName = "Redmine URL",
        HelpText = "The base URL of your Redmine instance (e.g., https://redmine.example.com)",
        IsOptional = false)]
    public string? RedmineUrl { get; set; }

    [SeqAppSetting(
        DisplayName = "API Key",
        HelpText = "Your Redmine API key",
        IsOptional = false)]
    public string? ApiKey { get; set; }

    [SeqAppSetting(
        DisplayName = "Project ID",
        HelpText = "The ID or identifier of the Redmine project where issues will be created",
        IsOptional = false)]
    public string? ProjectId { get; set; }

    [SeqAppSetting(
        DisplayName = "Tracker ID",
        HelpText = "The tracker ID to use for created issues (default: 1 for Bug)",
        IsOptional = true)]
    public int TrackerId { get; set; } = 1;

    [SeqAppSetting(
        DisplayName = "Priority ID",
        HelpText = "The priority ID to use for created issues (default: 4 for Normal)",
        IsOptional = true)]
    public int PriorityId { get; set; } = 4;

    [SeqAppSetting(
        DisplayName = "Minimum Level",
        HelpText = "Only create issues for events at or above this level",
        IsOptional = true)]
    public LogEventLevel MinimumLevel { get; set; } = LogEventLevel.Error;

    [SeqAppSetting(
        DisplayName = "Subject Template",
        HelpText = "Template for the issue subject. Use {Level}, {MessageTemplate}, {Exception} placeholders",
        IsOptional = true)]
    public string SubjectTemplate { get; set; } = "[{Level}] {MessageTemplate}";

    // Additional optional Redmine fields
    [SeqAppSetting(
        DisplayName = "Status ID",
        HelpText = "Optional status ID to set on created issues",
        IsOptional = true)]
    public int? StatusId { get; set; }

    [SeqAppSetting(
        DisplayName = "Category ID",
        HelpText = "Optional category ID to set on created issues",
        IsOptional = true)]
    public int? CategoryId { get; set; }

    [SeqAppSetting(
        DisplayName = "Fixed Version ID",
        HelpText = "Optional fixed version (target version) ID to set on created issues",
        IsOptional = true)]
    public int? FixedVersionId { get; set; }

    [SeqAppSetting(
        DisplayName = "Assigned To ID",
        HelpText = "Optional user ID to assign the created issue to",
        IsOptional = true)]
    public int? AssignedToId { get; set; }

    [SeqAppSetting(
        DisplayName = "Parent Issue ID",
        HelpText = "Optional parent issue ID",
        IsOptional = true)]
    public int? ParentIssueId { get; set; }

    [SeqAppSetting(
        DisplayName = "Watcher User IDs (CSV)",
        HelpText = "Optional comma-separated list of user IDs to add as watchers",
        IsOptional = true)]
    public string? WatcherUserIdsCsv { get; set; }

    [SeqAppSetting(
        DisplayName = "Is Private",
        HelpText = "Whether the created issue should be private",
        IsOptional = true)]
    public bool? IsPrivate { get; set; }

    [SeqAppSetting(
        DisplayName = "Estimated Hours",
        HelpText = "Optional estimated hours for the issue",
        IsOptional = true)]
    public decimal? EstimatedHours { get; set; }

    [SeqAppSetting(
        DisplayName = "Custom Fields",
        HelpText = "Optional custom fields. Provide a JSON array like [{\"id\":1,\"value\":\"x\"}] or CSV like '1:value,2:value'. If empty a default set will be used.",
        IsOptional = true)]
    public string? CustomFields { get; set; }

    [SeqAppSetting(
        DisplayName = "Seq Instance URL",
        HelpText = "Optional base URL of your Seq instance used to build a link to the original event (e.g., https://seq.example.com)",
        IsOptional = true)]
    public string? SeqInstanceUrl { get; set; }

    [SeqAppSetting(
        DisplayName = "Description Template",
        HelpText = "Optional template for the issue description. If not set, the app will include a link to the event, stack trace (if any) and all event properties.",
        IsOptional = true)]
    public string? DescriptionTemplate { get; set; }

    private static readonly HttpClient HttpClient = new();
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = null, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };

    public async Task OnAsync(Event<LogEventData> evt)
    {
        try
        {
            // Check if event meets minimum level requirement
            if (evt.Data.Level < MinimumLevel)
                return;

            // Validate required settings
            if (string.IsNullOrWhiteSpace(RedmineUrl) ||
                string.IsNullOrWhiteSpace(ApiKey) ||
                string.IsNullOrWhiteSpace(ProjectId))
            {
                Log.Warning("Redmine app not configured properly. Missing URL, API key, or project ID.");
                return;
            }

            await CreateRedmineIssue(evt);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to create Redmine issue for event {EventId}", evt.Id);
        }
    }

    private async Task CreateRedmineIssue(Event<LogEventData> evt)
    {
        var options = new RedmineOptions
        {
            ProjectId = ProjectId,
            TrackerId = TrackerId,
            PriorityId = PriorityId,
            StatusId = StatusId,
            CategoryId = CategoryId,
            FixedVersionId = FixedVersionId,
            AssignedToId = AssignedToId,
            ParentIssueId = ParentIssueId,
            WatcherUserIdsCsv = WatcherUserIdsCsv,
            IsPrivate = IsPrivate,
            EstimatedHours = EstimatedHours,
            CustomFields = CustomFields,
            SubjectTemplate = SubjectTemplate,
            SeqInstanceUrl = SeqInstanceUrl,
            DescriptionTemplate = DescriptionTemplate
        };

        var issueNode = RedmineIssueBuilder.BuildIssueJObject(options, evt);
        var root = new JsonObject { ["issue"] = issueNode };

        var json = JsonSerializer.Serialize(root, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var requestUrl = $"{RedmineUrl?.TrimEnd('/')}/issues.json";

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
        request.Content = content;
        request.Headers.Add("X-Redmine-API-Key", ApiKey);

        var response = await HttpClient.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var createdNode = JsonNode.Parse(responseContent);
            var issueId = createdNode? ["issue"]? ["id"]?.GetValue<int?>();
            Log.Information("Created Redmine issue #{IssueId} for event {EventId}",
                issueId, evt.Id);
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Log.Error("Failed to create Redmine issue. Status: {StatusCode}, Response: {Response}",
                response.StatusCode, errorContent);
        }

    }
}