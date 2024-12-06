using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace CheesyFX
{
    public class SyncedForceGroup
    {
        private List<Force> forces;
        private List<Force> activeForces = new List<Force>();
        private Force driver;
        public Dictionary<Force, int> priorities = new Dictionary<Force, int>();

        private bool enabled = true;
        private int mode;
        public JSONStorableStringChooser modeChooser =
            new JSONStorableStringChooser("Mode", new List<string> {"Priority", "First Served", "Randomized", "Disabled"}, "Priority", "Mode");
        private List<JSONStorableStringChooser> prioChoosers = new List<JSONStorableStringChooser>();

        private JSONStorableFloat randomizeMean = new JSONStorableFloat("Randomize Time Mean", 10f, 5f, 30f);
        private JSONStorableFloat randomizeDelta = new JSONStorableFloat("Randomize Time Delta", 5f, 0f, 25f);
        private float timer;

        public JSONStorableString driverInfo = new JSONStorableString("Driver", "");

        private bool prioUIOpen;
        private ColorBlock driverColorBlock = ColorBlock.defaultColorBlock;
        private List<object> UIElements = new List<object>();
        
        public SyncedForceGroup(List<Force> forces, List<int> prioritiesList)
        {
            modeChooser.setCallbackFunction += SetMode;
            this.forces = forces;
            for (int i = 0; i < forces.Count; i++)
            {
                var force = forces[i];
                force.AddSync().phaseOffsetDelta.val = .25f;
                force.forceGroups.Add(this);
                force.priorities[this] = priorities[force] = prioritiesList[i];
                var split = force.name.Split(':');
                var label = split[0]; 
                label += split[1].StartsWith("M")? "M Prio" : " Prio"; 
                prioChoosers.Add(
                    new JSONStorableStringChooser($"{force.name} Prio", new List<string> { "1", "2", "3","4","5","6", "0" }, prioritiesList[i].ToString(), label)
                );
                prioChoosers[i].setCallbackFunction += val =>
                {
                    force.priorities[this] = priorities[force] = int.Parse(val);
                    SetPrioDriver();
                };
            }
            driverColorBlock.normalColor = new Color(0.6f, 1f, 0.59f);
            driverColorBlock.highlightedColor = driverColorBlock.pressedColor = new Color(0.6f, 1f, 0.59f) * .85f;
        }

        public JSONClass Store()
        {
            var jc = new JSONClass();
            modeChooser.Store(jc);
            prioChoosers.ForEach(x => x.Store(jc));
            randomizeMean.Store(jc);
            randomizeDelta.Store(jc);
            return jc;
        }

        public void Load(JSONClass jc)
        {
            modeChooser.Load(jc);
            prioChoosers.ForEach(x => x.Load(jc));
            randomizeMean.Load(jc);
            randomizeDelta.Load(jc);
        }

        public void SetToDefault()
        {
            modeChooser.SetValToDefault();
            prioChoosers.ForEach(x => x.SetValToDefault());
            randomizeMean.SetValToDefault();
            randomizeDelta.SetValToDefault();
        }

        public void CreateUI()
        {
            prioUIOpen = false;
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
                });
            UIElements.Add(button);
            var chooser = modeChooser.CreateUI(UIElements, true);
            chooser.ForceHeight(50f);
            if(mode == 0)
            {
                for (int i = 0; i < prioChoosers.Count; i++)
                {
                    prioChoosers[i].CreateUI(UIElements, i % 2 == 1);
                }
                
                if (driver != null)
                {
                    prioChoosers[forces.IndexOf(driver)].popup.topButton.colors = driverColorBlock;
                }
                
                prioUIOpen = true;
            }
            else if (mode == 2)
            {
                randomizeMean.CreateUI(UIElements);
                randomizeDelta.CreateUI(UIElements, true);
            }

            // Utils.SetupInfoOneLine(FillMeUp.singleton, driverInfo, false);
            driverInfo.CreateUI(UIElements);
            FillMeUp.singleton.SetupButton("Set Random Driver", false, SetRandomDriver, UIElements);
        }

        public void AddForce(Force force, int priority = 3)
        {
            forces.Add(force);
            force.AddSync(driver);
            force.forceGroups.Add(this);
            force.priorities[this] = priorities[force] = priority;
        }

        public void MarkDisabled(Force force)
        {
            activeForces.Remove(force);
            if(mode > 1) return;
            if (driver == force)
            {
                if(mode == 0)
                {
                    SetPrioDriver();
                }
                else if(mode < 3) SetRandomDriver();
            }
        }

        public void MarkEnabled(Force force)
        {
            // $"{force.name} {priorities[force]}".Print();
            activeForces.Add(force);
            if(mode > 1 || priorities[force] == 0) return;
            if ((object)driver == null || (mode == 0 && priorities[force] < priorities[driver])) SetDriver(force);
            else
            {
                force.sync.driver = driver;
                force.sync.enabled = true;
            }
        }

        private void SetPrioDriver()
        {
            Force newDriver = null;
            int prio = 6;
            for (int i = 0; i < activeForces.Count; i++)
            {
                var force = activeForces[i];
                var thisPrio = priorities[force];
                if (thisPrio > 0 && thisPrio <= prio)
                {
                    prio = thisPrio;
                    newDriver = force;
                }
            }
            // newDriver.name.Print();
            SetDriver(newDriver);
        }

        private void SetRandomDriver()
        {
            if (activeForces.Count == 1 && (object)driver == null)
            {
                SetDriver(activeForces[0]);
                return;
            }
            var rand = Random.Range(0, activeForces.Count);
            SetDriver(activeForces[rand]);
        }

        private void SetDriver(Force force)
        {
            driver = force;
            if ((object)driver == null)
            {
                for (int i = 0; i < forces.Count; i++)
                {
                    var f = forces[i];
                    f.sync.driver = null;
                    f.sync.enabled = false;
                }
            }
            else
            {
                // driver.sync.driver = null;
                driver.sync.enabled = false;
                for (int i = 0; i < forces.Count; i++)
                {
                    var f = forces[i];
                    if(f == driver || priorities[f] == 0) continue;
                    f.sync.driver = driver;
                    f.sync.enabled = true;
                }
            }
            driverInfo.val = (object)driver == null? "None" : driver.name;
        }

        private void SetMode(string val)
        {
            enabled = val != "Disabled";
            mode = modeChooser.choices.IndexOf(val);
            if (mode == 3)
            {
                SetDriver(null);
            }
            // if(mode != 0) prioChoosers.ForEach(x => FillMeUp.singleton.RemovePopup(x));
            else
            {
                SetPrioDriver();
            }
            if (FillMeUp.singleton.UITransform != null && FillMeUp.singleton.UITransform.gameObject.activeSelf)
            {
                CreateUI();
            }
        }

        public void Update()
        {
            if(mode != 2 || activeForces.Count < 2) return;
            timer -= Time.fixedDeltaTime;
            if (timer < 0f)
            {
                timer = NormalDistribution.GetValue(randomizeMean.val, randomizeDelta.val);
                SetRandomDriver();
            }
        }
    }
}