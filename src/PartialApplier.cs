using System.Text.Json.Nodes;

namespace Philiprehberger.JsonPatch;

/// <summary>
/// Applies patch operations individually, collecting successes and failures rather
/// than failing on the first error.
/// </summary>
internal static class PartialApplier
{
    /// <summary>
    /// Applies each operation in the patch document individually, continuing past failures.
    /// Returns the partially-patched document along with lists of applied and failed operations.
    /// </summary>
    /// <param name="patch">The patch document to partially apply.</param>
    /// <param name="document">The target JSON document.</param>
    /// <returns>A <see cref="PartialApplicationResult"/> containing the result.</returns>
    internal static PartialApplicationResult ApplyPartial(JsonPatchDocument patch, JsonNode? document)
    {
        var working = PatchApplier.DeepClone(document);
        var applied = new List<PatchOperation>();
        var failures = new List<OperationFailure>();

        foreach (var operation in patch.Operations)
        {
            try
            {
                working = PatchApplier.Apply(working, new[] { operation });
                applied.Add(operation);
            }
            catch (Exception ex)
            {
                failures.Add(new OperationFailure(operation, ex.Message));
            }
        }

        return new PartialApplicationResult(working, applied.AsReadOnly(), failures.AsReadOnly());
    }
}
