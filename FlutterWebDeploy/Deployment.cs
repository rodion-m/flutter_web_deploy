using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AutoMapper;
using FluentFTP;
using FlutterWebDeploy.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace FlutterWebDeploy
{
    public static class Deployment
    {
        private const string DefaultConfigName = "flutter_web_deploy.yaml";

        public static async Task RunOptions(ShellOptions options, bool optionsSet)
        {
            if (!string.IsNullOrWhiteSpace(options.ConfigPath))
                if (!File.Exists(options.ConfigPath))
                    throw new ApplicationException($"Deploy config path is not found: {options.ConfigPath}");

            var configPath = options.ConfigPath.NullIfWhiteSpace()
                             ?? Path.Combine(Directory.GetCurrentDirectory(), DefaultConfigName);

            var mapperConfig = new MapperConfiguration(
                cfg => cfg.CreateMap<ShellOptions, DeployConfig>()
                    .ForMember(m => m.FlutterArguments, o => o.Ignore())
            );
            mapperConfig.AssertConfigurationIsValid();
            var mapper = new Mapper(mapperConfig);
            var optionsConfig = mapper.Map<DeployConfig>(options);
            DeployConfig config;
            if (File.Exists(configPath))
            {
                string content = await File.ReadAllTextAsync(configPath);

                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .Build();

                config = deserializer.Deserialize<DeployConfig>(content);
                if (optionsSet)
                {
                    // Replace configs from command line is they are set
                    foreach (var propertyInfo in optionsConfig.GetType().GetProperties())
                    {
                        var value = propertyInfo.GetValue(optionsConfig);
                        var @default = propertyInfo.GetValue(DeployConfig.Default);
                        if (value != null && value.GetHashCode() != @default?.GetHashCode()) propertyInfo.SetValue(config, value);
                    }
                }
            }
            else
            {
                config = optionsConfig;
            }

            await Deploy(config);
        }

        public static void SaveConfig(DeployConfig config, string fn)
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();
            var yaml = serializer.Serialize(config);
            File.WriteAllText(fn, yaml);
        }

        private static async Task Deploy(DeployConfig config)
        {
            Console.WriteLine("--- Flutter Web Deploy ---");
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new ApplicationException("Sorry, only Windows is supported at the moment.");

            if (string.IsNullOrWhiteSpace(config.FtpLogin))
                throw new ApplicationException($"FTP login is not set (argument -l). Set the login or use config file ({DefaultConfigName}).");

            if (string.IsNullOrWhiteSpace(config.FtpPassword))
                throw new ApplicationException("FTP password is not set (argument -p).");

            if (string.IsNullOrWhiteSpace(config.FtpHost))
                throw new ApplicationException("FTP host is not set (argument -h).");

            var flutterPath = config.FlutterPath;
            if (string.IsNullOrWhiteSpace(flutterPath))
            {
                flutterPath = FindFlutterPath();
                if (flutterPath == null)
                    throw new ApplicationException("Cannot find a flutter path. Try to set it manually (argument --flutter_path).");
            }
            else if (!File.Exists(flutterPath))
            {
                throw new ApplicationException($"The provided flutter path is not found: {flutterPath}");
            }

            var projectPath = config.ProjectPath;
            var buildRelativePath = Path.Combine("build", "web");
            string buildLocalPath;
            if (!string.IsNullOrWhiteSpace(projectPath))
            {
                buildLocalPath = Path.Combine(projectPath, buildRelativePath);
                if (!Directory.Exists(buildLocalPath))
                    throw new ApplicationException($"A build local path is not found: {buildLocalPath}");
            }
            else
            {
                // Check project directory in current folder and in one level up
                var currentDir = Directory.GetCurrentDirectory();
                buildLocalPath = Path.Combine(currentDir, buildRelativePath);
                if (!Directory.Exists(buildLocalPath))
                {
                    var parentDir = Directory.GetParent(currentDir).FullName;
                    buildLocalPath = Path.Combine(parentDir, buildRelativePath);
                    if (!Directory.Exists(buildLocalPath))
                        throw new ApplicationException("Cannot find project directory. " +
                                                       "Please specify a path to the flutter project (argument --project_path).");
                }
            }

            HashSet<string> arguments = new();
            if (config.FlutterArguments != null)
                foreach (var propertyInfo in config.FlutterArguments.GetType().GetProperties())
                {
                    var attrib = propertyInfo.CustomAttributes
                        .FirstOrDefault(it => it.AttributeType == typeof(FlutterArgumentAttribute));
                    if (attrib == null)
                        continue;
                    if (propertyInfo.GetValue(config.FlutterArguments) is bool v && v)
                    {
                        var argument = attrib.ConstructorArguments[0].Value as string;
                        arguments.Add(argument!);
                    }
                }

            if (!string.IsNullOrWhiteSpace(config.FlutterCustomArguments))
                arguments.Add(config.FlutterCustomArguments);
            var argumentsString = $"build web {string.Join(' ', arguments)}";
            Console.WriteLine($"Running 'flutter {argumentsString}' ...");
            var cmd = new Process
            {
                StartInfo =
                {
                    FileName = flutterPath,
                    Arguments = argumentsString,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WorkingDirectory = projectPath
                }
            };

            cmd.Start();

            cmd.WaitForExit();

            if (cmd.ExitCode == 0)
            {
                Console.WriteLine(await cmd.StandardOutput.ReadToEndAsync());
                Console.WriteLine("Build complete.");
            }
            else
            {
                Console.WriteLine(await cmd.StandardError.ReadToEndAsync());
                throw new ApplicationException($"'flutter {argumentsString}' throwed en error. Error code: {cmd.ExitCode}");
            }

            var client = new FtpClient(config.FtpHost, config.FtpPort ?? 0, config.FtpLogin, config.FtpPassword)
            {
                RetryAttempts = config.UploadRetryAttempts ?? 3
            };
            try
            {
                Console.WriteLine("Connecting...");
                await client.ConnectAsync();
                
                var appRemotePath = config.RemotePath.NullIfWhiteSpace() ?? "/";
                Console.WriteLine($"Uploading files from '{buildLocalPath}' to '{appRemotePath}' ...");
                var cursorTop = Console.CursorTop;
                Dictionary<string, string> filesProgress = new();
                var _mutex = new object();
                void ReportProgress(FtpProgress progress)
                {
                    lock (_mutex)
                    {
                        var shortName = progress.LocalPath.Substring(buildLocalPath.Length + 1);
                        filesProgress[progress.LocalPath] = $"[{progress.FileIndex + 1} of {progress.FileCount}] " +
                                                            $"{shortName} {progress.Progress:N0}%";
                        
                        Console.SetCursorPosition(0, cursorTop);
                        foreach (var (_, text) in filesProgress)
                        {
                            Console.WriteLine(text.PadRight(100));
                        }
                        Console.Title = $"Uploading: [{progress.FileIndex} of {progress.FileCount}] ({progress.Progress:N0}%)";
                    }
                }

                bool result;
                if (config.FastMode)
                {
                    var files = new[] {"index.html", "main.dart.js", "flutter_service_worker.js", ".last_build_id"};
                    var successCount = await client.UploadFilesAsync(
                        files.Select(file => Path.Combine(buildLocalPath!, file)),
                        appRemotePath,
                        FtpRemoteExists.Overwrite, 
                        verifyOptions: FtpVerify.Retry,
                        progress: new Progress<FtpProgress>(ReportProgress)
                    );
                    Console.WriteLine("\r".PadRight(100));
                    
                    result = files.Length == successCount;
                }
                else
                {
                    var results = await client.UploadDirectoryAsync(
                        buildLocalPath,
                        appRemotePath,
                        FtpFolderSyncMode.Mirror, //Creates an exact mirror of the source
                        FtpRemoteExists.Overwrite,
                        FtpVerify.Retry,
                        progress: new Progress<FtpProgress>(ReportProgress)
                    );
                    Console.WriteLine("\r".PadRight(100));
                    
                    result = results.All(it => !it.IsFailed);
                    if (!result)
                    {
                        foreach (var ftpResult in results.Where(it => !it.IsFailed))
                        {
                            Console.WriteLine(ftpResult.ToString());
                        }
                    }
                }

                Console.WriteLine(result ? "Succeeded! :)" : "Something is failed :(");
            }
            finally
            {
                if (client.IsConnected) await client.DisconnectAsync();
            }
        }
        
        private static string? FindFlutterPath()
        {
            var result = Utils.FindAppInPathDirectories("flutter");
            if (result == null)
            {
                string windir = Environment.SystemDirectory;
                if (windir != null)
                {
                    var windrive = Path.GetPathRoot(Environment.SystemDirectory)!;
                    var possiblePath = Path.Combine(windrive, "src", "flutter", "bin", "flutter.bat");
                    if (File.Exists(possiblePath)) result = possiblePath;
                }
            }

            return result;
        }
    }
}