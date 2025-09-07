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

            // Handle git command specially to avoid argument parsing conflicts
            if (args.Length > 0 && args[0] == "git")
            {
                return await HandleGitCommandAsync(args);
            }

            // Parse command line arguments using CommandLineParser with verb support
            var parserResult = Parser.Default.ParseArguments<BackupCommand>(args);
            
            return await parserResult.MapResult(
                async (BackupCommand cmd) => await RunBackupCommandAsync(cmd),
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

    private static async Task<int> HandleGitCommandAsync(string[] args)
    {
        // Parse git command manually: git [--config file] [--verbose] <git-args...>
        var configFile = "gitbackup.ini";
        var verbose = false;
        var gitArgsStartIndex = 1; // Skip "git"

        // Look for our options before git arguments
        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "--config" || args[i] == "-c")
            {
                if (i + 1 < args.Length)
                {
                    configFile = args[i + 1];
                    i++; // Skip the config file value
                    gitArgsStartIndex = i + 1;
                }
            }
            else if (args[i] == "--verbose")
            {
                verbose = true;
                gitArgsStartIndex = i + 1;
            }
            else
            {
                // First non-option argument starts git args
                gitArgsStartIndex = i;
                break;
            }
        }

        // Extract git arguments
        var gitArgs = args.Skip(gitArgsStartIndex).ToArray();

        if (gitArgs.Length == 0)
        {
            Console.WriteLine("Error: No git command specified.");
            Console.WriteLine("Usage: GitBackup git [--config file] [--verbose] <git-command> [git-args...]");
            Console.WriteLine("Example: GitBackup git --config backup.ini log --oneline");
            return 1;
        }

        // Create a GitCommand object
        var gitCommand = new GitCommand
        {
            ConfigFile = configFile,
            Verbose = verbose,
            GitArgs = gitArgs
        };

        return await RunGitCommandAsync(gitCommand);
    }

    private static async Task<int> RunBackupCommandAsync(BackupCommand options)
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

    private static async Task<int> RunGitCommandAsync(GitCommand options)
    {
        try
        {
            // Load configuration to get backup directory
            if (!File.Exists(options.ConfigFile))
            {
                Console.WriteLine($"Configuration file not found: {options.ConfigFile}");
                Console.WriteLine($"Use 'GitBackup --create-config' to create a sample configuration file.");
                return 1;
            }

            var config = ConfigurationLoader.LoadFromIni(options.ConfigFile);
            
            // Validate that backup directory exists and is a git repository
            if (!Directory.Exists(config.BackupDir))
            {
                Console.WriteLine($"Backup directory not found: {config.BackupDir}");
                Console.WriteLine($"Run 'GitBackup backup --config {options.ConfigFile}' first to create a backup.");
                return 1;
            }

            if (!Directory.Exists(Path.Combine(config.BackupDir, ".git")) && 
                !Directory.Exists(Path.Combine(config.BackupDir, "refs"))) // bare repo check
            {
                Console.WriteLine($"Backup directory is not a git repository: {config.BackupDir}");
                Console.WriteLine($"Run 'GitBackup backup --config {options.ConfigFile}' first to create a backup.");
                return 1;
            }

            if (options.Verbose)
            {
                Console.WriteLine($"Configuration: {options.ConfigFile}");
                Console.WriteLine($"Backup repository: {config.BackupDir}");
                Console.WriteLine($"Git command: git {string.Join(" ", options.GitArgs)}");
                Console.WriteLine();
            }

            // Execute git command in the backup directory
            return await ExecuteGitCommandAsync(config.BackupDir, options.GitArgs.ToArray());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing git command: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> ExecuteGitCommandAsync(string workingDirectory, string[] gitArgs)
    {
        try
        {
            // Note: GitBackup uses bare repositories for efficiency (no file copying).
            // This means some git commands like 'status' may show unexpected results.
            // The backup data is stored correctly in git objects.
            
            var processStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "git",
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            // Add all git arguments
            foreach (var arg in gitArgs)
            {
                processStartInfo.ArgumentList.Add(arg);
            }

            using var process = new System.Diagnostics.Process { StartInfo = processStartInfo };
            
            process.Start();
            
            // Read output and error streams
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            
            await process.WaitForExitAsync();
            
            var output = await outputTask;
            var error = await errorTask;
            
            // Write output to console
            if (!string.IsNullOrEmpty(output))
            {
                Console.Write(output);
            }
            
            if (!string.IsNullOrEmpty(error))
            {
                Console.Error.Write(error);
            }
            
            return process.ExitCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to execute git command: {ex.Message}");
            Console.WriteLine("Make sure git is installed and available in your PATH.");
            return 1;
        }
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
