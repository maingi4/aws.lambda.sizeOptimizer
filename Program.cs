using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace aws.lambda.sizeOptimizer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var regionName = PrintAndRetrieveInput("aws region name (e.g. us-east-1)");
            var functionName = PrintAndRetrieveInput("function name");
            var payloadFilePath = PrintAndRetrieveInput("payload file path (input to the Lambda function in json format)");
            var startFrom64 = PrintAndRetrieveInput("whether the given region has 64 MB as available memory size (not all do) [y/n]");
            var acceptableDropInPerformanceString = PrintAndRetrieveInput("the acceptable drop in performance in % in favour of reduced price (e.g. 3.5 if upto 3.5% drop is fine)");

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

        private async Task ResizeToRecommended(LambdaSizeCalculationResult result, string regionName, string functionName)
        {
            if (result.OriginalMemory != result.Recommended.MemoryAllocated)
            {
                var resizeToRecommended = PrintAndRetrieveInput(" 'y' if you want to resize to the recommended value:");

                if (resizeToRecommended == "y")
                {
                    var conProvider = new LambdaConnectionProvider();
                    var client = conProvider.GetLambdaClient(regionName);
                    var resizer = new LambdaResizer(client);
                    await resizer.Resize(functionName, result.Recommended.MemoryAllocated);
                    Console.WriteLine("successfully resized.");
                }
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
    }
}
