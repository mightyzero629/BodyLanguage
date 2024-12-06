using System;
using System.Collections;
using System.Collections.Generic;
using MacGruber;
using SimpleJSON;
using UnityEngine;

namespace CheesyFX
{
    public class CamAngle
    {
        private Pose pose;
        public int id;
        public byte[] img;
        private Vector3 camPosition;
        // private float rigRotation;
        private float camRotation;
        private float focus;
        public bool hasAngle;
        public bool hasImage;
        
        public EventTrigger onCamEnter;
        public EventTrigger onCamExit;

        private static SuperController sc = SuperController.singleton;
        private static WaitForFixedUpdate wait = new WaitForFixedUpdate();
        
        public JSONStorableBool cumMales = new JSONStorableBool("Cam Cum Males", false);
        public JSONStorableBool cumFemale = new JSONStorableBool("Cam Cum Female", false);
        public JSONStorableFloat dialogPoolLevel = new JSONStorableFloat("Dialog Pool Level", -1f, -1f, 2f);
        
        public DialogSet dialogs = new DialogSet();
        public JSONStorableAction timelineClip;

        public CamAngle(Pose pose, byte[] img)
        {
            this.pose = pose;
            this.img = img;
            if (this.img != null) hasImage = true;
            id = pose.camAngles.Count;
            GetAngle();
            onCamEnter = new EventTrigger(PoseMe.singleton, "On Angle Enter");
            onCamExit = new EventTrigger(PoseMe.singleton, "On Angle Exit");
            PoseMe.SyncCamActionsUI(this);
        }
        
        public CamAngle(Pose pose, JSONClass jc)
        {
            this.pose = pose;
            id = pose.camAngles.Count;
            onCamEnter = new EventTrigger(PoseMe.singleton, "On Angle Enter");
            onCamExit = new EventTrigger(PoseMe.singleton, "On Angle Exit");
            PoseMe.SyncCamActionsUI(this);
            Load(jc);
        }

        private Quaternion rigQuat;
        private void GetAngle()
        {
            camPosition = pose.storedWorldToLocalMatrix.MultiplyPoint(GetCamPosition());
            rigQuat = (pose.storedWorldToLocalMatrix.rotation * PoseMe.cameraRig.rotation);
            camRotation = PoseMe.camera.localEulerAngles.x;
            focus = SuperController.singleton.focusDistance;
            hasAngle = true;
        }

        public void Update(byte[] img)
        {
            GetAngle();
            this.img = img;
            hasImage = true;
        }

        public void Apply(bool poseChanged = false)
        {
            try
            {
                if(!hasAngle) return;
                var last = PoseMe.currentPose.currentCam;
                if(!poseChanged && pose.currentCam != this)
                {
                    if (!PoseMe.ignoreTriggers.val) last.onCamExit.Trigger();
                    last.InvokeDialogs(false);
                }
                Pose.smoothCam.Stop();
                var refMatrix = pose.refMatrix;
                var q = Quaternion.Euler(
                            new Vector3(0f, (refMatrix.rotation * pose.storedWorldToLocalMatrix.rotation).eulerAngles.y, 0f)) *
                        Quaternion.Inverse(pose.storedWorldToLocalMatrix.rotation);
                var mtx = Matrix4x4.TRS(refMatrix.GetColumn(3), q, Vector3.one);
                var camPos = mtx.MultiplyPoint(camPosition);
                var rigRot = (mtx.rotation * rigQuat);
                switch (PoseMe.camMode.val)
                {
                    case "None":
                    {
                        break;
                    }
                    case "Exponential":
                    {
                        Pose.smoothCam = SmoothCamRestoreExp(camPos, rigRot.eulerAngles.y, camRotation, focus).Start();
                        break;
                    }
                    case "Bezier":
                    {
                        bezier.p0 = GetCamPosition();
                        bezier.p2 = camPos;
                        var delta = (bezier.p2 - bezier.p0).normalized;
                
                        var dot = Vector3.Dot(delta, PoseMe.camera.forward);
                        var dot3 = dot * dot * dot;
                        var cross = Vector3.Cross(delta, Vector3.Cross(delta, PoseMe.camera.forward));
                        if (cross != Vector3.zero)
                        {
                            bezier.p1 = .5f * (bezier.p0 + bezier.p2) + dot3 * PoseMe.smoothCamBezierStrength.val * cross.normalized;
                        }
                        else bezier.p1 = .5f * (bezier.p0 + bezier.p2);
                        Pose.smoothCam = SmoothCamRestoreBezier(rigRot.eulerAngles.y, camRotation, focus, 1f+Mathf.Min(.3f, Mathf.Abs(dot3*dot3*dot3))).Start();
                        break;
                    }
                    case "Linear":
                    {
                        Pose.smoothCam = SmoothCamRestoreLin(camPos, rigRot.eulerAngles.y, camRotation, focus).Start();
                        break;
                    }
                    default:
                    {
                        PoseMe.cameraRig.rotation = Quaternion.Euler(0f, rigRot.eulerAngles.y, 0f);;
                        PoseMe.camera.localEulerAngles = new Vector3(camRotation,0f,0f);
                        sc.focusDistance = focus;
                        SetCamPosition(camPos);
                        sc.SyncMonitorRigPosition();
                        break;
                    }
                }
                pose.currentCam = this;
                
                PoseMe.camChooser.valNoCallback = id.ToString();
                if(hasImage) pose.SetUIImage(img);
                pose.SetButtonText();
                if(!poseChanged)
                {
                    InvokeDialogs(true);
                    if (!PoseMe.ignoreTriggers.val) onCamEnter.Trigger();
                }
                PoseMe.SyncCamActionsUI(this);
                if (cumMales.val) ReadMyLips.orgasmMalesNow.actionCallback.Invoke();
                if(cumFemale.val) ReadMyLips.orgasmNow.actionCallback.Invoke();
                CreateBubbleItems();
                Story.currentLevel?.SyncButtons();
                
                if(!poseChanged && PoseMe.GetTimeline())
                {
                    if (timelineClip != null)
                    {
                        if(timelineClip.name != last.timelineClip?.name)
                        {
                            timelineClip.actionCallback.Invoke();
                        }
                    }
                    else
                    {
                        PoseMe.timelineStop.Invoke();
                        PoseMe.timeline.SetFloatParamValue("Scrubber", 0f);
                    }
                }
                
                
                // id.Print();
            }
            catch (Exception e)
            {
                SuperController.LogError(e.ToString());
            }

        }
        
        public void CreateBubbleItems()
        {
            if(Dialog.configureUIOpen) Dialog.CloseConfigureUI();
            if (PoseMe.currentTab != 1) return;
            PoseMe.singleton.RemoveUIElements(PoseMe.camBubbleItems);
            dialogs.Sort();
            for (int i = 0; i < dialogs.Count; i++)
            {
                PoseMe.camBubbleItems.Add(PoseMe.CreateDialogUIItem(dialogs[i], true));
            }
        }

        public static Bezier bezier = new Bezier();
        
        private IEnumerator SmoothCamRestoreExp(Vector3 camPos, float rigRotY, float camRotX, float focus)
        {
            var sc = SuperController.singleton;
            var deltaT = PoseMe.smoothCamSpeedJ.val * Time.fixedDeltaTime;
            var rigRot = Quaternion.Euler(0f, rigRotY, 0f);
            var camRot = Quaternion.Euler(camRotX, 0f, 0f);
            var curCamPos = GetCamPosition();
            float t = 0f;
            while (!smoothCamAbort && (Mathf.Abs(Quaternion.Dot(camRot, PoseMe.camera.localRotation)) < .99999999f  ||
                                       Vector3.SqrMagnitude(curCamPos - camPos) > 1e-4f ||
                                       Mathf.Abs(Quaternion.Dot(rigRot, PoseMe.cameraRig.rotation)) < .99999999f  ||
                                       Mathf.Abs(sc.focusDistance - focus) > 1e-3f))
            {
                sc.focusDistance = Mathf.Lerp(sc.focusDistance, focus, deltaT);
                curCamPos = Vector3.Lerp(curCamPos, camPos, 1.25f * deltaT);
                SetCamPosition(curCamPos);
                PoseMe.cameraRig.rotation = Quaternion.Slerp(PoseMe.cameraRig.rotation, rigRot, deltaT);
                PoseMe.camera.localRotation = Quaternion.Slerp(PoseMe.camera.localRotation, camRot, deltaT);
                yield return wait;
            }
            // PoseMe.InvokeSpeechBubble(onEnterBubbleText, onEnterBubbleLifeTime.val, onEnterBubbleDelay.val);
        }
        
        private IEnumerator SmoothCamRestoreBezier(float rigRotY, float camRotX, float focus, float rotationSpeedMult)
        {
            var speed = (PoseMe.cinematicEnabled.val && PoseMe.cinematicCamMode > 0)
                ? PoseMe.currentCamSpeed
                : PoseMe.smoothCamSpeedJ.val;
            var deltaT = speed * Time.fixedDeltaTime;
            // var deltaT2 = deltaT * rotationSpeedMult;
            var rigRot = Quaternion.Euler(0f, rigRotY, 0f);
            var camRot = Quaternion.Euler(camRotX, 0f, 0f);
            var curCamPos = GetCamPosition();
            float t = 0f;
            int steps = 5;
            float a = 0;
            // a = deltaT;
            while (!smoothCamAbort && (Mathf.Abs(Quaternion.Dot(camRot, PoseMe.camera.localRotation)) < .99999999f  ||
                                       t < .999f ||
                                       Mathf.Abs(Quaternion.Dot(rigRot, PoseMe.cameraRig.rotation)) < .99999999f  ||
                                       Mathf.Abs(sc.focusDistance - focus) > 1e-3f))
            {
                if (steps > 0)
                {
                    a = deltaT / steps;
                    steps--;
                }
                sc.focusDistance = Mathf.Lerp(sc.focusDistance, focus, a);
                t = Mathf.Lerp(t, 1f, a);
                var p = bezier.Evaluate(t);
                SetCamPosition(p);
                PoseMe.cameraRig.rotation = Quaternion.Slerp(PoseMe.cameraRig.rotation, rigRot, a*rotationSpeedMult);
                PoseMe.camera.localRotation = Quaternion.Slerp(PoseMe.camera.localRotation, camRot, a*rotationSpeedMult);
                
                yield return wait;
            }
            // PoseMe.InvokeSpeechBubble(onEnterBubbleText, onEnterBubbleLifeTime.val, onEnterBubbleDelay.val);
        }
        
        private IEnumerator SmoothCamRestoreLin(Vector3 camPos, float rigRotY, float camRotX, float focus)
        {
            var camPosCur = PoseMe.camera.position;
            var rigRotCur = PoseMe.cameraRig.rotation;
            var camRotCur = PoseMe.camera.localRotation;
            var focusCur = sc.focusDistance;
            var rigRot = Quaternion.Euler(0f, rigRotY, 0f);
            var camRot = Quaternion.Euler(camRotX, 0f, 0f);
            float t = 0f;
            while (!smoothCamAbort && t < 1f)
            {
                SuperController.singleton.focusDistance = Mathf.Lerp(focusCur, focus, t);
                t += .5f*PoseMe.smoothCamSpeedJ.val*Time.fixedDeltaTime;
                if (t > 1f) t = 1f;
                SetCamPosition(Vector3.Lerp(camPosCur, camPos, t));
                PoseMe.cameraRig.rotation = Quaternion.Slerp(rigRotCur, rigRot, t);
                PoseMe.camera.localRotation = Quaternion.Slerp(camRotCur, camRot, t);
                yield return wait;
            }
            // PoseMe.InvokeSpeechBubble(onEnterBubbleText, onEnterBubbleLifeTime.val, onEnterBubbleDelay.val);
        }

        private void SetCamPosition(Vector3 val)
        {
            sc.playerHeightAdjust = val.y - 1.6f;
            val.y = 0f;
            PoseMe.cameraRig.position = val;
            sc.SyncMonitorRigPosition();
        }

        private Vector3 GetCamPosition()
        {
            return PoseMe.cameraRig.position + new Vector3(0f, PoseMe.camera.position.y, 0f);
        }

        private void InvokeDialogs(bool onEnter)
        {
            if(PoseMe.ignoreDialogs.val) return;
            if (dialogPoolLevel.val > -1f && DialogPool.dialogs[(int)dialogPoolLevel.val].Count > 0) DialogPool.InvokeRandom((int)dialogPoolLevel.val, onEnter);
            dialogs.Invoke(onEnter);
        }
        
        private bool smoothCamAbort => !PoseMe.buttonHovered && 
                                       (Input.GetKeyDown(KeyCode.Mouse1) ||
                                       Input.GetKeyDown(KeyCode.Mouse2) ||
                                       Input.GetAxis("Mouse ScrollWheel") != 0f ||
                                       Input.GetKeyDown(KeyCode.Tab) ||
                                       Input.GetKeyDown(KeyCode.W) ||
                                       Input.GetKeyDown(KeyCode.A) ||
                                       Input.GetKeyDown(KeyCode.S) ||
                                       Input.GetKeyDown(KeyCode.D) ||
                                       SuperController.singleton.worldUI.gameObject.activeSelf
                                       // || (!SuperController.singleton.IsMonitorOnly && 
                                       //  (JoystickControl.GetAxis(JoystickControl.Axis.RightStickX) > 0f || 
                                       //   JoystickControl.GetAxis(JoystickControl.Axis.RightStickY) > 0f ||
                                       //   JoystickControl.GetAxis(JoystickControl.Axis.LeftStickX) > 0f ||
                                       //   JoystickControl.GetAxis(JoystickControl.Axis.LeftStickY) > 0f))
                                       );
        
        public JSONClass Store()
        {
            var jc = new JSONClass
            {
                ["camPosition"] = camPosition.ToJA(),
                // ["rigRotation"] = {AsFloat = rigRotation},
                ["rigRotationFull"] = rigQuat.ToJA(),
                ["cameraRotation"] = {AsFloat = camRotation},
                ["focusDistance"] = {AsFloat = focus},
                [onCamEnter.Name] = onCamEnter.GetJSON(PoseMe.singleton.subScenePrefix),
                [onCamExit.Name] = onCamExit.GetJSON(PoseMe.singleton.subScenePrefix),
            };
            dialogPoolLevel.Store(jc);
            cumMales.Store(jc);
            cumFemale.Store(jc);
            if(timelineClip != null) jc["timelineClip"] = timelineClip.name;
            if (img != null) jc["image"] = Convert.ToBase64String(img);
            var bubblesJA = new JSONArray();
            foreach (var bubble in dialogs)
            {
                bubblesJA.Add(bubble.Store());
            }
            jc["Bubbles"] = bubblesJA;
            return jc;
        }

        public void Load(JSONClass jc)
        {
            if (jc.HasKey("image"))
            {
                img = Convert.FromBase64String(jc["image"]);
                hasImage = true;
            }
            if (jc.HasKey("rigPosition"))
            {
                var rigPosition = jc["rigPosition"].AsArray.ToV3();
                camPosition = new Vector3(rigPosition.x, rigPosition.y + jc["playerHeight"].AsFloat + 1.6f, rigPosition.z);
            }
            else camPosition = jc["camPosition"].AsArray.ToV3();
            if (jc.HasKey("rigRotation")) rigQuat = Quaternion.Euler(new Vector3(0f, jc["rigRotation"].AsFloat, 0f));
            else rigQuat = jc["rigRotationFull"].AsArray.ToQuat();
            camRotation = jc["cameraRotation"].AsFloat;
            focus = jc["focusDistance"].AsFloat;
            onCamEnter.RestoreFromJSON(jc, PoseMe.singleton.subScenePrefix, false, true);
            onCamExit.RestoreFromJSON(jc, PoseMe.singleton.subScenePrefix, false, true);
            dialogPoolLevel.Load(jc);
            cumMales.Load(jc);
            cumFemale.Load(jc);
            if (jc.HasKey("Bubbles"))
            {
                foreach (var bubble in jc["Bubbles"].Childs)
                {
                    dialogs.Add(new Dialog(bubble.AsObject));
                }
            }

            if (PoseMe.GetTimeline())
            {
                if(jc.HasKey("timelineClip"))
                {
                    var actionName = jc["timelineClip"].Value;
                    timelineClip = PoseMe.timeline.GetAction(actionName);
                }
            }
            hasAngle = true;
        }
    }
}