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
    /// Backs up to a bare repository (no working directory files, no temporary files)
    /// </summary>
    private Task BackupToBareRepositoryAsync()
    {
        var repoPath = _config.BackupDir;
        
        // Initialize bare repository if it doesn't exist
        if (!Repository.IsValid(repoPath))
        {
            Console.WriteLine("Initializing new bare git repository...");
            Repository.Init(repoPath, isBare: true);
        }

        using (var repo = new Repository(repoPath))
        {
            Console.WriteLine("Creating git objects directly from source files (no file copying)...");
            
            // For simplicity, let's create a single commit with all files as blobs at the root
            // This is a simplified approach that avoids the complex TreeDefinition API
            var treeBuilder = new TreeDefinition();
            int fileCount = 0;
            
            fileCount = AddFilesToTreeSimple(treeBuilder, _config.RootDir, repo);
            
            if (fileCount == 0)
            {
                Console.WriteLine("No files found to backup (all excluded or empty directory).");
                return Task.CompletedTask;
            }
            
            // Create the tree object
            var tree = repo.ObjectDatabase.CreateTree(treeBuilder);
            Console.WriteLine($"Created git tree with {fileCount} files");
            
            // Check if this tree is different from the last commit
            if (repo.Head?.Tip?.Tree?.Sha == tree.Sha)
            {
                Console.WriteLine("No changes detected. Backup is up to date.");
                return Task.CompletedTask;
            }
            
            // Create commit
            var signature = new Signature(_config.GitUserName, _config.GitUserEmail, DateTimeOffset.Now);
            var commitMessage = $"Backup created at {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            
            // Get parent commit (if any)
            Commit? parentCommit = null;
            if (repo.Head?.Tip != null)
            {
                parentCommit = repo.Head.Tip;
            }
            
            // Create commit
            Commit commit;
            if (parentCommit != null)
            {
                commit = repo.ObjectDatabase.CreateCommit(signature, signature, commitMessage, tree, new[] { parentCommit }, false);
            }
            else
            {
                commit = repo.ObjectDatabase.CreateCommit(signature, signature, commitMessage, tree, Enumerable.Empty<Commit>(), false);
                
                // For the first commit, we need to create the default branch
                repo.Refs.Add("refs/heads/main", commit.Id);
                repo.Refs.UpdateTarget("HEAD", "refs/heads/main");
            }
            
            // Update the branch reference
            var branchName = repo.Head?.FriendlyName == "HEAD" ? "main" : (repo.Head?.FriendlyName ?? "main");
            var refName = $"refs/heads/{branchName}";
            
            if (repo.Refs[refName] != null)
            {
                repo.Refs.UpdateTarget(repo.Refs[refName], commit.Sha);
            }
            else
            {
                repo.Refs.Add(refName, commit.Id);
                if (repo.Head?.FriendlyName == "HEAD")
                {
                    repo.Refs.UpdateTarget(repo.Refs["HEAD"]!, refName);
                }
            }
            
            Console.WriteLine($"Backup committed with hash: {commit.Sha[..8]}");
            Console.WriteLine($"Changes committed: {fileCount} files");
        }
        
        return Task.CompletedTask;
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
    /// Copies files from source to destination with parallel processing, respecting exclude patterns
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
        Console.WriteLine($"Found {files.Length} files to evaluate for copying...");

        // Filter and prepare file operations in parallel
        var filesToCopy = files.AsParallel()
            .Select(file => new
            {
                SourceFile = file,
                RelativePath = Path.GetRelativePath(sourceDir, file.FullName),
                DestPath = Path.Combine(destDir, Path.GetRelativePath(sourceDir, file.FullName))
            })
            .Where(item => !ShouldExcludeFile(item.RelativePath))
            .Select(item => new
            {
                item.SourceFile,
                item.RelativePath,
                item.DestPath,
                DestFile = new FileInfo(item.DestPath),
                ShouldCopy = !File.Exists(item.DestPath) || item.SourceFile.LastWriteTime > new FileInfo(item.DestPath).LastWriteTime
            })
            .Where(item => item.ShouldCopy)
            .ToArray();

        Console.WriteLine($"Processing {filesToCopy.Length} files that need copying...");

        if (filesToCopy.Length == 0)
        {
            Console.WriteLine("No files need to be copied");
            return;
        }

        // Copy files in parallel (safe - independent file operations)
        var copiedCount = 0;
        var lockObj = new object();

        await Task.Run(() =>
        {
            Parallel.ForEach(filesToCopy, new ParallelOptions 
            { 
                MaxDegreeOfParallelism = Environment.ProcessorCount 
            }, item =>
            {
                try
                {
                    // Create directory if it doesn't exist
                    item.DestFile.Directory?.Create();

                    // Copy file
                    item.SourceFile.CopyTo(item.DestPath, true);

                    lock (lockObj)
                    {
                        copiedCount++;
                        
                        // Show progress every 50 files or at the end
                        if (copiedCount % 50 == 0 || copiedCount == filesToCopy.Length)
                        {
                            Console.WriteLine($"Copied {copiedCount}/{filesToCopy.Length} files ({(copiedCount * 100.0 / filesToCopy.Length):F1}%)");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to copy {item.RelativePath}: {ex.Message}");
                }
            });
        });

        Console.WriteLine($"Completed copying {copiedCount} files to backup directory");
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

    /// <summary>
    /// Multi-threaded method to add files to tree (flattened structure for bare repository)
    /// 
    /// Thread Safety Implementation:
    /// - Parallel file reading: Safe (independent disk I/O operations)
    /// - Sequential git object creation: Required for LibGit2Sharp thread safety
    /// - LibGit2Sharp Repository/ObjectDatabase is NOT thread-safe for concurrent writes
    /// - Underlying libgit2 git_odb has internal locking but we avoid concurrent access
    /// </summary>
    private int AddFilesToTreeSimple(TreeDefinition treeBuilder, string sourcePath, Repository repo)
    {
        var files = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories);
        Console.WriteLine($"Found {files.Length} files to process...");
        
        // Filter files in parallel
        var validFiles = files.AsParallel()
            .Where(file => !ShouldExcludeFile(file))
            .ToArray();
            
        Console.WriteLine($"Processing {validFiles.Length} files after exclusions...");
        
        if (validFiles.Length == 0)
            return 0;

        // Process files in parallel to prepare blob data
        var fileDataList = new List<(string flatName, byte[] data)>(validFiles.Length);
        var lockObj = new object();
        var processed = 0;
        
        Console.WriteLine("Reading files in parallel...");
        
        // Read files in parallel (safe - only reading from disk)
        Parallel.ForEach(validFiles, new ParallelOptions 
        { 
            MaxDegreeOfParallelism = Environment.ProcessorCount 
        }, file =>
        {
            try
            {
                // Create a flattened filename (replace path separators with underscores)
                var relativePath = Path.GetRelativePath(sourcePath, file);
                var flatName = relativePath.Replace(Path.DirectorySeparatorChar, '_').Replace(Path.AltDirectorySeparatorChar, '_');
                
                // Read file data
                var data = File.ReadAllBytes(file);
                
                // Thread-safe addition to collection
                lock (lockObj)
                {
                    fileDataList.Add((flatName, data));
                    processed++;
                    
                    // Show progress every 100 files or at the end
                    if (processed % 100 == 0 || processed == validFiles.Length)
                    {
                        Console.WriteLine($"Processed {processed}/{validFiles.Length} files ({(processed * 100.0 / validFiles.Length):F1}%)");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to process file {file}: {ex.Message}");
            }
        });
        
        Console.WriteLine("Creating git objects sequentially...");
        
        // Create git objects sequentially for thread safety
        // LibGit2Sharp Repository/ObjectDatabase operations are not thread-safe
        // but git_odb (underlying object database) has internal locking
        int addedCount = 0;
        foreach (var (flatName, data) in fileDataList)
        {
            try
            {
                using var stream = new MemoryStream(data);
                var blob = repo.ObjectDatabase.CreateBlob(stream);
                treeBuilder.Add(flatName, blob, Mode.NonExecutableFile);
                addedCount++;
                
                // Show progress for git object creation
                if (addedCount % 200 == 0 || addedCount == fileDataList.Count)
                {
                    Console.WriteLine($"Created git objects: {addedCount}/{fileDataList.Count} ({(addedCount * 100.0 / fileDataList.Count):F1}%)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to create git object for {flatName}: {ex.Message}");
            }
        }
        
        return addedCount;
    }

    /// <summary>
    /// Counts the total number of entries in a tree definition by creating a temporary tree
    /// </summary>
    private int CountTreeEntries(TreeDefinition treeDefinition)
    {
        // We can't directly count TreeDefinition entries, so we estimate based on the backup
        return 1; // Placeholder - actual count will be shown after tree creation
    }

    /// <summary>
    /// Counts changed files between two trees
    /// </summary>
    private int CountChangedFiles(Tree? oldTree, Tree newTree, Repository repo)
    {
        if (oldTree == null)
        {
            // First commit - count all files in new tree
            return CountTreeFiles(newTree);
        }
        
        var changes = repo.Diff.Compare<TreeChanges>(oldTree, newTree);
        return changes.Count();
    }

    /// <summary>
    /// Recursively counts files in a tree
    /// </summary>
    private int CountTreeFiles(Tree tree)
    {
        int count = 0;
        foreach (var entry in tree)
        {
            if (entry.TargetType == TreeEntryTargetType.Blob)
            {
                count++;
            }
            else if (entry.TargetType == TreeEntryTargetType.Tree)
            {
                var subTree = (Tree)entry.Target;
                count += CountTreeFiles(subTree);
            }
        }
        return count;
    }
}
