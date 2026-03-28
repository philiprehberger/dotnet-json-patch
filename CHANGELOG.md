# Changelog

## 0.2.0 (2026-03-28)

- Add reverse patch generation via `GenerateReverse()` for producing undo patches
- Add patch validation via `Validate()` for pre-checking whether a patch can be cleanly applied
- Add patch composition via `Compose()` for merging two patches into a single equivalent patch
- Add partial application via `ApplyPartial()` for applying operations individually with failure collection
- Add `ValidationResult`, `ValidationError`, `PartialApplicationResult`, and `OperationFailure` types
- Add unit test project with xUnit

## 0.1.1 (2026-03-23)

- Shorten package description to meet 120-character limit

## 0.1.0 (2026-03-21)

- Initial release
- RFC 6902 JSON Patch operations (add, remove, replace, move, copy, test)
- RFC 6901 JSON Pointer navigation
- Diff generation between two JSON documents
