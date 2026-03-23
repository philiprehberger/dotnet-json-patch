# Philiprehberger.JsonPatch

[![CI](https://github.com/philiprehberger/dotnet-json-patch/actions/workflows/ci.yml/badge.svg)](https://github.com/philiprehberger/dotnet-json-patch/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Philiprehberger.JsonPatch.svg)](https://www.nuget.org/packages/Philiprehberger.JsonPatch)
[![License](https://img.shields.io/github/license/philiprehberger/dotnet-json-patch)](LICENSE)

RFC 6902 JSON Patch operations for System.Text.Json — apply, generate, and parse patch documents.

## Installation

```bash
dotnet add package Philiprehberger.JsonPatch
```

## Usage

### Apply a Patch

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

### Test Operations

```csharp
using System.Text.Json.Nodes;
using Philiprehberger.JsonPatch;

var document = JsonNode.Parse("""{"name": "Alice"}""");

// Test passes — value matches
var ops = new[] { PatchOperation.Test("/name", JsonValue.Create("Alice")) };
var result = JsonPatch.Apply(document, ops);

// Test fails — throws InvalidOperationException
var failing = new[] { PatchOperation.Test("/name", JsonValue.Create("Bob")) };
JsonPatch.Apply(document, failing); // throws
```

## API

### `JsonPatch`

| Method | Description |
|--------|-------------|
| `Apply(JsonNode?, IEnumerable<PatchOperation>)` | Apply patch operations to a document, returning a modified copy |
| `Diff(JsonNode?, JsonNode?)` | Generate patch operations that transform one document into another |
| `Parse(string)` | Parse a JSON array of RFC 6902 operations into a `JsonPatchDocument` |

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

## Development

```bash
dotnet build src/Philiprehberger.JsonPatch.csproj --configuration Release
```

## License

MIT
