# Versioning Policy

Agent Supervisor follows [Semantic Versioning 2.0.0](https://semver.org/) principles for version numbering.

## Version Format

Versions are formatted as: **MAJOR.MINOR.PATCH**

Example: `1.2.3`

- **MAJOR** = 1
- **MINOR** = 2  
- **PATCH** = 3

## Version Increment Rules

### Major Version (X.0.0)

Increment the MAJOR version when making **incompatible changes** or adding **significant features**, including:

- **Breaking Changes**: Changes that break backward compatibility
  - Removing or renaming public APIs
  - Changing configuration file format in incompatible ways
  - Modifying command-line arguments or behavior
  - Removing support for previously supported platforms
- **Major Features**: Significant new functionality that substantially changes the application's capabilities
  - Complete redesign or major architectural changes
  - Addition of major new subsystems or modules
  - Fundamental changes to the application's purpose or behavior

**Examples:**
- Changing from console application to GUI application (breaking change)
- Removing deprecated configuration options
- Upgrading to a new .NET major version that drops backward compatibility

### Minor Version (0.X.0)

Increment the MINOR version when adding **new features** in a backward-compatible manner, including:

- New functionality that doesn't break existing behavior
- New configuration options (with sensible defaults)
- New command-line options
- Performance improvements
- Enhanced UI/UX without breaking existing workflows
- New notification types or monitoring capabilities
- Additional platform support

**Examples:**
- Adding support for GitHub Enterprise
- Adding new notification channels (Slack, email, etc.)
- Adding filtering options for pull requests
- Adding new configuration settings with backward-compatible defaults

### Patch Version (0.0.X)

Increment the PATCH version for **bug fixes** and minor improvements that don't add new features:

- Bug fixes that don't change functionality
- Security patches
- Performance optimizations without behavior changes
- Documentation updates
- Dependency updates (unless they introduce breaking changes)
- Code refactoring without functional changes
- Fixing typos in UI or messages

**Examples:**
- Fixing a crash when notification history file is corrupted
- Correcting timezone handling in notifications
- Fixing memory leaks
- Updating vulnerable dependencies
- Improving error messages

## Pre-release Versions

For development and testing, pre-release versions may use additional labels:

- **Alpha**: `1.0.0-alpha.1` - Early development, unstable
- **Beta**: `1.0.0-beta.1` - Feature complete, testing phase
- **Release Candidate**: `1.0.0-rc.1` - Final testing before release

## Version in Source Code

The version is maintained in `AgentSupervisor.csproj`:

```xml
<PropertyGroup>
  <Version>1.0.0</Version>
  <AssemblyVersion>1.0.0</AssemblyVersion>
  <FileVersion>1.0.0</FileVersion>
</PropertyGroup>
```

All three version properties should be kept in sync.

## Release Process

1. **Update Version**: Modify version numbers in `AgentSupervisor.csproj`
2. **Commit Changes**: Commit the version change with message `"Bump version to X.Y.Z"`
3. **Create Tag**: Create a git tag `vX.Y.Z` matching the version
4. **Push Tag**: Push the tag to trigger automated release workflow

The GitHub Actions workflow will:
- Verify tag version matches source version
- Build the application
- Check for security vulnerabilities
- Create GitHub release with artifacts

## Version History

Version history is tracked through:
- Git tags (e.g., `v1.0.0`, `v1.1.0`)
- GitHub Releases with auto-generated release notes
- Git commit history

## Decision: Build/Revision Version

The fourth version component (build/revision number) is **not used** in this project because:

1. **Semantic Versioning** uses three components (MAJOR.MINOR.PATCH)
2. **Simplicity**: Three components are sufficient for clear versioning
3. **Git Commits**: Detailed change tracking is handled by git commit SHAs
4. **Automated Builds**: CI/CD builds are identified by workflow run numbers, not version numbers

### CI Build Version Examples

While the project uses three-component versions (e.g., `1.0.0`), CI builds can be identified using:

- **GitHub Actions Run ID**: Build artifacts are named like `AgentSupervisor-Release` with workflow run `#123`
- **Build Metadata (optional)**: If needed, build metadata can be appended using a plus sign without affecting version precedence:
  - `1.0.0+build.123` - includes build/run number
  - `1.0.0+20231113.1` - includes date and build count
  - `1.0.0+sha.a1b2c3d` - includes git commit SHA
  - `1.0.0+ci.456.a1b2c3d` - combines run number and commit SHA

**Note**: Build metadata after the `+` sign is for informational purposes only and does not affect version precedence in Semantic Versioning.

## Guidelines for Contributors

When contributing changes:

1. **Don't update version numbers** in your pull request - maintainers will handle versioning
2. **Describe your changes clearly** so maintainers can determine the appropriate version bump
3. **Note breaking changes** prominently in your PR description
4. **Update CHANGELOG** or release notes if applicable (when implemented)

## Questions and Suggestions

For questions about versioning or suggestions for this policy, please:
- Open an issue in the GitHub repository
- Start a discussion in the project's discussion forum
- Contact the maintainers

## References

- [Semantic Versioning 2.0.0](https://semver.org/)
- [GitHub Release Workflow](.github/workflows/release.yml)
- [Keep a Changelog](https://keepachangelog.com/)
