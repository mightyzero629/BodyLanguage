using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CheesyFX
{
    public static class Debug
    {
        private static List<DebugObject> debugObjects = new List<DebugObject>();
        public static DebugVector vector;
        
        public static void Draw(this Transform t)
        {
            debugObjects.Add(new DebugLine(t,0));
            debugObjects.Add(new DebugLine(t,1));
            debugObjects.Add(new DebugLine(t,2));
        }

        public static void SetDebugWidth(this Transform t, float multiplier)
        {
            var lr = t.Find("BodyLanguage.DebugLine0")?.GetComponent<LineRenderer>();
            t.PrintHierarchy();
            lr.NullCheck();
            if(lr == null) return;
            lr.widthMultiplier = 0.003f * multiplier;
            lr = t.Find("BodyLanguage.DebugLine1").GetComponent<LineRenderer>();
            lr.widthMultiplier = 0.003f * multiplier;
            lr = t.Find("BodyLanguage.DebugLine2").GetComponent<LineRenderer>();
            lr.widthMultiplier = 0.003f * multiplier;
            multiplier.Print();
        }
        
        public static DebugVector Vector(this Transform t)
        {
            var debugVector = new DebugVector(t);
            debugObjects.Add(debugVector);
            return debugVector;
        }
        
        public static DebugVector Vector(this Transform t, Vector3 tip)
        {
            if (t == null) return null;
            if (vector == null)
            {
                vector = new DebugVector(t);
            }
            vector.SetTip(tip);
            debugObjects.Add(vector);
            return vector;
        }

        public static void Clear()
        {
            foreach (var debugObject in debugObjects)
            {
                debugObject.Destroy();
            }
            debugObjects.Clear();
        }
         
    }

    public class DebugObject
    {
        protected GameObject go;
        protected Material material = new Material(Shader.Find("Battlehub/RTGizmos/Handles"));
        
        public void Destroy()
        {
            Object.Destroy(go);
        }
    }

    public class DebugVector : DebugObject
    {
        private Transform root;
        private LineRenderer lineRenderer;
        private Vector3[] points = new Vector3[2];
        
        public DebugVector(Transform root)
        {
            this.root = root;
            go = new GameObject("DebugVector");
            go.transform.SetParent(root, false);
            lineRenderer = go.AddComponent<LineRenderer>();
            lineRenderer.material = material;
            material.color = Color.cyan;
            lineRenderer.widthMultiplier = 0.002f;
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;
            // lineRenderer.SetPositions(new []{t.position, t.position + t.up*.1f});
            // lineRenderer.SetPositions(new []{root.position, Vector3.zero});2.Print();
        }

        public void SetTip(Vector3 tip)
        {
            try
            {
                points[0] = root.position;
                points[1] = tip;
                lineRenderer.SetPositions(points);
            }
            catch (Exception e)
            {
                SuperController.LogError(e.ToString());
            }
        }
    }

    public class DebugLine : DebugObject
    {
        private LineRenderer lineRenderer;
        private Vector3 direction;
        private float length;

        public DebugLine(Transform t, Vector3 direction)
        {
            this.direction = direction;
            length = .1f / Vector3.Dot(t.lossyScale, direction);
            go = new GameObject();
            go.transform.SetParent(t, false);
            lineRenderer = go.AddComponent<LineRenderer>();
            lineRenderer.material = material;
            lineRenderer.widthMultiplier = 0.003f;
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = false;
            // lineRenderer.SetPositions(new []{t.position, t.position + t.up*.1f});
            lineRenderer.SetPositions(new []{Vector3.zero, this.direction*length});
            material.color = Color.cyan;
        }
        
        public DebugLine(Transform t, int axis)
        {
            switch (axis)
            {
                case 0:
                {
                    direction = Vector3.right;
                    material.color = Color.red;
                    break;
                }
                case 1:
                {
                    direction = Vector3.up;
                    material.color = Color.green;
                    break;
                }
                case 2:
                {
                    direction = Vector3.forward;
                    material.color = Color.blue;
                    break;
                }
            }
            length = .1f / t.lossyScale[axis];
            go = new GameObject($"BodyLanguage.DebugLine{axis}");
            go.transform.SetParent(t, false);
            lineRenderer = go.AddComponent<LineRenderer>();
            lineRenderer.material = material;
            lineRenderer.widthMultiplier = 0.003f;
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = false;
            // lineRenderer.SetPositions(new []{t.position, t.position + t.up*.1f});
            lineRenderer.SetPositions(new []{Vector3.zero, direction*length});
        }
    }
}