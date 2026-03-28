using System.Text.Json.Nodes;

namespace Philiprehberger.JsonPatch;

/// <summary>
/// Pre-checks whether a patch document can be cleanly applied to a JSON document
/// without actually modifying it.
/// </summary>
internal static class PatchValidator
{
    /// <summary>
    /// Validates all operations in a patch document against the given JSON document.
    /// Applies operations to a working copy so that each subsequent operation is
    /// validated against the document state after previous operations.
    /// </summary>
    /// <param name="patch">The patch document to validate.</param>
    /// <param name="document">The target JSON document.</param>
    /// <returns>A <see cref="ValidationResult"/> containing any errors found.</returns>
    internal static ValidationResult Validate(JsonPatchDocument patch, JsonNode? document)
    {
        var errors = new List<ValidationError>();
        var working = PatchApplier.DeepClone(document);

        foreach (var operation in patch.Operations)
        {
            var error = ValidateOperation(operation, working);
            if (error is not null)
            {
                errors.Add(error);
            }
            else
            {
                try
                {
                    working = PatchApplier.Apply(working, new[] { operation });
                }
                catch
                {
                    // If apply fails after validation passed, treat as validation error
                    errors.Add(new ValidationError(operation, $"Operation '{operation.Op}' at '{operation.Path}' failed unexpectedly."));
                }
            }
        }

        return new ValidationResult(errors);
    }

    /// <summary>
    /// Validates a single patch operation against the current document state.
    /// </summary>
    private static ValidationError? ValidateOperation(PatchOperation operation, JsonNode? document)
    {
        return operation.Op switch
        {
            "add" => ValidateAdd(operation, document),
            "remove" => ValidateRemove(operation, document),
            "replace" => ValidateReplace(operation, document),
            "move" => ValidateMove(operation, document),
            "copy" => ValidateCopy(operation, document),
            "test" => ValidateTest(operation, document),
            _ => new ValidationError(operation, $"Unknown operation type '{operation.Op}'.")
        };
    }

    /// <summary>
    /// Validates an add operation — the parent path must exist.
    /// </summary>
    private static ValidationError? ValidateAdd(PatchOperation operation, JsonNode? document)
    {
        if (string.IsNullOrEmpty(operation.Path))
        {
            return null;
        }

        try
        {
            var segments = JsonPointer.Parse(operation.Path);
            if (segments.Length > 1 && document is not null)
            {
                var parentSegments = segments[..^1];
                var current = document;
                foreach (var segment in parentSegments)
                {
                    current = JsonPointer.Resolve(current, segment);
                    if (current is null)
                    {
                        return new ValidationError(operation, $"Parent path does not exist for add at '{operation.Path}'.");
                    }
                }
            }
            else if (segments.Length > 0 && document is null)
            {
                return new ValidationError(operation, $"Cannot add to path '{operation.Path}' on a null document.");
            }

            return null;
        }
        catch (Exception ex)
        {
            return new ValidationError(operation, $"Invalid path '{operation.Path}': {ex.Message}");
        }
    }

    /// <summary>
    /// Validates a remove operation — the target path must exist.
    /// </summary>
    private static ValidationError? ValidateRemove(PatchOperation operation, JsonNode? document)
    {
        if (string.IsNullOrEmpty(operation.Path))
        {
            return null;
        }

        if (document is null)
        {
            return new ValidationError(operation, $"Cannot remove from path '{operation.Path}' on a null document.");
        }

        try
        {
            var value = JsonPointer.GetValue(document, operation.Path);
            if (value is null)
            {
                var segments = JsonPointer.Parse(operation.Path);
                if (segments.Length > 0)
                {
                    var (parent, key) = JsonPointer.Navigate(document, segments);
                    if (parent is JsonObject obj && !obj.ContainsKey(key))
                    {
                        return new ValidationError(operation, $"Path '{operation.Path}' does not exist for remove.");
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            return new ValidationError(operation, $"Path '{operation.Path}' does not exist for remove: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates a replace operation — the target path must exist.
    /// </summary>
    private static ValidationError? ValidateReplace(PatchOperation operation, JsonNode? document)
    {
        if (string.IsNullOrEmpty(operation.Path))
        {
            return null;
        }

        if (document is null)
        {
            return new ValidationError(operation, $"Cannot replace at path '{operation.Path}' on a null document.");
        }

        try
        {
            var segments = JsonPointer.Parse(operation.Path);
            var (parent, key) = JsonPointer.Navigate(document, segments);

            if (parent is JsonObject obj && !obj.ContainsKey(key))
            {
                return new ValidationError(operation, $"Path '{operation.Path}' does not exist for replace.");
            }

            if (parent is JsonArray arr)
            {
                if (!int.TryParse(key, out var index) || index < 0 || index >= arr.Count)
                {
                    return new ValidationError(operation, $"Array index '{key}' is out of bounds for replace at '{operation.Path}'.");
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            return new ValidationError(operation, $"Invalid path '{operation.Path}' for replace: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates a move operation — both the source and parent of the destination must exist.
    /// </summary>
    private static ValidationError? ValidateMove(PatchOperation operation, JsonNode? document)
    {
        if (string.IsNullOrEmpty(operation.From))
        {
            return new ValidationError(operation, "Move operation is missing required 'from' field.");
        }

        if (document is null)
        {
            return new ValidationError(operation, $"Cannot move from '{operation.From}' on a null document.");
        }

        try
        {
            JsonPointer.GetValue(document, operation.From);
            return null;
        }
        catch (Exception ex)
        {
            return new ValidationError(operation, $"Source path '{operation.From}' does not exist for move: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates a copy operation — the source path must exist.
    /// </summary>
    private static ValidationError? ValidateCopy(PatchOperation operation, JsonNode? document)
    {
        if (string.IsNullOrEmpty(operation.From))
        {
            return new ValidationError(operation, "Copy operation is missing required 'from' field.");
        }

        if (document is null)
        {
            return new ValidationError(operation, $"Cannot copy from '{operation.From}' on a null document.");
        }

        try
        {
            JsonPointer.GetValue(document, operation.From);
            return null;
        }
        catch (Exception ex)
        {
            return new ValidationError(operation, $"Source path '{operation.From}' does not exist for copy: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates a test operation — the target path must exist and the value must match.
    /// </summary>
    private static ValidationError? ValidateTest(PatchOperation operation, JsonNode? document)
    {
        if (document is null && !string.IsNullOrEmpty(operation.Path))
        {
            return new ValidationError(operation, $"Cannot test path '{operation.Path}' on a null document.");
        }

        try
        {
            var actual = document is not null
                ? JsonPointer.GetValue(document, operation.Path)
                : null;

            if (!PatchApplier.JsonNodesEqual(actual, operation.Value))
            {
                var actualJson = actual is null ? "null" : actual.ToJsonString();
                var expectedJson = operation.Value is null ? "null" : operation.Value.ToJsonString();
                return new ValidationError(operation, $"Test failed at '{operation.Path}': expected {expectedJson} but found {actualJson}.");
            }

            return null;
        }
        catch (Exception ex)
        {
            return new ValidationError(operation, $"Test at path '{operation.Path}' failed: {ex.Message}");
        }
    }
}
