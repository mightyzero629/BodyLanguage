
using System.Collections.Generic;
using System;
using System.CodeDom;
using System.Linq;
using Battlehub.Utils;
using MacGruber;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CheesyFX {
    public class UIManager
    {
        public static MVRScript script;
        public static bool UIOpened => script.UITransform.gameObject.activeSelf;
        public static int tabLevel;

        public static List<Func<List<object>>> tabActions = new List<Func<List<object>>>
            { null, null, null, null, null };

        // public static List<Func<int, List<object>>> tabActions = new List<Func<int, List<object>>>();
        public static List<int> tabMenuItems = new List<int> { 0, 0, 0, 0, 0 };

        // public static List<Action> returnToTabActions = new List<Action>{null,null,null};
        public static List<Action> windowActions = new List<Action>();

        // public static Action currentWindowAction;
        public static List<List<object>> UIElements = new List<List<object>>
            { new List<object>(), new List<object>(), new List<object>() };

        public static List<object> UIItems = new List<object>();
        public static List<object> newWindowElements = new List<object>();
        public static readonly Atom cameraRig = SuperController.singleton.GetAtomByUid("[Camera Rig]");

        public static void ClearUI()
        {
            Utils.RemoveUIElements(script, UIElements.SelectMany(x => x).ToList());
            Utils.RemoveUIElements(script, newWindowElements);
            UIElements.ForEach(x => x.Clear());
            newWindowElements.Clear();
            ClearItems();
        }

        public static void ClearTab(int level)
        {
            // level.Print();
            Utils.RemoveUIElements(script, UIElements[level]);
            UIElements[level].Clear();
            // lastUIActions = lastUIActions.Take(level).ToList();
            ClearItems();
        }

        public static void ClearItems()
        {
            Utils.RemoveUIElements(script, UIItems);
            UIItems.Clear();
        }

        public static void Return()
        {
            ClearUI();
            int n = windowActions.Count;
            if (n == 1)
            {
                for (int i = 0; i < tabActions.Count; i++)
                {
                    if (tabActions[i] != null)
                    {
                        UIElements[i] = tabActions[i]();
                        tabLevel = i;
                        "tabActions.Count".Print();
                    }
                    else break;
                }

                windowActions.Clear();
            }
            else
            {
                windowActions.RemoveAt(n - 1);
                windowActions[n - 2]();
            }
        }

        // public static void Return(){
        //     ClearUI();
        //     int n = windowActions.Count;
        //     if(n == 1){
        //         for(int i=0; i<tabActions.Count; i++){
        //             if(tabActions[i] != null){
        //                 tabActions[i](tabMenuItems[i]);
        //                 tabLevel = i;
        //             }
        //             else break;
        //         }
        //         windowActions.Clear();
        //     }
        //     else{
        //         windowActions.RemoveAt(n-1);
        //         windowActions[n-2]();
        //     }
        // }

        public static Action NewWindow(Action windowAction)
        {
            return () =>
            {
                ClearUI();
                windowActions.Add(windowAction);
                windowAction();
                // windowActions.Count.Print();
            };
        }

        public static void RefreshWindow()
        {
            ClearUI();
            windowActions[windowActions.Count - 1]();
        }

        public static void SelectTab(Func<List<object>> createTab, int newTabLevel, bool clear = true)
        {
            if (newTabLevel <= tabLevel && clear)
            {
                for (int i = newTabLevel; i <= tabLevel; i++)
                {
                    ClearTab(i);
                }
            }

            ClearItems();
            tabLevel = newTabLevel;

            tabActions[newTabLevel] = createTab;

            if (clear) UIElements[tabLevel] = createTab();
            else createTab();

        }

        public static void SetTabElements(List<object> newUIElements)
        {
            UIElements[tabLevel] = newUIElements;
        }

        public static UIDynamicButton CreateButton(string label, Action action, List<object> UIElements = null,
            bool rightSide = false, string audioName = null)
        {
            UIDynamicButton button = script.CreateButton(label, rightSide);
            button.button.onClick.AddListener(new UnityAction(action));
            // UIMouseOver uiMouseOver = ((UIDynamic)button).AddMouseOver();
            // uiMouseOver.name = audioName == null ? label : audioName;
            if (UIElements != null) UIElements.Add(button);

            return button;
        }

        public static UIDynamicTwinToggle CreateTwinToggle(string leftLabel = "", Action leftCallback = null,
            string rightLabel = "", Action rightCallback = null, bool rightSide = false, List<object> UIElements = null,
            string leftAudioName = null, string rightAudioName = null)
        {
            if (leftCallback == null) leftCallback = delegate { };
            if (rightCallback == null) rightCallback = delegate { };
            UIDynamicTwinToggle twinToggle = Utils.SetupTwinToggle(script, leftLabel, new UnityAction(leftCallback),
                rightLabel, new UnityAction(rightCallback), rightSide);
            // UIMouseOver uiMouseOver = (twinToggle.toggleLeft).AddMouseOver();
            // uiMouseOver.name = leftAudioName == null ? leftLabel : leftAudioName;
            //
            // uiMouseOver = (twinToggle.toggleRight).AddMouseOver();
            // uiMouseOver.name = rightAudioName == null ? rightLabel : rightAudioName;

            if (UIElements != null) UIElements.Add(twinToggle);
            return twinToggle;
        }

        public static CreateUIElement ourCreateUIElement;

        public delegate Transform CreateUIElement(Transform prefab, bool rightSide);

        public static void OnInitUI(CreateUIElement createUIElementCallback)
        {
            ourCreateUIElement = createUIElementCallback;
        }

        public static void SetScript(MVRScript mvrscript, CreateUIElement createUIElementCallback, List<Transform> leftUIEls,
            List<Transform> rightUIEls)
        {
            script = mvrscript;
            ourCreateUIElement = createUIElementCallback;
            leftUIElements = leftUIEls;
            rightUIElements = rightUIEls;
        }
        
        public static UIDynamicTabBarWithToggle CreateTabBarWithTogle(JSONStorableBool jsb, string[] menuItems, Action<int> selectTab, int columns = -1,
            MVRScript script = null, bool invokeDefault = true, float width = 1f)
        {
            if (script == null) script = UIManager.script;
            if (columns == -1) columns = menuItems.Length+1;
            float rowCount = Mathf.Ceil((float)menuItems.Length / columns);
            float totalHeight = 60 + 45f * (rowCount - 1);
            GameObject tabbarPrefab = new GameObject("TabBar");
            RectTransform rt = tabbarPrefab.AddComponent<RectTransform>();
            rt.anchorMax = new Vector2(0, 1);
            rt.anchorMin = new Vector2(0, 1);
            rt.offsetMax = new Vector2(1064, -500);
            rt.offsetMin = new Vector2(10, -600);
            LayoutElement le = tabbarPrefab.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.minHeight = totalHeight;
            le.minWidth = 1064;
            le.preferredHeight = totalHeight;
            le.preferredWidth = 1064;

            RectTransform backgroundTransform =
                script.manager.configurableScrollablePopupPrefab.transform.Find("Background") as RectTransform;
            backgroundTransform = UnityEngine.Object.Instantiate(backgroundTransform, tabbarPrefab.transform);
            backgroundTransform.name = "Background";
            backgroundTransform.anchorMax = new Vector2(1, 1);
            backgroundTransform.anchorMin = new Vector2(0, 0);
            //backgroundTransform.offsetMax = new Vector2(0, -15);
            //backgroundTransform.offsetMin = new Vector2(0, 15);
            
            UIDynamicTabBarWithToggle uid = tabbarPrefab.AddComponent<UIDynamicTabBarWithToggle>();
            float padding = 5.0f;
            float itemWidth, rowOffset;
            itemWidth = 1044f * width / columns - padding;
            float x = 15;
            
            rt = Object.Instantiate(
                script.manager.configurableTogglePrefab.transform,
                tabbarPrefab.transform,
                false) as RectTransform;
            uid.toggleUid = rt.GetComponent<UIDynamicToggle>();
            // rt.Find("Text").GetComponent<Text>().fontSize = 24;
            // uiDynamicV4Slider.sliders.Add(uiDynamicSlider);
            rt.anchorMax = new Vector2(0,1);
            rt.anchorMin = new Vector2(0,1);
            rt.offsetMax = new Vector2(x+itemWidth, -10);
            rt.offsetMin = new Vector2(15, -50);
            rt.gameObject.SetActive(true);

            x += itemWidth + padding;
            for (int i = 1; i < menuItems.Length+1; ++i)
            {
                int rowNumber = i / columns;
                rowOffset = 45f * rowNumber;
                if (i % columns == 0) x = 15f;
                RectTransform buttonTransform =
                    script.manager.configurableScrollablePopupPrefab.transform.Find("Button") as RectTransform;
                buttonTransform = UnityEngine.Object.Instantiate(buttonTransform, tabbarPrefab.transform);
                buttonTransform.name = "Button";
                buttonTransform.anchorMax = new Vector2(0, 1);
                buttonTransform.anchorMin = new Vector2(0, 1);
                buttonTransform.offsetMax = new Vector2(x + itemWidth, -10 - rowOffset);
                buttonTransform.offsetMin = new Vector2(x, -50 - rowOffset);
                Button buttonButton = buttonTransform.GetComponent<Button>();
                uid.buttons.Add(buttonButton);
                Text buttonText = buttonTransform.Find("Text").GetComponent<Text>();
                buttonText.text = menuItems[i-1];
                x += itemWidth + padding;
            }

            Transform t = ourCreateUIElement(tabbarPrefab.transform, false);
            UIDynamicTabBarWithToggle tabBar = t.gameObject.GetComponent<UIDynamicTabBarWithToggle>();
            for (int i = 0; i < tabBar.buttons.Count; ++i)
            {
                int menuID = i;
                tabBar.buttons[i].onClick.AddListener(() =>
                {
                    tabBar.lastId = tabBar.id;
                    tabBar.id = menuID;
                    for (int j = 0; j < tabBar.buttons.Count; ++j) tabBar.buttons[j].interactable = (j != menuID);
                    selectTab(menuID);
                });
                ColorBlock cb = ColorBlock.defaultColorBlock;
                cb.disabledColor = new Color(0.55f, 0.90f, 1f);
                cb.highlightedColor *= .8f;
                tabBar.buttons[i].colors = cb;
            }
            
            jsb.RegisterToggle(tabBar.toggleUid.toggle);
            tabBar.toggleUid.label = jsb.name;

            Object.Destroy(tabbarPrefab);
            tabBar.spacer = script.CreateSpacer(true);
            tabBar.spacer.height = totalHeight;
            tabBar.SelectTab = val => tabBar.buttons[val].onClick.Invoke();
            tabBar.SelectLast = () => tabBar.SelectTab(tabBar.id);
            if (invokeDefault) tabBar.SelectTab(tabBar.id);
            return tabBar;
        }

        public static UIDynamicTabBar CreateTabBar(string[] menuItems, Action<int> selectTab, int columns = -1,
            MVRScript script = null, bool invokeDefault = false, float width = 1f, float itemHeight = 50f)
        {
            if (script == null) script = UIManager.script;
            if (columns == -1 || columns >= menuItems.Length) columns = menuItems.Length;
            float rowCount = Mathf.Ceil((float)menuItems.Length / columns);
            float totalHeight = 10f + itemHeight * (rowCount);
            UIDynamicTabBar tabBar;
            GameObject tabbarPrefab = new GameObject("TabBar");
            RectTransform rt = tabbarPrefab.AddComponent<RectTransform>();
            rt.anchorMax = new Vector2(0, 1);
            rt.anchorMin = new Vector2(0, 1);
            rt.offsetMax = new Vector2(1064, -500);
            rt.offsetMin = new Vector2(10, -600);
            LayoutElement le = tabbarPrefab.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.minHeight = totalHeight;
            le.minWidth = 1064;
            le.preferredHeight = totalHeight;
            le.preferredWidth = 1064;

            RectTransform backgroundTransform =
                script.manager.configurableScrollablePopupPrefab.transform.Find("Background") as RectTransform;
            backgroundTransform = Object.Instantiate(backgroundTransform, tabbarPrefab.transform);
            backgroundTransform.name = "Background";
            backgroundTransform.anchorMax = new Vector2(1, 1);
            backgroundTransform.anchorMin = new Vector2(0, 0);
            //backgroundTransform.offsetMax = new Vector2(0, -15);
            //backgroundTransform.offsetMin = new Vector2(0, 15);

            UIDynamicTabBar uid = tabbarPrefab.AddComponent<UIDynamicTabBar>();
            float padding = 5.0f;
            float itemWidth, rowOffset;
            itemWidth = 1044f * width / columns - padding;

            float x = 15;
            for (int i = 0; i < menuItems.Length; ++i)
            {
                int rowNumber = i / columns;
                rowOffset = itemHeight * rowNumber;
                if (i % columns == 0) x = 15f;
                RectTransform buttonTransform =
                    script.manager.configurableScrollablePopupPrefab.transform.Find("Button") as RectTransform;
                buttonTransform = UnityEngine.Object.Instantiate(buttonTransform, tabbarPrefab.transform);
                buttonTransform.name = "Button";
                buttonTransform.anchorMax = new Vector2(0, 1);
                buttonTransform.anchorMin = new Vector2(0, 1);
                buttonTransform.offsetMax = new Vector2(x + itemWidth, -10 - rowOffset);
                buttonTransform.offsetMin = new Vector2(x, -itemHeight - rowOffset);
                Button buttonButton = buttonTransform.GetComponent<Button>();
                uid.buttons.Add(buttonButton);
                Text buttonText = buttonTransform.Find("Text").GetComponent<Text>();
                buttonText.text = menuItems[i];
                x += itemWidth + padding;
            }
            
            Transform t = ourCreateUIElement(tabbarPrefab.transform, false);
            tabBar = t.gameObject.GetComponent<UIDynamicTabBar>();
            for (int i = 0; i < tabBar.buttons.Count; ++i)
            {
                int menuID = i;
                tabBar.buttons[i].onClick.AddListener(() =>
                {
                    tabBar.lastId = tabBar.id;
                    tabBar.id = menuID;
                    for (int j = 0; j < tabBar.buttons.Count; ++j) tabBar.buttons[j].interactable = (j != menuID);
                    selectTab(menuID);
                });
                ColorBlock cb = ColorBlock.defaultColorBlock;
                cb.disabledColor = new Color(0.55f, 0.90f, 1f);
                cb.highlightedColor *= .8f;
                tabBar.buttons[i].colors = cb;
            }

            Object.Destroy(tabbarPrefab);
            tabBar.spacer = script.CreateSpacer(true);
            tabBar.spacer.height = totalHeight;
            tabBar.SelectTab = val => tabBar.buttons[val].onClick.Invoke();
            tabBar.SelectLast = () => tabBar.SelectTab(tabBar.id);
            if (invokeDefault) tabBar.SelectTab(tabBar.id);
            return tabBar;
        }

        public static UIDynamicTabBar CreateTabBar(List<object> UIElements, string[] menuItems,
            Func<int, List<object>> selectTab, int level = 0, int columns = -1, float width = 1f)
        {
            if (columns == -1) columns = menuItems.Length;
            float rowCount = Mathf.Ceil((float)menuItems.Length / columns);
            float totalHeight = 90 + 45f * (rowCount - 1);
            UIDynamicTabBar tabBar;
            GameObject tabbarPrefab = new GameObject("TabBar");
            RectTransform rt = tabbarPrefab.AddComponent<RectTransform>();
            rt.anchorMax = new Vector2(0, 1);
            rt.anchorMin = new Vector2(0, 1);
            rt.offsetMax = new Vector2(1064, -500);
            rt.offsetMin = new Vector2(10, -600);
            LayoutElement le = tabbarPrefab.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.minHeight = totalHeight;
            le.minWidth = 1064;
            le.preferredHeight = totalHeight;
            le.preferredWidth = 1064;

            RectTransform backgroundTransform =
                script.manager.configurableScrollablePopupPrefab.transform.Find("Background") as RectTransform;
            backgroundTransform = UnityEngine.Object.Instantiate(backgroundTransform, tabbarPrefab.transform);
            backgroundTransform.name = "Background";
            backgroundTransform.anchorMax = new Vector2(1, 1);
            backgroundTransform.anchorMin = new Vector2(0, 0);
            //backgroundTransform.offsetMax = new Vector2(0, -15);
            //backgroundTransform.offsetMin = new Vector2(0, 15);

            UIDynamicTabBar uid = tabbarPrefab.AddComponent<UIDynamicTabBar>();
            float padding = 5.0f;
            float itemWidth, rowOffset;
            itemWidth = 1024f * width / columns - padding;

            float x = 15;
            for (int i = 0; i < menuItems.Length; ++i)
            {
                int rowNumber = i / columns;
                rowOffset = 45f * rowNumber;
                if (i % columns == 0) x = 15f;
                RectTransform buttonTransform =
                    script.manager.configurableScrollablePopupPrefab.transform.Find("Button") as RectTransform;
                buttonTransform = UnityEngine.Object.Instantiate(buttonTransform, tabbarPrefab.transform);
                buttonTransform.name = "Button";
                buttonTransform.anchorMax = new Vector2(0, 1);
                buttonTransform.anchorMin = new Vector2(0, 1);
                buttonTransform.offsetMax = new Vector2(x + itemWidth, -25 - rowOffset);
                buttonTransform.offsetMin = new Vector2(x, -65 - rowOffset);
                Button buttonButton = buttonTransform.GetComponent<Button>();
                uid.buttons.Add(buttonButton);
                Text buttonText = buttonTransform.Find("Text").GetComponent<Text>();
                buttonText.text = menuItems[i];
                x += itemWidth + padding;
            }

            Transform t = ourCreateUIElement(tabbarPrefab.transform, false);
            tabBar = t.gameObject.GetComponent<UIDynamicTabBar>();
            for (int i = 0; i < tabBar.buttons.Count; ++i)
            {
                int menuID = i;
                tabBar.buttons[i].onClick.AddListener(() =>
                {
                    tabBar.lastId = tabBar.id;
                    tabBar.id = menuID;
                    for (int j = 0; j < tabBar.buttons.Count; ++j) tabBar.buttons[j].interactable = (j != menuID);
                    SelectTab(() => selectTab(menuID), level);
                });
                ColorBlock cb = ColorBlock.defaultColorBlock;
                cb.disabledColor = new Color(0.55f, 0.90f, 1f);
                cb.highlightedColor *= .8f;
                tabBar.buttons[i].colors = cb;
            }

            UnityEngine.Object.Destroy(tabbarPrefab);
            UIElements.Add(tabBar);
            UIElements.Add(CreateMenuSpacer(UIElements, totalHeight, true));
            tabBar.buttons[tabBar.id].onClick.Invoke();
            return tabBar;
        }
        
        public static UIDynamicTabBarWithBG CreateTabBarWithBG(string[] menuItems, Action<int> selectTab, int columns = -1,
            MVRScript script = null, bool invokeDefault = false, float width = 1f, float itemHeight = 50f, Color bgColor = new Color())
        {
            if (script == null) script = UIManager.script;
            if (columns == -1 || columns >= menuItems.Length) columns = menuItems.Length;
            float rowCount = Mathf.Ceil((float)menuItems.Length / columns);
            float totalHeight = 10f + itemHeight * (rowCount);
            UIDynamicTabBarWithBG tabBar;
            GameObject tabbarPrefab = new GameObject("TabBar");
            RectTransform rt = tabbarPrefab.AddComponent<RectTransform>();
            rt.anchorMax = new Vector2(0, 1);
            rt.anchorMin = new Vector2(0, 1);
            rt.offsetMax = new Vector2(1064, -500);
            rt.offsetMin = new Vector2(10, -600);
            LayoutElement le = tabbarPrefab.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.minHeight = totalHeight;
            le.minWidth = 1064;
            le.preferredHeight = totalHeight;
            le.preferredWidth = 1064;

            RectTransform backgroundTransform =
                script.manager.configurableScrollablePopupPrefab.transform.Find("Background") as RectTransform;
            backgroundTransform = Object.Instantiate(backgroundTransform, tabbarPrefab.transform);
            backgroundTransform.name = "Background";
            backgroundTransform.anchorMax = new Vector2(1, 1);
            backgroundTransform.anchorMin = new Vector2(0, 0);
            //backgroundTransform.offsetMax = new Vector2(0, -15);
            //backgroundTransform.offsetMin = new Vector2(0, 15);

            UIDynamicTabBarWithBG uid = tabbarPrefab.AddComponent<UIDynamicTabBarWithBG>();
            float padding = 5.0f;
            float itemWidth, rowOffset;
            itemWidth = 1044f * width / columns - padding;

            float x = 15;
            for (int i = 0; i < menuItems.Length; ++i)
            {
                int rowNumber = i / columns;
                rowOffset = itemHeight * rowNumber;
                if (i % columns == 0) x = 15f;
                var bg = Object.Instantiate(Pose.blankButtonPrefab, tabbarPrefab.transform);
                bg.anchorMax = new Vector2(0, 1);
                bg.anchorMin = new Vector2(0, 1);
                bg.offsetMax = new Vector2(x + itemWidth+3f, -10 - rowOffset+3f);
                bg.offsetMin = new Vector2(x-3f, -itemHeight - rowOffset-3f);
                var img = bg.GetComponent<Image>();
                img.color = bgColor;
                uid.bgImages.Add(img);
                
                RectTransform buttonTransform =
                    script.manager.configurableScrollablePopupPrefab.transform.Find("Button") as RectTransform;
                buttonTransform = UnityEngine.Object.Instantiate(buttonTransform, tabbarPrefab.transform);
                buttonTransform.name = "Button";
                buttonTransform.anchorMax = new Vector2(0, 1);
                buttonTransform.anchorMin = new Vector2(0, 1);
                buttonTransform.offsetMax = new Vector2(x + itemWidth, -10 - rowOffset);
                buttonTransform.offsetMin = new Vector2(x, -itemHeight - rowOffset);
                Button buttonButton = buttonTransform.GetComponent<Button>();
                uid.buttons.Add(buttonButton);
                Text buttonText = buttonTransform.Find("Text").GetComponent<Text>();
                buttonText.text = menuItems[i];

                x += itemWidth + padding;
            }
            
            Transform t = ourCreateUIElement(tabbarPrefab.transform, false);
            tabBar = t.gameObject.GetComponent<UIDynamicTabBarWithBG>();
            for (int i = 0; i < tabBar.buttons.Count; ++i)
            {
                int menuID = i;
                tabBar.buttons[i].onClick.AddListener(() =>
                {
                    tabBar.lastId = tabBar.id;
                    tabBar.id = menuID;
                    for (int j = 0; j < tabBar.buttons.Count; ++j) tabBar.buttons[j].interactable = (j != menuID);
                    selectTab(menuID);
                });
                ColorBlock cb = ColorBlock.defaultColorBlock;
                cb.disabledColor = new Color(0.55f, 0.90f, 1f);
                cb.highlightedColor *= .8f;
                tabBar.buttons[i].colors = cb;
            }

            Object.Destroy(tabbarPrefab);
            tabBar.spacer = script.CreateSpacer(true);
            tabBar.spacer.height = totalHeight;
            tabBar.SelectTab = val => tabBar.buttons[val].onClick.Invoke();
            tabBar.SelectLast = () => tabBar.SelectTab(tabBar.id);
            if (invokeDefault) tabBar.SelectTab(tabBar.id);
            return tabBar;
        }

        public static MyUIDynamicSlider CreateReusableUIDynamicSlider(bool rightSide = false, JSONStorableFloat jfloat = null)
        {
            Transform uiElement = ourCreateUIElement(script.manager.configurableSliderPrefab.transform, rightSide);
            var oldUid = uiElement.GetComponent<UIDynamicSlider>();
            // Object.Destroy(toggleUid);
            var myUid = uiElement.gameObject.AddComponent<MyUIDynamicSlider>();
            myUid.slider = oldUid.slider;
            myUid.defaultButton = oldUid.defaultButton;
            myUid.labelText = oldUid.labelText;
            if(jfloat == null) myUid.label = "Toggle";
            else myUid.RegisterFloat(jfloat);
            Object.Destroy(oldUid);
            return myUid;
        }
        
        public static MyUIDynamicToggle CreateReusableUIDynamicToggle(bool rightSide = false, JSONStorableBool jBool = null)
        {
            Transform uiElement = ourCreateUIElement(script.manager.configurableTogglePrefab.transform, rightSide);
            var toggleUid = uiElement.GetComponent<UIDynamicToggle>();
            // Object.Destroy(toggleUid);
            var myToggleUid = uiElement.gameObject.AddComponent<MyUIDynamicToggle>();
            myToggleUid.toggle = toggleUid.toggle;
            myToggleUid.labelText = toggleUid.labelText;
            if(jBool == null) myToggleUid.label = "Toggle";
            else myToggleUid.RegisterBool(jBool);
            Object.Destroy(toggleUid);
            return myToggleUid;
        }

        public static UIDynamicToggleArray CreateToggleArray(JSONStorableBool[] bjsons, float width = 1f, int columns = -1, MVRScript script = null)
        {
            if (script == null) script = UIManager.script;
            if (columns == -1) columns = bjsons.Length;
            float rowCount = Mathf.Ceil((float)bjsons.Length / columns);
            float totalHeight = 70f + 45f * (rowCount - 1);
            GameObject prefab = new GameObject("ToggleArray");
            var uid = prefab.AddComponent<UIDynamicToggleArray>();
            RectTransform rt = prefab.AddComponent<RectTransform>();
            rt.anchorMax = new Vector2(0, 1);
            rt.anchorMin = new Vector2(0, 1);
            // rt.offsetMax = new Vector2(1064, 0);
            // rt.offsetMin = new Vector2(10, -60);
            LayoutElement le = prefab.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.minHeight = totalHeight;
            le.minWidth = 1064;
            le.preferredHeight = totalHeight;
            le.preferredWidth = 1064;
            
            RectTransform backgroundTransform =
                script.manager.configurableScrollablePopupPrefab.transform.Find("Background") as RectTransform;
            backgroundTransform = Object.Instantiate(backgroundTransform, prefab.transform);
            backgroundTransform.name = "Background";
            backgroundTransform.anchorMax = new Vector2(1, 1);
            backgroundTransform.anchorMin = new Vector2(0, 0);
            // rt.offsetMax = new Vector2(1064, 0);
            // rt.offsetMin = new Vector2(10, -45);
            
            float padding = 5.0f;
            float itemWidth, rowOffset;
            itemWidth = 1024f * width / columns - padding;
            
            float x = 15;
            for (int i = 0; i < bjsons.Length; ++i)
            {
                int rowNumber = i / columns;
                rowOffset = 45f * rowNumber;
                if (i % columns == 0) x = 15f;
                rt = Object.Instantiate(
                    script.manager.configurableTogglePrefab.transform,
                    prefab.transform,
                    false) as RectTransform;
                rt.name = "Toggle";
                rt.anchorMax = new Vector2(0, 1);
                rt.anchorMin = new Vector2(0, 1);
                rt.offsetMax = new Vector2(x + itemWidth, -15 - rowOffset);
                rt.offsetMin = new Vector2(x, -55 - rowOffset);
                var toggle = rt.GetComponent<UIDynamicToggle>();
                uid.toggles.Add(toggle);
                Text label = rt.Find("Label").GetComponent<Text>();
                uid.labels.Add(label);
                label.text = bjsons[i].name;
                label.fontSize = 24;
                x += itemWidth + padding;
            }
            
            Transform t = ourCreateUIElement(prefab.transform, false);
            uid = t.gameObject.GetComponent<UIDynamicToggleArray>();
            uid.jbools = new JSONStorableBool[bjsons.Length];
            for (int i = 0; i < bjsons.Length; ++i)
            {
                uid.jbools[i] = bjsons[i];
                bjsons[i].RegisterToggle(uid.toggles[i].toggle);
            }
            UIDynamic spacer = script.CreateSpacer(true);
            spacer.ForceHeight(totalHeight);
            uid.spacer = spacer;
            Object.Destroy(prefab);
            return uid;
        }
        
        public static UIDynamicChooserArray CreateChooserArray(JSONStorableStringChooser[] sjsons, float width = 1f, int columns = -1, MVRScript script = null, float leftOffset = 60f)
        {
            if (script == null) script = UIManager.script;
            if (columns == -1) columns = sjsons.Length;
            float rowCount = Mathf.Ceil((float) sjsons.Length / columns);
            float totalHeight = 195f + 170f * (rowCount - 1);
            GameObject prefab = new GameObject("ToggleArray");
            var uid = prefab.AddComponent<UIDynamicChooserArray>();
            RectTransform rt = prefab.AddComponent<RectTransform>();
            rt.anchorMax = new Vector2(0, 1);
            rt.anchorMin = new Vector2(leftOffset, 1);
            // rt.offsetMax = new Vector2(1064, 0);
            // rt.offsetMin = new Vector2(10, -60);
            LayoutElement le = prefab.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.minHeight = totalHeight;
            le.minWidth = 1064 - leftOffset;
            le.preferredHeight = totalHeight;
            le.preferredWidth = 1064 - leftOffset;
            
            RectTransform backgroundTransform =
                script.manager.configurableScrollablePopupPrefab.transform.Find("Background") as RectTransform;
            backgroundTransform = Object.Instantiate(backgroundTransform, prefab.transform);
            backgroundTransform.name = "Background";
            backgroundTransform.anchorMax = new Vector2(1, 1);
            backgroundTransform.anchorMin = new Vector2(0, 0);
            // rt.offsetMax = new Vector2(1064, 0);
            // rt.offsetMin = new Vector2(10, -45);
            float padding = 5.0f;
            float itemWidth, rowOffset;
            itemWidth = (1064f - leftOffset) * width / columns - padding;
            
            float x = 0f;
            for (int i = 0; i < sjsons.Length; ++i)
            {
                int rowNumber = i / columns;
                rowOffset = 45f * rowNumber;
                if (i % columns == 0) x = 0f;
                rt = Object.Instantiate(
                    script.manager.configurableFilterablePopupPrefab.transform,
                    prefab.transform,
                    false) as RectTransform;
                rt.name = "Chooser";
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.offsetMax = new Vector2(leftOffset + x + itemWidth, -15 - rowOffset);
                rt.offsetMin = new Vector2(leftOffset + x, -140 - rowOffset);
                UIPopup popup = rt.GetComponent<UIPopup>();
                uid.popups.Add(popup);
                // Text label = rt.Find("Label").GetComponent<Text>();
                // label.text = sjsons[i].name;
                x += itemWidth + padding;
            }
            
            Transform t = ourCreateUIElement(prefab.transform, false);
            uid = t.gameObject.GetComponent<UIDynamicChooserArray>();
            for (int i = 0; i < sjsons.Length; ++i)
            {
                sjsons[i].RegisterPopup(uid.popups[i]);
            }
            UIDynamic spacer = script.CreateSpacer(true);
            spacer.height = totalHeight;
            Object.Destroy(prefab);
            return uid;
        }
        
        

        private static GameObject vector3SliderPrefab;
        private static GameObject vector4SliderPrefab;
        public static List<Transform> leftUIElements;
        public static List<Transform> rightUIElements;

        public static UIDynamic EvenLeftToRight(MVRScript script = null, List<object> UIElements = null)
        {
            if (leftUIHeight == rightUIHeight) return null;
            if (script == null) script = UIManager.script;
            // leftUIHeight.Print();
            // rightUIHeight.Print();
            // leftUIElements.Count.Print();
            // rightUIElements.Count.Print();
            var spacer = script.CreateSpacer(true);
            spacer.ForceHeight(leftUIHeight - rightUIHeight);
            if(UIElements != null) UIElements.Add(spacer);
            
            return spacer;
        }
        
        public static UIDynamic EvenRightToLeft(MVRScript script = null, List<object> UIElements = null)
        {
            if (leftUIHeight == rightUIHeight) return null;
            if (script == null) script = UIManager.script;
            var spacer = script.CreateSpacer(false);
            spacer.ForceHeight(rightUIHeight - leftUIHeight);
            if(UIElements != null) UIElements.Add(spacer);
            return spacer;
        }

        public static float leftUIHeight
        {
            get
            {
                float height = 0f;
                for (int i = 0; i < leftUIElements.Count; i++)
                {
                    // var le = leftUIElements[i].transform.GetComponent<LayoutElement>();
                    var le = leftUIElements[i].GetComponent<UIDynamic>();
                    height += le.height;
                }

                return height;
            }
        }
        
        public static float rightUIHeight
        {
            get
            {
                float height = 0f;
                for (int i = 0; i < rightUIElements.Count; i++)
                {
                    // var le = rightUIElements[i].transform.GetComponent<LayoutElement>();
                    var le = rightUIElements[i].GetComponent<UIDynamic>();
                    height += le.height;
                }

                return height;
            }
        }

        public static UIDynamicV3Slider CreateV3Slider(MyJSONStorableVector3 vector = null, MVRScript script = null)
        {
            if (script == null) script = UIManager.script;
            if (vector3SliderPrefab == null) vector3SliderPrefab = CreateVector3SliderPrefab(script);

            Transform t = ourCreateUIElement(vector3SliderPrefab.transform, false);
            var uid = t.GetComponent<UIDynamicV3Slider>();
            if (vector != null) uid.RegisterVector(vector);
            t.gameObject.SetActive(true);
            float spacerHeight = leftUIHeight - rightUIHeight - 5f;
            var spacer = script.CreateSpacer(true);
            spacer.height = spacerHeight;
            uid.spacer = spacer;
            return uid;
        }

        public static void RemoveV3Slider(MVRScript script, UIDynamicV3Slider uid)
        {
            Transform transform = uid.transform;
            rightUIElements.Remove(transform);
            leftUIElements.Remove(transform);
            uid.sliders.ForEach(x => x.slider = null);
            script.RemoveSpacer(uid.spacer);
            leftUIElements.Remove(transform);
            UnityEngine.Object.Destroy(transform.gameObject);

            
        }

        public static UIDynamicV4Slider CreateV4Slider(
            JSONStorableVector4 vector = null,
            MVRScript script = null,
            bool hideToggle = false,
            UIDynamicV4Slider.SetToggleCallback toggleCallback = null,
            bool constrained = false,
            int fontSize = 22,
            string[] suffices = null)
        {
            if (vector4SliderPrefab == null) vector4SliderPrefab = CreateVector4SliderPrefab(script);
            Transform t = ourCreateUIElement(vector4SliderPrefab.transform, false);
            var uid = t.GetComponent<UIDynamicV4Slider>();
            if(!hideToggle)
            {
                uid.toggle.toggle.onValueChanged.AddListener(val => uid.setToggleCallbackFunction(val));
                if (toggleCallback != null) uid.setToggleCallbackFunction += toggleCallback;
            }
            else Object.Destroy(uid.toggle.gameObject);

            foreach (var slider in uid.sliders)
            {
                slider.transform.Find("Text").GetComponent<Text>().fontSize = fontSize;
                slider.sliderControl.clamp = constrained;
            }
            if (vector != null) uid.RegisterVector(vector, labelsuffixes: suffices);
            t.gameObject.SetActive(true);
            float spacerHeight = leftUIHeight - rightUIHeight - 5f;
            var spacer = script.CreateSpacer(true);
            spacer.height = spacerHeight;
            uid.spacer = spacer;
            return uid;
        }
        
        public static void RemoveV4Slider(MVRScript script, UIDynamicV4Slider uid)
        {
            if (uid == null) return;
            Transform transform = uid.transform;
            rightUIElements.Remove(transform);
            leftUIElements.Remove(transform);
            // uid.sliders.ForEach(x => x.slider = null);
            script.RemoveSpacer(uid.spacer);
            leftUIElements.Remove(transform);
            Object.Destroy(transform.gameObject);
        }

        public static void CreateToggleWithButton(JSONStorableBool jbool, bool rightSide = false)
        {
            if (toggleWithButtonPrefab == null) toggleWithButtonPrefab = CreateToggleWithButtonPrefab();
            Transform t = ourCreateUIElement(toggleWithButtonPrefab.transform, rightSide);
            var uid = t.GetComponent<UIDynamicToggleWithButton>();
            uid.NullCheck();
            jbool.toggle = uid.toggle;
            uid.toggle.isOn = jbool.val;
            uid.label = jbool.name;
            uid.button.onClick.AddListener(() => "pipi".Print());
        }

        public static UIDynamic CreateMenuSpacer(List<object> UIElements, float height, bool rightSide)
        {
            UIDynamic spacer = script.CreateSpacer(rightSide);
            spacer.ForceHeight(height);
            UIElements?.Add(spacer);
            return spacer;
        }

        public static UIDynamicTextInfo SetupInfoOneLine(List<object> UIElements, string text, bool addSpacer = true,
            bool rightside = false, MVRScript script = null)
        {
            if (script == null) script = UIManager.script;
            UIDynamicTextInfo infoline;
            infoline = SetupInfoOneLine(script, text, rightside);
            infoline.height = 30f;
            UIElements?.Add(infoline);
            if (addSpacer)
            {
                CreateMenuSpacer(UIElements, 35f, !rightside);
            }
            return infoline;
        }

        public static void RemoveUIElements(MVRScript script, List<object> UIElements)
        {
            for (int i = 0; i < UIElements.Count; ++i)
            {
                
                if (UIElements[i] is UIDynamicV3Slider)
                {
                    RemoveV3Slider(script, (UIDynamicV3Slider)UIElements[i]);
                }
                else if (UIElements[i] is UIDynamicV4Slider)
                {
                    RemoveV4Slider(script, (UIDynamicV4Slider)UIElements[i]);
                }
            }
            Utils.RemoveUIElements(script, UIElements);
        }

        public static GameObject CreateVector4LegendPrefab(MVRScript script)
        {
            GameObject prefab = new GameObject("Vector4SliderLegend");
            prefab.SetActive(false);
            UIDynamicTVector4Legend uid = prefab.AddComponent<UIDynamicTVector4Legend>();
            RectTransform background = prefab.AddComponent<RectTransform>();
            background.anchorMax = new Vector2(0, 1);
            background.anchorMin = new Vector2(0, 1);
            background.offsetMax = new Vector2(1064, 0);
            background.offsetMin = new Vector2(10, 0);
            
            LayoutElement le = prefab.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.minHeight = 30;
            le.minWidth = 1064;
            le.preferredHeight = 30;
            le.preferredWidth = 1064;
            
            float w = .2359f;
            for (int k = 0; k < 4; k++)
            {
                background = Object.Instantiate(
                    script.manager.configurableScrollablePopupPrefab.transform.Find("Background"),
                    prefab.transform,
                    false) as RectTransform;
                background.anchorMax = new Vector2((k + 1f) * w, 1);
                background.anchorMin = new Vector2(k * w, 1);
                background.offsetMax = new Vector2(-4 + 60f, 0);
                background.offsetMin = new Vector2(4f + 60f, -40);
                background.gameObject.SetActive(true);
                
                RectTransform labelTransform = script.manager.configurableScrollablePopupPrefab.transform.Find("Button/Text") as RectTransform;;
                labelTransform = Object.Instantiate(labelTransform, background);
                labelTransform.name = "Text";
                labelTransform.anchorMax = new Vector2(1, 1);
                labelTransform.anchorMin = new Vector2(0, 0);
                labelTransform.offsetMax = new Vector2(-5, 0);
                labelTransform.offsetMin = new Vector2(5, 0);
                Text labelText = labelTransform.GetComponent<Text>();
                labelText.alignment = TextAnchor.MiddleCenter;
                uid.texts.Add(labelText);
                uid.backgrounds.Add(background);
            }
            return prefab;
        }

        private static GameObject vector4LegendPrefab;
        public static UIDynamicTVector4Legend CreateVector4Legend(MVRScript script,string[] labels = null)
        {
            if (vector4LegendPrefab == null) vector4LegendPrefab = CreateVector4LegendPrefab(script);
            Transform t = ourCreateUIElement(vector4LegendPrefab.transform, false);
            var uid = t.GetComponent<UIDynamicTVector4Legend>();
            t.gameObject.SetActive(true);
            float spacerHeight = leftUIHeight - rightUIHeight - 5f;
            var spacer = script.CreateSpacer(true);
            spacer.height = spacerHeight;
            uid.spacer = spacer;
            if (labels != null)
            {
                uid.SetLabels(labels);
            }
            return uid;
        }

        public static GameObject CreateVector4SliderPrefab(MVRScript script)
        {
            GameObject prefab = new GameObject("Vector4Slider");
            prefab.SetActive(false);
            UIDynamicV4Slider uiDynamicV4Slider = prefab.AddComponent<UIDynamicV4Slider>();
            RectTransform rt = prefab.AddComponent<RectTransform>();
            rt.anchorMax = new Vector2(0, 1);
            rt.anchorMin = new Vector2(0, 1);
            rt.offsetMax = new Vector2(1064, 0);
            rt.offsetMin = new Vector2(10, 0);

            LayoutElement le = prefab.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.minHeight = 130;
            le.minWidth = 1064;
            le.preferredHeight = 130;
            le.preferredWidth = 1064;
            RectTransform backgroundTransform =
                script.manager.configurableScrollablePopupPrefab.transform.Find("Background") as RectTransform;
            backgroundTransform =
                Object.Instantiate(backgroundTransform, prefab.transform);
            backgroundTransform.name = "Background";
            backgroundTransform.anchorMax = new Vector2(1, 1);
            backgroundTransform.anchorMin = new Vector2(0, 0);
            backgroundTransform.offsetMax = new Vector2(0, -2.5f);
            backgroundTransform.offsetMin = new Vector2(0, 2.5f);

            float w = .2359f;
            for (int k = 0; k < 4; k++)
            {
                rt = Object.Instantiate(
                    script.manager.configurableSliderPrefab.transform,
                    prefab.transform,
                    false) as RectTransform;
                var uiDynamicSlider = rt.GetComponent<UIDynamicSlider>();
                uiDynamicSlider.sliderControl.clamp = false;
                rt.Find("Text").GetComponent<Text>().fontSize = 22;
                uiDynamicV4Slider.sliders.Add(uiDynamicSlider);
                rt.anchorMax = new Vector2((k + 1f) * w, 1);
                rt.anchorMin = new Vector2(k * w, 1);
                rt.offsetMax = new Vector2(-4 + 60f, -10);
                rt.offsetMin = new Vector2(4f + 60f, -120);
                rt.gameObject.SetActive(true);
            }

            rt = script.manager.configurableTogglePrefab.transform as RectTransform;
            rt = Object.Instantiate(rt, prefab.transform);
            // var toggle = rt.GetComponent<UIDynamicToggle>();
            // toggle.label = "sync";
            uiDynamicV4Slider.toggle = rt.GetComponent<UIDynamicToggle>();
            rt.pivot = new Vector2(0f, 1f);
            rt.Rotate(new Vector3(0f, 0f, -90f));
            rt.anchorMax = new Vector2(0f, 1f);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.offsetMax = new Vector2(165f, -10);
            rt.offsetMin = new Vector2(55f, -60);
            var checkmark = rt.Find("Background").Find("Checkmark");
            checkmark.Rotate(new Vector3(0f, 0f, 90f));
            var label = rt.Find("Label");
            label.Rotate(new Vector3(0f, 0f, 180f));
            var text = label.GetComponent<Text>();
            text.fontSize = 20;
            text.alignment = TextAnchor.MiddleRight;

            return prefab;
        }

        private static GameObject CreateVector3SliderPrefab(MVRScript script)
        {
            GameObject prefab = new GameObject("Vector3Slider");
            prefab.SetActive(false);
            UIDynamicV3Slider uiDynamicV3Slider = prefab.AddComponent<UIDynamicV3Slider>();
            RectTransform rt = prefab.AddComponent<RectTransform>();
            rt.anchorMax = new Vector2(0, 1);
            rt.anchorMin = new Vector2(0, 1);
            rt.offsetMax = new Vector2(1064, 0);
            rt.offsetMin = new Vector2(10, 0);

            LayoutElement le = prefab.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.minHeight = 130;
            le.minWidth = 1064;
            le.preferredHeight = 130;
            le.preferredWidth = 1064;

            RectTransform backgroundTransform =
                script.manager.configurableScrollablePopupPrefab.transform.Find("Background") as RectTransform;
            backgroundTransform =
                Object.Instantiate(backgroundTransform, prefab.transform);
            backgroundTransform.name = "Background";
            backgroundTransform.anchorMax = new Vector2(1, 1);
            backgroundTransform.anchorMin = new Vector2(0, 0);
            backgroundTransform.offsetMax = new Vector2(0, -2.5f);
            backgroundTransform.offsetMin = new Vector2(0, 2.5f);

            float w = .333f;
            float d = 0f;
            for (int k = 0; k < 3; k++)
            {
                float leftOffset = k == 0 ? 60f : 0f;
                rt = Object.Instantiate(
                    script.manager.configurableSliderPrefab.transform,
                    prefab.transform,
                    false) as RectTransform;
                var uiDynamicSlider = rt.GetComponent<UIDynamicSlider>();
                uiDynamicSlider.sliderControl.clamp = false;
                uiDynamicV3Slider.sliders.Add(uiDynamicSlider);
                rt.anchorMax = new Vector2(k * w + .333f, 1);
                rt.anchorMin = new Vector2(k * (w) + d, 1);
                rt.offsetMax = new Vector2(-4, -10);
                rt.offsetMin = new Vector2(leftOffset + 4f, -120);
                rt.gameObject.SetActive(true);
            }

            rt = script.manager.configurableTogglePrefab.transform as RectTransform;
            rt = Object.Instantiate(rt, prefab.transform);
            var toggle = rt.GetComponent<UIDynamicToggle>();
            toggle.label = "sync";
            // UIDynamicV3Slider.toggle = toggle;
            rt.pivot = new Vector2(0f, 1f);
            rt.Rotate(new Vector3(0f, 0f, -90f));
            rt.anchorMax = new Vector2(0f, 1f);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.offsetMax = new Vector2(165f, -10);
            rt.offsetMin = new Vector2(55f, -60);
            var checkmark = rt.Find("Background").Find("Checkmark");
            checkmark.Rotate(new Vector3(0f, 0f, 90f));
            var label = rt.Find("Label");
            label.Rotate(new Vector3(0f, 0f, 180f));
            var text = label.GetComponent<Text>();
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleRight;

            return prefab;
        }

        private static GameObject toggleWithButtonPrefab;
        
        public static GameObject CreateToggleWithButtonPrefab()
        {
            // var prefab = new GameObject("ToggleWithButton");
            // var rt = script.manager.configurableTogglePrefab.transform as RectTransform;
            var prefab = Object.Instantiate(script.manager.configurableTogglePrefab.gameObject);
            var originalUid = prefab.GetComponent<UIDynamicToggle>();
            var toggle = originalUid.toggle;
            var image = originalUid.backgroundImage;
            var labelText = originalUid.labelText;
            Object.DestroyImmediate(originalUid);
            var uid = prefab.gameObject.AddComponent<UIDynamicToggleWithButton>();
            uid.toggle = toggle;
            uid.backgroundImage = image;
            uid.labelText = labelText;
            uid.button = prefab.transform.Find("Label").gameObject.AddComponent<Button>();
            // uid.button.colors = script.manager.configurableButtonPrefab.GetComponent<Button>().colors;
            uid.button.onClick.AddListener(() => "popo".Print());
            return prefab;
        }

        private static GameObject ourTextInfoPrefab;
        public static UIDynamicTextInfo SetupInfoTextNoScroll(MVRScript script, string text, float height, bool rightSide)
        {
			if (ourTextInfoPrefab == null)
			{
				ourTextInfoPrefab = new GameObject("TextInfo");
				ourTextInfoPrefab.SetActive(false);
				RectTransform rt = ourTextInfoPrefab.AddComponent<RectTransform>();
				rt.anchorMax = new Vector2(0, 1);
				rt.anchorMin = new Vector2(0, 1);
				rt.offsetMax = new Vector2(535, -500);
				rt.offsetMin = new Vector2(10, -600);
				LayoutElement le = ourTextInfoPrefab.AddComponent<LayoutElement>();
				le.flexibleWidth = 1;
				le.minHeight = 35;
				le.minWidth = 350;
				le.preferredHeight = 35;
				le.preferredWidth = 500;

				RectTransform backgroundTransform = script.manager.configurableScrollablePopupPrefab.transform.Find("Background") as RectTransform;
				backgroundTransform = UnityEngine.Object.Instantiate(backgroundTransform, ourTextInfoPrefab.transform);
				backgroundTransform.name = "Background";
				backgroundTransform.anchorMax = new Vector2(1, 1);
				backgroundTransform.anchorMin = new Vector2(0, 0);
				backgroundTransform.offsetMax = new Vector2(0, 0);
				backgroundTransform.offsetMin = new Vector2(0, -10);

				RectTransform labelTransform = script.manager.configurableScrollablePopupPrefab.transform.Find("Button/Text") as RectTransform;;
				labelTransform = UnityEngine.Object.Instantiate(labelTransform, ourTextInfoPrefab.transform);
				labelTransform.name = "Text";
				labelTransform.anchorMax = new Vector2(1, 1);
				labelTransform.anchorMin = new Vector2(0, 0);
				labelTransform.offsetMax = new Vector2(-5, 0);
				labelTransform.offsetMin = new Vector2(5, 0);
				Text labelText = labelTransform.GetComponent<Text>();
				labelText.alignment = TextAnchor.UpperLeft;

				UIDynamicTextInfo uid = ourTextInfoPrefab.AddComponent<UIDynamicTextInfo>();
				uid.text = labelText;
				uid.layout = le;
				uid.background = backgroundTransform;
			}

			{
				Transform t = ourCreateUIElement(ourTextInfoPrefab.transform, rightSide);
				UIDynamicTextInfo uid = t.gameObject.GetComponent<UIDynamicTextInfo>();
				uid.text.text = text;
				uid.layout.minHeight = height;
				uid.layout.preferredHeight = height;
				t.gameObject.SetActive(true);
				return uid;
			}
        }

        public static UIDynamicTextInfo SetupInfoTextNoScroll(MVRScript script, JSONStorableString storable, float height, bool rightSide)
		{
			UIDynamicTextInfo uid = SetupInfoTextNoScroll(script, storable.val, height, rightSide);
			storable.setCallbackFunction = (string text) => { 
				if (uid != null && uid.text != null)
					uid.text.text = text;
			};
			return uid;
		}

		public static UIDynamicTextInfo SetupInfoOneLine(MVRScript script, string text, bool rightSide)
		{
			UIDynamicTextInfo uid = SetupInfoTextNoScroll(script, text, 35, rightSide);
			uid.background.offsetMin = new Vector2(0, 0);
			return uid;
		}

        public static T CreatePlaceboUiDynamic<T>(string label, List<object> UIElements, bool rightside = false)
        {
            if (typeof(T) == typeof(UIDynamicSlider))
            {
                var jparam = new JSONStorableFloat(label, 0f, 0f, 0f);
                return (T)(object)jparam.CreateUI(UIElements, rightside);
            }

            return (T)(object)null;
        }
        
        public static void RemoveUIElements(List<object> UIElements)
		{
			for (int i=0; i<UIElements.Count; ++i)
			{
				if(UIElements[i] == null) continue;
				if (UIElements[i] is JSONStorableParam)
				{
					JSONStorableParam jsp = UIElements[i] as JSONStorableParam;
					if (jsp is JSONStorableFloat)
						script.RemoveSlider(jsp as JSONStorableFloat);
					else if (jsp is JSONStorableBool)
                        script.RemoveToggle(jsp as JSONStorableBool);
					else if (jsp is JSONStorableColor)
                        script.RemoveColorPicker(jsp as JSONStorableColor);
					else if (jsp is JSONStorableString)
                        script.RemoveTextField(jsp as JSONStorableString);
					else if (jsp is JSONStorableStringChooser)
					{
						// Workaround for VaM not cleaning its panels properly.
						JSONStorableStringChooser jssc = jsp as JSONStorableStringChooser;
						RectTransform popupPanel = jssc.popup?.popupPanel;
                        script.RemovePopup(jssc);
						if (popupPanel != null)
							UnityEngine.Object.Destroy(popupPanel.gameObject);
					}
				}
				else if (UIElements[i] is UIDynamic)
				{
					UIDynamic uid = UIElements[i] as UIDynamic;
					if (uid is UIDynamicButton)
                        script.RemoveButton(uid as UIDynamicButton);
                    else if (uid is UIDynamicSlider)
                        script.RemoveSlider(uid as UIDynamicSlider);
					else if (uid is UIDynamicToggle)
                        script.RemoveToggle(uid as UIDynamicToggle);
					else if (uid is UIDynamicColorPicker)
                        script.RemoveColorPicker(uid as UIDynamicColorPicker);
					else if (uid is UIDynamicTextField)
                        script.RemoveTextField(uid as UIDynamicTextField);
					else if (uid is UIDynamicPopup)
					{
						// Workaround for VaM not cleaning its panels properly.
						UIDynamicPopup uidp = uid as UIDynamicPopup;
						RectTransform popupPanel = uidp.popup?.popupPanel;
                        script.RemovePopup(uidp);
						if (popupPanel != null)
							UnityEngine.Object.Destroy(popupPanel.gameObject);
					}
                    else if (uid is UIDynamicV3Slider)
                    {
                        var v3Slider = uid as UIDynamicV3Slider;
                        leftUIElements.Remove(v3Slider.transform);
                        rightUIElements.Remove(v3Slider.spacer.transform);
                        Object.Destroy(v3Slider.spacer.gameObject);
                        Object.Destroy(v3Slider.gameObject);
                    }
                    else if (uid is UIDynamicTabBar)
                    {
                        var tabbar = uid as UIDynamicTabBar;
                        leftUIElements.Remove(tabbar.transform);
                        rightUIElements.Remove(tabbar.spacer.transform);
                        Object.Destroy(tabbar.spacer.gameObject);
                        Object.Destroy(tabbar.gameObject);
                    }
                    else
                        script.RemoveSpacer(uid);
				}
			}

			UIElements.Clear();
		}
    }

    public class UIItem
    {
        private UIItemHolder _containingItemHolder;
        public UIItemHolder containingItemHolder{
            get{return _containingItemHolder;}
            set{_containingItemHolder = value;
                value.AddItem(this);
            }
        }
        public UIItemHolder itemHolder;
        public JSONStorableBool enabled;
        public Action<bool> toggle;
        // public string name { get{return containingItemHolder.items[this].name;} set{containingItemHolder.items[this].name = value;} }
        public string name { get{return enabled.name;} set{enabled.name = value;} }
        
        public UIItem(object uiItemHolder = null, string name = "name"){
            enabled = new JSONStorableBool(name, true, ToggleCB);
            if(uiItemHolder != null) containingItemHolder = uiItemHolder as UIItemHolder;
        }

        void ToggleCB(bool b){
            if(toggle != null) toggle(b);
            _containingItemHolder.ToggleItem?.Invoke(this, b);
        }
        public virtual void CreateUI(){}
    }

    public class UIItemHolder
    {
        public Action syncToggles;
        public Func<UIItem, bool> GetToggleState;
        public Action<UIItem, bool> ToggleItem;
        public Action<UIItem> createItemUI;
        // public Dictionary<UIItem, JSONStorableBool> items = new Dictionary<UIItem, JSONStorableBool>();
        public List<UIItem> items = new List<UIItem>();
        public UIItem selectedItem;
        public object item{
            get{return items.LastOrDefault();}
            set{
                UIItem item = (UIItem) value;
                items.Add(item);
                item.containingItemHolder = this;
                // items[item] = new JSONStorableBool("item.name", true);
                // CreateToggle(item);
            }
        }

        public void AddItem(object obj, bool createUIImmediate = false){
            UIItem item = obj as UIItem;
            if(!items.Contains(item)) items.Add(item);
            if(createUIImmediate) createItemUI(item);
        }

        public virtual void SyncToggles(){
            if(syncToggles != null){
                syncToggles();
                return;
            }
            if(GetToggleState != null){
                foreach(UIItem item in items){
                    item.enabled.valNoCallback = GetToggleState(item);
                }
            }
        }

        public void CreateLabel(UIItem item){
            UIDynamicTextInfo infoline;
            infoline = Utils.SetupInfoOneLine(UIManager.script, item.name, false);
			infoline.height = 50f;
            UIManager.UIItems.Add(infoline);
        }
        
        public void CreateToggle(UIItem item){
            UIDynamicToggle toggle;
            // item.enabled.valNoCallback = GetToggleState(item);
            toggle = UIManager.script.CreateToggle(item.enabled, false);
            UIManager.UIItems.Add(toggle);
            
        }

        public void CreateConfigure(UIItem item){
            UIDynamicButton button;
            button = UIManager.CreateButton("Configure", UIManager.NewWindow(() =>
            {
                item.CreateUI(); selectedItem = item; SyncToggles();
            }), UIManager.UIItems, true);
        }

        // public void CreateItemsToggle(){
        //     UIManager.ClearItems();

        //     foreach(UIItem item in items){
        //         CreateToggle(item);
        //     }
        // }

        public void CreateItems(bool clearItems = true){
            if(createItemUI != null){
                if(clearItems) UIManager.ClearItems();
                foreach(UIItem item in items){
                    createItemUI(item);
                    // item.CreateUI();
                }
            }
        }
    }

    public class UnityEventsListener : MonoBehaviour
    {
        public readonly UnityEvent onDisabled = new UnityEvent();
        public readonly UnityEvent onEnabled = new UnityEvent();

        public void OnDisable()
        {
            onDisabled.Invoke();
        }

        public void OnEnable()
        {
            onEnabled.Invoke();
        }

        private void OnDestroy()
        {
            onDisabled.RemoveAllListeners();
            onEnabled.RemoveAllListeners();
        }
    }

    // public class MyScript : MVRScript {
    //     private UnityEventsListener _uiListener;
    //     public override void InitUI()
    //     {
    //         base.InitUI();
    //         if(UITransform == null || _uiListener != null)
    //         {
    //             return;
    //         }

    //         _uiListener = UITransform.gameObject.AddComponent<UnityEventsListener>();
    //         if(_uiListener != null)
    //         {
    //             _uiListener.onEnabled.AddListener(() => {
    //                 // do stuff
    //             });
    //         }
    //     }

    //     public void OnDestroy()
    //     {
    //         if(_uiListener != null)
    //         {
    //             DestroyImmediate(_uiListener);
    //         }
    //     }
    // }

    public class UIDynamicToggleWithButton : UIDynamicToggle
    {
        public Button button;
    }
    

    public class UIDynamicTabBar : UIDynamicUtils
    {
        public int id;
        public int lastId;
        public List<Button> buttons = new List<Button>();
        public Action<int> SelectTab;
        public Action SelectLast;
        public UIDynamic spacer;

        public void ToggleButtons(bool val, int id = -1)
        {
            if (id == -1)
            {
                for (int i=0; i<buttons.Count; i++) buttons[i].gameObject.SetActive(val);
                return;
            }
            buttons[id].gameObject.SetActive(val);
        }
    }
    
    public class UIDynamicTabBarWithBG : UIDynamicTabBar
    {
        public List<Image> bgImages = new List<Image>();
    }

    public class UIDynamicTabBarWithToggle : UIDynamicTabBar
    {
        public UIDynamicToggle toggleUid;
    }
    
    // public class UIDynamicToggleArray : UIDynamicUtils
    // {
    //     public List<UIDynamicToggle> toggles = new List<UIDynamicToggle>();
    //     public List<Text> labels = new List<Text>();
    // }

    public class UIDynamicChooserArray : UIDynamicUtils
    {
        public List<UIPopup> popups = new List<UIPopup>();
    }
    
    public class UIDynamicTVector4Legend : UIDynamicUtils
    {
        public UIDynamic spacer;
        public List<Text> texts = new List<Text>();
        public List<LayoutElement> layouts = new List<LayoutElement>();
        public List<RectTransform> backgrounds = new List<RectTransform>();

        public void SetLabels(string[] labels)
        {
            for (int i = 0; i < 4; i++) texts[i].text = labels[i];
        }
    }

    public class TabBar
    {
        public int menuItem;
        private string[] menuItems;
        private Func<int, List<object>> selectTab;
        private int level;
        private int columns;

        public TabBar(string[] menuItems, Func<int, List<object>> selectTab,
            int level, int columns = -1)
        {
            this.menuItems = menuItems;
            this.selectTab = selectTab;
            this.level = level;
            if (columns == -1) this.columns = menuItems.Length;
        }
        
        public void Create(List<object> uiElements)
        {
            float rowCount = Mathf.Ceil((float)menuItems.Length / columns);
            float totalHeight = 90 + 45f * (rowCount - 1);
            UIDynamicTabBar tabBar;
            GameObject tabbarPrefab = new GameObject("TabBar");
            RectTransform rt = tabbarPrefab.AddComponent<RectTransform>();
            rt.anchorMax = new Vector2(0, 1);
            rt.anchorMin = new Vector2(0, 1);
            rt.offsetMax = new Vector2(1064, -500);
            rt.offsetMin = new Vector2(10, -600);
            LayoutElement le = tabbarPrefab.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.minHeight = totalHeight;
            le.minWidth = 1064;
            le.preferredHeight = totalHeight;
            le.preferredWidth = 1064;

            RectTransform backgroundTransform = UIManager.script.manager.configurableScrollablePopupPrefab.transform.Find("Background") as RectTransform;
            backgroundTransform = UnityEngine.Object.Instantiate(backgroundTransform, tabbarPrefab.transform);
            backgroundTransform.name = "Background";
            backgroundTransform.anchorMax = new Vector2(1, 1);
            backgroundTransform.anchorMin = new Vector2(0, 0);
            //backgroundTransform.offsetMax = new Vector2(0, -15);
            //backgroundTransform.offsetMin = new Vector2(0, 15);

            UIDynamicTabBar uid = tabbarPrefab.AddComponent<UIDynamicTabBar>();
            float padding = 5.0f;
            float width, rowOffset;
            width = 1024f/columns-padding;
            
            float x = 15;
            for (int i=0; i<menuItems.Length; ++i)
            {
                int rowNumber = i / columns;
                rowOffset = 45f * rowNumber;
                if (i % columns == 0) x = 15f;
                RectTransform buttonTransform = UIManager.script.manager.configurableScrollablePopupPrefab.transform.Find("Button") as RectTransform;
                buttonTransform = UnityEngine.Object.Instantiate(buttonTransform, tabbarPrefab.transform);
                buttonTransform.name = "Button";
                buttonTransform.anchorMax = new Vector2(0, 1);
                buttonTransform.anchorMin = new Vector2(0, 1);
                buttonTransform.offsetMax = new Vector2(x+width, -25-rowOffset);
                buttonTransform.offsetMin = new Vector2(x, -65-rowOffset);
                Button buttonButton = buttonTransform.GetComponent<Button>();
                uid.buttons.Add(buttonButton);
                Text buttonText = buttonTransform.Find("Text").GetComponent<Text>();
                buttonText.text = menuItems[i];
                x += width + padding;
            }
            Transform t = UIManager.ourCreateUIElement(tabbarPrefab.transform, false);
            tabBar = t.gameObject.GetComponent<UIDynamicTabBar>();
            for (int i=0; i<tabBar.buttons.Count; ++i)
            {
                int menuID = i;
                tabBar.buttons[i].onClick.AddListener(() => {
                    for (int j=0; j<tabBar.buttons.Count; ++j)  tabBar.buttons[j].interactable = (j != menuID);
                    UIManager.SelectTab(() => selectTab(menuID), level);
                    menuItem = menuID;
                    // menuItem.Print();
                });
                ColorBlock cb = ColorBlock.defaultColorBlock;
                cb.disabledColor = new Color(0.55f, 0.90f, 1f);
                cb.highlightedColor *= .8f;
                tabBar.buttons[i].colors = cb;
            }
            UnityEngine.Object.Destroy(tabbarPrefab);
            uiElements.Add(tabBar);
            uiElements.Add(UIManager.CreateMenuSpacer(uiElements, totalHeight, true));
            tabBar.buttons[menuItem].onClick.Invoke();
        }
    }
}