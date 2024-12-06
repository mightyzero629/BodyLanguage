using System.Collections;
using System.Linq;
using SimpleJSON;
using UnityEngine;
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
        private LerpingMorph[] thumbMorphs;
        private LerpingMorph[] indexMorphs;
        private LerpingMorph[] midMorphs;
        private LerpingMorph[] ringMorphs;
        private LerpingMorph[] pinkyMorphs;
        private Transform[] thumb;
        private Transform[] index;
        private Transform[] mid;
        private Transform[] ring;
        private Transform[] pinky;
        private LerpingMorph thumbOut;

        private float[] indexTargets = new float [3];
        private float[] midTargets = new float [3];
        private float[] ringTargets = new float [3];
        private float[] pinkyTargets = new float [3];
        private float[] thumbAngles = new float[3];

        // private DAZMorph foreski;

        private float speedIncrement;

        private IEnumerator zeroSpeed;
        private IEnumerator zeroDepth;
        private IEnumerator zeroForce;

        private Vector3 snapForce;
        public Force radialForce;

        private HandJobTrigger handjobTrigger;
        
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
            thumb = FillMeUp.atom.GetComponentsInChildren<DAZBone>().First(x => x.name == $"{side}Thumb1")
                .GetComponentsInChildren<DAZBone>().Select(x => x.transform).ToArray();
            index = FillMeUp.atom.GetComponentsInChildren<DAZBone>().First(x => x.name == $"{side}Index1")
                .GetComponentsInChildren<DAZBone>().Select(x => x.transform).ToArray();
            mid = FillMeUp.atom.GetComponentsInChildren<DAZBone>().First(x => x.name == $"{side}Mid1")
                .GetComponentsInChildren<DAZBone>().Select(x => x.transform).ToArray();
            ring = FillMeUp.atom.GetComponentsInChildren<DAZBone>().First(x => x.name == $"{side}Ring1")
                .GetComponentsInChildren<DAZBone>().Select(x => x.transform).ToArray();
            pinky = FillMeUp.atom.GetComponentsInChildren<DAZBone>().First(x => x.name == $"{side}Pinky1")
                .GetComponentsInChildren<DAZBone>().Select(x => x.transform).ToArray();

            audioSource = BodyRegionMapping.touchZones[$"{side}Palm"].slapAudioSource;

            triggerGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            triggerGO.name = $"BL_{side}HandTrigger";
            enterPointTF = triggerGO.transform;
            enterPointTF.SetParent(index[0].parent, false);
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
            
            // depthReference = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            // depthReference.transform.localScale = .01f * Vector3.one;
            // depthReference.GetComponent<Collider>().enabled = false;
            // depthReference.transform.SetParent(mid[0].parent, false);
            // depthReference.transform.localPosition = new Vector3(sideId * .055f, -.02f, -.01f);

            // depthReference = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            // depthReference.transform.localScale = .01f * Vector3.one;
            // depthReference.GetComponent<Collider>().enabled = false;
            depthReference = new GameObject($"BL_{side}DepthReference");
            depthReference.transform.SetParent(index[0].parent, false);
            depthReference.transform.localPosition = new Vector3(sideId * .065f, -.03f, .01f);

            distReference = new GameObject($"BL_{side}DistReference");
            distReference.transform.SetParent(enterPointTF, false);
            distReference.transform.localPosition = new Vector3(-sideId*0.3f, 0f, 0f);

            thumbMorphs = new[]
            {
                gameObject.AddComponent<LerpingMorph>().Init(FillMeUp.packageUid +
                                                             $"Custom/Atom/Person/Morphs/female/CheesyFX/BodyLanguage/Hands/Thumb/BL-Finger_{side}Thumb1.vmi"),
                gameObject.AddComponent<LerpingMorph>().Init(FillMeUp.packageUid +
                                                             $"Custom/Atom/Person/Morphs/female/CheesyFX/BodyLanguage/Hands/Thumb/BL-Finger_{side}Thumb2.vmi"),
                gameObject.AddComponent<LerpingMorph>().Init(FillMeUp.packageUid +
                                                             $"Custom/Atom/Person/Morphs/female/CheesyFX/BodyLanguage/Hands/Thumb/BL-Finger_{side}Thumb3.vmi")
            };
            indexMorphs = new[]
            {
                gameObject.AddComponent<LerpingMorph>().Init(FillMeUp.packageUid +
                                                             $"Custom/Atom/Person/Morphs/female/CheesyFX/BodyLanguage/Hands/Index/BL-Finger_{side}Index1.vmi"),
                gameObject.AddComponent<LerpingMorph>().Init(FillMeUp.packageUid +
                                                             $"Custom/Atom/Person/Morphs/female/CheesyFX/BodyLanguage/Hands/Index/BL-Finger_{side}Index2.vmi"),
                gameObject.AddComponent<LerpingMorph>().Init(FillMeUp.packageUid +
                                                             $"Custom/Atom/Person/Morphs/female/CheesyFX/BodyLanguage/Hands/Index/BL-Finger_{side}Index3.vmi")
            };
            midMorphs = new[]
            {
                gameObject.AddComponent<LerpingMorph>().Init(FillMeUp.packageUid +
                                                             $"Custom/Atom/Person/Morphs/female/CheesyFX/BodyLanguage/Hands/Mid/BL-Finger_{side}Mid1.vmi"),
                gameObject.AddComponent<LerpingMorph>().Init(FillMeUp.packageUid +
                                                             $"Custom/Atom/Person/Morphs/female/CheesyFX/BodyLanguage/Hands/Mid/BL-Finger_{side}Mid2.vmi"),
                gameObject.AddComponent<LerpingMorph>().Init(FillMeUp.packageUid +
                                                             $"Custom/Atom/Person/Morphs/female/CheesyFX/BodyLanguage/Hands/Mid/BL-Finger_{side}Mid3.vmi")
            };
            ringMorphs = new[]
            {
                gameObject.AddComponent<LerpingMorph>().Init(FillMeUp.packageUid +
                                                             $"Custom/Atom/Person/Morphs/female/CheesyFX/BodyLanguage/Hands/Ring/BL-Finger_{side}Ring1.vmi"),
                gameObject.AddComponent<LerpingMorph>().Init(FillMeUp.packageUid +
                                                             $"Custom/Atom/Person/Morphs/female/CheesyFX/BodyLanguage/Hands/Ring/BL-Finger_{side}Ring2.vmi"),
                gameObject.AddComponent<LerpingMorph>().Init(FillMeUp.packageUid +
                                                             $"Custom/Atom/Person/Morphs/female/CheesyFX/BodyLanguage/Hands/Ring/BL-Finger_{side}Ring3.vmi")
            };
            pinkyMorphs = new[]
            {
                gameObject.AddComponent<LerpingMorph>().Init(FillMeUp.packageUid +
                                                             $"Custom/Atom/Person/Morphs/female/CheesyFX/BodyLanguage/Hands/Pinky/BL-Finger_{side}Pinky1.vmi"),
                gameObject.AddComponent<LerpingMorph>().Init(FillMeUp.packageUid +
                                                             $"Custom/Atom/Person/Morphs/female/CheesyFX/BodyLanguage/Hands/Pinky/BL-Finger_{side}Pinky2.vmi"),
                gameObject.AddComponent<LerpingMorph>().Init(FillMeUp.packageUid +
                                                             $"Custom/Atom/Person/Morphs/female/CheesyFX/BodyLanguage/Hands/Pinky/BL-Finger_{side}Pinky3.vmi")
            };
            // Custom/Atom/Person/Morphs/female/CheesyFX/BodyLanguage/Hands/Thumb/BL_lThumbOut.vmi
            // Custom/Atom/Person/Morphs/female/CheesyFX/BodyLanguage/Hands/Thumb/BL_rThumbOut.vmi
            thumbOut = gameObject.AddComponent<LerpingMorph>().Init(FillMeUp.packageUid +
                                                                    $"Custom/Atom/Person/Morphs/female/CheesyFX/BodyLanguage/Hands/Thumb/BL-Finger_{side}ThumbOut.vmi");
            thumbOut.quicknessIn = 5f;
            
            thrustForce.amplitude.mean.SetWithDefault(150f);
            thrustForce.amplitude.delta.SetWithDefault(100f);
            thrustForce.period.sharpness.SetWithDefault(2f);

            maleForce.amplitude.mean.SetWithDefault(100f);
            maleForce.amplitude.delta.SetWithDefault(50f);
            maleForce.offsetTarget = 0f;
            maleForce.enabledJ.val = false;
            
            radialForce = gameObject.AddComponent<Force>().Init(name+":RadialForce", handRB, () => handRB.transform.right);
            radialForce.constant.val = true;
            radialForce.amplitude.delta.SetWithDefault(100f);
            radialForce.amplitude.sharpness.SetWithDefault(2f);
            radialForce.enabled = false;

            thrustForce.enabledJ.AddCallback(val =>
            {
                radialForce.SetActive(val && enabled);
            });
            penetrationSoundsVolume.AddCallback(val => audioSource.volume = val);

            sensitivity.SetWithDefault(0f);
            
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

        private void StiffenFingers()
        {

            var cj = pinky[2].GetComponent<ConfigurableJoint>();

            var limit = cj.angularYLimit;
            limit.limit = .001f; //3
            cj.angularYLimit = limit;
            limit = cj.angularZLimit;
            limit.limit = .001f; //3
            cj.angularYLimit = limit;
            // drive = cj.angularXDrive;
            // drive.positionSpring = 
            // drive.positionSpring.Print();
        }
        
        public void OnDisable()
        {
            thrustForce.ShutDownImmediate();
            maleForce.ShutDownImmediate();
            quicknessOut = Random.Range(1f, 6f);
            ResetFingerTargets(quicknessOut);
            // if(!initialized) return;
            isClose = false;
            isGrabbing = false;
            thumbOut.target = 0f;
            speedIncrement = 0f;
            info.val = "";
            zeroSpeed = ZeroSpeed().Start();
            zeroDepth = ZeroDepth().Start();
            zeroForce = ZeroForce().Start();
            radialForce.enabled = false;
            StimReceiver receiver;
            if (penetrator != null && FillMeUp.stimReceivers.TryGetValue(penetrator, out receiver)) receiver.SetFucking(null);
            penetrator = null;
            depthUnsmoothed = 0f;
            isPenetrated = false;
        }

        private void OnDestroy()
        {
            Destroy(triggerGO);
            Destroy(depthReference);
            zeroDepth.Stop();
            zeroSpeed.Stop();
            zeroForce.Stop();
            // ResetFingers();
            // for (int i = 0; i < 3; i++)
            // {
            //     indexMorphs[i].morphVal = 0f;
            // }
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
            for (int i = 0; i < 3; i++)
            {
                // thumbMorphs[i].quicknessIn = indexMorphs[i].quicknessIn = midMorphs[i].quicknessIn =
                //     ringMorphs[i].quicknessIn = pinkyMorphs[i].quicknessIn = 0f;
                // indexMorphs[i].target = widthToBendScale*.3f + .15f * 1f + indexMorphs[i].morphVal + sideId * .01f * GetAngle(index[i]);
                // midMorphs[i].target = widthToBendScale*.35f + .15f * 1f + midMorphs[i].morphVal + sideId * .01f * GetAngle(mid[i]);
                // ringMorphs[i].target = widthToBendScale*.35f + .15f * 1f + ringMorphs[i].morphVal + sideId * .01f * GetAngle(ring[i]);
                // pinkyMorphs[i].target = widthToBendScale*.3f + .15f * 1f + pinkyMorphs[i].morphVal + sideId * .01f * GetAngle(pinky[i]);
                indexMorphs[i].target = -.3f;
                midMorphs[i].target = -.3f;
                ringMorphs[i].target = -.3f;
                pinkyMorphs[i].target = -.3f;
                thumbAngles[i] = GetAngle(thumb[i]);
            }
            for (int i = 0; i < 3; i++)
            {
                indexTargets[i] = widthToBendScale*.3f + .15f + indexMorphs[i].morphVal + sideId * .01f * GetAngle(index[i]);
                midTargets[i] = widthToBendScale*.35f + .15f + midMorphs[i].morphVal + sideId * .01f * GetAngle(mid[i]);
                ringTargets[i] = widthToBendScale*.35f + .15f + ringMorphs[i].morphVal + sideId * .01f * GetAngle(ring[i]);
                pinkyTargets[i] = widthToBendScale*.3f + .15f + pinkyMorphs[i].morphVal + sideId * .01f * GetAngle(pinky[i]);
            }

            thumbOut.target = 2f;
            
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
            // thumbOut.target = 2f;
            // quicknessIn = 0f;
            // foreski = penetrator.type == 1 ? penetrator.stimReceiver.foreski : null;

            penetratorRadius = penetrator.width / 18f;
        }

        public override void ResetPenetration()
        {
            maleForce.ShutDownImmediate();
            handjobTrigger.Reset();
        }

        public void ResetFingerTargets(float quicknessOut)
        {
            for (int i = 0; i < 3; i++)
            {
                thumbMorphs[i].target = indexMorphs[i].target =
                    midMorphs[i].target = ringMorphs[i].target = pinkyMorphs[i].target = 0f;
                thumbMorphs[i].quicknessOut = indexMorphs[i].quicknessOut = midMorphs[i].quicknessOut =
                    ringMorphs[i].quicknessOut = pinkyMorphs[i].quicknessOut = quicknessOut;
            }
        }
        
        public void ResetFingers()
        {
            for (int i = 0; i < 3; i++)
            {
                thumbMorphs[i].morphVal = indexMorphs[i].morphVal =
                    midMorphs[i].morphVal = ringMorphs[i].morphVal = pinkyMorphs[i].morphVal = 0f;
            }
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
            var grabScale = Mathf.Clamp(1f - radialDistance * 20f, 0f, 1f);
            // grabScale.Print();
            for (int i = 0; i < 3; i++)
            {
                indexMorphs[i].target = indexTargets[i] * grabScale;
                midMorphs[i].target = midTargets[i] * grabScale;
                ringMorphs[i].target = ringTargets[i] * grabScale;
                pinkyMorphs[i].target = pinkyTargets[i] * grabScale;
            }
            if (!isClose && radialDistance < .02f)
            {
                isClose = true;
                thumbOut.target = 0f;
            }
            // isGrabbing.Print();
            if (!isGrabbing)
            {
                if (magnetic.val) ApplyForce(true);
                if (isClose)
                {
                    if (indexMorphs[0].morphVal / indexMorphs[0].target > .75f)
                    {
                        isGrabbing = true;
                        var scale = widthToBendScale ;
                        thumbMorphs[0].target = scale*.58f - sideId * .01f * thumbAngles[0];
                        thumbMorphs[1].target = scale*.2f - sideId * .01f * thumbAngles[1];
                        thumbMorphs[2].target = scale*.4f + sideId * .01f * thumbAngles[2];
                        if (thrustForce.enabledJ.val)
                        {
                            thrustForce.Enable();
                            radialForce.enabled = true;
                        }
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
            }

            if(sensitivity.val > 0f) ReadMyLips.Stimulate(50f * speed.val * sensitivity.val, doStim: true, doPlease: true);
            lastDepth = depth.val;
            lastDepthUnsmoothed = depthUnsmoothed;
            
        }

        protected float GetDepth(Transform point)
        {
            var dist = strokeDirection * Vector3.Dot(point.position - depthReference.transform.position,
                depthReference.transform.forward);
            dist /= penetrator.length;
            if (preventPullout.val && dist < .1f)
            {
                dist = .1f;
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
            if (d < 0f) d = 0f;
            // d.Print();
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
                foreach (var lerpingMorph in indexMorphs)
                {
                    lerpingMorph.Reset();
                }

                foreach (var lerpingMorph in midMorphs)
                {
                    lerpingMorph.Reset();
                }

                foreach (var lerpingMorph in ringMorphs)
                {
                    lerpingMorph.Reset();
                }

                foreach (var lerpingMorph in pinkyMorphs)
                {
                    lerpingMorph.Reset();
                }

                foreach (var lerpingMorph in thumbMorphs)
                {
                    lerpingMorph.Reset();
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
            if(SuperController.singleton.freezeAnimation) return;
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
        }
    }
}