using System;
using System.Linq;
using UnityEngine;

namespace CheesyFX
{
    public class NippleDriver : MonoBehaviour
    {
        public int side;
        public NippleDriver peer;
        public JSONStorableFloat stimulation = new JSONStorableFloat("Left Stimulation", 0f, 0f, 1f, true, false);
        public JSONStorableFloat erection = new JSONStorableFloat("Left Erection", 0f, 0f, 1f, true, false);
        public float erectionScale = 1.5f;
        public bool isStimulated;
        
        public JSONStorableFloat[] nippleSprings;
        public JSONStorableFloat[] nippleDampers;
        public DAZMorph nippleMorph;
        private ConfigurableJoint joint;
        private float lastTorque;
        private TouchCollisionListener nipple;

        public void Init(int side)
        {
            this.side = side;
            peer = NippleManager.nippleDrivers[1-side];
            var atom = TouchMe.singleton.containingAtom;
            if (side == 1)
            {
                stimulation.name = "Right Stimulation";
                erection.name = "Right Erection";
            }
            var breastPhysicsMesh = (DAZPhysicsMesh) atom.GetStorableByID("BreastPhysicsMesh");
            nippleSprings = new []{
                // breastPhysicsMesh.GetFloatJSONParam("groupASpringMultiplier"),
                // breastPhysicsMesh.GetFloatJSONParam("groupBSpringMultiplier"),
                // breastPhysicsMesh.GetFloatJSONParam("groupCSpringMultiplier"),
                breastPhysicsMesh.GetFloatJSONParam("groupDSpringMultiplier")
            };
            nippleDampers = new []
            {
                // breastPhysicsMesh.GetFloatJSONParam("groupADamperMultiplier"),
                // breastPhysicsMesh.GetFloatJSONParam("groupBDamperMultiplier"),
                // breastPhysicsMesh.GetFloatJSONParam("groupCDamperMultiplier"),
                breastPhysicsMesh.GetFloatJSONParam("groupDDamperMultiplier")
            };
            erection.setCallbackFunction += AdjustNipple;
            erection.val = 0f;

            string morphUid = TouchMe.packageUid + "Custom/Atom/Person/Morphs/female/CheesyFX/BodyLanguage/Nipples/BL_lNippleStiff.vmi";
            if (side == 1) morphUid = morphUid.Replace("BL_l", "BL_r");
            DAZCharacterSelector characterSelector = atom.GetStorableByID("geometry") as DAZCharacterSelector;
            GenerateDAZMorphsControlUI morphControl = characterSelector.morphsControlUI;
            nippleMorph = morphControl.GetMorphByUid(morphUid);
            // nippleMorph = morphControl.GetMorphByDisplayName("TM_NippleErection");
            // nippleMorph.jsonFloat.val = 0f;
            // nippleSprings.ToList().ForEach(x => x.val = 500f);
            // nippleDampers.ToList().ForEach(x => x.val = 6f);
            // nippleMorph.morphValue = 1f;
            // breastPhysicsMesh.GetFloatParamNames().ForEach(x => x.Print());
            // Reset();
            // nippleSprings.ToList().ForEach(x => x.val.Print());
            // nippleDampers.ToList().ForEach(x => x.val.Print());
            if (side == 0)
            {
                nipple = BodyRegionMapping.touchZones["lNipple"].touchCollisionListener;
                joint = atom.transform.Find("rescale2/geometry/FemaleMorphers/BreastPhysicsMesh/PhysicsMeshleftnipple/PhysicsMeshJointleftnipple0").GetComponent<ConfigurableJoint>();
            }
            else
            {
                nipple = BodyRegionMapping.touchZones["rNipple"].touchCollisionListener;
                joint = atom.transform.Find("rescale2/geometry/FemaleMorphers/BreastPhysicsMesh/PhysicsMeshrightnipple/PhysicsMeshJointrightnipple0").GetComponent<ConfigurableJoint>();
            }
            Reset();
            // joint.connectedBody.name.Print();
            // kbr = atom.transform.Find("rescale2/geometry/FemaleMorphers/BreastPhysicsMesh/PhysicsMeshleftnipple/PhysicsMeshKRBleftnipple0");
        }

        private float stimTimer;
        
        void AdjustNipple(float erection){
            // nippleSprings.ToList().ForEach( x => x.val = Mathf.Lerp(x.defaultVal, 15f, erection));
            // nippleDampers.ToList().ForEach( x => x.val = Mathf.Lerp(x.defaultVal, 3f, erection));
            nippleMorph.jsonFloat.val = erection * erectionScale;
        }
        
        public void FixedUpdate()
        {
            var currentTorque = joint.currentTorque.sqrMagnitude;
            if(nipple.isOnStay)
            {
                var torqueChangeRate = Mathf.Abs(currentTorque - lastTorque) / Time.fixedDeltaTime;
                float stim = Mathf.Max(currentTorque * 20f, .1f * torqueChangeRate);
                // stim = .1f * torqueChangeRate;
                if (stim > .001f)
                {
                    // $"{currentTorque * 50f} {.1f * torqueChangeRate}".Print();
                    stim *= NippleManager.stimulationGain.val;
                    stimulation.val += stim;
                    stimTimer = 3f;
                    peer.stimulation.val += stim * NippleManager.bleed.val;
                    peer.stimTimer = 3f;
                    ReadMyLips.Stimulate(Mathf.Min(stim*NippleManager.generalStimulationGain.val, 3f), doStim:true);
                }
            }
            if(erection.val > .05f || stimulation.val > 0f){
                stimTimer -= Time.fixedDeltaTime;
                AdjustStimulation();
                AdjustErection();
            }
            else if(erection.val > 0f) erection.val = 0f;
            lastTorque = currentTorque;
        }

        private void AdjustErection()
        {
            if (stimulation.val > 0f)
            {
                erection.val = Mathf.Lerp(erection.val, 1f, stimulation.val * NippleManager.erectionGain.val*Time.fixedDeltaTime);
            }
            else
            {
                if (erection.val > .05f)
                {
                    erection.val = Mathf.Lerp(erection.val, 0f, NippleManager.erectionRegression.val*Time.fixedDeltaTime);
                }
                else if (erection.val > 0f)
                {
                    erection.val = 0f;
                }
            }
        }

        private void AdjustStimulation()
        {
            if (stimulation.val > 0f)
            {
                stimulation.val = Mathf.Lerp(stimulation.val, 0f, NippleManager.stimulationRegression.val * Time.fixedDeltaTime);
                if (stimTimer < 0f && stimulation.val < .1f) stimulation.val = 0f;
            }
        }

        public void Stimulate(float val)
        {
            stimulation.val += val;
            stimTimer = 3f;
        }
        
        private void OnDisable()
        {
            // "OnDisable".Print();
            Reset();
        }

        public void Reset(){
            stimulation.val = 0f;
            erection.val = 0f;
            nippleSprings.ToList().ForEach(x => x.val = x.defaultVal);
            nippleDampers.ToList().ForEach(x => x.val = x.defaultVal);
            nippleMorph.morphValue = 0f;
        }
    }
}