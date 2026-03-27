namespace BuildingBlocks.Kernel.Exceptions;

/// <summary>
/// Exception thrown when a domain invariant is violated.
/// </summary>
public sealed class DomainInvariantException : DomainException
{
    public DomainInvariantException(string message)
        : base(message)
    {
    }

    public DomainInvariantException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

