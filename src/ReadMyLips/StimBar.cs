using UnityEngine;
using UnityEngine.UI;

namespace CheesyFX
{
    public class StimBar
    {
        private GameObject bar;
        private Slider slider;

        public float val
        {
            set { slider.value = value; }
        }

        public StimBar(GameObject parent)
        {
            bar = new GameObject("StimBar");
            bar.transform.SetParent(parent.transform, false);
            bar.AddComponent<Image>().material.color = Color.white;
            
            var rt = bar.GetComponent<RectTransform>();
            // rt.NullCheck();
            slider = bar.AddComponent<Slider>();
            slider.transition = Selectable.Transition.None;
            slider.interactable = false;
            slider.fillRect = rt;
            
        }
    }
}