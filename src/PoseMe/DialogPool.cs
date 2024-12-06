using System.Collections.Generic;
using System.Linq;
using MacGruber;
using MVR.FileManagementSecure;
using SimpleJSON;
using UnityEngine.UI;

namespace CheesyFX
{
    public class DialogPool
    {
        public static List<List<Dialog>> dialogs = new List<List<Dialog>>();
        private static Dialog last;
        private static string help = "Each pose and cam that has a 'Dialog Pool Level' greater than -1 will play a random dialog out of the level you set.";
        public static bool uiOpen;
        private static int currentLevel;
        private static UIDynamicTabBar tabbar;
        private static JSONStorableUrl loadURL;
        private static List<string> levelNames = new List<string>{ "Lvl 0", "Lvl 1", "Lvl 2" };
        private static string saveDir = "Saves/PluginData/CheesyFX/BodyLanguage/PoseMe/DialogPools/";
        private static JSONNode levelCache;
        private static List<Dialog> available = new List<Dialog>();
        private static JSONStorableBool syncPoseLevels = new JSONStorableBool("Sync Pose Levels", true);

        public static void Init()
        {
            dialogs.Add(new List<Dialog>());
            dialogs.Add(new List<Dialog>());
            dialogs.Add(new List<Dialog>());
            FileManagerSecure.CreateDirectory(saveDir);
            loadURL = new JSONStorableUrl("loadURL", "", UILoadJSON, "json", true);
            loadURL.hideExtension = true;
            loadURL.allowFullComputerBrowse = false;
            loadURL.allowBrowseAboveSuggestedPath = true;
            loadURL.SetFilePath(saveDir);
            
        }
        
        public static void UILoadJSON(string url)
        {
            if(!FileManagerSecure.FileExists(url)) return;
            Load(SuperController.singleton.LoadJSON(url).AsObject, true);
            CreateUI();
        }

        private static void AddLevel(List<Dialog> newDialogs = null)
        {
            levelNames.Add($"Lvl {levelNames.Count}");
            if(newDialogs == null) dialogs.Add(new List<Dialog>());
            else dialogs.Add(new List<Dialog>(newDialogs));
            SyncPoseLevelsMax();
            if(uiOpen) CreateUI();
        }
        
        private static void InsertLevel()
        {
            levelNames.Add($"Lvl {levelNames.Count}");
            dialogs.Insert(currentLevel+1, new List<Dialog>());
            SyncPoseLevelsMax();
            SyncPoseLevelsGreaterThanCurrent(1);
            if(uiOpen) CreateUI();
        }

        private static void DeleteLevel()
        {
            var last = currentLevel == dialogs.Count - 1;
            levelNames.RemoveAt(currentLevel);
            dialogs.RemoveAt(currentLevel);
            if (last) currentLevel--;
            for (int i = 0; i < levelNames.Count; i++)
            {
                levelNames[i] = $"Lvl {i}";
            }
            SyncPoseLevelsGreaterThanCurrent(-1);
            SyncPoseLevelsMax();
            if(uiOpen) CreateUI();
        }
        
        private static void DeleteLastLevel()
        {
            var id = levelNames.Count - 1;
            if(id == currentLevel) currentLevel--;
            levelNames.RemoveAt(id);
            dialogs.RemoveAt(id);
            SyncPoseLevelsMax();
            if(uiOpen) CreateUI();
        }

        private static void SyncPoseLevelsMax()
        {
            float max = levelNames.Count - 1;
            PoseMe.dialogPoolLevelPose.max = levelNames.Count - 1;
            PoseMe.dialogPoolLevelCam.max = levelNames.Count - 1;
            for (int i = 0; i < PoseMe.poses.Count; i++)
            {
                var pose = PoseMe.poses[i];
                pose.dialogPoolLevel.max = max;
                for (int j = 0; j < pose.camAngles.Count; j++)
                {
                    pose.camAngles[j].dialogPoolLevel.max = max;
                }
            }
        }

        private static void SyncPoseLevels(int level, int delta)
        {
            if(!syncPoseLevels.val) return;
            for (int i = 0; i < PoseMe.poses.Count; i++)
            {
                var pose = PoseMe.poses[i];
                if(pose.dialogPoolLevel.val < 0f) continue;
                if((int)pose.dialogPoolLevel.val == level) pose.dialogPoolLevel.val += delta;
                for (int j = 0; j < pose.camAngles.Count; j++)
                {
                    if((int)pose.camAngles[j].dialogPoolLevel.val == level) pose.camAngles[j].dialogPoolLevel.val += delta;
                }
            }
        }
        
        private static void SyncPoseLevelsGreaterThanCurrent(int delta)
        {
            if(!syncPoseLevels.val) return;
            for (int i = 0; i < PoseMe.poses.Count; i++)
            {
                var pose = PoseMe.poses[i];
                if(pose.dialogPoolLevel.val < 0f) continue;
                if(delta < 0 && pose.dialogPoolLevel.val == 0f) continue;
                // $"{pose.id}: {pose.dialogPoolLevel.val} > {pose.dialogPoolLevel.val+delta}".Print();
                if((int)pose.dialogPoolLevel.val > currentLevel) pose.dialogPoolLevel.val += delta;
                
                for (int j = 0; j < pose.camAngles.Count; j++)
                {
                    if((int)pose.camAngles[j].dialogPoolLevel.val > currentLevel) pose.camAngles[j].dialogPoolLevel.val += delta;
                }
            }
        }

        public static void AddDialog()
        {
            var dialog = new Dialog(isPoolDialog:true);
            float rightItems = dialogs[currentLevel].Count(x => x.uidItem.rightSide);
            var rightSide = rightItems < dialogs[currentLevel].Count / 2f;
            dialogs[tabbar.id].Add(dialog);
            
            PoseMe.UIElements.Add(PoseMe.CreateDialogUIItem(dialog, rightSide));
        }
        
        public static void AddDialog(JSONClass jc)
        {
            if(jc == null) "Copy a dialog first.".Print();
            var dialog = new Dialog(jc)
            {
                isPoolDialog = true
            };
            float rightItems = dialogs[currentLevel].Count(x => x.uidItem.rightSide);
            var rightSide = rightItems < dialogs[currentLevel].Count / 2f;
            dialogs[tabbar.id].Add(dialog);
            
            PoseMe.UIElements.Add(PoseMe.CreateDialogUIItem(dialog, rightSide));
        }
        
        public static void AddDialog(JSONClass jc, int level)
        {
            var dialog = new Dialog(jc);
            dialog.isPoolDialog = true;
            dialogs[level].Add(dialog);
        }

        public static void RemoveDialog(Dialog dialog)
        {
            dialogs[tabbar.id].Remove(dialog);
            PoseMe.singleton.RemoveUIElement(dialog.uidItem);
            last = null;
        }
        
        public static void InvokeRandom(int level, bool onEnter)
        {
            // if (level < 0) return;
            available.Clear();
            var levelDialogs = dialogs[level];
            for (int i = 0; i < levelDialogs.Count; i++)
            {
                if(levelDialogs[i].onEnter == onEnter) available.Add(levelDialogs[i]);
            }
            last = available.TakeRandom(last);
            last?.Invoke();
        }

        public static void CreateUI()
        {
            PoseMe.singleton.ClearUI();
            var button = PoseMe.singleton.CreateButton("Return");
            button.buttonColor = PoseMe.navColor;
            button.button.onClick.AddListener(CloseUI);
            
            // Utils.SetupTwinButton(PoseMe.singleton, "Add Dialog", AddDialog, "Add Level", () => AddLevel(), false);
            
            
            
            // PoseMe.singleton.SetupButton("Copy Level", true, CopyLevel);
            syncPoseLevels.CreateUI(rightSide:true);
            Utils.SetupTwinButton(PoseMe.singleton, "Copy Level", CopyLevel, "Merge Paste Level", PasteLevel, true);
            // Utils.SetupTwinButton(PoseMe.singleton, "Insert Level", () => InsertLevel(),"Merge Paste Level", PasteLevel, true);
            PoseMe.singleton.SetupButton("Insert Level", true, InsertLevel);
            PoseMe.singleton.SetupButton("Help", false, () => help.Print());
            var twinButton = Utils.SetupTwinButton(PoseMe.singleton, "Save Preset", UISaveJSONDialog, "Merge Load", delegate {}, false);
            loadURL.RegisterFileBrowseButton(twinButton.buttonRight);
            
            
            Utils.SetupTwinButton(PoseMe.singleton, "Add Dialog", AddDialog,"Paste Dialog", () => AddDialog(Dialog.cache), false);
            
            
            // twinButton = Utils.SetupTwinButton(PoseMe.singleton, "Move Level Down", () => MoveLevel(false), "Move Level Up", () => MoveLevel(true), true);
            
            twinButton = Utils.SetupTwinButton(PoseMe.singleton, "Clear Level", ClearLevel,"Delete Level", DeleteLevel, true);
            twinButton.buttonLeft.GetComponentInChildren<Image>().color = PoseMe.severeWarningColor;
            twinButton.buttonRight.GetComponentInChildren<Image>().color = PoseMe.severeWarningColor;
            // PoseMe.singleton.SetupButton("Clear Level", ClearLevel, PoseMe.severeWarningColor);
            // PoseMe.singleton.SetupButton("Delete Last Level", DeleteLastLevel, PoseMe.severeWarningColor, rightSide:true);
            tabbar = UIManager.CreateTabBar(levelNames.ToArray(), UISelectLevel);
            tabbar.SelectTab(currentLevel);

            uiOpen = true;
        }

        private static void CopyLevel()
        {
            levelCache = StoreLevel(currentLevel);
        }

        private static void PasteLevel()
        {
            if (levelCache == null)
            {
                "Copy a level first.".Print();
                return;
            }
            LoadLevel(currentLevel, levelCache, true);
            tabbar.SelectTab(currentLevel);
        }

        private static void ClearLevel()
        {
            foreach (var bubble in dialogs[currentLevel])
            {
                PoseMe.singleton.RemoveUIElement(bubble.uidItem);
            }
            dialogs[currentLevel].Clear();
            tabbar.SelectTab(currentLevel);
        }

        private static void MoveLevel(bool up)
        {
            if (up)
            {
                if (currentLevel == dialogs.Count-1)
                {
                    AddLevel(dialogs[currentLevel]);
                    dialogs[currentLevel].Clear();
                }
                else
                {
                    var nextLevel = dialogs[currentLevel + 1];
                    dialogs[currentLevel + 1] = dialogs[currentLevel];
                    dialogs[currentLevel] = nextLevel;
                }
                currentLevel++;
            }
            else
            {
                if (currentLevel == 0) return;
                var previousLevel = dialogs[currentLevel - 1];
                dialogs[currentLevel - 1] = dialogs[currentLevel];
                dialogs[currentLevel] = previousLevel;
                currentLevel--;
            }
            tabbar.SelectTab(currentLevel);
        }
        
        private static void UISaveJSONDialog()
        {
            SuperController.singleton.GetMediaPathDialog(UISaveJSON, "json", saveDir, false, true, false, null, true, null, false, false);
            SuperController.singleton.mediaFileBrowserUI.SetTextEntry(true);
            if(SuperController.singleton.mediaFileBrowserUI.fileEntryField != null){
                string filename = PoseMe.sceneName;
                SuperController.singleton.mediaFileBrowserUI.fileEntryField.text = filename;
                SuperController.singleton.mediaFileBrowserUI.ActivateFileNameField();
            }
        }
        
        private static void UISaveJSON(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            string path1 = path.Replace('\\', '/')+".json";
            JSONClass jc = Store();
            SuperController.singleton.SaveJSON(jc, path1);
            SuperController.singleton.DoSaveScreenshot(path+".jpg");
        }

        private static void UISelectLevel(int id)
        {
            foreach (var bubble in dialogs[currentLevel])
            {
                PoseMe.singleton.RemoveUIElement(bubble.uidItem);
            }
            CreateItems(id);
            currentLevel = id;
        }

        private static void CreateItems(int level)
        {
            for (int i = 0; i < dialogs[level].Count; i++)
            {
                PoseMe.UIElements.Add(PoseMe.CreateDialogUIItem(dialogs[level][i], i%2 == 1));
            }
        }

        private static void CloseUI()
        {
            PoseMe.singleton.ClearUI();
            uiOpen = false;
            PoseMe.singleton.CreateUI();
        }

        public static JSONClass Store()
        {
            var jc = new JSONClass();
            syncPoseLevels.Store(jc);
            for (int i = 0; i < dialogs.Count; i++)
            {
                jc[$"Lvl{i}"] = StoreLevel(i);
            }

            return jc;
        }

        private static JSONArray StoreLevel(int i)
        {
            var ja = new JSONArray();
            foreach (var dialog in dialogs[i])
            {
                ja.Add(dialog.Store());
            }
            return ja;
        }

        public static void Load(JSONClass jc, bool merge)
        {
            syncPoseLevels.Load(jc);
            if (!jc.HasKey("Lvl0")) return;
            foreach (var key in jc.Keys)
            {
                int i;
                if (int.TryParse(key.Substring(3), out i))
                {
                    LoadLevel(i, jc[key], merge);
                }
            }
        }

        private static void LoadLevel(int i, JSONNode ja, bool merge)
        {
            // $"{i} {ja.Childs.Count()}".Print();
            if(i == dialogs.Count) AddLevel();
            else if(!merge) dialogs[i].Clear();
            foreach (var bubble in ja.Childs)
            {
                AddDialog(bubble.AsObject, i);
            }
        }
    }
}