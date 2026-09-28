using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace OmniScan.Api
{
    public class ApiClient
    {
        private readonly string baseUrl;

        public ApiClient(string baseUrl = "http://localhost:8000/api/v1")
        {
            this.baseUrl = baseUrl.TrimEnd('/');
        }

        public async Task<ApiResult<ClassifyResult>> ClassifyAsync(byte[] jpeg)
        {
            var form = new WWWForm();
            form.AddBinaryData("image", jpeg, "frame.jpg", "image/jpeg");

            using var request = UnityWebRequest.Post($"{baseUrl}/cv/classify", form);
            await request.SendWebRequest();
            return ParseResponse<ClassifyResult>(request);
        }

        public async Task<ApiResult<SubgraphResponse>> GetSubgraphAsync(string machineId)
        {
            var url = $"{baseUrl}/machines/{UnityWebRequest.EscapeURL(machineId)}/subgraph";
            using var request = UnityWebRequest.Get(url);
            await request.SendWebRequest();
            return ParseResponse<SubgraphResponse>(request);
        }

        private static ApiResult<T> ParseResponse<T>(UnityWebRequest request)
        {
            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.DataProcessingError)
            {
                return ApiResult<T>.Failure(new ApiError(ApiErrorKind.NetworkError, "NETWORK_ERROR", request.error));
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                var value = JsonConvert.DeserializeObject<T>(request.downloadHandler.text);
                return ApiResult<T>.Success(value);
            }

            var status = request.responseCode;
            var detail = TryParseErrorDetail(request.downloadHandler.text);
            var code = detail?.code ?? "UNKNOWN_ERROR";
            var message = detail?.message ?? $"Request failed with status {status}";
            var kind = status == 404 ? ApiErrorKind.NotFound : ApiErrorKind.ServerError;

            return ApiResult<T>.Failure(new ApiError(kind, code, message, status));
        }

        private static ErrorDetail TryParseErrorDetail(string body)
        {
            try
            {
                return JsonConvert.DeserializeObject<ErrorEnvelope>(body)?.error;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
