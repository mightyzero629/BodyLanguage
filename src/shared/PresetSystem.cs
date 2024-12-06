using System;
using System.Collections.Generic;
using System.Linq;
using MacGruber;
using MVR.FileManagementSecure;
using shared;
using SimpleJSON;

namespace CheesyFX
{
    public class PresetSystem
    {
        public MVRScript script;
        private JSONStorableUrl loadURL;
        public string saveDir = "";
        public Func<JSONClass> Store;
        public Action<JSONClass> Load;
        public JSONClass factoryDefaults;
        public JSONClass appliedPreset;
        public string IOLabelLeft = "Save Preset";
        public string IOLabelRight = "Load Preset";
        public JSONStorableActionPresetFilePath fileAction;

        private bool initialized;

        public PresetSystem(MVRScript script, string owner = "")
        {
            this.script = script;
            loadURL = new JSONStorableUrl("loadURL", "", UILoadJSON, "json", true);
            loadURL.hideExtension = true;
            loadURL.setCallbackFunction -= UILoadJSON;
            loadURL.allowFullComputerBrowse = false;
            loadURL.allowBrowseAboveSuggestedPath = true;
            if (owner != "") owner += "_";
            fileAction = new JSONStorableActionPresetFilePath($"{owner}LoadPreset", UILoadJSON, loadURL);
        }

        public void Init(bool doLoad = true)
        {
            if(!initialized)
            {
                FileManagerSecure.CreateDirectory(saveDir);
                loadURL.SetFilePath(saveDir);
                loadURL.setCallbackFunction += UILoadJSON;
                factoryDefaults = Store();
                if (doLoad) ApplyLatestMatchingPreset();
                script.RegisterPresetFilePathAction(fileAction);
                initialized = true;
            }
            else
            {
                factoryDefaults = Store();
                if (doLoad) ApplyLatestMatchingPreset();
            }
        }

        public void ConnectInstance(IPresetSystemReceiver receiver, bool applyPreset = true)
        {
            Load = receiver.Load;
            Store = receiver.Store;
            if(applyPreset) ApplyLatestMatchingPreset();
        }
        
        public void InitStatic()
        {
            FileManagerSecure.CreateDirectory(saveDir);
            loadURL.SetFilePath(saveDir);
            loadURL.setCallbackFunction += UILoadJSON;
            script.RegisterPresetFilePathAction(fileAction);
            initialized = true;
        }

        public void CreateUI()
        {
            var IOButtons = Utils.SetupTwinButton(script,
                IOLabelLeft, UISaveJSONDialog,
                IOLabelRight, () =>
                {
                    loadURL.shortCuts = FileManagerSecure.GetShortCutsForDirectory(saveDir);
                    loadURL.FileBrowse();
                }, false);
            // loadURL.RegisterFileBrowseButton(IOButtons.buttonRight);
            
            Utils.SetupTwinButton(script,
                "UserDefaults", () =>
                {
                    if (FileManagerSecure.FileExists(saveDir + "UserDefaults.json"))
                    {
                        ApplyFromUrl(saveDir + "UserDefaults.json");
                    }
                    else
                    {
                        ($"{script.name}: You tried to load some 'UserDefaults' but there is no such file. "+
                        "Save a preset with the name 'UserDefaults' first.").Print();
                    }
                },
                "FactoryDefaults", () => Load(factoryDefaults), true);
        }
        
        private void ApplyFromUrl(string path)
        {
            if(!FileManagerSecure.FileExists(path)) return;
            path = FileManagerSecure.NormalizePath(path);
            {
                appliedPreset = SuperController.singleton.LoadJSON(path).AsObject;
                Load(appliedPreset);
            }
        }

        public void ApplyLatestMatchingPreset()
        {
            List<string> matchingPresetPaths = new List<string>();
            string atomName = script.containingAtom.name;
            string[] paths = FileManagerSecure.GetFiles(saveDir);
            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                if (!path.EndsWith(".json")) continue;
                string presetName = path.Replace(saveDir+"\\","");
                
                if(presetName.StartsWith(atomName)) matchingPresetPaths.Add(presetName);
                else
                {
                    if(script.containingAtom.GetComponentInChildren<DAZCharacterSelector>().gender == DAZCharacterSelector.Gender.Male && presetName.StartsWith("UserDefaultsMale")) matchingPresetPaths.Insert(0, presetName);
                    else if (presetName.StartsWith("UserDefaults")) matchingPresetPaths.Insert(0, presetName);
                }
            }

            if (matchingPresetPaths.Count > 0)
            {
                string preset = matchingPresetPaths.Last();
                ApplyFromUrl(saveDir+preset);
                $"Successfully applied '{saveDir+preset}'.".Print();
            }
        }
        
        public void UILoadJSON(string url){
            JSONClass jc = SuperController.singleton.LoadJSON(url).AsObject;
            if (jc != null)
            {
                appliedPreset = jc;
                Load(appliedPreset);
            }
        }
        
        public void UISaveJSONDialog(){
            SuperController.singleton.GetMediaPathDialog(UISaveJSON, "json", saveDir, false, true, false, null, true, null, false, false);
            SuperController.singleton.mediaFileBrowserUI.SetTextEntry(true);
            if(SuperController.singleton.mediaFileBrowserUI.fileEntryField != null){
                string filename = "UserDefaults";
                SuperController.singleton.mediaFileBrowserUI.fileEntryField.text = filename;
                SuperController.singleton.mediaFileBrowserUI.ActivateFileNameField();
            }
        }
        
        public void UISaveJSON(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            string path1 = path.Replace('\\', '/')+".json";
            JSONClass jc = Store();
            SuperController.singleton.SaveJSON(jc, path1);
            SuperController.singleton.DoSaveScreenshot(path+".jpg");
        }
    }
}