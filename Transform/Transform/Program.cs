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
                var service = new Service();
                var generateConfig = GetArgumentByName(args, "--generateConfig");

                if (generateConfig)
                {
                    service.GenerateConfig();
                    return;
                }

                var sourceFilenme = GetArgumentValueByName(args, "--source");
                var targetFilename = GetArgumentValueByName(args, "--target");

                if (string.IsNullOrEmpty(sourceFilenme) || string.IsNullOrEmpty(targetFilename))
                {
                    throw new ArgumentException("Invalid arguments");
                }

                var createTagNodes = GetArgumentByName(args, "--createTagNodes");

                if (!createTagNodes)
                {
                    service.CopyTagsFromRelationToWay(sourceFilenme, targetFilename);
                }
                else
                {
                    var trailmarkService = new TrailmarkService();
                    trailmarkService.CreateTrailmarks(sourceFilenme, targetFilename);
                    //service.CopyTagsFromRelationToWay(sourceFilenme, targetFilename);
                }
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
