using System.Text.Json;
using System.Text.Json.Nodes;
using Seq.Apps;
using Seq.Apps.LogEvents;
using System.Linq;

namespace Seq.App.Redmine;

public static class RedmineIssueBuilder
{
    public static JsonObject BuildIssueJObject(RedmineOptions options, Seq.Apps.Event<LogEventData> evt)
    {
        var issueObj = new JsonObject();

        // project_id can be numeric or identifier
        if (!string.IsNullOrWhiteSpace(options.ProjectId) && int.TryParse(options.ProjectId, out var projId))
            issueObj["project_id"] = projId;
        else
            issueObj["project_id"] = options.ProjectId;

        issueObj["tracker_id"] = options.TrackerId;
        issueObj["priority_id"] = options.PriorityId;

        // Use rendered message when available, fall back to message template
        var renderedMessage = evt.Data.RenderedMessage ?? evt.Data.MessageTemplate ?? string.Empty;

        var subject = (options.SubjectTemplate ?? "[{Level}] {MessageTemplate}")
            .Replace("{Level}", evt.Data.Level.ToString())
            .Replace("{MessageTemplate}", renderedMessage)
            .Replace("{RenderedMessage}", renderedMessage)
            .Replace("{Exception}", evt.Data.Exception?.GetType().Name ?? "");

        issueObj["subject"] = subject.Length > 255 ? subject.Substring(0, 252) + "..." : subject;

        // Build description using IssueContentBuilder which includes event link, stack trace and properties
        var descriptionBuilder = new IssueContentBuilder(evt, options.SeqInstanceUrl);
        issueObj["description"] = descriptionBuilder.BuildDescription();

        if (options.StatusId.HasValue)
            issueObj["status_id"] = options.StatusId.Value;

        if (options.CategoryId.HasValue)
            issueObj["category_id"] = options.CategoryId.Value;

        if (options.FixedVersionId.HasValue)
            issueObj["fixed_version_id"] = options.FixedVersionId.Value;

        if (options.AssignedToId.HasValue)
            issueObj["assigned_to_id"] = options.AssignedToId.Value;

        if (options.ParentIssueId.HasValue)
            issueObj["parent_issue_id"] = options.ParentIssueId.Value;

        if (options.IsPrivate.HasValue)
            issueObj["is_private"] = options.IsPrivate.Value;

        if (options.EstimatedHours.HasValue)
            issueObj["estimated_hours"] = options.EstimatedHours.Value;

        // Parse watcher IDs CSV into array if provided
        if (!string.IsNullOrWhiteSpace(options.WatcherUserIdsCsv))
        {
            var ids = options.WatcherUserIdsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.Trim())
                .Where(s => int.TryParse(s, out _))
                .Select(int.Parse)
                .ToArray();

            if (ids.Length > 0)
            {
                var arr = new JsonArray();
                foreach (var id in ids)
                    arr.Add(id);
                issueObj["watcher_user_ids"] = arr;
            }
        }

        // custom_fields
        JsonArray customFieldsArray;
        if (!string.IsNullOrWhiteSpace(options.CustomFields))
        {
            var s = options.CustomFields.Trim();
            try
            {
                if (s.StartsWith("[") || s.StartsWith("{"))
                {
                    var parsed = JsonNode.Parse(s);
                    if (parsed is JsonArray arr)
                        customFieldsArray = arr;
                    else if (parsed is JsonObject obj)
                        customFieldsArray = new JsonArray(obj);
                    else
                        customFieldsArray = new JsonArray();
                }
                else
                {
                    customFieldsArray = new JsonArray();
                    var parts = s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var part in parts)
                    {
                        var kv = part.Split(new[] { ':', '=' }, 2);
                        if (kv.Length == 2 && int.TryParse(kv[0].Trim(), out var id))
                        {
                            var val = kv[1].Trim();
                            JsonNode? valNode;
                            if (int.TryParse(val, out var ival))
                                valNode = JsonValue.Create(ival);
                            else if (decimal.TryParse(val, out var dval))
                                valNode = JsonValue.Create(dval);
                            else if (bool.TryParse(val, out var bval))
                                valNode = JsonValue.Create(bval);
                            else
                                valNode = JsonValue.Create(val);

                            var obj = new JsonObject
                            {
                                ["id"] = id,
                                ["value"] = valNode
                            };
                            customFieldsArray.Add(obj);
                        }
                    }
                }
            }
            catch
            {
                customFieldsArray = new JsonArray();
            }

            if (customFieldsArray.Count == 0)
            {
                customFieldsArray = new JsonArray
                {
                    new JsonObject { ["id"] = 1, ["value"] = evt.Id },
                    new JsonObject { ["id"] = 2, ["value"] = evt.Data.Level.ToString() }
                };
            }
        }
        else
        {
            customFieldsArray = new JsonArray
            {
                new JsonObject { ["id"] = 1, ["value"] = evt.Id },
                new JsonObject { ["id"] = 2, ["value"] = evt.Data.Level.ToString() }
            };
        }

        issueObj["custom_fields"] = customFieldsArray;

        return issueObj;
    }
}
