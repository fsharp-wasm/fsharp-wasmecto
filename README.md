# fsharp-wasmecto

**An Expecto-style test framework in F#, compiled to WebAssembly GC.** Tests are written with `testList` / `test` and run *inside* a WebAssembly GC engine, so
a test of the base library is checked in the environment where the library runs, not only under .NET.

```fsharp
open Wasmecto

[<Tests>]
let tests =
    testList "Stack" [
        test "push then pop" { Expect.equal (pop (push 1 empty)) (Some 1) "the pushed value comes back" }
    ]
```

## Why

[fsharp-wasm-core](https://github.com/fsharp-wasm/fsharp-wasm-core) is the F# base library of programs compiled by
[fsharp-wasm](https://github.com/fsharp-wasm/fsharp-wasm). A test that passes under .NET but fails as WebAssembly would be worse than none, and Expecto itself needs reflection, threads
and async. Wasmecto is a small subset (about 470 lines) that needs none of them; the design is in [docs/WASMECTO-SPEC.md](docs/WASMECTO-SPEC.md).

| file | what it is |
|---|---|
| `src/DSL.fs`, `src/Types.fs` | `testList`, `test`, the test and result types |
| `src/Expect.fs` | the assertions (`Expect.equal`, …) |
| `src/Runner.fs`, `src/ExnState.fs` | the runner and its exception state |
| `runtime/test-runner.js` | a host that instantiates the module, runs it and exits with the number of failures |
| `tests/` | tests of the base library: Dictionary, Math, Stack, StringBuilder |

## Status

**Experimental, and its runner is out of date.** `run-tests.sh` expects the layout of an older repository (the compiler and the base library as siblings of this
directory); in the current layout it does not run, and the tests that use the framework are not run by any CI. The base library's own tests currently run under .NET with Expecto
(`fsharp-wasm-core/tests`, 90 tests). Bringing this runner up to date is on the [roadmap](https://fsharp-wasm.github.io/docs/roadmap) ("tests that run everywhere").

## License

[Apache License 2.0](LICENSE); see [NOTICE](NOTICE).
