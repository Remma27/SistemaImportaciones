namespace API.Models
{
    public class OperationResult
    {
        internal string? Message { get; set; }
        internal string? message { get; set; }

        public bool Success { get; set; }
        public object? Data { get; set; }
        public string? ErrorMessage { get; set; }

    }
}
