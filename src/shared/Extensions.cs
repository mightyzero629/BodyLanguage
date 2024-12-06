using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using SimpleJSON;
using MacGruber;
using Random = System.Random;

namespace CheesyFX
{
	public static class Extensions
	{
		private static Random rnd = new Random();
		
		public static bool IsInPackage(this MVRScript self)
		{
			string id = self.name.Substring(0, self.name.IndexOf('_'));
			string filename = self.manager.GetJSON()["plugins"][id].Value;
			return filename.IndexOf(":/") >= 0;
		}

		public static MVRScript FindPluginPeer(this MVRScript script, string name)
		{
			MVRScript peer = null;
			Transform container = script.transform.parent;
			for (int i = 0; i < container.childCount; i++)
			{
				var child = container.GetChild(i);
				if (child.name.EndsWith(name))
				{
					peer = child.GetComponent<MVRScript>();
					break;
				}
			}
			return peer;
		}
		
		public static IEnumerable<T> TakeRandomN<T>(this IList<T> list, int needed)
		{
			for (int i = 0; i < needed; i++)
			{
				var index = rnd.Next(i, list.Count);
				var item = list[index];
				list[index] = list[i];
				list[i] = item;
			}
    
			return list.Take(needed);
		}
		
		public static T TakeRandom<T>(this IList<T> list, T exclude)
		{
			if (list.Count == 0) return default(T);
			if (list.Count == 1) return list[0];
			var id = list.IndexOf(exclude);
			if (id != -1)
			{
				list.RemoveAt(id);
			}
			var item = list[rnd.Next(list.Count)];
			if (id != -1) list.Insert(id, exclude);
			return item;
		}
		
		public static T TakeRandom<T>(this IList<T> list, int excludedId)
		{
			var excluded = list[excludedId];
			list.RemoveAt(excludedId);
			var item = list[rnd.Next(list.Count)];
			list.Insert(excludedId, excluded);
			return item;
		}
		
		internal static StringBuilder Clear(this StringBuilder sb, int length = 0)
		{
			sb.Length = 0;
			sb.Capacity = length;
			return sb;
		}

		public static DAZCharacterSelector.Gender GetGender(this Atom atom)
		{
			return atom.GetComponentInChildren<DAZCharacterSelector>().gender;
		}
		
		public static void AddCallback(this JSONStorableBool jsonParam, Action<bool> callback, bool useDefault = true)
		{
			jsonParam.setCallbackFunction += val => callback(val);
			if(useDefault) callback(jsonParam.defaultVal);
			else callback(jsonParam.val);
		}
        
		public static void AddCallback(this JSONStorableFloat jsonParam, Action<float> callback)
		{
			jsonParam.setCallbackFunction += val => callback(val);
			callback(jsonParam.defaultVal);
		}
        
		public static void AddCallback(this JSONStorableString jsonParam, Action<string> callback)
		{
			jsonParam.setCallbackFunction += val => callback(val);
			callback(jsonParam.defaultVal);
		}
        
		public static void AddCallback(this JSONStorableStringChooser jsonParam, Action<string> callback)
		{
			jsonParam.setCallbackFunction += val => callback(val);
			callback(jsonParam.defaultVal);
		}
        
		public static void AddCallback(this JSONStorableUrl jsonParam, Action<string> callback)
		{
			jsonParam.setCallbackFunction += val => callback(val);
			callback(jsonParam.defaultVal);
		}
		
        public static void AddCallback(this JSONStorableVector3 jsonParam, Action<Vector3> callback)
        {
            jsonParam.setCallbackFunction += val => callback(val);
            callback(jsonParam.defaultVal);
        }
        
        public static void AddCallback(this JSONStorableVector4 jsonParam, Action<Vector4> callback)
        {
            jsonParam.setCallbackFunction += val => callback(val);
            callback(jsonParam.defaultVal);
        }
        
        public static void SyncChoices(this JSONStorableStringChooser jsonParam)
        {
	        var reference = jsonParam.choices;
	        jsonParam.choices = null;
	        jsonParam.choices = reference;
        }
        
        public static void SetChoices(this JSONStorableStringChooser jsonParam, IEnumerable<string> choices)
        {
	        var reference = jsonParam.choices;
	        jsonParam.choices = null;
	        if (reference == null) reference = new List<string>();
		    else reference.Clear();
	        reference.AddRange(choices);
	        jsonParam.choices = reference;
        }
        
        public static void AddChoice(this JSONStorableStringChooser jsonParam, string choice, bool sort = false)
        {
	        var reference = jsonParam.choices;
	        jsonParam.choices = null;
	        if (reference == null) reference = new List<string>();
	        reference.Add(choice);
	        if(sort) reference.Sort();
	        jsonParam.choices = reference;
        }
        
        public static void InsertChoice(this JSONStorableStringChooser jsonParam, string choice, int id)
        {
	        var reference = jsonParam.choices;
	        jsonParam.choices = null;
	        if (reference == null) reference = new List<string>{choice};
	        else reference.Insert(id, choice);
	        jsonParam.choices = reference;
        }

        public static void SetVisible(this UIDynamic uiDynamic, bool val)
        {
	        foreach (Transform child in uiDynamic.transform)
	        {
		        child.gameObject.SetActive(val);
	        }
        }

        public static UIDynamicButton SetupButton(this MVRScript script, 
	        string label, 
	        bool rightSide = false, 
	        Action callback = null, 
	        List<object> UIElements = null)
        {
	        if(callback == null) callback = delegate {};
	        var button = script.CreateButton(label, rightSide);
	        button.button.onClick.AddListener(() => callback());
	        UIElements?.Add(button);
	        return button;
        }
        
        public static UIDynamicButton SetupButton(this MVRScript script, 
	        string label,
	        Action callback, 
	        Color color,
	        List<object> UIElements = null,
	        bool rightSide = false)
        {
	        if(callback == null) callback = delegate {};
	        var button = script.CreateButton(label, rightSide);
	        button.button.onClick.AddListener(() => callback());
	        button.GetComponentInChildren<Image>().color = color;
	        UIElements?.Add(button);
	        return button;
        }

        public static UIDynamicTextInfo CreateStaticInfo(this MVRScript script, string text, float height, List<object> UIElements = null, bool rightSide = false)
        {
	        var uid = Utils.SetupInfoTextNoScroll(FillMeUp.singleton, text, height, true);
	        uid.background.offsetMin = new Vector2(0, 0);
	        UIElements?.Add(uid);
	        return uid;
        }

        public static FloatTriggerManager AddFloatTriggerManager(this MVRScript script, JSONStorableFloat driver, bool absoluteDef = false, float thresholdDef = 0f, float fromDef = 0f, float toDef = 1f)
        {
	        var manager = script.gameObject.AddComponent<FloatTriggerManager>();
	        manager.Init(script, driver, absoluteDef, thresholdDef, fromDef, toDef);
	        return manager;
        }

        public static void SetCallback(this UIDynamicButton button, Action callback)
        {
	        button.button.onClick.RemoveAllListeners();
	        button.button.onClick.AddListener(() => callback());
        }
	}
	
	public static class VectorExt
	{
		public static Vector3 SetComponent(this Vector3 v, int i, float val)
		{
			v[i] = val;
			return v;
		}
		
		public static float Max(this Vector3 v)
		{
			return Mathf.Max(v.x, Mathf.Max(v.y, v.z));
		}
		
		public static float MaxExcluding(this Vector3 v, int i)
		{
			if(i != 0)
			{
				float max = v[0];
				for (int j = 1; j < 3; j++)
				{
					if (j != i && v[j] > max) max = v[j];
				}

				return max;
			}
			return Mathf.Max(v[1], v[2]);
		}
		
		public static Vector3 ProjectY(this Vector3 v)
		{
			return new Vector3(v.x, 0f, v.z);
		}

		public static Vector3 MirrorY(this Vector3 v)
		{
			return new Vector3(v.x, -v.y, v.z);
		}

		public static float RadialDistance(this Vector3 v, Vector3 w)
		{
			return (ProjectY(v) - ProjectY(w)).magnitude;
		}

		public static float RadialSqrDistance(this Vector3 v, Vector3 w)
		{
			return (ProjectY(v) - ProjectY(w)).sqrMagnitude;
		}
		
		public static Vector3 Scale(this Vector3 v, Vector3 w)
		{
			return new Vector3(v.x * w.x, v.y*w.y, v.z*w.z);
		}

		public static Vector3 ProjectOnUnit(this Vector3 v, Vector3 unit)
		{
			return Vector3.Dot(v, unit) * unit;
		}

		public static Vector3 ScaleComponentsAlongUnit(this Vector3 v, Vector3 unit1, Vector3 unit2,
			float scale1, float scale2)
		{
			return scale1 * v.ProjectOnUnit(unit1) + scale2 * v.ProjectOnUnit(unit2);
		}

		public static JSONArray ToJA(this Vector3 v)
		{
			JSONArray ja = new JSONArray();
			for (int i = 0; i < 3; i++)
				ja[i].AsFloat = v[i];
			return ja;
		}
		
		public static JSONArray ToJA(this Quaternion v)
		{
			JSONArray ja = new JSONArray();
			for (int i = 0; i < 4; i++)
				ja[i].AsFloat = v[i];
			return ja;
		}
		
		public static Quaternion ToQuat(this JSONArray v)
		{
			Quaternion q = new Quaternion(v[0].AsFloat, v[1].AsFloat, v[2].AsFloat, v[3].AsFloat);
			return q;
		}
		
		public static JSONClass ToJC(this Vector3 v)
		{
			JSONClass jc = new JSONClass();
			jc["x"].AsFloat = v.x;
			jc["y"].AsFloat = v.y;
			jc["z"].AsFloat = v.z;
			return jc;
		}
		
		public static JSONArray ToJA(this Vector4 v)
		{
			JSONArray ja = new JSONArray();
			for (int i = 0; i < 4; i++)
				ja[i].AsFloat = v[i];
			return ja;
		}

		public static Vector2 Scale(this Vector2 v, Vector2 w)
		{
			return new Vector2(v.x * w.x, v.y * w.y);
		}
		
		public static Vector2 ScaleX(this Vector2 v, float scale)
		{
			return new Vector2(v.x * scale, v.y);
		}
		
		public static Vector2 ScaleY(this Vector2 v, float scale)
		{
			return new Vector2(v.x, v.y * scale);
		}
		
		public static Vector2 SetX(this Vector2 v, float val)
		{
			return new Vector2(val, v.y);
		}
		
		public static Vector2 SetY(this Vector2 v, float val)
		{
			return new Vector2(v.x, val);
		}
		
		public static Vector3 ToV3(this Vector4 v)
		{
			return new Vector3(v.x, v.y, v.z);
		}
		
		public static Vector4 ToV4(this Vector3 v, float w = 0f)
		{
			return new Vector4(v.x, v.y, v.z, w);
		}
		
		public static Vector4 SetComponent(this Vector4 v, int i, float val)
		{
			Vector4 w = v;
			w[i] = val;
			return w;
		}

	}
	
	public static class TransformExt
	{
		public static Vector3 Axis(this Transform t, int axis)
		{
			switch (axis)
			{
				case 0: return t.right;
				case 1: return t.up;
				case 2: return t.forward;
				default: return Vector3.zero;
			}
		}
		
		public static void PrintChildren(this Transform t)
		{
			for (int i = 0; i < t.childCount; i++)
			{
				t.GetChild(i).name.Print();
			}
		}
		
		public static void PrintHierarchy(this Transform t, string parent = null, int depth = 100)
		{
			if(depth < 0) return;
			if(parent == null)
			{
				for (int i = 0; i < t.childCount; i++)
				{
					var child = t.GetChild(i);
					if(child.childCount == 0) child.name.Print();
					else child.PrintHierarchy(child.name, depth - 1);
				}
			}
			else
			{
				for (int i = 0; i < t.childCount; i++)
				{
					var child = t.GetChild(i);
					if(child.childCount == 0) $"{parent}/{child.name}".Print();
					else child.PrintHierarchy($"{parent}/{child.name}", depth - 1);
				}
			}
		}

		public static List<Transform> GetAllChildren(this Transform t, List<Transform> children = null)
		{
			if(children == null) children = new List<Transform>();
			foreach (Transform child in t)
			{
				children.Add(child);
				GetAllChildren(child, children);
			}

			return children;
		}
		
		public static void PrintParents(this Transform t)
		{
			var parent = t;
			string path = "";
			while (parent != null)
			{
				path = path.Insert(0, $"{parent.name}/");
				parent = parent.parent;
			}
			path.Print();
		}
	}

	public static class JSONArrayExt
	{
		public static Vector3 ToV3(this JSONArray ja)
		{
			Vector3 v = new Vector3();
			for (int i = 0; i < 3; i++)
				v[i] = ja[i].AsFloat;
			return v;
		}
		
		public static Vector3 ToV3(this JSONClass ja)
		{
			Vector3 v = new Vector3();
			v.x = ja["x"].AsFloat;
			v.y = ja["y"].AsFloat;
			v.z = ja["z"].AsFloat;
			return v;
		}

		public static Vector4 ToV4(this JSONArray ja)
		{
			Vector4 v = new Vector4();
			for (int i = 0; i < 4; i++)
				v[i] = ja[i].AsFloat;
			return v;
		}
	}

	public static class ColliderExt
	{
		public static string GetBodyRegion(this Collider rb)
		{
			if (rb.name.Contains("Link")) return null;
			else if (rb.name.Contains("Control")) return null;

			else if (rb.name.Contains("Lip")) return "Lips";
			else if (rb.name.Contains("FaceHardLeft5") || rb.name.Contains("FaceHardRight5"))
			{
				return "Eyes";
				rb.name.Print();
			}
			else if (rb.name.ToLower().Contains("head")) return "Head";
			else if (rb.name.Contains("Face")) return "Head";
			else if (rb.name.Contains("chest")) return "Chest";
			else if (rb.name.Contains("pelvis")) return "Pelvis";
			else if (rb.name.Contains("rThigh")) return "rThigh";
			else if (rb.name.Contains("lThigh")) return "lThigh";
			else if (rb.name.Contains("rShin")) return "rShin";
			else if (rb.name.Contains("lShin")) return "lShin";
			else if (!rb.name.Contains("rShoe") && (rb.name.Contains("rFoot") || rb.name.Contains("rBigToe") ||
			                                        rb.name.Contains("rSmallToe") ||
			                                        rb.name.Contains("rToe"))) return "rFoot";
			else if (!rb.name.Contains("lShoe") && (rb.name.Contains("lFoot") || rb.name.Contains("lBigToe") ||
			                                        rb.name.Contains("lSmallToe") ||
			                                        rb.name.Contains("lToe"))) return "lFoot";
			else if (rb.name.Contains("PhysicsMeshJointleft glute")) return "lGlutes";
			else if (rb.name.Contains("PhysicsMeshJointright glute")) return "rGlutes";
			else if (rb.name.Contains("AutoColliderFemaleAutoColliderslPectoral")) return "lBreast";
			else if (rb.name.Contains("AutoColliderFemaleAutoCollidersrPectoral")) return "rBreast";
			else if (rb.name.Contains("PhysicsMeshJointleft")) return "lBreast";
			else if (rb.name.Contains("PhysicsMeshJointright")) return "rBreast";
			else if (rb.name.Contains("JointG")) return "Labia";
			else if (rb.name.Contains("PhysicsMeshJointgen")) return "Labia";
			else if (rb.name.Contains("PhysicsMeshJointlab")) return "Labia";
			else if (rb.name.Contains("JointA")) return "Anus";
			else if (rb.name.Contains("PhysicsMeshJointan")) return "Anus";
			else if (rb.name.Contains("PhysicsMeshJointbridge")) return "Anus";
			// else if(rb.name.Contains("PhysicsMeshJoint")) return "Lips";
			else if (rb.name.Contains("chest")) return "Chest";
			else if (rb.name.Contains("abdomen")) return "Abdomen";
			else if (rb.name.Contains("pelvis")) return "Pelvis";
			else if (rb.name.Contains("lForeArm") || rb.name.Contains("lShldr")) return "lArm";
			else if (rb.name.Contains("rForeArm") || rb.name.Contains("rShldr")) return "rArm";
			else if (rb.name.Contains("lHand") || rb.name.Contains("lCarpal") || rb.name.Contains("lThumb") ||
			         rb.name.Contains("lIndex") || rb.name.Contains("lMid") || rb.name.Contains("lRing") ||
			         rb.name.Contains("lPinky")) return "lHand";
			else if (rb.name.Contains("rHand") || rb.name.Contains("rCarpal") || rb.name.Contains("rThumb") ||
			         rb.name.Contains("rIndex") || rb.name.Contains("rMid") || rb.name.Contains("rRing") ||
			         rb.name.Contains("rPinky")) return "rHand";

			else
			{
				// (name+" is not a valid region.").Print();
				return null;
			}
		}
		
		public static Atom GetAtom(this Collider collider)
		{
			Atom atom = null;
			Transform parent = collider.transform.parent;
			int i = 0;
			while (atom == null && i < 20)
			{
				atom = parent.gameObject.GetComponent<Atom>();
				parent = parent.parent;
				i += 1;
			}

			return atom;
		}
	}

	public static class RigidbodyExt
	{
		static Vector3 lastVelocity;

		static Vector3 lastAngularVelocity;
		// public static Actor GetActor(this Rigidbody rb, CheesyFX_OmniForce script){
		// 	Atom atom = null;
		// 	FreeControllerV3 ctrl = rb.GetComponent<FreeControllerV3>();
		// 	if (ctrl != null) atom = ctrl.containingAtom;
		// 	else{
		// 		ForceReceiver fr = rb.GetComponent<ForceReceiver>();
		// 		if (fr != null) atom = fr.containingAtom;
		// 	}
		// 	return script.actors.First(x => x.atom == atom);
		// }

		public static Atom GetAtom(this Rigidbody rb)
		{
			Atom atom = null;
			Transform parent = rb.transform.parent;
			int i = 0;
			while (atom == null && i < 20)
			{
				atom = parent.gameObject.GetComponent<Atom>();
				// if(atom != null) parent.name.Print();
				parent = parent.parent;
				i += 1;
			}

			return atom;
		}

		public static Vector3 GetAcceleration(this Rigidbody rb)
		{
			Vector3 acceleration = (rb.velocity - lastVelocity) / Time.deltaTime;
			lastVelocity = rb.velocity;
			return acceleration;
		}

		public static Vector3 GetAngularAcceleration(this Rigidbody rb)
		{
			Vector3 angularAcceleration = (rb.angularVelocity - lastAngularVelocity) / Time.deltaTime;
			lastAngularVelocity = rb.angularVelocity;
			return angularAcceleration;
		}

		public static float GetUpAngle(this Rigidbody rb)
		{
			return Vector3.Dot(rb.transform.up, Vector3.up);
		}
	}


	public static class ObjectExt
	{
		static float timer = 0f;

		public static void Print(this object f)
		{
			SuperController.LogMessage(f.ToString());
		}
		
		public static void Print(this List<string> f)
		{
			f.ForEach(x => SuperController.LogMessage(x));
		}
		
		public static void Print(this List<float> f)
		{
			f.ForEach(x => SuperController.LogMessage(x.ToString()));
		}
		
		public static void Print(this Vector3 v)
		{
			SuperController.LogMessage($"({v.x:0.000}, {v.y:0.000}, {v.z:0.000})");
		}

		public static void PrintEvery(this object f, float interval)
		{
			timer -= Time.fixedDeltaTime;
			if (timer < 0f)
			{
				timer = interval;
				SuperController.LogMessage(f.ToString());
			}
		}

		public static Collider GetColliderFromCol(this object obj)
		{
			Collider collider = null;
			if (obj is Collider) collider = (Collider)obj;
			if (obj is Collision) collider = ((Collision)obj).collider;
			else SuperController.LogError("Object has no Collider.");
			return collider;
		}

		public static Rigidbody GetRigidbodyFromCol(this object obj)
		{
			Rigidbody rigidbody = null;
			if (obj is Collider) rigidbody = ((Collider)obj).attachedRigidbody;
			if (obj is Collision) rigidbody = ((Collision)obj).rigidbody;
			// else SuperController.LogError("Object has no rigidbody.");
			else obj.GetType().Print();
			return rigidbody;
		}

		public static bool NullCheck(this object obj)
		{
			bool val = obj == null;
			SuperController.LogMessage($"{val}");
			return val;
		}
	}

	public static class IntExt
	{
		static float timer = 0f;

		public static void Print(this int f)
		{
			SuperController.LogMessage(f.ToString());
		}

		public static void PrintEvery(this int f, float interval)
		{
			timer -= Time.fixedDeltaTime;
			if (timer < 0f)
			{
				timer = interval;
				SuperController.LogMessage(f.ToString());
			}
		}
	}

	public static class FloatExt
	{
		static float timer = 0f;

		public static void Print(this float f)
		{
			SuperController.LogMessage(f.ToString());
		}

		public static void PrintEvery(this float f, float interval)
		{
			timer -= Time.fixedDeltaTime;
			if (timer < 0f)
			{
				timer = interval;
				SuperController.LogMessage(f.ToString());
			}
		}
	}

	public static class BoolExt
	{
		static float timer = 0f;

		public static void Print(this bool f)
		{
			SuperController.LogMessage(f.ToString());
		}

		public static void PrintEvery(this bool f, float interval)
		{
			timer -= Time.fixedDeltaTime;
			if (timer < 0f)
			{
				timer = interval;
				SuperController.LogMessage(f.ToString());
			}
		}
	}

	public static class ArrayExt
	{
		static float timer = 0f;

		public static void Print(this IEnumerable<float> f)
		{
			string[] sArray = f.Select(x => x.ToString("0.000")).ToArray();
			SuperController.LogMessage("[" + string.Join(", ", sArray) + "]");
		}

		// public static void PrintEvery(this bool f, float interval){
		// 	timer -= Time.fixedDeltaTime;
		// 	if(timer < 0f){
		// 		timer = interval;
		// 		SuperController.LogMessage(f.ToString());
		// 	}
		// }
	}

	public static class StringExt
	{
		static float timer = 0f;

		public static void Print(this string s)
		{
			SuperController.LogMessage(s);
		}

		public static void PrintEvery(this string s, float interval)
		{
			timer -= Time.fixedDeltaTime;
			if (timer < 0f)
			{
				timer = interval;
				SuperController.LogMessage(s);
			}
		}
	}

	public static class IEumeratorExt
	{
		public static IEnumerator Start(this IEnumerator ienum)
		{
			if (ienum != null) SuperController.singleton.StartCoroutine(ienum);
			return ienum;
		}

		public static void Stop(this IEnumerator ienum)
		{
			if (ienum != null) SuperController.singleton.StopCoroutine(ienum);
		}
	}

	public static class GameObjectExt
	{

		public static Atom GetAtom(this GameObject go)
		{
			Atom atom = go.GetComponent<Atom>();
			Transform parent = go.transform.parent;
			int i = 0;
			while (atom == null && parent != null && i < 20)
			{
				atom = parent.GetComponent<JSONStorable>()?.containingAtom;
				parent = parent.transform.parent;
				i++;
			}
			return atom;
		}
		
		public  static string GetPath(this GameObject go){
			if (go.transform.parent == null)
				return "/" + go.name;
			return GetPath(go.transform.parent.gameObject) + "/" + go.name;
		}
	}

	public static class JSONStorableStringChooserExt
	{
		public static void Sort(this JSONStorableStringChooser sc)
		{
			List<string> choices = new List<string>(sc.choices);
			choices.Sort();
			sc.choices = choices;
		}
	}

	public static class AtomExt
	{
		public static bool IsToy(this Atom atom)
		{
			return atom.type == "ToyBP" || atom.type == "ToyAH";
		}
		
		public static bool IsToyOrDildo(this Atom atom)
		{
			return atom.type == "ToyBP" || atom.type == "ToyAH" || atom.type == "Dildo";
		}
		
		public static JSONStorable GetStorable(this Atom atom, string name, bool exactMatch = false)
		{
			string storableId;
			if (!exactMatch)
			{
				storableId = atom.GetStorableIDs().FirstOrDefault(x => x.Contains(name));
				if (storableId != null)
				{
					return atom.GetStorableByID(storableId);
				}
				else return null;
			}
			// else storableId = atom.GetStorableIDs().FirstOrDefault(x => x == name);
			else return atom.GetStorableByID(name);
		}

		public static void PrintStorablesOfType<T>(this Atom atom)
		{
			foreach (var name in atom.GetStorableIDs())
			{
				if (atom.GetStorableByID(name) is T) name.Print();
			}
		}

		public static void PrintStorables(this Atom atom, string filter = null)
		{
			foreach (var name in atom.GetStorableIDs())
			{
				if (filter == null || name.ToLower().Contains(filter.ToLower()))
				{
					(name + ": " + atom.GetStorableByID(name).GetType()).Print();
				}
			}
		}

		public static bool IsVisible(this Atom atom)
		{
			if (atom.name == "CoreControl" || atom.name == "[CameraRig]" || atom.name == "Chokpahi-Asset") return false;
			Renderer[] renderers = atom.GetComponentsInChildren<Renderer>();
			return renderers.Any(x => x.enabled == true);
		}

		public static float GetActivity(this Atom atom)
		{
			float activity = 0f;
			foreach (ForceReceiver fr in atom.forceReceivers)
			{
				Rigidbody rb = fr.GetComponent<Rigidbody>();
				activity += rb.mass * Math.Abs(Vector3.Dot(rb.GetAcceleration(), rb.velocity));
				Vector3 angularAcc = rb.GetAngularAcceleration();
				for (int i = 0; i < 3; i++)
				{
					activity += Math.Abs(angularAcc[i] * rb.inertiaTensor[i] * rb.angularVelocity[i]);
				}
			}

			return activity;
		}

		public static List<string> GetInterestRegions(this Atom atom)
		{
			return new List<string>
			{
				"Eyes",
				"Lips",
				"Titts",
				"Ass",
				"Anus",
				"Pussy",
				"Legs",
				"Feet",
				// "Chest",
				// "Pelvis",
				// "Arms",
			};
		}

		public static List<Rigidbody> GetRigidbodiesByRegion(this Atom atom, string name)
		{
			// List<Rigidbody> rigidbodies = atom.rigidbodies.ToList();
			List<Rigidbody> rigidbodies = atom.GetComponentsInChildren<Rigidbody>().ToList();
			List<Rigidbody> output;
			switch (name)
			{
				case "Head":
					output = rigidbodies.Where(x => x.name.Contains("head") && !x.name.Contains("Control")).ToList();
					break;
				case "Lips":
					output = atom.transform.Find("rescale2/geometry/FemaleMorphers/MouthPhysicsMesh").GetComponentsInChildren<Rigidbody>().Where(x => !x.name.Contains("KRB")).ToList();
					break;
				// case "Mouth":
				// 	output = rigidbodies.Where(x => x.name == "MouthTrigger").ToList();
				// 	break;
				// case "Throat":
				// 	output = rigidbodies.Where(x => x.name == "ThroatTrigger").ToList();
				// 	break;
				case "Chest":
					output = rigidbodies.Where(x => x.name.Contains("chest") && !x.name.Contains("Control")).ToList();
					break;
				case "Pelvis":
					output = rigidbodies.Where(x => x.name.Contains("pelvis") && !x.name.Contains("Control")).ToList();
					break;
				case "Abdomen":
					output = rigidbodies.Where(x => x.name.Contains("abdomen")).ToList();
					break;
				case "lBreast":
					output = rigidbodies.Where(x => x.name.Contains("lPectoral")).ToList();
					break;
				case "rBreast":
					output = rigidbodies.Where(x => x.name.Contains("rPectoral")).ToList();
					break;
				case "lAreola":
					output = atom.transform.Find("rescale2/geometry/FemaleMorphers/BreastPhysicsMesh/PhysicsMeshleftareola").GetComponentsInChildren<Rigidbody>().Where(x => !x.name.Contains("KRB")).ToList();
					break;
				case "rAreola":
					output = atom.transform.Find("rescale2/geometry/FemaleMorphers/BreastPhysicsMesh/PhysicsMeshrightareola").GetComponentsInChildren<Rigidbody>().Where(x => !x.name.Contains("KRB")).ToList();
					break;
				case "lNipple":
					output = atom.transform.Find("rescale2/geometry/FemaleMorphers/BreastPhysicsMesh/PhysicsMeshleftnipple").GetComponentsInChildren<Rigidbody>().Where(x => !x.name.Contains("KRB")).ToList();
					break;
				case "rNipple":
					output = atom.transform.Find("rescale2/geometry/FemaleMorphers/BreastPhysicsMesh/PhysicsMeshrightnipple").GetComponentsInChildren<Rigidbody>().Where(x => !x.name.Contains("KRB")).ToList();
					break;
				case "lGlutes":
					output = rigidbodies.Where(x => x.name.Contains("LGlute") || x.name.Contains("PhysicsMeshJointleft glute")).ToList();
					break;
				case "rGlutes":
					output = rigidbodies.Where(x => x.name.Contains("RGlute") || x.name.Contains("PhysicsMeshJointright glute")).ToList();
					break;
				case "Labia":
					output = rigidbodies.Where(x => x.name.Contains("PhysicsMeshJointgen") || x.name.Contains("PhysicsMeshJointlab")).ToList();
					break;
				case "Penis":
					output = rigidbodies.Where(x => x.name.Contains("Gen")).ToList();
					break;
				case "Testes":
					output = rigidbodies.Where(x => x.name == "Testes").ToList();
					break;
				// case "Vagina":
				// 	output = rigidbodies.Where(x => x.name == "VaginaTrigger").ToList();
				// 	break;
				// case "DeepVagina":
				// 	output = rigidbodies.Where(x => x.name == "DeepVaginaTrigger").ToList();
				// 	break;
				// case "DeeperVagina":
				// 	output = rigidbodies.Where(x => x.name == "DeeperVaginaTrigger").ToList();
				// 	break;
				case "Anus":
					output = rigidbodies.Where(x => x.name.Contains("_JointA")).ToList();
					break;
				case "lThigh":
					output = rigidbodies.Where(x => x.name.Contains("lThigh") && !x.name.Contains("Control")).ToList();
					break;
				case "rThigh":
					output = rigidbodies.Where(x => x.name.Contains("rThigh") && !x.name.Contains("Control")).ToList();
					break;
				case "lShin":
					output = rigidbodies.Where(x => x.name.Contains("lShin")).ToList();
					break;
				case "rShin":
					output = rigidbodies.Where(x => x.name.Contains("rShin")).ToList();
					break;
				case "lFoot":
					output = rigidbodies.Where(x => x.name.Contains("lFoot")).ToList();
					break;
				case "lBigToe":
					output = rigidbodies.Where(x => x.name.Contains("lBigToe")).ToList();
					break;
				case "lSmallToe1":
					output = rigidbodies.Where(x => x.name.Contains("lSmallToe1")).ToList();
					break;
				case "lSmallToe2":
					output = rigidbodies.Where(x => x.name.Contains("lSmallToe2")).ToList();
					break;
				case "lSmallToe3":
					output = rigidbodies.Where(x => x.name.Contains("lSmallToe3")).ToList();
					break;
				case "lSmallToe4":
					output = rigidbodies.Where(x => x.name.Contains("lSmallToe4")).ToList();
					break;
				case "rFoot":
					output = rigidbodies.Where(x => x.name.Contains("rFoot")).ToList();
					break;
				case "rBigToe":
					output = rigidbodies.Where(x => x.name.Contains("rBigToe")).ToList();
					break;
				case "rSmallToe1":
					output = rigidbodies.Where(x => x.name.Contains("rSmallToe1")).ToList();
					break;
				case "rSmallToe2":
					output = rigidbodies.Where(x => x.name.Contains("rSmallToe2")).ToList();
					break;
				case "rSmallToe3":
					output = rigidbodies.Where(x => x.name.Contains("rSmallToe3")).ToList();
					break;
				case "rSmallToe4":
					output = rigidbodies.Where(x => x.name.Contains("rSmallToe4")).ToList();
					break;
				// if(name == "lToes") output = rigidbodies.Where(x => x.name.Contains("lBigToe") && x.name.Contains("lIndex") && x.name.Contains("lMid") && x.name.Contains("lRing") && x.name.Contains("lPinky")).ToList();
				// if(name == "rFingers") output = rigidbodies.Where(x => x.name.Contains("rThumb") && x.name.Contains("rIndex") && x.name.Contains("rMid") && x.name.Contains("rRing") && x.name.Contains("rPinky")).ToList();
				case "lShoulder":
					output = rigidbodies.Where(x => x.name == "lShldr").ToList();
					break;
				case "rShoulder":
					output = rigidbodies.Where(x => x.name == "rShldr").ToList();
					break;
				case "lForeArm":
					output = rigidbodies.Where(x => x.name == "lForeArm").ToList();
					break;
				case "rForeArm":
					output = rigidbodies.Where(x => x.name == "rForeArm").ToList();
					break;
				case "lPalm":
					output = rigidbodies.Where(x => x.name.Contains("lHand") || x.name.Contains("lCarpal")).ToList();
					break;
				case "lThumb":
					output = rigidbodies.Where(x => x.name.Contains("lThumb")).ToList();
					break;
				case "lIndex":
					output = rigidbodies.Where(x => x.name.Contains("lIndex")).ToList();
					break;
				case "lMid":
					output = rigidbodies.Where(x => x.name.Contains("lMid")).ToList();
					break;
				case "lRing":
					output = rigidbodies.Where(x => x.name.Contains("lRing")).ToList();
					break;
				case "lPinky":
					output = rigidbodies.Where(x => x.name.Contains("lPinky")).ToList();
					break;
				case "rPalm":
					output = rigidbodies.Where(x => x.name.Contains("rHand") || x.name.Contains("rCarpal")).ToList();
					break;
				case "rThumb":
					output = rigidbodies.Where(x => x.name.Contains("rThumb")).ToList();
					break;
				case "rIndex":
					output = rigidbodies.Where(x => x.name.Contains("rIndex")).ToList();
					break;
				case "rMid":
					output = rigidbodies.Where(x => x.name.Contains("rMid")).ToList();
					break;
				case "rRing":
					output = rigidbodies.Where(x => x.name.Contains("rRing")).ToList();
					break;
				case "rPinky":
					output = rigidbodies.Where(x => x.name.Contains("rPinky")).ToList();
					break;
				// if(name == "lHand") output = rigidbodies.Where(x => x.name.Contains("lHand") || x.name.Contains("lCarpal") || x.name.Contains("lThumb") || x.name.Contains("lIndex") || x.name.Contains("lMid") || x.name.Contains("lRing") || x.name.Contains("lPinky")).ToList();
				// if(name == "rHand") output = rigidbodies.Where(x => x.name.Contains("rHand") || x.name.Contains("rCarpal") || x.name.Contains("rThumb") || x.name.Contains("rIndex") || x.name.Contains("rMid") || x.name.Contains("rRing") || x.name.Contains("rPinky")).ToList();
				default:
					// (name + " is not a valid region.").Print();
					output = new List<Rigidbody>();
					break;
			}

			return output;
		}
	}

	public static class UIDynamicExt
	{
		public static UIDynamic ForceHeight(this UIDynamic uIDynamic, float height)
		{
			LayoutElement component = uIDynamic.GetComponent<LayoutElement>();
			component.minHeight = height;
			component.preferredHeight = height;
			return uIDynamic;
		}
	}

	public static class JSONStorableParamExt
	{
		public static void SetWithDefault(this JSONStorableFloat jsonParam, float val)
		{
			jsonParam.val = jsonParam.defaultVal = val;
		}
		
		public static void SetWithDefault(this JSONStorableBool jsonParam, bool val)
		{
			jsonParam.val = jsonParam.defaultVal = val;
		}
		
		public static void SetWithDefault(this JSONStorableString jsonParam, string val)
		{
			jsonParam.val = jsonParam.defaultVal = val;
		}
		
		public static void SetWithDefault(this JSONStorableStringChooser jsonParam, string val)
		{
			jsonParam.val = jsonParam.defaultVal = val;
		}
		
		public static UIDynamic CreateUI(this JSONStorableParam jsonParam, MVRScript script = null, bool rightSide = false, List<object> UIElements = null, int chooserType = 0)
		{
			if (script == null) script = UIManager.script;
			UIDynamic uiDynamic = null;
		
			if (jsonParam is JSONStorableBool)
				uiDynamic = script.CreateToggle((JSONStorableBool)jsonParam, rightSide);
			else if (jsonParam is JSONStorableFloat)
				uiDynamic = script.CreateSlider((JSONStorableFloat)jsonParam, rightSide);
			else if (jsonParam is JSONStorableStringChooser)
			{
				if (chooserType == 0)
					uiDynamic = script.CreatePopup((JSONStorableStringChooser)jsonParam, rightSide);
				else if (chooserType == 1)
					uiDynamic = script.CreateScrollablePopup((JSONStorableStringChooser)jsonParam, rightSide);
				else if (chooserType == 2)
					uiDynamic = script.CreateFilterablePopup((JSONStorableStringChooser)jsonParam, rightSide);
			}
			else if (jsonParam is JSONStorableString)
			{
				JSONStorableString storableString = jsonParam as JSONStorableString;
				uiDynamic = Utils.SetupTextInput(script, storableString.name, storableString, rightSide);
			}
			else if (jsonParam is JSONStorableColor)
			{
				uiDynamic = script.CreateColorPicker((JSONStorableColor)jsonParam, rightSide);
			}
			else return null;
		
			if (UIElements != null) UIElements.Add(uiDynamic);
			return uiDynamic;
		}
		
		public static UIDynamic CreateUI(this JSONStorableParam jsonParam, List<object> UIElements, bool rightSide = false, int chooserType = 0)
		{
			var script = UIManager.script;
			UIDynamic uiDynamic = null;

			if (jsonParam is JSONStorableBool)
				uiDynamic = script.CreateToggle((JSONStorableBool)jsonParam, rightSide);
			else if (jsonParam is JSONStorableFloat)
				uiDynamic = script.CreateSlider((JSONStorableFloat)jsonParam, rightSide);
			else if (jsonParam is JSONStorableStringChooser)
			{
				if (chooserType == 0)
					uiDynamic = script.CreatePopup((JSONStorableStringChooser)jsonParam, rightSide);
				else if (chooserType == 1)
					uiDynamic = script.CreateScrollablePopup((JSONStorableStringChooser)jsonParam, rightSide);
				else if (chooserType == 2)
					uiDynamic = script.CreateFilterablePopup((JSONStorableStringChooser)jsonParam, rightSide);
				else if (chooserType == 3)
				{
					var uid = script.CreateScrollablePopup((JSONStorableStringChooser)jsonParam, rightSide);
					uid.popup.showSlider = false;
					uid.popup.popupPanelHeight = 600f;
					uid.ForceHeight(70f);
					uiDynamic = uid;
				}
				// ((UIDynamicPopup)uiDynamic).popup.popupPanelHeight = popupHeight;
			}
			else if (jsonParam is JSONStorableString)
			{
				JSONStorableString storableString = jsonParam as JSONStorableString;
				uiDynamic = Utils.SetupTextInput(script, storableString.name, storableString, rightSide);
			}
			else if (jsonParam is JSONStorableColor)
			{
				uiDynamic = script.CreateColorPicker((JSONStorableColor)jsonParam, rightSide);
			}
			else return null;

			if (UIElements != null) UIElements.Add(uiDynamic);
			return uiDynamic;
		}

		public static void SetInteractable(this UIDynamicSlider slider, bool val)
		{
			var img = slider.transform.Find("Panel").GetComponent<Image>();
			img.color = val? Color.white : img.color = Color.white * .8f;
			slider.transform.Find("ValueInputField").GetComponent<InputField>().interactable = val;
			slider.transform.Find("QuickButtonsGroup").gameObject.SetActive(val);
			slider.transform.Find("DefaultValueButton").gameObject.SetActive(val);
			slider.slider.interactable = val;
		}
		
		public static void SetInteractable(this JSONStorableFloat jFloat, bool val)
		{
			if (jFloat.slider == null) return;
			var parent = jFloat.slider.transform.parent;
			var img = parent.Find("Panel").GetComponent<Image>();
			img.color = val? Color.white : img.color = Color.white * .8f;
			parent.Find("ValueInputField").GetComponent<InputField>().interactable = val;
			parent.Find("QuickButtonsGroup").gameObject.SetActive(val);
			parent.Find("DefaultValueButton").gameObject.SetActive(val);
			jFloat.slider.interactable = val;
		}

		public static void StoreWithMinMax(this JSONStorableFloat jParam, JSONClass jc)
		{
			var jc1 = new JSONClass();
			jc1["min"].AsFloat = jParam.min;
			jc1["max"].AsFloat = jParam.max;
			jc1["val"].AsFloat = jParam.val;
			jc[jParam.name] = jc1;
		}
		
		public static bool LoadWithMinMax(this JSONStorableFloat jParam, JSONClass jc)
		{
			if (!jc.HasKey(jParam.name)) return false;
			var jc1 = jc[jParam.name].AsObject;
			if (jc1 == null) return false;
			jParam.min = jc1["min"].AsFloat;
			jParam.max = jc1["max"].AsFloat;
			jParam.val = jc1["val"].AsFloat;
			return true;
		}

		public static bool Store(this JSONStorableParam jsonParam, JSONClass jc, bool forceStore = true)
		{
			bool needsStore = false;
			if (jsonParam is JSONStorableBool)
			{
				JSONStorableBool jParam = (JSONStorableBool)jsonParam;
				if (forceStore || jParam.val != jParam.defaultVal)
				{
					jc[jParam.name].AsBool = jParam.val;
					needsStore = true;
				}
			}
			else if (jsonParam is JSONStorableFloat)
			{
				JSONStorableFloat jParam = (JSONStorableFloat)jsonParam;
				if (forceStore || jParam.val != jParam.defaultVal)
				{
					jc[jParam.name].AsFloat = jParam.val;
					needsStore = true;
				}
			}
			else if (jsonParam is JSONStorableString)
			{
				JSONStorableString jParam = (JSONStorableString)jsonParam;
				if (forceStore || jParam.val != jParam.defaultVal)
				{
					jc[jParam.name] = jParam.val;
					needsStore = true;
				}
			}
			else if (jsonParam is JSONStorableStringChooser)
			{
				JSONStorableStringChooser jParam = (JSONStorableStringChooser)jsonParam;
				if (forceStore || jParam.val != jParam.defaultVal)
				{
					jc[jParam.name] = jParam.val;
					needsStore = true;
				}
			}
			else if (jsonParam is JSONStorableColor)
			{
				JSONStorableColor jParam = (JSONStorableColor)jsonParam;
				JSONArray ja = new JSONArray();
				ja[0].AsFloat = jParam.val.H;
				ja[1].AsFloat = jParam.val.S;
				ja[2].AsFloat = jParam.val.V;
				jc[jParam.name] = ja;
				needsStore = true;
			}

			return needsStore;
		}

		public static void RegisterWithKeybingings(this JSONStorableAction jAction, List<object> keyBindings)
		{
			var script = UIManager.script;
			script.RegisterAction(jAction);
			keyBindings.Add(jAction);
		}
		
		public static void RegisterWithKeybingings(this JSONStorableBool jBool, List<object> keyBindings, bool managed = false)
		{
			var script = UIManager.script;
			script.RegisterBool(jBool);
			jBool.isStorable = managed;
			jBool.isRestorable = managed;
			JSONStorableAction toggleAction = new JSONStorableAction($"Toggle {jBool.name}", () => jBool.val = !jBool.val);
			keyBindings.Add(toggleAction);
		}

		public static void Load(this JSONStorableParam jsonParam, JSONClass jc, bool setMissingToDefault = false)
		{
			if (jsonParam is JSONStorableBool)
			{
				JSONStorableBool jParam = (JSONStorableBool)jsonParam;
				if (jc.HasKey(jParam.name)) jParam.val = jc[jParam.name].AsBool;
				else if(setMissingToDefault) jParam.SetValToDefault();
			}
			else if (jsonParam is JSONStorableFloat)
			{
				JSONStorableFloat jParam = (JSONStorableFloat)jsonParam;
				if (jc.HasKey(jParam.name)) jParam.val = jc[jParam.name].AsFloat;
				else if(setMissingToDefault) jParam.SetValToDefault();
			}
			else if (jsonParam is JSONStorableString)
			{
				JSONStorableString jParam = (JSONStorableString)jsonParam;
				if (jc.HasKey(jParam.name)) jParam.val = jc[jParam.name].Value;
				else if(setMissingToDefault) jParam.SetValToDefault();
			}
			else if (jsonParam is JSONStorableStringChooser)
			{
				JSONStorableStringChooser jParam = (JSONStorableStringChooser)jsonParam;
				if (jc.HasKey(jParam.name)) jParam.val = jc[jParam.name].Value;
				else if(setMissingToDefault) jParam.SetValToDefault();
			}
			else if (jsonParam is JSONStorableColor)
			{
				JSONStorableColor jParam = (JSONStorableColor)jsonParam;
				if (jc.HasKey(jParam.name))
				{
					HSVColor loadedColor = new HSVColor
					{
						H = jc[jParam.name][0].AsFloat,
						S = jc[jParam.name][1].AsFloat,
						V = jc[jParam.name][2].AsFloat
					};
					jParam.val = loadedColor;
				}
				else if(setMissingToDefault) jParam.SetValToDefault();
			}
		}

		public static void Register(this JSONStorableParam jsonParam, MVRScript script = null)
		{
			if (script == null) script = UIManager.script;
			if (jsonParam is JSONStorableBool)
			{
				JSONStorableBool jParam = jsonParam as JSONStorableBool;
				script.RegisterBool(jParam);
			}
			else if (jsonParam is JSONStorableFloat)
			{
				JSONStorableFloat jParam = jsonParam as JSONStorableFloat;
				script.RegisterFloat(jParam);
			}
			else if (jsonParam is JSONStorableString)
			{
				JSONStorableString jParam = jsonParam as JSONStorableString;
				script.RegisterString(jParam);
			}
			else if (jsonParam is JSONStorableStringChooser)
			{
				JSONStorableStringChooser jParam = jsonParam as JSONStorableStringChooser;
				script.RegisterStringChooser(jParam);
			}
		}
		
		public static void RegisterNonRestore(this JSONStorableParam jsonParam, MVRScript script = null)
		{
			if (script == null) script = UIManager.script;
			if (jsonParam is JSONStorableBool)
			{
				JSONStorableBool jParam = jsonParam as JSONStorableBool;
				script.RegisterBool(jParam);
			}
			else if (jsonParam is JSONStorableFloat)
			{
				JSONStorableFloat jParam = jsonParam as JSONStorableFloat;
				script.RegisterFloat(jParam);
			}
			else if (jsonParam is JSONStorableString)
			{
				JSONStorableString jParam = jsonParam as JSONStorableString;
				script.RegisterString(jParam);
			}
			else if (jsonParam is JSONStorableStringChooser)
			{
				JSONStorableStringChooser jParam = jsonParam as JSONStorableStringChooser;
				script.RegisterStringChooser(jParam);
			}
			jsonParam.isStorable = false;
			jsonParam.isRestorable = false;
		}
		
		public static void Deregister(this JSONStorableParam jsonParam, MVRScript script = null)
		{
			if (script == null) script = UIManager.script;
			if (jsonParam is JSONStorableBool)
			{
				JSONStorableBool jParam = jsonParam as JSONStorableBool;
				script.DeregisterBool(jParam);
			}
			else if (jsonParam is JSONStorableFloat)
			{
				JSONStorableFloat jParam = jsonParam as JSONStorableFloat;
				script.DeregisterFloat(jParam);
			}
			else if (jsonParam is JSONStorableString)
			{
				JSONStorableString jParam = jsonParam as JSONStorableString;
				script.DeregisterString(jParam);
			}
			else if (jsonParam is JSONStorableStringChooser)
			{
				JSONStorableStringChooser jParam = jsonParam as JSONStorableStringChooser;
				script.DeregisterStringChooser(jParam);
			}
		}

		public static void DeregisterToggle(this JSONStorableBool param)
		{
			param.toggle.onValueChanged.RemoveAllListeners();
			param.toggle = null;
		}

		public static void Link(this JSONStorableFloat driver, JSONStorableFloat second)
		{
			driver.setCallbackFunction = second.SetVal;
			// second.setCallbackFunction = val => driver.val = val;
			driver.valNoCallback = second.val;
		}
	}

}

