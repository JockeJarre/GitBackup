# GitBackup

A C# console application that creates git-based backups of directories. GitBackup monitors changes in a source directory and commits them to a git repository, making it easy to track changes over time using any git tool.

## Features

- **Git-based versioning**: Every backup is a git commit, allowing you to browse history and restore specific versions
- **INI configuration**: Simple configuration file format
- **Incremental backups**: Only copies changed files to minimize backup time
- **Flexible exclusions**: Configure patterns to exclude files and directories
- **Cross-platform**: Built with .NET 8, runs on Windows, Linux, and macOS

## Quick Start

1. **Create a configuration file**:
   ```bash
   GitBackup --create-config
   ```

2. **Edit the configuration** (`gitbackup.ini`):
   ```ini
   [GitBackup]
   RootDir=C:\MyDocuments
   BackupDir=C:\Backups\MyDocuments
   GitUserName=GitBackup
   GitUserEmail=gitbackup@localhost
   
   Exclude:0=.git/
   Exclude:1=*.tmp
   Exclude:2=node_modules/
   ```

3. **Run the backup**:
   ```bash
   GitBackup
   ```

## Configuration

The application uses INI files for configuration. Here's a complete example:

```ini
[GitBackup]
# Source directory to backup
RootDir=C:\Projects\MyApp

# Destination directory for backups
BackupDir=C:\Backups\MyApp

# Git commit author information
GitUserName=John Doe
GitUserEmail=john.doe@example.com

# Files and patterns to exclude from backup
Exclude:0=.git/
Exclude:1=*.tmp
Exclude:2=*.temp
Exclude:3=*.log
Exclude:4=Thumbs.db
Exclude:5=.DS_Store
Exclude:6=node_modules/
Exclude:7=bin/
Exclude:8=obj/
```

### Configuration Options

- **RootDir**: The source directory to backup (required)
- **BackupDir**: Where backup files will be stored (required)
- **GitUserName**: Name used for git commits (default: "GitBackup")
- **GitUserEmail**: Email used for git commits (default: "gitbackup@localhost")
- **Exclude:N**: Patterns for files/directories to exclude (supports wildcards)

## Command Line Options

GitBackup uses the professional CommandLineParser library for robust argument parsing:

```
GitBackup [options]

Options:
  -c, --config <file>     Use specified configuration file (default: gitbackup.ini)
  --create-config         Create a sample configuration file
  -v, --version          Display detailed version information
  --verbose              Enable verbose output for debugging
  --dry-run              Show what would be backed up without performing backup
  --force                Force backup even if no changes are detected
  --help                 Display help screen

Examples:
  GitBackup                              # Use default gitbackup.ini
  GitBackup -c mybackup.ini             # Use custom config file
  GitBackup --create-config             # Create sample gitbackup.ini  
  GitBackup --create-config custom.ini  # Create sample with custom name
  GitBackup --version                   # Show detailed version info
  GitBackup --verbose --dry-run         # Verbose dry run
```

### New Features
- **Professional CLI parsing** using CommandLineParser library
- **Cross-platform compatibility** (Windows, Linux, macOS)
- **Robust argument validation** with helpful error messages
- **Verbose mode** for troubleshooting
- **Dry-run mode** to preview backup operations
- **Enhanced version information** with build details

## How It Works

1. **Initialization**: On first run, GitBackup creates a git repository in the backup directory
2. **File Scanning**: Scans the source directory for files, respecting exclusion patterns
3. **Incremental Copy**: Only copies files that have changed since the last backup
4. **Git Commit**: Stages all changes and creates a commit with timestamp
5. **History**: Each backup becomes a commit, creating a complete version history

## Using Git Tools

Since backups are stored as git repositories, you can use any git tool to:

- **View history**: `git log --oneline`
- **See changes**: `git diff HEAD~1`
- **Restore files**: `git checkout <commit-hash> -- <file>`
- **Browse versions**: Use any git GUI like GitKraken, SourceTree, or VS Code

## Building from Source

1. Clone the repository
2. Ensure you have .NET 8 SDK installed
3. Build the project:
   ```bash
   dotnet build
   ```
4. Run the application:
   ```bash
   dotnet run
   ```

### GitHub Actions CI/CD

The project includes a complete GitHub Actions workflow that:

- **Builds** on every push and pull request
- **Tests** automatically with Robot Framework on Windows and Linux
- **Increments build number** automatically using git commit count
- **Creates releases** on main/master branch pushes
- **Publishes artifacts** for Windows and Linux (x64)
- **Generates version information** in assemblies

The workflow creates releases with the format `v1.0.0.{build_number}` where the build number is the total number of commits in the repository.

#### Automated Testing with Robot Framework

Every commit triggers comprehensive testing on both platforms:

**Test Coverage:**
- ✅ **Command-line interface** validation
- ✅ **Configuration file** handling  
- ✅ **Cross-platform compatibility** (Windows/Linux)
- ✅ **File system operations** and permissions
- ✅ **Git repository** creation and management
- ✅ **Error handling** and edge cases
- ✅ **Dry-run and verbose modes**

**Test Execution:**
- **Windows**: Tests Windows-specific paths, file permissions, and system integration
- **Linux**: Tests Unix-style paths, symbolic links, hidden files, and case sensitivity
- **Reports**: HTML reports and logs uploaded as GitHub Actions artifacts

#### Local Testing

Run Robot Framework tests locally:

**Windows:**
```powershell
.\run-tests.ps1                    # Run all tests
.\run-tests.ps1 -TestSuite core    # Run core tests only
.\run-tests.ps1 -Verbose -DryRun   # Verbose dry run
```

**Linux/macOS:**
```bash
./run-tests.sh                     # Run all tests  
./run-tests.sh -s core             # Run core tests only
./run-tests.sh -v --dry-run        # Verbose dry run
```

**Manual Robot Framework:**
```bash
# Install dependencies
pip install -r tests/requirements.txt

# Run specific test suite
robot --outputdir test_results tests/gitbackup_tests.robot

# Run with custom variables
robot --variable GITBACKUP_EXE:./bin/Release/net8.0/GitBackup tests/
```

#### Local Testing

You can test the build process locally using the included PowerShell script:

```powershell
# Test with default build number
.\build-test.ps1

# Test with specific build number
.\build-test.ps1 -BuildNumber 123

# Test with version suffix (for pre-release)
.\build-test.ps1 -BuildNumber 456 -VersionSuffix "beta"
```

## Dependencies

- **.NET 8**: Runtime framework
- **LibGit2Sharp**: Git operations library
- **Microsoft.Extensions.Configuration.Ini**: INI file support

## License

This project is licensed under the MIT License.

## Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues for bugs and feature requests.
