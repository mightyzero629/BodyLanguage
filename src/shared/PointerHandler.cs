using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace CheesyFX
{
    public class PointerHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public PointerEnterEvent onPointerEnter = new PointerEnterEvent();
        public PointerExitEvent onPointerExit = new PointerExitEvent();
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            onPointerEnter.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            onPointerExit.Invoke();
        }
        
        public class PointerEnterEvent : UnityEvent
        {
        }
        
        public class PointerExitEvent : UnityEvent
        {
        }
    }
}