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
            GitUserEmail = configuration["GitBackup:GitUserEmail"] ?? "gitbackup@localhost",
            BareRepository = bool.Parse(configuration["GitBackup:BareRepository"] ?? "true")
        };

        // Load exclude patterns - support multiple formats
        config.ExcludePatterns = LoadExcludePatterns(configuration);

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
    /// Loads exclude patterns from configuration, supporting multiple formats:
    /// 1. Single line with comma-separated values: Exclude=pattern1,pattern2,pattern3
    /// 2. Multiple numbered keys: Exclude1=pattern1, Exclude2=pattern2, etc.
    /// 3. Multiple lines with same key (if INI provider supports it)
    /// </summary>
    private static List<string> LoadExcludePatterns(IConfiguration configuration)
    {
        var excludePatterns = new List<string>();
        
        // Method 1: Try single comma-separated format first
        var excludeValue = configuration["GitBackup:Exclude"];
        if (!string.IsNullOrWhiteSpace(excludeValue))
        {
            var commaSeparated = excludeValue.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p) && !p.StartsWith('#'));
            excludePatterns.AddRange(commaSeparated);
        }
        
        // Method 2: Try numbered format (Exclude1, Exclude2, etc.)
        if (!excludePatterns.Any())
        {
            for (int i = 1; i <= 50; i++) // Reasonable limit
            {
                var patternValue = configuration[$"GitBackup:Exclude{i}"];
                if (!string.IsNullOrWhiteSpace(patternValue))
                {
                    excludePatterns.Add(patternValue.Trim());
                }
                else if (i > 10) // Stop if we've checked 10 consecutive empty slots
                {
                    break;
                }
            }
        }
        
        return excludePatterns;
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
            
            # Create bare repository (true) or repository with working files (false)
            # Bare repositories store only git history without copying files to backup directory
            # Default: true (space-efficient, git history only)
            BareRepository=true
            
            # Files and patterns to exclude from backup
            # Comma-separated format (recommended):
            Exclude=.git/,*.tmp,*.temp,*.log,Thumbs.db,.DS_Store,node_modules/,bin/,obj/
            
            # Alternative numbered format also supported:
            # Exclude1=.git/
            # Exclude2=*.tmp
            # Exclude3=*.temp
            # Exclude4=node_modules/
            """;

        File.WriteAllText(iniFilePath, sampleConfig);
        Console.WriteLine($"Sample configuration file created: {iniFilePath}");
    }
}
