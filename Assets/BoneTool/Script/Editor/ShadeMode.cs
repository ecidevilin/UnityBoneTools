using System;
using System.Collections;
using System.Collections.Generic;
using BoneTool.Script.Runtime;
using UnityEditor;
using UnityEngine;


public class ShadeMode : Editor
{
    private static bool _active;
    private static ComputeShader _compute;
    private static List<BoneColorDrawerEditor> _drawers = new List<BoneColorDrawerEditor>();
    [MenuItem("Tools/BoneMode", true)]
    static bool ValidateSceneViewCustomSceneMode()
    {
        Menu.SetChecked("Tools/BoneMode", _active);
        return true;
    }

    [MenuItem("Tools/BoneMode")]
    static void BoneMode()
    {
        _active = !_active;
        if (_active)
        {
            SceneView view = SceneView.lastActiveSceneView;
            if (null != view)
            {
                Shader VertexColor = Shader.Find("Hidden/VertexColor");
                view.SetSceneViewShaderReplace(VertexColor, null);
            }
            if (null == _compute)
            {
                string path = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("BoneWeightColor")[0]);
                _compute = AssetDatabase.LoadMainAssetAtPath(path) as ComputeShader;
            }
            Selection.selectionChanged += SceneViewCustomSceneMode;
			#if UNITY_2018_1_OR_NEWER
            EditorApplication.quitting += SceneViewClearSceneView;
            #endif
            SceneViewCustomSceneMode();
            //BoneDrawer drawer;
            //while ((drawer = view.camera.gameObject.GetComponent<BoneDrawer>()) != null)
            //{
            //    GameObject.DestroyImmediate(drawer);
            //}
            //drawer = view.camera.gameObject.AddComponent<BoneDrawer>();
        }
        else
        {
            SceneViewClearSceneView();
            Selection.selectionChanged -= SceneViewCustomSceneMode;
            //SceneView view = SceneView.lastActiveSceneView;
            //if (null != view)
            //{
            //    GameObject.DestroyImmediate(view.camera.gameObject.GetComponent<BoneDrawer>());
            //}
        }
    }

    static void SceneViewCustomSceneMode()
    {
        SceneView view = SceneView.lastActiveSceneView;
        Transform selected = Selection.activeTransform;
        if (null != view || null != selected)
        {
            foreach (var drawer in _drawers)
            {
                drawer.Dispose();
            }
            _drawers.Clear();
            SkinnedMeshRenderer[] skins = Editor.FindObjectsOfType<SkinnedMeshRenderer>();

            for (int i = 0; i < skins.Length; i++)
            {
                BoneColorDrawerEditor drawer = new BoneColorDrawerEditor(skins[i], _compute);
                drawer.Draw(selected);
                _drawers.Add(drawer);
            }
            view.Repaint();
        }
    }
    //
    //[UnityEditor.Callbacks.DidReloadScripts]
    //static void DidReloadScripts()
    //{
    //    GC.Collect();
    //    Resources.UnloadUnusedAssets();
    //    ArrayList views = SceneView.sceneViews;
    //    SkinnedMeshRenderer[] skins = Editor.FindObjectsOfType<SkinnedMeshRenderer>();
    //    HashSet<string> pathSet = new HashSet<string>();
    //    foreach (var sr in skins)
    //    {
    //        pathSet.Add(AssetDatabase.GetAssetPath(sr.sharedMesh));
    //    }
    //    foreach (var path in pathSet)
    //    {

    //        AssetDatabase.ImportAsset(path);
    //    }
    //    foreach (var view in views)
    //    {
    //        (view as SceneView).SetSceneViewShaderReplace(null, null);
    //    }
    //}

    static void SceneViewClearSceneView()
    {
        GC.Collect();
        Resources.UnloadUnusedAssets();
        SceneView view = SceneView.lastActiveSceneView;
        if (null != view)
        {
            foreach (var drawer in _drawers)
            {
                drawer.Dispose();
            }
            _drawers.Clear();
            view.SetSceneViewShaderReplace(null, null);
            view.Repaint();
        }
    }
}


public class BoneColorDrawerEditor : IDisposable
{
    public const int THREADS_NUM = 8;

    private SkinnedMeshRenderer _skin;
    private Mesh _mesh;
    private Material _material;
    private Transform[] _bones;
    private ComputeBuffer _colorBuffer;
    private ComputeBuffer _bwBuffer;
    private ComputeShader _compute;
    private int _kernelBW;
    private int _kernelBC;

    public BoneColorDrawerEditor(SkinnedMeshRenderer skin, ComputeShader compute)
    {
        _skin = skin;
        _material = skin.sharedMaterial;
        _mesh = _skin.sharedMesh;
        _bones = _skin.bones;
        _bwBuffer = new ComputeBuffer(_mesh.vertexCount, sizeof(int) * 4 + sizeof(float) * 4, ComputeBufferType.Default);
        _bwBuffer.SetData(_mesh.boneWeights);
        _colorBuffer = new ComputeBuffer(_mesh.vertexCount, sizeof(float) * 4, ComputeBufferType.Default);
        _compute = compute;
        _kernelBC = _compute.FindKernel("BoneColors");
        _kernelBW = _compute.FindKernel("BoneWeights");
    }

    public void Draw(Transform selectedBoneTransform)
    {
        if (selectedBoneTransform == null)
        {
            return;
        }
        SkinnedMeshRenderer selectedSkin = selectedBoneTransform.GetComponent<SkinnedMeshRenderer>();
        int kernel;
        if (selectedSkin != null)
        {
            _compute.SetInt("selected", selectedSkin == _skin ? 1 : 0);
            kernel = _kernelBW;
        }
        else
        {
            int sel = Array.IndexOf(_bones, selectedBoneTransform);
            _compute.SetInt("selected", sel);
            kernel = _kernelBC;
        }
        _compute.SetBuffer(kernel, "boneWeights", _bwBuffer);
        _compute.SetBuffer(kernel, "colors", _colorBuffer);
        _compute.SetInt("total", _mesh.vertexCount);
        _compute.Dispatch(kernel, _mesh.vertexCount / THREADS_NUM + 1, 1, 1);
        _material.SetBuffer("boneColors", _colorBuffer);
    }

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        
        _bwBuffer.Dispose();
        _colorBuffer.Dispose();
        _skin = null;

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}