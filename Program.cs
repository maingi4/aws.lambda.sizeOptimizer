using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;

namespace aws.lambda.sizeOptimizer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //[-?|-h|--help] [-r |--aws-region] [-n |--fn-name] [-p |--payload-path] [-64 |--use-64mb] [-l |--latency-margin] [-ar |--auto-resize]
            // var manualArgs = new List<string>()
            // {
            //     "-r",
            //     "us-west-2",
            //     "-n",
            //     "cloudncodeLogTransformer",
            //     "-p",
            //     @"C:\temp\lambda.json"
            // };
            // args = manualArgs.ToArray();

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

                double acceptableDropInPerformance;
                if (!double.TryParse(acceptableDropInPerformanceString, out acceptableDropInPerformance))
                    acceptableDropInPerformance = 3.0;
                try
                {
                    Task.Run(async () =>
                                {
                                    var prog = new Program();
                                    var result = await prog.RunOptimizer(regionName, functionName, payloadFilePath, startFrom64 == "y", acceptableDropInPerformance);

                                    PrintResult(result);

                                    if (resizeToRecommended == "y")
                                        await prog.ResizeToRecommended(result, regionName, functionName);

                                }).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("---------------------------------");
                    Console.WriteLine(ex.ToString());
                    Console.WriteLine("---------------------------------");
                }
                Console.WriteLine("press any key to exit...");
                Console.ReadKey();
            }
        }

        private async Task ResizeToRecommended(LambdaSizeCalculationResult result, string regionName, string functionName)
        {
            if (result.OriginalMemory != result.Recommended.MemoryAllocated)
            {
                var conProvider = new LambdaConnectionProvider();
                var client = conProvider.GetLambdaClient(regionName);
                var resizer = new LambdaResizer(client);
                await resizer.Resize(functionName, result.Recommended.MemoryAllocated);
                Console.WriteLine("successfully resized.");
            }
        }

        private async Task<LambdaSizeCalculationResult> RunOptimizer(string regionName, string functionName, string payloadFilePath, bool startFrom64, double acceptableDropInPerformance)
        {
            var conProvider = new LambdaConnectionProvider();
            var client = conProvider.GetLambdaClient(regionName);
            var runner = new LambdaTestRunner(client);

            var originalResult = await runner.RunTestAsync(new LambdaInvocationRequest(functionName, LambdaInvocationType.RequestResponse, payloadFilePath));

            var sizes = new LambdaSizeListProvider(startFrom64).GetLambdaSizes(originalResult.MemoryUsed).Where(x => x != originalResult.MemoryAllocated).ToList();
            var resizer = new LambdaResizer(client);

            var results = new List<LambdaSizeCalculationResultItem>(sizes.Count + 1);
            results.Add(originalResult);
            bool didResizeHappen = false;
            try
            {
                foreach (var size in sizes)
                {
                    await resizer.Resize(functionName, size);
                    didResizeHappen = true;

                    var result = await runner.RunTestAsync(new LambdaInvocationRequest(functionName, LambdaInvocationType.RequestResponse, payloadFilePath));
                    results.Add(result);
                }
            }
            finally
            {
                if (didResizeHappen)
                {
                    try
                    {
                        await resizer.Resize(functionName, originalResult.MemoryAllocated);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("ATTENTION ATTENTION ATTENTION ATTENTION ATTENTION ATTENTION");
                        Console.WriteLine($"Resize to original memory size '{originalResult.MemoryAllocated}' failed, do it manually.");
                        Console.WriteLine("ATTENTION ATTENTION ATTENTION ATTENTION ATTENTION ATTENTION");
                        throw;
                    }
                }
            }

            var calc = new LambdaBestSizeCalculator();

            var calculatedResult = calc.Calculate(results, acceptableDropInPerformance, originalResult.MemoryAllocated);

            return calculatedResult;
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

        public static void ConfigureCommandLine(string[] args)
        {
            /*aws.lambda.sizeOptimizer.dll [-?|-h|--help] [-r |--aws-region] [-n |--fn-name] [-p |--payload-path] [-64 |--use-64mb] [-l |--latency-margin] [-ar |--auto-resize]
            */
            var commandLine = new CommandLineApplication(true);

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
                    var prog = new Program();
                    var result = await prog.RunOptimizer(region.Value(), functionName.Value(), payloadPath.Value(), startFrom64.HasValue(), latencyMargin);

                    PrintResult(result);

                    if (autoResizeToRecommended.HasValue())
                        await prog.ResizeToRecommended(result, region.Value(), functionName.Value());
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
            // commandLine.Command("auto",
            // (target) =>
            // {

            // }
            // );
        }
    }
}
