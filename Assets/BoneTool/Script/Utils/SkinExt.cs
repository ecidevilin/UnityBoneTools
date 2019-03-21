using UnityEngine;

namespace Chaos
{
    public static class SkinExt
    {
        public static Vector3 GetSkinnedVertexWS(this SkinnedMeshRenderer skin, int idx)
        {
            Mesh mesh = skin.sharedMesh;
            Transform[] bones = skin.bones;
            Matrix4x4[] bindPoses = mesh.bindposes;
            BoneWeight bw = mesh.boneWeights[idx];
            Vector4 v4 = mesh.vertices[idx];
            v4.w = 1;
            Matrix4x4 m0 = bones[bw.boneIndex0].localToWorldMatrix * bindPoses[bw.boneIndex0];
            Matrix4x4 m1 = bones[bw.boneIndex1].localToWorldMatrix * bindPoses[bw.boneIndex1];
            Matrix4x4 m2 = bones[bw.boneIndex2].localToWorldMatrix * bindPoses[bw.boneIndex2];
            Matrix4x4 m3 = bones[bw.boneIndex3].localToWorldMatrix * bindPoses[bw.boneIndex3];
            Vector3 ret = m0 * v4 * bw.weight0 + m1 * v4 * bw.weight1 + m2 * v4 * bw.weight2 + m3 * v4 * bw.weight3;
            return ret;
        }
        public static Vector3 GetSkinnedVertexLS(this SkinnedMeshRenderer skin, int idx)
        {
            Matrix4x4 world2Model = skin.worldToLocalMatrix;
            Vector3 ret = GetSkinnedVertexWS(skin, idx);
            return world2Model.MultiplyPoint3x4(ret);
        }
    }
}
