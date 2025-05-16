using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public class BezierCurveHelper : MonoBehaviour
{
    public static float3[] PointsToCurve(float3[] points, float curveIntensity = 0.3f)
    {
        if (points is null)
        {
            Debug.LogError($"points given is null");
            return null;
        }
        
        points = points.Distinct().ToArray();
        List<float3> bezierPoints = new List<float3>();

        if (points.Length == 1)
        {
            bezierPoints.Add(points[0]);
            bezierPoints.Add(points[0]);
            bezierPoints.Add(points[0]);
            bezierPoints.Add(points[0]);
            return bezierPoints.ToArray();
        }

        if (points.Length == 2)
        {
            bezierPoints.Add(points[0]);
            bezierPoints.Add(points[0]);
            bezierPoints.Add(points[1]);
            bezierPoints.Add(points[1]);
            return bezierPoints.ToArray();
        }
        
        if (points.Length == 3)
        {
            {
                float3 posA = points[0];
                float3 posB = points[1];
                float3 posC = points[2];
        
                float3 dirAB = math.normalize(posB - posA);
                float3 dirBC = math.normalize(posC - posB);
        
                float3 posE = posB + -dirAB * curveIntensity + -dirBC * curveIntensity;
        
                bezierPoints.Add(posA);
                bezierPoints.Add(posA);
                bezierPoints.Add(posE);
                bezierPoints.Add(posB);
            }
            
            {
                float3 posB = points[^3];
                float3 posC = points[^2];
                float3 posD = points[^1];
        
                float3 dirBC = math.normalize(posC - posB);
                float3 dirCD = math.normalize(posD - posC);
        
                float3 posF = posC + dirCD * curveIntensity + dirBC * curveIntensity;
        
                bezierPoints.Add(posC);
                bezierPoints.Add(posF);
                bezierPoints.Add(posD);
                bezierPoints.Add(posD);
            }
            
            return bezierPoints.ToArray();
        }
            
        {
            float3 posA = points[0];
            float3 posB = points[1];
            float3 posC = points[2];
        
            float3 dirAB = math.normalize(posB - posA);
            float3 dirBC = math.normalize(posC - posB);
        
            float3 posE = posB + -dirAB * curveIntensity + -dirBC * curveIntensity;
        
            bezierPoints.Add(posA);
            bezierPoints.Add(posA);
            bezierPoints.Add(posE);
            bezierPoints.Add(posB);
        }
        
        for (int i = 1; i < points.Length - 2; i++)
        {
            float3 posA = points[i - 1];
            float3 posB = points[i];
            float3 posC = points[i + 1];
            float3 posD = points[i + 2];
        
            float3 dirAB = math.normalize(posB - posA);
            float3 dirBC = math.normalize(posC - posB);
            float3 dirCD = math.normalize(posD - posC);
        
            float3 posE = posB + dirAB * curveIntensity + dirBC * curveIntensity;
            float3 posF = posC + -dirCD * curveIntensity + -dirBC * curveIntensity;
        
            bezierPoints.Add(posB);
            bezierPoints.Add(posE);
            bezierPoints.Add(posF);
            bezierPoints.Add(posC);
        }
        
        {
            float3 posB = points[^3];
            float3 posC = points[^2];
            float3 posD = points[^1];
        
            float3 dirBC = math.normalize(posC - posB);
            float3 dirCD = math.normalize(posD - posC);
        
            float3 posF = posC + dirCD * curveIntensity + dirBC * curveIntensity;
        
            bezierPoints.Add(posC);
            bezierPoints.Add(posF);
            bezierPoints.Add(posD);
            bezierPoints.Add(posD);
        }

        return bezierPoints.ToArray();
    }
    
    public static float3[] PointsToCurve(Vector3[] points, float curveIntensity = 0.3f)
    {
        if (points is null)
        {
            Debug.LogError($"points given is null");
            return null;
        }
        
        points = points.Distinct().ToArray();
        List<float3> bezierPoints = new List<float3>();

        if (points.Length == 1)
        {
            bezierPoints.Add(points[0]);
            bezierPoints.Add(points[0]);
            bezierPoints.Add(points[0]);
            bezierPoints.Add(points[0]);
            return bezierPoints.ToArray();
        }

        if (points.Length == 2)
        {
            bezierPoints.Add(points[0]);
            bezierPoints.Add(points[0]);
            bezierPoints.Add(points[1]);
            bezierPoints.Add(points[1]);
            return bezierPoints.ToArray();
        }
        
        if (points.Length == 3)
        {
            {
                float3 posA = points[0];
                float3 posB = points[1];
                float3 posC = points[2];
        
                float3 dirAB = math.normalize(posB - posA);
                float3 dirBC = math.normalize(posC - posB);
        
                float3 posE = posB + -dirAB * curveIntensity + -dirBC * curveIntensity;
        
                bezierPoints.Add(posA);
                bezierPoints.Add(posA);
                bezierPoints.Add(posE);
                bezierPoints.Add(posB);
            }
            
            {
                float3 posB = points[^3];
                float3 posC = points[^2];
                float3 posD = points[^1];
        
                float3 dirBC = math.normalize(posC - posB);
                float3 dirCD = math.normalize(posD - posC);
        
                float3 posF = posC + dirCD * curveIntensity + dirBC * curveIntensity;
        
                bezierPoints.Add(posC);
                bezierPoints.Add(posF);
                bezierPoints.Add(posD);
                bezierPoints.Add(posD);
            }
            
            return bezierPoints.ToArray();
        }
            
        {
            float3 posA = points[0];
            float3 posB = points[1];
            float3 posC = points[2];
        
            float3 dirAB = math.normalize(posB - posA);
            float3 dirBC = math.normalize(posC - posB);
        
            float3 posE = posB + -dirAB * curveIntensity + -dirBC * curveIntensity;
        
            bezierPoints.Add(posA);
            bezierPoints.Add(posA);
            bezierPoints.Add(posE);
            bezierPoints.Add(posB);
        }
        
        for (int i = 1; i < points.Length - 2; i++)
        {
            float3 posA = points[i - 1];
            float3 posB = points[i];
            float3 posC = points[i + 1];
            float3 posD = points[i + 2];
        
            float3 dirAB = math.normalize(posB - posA);
            float3 dirBC = math.normalize(posC - posB);
            float3 dirCD = math.normalize(posD - posC);
        
            float3 posE = posB + dirAB * curveIntensity + dirBC * curveIntensity;
            float3 posF = posC + -dirCD * curveIntensity + -dirBC * curveIntensity;
        
            bezierPoints.Add(posB);
            bezierPoints.Add(posE);
            bezierPoints.Add(posF);
            bezierPoints.Add(posC);
        }
        
        {
            float3 posB = points[^3];
            float3 posC = points[^2];
            float3 posD = points[^1];
        
            float3 dirBC = math.normalize(posC - posB);
            float3 dirCD = math.normalize(posD - posC);
        
            float3 posF = posC + dirCD * curveIntensity + dirBC * curveIntensity;
        
            bezierPoints.Add(posC);
            bezierPoints.Add(posF);
            bezierPoints.Add(posD);
            bezierPoints.Add(posD);
        }

        return bezierPoints.ToArray();
    }
    
    public static float3 GetPoint(float3 p0, float3 p1, float3 p2, float3 p3, float t)
    {
        t = Mathf.Clamp01(t);
        float oneMinusT = 1f - t;
        return
            oneMinusT * oneMinusT * oneMinusT * p0 +
            3f * oneMinusT * oneMinusT * t * p1 +
            3f * oneMinusT * t * t * p2 +
            t * t * t * p3;
    }

    //call this to get the accurate t for a given bezier curve
    public static float GetApproximatedTime(List<(float ArcTime, float ArcLength)> arcTimeLengthMap, float arcLength, float u)
    {
        float target = u * arcLength;
        int low = 0;
        int high = 1;
        float min = 0.0f;
        float max = 0.0f;

        for (int i = 1; i < arcTimeLengthMap.Count; i++)
        {
            max = arcTimeLengthMap[i].ArcLength;
            if (target > min && target <= max)
            {
                high = i;
                low = i - 1;
                break;
            }

            min = max;
        }

        float p = (target - min) / (max - min);
        float lowTime = arcTimeLengthMap[low].ArcTime;
        float highTime = arcTimeLengthMap[high].ArcTime;
        float lowHighDelta = highTime - lowTime;
        return arcTimeLengthMap[low].ArcTime + (lowHighDelta * p);
    }

    //Call this once for every bezier segment
    public static (List<(float ArcTime, float ArcLength)>, float) GetArcTimeLengthMap(float3 p0, float3 p1, float3 p2, float3 p3)
    {
        int resolution = 100;
        float ratio = 1.0f / resolution;
        float arcLength = 0.0f;
        float3 p0Current = GetPoint(p0, p1, p2, p3, 0.0f);
        List<(float ArcTime, float ArcLength)> arcTimeLengthMap = new List<(float, float)>();
        arcTimeLengthMap.Add((0.0f, 0.0f));

        for (int i = 1; i <= resolution; i++)
        {
            float t = i * ratio;
            float3 p1Current = GetPoint(p0, p1, p2, p3, t);
            arcLength += math.distance(p0Current, p1Current);
            arcTimeLengthMap.Add((t, arcLength));
            p0Current = p1Current;
        }

        return (arcTimeLengthMap, arcLength);
    }
}
