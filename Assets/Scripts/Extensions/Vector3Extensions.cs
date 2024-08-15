using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Vector3Extensions
{
    public static Vector3 Average(this IEnumerable<Vector3> source)
    {
        var average = Vector3.zero;
        foreach (var vector in source) average += vector;
        average /= source.Count();

        return average;
    }
}