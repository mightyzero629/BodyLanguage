using UnityEngine.Serialization;
using UnityEngine.UI;

namespace CheesyFX
{
    public class UIDynamicStoryLevel : UIDynamic
    {
        public Text name;
        public Button playButton;
        public Button configureButton;
        public Button deleteButton;
        public DualSlider slider;
        public Button increaseHighButton;
        public Button decreaseHighButton;
        public Button increaseLowButton;
        public Button decreaseLowButton;
        public InputField highInputField;
        public InputField lowInputField;
    }
}