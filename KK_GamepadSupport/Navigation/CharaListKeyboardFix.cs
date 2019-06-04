using Harmony;
using Illusion.Component.UI;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KK_GamepadSupport.Navigation
{
    [DisallowMultipleComponent]
    public class CharaListKeyboardFix : MonoBehaviour, ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        public void OnSelect(BaseEventData eventData)
        {
            var trigger = GetComponent<ObservablePointerEnterTrigger>();
            var subject = Traverse.Create(trigger).Field("onPointerEnter").GetValue<Subject<PointerEventData>>();
            subject.OnNext(null);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            var trigger = GetComponent<ObservablePointerExitTrigger>();
            var subject = Traverse.Create(trigger).Field("onPointerExit").GetValue<Subject<PointerEventData>>();
            subject.OnNext(null);
        }

        public void OnSubmit(BaseEventData eventData)
        {
            GetComponent<PointerClickCheck>().onPointerClick.Invoke(null);
        }
    }
}