using Seq.Apps;
using Seq.Apps.LogEvents;
using Serilog;
using System.Text;
using Newtonsoft.Json;

namespace Seq.App.Redmine;

[SeqApp("Redmine Issue Creator", Description = "Creates Redmine issues from Seq events")]
public class RedmineApp : SeqApp, ISubscribeTo<LogEventData>
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

    private static readonly HttpClient HttpClient = new();

    public void On(Event<LogEventData> evt)
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

            CreateRedmineIssue(evt).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to create Redmine issue for event {EventId}", evt.Id);
        }
    }

    private async Task CreateRedmineIssue(Event<LogEventData> evt)
    {
        try
        {
            var subject = BuildSubject(evt);
            var description = BuildDescription(evt);

            var issue = new
            {
                issue = new
                {
                    project_id = ProjectId,
                    tracker_id = TrackerId,
                    priority_id = PriorityId,
                    subject = subject,
                    description = description,
                    custom_fields = new[]
                    {
                        new { id = 1, value = evt.Id }, // Assuming custom field for Seq Event ID
                        new { id = 2, value = evt.Data.Level.ToString() }, // Assuming custom field for Level
                    }
                }
            };

            var json = JsonConvert.SerializeObject(issue);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var requestUrl = $"{RedmineUrl?.TrimEnd('/')}/issues.json";
            
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Content = content;
            request.Headers.Add("X-Redmine-API-Key", ApiKey);

            var response = await HttpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var createdIssue = JsonConvert.DeserializeObject<dynamic>(responseContent);
                Log.Information("Created Redmine issue #{IssueId} for event {EventId}", 
                    createdIssue?.issue?.id, evt.Id);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Log.Error("Failed to create Redmine issue. Status: {StatusCode}, Response: {Response}", 
                    response.StatusCode, errorContent);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception occurred while creating Redmine issue");
        }
    }

    private string BuildSubject(Event<LogEventData> evt)
    {
        var subject = SubjectTemplate
            .Replace("{Level}", evt.Data.Level.ToString())
            .Replace("{MessageTemplate}", evt.Data.MessageTemplate ?? "")
            .Replace("{Exception}", evt.Data.Exception?.GetType().Name ?? "");

        // Limit subject length to avoid Redmine limits
        return subject.Length > 255 ? subject.Substring(0, 252) + "..." : subject;
    }

    private string BuildDescription(Event<LogEventData> evt)
    {
        var eventData = evt.Data;
        var description = new StringBuilder();
        
        description.AppendLine($"**Seq Event ID:** {evt.Id}");
        description.AppendLine($"**Timestamp:** {evt.Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
        description.AppendLine($"**Level:** {eventData.Level}");
        description.AppendLine($"**Message Template:** {eventData.MessageTemplate}");
        
        if (!string.IsNullOrWhiteSpace(eventData.RenderedMessage))
        {
            description.AppendLine($"**Rendered Message:** {eventData.RenderedMessage}");
        }

        if (eventData.Exception != null)
        {
            description.AppendLine();
            description.AppendLine("**Exception:**");
            description.AppendLine("```");
            description.AppendLine(eventData.Exception.ToString());
            description.AppendLine("```");
        }

        if (eventData.Properties?.Count > 0)
        {
            description.AppendLine();
            description.AppendLine("**Properties:**");
            foreach (var prop in eventData.Properties)
            {
                description.AppendLine($"- **{prop.Key}:** {prop.Value}");
            }
        }

        return description.ToString();
    }
}