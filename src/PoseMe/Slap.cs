using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MacGruber;
using SimpleJSON;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace CheesyFX
{
    public class Slap : MonoBehaviour
    {
        private static WaitForFixedUpdate wait = new WaitForFixedUpdate();
        public Pose pose;
        private float timer;
        private int sideId = -1;
        public Person person;
        private int slapQueue;
        private int rSlapQueue;
        private Rigidbody hand;
        private Rigidbody foreArm;
        private FreeControllerV3 handCtrl;
        private ControllerSetting controllerSetting;
        private bool initialized;
        
        public static RaycastHit[] rayCastBuffer = new RaycastHit[50];
        private List<SlapTarget> targets = new List<SlapTarget>();
        private SlapTarget currentTarget;

        private Torque caressTorqueX;
        private Torque caressTorqueY;
        private Torque caressTorqueZ;
        public Force caressForceZ;
        private Force caressForceX;
        private Force caressForceY;

        public static JSONStorableBool autoLinkHand = new JSONStorableBool("Auto Parent Link Hand", true);
        
        public JSONStorableBool enabledJ = new JSONStorableBool("Enabled", true);
        
        public JSONStorableBool autoSlap = new JSONStorableBool("Auto Slap & Push", true);

        public JSONStorableStringChooser personChooser =
            new JSONStorableStringChooser("personChooser", FillMeUp.persons.Select(x => x.uid).ToList(), "", "Person");

        public JSONStorableStringChooser handChooser =
            new JSONStorableStringChooser("handChooser", new List<string> { "Left", "Right" }, "Left", "Hand");

        public JSONStorableStringChooser targetChooser = new JSONStorableStringChooser("targetChooser", new List<string>(), null, "target");
        
        private JSONStorableFloat slapIntensityMean = new JSONStorableFloat("Slap Intensity Mean", 1f, 0f, 3f, false);
        private JSONStorableFloat slapIntensityDelta = new JSONStorableFloat("Slap Intensity Delta", .5f, 0f, 1f, false);
        private JSONStorableBool slapIntensityOneSided = new JSONStorableBool("Slap Intensity OneSided", false);

        private JSONStorableFloat occurenceMean = new JSONStorableFloat("Occurence Mean", 6f, 0f, 20f, false);
        private JSONStorableFloat occurenceDelta = new JSONStorableFloat("Occurence Delta", 5f, 0f, 20f, false);
        private JSONStorableBool occurenceOneSided = new JSONStorableBool("Occurence OneSided", false);
        
        private JSONStorableFloat backwardsMean = new JSONStorableFloat("Slap X Mean", 0f, -3f, 3f, false);
        private JSONStorableFloat backwardsDelta = new JSONStorableFloat("Slap X Delta", 0f, 0f, 1f, false);
        private JSONStorableBool backwardsOneSided = new JSONStorableBool("Slap X OneSided", false);
        
        private JSONStorableFloat sidewaysMean = new JSONStorableFloat("Slap Z Mean", 0f, -3f, 3f, false);
        private JSONStorableFloat sidewaysDelta = new JSONStorableFloat("Slap Z Delta", 0f, 0f, 2f, false);
        private JSONStorableBool sidewaysOneSided = new JSONStorableBool("Slap Z OneSided", false);
        
        private JSONStorableFloat backSlapChance = new JSONStorableFloat("BackSlap Chance", 0f, 0f, 1f);
        
        private JSONStorableFloat pushForceIntensityMean = new JSONStorableFloat("Push Force Intensity Mean", 1f, 0f, 3f, false);
        private JSONStorableFloat pushForceIntensityDelta = new JSONStorableFloat("Push Force Intensity Delta", .5f, 0f, 1f, false);
        private JSONStorableBool pushForceIntensityOneSided = new JSONStorableBool("Push Force Intensity OneSided", false);
        
        private JSONStorableFloat pushTorqueIntensityMean = new JSONStorableFloat("Push Torque Intensity Mean", 1f, 0f, 3f, false);
        private JSONStorableFloat pushTorqueIntensityDelta = new JSONStorableFloat("Push Torque Intensity Delta", .5f, 0f, 1f, false);
        private JSONStorableBool pushTorqueIntensityOneSided = new JSONStorableBool("Push Torque Intensity OneSided", false);
        
        private JSONStorableFloat pushDurationMean = new JSONStorableFloat("Push Duration Mean", 3f, 1f, 10f, false);
        private JSONStorableFloat pushDurationDelta = new JSONStorableFloat("Push Duration Delta", 2f, 0f, 10f, false);
        private JSONStorableBool pushDurationOneSided = new JSONStorableBool("Push Duration OneSided", false);
        
        private JSONStorableFloat pushXMean = new JSONStorableFloat("Push X Mean", 0f, -3f, 3f, false);
        private JSONStorableFloat pushXDelta = new JSONStorableFloat("Push X Delta", 3f, 0f, 3f, false);
        private JSONStorableBool pushXOneSided = new JSONStorableBool("Push X OneSided", false);
        
        private JSONStorableFloat pushZMean = new JSONStorableFloat("Push Z Mean", 0f, -3f, 3f, false);
        private JSONStorableFloat pushZDelta = new JSONStorableFloat("Push Z Delta", 3f, 0f, 3f, false);
        private JSONStorableBool pushZOneSided = new JSONStorableBool("Push Z OneSided", false);
        
        private JSONStorableFloat pushChance = new JSONStorableFloat("Push Chance", .5f, 0f, 1f);
        
        private JSONStorableFloat multiSlaps = new JSONStorableFloat("Multi Slaps", 3f, 1f, 5f, false);
        
        
        public JSONStorableFloat targetIntensity = new JSONStorableFloat("Intensity Multiplier", 1f, 0f, 4f);
        public JSONStorableBool targetAllowSlaps = new JSONStorableBool("Allow Slaps", true);
        public JSONStorableBool targetAllowPushes = new JSONStorableBool("Allow Pushes", true);

        public static bool configureUIOpen;
        public static bool targetUIOpen;
        private static List<object> UIElements = new List<object>();
        private static UIDynamicTabBar tabbar;
        private static int lastTabId;

        private FreeControllerV3.PositionState cachedPositionState;
        private FreeControllerV3.RotationState cachedRotationState;
        private Rigidbody cachedLinkToRB;

        private static JSONStorableString generalInfo = new JSONStorableString("", 
            "Warning: All targets will be lost after changing the person or hand. A new one will be generated at the current position.\nI recommend parent linking the hand controller to the closest body part of the target person (not the controller) to ensure a stable rest position.");
        private static JSONStorableString slapsInfo = new JSONStorableString("", 
            "Backwards and Sideways defines the direction of the back swing. Zero means straight up (when not switching target).\nA back slap has reversed direction, slapping with the back of the hand. To allow both you need proper hand/target placement. A backslap can only occur if the action is not a push.");
        private static JSONStorableString pushesInfo = new JSONStorableString("", 
            "A push presses the palm forward without a back swing. Increase the <b>Push Chance</b> to allow slaps to be performed as a push.\nTo set up caressing strokes to nearby targets set Push Chance to 1 and lower the force and torque as needed. The targets have to be rather close.");
        private static JSONStorableString caressInfo = new JSONStorableString("", 
            "Caressing movements are periodic and defined by forces. They are played in between the slaps or pushes.");
        private static JSONStorableString targetInfo = new JSONStorableString("", 
            "A slap component can have multiple targets. One of these is selected when a slap action is played. The hand will lift from the current position and try to slap the selected target. After that it will go back to the original position.\nTo add a target place the hand where you want it to land. Then press 'Add target'. After that I recommend reloading the pose. The new target will be parented to the person/object you want to slap and move with it.");

        public JSONClass Store()
        {
            var jc = new JSONClass();
            enabledJ.Store(jc, false);
            autoSlap.Store(jc, false);
            autoLinkHand.Store(jc, false);
            personChooser.Store(jc);
            handChooser.Store(jc);
            
            slapIntensityMean.Store(jc, false);
            slapIntensityDelta.Store(jc, false);
            slapIntensityOneSided.Store(jc, false);

            occurenceMean.Store(jc, false);
            occurenceDelta.Store(jc, false);
            occurenceOneSided.Store(jc, false);
            
            backwardsMean.Store(jc, false);
            backwardsDelta.Store(jc, false);
            backwardsOneSided.Store(jc, false);
            
            sidewaysMean.Store(jc, false);
            sidewaysDelta.Store(jc, false);
            sidewaysOneSided.Store(jc, false);
            
            backSlapChance.Store(jc, false);
            multiSlaps.Store(jc, false);


            pushChance.Store(jc, false);
            
            pushForceIntensityMean.Store(jc, false);
            pushForceIntensityDelta.Store(jc, false);
            pushForceIntensityOneSided.Store(jc, false);
            
            pushTorqueIntensityMean.Store(jc, false);
            pushTorqueIntensityDelta.Store(jc, false);
            pushTorqueIntensityOneSided.Store(jc, false);
            
            pushDurationMean.Store(jc, false);
            pushDurationDelta.Store(jc, false);
            pushDurationOneSided.Store(jc, false);
            
            pushXMean.Store(jc, false);
            pushXDelta.Store(jc, false);
            pushXOneSided.Store(jc, false);
            
            pushZMean.Store(jc, false);
            pushZDelta.Store(jc, false);
            pushZOneSided.Store(jc, false);

            jc[caressTorqueX.name] = caressTorqueY.Store(false);
            jc[caressTorqueY.name] = caressTorqueY.Store(false);
            jc[caressTorqueZ.name] = caressTorqueY.Store(false);
            jc[caressForceZ.name] = caressForceZ.Store(false);
            jc[caressForceX.name] = caressForceX.Store(false);
            jc[caressForceY.name] = caressForceY.Store(false);

            JSONArray ja = new JSONArray();
            for (int i = 0; i < targets.Count; i++)
            {
                ja.Add(targets[i].Store());
            }
            jc["targets"] = ja;
            
            return jc;
        }

        public void Load(JSONClass jc)
        {
            enabledJ.Load(jc, true);
            autoSlap.Load(jc, true);
            autoLinkHand.Load(jc, true);
            personChooser.valNoCallback = jc[personChooser.name].Value;
            handChooser.valNoCallback = jc[handChooser.name].Value;
            SetReceiver(addTarget:false);
            
            slapIntensityMean.Load(jc, true);
            slapIntensityDelta.Load(jc, true);
            slapIntensityOneSided.Load(jc, true);
            
            pushForceIntensityMean.Load(jc, true);
            pushForceIntensityDelta.Load(jc, true);
            pushForceIntensityOneSided.Load(jc, true);
            
            occurenceMean.Load(jc, true);
            occurenceDelta.Load(jc, true);
            occurenceOneSided.Load(jc, true);
            
            backwardsMean.Load(jc, true);
            backwardsDelta.Load(jc, true);
            backwardsOneSided.Load(jc, true);
            
            sidewaysMean.Load(jc, true);
            sidewaysDelta.Load(jc, true);
            sidewaysOneSided.Load(jc, true);
            
            backSlapChance.Load(jc, true);
            multiSlaps.Load(jc, true);
            
            
            pushChance.Load(jc, true);
            
            pushForceIntensityMean.Load(jc, true);
            pushForceIntensityDelta.Load(jc, true);
            pushForceIntensityOneSided.Load(jc, true);
            
            pushTorqueIntensityMean.Load(jc, true);
            pushTorqueIntensityDelta.Load(jc, true);
            pushTorqueIntensityOneSided.Load(jc, true);
            
            pushDurationMean.Load(jc, true);
            pushDurationDelta.Load(jc, true);
            pushDurationOneSided.Load(jc, true);
            
            pushXMean.Load(jc, true);
            pushXDelta.Load(jc, true);
            pushXOneSided.Load(jc, true);
            
            pushZMean.Load(jc, true);
            pushZDelta.Load(jc, true);
            pushZOneSided.Load(jc, true);
            
            caressTorqueX.Load(jc[caressTorqueX.name].AsObject);
            caressTorqueY.Load(jc[caressTorqueY.name].AsObject);
            caressTorqueZ.Load(jc[caressTorqueZ.name].AsObject);
            caressForceZ.Load(jc[caressForceZ.name].AsObject);
            caressForceX.Load(jc[caressForceX.name].AsObject);
            caressForceY.Load(jc[caressForceY.name].AsObject);
            foreach (var item in jc["targets"].Childs)
            {
                var target = new SlapTarget(hand, item.AsObject);
                targets.Add(target);
                var choices = targetChooser.choices;
                choices.Add((targets.Count - 1).ToString());
                targetChooser.choices = null;
                targetChooser.choices = choices;
            }
            // targetChooser.setCallbackFunction += val =>
            // {
            //     int i = 0;
            //     if(!int.TryParse(val, out i)) return;
            //     for (int j = 0; j < targets.Count; j++)
            //     {
            //         var target = targets[i];
            //         if(j == i) target.transform.SetDebugWidth(5f);
            //         else target.transform.SetDebugWidth(1f);
            //     }
            // };
            if (targets.Count > 0) targetChooser.valNoCallback = "0";
            timer = NormalDistribution.GetValue(occurenceMean.val, occurenceDelta.val, 1f, occurenceOneSided.val);
        }

        private void OnEnable()
        {
            slapQueue = 0;
            if(!initialized) return;
            ToggleCaress(true);
        }

        private void OnDisable()
        {
            try
            {
                ToggleCaress(false);
                invokeCo.Stop();
                isSlapping = false;
            }
            catch (Exception e)
            {
                SuperController.LogError(e.ToString());
            }
        }

        private void OnDestroy()
        {
            Destroy(caressTorqueX);
            Destroy(caressTorqueY);
            Destroy(caressTorqueZ);
            Destroy(caressForceZ);
            Destroy(caressForceX);
            Destroy(caressForceY);
            for (int i = 0; i < targets.Count; i++)
            {
                targets[i].Destroy();
            }
            invokeCo.Stop();
        }

        public void Toggle(bool enable)
        {
            var val = enable && enabledJ.val;
            ToggleCaress(val);
            enabled = val;
        }

        public void ToggleCaress(bool enable)
        {
            caressTorqueX.enabled = enable && caressTorqueX.enabledJ.val;
            caressTorqueY.enabled = enable && caressTorqueY.enabledJ.val;
            caressTorqueZ.enabled = enable && caressTorqueZ.enabledJ.val;
            caressForceZ.enabled = enable && caressForceZ.enabledJ.val;
            caressForceX.enabled = enable && caressForceX.enabledJ.val;
            caressForceY.enabled = enable && caressForceY.enabledJ.val;
        }

        public Slap Init(Pose pose, JSONClass jc = null)
        {
            this.pose = pose;
            controllerSetting = new ControllerSetting();
            personChooser.setCallbackFunction += val => SetReceiver();
            handChooser.setCallbackFunction += val => SetReceiver();
            // personChooser.val = PoseMe.persons[1].atom.uid;
            // this.person = person;
            if(PoseMe.persons.Count > 1) person = PoseMe.persons.FirstOrDefault(x => x.atom != PoseMe.atom);
            else person = PoseMe.persons[0];
            
            enabledJ.setCallbackFunction += val => enabled = val;
            
            caressTorqueX = gameObject.AddComponent<Torque>().Init("CaressTorque X", hand, () => hand.transform.right);
            caressTorqueX.amplitude.mean.SetWithDefault(5f);
            caressTorqueX.amplitude.delta.SetWithDefault(3f);
            caressTorqueX.period.mean.SetWithDefault(5f);
            caressTorqueX.period.delta.SetWithDefault(3f);

            caressTorqueY = gameObject.AddComponent<Torque>().Init("CaressTorque Y", hand, () => hand.transform.up);
            caressTorqueY.amplitude.mean.SetWithDefault(8f);
            caressTorqueY.amplitude.delta.SetWithDefault(6f);
            caressTorqueY.period.mean.SetWithDefault(3f);
            caressTorqueY.period.delta.SetWithDefault(1f);
            
            caressTorqueZ = gameObject.AddComponent<Torque>().Init("CaressTorque Z", hand, () => hand.transform.forward);
            caressTorqueZ.amplitude.mean.SetWithDefault(3f);
            caressTorqueZ.amplitude.delta.SetWithDefault(2f);
            caressTorqueZ.period.mean.SetWithDefault(5f);
            caressTorqueZ.period.delta.SetWithDefault(3f);
            
            caressForceZ = gameObject.AddComponent<Force>().Init("CaressForce Z", hand, () => hand.transform.forward);
            caressForceZ.amplitude.mean.SetWithDefault(30f);
            caressForceZ.amplitude.delta.SetWithDefault(30f);
            caressForceZ.quickness.mean.SetWithDefault(3f);
            caressForceZ.quickness.delta.SetWithDefault(1f);
            caressForceZ.period.mean.SetWithDefault(2f);
            caressForceZ.period.delta.SetWithDefault(.5f);
            caressForceZ.periodRatio.delta.SetWithDefault(.1f);
            
            caressForceX = gameObject.AddComponent<Force>().Init("CaressForce X", hand, () => hand.transform.right);
            caressForceX.amplitude.mean.SetWithDefault(30f);
            caressForceX.amplitude.delta.SetWithDefault(30f);
            caressForceX.quickness.mean.SetWithDefault(3f);
            caressForceX.quickness.delta.SetWithDefault(1f);
            caressForceX.period.mean.SetWithDefault(2f);
            caressForceX.period.delta.SetWithDefault(.5f);
            caressForceX.periodRatio.delta.SetWithDefault(.1f);
            
            caressForceY = gameObject.AddComponent<Force>().Init("CaressForce Y", hand, () => -hand.transform.up);
            caressForceY.amplitude.mean.SetWithDefault(30f);
            caressForceY.amplitude.delta.SetWithDefault(30f);
            caressForceY.quickness.mean.SetWithDefault(3f);
            caressForceY.quickness.delta.SetWithDefault(1f);
            caressForceY.period.mean.SetWithDefault(2f);
            caressForceY.period.delta.SetWithDefault(1.2f);
            caressForceY.periodRatio.delta.SetWithDefault(.2f);
            // caressForcePush.enabledJ.SetWithDefault(false);

            caressTorqueX.enabledJ.setCallbackFunction += val => caressTorqueX.SetActive(enabled && val);
            caressTorqueY.enabledJ.setCallbackFunction += val => caressTorqueY.SetActive(enabled && val);
            caressTorqueZ.enabledJ.setCallbackFunction += val => caressTorqueZ.SetActive(enabled && val);
            caressForceZ.enabledJ.setCallbackFunction += val => caressForceZ.SetActive(enabled && val);
            caressForceX.enabledJ.setCallbackFunction += val => caressForceX.SetActive(enabled && val);
            caressForceY.enabledJ.setCallbackFunction += val => caressForceY.SetActive(enabled && val);
            caressTorqueX.enabled = true;
            caressTorqueY.enabled = true;
            caressTorqueZ.enabled = true;
            caressForceZ.enabled = true;
            caressForceX.enabled = true;
            caressForceY.enabled = false;
            if (jc == null)
            {
                SetReceiver(person, true);
            }
            else
            {
                Load(jc);
            }
            caressTorqueX.presetSystem.script = PoseMe.singleton;
            caressTorqueY.presetSystem.script = PoseMe.singleton;
            caressTorqueZ.presetSystem.script = PoseMe.singleton;
            caressForceZ.presetSystem.script = PoseMe.singleton;
            caressForceX.presetSystem.script = PoseMe.singleton;
            caressForceY.presetSystem.script = PoseMe.singleton;

            targetChooser.setCallbackFunction += OnTargetSelected;
            initialized = true;
            // var collider = PoseMe.atom.GetComponentInChildren<CapsuleCollider>();
            // enabled = false;
            return this;
        }
        
        private void SetReceiver(Person person = null, bool addTarget = true)
        {
            Debug.Clear();
            invokeCo.Stop();
            isSlapping = false;
            if(person == null) person = FillMeUp.persons.FirstOrDefault(x => x.atom.uid == personChooser.val);
            else personChooser.valNoCallback = person.atom.uid;
            if (person == null) return;
            if (handChooser.val.StartsWith("L"))
            {
                hand = person.lHand;
                foreArm = person.lForeArm;
                sideId = -1;
            }
            else
            {
                hand = person.rHand;
                foreArm = person.rForeArm;
                sideId = 1;
            }
            caressTorqueX.rb = caressTorqueY.rb = caressTorqueZ.rb = caressForceZ.rb = caressForceX.rb = caressForceY.rb = hand;
            this.person = person;
            // person.atom.freeControllers.ToList().ForEach(x => x.name.Print());
            string ctrlName = $"{hand.name}Control";
            if (initialized)
            {
                controllerSetting.Restore(handCtrl);
            }
            handCtrl = person.atom.freeControllers.FirstOrDefault(x => x.name == ctrlName);
            controllerSetting.Store(handCtrl);
            for (int i = 0; i < targets.Count; i++)
            {
                targets[i].Destroy();
            }
            targets.Clear();
            targetChooser.choices.Clear();
            targetChooser.SyncChoices();
            if (addTarget)
            {
                var target = AddTarget();
                LinkHandControl();
                // string region;
                // if (target.hasTarget && BodyRegionMapping.bodyRegionByRigidbodyName.TryGetValue(target.transform.parent.name, out region) && region.Contains("Breast"))
                // {
                //     pushChance.val = .5f;
                // }
            }
        }

        public void CreateConfigureUI()
        {
            PoseMe.singleton.ClearUI();
            // UIElements.Clear();
            var button = PoseMe.singleton.CreateButton("Return");
            button.buttonColor = PoseMe.navColor;
            button.button.onClick.AddListener(CloseConfigureUI);
            enabledJ.CreateUI(rightSide: true);
            

            tabbar = UIManager.CreateTabBar(new[] { "General", "Slaps", "Pushes", "Caress", "Targets" }, SelectTab);
            if(targets[0].hasTarget)
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    targets[i].transform?.Draw();
                }
            }
            configureUIOpen = true;
            tabbar.SelectTab(lastTabId);
        }

        private void SelectTab(int id)
        {
            PoseMe.singleton.RemoveUIElements(UIElements);
            SuperController.singleton.SetFreezeAnimation(false);
            for (int i = 0; i < PoseMe.currentPose.slaps.Count; i++)
            {
                PoseMe.currentPose.slaps[i].Toggle(true);
            }
            targetUIOpen = false;
            lastTabId = id;
            switch (id)
            {
                case 0:
                {
                    CreateGeneralUI();
                    break;
                }
                case 1:
                {
                    CreateSlapUI();
                    break;
                }
                case 2:
                {
                    CreatePushUI();
                    break;
                }
                case 3:
                {
                    CreateCaressUI();
                    break;
                }
                case 4:
                {
                    CreateTargetUI();
                    SuperController.singleton.SetFreezeAnimation(true);
                    for (int i = 0; i < PoseMe.currentPose.slaps.Count; i++)
                    {
                        PoseMe.currentPose.slaps[i].Toggle(false);
                    }
                    targetUIOpen = true;
                    break;
                }
            }
        }

        private void CreateGeneralUI()
        {
            personChooser.CreateUI(UIElements);
            personChooser.choices = FillMeUp.persons.Select(x => x.uid).ToList();
            handChooser.CreateUI(UIElements, rightSide:true);
            
            occurenceMean.CreateUI(UIElements);
            occurenceDelta.CreateUI(UIElements, true);
            occurenceOneSided.CreateUI(UIElements, true);
            multiSlaps.CreateUI(UIElements);
            multiSlaps.slider.wholeNumbers = true;
            
            PoseMe.singleton.SetupButton("Parent Link Hand Controller", false, LinkHandControl, UIElements);

            autoSlap.CreateUI(UIElements);

            PoseMe.singleton.SetupButton("Invoke", false, () => Invoke(forced:true), UIElements);

            var tf = PoseMe.singleton.CreateTextField(slapsInfo, true);
            tf.ForceHeight(300f);
            UIElements.Add(tf);
        }

        private void CreateSlapUI()
        {
            slapIntensityMean.CreateUI(UIElements);
            slapIntensityDelta.CreateUI(UIElements, true);
            backwardsMean.CreateUI(UIElements);
            backwardsDelta.CreateUI(UIElements, true);
            sidewaysMean.CreateUI(UIElements);
            sidewaysDelta.CreateUI(UIElements, true);
            slapIntensityOneSided.CreateUI(UIElements, true);
            backwardsOneSided.CreateUI(UIElements, true);
            sidewaysOneSided.CreateUI(UIElements, true);
            backSlapChance.CreateUI(UIElements);
            PoseMe.singleton.SetupButton("Invoke Slap", false, () => Invoke(1, true), UIElements);
            PoseMe.singleton.SetupButton("Invoke Backslap", false, () => Invoke(2, true), UIElements);
            var tf = PoseMe.singleton.CreateTextField(slapsInfo, true);
            tf.ForceHeight(300f);
            UIElements.Add(tf);
        }

        private void CreatePushUI()
        {
            pushForceIntensityMean.CreateUI(UIElements);
            pushForceIntensityDelta.CreateUI(UIElements, true);
            pushTorqueIntensityMean.CreateUI(UIElements);
            pushTorqueIntensityDelta.CreateUI(UIElements, true);
            pushDurationMean.CreateUI(UIElements);
            pushDurationDelta.CreateUI(UIElements, true);
            pushXMean.CreateUI(UIElements);
            pushXDelta.CreateUI(UIElements, true);
            pushZMean.CreateUI(UIElements);
            pushZDelta.CreateUI(UIElements, true);
            pushForceIntensityOneSided.CreateUI(UIElements, true);
            pushTorqueIntensityOneSided.CreateUI(UIElements, true);
            pushXOneSided.CreateUI(UIElements, true);
            pushZOneSided.CreateUI(UIElements, true);
            pushDurationOneSided.CreateUI(UIElements, true);
            pushChance.CreateUI(UIElements);
            PoseMe.singleton.SetupButton("Invoke Push", false, () => Invoke(-1, true), UIElements);
            var tf = PoseMe.singleton.CreateTextField(pushesInfo, true);
            tf.ForceHeight(300f);
            UIElements.Add(tf);
        }

        private void CreateCaressUI()
        {
            caressTorqueX.enabledJ.CreateUI(UIElements);
            PoseMe.singleton.SetupButton("Configure Caress Torque X", true, () => CreateForceUINewPage(caressTorqueX), UIElements);
            caressTorqueY.enabledJ.CreateUI(UIElements);
            PoseMe.singleton.SetupButton("Configure Caress Torque Y", true, () => CreateForceUINewPage(caressTorqueY), UIElements);
            caressTorqueZ.enabledJ.CreateUI(UIElements);
            PoseMe.singleton.SetupButton("Configure Caress Torque Z", true, () => CreateForceUINewPage(caressTorqueZ), UIElements);
            
            caressForceX.enabledJ.CreateUI(UIElements);
            PoseMe.singleton.SetupButton("Configure Caress Force X", true, () => CreateForceUINewPage(caressForceX), UIElements);
            caressForceY.enabledJ.CreateUI(UIElements);
            PoseMe.singleton.SetupButton("Configure Caress Force Y", true, () => CreateForceUINewPage(caressForceY), UIElements);
            caressForceZ.enabledJ.CreateUI(UIElements);
            PoseMe.singleton.SetupButton("Configure Caress Force Z", true, () => CreateForceUINewPage(caressForceZ), UIElements);
            UIElements.Add(PoseMe.singleton.CreateTextField(caressInfo, true));
        }

        private void CreateTargetUI()
        {
            var chooser = targetChooser.CreateUI(rightSide:true, UIElements:UIElements, chooserType:0);
            chooser.ForceHeight(50f);
            PoseMe.singleton.SetupButton("Add Target", false, () => AddTarget(), UIElements);
            PoseMe.singleton.SetupButton("Update Target", false, UpdateTarget, UIElements);
            PoseMe.singleton.SetupButton("Remove Target", false, RemoveTarget, UIElements);
            PoseMe.singleton.SetupButton("Go To Target", false, HandToTarget, UIElements);

            targetIntensity.CreateUI(UIElements);
            targetAllowSlaps.CreateUI(UIElements);
            targetAllowPushes.CreateUI(UIElements);
            
            var tf = PoseMe.singleton.CreateTextField(targetInfo, true);
            tf.ForceHeight(500f);
            UIElements.Add(tf);
        }

        private void OnTargetSelected(string val)
        {
            int id;
            if(!int.TryParse(val, out id)) return;
            var target = targets[id];
            targetIntensity.setCallbackFunction = v => target.intensityMultiplier.val = v;
            targetAllowSlaps.setCallbackFunction = v => target.allowSlaps.val = v;
            targetAllowPushes.setCallbackFunction = v => target.allowPushes.val = v;
            targetIntensity.valNoCallback = target.intensityMultiplier.val;
            targetAllowSlaps.valNoCallback = target.allowSlaps.val;
            targetAllowPushes.valNoCallback = target.allowPushes.val;
        }

        private void HandToTarget()
        {
            int id;
            if(!int.TryParse(targetChooser.val, out id) || !targets[id].hasTarget) return;
            var posState = handCtrl.currentPositionState;
            var rotState = handCtrl.currentRotationState;
            handCtrl.currentPositionState = FreeControllerV3.PositionState.On;
            handCtrl.currentRotationState = FreeControllerV3.RotationState.On;
            var target = targets[id];
            handCtrl.transform.position = target.transform.TransformPoint(target.controllerPos);
            handCtrl.transform.rotation = target.transform.rotation * target.controllerRot;
            // var transform = targets[id].controllerTarget.transform;
            // handCtrl.transform.position = transform.position;
            // handCtrl.transform.rotation = transform.rotation;
            handCtrl.currentPositionState = posState;
            handCtrl.currentRotationState = rotState;
        }

        public void LinkHandControl()
        {
            try
            {
                if (targets.Count == 0 || !targets[0].hasTarget || !autoLinkHand.val) return;
                controllerSetting.Store(handCtrl);
                var parent = targets[0].transform.parent;
                if(handCtrl.linkToAtomSelectionPopup == null)
                {
                    SuperController.singleton.SelectController(handCtrl);
                    SuperController.singleton.SelectController(PoseMe.atom.mainController);
                }
                handCtrl.linkToAtomSelectionPopup.currentValue = "None";
                handCtrl.linkToAtomSelectionPopup.currentValue = targets[0].targetAtom.uid;
                var parentRegion = GetParentRigidbody(parent, handCtrl.linkToSelectionPopup.popupValues);
                if(parentRegion == null) return;
                handCtrl.currentPositionState = FreeControllerV3.PositionState.ParentLink;
                handCtrl.currentRotationState = FreeControllerV3.RotationState.ParentLink;
                handCtrl.linkToSelectionPopup.currentValue = parentRegion;
            }
            catch (Exception e)
            {
                SuperController.LogError(e.ToString());
            }
        }

        private class ControllerSetting
        {
            private FreeControllerV3.PositionState pState;
            private FreeControllerV3.RotationState rState;
            private string linkToAtomUid = "None";
            private string linkToSelection = "None";

            public void Store(FreeControllerV3 ctrl)
            {
                pState = ctrl.currentPositionState;
                rState = ctrl.currentRotationState;
                if(ctrl.linkToRB)
                {
                    linkToAtomUid = ctrl.linkToRB.GetAtom().uid;
                    linkToSelection = ctrl.linkToRB.name;
                }
                else
                {
                    linkToAtomUid = linkToSelection = "None";
                }
            }

            public void Restore(FreeControllerV3 ctrl)
            {
                ctrl.currentPositionState = pState;
                ctrl.currentRotationState = rState;
                if(ctrl.linkToAtomSelectionPopup == null)
                {
                    SuperController.singleton.SelectController(ctrl);
                    SuperController.singleton.SelectController(PoseMe.atom.mainController);
                }
                ctrl.linkToAtomSelectionPopup.currentValue = "None";
                ctrl.linkToAtomSelectionPopup.currentValue = linkToAtomUid;
                ctrl.linkToSelectionPopup.currentValue = linkToSelection;
            }
        }

        private string GetParentRigidbody(Transform t, string[] choices)
        {
            if (choices == null)
            {
                SuperController.LogError("Choices is null");
                return null;
            }
            else if (choices.Length == 1)
            {
                SuperController.LogError("Choices length is one (None).");
                return null;
            }
            if (t == null)
            {
                SuperController.LogError("No RB hit");
                return null;
            }
            string choice;
            Transform parent = t;
            while (parent != null)
            {
                choice = choices.FirstOrDefault(x => x == parent.name);
                if (choice != null)
                {
                    $"{hand.gameObject.GetAtom().uid}:{hand.name}Control parent linked to {t.gameObject.GetAtom().uid}:{choice}".Print();
                    return choice;
                }
                parent = parent.parent;
            }
            string region;
            if (!BodyRegionMapping.bodyRegionByRigidbodyName.TryGetValue(t.name, out region))
            {
                SuperController.LogError($"BodyLanguage: Auto parenting failed. Region not found for {t.name}. Please contact the author.");
                return null;
            }

            if (region == null)
            {
                SuperController.LogError($"region is null for {t.name}");
                return null;
            }
            region = MapRegion(region);
            
            choice = choices.FirstOrDefault(x => x.ToLower() == region.ToLower());
            if (choice == null)
            {
                SuperController.LogError($"BodyLanguage: Auto parenting failed. Parent region not found for {t.name}/{region}. Please contact the author.");
                return null;
            }
            $"{hand.gameObject.GetAtom().uid}:{hand.name}Control parent linked to {t.gameObject.GetAtom().uid}:{choice}".Print();
            return choice;
        }

        private string MapRegion(string region)
        {
            if(region.Contains("Glutes")) 
                return region.Replace("Glutes", "Glute");
            if (region.Contains("Breast") || region.Contains("Areola") || region.Contains("Nipple"))
                return "chest";
            return region;
        }
        
        private SlapTarget AddTarget()
        {
            currentTarget = new SlapTarget(handCtrl);
            targets.Add(currentTarget);
            var choices = targetChooser.choices;
            choices.Add((targets.Count - 1).ToString());
            targetChooser.choices = null;
            targetChooser.choices = choices;
            targetChooser.val = (targets.Count - 1).ToString();
            return currentTarget;
        }
        
        private void UpdateTarget()
        {
            int id;
            if (!int.TryParse(targetChooser.val, out id)) return;
            var target = targets[id];
            target.Destroy();
            targets[id] = new SlapTarget(handCtrl);
        }

        public void Sync()
        {
            for (int i = 0; i < targets.Count; i++)
            {
                targets[i].Sync();
            }
        }

        private void RemoveTarget()
        {
            int id;
            if (!int.TryParse(targetChooser.val, out id)) return;
            var target = targets[id];
            target.Destroy();
            targets.Remove(target);
            var choices = targetChooser.choices;
            choices.Clear();
            for (int i = 0; i < targets.Count; i++)
            {
                choices.Add(i.ToString());
            }
            targetChooser.choices = null;
            targetChooser.choices = choices;
        }

        public virtual void CreateForceUINewPage(Force force)
        {
            PoseMe.singleton.ClearUI();
            var button = PoseMe.singleton.CreateButton("Return");
            button.buttonColor = new Color(0.55f, 0.90f, 1f);
            button.button.onClick.AddListener(() =>
            {
                PoseMe.singleton.ClearUI();
                CreateConfigureUI();
                // PoseMe.singleton.settingsTabbar.SelectTab(4);
                force.paramControl.UIOpen = false;
            });
            Utils.SetupInfoTextNoScroll(PoseMe.singleton, new JSONStorableString("", force.name), 50f, true);
            // UIElements.Add(tf);
            // PoseMe.forceChooser.valNoCallback = force.name;
            // force.presetSystem.CreateUI();
            force.paramControl.CreateUI(PoseMe.singleton);
        }

        public void RegisterUid(UIDynamicSlapItem uid)
        {
            uid.configureButtonText.text = person.uid;
            uid.activeToggle.isOn = enabledJ.val;
            uid.sideToggle.isOn = sideId == -1;
            // this.uid = uid;
        }
        
        public static void CloseConfigureUI()
        {
            // if(!PoseMe.singleton.UITransform.gameObject.activeSelf) return;
            configureUIOpen = false;
            PoseMe.singleton.ClearUI();
            if (PoseMe.singleton.UITransform.gameObject.activeSelf) PoseMe.singleton.CreateUI();
            else PoseMe.needsUIRefresh = true;
            if(targetUIOpen) SuperController.singleton.SetFreezeAnimation(false);
            for (int i = 0; i < PoseMe.currentPose.slaps.Count; i++)
            {
                PoseMe.currentPose.slaps[i].Toggle(true);
            }
            Debug.Clear();
        }

        public void Update()
        {
            // hand.NullCheck();
            if(isSlapping || !autoSlap.val || SuperController.singleton.freezeAnimation) return;
            timer -= Time.deltaTime;
            if (timer < 0f)
            {
                timer = NormalDistribution.GetValue(occurenceMean.val, occurenceDelta.val, 1f, occurenceOneSided.val);
                Invoke();
            }
        }

        public void Invoke(int type = 0, bool forced = false)
        {
            try
            {
                if (forced)
                {
                    invokeCo.Stop();
                    isSlapping = false;
                }
                else if(isSlapping) return;
                var target = targets[Random.Range(0, targets.Count)];
                if(target.hasTarget)
                {
                    var targetSwitchForce = 1500f * (target.transform.position - hand.transform.position);
                    invokeCo = InvokeTargeted(target, targetSwitchForce, Random.Range(1, (int)multiSlaps.val + 1), type).Start();
                }
                // else
                // {
                //     invokeCo = InvokeFree(Random.Range(1, (int)multiSlaps.val + 1));
                // }
            }
            catch (Exception e)
            {
                SuperController.LogError(e.ToString());
            }
        }

        private bool isSlapping;
        private IEnumerator invokeCo;

        
        
        public IEnumerator InvokeTargeted(SlapTarget target, Vector3 targetSwitchForce, int amount, int type)
        {
            isSlapping = true;
            var count = amount;
            var push = false;
            float timer;
            bool hjRunning = false;
            Hand FMUHand = null;
            if (handCtrl.containingAtom == PoseMe.atom)
            {
                int handId = (int)(.5f * (sideId + 1));
                FMUHand = FillMeUp.hands[handId];
            }
            while(count > 0)
            {
                if (FMUHand != null)
                {
                    if (FMUHand.enabled) FMUHand.enabled = false;
                }

                if (type == -1) push = true;
                else if(type == 0 && target.allowPushes.val) push = pushChance.val > 0f && pushChance.val == 1f || Random.Range(0f, 1f) < pushChance.val;
                Vector3 relativeTorque;
                if(!push)
                {
                    ToggleCaress(false);
                    int backslap = type == 2 || (backSlapChance.val > 0f && backSlapChance.val == 1f ||
                                                 Random.Range(0f, 1f) < backSlapChance.val)? -1 : 1;
                    var intensity = Mathf.Abs(NormalDistribution.GetValue(slapIntensityMean.val, slapIntensityDelta.val,
                        2f,
                        slapIntensityOneSided.val));
                    Vector3 combinedForce;
                    Vector3 torque = backslap * intensity * sideId * 5000f * Vector3.forward;
                    var dist = targetSwitchForce.sqrMagnitude;

                    var forward =
                        NormalDistribution.GetValue(backwardsMean.val, backwardsDelta.val, 2, backwardsOneSided.val);
                    var sideways =
                        NormalDistribution.GetValue(sidewaysMean.val, sidewaysDelta.val, 2, sidewaysOneSided.val);
                    if (count == amount)
                    {
                        var duration = Mathf.Lerp(.4f, .8f, dist / 3.5e5f);
                        timer = 0f;

                        while (timer <= duration)
                        {
                            combinedForce = backslap * intensity * 300f *
                                            (hand.transform.up + .5f * foreArm.transform.up
                                             - sideId * forward * hand.transform.right -
                                             sideways * hand.transform.forward).normalized +
                                            targetSwitchForce;
                            relativeTorque = 20000f * Vector3.Cross(hand.transform.forward, target.transform.forward);
                            // relativeTorque = Vector3.Cross(hand.transform.up, combinedForce);
                            float time = Mathf.Sqrt(timer / duration);
                            hand.AddForce(Vector3.Lerp(Vector3.zero, combinedForce, time));
                            hand.AddRelativeTorque(Vector3.Lerp(Vector3.zero, torque, time));
                            hand.AddTorque(Vector3.Lerp(Vector3.zero, relativeTorque, time));
                            timer += Time.fixedDeltaTime;
                            yield return wait;
                        }
                    }
                    else
                    {
                        timer = 0f;
                        while (timer <= .24f)
                        {
                            combinedForce = backslap * intensity * 300f *
                                            (hand.transform.up + .5f * foreArm.transform.up
                                             - sideId * forward * hand.transform.right -
                                             sideways * hand.transform.forward).normalized +
                                            targetSwitchForce;
                            relativeTorque = 20000f * Vector3.Cross(hand.transform.forward, target.transform.forward);
                            // relativeTorque = Vector3.Cross(hand.transform.up, combinedForce);
                            hand.AddForce(combinedForce);
                            hand.AddRelativeTorque(torque);
                            hand.AddTorque(relativeTorque);
                            timer += Time.fixedDeltaTime;
                            yield return wait;
                        }
                    }

                    torque *= -10f;
                    timer = 0f;
                    while (timer <= .16f)
                    {
                        combinedForce = 1000f * (target.transform.position - hand.transform.position) - 50f*sideId*hand.transform.right + targetSwitchForce;
                        relativeTorque = 20000f * Vector3.Cross(hand.transform.forward, target.transform.forward);
                        hand.AddForce(combinedForce);
                        hand.AddRelativeTorque(torque);
                        hand.AddTorque(relativeTorque);
                        timer += Time.fixedDeltaTime;
                        yield return wait;
                    }

                    if (count == 1 &&
                        Vector3.SqrMagnitude(hand.transform.position - handCtrl.transform.position) > .01f)
                    {
                        timer = 0f;
                        var duration = Mathf.Lerp(.64f, 1f, dist / 3.5e5f);
                        while (timer <= duration)
                        {
                            relativeTorque = 1000f * Vector3.Cross(hand.transform.forward, target.transform.forward);
                            // relativeTorque = Vector3.Cross(hand.transform.up, combinedForce);
                            float time = timer / duration;
                            hand.AddForce(Vector3.Lerp(targetSwitchForce, Vector3.zero, time) +
                                          backslap * 1000f * time * (1f - time) * hand.transform.up);
                            // Pong(time, 200f)
                            hand.AddTorque(Vector3.Lerp(relativeTorque, Vector3.zero, time));
                            timer += Time.fixedDeltaTime;
                            yield return wait;
                        }
                    }

                    if (enabled) ToggleCaress(true);
                }
                else
                {
                    var forward = NormalDistribution.GetValue(pushXMean.val, pushXDelta.val, 2, pushXOneSided.val);
                    var sideways = NormalDistribution.GetValue(pushZMean.val, pushZDelta.val, 2, pushZOneSided.val);
                    var forceIntensity = Mathf.Abs(NormalDistribution.GetValue(pushForceIntensityMean.val, pushForceIntensityDelta.val, 2f, pushForceIntensityOneSided.val));
                    var torqueIntensity = Mathf.Abs(NormalDistribution.GetValue(pushTorqueIntensityMean.val, pushTorqueIntensityDelta.val, 2f, pushTorqueIntensityOneSided.val));
                    var combinedForce = targetSwitchForce;
                    combinedForce -= 5f*forceIntensity* (hand.transform.up + .5f * foreArm.transform.up + sideId*forward*hand.transform.right + sideways*hand.transform.forward).normalized;
                    Vector3 torque = -20f * sideId * torqueIntensity * Vector3.forward;
                    var duration = NormalDistribution.GetValue(pushDurationMean.val, pushDurationDelta.val, 2, pushDurationOneSided.val);
                    timer = 0f;
                    while(timer <= duration)
                    {
                        float time = timer/duration;
                        time *= time;
                        relativeTorque = 10f * Vector3.Cross(hand.transform.forward, target.transform.forward);
                        hand.AddForce(Vector3.Lerp(Vector3.zero, combinedForce, time));
                        hand.AddRelativeTorque(Vector3.Lerp(Vector3.zero, torque, time));
                        hand.AddTorque(Vector3.Lerp(Vector3.zero, relativeTorque, time));
                        timer += Time.fixedDeltaTime;
                        yield return wait;
                    }
                    duration = Random.Range(.25f, 1f);
                    timer = 0f;
                    while(timer <= duration)
                    {
                        float time = timer/duration;
                        time *= time;
                        relativeTorque = 10f * Vector3.Cross(hand.transform.forward, target.transform.forward);
                        hand.AddForce(Vector3.Lerp(combinedForce, Vector3.zero, time));
                        hand.AddRelativeTorque(Vector3.Lerp(torque, Vector3.zero, time));
                        hand.AddTorque(Vector3.Lerp(relativeTorque, Vector3.zero, time));
                        timer += Time.fixedDeltaTime;
                        yield return wait;
                    }
                }

                count--;
            }

            isSlapping = false;
        }

        private float Pong(float t, float max)
        {
            if (t < .5f) return 2f * max * t;
            return 2f * max * (1f-t);
        }

        public static GameObject slapUidPrefab;
        
        public static void CreateSlapUidPrefab()
        {
			if (slapUidPrefab == null)
			{
                slapUidPrefab = new GameObject("DynamicSlapItem");
                slapUidPrefab.SetActive(false);
				RectTransform rt = slapUidPrefab.AddComponent<RectTransform>();
				rt.anchorMax = new Vector2(0, 1);
				rt.anchorMin = new Vector2(0, 1);
				rt.offsetMax = new Vector2(535, -500);
				rt.offsetMin = new Vector2(10, -600);
				LayoutElement le = slapUidPrefab.AddComponent<LayoutElement>();
				le.flexibleWidth = 1;
				le.minHeight = 50;
				le.minWidth = 350;
				le.preferredHeight = 50;
				le.preferredWidth = 500;

				RectTransform backgroundTransform = PoseMe.singleton.manager.configurableScrollablePopupPrefab.transform.Find("Background") as RectTransform;
				backgroundTransform = Instantiate(backgroundTransform, slapUidPrefab.transform);
				backgroundTransform.name = "Background";
				backgroundTransform.anchorMax = new Vector2(1, 1);
				backgroundTransform.anchorMin = new Vector2(0, 0);
				backgroundTransform.offsetMax = new Vector2(0, 0);
				backgroundTransform.offsetMin = new Vector2(0, 0);
                
                RectTransform buttonPrefab = PoseMe.singleton.manager.configurableScrollablePopupPrefab.transform.Find("Button") as RectTransform;
                var buttonTransform = Instantiate(buttonPrefab, slapUidPrefab.transform);
                DestroyImmediate(buttonTransform.GetComponent<Button>());
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

                buttonTransform = Instantiate(buttonPrefab, slapUidPrefab.transform);
                buttonTransform.name = "Button";
                buttonTransform.anchorMax = new Vector2(1, 1);
                buttonTransform.anchorMin = new Vector2(0, 0);
                buttonTransform.offsetMax = new Vector2(-50, 0);
                buttonTransform.offsetMin = new Vector2(150, 0);
                var configureButton = buttonTransform.GetComponent<Button>();
                var configureButtonText = buttonTransform.Find("Text").GetComponent<Text>();

                buttonTransform = Instantiate(buttonPrefab, slapUidPrefab.transform);
                DestroyImmediate(buttonTransform.GetComponent<Button>());
                buttonTransform.name = "SideToggle";
                buttonTransform.anchorMax = new Vector2(0, 1);
                buttonTransform.anchorMin = new Vector2(0, 1);
                buttonTransform.offsetMax = new Vector2(150, 0);
                buttonTransform.offsetMin = new Vector2(100, -50);
                var sideToggle = buttonTransform.gameObject.AddComponent<Toggle>();
                var sideToggleText = buttonTransform.Find("Text").GetComponent<Text>();
                sideToggleText.text = "<b>L</b>";
                sideToggleText.fontSize = 28;
                sideToggle.isOn = true;
                
                buttonTransform = Instantiate(buttonPrefab, slapUidPrefab.transform);
                buttonTransform.name = "Button";
                buttonTransform.anchorMax = new Vector2(0, 1);
                buttonTransform.anchorMin = new Vector2(0, 1);
                buttonTransform.offsetMax = new Vector2(100, 0);
                buttonTransform.offsetMin = new Vector2(50, -50);
                var personButton = buttonTransform.GetComponent<Button>();
                var personText = buttonTransform.Find("Text").GetComponent<Text>();
                personText.text = "<b>P</b>";
                personText.fontSize = 28;

                buttonTransform = Instantiate(buttonPrefab, slapUidPrefab.transform);
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

                UIDynamicSlapItem uid = slapUidPrefab.AddComponent<UIDynamicSlapItem>();
                uid.activeToggle = activeToggle;
                uid.sideToggle = sideToggle;
                uid.toggleText = activeToggleText;
                uid.sideText = sideToggleText;
                uid.deleteButton = deleteButton;
                uid.configureButton = configureButton;
                uid.personButton = personButton;
                uid.configureButtonText = configureButtonText;
            }
		}
    }
}