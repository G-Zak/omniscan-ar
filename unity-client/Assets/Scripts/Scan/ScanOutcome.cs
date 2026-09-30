using OmniScan.Api;

namespace OmniScan.Scan
{
    public class ScanOutcome
    {
        public bool IsSuccess { get; }
        public string Message { get; }
        public ClassifyResult Classification { get; }
        public SubgraphResponse Subgraph { get; }

        private ScanOutcome(bool isSuccess, string message, ClassifyResult classification, SubgraphResponse subgraph)
        {
            IsSuccess = isSuccess;
            Message = message;
            Classification = classification;
            Subgraph = subgraph;
        }

        public static ScanOutcome Success(ClassifyResult classification, SubgraphResponse subgraph) =>
            new(true, $"Recognised {subgraph.machine?.name ?? classification.machineId}", classification, subgraph);

        /// <summary>Message is user-facing, so keep it free of stack traces and raw server errors.</summary>
        public static ScanOutcome Failure(string message, ClassifyResult classification = null) =>
            new(false, message, classification, null);
    }
}
