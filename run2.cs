using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static readonly char[] keys_char = Enumerable.Range('a', 26).Select(i => (char)i).ToArray();
    static readonly char[] doors_char = keys_char.Select(char.ToUpper).ToArray();
    
    static List<List<char>> GetInput()
    {
        var data = new List<List<char>>();
        string line;
        while ((line = Console.ReadLine()) != null && line != "")
        {
            data.Add(line.ToCharArray().ToList());
        }
        return data;
    }

    struct State
    {
        public (int x, int y)[] Robots { get; }
        public int KeysMask { get; }

        public State((int x, int y)[] robots, int keysMask)
        {
            Robots = robots;
            KeysMask = keysMask;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is State other)) return false;
            if (KeysMask != other.KeysMask) return false;
            for (var i = 0; i < Robots.Length; i++)
                if (Robots[i] != other.Robots[i]) return false;
            return true;
        }

        public override int GetHashCode()
        {
            var hash = KeysMask.GetHashCode();
            foreach (var (x, y) in Robots)
                hash = hash * 31 + x.GetHashCode() + y.GetHashCode();
            return hash;
        }
    }

    static int Solve(List<List<char>> data)
    {
        var robots = new List<(int x, int y)>();
        for (var i = 0; i < data.Count; i++)
            for (var j = 0; j < data[i].Count; j++)
                if (data[i][j] == '@') robots.Add((i, j));
        if (robots.Count != 4) return -1;

        var keys = new HashSet<char>();
        foreach (var row in data)
            foreach (var c in row)
                if (char.IsLower(c)) keys.Add(c);
        var targetMask = keys.Aggregate(0, (mask, key) => mask | (1 << (key - 'a')));

        int[] dx = { -1, 1, 0, 0 }, dy = { 0, 0, -1, 1 };
        var queue = new PriorityQueue<State, int>();
        var visited = new Dictionary<State, int>();
        var initialState = new State(robots.ToArray(), 0);
        queue.Enqueue(initialState, 0);

        while (queue.Count > 0)
        {
            queue.TryDequeue(out State current, out int steps);
            if (current.KeysMask == targetMask) return steps;
            if (visited.TryGetValue(current, out int existing) && existing <= steps) continue;
            visited[current] = steps;

            for (var r = 0; r < 4; r++)
            {
                var (x, y) = current.Robots[r];
                for (var d = 0; d < 4; d++)
                {
                    int nx = x + dx[d], ny = y + dy[d];
                    if (nx < 0 || ny < 0 || nx >= data.Count || ny >= data[nx].Count) continue;
                    var cell = data[nx][ny];
                    if (cell == '#') continue;
                    if (char.IsUpper(cell) && (current.KeysMask & (1 << (cell - 'A'))) == 0) continue;

                    var newMask = current.KeysMask;
                    if (char.IsLower(cell)) newMask |= 1 << (cell - 'a');
                    var newRobots = current.Robots.ToArray();
                    newRobots[r] = (nx, ny);
                    var newState = new State(newRobots, newMask);
                    var newSteps = steps + 1;
                    if (!visited.ContainsKey(newState) || newSteps < visited.GetValueOrDefault(newState, int.MaxValue))
                        queue.Enqueue(newState, newSteps);
                }
            }
        }

        return -1;
    }

    static void Main()
    {
        var data = GetInput();
        int result = Solve(data);
        Console.WriteLine(result == -1 ? "No solution found" : result.ToString());
    }
}