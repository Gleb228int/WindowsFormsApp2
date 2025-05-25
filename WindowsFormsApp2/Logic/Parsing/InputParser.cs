using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace WindowsFormsApp1.Logic.Parsing
{
    public static class InputParser
    {
        private static readonly Regex NumberPattern = new Regex(@"^(?:0|[1-9]\d*)(?:\.\d+)?$", RegexOptions.Compiled);
        private const int MaxSize = 25;
        private const double MaxWeight = 10_000_000;

        public static bool TryParseMatrix(string[] lines, out double[,] graph, out string errorMessage)
        {
            graph = null;
            errorMessage = null;

            string[] trimmed = Array.FindAll(lines, l => !string.IsNullOrWhiteSpace(l));
            int n = trimmed.Length;

            if (n == 0)
            {
                errorMessage = "Input is empty. Please enter an n×n matrix.";
                return false;
            }

            if (n > MaxSize)
            {
                errorMessage = $"Matrix size ({n}×{n}) exceeds maximum allowed {MaxSize}×{MaxSize}.";
                return false;
            }

            graph = new double[n, n];

            try
            {
                for (int i = 0; i < n; i++)
                {
                    string[] parts = trimmed[i].Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != n)
                        throw new ApplicationException($"In line {i + 1} should be {n} values, found {parts.Length}.");

                    for (int j = 0; j < n; j++)
                    {
                        string token = parts[j];
                        if (!NumberPattern.IsMatch(token))
                            throw new ApplicationException($"Incorrect number format '{token}' in row {i + 1}, column {j + 1}.");

                        if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
                            throw new ApplicationException($"Failed to convert '{token}' to a number in row {i + 1}, column {j + 1}.");

                        if (val < 0)
                            throw new ApplicationException($"Negative weight {val} in row {i + 1}, column {j + 1}.");

                        if (val > MaxWeight)
                            throw new ApplicationException($"Weight {val} in row {i + 1}, column {j + 1} exceeds maximum allowed ({MaxWeight}).");

                        graph[i, j] = val;
                    }
                }

                const double eps = 1e-9;
                for (int i = 0; i < n; i++)
                {
                    if (Math.Abs(graph[i, i]) > eps)
                        throw new ApplicationException($"Diagonal element ({i + 1},{i + 1}) should be zero (found {graph[i, i]}).");

                    for (int j = i + 1; j < n; j++)
                    {
                        if (Math.Abs(graph[i, j] - graph[j, i]) > eps)
                            throw new ApplicationException($"Matrix is asymmetric at ({i + 1},{j + 1}).");

                        if (Math.Abs(graph[i, j]) < eps)
                            throw new ApplicationException($"No connection between nodes {i + 1} and {j + 1}.");
                    }
                }

                return true;
            }
            catch (ApplicationException ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }
    }
}