# Seq.App.Redmine

Redmine Issue Creator for Seq

This package contains a Seq app that creates Redmine issues from Seq events.

## Usage

Configure the app with the following settings (available in the app settings when adding the app in Seq):

- `RedmineUrl` (required): Base URL of your Redmine instance, e.g. `https://redmine.example.com`
- `ApiKey` (required): Your Redmine API key
- `ProjectId` (required): Redmine project id or identifier
- `TrackerId` (optional): Tracker id to use (default: 1)
- `PriorityId` (optional): Priority id to use (default: 4)
- `SeqInstanceUrl` (optional): Base URL of your Seq server; used to construct a link back to the original event from the created Redmine issue.
- `CustomFields` (optional): Configure custom fields using JSON array or CSV style (`1:abc,2:123`). If omitted, the app sets defaults:
  - custom field `1` -> Seq event id
  - custom field `2` -> event level

## Description content

When no `DescriptionTemplate` is provided, the app will include in the Redmine issue description:

1. A link to the original Seq event (if `SeqInstanceUrl` is set), otherwise the event ID and timestamp.
2. The full exception stack trace (if an exception is present on the event).
3. All event properties as a list of key/value pairs.

## Packaging

This README is included in the NuGet package and will be displayed on nuget.org when the package is published.

## License

Apache-2.0
