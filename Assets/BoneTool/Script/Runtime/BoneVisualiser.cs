#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BoneTool.Script.Runtime
{
    [ExecuteInEditMode]
    public class BoneVisualiser : MonoBehaviour
    {
        public SkinnedMeshRenderer Renderer;
        public float BoneGizmosSize = 0.01f;
        public Color BoneColor = Color.white;
        public bool HideRoot;
        public bool EnableConstraint = true;

        private Transform _preRootNode;
        private Transform[] _childNodes;
        private BoneTransform[] _previousTransforms;
        private Transform _rootNode;

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
                if (_childNodes == null || _childNodes.Length == 0 || _previousTransforms == null || _previousTransforms.Length == 0)
                    PopulateChildren();

                Handles.color = BoneColor;

                Transform[] children = Selection.activeTransform.GetComponentsInChildren<Transform>();

                foreach (var node in _childNodes) {
                    if (!node.transform.parent) continue;
                    if (HideRoot && node == _preRootNode) continue;

                    var start = node.transform.parent.position;
                    var end = node.transform.position;

                    if (Selection.activeGameObject == node.gameObject)
                    {
                        Handles.color = Color.magenta;
                    }
                    else if (children.Contains(node))
                    {
                        Handles.color = Color.red;
                    }
                    if (Handles.Button(node.transform.position, Quaternion.identity, BoneGizmosSize, BoneGizmosSize, Handles.SphereHandleCap)) {
                        Selection.activeGameObject = node.gameObject;
                    }
                    Handles.color = BoneColor;

                    if (HideRoot && node.parent == _preRootNode) continue;

                    if (node.transform.parent.childCount == 1)
                        Handles.DrawAAPolyLine(5f, start, end);
                    else
                        Handles.DrawDottedLine(start, end, 0.5f);
                }
            }
        }

        public void PopulateChildren() {
            if (!Renderer) return;
            _rootNode = Renderer.rootBone;
            _preRootNode = _rootNode;
            _childNodes = Renderer.bones;//_rootNode.GetComponentsInChildren<Transform>();
            _previousTransforms = new BoneTransform[_childNodes.Length];
            for (var i = 0; i < _childNodes.Length; i++) {
                var childNode = _childNodes[i];
                _previousTransforms[i] = new BoneTransform(childNode, childNode.localPosition);
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