using System.Text.Json.Nodes;

namespace Philiprehberger.JsonPatch;

/// <summary>
/// Represents a failed patch operation with the associated error message.
/// </summary>
/// <param name="Operation">The patch operation that failed.</param>
/// <param name="Error">The error message describing why the operation failed.</param>
public record OperationFailure(PatchOperation Operation, string Error);

/// <summary>
/// Represents the result of partially applying a patch document, containing the
/// modified document and any operations that failed.
/// </summary>
public class PartialApplicationResult
{
    /// <summary>
    /// Initializes a new <see cref="PartialApplicationResult"/>.
    /// </summary>
    /// <param name="document">The document after applying all successful operations.</param>
    /// <param name="applied">The operations that were successfully applied.</param>
    /// <param name="failures">The operations that failed along with their error messages.</param>
    internal PartialApplicationResult(
        JsonNode? document,
        IReadOnlyList<PatchOperation> applied,
        IReadOnlyList<OperationFailure> failures)
    {
        Document = document;
        Applied = applied;
        Failures = failures;
    }

    /// <summary>
    /// Gets the document after applying all successful operations.
    /// </summary>
    public JsonNode? Document { get; }

    /// <summary>
    /// Gets the operations that were successfully applied.
    /// </summary>
    public IReadOnlyList<PatchOperation> Applied { get; }

    /// <summary>
    /// Gets the operations that failed along with their error messages.
    /// </summary>
    public IReadOnlyList<OperationFailure> Failures { get; }

    /// <summary>
    /// Gets a value indicating whether all operations were successfully applied.
    /// </summary>
    public bool IsFullyApplied => Failures.Count == 0;
}
