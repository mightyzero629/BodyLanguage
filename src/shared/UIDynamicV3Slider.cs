using System.Collections.Generic;
using MacGruber;
using UnityEngine.UI;

namespace CheesyFX
{
    public class UIDynamicV3Slider : UIDynamicUtils
    {
        public List<UIDynamicSlider> sliders = new List<UIDynamicSlider>();
        public Toggle toggle;
        public UIDynamic spacer;
        private List<MyJSONStorableVector3> vectors = new List<MyJSONStorableVector3>();
        
        public void RegisterVector(MyJSONStorableVector3 vector, bool clear=true)
        {
            if (toggle == null)
            {
                toggle = transform.GetComponentInChildren<UIDynamicToggle>().toggle;
                toggle.onValueChanged.AddListener(OnSyncChanged);
            }
            else toggle.isOn = vector.sync;
            if(clear) ClearVectors();
            for (int k = 0; k < 3; k++)
            {
                UIDynamicSlider uiDynamicSlider = sliders[k];
                switch (k)
                {
                    case 0:
                        vector.RegisterSliderX(uiDynamicSlider.slider);
                        vector.sliderX.onValueChanged.RemoveListener(vector.SetXVal);
                        vector.sliderX.onValueChanged.AddListener(vector.SetValX);
                        uiDynamicSlider.label = $"{vector.name}.x";
                        break;
                    case 1:
                        vector.RegisterSliderY(uiDynamicSlider.slider);
                        vector.sliderY.onValueChanged.RemoveListener(vector.SetXVal);
                        vector.sliderY.onValueChanged.AddListener(vector.SetValY);
                        uiDynamicSlider.label = $"{vector.name}.y";
                        break;
                    case 2:
                        vector.RegisterSliderZ(uiDynamicSlider.slider);
                        vector.sliderZ.onValueChanged.RemoveListener(vector.SetXVal);
                        vector.sliderZ.onValueChanged.AddListener(vector.SetValZ);
                        uiDynamicSlider.label = $"{vector.name}.z";
                        break;
                }
            }
            vectors.Add(vector);
            // $"Registered {vector.name}".Print();
        }
        
        public void RegisterVectors(List<MyJSONStorableVector3> newVectors, bool clear=true)
        {
            if(clear) ClearVectors();
            for(int i =0; i<newVectors.Count; i++) RegisterVector(newVectors[i],false);
        }

        public void DeregisterVector(MyJSONStorableVector3 vector)
        {
            if (!vectors.Remove(vector)) return;
            for (int k = 0; k < 3; k++)
            {
                switch (k)
                {
                    case 0:
                        sliders[k].slider.onValueChanged.RemoveListener(vector.SetValX);
                        break;
                    case 1:
                        sliders[k].slider.onValueChanged.RemoveListener(vector.SetValY);
                        break;
                    case 2:
                        sliders[k].slider.onValueChanged.RemoveListener(vector.SetValZ);
                        break;
                }
            }
            vector.sliderX = null;
            vector.sliderY = null;
            vector.sliderZ = null;
            // $"Deregistered {vector.name}".Print();
        }

        public void ClearVectors()
        {
            for(int i=0; i<vectors.Count; i++)
            {
                var vector = vectors[i];
                for (int k = 0; k < 3; k++)
                {
                    switch (k)
                    {
                        case 0:
                            sliders[k].slider.onValueChanged.RemoveListener(vector.SetValX);
                            break;
                        case 1:
                            sliders[k].slider.onValueChanged.RemoveListener(vector.SetValY);
                            break;
                        case 2:
                            sliders[k].slider.onValueChanged.RemoveListener(vector.SetValZ);
                            break;
                    }
                }
                vector.sliderX = null;
                vector.sliderY = null;
                vector.sliderZ = null;
                // $"Deregistered {vector.name}".Print();
            }
            vectors.Clear();
        }

        private void OnSyncChanged(bool val)
        {
            for (int i = 0; i < vectors.Count; i++)
            {
                vectors[i].sync = val;
            }
        }
    }
}