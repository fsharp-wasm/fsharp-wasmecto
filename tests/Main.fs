/// Entry point — register all test modules.
/// A compiled module exports the functions of its last file, so this file exports `wasmecto_run`
/// (the host calls it; the module's initialiser has registered the suites by then).
module WasmTests.Main

open Wasmecto
open Wasmecto.Runner
open WasmTests.TestsMath
open WasmTests.TestsDictionary
open WasmTests.TestsStack
open WasmTests.TestsStringBuilder

// Register all test suites (module-initializer runs at WASM startup)
do
    register [
        MathTests.tests
        DictionaryTests.tests
        StackTests.tests
        StringBuilderTests.tests
    ]

let wasmecto_run () : int = runRegistered ()
