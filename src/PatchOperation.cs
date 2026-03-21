using System.Text.Json.Nodes;

namespace Philiprehberger.JsonPatch;

/// <summary>
/// Represents a single RFC 6902 JSON Patch operation.
/// </summary>
/// <param name="Op">The operation type (add, remove, replace, move, copy, test).</param>
/// <param name="Path">A JSON Pointer (RFC 6901) string identifying the target location.</param>
/// <param name="From">A JSON Pointer string for move and copy operations indicating the source location.</param>
/// <param name="Value">The value to apply for add, replace, and test operations.</param>
public record PatchOperation(string Op, string Path, string? From = null, JsonNode? Value = null)
{
    /// <summary>
    /// Creates an add operation that inserts a value at the specified path.
    /// </summary>
    /// <param name="path">The JSON Pointer target path.</param>
    /// <param name="value">The value to add.</param>
    /// <returns>A new <see cref="PatchOperation"/> with op "add".</returns>
    public static PatchOperation Add(string path, JsonNode? value) =>
        new("add", path, Value: value);

    /// <summary>
    /// Creates a remove operation that deletes the value at the specified path.
    /// </summary>
    /// <param name="path">The JSON Pointer target path.</param>
    /// <returns>A new <see cref="PatchOperation"/> with op "remove".</returns>
    public static PatchOperation Remove(string path) =>
        new("remove", path);

    /// <summary>
    /// Creates a replace operation that substitutes the value at the specified path.
    /// </summary>
    /// <param name="path">The JSON Pointer target path.</param>
    /// <param name="value">The replacement value.</param>
    /// <returns>A new <see cref="PatchOperation"/> with op "replace".</returns>
    public static PatchOperation Replace(string path, JsonNode? value) =>
        new("replace", path, Value: value);

    /// <summary>
    /// Creates a move operation that relocates a value from one path to another.
    /// </summary>
    /// <param name="from">The source JSON Pointer path.</param>
    /// <param name="path">The destination JSON Pointer path.</param>
    /// <returns>A new <see cref="PatchOperation"/> with op "move".</returns>
    public static PatchOperation Move(string from, string path) =>
        new("move", path, From: from);

    /// <summary>
    /// Creates a copy operation that duplicates a value from one path to another.
    /// </summary>
    /// <param name="from">The source JSON Pointer path.</param>
    /// <param name="path">The destination JSON Pointer path.</param>
    /// <returns>A new <see cref="PatchOperation"/> with op "copy".</returns>
    public static PatchOperation Copy(string from, string path) =>
        new("copy", path, From: from);

    /// <summary>
    /// Creates a test operation that verifies the value at the specified path matches the expected value.
    /// </summary>
    /// <param name="path">The JSON Pointer target path.</param>
    /// <param name="value">The expected value to compare against.</param>
    /// <returns>A new <see cref="PatchOperation"/> with op "test".</returns>
    public static PatchOperation Test(string path, JsonNode? value) =>
        new("test", path, Value: value);
}
