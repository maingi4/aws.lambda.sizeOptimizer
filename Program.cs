using System;
using System.Collections.Generic;
using Microsoft.Extensions.CommandLineUtils;

namespace aws.lambda.sizeOptimizer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var isCommandlineMode = args.Length > 0;

            if (isCommandlineMode)
            {
                ConfigureCommandLine(args);
            }
            else
            {
                var regionName = PrintAndRetrieveInput("aws region name (e.g. us-east-1)");
                var functionName = PrintAndRetrieveInput("function name");
                var payloadFilePath = PrintAndRetrieveInput("payload file path (input to the Lambda function in json format)");
                var startFrom64 = PrintAndRetrieveInput("whether the given region has 64 MB as available memory size (not all do) [y/n]");
                var acceptableDropInPerformanceString = PrintAndRetrieveInput("the acceptable drop in performance in % in favour of reduced price (e.g. 3.5 if upto 3.5% drop is fine)");
                var resizeToRecommended = PrintAndRetrieveInput(" 'y' if you want to resize to the recommended value:");

                var manualArgs = new List<string>()
                {
                    "-r",
                    regionName,
                    "-n",
                    functionName,
                    "-p",
                    payloadFilePath,
                    "-l",
                    string.IsNullOrWhiteSpace(acceptableDropInPerformanceString) ? "3": acceptableDropInPerformanceString
                };

                if (startFrom64 == "y")
                    manualArgs.Add("-64");

                if (resizeToRecommended == "y")
                    manualArgs.Add("-ar");

                ConfigureCommandLine(manualArgs.ToArray(), true);
            }
        }

        private static void StopForInputBeforeExit()
        {
            Console.WriteLine("press any key to exit...");
            Console.ReadKey();
        }

        public static string PrintAndRetrieveInput(string inputName)
        {
            string input = null;
            while (input == null)
            {
                Console.WriteLine($"Enter {inputName}:");
                input = Console.ReadLine();
            }
            return input;
        }

        public static void PrintResult(LambdaSizeCalculationResult result)
        {
            Console.WriteLine("All results:");
            Console.WriteLine("Memory Allocated\tLatency\tMemory Used");

            foreach (var resultItem in result.Items)
            {
                Console.WriteLine($"{resultItem.MemoryAllocated} MB          \t{resultItem.Latency} ms\t{resultItem.MemoryUsed}");
            }

            if (result.OriginalMemory != result.Recommended.MemoryAllocated)
                Console.WriteLine($"Recommended result: Resize Lambda to {result.Recommended.MemoryAllocated} MB, it had latency of {result.Recommended.Latency} ms.");
            else
                Console.WriteLine($"Your Lambda is already appropriately sized, memory size is at {result.Recommended.MemoryAllocated} MB, it had latency of {result.Recommended.Latency} ms.");
        }

        public static void ConfigureCommandLine(string[] args, bool stopForInput = false)
        {
            /*aws.lambda.sizeOptimizer.dll [-?|-h|--help] [-r |--aws-region <region>] [-n |--fn-name <functionName>] [-p |--payload-path <payloadPath>] [-64 |--use-64mb] [-l |--latency-margin <acceptableDropInPerformance>] [-ar |--auto-resize]
            */
            var commandLine = new CommandLineApplication(false);

            var region = commandLine.Option("-r |--aws-region <region>", "The AWS region in which the Lambda function resides, e.g. us-east-1", CommandOptionType.SingleValue);
            var functionName = commandLine.Option("-n |--fn-name <functionName>", "The name of the Lambda function which is the target", CommandOptionType.SingleValue);
            var payloadPath = commandLine.Option("-p |--payload-path <payloadPath>", "The path of the Json payload which will be used as a test event for the Lambda", CommandOptionType.SingleValue);
            var startFrom64 = commandLine.Option("-64 |--use-64mb", "[Optional] Set this to true if the target region supports 64MB Lambda functions, not all do, by default it will be considered not supported.", CommandOptionType.NoValue);
            var acceptableDropInPerformance = commandLine.Option("-l |--latency-margin <acceptableDropInPerformance>", "[Optional] The amount of latency drop in % which is deemed acceptable in return for greater cost savings e.g. 3.5 would mean 3.5% is an acceptable increase of latency.", CommandOptionType.SingleValue);
            var autoResizeToRecommended = commandLine.Option("-ar |--auto-resize", "[Optional] When provided auto resizes the Lambda function to recommended size", CommandOptionType.NoValue);
            commandLine.HelpOption("-? | -h | --help");
            commandLine.OnExecute(async () =>
            {
                if (!region.HasValue() || !functionName.HasValue() || !payloadPath.HasValue())
                {
                    Console.WriteLine("required fields were missing.");
                    commandLine.ShowHelp();
                    return 0;
                }
                double latencyMargin;
                if (acceptableDropInPerformance.HasValue() && !double.TryParse(acceptableDropInPerformance.Value(), out latencyMargin))
                {
                    Console.WriteLine("latency-margin had an invalid value");
                    commandLine.ShowHelp();
                    return 0;
                }
                else
                {
                    latencyMargin = 3.0;
                }

                try
                {
                    var prog = new CommandRunner();
                    var result = await prog.RunOptimizerAsync(region.Value(), functionName.Value(), payloadPath.Value(), startFrom64.HasValue(), latencyMargin);

                    PrintResult(result);

                    if (autoResizeToRecommended.HasValue())
                        await prog.ResizeToRecommendedAsync(result, region.Value(), functionName.Value());
                }
                catch (Exception ex)
                {
                    Console.WriteLine("---------------------------------");
                    Console.WriteLine(ex.ToString());
                    Console.WriteLine("---------------------------------");
                }
                return 0;
            });

            commandLine.Execute(args);

            if (stopForInput)
                StopForInputBeforeExit();
        }
    }
}
