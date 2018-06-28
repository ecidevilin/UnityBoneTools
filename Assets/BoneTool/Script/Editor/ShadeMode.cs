using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ShadeMode : Editor
{
    private static bool _active = false;
    private static bool _selected = true;
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
            SceneViewCustomSceneMode();
            SceneView view = SceneView.lastActiveSceneView;
            if (null != view)
            {
                Shader VertexColor = Shader.Find("Hidden/VertexColor");
                view.SetSceneViewShaderReplace(VertexColor, null);
            }
            Selection.selectionChanged += SceneViewCustomSceneMode;
        }
        else
        {
            SceneViewClearSceneView();
            Selection.selectionChanged -= SceneViewCustomSceneMode;
        }
    }

    static void SceneViewCustomSceneMode()
    {
        SceneView view = SceneView.lastActiveSceneView;
        if (null != view)
        {
            Transform selected = Selection.activeTransform;
            SkinnedMeshRenderer[] skins = FindObjectsOfType<SkinnedMeshRenderer>();
            List<SkinnedMeshRenderer> affectedSkins = new List<SkinnedMeshRenderer>();
            List<int> boneIndices = new List<int>();
            if (selected != null)
            {

                foreach (var sr in skins)
                {
                    Transform[] bones = sr.bones;
                    int idx = ArrayUtility.IndexOf(bones, selected);
                    if (idx >= 0)
                    {
                        affectedSkins.Add(sr);
                        boneIndices.Add(idx);
                    }
                }
            }
            if (affectedSkins.Count > 0)
            {
                foreach (var sr in skins)
                {
                    int idx = affectedSkins.IndexOf(sr);
                    int bone = -1;
                    if (idx >= 0)
                    {
                        bone = boneIndices[idx];
                    }
                    Mesh mesh = (sr.sharedMesh);
                    List<Color> colors = new List<Color>(mesh.vertexCount);
                    for (int j = 0, jmax = mesh.vertexCount; j < jmax; j++)
                    {
                        Color col = new Color(0,0,0,0);
                        if (mesh.boneWeights.Length > 0 && bone >= 0)
                        {
                            BoneWeight bws = mesh.boneWeights[j];
                            if (bws.boneIndex0 == bone)
                            {
                                col = bws.weight0 * Color.red;
                            }
                            else if (bws.boneIndex1 == bone)
                            {
                                col = bws.weight1 * Color.green;
                            }
                            else if (bws.boneIndex2 == bone)
                            {
                                col = bws.weight2 * Color.blue;
                            }
                            else if (bws.boneIndex3 == bone)
                            {
                                col = bws.weight3 * Color.yellow;
                            }
                        }
                        colors.Add(col);
                    }
                    mesh.SetColors(colors);
                    mesh.UploadMeshData(false);
                }
            }
            else if (_selected)
            {
                foreach (var sr in skins)
                {
                    Mesh mesh = (sr.sharedMesh);
                    if (mesh.boneWeights.Length == 0)
                    {
                        continue;
                    }
                    List<Vector4> uv2 = new List<Vector4>(mesh.vertexCount);
                    List<Color> colors = new List<Color>(mesh.vertexCount);
                    for (int i = 0, imax = mesh.vertexCount; i < imax; i++)
                    {
                        uv2.Add(new Vector4(mesh.boneWeights[i].boneIndex0, mesh.boneWeights[i].boneIndex1, mesh.boneWeights[i].boneIndex2, mesh.boneWeights[i].boneIndex3));
                        colors.Add(new Color(mesh.boneWeights[i].weight0, mesh.boneWeights[i].weight1, mesh.boneWeights[i].weight2, mesh.boneWeights[i].weight3));
                    }
                    mesh.SetUVs(2, uv2);
                    mesh.SetColors(colors);
                    mesh.UploadMeshData(false);
                }
            }
            _selected = affectedSkins.Count > 0;
        }
    }

    static void SceneViewClearSceneView()

    {
        GC.Collect();
        Resources.UnloadUnusedAssets();
        SceneView view = SceneView.lastActiveSceneView;
        if (null != view)
        {
            SkinnedMeshRenderer[] skins = FindObjectsOfType<SkinnedMeshRenderer>();
            HashSet<string> pathSet = new HashSet<string>();
            foreach (var sr in skins)
            {
                pathSet.Add(AssetDatabase.GetAssetPath(sr.sharedMesh));
            }
            foreach (var path in pathSet)
            {

                AssetDatabase.ImportAsset(path);
            }
                view.SetSceneViewShaderReplace(null, null);
        }

    }
}
