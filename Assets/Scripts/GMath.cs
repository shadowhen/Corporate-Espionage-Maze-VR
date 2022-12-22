using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GMath
{
    public static Vector2 ProjectileDisplacement(float v0, float t, float angle)
    {
        float x = v0 * t * Mathf.Cos(angle);
        float y = v0 * t * Mathf.Sin(angle) - 0.5f * -Physics.gravity.y * Mathf.Pow(t, 2);
        return new Vector2(x, y);
    }

    public static void CalculateProjectilePath(Vector3 targetPosition, float height, out float v0, out float angle, out float time)
    {
        float xt = targetPosition.x;
        float yt = targetPosition.y;
        float gravity = -Physics.gravity.y;

        float a = (-0.5f * gravity);
        float b = Mathf.Sqrt(2 * gravity * height);
        float c = -yt;

        float tPlus = QuadraticEquation(a, b, c, 1);
        float tMinus = QuadraticEquation(a, b, c, -1);

        time = tPlus > tMinus ? tPlus : tMinus;
        angle = Mathf.Atan(b * time / xt);
        v0 = b / Mathf.Sin(angle);
    }

    public static float QuadraticEquation(float a, float b, float c, float sign)
    {
        return (-b + sign * Mathf.Sqrt(b * b - 4 * a * c)) / (2 * a);
    }
}
