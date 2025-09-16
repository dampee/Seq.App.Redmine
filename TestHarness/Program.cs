using Microsoft.Extensions.Configuration;
using Seq.App.Redmine;
using Seq.Apps.LogEvents;
using Serilog;

var builder = new ConfigurationBuilder()
    .AddUserSecrets(typeof(Program).Assembly);
var configuration = builder.Build();

var apiKey = configuration["REDMINE_API_KEY"];
var redmineUrl = configuration["REDMINE_URL"];

using var logger = new LoggerConfiguration()
    .WriteTo.Console()
    .AuditTo.SeqApp<RedmineApp>(new Dictionary<string, string>
    {
        [nameof(Seq.App.Redmine.RedmineApp.ApiKey)] = apiKey,
        [nameof(RedmineApp.RedmineUrl)] = redmineUrl,
        [nameof(RedmineApp.ProjectId)] = "74", // Cardoen
        [nameof(RedmineApp.PriorityId)] = "4", // 2 (Normal)
        [nameof(RedmineApp.TrackerId)] = "14",
        [nameof(RedmineApp.StatusId)] = "1", // New
        //[nameof(RedmineApp.CategoryId)] = "Story",
        //[nameof(RedmineApp.ParentIssueId)] = "123456",
        [nameof(RedmineApp.MinimumLevel)] = LogEventLevel.Information.ToString(),

    })
    .CreateLogger();


logger.Information("Hello, {Name}!", Environment.UserName);