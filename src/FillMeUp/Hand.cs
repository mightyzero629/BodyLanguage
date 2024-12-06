using System.Collections;
using System.Linq;
using MacGruber;
using MeshVR.Hands;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace CheesyFX
{
    public class Hand : Fuckable
    {
        private float sideId;
        private Rigidbody handRB;
        public Hand other;
        private float penetratorRadius;
        private float widthToBendScale = 1f;
        private CapsuleCollider closestCollider;
        public GameObject triggerGO;
        private GameObject depthReference;
        private GameObject distReference;
        private float strokeDirection;
        private Vector3 radialInDirection;
        private Vector3 penetratorForward;
        private Vector3 fwrdCross;
        
        private float quicknessIn;
        private float quicknessOut;
        private bool fingersClosed;
        
        private Vector3 snapTorque;
        private float snapTorqueSign;

        private float lastDepth;
        private float depthUnsmoothed;
        private float lastDepthUnsmoothed;
        private LerpingFinger thumb;
        private LerpingFinger[] fingers;

        private Vector5 baseThumbTarget = new Vector5(25f, 30f, 24f,-20f,0f);
        private Vector5[] baseFingerTargets =
        {
            new Vector5(35f, 35f, 25f, 17f),
            new Vector5(40f, 40f, 25f, 8f),
            new Vector5(40f, 40f, 25f, 5f),
            new Vector5(30f, 30f, 25f, -8f),
            // new Vector5(30f, 30f, 30f)
        };


        private IEnumerator zeroSpeed;
        private IEnumerator zeroDepth;
        private IEnumerator zeroForce;

        private Vector3 snapForce;
        // public Force radialForce;

        private HandJobTrigger handjobTrigger;

        private HandControl handControl;
        private string cachedHandControlMode;

        private JSONStorableFloat grabScale = new JSONStorableFloat("Grab Scale", 1f, 0f, 2f);
        private JSONStorableFloat shrinkColliders = new JSONStorableFloat("Shrink Colliders", .9f, 0f, 1f, true);
        private JSONStorableFloat minFingersGrabbing = new JSONStorableFloat("Min Grabbing Fingers", 1f, 1f, 3f, true);
        private JSONStorableBool randomizeFingers = new JSONStorableBool("Randomize Fingers", true);
        
        private JSONStorableVector4 fingerGrabScale = new JSONStorableVector4("FingerGrab",
            new Vector4(1f, 1f, 1f, 1f),
            Vector4.zero,
            Vector4.one
        );
        
        public new Hand Init(string side)
        {
            type = 3;
            sideId = side == "l" ? -1f : 1f;
            base.Init(side + "Hand");
            CreateTriggersUI();
            ClearUI();

            depth.max = 1f;
            // depthTriggers = FillMeUp.singleton.AddFloatTriggerManager(depth, false, 0f, 0f, .25f);
            // speedTriggers = FillMeUp.singleton.AddFloatTriggerManager(speed, true, 0f, 0f, .5f);
            handRB = FillMeUp.atom.rigidbodies.FirstOrDefault(x => x.name == $"{side}Hand");
            HandOutput handOutput;
            if (sideId > 0f)
            {
                handControl = FillMeUp.atom.GetStorableByID("RightHandControl") as HandControl;
                handOutput = FillMeUp.atom.GetStorableByID("RightHandFingerControl") as HandOutput;
            }
            else
            {
                handControl = FillMeUp.atom.GetStorableByID("LeftHandControl") as HandControl;
                handOutput = FillMeUp.atom.GetStorableByID("LeftHandFingerControl") as HandOutput;
            }

            thumb = gameObject.AddComponent<LerpingFinger>().Init(handOutput, 4);
            fingers = new []
            {
                gameObject.AddComponent<LerpingFinger>().Init(handOutput, 0),
                gameObject.AddComponent<LerpingFinger>().Init(handOutput, 1),
                gameObject.AddComponent<LerpingFinger>().Init(handOutput, 2),
                gameObject.AddComponent<LerpingFinger>().Init(handOutput, 3)
            };
                
            audioSource = BodyRegionMapping.touchZones[$"{side}Palm"].slapAudioSource;

            triggerGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            triggerGO.name = $"BL_{side}HandTrigger";
            enterPointTF = triggerGO.transform;
            enterPointTF.SetParent(handControl.indexProximalBone.transform.parent, false);
            enterPointTF.localPosition = new Vector3(sideId * .05f, -.035f, -.01f);
            enterPointTF.localScale = new Vector3(.05f, .04f, .05f);
            enterPointTF.localRotation = Quaternion.Euler(0f, -sideId * 70f, -sideId * 85f);
            proximityTrigger = triggerGO.GetComponent<CapsuleCollider>();
            proximityTrigger.isTrigger = true;
            
            triggerGO.GetComponent<Renderer>().enabled = false;
            handjobTrigger = triggerGO.AddComponent<HandJobTrigger>();
            handjobTrigger.hand = this;
            foreach (var collider in FillMeUp.atom.GetComponentsInChildren<Collider>(true))
            {
                Physics.IgnoreCollision(proximityTrigger, collider);
            }

            foreach (var col in FillMeUp.atom.rigidbodies.First(x => x.name == "Gen1")
                         .GetComponentsInChildren<Collider>(true))
            {
                Physics.IgnoreCollision(proximityTrigger, col, false);
            }
            
            depthReference = new GameObject($"BL_{side}DepthReference");
            depthReference.transform.SetParent(handControl.indexProximalBone.transform.parent, false);
            depthReference.transform.localPosition = new Vector3(sideId * .065f, -.03f, .01f);

            distReference = new GameObject($"BL_{side}DistReference");
            distReference.transform.SetParent(enterPointTF, false);
            distReference.transform.localPosition = new Vector3(-sideId*0.3f, 0f, 0f);

            thrustForce.amplitude.mean.SetWithDefault(150f);
            thrustForce.amplitude.delta.SetWithDefault(100f);
            thrustForce.period.sharpness.SetWithDefault(2f);

            maleForce.amplitude.mean.SetWithDefault(100f);
            maleForce.amplitude.delta.SetWithDefault(50f);
            maleForce.offsetTarget = 0f;
            maleForce.enabledJ.val = false;
            
            // radialForce = gameObject.AddComponent<Force>().Init(name+":Radial Force", handRB, () => handRB.transform.right);
            // radialForce.constant.val = true;
            // radialForce.amplitude.delta.SetWithDefault(100f);
            // radialForce.amplitude.sharpness.SetWithDefault(2f);
            // radialForce.enabled = false;
            
            rollTorque = gameObject.AddComponent<Torque>().Init(name+":Roll Torque", handRB, () => handRB.transform.forward);
            rollTorque.constant.SetWithDefault(true);
            rollTorque.amplitude.delta.SetWithDefault(100f);
            rollTorque.amplitude.sharpness.SetWithDefault(2f);
            rollTorque.amplitude.transitionQuicknessMean.SetWithDefault(.5f);
            rollTorque.amplitude.transitionQuicknessDelta.SetWithDefault(.25f);
            rollTorque.enabled = false;
            rollTorque.sync = new ForceSync(rollTorque, thrustForce);
            
            pitchTorque = gameObject.AddComponent<Torque>().Init(name+":Pitch Torque", handRB, () => handRB.transform.up);
            pitchTorque.constant.SetWithDefault(true);
            pitchTorque.amplitude.delta.SetWithDefault(100f);
            pitchTorque.amplitude.sharpness.SetWithDefault(2f);
            pitchTorque.amplitude.transitionQuicknessMean.SetWithDefault(.5f);
            pitchTorque.amplitude.transitionQuicknessDelta.SetWithDefault(.25f);
            pitchTorque.enabled = false;
            pitchTorque.sync = new ForceSync(pitchTorque, thrustForce);

            // thrustForce.enabledJ.AddCallback(val =>
            // {
            //     // radialForce.SetActive(val && enabled);
            //     rollTorque.SetActive(val && enabled);
            //     pitchTorque.SetActive(val && enabled);
            // });
            rollTorque.enabledJ.setCallbackFunction += val =>
            {
                rollTorque.SetActive(val && enabled);
            };
            pitchTorque.enabledJ.setCallbackFunction += val =>
            {
                pitchTorque.SetActive(val && enabled);
            };
            // radialForce.enabledJ.setCallbackFunction += val =>
            // {
            //     radialForce.SetActive(val && enabled);
            // };
            
            penetrationSoundsVolume.AddCallback(val => audioSource.volume = val);

            // grab.setCallbackFunction += SetFingerRotations;
            shrinkColliders.setCallbackFunction += val => penetrator?.stimReceiver?.ShrinkColliders(val);
            grabScale.setCallbackFunction += val => SetFingerTargets();
            fingerGrabScale.setCallbackFunction += val => SetFingerTargets();

            sensitivity.SetWithDefault(.01f);
            xrayEnabled.SetWithDefault(false);
            xrayAlpha.SetWithDefault("full");
            
            InitForcePresets();
            enabled = false;
            initialized = true;
            // StiffenFingers();
            // GetAngle(thumb[1],1).Print();
            // FillMeUp.atom.GetComponentsInChildren<DAZBone>().ToList().ForEach(x =>
            // {
            //     if (x.name.ToLower().Contains("tongue")) x.name.Print();
            // });
            // FillMeUp.atom.GetComponentsInChildren<DAZBone>().First(x => x.name == "rIndex1");
            // triggerGO.transform.parent.Draw();
            // enterPointTF.transform.Draw();
            
            return this;
        }

        private static Quaternion target = Quaternion.Euler(0f, 0f, 30f);

        public void OnDisable()
        {
            thrustForce.ShutDownImmediate();
            maleForce.ShutDownImmediate();
            // quicknessOut = Random.Range(1f, 6f);
            // ResetFingerTargets(quicknessOut);
            ResetFingers();
            // if(!initialized) return;
            isClose = false;
            isGrabbing = false;
            info.val = "";
            zeroSpeed = ZeroSpeed().Start();
            zeroDepth = ZeroDepth().Start();
            zeroForce = ZeroForce().Start();
            pitchTorque.enabled = false;
            rollTorque.enabled = false;
            StimReceiver receiver;
            if (penetrator != null)
            {
                if (FillMeUp.stimReceivers.TryGetValue(penetrator, out receiver)) receiver.SetFucking(null);
                penetrator.stimReceiver.ShrinkColliders(1f);
                if (penetrator.type == 1)
                {
                    var person = penetrator.stimReceiver as Person;
                    person.xrayClient?.ShutDown();
                }
            }
            penetrator = null;
            depthUnsmoothed = 0f;
            isPenetrated = false;
            if(!string.IsNullOrEmpty(cachedHandControlMode)) handControl.fingerControlModeJSON.val = cachedHandControlMode;
        }

        private void OnDestroy()
        {
            Destroy(triggerGO);
            Destroy(depthReference);
            zeroDepth.Stop();
            zeroSpeed.Stop();
            zeroForce.Stop();
        }

        public void OnEnable()
        {
            if (!initialized) return;
            foreskiTimer = .5f;
            zeroSpeed.Stop();
            zeroDepth.Stop();
            zeroForce.Stop();
            if (penetrator.type > 0)
            {
                maleForce.rb = penetrator.root;
            }
            penetratorForce = Vector3.zero;
            snapInFactor = handjobTrigger.closestCollider == penetrator.tipCollider ? 200f : 0f;
            isPenetrated = true;
            SetFingerTargets();
        }

        private float relativeDistance;
        // private bool hasForeski;

        public override void SetPenetrator(CapsulePenetrator newPenetrator)
        {
            base.SetPenetrator(newPenetrator);
            penetrator = newPenetrator;
            penetrator.rootTransform = penetrator.rootTransform;
            penetrator.SetTipAndWith();
            // widthToBendScale = .486f / penetrator.width;
            widthToBendScale = .5f / penetrator.width;
            info.val = $"Stroking <b>{penetrator.atom.name}</b>";
            
            if (Vector3.Dot(thrustDirection, handRB.transform.forward) > 0f)
            {
                snapTorqueSign = -sideId;
                strokeDirection = -1f;
            }
            else
            {
                snapTorqueSign = sideId;
                strokeDirection = 1f;
            }

            fingersClosed = false;

            penetratorRadius = penetrator.width / 18f;
            // penetratorRadius.Print();
            // fingerTarget = new Vector3(0f, 0f, sideId*(45f - 2500f * (penetratorRadius - .02f)));
            // thumbTargets[0] *= sideId*(120f - 9000f * (penetratorRadius - .02f));
            // GetInitialRotations();
            penetrator.stimReceiver?.ShrinkColliders(shrinkColliders.val);
            cachedHandControlMode = handControl.fingerControlModeJSON.val;
            handControl.fingerControlModeJSON.val = "JSONParams";
            // handControl.handGraspStrengthJSON.val = .5f;
        }

        public override void ResetPenetration()
        {
            maleForce.ShutDownImmediate();
            handjobTrigger.Reset();
        }

        private void SetFingerTargets()
        {
            if(!isPenetrated) return;
            var widthScale = grabScale.val * Mathf.Lerp(1.4f, 1f, 100f*(penetratorRadius - .02f));
            // $"{penetratorRadius} : {thumbScale}".Print();
            thumb.target = baseThumbTarget.Multiply(1f, 1f, 1f*widthScale);
            // thumb.target.Print();
            for (int i = 0; i < 4; i++)
            {
                fingers[i].target = baseFingerTargets[i].MultiplyFirst3(widthScale * fingerGrabScale.val[i]);
            }
        }

        public void ResetFingerTargets(float quicknessOut)
        {
            for (int i = 0; i < 4; i++)
            {
                fingers[i].target = Vector5.zero;
                fingers[i].quicknessOut = quicknessOut;
            }

            thumb.target = Vector5.zero;
            thumb.quicknessOut = quicknessOut;
        }
        
        public void ResetFingers()
        {
            for (int i = 0; i < 4; i++)
            {
                fingers[i].Reset();
            }
            thumb.Reset();
        }

        private float GetAngle(Transform t, int axis = 2)
        {
            if (t.localEulerAngles[axis] > 180f) return t.localEulerAngles[axis] - 360f;
            return t.localEulerAngles[axis];
        }

        private float foreskiTimer;
        private bool isClose;
        private bool isGrabbing;

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            closestCollider = handjobTrigger.closestCollider;
            penetratorForward = penetrator.forwardByCollider[closestCollider]();
            fwrdCross = Vector3.Cross(penetratorForward, distReference.transform.position - closestCollider.transform.position);
            radialInDirection = Vector3.Cross(penetratorForward, fwrdCross).normalized;
            depthUnsmoothed = GetDepth(penetrator.tip);
            depth.val = Mathf.Lerp(depth.val, depthUnsmoothed, 10f * Time.fixedDeltaTime);
            if (isClose)
            {
                GetSpeed();
            }
            // else depth.val = 0f;
            
            if (Mathf.Abs(speed.val) > .1f && !audioSource.isPlaying)
            {
                audioSource.volume = Mathf.Abs(speed.val) * depth.val * depth.val * penetrationSoundsVolume.val;
                FillMeUp.squishLibrary.Play(audioSource);
            }
            var radialDistance = GetRadialDistance();
            // var grabScale = Mathf.Clamp(1f - radialDistance * 20f, 0f, 1f);
            var distanceFactor = Mathf.Lerp(1f, 0f, radialDistance * 50f);
            // radialDistance.Print();
            
            // for (int i = 0; i < 12; i++)
            // {
            //     fingers[i].target = Quaternion.LerpUnclamped(Quaternion.identity, fingerTargets[i], grabScale.val * distanceFactor);
            // }
            
            // var grab = grabScale.val * distanceFactor * fingerTarget;
            // for (int i = 0; i < grabbingFingers.val; i++)
            // {
            //     for (int j = 0; j < 2; j++)
            //     {
            //         if(i == 1 || i == 2) fingers[i][j].target = 1.3f*grab;
            //         else fingers[i][j].target = grab;
            //     }
            //     fingers[i][2].target = 1f*grab;
            // }
            //
            // if ((int)grabbingFingers.val == 3)
            // {
            //     for (int j = 0; j < 3; j++)
            //     {
            //         fingers[3][j].target = .3f*grab;
            //     }
            // }
            // else if(grabbingFingers.val < 3f)
            // {
            //     float x = 1f;
            //     for (int i = (int)grabbingFingers.val; i < 4; i++)
            //     {
            //         x -= .35f;
            //         if (i == 3) x -= .2f;
            //         for (int j = 0; j < 3; j++)
            //         {
            //             fingers[i][j].target = x * grab;
            //         }
            //     }
            // }
            
            if (!isClose && radialDistance < .035f)
            {
                isClose = true;
                // thumbOut.target = 0f;
            }
            // radialDistance.Print();
            if (!isGrabbing)
            {
                if (magnetic.val) ApplyForce(true);
                if (isClose)
                {
                    if (fingers[0].rotation.x > .75f * fingers[0].target.x)
                    {
                        isGrabbing = true;
                        // var scale = widthToBendScale ;
                        // thumb[0].target = scale*.58f - sideId * .01f * thumbAngles[0];
                        // thumb[1].target = scale*.2f - sideId * .01f * thumbAngles[1];
                        // thumb[2].target = scale*.4f + sideId * .01f * thumbAngles[2];
                        if (thrustForce.enabledJ.val) thrustForce.Enable();
                        if(rollTorque.enabledJ.val) rollTorque.Enable();
                        if(pitchTorque.enabledJ.val) pitchTorque.Enable();
                        if(maleForce.enabledJ.val) maleForce.Enable();
                    }
                }
            }
            else if (magnetic.val)
            {
                ApplyForce(false);
                if(foreski != null && Person.foreskiEnabled.val)
                {
                    if (foreskiTimer > 0f) foreskiTimer -= Time.fixedDeltaTime;
                    else
                    {
                        var delta = depthUnsmoothed - lastDepthUnsmoothed;
                        if (delta > 0f) delta *= 1.1f;
                        foreski.morphValue -= 5f * depth.val * delta;
                    }
                }
                if(randomizeFingers.val)
                {
                    fingerRNGTimer -= Time.fixedDeltaTime;
                    if (fingerRNGTimer < 0f)
                    {
                        fingerRNGTimer = 10f;
                        var rng = Random.Range(.5f, .85f);
                        var full = Random.Range((int)minFingersGrabbing.val-1, minFingersGrabbing.val<=2? 5:4);
                        Vector4 targets = Vector4.one;
                        if (full < 4)
                        {
                            for (int i = full + 1; i < 4; i++)
                            {
                                targets[i] = 1f - rng * (i - full) / (3f - full);
                            }
                        }
                        else targets = new Vector4(Random.Range(.2f, 1 - rng), 1f, 1f, Random.Range(.2f, 1 - rng));

                        fingerGrabScale.val = targets;
                    }
                }
                // thumb[0].target = grab;
                // var thumbBend = grabScale.val * distanceFactor * thumbTarget;
                // Vector3 thumbGrab = new Vector3(40f, -.1f*thumbBend, 0f);
                // for (int j = 0; j < 3; j++)
                // {
                //     thumb[j].target = Quaternion.LerpUnclamped(Quaternion.identity, thumbTargets[j], grabScale.val * distanceFactor);
                // }
            }

            if(sensitivity.val > 0f) ReadMyLips.Stimulate(50f * speed.val * sensitivity.val, doStim: true, doPlease: true);
            lastDepth = depth.val;
            lastDepthUnsmoothed = depthUnsmoothed;
            
        }

        private float fingerRNGTimer;

        protected float GetDepth(Transform point)
        {
            var dist = strokeDirection * Vector3.Dot(point.position - depthReference.transform.position,
                depthReference.transform.forward);
            dist /= penetrator.length;
            if (preventPullout.val && dist < .125f)
            {
                dist = .125f;
                thrustForce.ForceInwards();
                maleForce.ForceInwards();
                // thrustForce.UpdateDynamicScale(.5f);
                // maleForce.UpdateDynamicScale(.1f);
            }
            // else thrustForce.scale = 1f;
            dist = 1.1f*(.1f - dist) + 1f;
            return dist;
        }

        private float GetRadialDistance()
        {
            var d = fwrdCross.magnitude;
            d -= penetratorRadius;
            
            if (d < .01f) d = 0f;
            // $"{d} : {penetratorRadius}".Print();
            return d;
        }

        private void GetSpeed()
        {
            // speed.val = Mathf.Lerp(speed.val, (lastDistance - distance.val)/Time.deltaTime, 5f*Time.deltaTime);
            speed.val += 10f * (depth.val - lastDepth - speed.val * Time.fixedDeltaTime);
            // speed.val.Print();
        }

        // private void SetForeskiOnExit()
        // {
        //     if(penetrator.foreski.morphValue < .3f) 
        // }

        private IEnumerator ZeroSpeed()
        {
            var wait = new WaitForFixedUpdate();
            while (Mathf.Abs(speed.val) > .001f)
            {
                speed.val = Mathf.Lerp(speed.val, 0f, 10f * Time.fixedDeltaTime);
                yield return wait;
            }
            speed.val = 0f;
        }

        private IEnumerator ZeroDepth()
        {
            var wait = new WaitForFixedUpdate();
            while (Mathf.Abs(depth.val) > .001f)
            {
                depth.val = Mathf.Lerp(depth.val, 0f, 10f * Time.fixedDeltaTime);
                if(foreski != null && foreski.morphValue > 1f) foreski.morphValue = Mathf.Lerp(foreski.morphValue, 1f, 10f * Time.fixedDeltaTime);
                yield return wait;
            }
    
            depth.val = lastDepth = 0f;
        }

        private IEnumerator ZeroForce()
        {
            if (!magnetic.val) yield break;
            var wait = new WaitForFixedUpdate();
            while (!SuperController.singleton.freezeAnimation && snapForce.sqrMagnitude > .001f)
            {
                snapForce = Vector3.Lerp(snapForce, Vector3.zero, 2f * Time.fixedDeltaTime);
                snapTorque = Vector3.Lerp(snapTorque, Vector3.zero, Time.fixedDeltaTime * 2f);
                handRB.AddForce(snapForce, ForceMode.Force);
                handRB.AddTorque(snapTorque, ForceMode.Force);
                yield return wait;
            }
            snapForce = Vector3.zero;
        }

        public IEnumerator Reset()
        {
            enabled = false;
            yield return new WaitForFixedUpdate();
            if(!enabled)
            {
                for (int i = 0; i < 4; i++)
                {
                    fingers[i].Reset();
                }
                zeroForce.Stop();
                snapForce = snapTorque = Vector3.zero;
            }
        }

        private Vector3 penetratorForce;
        float snapInFactor = 1f;
        private void ApplyForce(bool moveIn)
        {
            // return;
            if(Pose.isApplying || SuperController.singleton.freezeAnimation) return;
            var inForce = -10f * radialInDirection;
            if (moveIn)
            {
                // var inForce = 200f * (distReference.transform.position - closestCollider.transform.position);
                snapForce = Vector3.Lerp(snapForce, (snapInFactor*thrustDirection + inForce), Time.fixedDeltaTime * 5f);
                // snapTorque = Vector3.Lerp(snapTorque, snapTorqueSign * sideId * 20f * Vector3.Cross(handRB.transform.forward, thrustDirection), Time.fixedDeltaTime * 2f);
                snapTorque = Vector3.Lerp(snapTorque, strokeDirection * 400f*Vector3.Cross(thrustDirection, triggerGO.transform.up), Time.fixedDeltaTime * 2f);
                penetratorForce = Vector3.Lerp(penetratorForce,
                    inForce, .5f*Time.fixedDeltaTime);
                for (int i = 1; i < penetrator.rigidbodies.Count; i++)
                {
                    penetrator.rigidbodies[i].AddForce(penetratorForce/(3-i));
                }
            }
            else
            {
                snapForce = Vector3.Lerp(snapForce, (2f*thrustDirection - inForce), Time.fixedDeltaTime);
                snapTorque = strokeDirection * 400f*Vector3.Cross(thrustDirection, triggerGO.transform.up);
                
                for (int i = 1; i < penetrator.rigidbodies.Count; i++)
                {
                    penetrator.rigidbodies[i].AddForce(inForce/(3-i));
                }
            }
            handRB.AddForce(snapForce, ForceMode.Force);
            handRB.AddTorque(snapTorque, ForceMode.Force);
        }

        public override void CreateSettingsUI()
        {
            sensitivity.CreateUI(UIElements, false);
            penetrationSoundsVolume.CreateUI(UIElements, true);
            grabScale.CreateUI(UIElements);
            shrinkColliders.CreateUI(UIElements, true);
            // grabbingFingers.CreateUI(UIElements);
            // grabbingFingers.slider.wholeNumbers = true;
            UIElements.Add(UIManager.CreateV4Slider(fingerGrabScale, FillMeUp.singleton,  constrained: true, suffices:new []{"1", "2","3","4"}));
            randomizeFingers.CreateUI(UIElements);
            minFingersGrabbing.CreateUI(UIElements);
            minFingersGrabbing.slider.wholeNumbers = true;
        }
        
        public override void CreateForcesUI()
        {
            // base.CreateForcesUI();
            thrustForce.enabledJ.CreateUI(UIElements);
            pitchTorque.enabledJ.CreateUI(UIElements);
            rollTorque.enabledJ.CreateUI(UIElements);
            // yawTorque.enabledJ.CreateUI(UIElements);
            maleForce.enabledJ.CreateUI(UIElements);
            magnetic.CreateUI(UIElements);
            FillMeUp.singleton.SetupButton("Configure Thrust", true, () => CreateForceUINewPage(thrustForce), UIElements);
            FillMeUp.singleton.SetupButton("Configure Pitch Torque", true, () => CreateForceUINewPage(pitchTorque), UIElements);
            FillMeUp.singleton.SetupButton("Configure Roll Torque", true, () => CreateForceUINewPage(base.rollTorque), UIElements);
            // FillMeUp.singleton.SetupButton("Configure Yaw Torque", true, () => CreateForceUINewPage(yawTorque), UIElements);
            FillMeUp.singleton.SetupButton("Configure Male Thrust", true, () => CreateForceUINewPage(maleForce), UIElements);
            if (magnet != null)
            {
                FillMeUp.singleton.SetupButton("Configure Magnet", true, magnet.CreateUINewPage, UIElements);
            }
            else
            {
                FillMeUp.singleton.SetupButton("N.A.", true, delegate {  }, UIElements);
            }
            FillMeUp.singleton.SetupButton("Configure Hands Sync Group", true, FillMeUp.handForceGroup.CreateUI, UIElements);
            FillMeUp.handForceGroup.driverInfo.CreateUI(UIElements);

            var info = Utils.SetupInfoTextNoScroll(FillMeUp.singleton, toggleInfoText, 200f, false);
            info.background.offsetMin = new Vector2(0, 0);
            UIElements.Add(info);
            
            preventPullout.CreateUI(UIElements, true);
        }

        public override JSONClass Store(string subScenePrefix, bool storeTriggers = true)
        {
            var jc = base.Store(subScenePrefix, storeTriggers);
            grabScale.Store(jc);
            shrinkColliders.Store(jc);
            fingerGrabScale.Store(jc);
            randomizeFingers.Store(jc);
            minFingersGrabbing.Store(jc);
            return jc;
        }

        public override void Load(JSONClass jc, string subScenePrefix)
        {
            base.Load(jc, subScenePrefix);
            if (jc.HasKey(name))
            {
                JSONClass tc = jc[name].AsObject;
                grabScale.Load(tc);
                shrinkColliders.Load(tc);
                fingerGrabScale.Load(tc);
                randomizeFingers.Load(tc);
                minFingersGrabbing.Load(tc);
            }
        }
        
        public override JSONClass StorePoseSettings(JSONClass parent)
        {
            JSONClass jc = base.StorePoseSettings(parent);
            grabScale.Store(jc,false);
            shrinkColliders.Store(jc, false);
            fingerGrabScale.Store(jc,false);
            randomizeFingers.Store(jc, false);
            minFingersGrabbing.Store(jc, false);
            return jc;
        }

        public override void LoadPoseSettings(JSONClass baseJsonClass)
        {
            base.LoadPoseSettings(baseJsonClass);
            if (!baseJsonClass.HasKey(name))
            {
                grabScale.SetValToDefault();
                shrinkColliders.SetValToDefault();
                fingerGrabScale.SetValToDefault();
                randomizeFingers.SetValToDefault();
                minFingersGrabbing.SetValToDefault();
                return;
            }
            JSONClass jc = baseJsonClass[name].AsObject;
            grabScale.Load(jc, true);
            shrinkColliders.Load(jc,true);
            fingerGrabScale.Load(jc,true);
            randomizeFingers.Load(jc,true);
            minFingersGrabbing.Load(jc,true);
        }
    }
}