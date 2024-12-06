using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CheesyFX
{
    public class AltFutaStim : Person
    {
        public override float stimGain => ReadMyLips.stimulationGain.val;
        public override float dynamicStimGain => ReadMyLips.dynamicStimGain;
        public new AltFutaStim Init(CapsulePenetrator penetrator)
        {
            base.Init(penetrator);
            dcs = penetrator.atom.GetStorableByID("geometry") as DAZCharacterSelector;
            stimulation = ReadMyLips.stimulation;
            
            // stimulation = ReadMyLips.stimulation;
            // foreski = dcs.morphsControlUIOtherGender.GetMorphByUid(FillMeUp.packageUid + "Custom/Atom/Person/Morphs/male_genitalia/CheesyFX/BodyLanguage/Foreski/BL_babul_foreskinFAP.vmi");
            // foreski.jsonFloat.max = 1.5f;
            // foreski.max = 1.5f;
            // foreski.jsonFloat.constrained = true;
            // foreskiBase = dcs.morphsControlUIOtherGender.GetMorphByUid(FillMeUp.packageUid + "Custom/Atom/Person/Morphs/male_genitalia/CheesyFX/BodyLanguage/Foreski/BL_babul_foreskinBASE.vmi");
            // foreskiBase.morphValue = foreskiEnabled.val ? 1f : 0f;
            //
            // testes = penetrator.atom.rigidbodies.FirstOrDefault(x => x.name == "Testes");
            return this;
        }
        
        protected override GameObject CreateFluidGO(ref ParticleSystem ps, string name)
        {
            var go = base.CreateFluidGO(ref ps, name);
            go.transform.localPosition = new Vector3(.025f, 0f, 0f);
            go.transform.localEulerAngles = new Vector3(0f, 90f, 0f);
            return go;
        }
        
        public override float Stimulate()
        {
            var delta = base.Stimulate();
            return delta;
        }
        
        public override void Update()
        {
            // isFucking.Print();
            if(isFucking) ReadMyLips.Stimulate(Stimulate()*400f, doStim:true);
            // cumshotHandler.load.val += .005f*ReadMyLips.stimulation.val;
            // if(!ReadMyLips.isOrgasmPleasure) fluidHandler.load.val += .005f*ReadMyLips.stimulation.val*10f;
            if (!isFucking)
            {
                enabled = false;
            }
        }
    }
}