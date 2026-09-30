using System.Threading.Tasks;
using OmniScan.Api;

namespace OmniScan.Scan
{
    public class ScanPipeline
    {
        private readonly ApiClient api;
        private readonly float minConfidence;

        public ScanPipeline(ApiClient api, float minConfidence = 0.6f)
        {
            this.api = api;
            this.minConfidence = minConfidence;
        }

        public async Task<ScanOutcome> RunAsync(byte[] jpeg)
        {
            if (jpeg == null || jpeg.Length == 0)
            {
                return ScanOutcome.Failure("Camera not ready. Point at the machine and try again.");
            }

            var classify = await api.ClassifyAsync(jpeg);
            if (!classify.IsSuccess)
            {
                return ScanOutcome.Failure(DescribeClassifyError(classify.Error));
            }

            var classification = classify.Value;
            if (classification == null || string.IsNullOrEmpty(classification.machineId))
            {
                return ScanOutcome.Failure("Could not recognise this machine. Try a different angle.");
            }
            if (classification.confidence < minConfidence)
            {
                return ScanOutcome.Failure(
                    $"Not sure what this is ({classification.confidence:P0} confidence). Move closer and retry.",
                    classification);
            }

            var subgraph = await api.GetSubgraphAsync(classification.machineId);
            if (!subgraph.IsSuccess)
            {
                return ScanOutcome.Failure(DescribeSubgraphError(subgraph.Error, classification), classification);
            }

            return ScanOutcome.Success(classification, subgraph.Value);
        }

        private static string DescribeClassifyError(ApiError error) => error.Kind switch
        {
            ApiErrorKind.NotFound => "No machine recognised. Try a different angle.",
            ApiErrorKind.NetworkError => "Can't reach the server. Check your connection and retry.",
            _ => "Recognition service error. Please try again later.",
        };

        private static string DescribeSubgraphError(ApiError error, ClassifyResult classification) => error.Kind switch
        {
            ApiErrorKind.NotFound => $"'{classification.label}' was recognised but has no documentation yet.",
            ApiErrorKind.NetworkError => "Can't reach the server. Check your connection and retry.",
            _ => "Documentation service error. Please try again later.",
        };
    }
}
