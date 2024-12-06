using System;
using System.Collections.Generic;
using MacGruber;
using UnityEngine.UI;

namespace CheesyFX
{
    public class UIDynamicV4Slider : UIDynamicUtils
    {
        public List<UIDynamicSlider> sliders = new List<UIDynamicSlider>();
        public UIDynamicToggle toggle;
        public UIDynamic spacer;
        private List<JSONStorableVector4> vectors = new List<JSONStorableVector4>();
        
        public delegate void SetToggleCallback(bool b);
        public SetToggleCallback setToggleCallbackFunction;
        
        public void RegisterVector(JSONStorableVector4 vector, bool clear=true, string[] labelsuffixes = null)
        {
            toggle.toggle.isOn = vector.sync;
            if(clear) ClearVectors();
            if (labelsuffixes == null) labelsuffixes = new[] { "x", "y", "z", "w" };
            for (int k = 0; k < 4; k++)
            {
                UIDynamicSlider uiDynamicSlider = sliders[k];
                switch (k)
                {
                    case 0:
                        vector.RegisterSliderX(uiDynamicSlider.slider);
                        vector.sliderX.onValueChanged.RemoveListener(vector.SetXVal);
                        vector.sliderX.onValueChanged.AddListener(vector.SetValX);
                        uiDynamicSlider.label = $"{vector.name}.{labelsuffixes[0]}";
                        break;
                    case 1:
                        vector.RegisterSliderY(uiDynamicSlider.slider);
                        vector.sliderY.onValueChanged.RemoveListener(vector.SetXVal);
                        vector.sliderY.onValueChanged.AddListener(vector.SetValY);
                        uiDynamicSlider.label = $"{vector.name}.{labelsuffixes[1]}";
                        break;
                    case 2:
                        vector.RegisterSliderZ(uiDynamicSlider.slider);
                        vector.sliderZ.onValueChanged.RemoveListener(vector.SetXVal);
                        vector.sliderZ.onValueChanged.AddListener(vector.SetValZ);
                        uiDynamicSlider.label = $"{vector.name}.{labelsuffixes[2]}";
                        break;
                    case 3:
                        vector.RegisterSliderW(uiDynamicSlider.slider);
                        vector.sliderW.onValueChanged.AddListener(vector.SetValW);
                        uiDynamicSlider.label = $"{vector.name}.{labelsuffixes[3]}";
                        break;
                }
            }
            vectors.Add(vector);
        }
        
        public void RegisterVectors(List<JSONStorableVector4> newVectors, bool clear=true)
        {
            if(clear) ClearVectors();
            for(int i =0; i<newVectors.Count; i++) RegisterVector(newVectors[i], false);
        }
        
        public void ClearVectors()
        {
            for(int i=0; i<vectors.Count; i++)
            {
                var vector = vectors[i];
                for (int k = 0; k < 4; k++)
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
                        case 3:
                            sliders[k].slider.onValueChanged.RemoveListener(vector.SetValW);
                            break;
                    }
                }
                vector.sliderX = null;
                vector.sliderY = null;
                vector.sliderZ = null;
                vector.sliderW = null;
                // $"Deregistered {vector.name}".Print();
            }
            vectors.Clear();
        }
        
        public void ToggleSync(bool val)
        {
            for (int i = 0; i < vectors.Count; i++)
            {
                vectors[i].sync = val;
            }
        }

        public void SliderSetActive(int id, bool val)
        {
            var slider = sliders[id];
            // slider.gameObject.SetActive(val);
            slider.slider.gameObject.SetActive(val);
            slider.transform.Find("Panel").gameObject.SetActive(val);
            slider.transform.Find("QuickButtonsGroup").gameObject.SetActive(val);
            slider.transform.Find("DefaultValueButton").gameObject.SetActive(val);
            slider.transform.Find("RangePanel").gameObject.SetActive(val);
            slider.transform.Find("ValueInputField").gameObject.SetActive(val);
        }

        public void SetConstrained(bool val)
        {
            for (int i = 0; i < 4; i++)
            {
                sliders[i].sliderControl.clamp = val;
                sliders[i].rangeAdjustEnabled = false;
            }
            for (int i = 0; i < vectors.Count; i++)
            {
                vectors[i].constrained = true;
            }
        }

        public void SetInteractable(bool val)
        {
            for (int i = 0; i < 4; i++) sliders[i].slider.interactable = val;
        }

        public void ToggleMonitoring(bool val)
        {
            for (int i = 0; i < 4; i++) sliders[i].slider.interactable = !val;
            for (int i = 0; i < vectors.Count; i++)
            {
                var v = vectors[i];
                v.sliderX.onValueChanged.RemoveListener(v.SetValX);
                v.sliderY.onValueChanged.RemoveListener(v.SetValY);
                v.sliderZ.onValueChanged.RemoveListener(v.SetValZ);
                v.sliderW.onValueChanged.RemoveListener(v.SetValW);
            }
            if(!val)
            {
                for (int i = 0; i < vectors.Count; i++)
                {
                    var v = vectors[i];
                    v.sliderX.onValueChanged.AddListener(v.SetValX);
                    v.sliderY.onValueChanged.AddListener(v.SetValY);
                    v.sliderZ.onValueChanged.AddListener(v.SetValZ);
                    v.sliderW.onValueChanged.AddListener(v.SetValW);
                }
            }
        }
    }
}