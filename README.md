# CompilerBrain

> NOTE: This project is completely under development, and most of the expected implementation is not yet complete. If the description below sparks your interest, please consider watching this repository. A minimal working version will be available as a dotnet tool soon.

CLI Coding Agent not for Vibe-coding, for C# Expert.

Most coding agents are general-purpose and perform file-based operations (grep, build, etc...). While LSP-based tools like [Serena](https://github.com/oraios/serena) exist, they serve as supplementary aids and don't fully leverage language semantics.

CompilerBrain parses solutions and operates against the C# Compiler (Roslyn) via in-memory compilation. Code modifications are additions to an immutable compilation, enabling fast and accurate diagnostics without dirtying files. Since everything is handled in-memory without requiring git worktree, multiple sub-agents can work in parallel. Code exploration leverages fast searches on in-memory SyntaxTrees and symbol analysis. Code editing is semantics-aware and operates on minimal scopes, saving tokens.

The system prompt is specialized for C#, providing rich context for C# developers, enabling it to function as a more complete C# Expert.
