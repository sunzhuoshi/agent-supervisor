# Architecture Documentation

## Overview

Agent Supervisor is a Windows system tray application written in C# using Windows Forms that monitors GitHub pull request reviews and provides desktop notifications. The application runs in the background with a taskbar presence and system tray icon.

## System Architecture

### Component Diagram

```
┌──────────────────────────────────────────────────────────────┐
│                         Program.cs                            │
│                  (AppApplicationContext)                     │
│  - Initializes services                                      │
│  - Manages application lifecycle                             │
│  - Coordinates monitoring loop                               │
└─────────────┬────────────────────────────────────────────────┘
              │
              ├──────────────────────────────────────────────┐
              │                                              │
              │                                              │
    ┌─────────▼─────────┐                        ┌──────────▼─────────┐
    │  Configuration.cs │                        │  GitHubService.cs  │
    │                   │                        │                    │
    │ - Registry storage│                        │ - GitHub API calls │
    │ - PAT management  │                        │ - Review polling   │
    │ - Settings        │                        │ - Authentication   │
    └───────────────────┘                        └──────────┬─────────┘
                                                            │
              ┌──────────────────────────────────────────┬──┴──────┐
              │                                          │         │
   ┌──────────▼──────────┐              ┌───────────────▼────┐   │
   │  MainWindow.cs      │              │ SystemTrayManager  │   │
   │                     │              │                    │   │
   │ - Taskbar presence  │              │ - Tray icon        │   │
   │ - Review list UI    │              │ - Balloon tips     │   │
   │ - Badge overlay     │              │ - Context menu     │   │
   └─────────┬───────────┘              └─────────┬──────────┘   │
             │                                    │               │
   ┌─────────▼───────────┐              ┌────────▼──────────┐    │
   │ TaskbarBadgeManager │              │ NotificationHistory│    │
   │                     │              │                    │    │
   │ - Badge overlay     │              │ - Persistent store │    │
   │ - Count display     │              │ - Deduplication    │    │
   └─────────────────────┘              │ - JSON file I/O    │    │
                                        └────────────────────┘    │
                                                                  │
                                        ┌─────────────────────────▼──┐
                                        │ ReviewRequestService       │
                                        │                            │
                                        │ - Track review requests    │
                                        │ - New/read status          │
                                        │ - Persistent storage       │
                                        └────────────────────────────┘
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

┌─────────────────────┐
│ ReviewRequestEntry  │
├─────────────────────┤
│ - Id                │
│ - Repository        │
│ - PullRequestNumber │
│ - HtmlUrl           │
│ - Title             │
│ - Author            │
│ - CreatedAt         │
│ - IsNew (bool)      │
└─────────────────────┘
```

## Key Components

## Design Patterns

### Observer Pattern (Model/View Separation)
The application uses the Observer pattern to maintain separation between the data model and views:

- **Model**: `ReviewRequestService` maintains the state of review requests
- **Observer Interface**: `IReviewRequestObserver` defines the contract for observers
- **Views**: `MainWindow` and `TaskbarBadgeManager` implement `IReviewRequestObserver`
- **Notifications**: When the model changes (add, update, mark as read), all observers are automatically notified
- **Benefits**: 
  - Loose coupling between model and views
  - Automatic UI updates without manual coordination
  - Easy to add new views without modifying existing code

### 1. Program.cs & AppApplicationContext
**Responsibility**: Application entry point and lifecycle management
- Single instance enforcement via mutex
- Initializes all services in proper order
- Manages the background monitoring loop
- Coordinates between all components
- Handles application shutdown

### 2. MainWindow.cs
**Responsibility**: Windows Forms window with taskbar presence (View/Observer)
- Hidden window that provides taskbar presence
- Displays list of review requests
- Shows new/read status for each request
- Supports double-click to open PRs in browser
- Implements `IReviewRequestObserver` to automatically update when model changes
- Integrates with TaskbarBadgeManager for badge overlay

### 3. TaskbarBadgeManager.cs
**Responsibility**: Taskbar badge overlay management (View/Observer)
- Creates and updates taskbar badge overlay
- Displays count of unread review requests
- Uses Windows API for badge rendering
- Implements `IReviewRequestObserver` to automatically update badge when model changes

### 4. SystemTrayManager.cs
**Responsibility**: System tray icon and notifications
- Creates system tray icon
- Displays balloon tip notifications
- Provides context menu (Settings, About, Exit)
- Opens review request list on double-click
- Updates tray icon tooltip with status

### 5. GitHubService.cs
**Responsibility**: GitHub API integration
- Authenticates using Personal Access Token
- Polls GitHub for PRs where user is requested as reviewer
- Fetches PR details via GitHub REST API
- Updates ReviewRequestService with current PRs
- Tracks new review requests for notifications

**Key Methods**:
- `GetCurrentUserAsync()`: Retrieves authenticated user information
- `GetPendingReviewsAsync()`: Fetches all pending review requests

### 6. ReviewRequestService.cs
**Responsibility**: Review request tracking and persistence (Model)
- Maintains list of review requests with new/read status
- Persists state to JSON file
- Provides counts of total and new requests
- Implements Observer pattern to notify views of changes
- Removes stale review requests

**Key Methods**:
- `Subscribe()`: Registers an observer to receive change notifications
- `Unsubscribe()`: Removes an observer from receiving notifications
- `AddOrUpdate()`: Updates or adds review request and notifies observers
- `MarkAsRead()`: Marks a request as read and notifies observers
- `MarkAllAsRead()`: Marks all requests as read and notifies observers
- `GetNewCount()`: Returns count of unread requests
- `GetTotalCount()`: Returns total request count
- `RemoveStaleRequests()`: Removes closed/completed PRs and notifies observers

### 7. NotificationHistory.cs
**Responsibility**: Persistent notification storage
- Maintains JSON file of all notifications shown
- Prevents duplicate notifications
- Thread-safe operations with locking
- Automatic size management (keeps last N entries)

**Key Methods**:
- `HasBeenNotified()`: Checks if a review was already notified
- `Add()`: Adds new notification and persists to disk
- `GetRecent()`: Retrieves N most recent notifications

### 8. ReviewRequestHistory.cs
**Responsibility**: Simple ID tracking for seen review requests
- Tracks which review requests have been seen before
- Helps identify new review requests
- Persists to review_requests.json

### 9. Configuration.cs
**Responsibility**: Application configuration via Windows Registry
- Loads/saves configuration from/to Windows Registry
- Interactive settings UI integration
- Default values management

**Configuration Fields**:
- `PersonalAccessToken`: GitHub PAT for API access
- `PollingIntervalSeconds`: Check interval (default: 60)
- `MaxHistoryEntries`: Max notifications to keep (default: 100)
- `EnableDesktopNotifications`: Enable/disable balloon tips
- `ProxyUrl` / `UseProxy`: Proxy configuration

### 10. SettingsForm.cs
**Responsibility**: Settings UI dialog
- Provides GUI for configuration
- Validates settings before saving
- Saves to Windows Registry via Configuration

### 11. AboutForm.cs
**Responsibility**: About dialog
- Displays application information
- Shows version number
- Application description and credits

### 12. Logger.cs
**Responsibility**: Application logging
- Writes to log file (AgentSupervisor.log)
- Automatic log rotation when size exceeds limit
- Keeps last 5 backup files
- Thread-safe logging operations

## Data Flow

1. **Initialization**:
   ```
   Program → Check Single Instance → Load Configuration from Registry → 
   Show Settings if No Token → Initialize Services → Connect to GitHub →
   Create MainWindow & SystemTrayManager → Start Background Monitoring
   ```

2. **Monitoring Loop** (repeats every N seconds):
   ```
   GitHubService → Fetch PRs via GitHub API → Update ReviewRequestService →
   Check NotificationHistory → Show Balloon Tip for New Reviews →
   Save to NotificationHistory → Update Taskbar Badge Count
   ```

3. **User Interactions**:
   ```
   Double-click Tray Icon → Show MainWindow with Review List
   Double-click Review → Mark as Read → Open in Browser
   Click Balloon Tip → Open PR in Browser
   Right-click Tray Icon → Context Menu:
     - Review Requests by Copilots → Show MainWindow
     - Settings → Show SettingsForm
     - About → Show AboutForm
     - Exit → Shutdown Application
   ```

## File System

### Generated Files
- `notification_history.json`: Notification history - **Excluded from git**
- `review_request_details.json`: Review requests with new/read status - **Excluded from git**
- `review_requests.json`: Simple ID tracking of seen requests - **Excluded from git**
- `AgentSupervisor.log`: Application log file with rotation - **Excluded from git**

### Configuration
- Configuration stored in Windows Registry under `HKEY_CURRENT_USER\Software\AgentSupervisor`

### Source Files
- `src/`: Source code directory
  - `Program.cs`: Main entry point and AppApplicationContext
  - `MainWindow.cs`: Hidden window with review list UI (implements IReviewRequestObserver)
  - `TaskbarBadgeManager.cs`: Badge overlay management (implements IReviewRequestObserver)
  - `SystemTrayManager.cs`: System tray icon and notifications
  - `GitHubService.cs`: GitHub API integration
  - `ReviewRequestService.cs`: Review request tracking (observable model)
  - `ReviewRequestHistory.cs`: Simple ID tracking
  - `NotificationHistory.cs`: Notification history management
  - `Configuration.cs`: Registry-based configuration
  - `SettingsForm.cs`: Settings dialog UI
  - `AboutForm.cs`: About dialog UI
  - `Logger.cs`: File-based logging
  - `IReviewRequestObserver.cs`: Observer pattern interface
  - `Models/`: Data models
    - `PullRequestReview.cs`: PR review model
    - `NotificationEntry.cs`: Notification model
    - `ReviewRequestEntry.cs`: Review request with new/read status

## Security Considerations

1. **Personal Access Token**: 
   - Stored in Windows Registry under `HKEY_CURRENT_USER\Software\AgentSupervisor`
   - Protected by Windows user account security
   - Never logged or displayed in UI

2. **Dependencies**:
   - System.Text.Json 8.0.5 (secure version)
   - Octokit 11.0.1 (GitHub API client)
   - Regular security scanning via GitHub Actions

3. **GitHub API**:
   - Uses HTTPS for all communications
   - Bearer token authentication
   - Respects GitHub API rate limits

4. **Windows Integration**:
   - Uses Windows Forms for UI
   - Integrates with Windows notification system
   - Uses Windows Registry for settings storage

## Threading Model

- **Main Thread (UI Thread)**: Handles UI interactions, forms, system tray
- **Background Task**: Runs monitoring loop asynchronously
- **Cancellation**: Uses `CancellationTokenSource` for graceful shutdown
- **Thread Safety**: 
  - NotificationHistory uses locking for concurrent access
  - ReviewRequestService uses locking for state management
  - UI updates use Invoke/BeginInvoke for cross-thread safety

## Build System

### .NET CLI Build
- .NET 8.0 Windows Forms project
- Uses `AgentSupervisor.csproj`
- Targets `net8.0-windows` framework
- Builds to `bin/Debug/net8.0-windows/` or `bin/Release/net8.0-windows/`
- Includes custom application icon (res/app_icon.ico)

### CI/CD
- GitHub Actions workflow for automated builds
- Release workflow for creating versioned releases
- CodeQL security scanning

## Error Handling

- Network errors: Logged to file, monitoring continues
- API errors: Logged with status codes, retried on next poll
- File I/O errors: Logged but don't crash the application
- Registry errors: Graceful fallback to defaults
- UI errors: Logged and shown to user via MessageBox when critical
- Graceful degradation: App continues running even if some operations fail

## Performance Characteristics

- **Memory**: Low footprint (< 100 MB typical)
- **Network**: Minimal API calls (1 search + N PR details per polling interval)
- **Disk I/O**: Only on configuration changes and notification additions
- **CPU**: Idle between polls, minimal processing during checks
- **UI**: Responsive with background async operations

## Extension Points

The architecture supports easy extension:

1. **Custom Notification Providers**: Extend SystemTrayManager or replace with alternative notification systems
2. **Additional GitHub Events**: Extend GitHubService to monitor other events (issues, mentions, etc.)
3. **Filtering Rules**: Add configurable filters for which reviews to notify
4. **Multiple Accounts**: Support multiple GitHub accounts/tokens
5. **Webhook Support**: Replace polling with webhook-based notifications
6. **Additional UI Forms**: Add more dialogs for advanced features
7. **Plugins/Extensions**: Design plugin system for community extensions

## Testing Strategy

While no unit tests are currently included, the architecture supports testing:

- Services are loosely coupled via interfaces (potential)
- Dependencies can be injected and mocked
- Configuration can be injected
- File I/O and Registry access can be abstracted
- UI components can be tested with Windows Forms testing frameworks

## Future Enhancements

Potential improvements:
- Native Windows 10/11 toast notifications (requires Windows Runtime APIs)
- Rich notifications with action buttons
- Webhook endpoint for real-time notifications (no polling)
- Support for GitHub Apps authentication
- Multi-repository filtering and grouping
- Custom notification templates and sounds
- Dark mode support for UI
- Keyboard shortcuts for common actions
- Integration with other development tools
- Statistics and analytics dashboard
