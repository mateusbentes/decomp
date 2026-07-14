# Decomp: Taleworlds Engine Module Decompiler (CLI)

A high-performance, cross-platform command-line tool that decompiles compiled module text files (`.txt`) back into Python source code (`.py`) compatible with the official Module System.

This is a modernized, headless fork of the original WPF decompiler, rewritten to run natively on **.NET 10** across Linux, macOS, and Windows.

---

## Features

- **Headless & Cross-Platform:** No Windows-only UI dependencies. Works natively on Linux/macOS.
- **Directory-to-Directory Processing:** Automatically scans a game module directory and decompiles all main files in one run.
- **Multi-Game Support:** Supports multiple game versions and spin-offs based on the Taleworlds engine (such as Warband, WSE, and Caribbean).

---

## Usage

Run the compiled executable pointing to your compiled module folder and your desired output directory:

`./Decomp <input_directory> <output_directory> [game_version]`

Example:

`./Decomp ~/.steam/steam/steamapps/common/Mount\&\Blade\ Warband/Modules/Native ./src_python VanillaWarband`

Supported Game/Engine Versions:

    VanillaClassic (Original Mount & Blade)

    VanillaWarband (M&B: Warband)

    WSE320 / WSE450 (Warband Script Enhancer)

    Caribbean (Caribbean! / Blood & Gold: Caribbean!)

⚠️ IMPORTANT!!!

This program is published solely for educational purposes and personal mod development.

Using this tool to decompile someone else's mod and publishing their assets, scripts, or code as your own without explicit consent from the original authors is highly discouraged and regarded as plagiarism. Please respect the modding community's hard work.