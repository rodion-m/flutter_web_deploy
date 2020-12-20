using CommandLine;

namespace FlutterWebDeploy.Models
{
    // ReSharper disable once ClassNeverInstantiated.Global

    public class ShellOptions : IDeployConfig
    {
        [Option('c', "config", Required = false, HelpText = "Path to the deploy config file (.yaml).")]
        public string? ConfigPath { get; set; }

        [Option('l', "login", Required = false, HelpText = "FTP connection login.")]
        public string? FtpLogin { get; set; }

        [Option('p', "pass", Required = false, HelpText = "FTP connection password.")]
        public string? FtpPassword { get; set; }

        [Option('h', "host", Required = false, HelpText = "FTP host started with 'ftp.'")]
        public string? FtpHost { get; set; }

        [Option("port", Required = false, HelpText = "FTP connection port (21 is default).")]
        public int? FtpPort { get; set; }

        [Option('f', "fast", Required = false, HelpText = "Upload only main project files (skip images and etc.)")]
        public bool FastMode { get; set; }

        [Option("attempts", Required = false, HelpText = "FTP upload retry attemps (3 is default).")]
        public int? UploadRetryAttempts { get; set; }

        [Option("project_path", Required = false, HelpText = "Path to the flutter app project directory (current is default).")]
        public string? ProjectPath { get; set; }

        [Option("remote_path", Required = false, HelpText = "Path to the app remote directory on a server (root is default).")]
        public string? RemotePath { get; set; }

        [Option("flutter_path", Required = false, HelpText = "Extra arguments for flutter build.")]
        public string? FlutterPath { get; set; }

        [Option("custom_args", Required = false, HelpText = "Custom arguments for flutter build.")]
        public string? FlutterCustomArguments { get; set; }
    }
}