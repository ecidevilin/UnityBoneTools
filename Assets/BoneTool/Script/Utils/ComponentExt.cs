using UnityEngine;

namespace Chaos
{
    public static class ComponentExt
    {
        public static T GetOrAddComponent<T>(this Component self) where T : Component
        {
            T com = self.GetComponent<T>();
            if (null == com)
            {
                com = self.gameObject.AddComponent<T>();
            }
            return com;
        }

        public static void DestroyComponentImmediate<T>(this Component self) where T : Component
        {
            T com = self.GetComponent<T>();
            if (null != com)
            {
                Object.DestroyImmediate(com);
            }
        }
    }
}
