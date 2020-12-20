namespace FlutterWebDeploy.Models
{
    public interface IDeployConfig
    {
        string? FtpLogin { get; set; }
        string? FtpPassword { get; set; }
        string? FtpHost { get; set; }
        int? FtpPort { get; set; }
        bool FastMode { get; set; }
        int? UploadRetryAttempts { get; set; }
        string? ProjectPath { get; set; }
        string? RemotePath { get; set; }
        string? FlutterPath { get; set; }
        string? FlutterCustomArguments { get; set; }
    }
}