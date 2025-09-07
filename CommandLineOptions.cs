using CommandLine;

namespace GitBackup;

/// <summary>
/// Base class for all GitBackup commands
/// </summary>
public abstract class BaseCommand
{
    [Option('c', "config", Required = false, Default = "gitbackup.ini", 
        HelpText = "Configuration file to use for backup settings.")]
    public string ConfigFile { get; set; } = "gitbackup.ini";

    [Option("verbose", Required = false, 
        HelpText = "Enable verbose output for debugging.")]
    public bool Verbose { get; set; }
}

/// <summary>
/// Backup command options
/// </summary>
[Verb("backup", isDefault: true, HelpText = "Perform a backup operation (default command).")]
public class BackupCommand : BaseCommand
{
    [Option("create-config", Required = false, 
        HelpText = "Create a sample configuration file. Optionally specify filename.")]
    public bool CreateConfigFlag { get; set; }

    [Value(0, Required = false, Hidden = true, 
        HelpText = "Optional filename for --create-config")]
    public string? CreateConfigFile { get; set; }

    [Option('v', "version", Required = false, 
        HelpText = "Display version information.")]
    public bool ShowVersion { get; set; }

    [Option("dry-run", Required = false, 
        HelpText = "Show what would be backed up without actually performing the backup.")]
    public bool DryRun { get; set; }

    [Option("force", Required = false, 
        HelpText = "Force backup even if no changes are detected.")]
    public bool Force { get; set; }
}

/// <summary>
/// Git command passthrough options  
/// </summary>
[Verb("git", HelpText = "Execute git commands on the backup repository.")]
public class GitCommand : BaseCommand
{
    [Value(0, Required = false, HelpText = "Git command and arguments to execute.")]
    public IEnumerable<string> GitArgs { get; set; } = new List<string>();
}

/// <summary>
/// Legacy command line options for backward compatibility
/// </summary>
public class CommandLineOptions : BackupCommand
{
    // This maintains backward compatibility with existing command structure
}
