# Philiprehberger.JsonPatch

[![CI](https://github.com/philiprehberger/dotnet-json-patch/actions/workflows/ci.yml/badge.svg)](https://github.com/philiprehberger/dotnet-json-patch/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Philiprehberger.JsonPatch.svg)](https://www.nuget.org/packages/Philiprehberger.JsonPatch)
[![Last updated](https://img.shields.io/github/last-commit/philiprehberger/dotnet-json-patch)](https://github.com/philiprehberger/dotnet-json-patch/commits/main)

RFC 6902 JSON Patch operations for System.Text.Json — apply, generate, and parse patch documents.

## Installation

```bash
dotnet add package Philiprehberger.JsonPatch
```

## Usage

```csharp
using System.Text.Json.Nodes;
using Philiprehberger.JsonPatch;

var document = JsonNode.Parse("""{"name": "Alice", "age": 30}""");

var operations = new[]
{
    PatchOperation.Replace("/name", JsonValue.Create("Bob")),
    PatchOperation.Add("/email", JsonValue.Create("bob@example.com")),
    PatchOperation.Remove("/age")
};

var result = JsonPatch.Apply(document, operations);
// {"name":"Bob","email":"bob@example.com"}
```

### Generate a Diff

```csharp
using System.Text.Json.Nodes;
using Philiprehberger.JsonPatch;

var before = JsonNode.Parse("""{"name": "Alice", "age": 30}""");
var after = JsonNode.Parse("""{"name": "Alice", "age": 31, "email": "alice@example.com"}""");

var operations = JsonPatch.Diff(before, after);

foreach (var op in operations)
    Console.WriteLine($"{op.Op} {op.Path}");
// replace /age
// add /email
```

### Parse from JSON

```csharp
using Philiprehberger.JsonPatch;

var json = """
[
  { "op": "replace", "path": "/name", "value": "Bob" },
  { "op": "remove", "path": "/age" }
]
""";

var patchDoc = JsonPatch.Parse(json);
var result = patchDoc.Apply(JsonNode.Parse("""{"name": "Alice", "age": 30}"""));
// {"name":"Bob"}
```

### Reverse Patch Generation

```csharp
using System.Text.Json.Nodes;
using Philiprehberger.JsonPatch;

var original = JsonNode.Parse("""{"name": "Alice", "age": 30}""");
var patch = new JsonPatchDocument(new[]
{
    PatchOperation.Replace("/name", JsonValue.Create("Bob")),
    PatchOperation.Remove("/age")
});

var patched = patch.Apply(original);
var reverse = JsonPatch.GenerateReverse(patch, JsonNode.Parse("""{"name": "Alice", "age": 30}"""));
var restored = reverse.Apply(patched);
// restored = {"name":"Alice","age":30}
```

### Patch Validation

```csharp
using System.Text.Json.Nodes;
using Philiprehberger.JsonPatch;

var document = JsonNode.Parse("""{"name": "Alice"}""");
var patch = new JsonPatchDocument(new[]
{
    PatchOperation.Remove("/nonexistent")
});

var result = JsonPatch.Validate(patch, document);
// result.IsValid == false
// result.Errors[0].Message contains "does not exist"
```

### Patch Composition

```csharp
using System.Text.Json.Nodes;
using Philiprehberger.JsonPatch;

var first = new JsonPatchDocument(new[]
{
    PatchOperation.Replace("/name", JsonValue.Create("Bob"))
});
var second = new JsonPatchDocument(new[]
{
    PatchOperation.Add("/email", JsonValue.Create("bob@example.com"))
});

var composed = JsonPatch.Compose(first, second);
var result = composed.Apply(JsonNode.Parse("""{"name": "Alice"}"""));
// {"name":"Bob","email":"bob@example.com"}
```

### Partial Application

```csharp
using System.Text.Json.Nodes;
using Philiprehberger.JsonPatch;

var document = JsonNode.Parse("""{"name": "Alice"}""");
var patch = new JsonPatchDocument(new[]
{
    PatchOperation.Replace("/name", JsonValue.Create("Bob")),
    PatchOperation.Remove("/nonexistent"),
    PatchOperation.Add("/age", JsonValue.Create(30))
});

var result = JsonPatch.ApplyPartial(patch, document);
// result.IsFullyApplied == false
// result.Applied.Count == 2
// result.Failures.Count == 1
// result.Document = {"name":"Bob","age":30}
```

## API

### `JsonPatch`

| Method | Description |
|--------|-------------|
| `Apply(JsonNode?, IEnumerable<PatchOperation>)` | Apply patch operations to a document, returning a modified copy |
| `Diff(JsonNode?, JsonNode?)` | Generate patch operations that transform one document into another |
| `Parse(string)` | Parse a JSON array of RFC 6902 operations into a `JsonPatchDocument` |
| `GenerateReverse(JsonPatchDocument, JsonNode?)` | Generate a reverse patch that undoes the given forward patch |
| `Validate(JsonPatchDocument, JsonNode?)` | Pre-check whether a patch can be cleanly applied to a document |
| `Compose(JsonPatchDocument, JsonPatchDocument)` | Merge two patches into a single equivalent patch |
| `ApplyPartial(JsonPatchDocument, JsonNode?)` | Apply operations individually, collecting successes and failures |

### `JsonPatchDocument`

| Member | Description |
|--------|-------------|
| `JsonPatchDocument(IEnumerable<PatchOperation>)` | Create a patch document from a sequence of operations |
| `Operations` | Read-only list of patch operations |
| `Apply(JsonNode?)` | Apply all operations to a document copy |

### `PatchOperation`

| Member | Description |
|--------|-------------|
| `Op` | Operation type (add, remove, replace, move, copy, test) |
| `Path` | Target JSON Pointer path |
| `From` | Source path for move/copy operations |
| `Value` | Value for add/replace/test operations |
| `Add(string, JsonNode?)` | Create an add operation |
| `Remove(string)` | Create a remove operation |
| `Replace(string, JsonNode?)` | Create a replace operation |
| `Move(string, string)` | Create a move operation |
| `Copy(string, string)` | Create a copy operation |
| `Test(string, JsonNode?)` | Create a test operation |

### `ValidationResult`

| Member | Description |
|--------|-------------|
| `IsValid` | Whether the patch can be cleanly applied without errors |
| `Errors` | List of validation errors encountered |

### `ValidationError`

| Member | Description |
|--------|-------------|
| `Operation` | The patch operation that failed validation |
| `Message` | Description of why the operation would fail |

### `PartialApplicationResult`

| Member | Description |
|--------|-------------|
| `Document` | The document after applying all successful operations |
| `Applied` | Operations that were successfully applied |
| `Failures` | Operations that failed with their error messages |
| `IsFullyApplied` | Whether all operations were successfully applied |

### `OperationFailure`

| Member | Description |
|--------|-------------|
| `Operation` | The patch operation that failed |
| `Error` | Error message describing why the operation failed |

## Development

```bash
dotnet build src/Philiprehberger.JsonPatch.csproj --configuration Release
```

## Support

If you find this project useful:

⭐ [Star the repo](https://github.com/philiprehberger/dotnet-json-patch)

🐛 [Report issues](https://github.com/philiprehberger/dotnet-json-patch/issues?q=is%3Aissue+is%3Aopen+label%3Abug)

💡 [Suggest features](https://github.com/philiprehberger/dotnet-json-patch/issues?q=is%3Aissue+is%3Aopen+label%3Aenhancement)

❤️ [Sponsor development](https://github.com/sponsors/philiprehberger)

🌐 [All Open Source Projects](https://philiprehberger.com/open-source-packages)

💻 [GitHub Profile](https://github.com/philiprehberger)

🔗 [LinkedIn Profile](https://www.linkedin.com/in/philiprehberger)

## License

[MIT](LICENSE)
