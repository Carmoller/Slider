namespace PDBGenerator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            /*
             * PDB = array initialized to UNVISITED
                initial_state = goal pattern:
                positions = goal positions of pattern tiles
                blank_pos = goal blank position
                initial_index = ENCODE(initial_state)
                PDB[initial_index] = 0
                current_frontier = [initial_state]
                depth = 0
                while current_frontier not empty:
                next_frontier = empty list
                parallel_for each state in current_frontier:
                for each neighbor_pos in NEIGHBORS(state.blank_pos):
                next_state = state (copy or mutable)
                moved_tile_index = FIND_TILE_AT(neighbor_pos, state)
                if moved_tile_index != NONE:
                // blank swaps with pattern tile
                next_state.positions[moved_tile_index] = state.blank_pos
                move_cost = 1
                else:
                // blank swaps with non-pattern tile
                move_cost = 0
                next_state.blank_pos = neighbor_pos
                idx = ENCODE(next_state)
                if PDB[idx] == UNVISITED:
                // atomic check+set if parallel
                PDB[idx] = depth + move_cost
                next_frontier.append(next_state)
                else if PDB[idx] > depth + move_cost:
                // optional: only needed if using 0-cost moves
                PDB[idx] = depth + move_cost
                next_frontier.append(next_state)
                current_frontier = next_frontier
                depth += 1
                return PDB*/
            Console.WriteLine("Hello, World!");
        }
    }
}
