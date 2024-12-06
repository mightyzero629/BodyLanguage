using System.Collections.Generic;
using System.Linq;
using MacGruber;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace CheesyFX
{
    public abstract class Fuckable : MonoBehaviour
    {
        protected bool initialized;
        public new string name;
        public int type;
        public virtual bool isPenetrated { get; set; }
        protected static List<object> UIElements = new List<object>();
        public JSONStorableString info = new JSONStorableString("info", "Idle");
        public JSONStorableFloat depth = new JSONStorableFloat("Depth", 0f, 0f, .4f, true, false);
        public JSONStorableFloat speed = new JSONStorableFloat("Speed", 0f, -5f, 5f, true, false);
        
        public JSONStorableFloat sensitivity = new JSONStorableFloat("Sensitivity (ReadMyLips)", 1f, 0f, 5f);
        public JSONStorableFloat penetrationSoundsVolume = new JSONStorableFloat("Penetration Sounds Volume", 1f, 0f, 1f);
        
        public JSONStorableBool magnetic = new JSONStorableBool("Magnetic", true);
        public JSONStorableBool preventPullout = new JSONStorableBool("Prevent Pullout", true);
        
        public JSONStorableFloat penetratorScaling = new JSONStorableFloat("Penetrator Scaling", .5f, .01f, 1f);

        public JSONStorableBool xrayEnabled = new JSONStorableBool("XRay Enabled", true);
        public JSONStorableBool xrayUseAngleScaling = new JSONStorableBool("XRay Angle Scaling", true);
        public JSONStorableBool xrayUseOcclusionScaling = new JSONStorableBool("XRay Occlusion Scaling", true);
        public JSONStorableFloat xrayTransparency = new JSONStorableFloat("XRay Transparency", .4f, 0f, 1f);
        public JSONStorableFloat xrayOcclusionOffset = new JSONStorableFloat("XRay Occlusion Offset", 20f, 0f, 50f);
        public JSONStorableFloat xrayOcclusionDecay = new JSONStorableFloat("XRay Occlusion Decay", .1f, .01f, .8f);
        public JSONStorableFloat obstructionInfo = new JSONStorableFloat("Obstruction Info", 0f, 0f, 1f, true, false);
        public JSONStorableStringChooser xrayAlpha = new JSONStorableStringChooser("XRay Alpha",
            new List<string> { "tip", "half", "full", "withBalls" }, "half", "Xray Alpha");

        public Texture2D xrayAlphaTexture;
        // public JSONStorableUrl xrayAlphaTexture = new JSONStorableUrl("XRay Alpha Texture", "");
        
        public float lastDepth;
        
        protected FloatTriggerManager depthTriggers;
        protected FloatTriggerManager speedTriggers;
        
        protected UIDynamicButton depthTriggerButton;
        protected UIDynamicButton speedTriggerButton;
        
        public Transform enterPointTF;
        public CapsuleCollider proximityTrigger;
        public AudioSource audioSource;
        
        public Penetrator penetrator;
        
        public Force thrustForce;
        public Torque pitchTorque;
        public Torque rollTorque;
        public Torque yawTorque;
        public Force maleForce;

        private bool _thrustDirectionSet;
        private Vector3 _thrustDirection;
        
        protected DAZMorph foreski;
        protected float foreskiTimer;
        public virtual Vector3 thrustDirection {
            get
            {
                if (_thrustDirectionSet || penetrator == null) return _thrustDirection;
                _thrustDirection = (penetrator.rootTransform.position - enterPointTF.position).normalized;
                _thrustDirectionSet = true;
                return _thrustDirection;
            }
        }
        public virtual Vector3 maleThrustDirection => -thrustDirection;
        
        protected Rigidbody rb;

        public Magnet magnet;


        public virtual void Init(string name)
        {
            this.name = name;
            depthTriggers = FillMeUp.singleton.AddFloatTriggerManager(depth, false, 0f, 0f, .25f);
            speedTriggers = FillMeUp.singleton.AddFloatTriggerManager(speed, true, 0f, 0f, .5f);
            if (this is Hand) rb = FillMeUp.atom.rigidbodies.First(x => x.name == name.First() + "Hand");
            else if(this is Throat) rb = FillMeUp.atom.rigidbodies.First(x => x.name == "head");
            else if(this is Cleavage) rb = FillMeUp.atom.rigidbodies.First(x => x.name == "chest");
            else rb = FillMeUp.atom.rigidbodies.First(x => x.name == "hip");
            thrustForce = gameObject.AddComponent<Force>().Init(name+":Thrust", rb, () => thrustDirection);
            thrustForce.enabledJ.setCallbackFunction += val =>
            {
                thrustForce.SetActive(val && enabled);
            };
            maleForce = gameObject.AddComponent<Force>().Init($"{name}:MaleThrust", null, () => maleThrustDirection);
            maleForce.paramControl.offset.SetWithDefault(200f);
            maleForce.amplitude.mean.SetWithDefault(300f);
            maleForce.amplitude.delta.SetWithDefault(200f);
            maleForce.period.mean.SetWithDefault(.5f);
            maleForce.enabledJ.setCallbackFunction += val => maleForce.SetActive(val && enabled);

            // penetratorScaling.setCallbackFunction = val => FillMeUp.persons.ForEach(x => x.penetrator.ShrinkWidth(val));

            FillMeUp.forceChooser.choices.Add(thrustForce.name);
            FillMeUp.forceChooser.choices.Add(maleForce.name);
            
            xrayEnabled.setCallbackFunction += val =>
            {
                if (penetrator?.type == 1)
                {
                    var person = penetrator.stimReceiver as Person;
                    if (person.xrayClient != null)
                    {
                        if(val) person.xrayClient.Enable(this);
                        else person.xrayClient.Disable();
                    };
                }
            };
            xrayUseAngleScaling.setCallbackFunction += val =>
            {
                if (penetrator?.type == 1)
                {
                    var person = penetrator.stimReceiver as Person;
                    if (person.xrayClient != null)
                    {
                        person.xrayClient.useAngleScaling = val;
                        if (val) person.xrayClient.xRayAlphaDriver.enabled = true;
                    };
                }
            };
            xrayUseOcclusionScaling.setCallbackFunction += val =>
            {
                if (penetrator?.type == 1)
                {
                    var person = penetrator.stimReceiver as Person;
                    if (person.xrayClient != null)
                    {
                        person.xrayClient.useOcclusionScaling = val;
                        if (val) person.xrayClient.xRayAlphaDriver.enabled = true;
                    };
                }
            };
            xrayOcclusionDecay.setCallbackFunction += val =>
            {
                if (penetrator?.type == 1)
                {
                    var person = penetrator.stimReceiver as Person;
                    if (person.xrayClient != null)
                    {
                        person.xrayClient.xRayAlphaDriver.occlusionDecay = val;
                    };
                }
            };
            xrayOcclusionOffset.setCallbackFunction += val =>
            {
                if (penetrator?.type == 1)
                {
                    var person = penetrator.stimReceiver as Person;
                    if (person.xrayClient != null)
                    {
                        person.xrayClient.xRayAlphaDriver.occlusionOffset = (int)val;
                    };
                }
            };
            xrayTransparency.setCallbackFunction += val =>
            {
                if (penetrator?.type == 1)
                {
                    var person = penetrator.stimReceiver as Person;
                    if(person.xrayClient !=null) person.xrayClient.maxAlpha = -val;
                }
            };
            // xrayAlphaTexture.val = FillMeUp.packageUid + "Custom/Scripts/CheesyFX/BodyLanguage/XRayTextures/half.png";
            // xrayAlphaTexture.setJSONCallbackFunction += val =>
            // {
            //     if (penetrator?.type == 1)
            //     {
            //         var person = penetrator.stimReceiver as Person;
            //         if (person.xrayClient != null) person.xrayClient.SetAlphaTexture(val);
            //     }
            // };
            xrayAlpha.setCallbackFunction += SelectXRayAlpha;
            SelectXRayAlpha(xrayAlpha.val);
            // CreateTriggersUI();
            InitForcePresets();
        }

        private void SelectXRayAlpha(string val)
        {
            switch (val)
            {
                case "full":
                {
                    xrayAlphaTexture = XRay.alphaTextures[0];
                    break;
                }
                case "half":
                {
                    xrayAlphaTexture = XRay.alphaTextures[1];
                    break;
                }
                case "tip":
                {
                    xrayAlphaTexture = XRay.alphaTextures[2];
                    break;
                }
                case "withBalls":
                {
                    xrayAlphaTexture = XRay.alphaTextures[3];
                    break;
                }
            }
            if (penetrator?.type == 1)
            {
                var person = penetrator.stimReceiver as Person;
                if (person.xrayClient != null) person.xrayClient.SetAlphaTexture(xrayAlphaTexture);
            }
        }

        public virtual void FixedUpdate()
        {
            _thrustDirectionSet = false;
        }

        public virtual void ResetPenetration()
        {
        }

        public void InitForcePresets()
        {
            thrustForce.presetSystem.Init();
            maleForce.presetSystem.Init();
            if(pitchTorque) pitchTorque.presetSystem.Init();
            if(rollTorque) rollTorque.presetSystem.Init();
            if(yawTorque) yawTorque.presetSystem.Init();
        }

        public void ApplyLatestMatchingForcePresets()
        {
            thrustForce.presetSystem.ApplyLatestMatchingPreset();
            maleForce.presetSystem.ApplyLatestMatchingPreset();
            if(pitchTorque) pitchTorque.presetSystem.ApplyLatestMatchingPreset();
            if(rollTorque) rollTorque.presetSystem.ApplyLatestMatchingPreset();
            if(yawTorque) yawTorque.presetSystem.ApplyLatestMatchingPreset();
        }

        public virtual void SetPenetrator(CapsulePenetrator newPenetrator)
        {
            StimReceiver receiver;
            if (FillMeUp.stimReceivers.TryGetValue(newPenetrator, out receiver)) receiver.SetFucking(this);
            if (newPenetrator.type == 1)
            {
                foreski = newPenetrator.stimReceiver.foreski;
                foreskiTimer = .5f;
                var person = newPenetrator.stimReceiver as Person;
                person.xrayClient?.Enable(this);
            }
            else foreski = null;
        }

        public void ClearUI()
        {
            FillMeUp.singleton.RemoveUIElements(UIElements);
        }
        
        public virtual void SelectTab(int id)
        {
            FillMeUp.singleton.RemoveUIElements(UIElements);
            FillMeUp.debugMode = false;
            if (id == 0) CreateTriggersUI();
            else if(id == 1) CreateSettingsUI();
            else if(id == 2) CreateXRayUI();
            else if(id == 3) CreateBulgeUI();
            else if(id == 4) CreateForcesUI();
            else if(id == 5) CreateDebugUI();
        }
        
        public virtual JSONClass Store(string subScenePrefix, bool storeTriggers = true)
        {
            JSONClass jc = new JSONClass();
            magnetic.Store(jc);
            preventPullout.Store(jc);
            sensitivity.Store(jc);
            penetrationSoundsVolume.Store(jc);
            penetratorScaling.Store(jc);
            xrayAlpha.Store(jc);
            xrayEnabled.Store(jc);
            xrayUseAngleScaling.Store(jc);
            xrayUseOcclusionScaling.Store(jc);
            xrayTransparency.Store(jc);
            StoreForces(jc);
            // if (thrustForce) jc[thrustForce.name] = thrustForce.Store();
            // if (pitchTorque) jc[pitchTorque.name] = pitchTorque.Store();
            // if (rollTorque) jc[rollTorque.name] = rollTorque.Store();
            // if (yawTorque) jc[yawTorque.name] = yawTorque.Store();
            // if (maleForce) jc[maleForce.name] = maleForce.Store();
            if(storeTriggers)
            {
                jc["DepthTriggers"] = depthTriggers.Store();
                jc["SpeedTriggers"] = speedTriggers.Store();
            }
            return jc;
        }

        public void StoreForces(JSONClass jc, bool forceStore = true)
        {
            if (thrustForce) jc[thrustForce.name] = thrustForce.Store(forceStore);
            if (pitchTorque) jc[pitchTorque.name] = pitchTorque.Store(forceStore);
            if (rollTorque) jc[rollTorque.name] = rollTorque.Store(forceStore);
            if (yawTorque) jc[yawTorque.name] = yawTorque.Store(forceStore);
            if (maleForce) jc[maleForce.name] = maleForce.Store(forceStore);
            preventPullout.Store(jc, forceStore);
            magnetic.Store(jc, forceStore);
        }
        
        public virtual void Load(JSONClass jc, string subScenePrefix)
        {
            if(jc.HasKey(name))
            {
                JSONClass tc = jc[name].AsObject;
                magnetic.Load(tc);
                preventPullout.Load(tc);
                sensitivity.Load(tc);
                penetrationSoundsVolume.Load(tc);
                penetratorScaling.Load(tc);
                xrayAlpha.Load(tc);
                xrayEnabled.Load(tc);
                xrayUseAngleScaling.Load(tc);
                xrayUseOcclusionScaling.Load(tc);
                xrayTransparency.Load(tc);
                LoadForces(tc);
                // if(thrustForce) thrustForce.Load(tc[thrustForce.name].AsObject);
                // if(pitchTorque) pitchTorque.Load(tc[pitchTorque.name].AsObject);
                // if(rollTorque) rollTorque.Load(tc[rollTorque.name].AsObject);
                // if(yawTorque) yawTorque.Load(tc[yawTorque.name].AsObject);
                // if(maleForce) maleForce.Load(tc[maleForce.name].AsObject);
                depthTriggers.Load(tc["DepthTriggers"].AsObject);
                speedTriggers.Load(tc["SpeedTriggers"].AsObject);
            }
            SyncTriggerButtons();
            // name.Print();
        }
        
        public virtual JSONClass StorePoseSettings(JSONClass parent)
        {
            JSONClass jc = new JSONClass();
            xrayAlpha.Store(jc, false);
            xrayEnabled.Store(jc, false);
            xrayUseAngleScaling.Store(jc, false);
            xrayUseOcclusionScaling.Store(jc, false);
            xrayTransparency.Store(jc, false);
            StoreForces(jc, false);
            parent[name] = jc;
            return jc;
        }
        
        public virtual void LoadPoseSettings(JSONClass baseJsonClass)
        {
            if (!baseJsonClass.HasKey(name))
            {
                xrayAlpha.SetValToDefault();
                xrayEnabled.SetValToDefault();
                xrayUseAngleScaling.SetValToDefault();
                xrayUseOcclusionScaling.SetValToDefault();
                xrayTransparency.SetValToDefault();
                LoadForces(baseJsonClass, true);
                return;
            }
            JSONClass jc = baseJsonClass[name].AsObject;
            xrayAlpha.Load(jc, true);
            xrayEnabled.Load(jc,true);
            xrayUseAngleScaling.Load(jc,true);
            xrayUseOcclusionScaling.Load(jc,true);
            xrayTransparency.Load(jc,true);
            LoadForces(jc, true);
        }
        
        public void LoadForces(JSONClass jc, bool setMissingToDefault = false)
        {
            if(thrustForce) thrustForce.Load(jc[thrustForce.name].AsObject, setMissingToDefault);
            if(pitchTorque) pitchTorque.Load(jc[pitchTorque.name].AsObject, setMissingToDefault);
            if(rollTorque) rollTorque.Load(jc[rollTorque.name].AsObject, setMissingToDefault);
            if(yawTorque) yawTorque.Load(jc[yawTorque.name].AsObject, setMissingToDefault);
            if(maleForce) maleForce.Load(jc[maleForce.name].AsObject, setMissingToDefault);
            preventPullout.Load(jc, true);
            magnetic.Load(jc, true);
        }
        
        public virtual void StopForcesImmediate()
        {
            if(thrustForce) thrustForce.ShutDownImmediate();
            if(pitchTorque) pitchTorque.ShutDownImmediate();
            if(rollTorque) rollTorque.ShutDownImmediate();
            if(yawTorque) yawTorque.ShutDownImmediate();
            if(maleForce) maleForce.ShutDownImmediate();
        }
        
        public virtual void SyncTriggerButtons()
        {
            depthTriggerButton.label = $"Depth Triggers ({depthTriggers.triggers.Count})";
            speedTriggerButton.label = $"Speed Triggers ({speedTriggers.triggers.Count})";
        }

        public virtual void CreateTriggersUI()
        {
            depthTriggerButton = FillMeUp.singleton.CreateButton($"Depth Triggers ({depthTriggers.triggers.Count})");
            depthTriggerButton.buttonColor = new Color(0.45f, 1f, 0.45f);
            depthTriggerButton.button.onClick.AddListener(() =>
            {
                {
                    FillMeUp.singleton.ClearUI();
                    depthTriggers.OpenPanel(FillMeUp.singleton, FillMeUp.singleton.CreateUI);
                }
            });
            UIElements.Add(depthTriggerButton);
            depth.CreateUI(FillMeUp.singleton, UIElements:UIElements);
            
            speedTriggerButton = FillMeUp.singleton.CreateButton($"Speed Triggers ({speedTriggers.triggers.Count})", true);
            speedTriggerButton.buttonColor = new Color(0.45f, 1f, 0.45f);
            speedTriggerButton.button.onClick.AddListener(() =>
            {
                {
                    FillMeUp.singleton.ClearUI();
                    speedTriggers.OpenPanel(FillMeUp.singleton, FillMeUp.singleton.CreateUI);
                }
            });
            UIElements.Add(speedTriggerButton);
            speed.CreateUI(FillMeUp.singleton, UIElements:UIElements, rightSide:true);
            var textField = FillMeUp.singleton.CreateTextField(info, true);
            UIElements.Add(textField);
            textField.height = 50f;
        }

        public virtual void CreateSettingsUI()
        {
        }
        
        public void CreateXRayUI()
        {
            XRay.enabled.CreateUI(UIElements);
            xrayEnabled.CreateUI(UIElements);
            xrayUseAngleScaling.CreateUI(UIElements, true);
            xrayUseOcclusionScaling.CreateUI(UIElements, true);
            xrayTransparency.CreateUI(UIElements);
            xrayAlpha.CreateUI(UIElements, true);
            xrayOcclusionOffset.CreateUI(UIElements, true);
            xrayOcclusionOffset.slider.wholeNumbers = true;
            xrayOcclusionDecay.CreateUI(UIElements, true);
            obstructionInfo.CreateUI(UIElements, true);
        }

        public virtual void CreateBulgeUI()
        {
            
        }

        protected static string toggleInfoText =
            "You can toggle all forces between 'off' and the state they were before by" +
            " pressing '<b>CTRL+F</b>' or calling the action '<b>Toggle Forces</b>' via trigger.\n" +
            "If you never want forces setup a FillMeUp preset called '<b>UserDefaults</b>'.";

        public virtual void CreateForcesUI()
        {
            thrustForce.enabledJ.CreateUI(UIElements);
            maleForce.enabledJ.CreateUI(UIElements);
            magnetic.CreateUI(UIElements);
            FillMeUp.singleton.SetupButton("Configure Thrust", true, () => CreateForceUINewPage(thrustForce), UIElements);
            FillMeUp.singleton.SetupButton("Configure Male Thrust", true, () => CreateForceUINewPage(maleForce), UIElements);
            if (magnet != null)
            {
                FillMeUp.singleton.SetupButton("Configure Magnet", true, magnet.CreateUINewPage, UIElements);
            }
            else
            {
                FillMeUp.singleton.SetupButton("N.A.", true, delegate {  }, UIElements);
            }
            if(this is Hand)
            {
                FillMeUp.singleton.SetupButton("Configure Hands Sync Group", true, FillMeUp.handForceGroup.CreateUI,
                    UIElements);
                FillMeUp.handForceGroup.driverInfo.CreateUI(UIElements);
            }
            else
            {
                FillMeUp.singleton.SetupButton("Configure Orifice Sync Group", true,
                    FillMeUp.orificeForceGroup.CreateUI, UIElements);
                FillMeUp.orificeForceGroup.driverInfo.CreateUI(UIElements);
            }

            preventPullout.CreateUI(UIElements, true);

            var info = Utils.SetupInfoTextNoScroll(FillMeUp.singleton, toggleInfoText, 200f, false);
            info.background.offsetMin = new Vector2(0, 0);
            UIElements.Add(info);
        }

        public virtual void CreateForceUINewPage(Force force)
        {
            FillMeUp.singleton.ClearUI();
            UIElements.Clear();
            var button = FillMeUp.singleton.CreateButton("Return");
            button.buttonColor = new Color(0.55f, 0.90f, 1f);
            button.button.onClick.AddListener(
                () =>
                {
                    FillMeUp.singleton.ClearUI();
                    FillMeUp.singleton.CreateUI();
                    FillMeUp.singleton.settingsTabbar.SelectTab(4);
                    force.paramControl.UIOpen = false;
                });
            UIElements.Add(button);
            var chooser = FillMeUp.forceChooser.CreateUI(UIElements, true, chooserType:0);
            chooser.ForceHeight(50f);
            FillMeUp.forceChooser.valNoCallback = force.name;
            force.presetSystem.CreateUI();
            force.paramControl.CreateUI(FillMeUp.singleton);
        }

        public virtual void CreateDebugUI()
        {
            FillMeUp.debugMode = true;
            FillMeUp.singleton.SetupButton("Print All Registered Penetrators", true, FillMeUp.PrintPenetrators, UIElements);
            FillMeUp.singleton.SetupButton("Reset Registered Penetrators", true, FillMeUp.ResetPenetrators, UIElements);
            FillMeUp.singleton.SetupButton("Reset Penis Colliders", true, () =>
            {
                foreach (var person in FillMeUp.persons)
                {
                    person.penetrator.colliders.ForEach(x => x.gameObject.GetComponent<AutoCollider>().AutoColliderSizeSet());
                }
            }, UIElements);
            FillMeUp.singleton.SetupButton("Sync XRay", true, XRay.SyncSkins, UIElements);
        }

        // public virtual void OnEnable()
        // {
        //     if(autoThrust.val) thrustForce.enabled = true;
        // }
    }
}