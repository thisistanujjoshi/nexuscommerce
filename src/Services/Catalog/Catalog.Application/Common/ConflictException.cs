namespace Catalog.Application.Common;

/// <summary>
/// Thrown when a request conflicts with existing state (e.g. duplicate SKU). Maps to HTTP 409.
/// </summary>
public class ConflictException(string message) : Exception(message);
