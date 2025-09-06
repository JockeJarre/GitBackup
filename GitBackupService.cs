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
    /// Performs a backup by copying changed files and committing to git repository
    /// </summary>
    public async Task BackupAsync()
    {
        try
        {
            Console.WriteLine($"Starting backup from '{_config.RootDir}' to '{_config.BackupDir}'");

            // Ensure backup directory exists
            Directory.CreateDirectory(_config.BackupDir);

            // Initialize or open git repository
            var repoPath = _config.BackupDir;
            Repository repo;

            if (!Repository.IsValid(repoPath))
            {
                Console.WriteLine("Initializing new git repository...");
                repo = new Repository(Repository.Init(repoPath));
                await CreateGitIgnoreAsync(repoPath);
            }
            else
            {
                repo = new Repository(repoPath);
            }

            using (repo)
            {
                // Copy files from source to backup directory
                await CopyChangedFilesAsync(_config.RootDir, _config.BackupDir);

                // Stage all changes
                Commands.Stage(repo, "*");

                // Check if there are any changes to commit
                var status = repo.RetrieveStatus();
                if (!status.Any())
                {
                    Console.WriteLine("No changes detected. Backup is up to date.");
                    return;
                }

                // Create commit
                var signature = new Signature(_config.GitUserName, _config.GitUserEmail, DateTimeOffset.Now);
                var commitMessage = $"Backup created at {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                
                var commit = repo.Commit(commitMessage, signature, signature);
                Console.WriteLine($"Backup committed with hash: {commit.Sha[..8]}");
                Console.WriteLine($"Changes committed: {status.Count()} files");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Backup failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Copies files from source to destination, respecting exclude patterns
    /// </summary>
    private async Task CopyChangedFilesAsync(string sourceDir, string destDir)
    {
        var sourceInfo = new DirectoryInfo(sourceDir);
        var files = sourceInfo.GetFiles("*", SearchOption.AllDirectories);
        var copiedCount = 0;

        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(sourceDir, file.FullName);
            
            // Check if file should be excluded
            if (ShouldExcludeFile(relativePath))
                continue;

            var destPath = Path.Combine(destDir, relativePath);
            var destFile = new FileInfo(destPath);

            // Create directory if it doesn't exist
            destFile.Directory?.Create();

            // Copy if file is newer or doesn't exist
            if (!destFile.Exists || file.LastWriteTime > destFile.LastWriteTime)
            {
                await Task.Run(() => file.CopyTo(destPath, true));
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
    /// Creates a .gitignore file with default exclusions
    /// </summary>
    private async Task CreateGitIgnoreAsync(string repoPath)
    {
        var gitIgnorePath = Path.Combine(repoPath, ".gitignore");
        var defaultIgnorePatterns = new[]
        {
            "# GitBackup generated .gitignore",
            ".git/",
            "*.tmp",
            "*.temp",
            "Thumbs.db",
            ".DS_Store"
        };

        await File.WriteAllLinesAsync(gitIgnorePath, defaultIgnorePatterns);
        Console.WriteLine("Created default .gitignore file");
    }
}
