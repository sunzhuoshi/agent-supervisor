# Versioning

This project follows [Semantic Versioning](https://semver.org/) (SemVer) with the format `MAJOR.MINOR.PATCH`:

- **MAJOR** version: Incremented for incompatible API changes
- **MINOR** version: Incremented for backwards-compatible functionality additions
- **PATCH** version: Incremented for backwards-compatible bug fixes

## Version Format

Version numbers follow the pattern: `X.Y.Z` (e.g., `1.2.3`)

- X = Major version
- Y = Minor version  
- Z = Patch version

## Creating a Release

To create a new release:

1. Go to the **Actions** tab in GitHub
2. Select the **Manual Release** workflow
3. Click **Run workflow**
4. Enter the new version number (e.g., `1.2.3`)
   - The new version must be greater than the current version
   - The workflow will validate the version before proceeding
5. The workflow will:
   - Update the version in `AgentSupervisor.csproj`
   - Commit the change with message "Bump version to X.Y.Z"
   - Create a git tag `vX.Y.Z`
   - Push both the commit and tag to the `main` branch

## Version Guidelines

- Start with version `1.0.0` for the first stable release
- Increment PATCH for bug fixes (e.g., `1.0.0` → `1.0.1`)
- Increment MINOR for new features (e.g., `1.0.1` → `1.1.0`)
- Increment MAJOR for breaking changes (e.g., `1.1.0` → `2.0.0`)
- When incrementing MINOR, reset PATCH to 0
- When incrementing MAJOR, reset both MINOR and PATCH to 0
