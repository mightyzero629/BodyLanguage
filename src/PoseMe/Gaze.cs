using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace CheesyFX
{
    public class Gaze
    {
        public bool initialized;
        private static bool globalsRestored;
        private bool isRestored;
        public static bool internalEnabled = true;
        public static bool tempDisabled = false;
        public bool update = true;
        private bool enabled;
        private Atom atom;
        public EyesControl eyeBehavior;
        private DAZMeshEyelidControl eyelidBehavior;
        private Transform centerEye;
        private GazeTarget target;
        private Transform subTarget;
        private bool tagetIsVirtual;
        private Transform targetGO;
        public static List<GazeTarget> targets;
        private List<GazeTarget> validTargets = new List<GazeTarget>();
        private static List<MirrorObject> mirrors;
        private MirrorObject mirror;
        private List<float> interests = new List<float>();
        private static PlayerFace playerFace;
        private PersonFace face;
        private static VRHand lVRHand;
        private Rigidbody head;
        private Rigidbody neck;
        private Vector3 torque;
        private Vector3 neckTorque;
        private float gazeSpeed;
        private FreeControllerV3 headCtrl;

        public JSONStorableBool enabledJ = new JSONStorableBool("Enabled", true);
        
        public static GazeSettings gazeSettings;
        
        
        public static JSONStorableStringChooser environmentChooser =
            new JSONStorableStringChooser("environment", null, null, "Environment", OnEnvironmentChanged);

        private static JSONStorableStringChooser objectChooser =
            new JSONStorableStringChooser("objectChooser", null, null, "Add Object", RegisterObjectFromChooser);
        private static JSONStorableStringChooser targetAtomFilter =
            new JSONStorableStringChooser("targetAtomFilter", new List<string>(), null, "Atom", (string val) =>
            {
                if(PoseMe.currentTab == 4 && currentTab == 2) SelectTab(currentTab);
            });

        public static JSONStorableAction focus =
            new JSONStorableAction("Focus Now", FocusOnDemand);
        
        public static JSONStorableAction tempDisable =
            new JSONStorableAction("Temp Disable Gaze (not stored)", () => tempDisabled = !tempDisabled);
        
        private JSONStorableString targetInfo = new JSONStorableString("", "");
        private static JSONStorableString performanceInfo = new JSONStorableString("", 
            "Targets are areas of interest.\n" +
            "Persons: Body regions\nObjects: Meshes inside the object.\n\n" +
            "SubTargets are points of interest inside those targets.\n" +
            "Persons: Colliders forming the body region\nObjects: Vertices on the mesh object\n\n" +
            "TargetOcclusion: Only unobstructed targets can be selected.\n\n" +
            "SubTarget Occlusion: Only unobstructed SubTargets inside the current target can be selected (persons only).");

        private static float dynamicGazeStrength;
        private static float gazeCosine;
        private float targetTimer;
        private float subTargetTimer;
        private float focusTimer;
        private bool focussing;
        private Vector3 meshVertex;
        private EyesControl.LookMode cachedLookMode;
        private static RaycastHit[] rayCastBuffer = new RaycastHit[50];
        private static List<Gaze> gazes = new List<Gaze>();
        public Renderer targetRenderer;
        private LineRenderer viewRenderer;
        private Vector3[] viewPoints = new Vector3[3];
        private static bool debug;
        private IEnumerator deferredInit;
        private static List<ObjectTarget> customTargets = new List<ObjectTarget>();
        // public static Dictionary<Atom, JSONStorableFloat> personInterests = new Dictionary<Atom, JSONStorableFloat>();

        private static ObjectTarget environment;
        private static CustomUnityAssetLoader environmentCuaLoader;

        private UnityEventsListener uiEnabledListener;

        private static UIDynamicTabBar tabbar;

        public Gaze(Atom atom)
        {
            this.atom = atom;
            targetGO = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            targetGO.name = $"BL_{atom.uid}_GazeTarget";
            Object.Destroy(targetGO.GetComponent<Collider>());
            targetRenderer = targetGO.GetComponent<Renderer>();
            targetRenderer.enabled = false;
            var material = targetRenderer.material;
            var dcs = atom.GetStorableByID("geometry") as DAZCharacterSelector;
            material.color = dcs.gender == DAZCharacterSelector.Gender.Female? ReadMyLips.femaleStimColor.val.ToRGB() : ReadMyLips.maleStimColor.val.ToRGB();
            material.shader = FillMeUp.debugShader;
            targetGO.localScale = new Vector3(.005f, .005f, .005f);
            
            
            var viewRenderGO = new GameObject($"BL_{atom.uid}_GazeViewRenderer");
            viewRenderer = viewRenderGO.AddComponent<LineRenderer>();
            viewRenderer.useWorldSpace = true;
            viewRenderer.material = new Material(Shader.Find("Sprites/Default")) {renderQueue = 4000};
            viewRenderer.material.color = dcs.gender == DAZCharacterSelector.Gender.Female? ReadMyLips.femaleStimColor.val.ToRGB() : ReadMyLips.maleStimColor.val.ToRGB();
            // SetLineColor(viewRenderer, Color.green);
            viewRenderer.widthMultiplier = 0.0004f;
            viewRenderer.positionCount = 3;

            eyelidBehavior = (DAZMeshEyelidControl) atom.GetStorableByID("EyelidControl");
            head = atom.forceReceivers.First(x => x.name == "head").GetComponent<Rigidbody>();
            headCtrl = atom.freeControllers.First(x => x.name == "headControl");
            neck = head.transform.parent.GetComponent<Rigidbody>();
            if (targets == null) StaticInit();
            
            enabledJ.name = $"{atom.uid} Enabled";
            enabledJ.setCallbackFunction += val =>
            {
                if (!eyeBehavior)
                {
                    enabled = false;
                    return;
                }
                enabled = val;
                if (val)
                {
                    var rotationState = headCtrl.currentRotationState;
                    if (!(rotationState == FreeControllerV3.RotationState.On || rotationState == FreeControllerV3.RotationState.ParentLink))
                    {
                        enabledJ.valNoCallback = false;
                        $"BL: Gaze on atom {atom.uid} not enabled. Head controller rotation state has to be either 'On' or 'Parent Link'.".Print();
                        return;
                    }
                    eyeBehavior.currentLookMode = EyesControl.LookMode.Custom;
                    eyeBehavior.SetStringChooserParamValue("targetAtom", "None");
                    eyeBehavior.lookAt1.target = targetGO;
                    eyeBehavior.lookAt2.target = targetGO;
                    SelectRandomTarget();
                }
                else
                {
                    eyeBehavior.currentLookMode = cachedLookMode;
                }
                if (debug)
                {
                    viewRenderer.gameObject.SetActive(val);
                    targetRenderer.enabled = val;
                }
            };
            
            gazes.Add(this);
            SyncEnvironmentChooser();
            // PoseMe.singleton.RegisterAction(focusOnTarget2);
            // PoseMe.singleton.RegisterAction(focusOnTarget5);
            // PoseMe.singleton.RegisterAction(focusOnTarget10);
            deferredInit = DeferredInit().Start();
        }

        private IEnumerator DeferredInit()
        {
            yield return new WaitUntil(() => !SuperController.singleton.loadingIcon.gameObject.activeSelf &&
                                              !SuperController.singleton.loadingUI.gameObject.activeSelf &&
                                              (eyeBehavior = (EyesControl)atom.GetStorableByID("Eyes")) != null);
            cachedLookMode = eyeBehavior.currentLookMode;
            dynamicGazeStrength = gazeSettings.gazeStrength.val;
            if (enabledJ.val)
            {
                eyeBehavior.currentLookMode = EyesControl.LookMode.Custom;
                var ui = atom.transform.Find("UI/UIPlaceHolderModel").gameObject;
                uiEnabledListener = ui.AddComponent<UnityEventsListener>();
                uiEnabledListener.onEnabled.AddListener(() =>
                {
                    if(!enabledJ.val) return;
                    eyeBehavior.lookAt1.target = targetGO;
                    eyeBehavior.lookAt2.target = targetGO;
                });
                // ui.AddComponent<UnityEventsListener>().onDisabled.AddListener(() => "ui disabled".Print());
                eyeBehavior.SetStringChooserParamValue("targetAtom", "None");
                eyeBehavior.lookAt1.target = targetGO;
                eyeBehavior.lookAt2.target = targetGO;
                eyeBehavior.lookAt1.MinEngageDistance = eyeBehavior.lookAt2.MinEngageDistance = 0f;
            }
            centerEye = eyeBehavior.lookAt1.centerForDepthAdjust;
            if (!isRestored && atom != FillMeUp.atom) enabledJ.val = false;
            enabled = enabledJ.val;
            initialized = true;
            SelectRandomTarget();
        }

        public static void StaticInit()
        {
            targets = new List<GazeTarget>();
            gazeSettings = new GazeSettings();
            playerFace = new PlayerFace();
            targets.Add(playerFace);
            targets.Add(new VRHand());
            targets.Add(new VRHand("r"));
            
            foreach (var a in SuperController.singleton.GetAtoms())
            {
                if(a.type == "ToyBP" || a.type == "ToyAH" || a.type == "Paddle") RegisterObject(a);
            }
            GetMirrors();
            gazeSettings.gazeAngle.AddCallback(val => gazeCosine = Mathf.Cos(Mathf.PI*val/180f));
            // environmentChooser.val = "CustomUnityAsset";
            
            // playerFace.interest.val = 0f;
            SyncObjectChooser();
            SetCallbacks();
            // RegisterObject(SuperController.singleton.GetAtomByUid("AptChair"));
        }

        public static void StoreGlobals(JSONClass parent)
        {
            var jc = new JSONClass();
            for (int i = 0; i < gazes.Count; i++)
            {
                gazes[i].enabledJ.Store(jc);
            }

            var ja = new JSONArray();
            for (int i = 0; i < customTargets.Count; i++)
            {
                var tgt = customTargets[i];
                if(tgt == environment) continue;
                ja.Add(tgt.name);
            }
            jc["customTargets"] = ja;
            environmentChooser.Store(jc);
            gazeSettings.Store(jc);
            parent["gaze"] = jc;
        }

        public static void LoadGlobals(JSONClass parent)
        {
            if(!parent.HasKey("gaze")) return;
            var jc = parent["gaze"].AsObject;
            for (int i = 0; i < gazes.Count; i++)
            {
                var gaze = gazes[i];
                gaze.enabledJ.Load(jc);
                gaze.isRestored = jc.HasKey(gaze.enabledJ.name);
            }

            foreach (var target in jc["customTargets"].Childs)
            {
                var targetAtom = SuperController.singleton.GetAtomByUid(target.Value);
                if(targetAtom == null) continue;
                RegisterObject(targetAtom);
            }
            environmentChooser.Load(jc);
            gazeSettings.Load(jc);
            globalsRestored = true;
        }

        private static void SetCallbacks()
        {
            gazeSettings.gazeStrength.setCallbackFunction += val =>
            {
                if (PoseMe.currentPose != null)
                {
                    PoseMe.currentPose.gazeSettings.gazeStrength.val = val;
                }

                lerpGazeStrength.Stop();
                if (FillMeUp.throat.isPenetrated)
                {
                    lerpGazeStrength = LerpGazeStrength(true).Start();
                }
                else dynamicGazeStrength = val;

                if (val <= 0f)
                {
                    for (int i = 0; i < gazes.Count; i++)
                    {
                        gazes[i].torque = Vector3.zero;
                    }
                }
            };
            gazeSettings.gazeStrengthDuringBJ.setCallbackFunction += val =>
            {
                if (PoseMe.currentPose != null)
                {
                    PoseMe.currentPose.gazeSettings.gazeStrengthDuringBJ.val = val;
                }
                if (FillMeUp.throat.isPenetrated)
                {
                    lerpGazeStrength.Stop();
                    lerpGazeStrength = LerpGazeStrength(true).Start();
                }
            };
            gazeSettings.gazeSpeedMean.setCallbackFunction += val =>
            {
                if (PoseMe.currentPose != null) PoseMe.currentPose.gazeSettings.gazeSpeedMean.val = val;
            };
            gazeSettings.gazeSpeedDelta.setCallbackFunction += val =>
            {
                if (PoseMe.currentPose != null) PoseMe.currentPose.gazeSettings.gazeSpeedDelta.val = val;
            };
            gazeSettings.gazeSpeedOnesided.setCallbackFunction += val =>
            {
                if (PoseMe.currentPose != null) PoseMe.currentPose.gazeSettings.gazeSpeedOnesided.val = val;
            };
            gazeSettings.targetTimeMean.setCallbackFunction += val =>
            {
                if (PoseMe.currentPose != null) PoseMe.currentPose.gazeSettings.targetTimeMean.val = val;
            };
            gazeSettings.targetTimeDelta.setCallbackFunction += val =>
            {
                if (PoseMe.currentPose != null) PoseMe.currentPose.gazeSettings.targetTimeDelta.val = val;
            };
            gazeSettings.targetTimeOnesided.setCallbackFunction += val =>
            {
                if (PoseMe.currentPose != null) PoseMe.currentPose.gazeSettings.targetTimeOnesided.val = val;
            };
            gazeSettings.subTargetTimeMean.setCallbackFunction += val =>
            {
                if (PoseMe.currentPose != null) PoseMe.currentPose.gazeSettings.subTargetTimeMean.val = val;
            };
            gazeSettings.subTargetTimeDelta.setCallbackFunction += val =>
            {
                if (PoseMe.currentPose != null) PoseMe.currentPose.gazeSettings.subTargetTimeDelta.val = val;
            };
            gazeSettings.subTargetTimeOnesided.setCallbackFunction += val =>
            {
                if (PoseMe.currentPose != null) PoseMe.currentPose.gazeSettings.subTargetTimeOnesided.val = val;
            };
            gazeSettings.autoTarget.setCallbackFunction += val =>
            {
                if (PoseMe.currentPose != null) PoseMe.currentPose.gazeSettings.autoTarget.val = val;
            };
            gazeSettings.touchReactionsEnabled.setCallbackFunction += val =>
            {
                if (PoseMe.currentPose != null) PoseMe.currentPose.gazeSettings.touchReactionsEnabled.val = val;
            };
            gazeSettings.gazeAngle.setCallbackFunction += val =>
            {
                if (PoseMe.currentPose != null) PoseMe.currentPose.gazeSettings.gazeAngle.val = val;
            };
            gazeSettings.selfInterest.setCallbackFunction += val =>
            {
                if (PoseMe.currentPose != null) PoseMe.currentPose.gazeSettings.selfInterest.val = val;
            };
            gazeSettings.targetOcclusion.setCallbackFunction += val =>
            {
                if (PoseMe.currentPose != null) PoseMe.currentPose.gazeSettings.targetOcclusion.val = val;
                if(!val) gazeSettings.subTargetOcclusion.val = false;
            };
            gazeSettings.subTargetOcclusion.setCallbackFunction += val =>
            {
                if (val && !gazeSettings.targetOcclusion.val) gazeSettings.targetOcclusion.val = true; 
                if (PoseMe.currentPose != null) PoseMe.currentPose.gazeSettings.subTargetOcclusion.val = val;
            };
            gazeSettings.useMirrors.setCallbackFunction += val =>
            {
                if (PoseMe.currentPose != null) PoseMe.currentPose.gazeSettings.useMirrors.val = val;
                for (int i = 0; i < targets.Count; i++)
                {
                    foreach (var item in targets[i].mirrors)
                    {
                        item.Value.Clear();
                    }
                }
            };
            gazeSettings.environmentEnabled.setCallbackFunction += val =>
            {
                if (PoseMe.currentPose != null) PoseMe.currentPose.gazeSettings.environmentEnabled.val = val;
                if (environment != null) environment.enabled.val = val;
            };
        }

        private static void RegisterObject(Atom atom, bool custom = false, bool isEnvironment = false)
        {
            if(atom.type != "CustomUnityAsset")
            {
                var meshes = new List<ObjectTarget.SubMesh>();
                // cua.reParentObject.PrintHierarchy();
                foreach (var meshFilter in atom.reParentObject.GetComponentsInChildren<MeshFilter>())
                {
                    var sharedMesh = meshFilter.sharedMesh;
                    if(!sharedMesh.isReadable) continue;
                    var verts = sharedMesh.vertices;
                    if (verts.Length == 0) continue;
                    meshes.Add(new ObjectTarget.SubMesh(meshFilter.transform, verts));
                    // meshFilter.transform.Draw();
                    // $"{sharedMesh.vertexCount}/{sharedMesh.vertices.Length}".Print();
                }

                var target = new ObjectTarget(atom, meshes, atom.uid);
                RegisterTarget(target);
                if (custom)
                {
                    customTargets.Add(target);
                    if(globalsRestored) PoseMe.CreateUIDynamicGazeTarget(target, false, UIElements);
                }
            }
            else
            {
                
                var target = new CuaTarget(atom, null, atom.uid);
                if (isEnvironment)
                {
                    environment = target;
                    target.interest.SetWithDefault(.3f);
                }
                RegisterTarget(target);
                customTargets.Add(target);
                SyncEnvironmentChooser();
                if (!isEnvironment && globalsRestored)
                {
                    PoseMe.CreateUIDynamicGazeTarget(target, false, UIElements);
                }
            }
            SyncObjectChooser();
        }

        public static GazeTarget RegisterPerson(Person person, Gaze gaze)
        {
            GazeTarget penis;
            if (targets == null)
            {
                targets = new List<GazeTarget>();
            }

            gaze.face = new PersonFace(person.atom);
            
            targets.Add(new Hand(person));
            targets.Add(new Hand(person, "r"));
            targets.Add(new Breast(person));
            targets.Add(new Breast(person, "r"));
            targets.Add(new Ass(person));
            targets.Add(new Ass(person, "r"));
            if (person.characterListener.gender == DAZCharacterSelector.Gender.Male)
            {
                penis = new Penis(person);
                targets.Add(penis);
                foreach (var tgt in targets.Where(x => x.atom == person.atom && x is Breast))
                {
                    tgt.interest.SetWithDefault(.3f);
                }
            }
            else
            {
                targets.Add(new Thigh(person));
                targets.Add(new Thigh(person, "r"));
                targets.Add(new Shin(person));
                targets.Add(new Shin(person, "r"));
                targets.Add(new Foot(person));
                targets.Add(new Foot(person, "r"));
                targets.Add(new Anus(person.atom));
                targets.Add(new Pussy(person.atom));
                penis = new Penis(person)
                {
                    enabled = {val = person.characterListener.isFuta}
                };
                person.characterListener.OnChangedToFuta.AddListener(() => penis.enabled.val = true);
                person.characterListener.OnChangedToFemale.AddListener(() => penis.enabled.val = false);
                targets.Add(penis);
            }
            targets.Add(new Pelvis(person));
            targets.Add(gaze.face);

            SyncTargetMirrors();
            SyncFocusTargetChooser();
            SyncTargetFilter();
            for (int i = 0; i < PoseMe.poses.Count; i++)
            {
                PoseMe.poses[i].gazeSettings.personInterests[person.atom] = new JSONStorableFloat(person.atom.uid+" Interest", 1f, 0f, 1f, false);
            }
            gazeSettings.personInterests[person.atom] = new JSONStorableFloat(person.atom.uid+" Interest", 1f, 
                val =>
                {
                    if (PoseMe.currentPose != null)
                    {
                        var atomInterests = PoseMe.currentPose.gazeSettings.personInterests;
                        atomInterests[person.atom].val = val;
                    }
                }, 0f, 1f, false);
            return penis;
        }
        
        public static void RegisterTarget(GazeTarget target)
        {
            if (targets == null)
            {
                targets = new List<GazeTarget>();
            }
            targets.Add(target);
            SyncTargetMirrors();
            SyncFocusTargetChooser();
            SyncTargetFilter();
        }

        private static void SyncTargetMirrors()
        {
            foreach (var gaze1 in gazes)
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    List<MirrorObject> mirs;
                    if(!targets[i].mirrors.TryGetValue(gaze1, out mirs))
                    {
                        targets[i].mirrors[gaze1] = new List<MirrorObject>();
                        // $"{gaze.atom.name}: {targets[i].name}".Print();
                    }
                }
            }
        }

        public static void DeregisterAtom(Atom atom)
        {
            foreach (var target in targets.Where(x => x.atom == atom))
            {
                for (int i = 0; i < PoseMe.poses.Count; i++)
                {
                    PoseMe.poses[i].gazeSettings.targetSettings.Remove(target);
                }
                target.Destroy();
            }
            targets.RemoveAll(x => x.atom == atom);
            customTargets.RemoveAll(x => x.atom == atom);
            if (atom.type == "Person")
            {
                gazes.Remove(gazes.FirstOrDefault(x => x.atom == atom));
                gazeSettings.personInterests.Remove(atom);
                for (int i = 0; i < PoseMe.poses.Count; i++)
                {
                    PoseMe.poses[i].gazeSettings.personInterests.Remove(atom);
                }
                if (debug && currentTab == 3)
                {
                    SelectTab(currentTab);
                }
            }
            SyncObjectChooser();
            SyncEnvironmentChooser();
            SyncFocusTargetChooser();
            SyncTargetFilter();
        }

        private static void FocusOnDemand()
        {
            if (string.IsNullOrEmpty(gazeSettings.focusTargetChooser.val))
            {
                "Select a target first.".Print();
                return;
            }
            var focusTarget = targets.First(x => x.name == gazeSettings.focusTargetChooser.val);
            for (int i = 0; i < gazes.Count; i++)
            {
                var gaze = gazes[i];
                gaze.focusTimer = gazeSettings.focusDuration.val;
                gaze.focussing = true;
                gaze.SelectTarget(focusTarget, .22f);
            }
            // focusTargetChooser.valNoCallback = "";
        }

        private static void SyncFocusTargetChooser()
        {
            gazeSettings.focusTargetChooser.SetChoices(targets.Select(x => x.name));
            if (!gazeSettings.focusTargetChooser.choices.Contains(gazeSettings.focusTargetChooser.val)) gazeSettings.focusTargetChooser.valNoCallback = "";
        }

        private static void SyncEnvironmentChooser()
        {
            environmentChooser.SetChoices(SuperController.singleton.GetAtoms().Where(x => customTargets.All(t => t.atom != x) && x.type == "CustomUnityAsset").Select(x => x.uid));
            environmentChooser.InsertChoice("", 0);
        }

        private static void SyncObjectChooser()
        {
            objectChooser.SetChoices(SuperController.singleton.GetAtoms().Where(x => customTargets.All(t => t.atom != x) &&!x.IsToyOrDildo() && x.type != "Person").Select(x => x.uid));
        }
        
        private static void SyncTargetFilter()
        {
            targetAtomFilter.SetChoices(targets.Select(x => x.atom.uid).Distinct());
            if (!targetAtomFilter.choices.Contains(targetAtomFilter.val)) targetAtomFilter.val = "";
        }

        private Vector3 saccade;
        private Vector3 GetSaccade()
        {
            return new Vector3(Random.Range(-.05f, .05f), Random.Range(-.05f, .05f), Random.Range(-.05f, .05f));
        }

        public void Focus(GazeTarget focusTarget, float minSpeed, float time = 2f)
        {
            focusTimer = time;
            focussing = true;
            SelectTarget(focusTarget, minSpeed);
            // $"Fucusing {focusTarget.atom.uid}:{focusTarget.name}".Print();
        }

        public static void FocusAll(GazeTarget focusTarget)
        {
            if (focusAllCo == null) focusAllCo = FocusAllCo().Start();
            focusAllTargets.Add(focusTarget);
        }

        private static IEnumerator focusAllCo;
        private static readonly List<GazeTarget> focusAllTargets = new List<GazeTarget>();
        private static IEnumerator FocusAllCo()
        {
            yield return new WaitForEndOfFrame();
            focusAllTargets.RemoveAll(x => x.interest.val == 0f);
            if (focusAllTargets.Count == 0) yield break;
            var rng = focusAllTargets.Count == 1? 0 : Random.Range(0, focusAllTargets.Count);
            var tgt = focusAllTargets[rng];
            for (int i = 0; i < gazes.Count; i++)
            {
                gazes[i].Focus(tgt, 0.3f, 3f);
            }
            focusAllTargets.Clear();
            focusAllCo = null;
        }

        public bool TouchFocus(Atom collidingAtom, Rigidbody rb)
        {
            focusTimer = 2f;
            if (collidingAtom.IsToyOrDildo())
            {
                PoseMe.gaze.Focus(targets.First(x => x.atom == collidingAtom), .22f);
                return true;
            }
            if(rb.IsInRegion("Hands"))
            {
                Focus(targets.First(x => x.atom == collidingAtom && x.name.Contains("Hand")), .22f);
                return true;
            }
            return false;
        }

        public static IEnumerator lerpGazeStrength;
        
        public static IEnumerator LerpGazeStrength(bool bj)
        {
            float target = bj ? gazeSettings.gazeStrengthDuringBJ.val : gazeSettings.gazeStrength.val;
            while (Mathf.Abs(dynamicGazeStrength - target) > 1f)
            {
                dynamicGazeStrength = Mathf.Lerp(dynamicGazeStrength, target, Time.deltaTime);
                // dynamicGazeStrength.PrintEvery(.5f);
                yield return null;
            }
        }

        public void FixedUpdate()
        {
            try
            {
                if(!enabled || !internalEnabled || tempDisabled || atom.mainController.isGrabbing) return;
                // if (eyeBehavior != (EyesControl)atom.GetStorableByID("Eyes"))
                // {
                //     "eyeBehavior mismatch".Print();
                // }
                var rotationState = headCtrl.currentRotationState;
                if (!(rotationState == FreeControllerV3.RotationState.On || rotationState == FreeControllerV3.RotationState.ParentLink))
                {
                    enabledJ.val = false;
                    $"BL: Gaze on atom {atom.uid} disabled. Head controller rotation state has to be either 'On' or 'Parent Link'.".Print();
                    return;
                }
                if(!headCtrl.isGrabbing && SuperController.singleton.GetSelectedController() != headCtrl)
                {
                    if (update)
                    {
                        if (focussing)
                        {
                            focusTimer -= Time.fixedDeltaTime;
                            if (focusTimer < 0f)
                            {
                                focussing = false;
                            }
                        }
                        else if (gazeSettings.autoTarget.val)
                        {
                            targetTimer -= Time.fixedDeltaTime;
                            if (targetTimer < 0f)
                            {
                                targetTimer = NormalDistribution.GetValue(gazeSettings.targetTimeMean.val,
                                    gazeSettings.targetTimeDelta.val, 2,
                                    gazeSettings.targetTimeOnesided.val);
                                if (!focussing) SelectRandomTarget();
                            }
                        }

                        subTargetTimer -= Time.fixedDeltaTime;

                        if (subTargetTimer < 0f)
                        {
                            // lerpToTarget.Stop();
                            subTargetTimer = NormalDistribution.GetValue(gazeSettings.subTargetTimeMean.val,
                                gazeSettings.subTargetTimeDelta.val,
                                2,
                                gazeSettings.subTargetTimeOnesided.val);
                            var objectTarget1 = target as ObjectTarget;
                            if (objectTarget1 == null)
                            {
                                if (target.hasSingleSubTarget)
                                {
                                    subTarget = target.root;
                                    saccade = GetSaccade();
                                }
                                else
                                {
                                    if (gazeSettings.subTargetOcclusion.val)
                                        subTarget = target.GetVisibleSubTarget(this);
                                    else subTarget = target.GetSubTarget();
                                }

                                if (debug && currentTab < 2)
                                {
                                    var virt = tagetIsVirtual ? "(virtual)" : "";
                                    targetInfo.val =
                                        $"{atom.uid}:\n    {target.atom.uid}\n        {target.name} {virt}\n            {subTarget.name}";
                                }
                            }
                            else
                            {
                                meshVertex = objectTarget1.SelectVertex();
                                if (debug && currentTab < 2)
                                {
                                    var virt = tagetIsVirtual ? "(virtual)" : "";
                                    targetInfo.val =
                                        $"{atom.uid}:\n    {target.atom.uid}\n        {objectTarget1.meshTransform.name} {virt}";
                                }

                            }
                        }

                        if (target == null) return;
                        var objectTarget2 = target as ObjectTarget;
                        if (objectTarget2 == null)
                        {
                            Vector3 pos = Vector3.zero;
                            if (tagetIsVirtual) pos = target.GetVirtualPosition(this, subTarget.position, mirror);
                            else
                            {
                                // var col = subTarget.GetComponent<Collider>();
                                // if (col) pos = subTarget.GetComponent<Collider>().ClosestPoint(centerEye.position);
                                pos = subTarget.position;
                            }

                            if (target.hasSingleSubTarget) pos += saccade;
                            targetGO.position = pos;
                        }
                        else
                        {
                            if (tagetIsVirtual)
                                targetGO.position = objectTarget2.GetVirtualPosition(this, meshVertex, mirror);
                            else targetGO.position = objectTarget2.GetWorldVertexPosition(meshVertex);
                        }

                        if (dynamicGazeStrength > 0f)
                        {
                            var targetDirection = targetGO.position - centerEye.position;
                            // var mag = targetDirection.magnitude;
                            var direction = Vector3.Lerp(target.root.position - centerEye.position, targetDirection,
                                targetDirection.magnitude * 0.25f);
                            torque = Vector3.Lerp(torque,
                                dynamicGazeStrength * Vector3.Cross(centerEye.transform.forward, direction.normalized),
                                gazeSpeed * Time.fixedDeltaTime);
                            neckTorque = neck.transform.InverseTransformVector(torque);
                            neckTorque.x *= .3f;
                            neckTorque.z *= .2f;
                        }

                    }
                }
                else
                {
                    torque = Vector3.Lerp(torque, Vector3.zero, 2f*Time.fixedDeltaTime);
                    neckTorque = neck.transform.InverseTransformVector(torque);
                    neckTorque.x *= .3f;
                    neckTorque.z *= .2f;
                }
                
                if (dynamicGazeStrength > 0f)
                {
                    head.AddTorque(torque);
                    neck.AddRelativeTorque(neckTorque);
                }

                if (debug)
                {
                    viewPoints[0] = eyeBehavior.lookAt1.transform.position;
                    viewPoints[1] = targetGO.position;
                    viewPoints[2] = eyeBehavior.lookAt2.transform.position;
                    viewRenderer.SetPositions(viewPoints);
                }
            }
            catch (Exception e)
            {
                // SuperController.LogError(atom.uid);
                // SuperController.LogError(target?.name);
                // SuperController.LogError(e.ToString());
            }
        }

        public static void ResetTorques()
        {
            for (int i = 0; i < gazes.Count; i++)
            {
                gazes[i].torque = Vector3.zero;
            }
        }
        
        public static void SelectRandomTargets()
        {
            try
            {
                for (int i = 0; i < gazes.Count; i++)
                {
                    gazes[i].SelectRandomTarget();
                }
            }
            catch (Exception e)
            {
                SuperController.LogError(e.ToString());
            }
        }

        public void SelectTarget(GazeTarget newTarget, float minSpeed = .1f)
        {
            if(!enabled) return;
            var objectTarget = newTarget as ObjectTarget;
            if (objectTarget != null){
                // if(!objectTarget.GetVisibleMeshes(this)) return;
                objectTarget.GetVisibleMeshes(this);
                objectTarget.SelectMesh();
            }
            else
            {
                var eyePos = centerEye.position + centerEye.forward * .05f;
                var v = newTarget.root.position + newTarget.offset - eyePos;
                if (gazeSettings.useMirrors.val && (Vector3.Dot(v, centerEye.forward) / v.magnitude < gazeCosine || target.IsOccluded(v)))
                {
                    if (!gazeSettings.useMirrors.val) return;
                    bool mirrorFound = false;
                    for (int i = 0; i < mirrors.Count; i++)
                    {
                        var mir = mirrors[i];
                        Vector3 m;
                        if (mir.Mirror(this, eyePos, newTarget.excludeFromOcclusion, out m))
                        {
                            mirror = mir;
                            tagetIsVirtual = true;
                            mirrorFound = true;
                            break;
                        }
                    }
                    tagetIsVirtual = mirrorFound;
                }
                else tagetIsVirtual = false;
            }
            gazeSpeed = Mathf.Max(NormalDistribution.GetValue(gazeSettings.gazeSpeedMean.val, gazeSettings.gazeSpeedDelta.val, 2, gazeSettings.gazeSpeedOnesided.val) * .1f, minSpeed);
            if (target != null && newTarget != target && Vector3.Angle(newTarget.root.position - head.position, target.root.transform.position - head.position) > 25f)
            {
                eyelidBehavior.Blink();
            }
            target = newTarget;
            subTargetTimer = 0f;
        }

        public void SelectRandomTarget()
        {
            if(!enabled) return;
            gazeSpeed = NormalDistribution.GetValue(gazeSettings.gazeSpeedMean.val, gazeSettings.gazeSpeedDelta.val, 2, gazeSettings.gazeSpeedOnesided.val) * .1f;
            validTargets.Clear();
            // $"{atom.name} {face == null}".Print();
            var basePosition = centerEye.position + centerEye.forward * .05f;
            for (int i = 0; i < targets.Count; i++)
            {
                // i.Print();
                var target = targets[i];
                if(!target.enabled.val || target.interest.val == 0f || (target.atom.type == "Person" && gazeSettings.personInterests[target.atom].val == 0f)) continue;
                if(target != face)
                {
                    var objectTarget = target as ObjectTarget;
                    if (objectTarget != null)
                    {
                        if (objectTarget.GetVisibleMeshes(this)) validTargets.Add(objectTarget);
                    }
                    else
                    {
                        var v = target.root.position + target.offset - basePosition;
                        // $"{target.name} {target.IsOccluded(-v)}".Print();
                        if (Vector3.Dot(v, centerEye.forward) / v.magnitude > gazeCosine &&
                            !target.IsOccluded(-v))
                        {
                            validTargets.Add(target);
                        }
                    }
                    if (target == environment) continue;
                }
                if(mirrors.Count == 0 || !gazeSettings.useMirrors.val) continue;
                var targetMirrors = target.mirrors[this];
                targetMirrors.Clear();
                for (int j = 0; j < mirrors.Count; j++)
                {
                    var mirror = mirrors[j];
                    if(!mirror.enabled) continue;
                    var plane = mirror.GetPlane();
                    
                    if (!plane.SameSide(target.root.position, basePosition)) continue;
                    Vector3 virtPos;
                    if(mirror.Mirror(this, plane, target.root.position+target.offset, target.excludeFromOcclusion, out virtPos))
                    {
                        var v = virtPos - basePosition;
                        if (Vector3.Dot(v, centerEye.transform.forward) / v.magnitude > gazeCosine)
                        {
                            // validTargets.Add(new VirtualTarget(this, target, mirror, v.sqrMagnitude));
                            targetMirrors.Add(mirror);
                            validTargets.Add(target);
                        }
                    }
                }
                // target.name.Print();
            }
            GazeTarget newTarget;
            if (validTargets.Count == 0) newTarget = playerFace;
            else newTarget = ChooseTarget();
            if (target != null && newTarget != target && Vector3.Angle(newTarget.root.position - centerEye.position, target.root.transform.position - centerEye.position) > 25f)
            {
                eyelidBehavior.Blink();
            }
            // 6.Print();
            target = newTarget;
            subTargetTimer = 0f;
            // $"{target.root.gameObject.GetAtom().uid}:{target.root.name} virtual: {newTarget is VirtualTarget}".Print();
        }
        
        private GazeTarget ChooseTarget()
        {
            interests.Clear();
            var sum = 0f;
            for (int i = 0; i < validTargets.Count; i++)
            {
                var target = validTargets[i];
                float squareDist;
                // if (target is VirtualTarget)
                // {
                //     squareDist = ((VirtualTarget)target).virtualSquareDist;
                // }
                // else squareDist = (target.root.position - head.position).sqrMagnitude;
                squareDist = (target.root.position - head.position).sqrMagnitude;
                var delta = target.interest.val * (target.velocity + 1f + .25f / (1f+squareDist));
                if (target.atom.type == "Person") delta *= gazeSettings.personInterests[target.atom].val;
                if (target.atom == atom) delta *= gazeSettings.selfInterest.val;
                sum += delta;
                interests.Add(sum);
                // $"{atom.uid} -> {target.atom.uid}:{target.name} | {delta}".Print();
            }
            var rng = Random.Range(0f, sum);
            int j;
            for (j = 0; j < validTargets.Count; j++)
            {
                if (interests[j] > rng)
                {
                    break;
                }
            }

            var choosenTarget = validTargets[j];
            int first = validTargets.IndexOf(choosenTarget);
            int last = validTargets.LastIndexOf(choosenTarget);
            int num = last - first + 1;
            var targetMirrors = choosenTarget.mirrors[this];
            if (num == targetMirrors.Count)
            {
                tagetIsVirtual = true;
                mirror = targetMirrors[j - first];
            }
            else
            {
                if (j > first)
                {
                    tagetIsVirtual = true;
                    mirror = targetMirrors[j - first - 1];
                }
                else tagetIsVirtual = false;
            }
            
            if (choosenTarget is ObjectTarget)
            {
                ((ObjectTarget)choosenTarget).SelectMesh();
            }

            if (choosenTarget.atom.IsToyOrDildo()) ReadMyLips.dynamicStimGain += .0005f;
            // if(atom != PoseMe.atom) $"{atom.uid} -> {t.atom.uid}:{t.name}".Print();
            if(choosenTarget is PersonFace && Random.Range(0f, 1f) > eyeContactChance.val) gazes.First(x => x.atom == choosenTarget.atom).Focus(face, 0f);
            return choosenTarget;
        }

        private static JSONStorableFloat eyeContactChance = new JSONStorableFloat("Eye Contact Chance", .5f, 0f, 1f);

        
        
        
        private static bool IsOccluded(Vector3 start, Vector3 direction, List<Collider> excludeFromOcclusion = null)
        {
            var rayHits = Physics.RaycastNonAlloc(start, direction, rayCastBuffer, direction.magnitude * .8f);
            for (int j = 0; j < rayHits; j++)
            {
                var col = rayCastBuffer[j].collider;
                if(col.isTrigger
                   || col.name.Contains("Control")
                   || col.name.Contains("Link")
                   || col.name.StartsWith("AutoColliderGen")
                   || BodyRegionMapping.IsInRegion(col.name, "Hand")
                   || excludeFromOcclusion.Contains(col)) continue;
                var atom = col.GetAtom();
                if(!atom) continue;
                if (atom.type.Contains("Glass")) continue;
                return true;
            }
            // $"{atom.name}:{root.name} is not occluded".Print();
            return false;
        }

        public class MirrorObject
        {
            public bool enabled = true;
            public Atom atom;
            private readonly BoxCollider box;

            public MirrorObject(Atom atom, BoxCollider box)
            {
                this.atom = atom;
                this.box = box;
                atom.GetBoolJSONParam("on").AddCallback(OnToggled, false);
            }
            
            public MirrorObject(Atom atom)
            {
                this.atom = atom;
                box = atom.GetComponentInChildren<BoxCollider>(true);
                atom.GetBoolJSONParam("on").AddCallback(OnToggled, false);
            }
            
            public bool Mirror(Gaze gaze, Vector3 position, List<Collider> excludeFromOcclusion, out Vector3 virtualPosition)
            {
                virtualPosition = Vector3.zero;
                return Mirror(gaze, GetPlane(), position, excludeFromOcclusion, out virtualPosition);
            }
            
            public bool Mirror(Gaze gaze, Plane plane, Vector3 position, List<Collider> excludeFromOcclusion, out Vector3 virtualPosition)
            {
                virtualPosition = Vector3.zero;
                var planePoint = plane.ClosestPointOnPlane(position);
            
                virtualPosition = planePoint + (planePoint - position);
                float dist;
                var centerPos = gaze.centerEye.position + gaze.centerEye.transform.forward * .05f;
                var viewDirection = virtualPosition - centerPos;
                var ray = new Ray(centerPos, viewDirection);
                plane.Raycast(new Ray(centerPos, viewDirection), out dist);
                var image = ray.GetPoint(dist);
                var transform = box.transform;
                var fromCenter = transform.InverseTransformPoint(image);
                if (Mathf.Abs(fromCenter.x) > .5f*box.size.x || Mathf.Abs(fromCenter.z) > .5f*box.size.z) return false;
                
                if (IsOccluded(image, position - image, excludeFromOcclusion)) return false;
                if(IsOccluded(image, centerPos - image, excludeFromOcclusion)) return false;
                return true;
            }

            public Plane GetPlane()
            {
                var mirrorTransform = box.transform;
                var mirrorPosition = mirrorTransform.position;
                var mirrorNormal = mirrorTransform.up;
                return new Plane(mirrorNormal, mirrorPosition);
            }

            private void OnToggled(bool val)
            {
                enabled = val;
                for (int i = 0; i < gazes.Count; i++)
                {
                    var gaze = gazes[i];
                    if(gaze.mirror == this) gaze.SelectRandomTarget();
                }
            }

            public void Destroy()
            {
                atom.GetBoolJSONParam("on").setCallbackFunction -= OnToggled;
                lerpGazeStrength.Stop();
            }
        }

        private static void GetMirrors()
        {
            mirrors = new List<MirrorObject>();
            foreach (var atom in SuperController.singleton.GetAtoms())
            {
                if (atom.type.Contains("Reflective") || atom.type.Contains("Glass"))
                {
                    var box = atom.GetComponentInChildren<BoxCollider>(true);
                    if (box)
                    {
                        mirrors.Add(new MirrorObject(atom, box));
                    }
                }
            }
        }

        public static void OnAtomAdded(Atom atom)
        {
            try
            {
                if (atom.type.Contains("Reflective") || atom.type.Contains("Glass"))
                {
                    var box = atom.GetComponentInChildren<BoxCollider>(true);
                    if (box)
                    {
                        mirrors.Add(new MirrorObject(atom, box));
                    }
                }
                // else if (atom.type == "Dildo")
                // {
                //     RegisterTarget(new GazeDildo(atom));
                // }
                else if (atom.IsToy() || atom.type == "Paddle")
                {
                    RegisterObject(atom);
                }
                else if (atom.type == "ToyBP")
                {
                    RegisterObject(atom);
                }
                else if (atom.type == "Person" && debug && currentTab == 3)
                {
                    SelectTab(currentTab);
                }
                SyncObjectChooser();
                SyncEnvironmentChooser();
                // atom.type.Print();
            }
            catch (Exception e)
            {
                SuperController.LogError(e.ToString());
            }
        }
        
        public static void OnAtomRemoved(Atom atom)
        {
            try
            {
                if (atom.type.Contains("Reflective") || atom.type.Contains("Glass"))
                {
                    var mirror = mirrors.Find(x => x.atom == atom);
                    mirrors.Remove(mirror);
                    for (int i = 0; i < gazes.Count; i++)
                    {
                        var gaze = gazes[i];
                        if(gaze.mirror == mirror) gaze.SelectRandomTarget(); 
                    }
                }
                DeregisterAtom(atom);
                // PoseMe.gaze.SelectRandomTarget();
                for (int i = 0; i < gazes.Count; i++)
                {
                    gazes[i].SelectRandomTarget();
                }
                SyncEnvironmentChooser();
                SyncObjectChooser();
            }
            catch (Exception e)
            {
                SuperController.LogError(e.ToString());
            }
        }

        public static void OnAtomRenamed(string oldUid, string newUid)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                target.name = target.name.Replace(oldUid, newUid);
            }
            SyncTargetFilter();
            SyncFocusTargetChooser();
        }
        

        public static void CreateUI()
        {
            var UIElements = PoseMe.UIElements;
            tabbar = UIManager.CreateTabBar(new[] { "Behavior", "Targets", "Interests", "Clients", "Performance" }, SelectTab);
            UIElements.Add(tabbar);
            tabbar.SelectTab(currentTab);
            SetDebugMode(true);
        }

        public static List<object> UIElements = new List<object>();
        private static int currentTab;
        private static void SelectTab(int i)
        {
            PoseMe.singleton.RemoveUIElements(UIElements);
            currentTab = i;
            switch (i)
            {
                case 0:
                {
                    CreateBehaviorUI();
                    break;
                }
                case 1:
                {
                    CreateTargetsUI();
                    break;
                }
                case 2:
                {
                    CreateInterestsUI();
                    break;
                }
                case 3:
                {
                    CreateClientsUI();
                    break;
                }
                case 4:
                {
                    CreatePerformanceUI();
                    break;
                }
            }
        }

        private static void CreateBehaviorUI()
        {
            gazeSettings.CreateUI(UIElements);
            // focusTargetChooser.CreateUI(UIElements, false, chooserType:2);
            // focusDuration.CreateUI(UIElements, true);
            // gazeSpeedMean.CreateUI(UIElements);
            // gazeSpeedDelta.CreateUI(UIElements, true);
            // targetTimeMean.CreateUI(UIElements);
            // targetTimeDelta.CreateUI(UIElements, true);
            // subTargetTimeMean.CreateUI(UIElements);
            // subTargetTimeDelta.CreateUI(UIElements, true);
            // subTargetTimeOnesided.CreateUI(UIElements, true);
            // targetTimeOnesided.CreateUI(UIElements, true);
            // gazeStrength.CreateUI(UIElements);
            // gazeStrengthDuringBJ.CreateUI(UIElements);
            // gazeAngle.CreateUI(UIElements, true);
            // touchReactionsEnabled.CreateUI(UIElements);
            // autoTarget.CreateUI(UIElements);
            PoseMe.singleton.SetupButton("Select Random Target", false, PoseMe.gaze.SelectRandomTarget, UIElements);
            PoseMe.singleton.SetupButton("Focus Now", false, FocusOnDemand, UIElements);
            var tf = PoseMe.singleton.CreateTextField(PoseMe.gaze.targetInfo, true);
            UIElements.Add(tf);
        }

        public static void CreateInterestsUI()
        {
            // PoseMe.singleton.SetupButton("Click here!! :)", true, () => "Congratulations! A new washing machine will be delivered to your location within the next three working days.\nPlease make sure to have sufficient founds available upon arrival.".Print(), UIElements);
            targetAtomFilter.CreateUI(UIElements, true, chooserType:2);
            // playerFace.interest.CreateUI(UIElements);
            if(string.IsNullOrEmpty(targetAtomFilter.val)) return;
            var atom = SuperController.singleton.GetAtomByUid(targetAtomFilter.val);
            if(!atom) return;
            if(atom.type == "Person") gazeSettings.personInterests[atom].CreateUI(UIElements);
            int i = 0;
            foreach (var target in targets.Where(x => x.atom == atom))
            {
                UIDynamicSlider slider = target.interest.CreateUI(UIElements, i%2==1) as UIDynamicSlider;
                if (!target.enabled.val)
                {
                    slider.SetInteractable(false);
                }
                i++;
            }
        }
        
        public static void CreateTargetsUI()
        {
            environmentChooser.CreateUI(UIElements, true, chooserType:2);
            gazeSettings.environmentEnabled.CreateUI(UIElements, true);
            objectChooser.CreateUI(UIElements, false, chooserType:2);
            var tf = PoseMe.singleton.CreateTextField(PoseMe.gaze.targetInfo, true);
            UIElements.Add(tf);
            for (int i = 0; i < customTargets.Count; i++)
            {
                var target = customTargets[i];
                if (target != environment)
                {
                    PoseMe.CreateUIDynamicGazeTarget(target, false, UIElements);
                }
            }
        }

        public static void CreateClientsUI()
        {
            for (int i = 0; i < PoseMe.persons.Count; i++)
            {
                PoseMe.persons[i].gaze.enabledJ.CreateUI(UIElements);
            }
        }

        public static void CreatePerformanceUI()
        {
            gazeSettings.useMirrors.CreateUI(UIElements);
            gazeSettings.targetOcclusion.CreateUI(UIElements);
            gazeSettings.subTargetOcclusion.CreateUI(UIElements);
            var tf = PoseMe.singleton.CreateTextField(performanceInfo, true);
            tf.ForceHeight(600f);
            UIElements.Add(tf);
        }

        public static void RegisterObjectFromChooser(JSONStorableStringChooser chooser)
        {
            var atom = SuperController.singleton.GetAtomByUid(chooser.val);
            if (atom == null) return;
            chooser.valNoCallback = "";
            if (atom.type == "CustomUnityAsset")
            {
                "For environments: Please use the 'Environment' chooser. They are treated differently.".Print();
            }
            RegisterObject(atom, true);
        }
        
        public static void OnEnvironmentChanged(string val)
        {
            if (val == "" && environment != null)
            {
                DeregisterAtom(environment.atom);
                return;
            } 
            var atom = SuperController.singleton.GetAtomByUid(val);
            if (atom == null) return;
            RegisterObject(atom, isEnvironment:true);
        }

        public static void SetDebugMode(bool val)
        {
            for (int i = 0; i < gazes.Count; i++)
            {
                var gaze = gazes[i];
                if(!gaze.enabledJ.val) continue;
                gaze.targetRenderer.enabled = val;
                debug = val;
                gaze.viewRenderer.gameObject.SetActive(val);
                gaze.targetInfo.val = "";
            }
        }

        public static void SetDebugColor()
        {
            for (int i = 0; i < gazes.Count; i++)
            {
                var gaze = gazes[i];
                var dcs = gaze.atom.GetStorableByID("geometry") as DAZCharacterSelector;
                var color = dcs.gender == DAZCharacterSelector.Gender.Female ? ReadMyLips.femaleStimColor.val.ToRGB() : ReadMyLips.maleStimColor.val.ToRGB();
                gaze.viewRenderer.material.color = gaze.targetRenderer.material.color = color;
            }
        }

        public void Destroy()
        {
            playerFace.Destroy();
            Object.Destroy(targetGO.gameObject);
            Object.Destroy(viewRenderer.gameObject);
            // lerpToTarget.Stop();
            deferredInit.Stop();
            for (int i = 0; i < customTargets.Count; i++)
            {
                customTargets[i].Destroy();
            }
            // environmentCuaLoader?.DeregisterAssetLoadedCallback(OnCUALoaded);
            Object.Destroy(lVRHandListener);
            Object.Destroy(rVRHandListener);
            eyeBehavior.currentLookMode = cachedLookMode;
            Object.Destroy(uiEnabledListener);
            for (int i = 0; i < mirrors.Count; i++)
            {
                mirrors[i].Destroy();
            }
        }
        
        public static GameObject targetUidPrefab;
        public static void CreateGazeUidPrefab()
        {
			if (targetUidPrefab == null)
			{
                targetUidPrefab = new GameObject("UIDynamicGazeTarget");
                targetUidPrefab.SetActive(false);
				RectTransform rt = targetUidPrefab.AddComponent<RectTransform>();
				rt.anchorMax = new Vector2(0, 1);
				rt.anchorMin = new Vector2(0, 1);
				rt.offsetMax = new Vector2(535, -500);
				rt.offsetMin = new Vector2(10, -600);
				LayoutElement le = targetUidPrefab.AddComponent<LayoutElement>();
				le.flexibleWidth = 1;
				le.minHeight = 50;
				le.minWidth = 350;
				le.preferredHeight = 50;
				le.preferredWidth = 500;

				RectTransform backgroundTransform = PoseMe.singleton.manager.configurableScrollablePopupPrefab.transform.Find("Background") as RectTransform;
				backgroundTransform = Object.Instantiate(backgroundTransform, targetUidPrefab.transform);
				backgroundTransform.name = "Background";
				backgroundTransform.anchorMax = new Vector2(1, 1);
				backgroundTransform.anchorMin = new Vector2(0, 0);
				backgroundTransform.offsetMax = new Vector2(0, 0);
				backgroundTransform.offsetMin = new Vector2(0, 0);
                backgroundTransform.GetComponent<Image>().color = new Color(0.839f, .839f, .839f);
                
                RectTransform buttonPrefab = PoseMe.singleton.manager.configurableScrollablePopupPrefab.transform.Find("Button") as RectTransform;
                var buttonTransform = Object.Instantiate(buttonPrefab, targetUidPrefab.transform);
                Object.DestroyImmediate(buttonTransform.GetComponent<Button>());
                buttonTransform.name = "ActiveToggle";
                buttonTransform.anchorMax = new Vector2(0, 1);
                buttonTransform.anchorMin = new Vector2(0, 1);
                buttonTransform.offsetMax = new Vector2(50, 0);
                buttonTransform.offsetMin = new Vector2(0, -50);
                var activeToggle = buttonTransform.gameObject.AddComponent<Toggle>();
                var activeToggleText = buttonTransform.Find("Text").GetComponent<Text>();
                activeToggleText.text = "✓";
                activeToggleText.fontSize = 28;
                activeToggle.isOn = true;

                // PoseMe.singleton.manager.configurablePopupPrefab.transform.PrintHierarchy();
                var popupTransform = Object.Instantiate(PoseMe.singleton.manager.configurablePopupPrefab.transform.Find("Text"), targetUidPrefab.transform) as RectTransform;
                popupTransform.name = "Label";
                popupTransform.anchorMax = new Vector2(1, 1);
                popupTransform.anchorMin = new Vector2(0, 0);
                popupTransform.offsetMax = new Vector2(-50, 0);
                popupTransform.offsetMin = new Vector2(50, 0);
                var label = popupTransform.GetComponentInChildren<Text>();
                label.text = "Atom";
                label.color = Color.black;

                buttonTransform = Object.Instantiate(buttonPrefab, targetUidPrefab.transform);
				buttonTransform.name = "Button";
				buttonTransform.anchorMax = new Vector2(1, 1);
				buttonTransform.anchorMin = new Vector2(1, 0);
				buttonTransform.offsetMax = new Vector2(0, 0);
				buttonTransform.offsetMin = new Vector2(-50, 0);
				var deleteButton = buttonTransform.GetComponent<Button>();
                var deleteButtonText = buttonTransform.Find("Text").GetComponent<Text>();
                deleteButtonText.fontSize = 28;
                deleteButtonText.text = "<b>X</b>";
                deleteButtonText.color = Color.white;
                buttonTransform.GetComponent<Image>().color = PoseMe.severeWarningColor;

                UIDynamicGazeItem uid = targetUidPrefab.AddComponent<UIDynamicGazeItem>();
                uid.activeToggle = activeToggle;
                uid.toggleText = activeToggleText;
                uid.deleteButton = deleteButton;
                uid.label = label;
            }
		}

        public class GazeTarget
        {
            private string _name;
            public string name {
                get
                {
                    return _name;
                }
                set
                {
                    _name = value;
                    interest.name = value + " Interest";
                }
            }

            public JSONStorableBool enabled = new JSONStorableBool("Enabled", true);
            public Rigidbody rootRB { get; set; }
            protected Transform _root;
            public virtual Transform root {
                get {return rootRB.transform; }
                set { _root = value; }
            }

            public virtual float velocity => rootRB.velocity.sqrMagnitude;

            public List<Collider> excludeFromOcclusion { get; set; } = new List<Collider>();
            public virtual Vector3 offset => root.forward*.05f;
            public List<Transform> subTargets { get; } = new List<Transform>();
            private Atom _atom;
            public Atom atom
            {
                get { return _atom; }
                set
                {
                    if(_atom == value) return;
                    _atom = value;
                    _atom.GetBoolJSONParam("on").setCallbackFunction += OnToggle;
                }
            }

            public JSONStorableFloat interest = new JSONStorableFloat("Interest", 1f, 0f, 1f, false);
            public bool hasSingleSubTarget { get; set; } = false;
            public Dictionary<Gaze, List<MirrorObject>> mirrors = new Dictionary<Gaze, List<MirrorObject>>();

            public GazeTarget()
            {
                enabled.setCallbackFunction += val =>
                {
                    if (PoseMe.currentPose != null) PoseMe.currentPose.gazeSettings.targetSettings[this].enabled.val = val;
                };
                interest.setCallbackFunction += val =>
                {
                    if (PoseMe.currentPose != null) PoseMe.currentPose.gazeSettings.targetSettings[this].interest.val = val;
                };
                for (int j = 0; j < PoseMe.poses.Count; j++)
                {
                    PoseMe.poses[j].gazeSettings.targetSettings[this] = new GazeSettings.TargetSetting(this);
                }
            }

            public void Store(JSONClass parent, bool forceStore)
            {
                var jc = new JSONClass();
                enabled.Store(jc, forceStore);
                interest.Store(jc, forceStore);
                parent[name] = jc;
            }
            
            public void Load(JSONClass parent, bool setMissingToDefault)
            {
                if(!parent.HasKey(name)) return;
                var jc = parent[name].AsObject;
                enabled.Load(jc, setMissingToDefault);
                interest.Load(jc, setMissingToDefault);
            }

            private List<Transform> visibleSubTargets = new List<Transform>();
            public Transform GetVisibleSubTarget(Gaze gaze)
            {
                visibleSubTargets.Clear();
                var eyePos = gaze.centerEye.position + .03f*gaze.centerEye.forward;
                for (int i = 0; i < subTargets.Count; i++)
                {
                    if (gaze.tagetIsVirtual)
                    {
                        Vector3 m;
                        if (gaze.mirror.Mirror(gaze, subTargets[i].position, excludeFromOcclusion, out m))
                        {
                            visibleSubTargets.Add(subTargets[i]);
                        }
                    }
                    else
                    {
                        if (!Gaze.IsOccluded(eyePos, subTargets[i].position - eyePos, excludeFromOcclusion))
                        {
                            visibleSubTargets.Add(subTargets[i]);
                        }
                    }
                }
                if (visibleSubTargets.Count == 0)
                {
                    gaze.SelectRandomTarget();
                    // $"{name}  {visibleSubTargets.Count}".Print();
                    return root;
                }
                return visibleSubTargets[Random.Range(0, visibleSubTargets.Count)];
            }
            
            public Transform GetSubTarget()
            {
                return subTargets[Random.Range(0, subTargets.Count)];
            }

            public void Debug()
            {
                for (int i = 0; i < subTargets.Count; i++)
                {
                    subTargets[i].Draw();
                }
            }

            public virtual Vector3 GetVirtualPosition(Gaze gaze, Vector3 position, MirrorObject mirror)
            {
                Vector3 virtPos;
                if (mirror.Mirror(gaze, position, excludeFromOcclusion, out virtPos))
                {
                    return virtPos;
                }
                else
                {
                    // $"{atom.uid} lost target {atom.uid}:{name}".Print();
                    gaze.SelectRandomTarget();
                    return Vector3.zero;
                }
            }

            public bool IsOccluded(Vector3 direction)
            {
                if (!gazeSettings.targetOcclusion.val) return false;
                var rayHits = Physics.RaycastNonAlloc(root.position+offset,
                    direction, rayCastBuffer, direction.magnitude);
                // $"{root.name} {rayHits}".Print();
                for (int j = 0; j < rayHits; j++)
                {
                    var col = rayCastBuffer[j].collider;
                    if(col.isTrigger || col.name.Contains("Control")
                                     || col.name.Contains("Link")
                                     || col.name.StartsWith("AutoColliderGen")
                                     || BodyRegionMapping.IsInRegion(col.name, "Hand")
                                     || excludeFromOcclusion.Contains(col)
                                     ) continue;
                    var atom = col.GetAtom();
                    if(!atom) continue;
                    if (atom.type.Contains("Glass")) continue;
                    // if(this is Ass && this.atom != PoseMe.atom) col.Print();
                    return true;
                }
                // if(this is Thigh) $"{atom.name}:{root.name} is not occluded".Print();
                return false;
            }

            private void OnToggle(bool val)
            {
                enabled.val = val;
                if (!val)
                {
                    for (int i = 0; i < gazes.Count; i++)
                    {
                        gazes[i].SelectRandomTarget();
                    }
                }
            }

            public virtual void Destroy()
            {
                atom.GetBoolJSONParam("on").setCallbackFunction -= OnToggle;
            }
        }
        
        public class PlayerFace : GazeTarget
        {
            public override Transform root { get; set; }
            public override float velocity => 0f;
            public PlayerFace()
            {
                name = "PlayerFace";
                atom = SuperController.singleton.GetAtomByUid("[CameraRig]");
                // Camera.main.transform.position.Print();
                // foreach (var rb in PoseMe.camera.parent.parent.GetComponentsInChildren<Rigidbody>(true))
                // {
                //     if(rb.name == "CenterEye") rb.transform.PrintParents();
                // }
                // var cam = Camera.main.transform;
                
                root = Camera.main.transform;;
                // cam.PrintParents();
                // cam.parent.PrintChildren();
                // cam.parent.parent.GetComponentsInChildren<Rigidbody>(true).ToList().ForEach(x => x.name.Print());
                // rootRB = cam.Find("CenterEye").GetComponent<Rigidbody>();
                subTargets.Add(new GameObject("BL_PlayerFace_lEye").transform);
                subTargets.Add(new GameObject("BL_PlayerFace_rEye").transform);
                subTargets.Add(new GameObject("BL_PlayerFace_mouth").transform);
                for (int i = 0; i < subTargets.Count; i++)
                {
                    subTargets[i].SetParent(root, false);
                }
                subTargets[0].localPosition = -.03f * Vector3.right;
                subTargets[1].localPosition = .03f * Vector3.right;
                subTargets[2].localPosition = -.09f * Vector3.up;
            }

            public override void Destroy()
            {
                base.Destroy();
                for (int i = 1; i < subTargets.Count; i++)
                {
                    Object.Destroy(subTargets[i].gameObject);
                }
            }
        }

        private static UnityEventsListener lVRHandListener;
        private static UnityEventsListener rVRHandListener;
        public class VRHand : GazeTarget
        {
            public VRHand(string side = "l")
            {
                name = $"{side}VRHand";
                string s = side == "l" ? "Left" : "Right";
                atom = SuperController.singleton.GetAtomByUid("[CameraRig]");
                var wrist = atom.transform.Find($"HeightOffset/Hands/{s}HandPhysical/MaleHand02{side.ToUpper()}/HandPhysical");
                rootRB = wrist.GetComponent<Rigidbody>();
                excludeFromOcclusion = wrist.GetComponentsInChildren<Collider>(true).ToList();
                subTargets.AddRange(excludeFromOcclusion.Select(x => x.transform));
                var parent = wrist.parent.parent;
                enabled.val = parent.gameObject.activeSelf;
                UnityEventsListener listener = side == "l" ? lVRHandListener : rVRHandListener;
                listener = parent.gameObject.AddComponent<UnityEventsListener>();
                listener.onEnabled.AddListener(() =>
                {
                    enabled.val = true;
                    // $"{s} VR enabled".Print();
                });
                listener.onDisabled.AddListener(() =>
                {
                    enabled.val = false;
                    // $"{s} VR disabled".Print();
                });
            }
        }

        public class PersonFace : GazeTarget
        {
            public override Transform root {
                get {return _root; }
                set { _root = value; }
            }
            public override Vector3 offset => Vector3.zero;
            public PersonFace(Atom atom)
            {
                name = $"{atom.uid} Face";
                this.atom = atom;
                rootRB = atom.rigidbodies.First(x => x.name == "head");
                // root = rootRB.transform.Find("eyeCenter");
                root = new GameObject().transform;
                root.SetParent(rootRB.transform.Find("eyeCenter"), false);
                root.localPosition = new Vector3(0f, 0f, .04f);
                // rootRB.transform.PrintChildren();
                // excludeFromOcclusion = rootRB.GetComponentsInChildren<Collider>(true).ToList();
                // var bones = atom.transform.Find("rescale2").GetComponentsInChildren<DAZBone>();
                subTargets.Add(rootRB.transform.Find("lEye"));
                subTargets.Add(rootRB.transform.Find("rEye"));
                subTargets.Add(rootRB.transform.Find("LipTrigger"));
            }
            public override void Destroy()
            {
                base.Destroy();
                for (int i = 1; i < subTargets.Count; i++)
                {
                    Object.Destroy(root.gameObject);
                }
            }
        }
        
        public class Penis : GazeTarget
        {
            public Penis(Person person)
            {
                name = $"{person.atom.uid} Penis";
                atom = person.atom;
                excludeFromOcclusion = person.penetrator.rigidbodies[0].GetComponentsInChildren<Collider>(true).Where(x => x.attachedRigidbody).ToList();
                subTargets.AddRange(excludeFromOcclusion.Select(x => x.transform));
                subTargets.AddRange(person.atom.rigidbodies.First(x => x.name == "Testes").GetComponentsInChildren<Collider>(true).Select(x => x.transform));
                rootRB = person.penetrator.rigidbodies[2];
                person.penisGazeTarget = this;
                // 1.Print();
                // FillMeUp.containingPerson.NullCheck();
                // excludeFromOcclusion.AddRange(FillMeUp.containingPerson.lHand.GetComponentsInChildren<Collider>());
                // excludeFromOcclusion.AddRange(FillMeUp.containingPerson.rHand.GetComponentsInChildren<Collider>());
                // person.atom.name.Print();
            }
        }
        
        public class Breast : GazeTarget
        {
            // public override Vector3 offset => root.forward*-.01f;
            public override Vector3 offset => Vector3.zero;
            public Breast(Person person, string side = "l")
            {
                name = $"{person.atom.uid} {side}Breast";
                atom = person.atom;
                rootRB = atom.rigidbodies.First(x => x.name == $"{side}NippleTrigger");
                subTargets.Add(root.transform);
                if (person.dcs.gender == DAZCharacterSelector.Gender.Female)
                {
                    string s = side == "l" ? "left" : "right";
                    var path = "rescale2/geometry/FemaleMorphers/BreastPhysicsMesh/";
                    foreach (Transform child in atom.transform.Find(path))
                    {
                        if (child.name.Contains(s))
                            subTargets.AddRange(child.GetComponentsInChildren<Collider>(true).Select(x => x.transform));
                    }
                }
                else
                {
                    hasSingleSubTarget = true;
                }
            }
        }
        
        public class Pelvis : GazeTarget
        {
            public override Vector3 offset => -.02f*root.forward;
            public override Transform root
            {
                get;
                set;
            }
            public Pelvis(Person person)
            {
                name = $"{person.atom.uid} Pelvis";
                atom = person.atom;
                // rootRB.transform.Draw();
                rootRB = atom.rigidbodies.First(x => x.name == "pelvis");
                if (person.dcs.gender == DAZCharacterSelector.Gender.Female)
                {
                    excludeFromOcclusion = rootRB.transform.Find($"FemaleAutoColliderspelvis").GetComponentsInChildren<Collider>()
                        .Where(x => x.name.StartsWith("AutoColliderpelvisF")).ToList();
                    subTargets.AddRange(excludeFromOcclusion.Select(x => x.transform));
                    // excludeFromOcclusion.AddRange(person.penetrator.rigidbodies[0].GetComponentsInChildren<Collider>(true).Where(x => x.attachedRigidbody));
                    root = subTargets.Find(x => x.name.EndsWith("F10Joint"));
                }
                else
                {
                    excludeFromOcclusion = rootRB.transform.Find($"MaleAutoColliderspelvis").GetComponentsInChildren<Collider>()
                        .Where(x => x.name.StartsWith("AutoColliderpelvisF")).ToList();
                    subTargets.AddRange(excludeFromOcclusion.Select(x => x.transform));
                    // excludeFromOcclusion.AddRange(person.penetrator.rigidbodies[0].GetComponentsInChildren<Collider>(true).Where(x => x.attachedRigidbody));
                    root = subTargets[0];
                }
            }
        }
        
        public class Ass : GazeTarget
        {
            public override Vector3 offset => -.02f*root.forward;
            public override Transform root
            {
                get;
                set;
            }
            public Ass(Person person, string side = "l")
            {
                name = $"{person.atom.uid} {side}Ass";
                atom = person.atom;
                var s1 = $"{side.ToUpper()}Glute";
                var rootId = side == "l" ? 30 : 18;
                // rootRB.transform.Draw();
                if (person.dcs.gender == DAZCharacterSelector.Gender.Female)
                {
                    rootRB = person.atom.rigidbodies.First(x => x.name == s1);
                    string s = side == "l" ? "left" : "right";
                    var path = $"rescale2/geometry/FemaleMorphers/LowerPhysicsMesh/PhysicsMesh{s} glute";
                    excludeFromOcclusion = atom.transform.Find(path).GetComponentsInChildren<Collider>(true).ToList();
                    subTargets.AddRange(excludeFromOcclusion.Select(x => x.transform));
                    root = subTargets[rootId];
                }
                else
                {
                    rootRB = atom.rigidbodies.First(x => x.name == $"pelvis");
                    string s = side == "l" ? "AutoColliderpelvisBL" : "AutoColliderpelvisBR";
                    excludeFromOcclusion = rootRB.transform.Find($"MaleAutoColliderspelvis").GetComponentsInChildren<Collider>()
                        .Where(x => x.name.StartsWith(s)).ToList();
                    subTargets.AddRange(excludeFromOcclusion.Select(x => x.transform));
                    root = subTargets[0];
                }
            }
        }
        
        public class Anus : GazeTarget
        {
            public override Transform root { get; set; }
            public Anus(Atom atom)
            {
                name = $"{atom.uid} Anus";
                this.atom = atom;
                var path = $"rescale2/geometry/FemaleMorphers/LowerPhysicsMesh/PhysicsMeshan";
                excludeFromOcclusion = atom.transform.Find(path).GetComponentsInChildren<Collider>(true).ToList();
                subTargets.AddRange(excludeFromOcclusion.Select(x => x.transform));
                // $"{atom.name} {name} {excludeFromOcclusion[0].attachedRigidbody == null}".Print();
                root = subTargets[0];
                rootRB = atom.rigidbodies.First(x => x.name == "_JointAl");
                
            }
        }
        
        public class Pussy : GazeTarget
        {
            public Pussy(Atom atom)
            {
                name = $"{atom.uid} Pussy";
                this.atom = atom;
                var path = $"rescale2/geometry/FemaleMorphers/LowerPhysicsMesh/PhysicsMeshlab";
                excludeFromOcclusion = atom.transform.Find(path).GetComponentsInChildren<Collider>(true).ToList();
                subTargets.AddRange(excludeFromOcclusion.Select(x => x.transform));
                rootRB = atom.rigidbodies.First(x => x.name == "_JointGl");
                // $"{atom.name} {name} {excludeFromOcclusion[0].attachedRigidbody == null}".Print();
            }
        }
        
        public class Hand : GazeTarget
        {
            // public override Vector3 offset => root.up*.02f;
            public override Vector3 offset => Vector3.zero;
            public Hand(Person person, string side = "l")
            {
                name = $"{person.atom.uid} {side}Hand";
                atom = person.atom;
                var hand = side == "l" ? person.lHand.transform : person.rHand.transform;
                subTargets.Add(hand.Find($"{side}Carpal1/{side}Index1/{side}Index2"));
                subTargets.Add(hand.Find($"{side}Carpal1/{side}Mid1/{side}Mid2"));
                subTargets.Add(hand.Find($"{side}Carpal2/{side}Ring1/{side}Ring2"));
                subTargets.Add(hand.Find($"{side}Carpal2/{side}Pinky1/{side}Pinky2"));
                subTargets.Add(hand.Find($"{side}Thumb1/{side}Thumb2/{side}Thumb3"));
                rootRB = subTargets[1].GetComponent<Rigidbody>();
                interest.val = .1f;
                excludeFromOcclusion = hand.GetComponentsInChildren<Collider>().ToList();
            }
        }
        
        public class Thigh : GazeTarget
        {
            // public override Vector3 offset => root.forward*-.01f;
            public override Vector3 offset => Vector3.zero;
            public Thigh(Person person, string side = "l")
            {
                name = $"{person.atom.uid} {side}Thigh";
                atom = person.atom;
                var thigh = atom.rigidbodies.First(x => x.name == $"{side}Thigh");
                subTargets.Add(thigh.transform);
                // excludeFromOcclusion.Add(rootRB.GetComponent<Collider>());
                foreach (Transform child in thigh.transform)
                {
                    // child.Print();
                    var col = child.GetComponent<Collider>();
                    if (col)
                    {
                        excludeFromOcclusion.Add(col);
                    }

                    if (child.name == $"FemaleAutoColliders{side}Thigh")
                    {
                        foreach (Transform child1 in child)
                        {
                            var col1 = child1.GetChild(0).GetComponent<Collider>();
                            if (col1)
                            {
                                excludeFromOcclusion.Add(col1);
                                // col1.Print();
                            }
                        }
                    }
                }
                // excludeFromOcclusion = rootRB.GetComponentsInChildren<Collider>(true).ToList();
                
                subTargets.AddRange(excludeFromOcclusion.Select(x => x.transform));
                // hasSingleSubTarget = subTargets.Count == 1;
                // subTargets.ForEach(x => x.Draw());
                rootRB = subTargets[15].GetComponent<Rigidbody>();
                // root = rootRB.transform;
                // subTargets[15].Draw();
            }
        }
        
        public class Shin : GazeTarget
        {
            // public override Vector3 offset => root.forward*-.01f;
            public override Vector3 offset => Vector3.zero;
            public Shin(Person person, string side = "l")
            {
                name = $"{person.atom.uid} {side}Shin";
                atom = person.atom;
                var thigh = atom.rigidbodies.First(x => x.name == $"{side}Shin");
                subTargets.Add(thigh.transform);
                // excludeFromOcclusion.Add(rootRB.GetComponent<Collider>());
                foreach (Transform child in thigh.transform)
                {
                    // child.Print();
                    var col = child.GetComponent<Collider>();
                    if (col)
                    {
                        excludeFromOcclusion.Add(col);
                    }

                    if (child.name == $"FemaleAutoColliders{side}Shin")
                    {
                        foreach (Transform child1 in child)
                        {
                            var col1 = child1.GetChild(0).GetComponent<Collider>();
                            if (col1)
                            {
                                excludeFromOcclusion.Add(col1);
                                // col1.Print();
                            }
                        }
                    }
                }
                // excludeFromOcclusion = rootRB.GetComponentsInChildren<Collider>(true).ToList();
                
                subTargets.AddRange(excludeFromOcclusion.Select(x => x.transform));
                // hasSingleSubTarget = subTargets.Count == 1;
                // subTargets.ForEach(x => x.Draw());
                rootRB = subTargets[8].GetComponent<Rigidbody>();
                // root = rootRB.transform;
                // subTargets[8].Draw();
            }
        }
        
        public class Foot : GazeTarget
        {
            // public override Vector3 offset => root.up*.05f;
            public override Vector3 offset => Vector3.zero;
            public Foot(Person person, string side = "l")
            {
                name = $"{person.atom.uid} {side}Foot";
                atom = person.atom;
                var foot = atom.rigidbodies.First(x => x.name == $"{side}Foot").transform;
                // var foot = root.transform;
                subTargets.Add(foot);
                subTargets.Add(foot.Find($"{side}Toe/{side}BigToe"));
                subTargets.Add(foot.Find($"{side}Toe/{side}SmallToe1"));
                subTargets.Add(foot.Find($"{side}Toe/{side}SmallToe2"));
                subTargets.Add(foot.Find($"{side}Toe/{side}SmallToe3"));
                subTargets.Add(foot.Find($"{side}Toe/{side}SmallToe4"));
                rootRB = subTargets[1].GetComponent<Rigidbody>();
                excludeFromOcclusion = foot.GetComponentsInChildren<Collider>().ToList();
            }
        }
        
        public class GazeDildo : GazeTarget
        {
            public GazeDildo(Dildo dildo)
            {
                atom = dildo.atom;
                name = atom.type;
                excludeFromOcclusion = atom.GetComponentsInChildren<Collider>(true).Where(x => x.attachedRigidbody).ToList();
                subTargets.AddRange(excludeFromOcclusion.Where(x => x.name.ToLower() != "object").Select(x => x.transform));
                rootRB = atom.rigidbodies.First(x => x.name == "b3");
                excludeFromOcclusion.AddRange(FillMeUp.containingPerson.lHand.GetComponentsInChildren<Collider>());
                excludeFromOcclusion.AddRange(FillMeUp.containingPerson.rHand.GetComponentsInChildren<Collider>());
            }
        }
        
        public class ObjectTarget : GazeTarget
        {
            public Transform meshTransform;
            protected List<SubMesh> visibleMeshes = new List<SubMesh>();
            public List<SubMesh> meshes;
            private SubMesh selectedMesh;
            public UIDynamicGazeItem uid;

            public ObjectTarget(Atom atom, List<SubMesh> meshes, string name = null)
            {
                this.atom = atom;
                if (name != null) this.name = name;
                else this.name = atom.uid;
                this.meshes = meshes;
                rootRB = atom.rigidbodies[0];
                excludeFromOcclusion = atom.GetComponentsInChildren<Collider>(true).Where(x => x.attachedRigidbody).ToList();
                for (int j = 0; j < PoseMe.poses.Count; j++)
                {
                    PoseMe.poses[j].gazeSettings.targetSettings[this] = new GazeSettings.TargetSetting(this);
                }
                enabled.setCallbackFunction += OnToggled;
                enabled.val = !(meshes == null || meshes.Count == 0);
            }

            private void OnToggled(bool val)
            {
                if (val && (meshes == null || meshes.Count == 0))
                {
                    if (uid != null) uid.SetToggleState(false);
                    interest.SetInteractable(false);
                    enabled.valNoCallback = false;
                    "This object does not contain valid meshes.".Print();
                }
                else
                {
                    if (uid != null) uid.SetToggleState(val);
                    interest.SetInteractable(val);
                }
            }

            public virtual bool GetVisibleMeshes(Gaze gaze)
            {
                visibleMeshes.Clear();
                Transform transform = gaze.centerEye;

                for (int i = 0; i < meshes.Count; i++)
                {
                    var subMesh = meshes[i];
                    if (!subMesh.transform.gameObject.activeSelf) continue;
                    var v = (subMesh.GetAverage() - transform.position).normalized;
                    var dot = Vector3.Dot(v, transform.forward);
                    // var range = NormalDistribution.GetValue(.98f, -.2f, 3, true);
                    // if(dot > (1f+gazeCosine)*.5f) visibleMeshes.Add(subMesh);
                    if(dot > gazeCosine) visibleMeshes.Add(subMesh);
                    
                }
                return visibleMeshes.Count > 0;
            }

            public void SelectMesh()
            {
                int n = visibleMeshes.Count;
                switch (n)
                {
                    case 0:
                    {
                        selectedMesh = meshes.Count == 1 ? meshes[0] : meshes[Random.Range(0, meshes.Count)];
                        break;
                    }
                    case 1:
                    {
                        selectedMesh = visibleMeshes[0];
                        break;
                    }
                    default:
                    {
                        // $"{rnd}/{i}".Print();
                        selectedMesh = visibleMeshes[Random.Range(0, n)];
                        break;
                    }
                }
                // $"{i} {meshTransform.name}".Print();
                meshTransform = selectedMesh.transform;
            }
            
            public Vector3 SelectVertex()
            {
                return selectedMesh.vertices[Random.Range(0, selectedMesh.vertices.Length)];
            }

            public Vector3 GetWorldVertexPosition(Vector3 vertex)
            {
                return meshTransform.TransformPoint(vertex);
            }
            
            public override Vector3 GetVirtualPosition(Gaze gaze, Vector3 position, MirrorObject mirror)
            {
                Vector3 virtPos;
                if (mirror.Mirror(gaze, meshTransform.TransformPoint(position), excludeFromOcclusion, out virtPos))
                {
                    return virtPos;
                }
                gaze.SelectRandomTarget();
                return Vector3.zero;
            }

            public class SubMesh
            {
                public Transform transform;
                public Vector3[] vertices;
                public Vector3 averagePos;

                public SubMesh(Transform transform, Vector3[] verts)
                {
                    this.transform = transform;
                    vertices = verts;
                    averagePos = Vector3.zero;
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        averagePos += vertices[i];
                    }

                    averagePos /= vertices.Length;
                }

                // public Vector3 GetVertex(int i)
                // {
                //     return transform.TransformPoint(vertices[i]);
                // }

                public Vector3 GetAverage()
                {
                    return transform.TransformPoint(averagePos);
                }
            }
            
            // public virtual void Destroy(){}
        }

        public class CuaTarget : ObjectTarget
        {
            private CustomUnityAssetLoader cuaLoader;
            
            public CuaTarget(Atom atom, List<SubMesh> meshes, string name = null) : base(atom, meshes, name)
            {
                this.meshes = new List<SubMesh>();
                cuaLoader = atom.GetComponentInChildren<CustomUnityAssetLoader>();
                cuaLoader.RegisterAssetLoadedCallback(OnCuaLoaded);
                cuaLoader.RegisterAssetClearedCallback(OnCuaCleared);
                GetMeshes();
            }

            private void OnCuaLoaded()
            {
                DeferredOnCuaLoaded().Start();
            }
            
            private IEnumerator DeferredOnCuaLoaded()
            {
                yield return new WaitForEndOfFrame();
                GetMeshes();
            }

            private void GetMeshes()
            {
                meshes.Clear();
                // cua.reParentObject.PrintHierarchy();
                foreach (var meshFilter in atom.reParentObject.GetComponentsInChildren<MeshFilter>())
                {
                    var sharedMesh = meshFilter.sharedMesh;
                    if(!sharedMesh.isReadable) continue;
                    var verts = sharedMesh.vertices;
                    if(verts.Length == 0) continue;
                    meshes.Add(new SubMesh(meshFilter.transform, verts));
                    // meshFilter.transform.Draw();
                    // $"{sharedMesh.vertexCount}/{sharedMesh.vertices.Length}".Print();
                }
                if (meshes.Count == 0) OnCuaCleared();
                else enabled.val = true;
            }

            private void OnCuaCleared()
            {
                meshes.Clear();
                enabled.val = false;
                for (int i = 0; i < gazes.Count; i++)
                {
                    gazes[i].SelectRandomTarget();
                }
            }

            public override void Destroy()
            {
                base.Destroy();
                cuaLoader.DeregisterAssetLoadedCallback(OnCuaLoaded);
                cuaLoader.DeregisterAssetClearedCallback(OnCuaCleared);
            }
        }
        
        public class EnvironmentTarget : CuaTarget
        {
            public EnvironmentTarget(Atom atom, List<SubMesh> meshes, string name = null) : base(atom, meshes, name)
            {
                
            }
            
            public override bool GetVisibleMeshes(Gaze gaze)
            {
                visibleMeshes.Clear();
                Transform transform = gaze.centerEye;

                for (int i = 0; i < meshes.Count; i++)
                {
                    var subMesh = meshes[i];
                    var v = (subMesh.GetAverage() - transform.position).normalized;
                    var dot = Vector3.Dot(v, transform.forward);
                    // var range = NormalDistribution.GetValue(.98f, -.2f, 3, true);
                    if(dot > (1f+gazeCosine)*.5f) visibleMeshes.Add(subMesh);
                    
                }
                return visibleMeshes.Count > 0;
            }
        }
    }
}