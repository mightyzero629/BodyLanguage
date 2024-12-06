using System.Collections.Generic;
using UnityEngine.UI;

namespace CheesyFX
{
    public class UIDynamicToggleArray : UIDynamic
    {
        public JSONStorableBool[] jbools;
        public List<UIDynamicToggle> toggles = new List<UIDynamicToggle>();
        public List<Text> labels = new List<Text>();
        public UIDynamic spacer;

        public void RegisterBools(JSONStorableBool[] bools, bool clear = true)
        {
            for (int i = 0; i < bools.Length; i++)
            {
                var jbool = bools[i];
                var toggle = toggles[i];
                toggle.toggle.onValueChanged.RemoveAllListeners();
                // if (clear && jbools[i] != null)
                // {
                //     toggle.toggle.onValueChanged.RemoveListener(jbools[i].SetVal);
                // }
                toggle.toggle.isOn = jbool.val;
                toggle.toggle.onValueChanged.AddListener(jbool.SetVal);
                toggle.label = jbool.name;
                jbools[i] = jbool;
            }
        }
    }
}