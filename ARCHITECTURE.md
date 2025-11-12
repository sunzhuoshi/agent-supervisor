# Architecture Documentation

## Overview

GitHubCopilotAgentBot is a Windows console application written in C# that monitors GitHub pull request reviews and provides desktop notifications without interfering with Windows system notifications.

## System Architecture

### Component Diagram

```
┌──────────────────────────────────────────────────────────────┐
│                         Program.cs                            │
│                    (Main Entry Point)                        │
│  - Initializes services                                      │
│  - Manages application lifecycle                             │
│  - Handles user input (H, O, R, Q commands)                 │
└─────────────┬────────────────────────────────────────────────┘
              │
              ├──────────────────────────────────────────────┐
              │                                              │
    ┌─────────▼─────────┐                        ┌──────────▼─────────┐
    │  Configuration.cs │                        │  GitHubService.cs  │
    │                   │                        │                    │
    │ - Load/Save JSON  │                        │ - GitHub API calls │
    │ - PAT management  │                        │ - Review polling   │
    │ - Settings        │                        │ - Authentication   │
    └───────────────────┘                        └──────────┬─────────┘
                                                            │
                                                            │
                                         ┌──────────────────▼──────────────────┐
                                         │    NotificationService.cs           │
                                         │                                     │
                                         │ - Display notifications in console  │
                                         │ - Browser integration               │
                                         │ - History display                   │
                                         └──────────┬──────────────────────────┘
                                                    │
                                         ┌──────────▼──────────┐
                                         │ NotificationHistory │
                                         │                     │
                                         │ - Persistent storage│
                                         │ - Deduplication     │
                                         │ - JSON file I/O     │
                                         └─────────────────────┘
```

### Data Models

```
┌─────────────────────┐         ┌─────────────────────┐
│ PullRequestReview   │         │  NotificationEntry  │
├─────────────────────┤         ├─────────────────────┤
│ - Id                │────────▶│ - Id                │
│ - HtmlUrl           │         │ - Repository        │
│ - State             │         │ - PullRequestNumber │
│ - Body              │         │ - HtmlUrl           │
│ - User              │         │ - Reviewer          │
│ - SubmittedAt       │         │ - State             │
│ - RepositoryName    │         │ - Body              │
│ - PullRequestNumber │         │ - Timestamp         │
└─────────────────────┘         │ - NotifiedAt        │
                                └─────────────────────┘
```

## Key Components

### 1. Program.cs
**Responsibility**: Application entry point and orchestration
- Initializes all services
- Manages the monitoring loop
- Handles user input for interactive commands
- Coordinates between services

### 2. GitHubService.cs
**Responsibility**: GitHub API integration
- Authenticates using Personal Access Token
- Polls GitHub for PRs where user is requested as reviewer
- Fetches review data from GitHub REST API
- Tracks seen reviews to avoid duplicate notifications

**Key Methods**:
- `GetCurrentUserAsync()`: Retrieves authenticated user information
- `GetPendingReviewsAsync()`: Fetches all pending reviews for the user

### 3. NotificationService.cs
**Responsibility**: Notification display and management
- Displays formatted console notifications with colors
- Plays audio beeps (Windows-specific)
- Opens PRs in browser
- Shows notification history

**Key Methods**:
- `ShowNotification()`: Displays a new review notification
- `DisplayConsoleNotification()`: Formats and prints notification
- `OpenInBrowser()`: Opens URL in default browser
- `DisplayHistory()`: Shows recent notifications

### 4. NotificationHistory.cs
**Responsibility**: Persistent notification storage
- Maintains JSON file of all notifications
- Prevents duplicate notifications
- Thread-safe operations
- Automatic size management (keeps last N entries)

**Key Methods**:
- `HasBeenNotified()`: Checks if a review was already notified
- `Add()`: Adds new notification and persists to disk
- `GetRecent()`: Retrieves N most recent notifications

### 5. Configuration.cs
**Responsibility**: Application configuration
- Loads/saves configuration from/to JSON
- Interactive first-time setup
- Default values management

**Configuration Fields**:
- `PersonalAccessToken`: GitHub PAT for API access
- `PollingIntervalSeconds`: How often to check for reviews (default: 60)
- `MaxHistoryEntries`: Maximum notifications to keep (default: 100)

## Data Flow

1. **Initialization**:
   ```
   Program → Load Configuration → Initialize Services → Connect to GitHub
   ```

2. **Monitoring Loop** (repeats every N seconds):
   ```
   GitHubService → Fetch PRs → Fetch Reviews → Filter New Reviews →
   NotificationService → Check History → Display Notification → Save to History
   ```

3. **User Interaction**:
   ```
   User Input → Program → Execute Command:
     H → Display History
     O → Open Last PR
     R → Force Refresh
     Q → Exit
   ```

## File System

### Generated Files
- `config.json`: User configuration (PAT, settings) - **Sensitive, excluded from git**
- `notification_history.json`: Notification history - **Excluded from git**

### Source Files
- `src/`: Source code directory
  - `Program.cs`: Main entry point
  - `GitHubService.cs`: GitHub API integration
  - `NotificationService.cs`: Notification display
  - `NotificationHistory.cs`: History management
  - `Configuration.cs`: Config management
  - `Models/`: Data models
    - `PullRequestReview.cs`: PR review model
    - `NotificationEntry.cs`: Notification model

## Security Considerations

1. **Personal Access Token**: 
   - Stored in `config.json` 
   - File excluded from git via `.gitignore`
   - Never logged or displayed

2. **Dependencies**:
   - System.Text.Json 8.0.5 (secure version)
   - Octokit 11.0.1 (GitHub API client)
   - No known vulnerabilities

3. **GitHub API**:
   - Uses HTTPS for all communications
   - Bearer token authentication
   - Respects GitHub API rate limits

## Threading Model

- **Main Thread**: Handles UI and user input
- **Background Task**: Runs monitoring loop asynchronously
- **Cancellation**: Uses `CancellationTokenSource` for graceful shutdown
- **Thread Safety**: NotificationHistory uses locking for concurrent access

## Build System

### .NET CLI Build
- Standard .NET 6.0 Windows Forms project
- Uses `GitHubCopilotAgentBot.csproj`
- Targets `net6.0-windows` framework
- Builds to `bin/Debug/net6.0-windows/` or `bin/Release/net6.0-windows/`

## Error Handling

- Network errors: Logged to console, monitoring continues
- API errors: Logged with status codes, retried on next poll
- File I/O errors: Logged but don't crash the application
- Graceful degradation: App continues running even if some operations fail

## Performance Characteristics

- **Memory**: Low footprint (< 50 MB typical)
- **Network**: Minimal API calls (1 per polling interval)
- **Disk I/O**: Only on configuration load/save and notification additions
- **CPU**: Idle between polls, minimal processing during checks

## Extension Points

The architecture supports easy extension:

1. **Custom Notification Providers**: Implement alternative to console notifications
2. **Additional GitHub Events**: Extend GitHubService to monitor other events
3. **Filtering Rules**: Add configurable filters for which reviews to notify
4. **Multiple Accounts**: Support multiple GitHub accounts
5. **Webhook Support**: Replace polling with webhook-based notifications

## Testing Strategy

While no unit tests are currently included, the architecture supports testing:

- Services are loosely coupled
- Dependencies can be mocked
- Configuration can be injected
- File I/O can be abstracted

## Future Enhancements

Potential improvements:
- GUI application instead of console
- System tray integration
- Native Windows toast notifications (with user opt-in)
- Webhook endpoint for real-time notifications
- Support for GitHub Apps authentication
- Multi-repository filtering
- Custom notification templates
