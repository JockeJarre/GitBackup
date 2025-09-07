using Salaros.Configuration;

namespace GitBackup;

/// <summary>
/// Configuration loader for GitBackup INI files using Salaros ConfigParser
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

        // Configure parser for maximum multi-line flexibility
        var configParser = new ConfigParser(iniFilePath, new ConfigParserSettings
        {
            MultiLineValues = MultiLineValues.Simple | MultiLineValues.AllowValuelessKeys | MultiLineValues.AllowEmptyTopSection,
            Encoding = System.Text.Encoding.UTF8
        });

        var config = new GitBackupConfig
        {
            RootDir = configParser.GetValue("GitBackup", "RootDir") ?? string.Empty,
            BackupDir = configParser.GetValue("GitBackup", "BackupDir") ?? string.Empty,
            GitUserName = configParser.GetValue("GitBackup", "GitUserName") ?? "GitBackup",
            GitUserEmail = configParser.GetValue("GitBackup", "GitUserEmail") ?? "gitbackup@localhost"
        };

        // Handle file size filtering
        var maxFileSizeValue = configParser.GetValue("GitBackup", "MaxFileSizeBytes");
        if (!string.IsNullOrEmpty(maxFileSizeValue))
        {
            config.MaxFileSizeBytes = ParseFileSizeToBytes(maxFileSizeValue);
        }

        var minFileSizeValue = configParser.GetValue("GitBackup", "MinFileSizeBytes");
        if (!string.IsNullOrEmpty(minFileSizeValue))
        {
            config.MinFileSizeBytes = ParseFileSizeToBytes(minFileSizeValue);
        }

        // Handle binary file exclusion
        var excludeBinaryValue = configParser.GetValue("GitBackup", "ExcludeBinaryFiles");
        if (bool.TryParse(excludeBinaryValue, out bool excludeBinary))
            config.ExcludeBinaryFiles = excludeBinary;
        else
            config.ExcludeBinaryFiles = false; // Default value

        // Load exclude patterns using Salaros enhanced capabilities
        config.ExcludePatterns = LoadExcludePatternsFromSalaros(configParser);

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
    /// Load exclude patterns using Salaros enhanced capabilities
    /// Supports multiple formats:
    /// 1. Single line with comma-separated values: Exclude=pattern1,pattern2,pattern3
    /// 2. Multiple numbered keys: Exclude1=pattern1, Exclude2=pattern2, etc.
    /// 3. Multi-line values: Patterns on separate lines after Exclude=
    /// </summary>
    private static List<string> LoadExcludePatternsFromSalaros(ConfigParser configParser)
    {
        var excludePatterns = new List<string>();

        // Debug: Check if Exclude value exists
        var excludeValue = configParser.GetValue("GitBackup", "Exclude");
        if (string.IsNullOrWhiteSpace(excludeValue))
        {
            Console.WriteLine("[DEBUG] No Exclude value found by Salaros");
        }

        // Method 1: Try single comma-separated format first
        if (!string.IsNullOrWhiteSpace(excludeValue))
        {
            // Check if this is a simple comma-separated line
            if (excludeValue.Contains(',') && !excludeValue.Contains('\n'))
            {
                excludePatterns.AddRange(
                    excludeValue.Split(',', StringSplitOptions.RemoveEmptyEntries)
                               .Select(p => p.Trim())
                               .Where(p => !string.IsNullOrEmpty(p))
                );
            }
            else if (excludeValue.Contains('\n'))
            {
                // Method 2: Multi-line format - split by newlines and process
                var lines = excludeValue.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (!string.IsNullOrEmpty(trimmedLine))
                    {
                        excludePatterns.Add(trimmedLine);
                    }
                }
            }
            else
            {
                // Single pattern
                excludePatterns.Add(excludeValue.Trim());
            }
        }

        // Method 3: Try numbered exclude keys (Exclude1, Exclude2, etc.) as fallback
        if (!excludePatterns.Any())
        {
            int keyIndex = 1;
            string numberedKey;
            while (!string.IsNullOrWhiteSpace(numberedKey = configParser.GetValue("GitBackup", $"Exclude{keyIndex}")))
            {
                excludePatterns.Add(numberedKey.Trim());
                keyIndex++;
                if (keyIndex > 100) // Safety limit
                {
                    break;
                }
            }
        }

        Console.WriteLine($"[DEBUG] Total patterns after filtering: {excludePatterns.Count}");
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
            RootDir=C:\Source\Directory
            
            # Destination directory for git backup repository
            BackupDir=C:\Backup\Directory
            
            # Git user information for commits
            GitUserName=GitBackup
            GitUserEmail=gitbackup@localhost
            
            # File size filtering (optional)
            # Maximum file size to include in backup (0 = no limit)
            # Supports units: B, KB, MB, GB, TB
            # MaxFileSizeBytes=100MB
            
            # Minimum file size to include in backup (0 = no limit)  
            # MinFileSizeBytes=1KB
            
            # Binary file exclusion (optional)
            # Exclude binary files and only backup text files
            # Default: false (include all files)
            # ExcludeBinaryFiles=true
            
            # Files and patterns to exclude from backup
            # Comma-separated format (recommended):
            # Exclude=*.tmp,*.log,.git/,node_modules/
            
            # Multi-line format (advanced - each pattern on separate line with indentation):
            # Exclude=*.tmp
            #     *.log
            #     .git/
            #     node_modules/
            #     bin/
            #     obj/
            
            # Default patterns (automatically included if no Exclude specified):
            Exclude=.git/,*.tmp,*.temp,Thumbs.db,.DS_Store
            """;
            
        File.WriteAllText(iniFilePath, sampleConfig);
        Console.WriteLine($"Sample configuration created: {iniFilePath}");
    }

    /// <summary>
    /// Parses file size string to bytes (e.g., "100MB" -> 104857600)
    /// </summary>
    private static long ParseFileSizeToBytes(string sizeString)
    {
        if (string.IsNullOrWhiteSpace(sizeString))
            return 0;

        sizeString = sizeString.Trim().ToUpperInvariant();
        
        // Extract number and unit
        var numberPart = "";
        var unitPart = "";
        
        foreach (char c in sizeString)
        {
            if (char.IsDigit(c) || c == '.')
                numberPart += c;
            else
                unitPart += c;
        }

        if (!double.TryParse(numberPart, out double number))
        {
            Console.WriteLine($"Warning: Could not parse file size number '{numberPart}' from '{sizeString}'");
            return 0;
        }

        // Convert based on unit
        return unitPart switch
        {
            "" or "B" => (long)number,
            "KB" => (long)(number * 1024),
            "MB" => (long)(number * 1024 * 1024),
            "GB" => (long)(number * 1024 * 1024 * 1024),
            "TB" => (long)(number * 1024L * 1024 * 1024 * 1024),
            _ => throw new ArgumentException($"Unknown file size unit: {unitPart}")
        };
    }
}
