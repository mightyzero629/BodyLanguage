using System;
using MVR.FileManagementSecure;
using SimpleJSON;
using UnityEngine.UI;

namespace CheesyFX
{
    public class IOSystem
    {
        private JSONStorableUrl loadURL;
        private string _saveDir;

        public string saveDir
        {
            get
            {
                return _saveDir;
            }
            set
            {
                _saveDir = value;
                loadURL.SetFilePath(value);
                loadURL.setCallbackFunction += UILoadJSON;
            }
        }
        public string defaultName = "MyFile";
        public delegate void OnJSONLoaded(string val1, JSONClass val2);
        public delegate void OnJSONSaved(string val);
        public OnJSONLoaded onLoaded;
        public OnJSONSaved onSaved;
        public Func<JSONClass> store;

        public bool doScreenshot;

        public IOSystem()
        {
            loadURL = new JSONStorableUrl("loadURL", "", UILoadJSON, "json", true);
            loadURL.hideExtension = true;
            loadURL.setCallbackFunction -= UILoadJSON;
            loadURL.allowFullComputerBrowse = false;
            loadURL.allowBrowseAboveSuggestedPath = true;
            onLoaded = (val1, val2) => { };
            onSaved = val => { };
        }

        public void RegisterLoadButton(Button button)
        {
            // loadURL.shortCuts = FileManagerSecure.GetShortCutsForDirectory(saveDir);
            // loadURL.RegisterFileBrowseButton(button);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                saveDir.Print();
                loadURL.shortCuts = FileManagerSecure.GetShortCutsForDirectory(saveDir);
                loadURL.FileBrowse();
            });
        }
        
        public void UILoadJSON(string url){
            JSONClass jc = SuperController.singleton.LoadJSON(url).AsObject;
            if (jc != null)
            {
                onLoaded(url, jc);
            }
        }
        
        public void UISaveJSONDialog(){
            SuperController.singleton.GetMediaPathDialog(UISaveJSON, "json", saveDir, false, true, false, null, true, null, false, false);
            SuperController.singleton.mediaFileBrowserUI.SetTextEntry(true);
            if(SuperController.singleton.mediaFileBrowserUI.fileEntryField != null){
                SuperController.singleton.mediaFileBrowserUI.fileEntryField.text = defaultName;
                SuperController.singleton.mediaFileBrowserUI.ActivateFileNameField();
            }
        }
        
        public void UISaveJSON(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            path = FileManagerSecure.NormalizePath(path);
            if (path.Replace(saveDir, "").Contains("/"))
            {
                FileManagerSecure.CreateDirectory(path.Substring(0, path.LastIndexOf('/')+1));
            }
            // string path1 = FileManagerSecure.NormalizePath(path)+".json";
            JSONClass jc = store();
            SuperController.singleton.SaveJSON(jc, path+".json");
            if(doScreenshot) SuperController.singleton.DoSaveScreenshot(path+".jpg");
            onSaved(path+".json");
        }
    }
}