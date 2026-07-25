using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Solver
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    public class WeightedMmSolver
    {
        // High-performance priority queue structure from our earlier struct optimizations
        // Maps Board State -> Priority Key
        private PriorityQueue<State, double> _openF = new();
        private PriorityQueue<State, double> _openB = new();

        // Historical tracking registries (Closed Sets) for intersection detection
        private Dictionary<ulong, State> _closedF = new();
        private Dictionary<ulong, State> _closedB = new();

        public List<State> Solve10x10Bidirectional(State startState, State goalState, long maxDurationMilliseconds)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            // 1. Initialize global tracking variables
            double bestPathCost = double.MaxValue;
            State collisionNodeF = null;
            State collisionNodeB = null;

            // Initialize target map for backward search (relative to start layout)
            ArbitraryGoalInversionCounter.InitializeGoalMap(startState.Board, 0);

            // 2. Seed root nodes
            _openF.Enqueue(startState, 0.0);
            _openB.Enqueue(goalState, 0.0);

            // 3. Execution Search Loop
            while (_openF.Count > 0 && _openB.Count > 0)
            {
                // --- GAME SOLVER TIMEOUT FALLBACK ---
                if (watch.ElapsedMilliseconds >= maxDurationMilliseconds)
                {
                    // If we found any valid path, return it immediately (sub-optimal but valid)
                    if (bestPathCost < double.MaxValue)
                    {
                        return StitchPath(collisionNodeF, collisionNodeB);
                    }
                    return null; // Complete timeout with zero paths found
                }

                // --- QUEUE SELECTION LOGIC ---
                // Peek at the front element of both heaps to see their exact minimum priorities
                _openF.TryPeek(out _, out double minPriorityF);
                _openB.TryPeek(out _, out double minPriorityB);

                // Dynamically fetch the minimum path costs (G) waiting in both lines
                double minG_F = GetMinGScore(_openF);
                double minG_B = GetMinGScore(_openB);

                // --- RIGOROUS TERMINATION CONDITION ---
                // Stop searching if the current floors prove no better path exists
                if (bestPathCost <= Math.Max(minPriorityF, Math.Max(minPriorityB, minG_F + minG_B + 1)))
                {
                    return StitchPath(collisionNodeF, collisionNodeB);
                }

                // Dynamic decision: Expand from whichever queue has the lowest priority floor
                if (minPriorityF <= minPriorityB)
                {
                    ExpandFrontier(isForward: true, _openF, _closedF, _closedB, ref bestPathCost, ref collisionNodeF, ref collisionNodeB);
                }
                else
                {
                    ExpandFrontier(isForward: false, _openB, _closedB, _closedF, ref bestPathCost, ref collisionNodeB, ref collisionNodeF);
                }
            }

            return (bestPathCost < double.MaxValue) ? StitchPath(collisionNodeF, collisionNodeB) : null;
        }

        private void ExpandFrontier(
            bool isForward,
            PriorityQueue<State, double> activeOpen,
            Dictionary<ulong, State> activeClosed,
            Dictionary<ulong, State> oppositeClosed,
            ref double bestPathCost,
            ref State activeCollisionNode,
            ref State oppositeCollisionNode)
        {
            State current = activeOpen.Dequeue();
            ulong currentHash = current.GetBoardHash();

            // Standard duplicate defense
            if (activeClosed.ContainsKey(currentHash)) return;
            activeClosed.Add(currentHash, current);

            // --- HISTORICAL COLLISION DETECTION ---
            // We do not just check tips; we look back at the opposite side's entire history
            if (oppositeClosed.TryGetValue(currentHash, out State matchedOppositeNode))
            {
                double combinedCost = current.CurrentG + matchedOppositeNode.CurrentG;
                if (combinedCost < bestPathCost)
                {
                    bestPathCost = combinedCost;
                    activeCollisionNode = current;
                    oppositeCollisionNode = matchedOppositeNode;
                }
            }

            // Generate Child Transitions
            foreach (State child in current.GetValidMoves())
            {
                if (activeClosed.ContainsKey(child.GetBoardHash())) continue;

                // --- THE "FREEZING" CALCULATOR ---
                // 1. Fetch current dynamic weight based on distance remaining to that search's target
                int hScore = isForward ? CalculateHeuristicToGoal(child) : CalculateHeuristicToStart(child);
                double epsilon = SolverWeights.GetWeight(hScore);

                // 2. The Standard path score (Tie-breaking Weighted A*)
                double standardF = (hScore * epsilon + child.CurrentG) - (child.CurrentG * 0.0001);

                // 3. The Midpoint ceiling anchor (scaled by your current weight)
                double midpointCeiling = (epsilon + 1.0) * child.CurrentG;

                // Math.Max forces the node's key to inflate if it pushes too deep.
                // This pushes it down into the heap heap, "freezing" its deep branch 
                // and forcing the solver loop to prioritize shallow sibling angles instead.
                double finalPriority = Math.Max(standardF, midpointCeiling);

                activeOpen.Enqueue(child, finalPriority);
            }
        }

        private List<State> StitchPath(State nodeF, State nodeB)
        {
            var fullPath = new List<State>();

            // Trace forward path backward to root, then reverse it
            State curr = nodeF;
            while (curr != null)
            {
                fullPath.Add(curr);
                curr = curr.Parent;
            }
            fullPath.Reverse();

            // Append the backward path (skipping the duplicate intersecting root node)
            curr = nodeB?.Parent;
            while (curr != null)
            {
                fullPath.Add(curr); // Backward transitions are naturally already in forward execution direction
                curr = curr.Parent;
            }

            return fullPath;
        }

        private double GetMinGScore(PriorityQueue<State, double> queue)
        {
            if (queue.Count == 0) return double.MaxValue;
            // Optimization: In your production build, store the minimum G currently inside the queue 
            // to avoid peeking elements if your custom PriorityQueue structure doesn't support it natively.
            queue.TryPeek(out State element, out _);
            return element?.CurrentG ?? double.MaxValue;
        }

        // Placeholders for your optimized heuristic functions
        private int CalculateHeuristicToGoal(State s) => /* Manhattan + FenwickInversions + Corners to Goal */ 0;
        private int CalculateHeuristicToStart(State s) => /* FenwickInversions to Start Map */ 0;
    }
}
