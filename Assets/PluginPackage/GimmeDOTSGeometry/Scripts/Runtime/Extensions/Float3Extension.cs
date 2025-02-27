using Unity.Mathematics;

namespace GimmeDOTSGeometry
{
    public static class Float3Extension
    {
        public static bool ApproximatelyEquals(this float3 vec, float3 other, float epsilon = 10e-5f)
        {
            return math.all(math.abs(vec - other) < epsilon);
        }

        public static float SelectComponent(this float3 vec, int component)
        {
            return vec[component];
        }

        public static float2 SelectComponents(this float3 vec, int component0, int component1)
        {
            return new float2(vec[component0], vec[component1]);
        }
    }
}
