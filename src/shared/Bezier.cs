using UnityEngine;

namespace CheesyFX
{
    public class Bezier
    {
        public Vector3 p0;
        public Vector3 p1;
        public Vector3 p2;

        public Vector3 Evaluate(float t)
        {
            var a = 1 - t;
            return a * a * p0 + 2 * t * a * p1 + t * t * p2;
        }
    }
}