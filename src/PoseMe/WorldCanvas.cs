using System;
using System.Collections.Generic;
using System.Runtime.Remoting;
using SimpleJSON;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CheesyFX
{
    public class WorldCanvas : CanvasSettings
    {
        public bool active;
        private bool syncNeeded = true;
        public static GameObject go;
        private static List<GameObject> buttons = new List<GameObject>();
        public static RectTransform levelNav;
        private static Material worldButtonMat = new Material(Shader.Find("UI/Default"));
        public Color deselectedColor;

        public WorldCanvas(string type) : base(type)
        {
            deselectedColor = new Color(1f, 1f, 1f, buttonTransparency.val);
        }

        public void Sync()
        {
            try
            {
                if (!active)
                {
                    syncNeeded = true;
                    return;
                }

                if (go)
                {
                    SuperController.singleton.RemoveCanvas(go.GetComponent<Canvas>());
                    Object.DestroyImmediate(go);
                }
                buttons.Clear();
                go = Object.Instantiate(PoseMe.buttonGroup, null, false);
                go.SetActive(true);
                go.name = "BL_go";
                go.gameObject.layer = 0;
                go.transform.position = PoseMe.worldCanvasPosition.val;
                // go.transform.localScale = Vector3.one;
                go.transform.localScale = .0005f * Vector3.one;
                go.transform.localEulerAngles = PoseMe.worldCanvasRotation.val + new Vector3(0f, 180f, 0f);
                go.AddComponent<GraphicRaycaster>();
                go.AddComponent<IgnoreCanvas>();
                var canvas = go.GetComponent<Canvas>();
                canvas.worldCamera = Camera.main;
                canvas.renderMode = RenderMode.WorldSpace;
                SuperController.singleton.AddCanvas(canvas);
                // go.transform.PrintChildren();
                for (int i = 0; i < PoseMe.poses.Count; i++)
                {
                    var button = go.transform.GetChild(i).gameObject;
                    buttons.Add(button);
                    button.transform.GetChild(0).GetComponent<ClickListener>().Clone(PoseMe.poses[i].clickListener);
                }
                
                if (go.transform.childCount == PoseMe.poses.Count + 1)
                {
                    levelNav = go.transform.GetChild(PoseMe.poses.Count).GetComponent<RectTransform>();
                    if(!PoseMe.worldCanvasInFront.val)
                    {
                        foreach (var renderer in levelNav.GetComponentsInChildren<Text>())
                        {
                            renderer.material = worldButtonMat;
                        }
                    }
                    // levelNav.GetChild(0).GetChild(4).gameObject.SetActive(false);
                    Object.Destroy(levelNav.GetChild(0).GetChild(4).gameObject);
                    SetLevelNavCallbacks(Story.currentLevel);
                    levelNav.gameObject.SetActive(Story.currentLevel != null);
                }
                LayoutButtons(Story.currentLevel);
                SetInFront(PoseMe.worldCanvasInFront.val);
                syncNeeded = false;
            }
            catch (Exception e)
            {
                SuperController.LogError(e.ToString());
            }
        }

        public void SyncLevelNav(StoryLevel level)
        {
            try
            {
                if(level == null || !go) return;
                if(!levelNav)
                {
                    levelNav = Object.Instantiate(level.buttonPanel, go.transform);
                    Object.Destroy(levelNav.GetChild(0).GetChild(4).gameObject);
                    levelNav.gameObject.layer = 0;
                    if (!PoseMe.worldCanvasInFront.val)
                    {
                        foreach (var renderer in levelNav.GetComponentsInChildren<Text>())
                        {
                            renderer.material = worldButtonMat;
                        }
                    }
                }
                levelNav.gameObject.SetActive(true);
                SetLevelNavCallbacks(level);
                LayoutButtons(level);
                // "Sync".Print();
            }
            catch (Exception e)
            {
                SuperController.LogError(e.ToString());
            }
        }

        private void SetLevelNavCallbacks(StoryLevel level)
        {
            // levelNav?.GetChild(0).PrintChildren();
            if(level == null) return;
            Button button;
            var m0 = levelNav.GetChild(0);
            button = m0.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(ToggleButtons);
            
            button = m0.GetChild(1).GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(Story.Next);
            
            button = m0.GetChild(2).GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(Story.Previous);
            
            var m1 = m0.GetChild(3);
            button = m1.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(ToggleButtons);
            
            button = m1.GetChild(1).GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(level.Next);
            
            button = m1.GetChild(2).GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(level.Previous);
        }

        public void SyncLevelNavText(int id, string val)
        {
            var m0 = levelNav.GetChild(0);
            switch (id)
            {
                case 0:
                {
                    m0.GetChild(0).GetComponent<Text>().text = val;
                    break;
                }
                case 1:
                {
                    m0.GetChild(3).GetChild(0).GetComponent<Text>().text = val;
                    break;
                }
                // case 2:
                // {
                //     m0.GetChild(3).GetChild(0).GetComponent<Text>().text = val;
                //     break;
                // }
            }
        }

        public void SetActive(bool val)
        {
            active = val;
            if(val)
            {
                if(syncNeeded) Sync();
                else go.SetActive(true);
            }
            else if(go) go.SetActive(false);
        }

        public void SetButtonActive(int id, bool val)
        {
            if(!go) return;
            if (id >= buttons.Count)
            {
                return;
            }
            if (val)
            {
                buttons[id].transform.GetComponent<Image>().color = Pose.selectedBGColor;
                buttons[id].transform.GetChild(0).GetComponent<Image>().color = Pose.selectedColor;
            }
            else
            {
                buttons[id].transform.GetComponent<Image>().color = Pose.deselectedBGColor;
                buttons[id].transform.GetChild(0).GetComponent<Image>().color = deselectedColor;
            }
        }

        public static void SetButtonText(Pose pose, string val)
        {
            if(!go) return;
            var text = buttons[pose.id].transform.GetChild(0).GetChild(0).GetComponent<Text>();
            if(pose.camAngles.Count > 1)
            {
                
                text.text = val;
                text.alignment = TextAnchor.LowerRight;
                text.color = Color.white;
            }
            else text.color = Color.clear;
        }
        
        public void LayoutButtons(StoryLevel level)
        {
            if (level == null)
            {
                LayoutButtons();
                return;
            }
            int columnCount = (int)Math.Ceiling((float)(level.maxId - level.minId + 1) / maxRows.val);
            var totalHeight = ((level.maxId - level.minId) / columnCount+1) * (buttonSize.val + buttonSpacing.val);
            int row = 0, column = 0;
            for (int i = 0; i < level.minId; i++)
            {
                buttons[i].SetActive(false);
            }

            for (int i = level.maxId + 1; i < buttons.Count; i++)
            {
                buttons[i].SetActive(false);
            }

            for (var i = 0; i <= level.maxId - level.minId; i++)
            {
                var rt = buttons[i + level.minId].GetComponent<RectTransform>();
                rt.gameObject.SetActive(true);
                column = i % columnCount;
                row = i / columnCount;
                rt.offsetMax = new Vector2(-(columnCount - column - 1) * (buttonSize.val + buttonSpacing.val),
                    totalHeight-row * (buttonSize.val + buttonSpacing.val));
                rt.offsetMin = rt.offsetMax - new Vector2(buttonSize.val, buttonSize.val);
            }
            
            if(levelNav)
            {
                // var camButtonOffset = levelNav.childCount == 5 && levelNav.GetChild(4).gameObject.activeInHierarchy? 100f : 0f;
                
                if(!PoseMe.worldLevelNavOnTop.val)
                {
                    levelNav.offsetMax = levelNav.offsetMin =
                        buttons[level.minId + row * columnCount].GetComponent<RectTransform>().offsetMax -
                        new Vector2(buttonSize.val, -22);
                }
                else
                {
                    if (columnCount % 2 == 0)
                    {
                        levelNav.offsetMax = levelNav.offsetMin = 
                            buttons[level.minId + columnCount / 2].GetComponent<RectTransform>().offsetMin + new Vector2(185, 250+buttonSize.val);
                    }
                    else
                    {
                        levelNav.offsetMax = levelNav.offsetMin = 
                            buttons[level.minId + columnCount / 2].GetComponent<RectTransform>().offsetMin + new Vector2(185+.5f*buttonSize.val, 250+buttonSize.val);
                    }
                }
            }
        }
        
        public void LayoutButtons()
        {
            float buttonH;
            var buttonW = buttonH = buttonSize.val;
            int row = 0, column = 0;
            int columnCount = (int)Math.Ceiling((float)buttons.Count / maxRows.val);
            var totalHeight = ((buttons.Count - 1) / columnCount+1) * (buttonH + buttonSpacing.val);
            for (var i = 0; i < buttons.Count; i++)
            {
                var rt = buttons[i].GetComponent<RectTransform>();
                if (!rt.gameObject.activeSelf) rt.gameObject.SetActive(true);
                column = i % columnCount;
                row = i / columnCount;
                rt.offsetMax = new Vector2(-(columnCount - column - 1) * (buttonW + buttonSpacing.val),
                    totalHeight-row * (buttonH + buttonSpacing.val));
                rt.offsetMin = rt.offsetMax - new Vector2(buttonW, buttonH);
            }
        }

        public void ToggleButtons()
        {
            PoseMe.showWorldButtons.val = !PoseMe.showWorldButtons.val;
            for (var i = 0; i < buttons.Count; i++)
            {
                var button = buttons[i];
                if (button.activeSelf) button.SetActive(PoseMe.showWorldButtons.val);
            }

            if (!PoseMe.showWorldButtons.val)
            {
                if (levelNav) levelNav.offsetMax = new Vector2(0, 225);
            }
            else LayoutButtons(Story.currentLevel);
        }

        public void SetInFront(bool val)
        {
            if (val)
            {
                var mat = PoseMe.singleton.manager.configurableButtonPrefab.GetComponent<Image>().material;
                foreach (Transform child in go.transform)
                {
                    foreach (var img in child.GetComponentsInChildren<Image>())
                    {
                        img.material = mat;
                    }
                }
                if (levelNav)
                {
                    foreach (var image in levelNav.GetComponentsInChildren<Text>())
                    {
                        image.material = mat;
                    }
                }
            }
            else
            {
                foreach (Transform child in go.transform)
                {
                    foreach (var img in child.GetComponentsInChildren<Image>())
                    {
                        img.material = worldButtonMat;
                    }
                }
                if (levelNav)
                {
                    foreach (var image in levelNav.GetComponentsInChildren<Text>())
                    {
                        image.material = worldButtonMat;
                    }
                }
            }
        }

        public override void Update()
        {
            base.Update();
            deselectedColor.a = buttonTransparency.val;
        }

        public void Destroy()
        {
            SuperController.singleton.RemoveCanvas(go.GetComponent<Canvas>());
            if(go) Object.DestroyImmediate(go);
            Object.Destroy(worldButtonMat);
        }
    }
}