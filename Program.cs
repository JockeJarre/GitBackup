using CommandLine;
using GitBackup;
using System.Reflection;

namespace GitBackup;

class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            var version = GetVersionInfo();
            Console.WriteLine($"GitBackup - Git-based Directory Backup Tool v{version}");
            Console.WriteLine("==========================================");
            Console.WriteLine();

            // Parse command line arguments using CommandLineParser
            var parserResult = Parser.Default.ParseArguments<CommandLineOptions>(args);
            
            return await parserResult.MapResult(
                async (CommandLineOptions options) => await RunWithOptionsAsync(options),
                errors => Task.FromResult(HandleParseErrors(errors))
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
            return 1;
        }
    }

    private static async Task<int> RunWithOptionsAsync(CommandLineOptions options)
    {
        try
        {
            // Handle version request
            if (options.ShowVersion)
            {
                Console.WriteLine($"GitBackup version {GetVersionInfo()}");
                Console.WriteLine($"Build: {GetBuildInfo()}");
                Console.WriteLine($".NET Runtime: {Environment.Version}");
                Console.WriteLine($"OS: {Environment.OSVersion}");
                Console.WriteLine($"Platform: {(Environment.Is64BitProcess ? "x64" : "x86")}");
                return 0;
            }

            // Handle create config request
            if (options.CreateConfigFlag)
            {
                var configFile = !string.IsNullOrEmpty(options.CreateConfigFile) ? options.CreateConfigFile : "gitbackup.ini";
                ConfigurationLoader.CreateSampleConfig(configFile);
                return 0;
            }

            // Load and validate configuration
            if (!File.Exists(options.ConfigFile))
            {
                Console.WriteLine($"Configuration file not found: {options.ConfigFile}");
                Console.WriteLine($"Use 'GitBackup --create-config' to create a sample configuration file.");
                return 1;
            }

            var config = ConfigurationLoader.LoadFromIni(options.ConfigFile);
            config.Validate();

            if (options.Verbose)
            {
                Console.WriteLine($"Configuration loaded from: {options.ConfigFile}");
                Console.WriteLine($"Source: {config.RootDir}");
                Console.WriteLine($"Backup: {config.BackupDir}");
                Console.WriteLine($"Git User: {config.GitUserName} <{config.GitUserEmail}>");
                Console.WriteLine($"Exclude patterns: {config.ExcludePatterns.Count}");
                if (config.ExcludePatterns.Any())
                {
                    foreach (var pattern in config.ExcludePatterns)
                    {
                        Console.WriteLine($"  - {pattern}");
                    }
                }
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine($"Configuration: {options.ConfigFile}");
                Console.WriteLine($"Source: {config.RootDir}");
                Console.WriteLine($"Backup: {config.BackupDir}");
                Console.WriteLine();
            }

            // Perform backup
            var backupService = new GitBackupService(config);
            
            if (options.DryRun)
            {
                Console.WriteLine("DRY RUN MODE - No actual backup will be performed");
                Console.WriteLine();
                // Note: We'd need to add dry-run support to GitBackupService
                Console.WriteLine("Dry run functionality would be implemented in GitBackupService");
                return 0;
            }

            await backupService.BackupAsync();

            Console.WriteLine();
            Console.WriteLine("Backup completed successfully!");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            if (options.Verbose && ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            return 1;
        }
    }

    private static int HandleParseErrors(IEnumerable<Error> errors)
    {
        var errorsList = errors.ToList();
        
        // Don't show error messages for help or version requests
        if (errorsList.Any(e => e.Tag == ErrorType.HelpRequestedError || e.Tag == ErrorType.VersionRequestedError))
        {
            return 0;
        }

        Console.WriteLine("Command line parsing failed:");
        foreach (var error in errorsList)
        {
            Console.WriteLine($"  {error}");
        }
        
        return 1;
    }

    private static string GetVersionInfo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        
        // Try to get the informational version first (includes build info)
        var infoVersionAttr = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (infoVersionAttr != null && !string.IsNullOrWhiteSpace(infoVersionAttr.InformationalVersion))
        {
            return infoVersionAttr.InformationalVersion;
        }
        
        // Fall back to file version
        var fileVersionAttr = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
        if (fileVersionAttr != null && !string.IsNullOrWhiteSpace(fileVersionAttr.Version))
        {
            return fileVersionAttr.Version;
        }
        
        // Final fallback to assembly version
        var version = assembly.GetName().Version;
        return version?.ToString() ?? "Unknown";
    }

    private static string GetBuildInfo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        
        // Get build date from assembly
        var buildDate = File.GetCreationTime(assembly.Location);
        
        // Get file version
        var fileVersionAttr = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
        var fileVersion = fileVersionAttr?.Version ?? "Unknown";
        
        return $"{fileVersion} (built {buildDate:yyyy-MM-dd HH:mm:ss})";
    }
}
