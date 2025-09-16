using Newtonsoft.Json.Linq;
using Seq.Apps;
using Seq.Apps.LogEvents;
using System.Linq;

namespace Seq.App.Redmine;

public static class RedmineIssueBuilder
{
    public static JObject BuildIssueJObject(RedmineOptions options, Seq.Apps.Event<LogEventData> evt)
    {
        var issueObj = new JObject();

        // project_id can be numeric or identifier
        if (!string.IsNullOrWhiteSpace(options.ProjectId) && int.TryParse(options.ProjectId, out var projId))
            issueObj["project_id"] = projId;
        else
            issueObj["project_id"] = options.ProjectId;

        issueObj["tracker_id"] = options.TrackerId;
        issueObj["priority_id"] = options.PriorityId;

        var subject = (options.SubjectTemplate ?? "[{Level}] {MessageTemplate}")
            .Replace("{Level}", evt.Data.Level.ToString())
            .Replace("{MessageTemplate}", evt.Data.MessageTemplate ?? "")
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
                var arr = new JArray();
                foreach (var id in ids)
                    arr.Add(id);
                issueObj["watcher_user_ids"] = arr;
            }
        }

        // custom_fields
        JArray customFieldsArray;
        if (!string.IsNullOrWhiteSpace(options.CustomFields))
        {
            var s = options.CustomFields.Trim();
            try
            {
                if (s.StartsWith("[") || s.StartsWith("{"))
                {
                    var parsed = JToken.Parse(s);
                    if (parsed is JArray arr)
                        customFieldsArray = arr;
                    else if (parsed is JObject obj)
                        customFieldsArray = new JArray(obj);
                    else
                        customFieldsArray = new JArray();
                }
                else
                {
                    customFieldsArray = new JArray();
                    var parts = s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var part in parts)
                    {
                        var kv = part.Split(new[] { ':', '=' }, 2);
                        if (kv.Length == 2 && int.TryParse(kv[0].Trim(), out var id))
                        {
                            var val = kv[1].Trim();
                            JToken valToken;
                            if (int.TryParse(val, out var ival))
                                valToken = ival;
                            else if (decimal.TryParse(val, out var dval))
                                valToken = dval;
                            else if (bool.TryParse(val, out var bval))
                                valToken = bval;
                            else
                                valToken = val;

                            customFieldsArray.Add(new JObject { ["id"] = id, ["value"] = valToken });
                        }
                    }
                }
            }
            catch
            {
                customFieldsArray = new JArray();
            }

            if (customFieldsArray.Count == 0)
            {
                customFieldsArray = new JArray
                {
                    new JObject { ["id"] = 1, ["value"] = evt.Id },
                    new JObject { ["id"] = 2, ["value"] = evt.Data.Level.ToString() }
                };
            }
        }
        else
        {
            customFieldsArray = new JArray
            {
                new JObject { ["id"] = 1, ["value"] = evt.Id },
                new JObject { ["id"] = 2, ["value"] = evt.Data.Level.ToString() }
            };
        }

        issueObj["custom_fields"] = customFieldsArray;

        return issueObj;
    }
}
