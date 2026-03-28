using System.Text.Json.Nodes;

namespace Philiprehberger.JsonPatch;

/// <summary>
/// Generates a reverse (undo) patch from a forward patch and the original document,
/// producing operations that restore the original state when applied after the forward patch.
/// </summary>
internal static class ReversePatchGenerator
{
    /// <summary>
    /// Generates a reverse patch document that undoes the given forward patch.
    /// </summary>
    /// <param name="patch">The forward patch document to reverse.</param>
    /// <param name="original">The original document before the forward patch was applied.</param>
    /// <returns>A new <see cref="JsonPatchDocument"/> that undoes the forward patch.</returns>
    internal static JsonPatchDocument GenerateReverse(JsonPatchDocument patch, JsonNode? original)
    {
        var reverseOps = new List<PatchOperation>();
        var working = PatchApplier.DeepClone(original);

        foreach (var operation in patch.Operations)
        {
            var reverseOp = GenerateReverseOperation(operation, working);
            if (reverseOp is not null)
            {
                reverseOps.Add(reverseOp);
            }

            working = PatchApplier.Apply(working, new[] { operation });
        }

        reverseOps.Reverse();
        return new JsonPatchDocument(reverseOps);
    }

    /// <summary>
    /// Generates the reverse operation for a single forward operation.
    /// </summary>
    private static PatchOperation? GenerateReverseOperation(PatchOperation operation, JsonNode? document)
    {
        return operation.Op switch
        {
            "add" => ReverseAdd(operation, document),
            "remove" => ReverseRemove(operation, document),
            "replace" => ReverseReplace(operation, document),
            "move" => ReverseMove(operation),
            "copy" => ReverseCopy(operation, document),
            "test" => null,
            _ => null
        };
    }

    /// <summary>
    /// Reverses an add operation. If the path existed before, it becomes a replace
    /// restoring the original value. If it did not exist, it becomes a remove.
    /// </summary>
    private static PatchOperation ReverseAdd(PatchOperation operation, JsonNode? document)
    {
        if (string.IsNullOrEmpty(operation.Path))
        {
            return PatchOperation.Replace("", PatchApplier.DeepClone(document));
        }

        try
        {
            var segments = JsonPointer.Parse(operation.Path);
            var (parent, key) = JsonPointer.Navigate(document!, segments);

            if (parent is JsonObject obj && obj.ContainsKey(key))
            {
                var originalValue = PatchApplier.DeepClone(obj[key]);
                return PatchOperation.Replace(operation.Path, originalValue);
            }
        }
        catch
        {
            // Path doesn't exist in original — add becomes remove
        }

        return PatchOperation.Remove(operation.Path);
    }

    /// <summary>
    /// Reverses a remove operation by capturing the value being removed and
    /// producing an add operation to restore it.
    /// </summary>
    private static PatchOperation ReverseRemove(PatchOperation operation, JsonNode? document)
    {
        if (string.IsNullOrEmpty(operation.Path))
        {
            return PatchOperation.Add("", PatchApplier.DeepClone(document));
        }

        var value = JsonPointer.GetValue(document!, operation.Path);
        return PatchOperation.Add(operation.Path, PatchApplier.DeepClone(value));
    }

    /// <summary>
    /// Reverses a replace operation by capturing the current value and producing
    /// a replace that restores it.
    /// </summary>
    private static PatchOperation ReverseReplace(PatchOperation operation, JsonNode? document)
    {
        if (string.IsNullOrEmpty(operation.Path))
        {
            return PatchOperation.Replace("", PatchApplier.DeepClone(document));
        }

        var originalValue = JsonPointer.GetValue(document!, operation.Path);
        return PatchOperation.Replace(operation.Path, PatchApplier.DeepClone(originalValue));
    }

    /// <summary>
    /// Reverses a move operation by swapping the from and path fields.
    /// </summary>
    private static PatchOperation ReverseMove(PatchOperation operation)
    {
        return PatchOperation.Move(operation.Path, operation.From!);
    }

    /// <summary>
    /// Reverses a copy operation. Since copy adds a value at the destination,
    /// the reverse is a remove of the destination path.
    /// </summary>
    private static PatchOperation ReverseCopy(PatchOperation operation, JsonNode? document)
    {
        if (!string.IsNullOrEmpty(operation.Path))
        {
            try
            {
                var segments = JsonPointer.Parse(operation.Path);
                var (parent, key) = JsonPointer.Navigate(document!, segments);

                if (parent is JsonObject obj && obj.ContainsKey(key))
                {
                    var originalValue = PatchApplier.DeepClone(obj[key]);
                    return PatchOperation.Replace(operation.Path, originalValue);
                }
            }
            catch
            {
                // Path doesn't exist in original
            }
        }

        return PatchOperation.Remove(operation.Path);
    }
}
