<!-- Use this file to provide workspace-specific custom instructions to Copilot. For more details, visit https://code.visualstudio.com/docs/copilot/copilot-customization#_use-a-githubcopilotinstructionsmd-file -->

# GitBackup Project - Copilot Instructions

This file provides GitHub Copilot with context and guidelines specific to the GitBackup project.

## Project Overview

GitBackup is a cross-platform C# console application that creates git-based backups of directories. It uses LibGit2Sharp for git operations and provides incremental backup functionality with version control benefits.

## Architecture & Technology Stack

### Core Technologies
- **.NET 8.0** - Target framework
- **LibGit2Sharp 0.31.0** - Git operations (native git.dll integration)
- **CommandLineParser 2.9.1** - Professional CLI argument parsing
- **Microsoft.Extensions.Configuration.Ini 9.0.8** - INI configuration file support

### Testing Framework
- **Robot Framework 7.0.1** - End-to-end testing
- **Cross-platform testing** on Windows and Linux via GitHub Actions

## Code Style & Conventions

### C# Coding Standards
- Use **nullable reference types** (`#nullable enable`)
- Follow **Microsoft C# conventions** for naming and structure
- Use **async/await** for I/O operations
- Implement proper **exception handling** with meaningful error messages
- Use **XML documentation comments** for public APIs

### File Organization
```
GitBackup/
├── *.cs                    # Main application classes
├── .github/                # GitHub-specific files
├── .vscode/                # VS Code configuration
├── tests/                  # Robot Framework tests
├── *.md                    # Documentation files
└── *.ps1, *.sh            # Build and test scripts
```

## Key Classes & Responsibilities

### Core Classes
- **`Program.cs`** - Application entry point with CommandLineParser integration
- **`GitBackupConfig.cs`** - Configuration model with validation
- **`GitBackupService.cs`** - Core backup logic using LibGit2Sharp
- **`ConfigurationLoader.cs`** - INI file loading and sample generation
- **`CommandLineOptions.cs`** - CLI argument definitions

### Design Patterns
- **Dependency Injection** - Configuration passed to services
- **Factory Pattern** - Configuration creation from files
- **Command Pattern** - CLI options drive application behavior

## Configuration System

### INI File Structure
```ini
[GitBackup]
RootDir=C:\Source\Directory
BackupDir=C:\Backup\Directory
GitUserName=Backup User
GitUserEmail=backup@example.com
Exclude:0=*.tmp
Exclude:1=.git/
```

### Key Principles
- **Always validate** configuration before use
- **Provide helpful defaults** for optional settings
- **Support both relative and absolute paths**
- **Handle cross-platform path differences**

## Git Integration Guidelines

### LibGit2Sharp Usage
- **Always use `using` statements** for Repository objects
- **Initialize repositories** only in backup directories
- **Create meaningful commit messages** with timestamps
- **Handle git errors gracefully** with user-friendly messages
- **Support custom git user information**

### Backup Process Flow
1. Validate configuration
2. Ensure backup directory exists
3. Initialize/open git repository
4. Copy changed files (incremental)
5. Stage all changes
6. Create commit with metadata
7. Report results to user

## Command-Line Interface

### CommandLineParser Integration
- Use **Option attributes** for all command-line arguments
- Provide **meaningful help text** for each option
- Support **short and long option names** (-c, --config)
- Implement **proper error handling** for invalid arguments
- Use **Value attributes** for positional arguments when needed

### User Experience Principles
- **Always show progress** for long-running operations
- **Provide verbose mode** for debugging
- **Support dry-run mode** for preview operations
- **Display clear error messages** with actionable suggestions
- **Show version information** including build details

## Testing Guidelines

### Robot Framework Tests
- **Test all CLI options** and combinations
- **Verify cross-platform compatibility** (Windows/Linux)
- **Test error scenarios** and edge cases
- **Validate file system operations** and permissions
- **Check git repository integrity** after operations

### Test Structure
- **Core tests** - `gitbackup_tests.robot` (CLI, config, backup operations)
- **Windows tests** - `windows_tests.robot` (paths, permissions, exclusions)
- **Linux tests** - `linux_tests.robot` (symlinks, case sensitivity, permissions)

### Test Principles
- **Use descriptive test names** that explain functionality
- **Setup and teardown** test environments properly
- **Verify both exit codes and output content**
- **Handle platform-specific differences** appropriately

## CI/CD & Versioning

### GitHub Actions Workflow
- **Matrix builds** on Windows and Linux
- **Automatic version numbering** using git commit count
- **Robot Framework testing** before builds
- **Artifact generation** for releases
- **Test report uploads** for debugging

### Version Management
- **Semantic versioning** with build numbers (1.0.0.{commit_count})
- **Assembly versioning** in Directory.Build.props
- **Build number injection** via MSBuild properties
- **Version display** in CLI and about information

## Error Handling & Logging

### Exception Handling
- **Catch specific exceptions** rather than generic Exception
- **Provide context** in error messages (file paths, operation details)
- **Log errors appropriately** based on verbosity level
- **Return meaningful exit codes** for automation

### User Feedback
- **Use Console.WriteLine** for normal output
- **Use different colors/formatting** for warnings and errors (when possible)
- **Provide progress indicators** for long operations
- **Show summary information** after completion

## Development Workflow

### Local Development
- **Use `dotnet run`** for testing during development
- **Use `.\run-tests.ps1`** for local test execution
- **Use `.\build-test.ps1`** for version testing
- **Test on both platforms** when possible

### Code Reviews
- **Verify cross-platform compatibility**
- **Check test coverage** for new features
- **Validate error handling** and user experience
- **Ensure documentation** is updated

## Performance Considerations

### File Operations
- **Use async/await** for I/O operations where possible
- **Implement incremental copying** (only changed files)
- **Respect exclude patterns** to avoid unnecessary work
- **Handle large directories** efficiently

### Git Operations
- **Use LibGit2Sharp efficiently** (proper disposal)
- **Batch git operations** when possible
- **Monitor repository size** and performance
- **Consider git garbage collection** for large repositories

## Security Considerations

### File System Access
- **Validate all paths** before operations
- **Handle permission errors** gracefully
- **Respect file system security** (don't bypass protections)
- **Be careful with symlinks** and junctions

### Configuration Security
- **Validate configuration inputs**
- **Don't expose sensitive information** in logs
- **Handle configuration errors** securely

## Future Enhancement Areas

### Potential Features
- **Encryption support** for sensitive backups
- **Remote git repository** support
- **Backup scheduling** and automation
- **GUI interface** for non-technical users
- **Backup verification** and integrity checks
- **Incremental restore** functionality

When implementing new features or making changes, always consider:
1. Cross-platform compatibility
2. Test coverage (Robot Framework)
3. Error handling and user experience
4. Performance impact
5. Documentation updates
6. CI/CD pipeline compatibility
