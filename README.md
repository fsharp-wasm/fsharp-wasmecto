# fsharp-wasmecto

<!-- status -->
[Status of every repository](https://fsharp-wasm.github.io/status/)
<!-- /status -->

<!-- platform -->
> **Part of the [fsharp-wasm platform](https://fsharp-wasm.github.io/projects/)**: tests. Builds on the compiler ([fsharp-wasm](https://github.com/fsharp-wasm/fsharp-wasm)). Used by [core](https://github.com/fsharp-wasm/fsharp-wasm-core), [fsharp-wasm](https://github.com/fsharp-wasm/fsharp-wasm).
> [All projects](https://fsharp-wasm.github.io/projects/) · [Roadmap](https://fsharp-wasm.github.io/docs/roadmap/) · [Tests](https://fsharp-wasm.github.io/tests/) · [Benchmarks](https://fsharp-wasm.github.io/benchmarks/) · [Live demos](https://fsharp-wasm.github.io/demos/)

**An Expecto-style test framework in F#, compiled to WebAssembly GC.** Tests are written with `testList` / `test` and run *inside* a WebAssembly GC engine, so
a test of the base library is checked in the environment where the library runs, not only under .NET.

```fsharp
open Wasmecto
open System.Collections.Generic

let tests =
    testList "Stack<T>" [
        testCase "Push / Pop LIFO" <| fun () ->
            let s = Stack<int>()
            s.Push 1; s.Push 2
            Expect.equal (s.Pop()) 2 "pop 2"
            Expect.equal (s.Pop()) 1 "pop 1"
    ]
```

## Why

[fsharp-wasm-core](https://github.com/fsharp-wasm/fsharp-wasm-core) is the F# base library of programs compiled by
[fsharp-wasm](https://github.com/fsharp-wasm/fsharp-wasm). A test that passes under .NET but fails as WebAssembly would be worse than none, and Expecto itself needs reflection, threads
and async. Wasmecto is a small subset (about 470 lines) that needs none of them; the design is in [docs/WASMECTO-SPEC.md](docs/WASMECTO-SPEC.md).

| file | what it is |
|---|---|
| `src/DSL.fs`, `src/Types.fs` | `testList`, `testCase` (and `test`), pending and focused variants; the test and result types |
| `src/Expect.fs` | the assertions (`Expect.equal`, …) |
| `src/Runner.fs`, `src/ExnState.fs` | the runner and its exception state |
| `runtime/test-runner.js` | a host that instantiates the module, runs it and exits with the number of failures |
| `tests/` | tests of the base library: Dictionary, Math, Stack, StringBuilder |

## Status

**Experimental, and its runner is out of date.** `run-tests.sh` expects the layout of an older repository (the compiler and the base library as siblings of this
directory); in the current layout it does not run, and the tests that use the framework are not run by any CI. The base library's own tests currently run under .NET with Expecto
(`fsharp-wasm-core/tests`, 90 tests). Bringing this runner up to date is on the [roadmap](https://fsharp-wasm.github.io/docs/roadmap) ("tests that run everywhere").

## Contributing

Welcome. [CONTRIBUTING](https://github.com/fsharp-wasm/.github/blob/develop/CONTRIBUTING.md) is the organisation's guide and [AGENTS.md](AGENTS.md) says how the platform is worked on (the compiler comes first: a fault found here
is fixed in the compiler, not worked around here).

- **Run the tests:** `bash run-tests.sh` compiles Wasmecto's own tests with the compiler (a checkout of fsharp-wasm next to this one) and runs them in Node.
- **A good first contribution:** An assertion helper that Expecto has and this lacks. Issues labelled [`good first issue`](https://github.com/fsharp-wasm/fsharp-wasmecto/labels/good%20first%20issue) are the places to start.
- **A change** goes to `develop` as a pull request, with a test that fails without it. Open an issue first for anything larger than a fix.

## License

[Apache License 2.0](LICENSE); see [NOTICE](NOTICE).
