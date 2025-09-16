Redmine App - Extra Properties

This document describes the additional Redmine-related settings and how they are used by the app.

Settings added

- `StatusId` (optional): Numeric status ID to set on the created issue.
- `CategoryId` (optional): Numeric category ID to set on the created issue.
- `FixedVersionId` (optional): Numeric target/fixed version ID.
- `AssignedToId` (optional): Numeric user ID to assign the issue to.
- `ParentIssueId` (optional): Numeric parent issue ID.
- `WatcherUserIdsCsv` (optional): Comma-separated list of numeric user IDs to add as watchers. Example: `12,34,56`.
- `IsPrivate` (optional): `true` or `false` to mark the issue as private.
- `EstimatedHours` (optional): Decimal number of estimated hours.
- `CustomFields` (optional): Allows configuring Redmine custom fields. Two formats supported:
  - JSON: Provide a JSON array or object. Example: `[{"id":1,"value":"abc"},{"id":2,"value":123}]` or `{"id":1,"value":"abc"}`.
  - CSV style: `id:value` pairs separated by commas. Example: `1:abc,2:123,3:true`.

Behavior

- Optional fields are only included in the issue payload when set.
- `CustomFields` parsing will coerce numeric and boolean values when using CSV style.
- If `CustomFields` is empty or cannot be parsed, a default set is used:
  - custom field `1` -> Seq event id
  - custom field `2` -> event level

Examples

- Set watchers and assign the issue:
  - `WatcherUserIdsCsv`: `12,34`
  - `AssignedToId`: `78`

- Use JSON custom fields:
  - `CustomFields`: `[{"id":5,"value":"customer-123"},{"id":6,"value":45}]`

- Use CSV custom fields:
  - `CustomFields`: `5:customer-123,6:45`

Notes

- The app will only include fields in the payload that are configured; unset optional settings will be omitted from the request.
- Consider configuring custom field IDs to match your Redmine project's custom fields.
