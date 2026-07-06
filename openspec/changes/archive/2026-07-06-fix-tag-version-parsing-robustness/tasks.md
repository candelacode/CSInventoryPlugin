## 1. Implementation

- [x] 1.1 Update `ParseTagAsVersion` to strip `-` suffix and pad short segments to 4 with `.0`
- [x] 1.2 Verify the modified method compiles and handles all tag formats from the spec

## 2. Tests

- [x] 2.1 Update `ParseTagAsVersion_SingleSegmentWithVPrefix_PropagatesParseError` to expect `1.0.0.0` instead of `ArgumentException`
- [x] 2.2 Add test for two-segment tag `v1.0` → `1.0.0.0`
- [x] 2.3 Add test for three-segment tag `v1.0.0` → `1.0.0.0`
- [x] 2.4 Add test for pre-release suffix `v1.2.3.4-beta` → `1.2.3.4`
- [x] 2.5 Add test for pre-release suffix with short tag `v1-beta` → `1.0.0.0`
- [x] 2.6 Ensure existing tests (`LowercaseVPrefix`, `UppercaseVPrefix`, `NoPrefix`, `UnparseableTag`) still pass or are updated

## 3. Build and verify

- [x] 3.1 `dotnet build` passes with no errors
- [x] 3.2 `dotnet test` all tests pass
- [x] 3.3 `openspec validate --strict` passes
