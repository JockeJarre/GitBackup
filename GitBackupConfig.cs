namespace GitBackup;

/// <summary>
/// Configuration class for GitBackup application
/// </summary>
public class GitBackupConfig
{
    /// <summary>
    /// Root directory to backup (source)
    /// </summary>
    public string RootDir { get; set; } = string.Empty;

    /// <summary>
    /// Directory where backups will be stored (destination)
    /// </summary>
    public string BackupDir { get; set; } = string.Empty;

    /// <summary>
    /// Git user name for commits
    /// </summary>
    public string GitUserName { get; set; } = "GitBackup";

    /// <summary>
    /// Git user email for commits
    /// </summary>
    public string GitUserEmail { get; set; } = "gitbackup@localhost";

    /// <summary>
    /// Files and directories to exclude from backup (gitignore patterns)
    /// </summary>
    public List<string> ExcludePatterns { get; set; } = new();

    /// <summary>
    /// Whether to create a bare repository (no working directory files)
    /// Default is true for space-efficient backups
    /// </summary>
    public bool BareRepository { get; set; } = true;

    /// <summary>
    /// Validates the configuration
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(RootDir))
            throw new ArgumentException("RootDir cannot be empty");
            
        if (string.IsNullOrWhiteSpace(BackupDir))
            throw new ArgumentException("BackupDir cannot be empty");
            
        if (!Directory.Exists(RootDir))
            throw new DirectoryNotFoundException($"RootDir does not exist: {RootDir}");
    }
}
