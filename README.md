# Seq.App.Redmine

A Seq app that creates Redmine issues from Seq events.

## Features

- Automatically creates Redmine issues from Seq log events
- Configurable minimum log level filtering
- Customizable issue subject templates
- Supports custom fields for Seq event metadata
- Error handling and logging

## Installation

1. Build the project:
   ```bash
   dotnet build --configuration Release
   ```

2. Install the generated NuGet package (`bin/Release/Seq.App.Redmine.1.0.0.nupkg`) in your Seq instance

## Configuration

The app requires the following settings to be configured in Seq:

### Required Settings

- **Redmine URL**: The base URL of your Redmine instance (e.g., `https://redmine.example.com`)
- **API Key**: Your Redmine API key (found in your Redmine account settings)
- **Project ID**: The ID or identifier of the Redmine project where issues will be created

### Optional Settings

- **Tracker ID**: The tracker ID to use for created issues (default: 1 for Bug)
- **Priority ID**: The priority ID to use for created issues (default: 4 for Normal)
- **Minimum Level**: Only create issues for events at or above this level (default: Error)
- **Subject Template**: Template for the issue subject. Use `{Level}`, `{MessageTemplate}`, `{Exception}` placeholders (default: `[{Level}] {MessageTemplate}`)

## Usage

1. Install and configure the app in your Seq instance
2. Events that meet the minimum level criteria will automatically create Redmine issues
3. Each issue will include:
   - Seq event ID
   - Timestamp
   - Log level
   - Message template and rendered message
   - Exception details (if present)
   - Event properties

## Example Issue Content

**Subject**: `[Error] Database connection failed`

**Description**:
```
**Seq Event ID:** 123456
**Timestamp:** 2024-01-15 10:30:45 UTC
**Level:** Error
**Message Template:** Database connection failed
**Rendered Message:** Database connection failed for user 'admin'

**Exception:**
```
System.Data.SqlClient.SqlException: Cannot open database...
```

**Properties:**
- **UserId:** admin
- **Database:** MainDB
- **ConnectionString:** Server=localhost...
```

## Development

### Prerequisites

- .NET 8.0 SDK
- Access to a Redmine instance for testing

### Building

```bash
dotnet restore
dotnet build
```

### Packaging

```bash
dotnet pack --configuration Release
```

## License

Apache 2.0
