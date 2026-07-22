namespace Catalog.Domain.Exceptions;

/// <summary>
/// Thrown when a domain invariant is violated. Maps to HTTP 400 at the API boundary.
/// </summary>
public class DomainException(string message) : Exception(message);
