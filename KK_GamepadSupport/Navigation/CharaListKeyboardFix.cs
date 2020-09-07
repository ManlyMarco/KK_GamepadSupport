using System.Linq;
using HarmonyLib;
using Illusion.Component.UI;
using UGUI_AssistLibrary;
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
            if (trigger != null)
            {
                var subject = Traverse.Create(trigger).Field("onPointerEnter").GetValue<Subject<PointerEventData>>();
                subject.OnNext(null);
            }
            else
            {
                GetComponent<UIAL_EventTrigger>().triggers.First(x => x.eventID == EventTriggerType.PointerEnter).callback.Invoke(null);
            }
        }

        public void OnDeselect(BaseEventData eventData)
        {
            var trigger = GetComponent<ObservablePointerExitTrigger>();
            if (trigger != null)
            {
                var subject = Traverse.Create(trigger).Field("onPointerExit").GetValue<Subject<PointerEventData>>();
                subject.OnNext(null);
            }
            else
            {
                GetComponent<UIAL_EventTrigger>().triggers.First(x => x.eventID == EventTriggerType.PointerExit).callback.Invoke(null);
            }
        }

        public void OnSubmit(BaseEventData eventData)
        {
            var pc = GetComponent<PointerClickCheck>();
            if (pc != null)
            {
                pc.onPointerClick.Invoke(null);
            }
            else
            {
                GetComponent<UIAL_EventTrigger>().triggers.First(x => x.eventID == EventTriggerType.PointerClick).callback.Invoke(null);
            }
        }
    }
}