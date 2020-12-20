using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using FlutterWebDeploy.Models;

namespace FlutterWebDeploy
{
    internal static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            Console.WriteLine("--- Flutter Web Deploy ---");
            try
            {
                var parser = new Parser(settings => settings.EnableDashDash = true);
                await parser.ParseArguments<ShellOptions>(args)
                    .WithNotParsed(HandleParseError)
                    .WithParsedAsync(options => Deployment.RunOptions(options, args.Any()));
            }
            catch (ApplicationException e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }

            return 0;
        }

        private static void HandleParseError(IEnumerable<Error> errs)
        {
            throw new ApplicationException(
                string.Join('\n', errs.Where(it => it.Tag != ErrorType.MissingRequiredOptionError)
                    .Select(it =>
                    {
                        if (it is TokenError tokenError)
                            return $"{tokenError.Tag}: {tokenError.Token}";

                        return it.ToString();
                    }))
            );
        }
    }
}