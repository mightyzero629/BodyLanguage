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
        private LerpingBone[] thumb;
        private LerpingBone[] index;
        private LerpingBone[] mid;
        private LerpingBone[] ring;
        private LerpingBone[] pinky;
        private LerpingBone[] fingers;
        private Vector3 fingerTarget = new Vector3(0f, 0f, 30f);
        // private float thumbTarget = 30f;
        private Quaternion[] baseThumbTargets =
        {
            Quaternion.Euler(new Vector3(32f, -35f, -51f)), 
            Quaternion.Euler(new Vector3(2f, 47f, 0f)),
            Quaternion.Euler(new Vector3(-7f, 24f, 4f))
        };
        // private Quaternion[] baseThumbTargets =
        // {
        //     Quaternion.Euler(new Vector3(30f, 20f, -30)), 
        //     Quaternion.Euler(new Vector3(30f, 20f, 0f)),
        //     Quaternion.Euler(new Vector3(0f, 30f, 0f))
        // };
        // GetInitialRotations
        // 0: 357.8869 304.4893 338.9801
        //
        //
        // 1: 332.1729 27.21237 0.2309632
        //
        //
        // 2: 352.9247 354.3335 3.562391

        // private Quaternion[] baseFingerTargets =
        // {
        //         Quaternion.Euler(new Vector3(4f, -5f, 44f)),
        //         Quaternion.Euler(new Vector3(0f, 2f, 40f)),
        //         Quaternion.Euler(new Vector3(0f, 0f, 43f))
        //     ,
        //     
        //         Quaternion.Euler(new Vector3(0f, 4f, 46f)),
        //         Quaternion.Euler(new Vector3(-2f, 0f, 37f)),
        //         Quaternion.Euler(new Vector3(2f, 2f, 35f))
        //     ,
        //     
        //         Quaternion.Euler(new Vector3(5f, -2f, 46f)),
        //         Quaternion.Euler(new Vector3(0f, -2f, 40f)),
        //         Quaternion.Euler(new Vector3(-5f, 0f, 35f))
        //     ,
        //     
        //         Quaternion.Euler(new Vector3(0f, 0f, 39f)),
        //         Quaternion.Euler(new Vector3(0f, -4f, 30f)),
        //         Quaternion.Euler(new Vector3(0f, 0f, 40f))
        // };
        private Quaternion[] baseFingerTargets =
        {
            Quaternion.Euler(new Vector3(0f, 0f, 44f)),
            Quaternion.Euler(new Vector3(0f, 0f, 40f)),
            Quaternion.Euler(new Vector3(0f, 0f, 43f))
            ,
            
            Quaternion.Euler(new Vector3(0f, 0f, 46f)),
            Quaternion.Euler(new Vector3(0f, 0f, 37f)),
            Quaternion.Euler(new Vector3(0f, 0f, 35f))
            ,
            
            Quaternion.Euler(new Vector3(0f, 0f, 46f)),
            Quaternion.Euler(new Vector3(0f, 0f, 40f)),
            Quaternion.Euler(new Vector3(0f, 0f, 35f))
            ,
            
            Quaternion.Euler(new Vector3(0f, 0f, 39f)),
            Quaternion.Euler(new Vector3(0f, 0f, 30f)),
            Quaternion.Euler(new Vector3(0f, 0f, 40f))
        };
        // GetFingerTargets (base z=30)
        // 00: 14.32164
        // 01: 10.42912
        // 02: 13.49801
        // 10: 16.6727
        // 11: 7.051165
        // 12: 16.31274
        // 20: 16.21326
        // 21: 10.45886
        // 22: 12.40774
        // 30: 19.35728
        // 31: 359.281
        // 32: 21.31281

        // private DAZMorph foreski;

        private float speedIncrement;

        private IEnumerator zeroSpeed;
        private IEnumerator zeroDepth;
        private IEnumerator zeroForce;

        private Vector3 snapForce;
        public Force radialForce;

        private HandJobTrigger handjobTrigger;

        private JSONStorableFloat grabScale = new JSONStorableFloat("Grab Scale", 1f, 0f, 2f);
        private JSONStorableFloat shrinkColliders = new JSONStorableFloat("Shrink Colliders", .9f, 0f, 1f, true);
        private JSONStorableFloat grabbingFingers = new JSONStorableFloat("Grabbing Fingers", 4f, 1f, 4f, true);

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

            fingerTarget *= sideId;
            depth.max = 1f;
            // depthTriggers = FillMeUp.singleton.AddFloatTriggerManager(depth, false, 0f, 0f, .25f);
            // speedTriggers = FillMeUp.singleton.AddFloatTriggerManager(speed, true, 0f, 0f, .5f);
            handRB = FillMeUp.atom.rigidbodies.FirstOrDefault(x => x.name == $"{side}Hand");
            thumb = FillMeUp.atom.GetComponentsInChildren<DAZBone>().First(x => x.name == $"{side}Thumb1")
                .GetComponentsInChildren<DAZBone>().Select(x => gameObject.AddComponent<LerpingBone>().Init(x)).ToArray();
            index = FillMeUp.atom.GetComponentsInChildren<DAZBone>().First(x => x.name == $"{side}Index1")
                .GetComponentsInChildren<DAZBone>().Select(x => gameObject.AddComponent<LerpingBone>().Init(x)).ToArray();
            mid = FillMeUp.atom.GetComponentsInChildren<DAZBone>().First(x => x.name == $"{side}Mid1")
                .GetComponentsInChildren<DAZBone>().Select(x => gameObject.AddComponent<LerpingBone>().Init(x)).ToArray();
            ring = FillMeUp.atom.GetComponentsInChildren<DAZBone>().First(x => x.name == $"{side}Ring1")
                .GetComponentsInChildren<DAZBone>().Select(x => gameObject.AddComponent<LerpingBone>().Init(x)).ToArray();
            pinky = FillMeUp.atom.GetComponentsInChildren<DAZBone>().First(x => x.name == $"{side}Pinky1")
                .GetComponentsInChildren<DAZBone>().Select(x => gameObject.AddComponent<LerpingBone>().Init(x)).ToArray();
            if (sideId > 0f)
            {
                for (int i = 0; i < 3; i++)
                {
                    // baseThumbTargets[i].x *= -1f;
                    baseThumbTargets[i].y *= -1f;
                    baseThumbTargets[i].z *= -1f;
                }
            }
            else
            {
                baseFingerTargets = new[]
                {
                    Quaternion.Euler(new Vector3(0f, 0f, -44f)),
                    Quaternion.Euler(new Vector3(0f, 0f, -40f)),
                    Quaternion.Euler(new Vector3(0f, 0f, -43f)),

                    Quaternion.Euler(new Vector3(0f, 0f, -46f)),
                    Quaternion.Euler(new Vector3(0f, 0f, -37f)),
                    Quaternion.Euler(new Vector3(0f, 0f, -35f)),

                    Quaternion.Euler(new Vector3(0f, 0f, -46f)),
                    Quaternion.Euler(new Vector3(0f, 0f, -40f)),
                    Quaternion.Euler(new Vector3(0f, 0f, -35f)),

                    Quaternion.Euler(new Vector3(0f, 0f, -39f)),
                    Quaternion.Euler(new Vector3(0f, 0f, -30f)),
                    Quaternion.Euler(new Vector3(0f, 0f, -40f))
                };
            }
            // thumb.ToList().ForEach(x => x.dazBone.transform.Draw());
            fingers = new []
            {
                index[0], index[1], index[2],
                mid[0], mid[1], mid[2],
                ring[0], ring[1], ring[2],
                pinky[0], pinky[1], pinky[2]
            };
                
            audioSource = BodyRegionMapping.touchZones[$"{side}Palm"].slapAudioSource;

            triggerGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            triggerGO.name = $"BL_{side}HandTrigger";
            enterPointTF = triggerGO.transform;
            enterPointTF.SetParent(index[0].dazBone.transform.parent, false);
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
            depthReference.transform.SetParent(index[0].dazBone.transform.parent, false);
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

            // grab.setCallbackFunction += SetFingerRotations;
            shrinkColliders.setCallbackFunction += val => penetrator?.stimReceiver?.ShrinkColliders(val);

            sensitivity.SetWithDefault(.01f);
            
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
        
        private void SetFingerRotations(float val)
        {
            for (int i = 0; i < 3; i++)
            {
                index[i].baseRotation = (Quaternion.Inverse(target)*index[i].dazBone.transform.localRotation).eulerAngles;
                mid[i].baseRotation = (Quaternion.Inverse(mid[i].dazBone.transform.localRotation)).eulerAngles;
                ring[i].baseRotation = (Quaternion.Inverse(ring[i].dazBone.transform.localRotation) * target).eulerAngles;
                pinky[i].baseRotation = (Quaternion.Inverse(pinky[i].dazBone.transform.localRotation) * target).eulerAngles;
            }
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
            speedIncrement = 0f;
            info.val = "";
            zeroSpeed = ZeroSpeed().Start();
            zeroDepth = ZeroDepth().Start();
            zeroForce = ZeroForce().Start();
            radialForce.enabled = false;
            StimReceiver receiver;
            if (penetrator != null)
            {
                if (FillMeUp.stimReceivers.TryGetValue(penetrator, out receiver)) receiver.SetFucking(null);
                penetrator.stimReceiver.ShrinkColliders(1f);
            }
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
            GetInitialRotations();
            var thumbScale = Mathf.Lerp(1f, .5f, 100f*(penetratorRadius - .02f));
            for (int i = 0; i < 3; i++)
            {
                thumbTargets[i] = Quaternion.LerpUnclamped(Quaternion.identity, baseThumbTargets[i], thumbScale);
            }
            penetrator.stimReceiver?.ShrinkColliders(shrinkColliders.val);
            for (int i = 0; i < 12; i++)
            {
                fingers[i].target = Quaternion.LerpUnclamped(Quaternion.identity, fingerTargets[i], 1f);
            }
            
        }

        public override void ResetPenetration()
        {
            maleForce.ShutDownImmediate();
            handjobTrigger.Reset();
        }

        private Quaternion[] initialFingerRotations = new Quaternion[12];
        private Quaternion[] fingerTargets = new Quaternion[12];
        private Quaternion[] initialThumbRotations = new Quaternion[3];
        private Quaternion[] thumbTargets = new Quaternion[3];

        private void GetInitialRotations()
        {
            "GetInitialRotations".Print();
            var person = FillMeUp.persons.First(x => x.atom == FillMeUp.atom);
            // person.ResetFingerSprings(sideId);
            for (int i = 0; i < 12; i++)
            {
                initialFingerRotations[i] = fingers[i].dazBone.transform.localRotation;
                $"{i}: {initialFingerRotations[i].eulerAngles.z}".Print();
            }

            for (int i = 0; i < 3; i++)
            {
                initialThumbRotations[i] = thumb[i].dazBone.transform.localRotation;
                // $"{i}: {initialThumbRotations[i].eulerAngles.x} {initialThumbRotations[i].eulerAngles.y} {initialThumbRotations[i].eulerAngles.z}".Print();
                // "\n".Print();
            }
            GetFingerTargets();
        }

        private void GetFingerTargets()
        {
            "GetFingerTargets".Print();
            for (int i = 0; i < 12; i++)
            {
                fingerTargets[i] = initialFingerRotations[i] * baseFingerTargets[i];
                $"{i}: {fingerTargets[i].eulerAngles.z}".Print();
            }
            for (int i = 0; i < 3; i++)
            {
                thumbTargets[i] = initialThumbRotations[i] * baseThumbTargets[i];
                // $"{i}: {thumbTargets[i].z}".Print();
            }
        }

        public void ResetFingerTargets(float quicknessOut)
        {
            for (int i = 0; i < 3; i++)
            {
                thumb[i].target = index[i].target = mid[i].target = ring[i].target = pinky[i].target = Quaternion.identity;
                thumb[i].quicknessOut = index[i].quicknessOut = mid[i].quicknessOut =
                    ring[i].quicknessOut = pinky[i].quicknessOut = quicknessOut;
            }
        }
        
        public void ResetFingers()
        {
            for (int i = 0; i < 3; i++)
            {
                thumb[i].baseRotation = index[i].baseRotation =
                    mid[i].baseRotation = ring[i].baseRotation = pinky[i].baseRotation = Vector3.zero;
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
                    if (index[0].baseRotation.z > .75f * index[0].target.z)
                    {
                        isGrabbing = true;
                        // var scale = widthToBendScale ;
                        // thumb[0].target = scale*.58f - sideId * .01f * thumbAngles[0];
                        // thumb[1].target = scale*.2f - sideId * .01f * thumbAngles[1];
                        // thumb[2].target = scale*.4f + sideId * .01f * thumbAngles[2];
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
                // thumb[0].target = grab;
                // var thumbBend = grabScale.val * distanceFactor * thumbTarget;
                // Vector3 thumbGrab = new Vector3(40f, -.1f*thumbBend, 0f);
                for (int j = 0; j < 3; j++)
                {
                    thumb[j].target = Quaternion.LerpUnclamped(Quaternion.identity, thumbTargets[j], grabScale.val * distanceFactor);
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
                foreach (var lerpingMorph in index)
                {
                    lerpingMorph.Reset();
                }

                foreach (var lerpingMorph in mid)
                {
                    lerpingMorph.Reset();
                }

                foreach (var lerpingMorph in ring)
                {
                    lerpingMorph.Reset();
                }

                foreach (var lerpingMorph in pinky)
                {
                    lerpingMorph.Reset();
                }

                foreach (var lerpingMorph in thumb)
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
        }

        public override JSONClass Store(string subScenePrefix, bool storeTriggers = true)
        {
            var jc = base.Store(subScenePrefix, storeTriggers);
            grabScale.Store(jc);
            shrinkColliders.Store(jc);
            grabbingFingers.Store(jc);
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
                grabbingFingers.Load(tc);
            }
        }
        
        public override void StorePoseSettings(JSONClass baseJsonClass)
        {
            JSONClass jc = new JSONClass();
            grabScale.Store(jc,false);
            shrinkColliders.Store(jc, false);
            grabbingFingers.Store(jc,false);
            baseJsonClass[name] = jc;
        }

        public override void LoadPoseSettings(JSONClass baseJsonClass)
        {
            if (!baseJsonClass.HasKey(name))
            {
                grabScale.SetValToDefault();
                shrinkColliders.SetValToDefault();
                grabbingFingers.SetValToDefault();
                return;
            }
            JSONClass jc = baseJsonClass[name].AsObject;
            grabScale.Load(jc, true);
            shrinkColliders.Load(jc,true);
            grabbingFingers.Load(jc,true);
        }
    }
}