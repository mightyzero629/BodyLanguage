using UnityEngine;

namespace CheesyFX
{
    public class StomachBreather : Breather
    {
        public StomachBreather()
        {
            GetMorphs();
            FillMeUp.onMorphsDeactivated.AddListener(GetMorphs);
        }
        
        private void GetMorphs()
        {
            morphs.Clear();
            morphs.Add(ReadMyLips.morphControl.GetMorphByUid($"{FillMeUp.packageUid}Custom/Atom/Person/Morphs/female/CheesyFX/BodyLanguage/Breathing/BL_Breathing_Stomach.vmi"));
        }
        
        public override void SetParameters(float intensity)
        {
            delta = .5f * intensity * intensity + .5f;
            maxDepth = baseLine + delta;
            minDepth = baseLine - delta;
        }
    }
}