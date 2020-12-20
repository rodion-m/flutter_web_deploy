namespace FlutterWebDeploy.Models
{
    public class DeployConfig : IDeployConfig
    {
        public static DeployConfig Default = new();

        public FlutterArguments? FlutterArguments { get; set; }

        public string? FtpLogin { get; set; }
        public string? FtpPassword { get; set; }
        public string? FtpHost { get; set; }
        public int? FtpPort { get; set; }
        public bool FastMode { get; set; }
        public int? UploadRetryAttempts { get; set; }
        public string? ProjectPath { get; set; }
        public string? RemotePath { get; set; }
        public string? FlutterPath { get; set; }
        public string? FlutterCustomArguments { get; set; }
    }
}