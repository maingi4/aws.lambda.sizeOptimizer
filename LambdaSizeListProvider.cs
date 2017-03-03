using System.Collections.Generic;
using System.Linq;

namespace aws.lambda.sizeOptimizer
{
    public class LambdaSizeListProvider
    {
        private List<int> _lambdaSizes = new List<int>();

        public LambdaSizeListProvider() : this(false)
        { }

        public LambdaSizeListProvider(bool startFrom64)
        {
            for (var startSize = startFrom64 ? 64 : 128; startSize <= 1536; startSize += 64) //skipping 64, as its not available everywhere
            {
                _lambdaSizes.Add(startSize);
            }
        }

        public ICollection<int> GetLambdaSizes(double minimumMemoryRequired)
        {
            return _lambdaSizes.Where(x => x >= minimumMemoryRequired).ToList();
        }
    }
}