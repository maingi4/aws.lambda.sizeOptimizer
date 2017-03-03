namespace aws.lambda.sizeOptimizer
{
    public class LambdaConnectionProvider
    {
        public Amazon.Lambda.AmazonLambdaClient GetLambdaClient(string regionName)
        {
            var region = Amazon.RegionEndpoint.GetBySystemName(regionName);
            
            return new Amazon.Lambda.AmazonLambdaClient(region);
        }
    }
}