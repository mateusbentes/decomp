# Shader Decompilation Instructions

This guide explains how to decompile shaders for Mount & Blade games on Windows, Linux, and macOS.

## Supported Shader Formats

| Extension | Type                          | Format       | Tool required          |
|-----------|-------------------------------|--------------|------------------------|
| `.vsh`    | DirectX Vertex Shader         | Plain text   | None (read directly)   |
| `.psh`    | DirectX Pixel Shader          | Plain text   | None (read directly)   |
| `.fxc`    | DirectX Compiled Shader       | Binary       | `d3dcompiler_47.dll` (Windows) or `dxbc-disassembler` (Linux/macOS) |
| `.glsl`   | OpenGL Shader                 | Text/Binary  | `spirv-cross`          |

### Notes on `.vsh` and `.psh`
Mount & Blade (all versions including Warband, With Fire & Sword and Caribbean) stores
vertex shaders (`.vsh`) and pixel shaders (`.psh`) as **plain-text HLSL assembly** files.
They are already human-readable and do **not** require any external tool — the decompiler
reads them directly and prepends the standard shader header to the output.

## Prerequisites

### Windows
- **`.vsh` / `.psh`**: No tools required.
- **`.fxc`**: No additional tools required. The decompiler uses `d3dcompiler_47.dll` natively via Direct3D.
- **`.glsl`**: Install `spirv-cross` (see Linux/macOS section below).

### Linux/macOS
- **`.vsh` / `.psh`**: No tools required.
- **`.fxc`**: Install `dxbc-disassembler` (see below).
- **`.glsl`**: Install `spirv-cross` (see below).

#### 1. For OpenGL shaders (`.glsl`)
Install `spirv-cross`:
- **Linux (Debian/Ubuntu)**:
  ```bash
  sudo apt-get install spirv-cross
  ```
- **macOS (Homebrew)**:
  ```bash
  brew install spirv-cross
  ```
- **Manual installation**:
  Download from [SPIRV-Cross GitHub](https://github.com/KhronosGroup/SPIRV-Cross) and add it to your `PATH`.

#### 2. For compiled DirectX shaders (`.fxc`) on Linux/macOS
Install `dxbc-disassembler` (part of DirectXShaderCompiler):
- Download the latest release from [DirectXShaderCompiler GitHub](https://github.com/microsoft/DirectXShaderCompiler/releases).
- Extract the binary (`dxbc-disassembler` or `dxbc-disassembler.exe`) and place it in:
  - Your system `PATH`, **or**
  - The project's root directory.

## Troubleshooting

#### Error: "Platform not supported"
- This error occurs when trying to decompile `.fxc` shaders on Linux/macOS without `dxbc-disassembler`.
- Install `dxbc-disassembler` as described in the Prerequisites section.

#### Error: "Failed to decompile shader"
- Ensure the input file is a valid shader file (`.vsh`, `.psh`, `.fxc`, `.glsl`).
- Check the file permissions of the shader files and ensure the tools in your `PATH` have execution permissions:
  ```bash
  chmod +x /usr/local/bin/spirv-cross
  chmod +x /usr/local/bin/dxbc-disassembler
  ```

#### `.vsh` / `.psh` output is empty or garbled
- These files should be plain UTF-8 text. If the output is garbled, the file may be corrupted
  or from an unsupported engine version.
