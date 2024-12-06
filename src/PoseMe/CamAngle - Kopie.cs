using System;
using System.Collections;
using MacGruber;
using MVR.FileManagementSecure;
using SimpleJSON;
using UnityEngine;

namespace CheesyFX
{
    public class CamAngle
    {
        private Pose pose;
        public int id;
        public byte[] img;
        private Vector3 rigPosition;
        private float rigRotation;
        private float camRotation;
        private float height;
        private float focus;
        public bool hasCam = true;
        
        public EventTrigger onAngleEnter;
        public EventTrigger onAngleExit;

        public CamAngle(Pose pose, byte[] img)
        {
            this.pose = pose;
            this.img = img;
            id = pose.camAngles.Count;
            GetAngle();
            onAngleEnter = new EventTrigger(PoseMe.singleton, "On Angle Enter");
            onAngleExit = new EventTrigger(PoseMe.singleton, "On Angle Exit");
            PoseMe.onAngleEnterTriggerButton.button.onClick.RemoveAllListeners();
            PoseMe.onAngleExitTriggerButton.button.onClick.RemoveAllListeners();
            PoseMe.onAngleEnterTriggerButton.button.onClick.AddListener(onAngleEnter.OpenPanel);
            PoseMe.onAngleExitTriggerButton.button.onClick.AddListener(onAngleExit.OpenPanel);
        }
        
        public CamAngle(Pose pose, JSONClass jc)
        {
            this.pose = pose;
            id = pose.camAngles.Count;
            onAngleEnter = new EventTrigger(PoseMe.singleton, "On Angle Enter");
            onAngleExit = new EventTrigger(PoseMe.singleton, "On Angle Exit");
            PoseMe.onAngleEnterTriggerButton.button.onClick.RemoveAllListeners();
            PoseMe.onAngleExitTriggerButton.button.onClick.RemoveAllListeners();
            PoseMe.onAngleEnterTriggerButton.button.onClick.AddListener(onAngleEnter.OpenPanel);
            PoseMe.onAngleExitTriggerButton.button.onClick.AddListener(onAngleExit.OpenPanel);
            onAngleEnter.RestoreFromJSON(jc, PoseMe.singleton.subScenePrefix, false, true);
            onAngleExit.RestoreFromJSON(jc, PoseMe.singleton.subScenePrefix, false, true);
            Load(jc);
        }

        private void GetAngle()
        {
            rigPosition = pose.storedWorldToLocalMatrix.MultiplyPoint(PoseMe.cameraRig.position);
            rigRotation = (pose.storedWorldToLocalMatrix.rotation * PoseMe.cameraRig.rotation).eulerAngles.y;
            camRotation = PoseMe.camera.localEulerAngles.x;
            height = SuperController.singleton.playerHeightAdjust;
            focus = SuperController.singleton.focusDistance;
            hasCam = true;
        }

        public void Update(byte[] img)
        {
            GetAngle();
            this.img = img;
        }

        public void Apply()
        {
            // PoseMe.camera.position.y.Print();
            if(!hasCam) return;
            Pose.smoothCam.Stop();
            var refMatrix = pose.refMatrix;
            // refMatrix.Print();
            var rY = Quaternion.Euler(new Vector3(0f, refMatrix.rotation.eulerAngles.y, 0f));
            var mtx = Matrix4x4.TRS(refMatrix.GetColumn(3), rY, Vector3.one);
            var rigPos = mtx.MultiplyPoint(rigPosition);
            var hght = height + refMatrix.GetColumn(3)[1] + rigPosition.y;
            // hght += rigPos.y;
            rigPos.y = 0f;
            // mtx.Print();
            // $"{rigPosition} {rigPos}".Print();
            // $"{refMatrix.GetColumn(3)[1]} {rigPosition.y}".Print();
            
            var p0 = PoseMe.camera.position;
            PoseMe.cameraRig.position.Print();
            var end = rigPos + new Vector3(0f, hght+1.6f, 0f);
            PoseMe.test1.transform.position = p0;
            var p1 = PoseMe.test2.transform.position = .5f * (p0 + end) + 1f*Vector3.Cross(end - p0, Vector3.Cross(end - p0, PoseMe.camera.forward));
            // PoseMe.test2.transform.position = .5f * (start + end);
            PoseMe.test3.transform.position = end;
            // hght.Print();
            p0.Print();
            bezier.p0 = p0;
            bezier.p1 = p1;
            bezier.p2 = end;
            // bezier.Evaluate(0f).Print();
            
            var rigRot = refMatrix.rotation * Quaternion.Euler(0f, rigRotation, 0f);
            switch (PoseMe.camMode.val)
            {
                case "None":
                {
                    break;
                }
                case "Exponential":
                {
                    Pose.smoothCam = SmoothCamRestoreExp(rigPos, rigRot.eulerAngles.y, camRotation, hght, focus).Start();
                    break;
                }
                case "Linear":
                {
                    Pose.smoothCam = SmoothCamRestoreLin(rigPos, rigRot.eulerAngles.y, camRotation, hght, focus).Start();
                    break;
                }
                default:
                {
                    PoseMe.cameraRig.position = rigPos;
                    PoseMe.cameraRig.rotation = Quaternion.Euler(0f, rigRot.eulerAngles.y, 0f);;
                    PoseMe.camera.localEulerAngles = new Vector3(camRotation,0f,0f);
                    SuperController.singleton.playerHeightAdjust = hght;
                    SuperController.singleton.focusDistance = focus;
                    break;
                }
            }
            pose.currentAngle = this;
            PoseMe.angleChooser.valNoCallback = id.ToString();
            pose.SetPreviewImage(img);
            pose.SetButtonText();
            if(pose.currentAngle != null && pose.currentAngle != this) pose.currentAngle.onAngleExit.Trigger();
            onAngleEnter.Trigger();
            PoseMe.onAngleEnterTriggerButton.button.onClick.RemoveAllListeners();
            PoseMe.onAngleExitTriggerButton.button.onClick.RemoveAllListeners();
            PoseMe.onAngleEnterTriggerButton.button.onClick.AddListener(onAngleEnter.OpenPanel);
            PoseMe.onAngleExitTriggerButton.button.onClick.AddListener(onAngleExit.OpenPanel);

            

        }

        public static Bezier bezier = new Bezier();
        
        private IEnumerator SmoothCamRestoreExp(Vector3 rigPos, float rigRotY, float camRotX, float height, float focus)
        {
            var sc = SuperController.singleton;
            var deltaT = PoseMe.smoothCamSpeed.val * Time.fixedDeltaTime;
            var curRigRot = PoseMe.cameraRig.eulerAngles.y;
            var rigRot = Quaternion.Euler(0f, rigRotY, 0f);
            var camRot = Quaternion.Euler(camRotX, 0f, 0f);
            float t = 0f;
            while (!smoothCamAbort && (Mathf.Abs(Quaternion.Dot(camRot, PoseMe.camera.localRotation)) < .99999999f  ||
                                       Vector3.SqrMagnitude(PoseMe.cameraRig.position - rigPos) > 1e-4f ||
                                       Mathf.Abs(Quaternion.Dot(rigRot, PoseMe.cameraRig.rotation)) < .99999999f  ||
                                       Mathf.Abs(sc.playerHeightAdjust - height) > 1e-3f ||
                                       Mathf.Abs(sc.focusDistance - focus) > 1e-3f))
            {
                // PoseMe.cameraRig.position = Vector3.Lerp(PoseMe.cameraRig.position, rigPos, 1.25f*deltaT);
                t = Mathf.Lerp(t, 1f, 1.25f * deltaT);
                var p = bezier.Evaluate(t);
                p.y = 0f;
                // $"{p} {rigPos}".Print();
                PoseMe.cameraRig.position = p;
                PoseMe.cameraRig.rotation = Quaternion.Slerp(PoseMe.cameraRig.rotation, rigRot, deltaT);
                PoseMe.camera.localRotation = Quaternion.Slerp(PoseMe.camera.localRotation, camRot, deltaT);
                sc.playerHeightAdjust = Mathf.Lerp(sc.playerHeightAdjust, height, deltaT);
                sc.focusDistance = Mathf.Lerp(sc.focusDistance, focus, deltaT);
                yield return new WaitForFixedUpdate();
            }
        }
        
        private IEnumerator SmoothCamRestoreLin(Vector3 rigPos, float rigRotY, float camRotX, float height, float focus)
        {
            var rigPosCur = PoseMe.cameraRig.position;
            var rigRotCur = PoseMe.cameraRig.rotation;
            var camRotCur = PoseMe.camera.localRotation;
            var heightCur = SuperController.singleton.playerHeightAdjust;
            var focusCur = SuperController.singleton.focusDistance;
            var rigRot = Quaternion.Euler(0f, rigRotY, 0f);
            var camRot = Quaternion.Euler(camRotX, 0f, 0f);
            float t = 0f;
            while (!smoothCamAbort && t < 1f)
            {
                t += .5f*PoseMe.smoothCamSpeed.val*Time.fixedDeltaTime;
                if (t > 1f) t = 1f;
                PoseMe.cameraRig.position = Vector3.Lerp(rigPosCur, rigPos, t);
                PoseMe.cameraRig.rotation = Quaternion.Slerp(rigRotCur, rigRot, t);
                PoseMe.camera.localRotation = Quaternion.Slerp(camRotCur, camRot, t);
                SuperController.singleton.playerHeightAdjust = Mathf.Lerp(heightCur, height, t);
                SuperController.singleton.focusDistance = Mathf.Lerp(focusCur, focus, t);
                yield return new WaitForFixedUpdate();
            }
        }
        
        private bool smoothCamAbort => !PoseMe.buttonHovered && 
                                       (Input.GetKeyDown(KeyCode.Mouse1) ||
                                       Input.GetKeyDown(KeyCode.Mouse2) ||
                                       Input.GetAxis("Mouse ScrollWheel") != 0f ||
                                       Input.GetKeyDown(KeyCode.Tab) ||
                                       Input.GetKeyDown(KeyCode.W) ||
                                       Input.GetKeyDown(KeyCode.A) ||
                                       Input.GetKeyDown(KeyCode.S) ||
                                       Input.GetKeyDown(KeyCode.D));
        
        public JSONClass Store()
        {
            var jc = new JSONClass
            {
                ["image"] = Convert.ToBase64String(img),
                ["rigPosition"] = rigPosition.ToJA(),
                ["rigRotation"] = {AsFloat = rigRotation},
                ["cameraRotation"] = {AsFloat = camRotation},
                ["playerHeight"] = {AsFloat = height},
                ["focusDistance"] = {AsFloat = focus},
                [onAngleEnter.Name] = onAngleEnter.GetJSON(PoseMe.singleton.subScenePrefix),
                [onAngleExit.Name] = onAngleExit.GetJSON(PoseMe.singleton.subScenePrefix),
            };
            return jc;
        }

        public void Load(JSONClass jc)
        {
            if (jc.HasKey("image")) img = Convert.FromBase64String(jc["image"]);
            rigPosition = jc["rigPosition"].AsArray.ToV3();
            rigRotation = jc["rigRotation"].AsFloat;
            camRotation = jc["cameraRotation"].AsFloat;
            height = jc["playerHeight"].AsFloat;
            focus = jc["focusDistance"].AsFloat;
            onAngleEnter.RestoreFromJSON(jc, PoseMe.singleton.subScenePrefix, false, true);
            onAngleExit.RestoreFromJSON(jc, PoseMe.singleton.subScenePrefix, false, true);
        }
    }
}