using System.Net;

namespace Graphql
{
    public class ApiResponse<T>
    {
        public ApiResponse()
        {
            Errors = new List<string>();
        }

        public bool Success { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; }

        public static ApiResponse<T> SuccessResponse(T? data, HttpStatusCode statusCode = HttpStatusCode.OK, string message = "Success")
        {
            return new ApiResponse<T>
            {
                Success = true,
                StatusCode = statusCode,
                Message = message,
                Data = data
            };
        }

        public static ApiResponse<T> ErrorResponse(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest, List<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                StatusCode = statusCode,
                Message = message,
                Errors = errors ?? new List<string>()
            };
        }
    }
}
