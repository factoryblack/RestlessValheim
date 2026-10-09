namespace RestlessQoL.Core
{
    public class Config<T> { public T Value; public Config(T value) { Value = value; } }
    public static class ModConfig
    {
        public static Config<bool> GlobalReachEnabled = new(false);
        public static Config<int> GlobalReachProfile = new(2);
        public static Config<float> StorageRange = new(50), WorkbenchRange = new(40), AreaRepairRadius = new(31), AreaSealRadius = new(17);
    }
}
namespace UnityEngine
{
    public readonly struct Vector3
    {
        public readonly float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public float sqrMagnitude => x*x+y*y+z*z;
        public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.x-b.x,a.y-b.y,a.z-b.z);
    }
}
