// Copyright (C) 2018 KAMGAM e.U. - All rights reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace kamgam.editor.freeformselection
{
    public class TriangulatedShape2D
    {
        protected LibTessDotNet.ExtendedTess _tess;

        public int NumTriangles
        {
            get { return _tess.ElementCount; }
            set { }
        }

        public TriangulatedShape2D(List<Vector2> points, Vector2? extraEndPoint = null)
        {
            _tess = new LibTessDotNet.ExtendedTess();
            var contour = new LibTessDotNet.ContourVertex[points.Count + (extraEndPoint.HasValue ? 1 : 0)];
            for (int i = 0; i < points.Count; i++)
            {
                contour[i].Position = new LibTessDotNet.Vec3(points[i].x, points[i].y, 0);
            }
            if (extraEndPoint.HasValue)
            {
                contour[contour.Length - 1].Position = new LibTessDotNet.Vec3(extraEndPoint.Value.x, extraEndPoint.Value.y, 0);
            }
            _tess.AddContour(contour, LibTessDotNet.ContourOrientation.Original);
            _tess.Tessellate(LibTessDotNet.WindingRule.EvenOdd, LibTessDotNet.ElementType.Polygons, 3);
        }

        public void DrawShapeAsHandle()
        {
            var col = Handles.color;
            Handles.color = new Color(0.50f, 0.75f, 1f, 0.3f);

            float p0x, p0y, p1x, p1y, p2x, p2y;
            int numTriangles = _tess.ElementCount;
            for (int i = 0; i < numTriangles; i++)
            {
                p0x = _tess.Vertices[_tess.Elements[i * 3]].Position.X;
                p0y = _tess.Vertices[_tess.Elements[i * 3]].Position.Y;
                p1x = _tess.Vertices[_tess.Elements[i * 3 + 1]].Position.X;
                p1y = _tess.Vertices[_tess.Elements[i * 3 + 1]].Position.Y;
                p2x = _tess.Vertices[_tess.Elements[i * 3 + 2]].Position.X;
                p2y = _tess.Vertices[_tess.Elements[i * 3 + 2]].Position.Y;
                Handles.DrawAAConvexPolygon(
                    FreeFormSelection.GUIPointToWorldPoint(p0x, p0y),
                    FreeFormSelection.GUIPointToWorldPoint(p1x, p1y),
                    FreeFormSelection.GUIPointToWorldPoint(p2x, p2y)
                );
            }

            Handles.color = col;
        }

        public bool IsPointInShape(UnityEngine.Vector2 point)
        {
            return _tess.IsPointInShape(point);
        }
    }
}
#endif
