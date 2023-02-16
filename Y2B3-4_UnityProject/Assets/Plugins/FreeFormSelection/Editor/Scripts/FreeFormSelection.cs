// Copyright (C) 2018 KAMGAM e.U. - All rights reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace kamgam.editor.freeformselection
{
    [InitializeOnLoad]
    public class FreeFormSelection
    {
        public enum SelectionAction { Set, Add, Remove }
        public enum SelectionMode { FreeForm, Edges, Brush }

        private static Object[] _existingSelection;
        private static Object[] _newSelection;
        private static List<Vector2> _mousePathForOutline;
        private static Vector2? _mousePathForOutlineExtraEnd;
        private static List<Vector2> _mousePathForTesselation;
        private static Vector2? _mousePathForTesselationExtraEnd;
        private static TriangulatedShape2D _tesselatedShape;
        private static bool _isActivationKeyPressed = false;
        private static bool _isSelecting = false;
        private static Dictionary<Mesh, Vector3[]> _vertexCache;
        private static Vector2 _lastMousePosCache;
        public static int ControlId = EditorGUIUtility.GetControlID(FocusType.Passive);
        private static Vector2 _lastMousePosWhileDraggingBrush;
        private static Tool _lastTool;

        static FreeFormSelection()
        {
            if (FreeFormSelection_Settings.instance == null)
            {
                Debug.LogWarning("FreeFormSelection plugin did not find any settings and will do nothing.\nPlease create them in a 'Resources' folder via Assets -> Create -> FreeFormSelection Settings.");
            }

#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui += UpdateView;
#else
            SceneView.onSceneGUIDelegate += UpdateView;
#endif
        }

        private static bool isMouseInSceneView()
        {
            return EditorWindow.mouseOverWindow != null && SceneView.lastActiveSceneView == EditorWindow.mouseOverWindow;
        }

        /// <summary>
        /// Checks whether or not the object has anything to do with prefabs.
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        static bool IsPrefab(GameObject go)
        {
#if UNITY_2018_3_OR_NEWER
            return PrefabUtility.IsPartOfAnyPrefab(go);
#else
            return PrefabUtility.GetPrefabType(go) != PrefabType.None;
#endif
        }

        private static void UpdateView(SceneView sceneView)
        {
            if (!FreeFormSelection_Settings.enablePlugin)
            {
                return;
            }

            bool mouseInSceneView = isMouseInSceneView();
            // Stop if mouse is out of scene view, unless we
            // are in the middle of a selection.
            if (!mouseInSceneView && !_isSelecting)
            {
                if (_isActivationKeyPressed)
                {
                    _isActivationKeyPressed = false;
                    if (FreeFormSelection_Settings.hideHandlesWhileSelecting)
                    {
                        Tools.current = _lastTool;
                    }
                }
                return;
            }

            // remember selection to reset on error
            var selectedObjects = Selection.gameObjects;

            // Investigate: some refactoring might be in order, feature creeeeep!!
            try
            {
                Event evt = Event.current;
                if (evt != null)
                {
                    SelectionMode selectionMode = FreeFormSelection_Settings.selectionMode;

                    // key presses
                    if (evt.type == EventType.KeyDown)
                    {
                        if (Event.current.keyCode == FreeFormSelection_Settings.pushSelectionKey)
                        {
                            if (!_isActivationKeyPressed)
                            {
                                _isActivationKeyPressed = true;
                                if (FreeFormSelection_Settings.hideHandlesWhileSelecting)
                                {
                                    _lastTool = Tools.current;
                                    Tools.current = Tool.None;
                                }
                            }
                        }
                        if (_isActivationKeyPressed && Event.current.keyCode == FreeFormSelection_Settings.pushToChangeModeKey)
                        {
                            evt.Use();
                        }
                    }
                    else if (evt.type == EventType.KeyUp)
                    {
                        if (Event.current.keyCode == FreeFormSelection_Settings.pushSelectionKey)
                        {
                            if (_isActivationKeyPressed)
                            {
                                _isActivationKeyPressed = false;
                                if (FreeFormSelection_Settings.hideHandlesWhileSelecting)
                                {
                                    Tools.current = _lastTool;
                                }
                            }
                        }
                        if (_isActivationKeyPressed && Event.current.keyCode == FreeFormSelection_Settings.pushToChangeModeKey)
                        {
                            if (selectionMode == SelectionMode.FreeForm) FreeFormSelection_Settings.selectionMode = SelectionMode.Edges;
                            if (selectionMode == SelectionMode.Edges) FreeFormSelection_Settings.selectionMode = SelectionMode.Brush;
                            if (selectionMode == SelectionMode.Brush) FreeFormSelection_Settings.selectionMode = SelectionMode.FreeForm;
                            evt.Use();
                        }
                        if (Event.current.keyCode == KeyCode.Escape)
                        {
                            _isActivationKeyPressed = false;
                            _isSelecting = false;
                            GUIUtility.hotControl = 0;
                        }
                    }

                    // mouse events
                    bool forceRepaint = false;
                    if (_isActivationKeyPressed || _isSelecting)
                    {
                        int id = ControlId;
                        switch (evt.GetTypeForControl(id))
                        {
                            case EventType.Layout:
                                HandleUtility.AddDefaultControl(id);
                                break;

                            case EventType.MouseDown:
                                if (HandleUtility.nearestControl == id && evt.button == 0)
                                {
                                    GUIUtility.hotControl = id;
                                    if (!_isSelecting)
                                    {
                                        _isSelecting = true;
                                        _existingSelection = Selection.objects;
                                        _mousePathForTesselation = new List<Vector2>();
                                        _mousePathForOutline = new List<Vector2>();
                                        _vertexCache = new Dictionary<Mesh, Vector3[]>();

                                        if (selectionMode == SelectionMode.FreeForm)
                                        {
                                            _mousePathForTesselation.Add(evt.mousePosition);
                                            _mousePathForOutline.Add(evt.mousePosition);
                                        }
                                        else if (selectionMode == SelectionMode.Edges)
                                        {
                                            _mousePathForTesselation.Add(evt.mousePosition);
                                            _mousePathForOutline.Add(evt.mousePosition);
                                        }
                                        else if (selectionMode == SelectionMode.Brush)
                                        {
                                            _lastMousePosWhileDraggingBrush = evt.mousePosition;
                                            var brushPointsForTess = getCirclePoints(21, FreeFormSelection_Settings.brushSize);
                                            displacePoints(brushPointsForTess, evt.mousePosition);
                                            _mousePathForTesselation.Clear();
                                            _mousePathForTesselation = brushPointsForTess.ToList();
                                            if (!evt.shift && !EditorGUI.actionKey)
                                            {
                                                _existingSelection = new Object[0];
                                            }
                                            // Investigate: use circle as selection check instead of circle tesselation (performance)
                                            _existingSelection = tesselateAndSelect(true, EditorGUI.actionKey); // updates _tesselatedShape
                                        }
                                        evt.Use();
                                    }
                                    // edges selections are triggered in mouse up to allow dragging.
                                }
                                forceRepaint = true;
                                break;

                            case EventType.MouseDrag:
                                if (GUIUtility.hotControl == id)
                                {
                                    if (selectionMode == SelectionMode.FreeForm)
                                    {
                                        // Record the path on "every" frame to avoid sluggish feeling (used to draw the path).
                                        if (Vector2.Distance(_mousePathForTesselation.Last(), evt.mousePosition) > 1f)
                                        {
                                            _mousePathForOutline.Add(evt.mousePosition);
                                        }

                                        // Update triangulation and selection only if the cursor has moved a certain distance.
                                        if (Vector2.Distance(_mousePathForTesselation.Last(), evt.mousePosition) > 10f)
                                        {
                                            _mousePathForTesselation.Add(evt.mousePosition);
                                            tesselateAndSelect(evt.shift, EditorGUI.actionKey); // updates _tesselatedShape
                                        }

                                        evt.Use();
                                    }
                                    // Same code as in MouseMove for edge mode.
                                    else if (selectionMode == SelectionMode.Edges && _isSelecting)
                                    {
                                        // Record the path on "every" frame to avoid sluggish feeling (used to draw the path).
                                        if (Vector2.Distance(_mousePathForTesselation.Last(), evt.mousePosition) > 1f)
                                        {
                                            _mousePathForOutlineExtraEnd = evt.mousePosition;
                                        }

                                        // Update triangulation and selection only if the cursor has moved a certain distance.
                                        if (!_mousePathForTesselationExtraEnd.HasValue
                                            || Vector2.Distance(_mousePathForTesselationExtraEnd.Value, evt.mousePosition) > 10f)
                                        {
                                            _mousePathForTesselationExtraEnd = evt.mousePosition;
                                            tesselateAndSelect(evt.shift, EditorGUI.actionKey); // updates _tesselatedShape
                                        }
                                        evt.Use();
                                    }
                                    else if (selectionMode == SelectionMode.Brush)
                                    {
                                        float delta = Vector2.Distance(_lastMousePosWhileDraggingBrush, evt.mousePosition);
                                        float brushSize = FreeFormSelection_Settings.brushSize;
                                        if (delta > brushSize * 0.1f)
                                        {
                                            int brushSteps = (int)Mathf.Ceil(delta / brushSize);
                                            if (brushSteps > 1) { brushSteps *= 3; } // partitions should be max 1/3 of the brushshize apart
                                            var brushPoints = getCirclePoints(21, brushSize);
                                            Vector2[] brushPointsForTess;
                                            for (int s = 1; s <= brushSteps; ++s)
                                            {
                                                brushPointsForTess = displacePoints(brushPoints, _lastMousePosWhileDraggingBrush + ((evt.mousePosition - _lastMousePosWhileDraggingBrush) * (s / (float)brushSteps)), true);
                                                _mousePathForTesselation.Clear();
                                                _mousePathForTesselation = brushPointsForTess.ToList();
                                                // Investigate: use circle as selection check instead of circle tesselation (performance)
                                                _existingSelection = tesselateAndSelect(true, EditorGUI.actionKey); // updates _tesselatedShape
                                            }
                                            _lastMousePosWhileDraggingBrush = evt.mousePosition;
                                        }
                                    }
                                }
                                forceRepaint = true;
                                break;
                            case EventType.ScrollWheel:
                                if (FreeFormSelection_Settings.useMouseWheel)
                                {
                                    if (_isSelecting || (_isActivationKeyPressed && selectionMode == SelectionMode.Brush))
                                    {
                                        FreeFormSelection_Settings.brushSize -= evt.delta.y;
                                        evt.Use();
                                    }
                                }
                                forceRepaint = true;
                                break;

                            case EventType.MouseMove:
                                // Mouse position in mouse move is relative to the editor view which it's moving in,
                                // This means if we move outside of the sceneview the positin is wrong.
                                // To correct that we use the cached position from Repaint which seems to always be accurate.
                                // We'll do the same in MouseUp as well.
                                var mousePositionInMove = mouseInSceneView ? evt.mousePosition : _lastMousePosCache;
                                if (selectionMode == SelectionMode.Edges && _isSelecting)
                                {
                                    // Record the path on "every" frame to avoid sluggish feeling (used to draw the path).
                                    if (Vector2.Distance(_mousePathForTesselation.Last(), mousePositionInMove) > 1f)
                                    {
                                        _mousePathForOutlineExtraEnd = mousePositionInMove;
                                    }

                                    // Update triangulation and selection only if the cursor has moved a certain distance.
                                    if (!_mousePathForTesselationExtraEnd.HasValue
                                        || Vector2.Distance(_mousePathForTesselationExtraEnd.Value, mousePositionInMove) > 10f)
                                    {
                                        _mousePathForTesselationExtraEnd = mousePositionInMove;
                                        tesselateAndSelect(evt.shift, EditorGUI.actionKey); // updates _tesselatedShape
                                    }
                                    evt.Use();
                                }
                                forceRepaint = true;
                                break;

                            case EventType.Repaint:
                                // see comment on MouseMove
                                _lastMousePosCache = evt.mousePosition;

                                // draw cursor info
                                if (!_isSelecting && mouseInSceneView)
                                {
                                    drawCursorInfoIcon(evt.mousePosition, selectionMode, evt.shift);
                                }

                                if ((GUIUtility.hotControl == id || selectionMode == SelectionMode.Edges) && _isSelecting)
                                {
                                    // draw outline
                                    if (_mousePathForOutline != null && _mousePathForOutline.Count > 0)
                                    {
                                        // Investigate: The GUIPointToWorldRay calculation could be done
                                        // just once while dragging, but this leads to an offset in rendering.
                                        var outlinePoints = _mousePathForOutline.Select(v2 => GUIPointToWorldPoint(v2));
                                        if (_mousePathForOutlineExtraEnd.HasValue)
                                        {
                                            outlinePoints = outlinePoints.Concat(new Vector3[] { GUIPointToWorldPoint(_mousePathForOutlineExtraEnd.Value) });
                                        }
                                        var color = Handles.color;
                                        Handles.color = getOutlineColor(evt.shift, selectionMode);
                                        Handles.DrawPolyLine(outlinePoints.ToArray());
                                        Handles.color = color;
                                    }

                                    // draw shape
                                    if (_tesselatedShape != null)
                                    {
                                        _tesselatedShape.DrawShapeAsHandle();
                                    }

                                    if (selectionMode == SelectionMode.FreeForm)
                                    {
                                        // draw most recent triangle (to reduce the feeling of low latency while dragging)
                                        if (_mousePathForTesselation != null && _mousePathForTesselation.Count > 2)
                                        {
                                            var col = Handles.color;
                                            Handles.color = new Color(0.60f, 0.75f, 1f, 0.3f);
                                            var p0 = _mousePathForTesselation.First();
                                            var p1 = _mousePathForTesselation.Last();
                                            var p2 = evt.mousePosition;
                                            Handles.DrawAAConvexPolygon(
                                                GUIPointToWorldPoint(p0),
                                                GUIPointToWorldPoint(p1),
                                                GUIPointToWorldPoint(p2)
                                            );
                                            Handles.color = col;
                                        }
                                    }
                                    else if (selectionMode == SelectionMode.Edges)
                                    {
                                        // draw most recent triangles (to reduce the feeling of low latency while dragging)
                                        if (_mousePathForTesselation != null
                                            && _mousePathForTesselation.Count > 1
                                            && _mousePathForTesselationExtraEnd.HasValue)
                                        {
                                            var col = Handles.color;
                                            Handles.color = new Color(0.60f, 0.75f, 1f, 0.3f);
                                            // tri 1
                                            var p0 = _mousePathForTesselation.Last();
                                            var p1 = _mousePathForTesselationExtraEnd.Value;
                                            var p2 = evt.mousePosition;
                                            Handles.DrawAAConvexPolygon(
                                                GUIPointToWorldPoint(p0),
                                                GUIPointToWorldPoint(p1),
                                                GUIPointToWorldPoint(p2)
                                            );
                                            // tri 2
                                            p0 = _mousePathForTesselation.First();
                                            p1 = _mousePathForTesselationExtraEnd.Value;
                                            p2 = evt.mousePosition;
                                            Handles.DrawAAConvexPolygon(
                                                GUIPointToWorldPoint(p0),
                                                GUIPointToWorldPoint(p1),
                                                GUIPointToWorldPoint(p2)
                                            );
                                            Handles.color = col;
                                        }
                                    }
                                }

                                if (selectionMode == SelectionMode.Brush)
                                {
                                    var brushCircle = getCirclePoints(21, FreeFormSelection_Settings.brushSize);
                                    displacePoints(brushCircle, evt.mousePosition);
                                    var color = Handles.color;
                                    Handles.color = getOutlineColor(evt.shift, selectionMode);
                                    Handles.DrawPolyLine(iconGUIToWorld(brushCircle));
                                    Handles.color = color;
                                }
                                break;

                            case EventType.MouseUp:
                                if (GUIUtility.hotControl == id && evt.button == 0)
                                {
                                    if (selectionMode == SelectionMode.FreeForm
                                        || selectionMode == SelectionMode.Brush)
                                    {
                                        _isSelecting = false;
                                        _mousePathForTesselation.Clear();
                                        _mousePathForTesselation = null;
                                        _mousePathForOutline.Clear();
                                        _mousePathForOutline = null;
                                        _tesselatedShape = null;
                                        GUIUtility.hotControl = 0;
                                    }

                                    if (_isSelecting)
                                    {
                                        if (selectionMode == SelectionMode.Edges &&
                                            Vector2.Distance(_mousePathForTesselation.Last(), evt.mousePosition) > 1f)
                                        {
                                            // see comment on MouseMove
                                            var mousePositionInUp = mouseInSceneView ? evt.mousePosition : _lastMousePosCache;
                                            _mousePathForOutline.Add(mousePositionInUp);
                                            _mousePathForTesselation.Add(mousePositionInUp);
                                            tesselateAndSelect(evt.shift, EditorGUI.actionKey); // updates _tesselatedShape
                                            evt.Use();
                                        }
                                    }
                                }
                                forceRepaint = true;
                                break;
                        }
                    }

                    if (!_isActivationKeyPressed && _isSelecting && selectionMode == SelectionMode.Edges)
                    {
                        _isSelecting = false;
                        _mousePathForTesselation.Clear();
                        _mousePathForTesselation = null;
                        _mousePathForOutline.Clear();
                        _mousePathForOutline = null;
                        _tesselatedShape = null;
                        _mousePathForOutlineExtraEnd = null;
                        _mousePathForTesselationExtraEnd = null;
                        GUIUtility.hotControl = 0;
                    }

                    if (forceRepaint)
                    {
                        SceneView.lastActiveSceneView?.Repaint();
                    }
                }

                // Debug
                /*
                Handles.BeginGUI();
                if (_shapeFromMousePath != null)
                {
                    GUILayout.Label("Triangles: " + _shapeFromMousePath.NumTriangles);
                }
                Handles.EndGUI();
                */
            }
            catch (System.Exception e)
            {
                // reset selection in case of an error.
                Selection.objects = selectedObjects;
                Debug.LogWarning("Free Form Selection caused an unexpected Error:\n" + e.Message);
            }
        }

        private static Color getOutlineColor(bool shift, SelectionMode selectionMode)
        {
            if (EditorGUI.actionKey) return new Color(1.0f, 0.4f, 0.4f);
            if (shift) return new Color(0.4f, 1.0f, 0.4f);
            return Color.white;
        }

        private static void drawCursorInfoIcon(Vector2 mousePos, SelectionMode selectionMode, bool shift)
        {
            Vector2 dtm = new Vector2(14, 14); // distance to mouse
            float size = 8; // icons size (multiple of 2)
            Vector2[] icon = null;
            Vector2[] outline = null;

            // draw cursor info
            if (selectionMode == SelectionMode.FreeForm)
            {
                icon = getCirclePoints(8, size);
                // deform the circle to indicate "free form"
                icon[4] = icon[4] + new Vector2(size * 0.4f, 0);
                icon[0] = icon[0] + new Vector2(-size * 0.3f, 0);
                icon[8] = icon[8] + new Vector2(-size * 0.3f, 0);
                displacePoints(icon, new Vector2(size / 2, size / 2) + mousePos + dtm);
                outline = displacePoints(icon, new Vector2(1, 1), true);
            }

            if (selectionMode == SelectionMode.Edges)
            {
                icon = new Vector2[] {
                                        new Vector2( 0, size * 0.5f ),
                                        new Vector2( size, 0    ),
                                        new Vector2( size, size ),
                                        new Vector2( 0, size * 0.5f )
                                    };
                displacePoints(icon, mousePos + dtm);
                outline = displacePoints(icon, new Vector2(1, 1), true);
            }

            if (selectionMode == SelectionMode.Brush)
            {
                icon = getCirclePoints(8, size);
                displacePoints(icon, mousePos + dtm + new Vector2(size / 2, size / 2));
                outline = displacePoints(icon, new Vector2(1, 1), true);
            }

            if (outline != null && icon != null)
            {
                var color = Handles.color;
                Handles.color = Color.black;
                Handles.DrawPolyLine(iconGUIToWorld(outline));

                Handles.color = getOutlineColor(shift, selectionMode);
                Handles.DrawPolyLine(iconGUIToWorld(icon));
                if (selectionMode == SelectionMode.Brush)
                {
                    Handles.DrawAAConvexPolygon(iconGUIToWorld(icon));
                }
                else
                {
                    Handles.DrawPolyLine(iconGUIToWorld(icon));
                }
                Handles.color = color;
            }
        }

        private static Vector2[] getCirclePoints(int numOfPoints, float size)
        {
            Vector2[] points = null;

            var angleInRad = 0f;  // angle that will be increased each loop
            var step = Mathf.PI * 2f / numOfPoints;
            points = new Vector2[numOfPoints + 1];
            for (int i = 0; i < numOfPoints; ++i)
            {
                points[i] = new Vector2(size / 2 * Mathf.Cos(angleInRad), size / 2 * Mathf.Sin(angleInRad));
                angleInRad += step;
            }
            points[points.Length - 1] = points[0];

            return points;
        }


        private static Vector3[] iconGUIToWorld(Vector2[] points)
        {
            return points.Select(p => GUIPointToWorldPoint(p)).ToArray();
        }

        private static Vector2[] displacePoints(Vector2[] vectorList, Vector2 displacement, bool copy = false)
        {
            Vector2[] list;
            if (copy)
            {
                list = new Vector2[vectorList.Length];
                System.Array.Copy(vectorList, list, vectorList.Length);
            }
            else
            {
                list = vectorList;
            }

            for (int i = 0; i < list.Length; ++i)
            {
                list[i].x = list[i].x + displacement.x;
                list[i].y = list[i].y + displacement.y;
            }

            if (copy)
            {
                return list;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Sets _tesselatedShape and uses multiple external variables to do it (sideffects!).
        /// Returns the new final selection as array (equal to Selection.objects).
        /// </summary>
        /// <param name="shiftPressed"></param>
        /// <param name="actionKeyPressed"></param>
        /// <returns></returns>
        private static Object[] tesselateAndSelect(bool shiftPressed, bool actionKeyPressed)
        {
            // grab the settings
            int maxVerticesPerMesh = FreeFormSelection_Settings.maxVerticesPerMesh;
            bool greedyPrefabSelection = FreeFormSelection_Settings.greedyPrefabSelection;
            bool includeUI = FreeFormSelection_Settings.includeUI;

            // First we use unitys native rect selection to limit the set of possible objects.
            Rect rectSelection = new Rect();
            rectSelection.xMin = _mousePathForTesselation.Select(v2 => v2.x).Min();
            rectSelection.yMin = _mousePathForTesselation.Select(v2 => v2.y).Min();
            rectSelection.xMax = _mousePathForTesselation.Select(v2 => v2.x).Max();
            rectSelection.yMax = _mousePathForTesselation.Select(v2 => v2.y).Max();
            if (_mousePathForTesselationExtraEnd.HasValue)
            {
                rectSelection.xMin = Mathf.Min(rectSelection.xMin, _mousePathForTesselationExtraEnd.Value.x);
                rectSelection.yMin = Mathf.Min(rectSelection.yMin, _mousePathForTesselationExtraEnd.Value.y);
                rectSelection.xMax = Mathf.Max(rectSelection.xMax, _mousePathForTesselationExtraEnd.Value.x);
                rectSelection.yMax = Mathf.Max(rectSelection.yMax, _mousePathForTesselationExtraEnd.Value.y);
            }
            GameObject[] rectObjs = HandleUtility.PickRectObjects(rectSelection);

            // tesselate shape (triangulation)
            _tesselatedShape = new TriangulatedShape2D(_mousePathForTesselation, _mousePathForTesselationExtraEnd);

            // filter out those which are not inside the shape drawn by the path
            var set = new Dictionary<GameObject, bool>();
            foreach (var g in rectObjs)
            {
                bool addToSelection = true;
                bool usePivot = true;

                // MESH: if there is a mesh, then check if it's inside the selection
                // If it's a prefab, then also check the children (prefabs are selected automatically once a child is selected)
                var meshes = IsPrefab(g) ? g.GetComponentsInChildren<MeshFilter>() : g.GetComponents<MeshFilter>();
                if (meshes.Length > 0)
                {
                    usePivot = false;
                    for (int m = 0; m < meshes.Length; ++m)
                    {
                        var mesh = meshes[m];
                        // Improve: check for submeshes if verext count is 0, right now it's fall back to pivot.
                        if (mesh.sharedMesh == null || mesh.sharedMesh.vertexCount == 0)
                        {
                            usePivot = true;
                            break;
                        }

                        // Limit vertex check to "small" meshes (< N vertices in mesh).
                        if (mesh.sharedMesh.vertexCount < 30000)
                        {
                            if (!_vertexCache.ContainsKey(mesh.sharedMesh))
                            {
                                _vertexCache.Add(mesh.sharedMesh, mesh.sharedMesh.vertices);
                            }

                            // check the mesh vertices
                            bool allVerticesInShape = true;
                            int vertexCount = mesh.sharedMesh.vertexCount;
                            // We can't check every vertex due to performance.
                            // Current solution: check a maximum of "vertexStepSize" vertices and call it done.
                            // Investigate: improve performance (eliminate HandleUtility.WorldToGUIPoint maybe).
                            int vertexStepSize = Mathf.Max(1, vertexCount / maxVerticesPerMesh);
                            for (int i = 0; i < vertexCount; i += vertexStepSize)
                            {
                                // check if all vertices are in the shape. Stop if one isn't.
                                // Investigate: performance improvement by removing duplicate vertices before checking against the shape.
                                if (!_tesselatedShape.IsPointInShape(HandleUtility.WorldToGUIPoint(mesh.gameObject.transform.TransformPoint(_vertexCache[mesh.sharedMesh][i]))))
                                {
                                    addToSelection = false;
                                    allVerticesInShape = false;
                                    break;
                                }
                            }
                            // Select the whole prefab as soon as one of
                            // the submeshes is fully enclosed by the selection shape.
                            if (allVerticesInShape && IsPrefab(g))
                            {
                                if (greedyPrefabSelection || g == mesh.gameObject)
                                {
                                    addToSelection = true;
                                    break;
                                }
                                else
                                {
                                    set.Add(mesh.gameObject, true);
                                }
                            }
                            /*
                            if (addToSelection)
                                Debug.Log("selected " + g.name + "due to vertices");
                            //*/
                        }
                        else
                        {
                            // check the mesh bounding box
                            var bounds = mesh.sharedMesh.bounds;
                            Vector2[] bbPoints = new Vector2[8] // BB corners
                            {
                                                            HandleUtility.WorldToGUIPoint( mesh.gameObject.transform.TransformPoint( new Vector3( bounds.min.x,                 bounds.min.y,                 bounds.min.z ) ) ),
                                                            HandleUtility.WorldToGUIPoint( mesh.gameObject.transform.TransformPoint( new Vector3( bounds.min.x + bounds.size.x, bounds.min.y,                 bounds.min.z ) ) ),
                                                            HandleUtility.WorldToGUIPoint( mesh.gameObject.transform.TransformPoint( new Vector3( bounds.min.x,                 bounds.min.y + bounds.size.y, bounds.min.z ) ) ),
                                                            HandleUtility.WorldToGUIPoint( mesh.gameObject.transform.TransformPoint( new Vector3( bounds.min.x,                 bounds.min.y,                 bounds.min.z + bounds.size.z ) ) ),
                                                            HandleUtility.WorldToGUIPoint( mesh.gameObject.transform.TransformPoint( new Vector3( bounds.max.x,                 bounds.max.y,                 bounds.max.z ) ) ),
                                                            HandleUtility.WorldToGUIPoint( mesh.gameObject.transform.TransformPoint( new Vector3( bounds.max.x - bounds.size.x, bounds.max.y,                 bounds.max.z ) ) ),
                                                            HandleUtility.WorldToGUIPoint( mesh.gameObject.transform.TransformPoint( new Vector3( bounds.max.x,                 bounds.max.y - bounds.size.y, bounds.max.z ) ) ),
                                                            HandleUtility.WorldToGUIPoint( mesh.gameObject.transform.TransformPoint( new Vector3( bounds.max.x,                 bounds.max.y,                 bounds.max.z - bounds.size.z ) ) )
                            };
                            bool allBBVerticesInShape = true;
                            for (int i = 0; i < bbPoints.Length; ++i)
                            {
                                // check if all vertices are in the shape. Stop if one isn't.
                                if (!_tesselatedShape.IsPointInShape(bbPoints[i]))
                                {
                                    addToSelection = false;
                                    allBBVerticesInShape = false;
                                    break;
                                }
                            }
                            // Select the whole prefab as soon as one of
                            // the submesh BBs is fully enclosed by the selection shape.
                            if (allBBVerticesInShape && IsPrefab(g))
                            {
                                if (greedyPrefabSelection || g == mesh.gameObject)
                                {
                                    addToSelection = true;
                                    break;
                                }
                                else
                                {
                                    set.Add(mesh.gameObject, true);
                                }
                            }
                            /*
                            if (addToSelection)
                                Debug.Log("selected " + g.name + "due to bounding box");
                            //*/
                        }
                    }
                }

                // SKINNED MESHES (only partially supported)
                // Investigate: Improve skinned mesh support, aka find a performant solution without BB baking.
                //              This would probably require a deep dive into bindposes and bones. Right now it
                //              behaves just like unitys own native selection, wich is not very good in this case.
                //              I guess users should use the bones and not the meshes to change things anyway.
                //              Right now the solution is to just find the world space BB and use that.
                var skinnedMeshes = g.GetComponents<SkinnedMeshRenderer>();
                if (skinnedMeshes.Length > 0)
                {
                    usePivot = false;
                    for (int m = 0; m < skinnedMeshes.Length; ++m)
                    {
                        var bounds = skinnedMeshes[m].bounds;
                        Vector2[] bbPoints = new Vector2[8] // BB corners
                        {
                                                         HandleUtility.WorldToGUIPoint( new Vector3( bounds.min.x,                 bounds.min.y,                 bounds.min.z )) ,
                                                         HandleUtility.WorldToGUIPoint( new Vector3( bounds.min.x + bounds.size.x, bounds.min.y,                 bounds.min.z )) ,
                                                         HandleUtility.WorldToGUIPoint( new Vector3( bounds.min.x,                 bounds.min.y + bounds.size.y, bounds.min.z )) ,
                                                         HandleUtility.WorldToGUIPoint( new Vector3( bounds.min.x,                 bounds.min.y,                 bounds.min.z + bounds.size.z )) ,
                                                         HandleUtility.WorldToGUIPoint( new Vector3( bounds.max.x,                 bounds.max.y,                 bounds.max.z )) ,
                                                         HandleUtility.WorldToGUIPoint( new Vector3( bounds.max.x - bounds.size.x, bounds.max.y,                 bounds.max.z )) ,
                                                         HandleUtility.WorldToGUIPoint( new Vector3( bounds.max.x,                 bounds.max.y - bounds.size.y, bounds.max.z )) ,
                                                         HandleUtility.WorldToGUIPoint( new Vector3( bounds.max.x,                 bounds.max.y,                 bounds.max.z - bounds.size.z ))
                        };
                        bool allBBVerticesInShape = true;
                        for (int i = 0; i < bbPoints.Length; ++i)
                        {
                            // check if all vertices are in the shape. Stop if one isn't.
                            if (!_tesselatedShape.IsPointInShape(bbPoints[i]))
                            {
                                addToSelection = false;
                                allBBVerticesInShape = false;
                                break;
                            }
                        }
                        // Select the whole prefab as soon as one of
                        // the submesh BBs is fully enclosed by the selection shape.
                        if (allBBVerticesInShape && IsPrefab(g))
                        {
                            if (greedyPrefabSelection || g == skinnedMeshes[m].gameObject)
                            {
                                addToSelection = true;
                                break;
                            }
                            else
                            {
                                set.Add(skinnedMeshes[m].gameObject, true);
                            }
                        }
                        //*
                        if (addToSelection)
                            Debug.Log("selected " + g.name + "due to skinned mesh bounding box");
                        //*/
                    }
                }

                // RECT TRANSFORM
                var rectTransforms = g.GetComponentsInChildren<RectTransform>();
                if (rectTransforms.Length > 0)
                {
                    usePivot = false;
                    if (!includeUI)
                    {
                        addToSelection = false;
                    }
                    else
                    {
                        for (int r = 0; r < rectTransforms.Length; ++r)
                        {
                            var rectTransform = rectTransforms[r];
                            Vector3[] rectCorners = new Vector3[4];
                            rectTransform.GetWorldCorners(rectCorners);

                            bool allRectVerticesInShape = true;
                            for (int i = 0; i < rectCorners.Length; ++i)
                            {
                                // check if all vertices are in the shape. Stop if one isn't.
                                if (!_tesselatedShape.IsPointInShape(HandleUtility.WorldToGUIPoint(rectCorners[i])))
                                {
                                    addToSelection = false;
                                    allRectVerticesInShape = false;
                                    break;
                                }
                            }
                            // Select the whole prefab as soon as one of
                            // the subrects is fully enclosed by the selection shape.
                            if (allRectVerticesInShape && IsPrefab(g))
                            {
                                addToSelection = true;
                                break;
                            }
                        }
                    }
                }

                if (usePivot)
                {
                    // if there is no mesh, then use the pivot
                    addToSelection = _tesselatedShape.IsPointInShape(HandleUtility.WorldToGUIPoint(g.transform.position));
                    /*
                    if (addToSelection)
                        Debug.Log("selected " + g.name + "due to pivot");
                    //*/
                }

                set.Add(g, addToSelection);
            }

            var newSelection = set.Where(kv => kv.Value == true).Select(kv => kv.Key).ToArray();

            if (actionKeyPressed)
            {
                return UpdateSelection(_existingSelection, newSelection, SelectionAction.Remove);
            }
            else if (shiftPressed)
            {
                return UpdateSelection(_existingSelection, newSelection, SelectionAction.Add);
            }
            else
            {
                return UpdateSelection(_existingSelection, newSelection, SelectionAction.Set);
            }
        }

        public static Vector3 GUIPointToWorldPoint(float x, float y)
        {
            return GUIPointToWorldPoint(new Vector2(x, y));
        }

        public static Vector3 GUIPointToWorldPoint(Vector2 point)
        {
            return HandleUtility.GUIPointToWorldRay(point).origin + HandleUtility.GUIPointToWorldRay(point).direction * 0.01f;
        }

        private static Object[] UpdateSelection(Object[] existingSelection, Object[] newObjects, SelectionAction action)
        {
            Object[] newSelection;
            switch (action)
            {
                case SelectionAction.Add:
                    newSelection = existingSelection.Union(newObjects).ToArray();
                    Selection.objects = newSelection;
                    break;

                case SelectionAction.Remove:
                    newSelection = existingSelection.Except(newObjects).ToArray();
                    Selection.objects = newSelection;
                    break;

                case SelectionAction.Set:
                default:
                    Selection.objects = newObjects;
                    break;
            }

            return Selection.objects;
        }
    }
}
#endif
