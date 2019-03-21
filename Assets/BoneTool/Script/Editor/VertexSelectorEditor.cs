using BoneTool.Script.Runtime;
using Chaos;
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
    private static BoneWeight _bws;
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
            VertexSelector com = _selectedTransform.GetOrAddComponent<VertexSelector>();
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
            _selectedTransform.DestroyComponentImmediate<VertexSelector>();
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

    private static int GetNearestPoint(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 ip)
    {

        Vector3 p0 = ip - v0;
        Vector3 p1 = ip - v1;
        Vector3 p2 = ip - v2;
        float sd0 = p0.sqrMagnitude;
        float sd1 = p1.sqrMagnitude;
        float sd2 = p2.sqrMagnitude;
        if (sd0 < sd1 && sd0 < sd2)
        {
            return 0;
        }
        else if (sd1 < sd0 && sd1 < sd2)
        {
            return 1;
        }
        else
        {
            return 2;
        }
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
            Handles.Label(_pos + new Vector3(0, 0.01f, 0), string.Format("BoneWeights ({0}:{1}),({2}:{3}),({4}:{5}),({6}:{7})", _bws.boneIndex0, _bws.weight0, _bws.boneIndex1, _bws.weight1, _bws.boneIndex2, _bws.weight2, _bws.boneIndex3, _bws.weight3));
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
                                Vector3 v0 = smr.GetSkinnedVertexWS(i0);
                                Vector3 v1 = smr.GetSkinnedVertexWS(i1);
                                Vector3 v2 = smr.GetSkinnedVertexWS(i2);
                                Vector3 ip;
                                if (!ray.CheckIntersect(v0, v1, v2, out ip, ref minIntersectDistance))
                                {
                                    continue;
                                }
                                switch (GetNearestPoint(v0, v1, v2, ip))
                                {
                                    case 0:
                                        minIntersectVertexIndex = i0;
                                        minIntersectPosition = v0;
                                        break;
                                    case 1:
                                        minIntersectVertexIndex = i1;
                                        minIntersectPosition = v1;
                                        break;
                                    case 2:
                                    default:
                                        minIntersectVertexIndex = i2;
                                        minIntersectPosition = v2;
                                        break;
                                }
                            }
                        }
                        if (minIntersectVertexIndex >= 0)
                        {
                            _pos = minIntersectPosition;
                            _idx = minIntersectVertexIndex;
                            _vtx = vertices[minIntersectVertexIndex];
                            _bws = mesh.boneWeights[minIntersectVertexIndex];
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
                                if (!localRay.CheckIntersect(v0, v1, v2, out ip, ref minIntersectDistance))
                                {
                                    continue;
                                }
                                switch (GetNearestPoint(v0, v1, v2, ip))
                                {
                                    case 0:
                                        minIntersectVertexIndex = i0;
                                        minIntersectPosition = v0;
                                        break;
                                    case 1:
                                        minIntersectVertexIndex = i1;
                                        minIntersectPosition = v1;
                                        break;
                                    case 2:
                                    default:
                                        minIntersectVertexIndex = i2;
                                        minIntersectPosition = v2;
                                        break;
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