namespace API.Models
{
    public class OperationResult
    {
        public bool Success { get; set; }
        public object? Data { get; set; }
        public string? ErrorMessage { get; set; }

        public static OperationResult CreateSuccess(object data) =>
            new OperationResult { Success = true, Data = data };

        public static OperationResult CreateFailure(string errorMessage) =>
            new OperationResult { Success = false, ErrorMessage = errorMessage };
    }
}
