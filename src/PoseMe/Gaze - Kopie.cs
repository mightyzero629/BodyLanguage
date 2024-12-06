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
        private bool enabled;
        private Atom atom;
        private EyesControl eyeBehavior;
        private DAZMeshEyelidControl eyelidBehavior;
        private Transform centerEye;
        private ITarget target;
        private Transform subTarget;
        private Transform targetGO;
        public static List<GazeTarget> targets;
        private List<ITarget> validTargets = new List<ITarget>();
        private static List<BoxCollider> mirrors;
        private List<float> interests = new List<float>();
        private static PlayerFace playerFace;
        private static VRHand lVRHand;
        private Rigidbody head;
        private Vector3 torque;
        private float gazeSpeed;
        private FreeControllerV3 headCtrl;

        public JSONStorableBool enabledJ = new JSONStorableBool("Enabled", true);
        
        private static JSONStorableFloat gazeStrength = new JSONStorableFloat("Gaze Strength", 500f, 0f, 5000f);
        private static JSONStorableFloat gazeSpeedMean = new JSONStorableFloat("Gaze Speed Mean", 1.5f, .1f, 2f);
        private static JSONStorableFloat gazeSpeedDelta = new JSONStorableFloat("Gaze Speed Delta", 1.3f, 0f, 2f);
        private static JSONStorableBool gazeSpeedOnesided = new JSONStorableBool("Gaze Speed Onesided", true);
        private static JSONStorableFloat targetTimeMean = new JSONStorableFloat("Target Time Mean", 5f, .1f, 10f);
        private static JSONStorableFloat targetTimeDelta = new JSONStorableFloat("Target Time Delta", -4.5f, -10f, 10f);
        private static JSONStorableBool targetTimeOnesided = new JSONStorableBool("Target Time Onesided", true);
        private static JSONStorableFloat subTargetTimeMean = new JSONStorableFloat("SubTarget Time Mean", 1f, .1f, 10f);
        private static JSONStorableFloat subTargetTimeDelta = new JSONStorableFloat("SubTarget Time Delta", -.9f, -10f, 10f);
        private static JSONStorableBool subTargetTimeOnesided = new JSONStorableBool("SubTarget Time Onesided", true);
        
        private static JSONStorableBool autoTarget = new JSONStorableBool("Auto Switch Target", true);
        public static JSONStorableBool touchReactionsEnabled = new JSONStorableBool("Touch Reactions Enabled", true);
        private static JSONStorableFloat gazeAngle = new JSONStorableFloat("Gaze Angle", 70f, 0f, 90f);
        
        private static JSONStorableFloat selfInterest = new JSONStorableFloat("Self Interest", .25f, 0f, 1f);

        private static JSONStorableStringChooser focusTargetChooser =
            new JSONStorableStringChooser("FocusTarget", null, null, "Focus Target", val => OnFocusTargetChanged(val, 5f));
        private static JSONStorableStringChooser objectChooser =
            new JSONStorableStringChooser("objectChooser", null, null, "Object", RegisterObjectFromChooser);
        private static JSONStorableStringChooser environmentChooser =
            new JSONStorableStringChooser("cuaChooser", null, null, "Environment", RegisterObjectFromChooser);

        private static JSONStorableAction focusOnTarget2 =
            new JSONStorableAction("Focus Target 2s", () => OnFocusTargetChanged(focusTargetChooser.val, 2f));
        private static JSONStorableAction focusOnTarget5 =
            new JSONStorableAction("Focus Target 5s", () => OnFocusTargetChanged(focusTargetChooser.val, 5f));
        private static JSONStorableAction focusOnTarget10 =
            new JSONStorableAction("Focus Target 10s", () => OnFocusTargetChanged(focusTargetChooser.val, 10f));
            
        private JSONStorableString targetInfo = new JSONStorableString("", "");

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
        private bool debug;
        private IEnumerator deferredInit;
        private static List<ObjectTarget> customTargets = new List<ObjectTarget>();

        private static Atom environment;
        private static CustomUnityAssetLoader environmentCuaLoader;
        
        public static JSONClass Store()
        {
            var jc = new JSONClass();
            gazeStrength.Store(jc, false);
            gazeSpeedMean.Store(jc, false);
            gazeSpeedDelta.Store(jc, false);
            gazeSpeedOnesided.Store(jc, false);
            targetTimeMean.Store(jc, false);
            targetTimeDelta.Store(jc, false);
            targetTimeOnesided.Store(jc, false);
            subTargetTimeMean.Store(jc, false);
            subTargetTimeDelta.Store(jc, false);
            subTargetTimeOnesided.Store(jc, false);
            autoTarget.Store(jc, false);
            gazeAngle.Store(jc, false);
            selfInterest.Store(jc, false);
            for (int i = 0; i < targets.Count; i++)
            {
                targets[i].Store(jc, false);
            }
            return jc;
        }
        
        public static JSONClass Load()
        {
            var jc = new JSONClass();
            gazeStrength.Load(jc, true);
            gazeSpeedMean.Load(jc, true);
            gazeSpeedDelta.Load(jc, true);
            gazeSpeedOnesided.Load(jc, true);
            targetTimeMean.Load(jc, true);
            targetTimeDelta.Load(jc, true);
            targetTimeOnesided.Load(jc, true);
            subTargetTimeMean.Load(jc, true);
            subTargetTimeDelta.Load(jc, true);
            subTargetTimeOnesided.Load(jc, true);
            autoTarget.Load(jc, true);
            gazeAngle.Load(jc, true);
            selfInterest.Load(jc, true);
            for (int i = 0; i < targets.Count; i++)
            {
                targets[i].Load(jc, true);
            }
            return jc;
        }

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
            targetGO.localScale = new Vector3(.01f, .01f, .01f);
            
            
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
            if (targets == null)
            {
                StaticInit();
            }
            if(mirrors == null) GetMirrors();
            

            enabledJ.name = $"{atom.uid} Enabled";
            enabledJ.setCallbackFunction += val =>
            {
                if (val)
                {
                    var rotationState = headCtrl.currentRotationState;
                    if (!(rotationState == FreeControllerV3.RotationState.On || rotationState == FreeControllerV3.RotationState.ParentLink))
                    {
                        enabledJ.valNoCallback = false;
                        $"BL: Gaze on atom {atom.uid} not enabled. Head controller rotation state has to be either 'On' or 'Parent Link'.".Print();
                        return;
                    }
                    eyeBehavior.lookAt1.target = targetGO;
                    eyeBehavior.lookAt2.target = targetGO;
                    eyeBehavior.currentLookMode = EyesControl.LookMode.Custom;
                    SelectTarget();
                }
                else
                {
                    eyeBehavior.currentLookMode = cachedLookMode;
                }
                enabled = val;
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
            while ((eyeBehavior = (EyesControl)atom.GetStorableByID("Eyes")) == null)
            {
                yield return null;
            }
            cachedLookMode = eyeBehavior.currentLookMode;
            if (enabledJ.val)
            {
                eyeBehavior.currentLookMode = EyesControl.LookMode.Custom;
                eyeBehavior.lookAt1.target = targetGO;
                eyeBehavior.lookAt2.target = targetGO;
                eyeBehavior.lookAt1.MinEngageDistance = eyeBehavior.lookAt2.MinEngageDistance = 0f;
            }
            // lEye = eyeBehavior.lookAt1.gameObject.transform;
            // rEye = eyeBehavior.lookAt2.gameObject.transform;
            centerEye = eyeBehavior.lookAt1.centerForDepthAdjust;
            enabled = enabledJ.val;
        }

        private static void StaticInit()
        {
            targets = new List<GazeTarget>();
            playerFace = new PlayerFace();
            targets.Add(playerFace);
            targets.Add(new VRHand());
            targets.Add(new VRHand("r"));
            foreach (var a in SuperController.singleton.GetAtoms())
            {
                if(a.type == "Dildo") RegisterTarget(new Dildo(a));
                else if(a.type == "ToyBP" || a.type == "ToyAH" || a.type == "Paddle") RegisterObject(a);
            }
            gazeAngle.AddCallback(val => gazeCosine = Mathf.Cos(Mathf.PI*val/180f));
            // cuaChooser.val = "CustomUnityAsset";
            playerFace.interest.val = 0f;
            SyncObjectChooser();
            // RegisterObject(SuperController.singleton.GetAtomByUid("AptChair"));
        }

        private static void RegisterObject(Atom atom, bool custom = false)
        {
            if(atom.type != "CustomUnityAsset")
            {
                var meshes = new List<ObjectTarget.SubMesh>();
                // cua.reParentObject.PrintHierarchy();
                foreach (var meshFilter in atom.reParentObject.GetComponentsInChildren<MeshFilter>())
                {
                    var sharedMesh = meshFilter.sharedMesh;
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
                    PoseMe.CreateUIDynamicGazeTarget(target);
                }
            }
            else
            {
                var target = new CuaTarget(atom, null, atom.uid);
                RegisterTarget(target);
                if (custom && atom != environment)
                {
                    customTargets.Add(target);
                    PoseMe.CreateUIDynamicGazeTarget(target);
                }
            }
            SyncObjectChooser();
            SyncEnvironmentChooser();
            SyncFocusTargetChooser();
        }

        public static void RegisterPerson(Person person)
        {
            if (targets == null)
            {
                targets = new List<GazeTarget>();
            }

            targets.Add(new PersonFace(person.atom));
            targets.Add(new Hand(person));
            targets.Add(new Hand(person, "r"));
            targets.Add(new Breast(person));
            targets.Add(new Breast(person, "r"));
            if (person.characterListener.gender == DAZCharacterSelector.Gender.Male)
            {
                targets.Add(new Penis(person));
            }
            else
            {
                targets.Add(new Foot(person));
                targets.Add(new Foot(person, "r"));
                var penis = new Penis(person)
                {
                    enabled = {val = person.characterListener.isFuta}
                };
                person.characterListener.OnChangedToFuta.AddListener(() => penis.enabled.val = true);
                person.characterListener.OnChangedToFemale.AddListener(() => penis.enabled.val = false);
                targets.Add(penis);
            }
            SyncFocusTargetChooser();
        }
        
        public static void RegisterTarget(GazeTarget target)
        {
            if (targets == null)
            {
                targets = new List<GazeTarget>();
            }
            targets.Add(target);
            SyncObjectChooser();
            SyncEnvironmentChooser();
        }

        public static void DeregisterAtom(Atom atom)
        {
            foreach (var target in targets.Where(x => x.atom == atom))
            {
                target.Destroy();
            }
            targets.RemoveAll(x => x.atom == atom);
            customTargets.RemoveAll(x => x.atom == atom);
            SyncObjectChooser();
            SyncEnvironmentChooser();
        }
        
        private static void OnFocusTargetChanged(string val, float duration)
        {
            if (string.IsNullOrEmpty(val))
            {
                "Select a target first.".Print();
                return;
            }
            var focusTarget = targets.First(x => x.name == val);
            for (int i = 0; i < gazes.Count; i++)
            {
                var gaze = gazes[i];
                gaze.focusTimer = duration;
                gaze.focussing = true;
                gaze.SelectTarget(focusTarget, .22f);
            }
            // focusTargetChooser.valNoCallback = "";
        }

        private static void SyncFocusTargetChooser()
        {
            focusTargetChooser.SetChoices(targets.Select(x => x.name));
        }

        private static void SyncEnvironmentChooser()
        {
            environmentChooser.SetChoices(SuperController.singleton.GetAtoms().Where(x => customTargets.All(t => t.atom != x) && x.type == "CustomUnityAsset").Select(x => x.uid));
            if (!environmentChooser.choices.Contains(environmentChooser.val)) environmentChooser.valNoCallback = "";
        }

        private static void SyncObjectChooser()
        {
            objectChooser.SetChoices(SuperController.singleton.GetAtoms().Where(x => customTargets.All(t => t.atom != x) &&!x.IsToyOrDildo() && x.type != "Person").Select(x => x.uid));
        }

        private Vector3 saccade;
        private Vector3 GetSaccade()
        {
            return new Vector3(Random.Range(-.05f, .05f), Random.Range(-.05f, .05f), Random.Range(-.05f, .05f));
        }

        public void Focus(GazeTarget focusTarget, float minSpeed, float time = 2f)
        {
            focusTimer = 2f;
            focussing = true;
            SelectTarget(focusTarget, minSpeed);
            $"Fucusing {focusTarget.atom.uid}:{focusTarget.name}".Print();
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

        public void FixedUpdate()
        {
            try
            {
                if(!enabled) return;
                var rotationState = headCtrl.currentRotationState;
                if (!(rotationState == FreeControllerV3.RotationState.On || rotationState == FreeControllerV3.RotationState.ParentLink))
                {
                    enabledJ.val = false;
                    $"BL: Gaze on atom {atom.uid} disabled. Head controller rotation state has to be either 'On' or 'Parent Link'.".Print();
                    return;
                }
                if (focussing)
                {
                    focusTimer -= Time.fixedDeltaTime;
                    if (focusTimer < 0f)
                    {
                        focussing = false;
                    }
                }
                else if(autoTarget.val)
                {
                    targetTimer -= Time.fixedDeltaTime;
                    if (targetTimer < 0f)
                    {
                        targetTimer = NormalDistribution.GetValue(targetTimeMean.val, targetTimeDelta.val, 2, targetTimeOnesided.val);
                        SelectTarget();
                    }
                }

                subTargetTimer -= Time.fixedDeltaTime;
                
                if (subTargetTimer < 0f)
                {
                    // lerpToTarget.Stop();
                    subTargetTimer = NormalDistribution.GetValue(subTargetTimeMean.val, subTargetTimeDelta.val, 2,
                        subTargetTimeOnesided.val);
                    var objectTarget1 = target as ObjectTarget;
                    if(objectTarget1 == null)
                    {
                        if (target.hasSingleSubTarget)
                        {
                            subTarget = target.root;
                            saccade = GetSaccade();
                        }
                        else
                        {
                            subTarget = target.GetSubTarget();
                        }
                        
                        if(debug) targetInfo.val =
                            $"{atom.uid}:\n    {target.atom.uid}\n        {target.name}\n            {subTarget.name}";
                    }
                    else
                    {

                        // if (!objectTarget1.SelectVertex(out meshVertex))
                        // {
                        //     targetTimer = 0f;
                        //     return;
                        // }
                        meshVertex = objectTarget1.SelectVertex();
                        if(debug) targetInfo.val = $"{atom.uid}:\n    {target.atom.uid}\n        {objectTarget1.meshTransform.name}";
                        
                    }
                }
                if(target == null) return;
                var objectTarget2 = target as ObjectTarget;
                if(objectTarget2 == null)
                {
                    var pos = subTarget.position;
                    if (target.hasSingleSubTarget) pos += saccade;
                    targetGO.position = pos;
                }
                else
                {
                    targetGO.position = objectTarget2.meshTransform.TransformPoint(meshVertex);
                }

                if (target is VirtualTarget) ((VirtualTarget)target).Update();
                // gazeSpeed.Print();
                torque = Vector3.Lerp(torque,
                    gazeStrength.val * Vector3.Cross(head.transform.forward, (targetGO.position - head.position).normalized),
                    gazeSpeed*Time.fixedDeltaTime);
                
                head.AddTorque(torque);

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
                SuperController.LogError(e.ToString());
            }
        }

        public void SelectTarget(GazeTarget newTarget, float minSpeed = .1f)
        {
            gazeSpeed = Mathf.Max(NormalDistribution.GetValue(gazeSpeedMean.val, gazeSpeedDelta.val, 2, gazeSpeedOnesided.val) * 50f / gazeStrength.val, minSpeed);
            if (target != null && newTarget != target && Vector3.Angle(newTarget.root.position - head.position, target.root.transform.position - head.position) > 25f)
            {
                eyelidBehavior.Blink();
            }
            target = newTarget;
            subTargetTimer = 0f;
        }

        public void SelectTarget()
        {
            gazeSpeed = NormalDistribution.GetValue(gazeSpeedMean.val, gazeSpeedDelta.val, 2, gazeSpeedOnesided.val) * 50f / gazeStrength.val;
            validTargets.Clear();
            var basePosition = centerEye.position + head.transform.forward * .05f;
            for (int i = 0; i < targets.Count; i++)
            {
                var t = targets[i];
                if(!t.enabled.val || t.interest.val == 0f) continue;
                var objectTarget = t as ObjectTarget;
                if (objectTarget != null)
                {
                    if (objectTarget.GetVisibleMeshes()) validTargets.Add(objectTarget);
                }
                else
                {
                    var v = t.root.position + t.root.transform.forward * .05f - basePosition;
                    if (Vector3.Dot(v, head.transform.forward) / v.magnitude > gazeCosine && !t.IsOccluded(-v))
                    {
                        validTargets.Add(t);
                    }
                }
            }

            for (int j = 0; j < mirrors.Count; j++)
            {
                var mirror = mirrors[j];
                for (int k = 0; k < targets.Count; k++)
                {
                    var target = targets[k];
                    if(target.atom == environment) continue;
                    if(!target.enabled.val || target.interest.val == 0f) continue;
                    var plane = GetPlane(mirror);
                    if (!plane.SameSide(target.root.position, basePosition)) continue;
                    Vector3 virtPos;
                    if(Mirror(mirror, plane, target.root.position+target.offset, target.excludeFromOcclusion, out virtPos))
                    {
                        var v = virtPos - basePosition;
                        if (Vector3.Dot(v, centerEye.transform.forward) / v.magnitude > gazeCosine)
                        {
                            validTargets.Add(new VirtualTarget(this, target, mirror, v.sqrMagnitude));
                        }
                    }
                }
            }
            ITarget newTarget;
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
        
        private ITarget ChooseTarget()
        {
            interests.Clear();
            var sum = 0f;
            for (int i = 0; i < validTargets.Count; i++)
            {
                var target = validTargets[i];
                float squareDist;
                if (target is VirtualTarget)
                {
                    squareDist = ((VirtualTarget)target).virtualSquareDist;
                }
                else squareDist = (target.root.position - head.position).sqrMagnitude;
                var delta = target.interest.val * (target.rootRB.velocity.sqrMagnitude + 1f + 1f / (1f+squareDist));
                if (target.atom == atom) delta *= selfInterest.val;
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

            var t = validTargets[j];
            if (t is ObjectTarget)
            {
                ((ObjectTarget)t).SelectMesh();
            }

            if (t.atom.IsToyOrDildo()) ReadMyLips.dynamicStimGain += .0005f;
            // if(atom != PoseMe.atom) $"{atom.uid} -> {t.atom.uid}:{t.name}".Print();
            return t;
        }

        private static Plane GetPlane(BoxCollider mirror)
        {
            var mirrorTransform = mirror.transform;
            var mirrorPosition = mirrorTransform.position;
            var mirrorNormal = mirrorTransform.up;
            return new Plane(mirrorNormal, mirrorPosition);
        }
        
        private bool Mirror(BoxCollider mirror, Vector3 position, List<Collider> excludeFromOcclusion, out Vector3 virtualPosition)
        {
            virtualPosition = Vector3.zero;
            return Mirror(mirror, GetPlane(mirror), position, excludeFromOcclusion, out virtualPosition);
        }
        
        private bool Mirror(BoxCollider mirror, Plane plane, Vector3 position, List<Collider> excludeFromOcclusion, out Vector3 virtualPosition)
        {
            virtualPosition = Vector3.zero;
            var planePoint = plane.ClosestPointOnPlane(position);
            
            virtualPosition = planePoint + (planePoint - position);
            float dist;
            var centerPos = centerEye.position + head.transform.forward * .05f;
            var viewDirection = virtualPosition - centerPos;
            var ray = new Ray(centerPos, viewDirection);
            plane.Raycast(new Ray(centerPos, viewDirection), out dist);
            var image = ray.GetPoint(dist);
            var transform = mirror.transform;
            var fromCenter = transform.InverseTransformPoint(image);
            if (Mathf.Abs(fromCenter.x) > .5f*mirror.size.x || Mathf.Abs(fromCenter.z) > .5f*mirror.size.z) return false;

            if (IsOccluded(planePoint, position - planePoint, excludeFromOcclusion)) return false;
            if(IsOccluded(image, centerPos - image, excludeFromOcclusion)) return false;


            return true;
        }
        
        private bool IsOccluded(Vector3 start, Vector3 direction, List<Collider> excludeFromOcclusion = null)
        {
            var rayHits = Physics.RaycastNonAlloc(start, direction, rayCastBuffer, direction.magnitude * .8f);
            for (int j = 0; j < rayHits; j++)
            {
                var col = rayCastBuffer[j].collider;
                if(col.isTrigger || col.name.Contains("Control") || col.name.Contains("Link") || excludeFromOcclusion.Contains(col)) continue;
                var atom = col.GetAtom();
                if(!atom) continue;
                if (atom.type.Contains("Glass")) continue;
                return true;
            }
            // $"{atom.name}:{root.name} is not occluded".Print();
            return false;
        }

        private void GetMirrors()
        {
            mirrors = new List<BoxCollider>();
            foreach (var atom in SuperController.singleton.GetAtoms())
            {
                if (atom.type.Contains("Reflective") || atom.type.Contains("Glass"))
                {
                    var box = atom.GetComponentInChildren<BoxCollider>(true);
                    if (box)
                    {
                        mirrors.Add(box);
                    }
                }
            }
        }

        public void OnAtomAdded(Atom atom)
        {
            if (atom.type.Contains("Reflective") || atom.type.Contains("Glass"))
            {
                var box = atom.GetComponentInChildren<BoxCollider>(true);
                if (box)
                {
                    mirrors.Add(box);
                }
            }
            else if (atom.type == "Dildo")
            {
                RegisterTarget(new Dildo(atom));
            }
            else if (atom.IsToy() || atom.type == "Paddle")
            {
                RegisterObject(atom);
            }
            // else if (atom.type == "ToyBP")
            // {
            //     RegisterTarget(new ObjectTarget(atom, "ButtPlug"));
            // }
            SyncObjectChooser();
            SyncEnvironmentChooser();
            // atom.type.Print();
        }
        
        public void OnAtomRemoved(Atom atom)
        {
            if (atom.type.Contains("Reflective") || atom.type.Contains("Glass"))
            {
                var box = atom.GetComponentInChildren<BoxCollider>(true);
                if (box)
                {
                    mirrors.Remove(box);
                }
            }
            DeregisterAtom(atom);
            PoseMe.gaze.SelectTarget();
            for (int i = 0; i < PoseMe.persons.Count(); i++)
            {
                PoseMe.persons[i].RefreshGazeTarget();
            }
            SyncEnvironmentChooser();
            SyncObjectChooser();
        }

        public static void OnAtomRenamed(string oldUid, string newUid)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                target.name = target.name.Replace(oldUid, newUid);
            }
        }
        

        public static void CreateUI()
        {
            var UIElements = PoseMe.UIElements;
            focusTargetChooser.CreateUI(UIElements, false, chooserType:2);
            gazeSpeedMean.CreateUI(UIElements);
            gazeSpeedDelta.CreateUI(UIElements, true);
            targetTimeMean.CreateUI(UIElements);
            targetTimeDelta.CreateUI(UIElements, true);
            subTargetTimeMean.CreateUI(UIElements);
            subTargetTimeDelta.CreateUI(UIElements, true);
            subTargetTimeOnesided.CreateUI(UIElements, true);
            targetTimeOnesided.CreateUI(UIElements, true);
            gazeStrength.CreateUI(UIElements);
            gazeAngle.CreateUI(UIElements, true);

            PoseMe.singleton.SetupButton("Select Random Target", false, PoseMe.gaze.SelectTarget, UIElements);
            autoTarget.CreateUI(UIElements, true);
            
            var tf = PoseMe.singleton.CreateTextField(PoseMe.gaze.targetInfo, true);
            UIElements.Add(tf);
            PoseMe.singleton.SetupButton("Configure Interests", false, CreateConfigureInterestsUI, UIElements);
            PoseMe.singleton.SetupButton("Custom Targets", false, CreateCustomTargetsUI, UIElements);
            for (int i = 0; i < PoseMe.persons.Count; i++)
            {
                PoseMe.persons[i].gaze.enabledJ.CreateUI(UIElements);
            }

            environmentChooser.CreateUI(UIElements);
            
            

            // for (int i = 0; i < targets.Count; i++)
            // {
            //     targets[i].Debug();
            // }

            SetDebugMode(true);
        }

        public static void CreateConfigureInterestsUI()
        {
            PoseMe.singleton.ClearUI();
            var button = PoseMe.singleton.CreateButton("Return");
            button.buttonColor = PoseMe.navColor;
            button.button.onClick.AddListener(() =>
            {
                PoseMe.singleton.ClearUI();
                PoseMe.singleton.CreateUI();
            });
            PoseMe.UIElements.Add(button);
            PoseMe.singleton.SetupButton("Click here!! :)", true, () => "Congratulations! A new washing machine will be delivered to your location within the next three working days.\nPlease make sure to have sufficient founds available upon arrival.".Print(), PoseMe.UIElements);
            for (int i = 0; i < targets.Count; i++)
            {
                UIDynamicSlider slider = targets[i].interest.CreateUI(PoseMe.UIElements, i%2==1) as UIDynamicSlider;
                if (!targets[i].enabled.val)
                {
                    slider.SetInteractable(false);
                }
            }
        }
        
        public static void CreateCustomTargetsUI()
        {
            PoseMe.singleton.ClearUI();
            var button = PoseMe.singleton.CreateButton("Return");
            button.buttonColor = PoseMe.navColor;
            button.button.onClick.AddListener(() =>
            {
                PoseMe.singleton.ClearUI();
                PoseMe.singleton.CreateUI();
            });
            PoseMe.UIElements.Add(button);
            objectChooser.CreateUI(PoseMe.UIElements, true);
            for (int i = 0; i < customTargets.Count; i++)
            {
                var target = customTargets[i];
                if(target.atom != environment) PoseMe.CreateUIDynamicGazeTarget(target);
            }
        }

        public static void RegisterObjectFromChooser(JSONStorableStringChooser chooser)
        {
            var atom = SuperController.singleton.GetAtomByUid(chooser.val);
            if (atom == null) return;
            if (chooser == environmentChooser) environment = atom;
            else
            {
                chooser.valNoCallback = "";
                if (atom.type == "CustomUnityAsset")
                {
                    "For environments: Please use the 'Environment' chooser. They are treated differently.".Print();
                }
            }
            RegisterObject(atom, true);
        }

        public static void SetDebugMode(bool val)
        {
            for (int i = 0; i < gazes.Count; i++)
            {
                var gaze = gazes[i];
                gaze.targetRenderer.enabled = val;
                gaze.debug = val;
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
            // eyeBehavior.currentLookMode = cachedLookMode;
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

        public interface ITarget
        {
            string name { get; set; }
            Transform root {get; set; }
            Rigidbody rootRB {get;}
            Atom atom {get;}
            JSONStorableFloat interest {get;}
            Transform GetSubTarget();
            bool hasSingleSubTarget {get;}
        }
        
        public class GazeTarget : ITarget
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

            public JSONStorableFloat interest {get;} = new JSONStorableFloat("Interest", 1f, 0f, 1f);
            public bool hasSingleSubTarget { get; set; } = false;
            public List<BoxCollider> mirrors = new List<BoxCollider>();

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

            public Transform GetSubTarget()
            {
                // if (subTargets.Count == 1) return subTargets[0];
                return subTargets[Random.Range(0, subTargets.Count)];
            }

            public void Debug()
            {
                for (int i = 0; i < subTargets.Count; i++)
                {
                    subTargets[i].Draw();
                }
            }

            public bool IsOccluded(Vector3 direction)
            {
                var rayHits = Physics.RaycastNonAlloc(root.position+offset,
                    direction, rayCastBuffer, direction.magnitude);
                // $"{root.name} {rayHits}".Print();
                for (int j = 0; j < rayHits; j++)
                {
                    var col = rayCastBuffer[j].collider;
                    if(col.isTrigger || col.name.Contains("Control") || col.name.Contains("Link") || excludeFromOcclusion.Contains(col)) continue;
                    var atom = col.GetAtom();
                    if(!atom) continue;
                    if (atom.type.Contains("Glass")) continue;
                    if (this is PersonFace)
                    {
                        $"{this.atom.name}:{root.name} occluded by {atom.uid} {col}".Print();
                        // col.transform.Draw();
                    }
                    return true;
                }
                // $"{atom.name}:{root.name} is not occluded".Print();
                return false;
            }

            private void OnToggle(bool val)
            {
                enabled.val = val;
                if (!val)
                {
                    for (int i = 0; i < gazes.Count; i++)
                    {
                        gazes[i].SelectTarget();
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
            public PlayerFace()
            {
                name = "PlayerFace";
                atom = SuperController.singleton.GetAtomByUid("[CameraRig]");
                var cam = Camera.main.transform;
                rootRB = cam.Find("CenterEye").GetComponent<Rigidbody>();
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
            public PersonFace(Atom atom)
            {
                name = $"{atom.uid} Face";
                this.atom = atom;
                rootRB = atom.rigidbodies.First(x => x.name == "head");
                root = rootRB.transform.Find("eyeCenter");
                // rootRB.transform.PrintChildren();
                // excludeFromOcclusion = rootRB.GetComponentsInChildren<Collider>(true).ToList();
                // var bones = atom.transform.Find("rescale2").GetComponentsInChildren<DAZBone>();
                subTargets.Add(rootRB.transform.Find("lEye"));
                subTargets.Add(rootRB.transform.Find("rEye"));
                subTargets.Add(rootRB.transform.Find("LipTrigger"));
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
                excludeFromOcclusion.AddRange(FillMeUp.containingPerson.lHand.GetComponentsInChildren<Collider>());
                excludeFromOcclusion.AddRange(FillMeUp.containingPerson.rHand.GetComponentsInChildren<Collider>());
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
                    var path = $"rescale2/geometry/FemaleMorphers/BreastPhysicsMesh/PhysicsMesh{s}areola";
                    subTargets.AddRange(atom.transform.Find(path).GetComponentsInChildren<Transform>().Where(x => x.name.StartsWith($"PhysicsMeshJoint{s}areola")));
                }
                else
                {
                    hasSingleSubTarget = true;
                }
            }
        }
        
        // public class Ass : GazeTarget
        // {
        //     public Ass(Gaze gaze, Person person, string side = "l")
        //     {
        //         this.gaze = gaze;
        //         atom = person.atom;
        //         if (person.dcs.gender == DAZCharacterSelector.Gender.Female)
        //         {
        //             string s = side == "l" ? "left" : "right";
        //             var path = $"rescale2/geometry/FemaleMorphers/BreastPhysicsMesh/PhysicsMesh{s}areola";
        //             subTargets.AddRange(atom.transform.Find(path).GetComponentsInChildren<Transform>().Where(x => x.name.StartsWith($"PhysicsMeshJoint{s}areola")));
        //         }
        //         root = subTargets[0];
        //     }
        // }
        
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
        
        public class Dildo : GazeTarget
        {
            public Dildo(Atom atom)
            {
                this.atom = atom;
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
            protected List<SubMesh> meshes;
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
                enabled.setCallbackFunction += val =>
                {
                    if (uid != null) uid.SetToggleState(val);
                };
            }

            public virtual bool GetVisibleMeshes()
            {
                visibleMeshes.Clear();
                Transform transform = PoseMe.gaze.centerEye;

                for (int i = 0; i < meshes.Count; i++)
                {
                    var subMesh = meshes[i];
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
                        PoseMe.gaze.targetTimer = 0f;
                        // meshTransform = selectedSubMesh.transform;
                        // return PoseMe.gaze.meshVertex;
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
            
            public Vector3 SelectVertex1()
            {
                SubMesh selectedSubMesh;
                int n = visibleMeshes.Count;
                switch (n)
                {
                    case 0:
                    {
                        PoseMe.gaze.targetTimer = 0f;
                        // meshTransform = selectedSubMesh.transform;
                        // return PoseMe.gaze.meshVertex;
                        return PoseMe.gaze.meshVertex;
                    }
                    case 1:
                    {
                        selectedSubMesh = visibleMeshes[0];
                        break;
                    }
                    default:
                    {
                        var rnd = Random.Range(0, n);
                        // $"{rnd}/{i}".Print();
                        selectedSubMesh = visibleMeshes[rnd];
                        break;
                    }
                }
                // $"{i} {meshTransform.name}".Print();
                meshTransform = selectedSubMesh.transform;
                return selectedSubMesh.vertices[Random.Range(0, selectedSubMesh.vertices.Length)];
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
                OnCuaLoaded();
            }

            private void OnCuaLoaded()
            {
                try
                {
                    DeferredOnCuaLoaded().Start();
                }
                catch (Exception e)
                {
                    SuperController.LogError(e.ToString());
                }
            }
            
            private IEnumerator DeferredOnCuaLoaded()
            {
                yield return new WaitForEndOfFrame();
                meshes.Clear();
                // cua.reParentObject.PrintHierarchy();
                foreach (var meshFilter in atom.reParentObject.GetComponentsInChildren<MeshFilter>())
                {
                    var sharedMesh = meshFilter.sharedMesh;
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
                enabled.val = false;
                for (int i = 0; i < gazes.Count; i++)
                {
                    gazes[i].SelectTarget();
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
            
            public override bool GetVisibleMeshes()
            {
                visibleMeshes.Clear();
                Transform transform = PoseMe.gaze.centerEye;

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

        public struct VirtualTarget : ITarget
        {
            public string name { get; set; }
            // public JSONStorableBool enabled => target.enabled;
            public Rigidbody rootRB => target.rootRB;
            public Transform root { get; set; }
            public List<Collider> excludeFromOcclusion => target.excludeFromOcclusion;
            private readonly GazeTarget target;
            public readonly BoxCollider mirror;
            // private Transform subTarget;
            private readonly Gaze gaze;
            public Atom atom{ get; set; }
            public JSONStorableFloat interest => target.interest;
            public float virtualSquareDist{ get;}
            public bool hasSingleSubTarget => target.hasSingleSubTarget;

            public VirtualTarget(Gaze gaze, GazeTarget target, BoxCollider mirror, float virtualSquareDist)
            {
                this.gaze = gaze;
                this.target = target;
                this.mirror = mirror;
                this.virtualSquareDist = virtualSquareDist;
                // subTarget = target.subTargets[0];
                root = gaze.targetGO;
                atom = target.atom;
                name = target.name + " (virtual)";
            }

            public Transform GetSubTarget()
            {

                return target.GetSubTarget();
            }

            public void Update()
            {
                Vector3 virtPos;
                if (gaze.Mirror(mirror, target.root.position, excludeFromOcclusion, out virtPos))
                {
                    gaze.targetGO.position = virtPos;
                }
                else
                {
                    $"{gaze.atom.uid} lost target {atom.uid}:{name}".Print();
                    gaze.SelectTarget();
                }
            }
        }
    }
}