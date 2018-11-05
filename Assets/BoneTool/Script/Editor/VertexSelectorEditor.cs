using System;
using System.Collections;
using System.Collections.Generic;
using BoneTool.Script.Runtime;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VertexSelector))]
public class VertexSelectorEditor : Editor
{
    private static bool _active;

    private static Transform _selectedTransform;

    private static Vector3 _pos;
    private static int _idx;
    private static Vector3 _vtx;
#if DEBUG_VERTEX_SELECTOR
    private static Shader _shader;
#endif

    [MenuItem("Tools/VertexSelector", true)]
    static bool ValidateSceneViewCustomSceneMode()
    {
        Menu.SetChecked("Tools/VertexSelector", _active);
        return true;
    }

    [MenuItem("Tools/VertexSelector")]
    static void BoneMode()
    {
        _active = !_active;
        if (_active)
        {
            Transform trans = Selection.activeTransform;
            if (trans == null ||
                trans.GetComponent<MeshRenderer>() == null && trans.GetComponent<SkinnedMeshRenderer>() == null)
            {
                Debug.LogError("Please select some MeshRenderer or SkinnedMeshRenderer");
                _active = false;
                return;
            }
            _selectedTransform = trans;
            _idx = -1;
            VertexSelector com = _selectedTransform.GetComponent<VertexSelector>();
            if (null == com)
            {
                com = _selectedTransform.gameObject.AddComponent<VertexSelector>();
            }
            EditorApplication.update += Update;
#if DEBUG_VERTEX_SELECTOR
            SceneView view = SceneView.lastActiveSceneView;
            if (null != view)
            {
                _shader = Shader.Find("Hidden/VertexSelector");
                view.SetSceneViewShaderReplace(_shader, null);
            }
#if UNITY_2018_1_OR_NEWER
           EditorApplication.quitting += SceneViewClearSceneView;
#endif
#endif
        }
        else
        {
            VertexSelector com = _selectedTransform.GetComponent<VertexSelector>();
            if (null != com)
            {
                VertexSelector.DestroyImmediate(com);
            }
            EditorApplication.update -= Update;

#if DEBUG_VERTEX_SELECTOR
            SceneViewClearSceneView();
#endif
        }
    }

#if DEBUG_VERTEX_SELECTOR
    static void SceneViewClearSceneView()
    {
        GC.Collect();
        Resources.UnloadUnusedAssets();
        SceneView view = SceneView.lastActiveSceneView;
        if (null != view)
        {
            view.SetSceneViewShaderReplace(null, null);
            view.Repaint();
        }
        _shader = null;
    }
#endif

    private static void Update()
    {
        Selection.activeTransform = _selectedTransform;
    }

    float CheckIntersect(Vector3 v0, Vector3 v1, Vector3 v2, Ray ray, float maxDist, out Vector3 ip)
    {

        Vector3 normal = Vector3.Cross(v0 - v1, v1 - v2).normalized;
        float x = Vector3.Dot(normal, v1 - ray.origin) / Vector3.Dot(normal, ray.direction);
        ip = ray.origin + x * ray.direction;
        if (x > 0 && x < maxDist)
        {
            Vector3 p0 = (ip - v0).normalized;
            Vector3 p1 = (ip - v1).normalized;
            Vector3 p2 = (ip - v2).normalized;
            if (Mathf.Approximately(-1, Vector3.Dot(p0, p1)) ||
                Mathf.Approximately(-1, Vector3.Dot(p1, p2)) ||
                Mathf.Approximately(-1, Vector3.Dot(p2, p0)))
            {
                return x;
            }
            Vector3 np01 = Vector3.Cross(p0, p1).normalized;
            Vector3 np12 = Vector3.Cross(p1, p2).normalized;
            Vector3 np20 = Vector3.Cross(p2, p0).normalized;
            if (Mathf.Approximately(1, Vector3.Dot(np01, np12)) &&
                Mathf.Approximately(1, Vector3.Dot(np12, np20)))
            {
                return x;
            }
        }
        return float.MaxValue;
    }

    private void OnSceneGUI()
    {
        if (!_active)
        {
            return;
        }
        
        if (Event.current.type == EventType.Repaint && _idx >= 0)
        {
            Handles.color = Color.red;
            Handles.matrix = Matrix4x4.identity;
            Handles.CubeHandleCap(0, _pos, Quaternion.identity, 0.01f, EventType.Repaint);
            Handles.Label(_pos + new Vector3(0, 0.02f, 0), string.Format("Vertex ID {0}, World Position {1} Model Position {2}", _idx, _pos.ToString("F"), _vtx.ToString("F")));
        }
        Selection.activeTransform = _selectedTransform;
        if (Event.current != null && Event.current.type == EventType.MouseDown && Tools.current == Tool.Move)
        {
            Transform trans = Selection.activeTransform;
            if (trans != null)
            {
                SkinnedMeshRenderer smr;
                MeshRenderer mr;
                if (null != (smr = trans.GetComponent<SkinnedMeshRenderer>()))
                {
                    float minIntersectDistance = float.MaxValue;
                    int minIntersectVertexIndex = -1;
                    Vector3 minIntersectPosition = Vector3.zero;
                    Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                    Bounds bb = smr.bounds;
                    if (bb.IntersectRay(ray))
                    {
                        Mesh mesh = smr.sharedMesh;
                        Vector3[] vertices = mesh.vertices;
                        for (int s = 0, smax = mesh.subMeshCount; s < smax; s++)
                        {
                            int[] triangles = mesh.GetTriangles(s);
                            for (int t = 0, tmax = triangles.Length/3; t < tmax; t++)
                            {
                                int i0 = triangles[t * 3];
                                int i1 = triangles[t * 3 + 1];
                                int i2 = triangles[t * 3 + 2];
                                Vector3 v0 = smr.GetSkinnedVertex(i0);
                                Vector3 v1 = smr.GetSkinnedVertex(i1);
                                Vector3 v2 = smr.GetSkinnedVertex(i2);
                                Vector3 ip;
                                float x = CheckIntersect(v0, v1, v2, ray, minIntersectDistance, out ip);
                                if (x >= minIntersectDistance)
                                {
                                    continue;
                                }
                                minIntersectDistance = x;
                                Vector3 p0 = ip - v0;
                                Vector3 p1 = ip - v1;
                                Vector3 p2 = ip - v2;
                                float sd0 = p0.sqrMagnitude;
                                float sd1 = p1.sqrMagnitude;
                                float sd2 = p2.sqrMagnitude;
                                if (sd0 < sd1 && sd0 < sd2)
                                {
                                    minIntersectVertexIndex = i0;
                                    minIntersectPosition = v0;
                                }
                                else if (sd1 < sd0 && sd1 < sd2)
                                {
                                    minIntersectVertexIndex = i1;
                                    minIntersectPosition = v1;
                                }
                                else
                                {
                                    minIntersectVertexIndex = i2;
                                    minIntersectPosition = v2;
                                }
                            }
                        }
                        if (minIntersectVertexIndex >= 0)
                        {
                            _pos = minIntersectPosition;
                            _idx = minIntersectVertexIndex;
                            _vtx = vertices[minIntersectVertexIndex];
                        }
                    }
                }
                else if (null != (mr = trans.GetComponent<MeshRenderer>()))
                {
                    float minIntersectDistance = float.MaxValue;
                    int minIntersectVertexIndex = -1;
                    Vector3 minIntersectPosition = Vector3.zero;
                    Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                    Bounds bb = mr.bounds;
                    if (bb.IntersectRay(ray))
                    {
                        Matrix4x4 w2m = mr.worldToLocalMatrix;
                        Matrix4x4 m2w = mr.localToWorldMatrix;
                        Ray localRay = new Ray(w2m.MultiplyPoint3x4(ray.origin), w2m.MultiplyVector(ray.direction));
                        MeshFilter mf = mr.GetComponent<MeshFilter>();
                        Mesh mesh = mf.sharedMesh;
                        Vector3[] vertices = mesh.vertices;
                        for (int s = 0, smax = mesh.subMeshCount; s < smax; s++)
                        {
                            int[] triangles = mesh.GetTriangles(s);
                            for (int t = 0, tmax = triangles.Length / 3; t < tmax; t++)
                            {
                                int i0 = triangles[t * 3];
                                int i1 = triangles[t * 3 + 1];
                                int i2 = triangles[t * 3 + 2];
                                Vector3 v0 = vertices[i0];
                                Vector3 v1 = vertices[i1];
                                Vector3 v2 = vertices[i2];
                                Vector3 ip;
                                float x = CheckIntersect(v0, v1, v2, localRay, minIntersectDistance, out ip);
                                if (x > minIntersectDistance)
                                {
                                    continue;
                                }
                                minIntersectDistance = x;
                                Vector3 p0 = ip - v0;
                                Vector3 p1 = ip - v1;
                                Vector3 p2 = ip - v2;
                                float sd0 = p0.sqrMagnitude;
                                float sd1 = p1.sqrMagnitude;
                                float sd2 = p2.sqrMagnitude;
                                if (sd0 < sd1 && sd0 < sd2)
                                {
                                    minIntersectVertexIndex = i0;
                                    minIntersectPosition = v0;
                                }
                                else if (sd1 < sd0 && sd1 < sd2)
                                {
                                    minIntersectVertexIndex = i1;
                                    minIntersectPosition = v1;
                                }
                                else
                                {
                                    minIntersectVertexIndex = i2;
                                    minIntersectPosition = v2;
                                }
                            }
                        }
                        if (minIntersectVertexIndex >= 0)
                        {
                            _pos = m2w.MultiplyPoint3x4(minIntersectPosition);
                            _idx = minIntersectVertexIndex;
                            _vtx = vertices[minIntersectVertexIndex];
                        }
                    }
                }
            }
        }

#if DEBUG_VERTEX_SELECTOR
        if (_idx >= 0)
        {
            Shader.SetGlobalColor("SelectedColor", Color.red);
            Shader.SetGlobalInt("SelectedVid", _idx);
        }
#endif
    }
}