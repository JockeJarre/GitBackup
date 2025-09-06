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
- **`ConfigurationLoader.cs`** - INI file loading with dual parser system (Salaros + Microsoft fallback) and enhanced multi-line support

### Multi-Line Configuration Parser Implementation

The enhanced ConfigurationLoader uses **Salaros ConfigParser** as primary parser with Microsoft Extensions Configuration as fallback:

```csharp
private static GitBackupConfig LoadFromIniWithSalaros(string iniFilePath)
{
    var configParser = new ConfigParser(iniFilePath);
    // Load configuration with multi-line support
    // Falls back to Microsoft parser if Salaros fails
}

private static List<string> LoadExcludePatternsFromSalaros(ConfigParser parser)
{
    // Method 1: Comma-separated format
    // Method 2: Numbered format (Exclude1, Exclude2, etc.)
    // Method 3: Multi-line format (line-by-line parsing)
}
```
- **`CommandLineOptions.cs`** - CLI argument definitions

### Design Patterns
- **Dependency Injection** - Configuration passed to services
- **Factory Pattern** - Configuration creation from files
- **Command Pattern** - CLI options drive application behavior

## Configuration System

### Enhanced INI File Support with Salaros ConfigParser

GitBackup now uses **Salaros ConfigParser 0.3.8** as the primary INI parser, providing enhanced multi-line configuration support with automatic fallback to Microsoft Extensions Configuration for compatibility.

### Key Configuration Principles
- **Always validate** configuration before use
- **Provide helpful defaults** for optional settings
- **Support both relative and absolute paths**
- **Handle cross-platform path differences**
- **Multi-line patterns** for complex exclusion configurations

### Configuration File Formats

#### 1. Traditional Single-Line Format
```ini
[GitBackup]
RootDir=C:\Source\Directory
BackupDir=C:\Backup\Directory
Exclude=*.tmp,*.log,.git/,node_modules/
```

#### 2. Enhanced Multi-Line Format (Salaros ConfigParser)
```ini
[GitBackup]
RootDir=C:\Source\Directory
BackupDir=C:\Backup\Directory

# Multi-line exclusion patterns (one per line)
Exclude=*.vpx
*.directb2s
*.exe
**/assets
**/Music
!VPMAlias.txt
**/*DMD*/
*.png
**Cache
```

#### 3. Numbered Format (Enhanced Organization)
```ini
[GitBackup]
# Build outputs
Exclude1=bin/
Exclude2=obj/
# Dependencies  
Exclude3=node_modules/
Exclude4=packages/
# IDE files
Exclude5=.vscode/
Exclude6=.idea/
```

#### 4. Hybrid Format (Best of All Approaches)
```ini
[GitBackup]
# Quick comma-separated basics
Exclude=*.tmp,*.log,**/.git/

# Additional numbered patterns for organization
Exclude1=bin/
Exclude2=obj/
Exclude3=node_modules/

# Complex multi-line patterns when needed
ExcludeAdvanced=**/assets
**/Music
!important-file.txt
**/*DMD*/
```

## Pattern Matching & Exclusion System

### GitIgnore-Compatible Pattern Matching
GitBackup implements **full gitignore-compatible pattern matching** in `ShouldExcludeFile()` and `IsGitIgnoreMatch()` methods, supporting all major gitignore features:

#### Supported Pattern Types
- **Negation Patterns**: `!filename.txt` - Include files even if previously excluded
- **Recursive Directory Matching**: `**/dir`, `**/*.ext` - Match directories at any level
- **Single-Level Wildcards**: `*.dll`, `temp*` - Match files with wildcard patterns
- **Directory-Only Patterns**: `node_modules/`, `bin/` - Match directories specifically
- **Complex Patterns**: `**/*DMD*/`, `**/doc*` - Advanced recursive matching
- **Character Classes**: `[abc]`, `[0-9]` - Match specific character sets
- **Single Character**: `?` - Match any single character except `/`

#### Pattern Processing Rules
- **Order matters**: Later patterns can override earlier ones
- **Negation support**: `!pattern` includes files that were previously excluded
- **Path normalization**: Backslashes converted to forward slashes for cross-platform compatibility
- **Anchored vs relative**: Patterns without `/` match at any directory level
- **Case insensitive**: Pattern matching ignores case differences

#### Implementation Features
- **Full regex conversion**: Gitignore patterns converted to proper regex with escape handling
- **Error resilience**: Falls back to simple matching if regex fails
- **Performance optimized**: Inline exclusion checking before file I/O
- **Thread-safe**: Pattern matching works correctly in producer-consumer architecture

#### Configuration Examples
```ini
# Basic exclusions
Exclude=*.tmp,*.log,.git/

# Advanced gitignore-style patterns
Exclude=**/*.dll,**/bin/,**/obj/,!important.dll,build/**/cache/

# Visual Pinball example (real-world multi-line usage)
Exclude=*.vpx
*.directb2s
*.exe
**/assets
**/Music
!VPMAlias.txt
!ScreenRes.txt
**/*DMD*/
**/doc*
**Cache
*.png
*.pdf
```

#### Migration Notes
- **Legacy patterns**: Old simple patterns continue to work
- **Enhanced exclusion**: Complex directories now properly excluded (72% vs 46% in Visual Pinball example)
- **Negation patterns**: Previously unsupported `!pattern` syntax now fully functional

## Git Integration Guidelines

### LibGit2Sharp Usage
- **Always use `using` statements** for Repository objects
- **Initialize repositories** only in backup directories
- **Create meaningful commit messages** with timestamps
- **Handle git errors gracefully** with user-friendly messages
- **Support custom git user information**

### Backup Process Flow (Optimized)
1. Validate configuration
2. Ensure backup directory exists  
3. Initialize/open git repository
4. **Parallel file discovery** with `Directory.GetFiles()`
5. **Producer-consumer processing**:
   - **Producer**: Parallel exclusion filtering + file reading
   - **Consumer**: Sequential git object creation
6. **Streaming git operations** (immediate processing)
7. Create commit with metadata
8. Report results with performance metrics

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

### Core Performance Optimizations Implemented

#### 1. Bare Repository Architecture (Zero-Copy Backup)
- **Technique**: Direct git object creation without temporary files
- **Implementation**: Files read once and converted directly to git objects in memory
- **Benefit**: Eliminates file copying overhead entirely
- **Usage**: `BareRepository=true` in configuration

#### 2. Producer-Consumer Pattern with Concurrent Processing
- **Architecture**: Parallel file reading + Sequential git object creation  
- **Producer**: Multiple threads using `Parallel.ForEach()` with `MaxDegreeOfParallelism = Environment.ProcessorCount`
- **Consumer**: Single-threaded git operations (LibGit2Sharp thread safety requirement)
- **Implementation**: `ConcurrentQueue<(string flatName, byte[] data)>` for thread-safe communication
- **Performance**: **6.6x improvement** (48.6s → 7.33s for 400 files)

#### 3. Inline Exclusion Filtering  
- **Optimization**: Check `ShouldExcludeFile()` BEFORE reading file data
- **Benefit**: Avoids disk I/O on excluded files (logs, temp files, node_modules)
- **Implementation**: Early return in parallel loop prevents unnecessary `File.ReadAllBytes()`
- **Result**: Large excluded files never consume memory or disk bandwidth

#### 4. Thread-Safe Pipeline Processing
- **Pattern**: Git objects start being created as soon as first files are read
- **Benefit**: No waiting for all files to be read before git operations begin  
- **Safety**: `lock (lockObj)` for progress counters, sequential git operations for LibGit2Sharp

#### 5. Memory-Efficient Streaming
- **Implementation**: `MemoryStream` for git blob creation with immediate disposal
- **Pattern**: Read file → Create MemoryStream → Create git blob → Dispose
- **Benefit**: No accumulation of all file data in memory

### Performance Results Achieved
- **Processing Time**: 48.6s → 7.33s (**6.6x faster**)
- **File Copying**: Eliminated entirely (bare repository)
- **I/O Efficiency**: Excluded files never read (inline filtering)
- **Pipeline**: Immediate git object creation (producer-consumer)

### LibGit2Sharp Thread Safety Guidelines
- **Git operations must be sequential** (LibGit2Sharp/libgit2 limitation)
- **File I/O can be parallel** (reading, exclusion checking)
- **Use producer-consumer pattern** to separate parallel I/O from sequential git operations
- **Reference**: LibGit2Sharp Issue #787, libgit2 threading documentation

### File Operations Best Practices
- **Check exclusions before reading** files to minimize I/O
- **Use parallel file enumeration** with `Directory.GetFiles()`
- **Implement incremental processing** with producer-consumer queues
- **Handle large directories** with streaming rather than bulk loading
- **Monitor progress** with thread-safe counters

### Git Operations Best Practices
- **Use LibGit2Sharp efficiently** with proper `using` statements
- **Create git objects immediately** when file data becomes available
- **Use MemoryStream for blobs** rather than temporary files
- **Monitor repository performance** with progress reporting
- **Consider git garbage collection** for very large repositories

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
