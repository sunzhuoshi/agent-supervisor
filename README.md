# GitHubCopilotAgentBot

A Windows system tray application that helps improve GitHub Copilot agents workflows by monitoring pull request reviews and sending desktop notifications.

## Features

- **System Tray Application**: Runs in background with system tray icon
- **Monitor PR Reviews**: Automatically monitors pull request reviews assigned to the current user
- **Desktop Notifications**: Displays Windows balloon tip notifications when new reviews are detected
- **Settings UI**: Easy-to-use GUI for configuring GitHub Personal Access Token and polling interval
- **Configurable Polling**: Poll GitHub API periodically with configurable interval (default: 60 seconds)
- **Notification History**: Maintains a persistent history of all notifications
- **Browser Integration**: Click on notifications to open pull requests in your default browser
- **Windows Support**: Built specifically for Windows using C# and Windows Forms

## Requirements

- Windows OS
- .NET 6.0 SDK or later (for building)
- .NET 6.0 Runtime (for running)
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
# bin/Release/net6.0-windows/GitHubCopilotAgentBot.exe
```

## Running the Application

1. **First Run**:
   - Double-click `GitHubCopilotAgentBot.exe`
   - A settings dialog will appear
   - Enter your GitHub Personal Access Token
   - Configure polling interval (default: 60 seconds)
   - Click "Save"

2. **System Tray**:
   - The application runs in the system tray (notification area)
   - Look for the information icon in your system tray
   - The tooltip shows the current connection status

3. **Using the Application**:
   - **Right-click the tray icon** to access the menu:
     - "Recent Notifications" - View the last 10 notifications
     - "Settings" - Change your configuration
     - "Exit" - Close the application
   - **Double-click the tray icon** - View recent notifications
   - **Click a balloon notification** - Opens the PR in your browser

## Configuration

Configuration is stored in `config.json` in the application directory:

```json
{
  "PersonalAccessToken": "your_github_token_here",
  "PollingIntervalSeconds": 60,
  "MaxHistoryEntries": 100
}
```

You can edit this file manually or use the Settings UI (right-click tray icon → Settings).

## How It Works

1. **Background Monitoring**: The application runs in the background, checking GitHub every N seconds
2. **System Tray**: Displays an icon in the Windows notification area
3. **Balloon Notifications**: When a new PR review is detected, a Windows balloon tip notification appears
4. **Notification History**: All notifications are saved to `notification_history.json`
5. **Click to Open**: Click on a notification to open the PR in your default browser

## Files Created

- `config.json` - Configuration file (contains your PAT, keep it secure!)
- `notification_history.json` - Notification history

Both files are excluded from git via `.gitignore`.

## Project Structure

```
GitHubCopilotAgentBot/
├── GitHubCopilotAgentBot.csproj # .NET project file
├── src/
│   ├── Program.cs               # Main entry point and application context
│   ├── SettingsForm.cs          # Settings UI form
│   ├── SystemTrayManager.cs     # System tray icon and notifications
│   ├── GitHubService.cs         # GitHub API integration
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
- Ensure .NET 6.0 Runtime is installed
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
- Right-click tray icon → Recent Notifications to see history
- Check that Windows notifications are enabled for the application

### Can't find the system tray icon
- Look in the hidden icons area (click the ^ arrow in the system tray)
- The icon is a standard Windows information icon

### Build errors
- Ensure .NET 6.0 SDK is installed: `dotnet --version`
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
