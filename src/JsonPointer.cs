using System.Text.Json.Nodes;

namespace Philiprehberger.JsonPatch;

/// <summary>
/// Internal helper for RFC 6901 JSON Pointer parsing and navigation.
/// </summary>
internal static class JsonPointer
{
    /// <summary>
    /// Parses a JSON Pointer string into its unescaped path segments.
    /// </summary>
    /// <param name="pointer">A JSON Pointer string (e.g. "/foo/bar/0").</param>
    /// <returns>An array of unescaped path segments.</returns>
    /// <exception cref="ArgumentException">Thrown when the pointer is non-empty and does not start with '/'.</exception>
    internal static string[] Parse(string pointer)
    {
        if (string.IsNullOrEmpty(pointer))
        {
            return [];
        }

        if (!pointer.StartsWith('/'))
        {
            throw new ArgumentException($"JSON Pointer must start with '/' but was: '{pointer}'", nameof(pointer));
        }

        var raw = pointer[1..].Split('/');
        var segments = new string[raw.Length];

        for (var i = 0; i < raw.Length; i++)
        {
            segments[i] = Unescape(raw[i]);
        }

        return segments;
    }

    /// <summary>
    /// Navigates a <see cref="JsonNode"/> tree using the given JSON Pointer path segments
    /// and returns the parent node and the final segment key.
    /// </summary>
    /// <param name="root">The root node to navigate.</param>
    /// <param name="segments">The parsed pointer segments.</param>
    /// <returns>A tuple of the parent node and the final path segment.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a segment cannot be resolved in the tree.</exception>
    internal static (JsonNode Parent, string Key) Navigate(JsonNode root, string[] segments)
    {
        var current = root;

        for (var i = 0; i < segments.Length - 1; i++)
        {
            current = Resolve(current, segments[i])
                ?? throw new InvalidOperationException(
                    $"Cannot navigate to segment '{segments[i]}' — node is null.");
        }

        return (current, segments[^1]);
    }

    /// <summary>
    /// Resolves a single segment against a <see cref="JsonNode"/>, returning the child node.
    /// </summary>
    /// <param name="node">The current node.</param>
    /// <param name="segment">The segment to resolve (property name or array index).</param>
    /// <returns>The resolved child node, or <c>null</c> if the value is JSON null.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the segment cannot be applied to the node type.</exception>
    internal static JsonNode? Resolve(JsonNode node, string segment)
    {
        if (node is JsonObject obj)
        {
            return obj[segment];
        }

        if (node is JsonArray arr)
        {
            if (segment == "-")
            {
                throw new InvalidOperationException(
                    "The '-' segment cannot be used for reading; it is only valid for appending.");
            }

            if (!int.TryParse(segment, out var index))
            {
                throw new InvalidOperationException(
                    $"Expected numeric array index but got '{segment}'.");
            }

            if (index < 0 || index >= arr.Count)
            {
                throw new InvalidOperationException(
                    $"Array index {index} is out of bounds (array length: {arr.Count}).");
            }

            return arr[index];
        }

        throw new InvalidOperationException(
            $"Cannot resolve segment '{segment}' on a value node.");
    }

    /// <summary>
    /// Retrieves the value at the given JSON Pointer path within a document.
    /// </summary>
    /// <param name="root">The root document node.</param>
    /// <param name="pointer">The JSON Pointer string.</param>
    /// <returns>The node at the specified location.</returns>
    internal static JsonNode? GetValue(JsonNode root, string pointer)
    {
        var segments = Parse(pointer);

        if (segments.Length == 0)
        {
            return root;
        }

        var current = root;

        foreach (var segment in segments)
        {
            current = Resolve(current!, segment);
        }

        return current;
    }

    /// <summary>
    /// Unescapes a single JSON Pointer segment per RFC 6901.
    /// ~1 → /  and  ~0 → ~  (order matters).
    /// </summary>
    private static string Unescape(string segment) =>
        segment.Replace("~1", "/").Replace("~0", "~");

    /// <summary>
    /// Escapes a property name for use in a JSON Pointer segment per RFC 6901.
    /// ~ → ~0  and  / → ~1  (order matters).
    /// </summary>
    internal static string Escape(string segment) =>
        segment.Replace("~", "~0").Replace("/", "~1");
}
