using System;
using System.Collections.Generic;
using System.Linq;

namespace aws.lambda.sizeOptimizer
{
    public class LambdaBestSizeCalculator
    {
        public LambdaSizeCalculationResult Calculate(ICollection<LambdaSizeCalculationResultItem> items, double acceptableDropInPerformance, int originalMemory)
        {
            if (acceptableDropInPerformance < 0.1)
                acceptableDropInPerformance = 3.0;

            var sortedItems = items.OrderBy(x => x.MemoryAllocated).ToList();

            var minLatency = items.Min(x => x.Latency);

            var bestItem = sortedItems.First(x => InAcceptableRange(x.Latency, minLatency, acceptableDropInPerformance));

            return new LambdaSizeCalculationResult(bestItem, sortedItems, originalMemory);
        }

        private bool InAcceptableRange(double assessee, double target, double acceptableDropInPerformance)
        {
            var increase = Math.Abs(target - assessee);

            var increasePercent = (increase / target) * 100;

            return increase <= acceptableDropInPerformance;
        }
    }
}