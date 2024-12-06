using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MacGruber;
using MeshVR;
using MVR.FileManagementSecure;
using SimpleJSON;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace CheesyFX
{
    public class Pose
    {
        public UIDynamicButton uiButton;
        public ClickListener clickListener;
        public JSONClass pose;
        public List<CamAngle> camAngles = new List<CamAngle>();
        private int _id;
        public static IEnumerator smoothCam;
        public EventTrigger onPoseEnter;
        public EventTrigger onPoseExit;
        // private static Color selectedBGColor = new Color(0.48f, 0.48f, 0.48f);
        public static Color selectedBGColor = Color.white;
        public static Color deselectedBGColor = new Color(0.76f, 0.76f, 0.76f, 0f);
        public static Color selectedColor = Color.white;
        public static Color deselectedColor = new Color(1f, 1f, 1f, PoseMe.buttonTransparency.val);
        public static Color fadeColor = Color.white;
        private bool _selected;
        public CamAngle currentCam;
        public JSONStorableBool isHandjobPoseLeft = new JSONStorableBool("HandJobPoseLeft", false);
        public JSONStorableBool isHandjobPoseRight = new JSONStorableBool("HandJobPoseRight", false);

        public Dictionary<Atom, JSONStorableBool> actorToggles = new Dictionary<Atom, JSONStorableBool>();

        public PoseIdle poseIdle;
        public string actors => "<b>Stored actors:</b> "+string.Join(", ", pose["pose"].AsObject.Keys.ToArray());
        private static List<Atom> toysToRestore = new List<Atom>();
        private static WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();

        public DialogSet dialogs = new DialogSet();
        public List<Slap> slaps = new List<Slap>();
        public GazeSettings gazeSettings = new GazeSettings(Gaze.gazeSettings);

        public JSONStorableBool cumMales = new JSONStorableBool("Pose Cum Males", false);
        public JSONStorableBool cumFemale = new JSONStorableBool("Pose Cum Female", false);
        public JSONStorableBool disableAnatomy = new JSONStorableBool("Disable Anatomy", false);
        public JSONStorableFloat dialogPoolLevel = new JSONStorableFloat("Dialog Pool Level", -1f, -1f, 2f);
        
        public Matrix4x4 storedWorldToLocalMatrix = Matrix4x4.identity;
        private Matrix4x4 currentLocalToWorldMatrix = Matrix4x4.identity;
        public Matrix4x4 refMatrix;

        public static bool isApplying;
        public static bool isReapplying;
        public static bool timelineLocked;
        public JSONStorableAction timelineClip;
        
        public bool selected
        {
            get { return _selected;}
            set
            {
                _selected = value;
                clickListener.dragEnabled = value;
                if (value)
                {
                    uiButton.button.image.color = selectedColor;
                    backdropImage.color = selectedBGColor;
                    if(PoseMe.applyIdles.val && poseIdle.applyIdles.val) poseIdle.enabled = true;
                    poseIdle.ResetForces();
                }
                else
                {
                    uiButton.button.image.color = deselectedColor;
                    backdropImage.color = deselectedBGColor;
                    if(poseIdle) poseIdle.enabled = false;
                    for (int i = 0; i < slaps.Count; i++)
                    {
                        slaps[i].enabled = false;
                    }
                    for (int i = 0; i < movements.Count; i++)
                    {
                        movements[i].SetActiveImmediate(false);
                    }
                    // for (int i = 0; i < camAngles.Count; i++)
                    // {
                    //     camAngles[i].selected = false;
                    // }
                    // onPoseEnter.CloseTriggerActionsPanel();
                }

                PoseMe.worldCanvas.SetButtonActive(id, value);
            }
        }

        private static Atom containingAtom => PoseMe.atom;
        public int id
        {
            get { return _id; }
            set
            {
                if (value == _id) return;
                _id = value;
                if(uiButton != null) uiButton.label = _id.ToString();
            }
        }

        public Pose(int id)
        {
            try
            {
                if(PoseMe.currentPose != null)
                {
                    PoseMe.currentPose.selected = false;
                }
                storedWorldToLocalMatrix = containingAtom.mainController.transform.worldToLocalMatrix;
                refMatrix = containingAtom.mainController.transform.localToWorldMatrix;
                _id = id;
                SetupActorToggles();
                onPoseEnter = new EventTrigger(PoseMe.singleton, "On Pose Enter");
                onPoseExit = new EventTrigger(PoseMe.singleton, "On Pose Exit");
                poseIdle = PoseMe.singleton.gameObject.AddComponent<PoseIdle>().Init(this);
                PoseIdle.presetSystem.ConnectInstance(poseIdle);
                currentCam = new CamAngle(this, (byte[])null);
                camAngles.Add(currentCam);
                GetPose().Start();
            
                PoseMe.currentPose = this;
                PoseMe.SyncPoseActionsUI();
                AddButton();
                backdropImage.color = selectedBGColor;
                selected = true;
                TakeScreenshot(UpdateCurrentAngle);
                clickListener.dragEnabled = true;
                foreach (var level in Story.levels)
                {
                    if(id-1 > level.maxId) continue;
                    if (level.ContainsPose(id-1)) level.maxId++;
                    else
                    {
                        level.minId++;
                        level.maxId++;
                    }
                }
                // gazeSettings.FetchFromCurrent();
            }
            catch (Exception e)
            {
                SuperController.LogError(e.ToString());
            }
            
        }
        
        public Pose(int id, JSONClass pose)
        {
            this.pose = pose;
            _id = id;
            onPoseEnter = new EventTrigger(PoseMe.singleton, "On Pose Enter");
            onPoseExit = new EventTrigger(PoseMe.singleton, "On Pose Exit");
            poseIdle = PoseMe.singleton.gameObject.AddComponent<PoseIdle>().Init(this);
            PoseIdle.presetSystem.ConnectInstance(poseIdle);
            // PoseMe.currentPose = this;
            PoseMe.SyncPoseActionsUI();
            foreach (JSONClass jc in pose["pose"][containingAtom.name]["storables"].AsArray)
            {
                if (jc["id"].Value != "control") continue;
                storedWorldToLocalMatrix = Matrix4x4.TRS(jc["position"].AsObject.ToV3(), Quaternion.Euler(jc["rotation"].AsObject.ToV3()), Vector3.one).inverse;
                break;
            }
            Load(pose);
            SetupActorToggles();
            AddButton();
            SetButtonImage();
            // disableAnatomy.setCallbackFunction += val => PoseMe.disableAnatomy.val = val;
        }

        private void Load(JSONClass jc)
        {
            onPoseEnter.RestoreFromJSON(jc, PoseMe.singleton.subScenePrefix, false, true);
            onPoseExit.RestoreFromJSON(jc, PoseMe.singleton.subScenePrefix, false, true);
            isHandjobPoseLeft.Load(jc);
            isHandjobPoseRight.Load(jc);
            dialogPoolLevel.Load(pose);
            cumMales.Load(jc);
            cumFemale.Load(jc);
            disableAnatomy.Load(jc);
            if (jc.HasKey("Dialogs"))
            {
                foreach (var bubble in jc["Dialogs"].Childs)
                {
                    dialogs.Add(new Dialog(bubble.AsObject));
                }
            }
            if (jc.HasKey("cams"))
            {
                foreach (var cam in jc["cams"].Childs)
                {
                    camAngles.Add(new CamAngle(this, cam.AsObject));
                }

                if(camAngles.Count > 0) currentCam = camAngles.First();
            }
            // else if (jc.HasKey("img"))
            // {
            //     var camAngle = new CamAngle(this, Convert.FromBase64String(jc["img"]))
            //     {
            //         hasCam = true
            //     };
            //     camAngles.Add(camAngle);
            //     currentCam = camAngle;
            // }
            // else if (jc.HasKey("images"))
            // {
            //     var camAngle = new CamAngle(this, Convert.FromBase64String(jc["images"].Childs.First()))
            //     {
            //         hasCam = true
            //     };
            //     camAngles.Add(camAngle);
            //     currentCam = camAngle;
            // }
            if (jc.HasKey("idles"))
            {
                poseIdle.Load(jc["idles"].AsObject);
            }
            if (jc.HasKey("slaps"))
            {
                foreach (var item in jc["slaps"].Childs)
                {
                    if(SuperController.singleton.GetAtomByUid(item["personChooser"].Value) == null) return;
                    var slap = PoseMe.singleton.gameObject.AddComponent<Slap>().Init(this, item.AsObject);
                    slap.enabled = false;
                    slaps.Add(slap);
                }

                if(camAngles.Count > 0) currentCam = camAngles.First();
            }
            if (jc.HasKey("movements"))
            {
                foreach (var item in jc["movements"].Childs)
                {
                    var atom = SuperController.singleton.GetAtomByUid(item["atom"].Value);
                    if(atom == null) return;
                    var movement = new Movement(this, item.AsObject, false);
                    movements.Add(movement);
                    // $"{id} {movement.circularForce.enabled}".Print();
                }

                if(camAngles.Count > 0) currentCam = camAngles.First();
            }
            if (jc.HasKey("gaze"))
            {
                gazeSettings.Load(jc["gaze"].AsObject);
            }
            if (PoseMe.GetTimeline())
            {
                if(jc.HasKey("timelineClip"))
                {
                    var actionName = jc["timelineOnEnter"].Value;
                    timelineClip = PoseMe.timeline.GetAction(actionName);
                }
            }
            // disableAnatomy.setCallbackFunction += val => PoseMe.disableAnatomy.val = val;
        }

        public void Destroy()
        {
            Object.DestroyImmediate(poseIdle);
            RemoveButton();
            onPoseEnter.Remove();
            onPoseExit.Remove();
            foreach (var dialog in Dialog.dialogs)
            {
                dialog.onDialogEnter.Remove();
                // dialog.onDialogExit.Remove();
            }
            foreach (var cam in camAngles)
            {
                cam.onCamEnter.Remove();
                cam.onCamExit.Remove();
            }

            foreach (var slap in slaps)
            {
                Object.Destroy(slap);
            }
        }

        private void SetupActorToggles()
        {
            PoseMe.persons.ForEach(x => actorToggles[x.atom] = new JSONStorableBool(x.atom.uid, true));
            PoseMe.dildos.ForEach(x => actorToggles[x] = new JSONStorableBool(x.uid, true));
            PoseMe.toys.ForEach(x => actorToggles[x] = new JSONStorableBool(x.uid, true));
            PoseMe.RegisterActorToggles();
        }

        public void SyncActorToggleNames()
        {
            foreach (var item in actorToggles)
            {
                item.Value.name = item.Key.name;
            }
        }

        private void MapAtoms(JSONClass pose)
        {
            foreach (var person in FillMeUp.persons)
            {
                if(pose.HasKey(person.atom.uid)) continue;
                if (person.atom == containingAtom)
                {
                    var jc = pose.Childs.First(x => x.AsObject["containingAtom"].AsBool);
                }
            }
        }

        public void UpdatePose()
        {
            if(gettingPose) return;
            storedWorldToLocalMatrix = containingAtom.mainController.transform.worldToLocalMatrix;
            refMatrix = containingAtom.mainController.transform.localToWorldMatrix;
            GetPose().Start();
            PoseMe.actorsJ.val = actors;
        }
        
        public void AddCamAngle()
        {
            TakeScreenshot(AddCamAngle);
        }
        
        public void UpdateAngle()
        {
            TakeScreenshot(UpdateCurrentAngle);
        }

        public void DeleteAngle()
        {
            if (currentCam == null) return;
            if (camAngles.Count == 1)
            {
                "A pose has to have at least one camera angle.".Print();
                return;
            }
            camAngles.Remove(currentCam);
            PoseMe.SyncCamChooser();
            // PoseMe.angleChooser.valNoCallback = "";
            // PoseMe.previewImage.sprite = null;
            currentCam = camAngles[0];
            
            RenameAngles();
            currentCam.Apply();
        }

        private void RenameAngles()
        {
            for (int i = 0; i < camAngles.Count; i++)
            {
                camAngles[i].id = i;
            }
        }
        
        public void UpdateJSON()
        {
            pose[onPoseEnter.Name] = onPoseEnter.GetJSON(PoseMe.singleton.subScenePrefix);
            pose[onPoseExit.Name] = onPoseExit.GetJSON(PoseMe.singleton.subScenePrefix);
            isHandjobPoseLeft.Store(pose);
            isHandjobPoseRight.Store(pose);
            dialogPoolLevel.Store(pose);
            cumMales.Store(pose);
            cumFemale.Store(pose);
            disableAnatomy.Store(pose);
            var dialogsJA = new JSONArray();
            foreach (var dialogs in dialogs)
            {
                dialogsJA.Add(dialogs.Store());
            }
            pose["Dialogs"] = dialogsJA;
            pose["idles"] = poseIdle.Store();
            JSONArray ja = new JSONArray();
            foreach (var camAngle in camAngles)
            {
                ja.Add(camAngle.Store());
            }
            pose["cams"] = ja;
            
            ja = new JSONArray();
            foreach (var slap in slaps)
            {
                ja.Add(slap.Store());
            }
            pose["slaps"] = ja;
            ja = new JSONArray();
            foreach (var movement in movements)
            {
                ja.Add(movement.Store());
            }
            pose["movements"] = ja;
            pose["gaze"] = gazeSettings.Store();
            if(timelineClip != null) pose["timelineClip"] = timelineClip.name;
        }

        private static bool gettingPose;
        private IEnumerator GetPose()
        {
            yield return new WaitUntil(() => gettingPose == false);
            gettingPose = true;
            SuperController.singleton.freezeAnimationToggle.isOn = true;
            yield return new WaitForSeconds(.6f);
            var jc = new JSONClass();
            jc["SceneType"] = PoseMe.sceneType;
            jc["id"] = id.ToString();
            jc["idles"] = poseIdle.Store();
            var jcPose = jc["pose"] = new JSONClass();
            jcPose[containingAtom.uid] = GetPersonPose(containingAtom);
            foreach (var atom in SuperController.singleton.GetAtoms())
            {
                if (atom != containingAtom && atom.type == "Person") jcPose[atom.uid] = GetPersonPose(atom);
                else if(atom.IsToyOrDildo()) jcPose[atom.uid] = GetToyPose(atom);
            }
            var jcForces = new JSONClass();
            foreach (var fuckable in FillMeUp.fuckables)
            {
                fuckable.StorePoseSettings(jcForces);
            }
            jc["Forces"] = jcForces;
            jc["OrificeSync"] = FillMeUp.orificeForceGroup.Store();
            jc["HandSync"] = FillMeUp.handForceGroup.Store();
            
            jc[onPoseEnter.Name] = onPoseEnter.GetJSON(PoseMe.singleton.subScenePrefix);
            jc[onPoseExit.Name] = onPoseExit.GetJSON(PoseMe.singleton.subScenePrefix);
            
            pose = jc;
            PoseMe.actorsJ.val = actors;
            PoseMe.RegisterActorToggles();
            SuperController.singleton.freezeAnimationToggle.isOn = false;
            isHandjobPoseLeft.val = FillMeUp.hands[0].enabled;
            isHandjobPoseRight.val = FillMeUp.hands[1].enabled;
            
            gettingPose = false;
        }

        private JSONClass GetPersonPose(Atom atom)
        {
            JSONClass poseJSON = null;
            var storables = atom.GetStorableIDs().Select(atom.GetStorableByID)
                .Where(t => !t.exclude && t.gameObject.activeInHierarchy)
                .Where(ShouldStorableBeIncluded);
            var storablesJSON = new JSONArray();
            
            foreach (var storable in storables)
            {
                var jc = storable.GetJSON(true, false, storable.name != "geometry");
                if (atom != containingAtom && jc["id"].Value == "control")
                {
                    jc["position"] = storedWorldToLocalMatrix.MultiplyPoint(atom.mainController.transform.position).ToJC();
                    jc["rotation"] = (storedWorldToLocalMatrix.rotation * atom.mainController.transform.rotation).eulerAngles.ToJC();
                }
                storablesJSON.Add(jc);
            }

            var genderListener = FillMeUp.persons.First(x => x.atom == atom).characterListener;
            poseJSON = new JSONClass
            {
                ["setUnlistedParamsToDefault"] = { AsBool = true },
                ["V2"] = { AsBool = true },
                ["type"] = atom.type,
                ["on"] = {AsBool = atom.GetBoolParamValue("on")},
                ["containingAtom"] = {AsBool = atom == PoseMe.atom},
                ["gender"] = genderListener.dcs.gender.ToString(),
                ["futa"] = {AsBool = genderListener.isFuta},
                ["handPose"] = GetHandPose(atom),
                ["fingerSprings"] = GetFingerSprings(atom),
                ["penis"] = GetPenisState(atom),
                ["storables"] = storablesJSON
            };
            return poseJSON;
        }
        
        private static bool ShouldStorableBeIncluded(JSONStorable t)
        {
            if (t is FreeControllerV3) return true;
            if (t is DAZBone) return true;
            if (t.storeId == "geometry" && t is DAZCharacterSelector)
            {
                // var morph = t as DAZMorph;
                return false;
            }
            return false;
        }

        private JSONClass GetPenisState(Atom atom)
        {
            var jc = new JSONClass();
            var person = FillMeUp.persons.FirstOrDefault(x => x.atom == atom);
            var radiiJA = new JSONArray();
            for (int i = 0; i < person.penetrator.colliders.Count; i++)
            {
                var radius = ((CapsuleCollider)person.penetrator.colliders[i]).radius;
                radiiJA.Add(radius.ToString());
            }
            // jc["baseRotations"] = baseRotations;
            return jc;
        }

        private JSONArray GetHandPose(Atom atom)
        {
            var ja = new JSONArray();
            var dcs = atom.GetStorableByID("geometry") as DAZCharacterSelector;
            List<JSONClass> morphs = new List<JSONClass>();
            foreach (var morph in dcs.morphsControlUI.GetMorphs())
            {
                if (!morph.isPoseControl || !morph.hasFormulas || morph.displayName.StartsWith("BL-Finger") || !HasFingerFormula(morph)) continue;
                var jc = new JSONClass();
                if (morph.StoreJSON(jc, false))
                {
                    morphs.Add(jc);
                    // $"{atom.uid} : {morph.uid} : {morph.morphValue}: {morph.jsonFloat.defaultVal}".Print();
                }
                
                // if (morph.StoreJSON(jc, false))
                // {
                //     if (IsControlMorph(morph) && morph.morphValue == morph.jsonFloat.defaultVal)
                //     {
                //         morphs.Insert(0, jc);
                //     }
                //     else morphs.Add(jc);
                // }
            }
            for (int i = 0; i < morphs.Count; i++)
            {
                ja.Add(morphs[i]);
            }
            return ja;
        }

        private void ApplyHandPose(Atom atom, JSONNode jn)
        {
            var dcs = atom.GetStorableByID("geometry") as DAZCharacterSelector;
            foreach (var morph in dcs.morphsControlUI.GetMorphs())
            {
                if (!morph.isPoseControl || !morph.hasFormulas || morph.displayName.StartsWith("BL-Finger") || !HasFingerFormula(morph)) continue;
                bool stored = false;
                foreach (JSONClass morphJc in jn.Childs)
                {
                    if (morphJc["uid"].Value == morph.uid)
                    {
                        morph.morphValue = morphJc["value"].AsFloat;
                        // $"{atom.uid} : {morph.uid} : {morph.morphValue}".Print();
                        stored = true;
                        break;
                    }
                }
                if(!stored) morph.jsonFloat.SetValToDefault();
            }
        }

        private JSONClass GetFingerSprings(Atom atom)
        {
            var person = FillMeUp.persons.First(x => x.uid == atom.uid);
            var jc = new JSONClass();
            var jaLeftCarpal = new JSONArray();
            foreach (var bone in person.lCarpalBones)
            {
                jaLeftCarpal.Add(bone.baseJointRotation.ToJA());
            }
            jc["leftCarpal"] = jaLeftCarpal;
            var jaLeft = new JSONArray();
            foreach (var bone in person.lHandBones)
            {
                jaLeft.Add(bone.baseJointRotation.ToJA());
            }
            jc["left"] = jaLeft;
            var jaRightCarpal = new JSONArray();
            foreach (var bone in person.rCarpalBones)
            {
                jaRightCarpal.Add(bone.baseJointRotation.ToJA());
            }
            jc["rightCarpal"] = jaRightCarpal;
            var jaRight = new JSONArray();
            foreach (var bone in person.rHandBones)
            {
                jaRight.Add(bone.baseJointRotation.ToJA());
            }
            jc["right"] = jaRight;
            return jc;
        }
        
        private bool HasFingerFormula(DAZMorph morph)
        {
            var validFormulas = morph.formulas.Any(x =>
                    (x.target.Contains("Thumb") ||
                    x.target.Contains("Index") ||
                    x.target.Contains("Mid") ||
                    x.target.Contains("Ring") ||
                    x.target.Contains("Pinky") ||
                    x.target.Contains("Finger")) &&
                    Mathf.Abs(x.multiplier) > .1f);
            return validFormulas;
        }

        private bool IsControlMorph(DAZMorph morph)
        {
            return morph.formulas.Any(x => x.target.StartsWith("CTRL"));
        }
        
        private JSONClass GetToyPose(Atom atom)
        {
            JSONClass jc = new JSONClass();
            jc["type"] = atom.type;
            jc["on"].AsBool = atom.GetBoolParamValue("on");
            if (atom.type == "Dildo")
            {
                var dildo = FillMeUp.dildos.FirstOrDefault(x => x.penetrator.atom == atom);
                if(dildo) jc["spring"].AsFloat = dildo.stiffness.val;
            }
            jc["ctrl"] = atom.mainController.GetJSON();
            jc["ctrl"]["position"] = storedWorldToLocalMatrix.MultiplyPoint(atom.mainController.transform.position).ToJC();
            jc["ctrl"]["rotation"] = (storedWorldToLocalMatrix.rotation * atom.mainController.transform.rotation).eulerAngles.ToJC();
            foreach (var rb in atom.GetComponentsInChildren<Rigidbody>(true))
            {
                jc[rb.name] = new JSONClass
                {
                    ["pos"] = storedWorldToLocalMatrix.MultiplyPoint(rb.position).ToJA(),
                    ["rot"] = (storedWorldToLocalMatrix.rotation * rb.rotation).eulerAngles.ToJA()
                };
            }
            return jc;
        }

        public void Apply(bool applyCam = true)
        {
            try
            {
                // $"ApplyPose {isApplying}".Print();
                if(isApplying) return;
                isApplying = true;
                
                if(PoseMe.GetTimeline())
                {
                    PoseMe.timelineLock.val = true;
                    PoseMe.timelineStop.Invoke();
                }
                Gaze.internalEnabled = false;
                Gaze.ResetTorques();
                PoseMe.applyingHandjobPoseLeft = isHandjobPoseLeft.val;
                PoseMe.applyingHandjobPoseRight = isHandjobPoseRight.val;
                PoseMe.SyncCamChooser();
                PoseMe.actorsJ.val = actors;
                isReapplying = PoseMe.currentPose == this;
                if (PoseMe.currentPose != null && !isReapplying)
                {
                    var last = PoseMe.currentPose;
                    // last.onEnterBubbleText.inputField = last.onExitBubbleText.inputField = null;
                    if(!PoseMe.ignoreTriggers.val) last.onPoseExit.Trigger();
                    last.InvokeDialogs(false);
                    CreateSlapItems();
                    CreateMovementItems();
                    // for (int i = 0; i < last.slaps.Count; i++)
                    // {
                    //     last.slaps[i].enabled = false;
                    // }
                    // for (int i = 0; i < last.movements.Count; i++)
                    // {
                    //     last.movements[i].SetActiveImmediate(false);
                    // }
                    last.selected = false;
                }
                
                PoseMe.currentPose = this;
                gazeSettings.Apply();
                PoseMe.poseChooser.valNoCallback = PoseMe.poses.IndexOf(this).ToString();
                PoseMe.applyPosePost.Stop();
                PoseMe.softPhysicsStates.Clear();
                for (int i = 0; i < FillMeUp.persons.Count; i++)
                {
                    PoseMe.softPhysicsStates.Add(PoseMe.persons[i].GetSoftPhysicsState());
                }
                currentLocalToWorldMatrix = containingAtom.mainController.transform.localToWorldMatrix;
                refMatrix = !PoseMe.restoreRoot.val ? currentLocalToWorldMatrix : storedWorldToLocalMatrix.inverse;
                ApplyPersonPose(containingAtom);
                foreach (string atomUid in pose["pose"].AsObject.Keys)
                {
                    if(atomUid == PoseMe.atom.uid) continue;
                    var atom = SuperController.singleton.GetAtomByUid(atomUid);
                    if (atom == null)
                    {
                        $"Atom {atomUid} not found".Print();
                    }
                    else if(atom.type == "Person" && PoseMe.restoreOtherPersons.val)
                    {
                        ApplyPersonPose(atom);
                    }
                }
                if(pose.HasKey("Forces"))
                {
                    var jcForces = pose["Forces"].AsObject;
                    foreach (var fuckable in FillMeUp.fuckables)
                    {
                        fuckable.LoadPoseSettings(jcForces);
                    }
                }
                else
                {
                    foreach (var fuckable in FillMeUp.fuckables)
                    {
                        fuckable.preventPullout.SetValToDefault();
                    }
                }
                if(pose.HasKey("OrificeSync")) FillMeUp.orificeForceGroup.Load(pose["OrificeSync"].AsObject);
                else FillMeUp.orificeForceGroup.SetToDefault();
                if(pose.HasKey("HandSync")) FillMeUp.handForceGroup.Load(pose["HandSync"].AsObject);
                else FillMeUp.handForceGroup.SetToDefault();
                // foreach (var fuckable in FillMeUp.fuckables)
                // {
                //     fuckable.LoadPoseSettings(pose);
                // }
                
                PoseMe.applyPosePost = ApplyPost(pose, applyCam).Start();
                selected = true;
                if(IdleUIProvider.uiOpen) IdleUIProvider.SelectRegion(IdleUIProvider.lastTabId);
                // if(randomizeAngles != null) PoseMe.poseRandomizeCamUid.RegisterBool(randomizeAngles);
                clickListener.dragEnabled = true;
                PoseIdle.presetSystem.ConnectInstance(poseIdle, false);
                foreach (var person in PoseMe.persons)
                {
                    person.speechRoutine.Stop();
                    person.thoughtRoutine.Stop();
                }
                PoseMe.SyncCamChooser();
                PoseMe.SyncPoseActionsUI();
                PoseMe.RegisterActorToggles();
                CreateDialogItems();

                Story.currentLevel?.SyncButtons();
            }
            catch (Exception e)
            {
                SuperController.LogError(e.ToString());
            }
        }
        
        private IEnumerator ApplyPost(JSONClass pose, bool applyCam)
        {
            var poseJc = pose["pose"].AsObject;
            toysToRestore.Clear();
            for (int i = 0; i < PoseMe.dildos.Count; i++)
            {
                if(ShouldAtomBeRestored(PoseMe.dildos[i], poseJc)) toysToRestore.Add(PoseMe.dildos[i]);
            }
            for (int i = 0; i < PoseMe.toys.Count; i++)
            {
                if(ShouldAtomBeRestored(PoseMe.toys[i], poseJc)) toysToRestore.Add(PoseMe.toys[i]);
            }

            for (int i = 0; i < toysToRestore.Count; i++)
            {
                toysToRestore[i].collisionEnabled = false;
                // toysToRestore[i].SetBoolParamValue("on", false);
            }

            // foreach (string atomUid in poseJc.Keys)
            // {
            //     var atom = SuperController.singleton.GetAtomByUid(atomUid);
            //     if (atom == null) continue;
            //     if (atom.type == "Person" && ShouldAtomBeRestored(atom))
            //     {
            //         RestoreControllers(atom);
            //     }
            // }

            RestoreAllControllers();
            if (applyCam && camAngles.Count > 0)
            {
                yield return waitForFixedUpdate;
                yield return waitForFixedUpdate;
                yield return waitForFixedUpdate;
                smoothCam.Stop();
                if(PoseMe.fistCamOnPoseSwitch.val) camAngles[0].Apply(true);
                else
                {
                    if(currentCam != null) currentCam.Apply(true);
                    else "CurrentCam equals null".Print();
                }
                // ApplyNextCamAngle();
            }
            yield return new WaitUntil(() =>
            {
                for (int i = 0; i < PoseMe.persons.Count; i++)
                {
                    if (PoseMe.persons[i].GetSoftPhysicsState() != PoseMe.softPhysicsStates[i]) return false;
                }
                return true;
            });
            
            foreach (string atomUid in poseJc.Keys)
            {
                var atom = SuperController.singleton.GetAtomByUid(atomUid);
                if (atom == null) continue;
                var atomPose = poseJc[atomUid].AsObject;
                if (atom.type == "Person" && ShouldAtomBeRestored(atom))
                {
                    // RestoreControllers(atom);
                    JSONArray storables = atomPose["storables"].AsArray;
                    foreach (var bone in atom.GetComponentsInChildren<DAZBone>())
                    {
                        if (bone.name == "lEye" || bone.name == "rEye" || bone.name == "upperJaw" || bone.name == "lowerJaw")
                        {
                            continue;
                        }
                        for (int i = 0; i < storables.Count; i++)
                        {
                            JSONClass jc = storables[i].AsObject;
                            
                            
                            if(jc["id"].Value != bone.name) continue;
                            // bone.name.Print();
                            bone.RestoreFromJSON(jc);
                        }
                    }
                }
                else if (ShouldAtomBeRestored(atom)) ApplyToyPose(atom, atomPose).Start();
            }
            
            

            if (PoseMe.initialPoseLoaded) InvokeDialogs(true);
            else PoseMe.initialPoseLoaded = true;
            // bool enableSlaps = !Slap.configureUIOpen || !Slap.targetUIOpen 
            yield return new WaitForEndOfFrame();
            yield return waitForFixedUpdate;
            yield return waitForFixedUpdate;
            LateRestoreAllControllers();
            for (int i = 0; i < slaps.Count; i++)
            {
                var slap = slaps[i];
                slap.Sync();
                slap.enabled = slap.enabledJ.val;
                // slap.LinkHandControl();
            }
            for (int i = 0; i < movements.Count; i++)
            {
                movements[i].SetActive(true);
            }
            
            
            
            PoseMe.disableAnatomy.val = disableAnatomy.val;
            Gaze.internalEnabled = true;
            Gaze.SelectRandomTargets();
            if (!PoseMe.ignoreTriggers.val)
            {
                if (cumMales.val) ReadMyLips.orgasmMalesNow.actionCallback.Invoke();
                if (cumFemale.val) ReadMyLips.orgasmNow.actionCallback.Invoke();
            }
            if(PoseMe.GetTimeline())
            {
                PoseMe.timelineStop();
                if (currentCam.timelineClip != null)
                {
                    currentCam.timelineClip.actionCallback.Invoke();
                }
                else if (timelineClip != null)
                {
                    timelineClip.actionCallback.Invoke();
                }
                
                PoseMe.timelineLock.val = timelineLocked;
            }
            if(!PoseMe.ignoreTriggers.val) onPoseEnter.Trigger();
            isApplying = false;
            // FillMeUp.OnPoseSnap();
        }

        public void CreateDialogItems()
        {
            if(Dialog.configureUIOpen && !DialogPool.uiOpen) Dialog.CloseConfigureUI();
            if (PoseMe.currentTab != 1 || DialogPool.uiOpen) return;
            PoseMe.singleton.RemoveUIElements(PoseMe.poseBubbleItems);
            for (int i = 0; i < dialogs.Count; i++)
            {
                dialogs.Sort();
                PoseMe.poseBubbleItems.Add(PoseMe.CreateDialogUIItem(dialogs[i]));
            }
        }
        
        public void CreateSlapItems()
        {
            if(Slap.configureUIOpen) Slap.CloseConfigureUI();
            if (PoseMe.currentTab != 3) return;
            PoseMe.singleton.RemoveUIElements(PoseMe.slapItems);
            for (int i = 0; i < slaps.Count; i++)
            {
                PoseMe.slapItems.Add(PoseMe.CreateUIDynamicSlapItem(slaps[i]));
            }
        }
        
        public void CreateMovementItems()
        {
            if(Movement.configureUIOpen) Movement.CloseUI();
            if (PoseMe.currentTab != 3 || Movement.configureUIOpen) return;
            PoseMe.singleton.RemoveUIElements(PoseMe.movementItems);
            for (int i = 0; i < movements.Count; i++)
            {
                PoseMe.movementItems.Add(Movement.CreateUIDynamicMovement(movements[i], true));
            }
        }

        private void ApplyPersonPose(Atom atom)
        {
            try
            {
                // PoseMe.cj.targetRotation.eulerAngles.Print();
                // return;
                if(!actorToggles[atom].val) return;
                Quaternion headCtrlRot = Quaternion.identity;
                Quaternion headRot = Quaternion.identity;
                Quaternion neckRot = Quaternion.identity;
                Quaternion chestRot = Quaternion.identity;
                FreeControllerV3 headCtrl = null;
                DAZBone head = null;
                DAZBone neck = null;
                DAZBone chest = null;
                
                var personJc = pose["pose"][atom.uid].AsObject;
                if(personJc.HasKey("on")) atom.SetBoolParamValue("on", personJc["on"].AsBool);
                // if(!atom.GetBoolParamValue("on")) return;
            
                if (!PoseMe.restoreHeadRotation.val)
                {
                    headCtrl = atom.freeControllers.First(x => x.name == "headControl");
                    var bones = atom.GetComponentsInChildren<DAZBone>();
                    for (int i = 0; i < bones.Length; i++)
                    {
                        var bone = bones[i];
                        if (bone.name == "head") head = bone;
                        else if (bone.name == "neck") neck = bone;
                        else if (bone.name == "chest") chest = bone;
                    }
                    headCtrlRot = headCtrl.transform.localRotation;
                    headRot = head.transform.localRotation;
                    neckRot = neck.transform.localRotation;
                    chestRot = chest.transform.localRotation;
                }
            
                PresetManagerControl pmc = atom.presetManagerControls.First(x => x.name == "PosePresets");
                var pm = pmc.GetComponent<PresetManager>();
                
                if(atom == containingAtom)
                {
                    
                    if (!PoseMe.restoreRoot.val)
                    {
                        
                        var controlJc = personJc["storables"].Childs.First(x => x["id"].Value == "control");
                        controlJc["id"] = "_control";
                        pm.LoadPresetFromJSON(personJc);
                        controlJc["id"] = "control";
                    }
                    else pm.LoadPresetFromJSON(personJc);
                }
                else
                {
                    var controlJc = personJc["storables"].Childs.First(x => x["id"].Value == "control");
                    var storedPos = controlJc["position"].AsObject.ToV3();
                    var storedRot = controlJc["rotation"].AsObject.ToV3();
                    
                    controlJc["position"] = refMatrix.MultiplyPoint(storedPos).ToJC();
                    controlJc["rotation"] = (refMatrix.rotation * Quaternion.Euler(storedRot)).eulerAngles.ToJC();
                    pm.LoadPresetFromJSON(personJc);
                    controlJc["position"] = storedPos.ToJC();
                    controlJc["rotation"] = storedRot.ToJC();
                }

                if (!PoseMe.restoreHeadRotation.val)
                {
                    headCtrl.transform.localRotation = headCtrlRot;
                    head.transform.localRotation = headRot;
                    neck.transform.localRotation = neckRot;
                    chest.transform.localRotation = chestRot;
                }
                
                if (personJc.HasKey("handPose"))
                {
                    var dcs = atom.GetStorableByID("geometry") as DAZCharacterSelector;
                    if(dcs.gender.ToString() == personJc["gender"].Value)
                    {
                        ApplyHandPose(atom, personJc["handPose"]);
                    }
                    else $"{atom.uid}: GenderMismatch. Hand pose not restored. Update the pose.".Print();
                }

                if (personJc.HasKey("fingerSprings"))
                {
                    // if(atom == FillMeUp.atom) "LoadSprings".Print();
                    var person = FillMeUp.persons.First(x => x.uid == atom.uid);
                    var jc = personJc["fingerSprings"].AsObject;
                    ApplyFingerSprings(atom.uid, jc);
                }

                if (personJc.HasKey("penis"))
                {
                    var person = FillMeUp.persons.FirstOrDefault(x => x.atom == atom);
                    if (!personJc["penis"].AsObject.HasKey("radii")) return;
                    var radiiJA = personJc["penis"]["radii"].AsArray;
                    for (int i = 0; i < person.penetrator.colliders.Count; i++)
                    {
                        ((CapsuleCollider)person.penetrator.colliders[i]).radius = radiiJA[i].AsFloat;
                    }
                }
            }
            catch (Exception e)
            {
                SuperController.LogError(e.ToString());
            }
        }

        private void RestoreAllControllers()
        {
            foreach (string atomUid in pose["pose"].AsObject.Keys)
            {
                var atom = SuperController.singleton.GetAtomByUid(atomUid);
                if (atom == null) continue;
                if (ShouldAtomBeRestored(atom))
                {
                    RestoreControllers(atom, false);
                }
            }
        }
        
        private void LateRestoreAllControllers()
        {
            foreach (string atomUid in pose["pose"].AsObject.Keys)
            {
                var atom = SuperController.singleton.GetAtomByUid(atomUid);
                if (atom == null) continue;
                if (ShouldAtomBeRestored(atom))
                {
                    RestoreControllers(atom, true);
                }
            }
        }

        private void RestoreControllers(Atom atom, bool late)
        {
            var storables = pose["pose"][atom.uid]["storables"];
            if(atom == containingAtom)
            {
                for (int i = 0; i < atom.freeControllers.Length; i++)
                {
                    var ctrl = atom.freeControllers[i];
                    if (!PoseMe.restoreRoot.val && ctrl == atom.mainController) continue;
                    var data = storables.Childs.FirstOrDefault(x => x["id"].Value == ctrl.name);
                    if (data == null) continue;
                    // ctrl.linkToAtomSelectionPopup.currentValue.Print();
                    
                    // if (ctrl.name.StartsWith("lHand") && data.AsObject.HasKey("linkTo"))
                    // {
                    //     // data["linkTo"].Value.Print();
                    //     var link = data["linkTo"].Value.Split(':');
                    //     // link[0].Print();
                    //     // ctrl.SetLinkToAtom(link[0]);
                    //     // if(ctrl.linkToAtomSelectionPopup == null)
                    //     // {
                    //     //     SuperController.singleton.SelectController(ctrl);
                    //     //     SuperController.singleton.SelectController(PoseMe.atom.mainController);
                    //     // }
                    //     ctrl.linkToAtomSelectionPopup.currentValue.Print();
                    //     ctrl.physicalLocked.Print();
                    //     ctrl.IsCustomPhysicalParamLocked("linkTo").Print();
                    //     // ctrl.linkToAtomSelectionPopup.currentValue = link[0];
                    //     // ctrl.linkToSelectionPopup.currentValue = link[1];
                    //     // ctrl.SetLinkToRigidbodyObject(link[1]);
                    //     // ctrl.SetLinkToRigidbody(data["linkTo"]);
                    //     // ctrl.linkToSelectionPopup.currentValue = data["linkTo"].Value;
                    // }
                    
                    ctrl.RestoreFromJSON(data.AsObject);
                    if(late) ctrl.LateRestoreFromJSON(data.AsObject);
                }
            }
            else
            {
                for (int i = 0; i < atom.freeControllers.Length; i++)
                {
                    var ctrl = atom.freeControllers[i];
                    var data = storables.Childs.FirstOrDefault(x => x["id"].Value == ctrl.name);
                    if (data == null) continue;
                    if (ctrl == atom.mainController)
                    {
                        var storedPos = data["position"].AsObject.ToV3();
                        var storedRot = data["rotation"].AsObject.ToV3();
                        data["position"] = refMatrix.MultiplyPoint(storedPos).ToJC();
                        data["rotation"] = (refMatrix.rotation * Quaternion.Euler(storedRot)).eulerAngles.ToJC();
                        ctrl.RestoreFromJSON(data.AsObject);
                        data["position"] = storedPos.ToJC();
                        data["rotation"] = storedRot.ToJC();
                    }
                    else ctrl.RestoreFromJSON(data.AsObject);
                    if(late) ctrl.LateRestoreFromJSON(data.AsObject);
                }
                
            }
        }

        private void ApplyFingerSprings(string atomUid, JSONClass jc)
        {
            var person = FillMeUp.persons.First(x => x.uid == atomUid);
            for (int i = 0; i < person.lCarpalBones.Length; i++)
            {
                person.lCarpalBones[i].baseJointRotation = jc["leftCarpal"].AsArray[i].AsArray.ToV3();
            }
            for (int i = 0; i < person.lHandBones.Length; i++)
            {
                person.lHandBones[i].baseJointRotation = jc["left"].AsArray[i].AsArray.ToV3();
            }
            for (int i = 0; i < person.rCarpalBones.Length; i++)
            {
                person.rCarpalBones[i].baseJointRotation = jc["leftCarpal"].AsArray[i].AsArray.ToV3();
            }
            for (int i = 0; i < person.rHandBones.Length; i++)
            {
                person.rHandBones[i].baseJointRotation = jc["right"].AsArray[i].AsArray.ToV3();
            }
        }

        public static void ResetFingerSprings()
        {
            var person = FillMeUp.persons.First(x => x.uid == PoseMe.atom.uid);
            for (int i = 0; i < person.lCarpalBones.Length; i++)
            {
                person.lCarpalBones[i].baseJointRotation = Vector3.zero;
            }
            for (int i = 0; i < person.lHandBones.Length; i++)
            {
                person.lHandBones[i].baseJointRotation = Vector3.zero;
            }
            for (int i = 0; i < person.rCarpalBones.Length; i++)
            {
                person.rCarpalBones[i].baseJointRotation = Vector3.zero;
            }
            for (int i = 0; i < person.rHandBones.Length; i++)
            {
                person.rHandBones[i].baseJointRotation = Vector3.zero;
            }
        }
        
        private IEnumerator ApplyDildoPose(Atom atom, JSONClass pose)
        {
            if(pose.HasKey("on")) atom.SetBoolParamValue("on", pose["on"].AsBool);
            yield return new WaitForFixedUpdate();
            // if(!atom.GetBoolParamValue("on")) return;
            var controlJc = pose["control"];
            var pos = controlJc["pos"].AsArray.ToV3();
            var rot = controlJc["rot"].AsArray.ToV3();
            
            atom.mainController.transform.position = refMatrix.MultiplyPoint(pos);
            atom.mainController.transform.rotation = refMatrix.rotation * Quaternion.Euler(rot);
            
            foreach (var rb in atom.GetComponentsInChildren<Rigidbody>())
            {
                rb.position = refMatrix.MultiplyPoint(pose[rb.name]["pos"].AsArray.ToV3());
                rb.rotation = refMatrix.rotation * Quaternion.Euler(pose[rb.name]["rot"].AsArray.ToV3());
            }
        }

        private IEnumerator ApplyToyPose(Atom atom, JSONClass pose)
        {
            atom.collisionEnabled = true;
            if(pose.HasKey("on")) atom.SetBoolParamValue("on", pose["on"].AsBool);
            yield return new WaitForFixedUpdate();
            if(!pose.HasKey("ctrl")) yield break;
            var controlJc = pose["ctrl"].AsObject;
            atom.mainController.RestoreFromJSON(controlJc);
            atom.mainController.LateRestoreFromJSON(controlJc);
            var pos = controlJc["position"].AsObject.ToV3();
            var rot = controlJc["rotation"].AsObject.ToV3();
            
            atom.mainController.transform.position = refMatrix.MultiplyPoint(pos);
            atom.mainController.transform.rotation = refMatrix.rotation * Quaternion.Euler(rot);
            foreach (var rb in atom.GetComponentsInChildren<Rigidbody>(true))
            {
                rb.position = refMatrix.MultiplyPoint(pose[rb.name]["pos"].AsArray.ToV3());
                rb.rotation = refMatrix.rotation * Quaternion.Euler(pose[rb.name]["rot"].AsArray.ToV3());
            }

            if (pose.HasKey("spring"))
            {
                var dildo = FillMeUp.dildos.FirstOrDefault(x => x.penetrator.atom == atom);
                if(dildo) dildo.stiffness.val = pose["spring"].AsFloat;
            }
        }

        private bool ShouldAtomBeRestored(Atom atom, JSONClass poseJc)
        {
            bool typeIncluded;
            if (atom.type == "Person") typeIncluded = atom == containingAtom || PoseMe.restoreOtherPersons.val;
            else typeIncluded = atom.type == "Dildo" ? PoseMe.restoreDildos.val : PoseMe.restoreToys.val;
            if (!typeIncluded) return false;
            JSONStorableBool bJson;
            return actorToggles.TryGetValue(atom, out bJson) && bJson.val && poseJc.HasKey(atom.uid);
        }
        
        private bool ShouldAtomBeRestored(Atom atom)
        {
            bool typeIncluded;
            if (atom.type == "Person") typeIncluded = atom == containingAtom || PoseMe.restoreOtherPersons.val;
            else typeIncluded = atom.type == "Dildo" ? PoseMe.restoreDildos.val : PoseMe.restoreToys.val;
            if (!typeIncluded) return false;
            JSONStorableBool bJson;
            return actorToggles.TryGetValue(atom, out bJson) && bJson.val;
        }

        private void InvokeDialogs(bool onEnter)
        {
            if(PoseMe.ignoreDialogs.val) return;
            if (dialogPoolLevel.val > -1f && DialogPool.dialogs[(int)dialogPoolLevel.val].Count > 0) DialogPool.InvokeRandom((int)dialogPoolLevel.val, onEnter);
            else dialogs.Invoke(onEnter);
        }

        public void ApplyNextCamAngle()
        {
            if (camAngles.Count <= 1)
            {
                Apply();
                return;
            }
            camAngles[(currentCam.id+1) % camAngles.Count].Apply();
        }

        public void ApplyPreviousCamAngle()
        {
            if (camAngles.Count <= 1)
            {
                Apply();
                return;
            }
            int id = currentCam.id - 1;
            if (id == -1) id = camAngles.Count - 1;
            camAngles[id].Apply();
        }

        public void ApplyRandomCamAngle()
        {
            if (camAngles.Count <= 1)
            {
                Apply();
                return;
            }
            camAngles.TakeRandom(currentCam.id).Apply();
        }

        public RectTransform buttonRT;
        private Image backdropImage;
        public static RectTransform blankButtonPrefab;
        private static bool yes;
        
        private GameObject AddButton()
        {
            if (blankButtonPrefab == null)
            {
                blankButtonPrefab = Object.Instantiate(PoseMe.singleton.manager.configurableButtonPrefab) as RectTransform;
                Object.DestroyImmediate(blankButtonPrefab.GetComponentInChildren<UIDynamicButton>());
                Object.DestroyImmediate(blankButtonPrefab.GetComponentInChildren<Button>());
                Object.DestroyImmediate(blankButtonPrefab.GetChild(0).gameObject);
            }
            buttonRT = Object.Instantiate(blankButtonPrefab, PoseMe.buttonGroup.transform, false);
            buttonRT.name = "BL_PoseButtonBackdrop";
            buttonRT.gameObject.layer = 5;
            backdropImage = buttonRT.GetComponent<Image>();
            backdropImage.color = deselectedBGColor;

            RectTransform button = Object.Instantiate(PoseMe.singleton.manager.configurableButtonPrefab) as RectTransform;
            button.SetParent(buttonRT, false);
            button.name = "BL_PoseButton";
            button.anchorMax = Vector2.one;
            button.anchorMin = Vector2.zero;
            button.offsetMax = new Vector2(-5,-5);
            button.offsetMin = new Vector2(5,5);

            uiButton = button.GetComponent<UIDynamicButton>();
            uiButton.label = _id.ToString();
            uiButton.buttonText.fontSize = 25;
            uiButton.buttonText.color = Color.white;
            uiButton.button.image.color = deselectedColor;
            uiButton.button.image.material.shader = Shader.Find("UI/Default-Overlay");
            var cb = uiButton.button.colors;
            cb.pressedColor = cb.highlightedColor = cb.normalColor;
            uiButton.button.colors = cb;
            clickListener = button.gameObject.AddComponent<ClickListener>();
            clickListener.onLeftClick.AddListener(() =>
            {
                if (PoseMe.currentPose == this) PoseMe.onLeftClick.Invoke();
                else
                {
                    Apply();
                    if(PoseMe.leftClickChooser.val.StartsWith("R")) ApplyRandomCamAngle();
                }
            });
            clickListener.onRightClick.AddListener(() =>
            {
                if (PoseMe.currentPose == this) PoseMe.onRightClick.Invoke();
                else
                {
                    Apply();
                    if(PoseMe.rightClickChooser.val.StartsWith("R")) ApplyRandomCamAngle();
                }
            });
            clickListener.onMiddleClick.AddListener(() => Apply(false));
            clickListener.onPointerEnter.AddListener(() => {
                PoseMe.PrintInputConfig();
                PoseMe.buttonHovered = true;
                if(!selected) return;
                GlobalSceneOptions.singleton.disableNavigation = true;
                SuperController.singleton.disableNavigation = true;
                
            });
            clickListener.onPointerExit.AddListener(() => {
                GlobalSceneOptions.singleton.disableNavigation = false;
                SuperController.singleton.disableNavigation = false;
                PoseMe.buttonHovered = false;
            });
            clickListener.onDragUp.AddListener(PoseMe.onDragUp);
            clickListener.onDragDown.AddListener(PoseMe.onDragDown);
            clickListener.onDragLeft.AddListener(PoseMe.onDragLeft);
            clickListener.onDragRight.AddListener(PoseMe.onDragRight);
            
            // var lVRHand = SuperController.singleton.GetAtomByUid("[CameraRig]").transform.Find("HeightOffset/Hands/LeftHandPhysical/MaleHand02L/HandPhysical/L_Arm_Cut/L_Wrist");
            // if (!yes)
            // {
            //     canvasGO = new GameObject();
            //     var canvas = canvasGO.AddComponent<Canvas>();
            //     canvasGO.AddComponent<GraphicRaycaster>();
            //     canvasGO.AddComponent<IgnoreCanvas>();
            //     canvas.renderMode = RenderMode.WorldSpace;
            //     canvas.worldCamera = Camera.main;
            //     SuperController.singleton.AddCanvas(canvas);
            //     canvasGO.name = "WristButton";
            //     canvasGO.transform.localPosition = new Vector3(-.3f, .5f, 1f);
            //     canvasGO.transform.localScale = .005f * Vector3.one;
            //     canvasGO.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
            //     Object.Instantiate(buttonRT, canvasGO.transform);
            //     var wbutton = canvasGO.GetComponentInChildren<UIDynamicButton>();
            //     var cl = wbutton.GetComponentInChildren<ClickListener>();
            //     
            //     // cl.onLeftClick.AddListener(() => { 111.Print(); });
            //     cl.Clone(clickListener);
            //     yes = true;
            // }
            return buttonRT.gameObject;
        }

        private GameObject canvasGO;
        public void RemoveButton()
        {
            Object.DestroyImmediate(uiButton.transform.parent.gameObject);
        }

        public void StoreImage(string jsonPath)
        {
            if(camAngles.Count > 0 && camAngles[0].img != null) FileManagerSecure.WriteAllBytes(jsonPath.Replace(".json", ".jpg"), camAngles[0].img);
        }

        // public void DeleteFromDisk()
        // {
        //     if (!FileManagerSecure.FileExists(path.val + ".json")) return;
        //     FileManagerSecure.DeleteFile(path.val+".json");
        //     FileManagerSecure.DeleteFile(path.val+".jpg");
        // }

        public void StoreBackup()
        {
            var backupPath = $"{PoseMe.backUpStorePath}{PoseMe.sceneType}_{DateTime.Now:yyyy-MM-dd-HHMMss}";
            if(camAngles.Count > 0 && camAngles[0].img != null) FileManagerSecure.WriteAllBytes(backupPath+".jpg", camAngles[0].img);
            SuperController.singleton.SaveJSON(pose, backupPath+".json");
            // FileManagerSecure.MoveFile(path.val+".json", backupName + ".json");
            // FileManagerSecure.MoveFile(path.val + ".jpg", backupName + ".jpg");
            $"PoseMe: Backup stored at {backupPath}.".Print();
        }

        public void SyncAtomNames(string oldUid, string newUid)
        {
            onPoseExit.SyncAtomNames();
            onPoseExit.SyncAtomNames();
            var atomPoses = pose["pose"];
            if(atomPoses.AsObject.HasKey(oldUid))
            {
                atomPoses[newUid] = atomPoses[oldUid];
                atomPoses.Remove(oldUid);
            }
            dialogs.OnPersonRenamed(oldUid, newUid);

            foreach (var camAngle in camAngles)
            {
                camAngle.dialogs.OnPersonRenamed(oldUid, newUid);
            }
        }
        
        public void InvokeRandomSlapAction(int type)
        {
            if(slaps.Count == 0) return;
            var i = slaps.Count == 1 ? 0 : Random.Range(0, slaps.Count);
            slaps[i].Invoke(type, true);
        }

        private void TakeScreenshot(Action<byte[]> callback)
        {
            // var xrayState = XRay.toggleState;
            // if(xrayState) XRay.Toggle();
            SuperController.singleton.DoSaveScreenshot(PoseMe.tmpPath+"1.jpg", val =>
            {
                callback(GetImageBytes(val));
                // if(xrayState) XRay.Toggle();
            });
        }

        private byte[] GetImageBytes(string imagePath)
        {
            // if(!FileManagerSecure.FileExists(imagePath)) return null;
            var image = FileManagerSecure.ReadAllBytes(imagePath);
            FileManagerSecure.DeleteFile(imagePath);
            return image;
        }

        private void UpdateCurrentAngle(byte[] image)
        {
            currentCam.Update(image);
            SetButtonImage();
            SetUIImage(image);
            PoseMe.worldCanvas.Sync();
        }

        private void AddCamAngle(byte[] image)
        {
            int id;
            if (camAngles.Count == 0 || currentCam == null) id = 0;
            else id = currentCam.id + 1;
            currentCam = new CamAngle(this, image);
            camAngles.Insert(id, currentCam);
            SyncCamIds();
            SetButtonImage();
            SetUIImage(image);
            
            var choices = PoseMe.camChooser.choices;
            choices.Clear();
            for (int i = 0; i < camAngles.Count; i++)
            {
                choices.Add(i.ToString());
            }
            PoseMe.camChooser.choices = null;
            PoseMe.camChooser.choices = choices;
            PoseMe.camChooser.valNoCallback = id.ToString();
            Story.currentLevel?.SyncButtons();
        }

        private void SyncCamIds()
        {
            for (int i = 0; i < camAngles.Count; i++)
            {
                camAngles[i].id = i;
            }
        }

        public void MoveAngle(bool up)
        {
            if(currentCam == null) return;
            int i = currentCam.id;
            if (up)
            {
                if (i == 0) return;
                var upperPose = camAngles.LastOrDefault(x => x.id < i);
                if (upperPose != null)
                {
                    camAngles[i-1] = currentCam;
                    camAngles[i] = upperPose;
                    upperPose.id++;
                    currentCam.id--;
                }

                PoseMe.camChooser.valNoCallback = (i - 1).ToString();
            }
            else
            {
                if(i == camAngles.Count - 1) return;
                var lowerPose = camAngles.FirstOrDefault(x => x.id > i);
                if (lowerPose != null)
                {
                    camAngles[i+1] = currentCam;
                    camAngles[i] = lowerPose;
                    lowerPose.id--;
                    currentCam.id++;
                }
            }
            SetButtonImage();
            PoseMe.camChooser.valNoCallback = currentCam.id.ToString();
        }

        public void AddSlap()
        {
            var slap = PoseMe.singleton.gameObject.AddComponent<Slap>().Init(this);
            slaps.Add(slap);
            if (PoseMe.currentTab == 3) PoseMe.slapItems.Add(PoseMe.CreateUIDynamicSlapItem(slap, false));
        }

        private static List<JSONClass> cachedSlaps = new List<JSONClass>();
        private static List<JSONClass> cachedMovements = new List<JSONClass>();

        public void CopySlaps()
        {
            cachedSlaps.Clear();
            for (int i = 0; i < slaps.Count; i++)
            {
                cachedSlaps.Add(slaps[i].Store());
            }
        }

        public void PasteSlaps()
        {
            for (int i = 0; i < cachedSlaps.Count; i++)
            {
                var slap = PoseMe.singleton.gameObject.AddComponent<Slap>().Init(this, cachedSlaps[i]);
                slaps.Add(slap);
                if (PoseMe.currentTab == 3) PoseMe.slapItems.Add(PoseMe.CreateUIDynamicSlapItem(slap, false));
            }
        }
        
        public void CopyMovements()
        {
            cachedMovements.Clear();
            for (int i = 0; i < movements.Count; i++)
            {
                cachedMovements.Add(movements[i].Store());
            }
        }
        
        public void PasteMovements()
        {
            for (int i = 0; i < cachedMovements.Count; i++)
            {
                var movement = new Movement(this, cachedMovements[i]);
                movements.Add(movement);
                if (PoseMe.currentTab == 3) PoseMe.movementItems.Add(Movement.CreateUIDynamicMovement(movement, true));
            }
        }

        public List<Movement> movements = new List<Movement>();
        public void AddMovement()
        {
            var movement = new Movement(this);
            movements.Add(movement);
            if (PoseMe.currentTab == 3) PoseMe.movementItems.Add(Movement.CreateUIDynamicMovement(movement, true));
        }
        
        // public void FetchThumbnail(string path)
        // {
        //     img = FileManagerSecure.ReadAllBytes(path);
        //     FileManagerSecure.WriteAllBytes(this.path.val+".jpg", img);
        //     SetButtonImage(PoseMe.showThumbnails.val);
        // }
        
        public void SetButtonImage()
        {
            if(PoseMe.showThumbnails.val && camAngles.Count > 0 && camAngles[0].img != null)
            {
                var texture = LoadTexture(camAngles[0].img);
                uiButton.button.image.sprite = Sprite.Create(texture,
                    new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                uiButton.button.image.fillMethod = Image.FillMethod.Radial360;
                uiButton.button.image.fillAmount = 0f;
                // uiButton.button.image.type = Image.Type.Filled;
            }
            else
            {
                uiButton.button.image.sprite = null;
            }
            SetButtonText();
        }

        public void SetButtonText()
        {
            if (PoseMe.showThumbnails.val)
            {
                var text = $"{currentCam.id} / {camAngles.Count - 1}";
                if(camAngles.Count > 1)
                {
                    uiButton.buttonText.alignment = TextAnchor.LowerRight;
                    uiButton.buttonText.color = Color.white;
                    uiButton.buttonText.fontSize = 25;
                    // uiButton.buttonText.text = currentAngle.id.ToString();
                    uiButton.buttonText.text = text;
                }
                else uiButton.buttonText.color = Color.clear;
                WorldCanvas.SetButtonText(this, text);
            }
            else
            {
                uiButton.buttonText.alignment = TextAnchor.MiddleCenter;
                uiButton.buttonText.color = Color.black;
                if(PoseMe.buttonSizeJ.val >= PoseMe.buttonSizeJ.defaultVal) uiButton.buttonText.fontSize = 30;
                else uiButton.buttonText.fontSize = (int)(30 * PoseMe.buttonSizeJ.val / PoseMe.buttonSizeJ.defaultVal);
                uiButton.buttonText.text = camAngles.Count > 1 ? $"{id} / {currentCam.id}" : id.ToString();
            }
        }

        public void SetUIImage(byte[] image = null)
        {
            if(camAngles.Count == 0) return;
            if (image == null)
            {
                image = camAngles[0].img;
                if(image == null) return;
            }
            var texture = LoadTexture(image);
            if(PoseMe.currentTab == 0 && !IdleUIProvider.uiOpen)
            {
                PoseMe.UIImage.sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                PoseMe.UIImageButton.buttonText.text = $"{currentCam.id} / {camAngles.Count-1}";
            }
        }

        public static Texture2D LoadTexture(string path)
        {
            var tex = new Texture2D(2, 2);
            var data = FileManagerSecure.ReadAllBytes(path);
            tex.LoadImage(data);
            
            // var pixels = tex.GetPixels();
            // for (int i = 0; i < pixels.Length; i++)
            // {
            //     var pixel = pixels[i];
            //     
            //     if (pixel.ToHSV().V < .1f)
            //     {
            //         // pixel.ToHSV().V.Print();
            //         // pixel.a = 0f;
            //         // pixels[i] = pixel;
            //         var col = Color.white;
            //         col.a = 0f;
            //         pixels[i] = col;
            //     }
            //     // pixels[i] *= Color.Lerp(color, pixels[i], maskPixels[i].a);
            // }
            // tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private static Texture2D LoadTexture(byte[] image)
        {
            var tex = new Texture2D(2, 2);
            tex.LoadImage(image);
            tex.Apply();
            return tex;
        }
    }
}