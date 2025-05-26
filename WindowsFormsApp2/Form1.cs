using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using WindowsFormsApp1.Logic.Parsing;
using WindowsFormsApp1.Logic.Solvers;
using WindowsFormsApp1.UI.Rendering;

namespace WindowsFormsApp1
{
    public class MainForm : Form
    {
        private Label lblInstructions;
        private TextBox txtInput;
        private Label lblStart;
        private NumericUpDown numStart;
        private Label lblMethod;
        private ComboBox comboMethod;
        private Button btnSolve;
        private Button btnClear;
        private Panel panelGraph;
        private Label lblResult;
        private TextBox txtOutput;
        private Label lblLog;
        private TextBox txtLog;

        private double[,] graph;
        private List<PointF> nodePositions;
        private int[] currentPath;

        public MainForm()
        {
            SetupForm();
            nodePositions = new List<PointF>();
            comboMethod.Items.AddRange(new[] { "Greedy (Edge-based)", "Nearest Neighbor", "Simulated Annealing" });
            comboMethod.SelectedIndex = 0;
            comboMethod.SelectedIndexChanged += ComboMethod_SelectedIndexChanged;
            btnSolve.Click += BtnSolve_Click;
            btnClear.Click += BtnClear_Click;
            panelGraph.Paint += PanelGraph_Paint;
            this.Resize += OnFormResize;
        }

        private void SetupForm()
        {
            this.Text = "Traveling Salesman Solver";
            this.ClientSize = new Size(900, 600);

            lblInstructions = new Label
            {
                Text = "Enter weight matrix (symmetric, complete) – max size 25×25\r\n" +
                       "Values separated by commas, rows by newline",
                Location = new Point(10, 10),
                Size = new Size(300, 40)
            };

            txtInput = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Location = new Point(10, 55),
                Size = new Size(300, 180)
            };

            lblStart = new Label
            {
                Text = "Start node (1–n):",
                Location = new Point(10, 245),
                Size = new Size(120, 20)
            };

            numStart = new NumericUpDown
            {
                Location = new Point(135, 243),
                Size = new Size(50, 20),
                Minimum = 1,
                Maximum = 1000,
                Value = 1
            };

            lblMethod = new Label
            {
                Text = "Select method:",
                Location = new Point(330, 10),
                Size = new Size(100, 20)
            };

            comboMethod = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(330, 35),
                Size = new Size(200, 25)
            };

            btnSolve = new Button
            {
                Text = "Solve",
                Location = new Point(540, 35),
                Size = new Size(75, 25)
            };

            btnClear = new Button
            {
                Text = "Clear",
                Location = new Point(620, 35),
                Size = new Size(75, 25)
            };

            panelGraph = new Panel
            {
                Location = new Point(330, 70),
                Size = new Size(550, 500),
                BackColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            lblResult = new Label
            {
                Text = "Result:",
                Location = new Point(10, 280),
                Size = new Size(300, 20)
            };

            txtOutput = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Location = new Point(10, 305),
                Size = new Size(300, 60)
            };

            lblLog = new Label
            {
                Text = "Steps & Info:",
                Location = new Point(10, 380),
                Size = new Size(300, 20)
            };

            txtLog = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Location = new Point(10, 405),
                Size = new Size(300, 180)
            };

            this.Controls.AddRange(new Control[] {
                lblInstructions, txtInput, lblStart, numStart,
                lblMethod, comboMethod, btnSolve, btnClear,
                panelGraph, lblResult, txtOutput, lblLog, txtLog
            });
        }

        private void ComboMethod_SelectedIndexChanged(object sender, EventArgs e)
        {
            var method = comboMethod.SelectedItem.ToString();
            if (method.StartsWith("Nearest"))
            {
                numStart.Enabled = false;
                lblStart.Text = "Start node: (random)";
            }
            else
            {
                numStart.Enabled = true;
                lblStart.Text = "Start node (1–n):";
            }
        }

        private void OnFormResize(object sender, EventArgs e)
        {
            if (graph != null)
            {
                nodePositions = GraphRenderer.GenerateNodePositions(graph.GetLength(0), panelGraph.Width, panelGraph.Height);
                panelGraph.Invalidate();
            }
        }

        private void BtnSolve_Click(object sender, EventArgs e)
        {
            var lines = txtInput.Lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
            int n = lines.Length;
            if (n < 2 && n!=0)
            {
                MessageBox.Show("At least two nodes are required.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!InputParser.TryParseMatrix(lines, out graph, out string errMsg))
            {
                MessageBox.Show(errMsg, "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            txtLog.Clear();
            string method = comboMethod.SelectedItem.ToString();
            int[] pathResult = null;
            long iterations = 0;

            if (method.StartsWith("Greedy"))
            {
                txtLog.AppendText("Time Complexity (iterations): count of edges examined\r\n");
                pathResult = GraphSolver.SolveGreedy(graph, (int)numStart.Value - 1, out iterations);
            }
            else if (method.StartsWith("Nearest"))
            {
                pathResult = GraphSolver.SolveNearestNeighbor(graph, out var steps, out iterations);
                foreach (var s in steps) txtLog.AppendText(s + Environment.NewLine);
                txtLog.AppendText("Time Complexity (iterations): count of distance comparisons\r\n");
            }
            else
            {
                const int saSeed = 12345;
                pathResult = GraphSolver.SimulatedAnnealing(graph, (int)numStart.Value - 1, out var saSteps, out iterations, saSeed);
                foreach (var s in saSteps) txtLog.AppendText(s + Environment.NewLine);
                txtLog.AppendText("Time Complexity (iterations): SA loop count" + Environment.NewLine);
            }

            txtLog.AppendText($"Total iterations: {iterations:N0}" + Environment.NewLine);
            currentPath = pathResult;
            txtOutput.Text = GraphSolver.FormatResult(graph, currentPath);

            try
            {
                var output = $"{txtOutput.Text}\r\nIterations: {iterations}\r\n{txtLog.Text}";
                File.WriteAllText("tsp_result.txt", output);
                MessageBox.Show("Results (with iteration count) saved to tsp_result.txt", "Save Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save results: {ex.Message}", "File Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            nodePositions = GraphRenderer.GenerateNodePositions(graph.GetLength(0), panelGraph.Width, panelGraph.Height);
            panelGraph.Invalidate();
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            txtInput.Clear();
            txtOutput.Clear();
            txtLog.Clear();
            numStart.Value = 1;
            graph = null;
            nodePositions.Clear();
            currentPath = null;
            panelGraph.Invalidate();
        }

        private void PanelGraph_Paint(object sender, PaintEventArgs e)
        {
            if (graph == null || nodePositions.Count == 0)
                return;

            GraphRenderer.DrawFullGraph(e.Graphics, nodePositions);
            if (currentPath != null)
                GraphRenderer.DrawPath(e.Graphics, nodePositions, currentPath);
        }
    }
}