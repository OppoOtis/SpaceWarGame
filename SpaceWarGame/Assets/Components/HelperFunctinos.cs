using UnityEngine;

public class HelperFunctinos
{
    public static bool GetRayYPlaneIntersection(Ray ray, out Vector3 hitPos)
    {
        hitPos = Vector3.zero;
        // Prevent division by zero (ray is parallel to the plane)
        if (Mathf.Abs(ray.direction.y) < 0.0001f)
            return false;

        // t is the scalar for how far along the ray the intersection happens
        float t = -ray.origin.y / ray.direction.y;

        // If t < 0, the intersection is behind the ray origin
        if (t < 0)
            return false;

        // Get the intersection point
        hitPos = ray.origin + ray.direction * t;
        return true;
    }
    
    public static bool GetRayYPlaneIntersection(Vector3 rayOrigin, Vector3 rayDirection, out Vector3 hitPos)
    {
        hitPos = Vector3.zero;
        // Prevent division by zero (ray is parallel to the plane)
        if (Mathf.Abs(rayDirection.y) < 0.0001f)
            return false;

        // t is the scalar for how far along the ray the intersection happens
        float t = -rayOrigin.y / rayDirection.y;

        // If t < 0, the intersection is behind the ray origin
        if (t < 0)
            return false;

        // Get the intersection point
        hitPos = rayOrigin + rayDirection * t;
        return true;
    }
    
    public static Vector3 CubicBezier(Vector3 startPos, Vector3 endPos, Vector3 startTangent, Vector3 endTangent, float t)
    {
        // Clamp t between 0 and 1
        t = Mathf.Clamp01(t);

        // Bézier control points
        Vector3 p0 = startPos;
        Vector3 p1 = startTangent;
        Vector3 p2 = endTangent;
        Vector3 p3 = endPos;

        // Interpolation using the cubic Bézier formula
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector3 result =
            uuu * p0 +
            3 * uu * t * p1 +
            3 * u * tt * p2 +
            ttt * p3;

        return result;
    }
}
