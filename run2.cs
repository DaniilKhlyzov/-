using System;
using System.Collections.Generic;
using System.Linq;

public class Program
{
    static readonly char[] keys_char = Enumerable.Range('a', 26).Select(i => (char)i).ToArray();
    static readonly char[] doors_char = keys_char.Select(char.ToUpper).ToArray();
    
    // Метод для чтения входных данных
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

    private class Edge(int to, int dist, HashSet<char> doorsRequired)
    {
        public int To { get; } = to;
        public int Dist { get; } = dist;
        public HashSet<char> DoorsRequired { get; } = doorsRequired;
    }

    private class Entry(int x, int y, int dist, HashSet<char> doors)
    {
        public int X { get; } = x;
        public int Y { get; } = y;
        public int Dist { get; } = dist;
        public HashSet<char> Doors { get; } = doors;
    }

    public class LabyrinthState(int[] robotsPositions, HashSet<char> keys, int dist) : IComparable<LabyrinthState>
    {
        public int[] RobotsPositions { get; } = (int[])robotsPositions.Clone();
        public HashSet<char> Keys { get; } = new HashSet<char>(keys);
        public int Dist { get; } = dist;

        public int CompareTo(LabyrinthState? other)
        {
            return Dist.CompareTo(other!.Dist);
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            LabyrinthState s = (LabyrinthState)obj;
            return RobotsPositions.SequenceEqual(s.RobotsPositions) && Keys.SetEquals(s.Keys);
        }

        public override int GetHashCode()
        {
            var hash = RobotsPositions.Aggregate(17, (current, pos) => current * 31 + pos.GetHashCode());

            return Keys.OrderBy(k => k).Aggregate(hash, (current, key) => current * 31 + key.GetHashCode());
        }
    }

    private static int Solve(List<List<char>> data)
    {
        var rowsLength = data.Count;
        var columnsLength = data[0].Count;

        var robots = new List<int[]>();
        var keyPositions = new Dictionary<char, int[]>();

        for (var i = 0; i < rowsLength; i++)
        {
            for (var j = 0; j < columnsLength; j++)
            {
                var cell = data[i][j];
                switch (cell)
                {
                    case '@':
                        robots.Add(new int[] { i, j });
                        break;
                    case >= 'a' and <= 'z':
                        keyPositions[cell] = new int[] { i, j };
                        break;
                }
            }
        }

        var keysList = keyPositions.Keys.OrderBy(k => k).ToList();

        var nodes = new List<int[]>(robots);
        var keyIndexInNodesArray = new Dictionary<char, int>();

        foreach (var key in keysList)
        {
            nodes.Add(keyPositions[key]);
            keyIndexInNodesArray[key] = nodes.Count - 1;
        }

        var graph = new List<List<Edge>>();
        for (var i = 0; i < nodes.Count; i++)
        {
            graph.Add([]);
        }

        int[] directionsX = [-1, 1, 0, 0];
        int[] directionsY = [0, 0, -1, 1];

        for (var node = 0; node < nodes.Count; node++)
        {
            var visited = new bool[rowsLength][];
            for (var i = 0; i < rowsLength; i++)
                visited[i] = new bool[columnsLength];

            var queue = new Queue<Entry>();
            var startX = nodes[node][0];
            var startY = nodes[node][1];
            visited[startX][startY] = true;
            queue.Enqueue(new Entry(startX, startY, 0, []));

            while (queue.Count > 0)
            {
                var currEntry = queue.Dequeue();
                var cell = data[currEntry.X][currEntry.Y];
                var doors = new HashSet<char>(currEntry.Doors);

                switch (cell)
                {
                    case >= 'A' and <= 'Z':
                        doors.Add(cell);
                        break;
                    case >= 'a' and <= 'z' when keyIndexInNodesArray.TryGetValue(cell, out int newFromNode):
                    {
                        if (newFromNode != node)
                        {
                            graph[node].Add(new Edge(newFromNode, currEntry.Dist, doors));
                        }

                        break;
                    }
                }

                for (var direction = 0; direction < 4; direction++)
                {
                    var newX = currEntry.X + directionsX[direction];
                    var newY = currEntry.Y + directionsY[direction];

                    if (newX >= 0 && newX < rowsLength && newY >= 0 && newY < columnsLength
                        && !visited[newX][newY] && data[newX][newY] != '#')
                    {
                        visited[newX][newY] = true;
                        queue.Enqueue(new Entry(newX, newY, currEntry.Dist + 1, [..doors]));
                    }
                }
            }
        }

        var priorityQueue = new PriorityQueue<LabyrinthState, int>();
        var distMap = new Dictionary<LabyrinthState, int>();

        var initialPositions = Enumerable.Range(0, robots.Count).Select(i => i).ToArray();
        var start = new LabyrinthState(initialPositions, [], 0);
        distMap[start] = 0;
        priorityQueue.Enqueue(start, start.Dist);

        while (priorityQueue.Count > 0)
        {
            LabyrinthState state = priorityQueue.Dequeue();

            if (!distMap.TryGetValue(state, out int currentDist) || state.Dist != currentDist)
                continue;

            if (state.Keys.Count == keysList.Count)
                return state.Dist;

            for (int robot = 0; robot < robots.Count; robot++)
            {
                int currPosition = state.RobotsPositions[robot];

                foreach (Edge edge in graph[currPosition])
                {
                    if (edge.To >= robots.Count && edge.To < nodes.Count)
                    {
                        char key = keysList[edge.To - robots.Count];
                        if (state.Keys.Contains(key))
                            continue;

                        bool allDoorsOpen = true;
                        foreach (char door in edge.DoorsRequired)
                        {
                            if (!state.Keys.Contains(char.ToLower(door)))
                            {
                                allDoorsOpen = false;
                                break;
                            }
                        }
                        if (!allDoorsOpen)
                            continue;

                        var newKeys = new HashSet<char>(state.Keys) { key };
                        var newPositions = (int[])state.RobotsPositions.Clone();
                        newPositions[robot] = edge.To;

                        var newState = new LabyrinthState(newPositions, newKeys, state.Dist + edge.Dist);

                        if (!distMap.TryGetValue(newState, out int prevDist) || newState.Dist < prevDist)
                        {
                            distMap[newState] = newState.Dist;
                            priorityQueue.Enqueue(newState, newState.Dist);
                        }
                    }
                }
            }
        }

        return int.MaxValue;
    }

    static void Main()
    {
        var data = GetInput();
        int result = Solve(data);
        
        if (result == -1)
        {
            Console.WriteLine("No solution found");
        }
        else
        {
            Console.WriteLine(result);
        }
    }
}