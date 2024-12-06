using UnityEngine;

namespace CheesyFX
{
    public struct Vector5
    {
        public float x, y, z, q, p;

        public static Vector5 zero { get; } = new Vector5(0.0f, 0.0f, 0.0f, 0.0f, 0.0f);

        public Vector5(float x, float y, float z, float q, float p)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.p = p;
            this.q = q;
        }
        
        public Vector5(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.p = 0f;
            this.q = 0f;
        }
        
        public Vector5(float x, float y, float z, float p)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.p = p;
            this.q = 0f;
        }
        
        public static float SqrMagnitude(Vector5 vector) => (float) ((double) vector.x * (double) vector.x + (double) vector.y * (double) vector.y + (double) vector.z * (double) vector.z + (double) vector.p * (double) vector.p+(double) vector.q * (double) vector.q);
        public float sqrMagnitude => (float) ((double) this.x * (double) this.x + (double) this.y * (double) this.y + (double) this.z * (double) this.z+(double)this.p*(double)this.p+(double)this.q*(double)this.q);
        public float magnitude => Mathf.Sqrt((float) ((double) this.x * (double) this.x + (double) this.y * (double) this.y + (double) this.z * (double) this.z + (double) this.p * (double) this.p + (double) this.q * (double) this.q));
        
        public static Vector5 Lerp(Vector5 a, Vector5 b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Vector5(
                a.x + (b.x - a.x) * t,
                a.y + (b.y - a.y) * t,
                a.z + (b.z - a.z) * t,
                a.p + (b.p - a.p) * t,
                a.q + (b.q - a.q) * t
                );
        }
        
        // public float this[int index]
        // {
        //     get
        //     {
        //         switch (index)
        //         {
        //             case 0:
        //                 return this.x;
        //             case 1:
        //                 return this.y;
        //             case 2:
        //                 return this.z;
        //             case 3:
        //                 return this.p;
        //             case 4:
        //                 return this.q;
        //             default:
        //                 return 0f;
        //         }
        //     }
        //     set
        //     {
        //         switch (index)
        //         {
        //             case 0:
        //                 this.x = value;
        //                 break;
        //             case 1:
        //                 this.y = value;
        //                 break;
        //             case 2:
        //                 this.z = value;
        //                 break;
        //             case 3:
        //                 this.p = value;
        //                 break;
        //             case 4:
        //                 this.q = value;
        //                 break;
        //             default:
        //                 return;
        //         }
        //     }
        // }

        public Vector5 Multiply(Vector5 a) => new Vector5(x * a.x, y * a.y, z * a.z, p * a.p, q * a.q);
        public Vector5 Multiply(float a1, float a2, float a3) => new Vector5(x * a1, y * a2, z * a3, p, q);
        public Vector5 MultiplyFirst3(float a) => new Vector5(x * a, y * a, z * a, p, q);
        public Vector5 Multiply(float a1, float a2, float a3, float a4, float a5) => new Vector5(x * a1, y * a2, z * a3, p*a4, q*a5);

        public static Vector5 operator +(Vector5 a, Vector5 b) => new Vector5(a.x + b.x, a.y + b.y, a.z + b.z, a.p + b.p, a.q + b.q);

        public static Vector5 operator -(Vector5 a, Vector5 b) => new Vector5(a.x - b.x, a.y - b.y, a.z - b.z, a.p-b.p, a.q-b.q);

        public static Vector5 operator -(Vector5 a) => new Vector5(-a.x, -a.y, -a.z, -a.p, -a.q);

        public static Vector5 operator *(Vector5 a, float d) => new Vector5(a.x * d, a.y * d, a.z * d, a.p*d, a.q*d);

        public static Vector5 operator *(float d, Vector5 a) => new Vector5(a.x * d, a.y * d, a.z * d, a.p*d, a.q*d);

        public static Vector5 operator /(Vector5 a, float d) => new Vector5(a.x / d, a.y / d, a.z / d, a.p/d, a.q/d);

        public static bool operator ==(Vector5 lhs, Vector5 rhs) => (double) Vector5.SqrMagnitude(lhs - rhs) < 9.999999439624929E-11;

        public static bool operator !=(Vector5 lhs, Vector5 rhs) => !(lhs == rhs);

        public override string ToString() => $"[{x:0.00}, {y:0.00}, {z:0.00}, {p:0.00}, {q:0.00}]";
    }
}