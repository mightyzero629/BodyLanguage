using MacGruber;
using UnityEngine;

namespace CheesyFX
{
    public class ChestBreather : Breather
    {
        public ChestBreather()
        {
            GetMorphs();
            FillMeUp.onMorphsDeactivated.AddListener(GetMorphs);
        }

        private void GetMorphs()
        {
            morphs.Clear();
            morphs.Add(ReadMyLips.morphControl.GetMorphByUid($"{FillMeUp.packageUid}Custom/Atom/Person/Morphs/female/CheesyFX/BodyLanguage/Breathing/BL_Breathing_Chest.vmi"));
            morphs.Add(ReadMyLips.morphControl.GetMorphByUid($"{FillMeUp.packageUid}Custom/Atom/Person/Morphs/female/CheesyFX/BodyLanguage/Breathing/BL_RibCageDefine.vmi"));
        }
        
        public override void SetParameters(float intensity)
        {
            delta = 3f * intensity * intensity + .35f;
            maxDepth = baseLine + delta;
            minDepth = baseLine - delta;
        }
    }
}