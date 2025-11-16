# CI Data Polling Feature

## Overview

The Agent Supervisor application includes a special menu item that is only available in CI builds. This feature allows triggering an immediate polling of review requests from GitHub, bypassing the normal scheduled polling interval.

## How It Works

### Build-Time Configuration

This feature is controlled at **compile-time** using conditional compilation. The feature is enabled by defining the `ENABLE_CI_FEATURES` compilation symbol during the build process.

**CI Builds**: Include the `ENABLE_CI_FEATURES` symbol → Data polling menu item is available
**Release Builds**: Do not include the symbol → Data polling menu item is NOT available

This approach ensures that production/release builds never include the CI-specific features, providing a clean separation between testing and production functionality.

### Menu Items

When the application is built with CI features enabled, additional menu items appear in the system tray context menu:

```
┌─────────────────────────────────┐
│ Review Requests by Copilots     │
├─────────────────────────────────┤
│ Poll at Once          ← CI only │
│ Pause Polling         ← CI only │  
├─────────────────────────────────┤
│ Settings                        │
│ About                           │
├─────────────────────────────────┤
│ Exit                            │
└─────────────────────────────────┘
```

#### Poll at Once

When the "Poll at Once" menu item is clicked, the application:

1. **Immediately triggers** a polling of review requests from GitHub
2. Bypasses the normal scheduled polling interval
3. Updates the review request list and notification history
4. Refreshes the taskbar badge count
5. Updates the system tray status

This is useful for testing and debugging scenarios where you need to see the latest data immediately without waiting for the next scheduled poll.

#### Pause Polling

The "Pause Polling" menu item allows you to pause and resume the automatic data polling from GitHub:

- **Default State**: Not paused (polling is active)
- **Menu Text**: Displays "Pause Polling" when polling is active, "Resume Polling" when paused
- **Behavior When Paused**:
  - Scheduled polling continues to run but skips data polling from GitHub
  - Taskbar badge count still updates based on existing data
  - System tray status shows "Paused" prefix
  - No new review requests are fetched from GitHub
- **Behavior When Resumed**:
  - Normal scheduled polling resumes immediately
  - Data polling from GitHub continues as configured
  - System tray status returns to normal

The pause state is persisted in the Windows Registry and survives application restarts. This is useful for temporarily stopping data polling during testing or when you want to avoid API rate limits without changing the polling interval.

## Building with CI Features

### GitHub Actions (Automated)

The CI build workflow (`.github/workflows/build.yml`) automatically enables CI features by including the compilation symbol:

```yaml
- name: Build
  run: dotnet build --no-restore --configuration Release /p:DefineConstants="ENABLE_CI_FEATURES" ...
```

Builds from the CI workflow will include the "Poll at Once" menu item.

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

Release builds (`.github/workflows/release.yml`) do **not** include the `ENABLE_CI_FEATURES` symbol, ensuring that production releases never have the CI-specific menu items.

To build a release version locally without CI features:
```bash
dotnet build --configuration Release
```

## Use Cases

These CI features are useful for:

1. **Immediate Data Refresh** (Poll at Once): Force an immediate fetch of review requests without waiting for the scheduled polling interval
2. **Testing**: Verify that the GitHub API integration is working correctly during CI builds
3. **Debugging**: Quickly check for new review requests during development and testing
4. **CI/CD Pipeline Testing**: Validate that the application can successfully connect to GitHub and fetch data
5. **Pause/Resume Polling** (Pause Polling): Temporarily stop data polling without closing the application or changing polling interval settings
6. **API Rate Limit Management**: Pause polling when approaching GitHub API rate limits during testing
7. **Development Workflows**: Pause automatic polling while making code changes or debugging

## Privacy & Security

- No data is saved to files or transmitted externally
- These features simply control the existing GitHub API polling operation
- No sensitive credentials or tokens are exposed
- The pause state is persisted locally in the Windows Registry
- These features are completely absent from release builds (compile-time removal)

## Limitations

- These menu items are **only** visible in builds compiled with `ENABLE_CI_FEATURES` defined
- For regular release builds, these features are not available (by design)
- Triggering polling requires an active network connection to GitHub
