# Warband Module Decompiler

A cross-platform decompiler for classic TaleWorlds Engine module resources. The project reads compiled module text files and supported shader resources, then emits Python 3 source files compatible with the classic Module System workflow.

The repository targets **.NET 10** and **C# 14** and builds on Windows, macOS, and Linux. It contains a reusable core library, an asynchronous command-line application, and an Avalonia desktop application.

## Features

The decompiler preserves the original TaleWorlds resource formats while providing a modern execution model. Module directories can be processed as a unit, shared identifier tables are initialized before dependent resources are emitted, and progress updates are exposed to both CLI and GUI consumers. File paths are normalized at runtime, UTF-8 input is validated, and generated Python files use stable UTF-8 output with Unix line endings.

The supported game and engine profiles are:

| Profile | Description |
| --- | --- |
| `VanillaClassic` | Original Mount & Blade resource formats |
| `VanillaWarband` | Mount & Blade: Warband 1.153-compatible operators |
| `Warband1171` | Mount & Blade: Warband 1.171-compatible operators |
| `VanillaWFS` | Mount & Blade: With Fire & Sword |
| `WSE320` | Warband Script Enhancer 3.2.0 |
| `WSE450` | Warband Script Enhancer 4.5.0 |
| `Caribbean` | Caribbean! and Blood & Gold: Caribbean! |

Supported resource extensions are `.txt`, `.vsh`, `.psh`, `.fxc`, and `.glsl`. DirectX text shaders and GLSL files are preserved with the standard shader header. DirectX bytecode files use DirectX disassembly on Windows when available and fall back to `dxbc-disassembler` on other platforms.

## CLI usage

Build the complete solution first:

```bash
dotnet build Decomp.sln
```

Run the command-line decompiler with an input file or module directory. The output path is a directory containing the generated Python files.

```bash
dotnet run --project DecompilerCLI/DecompilerCLI.csproj -- \
  ./path/to/module \
  ./path/to/decompiled \
  VanillaWarband
```

For a single resource file:

```bash
dotnet run --project DecompilerCLI/DecompilerCLI.csproj -- \
  ./path/to/module/scripts.txt \
  ./path/to/decompiled \
  VanillaWarband
```

If the output directory is omitted, the decompiler creates a `decompiled` directory next to the input module or resource.

The CLI supports cancellation with `Ctrl+C`. The core operation also accepts a `CancellationToken` and reports typed progress through `DecompilationProgress`.

## Desktop application

Build and run the Avalonia application with:

```bash
dotnet run --project DecompilerGUI/DecompilerGUI.csproj
```

The GUI uses ReactiveUI’s Avalonia main-thread scheduler. File processing, metadata reads, shader disassembly, output copying, and module orchestration run through asynchronous APIs, while progress, status messages, and logs are marshaled back to the Avalonia UI thread.

## Publishing

The application can be published as a self-contained single-file executable for the supported runtime identifiers:

```bash
dotnet publish DecompilerGUI/DecompilerGUI.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -p:PublishSingleFile=true
```

Replace `linux-x64` with `linux-arm64`, `win-x64`, `win-arm64`, `osx-x64`, or `osx-arm64` as required. The CLI can be published using the same options with `DecompilerCLI/DecompilerCLI.csproj`.

## Python 3 compiler workflow

The decompiler emits **Python 3-compatible** module source. To compile the generated module system back into the classic `.txt` resources, use the modern community compiler fork [Vetrogor/wreck](https://github.com/Vetrogor/wreck), which is the recommended toolchain for this project.

Before compiling a generated module, review its source and verify that you have permission to use the original module’s assets and scripts. Decompilation and redistribution should respect the rights of the original authors and the applicable game and mod licenses.

## Architecture

The solution is organized into three layers:

| Project | Responsibility |
| --- | --- |
| `Decomp.csproj` | Core parsers, resource handlers, operator tables, shader support, and asynchronous orchestration |
| `DecompilerCLI/DecompilerCLI.csproj` | Cancellation-aware command-line interface and progress reporting |
| `DecompilerGUI/DecompilerGUI.csproj` | Avalonia and ReactiveUI desktop interface |

The legacy resource handlers remain synchronous internally because they expose the established TaleWorlds parsing API. They are isolated behind the asynchronous `Decompiler.DecompileAsync` coordinator, which performs asynchronous file discovery, header reads, variable copying, shader processing, cancellation checks, and progress reporting without blocking the UI thread.

## Development requirements

Install the .NET 10 SDK and use a shell appropriate for the host platform. The repository applies C# 14, nullable reference types, implicit usings, deterministic builds, and invariant numeric formatting through `Directory.Build.props`.

This project is intended for educational use and personal mod development. Do not publish another author’s assets, scripts, or source code as your own without explicit permission.
