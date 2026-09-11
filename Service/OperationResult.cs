namespace Document_Management.Service
{
    public sealed class OperationResult
    {
        public bool Succeeded { get; init; }

        public bool NotFound { get; init; }

        public string? GeneralError { get; init; }

        public Dictionary<string, string> Errors { get; } = new(StringComparer.Ordinal);

        public static OperationResult Success()
        {
            return new OperationResult { Succeeded = true };
        }

        public static OperationResult NotFoundResult()
        {
            return new OperationResult { NotFound = true };
        }

        public static OperationResult Failure(string message)
        {
            return new OperationResult { GeneralError = message };
        }

        public static OperationResult Validation(string key, string message)
        {
            var result = new OperationResult();
            result.Errors[key] = message;
            return result;
        }

        public static OperationResult Validation(IDictionary<string, string> errors)
        {
            var result = new OperationResult();
            foreach (var error in errors)
            {
                result.Errors[error.Key] = error.Value;
            }

            return result;
        }
    }
}
