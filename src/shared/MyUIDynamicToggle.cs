using UnityEngine;
using UnityEngine.UI;

namespace CheesyFX
{
    public class MyUIDynamicToggle : UIDynamicToggle
    {
        public JSONStorableBool jsbool;
        public void RegisterBool(JSONStorableBool jsbool, bool clear = true)
        {
            if (clear)
            {
                toggle.onValueChanged.RemoveAllListeners();
                if(this.jsbool != null) this.jsbool.setCallbackFunction -= SetToggleState;
            }
            this.jsbool = jsbool;
            toggle.isOn = jsbool.val;
            toggle.onValueChanged.AddListener(jsbool.SetVal);
            label = jsbool.name;
            jsbool.setCallbackFunction += SetToggleState;
        }

        private void SetToggleState(bool val)
        {
            toggle.isOn = val;
        }

        public void SetColor(Color color)
        {
            GetComponentInChildren<Image>().color = color;
        }
    }
}