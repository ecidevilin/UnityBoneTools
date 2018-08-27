#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BoneTool.Script.Runtime
{
    [ExecuteInEditMode]
    public class BoneVisualiser : MonoBehaviour
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

        public Transform[] GetChildNodes() {
            return _childNodes;
        }

        private void Update() {
            if (EnableConstraint && _previousTransforms != null) {
                foreach (var boneTransform in _previousTransforms) {
                    if (boneTransform.Target && boneTransform.Target.hasChanged) {
                        if (boneTransform.Target.parent.childCount == 1) {
                            boneTransform.Target.localPosition = boneTransform.LocalPosition;
                        }
                        else {
                            boneTransform.SetLocalPosition(boneTransform.Target.localPosition);
                        }
                    }
                }
            }
        }

        private void OnScene(SceneView sceneview) {
            if (_rootNode != null && Selection.activeTransform != null) {
                if (_childNodes == null || _childNodes.Length == 0 || _previousTransforms == null || _previousTransforms.Count == 0)
                    PopulateChildren();
                Handles.color = BoneColor;

                Transform[] children = Selection.activeTransform.GetComponentsInChildren<Transform>();

                foreach (var node in _childNodes)
                {
                    if (!node.transform.parent) continue;
                    if (HideRoot && node == _preRootNode) continue;

                    var start = node.transform.parent.position;
                    var end = node.transform.position;

                    if (Selection.activeGameObject == node.gameObject)
                    {
                        Handles.color = SelectedColor;
                    }
                    else if (children.Contains(node))
                    {
                        Handles.color = SelectedChildrenColor;
                    }
                    if (Handles.Button(node.transform.position, Quaternion.LookRotation(end - start), BoneGizmosSize, BoneGizmosSize, Handles.ConeHandleCap))
                    {
                        Selection.activeGameObject = node.gameObject;
                    }
                    Matrix4x4 matr = Handles.matrix;
                    Handles.matrix = Matrix4x4.TRS(start + (end - start) / 2, Quaternion.LookRotation(end - start), Vector3.one);
                    Handles.DrawWireCube(Vector3.zero, new Vector3(BoneGizmosSize, BoneGizmosSize, (end - start).magnitude));
                    Handles.matrix = matr;

                    if (HideRoot && node.parent == _preRootNode) continue;
                    if (node.transform.parent.childCount == 1)
                        Handles.DrawAAPolyLine(5f, start, end);
                    else
                        Handles.DrawDottedLine(start, end, 5f);

                    Handles.color = BoneColor;
                }
            }
        }

        public void PopulateChildren()
        {
            SkinnedMeshRenderer[] skins = GetComponentsInChildren<SkinnedMeshRenderer>();
            _rootNode = null;
            foreach (var skin in skins)
            {
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
            for (var i = 0; i < _childNodes.Length; i++) {
                var childNode = _childNodes[i];
                if (childNode.GetComponent<Renderer>() != null)
                {
                    continue;
                }
                _previousTransforms.Add(new BoneTransform(childNode, childNode.localPosition));
            }
        }

        private void OnEnable() {
            SceneView.onSceneGUIDelegate += OnScene;
        }

        private void OnDisable() {
            // ReSharper disable once DelegateSubtraction
            SceneView.onSceneGUIDelegate -= OnScene;
        }

        [Serializable]
        private struct BoneTransform
        {
            public Transform Target;
            public Vector3 LocalPosition;

            public BoneTransform(Transform target, Vector3 localPosition) {
                Target = target;
                LocalPosition = localPosition;
            }

            public void SetLocalPosition(Vector3 position) {
                LocalPosition = position;
            }
        }
    }
}
#endif