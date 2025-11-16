# Auto-Adjust Polling Interval Feature

## Overview

The Agent Supervisor can automatically adjust its polling interval based on GitHub API rate limit headers. This ensures optimal use of API requests while avoiding rate limit issues.

## How It Works

### Rate Limit Headers

GitHub API returns rate limit information in response headers:
- `x-ratelimit-limit`: Maximum requests per hour (typically 5000 for authenticated requests)
- `x-ratelimit-remaining`: Number of requests remaining in current window
- `x-ratelimit-reset`: Unix timestamp when the rate limit resets

### Optimal Interval Calculation

The system calculates the optimal polling interval using this formula:

```
optimal_interval = (time_until_reset / remaining_requests) * safety_margin
```

Where:
- `time_until_reset`: Seconds until the rate limit window resets
- `remaining_requests`: Number of API requests remaining
- `safety_margin`: 1.2 (20% buffer to avoid hitting the limit)

The calculated interval is clamped between:
- **Minimum**: 10 seconds (prevents excessive polling)
- **Maximum**: 3600 seconds (1 hour, prevents too long delays)

### Examples

**Example 1: Normal Operation**
- Limit: 5000
- Remaining: 4966
- Time until reset: 60 minutes (3600 seconds)
- Calculated: (3600 / 4966) * 1.2 ≈ 0.87 seconds
- **Applied: 10 seconds** (enforced minimum)

**Example 2: Moderate Usage**
- Limit: 5000
- Remaining: 100
- Time until reset: 60 minutes (3600 seconds)
- Calculated: (3600 / 100) * 1.2 ≈ 43.2 seconds
- **Applied: 44 seconds**

**Example 3: Near Limit**
- Limit: 5000
- Remaining: 50
- Time until reset: 60 minutes (3600 seconds)
- Calculated: (3600 / 50) * 1.2 ≈ 86.4 seconds
- **Applied: 87 seconds**

**Example 4: Rate Limited**
- Limit: 5000
- Remaining: 0
- Time until reset: 30 minutes (1800 seconds)
- **Applied: 1800 seconds** (waits until reset)

## Configuration

### Enabling/Disabling

The feature can be controlled via the Settings dialog:
1. Right-click the system tray icon
2. Select "Settings"
3. Check/uncheck "Auto Adjust Polling Interval (based on rate limits)"
4. Click "Save"

**Default**: Enabled

### Registry Setting

The setting is stored in Windows Registry:
- Path: `HKEY_CURRENT_USER\Software\AgentSupervisor`
- Key: `AutoAdjustPollingInterval`
- Type: `DWORD`
- Values: `1` (enabled) or `0` (disabled)

### Fallback Behavior

When auto-adjust is **disabled** or rate limit information is unavailable:
- Uses the fixed `PollingIntervalSeconds` from settings
- Default: 60 seconds
- Configurable range: 10-3600 seconds

## Benefits

1. **Efficient API Usage**: Polls as frequently as possible without hitting rate limits
2. **Automatic Adjustment**: Adapts to changing rate limit conditions
3. **Safety Buffer**: 20% margin prevents accidental rate limit violations
4. **Graceful Degradation**: Falls back to fixed interval when needed
5. **User Control**: Can be disabled if fixed interval is preferred

## Logging

The system logs rate limit information and interval calculations:

```
Rate Limit - Limit: 5000, Remaining: 4966, Reset: 2025-11-16 13:45:17 UTC
Auto-adjusted polling interval: 10 seconds (based on rate limit: 4966/5000 remaining)
```

Warning when near rate limit (< 10% remaining):
```
Approaching rate limit: 450 requests remaining
```

## Implementation Details

### RateLimitInfo Class

Located in `src/Models/RateLimitInfo.cs`, this class:
- Parses rate limit headers from API responses
- Calculates optimal polling intervals
- Provides rate limit status checks

### GitHubService Updates

- Extracts rate limit headers from all API responses
- Stores the latest rate limit information
- Exposes `LastRateLimitInfo` property

### Program.cs Changes

- Checks `AutoAdjustPollingInterval` setting each polling cycle
- Calculates next interval using rate limit data when enabled
- Falls back to configured interval on errors or when disabled

## Testing

To verify the feature:

1. Enable auto-adjust in Settings
2. Monitor the log output for interval calculations
3. Check that intervals change based on API usage
4. Verify graceful handling when rate limit is approached

## Known Limitations

1. Rate limit information only available after first API call
2. Initial polling cycle always uses configured interval
3. DEV features (pause/resume) take precedence over auto-adjust
4. Windows Forms application requires Windows OS to test
