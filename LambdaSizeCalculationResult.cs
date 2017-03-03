using System.Collections.Generic;

namespace aws.lambda.sizeOptimizer
{
    public class LambdaSizeCalculationResult
    {
        public LambdaSizeCalculationResult(LambdaSizeCalculationResultItem recommended, ICollection<LambdaSizeCalculationResultItem> allResults, int originalMemory)
        {
            Recommended = recommended;
            Items = allResults;
            OriginalMemory = originalMemory;
        }

        public LambdaSizeCalculationResultItem Recommended { get; private set; }
        public ICollection<LambdaSizeCalculationResultItem> Items { get; private set; }
        public int OriginalMemory { get; private set; }
    }
}