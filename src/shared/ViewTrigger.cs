using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CheesyFX
{
    public static class ViewTrigger
	{
		public static bool triggerObjectExists;
		public static JSONStorableFloat width = new JSONStorableFloat("Trigger Width", .1f, SetScale, 0f, 2f, false, true);
		public static JSONStorableFloat height = new JSONStorableFloat("Trigger Height", .2f, SetScale, 0f, 2f, false, true);
		// public static JSONStorableFloat triggerDepth = new JSONStorableFloat("Trigger Height", 10f, ScaleAdjust, 0f, 20f, false, true);
		private static float _depth = 10f;
		public static float triggerDepth
		{
			set
			{
				if (value < 0)
				{
					_depth = 10f;
					SetTriggerDepth();
					return;
				}
				if (Math.Abs(value - _depth) < .01f) return;
				// value.Print();
				_depth = value;
				SetTriggerDepth();
			}
		}
		public static JSONStorableBool showTrigger = new JSONStorableBool("Show Trigger", false, ToggleDebugLines);
		
		public static GameObject triggerObject;
		public static List<GameObject> debugLines = new List<GameObject>();
		public static Collider collider;
		private static int _uses;
		public static int uses
		{
			get { return _uses;}
			set
			{
				if (value <= 0)
				{
					_uses = 0;
					Destroy();
					return;
				}
				_uses = value;
				_uses.Print();
				if (!triggerObjectExists) CreateTriggerObject();
			}
		}
		// public ViewTriggerEvaluation triggerEvaluation;
		// public static ViewTrigger(){
		// 	triggerWidth = new JSONStorableFloat("Trigger Width", .1f, ScaleAdjust, 0f, 10f, false, true);
		// 	triggerHeight = new JSONStorableFloat("Trigger Height", .1f, ScaleAdjust, 0f, 10f, false, true);
		// 	triggerDepth = new JSONStorableFloat("Trigger Height", 10f, ScaleAdjust, 0f, 20f, false, true);
		// 	showTrigger = new JSONStorableBool("Show Trigger", false, ToggleDebugLines);
		// 	// CreateTriggerObject();
		// }
		
		
		public static GameObject CreateTriggerObject(){
			Vector3 scale = new Vector3(width.val, height.val, _depth);
			Transform cameraTransform = SuperController.singleton.lookCamera.transform;
			triggerObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
			triggerObject.name = "ViewTrigger";
			Renderer renderer = triggerObject.GetComponent<Renderer>();
			Material material = new Material(Shader.Find("Transparent/Diffuse"));
            triggerObject.transform.position = cameraTransform.position;
            triggerObject.transform.parent = cameraTransform;
            triggerObject.transform.localScale = scale;
			triggerObject.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
			triggerObject.transform.localPosition = new Vector3(0f, 0f, scale.z / 2);
			triggerObject.name = "ViewTrigger";
			collider = triggerObject.GetComponent<Collider>();
			collider.name = "ViewTrigger";
            collider.enabled = true;
			collider.isTrigger = true;
			// triggerEvaluation = triggerObject.AddComponent<ViewTriggerEvaluation>();
            // triggerObject.transform.localPosition = new Vector3(0f, -ShoeColliderBaseScale / 2, HeelColliderInitialBackwardOffset);
            
			
            // renderer.material.shader = Shader.Find("Transparent/Diffuse");
			
            // renderer.material.color = Color.red;
			renderer.material = material;
			renderer.enabled = false;
			ToggleDebugLines(showTrigger.defaultVal);
			IgnoreCollisionWithTriggers();
			triggerObjectExists = true;
			return triggerObject;
		}

		static void IgnoreCollisionWithTriggers(){
			foreach(Atom atom in SuperController.singleton.GetAtoms()){
				foreach(Rigidbody rb in atom.rigidbodies){
					Collider col = rb.GetComponent<Collider>();
					if(col != null && col.isTrigger) Physics.IgnoreCollision(col, collider);
				}
			}
		}

		static void SetScale(JSONStorableFloat axis){
			Vector3 scale = triggerObject.transform.localScale;
			if(axis == width) scale.x = axis.val;
			if(axis == height) scale.y = axis.val;
			// if(axis == triggerDepth) scale.z = axis.val;
			triggerObject.transform.localScale = scale;
			if(showTrigger.val){
				ToggleDebugLines(false);
				ToggleDebugLines(true);
			}
		}

		public static void SetTriggerDepth()
		{
			Vector3 scale = triggerObject.transform.localScale;
			triggerObject.transform.localScale = new Vector3(scale.x, scale.y, _depth);
			triggerObject.transform.localPosition = new Vector3(0f, 0f, _depth / 2);
			if(showTrigger.val){
				ToggleDebugLines(false);
				ToggleDebugLines(true);
			}
			// _triggerDepth.Print();
		}

		static void ToggleDebugLines(bool enabled){
			if(enabled){
				// Transform cT = SuperController.singleton.lookCamera.transform;
				// List<Vector3> directions = new List<Vector3>{
				// 	new Vector3(triggerWidth.val, 0f, 0f),
				// 	new Vector3(triggerHeight.val, 0f, 0f)
				// };
				Vector3 d1 = new Vector3(0f, 0f, _depth);
				List<Vector3> startPositions = new List<Vector3>{
					new Vector3(-width.val/2f, height.val/2f, 0f),
					new Vector3(width.val/2f, height.val/2f, 0f),
					new Vector3(width.val/2f, -height.val/2f, 0f),
					new Vector3(-width.val/2f, -height.val/2f, 0f),
					new Vector3(-width.val/2f, 0f, 0f),
					new Vector3(0f, height.val/2f, 0f),
					new Vector3(width.val/2f, 0f, 0f),
					new Vector3(0f, -height.val/2f, 0f),
				};
				startPositions.ForEach(x => CreateDebugLine(x, d1));
			}
			else{
				debugLines.ForEach(x => Object.Destroy(x));
				// triggerObject.GetComponent<Renderer>().enabled = false;
			}
		}

		public static void Destroy(){
			Object.Destroy(triggerObject);
			showTrigger.val = false;
			triggerObjectExists = false;
		}

		static void CreateDebugLine(Vector3 start, Vector3 direction)
		{
			Transform cameraTransform = SuperController.singleton.lookCamera.transform;
			GameObject myLine = new GameObject();
			myLine.transform.position = cameraTransform.position;
			myLine.transform.parent = cameraTransform;
			myLine.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
			LineRenderer lr = myLine.AddComponent<LineRenderer>();
			lr.useWorldSpace = false;
			// LineRenderer lr = myLine.GetComponent<LineRenderer>();
			// lr.material = new Material(Shader.Find("Particles/Alpha Blended Premultiply"));
			// Color color = new Color(1f, 0f, 0f);
			lr.SetColors(Color.red, Color.yellow);
			lr.SetWidth(0.001f, 0.002f);
			lr.SetPosition(0, start);
			lr.SetPosition(1, start+direction);
			
			// GameObject.Destroy(myLine, duration);
			debugLines.Add(myLine);
		}
	}

    // public static class ViewTriggerExtension
    // {
	   //  public static ViewTrigger AddViewTrigger(this GameObject obj)
	   //  {
		  //   ViewTrigger viewTrigger = obj.GetComponent<ViewTrigger>();
		  //   SuperController.LogMessage(viewTrigger == null? "ViewTrigger not found" : "ViewTrigger found");
		  //   if(viewTrigger == null) viewTrigger = obj.AddComponent<ViewTrigger>();
		  //   return viewTrigger;
	   //  }
    // }
}