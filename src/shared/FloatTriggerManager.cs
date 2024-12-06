using System;
using System.Collections.Generic;
using System.Linq;
using MacGruber;
using SimpleJSON;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CheesyFX
{
    public class FloatTriggerManager : MonoBehaviour
    {
        public MVRScript script;
        public JSONStorableFloat driver;
        public List<CustomFloatTrigger> triggers = new List<CustomFloatTrigger>();
        private CustomFloatTrigger current;
        private List<object> UIElements = new List<object>();
        private bool panelOpen;
        private string baseInfo;

        private JSONStorableStringChooser triggerChooser =
            new JSONStorableStringChooser("TriggerChooser", new List<string>(), "", "Trigger");

        private JSONStorableString info = new JSONStorableString("Info", "");
        private JSONStorableString nameSetter = new JSONStorableString("NameSetter", "");

        private bool absoluteDef;
        private float thresholdDef;
        private float inputFromDef;
        private float inputToDef;

        public void Init(MVRScript script, JSONStorableFloat driver, bool absoluteDef = false, float thresholdDef = 0f, float fromDef = 0f, float toDef = 1f)
        {
            this.script = script;
            this.driver = driver;
            this.absoluteDef = absoluteDef;
            this.thresholdDef = thresholdDef;
            inputFromDef = fromDef;
            inputToDef = toDef;
            baseInfo = driver.name + " Triggers";
            // AddTrigger();
            triggerChooser.setCallbackFunction += SelectTrigger;
            nameSetter.setCallbackFunction += RenameTrigger;
        }

        private void AddTrigger()
        {
            if(current != null) Utils.RemoveUIElements(script, current.UIElements);
            current = script.gameObject.AddComponent<CustomFloatTrigger>();
            triggers.Add(current);
            current.Init(script, driver, (triggers.Count - 1).ToString(), absoluteDef, thresholdDef, inputFromDef, inputToDef);
            SyncChooser();
            SelectTrigger(current);
        }

        private void Duplicate()
        {
            if(current == null) return;
            JSONClass settings = current.Store();
            AddTrigger();
            current.Load(settings);
        }

        private void RemoveTrigger()
        {
            // current = Math.Min(current, triggers.Count - 2);
            Destroy(current);
            triggers.Remove(current);
            SyncChooser();
            if(triggers.Count > 0) SelectTrigger(triggers[0]);
            else
            {
                Utils.RemoveUIElements(script, current.UIElements);
                nameSetter.valNoCallback = triggerChooser.valNoCallback = "";
            }
        }

        private void SelectTrigger(string name)
        {
            SelectTrigger(triggers.First(x => x.name == name));
        }

        private void SelectTrigger(CustomFloatTrigger trigger)
        {
            if(current == null) return;
            Utils.RemoveUIElements(script, current.UIElements);
            current = trigger;
            if(panelOpen) current.OpenPanel(script);
            nameSetter.valNoCallback = triggerChooser.valNoCallback = current.name;
        }

        public void OpenPanel(MVRScript script, Action back)
        {
            var button = script.CreateButton("Return");
            button.buttonColor = new Color(0.55f, 0.90f, 1f);
            button.button.onClick.AddListener(
                () =>
                {
                    Utils.RemoveUIElements(script, UIElements);
                    if(current != null) Utils.RemoveUIElements(script, current.UIElements);
                    panelOpen = false;
                    back();
                });
            UIElements.Add(button);
            script.SetupButton("Add New", false, AddTrigger, UIElements);
            script.SetupButton("Duplicate", false, Duplicate, UIElements);
            script.SetupButton("Remove", false, RemoveTrigger, UIElements);
            
            var textfield = script.CreateTextField(info, true);
            textfield.ForceHeight(65f);
            textfield.UItext.fontSize = 30;
            textfield.UItext.alignment = TextAnchor.MiddleCenter;
            UIElements.Add(textfield);
            triggerChooser.CreateUI(script: script, UIElements: UIElements, rightSide: true, chooserType: 1);
            // triggerChooser. = 60f;
            var input = Utils.SetupTextInput(script, "Rename", nameSetter, true);
            UIElements.Add(input);
            
            var spacer = script.CreateSpacer(true);
            spacer.height = 135f;
            UIElements.Add(spacer);
            spacer = script.CreateSpacer(false);
            spacer.height = 10f;
            UIElements.Add(spacer);
            panelOpen = true;
            SelectTrigger(current);
        }

        private void SyncChooser()
        {
            triggerChooser.choices = triggers.Select(x => x.name).ToList();
            if(current != null) triggerChooser.valNoCallback = current.name;
        }

        private void RenameTrigger(string newName)
        {
            if(current == null) return;
            string baseName = newName;
            var names = triggers.Select(x => x.name);
            int i = 0;
            while (names.Contains(newName))
            {
                newName = $"{baseName}{i}";
                i++;
            }
            current.name = newName;
            SyncChooser();
        }

        private void Update()
        {
            if (panelOpen) info.val = $"{baseInfo}\n{driver.val:0.00}";
        }

        public JSONClass Store()
        {
            JSONClass jc = new JSONClass();
            foreach (var trigger in triggers)
            {
                jc[trigger.name] = trigger.Store();
            }
            return jc;
        }

        public void Load(JSONClass jc)
        {
            foreach (var name in jc.Keys)
            {
                if(triggers.Exists(x => x.name == name)) continue;
                AddTrigger();
                current.name = name;
                current.Load(jc[name].AsObject);
            }
            SyncChooser();
        }
    }
}