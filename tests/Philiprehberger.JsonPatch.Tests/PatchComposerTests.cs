using System.Text.Json.Nodes;

namespace Philiprehberger.JsonPatch.Tests;

public class PatchComposerTests
{
    [Fact]
    public void Compose_TwoPatches_ConcatenatesOperations()
    {
        var first = new JsonPatchDocument(new[]
        {
            PatchOperation.Add("/name", JsonValue.Create("Alice"))
        });
        var second = new JsonPatchDocument(new[]
        {
            PatchOperation.Add("/age", JsonValue.Create(30))
        });

        var composed = JsonPatch.Compose(first, second);

        Assert.Equal(2, composed.Operations.Count);
        Assert.Equal("/name", composed.Operations[0].Path);
        Assert.Equal("/age", composed.Operations[1].Path);
    }

    [Fact]
    public void Compose_AdjacentReplaceSamePath_CollapsesToSecond()
    {
        var first = new JsonPatchDocument(new[]
        {
            PatchOperation.Replace("/name", JsonValue.Create("Bob"))
        });
        var second = new JsonPatchDocument(new[]
        {
            PatchOperation.Replace("/name", JsonValue.Create("Charlie"))
        });

        var composed = JsonPatch.Compose(first, second);

        Assert.Single(composed.Operations);
        Assert.Equal("replace", composed.Operations[0].Op);
        Assert.Equal("Charlie", composed.Operations[0].Value!.GetValue<string>());
    }

    [Fact]
    public void Compose_NonAdjacentReplaceSamePath_KeepsBoth()
    {
        var first = new JsonPatchDocument(new[]
        {
            PatchOperation.Replace("/name", JsonValue.Create("Bob")),
            PatchOperation.Add("/age", JsonValue.Create(25))
        });
        var second = new JsonPatchDocument(new[]
        {
            PatchOperation.Replace("/name", JsonValue.Create("Charlie"))
        });

        var composed = JsonPatch.Compose(first, second);

        Assert.Equal(3, composed.Operations.Count);
    }

    [Fact]
    public void Compose_EmptyFirst_ReturnsCopyOfSecond()
    {
        var first = new JsonPatchDocument(Array.Empty<PatchOperation>());
        var second = new JsonPatchDocument(new[]
        {
            PatchOperation.Add("/name", JsonValue.Create("Alice"))
        });

        var composed = JsonPatch.Compose(first, second);

        Assert.Single(composed.Operations);
        Assert.Equal("add", composed.Operations[0].Op);
    }

    [Fact]
    public void Compose_ProducesEquivalentResult()
    {
        var document = JsonNode.Parse("""{"name": "Alice", "age": 30}""");
        var first = new JsonPatchDocument(new[]
        {
            PatchOperation.Replace("/name", JsonValue.Create("Bob"))
        });
        var second = new JsonPatchDocument(new[]
        {
            PatchOperation.Add("/email", JsonValue.Create("bob@example.com"))
        });

        var composed = JsonPatch.Compose(first, second);
        var result = composed.Apply(document);

        Assert.Equal("Bob", result!["name"]!.GetValue<string>());
        Assert.Equal("bob@example.com", result["email"]!.GetValue<string>());
    }

    [Fact]
    public void Compose_NullFirst_ThrowsArgumentNullException()
    {
        var second = new JsonPatchDocument(Array.Empty<PatchOperation>());
        Assert.Throws<ArgumentNullException>(() => JsonPatch.Compose(null!, second));
    }

    [Fact]
    public void Compose_NullSecond_ThrowsArgumentNullException()
    {
        var first = new JsonPatchDocument(Array.Empty<PatchOperation>());
        Assert.Throws<ArgumentNullException>(() => JsonPatch.Compose(first, null!));
    }
}
