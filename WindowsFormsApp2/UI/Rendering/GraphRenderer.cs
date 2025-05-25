using System;
using System.Collections.Generic;
using System.Drawing;

namespace WindowsFormsApp1.UI.Rendering
{
    public static class GraphRenderer
    {
        public static List<PointF> GenerateNodePositions(int n, int panelWidth, int panelHeight)
        {
            var nodes = new List<PointF>();
            float radius = Math.Min(panelWidth, panelHeight) * 0.4f;
            var center = new PointF(panelWidth / 2f, panelHeight / 2f);
            for (int i = 0; i < n; i++)
            {
                float angle = (float)(2 * Math.PI * i / n);
                nodes.Add(new PointF(
                    center.X + (float)Math.Cos(angle) * radius,
                    center.Y + (float)Math.Sin(angle) * radius
                ));
            }
            return nodes;
        }

        public static void DrawFullGraph(Graphics g, List<PointF> nodePositions)
        {
            using (var edgePen = new Pen(Color.LightGray, 1))
            {
                int n = nodePositions.Count;
                for (int i = 0; i < n; i++)
                    for (int j = i + 1; j < n; j++)
                        g.DrawLine(edgePen, nodePositions[i], nodePositions[j]);
            }
            using (var font = new Font("Arial", 10))
            {
                int n = nodePositions.Count;
                for (int i = 0; i < n; i++)
                {
                    var pt = nodePositions[i];
                    g.FillEllipse(Brushes.White, pt.X - 10, pt.Y - 10, 20, 20);
                    g.DrawEllipse(Pens.Black, pt.X - 10, pt.Y - 10, 20, 20);
                    g.DrawString((i + 1).ToString(), font, Brushes.Black, pt.X - 5, pt.Y - 7);
                }
            }
        }

        public static void DrawPath(Graphics g, List<PointF> nodePositions, int[] path)
        {
            using (var pathPen = new Pen(Color.Red, 2))
            {
                for (int i = 0; i < path.Length - 1; i++)
                {
                    g.DrawLine(pathPen,
                               nodePositions[path[i]],
                               nodePositions[path[i + 1]]);
                }
            }
        }
    }
}