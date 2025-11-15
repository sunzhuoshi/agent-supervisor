# CI Data Collection Feature

## Overview

The Agent Supervisor application includes a special menu item that is only available in CI builds. This feature allows triggering an immediate collection of review requests from GitHub, bypassing the normal scheduled polling interval.

## How It Works

### Build-Time Configuration

This feature is controlled at **compile-time** using conditional compilation. The feature is enabled by defining the `ENABLE_CI_FEATURES` compilation symbol during the build process.

**CI Builds**: Include the `ENABLE_CI_FEATURES` symbol → Data collection menu item is available
**Release Builds**: Do not include the symbol → Data collection menu item is NOT available

This approach ensures that production/release builds never include the CI-specific features, providing a clean separation between testing and production functionality.

### Menu Item

When the application is built with CI features enabled, a "Collect at Once" menu item appears in the system tray context menu:

```
┌─────────────────────────────────┐
│ Review Requests by Copilots     │
├─────────────────────────────────┤
│ Collect at Once       ← CI only │  
├─────────────────────────────────┤
│ Settings                        │
│ About                           │
├─────────────────────────────────┤
│ Exit                            │
└─────────────────────────────────┘
```

### Data Collection

When the "Collect at Once" menu item is clicked, the application:

1. **Immediately triggers** a collection of review requests from GitHub
2. Bypasses the normal scheduled polling interval
3. Updates the review request list and notification history
4. Refreshes the taskbar badge count
5. Updates the system tray status

This is useful for testing and debugging scenarios where you need to see the latest data immediately without waiting for the next scheduled poll.

## Building with CI Features

### GitHub Actions (Automated)

The CI build workflow (`.github/workflows/build.yml`) automatically enables CI features by including the compilation symbol:

```yaml
- name: Build
  run: dotnet build --no-restore --configuration Release /p:DefineConstants="ENABLE_CI_FEATURES" ...
```

Builds from the CI workflow will include the "Collect at Once" menu item.

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

1. **Immediate Data Refresh**: Force an immediate fetch of review requests without waiting for the scheduled polling interval
2. **Testing**: Verify that the GitHub API integration is working correctly during CI builds
3. **Debugging**: Quickly check for new review requests during development and testing
4. **CI/CD Pipeline Testing**: Validate that the application can successfully connect to GitHub and fetch data

## Privacy & Security

- No data is saved to files or transmitted externally
- The feature simply triggers the existing GitHub API polling operation
- No sensitive credentials or tokens are exposed
- The feature is completely absent from release builds (compile-time removal)

## Limitations

- The menu item is **only** visible in builds compiled with `ENABLE_CI_FEATURES` defined
- For regular release builds, this feature is not available (by design)
- Triggering collection requires an active network connection to GitHub
