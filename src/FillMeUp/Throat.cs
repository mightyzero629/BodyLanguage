using System;
using System.Collections;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using MacGruber;
using SimpleJSON;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CheesyFX
{
    public class Throat : Orifice
    {
        private float nippleStim;
        private static IEnumerator blinkRestore;
        public static LerpingMorph mouthOpen;
        // public static JSONStorableFloat maxMouthMorph = new JSONStorableFloat("Max Mouth Morph", .25f, 0f, 1f);
        
        public override void Init(string name)
        {
            type = 2;
            relaxation = new JSONStorableFloat("Relaxation", 0f, 0f, 1f);
            base.Init(name);
            DAZCharacterSelector characterSelector = FillMeUp.atom.GetStorableByID("geometry") as DAZCharacterSelector;
            depthReference = FillMeUp.atom.rigidbodies.First(x => x.name == "ThroatTrigger");
            name = "Throat";
            guidance = FillMeUp.atom.forceReceivers.First(x => x.name == "head").transform.parent;
            // onEnterActions = orificeManager.bodyManager.audioManager.Stop;
            sensitivity.val = sensitivity.defaultVal = .2f;
            audioSource = depthReference.gameObject.AddComponent<AudioSource>();
            
            mouthOpen = gameObject.AddComponent<LerpingMorph>().Init("Mouth Open Wide 2");

            bulgeScale.val = bulgeScale.defaultVal = 2f;
            bulgeDepthScale.val = bulgeDepthScale.defaultVal = 1.1f;
            
            thrustForce.amplitude.mean.SetWithDefault(150f);
            thrustForce.amplitude.delta.SetWithDefault(50f);
            thrustForce.period.mean.SetWithDefault(.9f);
            thrustForce.period.delta.SetWithDefault(.4f);

            maleForce.amplitude.mean.SetWithDefault(150f);
            maleForce.amplitude.delta.SetWithDefault(50f);
            maleForce.enabledJ.SetWithDefault(false);

            pitchTorque = gameObject.AddComponent<Torque>().Init(name+":Pitch Torque", rb, () => -rb.transform.right);
            pitchTorque.AddSync(thrustForce);
            pitchTorque.syncToThrust.SetWithDefault(true);
            pitchTorque.sync.syncQuickness.SetWithDefault(true);
            pitchTorque.amplitude.mean.SetWithDefault(15f);
            pitchTorque.amplitude.useNormalDistribution.SetWithDefault(false);
            pitchTorque.amplitude.transitionQuicknessMean.SetWithDefault(.5f);
            pitchTorque.sync.phaseOffsetMean.SetWithDefault(0f);
            pitchTorque.sync.phaseOffsetDelta.SetWithDefault(.2f);
            
            rollTorque = gameObject.AddComponent<Torque>().Init(name+":Roll Torque", rb, () => rb.transform.forward);
            rollTorque.constant.SetWithDefault(true);
            rollTorque.amplitude.useNormalDistribution.SetWithDefault(false);
            rollTorque.amplitude.randomizeTimeDelta.SetWithDefault(4f);
            rollTorque.amplitude.mean.SetWithDefault(0f);
            rollTorque.amplitude.delta.SetWithDefault(30f);
            
            yawTorque = gameObject.AddComponent<Torque>().Init(name+":Yaw Torque", rb, () => rb.transform.up);
            yawTorque.enabledJ.SetWithDefault(false);
            yawTorque.constant.SetWithDefault(true);
            yawTorque.amplitude.useNormalDistribution.SetWithDefault(false);
            yawTorque.amplitude.randomizeTimeDelta.SetWithDefault(4f);
            yawTorque.amplitude.mean.SetWithDefault(0f);
            yawTorque.amplitude.delta.SetWithDefault(5f);
            // yawTorque.amplitude.sharpness.SetWithDefault(2f);
            
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
            // // thrustForce.control.presetSystem.Init(false);
            // pitchTorque.control.presetSystem.Init(false);
            // rollTorque.control.presetSystem.Init(false);
            // yawTorque.control.presetSystem.Init(false);
            // // maleForce.control.presetSystem.Init(false);
            //
            // thrustForce.control.presetSystem.StoreDefaults();
            // pitchTorque.control.presetSystem.StoreDefaults();
            // rollTorque.control.presetSystem.StoreDefaults();
            // yawTorque.control.presetSystem.StoreDefaults();
            // maleForce.control.presetSystem.StoreDefaults();
            
            InitForcePresets();
            initialized = true;
        }

        public DebugVector debugVector;
        
        public override void SetPenetrated(bool val)
        {
            if (val)
            {
                zeroRelaxation.Stop();
                Gagger.RestoreBlink();
                BJHelper.singleton.enabled = false;
                FillMeUp.eyelidBehavior.blinkSpaceMax = 10f;
                FillMeUp.eyelidBehavior.blinkSpaceMin = 2f;
                FillMeUp.eyelidBehavior.blinkTimeMin = .75f;
                FillMeUp.eyelidBehavior.blinkTimeMax = 2f;
                FillMeUp.eyelidBehavior.blinkDownUpRatio = .8f;
                if (Random.Range(0f, 1f) > .25f) FillMeUp.eyelidBehavior.Blink();
                blinkRestore.Stop();
                blinkRestore = BlinkRestore().Start();
                if (thrustForce.enabledJ.val)
                {
                    pitchTorque.Enable();
                    rollTorque.Enable();
                    yawTorque.Enable();
                }
                mouthOpen.target = 0f;
                Gaze.lerpGazeStrength.Stop();
                Gaze.lerpGazeStrength = Gaze.LerpGazeStrength(true).Start();
                // mouthOpen.target = Mathf.Min(maxMouthMorph.val, FillMeUp.throat.penetratorWidth);
            }
            else
            {
                // Gagger.RestoreBlink();
                if(BJHelper.penetrators.Count > 0) BJHelper.singleton.enabled = true;
                zeroStretch = ZeroStretch().Start();
                zeroRelaxation = ZeroRelaxation().Start();
                pitchTorque.ShutDown(3f);
                rollTorque.ShutDown();
                yawTorque.ShutDown();
                enabled = false;
                if(penetrator?.stimReceiver != null) penetrator.stimReceiver.ResetColliders();
                // if(penetrator?.type == 1)
                // {
                //     var capPen = (CapsulePenetrator)penetrator;
                //     capPen.ResetColliders();
                // }
                // mouthOpen.target = 0f;
                ResetBulge();
                Gaze.lerpGazeStrength.Stop();
                Gaze.lerpGazeStrength = Gaze.LerpGazeStrength(false).Start();
            }
            base.SetPenetrated(val);
        }

        public override void ResetBulge()
        {
            for (int i = 0; i < FillMeUp.throatBulgeMorphs.Count; i++)
            {
                FillMeUp.throatBulgeMorphs[i].morphValue = 0f;
            }
        }

        private static IEnumerator BlinkRestore()
        {
            yield return new WaitForSeconds(4f);
            Gagger.RestoreBlink();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            var absSpeed = Mathf.Abs(speed.val);
            if(!audioSource.isPlaying && penetrationSoundsVolume.val > 0 && absSpeed > .05f && depth.val > .05f)
            {
                var clip = FillMeUp.bjLibrary.GetRandomClip();
                audioSource.clip = clip;
                audioSource.volume = 10f * absSpeed * (1f + depth.val)*(1f + depth.val) * penetrationSoundsVolume.val;
                audioSource.Play();
            }

            if (depth.val > .1f)
            {
                nippleStim = (depth.val - .1f) * Time.fixedDeltaTime * 6f;
                NippleManager.nippleDrivers[0].Stimulate(nippleStim);
                NippleManager.nippleDrivers[1].Stimulate(nippleStim);
                if (depth.val > Gagger.gagThreshold.val) relaxation.val += .0005f;
            }

            // if (depth.val > Gagger.gagThreshold.val && Random.Range(0f, 1f) > relaxation.val)
            // {
            //     if(Gagger.gag == null && lastDepth <= Gagger.gagThreshold.val) Gagger.gag = Gagger.Gag().Start();
            // }
            // if (Gagger.gag != null && depth.val < Gagger.gagThreshold.val - .02f)
            // {
            //     Gagger.Stop();
            // }
            
            Gagger.Update(depth.val, lastDepth, relaxation);

            if (relaxation.val > 0f) relaxation.val -= .0125f * Time.fixedDeltaTime;
            else
            {
                relaxation.val = 0f;
            }

            DriveForeski();
            lastDepth = depth.val;
        }
        
        // private void SoftenAndBend(FreeControllerV3 ctrl, float defaultVal)
        // {
        //     var bone = ctrl == midCtrl ? penetrator.bones[1] : penetrator.bones[2];
        //     if(Vector3.Dot(FillMeUp.throat.entrance.transform.up, ctrl.followWhenOff.position - FillMeUp.throat.entrance.transform.position) > .02f)
        //     {
        //         if (ctrl.jointRotationDriveSpring > 4.1f)
        //         {
        //             // $"{ctrlToDepthMeter} : {FillMeUp.throat.depth.val}".Print();
        //             ctrl.jointRotationDriveSpring = Mathf.Lerp(ctrl.jointRotationDriveSpring, 4f, 2f*Time.deltaTime);
        //             var rot = Mathf.Lerp(bone.baseJointRotation.x, -70f, 2f * Time.deltaTime);
        //             bone.baseJointRotation = new Vector3(rot, 0f, 0f);
        //             // bone.baseJointRotation.Print();
        //         }
        //     }
        //     else 
        //     {
        //         // bone.baseJointRotation = Vector3.zero;
        //         // ctrl.jointRotationDriveSpring = defaultVal;
        //         if(ctrl.jointRotationDriveSpring < defaultVal-.1f)
        //         {
        //             // tipCtrl.jointRotationDriveSpring = 12f;
        //             ctrl.jointRotationDriveSpring = Mathf.Lerp(ctrl.jointRotationDriveSpring, 12f, 2f*Time.deltaTime);
        //             var rot = Mathf.Lerp(bone.baseJointRotation.x, 0f, 4f * Time.deltaTime);
        //             bone.baseJointRotation = new Vector3(rot, 0f, 0f);
        //         }
        //     }
        //
        //     // if (FillMeUp.throat.depth.val < .05f) ctrl.jointRotationDriveSpring = defaultVal;
        // }
        
        private IEnumerator zeroRelaxation;

        private IEnumerator ZeroRelaxation()
        {
            while (relaxation.val > 0f)
            {
                relaxation.val -= .075f * Time.fixedDeltaTime;
                yield return waitForFixedUpdate;
            }
            relaxation.val = 0f;
        }

        public override float Bulge(int x)
        {
            if (depth.val == 0f || bulgeDepthScale.val == 0f) return 0f;
            float val = (3f*penetratorWidth)*bulgeScale.val*Mathf.Min(10f * depth.val * bulgeDepthScale.val, 1f) * Mathf.Pow(2f, -x * x *1e-4f* Mathf.Pow(depth.val*bulgeDepthScale.val, -4));
            return val;
        }

        public override void CreateSettingsUI()
        {
            base.CreateSettingsUI();
            Gagger.gagThreshold.CreateUI(UIElements);
            Gagger.enabled.CreateUI(UIElements, true);
            Gagger.gagScale.CreateUI(UIElements, true);
            // maxMouthMorph.CreateUI(UIElements);
            penetratorScaling.CreateUI(UIElements);
        }
        
        public override void CreateBulgeUI()
        {
            bulgeScale.CreateUI(FillMeUp.singleton, UIElements:UIElements);
            bulgeDepthScale.CreateUI(FillMeUp.singleton, UIElements:UIElements);
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

            var info = Utils.SetupInfoTextNoScroll(FillMeUp.singleton, toggleInfoText, 200f, false);
            info.background.offsetMin = new Vector2(0, 0);
            UIElements.Add(info);

            correctiveTorqueEnabled.CreateUI(UIElements, true);
            correctiveYaw.CreateUI(UIElements, true);
            correctivePitch.CreateUI(UIElements, true);
            preventPullout.CreateUI(UIElements, true);
        }

        public override void RegisterCollision(Collider collider)
        {
            penetrator = GetPenetrator(collider);
            // depthMeter.transform.parent = penetrator.tip;
            depthMeter.transform.SetParent(penetrator.tip, true);
            depthMeter.transform.localPosition = Vector3.zero;
            depthMeter.transform.localRotation = Quaternion.identity;
            penetratorWidth = penetrator.width * stretchScale.val;
            isPenetrated = true;
            penetratingAtom = penetrator.atom;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            Gagger.gag.Stop();
            zeroRelaxation.Stop();
        }
        
        public void OnDisable()
        {
            Gagger.gag.Stop();
            // thrustDirection = Vector3.zero;
            // thrustForce.enabled = false;
        }
        
        public override JSONClass Store(string subScenePrefix, bool storeTriggers = true)
        {
            var jc = base.Store(subScenePrefix, storeTriggers);
            // maxMouthMorph.Store(jc);
            return jc;
        }

        public override void Load(JSONClass jc, string subScenePrefix)
        {
            base.Load(jc, subScenePrefix);
            // maxMouthMorph.Load(jc);
        }
    }
}