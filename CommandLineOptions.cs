using CommandLine;

namespace GitBackup;

/// <summary>
/// Command-line options for GitBackup application
/// </summary>
public class CommandLineOptions
{
    [Option('c', "config", Required = false, Default = "gitbackup.ini", 
        HelpText = "Configuration file to use for backup settings.")]
    public string ConfigFile { get; set; } = "gitbackup.ini";

    [Option("create-config", Required = false, 
        HelpText = "Create a sample configuration file. Optionally specify filename.")]
    public bool CreateConfigFlag { get; set; }

    [Value(0, Required = false, Hidden = true, 
        HelpText = "Optional filename for --create-config")]
    public string? CreateConfigFile { get; set; }

    [Option('v', "version", Required = false, 
        HelpText = "Display version information.")]
    public bool ShowVersion { get; set; }

    [Option("verbose", Required = false, 
        HelpText = "Enable verbose output for debugging.")]
    public bool Verbose { get; set; }

    [Option("dry-run", Required = false, 
        HelpText = "Show what would be backed up without actually performing the backup.")]
    public bool DryRun { get; set; }

    [Option("force", Required = false, 
        HelpText = "Force backup even if no changes are detected.")]
    public bool Force { get; set; }
}
