using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Battlehub.RTCommon;
using MacGruber;
using MeshVR;
using MVR.FileManagement;
using MVR.FileManagementSecure;
using SimpleJSON;
using UnityEngine;
using Object = UnityEngine.Object;
using Request = MeshVR.AssetLoader.AssetBundleFromFileRequest;

namespace CheesyFX
{
    public class EmoteManager
    {
        public static JSONStorableBool enabled = new JSONStorableBool("Emotes Enabled", true);
        private static List<object> UIElements = new List<object>();
        private static UIDynamicTabBar tabbar;
        private static int lastTabId;
        
        private static Request particleBundle;
        public static StimSprayer stimulationEmotes;
        public static OrgasmSprayer orgasmEmotes;
        public static OrgasmFadeSprayer orgasmFadeEmotes;

        public static List<string> textureChoices = new List<string>();
        
        private static bool ready;

        private static JSONStorableFloat test;

        public static void Init()
        {
            // Request request = new AssetLoader.AssetBundleFromFileRequest {path = ReadMyLips.packageUid+"Custom/Scripts/CheesyFX/BodyLanguage/assets/particle.assetbundle", callback = OnBundleLoaded};
            // AssetLoader.QueueLoadAssetBundleFromFile(request);
            GetTextureChoices();
            stimulationEmotes = new StimSprayer();
            orgasmEmotes = new OrgasmSprayer();
            orgasmFadeEmotes = new OrgasmFadeSprayer();
            stimulationEmotes.Init();
            orgasmEmotes.Init();
            orgasmFadeEmotes.Init();
            enabled.setCallbackFunction += val =>
            {
                stimulationEmotes.go.SetActive(val);
                orgasmEmotes.go.SetActive(val);
                orgasmFadeEmotes.go.SetActive(val);
            };
        }
        
        // private static void OnBundleLoaded(Request request)
        // {
        //     try
        //     {
        //         particleBundle = request;
        //         var go = request.assetBundle.LoadAsset<GameObject>("assets/particle/gameobject.prefab");
        //         stimulationEmotes.Init();
        //         orgasmEmotes.Init();
        //         orgasmFadeEmotes.Init();
        //         enabled.setCallbackFunction += val =>
        //         {
        //             stimulationEmotes.go.SetActive(val);
        //             orgasmEmotes.go.SetActive(val);
        //             orgasmFadeEmotes.go.SetActive(val);
        //         };
        //         ready = true;
        //     }
        //     catch (Exception e)
        //     {
        //         SuperController.LogError(e.ToString());
        //     }
        // }

        private static void GetTextureChoices()
        {
            int num;
            var dirs = FileManagerSecure.GetDirectories(ReadMyLips.packageUid + "Custom/Scripts/CheesyFX/BodyLanguage/EmoteTextures/");
            foreach (var dir in dirs)
            {
                var folder = dir.Substring(dir.IndexOf("\\") + 1);
                foreach (var file in FileManagerSecure.GetFiles(FileManagerSecure.NormalizePath(dir)).Select(x => x.Substring(x.LastIndexOf("\\")+1)))
                {
                    if (file.Length == 6 && int.TryParse(file.Substring(0, 2), out num))
                    {
                        textureChoices.Add($"{folder}/{file.Substring(0, 2)}");
                    }
                }
            }
        }

        public static void CreateUI()
        {
            ReadMyLips.singleton.presetSystem.CreateUI();
            UIDynamicButton button;
            button = ReadMyLips.singleton.CreateButton("Return");
            button.buttonColor = new Color(0.55f, 0.90f, 1f);
            button.button.onClick.AddListener(
                () =>
                {
                    ReadMyLips.singleton.ClearUI();
                    ReadMyLips.singleton.CreateUI();
                    ReadMyLips.mainWindowOpen = true;
                });
            var spacer = ReadMyLips.singleton.CreateSpacer(true);
            spacer.height = 50f;
            
            tabbar = UIManager.CreateTabBar(new [] {"Stimulation", "Orgasm", "OrgasmFade"}, SelectTab, script:ReadMyLips.singleton);
            tabbar.SelectTab(lastTabId);
            
        }

        private static void SelectTab(int id)
        {
            Utils.RemoveUIElements(ReadMyLips.singleton, UIElements);
            lastTabId = id;
            switch (id)
            {
                case 0:
                {
                    stimulationEmotes.CreateUI(UIElements);
                    break;
                }
                case 1: 
                {
                    orgasmEmotes.CreateUI(UIElements);
                    break;
                }
                case 2: 
                {
                    orgasmFadeEmotes.CreateUI(UIElements);
                    break;
                }
            }
        }

        public static void Destroy()
        {
            // stimulationEmotes.NullCheck();
            // orgasmEmotes.NullCheck();
            // orgasmFadeEmotes.NullCheck();
            stimulationEmotes.Destroy();
            orgasmEmotes.Destroy();
            orgasmFadeEmotes.Destroy();
            // AssetLoader.DoneWithAssetBundleFromFile(particleBundle.path);
        }

        public static JSONClass Store()
        {
            var jc = new JSONClass();
            enabled.Store(jc);
            jc["StimulationEmotes"] = stimulationEmotes.Store();
            jc["OrgasmEmotes"] = orgasmEmotes.Store();
            jc["OrgasmFadeEmotes"] = orgasmFadeEmotes.Store();
            return jc;
        }
        
        public static void Load(JSONClass jc)
        {
            enabled.Load(jc);
            stimulationEmotes.Load(jc["StimulationEmotes"].AsObject);
            orgasmEmotes.Load(jc["OrgasmEmotes"].AsObject);
            orgasmFadeEmotes.Load(jc["OrgasmFadeEmotes"].AsObject);
            // if (ready)
            // {
            //     enabled.Load(jc);
            //     stimulationEmotes.Load(jc["StimulationEmotes"].AsObject);
            //     orgasmEmotes.Load(jc["OrgasmEmotes"].AsObject);
            //     orgasmFadeEmotes.Load(jc["OrgasmFadeEmotes"].AsObject);
            // }
            // else DeferredLoad(jc).Start();
        }

        // private static IEnumerator DeferredLoad(JSONClass jc)
        // {
        //     while (!ready) yield return new WaitForSeconds(.1f);
        //     Load(jc);
        // }
    }
    
    internal static class ColorExt
    {
        public static HSVColor ToHSV(this Color color)
        {
            float h, s, v;
            Color.RGBToHSV(color, out h, out s, out v);
            return new HSVColor
            {
                H = h,
                S = s,
                V = v
            };
        }
        
        public static Color ToRGB(this HSVColor hsv)
        {
            float h, s, v;
            return Color.HSVToRGB(hsv.H, hsv.S, hsv.V);
        }
    }
}