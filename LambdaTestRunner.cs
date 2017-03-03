using System;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Model;

namespace aws.lambda.sizeOptimizer
{
    public class LambdaTestRunner
    {
        private readonly Amazon.Lambda.AmazonLambdaClient _client;
        public LambdaTestRunner(Amazon.Lambda.AmazonLambdaClient client)
        {
            _client = client;
        }

        public async Task<LambdaSizeCalculationResultItem> RunTestAsync(LambdaInvocationRequest invocationRequest)
        {
            Console.WriteLine("Starting config fetch prior to test run");

            var settings = await _client.GetFunctionConfigurationAsync(invocationRequest.FunctionName);

            var memory = settings.MemorySize;

            Console.WriteLine("Starting dry run");
            await _client.InvokeAsync(await CreateLambdaRequest(invocationRequest));
            Console.WriteLine("Starting test run # 1");
            var response = await _client.InvokeAsync(await CreateLambdaRequest(invocationRequest));
            Console.WriteLine("Starting test run # 2");
            var response2 = await _client.InvokeAsync(await CreateLambdaRequest(invocationRequest));

            if (response.StatusCode > 299 || response.StatusCode < 200)
            {
                throw new InvalidProgramException($"The Lambda which was invoked with the given payload returned with a status code not in the 200 series. The status code was '{response.StatusCode}'");
            }
            if (response2.StatusCode > 299 || response2.StatusCode < 200)
            {
                throw new InvalidProgramException($"The Lambda which was invoked with the given payload returned with a status code not in the 200 series. The status code was '{response2.StatusCode}'");
            }
            var log1 = response.LogResult;

            var item1 = ParseResultFromLog(log1, memory);

            var log2 = response2.LogResult;

            var item2 = ParseResultFromLog(log2, memory);

            var item = item2.Latency < item1.Latency ? item2 : item1;
            Console.WriteLine($"Selected best out of two test result:{item.ToString()}");

            return item;
        }

        private LambdaSizeCalculationResultItem ParseResultFromLog(string log, int memory)
        {
            var decodedLog = Encoding.UTF8.GetString(Convert.FromBase64String(log));

            var tuple = ParseLog(decodedLog);

            return new LambdaSizeCalculationResultItem(memory, tuple.Item1, tuple.Item2);
        }

        private Tuple<double, double> ParseLog(string log)
        {
            var split = log.Split(':');
            double duration = 0;
            double maxMemUsed = 0;

            for (var i = 0; i < split.Length - 1; i++)
            {
                var item = split[i];
                if (item.EndsWith("Billed Duration", StringComparison.OrdinalIgnoreCase))
                    continue;

                var nextItem = split[i + 1];
                if (item.EndsWith("Duration", StringComparison.OrdinalIgnoreCase))
                {
                    var durationString = nextItem.Substring(0, nextItem.IndexOf("ms", StringComparison.OrdinalIgnoreCase)).Trim();

                    double.TryParse(durationString, out duration);
                }

                if (item.EndsWith("Max Memory Used", StringComparison.OrdinalIgnoreCase))
                {
                    var maxMemString = nextItem.Substring(0, nextItem.IndexOf("MB", StringComparison.OrdinalIgnoreCase)).Trim();

                    double.TryParse(maxMemString, out maxMemUsed);
                }
            }
            if (duration == 0)
                throw new InvalidProgramException($"Amazon's log formatting has changed, parsing of this log will need to be changed in the code. Parsing failed for duration.");

            if (maxMemUsed == 0)
                throw new InvalidProgramException($"Amazon's log formatting has changed, parsing of this log will need to be changed in the code. Parsing failed for max memory used.");

            return new Tuple<double, double>(duration, maxMemUsed);
        }

        private async Task<InvokeRequest> CreateLambdaRequest(LambdaInvocationRequest systemRequest)
        {
            var request = new InvokeRequest();

            request.FunctionName = systemRequest.FunctionName;
            request.InvocationType = new InvocationTypeConverter().Convert(systemRequest.InvocationType);

            request.Payload = await new FileContentReader().ReadFileAsync(systemRequest.PayloadFilePath);
            request.LogType = LogType.Tail;

            return request;
        }
    }
}