using System;
using System.Diagnostics;

namespace Transform
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var trailmarkService = new TrailmarkService();

                var generateConfig = GetArgumentByName(args, "--generateConfig");

                if (generateConfig)
                {
                    trailmarkService.GenerateConfig();
                    return;
                }

                var sourceFilenme = GetArgumentValueByName(args, "--source");
                var targetFilename = GetArgumentValueByName(args, "--target");

                if (string.IsNullOrEmpty(sourceFilenme) || string.IsNullOrEmpty(targetFilename))
                {
                    throw new ArgumentException("Invalid arguments");
                }

                trailmarkService.CreateTrailmarks(sourceFilenme, targetFilename);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.ReadKey();
            }
        }

        static string GetArgumentValueByName(string[] args, string argumentName)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i]== argumentName)
                {
                    if (args.Length >= i + 1)
                    {
                        return args[i + 1];
                    }
                }
            }

            throw new ArgumentException($"Argument was not found: {argumentName}");
        }

        static bool GetArgumentByName(string[] args, string argumentName)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == argumentName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
