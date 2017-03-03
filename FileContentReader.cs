using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace aws.lambda.sizeOptimizer
{
    public class FileContentReader
    {
        public async Task<string> ReadFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new InvalidOperationException($"The file at '{filePath}' could not be found.");

            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                using (var memStream = new MemoryStream())
                {
                    await stream.CopyToAsync(memStream);

                    var data = memStream.ToArray();

                    return Encoding.UTF8.GetString(data);
                }
            }
        }
    }
}