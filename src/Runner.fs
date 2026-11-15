/// Test runner — flattens the tree, executes sequentially, emits TAP 13.
/// WasmGC-compatible version: no reflection, no async; Fable.Core only for the one host import (`wasmecto.writeLine`).
/// Exception handling via ExnState (module-level mutable) rather than typed catch.
module Wasmecto.Runner

open Fable.Core
open Wasmecto

// ─── Private: flat test record ───────────────────────────────────────────────

[<Struct>]
type private FlatTest =
    { FullName : string
      Code     : TestCode
      State    : FocusState }

// ─── Private: tree flattening ────────────────────────────────────────────────

let private flatten (test: Test) : FlatTest list =
    let rec go (prefix: string) (parentState: FocusState) (t: Test) (acc: FlatTest list) : FlatTest list =
        let join a b = if a = "" then b else a + "/" + b
        match t with
        | TestCase(name, code, state) ->
            let fullName = join prefix name
            let resolved =
                match parentState, state with
                | Pending, _      -> Pending
                | _, Focused      -> Focused
                | Focused, Normal -> Focused
                | _               -> state
            { FullName = fullName; Code = code; State = resolved } :: acc
        | TestList(name, tests, state) ->
            let prefix' = join prefix name
            let resolved =
                match parentState, state with
                | Pending, _ -> Pending
                | _, s       -> s
            List.foldBack (go prefix' resolved) tests acc
        | TestLabel(name, inner, state) ->
            let prefix' = join prefix name
            let resolved =
                match parentState, state with
                | Pending, _ -> Pending
                | _, s       -> s
            go prefix' resolved inner acc
    go "" Normal test []

// ─── Private: detect focus ───────────────────────────────────────────────────

let private hasFocus (tests: FlatTest list) =
    List.exists (fun t -> t.State = Focused) tests

// ─── Public: run ─────────────────────────────────────────────────────────────

/// Run a single Test tree.
/// writeLine: called once per TAP output line.
/// Returns: number of failures (0 = all passed).
///
///
/// WasmGC note: exception handling uses ExnState (module-level mutable) rather
/// than typed catch patterns, because the current fsharp-wasm runtime carries
/// exceptions as a zero-payload signal tag and `catch_all` receives no value.
/// A future compiler sprint will implement typed catch; the Runner API is
/// designed to be unchanged when that upgrade lands.
let run (writeLine: string -> unit) (test: Test) : int =
    let flat    = flatten test
    let total   = flat.Length
    let focused = hasFocus flat

    writeLine "TAP version 13"
    writeLine (sprintf "1..%d" total)

    let mutable n      = 0
    let mutable failed = 0

    for ft in flat do
        n <- n + 1

        let skip =
            match ft.State with
            | Pending               -> Some "pending"
            | Normal when focused   -> Some "not focused"
            | _                     -> None

        match skip with
        | Some reason ->
            writeLine (sprintf "ok %d - %s # SKIP %s" n ft.FullName reason)
        | None ->
            // Reset ExnState before each test (WasmGC exception workaround)
            ExnState.reset ()

            let result =
                match ft.Code with
                | Sync f ->
                    try
                        f ()
                        Passed
                    with _ ->
                        // Under WasmGC catch_all, we can't inspect the exception object.
                        // ExnState carries the message set by Expect.fail before throwing.
                        match ExnState.Kind with
                        | 1 -> Failed ExnState.Msg
                        | _ -> Errored "unexpected runtime exception"

            match result with
            | Passed ->
                writeLine (sprintf "ok %d - %s" n ft.FullName)
            | Failed msg ->
                failed <- failed + 1
                writeLine (sprintf "not ok %d - %s" n ft.FullName)
                writeLine "  ---"
                writeLine (sprintf "  message: %s" msg)
                writeLine "  ..."
            | Errored msg ->
                failed <- failed + 1
                writeLine (sprintf "not ok %d - %s" n ft.FullName)
                writeLine "  ---"
                writeLine (sprintf "  message: EXCEPTION: %s" msg)
                writeLine "  ..."
            | Skipped reason ->
                writeLine (sprintf "ok %d - %s # SKIP %s" n ft.FullName reason)

    writeLine ""
    if failed = 0 then
        writeLine (sprintf "# All %d tests passed." total)
    else
        writeLine (sprintf "# %d of %d tests FAILED." failed total)

    failed

/// Run multiple top-level Test trees (collected via register).
let runAll (writeLine: string -> unit) (tests: Test list) : int =
    let combined = TestList("", tests, Normal)
    run writeLine combined

// ─── Registry: explicit test registration (no reflection) ────────────────────

let private registry = System.Collections.Generic.List<Test>()

let register (tests: Test list) =
    for t in tests do registry.Add(t)

/// The host's `writeLine`: the module imports it as `wasmecto.writeLine` (a string in, nothing out), so the runner
/// needs no closure parameter (an exported `wasmecto_run` that took the printer as an F# function could not be called from a host).
[<Import("writeLine", "wasmecto")>]
let hostWriteLine (line: string) : unit = nativeOnly

/// Runs every registered suite, printing TAP through `wasmecto.writeLine`; the result is the number of failures (0 = all passed).
/// Call it after module initialisation, which is when `register` has run. A compiled module exports only the functions of its
/// last file, so the test project's last file wraps this: `let wasmecto_run () : int = Wasmecto.Runner.runRegistered ()`.
let runRegistered () : int =
    // Convert List<Test> to F# list without List.ofSeq (BCL gap)
    let mutable acc = []
    let mutable i = registry.Count - 1
    while i >= 0 do
        acc <- registry.[i] :: acc
        i <- i - 1
    runAll hostWriteLine acc
