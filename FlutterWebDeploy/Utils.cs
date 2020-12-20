using System;
using System.IO;
using System.Linq;

namespace FlutterWebDeploy
{
    public static class Utils
    {
        private static readonly string[] executableExtensions =
        {
            ".exe", ".com", ".bat", ".sh", ".vbs", ".vbscript", ".vbe", ".js", ".rb", ".cmd", ".cpl", ".ws", ".wsf",
            ".msc", ".gadget"
        };

        public static string? FindAppInPathDirectories(string appFileName)
        {
            var environmentPath = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrWhiteSpace(environmentPath))
                throw new ApplicationException("Environment 'PATH' is empty");

            var paths = environmentPath.Split(';');

            if (appFileName.Contains('.'))
                foreach (string fn in paths.Select(path => Path.Combine(path, appFileName)))
                    if (File.Exists(fn))
                        return fn;

            return paths.Select(path => Path.Combine(path, appFileName))
                .SelectMany(fn => executableExtensions.Select(ext => fn + ext))
                .FirstOrDefault(File.Exists);
        }
    }
}