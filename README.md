# Decomp: Classic Taleworlds Engine Module Decompiler (CLI)

A high-performance, cross-platform command-line tool that decompiles compiled module text files (`.txt`) back into Python source code (`.py`) compatible with the official Module System.

This is a modernized, headless fork of the original WPF decompiler, rewritten to run natively on **.NET 10** across Linux, macOS, and Windows.

---

## Features

- **Headless & Cross-Platform:** No Windows-only UI dependencies. Works natively on Linux/macOS.
- **Directory-to-Directory Processing:** Automatically scans a game module directory and decompiles all main files in one run.
- **Multi-Game Support:** Supports multiple game versions and spin-offs based on the Taleworlds engine (such as Warband, With Fire And Sword, WSE, and Caribbean).

---

## Usage

Run the compiled executable pointing to your compiled module folder and your desired output directory:

`./Decomp <input_directory> <output_directory> [game_version]`

Example:

`./Decomp ~/.steam/steam/steamapps/common/Mount\&\Blade\ Warband/Modules/Native ./src_python VanillaWarband`

Supported Game/Engine Versions:

    VanillaClassic (Original Mount & Blade)

    VanillaWarband (M&B: Warband)

    VanillaWFS (M&B: With Fire & Sword)  

    WSE320 / WSE450 (Warband Script Enhancer)

    Caribbean (Caribbean! / Blood & Gold: Caribbean!)

🛠️ Compilation & Publishing
1. Everyday Development Builds

To test individual changes during development without cross-compiling, use the target-specific commands:
Bash

# To update the core engine binary (Required if you change core logic)
`dotnet build Decomp.csproj`

# To build the desktop GUI application
`dotnet build DecompilerGUI/DecompilerGUI.csproj`

2. Cross-Platform Publishing (Production Releases)

To build production-ready, self-contained single-file executables for all platforms, ensure the core engine is built, then trigger the publish commands:

indows

`dotnet publish DecompilerGUI/DecompilerGUI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:AssemblyName=Decomp`

`dotnet publish DecompilerGUI/ecompilerGUI.csproj -c Release -r win-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:AssemblyName=Decom`

macOS

`dotnet publish DecompilerGUI/DecompilerGUI.csproj -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:AssemblyName=Decom`

`dotnet publish DecompilerGUI/DecompilerGUI.csproj -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:AssemblyName=Decom`

Linux

`dotnet publish DecompilerGUI/DecompilerGUI.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:AssemblyName=Decom`

`dotnet publish DecompilerGUI/DecompilerGUI.csproj -c Release -r linux-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:AssemblyName=Decom`

⚠️ IMPORTANT!!!

This program is published solely for educational purposes and personal mod development.

Using this tool to decompile someone else's mod and publishing their assets, scripts, or code as your own without explicit consent from the original authors is highly discouraged and regarded as plagiarism. Please respect the modding community's hard work.