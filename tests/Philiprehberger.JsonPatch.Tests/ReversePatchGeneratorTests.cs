using System.Text.Json.Nodes;

namespace Philiprehberger.JsonPatch.Tests;

public class ReversePatchGeneratorTests
{
    [Fact]
    public void GenerateReverse_AddOperation_ProducesRemove()
    {
        var original = JsonNode.Parse("""{"name": "Alice"}""");
        var patch = new JsonPatchDocument(new[]
        {
            PatchOperation.Add("/age", JsonValue.Create(30))
        });

        var reverse = JsonPatch.GenerateReverse(patch, original);

        Assert.Single(reverse.Operations);
        Assert.Equal("remove", reverse.Operations[0].Op);
        Assert.Equal("/age", reverse.Operations[0].Path);
    }

    [Fact]
    public void GenerateReverse_RemoveOperation_ProducesAddWithOriginalValue()
    {
        var original = JsonNode.Parse("""{"name": "Alice", "age": 30}""");
        var patch = new JsonPatchDocument(new[]
        {
            PatchOperation.Remove("/age")
        });

        var reverse = JsonPatch.GenerateReverse(patch, original);

        Assert.Single(reverse.Operations);
        Assert.Equal("add", reverse.Operations[0].Op);
        Assert.Equal("/age", reverse.Operations[0].Path);
        Assert.Equal(30, reverse.Operations[0].Value!.GetValue<int>());
    }

    [Fact]
    public void GenerateReverse_ReplaceOperation_RestoresOriginalValue()
    {
        var original = JsonNode.Parse("""{"name": "Alice"}""");
        var patch = new JsonPatchDocument(new[]
        {
            PatchOperation.Replace("/name", JsonValue.Create("Bob"))
        });

        var reverse = JsonPatch.GenerateReverse(patch, original);

        Assert.Single(reverse.Operations);
        Assert.Equal("replace", reverse.Operations[0].Op);
        Assert.Equal("/name", reverse.Operations[0].Path);
        Assert.Equal("Alice", reverse.Operations[0].Value!.GetValue<string>());
    }

    [Fact]
    public void GenerateReverse_MoveOperation_ReversesMoveDirection()
    {
        var original = JsonNode.Parse("""{"first": "Alice"}""");
        var patch = new JsonPatchDocument(new[]
        {
            PatchOperation.Move("/first", "/last")
        });

        var reverse = JsonPatch.GenerateReverse(patch, original);

        Assert.Single(reverse.Operations);
        Assert.Equal("move", reverse.Operations[0].Op);
        Assert.Equal("/last", reverse.Operations[0].From);
        Assert.Equal("/first", reverse.Operations[0].Path);
    }

    [Fact]
    public void GenerateReverse_AppliedToPatched_RestoresOriginal()
    {
        var original = JsonNode.Parse("""{"name": "Alice", "age": 30}""");
        var patch = new JsonPatchDocument(new[]
        {
            PatchOperation.Replace("/name", JsonValue.Create("Bob")),
            PatchOperation.Remove("/age"),
            PatchOperation.Add("/email", JsonValue.Create("bob@example.com"))
        });

        var patched = patch.Apply(original);
        var reverse = JsonPatch.GenerateReverse(patch, JsonNode.Parse("""{"name": "Alice", "age": 30}"""));
        var restored = reverse.Apply(patched);

        Assert.Equal("Alice", restored!["name"]!.GetValue<string>());
        Assert.Equal(30, restored["age"]!.GetValue<int>());
        Assert.Null(restored["email"]);
    }

    [Fact]
    public void GenerateReverse_NullPatch_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            JsonPatch.GenerateReverse(null!, JsonNode.Parse("{}")));
    }
}
