using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using shared;
using SimpleJSON;
using UnityEngine;

namespace CheesyFX
{
    public class PoseIdle : MonoBehaviour, IPresetSystemReceiver
    {
        public List<LimbIdle> limbIdles = new List<LimbIdle>();
        public JSONStorableBool applyIdles = new JSONStorableBool("Apply Idles (This Pose)", false);
        public JSONStorableFloat scale = new JSONStorableFloat("Scale (All Regions)", 1f, 0f, 5f, false);
        public JSONStorableFloat maxQuickness = new JSONStorableFloat("Max Quickness (All Regions)", 1.5f, .2f, 2f, false);
        private Pose pose;
        public static PresetSystem presetSystem;
        private bool factoryDefaultsSet;
        
        public PoseIdle Init(Pose pose)
        {
            this.pose = pose;
            PoseMe.forceTargets.ForEach(x => limbIdles.Add(new LimbIdle(this, x)));
            applyIdles.setCallbackFunction += val =>
            {
                if (PoseMe.currentPose != pose) return;
                enabled = val && PoseMe.applyIdles.val;
            };
            if (!factoryDefaultsSet)
            {
                presetSystem.factoryDefaults = Store();
                factoryDefaultsSet = true;
            }
            enabled = false;
            return this;
        }

        private void OnEnable()
        {
            if(limbIdles.Count == 0) return;
            ResetForces();
        }
        //
        // private void OnDisable()
        // {
        //     $"{pose.id} disable".Print();
        // }

        // private void OnDestroy()
        // {
        //     $"{pose.id} destroy".Print();
        // }

        private void FixedUpdate()
        {
            if (SuperController.singleton.freezeAnimation) return;
            for (int i = 0; i < limbIdles.Count; i++)
            {
                limbIdles[i].Update();
            }
        }

        public void ResetForces()
        {
            for (int i = 0; i < limbIdles.Count; i++)
            {
                limbIdles[i].Reset();
            }
        }

        public void RefreshTargets()
        {
            for (int i = 0; i < limbIdles.Count; i++)
            {
                limbIdles[i].RefreshTargets();
            }
        }

        public void InverseActive()
        {
            foreach (var limbIdle in limbIdles)
            {
                limbIdle.forceEnabled.val = !limbIdle.forceEnabled.val;
                limbIdle.torqueEnabled.val = !limbIdle.torqueEnabled.val;
            }
        }
        
        public void DisableAll()
        {
            limbIdles.ForEach(x => x.forceEnabled.val = x.torqueEnabled.val = false);
        }

        public static void InitPresetSystem()
        {
            presetSystem = new PresetSystem(PoseMe.singleton)
            {
                saveDir = "Saves/PluginData/CheesyFX/BodyLanguage/PoseMe/Idles/"
            };
            presetSystem.InitStatic();
        }

        public JSONClass Store()
        {
            var jc = new JSONClass();
            scale.Store(jc, false);
            maxQuickness.Store(jc, false);
            applyIdles.Store(jc, false);
            foreach (var limbForce in limbIdles)
            {
                jc[limbForce.target.name] = limbForce.Store();
            }

            return jc;
        }

        public void Load(JSONClass jc)
        {
            scale.Load(jc, true);
            maxQuickness.Load(jc, true);
            applyIdles.Load(jc, true);
            foreach (var limbForce in limbIdles)
            {
                limbForce.Load(jc);
            }             
        }
    }
}