# GitHubCopilotAgentBot

A bot application that helps improve GitHub Copilot agents workflows by monitoring pull request reviews and sending desktop notifications.

## Features

- **Monitor PR Reviews**: Automatically monitors pull request reviews assigned to the current user
- **Desktop Notifications**: Displays custom console-based notifications (avoids Windows system notifications to prevent interference with other applications)
- **Configurable Polling**: Poll GitHub API periodically with configurable interval (default: 60 seconds)
- **Notification History**: Maintains a persistent history of all notifications
- **Browser Integration**: Click to open associated pull requests in your default browser
- **Windows Support**: Built specifically for Windows using C# and CMake

## Requirements

- Windows OS
- .NET 6.0 SDK or later
- CMake 3.20 or later (optional, for CMake builds)
- Visual Studio 2019/2022 or Visual Studio Build Tools (for CMake builds)
- GitHub Personal Access Token with appropriate permissions

## GitHub Personal Access Token Setup

1. Go to GitHub Settings → Developer settings → Personal access tokens → Tokens (classic)
2. Click "Generate new token" → "Generate new token (classic)"
3. Select the following scopes:
   - `repo` (Full control of private repositories)
   - `read:user` (Read user profile data)
4. Copy the generated token (you won't be able to see it again!)

## Building the Application

### Option 1: Using .NET CLI (Recommended)

```bash
# Restore dependencies
dotnet restore

# Build the application
dotnet build

# Run the application
dotnet run
```

### Option 2: Using CMake

```bash
# Create build directory
mkdir build
cd build

# Configure with CMake
cmake ..

# Build
cmake --build . --config Release

# Run
./bin/Release/GitHubCopilotAgentBot.exe
```

## Configuration

On first run, the application will prompt you to enter:
- GitHub Personal Access Token
- Polling interval (in seconds, default: 60)

The configuration is saved to `config.json` in the application directory.

### Manual Configuration

You can also create a `config.json` file manually:

```json
{
  "PersonalAccessToken": "your_github_token_here",
  "PollingIntervalSeconds": 60,
  "MaxHistoryEntries": 100
}
```

## Usage

1. **Start the application**:
   ```bash
   dotnet run
   ```

2. **Available Commands** (press keys while running):
   - `H` - Show notification history (last 20 notifications)
   - `O` - Open the last pending PR in your browser
   - `R` - Refresh and check for new reviews immediately
   - `Q` - Quit the application

3. **Notifications**:
   - When a new PR review is detected, a colored notification appears in the console
   - Audio beeps alert you to new notifications (if supported by your system)
   - Press `O` to quickly open the PR in your browser

## How It Works

1. **Authentication**: The bot authenticates with GitHub using your Personal Access Token
2. **Polling**: Every N seconds (configurable), it queries GitHub for PRs where you're requested as a reviewer
3. **Review Detection**: It checks for new reviews on those PRs
4. **Notification**: When a new review is found, it displays a notification in the console
5. **History**: All notifications are saved to `notification_history.json` for future reference

## Files Created

- `config.json` - Configuration file (contains your PAT, keep it secure!)
- `notification_history.json` - Notification history

Both files are excluded from git via `.gitignore`.

## Project Structure

```
GitHubCopilotAgentBot/
├── CMakeLists.txt              # CMake build configuration
├── GitHubCopilotAgentBot.csproj # .NET project file
├── src/
│   ├── Program.cs               # Main entry point
│   ├── GitHubService.cs         # GitHub API integration
│   ├── NotificationService.cs   # Notification display and history
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

### "Failed to connect to GitHub"
- Verify your Personal Access Token is correct
- Check that your token has the required scopes (`repo`, `read:user`)
- Ensure you have internet connectivity

### "No new reviews" but you expect some
- Check that you're actually requested as a reviewer on open PRs
- Verify the polling interval - it may not have checked yet
- Press `R` to force an immediate refresh

### Build errors
- Ensure .NET 6.0 SDK is installed: `dotnet --version`
- Try cleaning the build: `dotnet clean` then `dotnet build`
- For CMake builds, ensure Visual Studio build tools are installed

## License

See LICENSE file for details.

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.
