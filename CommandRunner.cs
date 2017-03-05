using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace aws.lambda.sizeOptimizer
{
    public class CommandRunner
    {
        public async Task ResizeToRecommendedAsync(LambdaSizeCalculationResult result, string regionName, string functionName)
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

        public async Task<LambdaSizeCalculationResult> RunOptimizerAsync(string regionName, string functionName, string payloadFilePath, bool startFrom64, double acceptableDropInPerformance)
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
    }
}
