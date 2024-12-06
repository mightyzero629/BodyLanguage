using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

namespace CheesyFX
{
    public static class IdleUIProvider
    {
        public static bool uiOpen;
        private static MyUIDynamicToggle forceEnabledUid;
        private static MyUIDynamicToggle torqueEnabledUid;
        private static MyUIDynamicToggle idlesEnabledUid;
        private static UIDynamicSlider forceUid;
        private static UIDynamicSlider torqueUid;
        private static UIDynamicV3Slider directionalForceUid;
        private static UIDynamicV3Slider directionalTorqueUid;
        private static UIDynamicToggleArray forceOnesidedUid;
        private static UIDynamicToggleArray torqueOnesidedUid;

        private static MyUIDynamicSlider scaleUid;
        private static MyUIDynamicSlider maxQuicknessUid;
        
        // public static JSONStorableFloat scale = new JSONStorableFloat("Scale (All Regions)", 1f, 0f, 5f, false);
        // public static JSONStorableFloat maxQuickness = new JSONStorableFloat("Max Quickness (All Regions)", .75f, .2f, 2f, false);

        public static JSONStorableString info = new JSONStorableString("info", "");

        private static UIDynamicTabBar tabbar;
        private static LimbIdle _currentLimbIdle;
        private static LimbIdle currentLimbIdle
        {
            get { return _currentLimbIdle; }
            set
            {
                if (_currentLimbIdle == value) return;
                if (_currentLimbIdle != null) _currentLimbIdle.isEdited = false;
                _currentLimbIdle = value;
                _currentLimbIdle.isEdited = true;
            }
        }
        public static int lastTabId;
        private static JSONClass cachedIdles;
        
        

        private static List<object> UIElements = new List<object>();
        

        public static void CreateUI()
        {
            PoseMe.singleton.ClearUI();
            UIElements.Clear();
            var button = PoseMe.singleton.CreateButton("Return");
            button.buttonColor = PoseMe.navColor;
            button.button.onClick.AddListener(
                () =>
                {
                    AxisSetActive(false);
                    PoseMe.singleton.ClearUI();
                    uiOpen = false;
                    PoseMe.singleton.CreateUI();
                    // if(PoseMe.currentPose != null) PoseMe.currentPose.SetPreviewImage();
                });
            UIElements.Add(button);
            PoseMe.poseChooser.CreateUI(UIElements, true, chooserType:3).ForceHeight(50f);
            PoseIdle.presetSystem.CreateUI();
            PoseMe.singleton.SetupButton("Copy", false, () => cachedIdles = PoseMe.currentPose.poseIdle.Store());
            PoseMe.singleton.SetupButton("Paste", () => PoseMe.currentPose.poseIdle.Load(cachedIdles), PoseMe.warningColor, UIElements, true);
            tabbar = UIManager.CreateTabBar(PoseMe.forceTargets.Select(x => x.name).ToArray(), SelectRegion, columns:8);
            if (PoseMe.currentPose == null)
            {
                PoseMe.singleton.CreateTextField(new JSONStorableString("bla", "Create a pose first and come back!"));
                return;
            }

            currentLimbIdle = PoseMe.currentPose.poseIdle.limbIdles[lastTabId];
            directionalForceUid = UIManager.CreateV3Slider(currentLimbIdle.directionalForce);
            
            
            forceOnesidedUid = UIManager.CreateToggleArray(currentLimbIdle.forceOnesided);
            forceOnesidedUid.RegisterBools(currentLimbIdle.forceOnesided);

            directionalTorqueUid = UIManager.CreateV3Slider(currentLimbIdle.directionalTorque);
            torqueOnesidedUid = UIManager.CreateToggleArray(currentLimbIdle.torqueOnesided);
            torqueOnesidedUid.RegisterBools(currentLimbIdle.torqueOnesided);
            
            PoseMe.singleton.CreateSpacer(true).ForceHeight(245);
            scaleUid = UIManager.CreateReusableUIDynamicSlider(false, currentLimbIdle.poseIdle.scale);
            maxQuicknessUid = UIManager.CreateReusableUIDynamicSlider(true, currentLimbIdle.poseIdle.maxQuickness);
            // scale.CreateUI();
            // maxQuickness.CreateUI(rightSide:true);
            // forceEnabledUid = PoseMe.singleton.CreateToggle(lastLimbIdle.forceEnabled);
            forceEnabledUid = UIManager.CreateReusableUIDynamicToggle();
            torqueEnabledUid = UIManager.CreateReusableUIDynamicToggle();
            forceEnabledUid.RegisterBool(currentLimbIdle.forceEnabled);
            torqueEnabledUid.RegisterBool(currentLimbIdle.torqueEnabled);
            
            idlesEnabledUid = UIManager.CreateReusableUIDynamicToggle();
            
            PoseMe.applyIdles.CreateUI(UIElements);
            
            
            PoseMe.singleton.CreateTextField(info, true).ForceHeight(75f);
            PoseMe.singleton.SetupButton("Refresh Targets", true, () => currentLimbIdle.RefreshTargets(), UIElements);
            PoseMe.singleton.SetupButton("Refresh All Targets", true, () => currentLimbIdle.poseIdle.RefreshTargets(), UIElements);
            PoseMe.singleton.SetupButton("Disable All Regions", () => currentLimbIdle.poseIdle.DisableAll(), PoseMe.warningColor, UIElements, true);

            tabbar.SelectTab(lastTabId);
            uiOpen = true;
        }

        public static void SelectRegion(int id)
        {
            // PoseMe.currentPose.poseForce.limbForces[lastTabId].enabled.toggle
            
            // lastLimbIdle.forceEnabled.DeregisterToggle();
            // lastLimbIdle.torqueEnabled.DeregisterToggle();
            
            lastTabId = id;
            var lastPoseIdle = currentLimbIdle.poseIdle;
            currentLimbIdle = PoseMe.currentPose.poseIdle.limbIdles[id];

            forceEnabledUid.RegisterBool(currentLimbIdle.forceEnabled);
            torqueEnabledUid.RegisterBool(currentLimbIdle.torqueEnabled);
            idlesEnabledUid.RegisterBool(PoseMe.currentPose.poseIdle.applyIdles);
            directionalForceUid.RegisterVector(currentLimbIdle.directionalForce);
            directionalTorqueUid.RegisterVector(currentLimbIdle.directionalTorque);
            
            forceOnesidedUid.RegisterBools(currentLimbIdle.forceOnesided);
            torqueOnesidedUid.RegisterBools(currentLimbIdle.torqueOnesided);
            
            if(lastPoseIdle != currentLimbIdle.poseIdle)
            {
                scaleUid.RegisterFloat(currentLimbIdle.poseIdle.scale);
                maxQuicknessUid.RegisterFloat(currentLimbIdle.poseIdle.maxQuickness);
            }
            
            Debug.Clear();
            AxisSetActive(true);
            info.val = currentLimbIdle.GetInfo();
        }

        public static void AxisSetActive(bool val)
        {
            if(val) PoseMe.forceTargets[lastTabId].transform.Draw();
            else Debug.Clear();
        }
    }
}