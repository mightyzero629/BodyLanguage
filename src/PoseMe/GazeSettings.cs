using System.Collections.Generic;
using SimpleJSON;

namespace CheesyFX
{
    public class GazeSettings
    {
        // public Pose pose;
        public JSONStorableBool enabledJ = new JSONStorableBool("Enabled", true);
        public JSONStorableFloat gazeStrength = new JSONStorableFloat("Gaze Strength", 500f, 0f, 2000f);
        public JSONStorableFloat gazeStrengthDuringBJ = new JSONStorableFloat("Gaze Strength During BJ", 20f, 0f, 2000f);
        public JSONStorableFloat gazeSpeedMean = new JSONStorableFloat("Gaze Speed Mean", 1f, .1f, 2f);
        public JSONStorableFloat gazeSpeedDelta = new JSONStorableFloat("Gaze Speed Delta", 1f, 0f, 2f);
        public JSONStorableBool gazeSpeedOnesided = new JSONStorableBool("Gaze Speed Onesided", true);
        public JSONStorableFloat targetTimeMean = new JSONStorableFloat("Target Time Mean", 5f, .1f, 10f);
        public JSONStorableFloat targetTimeDelta = new JSONStorableFloat("Target Time Delta", -4.5f, -10f, 10f);
        public JSONStorableBool targetTimeOnesided = new JSONStorableBool("Target Time Onesided", true);
        public JSONStorableFloat subTargetTimeMean = new JSONStorableFloat("SubTarget Time Mean", 1f, .1f, 10f);
        public JSONStorableFloat subTargetTimeDelta = new JSONStorableFloat("SubTarget Time Delta", -.9f, -10f, 10f);
        public JSONStorableBool subTargetTimeOnesided = new JSONStorableBool("SubTarget Time Onesided", true);
        
        public JSONStorableBool autoTarget = new JSONStorableBool("Auto Switch Target", true);
        public JSONStorableBool touchReactionsEnabled = new JSONStorableBool("Touch Reactions Enabled", false);
        public JSONStorableFloat gazeAngle = new JSONStorableFloat("Gaze Angle", 70f, 0f, 90f);
        
        public JSONStorableFloat selfInterest = new JSONStorableFloat("Self Interest", .25f, 0f, 1f);
        
        public JSONStorableBool targetOcclusion = new JSONStorableBool("Target Occlusion", true);
        public JSONStorableBool subTargetOcclusion = new JSONStorableBool("SubTarget Occlusion", true);
        public JSONStorableBool useMirrors = new JSONStorableBool("Use Mirrors", true);

        public JSONStorableBool environmentEnabled = new JSONStorableBool("Environment Enabled", true);
        
        public JSONStorableStringChooser focusTargetChooser =
            new JSONStorableStringChooser("FocusTarget", null, null, "Focus Target");
        public JSONStorableFloat focusDuration = new JSONStorableFloat("Focus Duration", 5f, 1f, 60f, false);

        public Dictionary<Gaze.GazeTarget, TargetSetting> targetSettings =
            new Dictionary<Gaze.GazeTarget, TargetSetting>();
        public Dictionary<Atom, JSONStorableFloat> personInterests =
            new Dictionary<Atom, JSONStorableFloat>();

        // public JSONStorableStringChooser environmentChooser =
        //     new JSONStorableStringChooser("cuaChooser", null, null, "Environment");

        public GazeSettings(GazeSettings from)
        {
            gazeStrength.val = from.gazeStrength.val;
            gazeStrengthDuringBJ.val = from.gazeStrengthDuringBJ.val;
            gazeSpeedMean.val = from.gazeSpeedMean.val;
            gazeSpeedDelta.val = from.gazeSpeedDelta.val;
            gazeSpeedOnesided.val = from.gazeSpeedOnesided.val;
            targetTimeMean.val = from.targetTimeMean.val;
            targetTimeDelta.val = from.targetTimeDelta.val;
            targetTimeOnesided.val = from.targetTimeOnesided.val;
            subTargetTimeMean.val = from.subTargetTimeMean.val;
            subTargetTimeDelta.val = from.subTargetTimeDelta.val;
            subTargetTimeOnesided.val = from.subTargetTimeOnesided.val;
            
            autoTarget.val = from.autoTarget.val;
            touchReactionsEnabled.val = from.touchReactionsEnabled.val;
            gazeAngle.val = from.gazeAngle.val;
            selfInterest.val = from.selfInterest.val;
            targetOcclusion.val = from.targetOcclusion.val;
            subTargetOcclusion.val = from.subTargetOcclusion.val;
            useMirrors.val = from.useMirrors.val;
            focusTargetChooser.val = from.focusTargetChooser.val;
            focusDuration.val = from.focusDuration.val;
            environmentEnabled.val = from.environmentEnabled.val;
            for (int i = 0; i < Gaze.targets.Count; i++)
            {
                var tgt = Gaze.targets[i];
                targetSettings[tgt] = new TargetSetting(tgt);
            }

            foreach (var item in from.personInterests)
            {
                var atomInterest = new JSONStorableFloat(item.Key.uid + " Interest", 1f, 0f, 1f, false);
                personInterests[item.Key] = atomInterest;
                atomInterest.val = item.Value.val;
            }
        }

        public GazeSettings()
        {
            for (int i = 0; i < Gaze.targets.Count; i++)
            {
                var tgt = Gaze.targets[i];
                targetSettings[tgt] = new TargetSetting(tgt);
            }
            foreach (var person in PoseMe.persons)
            {
                var atomInterest = new JSONStorableFloat(person.uid + " Interest", 1f, 0f, 1f, false);
                personInterests[person.atom] = atomInterest;
            }
        }

        public void Apply()
        {
            var settings = Gaze.gazeSettings;
            settings.gazeStrength.val = gazeStrength.val;
            settings.gazeStrengthDuringBJ.val = gazeStrengthDuringBJ.val;
            settings.gazeSpeedMean.val = gazeSpeedMean.val;
            settings.gazeSpeedDelta.val = gazeSpeedDelta.val;
            settings.gazeSpeedOnesided.val = gazeSpeedOnesided.val;
            settings.targetTimeMean.val = targetTimeMean.val;
            settings.targetTimeDelta.val = targetTimeDelta.val;
            settings.targetTimeOnesided.val = targetTimeOnesided.val;
            settings.subTargetTimeMean.val = subTargetTimeMean.val;
            settings.subTargetTimeDelta.val = subTargetTimeDelta.val;
            settings.subTargetTimeOnesided.val = subTargetTimeOnesided.val;
            
            settings.autoTarget.val = autoTarget.val;
            settings.touchReactionsEnabled.val = touchReactionsEnabled.val;
            settings.gazeAngle.val = gazeAngle.val;
            settings.selfInterest.val = selfInterest.val;
            settings.targetOcclusion.val = targetOcclusion.val;
            settings.subTargetOcclusion.val = subTargetOcclusion.val;
            settings.useMirrors.val = useMirrors.val;
            settings.focusTargetChooser.val = focusTargetChooser.val;
            settings.focusDuration.val = focusDuration.val;
            settings.environmentEnabled.val = environmentEnabled.val;
            foreach (var item in targetSettings)
            {
                item.Value.Apply(item.Key);
            }

            foreach (var item in personInterests)
            {
                settings.personInterests[item.Key].val = item.Value.val;
            }
        }
        
        public JSONClass Store(JSONClass jc = null)
        {
            if(jc == null) jc = new JSONClass();
            gazeStrength.Store(jc, false);
            gazeStrengthDuringBJ.Store(jc, false);
            gazeSpeedMean.Store(jc, false);
            gazeSpeedDelta.Store(jc, false);
            gazeSpeedOnesided.Store(jc, false);
            targetTimeMean.Store(jc, false);
            targetTimeDelta.Store(jc, false);
            targetTimeOnesided.Store(jc, false);
            subTargetTimeMean.Store(jc, false);
            subTargetTimeDelta.Store(jc, false);
            subTargetTimeOnesided.Store(jc, false);
            autoTarget.Store(jc, false);
            touchReactionsEnabled.Store(jc, false);
            gazeAngle.Store(jc, false);
            selfInterest.Store(jc, false);
            focusTargetChooser.Store(jc, false);
            focusDuration.Store(jc, false);
            environmentEnabled.Store(jc, false);
            
            foreach (var item in targetSettings)
            {
                var jc1 = new JSONClass();
                var needsStore = item.Value.enabled.Store(jc1, false);
                needsStore = item.Value.interest.Store(jc1, false) || needsStore;
                if(needsStore) jc[item.Key.name] = jc1;
            }
            foreach (var item in personInterests)
            {
                item.Value.Store(jc, false);
            }
            return jc;
        }
        
        public void Load(JSONClass jc)
        {
            gazeStrength.Load(jc, true);
            gazeStrengthDuringBJ.Load(jc, true);
            gazeSpeedMean.Load(jc, true);
            gazeSpeedDelta.Load(jc, true);
            gazeSpeedOnesided.Load(jc, true);
            targetTimeMean.Load(jc, true);
            targetTimeDelta.Load(jc, true);
            targetTimeOnesided.Load(jc, true);
            subTargetTimeMean.Load(jc, true);
            subTargetTimeDelta.Load(jc, true);
            subTargetTimeOnesided.Load(jc, true);
            autoTarget.Load(jc, true);
            touchReactionsEnabled.Load(jc, true);
            gazeAngle.Load(jc, true);
            selfInterest.Load(jc, true);
            focusTargetChooser.Load(jc, true);
            focusDuration.Load(jc, true);
            environmentEnabled.Load(jc, true);
            foreach (var item in targetSettings)
            {
                if(!jc.HasKey(item.Key.name)) continue;
                var jc1 = jc[item.Key.name].AsObject;
                item.Value.enabled.Load(jc1, true);
                item.Value.interest.Load(jc1, true);
            }
            foreach (var item in personInterests)
            {
                item.Value.Load(jc, true);
            }
        }

        public void CreateUI(List<object> UIElements)
        {
            focusTargetChooser.CreateUI(UIElements, false, chooserType:2);
            focusDuration.CreateUI(UIElements, true);
            gazeSpeedMean.CreateUI(UIElements);
            gazeSpeedDelta.CreateUI(UIElements, true);
            targetTimeMean.CreateUI(UIElements);
            targetTimeDelta.CreateUI(UIElements, true);
            subTargetTimeMean.CreateUI(UIElements);
            subTargetTimeDelta.CreateUI(UIElements, true);
            subTargetTimeOnesided.CreateUI(UIElements, true);
            targetTimeOnesided.CreateUI(UIElements, true);
            gazeStrength.CreateUI(UIElements);
            gazeStrengthDuringBJ.CreateUI(UIElements);
            gazeAngle.CreateUI(UIElements, true);
            touchReactionsEnabled.CreateUI(UIElements);
            autoTarget.CreateUI(UIElements);
        }

        public class TargetSetting
        {
            // private Gaze.GazeTarget target;
            public JSONStorableBool enabled = new JSONStorableBool("Enabled", true);
            public JSONStorableFloat interest = new JSONStorableFloat("Interest", 1f, 0f, 1f, false);

            public TargetSetting(Gaze.GazeTarget target)
            {
                // this.target = target;
                Fetch(target);
            }

            public void Fetch(Gaze.GazeTarget target)
            {
                enabled.val = target.enabled.val;
                interest.val = target.interest.val;
            }

            public void Apply(Gaze.GazeTarget target)
            {
                target.enabled.val = enabled.val;
                target.interest.val = interest.val;
            }
        }
    }
}