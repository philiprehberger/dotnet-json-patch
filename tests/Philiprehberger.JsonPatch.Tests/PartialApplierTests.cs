using System.Text.Json.Nodes;

namespace Philiprehberger.JsonPatch.Tests;

public class PartialApplierTests
{
    [Fact]
    public void ApplyPartial_AllValid_AppliesAllOperations()
    {
        var document = JsonNode.Parse("""{"name": "Alice", "age": 30}""");
        var patch = new JsonPatchDocument(new[]
        {
            PatchOperation.Replace("/name", JsonValue.Create("Bob")),
            PatchOperation.Add("/email", JsonValue.Create("bob@example.com"))
        });

        var result = JsonPatch.ApplyPartial(patch, document);

        Assert.True(result.IsFullyApplied);
        Assert.Equal(2, result.Applied.Count);
        Assert.Empty(result.Failures);
        Assert.Equal("Bob", result.Document!["name"]!.GetValue<string>());
        Assert.Equal("bob@example.com", result.Document["email"]!.GetValue<string>());
    }

    [Fact]
    public void ApplyPartial_SomeFailing_AppliesValidAndReportsFailures()
    {
        var document = JsonNode.Parse("""{"name": "Alice"}""");
        var patch = new JsonPatchDocument(new[]
        {
            PatchOperation.Replace("/name", JsonValue.Create("Bob")),
            PatchOperation.Remove("/nonexistent"),
            PatchOperation.Add("/age", JsonValue.Create(30))
        });

        var result = JsonPatch.ApplyPartial(patch, document);

        Assert.False(result.IsFullyApplied);
        Assert.Equal(2, result.Applied.Count);
        Assert.Single(result.Failures);
        Assert.Equal("remove", result.Failures[0].Operation.Op);
        Assert.Equal("/nonexistent", result.Failures[0].Operation.Path);
        Assert.Equal("Bob", result.Document!["name"]!.GetValue<string>());
        Assert.Equal(30, result.Document["age"]!.GetValue<int>());
    }

    [Fact]
    public void ApplyPartial_AllFailing_ReturnsOriginalDocumentWithAllFailures()
    {
        var document = JsonNode.Parse("""{"name": "Alice"}""");
        var patch = new JsonPatchDocument(new[]
        {
            PatchOperation.Remove("/missing1"),
            PatchOperation.Remove("/missing2")
        });

        var result = JsonPatch.ApplyPartial(patch, document);

        Assert.False(result.IsFullyApplied);
        Assert.Empty(result.Applied);
        Assert.Equal(2, result.Failures.Count);
        Assert.Equal("Alice", result.Document!["name"]!.GetValue<string>());
    }

    [Fact]
    public void ApplyPartial_EmptyPatch_ReturnsUnmodifiedDocument()
    {
        var document = JsonNode.Parse("""{"name": "Alice"}""");
        var patch = new JsonPatchDocument(Array.Empty<PatchOperation>());

        var result = JsonPatch.ApplyPartial(patch, document);

        Assert.True(result.IsFullyApplied);
        Assert.Empty(result.Applied);
        Assert.Empty(result.Failures);
        Assert.Equal("Alice", result.Document!["name"]!.GetValue<string>());
    }

    [Fact]
    public void ApplyPartial_FailureContainsErrorMessage()
    {
        var document = JsonNode.Parse("""{"name": "Alice"}""");
        var patch = new JsonPatchDocument(new[]
        {
            PatchOperation.Remove("/nonexistent")
        });

        var result = JsonPatch.ApplyPartial(patch, document);

        Assert.Single(result.Failures);
        Assert.NotEmpty(result.Failures[0].Error);
    }

    [Fact]
    public void ApplyPartial_NullPatch_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            JsonPatch.ApplyPartial(null!, JsonNode.Parse("{}")));
    }
}
