# GitHub Copilot Instructions

## Commit Messages and Pull Requests

All commits and pull request titles must follow the [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) specification.

### Format

```
<type>[(<scope>)][!]: <description>

[optional body]

[optional footer(s)]
```

### Types

| Type | Description |
|------|-------------|
| `feat` | A new feature |
| `fix` | A bug fix |
| `docs` | Documentation only changes |
| `style` | Changes that do not affect the meaning of the code (white-space, formatting, etc.) |
| `refactor` | A code change that neither fixes a bug nor adds a feature |
| `perf` | A code change that improves performance |
| `test` | Adding missing tests or correcting existing tests |
| `build` | Changes that affect the build system or external dependencies |
| `ci` | Changes to CI configuration files and scripts |
| `chore` | Other changes that don't modify src or test files |
| `revert` | Reverts a previous commit |

### Rules

- The description must be in lowercase and not end with a period.
- Use the imperative mood in the description (e.g., "add feature" not "added feature").
- A `!` after the type/scope indicates a **breaking change** (e.g., `feat!: drop support for X`).
- Breaking changes must also include a footer starting with `BREAKING CHANGE:`.

### Examples

```
feat(notifications): add balloon tip for new PR reviews
fix(github): handle rate limit errors gracefully
docs: update README with proxy configuration steps
chore: bump .NET SDK to 8.0.4
ci: add codeql security scanning workflow
refactor(settings): extract registry helpers into Configuration class
feat!: remove legacy JSON config file support

BREAKING CHANGE: configuration is now stored exclusively in the Windows Registry.
```
