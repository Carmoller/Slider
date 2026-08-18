# 🎯 Rewrite DynamicWeightAStarSolver to C++ – Complete Strategy

## Context & Rationale

**Scope**: Port DynamicWeightAStarSolver + tightly coupled dependencies (IHeuristicCalculator, IStateInfoFactory) from C# to C++ as a standalone native DLL.

**Goal**: Eliminate GC pressure from millions of small object allocations during puzzle solving by using manual memory management and stack-based value semantics.

**Architecture Decision**: C++ class library (static or dynamic; callable via P/Invoke or C++/CLI wrapper from C# for integration).

**Key Challenges**:
- Replacing managed collection abstractions (`PriorityQueue<T>`, `MultiMap<T>`, custom pools) with STL equivalents or hand-written C++ versions.
- Translating C# struct value semantics and memory pooling patterns into unmanaged C++.
- Removing .NET runtime dependencies (no Span<T>, no delegates with state capture).
- Porting heuristic calculator implementations (complex, called millions of times).
- Testing parity and performance validation.

**Risk**: High scope. Recommend breaking into two sprints: Core Solver (days 1–5) + Heuristics & Integration (days 6–10).

---

**Last Updated**: 2026-08-17 09:17:31

## 📝 Plan Steps
-  **Extract & catalog all dependencies — Review `IHeuristicCalculator`, `IStateInfoFactory`, `IChunkedStructPool`, `IChunkedArrayPoolUnsafe`, `MultiMap<T>`, and `RefAction<>` implementations. Document method signatures, performance constraints, and memory lifecycle.**
-  **Design C++ memory model — Establish arena/pool allocator patterns, define object lifetime strategy (stack vs. heap), and plan for minimal allocations inside the solve loop.**
-  **Translate core data structures — Convert `StateInfo` struct, custom pool interfaces, and `MultiMap<T>` to C++. Use `std::priority_queue` or `boost::heap` for priority queue. Choose: `std::unordered_map` for fast closed-set lookups or custom hash table if performance critical.**
-  **Port DynamicWeightAStarSolver algorithm — Rewrite `Solve()`, `SprintSolve()`, `ProcessNewState()` in C++. Replace C# `Span<T>` with `std::span` (C++20) or `gsl::span`. Replace delegates (`RefAction<>`) with function pointers or `std::function`.**
-  **Port heuristic calculator interface — Translate `IHeuristicCalculator.GetHeuristic()` and all concrete implementations (e.g., Manhattan distance, pattern database lookup). Preserve algorithm logic exactly; benchmark against C# baseline.**
-  **Port state factory & move generation — Rewrite `IStateInfoFactory.GetAvailableMoves()` and logic for generating neighbor states (sliding puzzle moves). Ensure identical behavior to C#.**
-  **Create C# interop layer — Decide: Use P/Invoke with C-style exported functions and manual marshaling, or wrap in C++/CLI for cleaner abstraction. Export a `Solver::SolveAsync()` method callable from C#.**
-  **Build and link — Create C++ project (vcxproj or CMake). Link to any external heuristic libs (if applicable). Produce native DLL.**
-  **Write C++ unit tests — Port or create GoogleTest/Catch2 test suite matching original `DyamicWeightedAStarTests.cs` coverage. Run correctness tests (same input → same solution) and performance profiling.**
-  **Integration & validation — Replace C# solver in calling code or create hybrid mode. Benchmark against C# baseline. ProfileC++ code (memory allocation rate, CPU time, GC pause elimination). Fix any discrepancies.**
-  **Documentation & handoff — Document C++ API, memory ownership rules, build instructions, and performance characteristics. Update project README if applicable.**

