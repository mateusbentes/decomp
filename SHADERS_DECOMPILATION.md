# Shader Decompilation Instructions

This guide explains how to decompile shaders for Mount & Blade games on Windows, Linux, and macOS.

## Prerequisites

### Windows
- No additional tools are required for Windows. The decompiler uses Direct3D natively.

### Linux/macOS
You need to install the following tools:

#### 1. For Warband (OpenGL shaders)
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

#### 2. For other games (Direct3D shaders)
Install `dxbc-disassembler` (part of DirectXShaderCompiler):
- Download the latest release from [DirectXShaderCompiler GitHub](https://github.com/microsoft/DirectXShaderCompiler/releases).
- Extract the binary (`dxbc-disassembler` or `dxbc-disassembler.exe`) and place it in:
  - Your system `PATH`, **or**
  - The project's root directory.

Troubleshooting:
#### Error: "Platform not supported"

  - This error occurs when trying to decompile Direct3D shaders on Linux/macOS without dxbc-disassembler.

  - Install dxbc-disassembler as described in the Prerequisites section.

#### Error: "Failed to decompile shader"
- Ensure the input file is a valid shader file (`.fx`, `.glsl`, `.fxc`).
- Check the file permissions of the shader files and ensure the tools in your PATH have execution permissions (`chmod +x`).
