# CI Data Collection Feature

## Overview

The Agent Supervisor application includes a special menu item that is only available when running in CI (Continuous Integration) environments. This feature allows collecting all review request data and notification history at once for testing and analysis purposes.

## How It Works

### CI Detection

The application automatically detects CI environments by checking for common CI environment variables:

- `CI` - Generic CI indicator
- `GITHUB_ACTIONS` - GitHub Actions
- `JENKINS_HOME` - Jenkins
- `TRAVIS` - Travis CI
- `CIRCLECI` - Circle CI
- `GITLAB_CI` - GitLab CI
- `BUILDKITE` - Buildkite
- `TEAMCITY_VERSION` - TeamCity
- `TF_BUILD` - Azure Pipelines

If any of these environment variables are set, the application recognizes it's running in a CI environment.

### Menu Item

When running in CI, a "Collect Data" menu item appears in the system tray context menu:

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

## Testing in CI

### GitHub Actions

To test this feature in GitHub Actions, you can add a workflow step:

```yaml
- name: Test CI Data Collection
  run: |
    # Set CI environment variable (usually already set in GitHub Actions)
    export CI=true
    
    # Run the application (this is just an example, actual usage depends on your setup)
    # The "Collect Data" menu item will be available in the system tray
```

### Local Testing

To test the CI detection locally, set the CI environment variable:

**Windows (PowerShell):**
```powershell
$env:CI = "true"
.\AgentSupervisor.exe
```

**Windows (Command Prompt):**
```cmd
set CI=true
AgentSupervisor.exe
```

**Linux/macOS:**
```bash
export CI=true
./AgentSupervisor
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

## Limitations

- The menu item is **only** visible when running in a CI environment
- For regular users, this feature is not available (by design)
- The collected data reflects the state at the time of collection
