namespace Philiprehberger.JsonPatch;

/// <summary>
/// Composes two patch documents into a single equivalent patch by concatenating
/// operations and simplifying where possible.
/// </summary>
internal static class PatchComposer
{
    /// <summary>
    /// Merges two patch documents into a single equivalent patch document.
    /// Adjacent replace operations on the same path are collapsed so that
    /// only the second replace is kept.
    /// </summary>
    /// <param name="first">The first patch to apply.</param>
    /// <param name="second">The second patch to apply after the first.</param>
    /// <returns>A new <see cref="JsonPatchDocument"/> containing the merged operations.</returns>
    internal static JsonPatchDocument Compose(JsonPatchDocument first, JsonPatchDocument second)
    {
        var combined = new List<PatchOperation>();
        combined.AddRange(first.Operations);
        combined.AddRange(second.Operations);

        var simplified = Simplify(combined);
        return new JsonPatchDocument(simplified);
    }

    /// <summary>
    /// Simplifies a list of operations by collapsing adjacent replace operations
    /// that target the same path.
    /// </summary>
    private static List<PatchOperation> Simplify(List<PatchOperation> operations)
    {
        if (operations.Count <= 1)
        {
            return operations;
        }

        var result = new List<PatchOperation>();

        for (var i = 0; i < operations.Count; i++)
        {
            var current = operations[i];

            if (current.Op == "replace" && i + 1 < operations.Count)
            {
                var next = operations[i + 1];
                if (next.Op == "replace" && next.Path == current.Path)
                {
                    continue;
                }
            }

            result.Add(current);
        }

        return result;
    }
}
