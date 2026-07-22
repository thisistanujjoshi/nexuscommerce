namespace Orders.Domain.Exceptions;

/// <summary>
/// Thrown when an order invariant or state transition rule is violated.
/// Maps to HTTP 400/409 at the API boundary.
/// </summary>
public class OrderDomainException(string message) : Exception(message);
