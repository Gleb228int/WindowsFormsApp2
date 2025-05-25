using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApp1.Logic.Solvers
{
    public static class GraphSolver
    {
        public static int[] SolveGreedy(double[,] weights, int startNode, out long iterations)
        {
            int n = weights.GetLength(0);
            iterations = 0;
            if (n == 2)
            {
                iterations = 1;
                int other = 1 - startNode;
                return new[] { startNode, other, startNode };
            }

            var edges = new List<(int u, int v, double w)>();
            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                    edges.Add((i, j, weights[i, j]));
            edges.Sort((a, b) => a.w.CompareTo(b.w));

            int[] deg = new int[n];
            var parent = Enumerable.Range(0, n).ToArray();
            Func<int, int> find = null;
            find = x => parent[x] == x ? x : parent[x] = find(parent[x]);
            Action<int, int> unite = (a, b) => parent[find(a)] = find(b);

            var tourEdges = new List<(int u, int v)>();
            foreach (var (u, v, w) in edges)
            {
                iterations++;
                if (deg[u] < 2 && deg[v] < 2)
                {
                    int pu = find(u), pv = find(v);
                    bool createsCycle = pu == pv;
                    if (!createsCycle || tourEdges.Count == n - 1)
                    {
                        deg[u]++; deg[v]++;
                        tourEdges.Add((u, v));
                        if (!createsCycle)
                            unite(u, v);
                        if (tourEdges.Count == n) break;
                    }
                }
            }

            var cycle = ReconstructCycle(tourEdges, n);
            return RotateCycle(cycle.ToArray(), startNode);
        }

        public static int[] SolveNearestNeighbor(double[,] weights, out List<string> steps, out long iterations)
        {
            steps = new List<string>();
            iterations = 0;
            int n = weights.GetLength(0);
            var visited = new bool[n];
            var path = new int[n + 1];
            var rnd = new Random();
            int start = rnd.Next(n);

            steps.Add($"Start node: {start + 1}");
            path[0] = start;
            visited[start] = true;
            int current = start;

            for (int i = 1; i < n; i++)
            {
                double minDist = double.MaxValue;
                int nextCity = -1;
                for (int j = 0; j < n; j++)
                {
                    iterations++;
                    if (!visited[j] && weights[current, j] < minDist)
                    {
                        minDist = weights[current, j];
                        nextCity = j;
                    }
                }

                path[i] = nextCity;
                visited[nextCity] = true;
                steps.Add($"Step {i}: {current + 1} → {nextCity + 1} (Distance = {weights[current, nextCity]:F2})");
                current = nextCity;
            }

            path[n] = path[0];
            steps.Add($"Return: {current + 1} → {path[0] + 1} (Distance = {weights[current, path[0]]:F2})");
            return path;
        }

        public static int[] SimulatedAnnealing(double[,] weights, int startNode, out List<string> steps, out long iterations, int seed)
        {
            steps = new List<string>();
            iterations = 0;
            int n = weights.GetLength(0);
            var rnd = new Random(seed);

            var middle = Enumerable.Range(0, n)
                                   .Where(i => i != startNode)
                                   .OrderBy(x => rnd.Next())
                                   .ToList();

            var currList = new List<int> { startNode };
            currList.AddRange(middle);
            currList.Add(startNode);
            int[] curr = currList.ToArray();

            double currDist = TotalDistance(curr, weights);
            int[] best = (int[])curr.Clone();
            double bestDist = currDist;

            double T = 10000.0;
            double alpha = 0.995;
            int maxIter = 100000;

            steps.Add($"Initial distance: {currDist:F2}");

            for (int iter = 0; iter < maxIter; iter++)
            {
                iterations++;
                int[] next = (int[])curr.Clone();
                int i = rnd.Next(1, n);
                int j = rnd.Next(1, n);
                (next[i], next[j]) = (next[j], next[i]);

                double nextDist = TotalDistance(next, weights);
                double delta = nextDist - currDist;
                if (delta < 0 || Math.Exp(-delta / T) > rnd.NextDouble())
                {
                    curr = next;
                    currDist = nextDist;
                    if (currDist < bestDist)
                    {
                        best = (int[])curr.Clone();
                        bestDist = currDist;
                        steps.Add($"New best at iter {iterations}: {bestDist:F2}");
                    }
                }

                T *= alpha;
                if (T < 1e-8)
                    break;
            }

            steps.Add($"Final best distance: {bestDist:F2}");
            return best;
        }


        private static List<int> ReconstructCycle(List<(int u, int v)> edges, int n)
        {
            var adj = new List<int>[n];
            for (int i = 0; i < n; i++) adj[i] = new List<int>();
            foreach (var (u, v) in edges)
            {
                adj[u].Add(v);
                adj[v].Add(u);
            }

            var cycle = new List<int>();
            int curr = 0, prev = -1;
            for (int i = 0; i < n; i++)
            {
                cycle.Add(curr);
                int next = adj[curr][0] == prev ? adj[curr][1] : adj[curr][0];
                prev = curr;
                curr = next;
            }
            cycle.Add(cycle[0]);
            return cycle;
        }

        private static int[] RotateCycle(int[] cycle, int startNode)
        {
            int length = cycle.Length;
            int coreLen = length - 1;
            int idx = Array.IndexOf(cycle, startNode);
            var rotated = new int[length];
            for (int i = 0; i < length; i++)
                rotated[i] = cycle[(idx + i) % coreLen];
            rotated[length - 1] = rotated[0];
            return rotated;
        }

        private static double TotalDistance(int[] route, double[,] weights)
        {
            double sum = 0;
            for (int i = 0; i < route.Length - 1; i++)
                sum += weights[route[i], route[i + 1]];
            return sum;
        }

        public static string FormatResult(double[,] graph, int[] path)
        {
            if (path == null || path.Length == 0) return string.Empty;
            var disp = path.Select(i => (i + 1).ToString()).ToArray();
            double total = path.Length > 1 ? TotalDistance(path, graph) : 0;
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(" → ", disp));
            sb.AppendFormat("Total length: {0:F2}", total);
            return sb.ToString();
        }
    }
}