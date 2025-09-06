using LibGit2Sharp;
using System.Text;

namespace GitBackup;

/// <summary>
/// Core service for performing git-based backups
/// </summary>
public class GitBackupService
{
    private readonly GitBackupConfig _config;

    public GitBackupService(GitBackupConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// Performs a backup by creating git commits from source files
    /// </summary>
    public async Task BackupAsync()
    {
        try
        {
            Console.WriteLine($"Starting backup from '{_config.RootDir}' to '{_config.BackupDir}'");
            Console.WriteLine($"Repository type: {(_config.BareRepository ? "Bare" : "Standard")}");

            // Ensure backup directory exists
            Directory.CreateDirectory(_config.BackupDir);

            if (_config.BareRepository)
            {
                await BackupToBareRepositoryAsync();
            }
            else
            {
                await BackupToStandardRepositoryAsync();
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Backup failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Backs up to a bare repository (no working directory files)
    /// </summary>
    private async Task BackupToBareRepositoryAsync()
    {
        var repoPath = _config.BackupDir;
        var tempWorkingDir = Path.Combine(Path.GetTempPath(), $"gitbackup-{Guid.NewGuid():N}");
        
        try
        {
            // Initialize bare repository if it doesn't exist
            if (!Repository.IsValid(repoPath))
            {
                Console.WriteLine("Initializing new bare git repository...");
                Repository.Init(repoPath, isBare: true);
            }

            // Create temporary working directory
            Console.WriteLine("Creating temporary working directory for staging...");
            Directory.CreateDirectory(tempWorkingDir);
            
            // Initialize working repository
            var workingRepo = new Repository(Repository.Init(tempWorkingDir));
            
            using (workingRepo)
            {
                // Update .gitignore in working directory
                await UpdateGitIgnoreAsync(tempWorkingDir);

                // Copy files from source to working directory (excluding .git directories)
                await CopyChangedFilesAsync(_config.RootDir, tempWorkingDir);

                // Add bare repository as remote
                workingRepo.Network.Remotes.Add("origin", repoPath);

                // Try to fetch existing commits from bare repo (ignore if empty)
                try
                {
                    var remote = workingRepo.Network.Remotes["origin"];
                    Commands.Fetch(workingRepo, "origin", new string[0], null, null);
                    
                    // If there are remote branches, checkout the default branch
                    var remoteBranches = workingRepo.Branches.Where(b => b.IsRemote).ToList();
                    if (remoteBranches.Any())
                    {
                        var defaultRemoteBranch = remoteBranches.FirstOrDefault(b => b.FriendlyName.EndsWith("/main")) 
                                                ?? remoteBranches.FirstOrDefault(b => b.FriendlyName.EndsWith("/master"))
                                                ?? remoteBranches.First();
                        
                        var branchName = defaultRemoteBranch.FriendlyName.Split('/').Last();
                        var localBranch = workingRepo.CreateBranch(branchName, defaultRemoteBranch.Tip);
                        Commands.Checkout(workingRepo, localBranch);
                        Console.WriteLine($"Checked out existing branch: {branchName}");
                    }
                }
                catch (LibGit2SharpException)
                {
                    // Bare repository is empty, continue with initial commit
                    Console.WriteLine("Bare repository is empty, creating initial commit...");
                }

                // Stage and commit changes
                await CommitChangesAsync(workingRepo);

                // Push to bare repository
                if (workingRepo.Head.Commits.Any())
                {
                    var remote = workingRepo.Network.Remotes["origin"];
                    var currentBranch = workingRepo.Head.FriendlyName;
                    var pushRefSpec = $"refs/heads/{currentBranch}:refs/heads/{currentBranch}";
                    workingRepo.Network.Push(remote, pushRefSpec);
                    Console.WriteLine($"Changes pushed to bare repository (branch: {currentBranch})");
                }
            }
        }
        finally
        {
            // Clean up temporary working directory
            if (Directory.Exists(tempWorkingDir))
            {
                try
                {
                    // Remove read-only attributes that git might have set
                    var dirInfo = new DirectoryInfo(tempWorkingDir);
                    foreach (var file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
                    {
                        if (file.IsReadOnly)
                            file.IsReadOnly = false;
                    }
                    
                    Directory.Delete(tempWorkingDir, true);
                    Console.WriteLine("Cleaned up temporary working directory");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not clean up temporary directory: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Backs up to a standard repository (with working directory files)
    /// </summary>
    private async Task BackupToStandardRepositoryAsync()
    {
        var repoPath = _config.BackupDir;
        Repository repo;

        if (!Repository.IsValid(repoPath))
        {
            Console.WriteLine("Initializing new git repository...");
            repo = new Repository(Repository.Init(repoPath));
        }
        else
        {
            repo = new Repository(repoPath);
        }

        using (repo)
        {
            // Update .gitignore with exclude patterns
            await UpdateGitIgnoreAsync(repoPath);

            // Copy files from source to backup directory
            await CopyChangedFilesAsync(_config.RootDir, _config.BackupDir);

            // Commit changes
            await CommitChangesAsync(repo);
        }
    }

    /// <summary>
    /// Stages and commits changes in the repository
    /// </summary>
    private Task CommitChangesAsync(Repository repo)
    {
        // Stage all changes
        Commands.Stage(repo, "*");

        // Check if there are any changes to commit
        var status = repo.RetrieveStatus();
        if (!status.Any())
        {
            Console.WriteLine("No changes detected. Backup is up to date.");
            return Task.CompletedTask;
        }

        // Create commit
        var signature = new Signature(_config.GitUserName, _config.GitUserEmail, DateTimeOffset.Now);
        var commitMessage = $"Backup created at {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        
        var commit = repo.Commit(commitMessage, signature, signature);
        Console.WriteLine($"Backup committed with hash: {commit.Sha[..8]}");
        Console.WriteLine($"Changes committed: {status.Count()} files");
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Copies files from source to destination, respecting exclude patterns
    /// </summary>
    private async Task CopyChangedFilesAsync(string sourceDir, string destDir)
    {
        // Prevent copying when source and destination are the same
        var sourcePath = Path.GetFullPath(sourceDir);
        var destPath = Path.GetFullPath(destDir);
        
        if (sourcePath.Equals(destPath, StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Source and destination are the same directory, skipping file copy");
            return;
        }

        var sourceInfo = new DirectoryInfo(sourceDir);
        var files = sourceInfo.GetFiles("*", SearchOption.AllDirectories);
        var copiedCount = 0;

        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(sourceDir, file.FullName);
            
            // Check if file should be excluded
            if (ShouldExcludeFile(relativePath))
                continue;

            var destFilePath = Path.Combine(destDir, relativePath);
            var destFile = new FileInfo(destFilePath);

            // Create directory if it doesn't exist
            destFile.Directory?.Create();

            // Copy if file is newer or doesn't exist
            if (!destFile.Exists || file.LastWriteTime > destFile.LastWriteTime)
            {
                await Task.Run(() => file.CopyTo(destFilePath, true));
                copiedCount++;
            }
        }

        Console.WriteLine($"Copied {copiedCount} files to backup directory");
    }

    /// <summary>
    /// Checks if a file should be excluded based on configured patterns
    /// </summary>
    private bool ShouldExcludeFile(string relativePath)
    {
        // Convert backslashes to forward slashes for pattern matching
        var normalizedPath = relativePath.Replace('\\', '/');

        // Always exclude .git directories to prevent recursion issues
        if (normalizedPath.StartsWith(".git/") || normalizedPath.Contains("/.git/"))
            return true;

        foreach (var pattern in _config.ExcludePatterns)
        {
            if (IsMatch(normalizedPath, pattern))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Simple pattern matching for gitignore-style patterns
    /// </summary>
    private static bool IsMatch(string path, string pattern)
    {
        // Handle directory patterns ending with /
        if (pattern.EndsWith('/'))
        {
            var dirPattern = pattern[..^1];
            return path.Contains($"{dirPattern}/") || path.StartsWith($"{dirPattern}/");
        }

        // Handle wildcard patterns
        if (pattern.Contains('*'))
        {
            var regexPattern = "^" + pattern
                .Replace(".", "\\.")
                .Replace("*", ".*") + "$";
            return System.Text.RegularExpressions.Regex.IsMatch(path, regexPattern);
        }

        // Exact match or contains
        return path.Contains(pattern) || path.EndsWith(pattern);
    }

    /// <summary>
    /// Creates or updates .gitignore file with exclude patterns from configuration
    /// </summary>
    private async Task UpdateGitIgnoreAsync(string repoPath)
    {
        var gitIgnorePath = Path.Combine(repoPath, ".gitignore");
        
        var gitignoreContent = new List<string>
        {
            "# GitBackup generated .gitignore",
            "# Exclude patterns from configuration:",
            ""
        };

        // Add all exclude patterns from configuration
        foreach (var pattern in _config.ExcludePatterns)
        {
            if (!string.IsNullOrWhiteSpace(pattern))
            {
                gitignoreContent.Add(pattern);
            }
        }

        // Add some default patterns if no exclude patterns are configured
        if (!_config.ExcludePatterns.Any())
        {
            gitignoreContent.AddRange(new[]
            {
                "# Default exclusions",
                ".git/",
                "*.tmp",
                "*.temp",
                "Thumbs.db",
                ".DS_Store"
            });
        }

        await File.WriteAllLinesAsync(gitIgnorePath, gitignoreContent);
        Console.WriteLine($"Updated .gitignore with {_config.ExcludePatterns.Count} exclude patterns");
    }
}
