using Seq.Apps;
using Seq.Apps.LogEvents;
using System.Text;

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
            // Seq event link format: {base}/#/events?filter=@Id%20%3D%20"{id}" or direct event page depending on Seq version.
            // Use simple link to events filtered by id for compatibility.
            var link = $"{_seqBaseUrl}/#/events?filter=@Id%20%3D%20\"{_evt.Id}\"";
            description.AppendLine($"**Original Event:** {link}");
        }
        else
        {
            description.AppendLine($"**Seq Event ID:** {_evt.Id}");
            description.AppendLine($"**Timestamp:** {_evt.Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
        }

        // 2) Stack trace if exception
        if (eventData.Exception != null)
        {
            description.AppendLine();
            description.AppendLine("**Exception:**");
            description.AppendLine("```");
            description.AppendLine(eventData.Exception.ToString());
            description.AppendLine("```");
        }

        // 3) All properties
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
