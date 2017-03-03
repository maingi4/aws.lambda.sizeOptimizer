using System;
using Amazon.Lambda;

namespace aws.lambda.sizeOptimizer
{
    public class InvocationTypeConverter
    {
        public InvocationType Convert(LambdaInvocationType invocationType)
        {
            switch (invocationType)
            {
                case LambdaInvocationType.Event:
                    return InvocationType.Event;
                case LambdaInvocationType.RequestResponse:
                    return InvocationType.RequestResponse;
            }
            throw new NotImplementedException($"The invocation type '{invocationType.ToString("G")}' is not currently supported.");
        }
    }
}