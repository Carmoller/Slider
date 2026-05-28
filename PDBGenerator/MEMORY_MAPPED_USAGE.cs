// Memory-Mapped PDB Generation Examples

// For small puzzles (in-memory, faster):
var gen5x5 = new PDBGenerator.Generator(5, 6, useMemoryMappedFile: false);
var pdb5x5 = gen5x5.GeneratePdb(new PDBGenerator.Generator.PatternState
{
    TilePositions = [0, 1, 2, 3, 4, 5],
    BlankPosition = 24
});
Console.WriteLine($"5x5 PDB generated in {gen5x5.ElapsedMs}ms, processing {gen5x5.StatesProcessed} states");

// For larger puzzles (memory-mapped file, unlimited size):
var gen10x10 = new PDBGenerator.Generator(10, 6, useMemoryMappedFile: true);
var pdb10x10 = gen10x10.GeneratePdb(new PDBGenerator.Generator.PatternState
{
    TilePositions = [0, 1, 2, 3, 4, 5],
    BlankPosition = 99
});
Console.WriteLine($"10x10 PDB generated in {gen10x10.ElapsedMs}ms, processing {gen10x10.StatesProcessed} states");

// The resulting PDB can be used the same way for A* heuristic lookups:
// byte distance = pdb5x5.GetDistance(stateIndex);  // Fast O(1) lookup

// Memory Requirements Comparison:
// 
// 5x5, K=6:   In-memory ~128 MB    vs  MMF 128 MB + disk I/O
// 6x6, K=6:   In-memory ~1.3 GB    vs  MMF 1.3 GB + disk I/O (safer)
// 8x8, K=6:   In-memory ~45 GB!    vs  MMF 45 GB (requires MMF)
// 10x10, K=6: In-memory imposible  vs  MMF 56 GB on disk (use MMF!)
//
// The trade-off:
// - In-memory: O(1) access, no disk I/O, limited by RAM
// - MMF: Slightly slower (OS paging), unlimited size, limited by disk

// Note: Distances are stored as bytes (0-255), which is sufficient since the
// maximum heuristic distance is typically < 64 even for 10x10 puzzles.
