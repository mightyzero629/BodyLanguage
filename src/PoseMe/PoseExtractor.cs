using System;
using System.Collections.Generic;
using System.Linq;
using MacGruber;
using MVR.FileManagementSecure;
using SimpleJSON;
using UnityEngine;
using UnityEngine.UI;

namespace CheesyFX
{
    public static class PoseExtractor
    {
        private static JSONStorableUrl loadURL;
        private static string scenesDir = "Saves/scene/";
        // private static JSONClass loadedScene;
        private static List<JSONStorableBool> poseToggles = new List<JSONStorableBool>();
        public static List<Atom> mappedAtoms = new List<Atom>();
        private static JSONClass poseMeJC;
        private static JSONArray poses;
        // private static List<object> UIElements = new List<object>();
        private static bool insane;
        
        public static List<ActorMapping> actorMappings = new List<ActorMapping>();

        public static void Init()
        {
            loadURL = new JSONStorableUrl("loadURL", "", UILoadJSON, "json", true);
            loadURL.showDirs = true;
            loadURL.hideExtension = true;
            // loadURL.setCallbackFunction -= UILoadJSON;
            loadURL.allowFullComputerBrowse = true;
            loadURL.allowBrowseAboveSuggestedPath = true;
            loadURL.SetFilePath(scenesDir);
            loadURL.shortCuts = new List<ShortCut>{new ShortCut()};
            // SuperController.singleton.savesDirResolved.Print();
            // FileManagerSecure.GetShortCutsForDirectory(SuperController.singleton.savesDirResolved+"scene",true, generateAllFlattenedShortcut: true, includeRegularDirsInFlattenedShortcut: true).ForEach(x => x.path.Print());
            // loadURL.SetFilePath("Saves/PluginData/CheesyFX/");
            // loadURL.setCallbackFunction += UILoadJSON;
        }
        
        public static void UILoadJSON(string url)
        {
            if(!FileManagerSecure.FileExists(url)) return;
            ClearScene();
            GetPoses(SuperController.singleton.LoadJSON(url).AsObject);
            CreatePosesUI();
        }

        private static void GetPoses(JSONClass scene)
        {
            foreach (var atom in scene["atoms"].Childs)
            {
                var PoseMeNode = atom["storables"].AsArray.Childs
                    .FirstOrDefault(x => x["id"].Value.EndsWith("CheesyFX.PoseMe"));
                if (PoseMeNode == null) continue;
                var poseMe = PoseMeNode.AsObject;
                if (!poseMe.HasKey("poses") || !poseMe["poses"].Childs.Any()) continue;
                poses = poseMe["poses"].AsArray;
                poseMeJC = poseMe;
                foreach (var pose in poses.Childs)
                {
                    Image image = null;
                    var jbool = new JSONStorableBool("", true);
                    poseToggles.Add(jbool);
                }

                break;
            }
            if(poses == null) $"The scene '{loadURL.val}' doesn't contain BodyLanguage poses.".Print();
        }

        private static void CreatePosesUI()
        {
            // poseToggles.Clear();
            foreach (var pose in poses.Childs)
            {
                var poseJC = pose["pose"].AsObject;
                foreach (var name in poseJC.Keys)
                {
                    var actorMapping = actorMappings.FirstOrDefault(x => x.storedUid == name);
                    if (actorMapping == null)
                    {
                        actorMappings.Add(new ActorMapping(name, poseJC[name].AsObject));
                    }
                    else if (!actorMapping.mapping.inputField) actorMapping.CreateUI();
                }
            }
            PoseMe.singleton.SetupButton("Select All", false, SelectAll, PoseMe.UIElements);
            PoseMe.singleton.SetupButton("Inverse Selection", true, InverseSelection, PoseMe.UIElements);
            var poseArray = poses.Childs.ToArray();
            for (int i = 0; i< poseToggles.Count; i++)
            {
                Image image = null;
                var jbool = poseToggles[i];
                var uiDynamicToggle = PoseMe.singleton.CreateToggle(jbool, i%2==1);
                uiDynamicToggle.ForceHeight(512f);
                PoseMe.UIElements.Add(jbool);
                var texture = LoadTexture(Convert.FromBase64String(poseArray[i]["cams"].AsArray[0]["image"].Value));
                image = uiDynamicToggle.GetComponentInChildren<Image>();
                image.sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                jbool.setCallbackFunction = val =>
                {
                    image.color = val ? Color.white : new Color(1f, 1f, 1f, .85f);
                };
                if(!jbool.val) jbool.setCallbackFunction.Invoke(false);
                var rt = jbool.toggle.gameObject.transform.GetChild(1) as RectTransform;
                rt.anchorMin = new Vector2(0f, .945f);
                rt.anchorMax = new Vector2(.01f, .955f);
            }
        }

        public static void ClearScene()
        {
            PoseMe.singleton.RemoveUIElements(PoseMe.UIElements);
            poses = null;
            poseToggles.Clear();
            actorMappings.Clear();
            mappedAtoms.Clear();
            CreateUI();
            
        }

        public static void SelectAll()
        {
            for (int i = 0; i < poseToggles.Count; i++)
            {
                poseToggles[i].val = true;
            }
        }
        
        public static void InverseSelection()
        {
            for (int i = 0; i < poseToggles.Count; i++)
            {
                poseToggles[i].val = !poseToggles[i].val;
            }
        }

        private static void LoadPoses()
        {
            if (actorMappings.Any(x => x.insane))
            {
                "Not possible to load. Fix the mapping errors.".Print();
                return;
            }

            if (poseMeJC.HasKey("BubblePool"))
            {
                foreach (JSONClass bubble in poseMeJC["BubblePool"].AsArray)
                {
                    bubble["Person"] = actorMappings.First(x => x.storedUid == bubble["Person"].Value).mapping.val;
                }
            }
            for (int i = 0; i < poseToggles.Count; i++)
            {
                if(!poseToggles[i].val) continue;
                var poseJC = poses[i].AsObject;
                var copyJSON = JSON.Parse(poseJC.ToString()).AsObject;
                var copyPoseJSON = copyJSON["pose"].AsObject;
                
                if (copyJSON.HasKey("Bubbles"))
                {
                    foreach (JSONClass bubble in copyJSON["Bubbles"].AsArray)
                    {
                        bubble["Person"] = actorMappings.First(x => x.storedUid == bubble["Person"].Value).mapping.val;
                    }
                }

                foreach (var actorMapping in actorMappings)
                {
                    if(actorMapping.mapping.val == "" || actorMapping.mapping.val == actorMapping.storedUid) continue;
                    {
                        var uid = actorMapping.storedUid;
                        if (!poseJC["pose"].AsObject.HasKey(uid)) continue;
                        // var jc = JSON.Parse(copyPoseJSON[uid].ToString());
                        // copyPoseJSON[actorMapping.mapping.val] = jc;
                        copyPoseJSON[actorMapping.mapping.val] = poseJC["pose"][uid];
                        // copyPoseJSON.Remove(uid);
                        // if(actorMapping.mapping.val != "") $"{uid} >> {actorMapping.mapping.val}".Print();
                        // else $"{uid} >> not mapped".Print();
                    }
                }

                var actors = copyPoseJSON.Keys.ToArray();
                foreach (var actor in actors)
                {
                    if (actorMappings.All(x => x.mapping.val != actor))
                    {
                        copyPoseJSON.Remove(actor);
                    }
                }
                PoseMe.AddPose(copyJSON);
                // if(i == 0) SuperController.singleton.SaveJSON(copyJSON, "Saves/PluginData/CheesyFX/copy.json");
            }
        }

        public static void CreateUI()
        {
            var button = PoseMe.singleton.SetupButton("Browse Scene", false, () =>
            {
                loadURL.shortCuts = FileManagerSecure.GetShortCutsForDirectory(
                    scenesDir, true, generateAllFlattenedShortcut: true,
                    includeRegularDirsInFlattenedShortcut: true);
                loadURL.FileBrowse();
            }, PoseMe.UIElements);
            // loadURL.RegisterFileBrowseButton(button.button);
            PoseMe.UIElements.Add(Utils.SetupTwinButton(PoseMe.singleton, "Load", LoadPoses, "Clear", ClearScene, true));
            if (poses != null)
            {
                CreatePosesUI();
            }
        }
        
        private static Texture2D LoadTexture(byte[] image)
        {
            var tex = new Texture2D(2, 2);
            tex.LoadImage(image);
            tex.Apply();
            return tex;
        }
    }
}