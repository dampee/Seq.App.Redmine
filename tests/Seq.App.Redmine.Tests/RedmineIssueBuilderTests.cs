using System.Text.Json;
using System.Text.Json.Nodes;
using Seq.App.Redmine;
using Seq.Apps.LogEvents;
using Xunit;

namespace Seq.App.Redmine.Tests;

public class RedmineIssueBuilderTests
{
    [Fact]
    public void BuildIssue_IncludesSubjectAndCustomFields()
    {
        var options = new RedmineOptions
        {
            ProjectId = "42",
            TrackerId = 1,
            PriorityId = 4,
            CustomFields = "1:abc,2:123",
            SubjectTemplate = "[{Level}] {MessageTemplate}",
            SeqInstanceUrl = "https://seq.local"
        };

        var evt = new Seq.Apps.Event<LogEventData>
        {
            Id = "ABC123",
            Timestamp = DateTime.UtcNow,
            Data = new LogEventData
            {
                Level = LogEventLevel.Error,
                MessageTemplate = "Test message",
                RenderedMessage = "Test message rendered"
            }
        };

        var obj = RedmineIssueBuilder.BuildIssueJObject(options, evt);

        Assert.Equal("[Error] Test message", obj["subject"]?.GetValue<string>());
        var custom = obj["custom_fields"] as JsonArray;
        Assert.NotNull(custom);
        Assert.Equal(2, custom.Count);
    }

    [Fact]
    public void BuildIssue_IncludesWatcherIds()
    {
        var options = new RedmineOptions
        {
            ProjectId = "42",
            TrackerId = 1,
            PriorityId = 4,
            WatcherUserIdsCsv = "10,20"
        };

        var evt = new Seq.Apps.Event<LogEventData>
        {
            Id = "ID1",
            Timestamp = DateTime.UtcNow,
            Data = new LogEventData
            {
                Level = LogEventLevel.Warning,
                MessageTemplate = "Warn"
            }
        };

        var obj = RedmineIssueBuilder.BuildIssueJObject(options, evt);
        var watchers = obj["watcher_user_ids"] as JsonArray;
        Assert.NotNull(watchers);
        Assert.Equal(2, watchers.Count);
        Assert.Equal(10, watchers[0]?.GetValue<int>());
    }
}
