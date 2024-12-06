using System;
using System.Collections;
using System.Linq;
using MacGruber;
using SimpleJSON;
using UnityEngine;

namespace CheesyFX
{
    public class Cleavage : Fuckable
    {
        public GameObject triggerGO;
        private TitJobTrigger titJobTrigger;
        private IEnumerator zeroSpeed;
        private IEnumerator zeroDepth;

        // private float lastDepth;
        private float strokeDirection;
        
        public JSONStorableFloat proximityDepth = new JSONStorableFloat("Proximity Depth", 0f, -.1f, .1f, false, true);
        public JSONStorableFloat proximityHeight = new JSONStorableFloat("Proximity Height", 0f, -.1f, .1f, false, true);
        private Vector3 basePosition;

        private bool parentedToChest = true;
        
        public Cleavage Init()
        {
            type = 4;
            base.Init("Cleavage");
            CreateTriggersUI();
            depth.max = 1f;
            triggerGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            triggerGO.name = "BL_CleavageTirigger";
            enterPointTF = triggerGO.transform;
            enterPointTF.SetParent(rb.transform, false);
            enterPointTF.localPosition = new Vector3(0f, .09f, .135f);
            enterPointTF.localScale = new Vector3(.05f, .05f, .05f);
            enterPointTF.localEulerAngles = new Vector3(-20f, 0f, 0f);
            // FillMeUp.atom.rigidbodies.Where(x => x.name.Contains("AutoColliderFemaleAutoColliderschest")).ToList().ForEach(x => x.Print());
            // var parent = FillMeUp.atom.rigidbodies.FirstOrDefault(x => x.name == "AutoColliderFemaleAutoColliderschest3Joint").transform;
            var parent = FillMeUp.atom.GetComponentsInChildren<CapsuleCollider>()
                .FirstOrDefault(x => x.name == "AutoColliderFemaleAutoColliderschest3Joint");
            if (parent != null)
            {
                enterPointTF.parent = parent.transform;
                parentedToChest = false;
            }
            else "Cleavage: AutoColliderFemaleAutoColliderschest3Joint not found. Parented to Chest".Print();
            basePosition = enterPointTF.localPosition;
            proximityTrigger = triggerGO.GetComponent<CapsuleCollider>();
            proximityTrigger.isTrigger = true;
            triggerGO.GetComponent<Renderer>().enabled = false;
            titJobTrigger = triggerGO.AddComponent<TitJobTrigger>();
            titJobTrigger.cleavage = this;
            foreach (var collider in FillMeUp.atom.GetComponentsInChildren<Collider>(true))
            {
                Physics.IgnoreCollision(proximityTrigger, collider);
            }
            foreach (var col in FillMeUp.atom.rigidbodies.First(x => x.name == "Gen1").GetComponentsInChildren<Collider>(true))
            {
                Physics.IgnoreCollision(proximityTrigger, col, false);
            }
            
            audioSource = BodyRegionMapping.touchZones["Chest"].slapAudioSource;

            // thrustForce.applyReturn.SetWithDefault(false);
            thrustForce.amplitude.mean.SetWithDefault(200f);
            thrustForce.amplitude.delta.SetWithDefault(100f);
            thrustForce.period.mean.SetWithDefault(.7f);
            thrustForce.period.delta.SetWithDefault(.3f);
            
            maleForce.amplitude.mean.SetWithDefault(100f);
            maleForce.paramControl.offset.SetWithDefault(100f);
            maleForce.amplitude.delta.SetWithDefault(50f);
            // maleForce.GetDirection = () => thrustDirection + maleThrustDirection;
            
            pitchTorque = gameObject.AddComponent<Torque>().Init(name+":Pitch Torque", rb, () => -rb.transform.right);
            pitchTorque.AddSync(thrustForce);
            pitchTorque.syncToThrust.SetWithDefault(true);
            pitchTorque.amplitude.mean.SetWithDefault(40f);
            pitchTorque.amplitude.delta.SetWithDefault(20f);
            pitchTorque.amplitude.useNormalDistribution.SetWithDefault(false);
            pitchTorque.amplitude.transitionQuicknessMean.SetWithDefault(.5f);
            pitchTorque.sync.phaseOffsetMean.SetWithDefault(1f);
            pitchTorque.sync.phaseOffsetDelta.SetWithDefault(1f);
            
            rollTorque = gameObject.AddComponent<Torque>().Init(name+":Roll Torque", rb, () => rb.transform.forward);
            rollTorque.constant.SetWithDefault(true);
            rollTorque.amplitude.useNormalDistribution.SetWithDefault(false);
            rollTorque.amplitude.randomizeTimeDelta.SetWithDefault(4f);
            rollTorque.amplitude.mean.SetWithDefault(0f);
            rollTorque.amplitude.delta.SetWithDefault(50f);
            
            yawTorque = gameObject.AddComponent<Torque>().Init(name+":Yaw Torque", rb, () => rb.transform.up);
            // yawTorque.control.enabled.SetWithDefault(false);
            // yawTorque.constant.SetWithDefault(true);
            yawTorque.amplitude.onesided.SetWithDefault(true);
            yawTorque.amplitude.mean.SetWithDefault(0f);
            yawTorque.amplitude.delta.SetWithDefault(25f);
            yawTorque.amplitude.useNormalDistribution.SetWithDefault(false);
            
            // thrustForce.enabledJ.setCallbackFunction += val =>
            // {
            //     pitchTorque.SetActive(val && enabled);
            //     rollTorque.SetActive(val && enabled);
            //     yawTorque.SetActive(val && enabled);
            // };
            pitchTorque.enabledJ.setCallbackFunction += val =>
            {
                pitchTorque.SetActive(val && enabled);
            };
            rollTorque.enabledJ.setCallbackFunction += val =>
            {
                rollTorque.SetActive(val && enabled);
            };
            yawTorque.enabledJ.setCallbackFunction += val =>
            {
                yawTorque.SetActive(val && enabled);
            };
            proximityDepth.setCallbackFunction += val => AdjustTrigger();
            proximityHeight.setCallbackFunction += val => AdjustTrigger();
            
            xrayEnabled.SetWithDefault(false);
            xrayAlpha.SetWithDefault("full");
            
            // thrustForce.control.presetSystem.Init(false);
            // maleForce.control.presetSystem.Init(false);
            // pitchTorque.control.presetSystem.Init(false);
            // rollTorque.control.presetSystem.Init(false);
            // yawTorque.control.presetSystem.Init(false);
            
            InitForcePresets();
            
            sensitivity.SetWithDefault(0f);

            enabled = false;
            initialized = true;
            // enterPointTF.Draw();
            return this;
        }
        
        public void OnEnable()
        {
            if (penetrator == null) return;
            zeroSpeed.Stop();
            zeroDepth.Stop();
            if(thrustForce.enabledJ.val) thrustForce.Enable();
            if(pitchTorque.enabledJ.val) pitchTorque.Enable();
            if(rollTorque.enabledJ.val) rollTorque.Enable();
            if(yawTorque.enabledJ.val) yawTorque.Enable();
            if (penetrator.type > 0)
            {
                maleForce.rb = penetrator.root;
                if(maleForce.enabledJ.val) maleForce.Enable();
                // GetMaleShaker();
            }
            isPenetrated = true;
            foreskiTimer = .5f;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            depth.val = Mathf.Lerp(depth.val, GetDistance(penetrator.tip), 10f*Time.fixedDeltaTime);
            GetSpeed();
            
            if (Mathf.Abs(speed.val) > .1f && !audioSource.isPlaying)
            {
                audioSource.volume = 6f*Mathf.Abs(speed.val) * penetrationSoundsVolume.val;
                FillMeUp.squishLibrary.Play(audioSource);
            }
            // if(thrustForce.enabled) head.AddForce(thrustForce.currentForce);
            // thrustForce.currentForce.Print();
            if(sensitivity.val > 0f) ReadMyLips.Stimulate(50f * speed.val * sensitivity.val, doStim: true, doPlease: true);
            if(foreski != null && Person.foreskiEnabled.val)
            {
                if (foreskiTimer > 0f) foreskiTimer -= Time.fixedDeltaTime;
                else
                {
                    var delta = depth.val - lastDepth;
                    if (delta > 0f) delta *= 1.1f;
                    foreski.morphValue -= 10f * depth.val * delta;
                }
            }
            lastDepth = depth.val;
        }

        private void OnDisable()
        {
            thrustForce.ShutDown(4f);
            maleForce.ShutDown(4f);
            pitchTorque.ShutDown(3f);
            rollTorque.ShutDown();
            yawTorque.ShutDown();
            info.val = "";
            zeroSpeed = ZeroSpeed().Start();
            zeroDepth = ZeroDepth().Start();
            StimReceiver receiver;
            if (penetrator != null && FillMeUp.stimReceivers.TryGetValue(penetrator, out receiver)) receiver.SetFucking(null);
            if (penetrator?.type == 1)
            {
                var person = penetrator.stimReceiver as Person;
                person.xrayClient?.ShutDown();
            }
            penetrator = null;
            isPenetrated = false;
        }


        private void OnDestroy()
        {
            Destroy(triggerGO);
            // Destroy(depthReference);
            zeroDepth.Stop();
            zeroSpeed.Stop();
        }
        
        public override void ResetPenetration()
        {
            maleForce.ShutDownImmediate();
            titJobTrigger.Reset();
        }
        
        public float GetDistance(Transform point)
        {
            var dist = strokeDirection * Vector3.Dot(point.position - enterPointTF.transform.position,
                enterPointTF.transform.up);
            if (dist < 0f) dist = 0f;
            dist /= penetrator.length;
            if (preventPullout.val && dist < .01f)
            {
                thrustForce.ForceInwards();
                maleForce.ForceInwards();
            }
            dist = 1f - dist;
            // if (dist < .1f) dist = .1f;
            // dist = 1.1f*(.1f - dist) + 1f;
            return dist;
            // return Mathf.Abs(Vector3.Dot(point.position - depthReference.transform.position,
            //     depthReference.transform.forward));
        }
        
        private void GetSpeed()
        {
            // speed.val = Mathf.Lerp(speed.val, (lastDistance - distance.val)/Time.deltaTime, 5f*Time.deltaTime);
            speed.val += 10f * (depth.val - lastDepth - speed.val * Time.fixedDeltaTime);
            // speed.val.Print();
        }
        
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
                yield return wait;
            }

            depth.val = lastDepth = 0f;
        }
        
        public override void SetPenetrator(CapsulePenetrator newPenetrator)
        {
            base.SetPenetrator(newPenetrator);
            penetrator = newPenetrator;
            // forward = (penetrator.rootTransform.position - penetrator.tip.position).normalized;
            info.val = $"Stroking <b>{penetrator.atom.name}</b>";
            strokeDirection = Vector3.Dot(enterPointTF.transform.up, thrustDirection) > 0f ? -1f : 1f;
            if (penetrator.type == 0)
            {
                thrustForce.scale = pitchTorque.scale = yawTorque.scale = .1f;
            }
            else thrustForce.scale = pitchTorque.scale = yawTorque.scale = 1f;
        }
        
        public override void CreateSettingsUI()
        {
            sensitivity.CreateUI(UIElements, false);
            penetrationSoundsVolume.CreateUI(UIElements, true);
        }
        
        public override void CreateForcesUI()
        {
            thrustForce.enabledJ.CreateUI(UIElements);
            pitchTorque.enabledJ.CreateUI(UIElements);
            rollTorque.enabledJ.CreateUI(UIElements);
            yawTorque.enabledJ.CreateUI(UIElements);
            maleForce.enabledJ.CreateUI(UIElements);
            magnetic.CreateUI(UIElements);
            FillMeUp.singleton.SetupButton("Configure Thrust", true, () => CreateForceUINewPage(thrustForce), UIElements);
            FillMeUp.singleton.SetupButton("Configure Pitch Torque", true, () => CreateForceUINewPage(pitchTorque), UIElements);
            FillMeUp.singleton.SetupButton("Configure Roll Torque", true, () => CreateForceUINewPage(rollTorque), UIElements);
            FillMeUp.singleton.SetupButton("Configure Yaw Torque", true, () => CreateForceUINewPage(yawTorque), UIElements);
            FillMeUp.singleton.SetupButton("Configure Male Thrust", true, () => CreateForceUINewPage(maleForce), UIElements);
            if (magnet != null)
            {
                FillMeUp.singleton.SetupButton("Configure Magnet", true, magnet.CreateUINewPage, UIElements);
            }
            else
            {
                FillMeUp.singleton.SetupButton("N.A.", true, delegate {  }, UIElements);
            }
            FillMeUp.singleton.SetupButton("Configure Orifice Sync Group", true, FillMeUp.orificeForceGroup.CreateUI, UIElements);
            FillMeUp.orificeForceGroup.driverInfo.CreateUI(UIElements);
            preventPullout.CreateUI(UIElements, true);

            var info = Utils.SetupInfoTextNoScroll(FillMeUp.singleton, toggleInfoText, 200f, false);
            info.background.offsetMin = new Vector2(0, 0);
            UIElements.Add(info);

            // correctiveTorqueEnabled.CreateUI(UIElements, true);
            // correctiveYaw.CreateUI(UIElements, true);
            // correctivePitch.CreateUI(UIElements, true);
        }
        
        public override void CreateDebugUI()
        {
            proximityDepth.CreateUI(UIElements);
            proximityHeight.CreateUI(UIElements, true);
            base.CreateDebugUI();
        }

        private void AdjustTrigger()
        {
            if (parentedToChest)
            {
                triggerGO.transform.localPosition = new Vector3(
                    0f,
                    basePosition.y + proximityHeight.val,
                    basePosition.z + proximityDepth.val);
            }
            else
            {
                triggerGO.transform.localPosition = new Vector3(
                    basePosition.x + proximityHeight.val,
                    basePosition.y,
                    basePosition.z + proximityDepth.val);
            }
        }

        public override JSONClass Store(string subScenePrefix, bool storeTriggers = true)
        {
            var jc = base.Store(subScenePrefix, storeTriggers);
            proximityHeight.Store(jc);
            proximityDepth.Store(jc);
            // FillMeUp.singleton.SaveJSON(jc, "Custom/preset.json");
            return jc;
        }

        public override void Load(JSONClass jc, string subScenePrefix)
        {
            base.Load(jc, subScenePrefix);
            if (jc.HasKey(name))
            {
                JSONClass tc = jc[name].AsObject;
                proximityHeight.Load(tc);
                proximityDepth.Load(tc);
            }
        }
    }
}