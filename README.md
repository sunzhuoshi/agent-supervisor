# Agent Supervisor

[![Latest Release](https://img.shields.io/github/v/release/sunzhuoshi/agent-supervisor?label=version)](https://github.com/sunzhuoshi/agent-supervisor/releases/latest)
[![Build Status](https://img.shields.io/github/actions/workflow/status/sunzhuoshi/agent-supervisor/build.yml?branch=main)](https://github.com/sunzhuoshi/agent-supervisor/actions)
[![License](https://img.shields.io/github/license/sunzhuoshi/agent-supervisor)](LICENSE)

A Windows system tray application that helps improve GitHub Copilot agents workflows by monitoring pull request reviews and sending desktop notifications.

## Features

- **Auto-Update**: Automatically checks for new releases from GitHub and prompts to upgrade with one click
- **Taskbar Badge Overlay**: Shows the number of pending PR review requests on the taskbar icon
- **System Tray Application**: Runs in background with system tray icon
- **Monitor PR Reviews**: Automatically monitors pull request reviews assigned to the current user
- **Desktop Notifications**: Displays Windows balloon tip notifications when new reviews are detected
- **PR Review Requests Tracking**: View all review requests with new/read status in a dedicated dialog
- **Mark as Read**: Double-click review requests to open them and automatically mark as read
- **Bulk Mark as Read**: Button to mark all review requests as read at once
- **Persistent Storage**: Review requests are saved and restored between application restarts
- **Settings UI**: Easy-to-use GUI for configuring GitHub Personal Access Token and polling interval
- **Configurable Polling**: Poll GitHub API periodically with configurable interval (default: 60 seconds)
- **Notification History**: Maintains a persistent history of all notifications
- **Browser Integration**: Click on notifications to open pull requests in your default browser
- **Windows Support**: Built specifically for Windows using C# and Windows Forms

## Requirements

- Windows OS
- .NET 8.0 SDK or later (for building)
- .NET 8.0 Runtime (for running)
- GitHub Personal Access Token with appropriate permissions

## GitHub Personal Access Token Setup

1. Go to GitHub Settings → Developer settings → Personal access tokens → Tokens (classic)
2. Click "Generate new token" → "Generate new token (classic)"
3. Select the following scopes:
   - `repo` (Full control of private repositories)
   - `read:user` (Read user profile data)
4. Copy the generated token (you won't be able to see it again!)

## Building the Application

```bash
# Restore dependencies
dotnet restore

# Build the application
dotnet build --configuration Release

# The executable will be at:
# bin/Release/net8.0-windows/AgentSupervisor.exe
```

## Running the Application

1. **First Run**:
   - Double-click `AgentSupervisor.exe`
   - A settings dialog will appear
   - Enter your GitHub Personal Access Token
   - Configure polling interval (default: 60 seconds)
   - Click "Save"

2. **Taskbar**:
   - The application appears in the Windows taskbar with a custom icon
   - A badge overlay displays the number of pending PR review requests (e.g., a red bubble with "3")
   - The badge automatically updates when the pending count changes
   
3. **System Tray**:
   - The application also runs in the system tray (notification area)
   - Look for the custom purple-to-blue gradient icon with "A" in your system tray
   - The tooltip shows the current connection status

4. **Using the Application**:
   - **Right-click the tray icon** to access the menu:
     - "Review Requests by Copilots" - View all review requests with new/read status
     - "Settings" - Change your configuration
     - "About" - View application information
     - "Check for Updates" - Manually check for application updates
     - "Exit" - Close the application
   - **Double-click the tray icon** - View PR review requests
   - **Double-click a review request** - Opens the PR in your browser and marks it as read
   - **Click a balloon notification** - Opens the PR in your browser
   - **Check the taskbar badge** - See how many reviews are pending at a glance

## Auto-Update

The application automatically checks for updates from GitHub releases:

- **Automatic Check**: On startup, the app checks for new releases (can be disabled in config)
- **Manual Check**: Right-click the tray icon and select "Check for Updates"
- **Update Notification**: A balloon notification appears when a new version is available
- **One-Click Update**: Click "Yes" to download and install the update automatically
- **Data Preservation**: Your configuration and notification history are preserved during the update
- **Automatic Restart**: The application restarts automatically after the update is installed

The update process:
1. Downloads the latest release from GitHub
2. Backs up your configuration files (`config.json`, `notification_history.json`, `review_requests.json`)
3. **Backs up old version files to `rollback/` directory for rollback capability**
4. Extracts and installs the new version
5. Restores your configuration files
6. Restarts the application

### Rollback to Previous Version

If you encounter issues with a new version, you can manually rollback:

1. Close Agent Supervisor
2. Navigate to the `rollback/` folder in the application directory
3. Find the folder for your previous version (e.g., `version_1.0.0_20250113_095830`)
4. Copy all files from that folder back to the application directory
5. Restart Agent Supervisor

The rollback directory keeps backups of previous versions, allowing you to restore if needed.

## Configuration

Configuration is stored in the Windows Registry under `HKEY_CURRENT_USER\Software\AgentSupervisor`:

| Setting | Registry Value | Default |
|---------|---------------|---------|
| GitHub Personal Access Token | PersonalAccessToken | (empty) |
| Polling Interval (seconds) | PollingIntervalSeconds | 60 |
| Max History Entries | MaxHistoryEntries | 100 |
| Enable Desktop Notifications | EnableDesktopNotifications | 1 (enabled) |
| Proxy URL | ProxyUrl | (empty) |
| Use Proxy | UseProxy | 0 (disabled) |
| Pause Polling (CI builds only) | PausePolling | 0 (disabled) |

You can configure settings using the Settings UI (right-click tray icon → Settings).

**Benefits of Registry Storage:**
- Configuration persists across different versions of the application
- No need to manually reconfigure when testing new builds
- Settings survive application reinstalls (unless uninstalled via Windows Settings)
- Standard Windows approach for application settings

- `PersonalAccessToken`: Your GitHub Personal Access Token
- `PollingIntervalSeconds`: How often to check for new PR reviews (default: 60)
- `MaxHistoryEntries`: Maximum number of notifications to keep in history (default: 100)
- `CheckForUpdatesOnStartup`: Whether to check for updates when the app starts (default: true)
- `LastUpdateCheck`: Timestamp of the last update check (automatically managed)

## How It Works

1. **Background Monitoring**: The application runs in the background, checking GitHub every N seconds
2. **Taskbar Badge**: Shows a number bubble on the taskbar icon indicating pending review requests
3. **System Tray**: Displays an icon in the Windows notification area
4. **Balloon Notifications**: When a new PR review is detected, a Windows balloon tip notification appears
5. **Review Request Tracking**: All review requests are saved to `review_request_details.json` with new/read status
6. **Notification History**: All notifications are saved to `notification_history.json`
7. **Click to Open**: Click on a notification or double-click a review request to open the PR in your default browser
8. **Auto-Update**: Checks for new releases on startup and notifies when updates are available

## Files Created

- `notification_history.json` - Notification history
- `review_request_details.json` - PR review requests with new/read status
- `review_requests.json` - Tracking of review requests to avoid duplicate notifications

These files are excluded from git via `.gitignore`.


## Project Structure

```
AgentSupervisor/
├── AgentSupervisor.csproj # .NET project file
├── src/
│   ├── Program.cs               # Main entry point and application context
│   ├── MainWindow.cs            # Hidden window for taskbar presence
│   ├── TaskbarBadgeManager.cs   # Taskbar badge overlay management
│   ├── SettingsForm.cs          # Settings UI form
│   ├── AboutForm.cs             # About dialog
│   ├── SystemTrayManager.cs     # System tray icon and notifications
│   ├── GitHubService.cs         # GitHub API integration
│   ├── UpdateService.cs         # Auto-update functionality
│   ├── NotificationHistory.cs   # Persistent notification storage
│   ├── Configuration.cs         # Configuration management
│   └── Models/
│       ├── PullRequestReview.cs # PR review data model
│       └── NotificationEntry.cs # Notification data model
└── README.md
```

## Security Considerations

- **Never commit your `config.json`** - It contains your Personal Access Token
- The `.gitignore` file excludes sensitive files by default
- Store your token securely and never share it
- Use tokens with minimal required permissions
- Rotate your token periodically

## Troubleshooting

### Application doesn't start
- Ensure .NET 8.0 Runtime is installed
- Check Windows Event Viewer for error messages
- Try running from command line to see error output

### "Failed to connect to GitHub"
- Verify your Personal Access Token is correct
- Check that your token has the required scopes (`repo`, `read:user`)
- Ensure you have internet connectivity
- Check if your firewall is blocking the application

### No notifications appear
- Check that you're actually requested as a reviewer on open PRs
- Verify the polling interval - it may not have checked yet
- Right-click tray icon → Review Requests by Copilots to see all requests
- Check that Windows notifications are enabled for the application

### Can't find the system tray icon
- Look in the hidden icons area (click the ^ arrow in the system tray)
- The icon is a standard Windows information icon

### Build errors
- Ensure .NET 8.0 SDK is installed: `dotnet --version`
- Try cleaning the build: `dotnet clean` then `dotnet build`

## Differences from Console Version

This is a Windows Forms application that runs in the system tray, not a console application:
- No console window appears
- Uses Windows balloon tip notifications instead of console output
- Has a graphical settings UI instead of text prompts
- Runs silently in the background
- Can be controlled via system tray context menu

## License

See LICENSE file for details.

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.

## Releases

Releases are automatically created when a version tag is pushed to the repository. The release workflow:

1. Verifies that the tag version matches the version in `AgentSupervisor.csproj`
2. Builds the application in Release configuration
3. Checks for security vulnerabilities
4. Creates a GitHub release with auto-generated release notes
5. Uploads a zip file containing the application and dependencies

For detailed information about versioning strategy and release process, see **[VERSIONING.md](VERSIONING.md)**.

### Creating a Release

There are two ways to create a new release:

#### Option 1: Manual Workflow (Recommended)

The easiest way to create a release is using the manual workflow dispatch:

1. Update the version in `AgentSupervisor.csproj`:
   ```xml
   <Version>1.0.0</Version>
   <AssemblyVersion>1.0.0</AssemblyVersion>
   <FileVersion>1.0.0</FileVersion>
   ```

2. Commit and push the version change:
   ```bash
   git add AgentSupervisor.csproj
   git commit -m "Bump version to 1.0.0"
   git push
   ```

3. Trigger the release manually via GitHub Actions:
   - Go to the [Actions tab](../../actions/workflows/release.yml) in the GitHub repository
   - Click on "Release" workflow
   - Click "Run workflow" button
   - Enter the version number (e.g., `1.0.0`) without the `v` prefix
   - Click "Run workflow"

The workflow will:
- Validate the version format (MAJOR.MINOR.PATCH)
- Verify that the version matches the version in `AgentSupervisor.csproj`
- Create and push the corresponding git tag (e.g., `v1.0.0`)
- Build the application and create the GitHub release

#### Option 2: Push Git Tag

Alternatively, you can create a release by pushing a git tag:

1. Update the version in `AgentSupervisor.csproj`:
   ```xml
   <Version>1.0.0</Version>
   <AssemblyVersion>1.0.0</AssemblyVersion>
   <FileVersion>1.0.0</FileVersion>
   ```

2. Commit and push the version change:
   ```bash
   git add AgentSupervisor.csproj
   git commit -m "Bump version to 1.0.0"
   git push
   ```

3. Create and push a tag:
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```

The release workflow will automatically run and create a GitHub release with the build artifacts.

See **[VERSIONING.md](VERSIONING.md)** for version numbering rules and guidelines.
