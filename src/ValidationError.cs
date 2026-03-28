namespace Philiprehberger.JsonPatch;

/// <summary>
/// Represents a single validation error encountered when pre-checking a patch operation.
/// </summary>
/// <param name="Operation">The patch operation that failed validation.</param>
/// <param name="Message">A description of why the operation would fail.</param>
public record ValidationError(PatchOperation Operation, string Message);
