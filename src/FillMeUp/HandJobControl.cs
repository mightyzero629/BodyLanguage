using System.Collections.Generic;

namespace CheesyFX
{
    public class HandJobControl
    {
        private List<object> UIElements = new List<object>();
        private JSONStorableFloat amplitudeMean = new JSONStorableFloat("Amplitude Mean", 150f, 0f, 500f);
        private JSONStorableFloat amplitudeDelta = new JSONStorableFloat("Amplitude Mean", 100f, 0f, 500f);
        
        private JSONStorableFloat periodMean = new JSONStorableFloat("Amplitude Mean", .5f, .2f, 5f);
        private JSONStorableFloat periodDelta = new JSONStorableFloat("Amplitude Delta", .2f, .2f, 5f);
        
        private JSONStorableFloat quicknessMean = new JSONStorableFloat("Quickness Mean", 4f, 0f, 10f);
        private JSONStorableFloat quicknessDelta = new JSONStorableFloat("Quickness Delta", 3f, 0f, 10f);
        
        private JSONStorableFloat periodRationMean = new JSONStorableFloat("PeriodRatio Mean", .5f, 0f, 1f);
        private JSONStorableFloat periodRationDelta = new JSONStorableFloat("PeriodRatio Mean", .2f, 0f, 1f);

        public void CreateUI(Force force)
        {
            if (amplitudeMean.slider == null)
            {
                amplitudeMean.CreateUI(UIElements);
                amplitudeDelta.CreateUI(UIElements, true);
            }
            
        }
    }
}