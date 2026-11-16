#!/usr/bin/env bash
# Compile Wasmecto's own tests with the fsharp-wasm compiler and run them in Node.
#
#   ./run-tests.sh
#
# Needs a checkout of fsharp-wasm (the compiler) next to this repository, with its submodules (core);
# FSHARP_WASM and FSHARP_WASM_CORE_DIR say where they are if they are somewhere else.
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
FSHARP_WASM="${FSHARP_WASM:-$HERE/../fsharp-wasm}"
# the base library: the compiler's `core` submodule (the revision it is built and tested with), or a sibling checkout
CORE="${FSHARP_WASM_CORE_DIR:-$FSHARP_WASM/core}"
[ -d "$CORE/FSharp.Wasm.Core" ] || CORE="$HERE/../fsharp-wasm-core"
export FSHARP_WASM_CORE_DIR="$CORE"          # the project reference of src/Wasmecto.fsproj follows it
OUT="$HERE/dist"

[ -d "$FSHARP_WASM/tools/FSharp.Wasm.Cli" ] || { echo "fsharp-wasm not found at $FSHARP_WASM (set FSHARP_WASM)" >&2; exit 2; }
[ -d "$CORE/FSharp.Wasm.Core" ]             || { echo "fsharp-wasm-core not found at $CORE (set FSHARP_WASM_CORE_DIR)" >&2; exit 2; }

# The tests reach fsharp-wasm-core through a ProjectReference; the front end wants its reference assembly on disk.
echo "==> Building the project references (reference assemblies)..."
dotnet build "$CORE/FSharp.Wasm.Core/FSharp.Wasm.Core.fsproj" -c Release --nologo -v q

echo "==> Compiling Wasmecto and its tests with fsharp-wasm..."
rm -rf "$OUT"
dotnet run -c Release --project "$FSHARP_WASM/tools/FSharp.Wasm.Cli" -- "$HERE/tests/WasmTests.fsproj" --outDir "$OUT" --noCache

WASM="$(ls "$OUT"/*.wasm | head -1)"
command -v wasm-tools >/dev/null && wasm-tools validate "$WASM" --features gc

echo "==> Running $(basename "$WASM") in Node..."
node "$FSHARP_WASM/tests/Wasm/run.mjs" "$WASM"
