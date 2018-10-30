#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BoneTool.Script.Runtime
{
    [ExecuteInEditMode]
    public class BoneDrawer : MonoBehaviour
    {
        public float BoneGizmosSize = 0.01f;
        public Color BoneColor = Color.white;
        public Color SelectedColor = Color.red;
        public Color SelectedChildrenColor = Color.magenta;
        public bool HideRoot;
        public bool EnableConstraint = true;

        private Transform _preRootNode;
        private Transform[] _childNodes;
        private List<BoneTransform> _previousTransforms;
        private Transform _rootNode;

        private Matrix4x4[] _bonesMatrices;
        private Mesh _mesh;
        private Material _material;

        public Transform[] GetChildNodes()
        {
            return _childNodes;
        }

        //private void Update()
        //{
        //    if (EnableConstraint && _previousTransforms != null)
        //    {
        //        foreach (var boneTransform in _previousTransforms)
        //        {
        //            if (boneTransform.Target && boneTransform.Target.hasChanged)
        //            {
        //                if (boneTransform.Target.parent.childCount == 1)
        //                {
        //                    boneTransform.Target.localPosition = boneTransform.LocalPosition;
        //                }
        //                else
        //                {
        //                    boneTransform.SetLocalPosition(boneTransform.Target.localPosition);
        //                }
        //            }
        //        }
        //    }
        //}
        private ComputeBuffer _buffer;
        private void OnPostRender()
        {
            //lock (lockObj)
            {
                if (_childNodes == null || _childNodes.Length == 0 || _previousTransforms == null || _previousTransforms.Count == 0)
                    PopulateChildren();

                int len = _childNodes.Length;
                if (_material == null)
                {
                    _material = new Material(Shader.Find("BoneDrawer"));
                }
                if (_mesh == null)
                {
                    _mesh = new Mesh();
                    Vector3[] points = new Vector3[len * 2];
                    Transform sceneRoot = _rootNode;
                    while (sceneRoot.parent != null)
                    {
                        sceneRoot = sceneRoot.parent;
                    }
                    for (int i = 0; i < len; i++)
                    {
                        points[i * 2] = sceneRoot.worldToLocalMatrix.MultiplyPoint3x4(_childNodes[i].position);
                        points[i * 2 + 1] = sceneRoot.worldToLocalMatrix.MultiplyPoint3x4(_childNodes[i].parent.position);
                    }
                    int[] lines = new int[len * 2];
                    for (int i = 0; i < len * 2; i++)
                    {
                        lines[i] = i;
                    }
                    _mesh.vertices = points;
                    _mesh.SetIndices(lines, MeshTopology.Lines, 0);
                }
                _bonesMatrices = new Matrix4x4[len];
                Transform sceneRoot_ = _rootNode;
                while (sceneRoot_.parent != null)
                {
                    sceneRoot_ = sceneRoot_.parent;
                }
                for (int i = 0; i < len; i++)
                {
                    _bonesMatrices[i] = sceneRoot_.worldToLocalMatrix * _childNodes[i].localToWorldMatrix;
                }
                if (_buffer != null)
                {
                    _buffer.Dispose();
                }
                _buffer = new ComputeBuffer(len, sizeof(float) * 16);
                _material.SetBuffer("Bones", _buffer);
                _material.SetPass(0);
                Graphics.DrawMeshNow(_mesh, Vector3.zero, Quaternion.identity);
                //Handles.color = BoneColor;

                //Transform[] children = Selection.activeTransform.GetComponentsInChildren<Transform>();

                //foreach (var node in _childNodes) {
                //    if (!node.transform.parent) continue;
                //    if (HideRoot && node == _preRootNode) continue;

                //    var start = node.transform.parent.position;
                //    var end = node.transform.position;

                //    if (Selection.activeGameObject == node.gameObject)
                //    {
                //        Handles.color = SelectedColor;
                //    }
                //    else if (children.Contains(node))
                //    {
                //        Handles.color = SelectedChildrenColor;
                //    }
                //    if (Handles.Button(node.transform.position, Quaternion.LookRotation(end-start), BoneGizmosSize, BoneGizmosSize, Handles.ConeHandleCap))
                //    {
                //        Selection.activeGameObject = node.gameObject;
                //    }
                //    Matrix4x4 matr = Handles.matrix;
                //    Handles.matrix = Matrix4x4.TRS(start + (end - start) / 2, Quaternion.LookRotation(end - start), Vector3.one);
                //    Handles.DrawWireCube(Vector3.zero, new Vector3(BoneGizmosSize, BoneGizmosSize, (end - start).magnitude));
                //    Handles.matrix = matr;

                //    if (HideRoot && node.parent == _preRootNode) continue;
                //    if (node.transform.parent.childCount == 1)
                //        Handles.DrawAAPolyLine(5f, start, end);
                //    else
                //        Handles.DrawDottedLine(start, end, 5f);

                //    Handles.color = BoneColor;
                //}
            }
        }

        public void PopulateChildren()
        {
            UnityEngine.Object[] skins = FindObjectsOfType(typeof (SkinnedMeshRenderer));
            _rootNode = null;
            foreach (var obj in skins)
            {
                SkinnedMeshRenderer skin = obj as SkinnedMeshRenderer;
                if (null == skin || null == skin.rootBone)
                {
                    continue;
                }
                if (_rootNode != null)
                {
                    if (_rootNode.IsChildOf(skin.rootBone))
                    {
                        _rootNode = skin.rootBone;
                    }
                }
                else
                {
                    _rootNode = skin.rootBone;
                }
            }
            _preRootNode = _rootNode;
            _childNodes = _rootNode.GetComponentsInChildren<Transform>();
            _previousTransforms = new List<BoneTransform>(_childNodes.Length);
            for (var i = 0; i < _childNodes.Length; i++)
            {
                var childNode = _childNodes[i];
                if (childNode.GetComponent<Renderer>() != null)
                {
                    continue;
                }
                _previousTransforms.Add(new BoneTransform(childNode, childNode.localPosition));
            }
        }

        //private void OnEnable()
        //{
        //    SceneView.onSceneGUIDelegate += OnScene;
        //}

        //private void OnDisable()
        //{
        //    // ReSharper disable once DelegateSubtraction
        //    SceneView.onSceneGUIDelegate -= OnScene;
        //}

        [Serializable]
        private struct BoneTransform
        {
            public Transform Target;
            public Vector3 LocalPosition;

            public BoneTransform(Transform target, Vector3 localPosition)
            {
                Target = target;
                LocalPosition = localPosition;
            }

            public void SetLocalPosition(Vector3 position)
            {
                LocalPosition = position;
            }
        }
    }
}
#endif