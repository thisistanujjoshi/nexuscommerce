namespace Catalog.Application.Common;

/// <summary>
/// Thrown when a requested resource does not exist. Maps to HTTP 404 at the API boundary.
/// </summary>
public class NotFoundException(string resource, object key)
    : Exception($"{resource} with id '{key}' was not found.");
