using Seq.Apps;
using Seq.Apps.LogEvents;
using System.Text;
using System;

namespace Seq.App.Redmine;

public class IssueContentBuilder
{
    private readonly Seq.Apps.Event<LogEventData> _evt;
    private readonly string? _seqBaseUrl;

    public IssueContentBuilder(Seq.Apps.Event<LogEventData> evt, string? seqBaseUrl = null)
    {
        _evt = evt;
        _seqBaseUrl = seqBaseUrl?.TrimEnd('/');
    }

    public string BuildDescription()
    {
        var eventData = _evt.Data;
        var description = new StringBuilder();

        // 1) Link to original event
        if (!string.IsNullOrWhiteSpace(_seqBaseUrl))
        {
            // Build a URL-encoded filter for the Seq events page to locate this event by id
            var filter = Uri.EscapeDataString($"@Id = \"{_evt.Id}\"");
            var link = $"{_seqBaseUrl}/#/events?filter={filter}";
            // Render as a markdown-style link - Redmine supports Markdown/Textile depending on configuration,
            // this is broadly compatible as plain link as well.
            description.AppendLine($"**Original Event:** [View in Seq]({link})");
            description.AppendLine();
        }
        else
        {
            description.AppendLine($"**Seq Event ID:** {_evt.Id}");
            description.AppendLine($"**Timestamp:** {_evt.Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
            description.AppendLine();
        }

        // 2) Stack trace if exception
        if (eventData.Exception != null)
        {
            description.AppendLine("**Exception:**");
            description.AppendLine("```");
            description.AppendLine(eventData.Exception.ToString());
            description.AppendLine("```");
            description.AppendLine();
        }

        // 3) All properties
        if (eventData.Properties?.Count > 0)
        {
            description.AppendLine("**Properties:**");
            foreach (var prop in eventData.Properties)
            {
                description.AppendLine($"- **{prop.Key}:** {prop.Value}");
            }
            description.AppendLine();
        }

        return description.ToString();
    }
}
