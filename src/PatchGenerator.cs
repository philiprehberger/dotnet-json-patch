using System.Text.Json.Nodes;

namespace Philiprehberger.JsonPatch;

/// <summary>
/// Internal class that generates RFC 6902 diff operations by recursively
/// comparing two <see cref="JsonNode"/> trees.
/// </summary>
internal static class PatchGenerator
{
    /// <summary>
    /// Generates a list of patch operations that, when applied to <paramref name="before"/>,
    /// produce <paramref name="after"/>.
    /// </summary>
    /// <param name="before">The original document.</param>
    /// <param name="after">The modified document.</param>
    /// <returns>An ordered list of patch operations representing the diff.</returns>
    internal static List<PatchOperation> Generate(JsonNode? before, JsonNode? after)
    {
        var operations = new List<PatchOperation>();
        Diff(operations, "", before, after);
        return operations;
    }

    /// <summary>
    /// Recursively compares two nodes at the given path and appends diff operations.
    /// </summary>
    private static void Diff(List<PatchOperation> operations, string path, JsonNode? before, JsonNode? after)
    {
        if (before is null && after is null)
        {
            return;
        }

        if (before is null)
        {
            operations.Add(PatchOperation.Add(path, PatchApplier.DeepClone(after)));
            return;
        }

        if (after is null)
        {
            operations.Add(PatchOperation.Replace(path, null));
            return;
        }

        if (before.GetValueKind() != after.GetValueKind())
        {
            operations.Add(PatchOperation.Replace(path, PatchApplier.DeepClone(after)));
            return;
        }

        switch (before)
        {
            case JsonObject beforeObj when after is JsonObject afterObj:
                DiffObjects(operations, path, beforeObj, afterObj);
                break;

            case JsonArray beforeArr when after is JsonArray afterArr:
                DiffArrays(operations, path, beforeArr, afterArr);
                break;

            default:
                if (!PatchApplier.JsonNodesEqual(before, after))
                {
                    operations.Add(PatchOperation.Replace(path, PatchApplier.DeepClone(after)));
                }
                break;
        }
    }

    /// <summary>
    /// Compares two JSON objects and generates add, remove, and replace operations
    /// for their properties.
    /// </summary>
    private static void DiffObjects(
        List<PatchOperation> operations, string path, JsonObject before, JsonObject after)
    {
        var beforeKeys = new HashSet<string>();

        foreach (var kvp in before)
        {
            beforeKeys.Add(kvp.Key);
        }

        // Check properties removed or changed
        foreach (var key in beforeKeys)
        {
            var escapedKey = JsonPointer.Escape(key);
            var childPath = $"{path}/{escapedKey}";

            if (!after.ContainsKey(key))
            {
                operations.Add(PatchOperation.Remove(childPath));
            }
            else
            {
                Diff(operations, childPath, before[key], after[key]);
            }
        }

        // Check properties added
        foreach (var kvp in after)
        {
            if (!beforeKeys.Contains(kvp.Key))
            {
                var escapedKey = JsonPointer.Escape(kvp.Key);
                var childPath = $"{path}/{escapedKey}";
                operations.Add(PatchOperation.Add(childPath, PatchApplier.DeepClone(kvp.Value)));
            }
        }
    }

    /// <summary>
    /// Compares two JSON arrays by index and generates replace, add, and remove
    /// operations to transform <paramref name="before"/> into <paramref name="after"/>.
    /// </summary>
    private static void DiffArrays(
        List<PatchOperation> operations, string path, JsonArray before, JsonArray after)
    {
        var commonLength = Math.Min(before.Count, after.Count);

        // Compare common elements
        for (var i = 0; i < commonLength; i++)
        {
            Diff(operations, $"{path}/{i}", before[i], after[i]);
        }

        // Elements added to the end
        for (var i = commonLength; i < after.Count; i++)
        {
            operations.Add(PatchOperation.Add($"{path}/-", PatchApplier.DeepClone(after[i])));
        }

        // Elements removed from the end (remove in reverse order to preserve indices)
        for (var i = before.Count - 1; i >= commonLength; i--)
        {
            operations.Add(PatchOperation.Remove($"{path}/{i}"));
        }
    }
}
