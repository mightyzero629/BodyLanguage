using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CheesyFX
{
    public class Ahegao : MonoBehaviour
    {
        private static Transform lEye;
        private static Transform rEye;
        private static Rigidbody head;
        private static EyesControl eyeBehavior;
        private static LerpableMorph tongueRaiseLower;
        private static LerpableMorph tongueUp;
        private static LerpableMorph tongueBendTip;
        private static LerpableMorph tongueOut;
        private static LerpableMorph tongueRoll;

        private static Quaternion lEyeRotation;
        private static Quaternion rEyeRotation;

        private static Quaternion lEyeTarget;
        private static Quaternion rEyeTarget;
        private static float tongueUpTarget;
        private static float tongueOutTarget;
        
        public static List<Shaker> legShakers = new List<Shaker>();
        private static ChestShaker chestShaker;
        
        public static bool shutDown;
        private static float shutDownTimer;

        private static float rollUp_mean = -18f;
        private static float rollUp_delta = 6f;
        private static float rollIn_mean = 10f;
        private static float rollIn_delta = 3f;

        private static JSONStorableFloat ahegaoChance = new JSONStorableFloat("Ahegao Chance", 1f, 0f, 1f);
        private static JSONStorableFloat shakeChance = new JSONStorableFloat("Shake Chance", 1f, 0f, 1f);
        private static JSONStorableFloat chestShakeStrength = new JSONStorableFloat("Chest Shake Strength", 1f, 0f, 1f, false);
        private static JSONStorableFloat legsShakeStrength = new JSONStorableFloat("Legs Shake Strength", 1f, 0f, 1f, false);
        // private static JSONStorableFloat maleShakeStrength = new JSONStorableFloat("Male Shake Strength", 1f, 0f, 1f, false);
        private static JSONStorableFloat tongueScale = new JSONStorableFloat("Tongue Morph Scale", 1f, 0f, 3f, false);

        private static JSONStorableBool ahegaoEyesEnabled = new JSONStorableBool("Ahegao Eyes Enabled", true);
        private static JSONStorableBool ahegaoTongueEnabled = new JSONStorableBool("Ahegao Tongue Enabled", true);
        private static JSONStorableBool ahegaoHeadEnabled = new JSONStorableBool("Ahegao Head Enabled", true);

        private static LookAtWithLimits lPlaceboLookat;
        private static LookAtWithLimits rPlaceboLookat;
        private GameObject lPlaceboLookatGO;
        private GameObject rPlaceboLookatGO;
        
        private static JSONStorableFloat headTorqueMean = new JSONStorableFloat("Head Torque Mean", 500f, 0f, 1000f);
        private static JSONStorableFloat headTorqueDelta = new JSONStorableFloat("Head Torque Delta", 300f, 0f, 1000f);
        
        private float headTorqueTarget;
        private Vector3 headTorque;
        private float timer;
        
        public Ahegao Init()
        {
            legShakers.Add(gameObject.AddComponent<Shaker>().Init(ReadMyLips.singleton.containingAtom.rigidbodies.FirstOrDefault(x => x.name == "rShin")));
            legShakers.Add(gameObject.AddComponent<Shaker>().Init(ReadMyLips.singleton.containingAtom.rigidbodies.FirstOrDefault(x => x.name == "lShin")));
            chestShaker = gameObject.AddComponent<ChestShaker>().Init(ReadMyLips.singleton.containingAtom.rigidbodies.FirstOrDefault(x => x.name == "chest"));
            lEye = ReadMyLips.singleton.containingAtom.GetComponentsInChildren<DAZBone>().First(x => x.name == "lEye").transform;
            rEye = ReadMyLips.singleton.containingAtom.GetComponentsInChildren<DAZBone>().First(x => x.name == "rEye").transform;
            head = ReadMyLips.singleton.containingAtom.rigidbodies.First(x => x.name == "head");
            
            
            tongueOut = ReadMyLips.GetMorph("Tongue In-Out");
            tongueRaiseLower = ReadMyLips.GetMorph("Tongue Raise-Lower");
            tongueUp = ReadMyLips.GetMorph("Tongue Up-Down");
            tongueBendTip = ReadMyLips.GetMorph("Tongue Bend Tip");
            tongueRoll = ReadMyLips.GetMorph("Tongue Roll 1");
            
            ahegaoChance.Register(ReadMyLips.singleton);
            shakeChance.Register(ReadMyLips.singleton);
            chestShakeStrength.Register(ReadMyLips.singleton);
            legsShakeStrength.Register(ReadMyLips.singleton);
            // maleShakeStrength.Register(ReadMyLips.singleton);
            ahegaoEyesEnabled.Register(ReadMyLips.singleton);
            ahegaoTongueEnabled.Register(ReadMyLips.singleton);

            // legsShakeStrength.AddCallback(val =>
            // {
            //     legShakers[0].forceFactor = legShakers[0].baseForceFactor * val;
            //     legShakers[1].forceFactor = legShakers[1].baseForceFactor * val;
            // });
            // chestShakeStrength.AddCallback(val => legShakers[2].forceFactor = legShakers[2].baseForceFactor * val);

            eyeBehavior = (EyesControl)ReadMyLips.singleton.containingAtom.GetStorableByID("Eyes");
            lPlaceboLookatGO = new GameObject("lPlaceboLookAt");
            lPlaceboLookatGO.transform.SetParent(head.transform, false);
            lPlaceboLookatGO.transform.position = lEye.position;
            rPlaceboLookatGO = new GameObject("rPlaceboLookAt");
            rPlaceboLookatGO.transform.SetParent(head.transform, false);
            rPlaceboLookatGO.transform.position = rEye.position;
            lPlaceboLookat = lPlaceboLookatGO.AddComponent<LookAtWithLimits>();
            // lPlaceboLookat.lookAtCameraLocation = eyeBehavior.lookAt1.lookAtCameraLocation;
            lPlaceboLookat.lookAtCameraLocation = CameraTarget.CameraLocation.None;
            lPlaceboLookat.target = eyeBehavior.lookAt1.target;
            lPlaceboLookat.updateTime = eyeBehavior.lookAt1.updateTime;
            lPlaceboLookat.MaxRight = eyeBehavior.lookAt1.MaxRight;
            lPlaceboLookat.MaxLeft = eyeBehavior.lookAt1.MaxLeft;
            lPlaceboLookat.MaxUp = eyeBehavior.lookAt1.MaxUp;
            lPlaceboLookat.MaxDown = eyeBehavior.lookAt1.MaxDown;
            lPlaceboLookat.MinEngageDistance = eyeBehavior.lookAt1.MinEngageDistance;
            lPlaceboLookat.smoothFactor = eyeBehavior.lookAt1.smoothFactor;
            lPlaceboLookat.MoveFactor = eyeBehavior.lookAt1.MoveFactor;
            lPlaceboLookat.LeftRightAngleAdjust = eyeBehavior.lookAt1.LeftRightAngleAdjust;
            lPlaceboLookat.UpDownAngleAdjust = eyeBehavior.lookAt1.UpDownAngleAdjust;
            lPlaceboLookat.DepthAdjust = eyeBehavior.lookAt1.DepthAdjust;
            
            rPlaceboLookat = rPlaceboLookatGO.AddComponent<LookAtWithLimits>();
            rPlaceboLookat.lookAtCameraLocation = CameraTarget.CameraLocation.None;
            rPlaceboLookat.target = eyeBehavior.lookAt2.target;
            rPlaceboLookat.updateTime = eyeBehavior.lookAt2.updateTime;
            rPlaceboLookat.MaxRight = eyeBehavior.lookAt2.MaxRight;
            rPlaceboLookat.MaxLeft = eyeBehavior.lookAt2.MaxLeft;
            rPlaceboLookat.MaxUp = eyeBehavior.lookAt2.MaxUp;
            rPlaceboLookat.MaxDown = eyeBehavior.lookAt2.MaxDown;
            rPlaceboLookat.MinEngageDistance = eyeBehavior.lookAt2.MinEngageDistance;
            rPlaceboLookat.smoothFactor = eyeBehavior.lookAt2.smoothFactor;
            rPlaceboLookat.MoveFactor = eyeBehavior.lookAt2.MoveFactor;
            rPlaceboLookat.LeftRightAngleAdjust = eyeBehavior.lookAt2.LeftRightAngleAdjust;
            rPlaceboLookat.UpDownAngleAdjust = eyeBehavior.lookAt2.UpDownAngleAdjust;
            rPlaceboLookat.DepthAdjust = eyeBehavior.lookAt2.DepthAdjust;

            enabled = false;
            return this;
        }

        public void OnDisable()
        {
            tongueOut.morphVal = 1f;
            tongueRaiseLower.morphVal = 0f;
            tongueUp.morphVal = 0f;
            tongueBendTip.morphVal = 0f;
            tongueRoll.morphVal = .5f;
            lPlaceboLookat.enabled = false;
            rPlaceboLookat.enabled = false;
            if(eyeBehavior.currentLookMode != EyesControl.LookMode.None)
            {
                eyeBehavior.lookAt1.enabled = true;
                eyeBehavior.lookAt2.enabled = true;
            }
        }
        
        private void OnDestroy()
        {
            EmoteManager.Destroy();
            Destroy(lPlaceboLookatGO);
            Destroy(rPlaceboLookatGO);
            tongueOut.morphVal = 1f;
            tongueRaiseLower.morphVal = 0f;
            tongueUp.morphVal = 0f;
            tongueBendTip.morphVal = 0f;
            tongueRoll.morphVal = .5f;
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (shutDown)
            {
                shutDownTimer -= Time.deltaTime;
                lPlaceboLookat.target = eyeBehavior.lookAt1.target;
                rPlaceboLookat.target = eyeBehavior.lookAt2.target;
                lEye.localRotation = Quaternion.Lerp(lEye.localRotation, lPlaceboLookatGO.transform.localRotation, 4f*Time.deltaTime);
                rEye.localRotation = Quaternion.Lerp(rEye.localRotation, rPlaceboLookatGO.transform.localRotation, 4f*Time.deltaTime);
                if(ahegaoHeadEnabled.val)
                {
                    headTorque.x = Mathf.Lerp(headTorque.x, 0f, Time.deltaTime * 2f);
                    if (shutDownTimer < 2f)
                    {
                        PoseMe.gaze.update = true;
                    }

                    if (shutDownTimer < 0f && -headTorque.x < 10f)
                    {
                        enabled = false;
                    }
                }
                else if (shutDownTimer < 0f)
                {
                    PoseMe.gaze.update = true;
                    enabled = false;
                }
            }
            else
            {
                Vector3 rng = new Vector3(Random.Range(-10f, 10f), Random.Range(-10f, 10f), 0f);
                lEye.localRotation = Quaternion.Lerp(lEye.localRotation, lEyeTarget * Quaternion.Euler(rng), 3f*Time.deltaTime);
                rEye.localRotation = Quaternion.Lerp(rEye.localRotation, rEyeTarget * Quaternion.Euler(rng.x, -rng.y, rng.z), 3f*Time.deltaTime);
                if(ahegaoHeadEnabled.val)
                {
                    if (timer < 1.5f) headTorque.x = Mathf.Lerp(headTorque.x, headTorqueTarget, Time.deltaTime);
                    else headTorque.x = Mathf.Lerp(headTorque.x, 0f, .4f * Time.deltaTime);
                }
                // headTorque.x.Print();
            }
            if(ahegaoTongueEnabled.val)
            {
                tongueOut.LerpToTarget();
                tongueRaiseLower.LerpToTarget();
                tongueUp.LerpToTarget();
                tongueBendTip.LerpToTarget();
                tongueRoll.LerpToTarget();
            }
        }

        private void FixedUpdate()
        {
            if(!Pose.isApplying || ahegaoHeadEnabled.val && !SuperController.singleton.freezeAnimation) head.AddRelativeTorque(headTorque);
        }

        public static void ShutDown()
        {
            shutDown = true;
            tongueOut.target = 1f;
            tongueRaiseLower.target = 0f;
            tongueUp.target = 0f;
            tongueBendTip.target = 0f;
            tongueRoll.target = .5f;
            shutDownTimer = 4f;
            ToggleShaking(false);
            // ToggleMaleOrgasm(false);
            lPlaceboLookat.target = eyeBehavior.lookAt1.target;
            rPlaceboLookat.target = eyeBehavior.lookAt2.target;
            lPlaceboLookat.enabled = true;
            rPlaceboLookat.enabled = true;
        }

        public void Run()
        {
            shutDown = false;
            if (shakeChance.val > 0f && Random.Range(0f, 1f) < shakeChance.val)
            {
                ToggleShaking(true);
                // ToggleMaleOrgasm(true);
            }
            if (ahegaoChance.val > 0f && (ahegaoChance.val > .9999f || Random.Range(0f, 1f) < ahegaoChance.val))
            {
                if(ahegaoEyesEnabled.val)
                {
                    lEyeRotation = lEye.localRotation;
                    rEyeRotation = rEye.localRotation;
                    eyeBehavior.lookAt1.enabled = false;
                    eyeBehavior.lookAt2.enabled = false;
                    lEye.localRotation = lEyeRotation;
                    rEye.localRotation = rEyeRotation;
                    float rollUp = NormalDistribution.GetValue(rollUp_mean, rollUp_delta, sharpness: 2f);
                    float rollIn = NormalDistribution.GetValue(rollIn_mean, rollIn_delta, sharpness: 2f);
                    lEyeTarget = Quaternion.Euler(rollUp, rollIn, 0f);
                    rEyeTarget = Quaternion.Euler(rollUp, -rollIn, 0f);
                }
                if(ahegaoTongueEnabled.val)
                {
                    tongueOut.target = -.6f * tongueScale.val;
                    tongueRaiseLower.target = .4f;
                    tongueUp.target = -1f;
                    tongueBendTip.target = .16f;
                    tongueRoll.target = .2f;
                }
                if (EmoteManager.enabled.val)
                {
                    EmoteManager.orgasmEmotes.ps.Play();
                }

                headTorqueTarget = -NormalDistribution.GetValue(headTorqueMean.val, headTorqueDelta.val, 2);
                headTorque.x = 0f;
                timer = 0f;
                PoseMe.gaze.update = false;
                enabled = true;
            }
            
        }
        
        // public static void ToggleMaleOrgasm(bool val)
        // {
        //     if(val)
        //     {
        //         foreach (var orifice in FillMeUp.orifices)
        //         {
        //             if (orifice.isPenetrated && (object)orifice.maleShaker != null) orifice.maleShaker.Run(maleShakeStrength.val);
        //         }
        //     }
        //     else
        //     {
        //         FillMeUp.maleShakers.Values.ToList().ForEach(x => x.ShutDown());
        //     }
        // }

        private static void ToggleShaking(bool val)
        {
            if (val)
            {
                legShakers.ForEach(x => x.Run(legsShakeStrength.val));
                chestShaker.Run(chestShakeStrength.val);
            }
            else
            {
                legShakers.ForEach(x => x.ShutDown());
                chestShaker.ShutDown();
            }
        }

        public static void CreateUI(List<object> UIElements)
        {
            var textField = ReadMyLips.singleton.CreateTextField(new JSONStorableString("bla", "Ahegao is only played on orgasm."));
            textField.ForceHeight(55f);
            UIElements.Add(textField);
            var button = ReadMyLips.singleton.SetupButton("Orgasm Now", false, ReadMyLips.ForceOrgasm);
            UIElements.Add(button);
            ahegaoChance.CreateUI(UIElements);
            shakeChance.CreateUI(UIElements);
            ahegaoEyesEnabled.CreateUI(UIElements);
            ahegaoTongueEnabled.CreateUI(UIElements);
            ahegaoHeadEnabled.CreateUI(UIElements);
            EmoteManager.enabled.CreateUI(UIElements, false);
            ReadMyLips.singleton.SetupButton("Configure Emotes",false , () =>
            {
                ReadMyLips.singleton.ClearUI();
                EmoteManager.CreateUI();
                ReadMyLips.mainWindowOpen = false;
            }, UIElements);
            
            chestShakeStrength.CreateUI(UIElements, true);
            legsShakeStrength.CreateUI(UIElements, true);
            // maleShakeStrength.CreateUI(UIElements, true);
            tongueScale.CreateUI(UIElements, true);

            // EmotionSprayer.orgasm.CreateUI(UIElements);
        }

        public static void Store(JSONClass jc)
        {
            ahegaoChance.Store(jc);
            shakeChance.Store(jc);
            ahegaoEyesEnabled.Store(jc);
            ahegaoTongueEnabled.Store(jc);
            ahegaoHeadEnabled.Store(jc);
            
            chestShakeStrength.Store(jc);
            legsShakeStrength.Store(jc);
            headTorqueMean.Store(jc);
            headTorqueDelta.Store(jc);
            // maleShakeStrength.Store(jc);
            jc["Emotes"] = EmoteManager.Store();
        }

        public static void Load(JSONClass jc)
        {
            ahegaoChance.Load(jc);
            shakeChance.Load(jc);
            ahegaoEyesEnabled.Load(jc);
            ahegaoTongueEnabled.Load(jc);
            ahegaoHeadEnabled.Load(jc);
            
            chestShakeStrength.Load(jc);
            legsShakeStrength.Load(jc);
            headTorqueMean.Load(jc);
            headTorqueDelta.Load(jc);
            // maleShakeStrength.Load(jc);
            EmoteManager.Load(jc["Emotes"].AsObject);
        }
    }
}