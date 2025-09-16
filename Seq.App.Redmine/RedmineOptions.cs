namespace Seq.App.Redmine;

public class RedmineOptions
{
    public string? ProjectId { get; set; }
    public int TrackerId { get; set; }
    public int PriorityId { get; set; }
    public int? StatusId { get; set; }
    public int? CategoryId { get; set; }
    public int? FixedVersionId { get; set; }
    public int? AssignedToId { get; set; }
    public int? ParentIssueId { get; set; }
    public string? WatcherUserIdsCsv { get; set; }
    public bool? IsPrivate { get; set; }
    public decimal? EstimatedHours { get; set; }
    public string? CustomFields { get; set; }
    public string? SubjectTemplate { get; set; }
    // Base URL of the Seq instance to build a link to the original event
    public string? SeqInstanceUrl { get; set; }
    // Optional description template (if provided can be used in future)
    public string? DescriptionTemplate { get; set; }
}
