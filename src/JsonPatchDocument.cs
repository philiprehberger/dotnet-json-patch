using System.Text.Json.Nodes;

namespace Philiprehberger.JsonPatch;

/// <summary>
/// Represents an RFC 6902 JSON Patch document containing an ordered list of operations.
/// </summary>
public class JsonPatchDocument
{
    private readonly List<PatchOperation> _operations;

    /// <summary>
    /// Initializes a new <see cref="JsonPatchDocument"/> with the specified operations.
    /// </summary>
    /// <param name="operations">The ordered sequence of patch operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="operations"/> is <c>null</c>.</exception>
    public JsonPatchDocument(IEnumerable<PatchOperation> operations)
    {
        ArgumentNullException.ThrowIfNull(operations);
        _operations = operations.ToList();
    }

    /// <summary>
    /// Gets the ordered list of patch operations in this document.
    /// </summary>
    public IReadOnlyList<PatchOperation> Operations => _operations.AsReadOnly();

    /// <summary>
    /// Applies all operations in order to a deep copy of the given document.
    /// </summary>
    /// <param name="document">The JSON document to patch.</param>
    /// <returns>A new <see cref="JsonNode"/> with all patch operations applied.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a patch operation fails.</exception>
    public JsonNode? Apply(JsonNode? document)
    {
        return PatchApplier.Apply(document, _operations);
    }
}
