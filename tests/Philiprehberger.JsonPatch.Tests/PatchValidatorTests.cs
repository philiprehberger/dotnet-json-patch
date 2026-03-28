using System.Text.Json.Nodes;

namespace Philiprehberger.JsonPatch.Tests;

public class PatchValidatorTests
{
    [Fact]
    public void Validate_ValidPatch_ReturnsIsValid()
    {
        var document = JsonNode.Parse("""{"name": "Alice", "age": 30}""");
        var patch = new JsonPatchDocument(new[]
        {
            PatchOperation.Replace("/name", JsonValue.Create("Bob")),
            PatchOperation.Remove("/age")
        });

        var result = JsonPatch.Validate(patch, document);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_RemoveNonExistentPath_ReturnsError()
    {
        var document = JsonNode.Parse("""{"name": "Alice"}""");
        var patch = new JsonPatchDocument(new[]
        {
            PatchOperation.Remove("/nonexistent")
        });

        var result = JsonPatch.Validate(patch, document);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("nonexistent", result.Errors[0].Message);
    }

    [Fact]
    public void Validate_ReplaceNonExistentPath_ReturnsError()
    {
        var document = JsonNode.Parse("""{"name": "Alice"}""");
        var patch = new JsonPatchDocument(new[]
        {
            PatchOperation.Replace("/missing", JsonValue.Create("value"))
        });

        var result = JsonPatch.Validate(patch, document);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void Validate_TestWithWrongValue_ReturnsError()
    {
        var document = JsonNode.Parse("""{"name": "Alice"}""");
        var patch = new JsonPatchDocument(new[]
        {
            PatchOperation.Test("/name", JsonValue.Create("Bob"))
        });

        var result = JsonPatch.Validate(patch, document);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("Test failed", result.Errors[0].Message);
    }

    [Fact]
    public void Validate_UnknownOperation_ReturnsError()
    {
        var document = JsonNode.Parse("""{"name": "Alice"}""");
        var patch = new JsonPatchDocument(new[]
        {
            new PatchOperation("unknown", "/name")
        });

        var result = JsonPatch.Validate(patch, document);

        Assert.False(result.IsValid);
        Assert.Contains("Unknown operation", result.Errors[0].Message);
    }

    [Fact]
    public void Validate_NullPatch_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            JsonPatch.Validate(null!, JsonNode.Parse("{}")));
    }

    [Fact]
    public void Validate_MultipleErrors_ReturnsAllErrors()
    {
        var document = JsonNode.Parse("""{"name": "Alice"}""");
        var patch = new JsonPatchDocument(new[]
        {
            PatchOperation.Remove("/missing1"),
            PatchOperation.Replace("/missing2", JsonValue.Create("value"))
        });

        var result = JsonPatch.Validate(patch, document);

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
    }
}
