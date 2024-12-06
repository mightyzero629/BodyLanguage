using System.Collections;
using MVR.FileManagementSecure;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CheesyFX {
    public class CheesyUtils{
        public static string GetLatestPackageUid(string name){
            try
            {
                int version = FileManagerSecure.GetPackageVersion(name+".latest");
                if (version == -1) return null;
                return $"{name}.{version}";
            }
            catch
            {
                int version = -1;
                for (int i = 0; i < 50; i++)
                {
                    if (FileManagerSecure.PackageExists($"{name}.{i}")) version = i;
                }
                if (version == -1) return null;
                return $"{name}.{version}";
            }
        }

        public static void DrawDebugLine(Transform parent, Vector3 direction, float duration = 5f)
        {
            GameObject myLine = new GameObject();
            var position = parent.position;
            myLine.transform.position = position;
            myLine.transform.parent = parent;
            myLine.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
            LineRenderer lr = myLine.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            // LineRenderer lr = myLine.GetComponent<LineRenderer>();
            // lr.material = new Material(Shader.Find("Particles/Alpha Blended Premultiply"));
            // Color color = new Color(1f, 0f, 0f);
            lr.startColor = Color.red;
            lr.startWidth = 0.002f;
            lr.SetPosition(0, position);
            lr.SetPosition(1, position+direction);
			
            Object.Destroy(myLine, duration);
        }

        public static JSONStorableFloat SetupJSONFloat(string name, float defaultVal, float minVal, float maxVal, bool constrain = false, bool interactable = true, bool register = true)
        {
            JSONStorableFloat JSONFloat = new JSONStorableFloat(name, defaultVal, minVal, maxVal, constrain, interactable);
            if (register) UIManager.script.RegisterFloat(JSONFloat);
            return JSONFloat;
        }
        
        public static JSONStorableFloat SetupJSONFloat(string name, float defaultVal, JSONStorableFloat.SetJSONFloatCallback callback, float minVal, float maxVal, bool constrain = false, bool interactable = true, bool register = true)
        {
            JSONStorableFloat JSONFloat = new JSONStorableFloat(name, defaultVal, callback, minVal, maxVal, constrain, interactable);
            if (register) UIManager.script.RegisterFloat(JSONFloat);
            return JSONFloat;
        }
        
        public static JSONStorableFloat SetupJSONFloat(string name, float defaultVal, JSONStorableFloat.SetFloatCallback callback, float minVal, float maxVal, bool constrain = false, bool interactable = true, bool register = true)
        {
            JSONStorableFloat JSONFloat = new JSONStorableFloat(name, defaultVal, callback, minVal, maxVal, constrain, interactable);
            if (register) UIManager.script.RegisterFloat(JSONFloat);
            return JSONFloat;
        }

        public static JSONStorableBool SetupJSONBool(string name, bool defaultVal, bool register=true)
        {
            JSONStorableBool JSONBool = new JSONStorableBool(name, defaultVal);
            if(register) UIManager.script.RegisterBool(JSONBool);
            return JSONBool;
        }
        
        public static JSONStorableBool SetupJSONBool(string name, bool defaultVal, JSONStorableBool.SetJSONBoolCallback callback, bool register=true)
        {
            JSONStorableBool JSONBool = new JSONStorableBool(name, defaultVal, callback);
            if(register) UIManager.script.RegisterBool(JSONBool);
            return JSONBool;
        }
        
        public static JSONStorableBool SetupJSONBool(string name, bool defaultVal, JSONStorableBool.SetBoolCallback callback, bool register=true)
        {
            JSONStorableBool JSONBool = new JSONStorableBool(name, defaultVal, callback);
            if(register) UIManager.script.RegisterBool(JSONBool);
            return JSONBool;
        }
    }

    public class Delay
    {
        public bool isOnTimeout;
        // public Delay(float t)
        // {
        //     StartDelay(t).Start();
        // }

        public IEnumerator StartDelay(float t)
        {
            isOnTimeout = true;
            yield return new WaitForSeconds(t);
            isOnTimeout = false;
        }
    }
}