using System;
using System.Threading.Tasks;
using Amazon.Lambda.Model;

namespace aws.lambda.sizeOptimizer
{
    public class LambdaResizer
    {
        private readonly Amazon.Lambda.AmazonLambdaClient _client;
        public LambdaResizer(Amazon.Lambda.AmazonLambdaClient client)
        {
            _client = client;
        }

        public async Task Resize(string functionName, int targetMemory)
        {
            var request = new UpdateFunctionConfigurationRequest();

            request.FunctionName = functionName;
            request.MemorySize = targetMemory;

            Console.WriteLine("Resizing Lambda to " + targetMemory + "MB");
            await _client.UpdateFunctionConfigurationAsync(request);
        }
    }
}