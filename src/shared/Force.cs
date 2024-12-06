using System;
using System.Collections.Generic;
using System.Text;
using SimpleJSON;
using UnityEngine;

namespace CheesyFX
{
    public class Force : MonoBehaviour
    {
        public new string name;
        private Rigidbody _rb;

        public Rigidbody rb
        {
            get { return _rb; }
            set
            {
                if (_rb == value) return;
                _rb = value;
                atom = rb.GetAtom();
                drivesDildo = atom.type == "Dildo";
            }
        }

        protected Atom atom;
        private bool drivesDildo;
        private bool dildoSpringAtTarget;
        public Func<Vector3> GetDirection;

        public ForceParam amplitude;
        public ForceParam period;
        public ForceParam periodRatio;
        public ForceParam quickness;
        public ForceParam[] parameters;

        public JSONStorableBool enabledJ = new JSONStorableBool("Enabled", true);
        
        public JSONStorableBool applyReturn = new JSONStorableBool("ApplyOnReturn", true);
        public JSONStorableBool constant = new JSONStorableBool("Constant Force", false);
        public JSONStorableBool syncToThrust = new JSONStorableBool("Sync To Thrust", false);

        public bool ampAtZero;
        public float scale = 1f;

        public float timer;
        public float flip = 1f;
        protected Vector3 targetForce;
        public Vector3 currentForce;
        public bool shutDown;

        // public float offset;
        public float offsetTarget;
        private float currentOffset;
        private float offsetQuickness;
        public bool offsetAtTarget;

        public ForceParamControl paramControl;
        public ForceSync sync;
        protected StringBuilder sb = new StringBuilder();

        public List<SyncedForceGroup> forceGroups = new List<SyncedForceGroup>();
        public Dictionary<SyncedForceGroup, int> priorities = new Dictionary<SyncedForceGroup, int>();
        
        public PresetSystem presetSystem;

        private bool initialized;

        public Force Init(string name, Rigidbody rb, Func<Vector3> getDirection)
        {
            this.name = name;
            if(rb != null) this.rb = rb;
            GetDirection = getDirection;
            amplitude = new ForceParam("amplitude",0f, 0f);
            period = new ForceParam("period", .5f, .25f);
            periodRatio = new ForceParam("periodRatio", .5f, .2f);
            quickness = new ForceParam("quickness", 4f, 3f);
            period.current = period.mean.val;
            periodRatio.current = periodRatio.mean.val;
            periodRatio.mean.max = 1f;
            periodRatio.delta.max = 1f;
            parameters = new ForceParam[] { amplitude, period, periodRatio, quickness };
            paramControl = new ForceParamControl(this);
            enabled = false;
            constant.setCallbackFunction += SetConstant;
            syncToThrust.setCallbackFunction += SetSyncToThrust;
            ForceToggle.AddForce(this);
            presetSystem = new PresetSystem(FillMeUp.singleton, name){
                saveDir = $"Saves/PluginData/CheesyFX/BodyLanguage/FillMeUp/Forces/{string.Join("/", name.Split(':'))}/",
                Store = Store,
                Load = Load
            };
            initialized = true;
            return this;
        }

        private void OnEnable()
        {
            if(!initialized) return;
            flip = 0f;
            timer = 0f;
            amplitude.Reset();
            // period.Reset();
            // periodRatio.Reset();
            // quickness.Reset();
            currentForce = targetForce = Vector3.zero;
            shutDown = false;
        }

        private void OnDisable()
        {
            currentOffset = 0f;
            paramControl.info.val = "Inactive";
        }

        public void ShutDown(float quickness = 0f)
        {
            if (!enabled) return;
            amplitude.target = 0f;
            if (quickness > 0f)
            {
                amplitude.transitionQuickness = quickness;
                offsetQuickness = quickness*.5f;
            }
            offsetTarget = 0f;
            offsetAtTarget = false;
            shutDown = true;
            
            if (amplitude.atTarget) enabled = false;
            for (var index = 0; index < forceGroups.Count; index++)
            {
                forceGroups[index].MarkDisabled(this);
            }
            // if (drivesDildo) atom.mainController.RBHoldPositionSpring = 1e5f;
        }
        
        public void ShutDownImmediate()
        {
            if (!enabled) return;
            amplitude.target = amplitude.current = 0f;
            shutDown = false;
            offsetTarget = currentOffset = 0f;
            enabled = false;
            for (var index = 0; index < forceGroups.Count; index++)
            {
                forceGroups[index].MarkDisabled(this);
            }
            if (drivesDildo) atom.mainController.RBHoldPositionSpring = 1e5f;
        }

        public void Enable()
        {
            if(!initialized || ! enabledJ.val) return;
            shutDown = false;
            amplitude.GetNewTarget();
            offsetTarget = paramControl.offset.val;
            offsetQuickness = paramControl.offsetQuickness.val;
            offsetAtTarget = false;
            enabled = true;
            for (var index = 0; index < forceGroups.Count; index++)
            {
                forceGroups[index].MarkEnabled(this);
            }
            dildoSpringAtTarget = false;
            
        }
        
        public void SetActive(bool val)
        {
            if(val) Enable();
            else ShutDown(4f);
        }
        
        public void SetActiveImmediate(bool val)
        {
            if(val) Enable();
            else ShutDownImmediate();
        }

        public void OnAmpChanged()
        {
            ampAtZero = amplitude.mean.val == 0f && amplitude.delta.val == 0f;
            if (ampAtZero)
            {
                enabledJ.val = enabled = false;
            }
            else enabledJ.val = enabled = true;
        }

        public ForceSync AddSync(Force driver = null)
        {
            if (sync != null) return sync;
            sync = new ForceSync(this, driver);
            return sync;
        }

        public void UpdateParams()
        {
            if(!shutDown) amplitude.Update();
            else amplitude.LerpToTarget();
            period.Update();
            periodRatio.Update();
            quickness.Update();
        }

        public void GetPhase()
        {
            timer -= Time.fixedDeltaTime;
            if (timer < 0.0f) {
                if ((flip > 0f && periodRatio.current != 1f) || periodRatio.current == 0f) {
                    flip = applyReturn.val ? -2f*periodRatio.current : 0f;
                    timer = period.current * (1f - periodRatio.current);
                } else {
                    flip = .5f/periodRatio.current;
                    timer = period.current * periodRatio.current;
                }
            }
        }

        public virtual void FixedUpdate() {
            try
            {
                if (!offsetAtTarget)
                {
                    currentOffset = Mathf.Lerp(currentOffset, offsetTarget, offsetQuickness*Time.fixedDeltaTime);
                    if (Mathf.Abs(currentOffset - offsetTarget) < 1f)
                    {
                        currentOffset = offsetTarget;
                        offsetAtTarget = true;
                    }
                }
                // currentOffset.Print();
                if(sync != null && sync.enabled && sync.driver.enabled)
                {
                    if(!shutDown) sync.UpdateParams();
                    else amplitude.LerpToTarget();
                    sync.GetPhase();
                    targetForce = (flip * amplitude.current + currentOffset) * GetDirection();
                    currentForce = Vector3.Lerp(currentForce, targetForce, Time.fixedDeltaTime * quickness.current/period.current);
                }
                else
                {
                    if(!constant.val)
                    {
                        UpdateParams();
                        GetPhase();
                        targetForce = (flip * amplitude.current + currentOffset) * GetDirection();
                        currentForce = Vector3.Lerp(currentForce, targetForce, Time.fixedDeltaTime * quickness.current/period.current);
                    }
                    else
                    {
                        if(!shutDown) amplitude.Update();
                        else amplitude.LerpToTarget();
                        currentForce = (amplitude.current + currentOffset) * GetDirection();
                    }
                }

                if (rb && !SuperController.singleton.freezeAnimation) AddForce();
                if (shutDown)
                {
                    if (drivesDildo)
                    {
                        atom.mainController.RBHoldPositionSpring = Mathf.Lerp(atom.mainController.RBHoldPositionSpring, 1e5f, 5f*Time.fixedDeltaTime);
                    }

                    if (amplitude.atTarget && offsetAtTarget)
                    {
                        atom.mainController.RBHoldPositionSpring = 1e5f;
                        enabled = false;
                    }
                }
                else if (drivesDildo && !dildoSpringAtTarget)
                {
                    if (atom.mainController.RBHoldPositionSpring - 5000f > 10f)
                    {
                        atom.mainController.RBHoldPositionSpring = Mathf.Lerp(atom.mainController.RBHoldPositionSpring,
                            5000f, 5f * Time.fixedDeltaTime);
                    }
                    else
                    {
                        atom.mainController.RBHoldPositionSpring = 5000f;
                        dildoSpringAtTarget = true;
                    }
                }
                
                if (paramControl.UIOpen && (FillMeUp.singleton.UITransform.gameObject.activeSelf || PoseMe.singleton.UITransform.gameObject.activeSelf))
                {
                    if(!constant.val)
                    {
                        sb = sb.Clear(87)
                            .AppendLine($"<b>{rb.name}</b>" + (sync != null && sync.enabled && sync.driver
                                ? $" (<color=#ff0000>Synced</color> to {sync.driver.name})"
                                : " (Master)"));
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            sb.Append($"{parameters[i].name} ").AppendFormat("{0:F1}", parameters[i].current).Append("\n");
                        }
                    }
                    else
                    {
                        sb = sb.Clear(87)
                            .AppendLine($"<b>{rb.name}</b>" + " (constant)")
                            .Append("Amplitude: ").AppendFormat("{0:F1}", amplitude.current);
                    }
                    paramControl.info.val = sb.ToString();
                }
                // currentForce.Print();
            }
            catch (Exception e) {
                SuperController.LogError($"Exception caught: {name} {e}");
            }
        }
        
        protected virtual void AddForce()
        {
            if(!Pose.isApplying && !SuperController.singleton.freezeAnimation && !atom.mainController.isGrabbing) rb.AddForce(currentForce * scale, ForceMode.Force);
        }

        public JSONClass Store(bool forceStore)
        {
            var jc = paramControl.Store();
            enabledJ.Store(jc, forceStore);
            applyReturn.Store(jc, forceStore);
            constant.Store(jc, forceStore);
            syncToThrust.Store(jc, forceStore);
            return jc;
        }

        public JSONClass Store() => Store(true);

        public void Load(JSONClass jc, bool setMissingToDefault)
        {
            // jc.HasKey(enabledJ.name).Print();
            enabledJ.Load(jc, setMissingToDefault);
            applyReturn.Load(jc, setMissingToDefault);
            constant.Load(jc, setMissingToDefault);
            syncToThrust.Load(jc, setMissingToDefault);
            paramControl.Load(jc, setMissingToDefault);
        }

        public void Load(JSONClass jc) => Load(jc, false);

        private void SetSyncToThrust(bool val)
        {
            if (sync == null)
            {
                $"{name}: Sync not set up".Print();
                syncToThrust.valNoCallback = false;
                return;
            }
            sync.enabled = val;
            if(val) constant.val = false;
        }

        public void UpdateDynamicScale(float min)
        {
            if(enabledJ.val)
            {
                scale = flip < 0f ? min : 1f;
            }
        }

        public void ForceInwards()
        {
            if (flip < 0f)
            {
                // flip *= -1f;
                timer = -1f;
            }
        }

        public void SetConstant(bool val)
        {
            flip = 1f;
            if (val && sync != null) syncToThrust.val = false;
            applyReturn.toggle?.gameObject.SetActive(!val);
            syncToThrust.toggle?.gameObject.SetActive(!val && sync != null);
            var tabbar = paramControl.tabbar;
            if(tabbar == null) return;
            for (int i = 1; i < 4; i++)
            {
                tabbar.buttons[i].gameObject.SetActive(!val);
            }

            if(val && paramControl.lastId > 0) tabbar.SelectTab(0);
        }
        
        // public void Load1(JSONClass jc)
        // {
        //     if (!jc.HasKey(name)) return;
        //     control.Load(jc[name].AsObject);
        // }
    }
}