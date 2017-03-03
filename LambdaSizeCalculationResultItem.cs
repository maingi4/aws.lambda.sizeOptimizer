namespace aws.lambda.sizeOptimizer
{
    public class LambdaSizeCalculationResultItem
    {
        public LambdaSizeCalculationResultItem(int memoryAllocated, double latency, double memoryUsed)
        {
            MemoryAllocated = memoryAllocated;
            Latency = latency;
            MemoryUsed = memoryUsed;
        }
        public int MemoryAllocated { get; private set; }
        public double Latency { get; private set; }
        public double MemoryUsed { get; private set; }

        public override string ToString()
        {
            return $"Memory Allocated: {MemoryAllocated}, Latency: {Latency}, Memory Used: {MemoryUsed}";
        }
    }
}