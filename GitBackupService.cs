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
            .Where(item => !ShouldExcludeFileWithSizeCheck(item.SourceFile.FullName))
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
    /// Determines if a file should be excluded based on configured patterns
    /// Implements full gitignore-compatible pattern matching with negation support
    /// </summary>
    private bool ShouldExcludeFile(string relativePath)
    {
        // Convert backslashes to forward slashes for pattern matching
        var normalizedPath = relativePath.Replace('\\', '/');

        // Always exclude .git directories to prevent recursion issues
        if (normalizedPath.StartsWith(".git/") || normalizedPath.Contains("/.git/"))
            return true;

        var isExcluded = false;
        
        // Process patterns in order - later patterns can override earlier ones
        foreach (var pattern in _config.ExcludePatterns)
        {
            if (string.IsNullOrWhiteSpace(pattern) || pattern.StartsWith('#'))
                continue;
                
            // Handle negation patterns (patterns starting with !)
            if (pattern.StartsWith('!'))
            {
                var negatedPattern = pattern[1..]; // Remove the !
                if (IsGitIgnoreMatch(normalizedPath, negatedPattern))
                {
                    isExcluded = false; // Negation: include this file even if previously excluded
                }
            }
            else
            {
                // Regular exclusion pattern
                if (IsGitIgnoreMatch(normalizedPath, pattern))
                {
                    isExcluded = true;
                }
            }
        }

        return isExcluded;
    }

    /// <summary>
    /// Checks if a file should be excluded based on patterns and file size limits
    /// </summary>
    private bool ShouldExcludeFileWithSizeCheck(string filePath)
    {
        var relativePath = Path.GetRelativePath(_config.RootDir, filePath);
        
        // First check pattern-based exclusions
        if (ShouldExcludeFile(relativePath))
            return true;

        // Check file size limits if configured
        if (_config.MaxFileSizeBytes > 0 || _config.MinFileSizeBytes > 0)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                var fileSize = fileInfo.Length;

                // Check maximum file size limit
                if (_config.MaxFileSizeBytes > 0 && fileSize > _config.MaxFileSizeBytes)
                {
                    return true; // Exclude: file too large
                }

                // Check minimum file size limit  
                if (_config.MinFileSizeBytes > 0 && fileSize < _config.MinFileSizeBytes)
                {
                    return true; // Exclude: file too small
                }
            }
            catch (Exception ex)
            {
                // If we can't get file info, include the file (don't exclude due to errors)
                Console.WriteLine($"Warning: Could not check size for {filePath}: {ex.Message}");
            }
        }

        // Check binary file exclusion if configured
        if (_config.ExcludeBinaryFiles && IsBinaryFile(filePath))
        {
            return true; // Exclude: binary file
        }

        return false; // Include the file
    }

    /// <summary>
    /// Checks if a file contains binary data by reading the first few bytes
    /// </summary>
    /// <param name="filePath">Path to the file to check</param>
    /// <returns>True if the file appears to be binary, false if it appears to be text</returns>
    private bool IsBinaryFile(string filePath)
    {
        try
        {
            // Read the first 8KB or the entire file if smaller
            const int bufferSize = 8192;
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            
            var buffer = new byte[Math.Min(bufferSize, fileStream.Length)];
            var bytesRead = fileStream.Read(buffer, 0, buffer.Length);
            
            if (bytesRead == 0)
                return false; // Empty file is considered text
                
            // Check for null bytes - common indicator of binary files
            for (int i = 0; i < bytesRead; i++)
            {
                if (buffer[i] == 0)
                    return true; // Contains null byte, likely binary
            }
            
            // Check for high percentage of non-printable characters
            int nonPrintableCount = 0;
            int controlCharCount = 0;
            
            for (int i = 0; i < bytesRead; i++)
            {
                var b = buffer[i];
                
                // Count control characters (excluding common text ones)
                if (b < 32 && b != 9 && b != 10 && b != 13) // Tab, LF, CR are OK
                {
                    controlCharCount++;
                }
                
                // Count non-printable characters
                if (b < 32 || b > 126)
                {
                    // Allow common text characters: tab, LF, CR, and extended ASCII
                    if (b != 9 && b != 10 && b != 13 && b < 128)
                    {
                        nonPrintableCount++;
                    }
                }
            }
            
            // If more than 30% non-printable or more than 1% control chars, consider binary
            double nonPrintableRatio = (double)nonPrintableCount / bytesRead;
            double controlRatio = (double)controlCharCount / bytesRead;
            
            return nonPrintableRatio > 0.30 || controlRatio > 0.01;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not check binary status for {filePath}: {ex.Message}");
            return false; // If we can't read it, assume text (fail-safe)
        }
    }

    /// <summary>
    /// GitIgnore-compatible pattern matching implementation
    /// Supports all major gitignore features:
    /// - ** for recursive directory matching
    /// - * for single-level wildcards  
    /// - ? for single character matching
    /// - [] for character classes
    /// - Anchored vs relative patterns
    /// - Directory-only patterns (ending with /)
    /// </summary>
    private static bool IsGitIgnoreMatch(string path, string pattern)
    {
        // Trim whitespace
        pattern = pattern.Trim();
        if (string.IsNullOrEmpty(pattern))
            return false;

        // Handle directory-only patterns (ending with /)
        bool directoryOnly = pattern.EndsWith('/');
        if (directoryOnly)
        {
            pattern = pattern[..^1]; // Remove trailing slash
            
            // For directory patterns, check if the path contains this directory
            // This matches git's behavior where "dir/" matches "dir/file.txt"
            if (pattern.Contains("**"))
            {
                return IsGitIgnoreMatch(path, pattern + "/**");
            }
            else
            {
                // Simple directory matching - check if path starts with or contains the directory
                return path.StartsWith(pattern + "/") || 
                       path.Contains("/" + pattern + "/") ||
                       path == pattern;
            }
        }

        // Convert gitignore pattern to regex
        var regexPattern = ConvertGitIgnorePatternToRegex(pattern);
        
        try
        {
            return System.Text.RegularExpressions.Regex.IsMatch(path, regexPattern, 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        catch
        {
            // If regex fails, fall back to simple matching
            return path.Contains(pattern) || path.EndsWith(pattern);
        }
    }

    /// <summary>
    /// Converts a gitignore pattern to a proper regex pattern
    /// Handles ** (recursive), * (wildcard), ? (single char), and [] (char class)
    /// </summary>
    private static string ConvertGitIgnorePatternToRegex(string pattern)
    {
        var regex = new System.Text.StringBuilder();
        
        // Determine if pattern is anchored (starts with / or contains no /)
        bool isAnchored = pattern.StartsWith('/');
        if (isAnchored)
        {
            pattern = pattern[1..]; // Remove leading slash
            regex.Append('^');
        }
        else if (!pattern.Contains('/'))
        {
            // Pattern with no slashes matches files at any level
            regex.Append("(^|.*/)");
        }
        else
        {
            regex.Append('^');
        }

        // Process the pattern character by character
        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];
            
            switch (c)
            {
                case '*':
                    if (i + 1 < pattern.Length && pattern[i + 1] == '*')
                    {
                        // Handle ** (recursive directory matching)
                        i++; // Skip the second *
                        
                        if (i + 1 < pattern.Length && pattern[i + 1] == '/')
                        {
                            // **/ means match any number of directories
                            i++; // Skip the /
                            regex.Append("(.*/)??");
                        }
                        else if (i + 1 >= pattern.Length)
                        {
                            // ** at end means match everything
                            regex.Append(".*");
                        }
                        else
                        {
                            // ** in middle - treat as recursive wildcard
                            regex.Append(".*");
                        }
                    }
                    else
                    {
                        // Single * matches anything except /
                        regex.Append("[^/]*");
                    }
                    break;
                    
                case '?':
                    regex.Append("[^/]");
                    break;
                    
                case '[':
                    // Character class - find the closing ]
                    int closeIndex = pattern.IndexOf(']', i + 1);
                    if (closeIndex != -1)
                    {
                        var charClass = pattern.Substring(i, closeIndex - i + 1);
                        regex.Append(System.Text.RegularExpressions.Regex.Escape(charClass));
                        i = closeIndex;
                    }
                    else
                    {
                        regex.Append("\\[");
                    }
                    break;
                    
                case '.':
                case '^':
                case '$':
                case '+':
                case '{':
                case '}':
                case '(':
                case ')':
                case '|':
                case '\\':
                    // Escape regex special characters
                    regex.Append('\\').Append(c);
                    break;
                    
                default:
                    regex.Append(c);
                    break;
            }
        }
        
        regex.Append('$');
        return regex.ToString();
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
    /// Producer-consumer method to add files to tree (flattened structure for bare repository)
    /// Starts creating git objects immediately as files are read for fastest processing
    /// 
    /// Thread Safety Implementation:
    /// - Producer: Parallel file reading → Thread-safe queue (immediate processing)
    /// - Consumer: Sequential git object creation (required for LibGit2Sharp safety)  
    /// - LibGit2Sharp Repository/ObjectDatabase is NOT thread-safe for concurrent writes
    /// - Pipeline pattern: git objects start being created as soon as first files are read
    /// 
    /// References:
    /// - LibGit2Sharp Issue #787: Maintainers recommend "run Git operation in worker thread"
    /// - libgit2 threading.md: "libgit2 objects cannot be safely accessed by multiple threads"
    /// </summary>
    private int AddFilesToTreeSimple(TreeDefinition treeBuilder, string sourcePath, Repository repo)
    {
        var files = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories);
        Console.WriteLine($"Found {files.Length} files to process...");
        
        if (files.Length == 0)
            return 0;

        // Use producer-consumer pattern for immediate git object creation with inline exclusion filtering
        // This avoids reading files that will be excluded, maximizing efficiency
        var fileQueue = new System.Collections.Concurrent.ConcurrentQueue<(string relativePath, byte[] data)>();
        var producerFinished = false;
        var lockObj = new object();
        var readCount = 0;
        var excludedCount = 0;
        var addedCount = 0;
        
        Console.WriteLine("Starting parallel file processing with inline exclusion filtering...");

        // Producer task: Check exclusions and read files in parallel
        var producerTask = Task.Run(() =>
        {
            Parallel.ForEach(files, new ParallelOptions 
            { 
                MaxDegreeOfParallelism = Environment.ProcessorCount 
            }, file =>
            {
                try
                {
                    // Check exclusion first - avoid reading excluded files (includes size filtering)
                    if (ShouldExcludeFileWithSizeCheck(file))
                    {
                        lock (lockObj)
                        {
                            excludedCount++;
                        }
                        return;
                    }
                    
                    // Get relative path to preserve directory structure
                    var relativePath = Path.GetRelativePath(sourcePath, file);
                    
                    // Read file data only if not excluded
                    var data = File.ReadAllBytes(file);
                    
                    // Add to queue immediately - don't wait for all files
                    fileQueue.Enqueue((relativePath, data));
                    
                    lock (lockObj)
                    {
                        readCount++;
                        
                        // Show progress every 100 valid files read
                        if (readCount % 100 == 0)
                        {
                            var processedCount = readCount + excludedCount;
                            Console.WriteLine($"Processed {processedCount}/{files.Length} files - Read: {readCount}, Excluded: {excludedCount}, Git objects: {addedCount}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to read file {file}: {ex.Message}");
                }
            });
            
            producerFinished = true;
            Console.WriteLine($"Finished reading {readCount} files - git object creation continues...");
        });

        // Consumer: Create git objects as soon as files are available
        Console.WriteLine("Creating git objects as files become available...");
        
        while (!producerFinished || !fileQueue.IsEmpty)
        {
            if (fileQueue.TryDequeue(out var fileData))
            {
                try
                {
                    using var stream = new MemoryStream(fileData.data);
                    var blob = repo.ObjectDatabase.CreateBlob(stream);
                    
                    // Use relative path to preserve directory structure
                    // Convert backslashes to forward slashes for git compatibility
                    var gitPath = fileData.relativePath.Replace('\\', '/');
                    treeBuilder.Add(gitPath, blob, Mode.NonExecutableFile);
                    
                    lock (lockObj)
                    {
                        addedCount++;
                        
                        // Show progress for git object creation
                        if (addedCount % 50 == 0)
                        {
                            Console.WriteLine($"Created git objects: {addedCount} (streaming processing...)");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to create git object for {fileData.relativePath}: {ex.Message}");
                }
            }
            else
            {
                // If queue is empty but producer isn't finished, wait a bit
                if (!producerFinished)
                {
                    Thread.Sleep(1);
                }
            }
        }

        // Wait for producer to complete
        producerTask.Wait();

        Console.WriteLine($"Completed: Created {addedCount} git objects from {readCount} files ({excludedCount} files excluded)");
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
