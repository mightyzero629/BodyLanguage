using System;
using System.Collections.Generic;
using System.Linq;
using MVR.FileManagementSecure;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CheesyFX
{
    public static class XRay
    {
        private static GameObject extraCameraContainer;

        public static Camera overlayCamera;
        // Layer to use as a culling mask for the extra camera
        // 18 appears unused 
        public static int layer = 18; 
        static int cullingMask = 1 << layer;
        public static List<XRayClient> clients = new List<XRayClient>();

        private static Dictionary<Camera, int> modifiedCullingMasks = new Dictionary<Camera, int>();
        public static JSONStorableBool enabled = new JSONStorableBool("XRay Enabled (Global)", true, SetActive);
        private static UnityEventsListener onLowResScreenshot;
        private static UnityEventsListener onHighResScreenshot;
        public static Texture2D[] alphaTextures = new Texture2D[4];

        // This class is based on the work of @Stopper.
        public static void Init()
        {
            // extraCameraContainer = GetOrCreateChildObject(Camera.main.gameObject, "extraCamera");
            extraCameraContainer = new GameObject("extraCameraContainer");
            // extraCameraContainer.transform.parent = FillMeUp.singleton.transform;
			
            // Disable rendering of our layer for the main camera and link to it
            Camera mainCamera = null;
            if(SuperController.singleton.isOpenVR) {
                mainCamera = SuperController.singleton.ViveCenterCamera;
            } else if (SuperController.singleton.isOVR) {
                mainCamera = SuperController.singleton.OVRCenterCamera;
            } else {
                mainCamera = SuperController.singleton.MonitorCenterCamera;
            }

            ConnectToCamera(mainCamera);
            LoadAlphaTextures();
            onLowResScreenshot = SuperController.singleton.screenshotPreview.gameObject.AddComponent<UnityEventsListener>();
            onHighResScreenshot = SuperController.singleton.hiResScreenshotPreview.gameObject.AddComponent<UnityEventsListener>();
            onLowResScreenshot.onEnabled.AddListener(() => ToggleInternal(false));
            onLowResScreenshot.onDisabled.AddListener(() => ToggleInternal(true));
            onHighResScreenshot.onEnabled.AddListener(() => ToggleInternal(false));
            onHighResScreenshot.onDisabled.AddListener(() => ToggleInternal(true));
            
            SuperController.singleton.monitorCameraFOVSlider.onValueChanged.AddListener(SyncFOV);
            FillMeUp.singleton.RegisterBool(enabled);
        }

        private static void SyncFOV(float val)
        {
            if(overlayCamera == null) return;
            overlayCamera.fieldOfView = val;
        }

        public static void LogOutClient()
        {
            if(overlayCamera == null) return;
            if (clients.All(x => x.meshContainer != null && !x.meshContainer.gameObject.activeSelf)) overlayCamera.enabled = false;
        }

        public static XRayClient RegisterClient(StimReceiver stimReceiver)
        {
            var client = new XRayClient(stimReceiver);
            clients.Add(client);
            return client;
        }
        
        public static void DeregisterClient(XRayClient client)
        {
            // "DeregisterClient".Print();
            if (client == null) return;
            clients.Remove(client);
            client.Destroy();
            LogOutClient();
        }

        public static void SyncSkins()
        {
            for (int i = 0; i < clients.Count; i++)
            {
                clients[i].SyncSkin();
            }
        }
        
        public static void ConnectToCamera(Camera camera){
            extraCameraContainer.transform.parent = camera.gameObject.transform;
            overlayCamera = extraCameraContainer.AddComponent<Camera>();
            overlayCamera.CopyFrom(camera);
            overlayCamera.enabled = false;
            overlayCamera.clearFlags = CameraClearFlags.Depth;
            overlayCamera.depth = camera.depth + 1;
            overlayCamera.cullingMask = cullingMask;
			
            // Disable the culling mask for the camera.
            modifiedCullingMasks[camera] = camera.cullingMask;
            camera.cullingMask &= ~cullingMask; 
        }

        public static GameObject GetOrCreateChildObject(GameObject parent, string name) {
            GameObject gameObject = FindGameObject(parent, name);
            if(gameObject == null) {
                gameObject = new GameObject(name);
                gameObject.transform.parent = parent.transform;
            } 
			
            return gameObject;
        }
        
        private static GameObject FindGameObject(GameObject parent, string name){
            Transform childTransform = parent.transform.Find(name);
			
            if(childTransform == null){
                return null;
            } else {
                return childTransform.gameObject;
            }
        }

        private static void ToggleInternal(bool val)
        {
            SetActive(enabled.val && val);
        }
        
        public static void Toggle()
        {
            enabled.val = !enabled.val;
        }
        
        private static void SetActive(bool val)
        {
            if (val)
            {
                for (int i = 0; i < FillMeUp.orifices.Count; i++)
                {
                    var orifice = FillMeUp.orifices[i];
                    if (orifice.penetrator?.type == 1)
                    {
                        var person = orifice.penetrator.stimReceiver as Person;
                        if (person.xrayClient != null)
                        {
                            person.xrayClient.Enable(orifice);
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < clients.Count; i++)
                {
                    clients[i].Disable();
                }
                foreach(var entry in modifiedCullingMasks){
                    entry.Key.cullingMask = entry.Value;
                }
            }
        }

        private static void LoadAlphaTextures()
        {
            var files = FileManagerSecure.GetFiles($"{FillMeUp.packageUid}Custom/Scripts/CheesyFX/BodyLanguage/XRayTextures/");
            for (int i = 0; i < files.Length; i++)
            {
                var path = FileManagerSecure.NormalizePath(files[i]);
                alphaTextures[i] = LoadTexture(path);
            }
        }
        
        public static Texture2D LoadTexture(string path)
        {
            var tex = new Texture2D(2, 2);
            var data = FileManagerSecure.ReadAllBytes(path);
            tex.LoadImage(data);
            tex.Apply();
            return tex;
        }

        public static void Destroy()
        {
            try
            {
                // "Destroy".Print();
                for (int i = 0; i < clients.Count; i++)
                {
                    clients[i]?.Destroy();
                }
                foreach(var entry in modifiedCullingMasks){
                    entry.Key.cullingMask = entry.Value;
                }
                Object.DestroyImmediate(extraCameraContainer);
                Object.Destroy(onLowResScreenshot);
                Object.Destroy(onHighResScreenshot);
                SuperController.singleton.monitorCameraFOVSlider.onValueChanged.RemoveListener(SyncFOV);
            }
            catch (Exception e)
            {
                SuperController.LogError("XRay:Destroy "+e.ToString());
            }
        }
        
    }
}