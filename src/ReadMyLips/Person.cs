using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace CheesyFX
{
    public class Person : StimReceiver
    {
        // public static JSONStorableFloat stimulation = new JSONStorableFloat("Stimulation", 0f, 0f, 1f, true, false);
        private List<CumClothing> cumClothings = new List<CumClothing>();
        public static JSONStorableBool cumClothingEnabled = new JSONStorableBool("Cum Clothes´Enabled", true);
        public static JSONStorableFloat cumShotPower = new JSONStorableFloat("Cumshot Power (M/F)", 1f, 0f, 5f, false);
        public static JSONStorableFloat maxLoad = new JSONStorableFloat("Max Load (M/F)", 20f, 5f, 50f);
        // public static JSONStorableFloat clothesForceJ = new JSONStorableFloat("Clothes Force", 30f, 0f, 100f, false);
        public static JSONStorableFloat clothingFadeTime = new JSONStorableFloat("Clothing Fade Time", 10f, 5f, 60f);
        public static JSONStorableFloat clothingBreakThreshold = new JSONStorableFloat("Clothing Break Threshold", 1f, 0f, 2f);
        public static JSONStorableBool particlesEnabled = new JSONStorableBool("Particles Enabled (M/F)", true);
        public static JSONStorableFloat particleSpeed = new JSONStorableFloat("Particle Speed (M/F)", 1f, 0f, 10f);
        public static JSONStorableFloat particleAmount = new JSONStorableFloat("Particle Amount (M/F)", 1f, 0f, 10f);
        public static JSONStorableFloat particleOpacity = new JSONStorableFloat("Particle Opacity (M/F)", 1f,SetParticleOpacity, 0f, 1f);
        public static JSONStorableBool foreskiEnabled = new JSONStorableBool("Foreskin Enabled", true, FillMeUp.ForeskiSetActive);
        public static JSONStorableBool cumInteracting = new JSONStorableBool("Forced: Only Interacting (M/F)", false);
        public static JSONStorableBool forceFullLoadJ = new JSONStorableBool("Forced: Full Load (M/F)", true);
        public static JSONStorableFloat stimGainJ = new JSONStorableFloat("Stim Gain (M/F)", 1f, 0f, 10f, false);
        public static JSONStorableFloat stimRegressionJ = new JSONStorableFloat("Stim Regression (M/F)", 1f, 0f, 10f, false);
        public static JSONStorableFloat loadGainJ = new JSONStorableFloat("Load Gain (M/F)", 1f, 0f, 10f, false);

        public JSONStorableBool disableAnatomy;
        
        public static JSONStorableBool stiffenEnabled = new JSONStorableBool("Penis Stiffen Enabled", true);
        public static JSONStorableFloat stiffenAmount = new JSONStorableFloat("Penis Stiffen Amount", 200f, 50f, 500f);

        public static JSONStorableStringChooser cumKeyChooser = new JSONStorableStringChooser("cumKeyChooserMales",
            new List<string> { "Q", "Y", "I", "O", "P", "K", "L" }, "Q", "Orgasm Males", SetCumKey);
        public static JSONStorableStringChooser cleanKeyChooser = new JSONStorableStringChooser("cleanKeyChooser",
            new List<string> { "Q", "Y", "I", "O", "P", "K", "L" }, "Y", "Clean Up", SetCleanKey);
        
        public override float stimGain => stimGainJ.val;
        public override float stimRegression => stimRegressionJ.val;
        public override float loadGain => loadGainJ.val;
        public override bool forceFullLoad => forceFullLoadJ.val;
        public override bool infoUIOpen => ReadMyLips.maleInfoUIOpen;
        public static KeyCode cumKey = KeyCode.Q;
        public static KeyCode cleanKey = KeyCode.Y;

        public DAZCharacterSelector dcs;
        public DAZMorph foreskiBase;

        public CharacterListener characterListener;
        
        public string uid => penetrator.atom.uid;

        public Rigidbody softGlute;
        public Rigidbody softBreast;

        private FreeControllerV3 baseCtrl;
        public FreeControllerV3 midCtrl;
        private FreeControllerV3 tipCtrl;
        
        public List<AutoCollider> autoColliders;

        public SpeechBubbleControl speechControl;
        public SpeechBubbleControl thoughtControl;
        public IEnumerator speechRoutine;

        public IEnumerator thoughtRoutine;

        [FormerlySerializedAs("lCarpalones")] public DAZBone[] lCarpalBones = new DAZBone[2];
        [FormerlySerializedAs("rCarpalones")] public DAZBone[] rCarpalBones = new DAZBone[2];
        public DAZBone[] lHandBones = new DAZBone[15];
        public DAZBone[] rHandBones = new DAZBone[15];

        public Rigidbody lHand;
        public Rigidbody rHand;
        public Rigidbody lForeArm;
        public Rigidbody rForeArm;

        public Gaze gaze;

        
        //
        // private DAZMorph shGirthBase;
        // private DAZMorph shGirth;
        // private DAZMorph shLength;
        // private DAZMorph erect;
        
        // private float clothForce;

        private static float GetClothesForce()
        {
            return cumShotPower.val * (5f + 675f * (Time.deltaTime - .0111f));
        }

        private void GetMorphs()
        {
            GenerateDAZMorphsControlUI morphsControlUI;
            if (dcs.gender == DAZCharacterSelector.Gender.Male)
            {
                morphsControlUI = dcs.morphsControlUI;
            }
            else morphsControlUI = dcs.morphsControlUIOtherGender;
            foreski = morphsControlUI.GetMorphByUid(FillMeUp.packageUid + "Custom/Atom/Person/Morphs/male_genitalia/CheesyFX/BodyLanguage/Foreski/BL_babul_foreskinFAP.vmi");
            foreski.jsonFloat.max = 1.25f;
            foreski.max = 1.25f;
            foreski.jsonFloat.constrained = true;
            foreskiBase = morphsControlUI.GetMorphByUid(FillMeUp.packageUid + "Custom/Atom/Person/Morphs/male_genitalia/CheesyFX/BodyLanguage/Foreski/BL_babul_foreskinBASE.vmi");
            foreskiBase.morphValue = foreskiEnabled.val? 1f : 0f;
        }

        public new Person Init(CapsulePenetrator penetrator)
        {
            base.Init(penetrator);
            dcs = penetrator.atom.GetStorableByID("geometry") as DAZCharacterSelector;
            
            GenerateDAZMorphsControlUI morphsControlUI;
            if (dcs.gender == DAZCharacterSelector.Gender.Male)
            {
                morphsControlUI = dcs.morphsControlUI;
                GetCumClothes();
                disableAnatomy = dcs.GetBoolJSONParam("disableAnatomy");
            }
            else morphsControlUI = dcs.morphsControlUIOtherGender;
            foreski = morphsControlUI.GetMorphByUid(FillMeUp.packageUid + "Custom/Atom/Person/Morphs/male_genitalia/CheesyFX/BodyLanguage/Foreski/BL_babul_foreskinFAP.vmi");
            foreski.jsonFloat.max = 1.25f;
            foreski.max = 1.25f;
            foreski.jsonFloat.constrained = true;
            foreskiBase = morphsControlUI.GetMorphByUid(FillMeUp.packageUid + "Custom/Atom/Person/Morphs/male_genitalia/CheesyFX/BodyLanguage/Foreski/BL_babul_foreskinBASE.vmi");
            foreskiBase.morphValue = foreskiEnabled.val? 1f : 0f;
            
            FillMeUp.onMorphsDeactivated.AddListener(GetMorphs);
            
            // shGirth = morphsControlUI.GetMorphByUid("Sh_Girth");
            // shLength = morphsControlUI.GetMorphByUid("Sh_Length");
            // shGirthBase = morphsControlUI.GetMorphByUid("Sh_Girth_Base");
            // erect = morphsControlUI.GetMorphByUid("Erect");
            //
            baseCtrl = penetrator.atom.freeControllers.First(x => x.name == "penisBaseControl");
            midCtrl = penetrator.atom.freeControllers.First(x => x.name == "penisMidControl");
            tipCtrl = penetrator.atom.freeControllers.First(x => x.name == "penisTipControl");
            
            // baseCtrl.jointRotationDriveSpring = midCtrl.jointRotationDriveSpring = 1f;
            // tipCtrl.jointRotationDriveSpring = .2f;
            //
            // shGirth.morphValue = shLength.morphValue = -1f;
            // shGirthBase.morphValue = erect.morphValue = 0f;

            characterListener = new CharacterListener(penetrator.atom);
            characterListener.OnGenderChanged.AddListener(GetCumClothes);
            characterListener.OnGenderChanged.AddListener(() => RefreshGaze().Start());
            characterListener.OnChangedToFuta.AddListener(GetCumClothes);
            characterListener.OnChangedToFemale.AddListener(OnChangedToFemale);
            characterListener.onSkinChangedTo.AddListener(val =>
            {
                if(characterListener.gender == DAZCharacterSelector.Gender.Male) xrayClient?.SyncSkin();
            });
            characterListener.OnChangedToMale.AddListener(() =>
            {
                xrayClient = XRay.RegisterClient(this);
            });
            characterListener.OnChangedToFemale.AddListener(() => XRay.DeregisterClient(xrayClient));
            
            if(characterListener.isFuta) GetCumClothes();
            
            maleShaker = ReadMyLips.singleton.gameObject.AddComponent<MaleShaker>().Init(penetrator.atom);
            
            // stimulation.slider.GetComponentsInChildren<Image>().First(x => x.name == "Fill").color = Color.cyan;
            
            clothingFadeTime.setCallbackFunction += val => CumClothing.waitForFadeOut = new WaitForSeconds(val);

            stimGainJ.setCallbackFunction += val => dynamicStimGain = val;
            stimRegressionJ.setCallbackFunction += val => dynamicStimRegression = val;
            maxLoad.setCallbackFunction += val => cumshotHandler.load.max = val;
            clothingBreakThreshold.setCallbackFunction += val => SyncClothesSettings();
            
            softGlute = atom.GetComponentsInChildren<Rigidbody>(true)
                .First(x => x.name.StartsWith("PhysicsMeshJointleft glute"));
            softBreast = atom.GetComponentsInChildren<Rigidbody>(true)
                .First(x => x.name.StartsWith("PhysicsMeshJointleft"));
            
            speechControl = atom.GetStorableByID("SpeechBubble") as SpeechBubbleControl;
            thoughtControl = atom.GetStorableByID("ThoughtBubble") as SpeechBubbleControl;

            lHand = atom.forceReceivers.First(x => x.name == "lHand").GetComponent<Rigidbody>();
            var bones = lHand.GetComponentsInChildren<DAZBone>();
            foreach (var bone in bones)
            {
                if (bone.name.StartsWith("lC"))
                {
                    lCarpalBones[int.Parse(bone.name.Last().ToString()) - 1] = bone;
                    continue;
                }
                int finger = -1;
                if (bone.name.StartsWith("lT")) finger = 0;
                else if (bone.name.StartsWith("lI")) finger = 3;
                else if(bone.name.StartsWith("lM")) finger = 6;
                else if(bone.name.StartsWith("lR")) finger = 9;
                else if(bone.name.StartsWith("lP")) finger = 12;
                if(finger == -1) continue;
                lHandBones[int.Parse(bone.name.Last().ToString())-1+finger] = bone;
            }
            rHand = atom.forceReceivers.First(x => x.name == "rHand").GetComponent<Rigidbody>();
            bones = rHand.GetComponentsInChildren<DAZBone>();
            foreach (var bone in bones)
            {
                if (bone.name.StartsWith("rC"))
                {
                    rCarpalBones[int.Parse(bone.name.Last().ToString()) - 1] = bone;
                    continue;
                }
                int finger = -1;
                if (bone.name.StartsWith("rT")) finger = 0;
                else if (bone.name.StartsWith("rI")) finger = 3;
                else if(bone.name.StartsWith("rM")) finger = 6;
                else if(bone.name.StartsWith("rR")) finger = 9;
                else if(bone.name.StartsWith("rP")) finger = 12;
                if(finger == -1) continue;
                rHandBones[int.Parse(bone.name.Last().ToString())-1+finger] = bone;
            }
            lForeArm = atom.forceReceivers.First(x => x.name == "lForeArm").GetComponent<Rigidbody>();
            rForeArm = atom.forceReceivers.First(x => x.name == "rForeArm").GetComponent<Rigidbody>();
            
            autoColliders = penetrator.root.GetComponentsInChildren<AutoCollider>().ToList();
            if(atom != FillMeUp.atom)
            {
                gaze = new Gaze(penetrator.atom);
                penisGazeTarget = Gaze.RegisterPerson(this, gaze);
                if (characterListener.gender == DAZCharacterSelector.Gender.Male)
                {
                    xrayClient = XRay.RegisterClient(this);
                }
            }
            // if (atom == FillMeUp.atom)
            // {
            //     atom.PrintStorables("hand");
            //     var ctrl = atom.GetStorableByID("RightHandControl") as HandControl;
            //     var fingerCtrl = atom.GetStorableByID("RightHandFingerControl") as HandOutput;
            //     fingerCtrl.indexProximalBendJSON.val = 20f;
            //     ctrl.fingerControlModeJSON.val = "JSONParams";
            // }
            // lCarpalBones.ToList().ForEach(x => x.name.Print());
            // if (atom != FillMeUp.atom)
            // {
            //     enabled = true;
            //     this.fuckable = FillMeUp.anus;
            // }
            return this;
        }

        private IEnumerator RefreshGaze()
        {
            yield return null;
            if (!atom.gameObject.activeSelf)
            {
                "RefreshGaze atom disabled".Print();
                yield break;
            }
            if(atom != FillMeUp.atom)
            {
                var enabled = gaze.enabledJ.val;
                Gaze.DeregisterAtom(atom);
                gaze = new Gaze(penetrator.atom);
                penisGazeTarget = Gaze.RegisterPerson(this, gaze);
                yield return new WaitUntil(() => gaze.initialized);
                gaze.enabledJ.val = enabled;
            }
        }

        private bool midIn;
        // private Vector3 bendTorque;

        // private void FixedUpdate()
        // {
        //     if (!(penetrator?.fuckable is Throat)) return;
        //     var cross = -10000f * Vector3.Cross(penetrator.rigidbodies[2].transform.forward, FillMeUp.neck.up);
        //     penetrator.rigidbodies[2].AddTorque(cross, ForceMode.Acceleration);
        // }

        public void FixedUpdate()
        {
            // if (!(penetrator?.fuckable is Throat)) return;
            if(penetrator == null || !(fuckable is Orifice)) return;
            var orifice = fuckable as Orifice;
            if (orifice is Throat)
            {
                Soften(tipCtrl, 12f, orifice, -5000f, true);
                Soften(midCtrl, 24f, orifice, -10000f, false, -.02f);
                SyncCollidersBJ();
            }
            else
            {
                Soften(tipCtrl, 12f, orifice, 5000f, true);
                Soften(midCtrl, 24f, orifice, 5000f);
            }
        }
        
        public void SyncColliderBJ(int id)
        {
            CapsuleCollider cap = penetrator.colliders[id] as CapsuleCollider;
            float defaultRadius = autoColliders[id].colliderRadius;
            if (Vector3.Dot(FillMeUp.throat.entrance.transform.up, cap.transform.position - FillMeUp.throat.entrance.transform.position) > .4f*cap.height)
            {
                var targetRadius = fuckable.penetratorScaling.val * defaultRadius;
                if (cap.radius > 1.01f * targetRadius)
                {
                    penetrator.rigidbodies[Math.Min(id, 2)].mass = .02f;
                    cap.radius = Mathf.Lerp(cap.radius, targetRadius, 3f * Time.deltaTime);
                    if(id == 3) cap.height = Mathf.Lerp(cap.height, autoColliders[id].colliderLength * (1f+fuckable.penetratorScaling.val)*.5f, 3f * Time.deltaTime);
                }
            }
            else if(cap.radius != defaultRadius)
            {
                cap.radius = defaultRadius;
                if (id == 3) cap.height = autoColliders[id].colliderLength;
                penetrator.rigidbodies[Math.Min(id, 2)].mass = .2f;
            }
        }
        
        public override void SyncCollidersBJ()
        {
            for (int i = 0; i < penetrator.colliders.Count; i++)
            {
                SyncColliderBJ(i);
            }
        }

        public override void ShrinkColliders(float percent)
        {
            for (int i = 0; i < penetrator.colliders.Count; i++)
            {
                CapsuleCollider cap = penetrator.colliders[i] as CapsuleCollider;
                cap.radius = autoColliders[i].colliderRadius * percent;
            }
            // $"{((CapsuleCollider)colliders[0]).radius} {autoColliders[0].colliderRadius}".Print();
        }
        
        public override void ResetColliders()
        {
            for (int i = 0; i < penetrator.colliders.Count; i++)
            {
                CapsuleCollider cap = penetrator.colliders[i] as CapsuleCollider;
                cap.radius = autoColliders[i].colliderRadius;
                if (i == 3) cap.height = autoColliders[i].colliderLength;
            }

            for (int i = 0; i < 3; i++)
            {
                penetrator.rigidbodies[i].mass = .2f;
            }
        }

        private void Soften(FreeControllerV3 ctrl, float defaultVal, Orifice orifice, float bendFactor, bool dialIn = false, float depth = .02f)
        {
            var rigidbody = ctrl == midCtrl ? penetrator.rigidbodies[1] : penetrator.rigidbodies[2];
            var dot = Vector3.Dot(orifice.entrance.transform.up,
                ctrl.followWhenOff.position - orifice.entrance.transform.position);
            if(dot > depth)
            {
                if (ctrl == midCtrl) midIn = true;
                else if (!midIn) midCtrl.jointRotationDriveSpring = 35f;
                if (!SuperController.singleton.freezeAnimation)
                {
                    // if(ctrl.name == "penisTipControl") dot.Print();
                    var scale = dialIn ? Mathf.Lerp(0f, bendFactor, (dot - .02f) * 5f) : bendFactor;
                    var cross = scale * Vector3.Cross(rigidbody.transform.forward, orifice.guidance.up);
                    rigidbody.AddTorque(cross, ForceMode.Acceleration);
                }
                if (ctrl.jointRotationDriveSpring > 4.1f)
                {
                    ctrl.jointRotationDriveSpring = Mathf.Lerp(ctrl.jointRotationDriveSpring, 4f, 2f*Time.deltaTime);
                }
            }
            else 
            {
                if (ctrl == midCtrl) midIn = false;
                if(ctrl.jointRotationDriveSpring < defaultVal-.1f)
                {
                    ctrl.jointRotationDriveSpring = Mathf.Lerp(ctrl.jointRotationDriveSpring, defaultVal, 2f*Time.deltaTime);
                }
            }
        }

        private static void SetParticleOpacity(float val)
        {
            foreach (var person in FillMeUp.persons)
            {
                var main = person.ps1.main;
                main.startColor = new Color(1f, 0.97f, 0.91f, val);
            }
        }

        private IEnumerator stiffenCo;
        private float stiffenSpeed = 2f;
        private float[] stiffenTargets = new float[3];
        private static WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();
        private bool stiffenCoRunning;
        public override void Stiffen()
        {
            for (int i = 0; i < 3; i++)
            {
                stiffenTargets[i] = stiffenAmount.val * (i == 0? 3f : 1f);
            }
            if(!stiffenCoRunning) stiffenCo = StiffenCo().Start();
            // "Stiffen".Print();
        }
        
        public override void StiffenHalf()
        {
            if (!stiffenEnabled.val) return;
            for (int i = 0; i < 3; i++)
            {
                stiffenTargets[i] = stiffenAmount.val * .5f;
            }
            if(!stiffenCoRunning) stiffenCo = StiffenCo().Start();
            // "StiffenHalf".Print();
        }
        
        public override void StiffenReset()
        {
            if (!stiffenEnabled.val) return;
            stiffenTargets[0] = 25f;
            stiffenTargets[1] = 24f;
            stiffenTargets[2] = 12f;
            if(!stiffenCoRunning) stiffenCo = StiffenCo().Start();
            // "StiffenReset".Print();
        }

        private IEnumerator StiffenCo()
        {
            stiffenCoRunning = true;
            var speed = stiffenSpeed * Time.fixedDeltaTime;
            while (Mathf.Abs(baseCtrl.jointRotationDriveSpring - stiffenTargets[0]) > 1f)
            {
                baseCtrl.jointRotationDriveSpring = Mathf.Lerp(baseCtrl.jointRotationDriveSpring, stiffenTargets[0], speed);
                midCtrl.jointRotationDriveSpring = Mathf.Lerp(midCtrl.jointRotationDriveSpring, stiffenTargets[1], speed);
                tipCtrl.jointRotationDriveSpring = Mathf.Lerp(tipCtrl.jointRotationDriveSpring, stiffenTargets[2], speed);
                yield return waitForFixedUpdate;
            }

            baseCtrl.jointRotationDriveSpring = stiffenTargets[0];
            midCtrl.jointRotationDriveSpring = stiffenTargets[1];
            tipCtrl.jointRotationDriveSpring = stiffenTargets[2];
            stiffenCoRunning = false;
            // "fin".Print();
        }

        public int GetSoftPhysicsState()
        {
            if (!softBreast.detectCollisions && !softGlute.detectCollisions) return 0;
            if (softBreast.detectCollisions && !softGlute.detectCollisions) return 1;
            if (!softBreast.detectCollisions && softGlute.detectCollisions) return 2;
            if (softBreast.detectCollisions && softGlute.detectCollisions) return 3;
            return 0;
        }

        // private float erectionTimer;
        // public override void Update()
        // {
        //     base.Update();
        //     // erectionTimer = Mathf.Lerp(erectionTimer, 1f, .01f * Time.deltaTime);
        //     var timestep = .01f * Time.deltaTime;
        //     baseCtrl.jointRotationDriveSpring = Mathf.Lerp(baseCtrl.jointRotationDriveSpring, 25f, .5f*timestep);
        //     midCtrl.jointRotationDriveSpring = Mathf.Lerp(midCtrl.jointRotationDriveSpring, 24f, .5f*timestep);
        //     tipCtrl.jointRotationDriveSpring = Mathf.Lerp(tipCtrl.jointRotationDriveSpring, 12f, .5f*timestep);
        //
        //     shGirth.morphValue = Mathf.Lerp(shGirth.morphValue, 1f, 10f*timestep);
        //     shLength.morphValue = Mathf.Lerp(shLength.morphValue, .5f, 10f*timestep);
        //     shGirthBase.morphValue = Mathf.Lerp(shGirthBase.morphValue, -.7f, timestep);
        //     erect.morphValue = Mathf.Lerp(erect.morphValue, .4f, timestep);
        // }

        private void OnChangedToFemale()
        {
            if (!isFucking) return;
            fuckable.ResetPenetration();
            var morphsControlUI = dcs.morphsControlUI;
            foreach (var uid in morphsControlUI.GetMorphUids().Where(x => x.EndsWith("AltFuta Vagina Hide.vmi")))
            {
                morphsControlUI.GetMorphByUid(uid).morphValue = 0f;
            }
        }

        private void SyncClothesSettings()
        {
            for (int i = 0; i < cumClothings.Count; i++)
            {
                SyncClothSetting(cumClothings[i]);
            }
        }

        public static void SyncClothSetting(CumClothing clothing)
        {
            clothing.detachTreshold.val = .03f * clothingBreakThreshold.val;
        }

        public void GetCumClothes()
        {
            UnequipClothes();
            cumClothings.Clear();
            foreach (var item in atom.GetComponentsInChildren<DAZClothingItem>(true))
            {
                if (item.uid.StartsWith(FillMeUp.packageUid + $"Custom/Clothing/{dcs.gender.ToString()}/CheesyFX/BodyLanguage/BL_Cum/"))
                {
                    cumClothings.Add(new CumClothing(item, this));
                }
            }
            permamentClothings = cumClothings.Where(x => x.isPermament).ToList();
            // $"{penetrator.name} GetCumClothes".Print();
        }

        private List<CumClothing> availableClothes = new List<CumClothing>();
        private List<CumClothing> permamentClothings;
        // private List<CumClothing> activePermamentClothings = new List<CumClothing>();
        
        private void GetInactiveClothes()
        {
            availableClothes.Clear();
            for (int i = 0; i < cumClothings.Count; i++)
            {
                var item = cumClothings[i];
                if(!item.active) availableClothes.Add(item);
            }
            // inactiveClothings.Count.Print();
        }

        public override void LaunchClothing(float strength)
        {
            try
            {
                LaunchClothingRoutine(strength).Start();
            }
            catch (Exception e)
            {
                SuperController.LogError(e.ToString());
            }
        }

        public IEnumerator LaunchClothingRoutine(float strength)
        {
            var force = GetClothesForce() * strength;
            List<CumClothing> selectedClothings = new List<CumClothing>();
            int count = (int)strength - 1;
            GetInactiveClothes();
            if (availableClothes.Count == 0)
            {
                // "No Shots Left".Print();
                yield break;
            }

            while (count > 0 && availableClothes.Count > 0)
            {
                var item = availableClothes[Random.Range(0, availableClothes.Count)];
                    
                selectedClothings.Add(item);
                // if(item.isPermament) activePermamentClothings.Add(item);
                availableClothes.Remove(item);
                item.ShotStart(force);
                count--;
            }

            float timer = .8f;
            float percent = 0f;
            while (timer > 0f)
            {
                if (timer < .1f)
                {
                    percent = 1f - timer * 10f;
                    for (int i = 0; i < selectedClothings.Count; i++)
                    {
                        selectedClothings[i].BlendAlpha(percent);
                    }
                    // for (int i = 0; i < activePermamentClothings.Count; i++)
                    // {
                    //     activePermamentClothings[i].ShotStart(5f*force);
                    // }
                }
                timer -= Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }
            for (int i = 0; i < selectedClothings.Count; i++)
            {
                selectedClothings[i].BlendAlpha(1f);
            }
            for (int i = 0; i < selectedClothings.Count; i++)
            {
                selectedClothings[i].ShotEnd();
            }
            // for (int i = 0; i < activePermamentClothings.Count; i++)
            // {
            //     activePermamentClothings[i].ShotEnd();
            // }
        }

        public void ResetFingerSprings(float sideId)
        {
            if(sideId < 0)
            {
                for (int i = 0; i < lCarpalBones.Length; i++)
                {
                    lCarpalBones[i].baseJointRotation = Vector3.zero;
                }

                for (int i = 0; i < lHandBones.Length; i++)
                {
                    lHandBones[i].baseJointRotation = Vector3.zero;
                }
            }
            else
            {
                for (int i = 0; i < rCarpalBones.Length; i++)
                {
                    rCarpalBones[i].baseJointRotation = Vector3.zero;
                }

                for (int i = 0; i < rHandBones.Length; i++)
                {
                    rHandBones[i].baseJointRotation = Vector3.zero;
                }
            }
        }

        public void Reset()
        {
            UnequipClothes();
            ForeskiSetActive(false);
        }

        public override void UnequipClothes()
        {
            for (int i = 0; i < cumClothings.Count; i++)
            {
                cumClothings[i].Reset();
            }
        }

        public void ForeskiSetActive(bool val)
        {
            foreskiBase.morphValue = val ? 1f : 0f;
            foreski.morphValue = 0f;
        }

        public void ApplyForeskiBase()
        {
            foreskiBase.morphValue = 1f;
        }
        
        private static void SetCumKey(string val)
        {
            cumKey = (KeyCode)Enum.Parse(typeof(KeyCode), val);
        }
        
        private static void SetCleanKey(string val)
        {
            cleanKey = (KeyCode)Enum.Parse(typeof(KeyCode), val);
        }

        protected override GameObject CreateFluidGO(ref ParticleSystem ps, string name)
        {
            var go = base.CreateFluidGO(ref ps, name);
            go.transform.localPosition = new Vector3(.025f, 0f, 0f);
            go.transform.localEulerAngles = new Vector3(0f, 90f, 0f);
            return go;
        }
        
        public override void OnDestroy()
        {
            base.OnDestroy();
            characterListener.Destroy();
            for (int i = 0; i < cumClothings.Count; i++)
            {
                var item = cumClothings[i];
                item.remove.Stop();
            }
        }
    }
}