using System;
using System.Collections.Generic;
using System.Text;
namespace Slider.Heuristics
{
    public class MinimumSpanningTree
    {
        public int CalculateMST(Span<byte> misplacedTilePositions, int gridSize)
        {
            int missingTilesCount = misplacedTilePositions.Length;
            // Track which nodes are already in the MST
            Span<bool> inMST = stackalloc bool[missingTilesCount];
            // Cheapest known edge connecting each node to the MST
            Span<int> cheapestEdge = stackalloc int[missingTilesCount];
            for (int i = 0; i < missingTilesCount; i++)
                cheapestEdge[i] = int.MaxValue;
            cheapestEdge[0] = 0;  // Start from the blank
            int totalCost = 0;
            for (int i = 0; i < missingTilesCount; i++)
            {
                // Pick the cheapest node not yet in MST
                int u = -1;
                for (int v = 0; v < missingTilesCount; v++)
                {
                    if (!inMST[v] && (u == -1 || cheapestEdge[v] < cheapestEdge[u]))
                    {
                        u = v;
                    }
                }
                inMST[u] = true;
                totalCost += cheapestEdge[u];
                // Update cheapest edges to remaining nodes
                for (int v = 0; v < missingTilesCount; v++)
                {
                    if (!inMST[v])
                    {
                        int dist = GetManhattanDistance(misplacedTilePositions[u], misplacedTilePositions[v], gridSize);
                        if (dist < cheapestEdge[v])
                            cheapestEdge[v] = dist;
                    }
                }
            }
            return totalCost;
        }
        private int GetManhattanDistance(int a, int b, int gridSize)
        {
            (int rowA, int colA) = a.ToRowAndColumn(gridSize);
            (int rowB, int colB) = b.ToRowAndColumn(gridSize);
            return Math.Abs(rowA - rowB) + Math.Abs(colA - colB);
        }
    }
 }
