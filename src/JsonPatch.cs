using System.Text.Json;
using System.Text.Json.Nodes;

namespace Philiprehberger.JsonPatch;

/// <summary>
/// Static entry point for RFC 6902 JSON Patch operations — apply patches, generate diffs,
/// and parse patch documents.
/// </summary>
public static class JsonPatch
{
    /// <summary>
    /// Applies a sequence of patch operations to a JSON document, returning a modified copy.
    /// </summary>
    /// <param name="document">The source JSON document.</param>
    /// <param name="operations">The ordered patch operations to apply.</param>
    /// <returns>A new <see cref="JsonNode"/> with all operations applied.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="operations"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a patch operation fails.</exception>
    public static JsonNode? Apply(JsonNode? document, IEnumerable<PatchOperation> operations)
    {
        ArgumentNullException.ThrowIfNull(operations);
        return PatchApplier.Apply(document, operations);
    }

    /// <summary>
    /// Generates a minimal set of RFC 6902 patch operations that transform
    /// <paramref name="before"/> into <paramref name="after"/>.
    /// </summary>
    /// <param name="before">The original JSON document.</param>
    /// <param name="after">The modified JSON document.</param>
    /// <returns>An ordered list of patch operations representing the diff.</returns>
    public static IReadOnlyList<PatchOperation> Diff(JsonNode? before, JsonNode? after)
    {
        return PatchGenerator.Generate(before, after).AsReadOnly();
    }

    /// <summary>
    /// Generates a reverse (undo) patch that, when applied after the forward patch,
    /// restores the document to its original state.
    /// </summary>
    /// <param name="patch">The forward patch document to reverse.</param>
    /// <param name="original">The original document before patching.</param>
    /// <returns>A new <see cref="JsonPatchDocument"/> containing the reverse operations.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="patch"/> is <c>null</c>.</exception>
    public static JsonPatchDocument GenerateReverse(JsonPatchDocument patch, JsonNode? original)
    {
        ArgumentNullException.ThrowIfNull(patch);
        return ReversePatchGenerator.GenerateReverse(patch, original);
    }

    /// <summary>
    /// Pre-checks whether a patch can be cleanly applied to a document, returning
    /// a validation result with any errors found without modifying the document.
    /// </summary>
    /// <param name="patch">The patch document to validate.</param>
    /// <param name="document">The target JSON document.</param>
    /// <returns>A <see cref="ValidationResult"/> indicating whether the patch is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="patch"/> is <c>null</c>.</exception>
    public static ValidationResult Validate(JsonPatchDocument patch, JsonNode? document)
    {
        ArgumentNullException.ThrowIfNull(patch);
        return PatchValidator.Validate(patch, document);
    }

    /// <summary>
    /// Merges two patch documents into a single equivalent patch by concatenating
    /// their operations and simplifying where possible (e.g., collapsing adjacent
    /// replace operations on the same path).
    /// </summary>
    /// <param name="first">The first patch to apply.</param>
    /// <param name="second">The second patch to apply after the first.</param>
    /// <returns>A new <see cref="JsonPatchDocument"/> containing the composed operations.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="first"/> or <paramref name="second"/> is <c>null</c>.</exception>
    public static JsonPatchDocument Compose(JsonPatchDocument first, JsonPatchDocument second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        return PatchComposer.Compose(first, second);
    }

    /// <summary>
    /// Applies patch operations individually, collecting successes and failures rather
    /// than stopping on the first error. Returns the partially-patched document and
    /// a list of failed operations with their error messages.
    /// </summary>
    /// <param name="patch">The patch document to apply.</param>
    /// <param name="document">The target JSON document.</param>
    /// <returns>A <see cref="PartialApplicationResult"/> containing the result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="patch"/> is <c>null</c>.</exception>
    public static PartialApplicationResult ApplyPartial(JsonPatchDocument patch, JsonNode? document)
    {
        ArgumentNullException.ThrowIfNull(patch);
        return PartialApplier.ApplyPartial(patch, document);
    }

    /// <summary>
    /// Parses a JSON string containing an array of RFC 6902 patch operations
    /// into a <see cref="JsonPatchDocument"/>.
    /// </summary>
    /// <param name="json">A JSON array of patch operation objects.</param>
    /// <returns>A <see cref="JsonPatchDocument"/> with the parsed operations.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or whitespace.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is malformed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a patch operation is missing required fields.</exception>
    public static JsonPatchDocument Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON string cannot be null or empty.", nameof(json));
        }

        var array = JsonNode.Parse(json)?.AsArray()
            ?? throw new InvalidOperationException("JSON Patch document must be an array.");

        var operations = new List<PatchOperation>();

        foreach (var element in array)
        {
            if (element is not JsonObject obj)
            {
                throw new InvalidOperationException("Each patch operation must be a JSON object.");
            }

            var op = obj["op"]?.GetValue<string>()
                ?? throw new InvalidOperationException("Patch operation is missing required 'op' field.");

            var path = obj["path"]?.GetValue<string>()
                ?? throw new InvalidOperationException("Patch operation is missing required 'path' field.");

            var from = obj["from"]?.GetValue<string>();
            var value = obj.ContainsKey("value") ? obj["value"]?.DeepClone() : null;

            operations.Add(new PatchOperation(op, path, from, value));
        }

        return new JsonPatchDocument(operations);
    }
}
