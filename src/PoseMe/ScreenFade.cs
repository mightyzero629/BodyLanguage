using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CheesyFX
{
	
    public class ScreenFade
    {
	    private static GameObject myWindowCamera;
	    private static GameObject myMonitorRig;
	    private static Mesh myMesh;
	    private static Material myMaterial;
	    private static Color myColor = Color.black;
	    private float fadeClock = 0.0f;
	    private JSONStorableBool myActive;
	    private JSONStorableFloat timer;

	    private static IEnumerator fade;
	    
        public void Init()
        {
            // Utils.SetupInfoText(this,
            //     "<color=#606060><size=40><b>ScreenFade</b></size>\nLogicBrick that can fade your scene on the screen to the specified color. Among other things, helpful for nice scene transitions, covering up loading sequences.\n\n" + 
            //     "<b>Notes:</b>\n" + 
            //     "- Does NOT affect VaM's main UI.\n" + 
            //     "- Does affect UI placed in the scene, e.g. UIButton atom.\n" + 
            //     "- Does affect the green/red target gizmos in edit mode, making it hard to select things by clicking them. Use the selection menu instead.\n" + 
            //     "- Can only affect only a single camera. If the WindowCamera is active, it applies to that.</color>",
            //     600.0f, true
            // );
			
			
            GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            myMesh = prefab.GetComponent<MeshFilter>().mesh;
            Object.DestroyImmediate(prefab);
			
            Shader[] shaders = Resources.FindObjectsOfTypeAll<Shader>();
            Shader shader = Array.Find(shaders, s => s.name == "Custom/TransparentHUD");
            myMaterial = new Material(shader);
            myMaterial.renderQueue = 10000;
			
            Atom windowCameraAtom = SuperController.singleton.GetAtoms().Find(a => a.type == "WindowCamera");
            CameraControl windowCamera = windowCameraAtom?.GetStorableByID("CameraControl") as CameraControl;
            myWindowCamera = windowCamera?.cameraGroup.gameObject;
			
            myMonitorRig = SuperController.singleton.MonitorRig.gameObject;

            // myActive = Utils.SetupToggle(this, "Active", false, false);
            // myTime = Utils.SetupSliderFloatWithRange(this, "Time (sec)", 1, 0, 10, false);
            // myTime.constrained = false;
			         //
            // JSONStorableColor colorUI = Utils.SetupColor(this, "Color", myColor, false);
            // colorUI.setCallbackFunction += (h, s, v) => {	myColor = HSVColorPicker.HSVToRGB(h, s, v);	};
        }
        
        private void Destroy()
        {
	        Object.Destroy(myMaterial);
	        myMaterial = null;
	        myMesh = null;
        }

        public static void Start()
        {
	        fade.Stop();
	        fade = Fade().Start();
        }

        private static IEnumerator Fade()
        {
	        myColor.a = Mathf.SmoothStep(0.0f, 1.0f, fadeClock);
	        myMaterial.color = myColor;			
				
	        bool usingWindowCamera = myWindowCamera != null && myWindowCamera.activeSelf &&	!myMonitorRig.activeSelf;
	        Transform transform = usingWindowCamera ? myWindowCamera.transform : CameraTarget.centerTarget.transform;
	        Graphics.DrawMesh(myMesh, transform.localToWorldMatrix, myMaterial, PoseMe.singleton.gameObject.layer, null, 0, null, castShadows: false, receiveShadows: false);
        }
    }
}