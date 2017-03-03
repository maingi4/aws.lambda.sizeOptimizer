using System;

namespace aws.lambda.sizeOptimizer
{
    public class LambdaInvocationRequest
    {
        public LambdaInvocationRequest(string functionName, LambdaInvocationType invocationType, string payloadFilePath)
        {
            if (string.IsNullOrWhiteSpace(functionName))
                throw new InvalidOperationException($"The function name cannot be empty");

            FunctionName = functionName;
            InvocationType = invocationType;
            PayloadFilePath = payloadFilePath;
        }

        public string FunctionName { get; private set; }
        public LambdaInvocationType InvocationType { get; private set; }

        public string PayloadFilePath { get; private set; }
    }
}