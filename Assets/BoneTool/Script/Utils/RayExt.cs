using UnityEngine;

namespace Chaos
{
    public static class RayExt
    {

        public static bool CheckIntersect(this Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, out Vector3 ip, ref float maxDist)
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
                    maxDist = x;
                    return true;
                }
                Vector3 np01 = Vector3.Cross(p0, p1).normalized;
                Vector3 np12 = Vector3.Cross(p1, p2).normalized;
                Vector3 np20 = Vector3.Cross(p2, p0).normalized;
                if (Mathf.Approximately(1, Vector3.Dot(np01, np12)) &&
                    Mathf.Approximately(1, Vector3.Dot(np12, np20)))
                {
                    maxDist = x;
                    return true;
                }
            }
            return false;
        }
    }
}
