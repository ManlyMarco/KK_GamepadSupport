using SceneAssist;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KK_GamepadSupport.Navigation
{
    [DisallowMultipleComponent]
    public class PointerActionKeyboardFix : MonoBehaviour, ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        public void OnSelect(BaseEventData eventData)
        {
            var trigger = GetComponent<PointerAction>();
            trigger.OnPointerEnter(new PointerEventData(EventSystem.current));
        }

        public void OnDeselect(BaseEventData eventData)
        {
            var trigger = GetComponent<PointerAction>();
            trigger.OnPointerExit(new PointerEventData(EventSystem.current));
        }

        public void OnSubmit(BaseEventData eventData)
        {
            var trigger = GetComponent<PointerAction>();
            trigger.OnPointerClick(new PointerEventData(EventSystem.current));
        }
    }
}