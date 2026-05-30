namespace CoreFlow.OrderFileIngestion.Validation;

public enum ValidationFailureKind
{
    EmptyFile,
    InvalidFilename,
    InvalidProvider,
    InvalidSchema,
    UnexpectedFileSize,
}

public sealed class ValidationException : Exception
{
    public ValidationFailureKind Kind { get; }

    public ValidationException(ValidationFailureKind kind, string message)
        : base(message)
    {
        Kind = kind;
    }
}
