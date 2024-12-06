using UnityEngine.Serialization;
using UnityEngine.UI;

namespace CheesyFX
{
    public class UIDynamicSlapItem : UIDynamic
    {
        public bool rightSide;
        public Toggle activeToggle;
        public Toggle sideToggle;
        public Text toggleText;
        public Text sideText;
        public Button deleteButton;
        public Button personButton;
        public Button configureButton;
        public Text configureButtonText;
    }
}