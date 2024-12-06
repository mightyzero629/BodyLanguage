using System;
using MeshVR;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Weelco.VRInput;

namespace CheesyFX
{
    public class ClickListener : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public UnityEvent onLeftClick = new UnityEvent();
        public UnityEvent onMiddleClick = new UnityEvent();
        public UnityEvent onRightClick = new UnityEvent();
        public UnityEvent onDragUp = new UnityEvent();
        public UnityEvent onDragDown = new UnityEvent();
        public UnityEvent onDragRight = new UnityEvent();
        public UnityEvent onDragLeft = new UnityEvent();
        public UnityEvent onPointerEnter = new UnityEvent();
        public UnityEvent onPointerExit = new UnityEvent();

        private bool hovered;
        public bool dragEnabled;
        private bool leftMouseDown;
        private Vector2 mousePosition;
        
        public void Clone(ClickListener original)
        {
            onLeftClick = original.onLeftClick;
            onRightClick = original.onRightClick;
            onMiddleClick = original.onMiddleClick;
            onDragUp = original.onDragUp;
            onDragDown = original.onDragDown;
            onDragRight = original.onDragRight;
            onDragLeft = original.onDragLeft;
            onPointerEnter = original.onPointerEnter;
            onPointerExit = original.onPointerExit;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
            onPointerEnter.Invoke();
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            onPointerExit.Invoke();
            if (leftMouseDown && Input.GetKey(KeyCode.Mouse0))
            {
                Vector2 drag = (Vector2)Input.mousePosition - mousePosition;
                if (Mathf.Abs(drag.y) > Mathf.Abs(drag.x))
                {
                    if(drag.y > 0) onDragUp.Invoke();
                    else onDragDown.Invoke();
                }
                else
                {
                    if(drag.x > 0) onDragRight.Invoke();
                    else onDragLeft.Invoke();
                }
            }
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                onLeftClick.Invoke();
            }
            else if (eventData.button == PointerEventData.InputButton.Middle)
            {
                onMiddleClick.Invoke();
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                onRightClick.Invoke();
            }
        }

        private void Update()
        {
            if(!dragEnabled || !hovered) return;
            transform.GetComponent<Image>().fillAmount += Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                mousePosition = Input.mousePosition;
                leftMouseDown = true;
            }
        
            if (Input.GetKeyUp(KeyCode.Mouse0)) leftMouseDown = false;
        }
    }
}