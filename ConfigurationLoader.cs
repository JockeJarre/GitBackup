using Microsoft.Extensions.Configuration;

namespace GitBackup;

/// <summary>
/// Configuration loader for GitBackup INI files
/// </summary>
public class ConfigurationLoader
{
    /// <summary>
    /// Loads configuration from an INI file
    /// </summary>
    public static GitBackupConfig LoadFromIni(string iniFilePath)
    {
        if (!File.Exists(iniFilePath))
        {
            throw new FileNotFoundException($"Configuration file not found: {iniFilePath}");
        }

        var configuration = new ConfigurationBuilder()
            .AddIniFile(iniFilePath)
            .Build();

        var config = new GitBackupConfig
        {
            RootDir = configuration["GitBackup:RootDir"] ?? string.Empty,
            BackupDir = configuration["GitBackup:BackupDir"] ?? string.Empty,
            GitUserName = configuration["GitBackup:GitUserName"] ?? "GitBackup",
            GitUserEmail = configuration["GitBackup:GitUserEmail"] ?? "gitbackup@localhost"
        };

        // Load exclude patterns
        var excludeSection = configuration.GetSection("GitBackup:Exclude");
        if (excludeSection.Exists())
        {
            config.ExcludePatterns = excludeSection.GetChildren()
                .Select(x => x.Value ?? string.Empty)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
        }

        // Add default exclude patterns if none specified
        if (!config.ExcludePatterns.Any())
        {
            config.ExcludePatterns = new List<string>
            {
                ".git/",
                "*.tmp",
                "*.temp",
                "Thumbs.db",
                ".DS_Store"
            };
        }

        return config;
    }

    /// <summary>
    /// Creates a sample INI configuration file
    /// </summary>
    public static void CreateSampleConfig(string iniFilePath)
    {
        var sampleConfig = """
            [GitBackup]
            # Source directory to backup
            RootDir=C:\MyDocuments
            
            # Destination directory for backups
            BackupDir=C:\Backups\MyDocuments
            
            # Git commit author information
            GitUserName=GitBackup
            GitUserEmail=gitbackup@localhost
            
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
            """;

        File.WriteAllText(iniFilePath, sampleConfig);
        Console.WriteLine($"Sample configuration file created: {iniFilePath}");
    }
}
