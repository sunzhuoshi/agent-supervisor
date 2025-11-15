# CI Data Collection Feature

## Overview

The Agent Supervisor application includes a special menu item that is only available in CI builds. This feature allows collecting all review request data and notification history at once for testing and analysis purposes.

## How It Works

### Build-Time Configuration

This feature is controlled at **compile-time** using conditional compilation. The feature is enabled by defining the `ENABLE_CI_FEATURES` compilation symbol during the build process.

**CI Builds**: Include the `ENABLE_CI_FEATURES` symbol → Data collection menu item is available
**Release Builds**: Do not include the symbol → Data collection menu item is NOT available

This approach ensures that production/release builds never include the CI-specific features, providing a clean separation between testing and production functionality.

### Menu Item

When the application is built with CI features enabled, a "Collect Data" menu item appears in the system tray context menu:

```
┌─────────────────────────────────┐
│ Review Requests by Copilots     │
├─────────────────────────────────┤
│ Collect Data          ← CI only │  
├─────────────────────────────────┤
│ Settings                        │
│ About                           │
├─────────────────────────────────┤
│ Exit                            │
└─────────────────────────────────┘
```

### Data Collection

When the "Collect Data" menu item is clicked, the application:

1. Collects all review request data
2. Collects notification history (up to 1000 entries)
3. Gathers environment information
4. Calculates statistics
5. Saves everything to a JSON file with a timestamp

### Output File

The collected data is saved to a file named:
```
ci_data_collection_YYYYMMDD_HHmmss.json
```

Example: `ci_data_collection_20250115_131600.json`

### Output Format

The JSON file contains:

```json
{
  "CollectedAt": "2025-01-15T13:16:00.000Z",
  "Environment": {
    "MachineName": "github-runner-1",
    "OSVersion": "Microsoft Windows NT 10.0.17763.0",
    "RuntimeVersion": "8.0.0"
  },
  "ReviewRequests": [
    {
      "Id": "user/repo#123",
      "Repository": "user/repo",
      "PullRequestNumber": 123,
      "HtmlUrl": "https://github.com/user/repo/pull/123",
      "Title": "Fix issue #456",
      "Author": "contributor",
      "CreatedAt": "2025-01-15T12:00:00.000Z",
      "IsNew": true,
      "AddedAt": "2025-01-15T12:05:00.000Z"
    }
  ],
  "NotificationHistory": [
    {
      "Id": 987654321,
      "Repository": "user/repo",
      "PullRequestNumber": 123,
      "HtmlUrl": "https://github.com/user/repo/pull/123",
      "Reviewer": "reviewer-name",
      "State": "approved",
      "Body": "LGTM!",
      "Timestamp": "2025-01-15T12:10:00.000Z",
      "NotifiedAt": "2025-01-15T12:10:05.000Z"
    }
  ],
  "Statistics": {
    "TotalReviewRequests": 5,
    "NewReviewRequests": 3,
    "ReadReviewRequests": 2,
    "TotalNotifications": 10
  }
}
```

## Building with CI Features

### GitHub Actions (Automated)

The CI build workflow (`.github/workflows/build.yml`) automatically enables CI features by including the compilation symbol:

```yaml
- name: Build
  run: dotnet build --no-restore --configuration Release /p:DefineConstants="ENABLE_CI_FEATURES" ...
```

Builds from the CI workflow will include the "Collect Data" menu item.

### Local Testing

To build locally with CI features enabled:

**Command Line:**
```bash
dotnet build --configuration Release /p:DefineConstants="ENABLE_CI_FEATURES"
```

**Or to run directly:**
```bash
dotnet run --configuration Release /p:DefineConstants="ENABLE_CI_FEATURES"
```

### Release Builds

Release builds (`.github/workflows/release.yml`) do **not** include the `ENABLE_CI_FEATURES` symbol, ensuring that production releases never have the CI-specific menu item.

To build a release version locally without CI features:
```bash
dotnet build --configuration Release
```

## Use Cases

This feature is useful for:

1. **Automated Testing**: Collect data during CI runs to verify the application is working correctly
2. **Data Analysis**: Analyze review patterns and notification behavior
3. **Debugging**: Capture application state during CI builds for troubleshooting
4. **Metrics**: Gather statistics about review requests and notifications over time

## Privacy & Security

- The collected data includes only information already visible in the application
- No sensitive credentials or tokens are included in the output
- The JSON files are excluded from version control via `.gitignore`
- Data is stored locally and not transmitted anywhere
- The feature is completely absent from release builds (compile-time removal)

## Limitations

- The menu item is **only** visible in builds compiled with `ENABLE_CI_FEATURES` defined
- For regular release builds, this feature is not available (by design)
- The collected data reflects the state at the time of collection
