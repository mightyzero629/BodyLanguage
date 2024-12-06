using UnityEngine.UI;

namespace CheesyFX
{
    public class UIDynamicGazeItem : UIDynamic
    {
        public bool rightSide;
        public Toggle activeToggle;
        public Text toggleText;
        public Text label;
        public Button deleteButton;

        public void SetToggleState(bool val)
        {
            // toggleText.text = val ? "✓" : "";
            activeToggle.isOn = val;
        }
    }
}