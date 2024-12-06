using UnityEngine;
using UnityEngine.UI;

namespace CheesyFX
{
    public class MyUIDynamicSlider : UIDynamicSlider
    {
        public JSONStorableFloat jsFloat;
        public void RegisterFloat(JSONStorableFloat jsFloat, bool clear = true)
        {
            if (clear && this.jsFloat != null)
            {
                slider.onValueChanged.RemoveAllListeners();
                jsFloat.setCallbackFunction -= SetSlider;
            }
            this.jsFloat = jsFloat;
            slider.maxValue = jsFloat.max;
            slider.minValue = jsFloat.min;
            defaultButton.onClick.RemoveAllListeners();
            defaultButton.onClick.AddListener(jsFloat.SetValToDefault);
            slider.value = jsFloat.val;
            slider.onValueChanged.AddListener(jsFloat.SetVal);
            label = jsFloat.name;
            jsFloat.setCallbackFunction += SetSlider;
        }

        private void SetSlider(float val)
        {
            slider.value = val;
        }

        public void SetColor(Color color)
        {
            GetComponentInChildren<Image>().color = color;
        }
    }
}