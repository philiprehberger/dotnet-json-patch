using System.Text.Json;
using System.Text.Json.Nodes;

namespace Philiprehberger.JsonPatch;

/// <summary>
/// Internal class that implements the actual patch application logic
/// for RFC 6902 operations on <see cref="JsonNode"/> trees.
/// </summary>
internal static class PatchApplier
{
    /// <summary>
    /// Applies a sequence of patch operations to a deep copy of the document.
    /// </summary>
    /// <param name="document">The source document to patch.</param>
    /// <param name="operations">The ordered patch operations to apply.</param>
    /// <returns>A new <see cref="JsonNode"/> with all operations applied.</returns>
    /// <exception cref="InvalidOperationException">Thrown when an operation fails.</exception>
    internal static JsonNode? Apply(JsonNode? document, IEnumerable<PatchOperation> operations)
    {
        var result = DeepClone(document);

        foreach (var op in operations)
        {
            result = ApplyOperation(result, op);
        }

        return result;
    }

    /// <summary>
    /// Applies a single patch operation to the document, mutating it in place.
    /// </summary>
    private static JsonNode? ApplyOperation(JsonNode? document, PatchOperation operation)
    {
        return operation.Op switch
        {
            "add" => ApplyAdd(document, operation.Path, DeepClone(operation.Value)),
            "remove" => ApplyRemove(document, operation.Path),
            "replace" => ApplyReplace(document, operation.Path, DeepClone(operation.Value)),
            "move" => ApplyMove(document, operation.From!, operation.Path),
            "copy" => ApplyCopy(document, operation.From!, operation.Path),
            "test" => ApplyTest(document, operation.Path, operation.Value),
            _ => throw new InvalidOperationException($"Unknown patch operation: '{operation.Op}'.")
        };
    }

    /// <summary>
    /// Applies an "add" operation.
    /// </summary>
    private static JsonNode? ApplyAdd(JsonNode? document, string path, JsonNode? value)
    {
        if (string.IsNullOrEmpty(path))
        {
            return value;
        }

        var segments = JsonPointer.Parse(path);
        var (parent, key) = JsonPointer.Navigate(document!, segments);

        if (parent is JsonObject obj)
        {
            obj[key] = value;
        }
        else if (parent is JsonArray arr)
        {
            if (key == "-")
            {
                arr.Add(value);
            }
            else
            {
                var index = ParseArrayIndex(key);
                if (index < 0 || index > arr.Count)
                {
                    throw new InvalidOperationException(
                        $"Array index {index} is out of bounds for add (array length: {arr.Count}).");
                }
                arr.Insert(index, value);
            }
        }
        else
        {
            throw new InvalidOperationException(
                $"Cannot add to a value node at path '{path}'.");
        }

        return document;
    }

    /// <summary>
    /// Applies a "remove" operation.
    /// </summary>
    private static JsonNode? ApplyRemove(JsonNode? document, string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        var segments = JsonPointer.Parse(path);
        var (parent, key) = JsonPointer.Navigate(document!, segments);

        if (parent is JsonObject obj)
        {
            if (!obj.Remove(key))
            {
                throw new InvalidOperationException(
                    $"Cannot remove non-existent property '{key}'.");
            }
        }
        else if (parent is JsonArray arr)
        {
            var index = ParseArrayIndex(key);
            if (index < 0 || index >= arr.Count)
            {
                throw new InvalidOperationException(
                    $"Array index {index} is out of bounds for remove (array length: {arr.Count}).");
            }
            arr.RemoveAt(index);
        }
        else
        {
            throw new InvalidOperationException(
                $"Cannot remove from a value node at path '{path}'.");
        }

        return document;
    }

    /// <summary>
    /// Applies a "replace" operation (equivalent to remove + add).
    /// </summary>
    private static JsonNode? ApplyReplace(JsonNode? document, string path, JsonNode? value)
    {
        if (string.IsNullOrEmpty(path))
        {
            return value;
        }

        var segments = JsonPointer.Parse(path);
        var (parent, key) = JsonPointer.Navigate(document!, segments);

        if (parent is JsonObject obj)
        {
            if (!obj.ContainsKey(key))
            {
                throw new InvalidOperationException(
                    $"Cannot replace non-existent property '{key}'.");
            }
            obj[key] = value;
        }
        else if (parent is JsonArray arr)
        {
            var index = ParseArrayIndex(key);
            if (index < 0 || index >= arr.Count)
            {
                throw new InvalidOperationException(
                    $"Array index {index} is out of bounds for replace (array length: {arr.Count}).");
            }
            arr[index] = value;
        }
        else
        {
            throw new InvalidOperationException(
                $"Cannot replace on a value node at path '{path}'.");
        }

        return document;
    }

    /// <summary>
    /// Applies a "move" operation (equivalent to remove from source + add to target).
    /// </summary>
    private static JsonNode? ApplyMove(JsonNode? document, string from, string path)
    {
        var value = JsonPointer.GetValue(document!, from);
        var cloned = DeepClone(value);
        document = ApplyRemove(document, from);
        document = ApplyAdd(document, path, cloned);
        return document;
    }

    /// <summary>
    /// Applies a "copy" operation (equivalent to get from source + add to target).
    /// </summary>
    private static JsonNode? ApplyCopy(JsonNode? document, string from, string path)
    {
        var value = JsonPointer.GetValue(document!, from);
        var cloned = DeepClone(value);
        document = ApplyAdd(document, path, cloned);
        return document;
    }

    /// <summary>
    /// Applies a "test" operation that verifies the value at the path equals the expected value.
    /// </summary>
    private static JsonNode? ApplyTest(JsonNode? document, string path, JsonNode? expected)
    {
        var actual = JsonPointer.GetValue(document!, path);

        if (!JsonNodesEqual(actual, expected))
        {
            var actualJson = actual is null ? "null" : actual.ToJsonString();
            var expectedJson = expected is null ? "null" : expected.ToJsonString();
            throw new InvalidOperationException(
                $"Test operation failed at path '{path}'. Expected {expectedJson} but found {actualJson}.");
        }

        return document;
    }

    /// <summary>
    /// Deep-clones a <see cref="JsonNode"/> by serializing and deserializing.
    /// </summary>
    internal static JsonNode? DeepClone(JsonNode? node)
    {
        if (node is null)
        {
            return null;
        }

        return JsonNode.Parse(node.ToJsonString());
    }

    /// <summary>
    /// Compares two <see cref="JsonNode"/> instances for structural equality.
    /// </summary>
    internal static bool JsonNodesEqual(JsonNode? a, JsonNode? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;

        var aJson = a.ToJsonString();
        var bJson = b.ToJsonString();
        return aJson == bJson;
    }

    /// <summary>
    /// Parses a string as an array index.
    /// </summary>
    private static int ParseArrayIndex(string segment)
    {
        if (!int.TryParse(segment, out var index))
        {
            throw new InvalidOperationException(
                $"Expected numeric array index but got '{segment}'.");
        }

        return index;
    }
}
