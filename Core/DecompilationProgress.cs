namespace Decomp.Core;

/// <summary>
/// Describes a progress update emitted while a module or shader is being decompiled.
/// </summary>
public sealed record DecompilationProgress(
    string Message,
    int ProcessedFiles = 0,
    int TotalFiles = 0,
    bool IsCompleted = false);
