using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KK_GamepadSupport.Navigation
{
    /// <summary>
    /// https://bitbucket.org/UnityUIExtensions/unity-ui-extensions/src/88c43891c0bf44f136e3021ad6c89d704dfebc83/Scripts/Utilities/UIScrollToSelection.cs?at=master&fileviewer=file-view-default
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public sealed class ScrollToSelection : MonoBehaviour
    {
        private RectTransform LayoutListGroup => TargetScrollRect != null ? TargetScrollRect.content : null;

        private bool IsInsideScrollView(Transform selected)
        {
            if (selected == null)
                return false;

            if (selected == LayoutListGroup.transform)
                return true;

            if (selected.name == "ScrollView" || selected.name == "Canvas")
                return false;

            return IsInsideScrollView(selected.parent);
        }

        private ScrollType ScrollDirection { get; } = ScrollType.Vertical;
        private float ScrollSpeed { get; } = 10f;

        private bool CancelScrollOnInput { get; } = false;

        private List<KeyCode> CancelScrollKeycodes { get; } = new List<KeyCode>();

        private RectTransform ScrollWindow { get; set; }
        private ScrollRect TargetScrollRect { get; set; }

        private GameObject LastCheckedGameObject { get; set; }
        private static GameObject CurrentSelectedGameObject => EventSystem.current.currentSelectedGameObject;

        private RectTransform CurrentTargetRectTransform { get; set; }
        private bool IsManualScrollingAvailable { get; set; }

        private void Awake()
        {
            TargetScrollRect = GetComponent<ScrollRect>();
            ScrollWindow = TargetScrollRect.GetComponent<RectTransform>();
        }

        private void Update()
        {
            UpdateReferences();
            CheckIfScrollingShouldBeLocked();
            ScrollRectToLevelSelection();
        }

        private void UpdateReferences()
        {
            // update current selected rect transform
            if (CurrentSelectedGameObject != LastCheckedGameObject)
            {
                CurrentTargetRectTransform = (CurrentSelectedGameObject != null) ?
                    CurrentSelectedGameObject.GetComponent<RectTransform>() :
                    null;

                // unlock automatic scrolling
                if (CurrentSelectedGameObject != null && IsInsideScrollView(CurrentSelectedGameObject.transform.parent))
                {
                    IsManualScrollingAvailable = false;
                }
                LastCheckedGameObject = CurrentSelectedGameObject;
            }
        }

        private void CheckIfScrollingShouldBeLocked()
        {
            if (!CancelScrollOnInput || IsManualScrollingAvailable)
                return;

            foreach (var code in CancelScrollKeycodes)
            {
                if (Input.GetKeyDown(code))
                {
                    IsManualScrollingAvailable = true;

                    break;
                }
            }
        }

        private void ScrollRectToLevelSelection()
        {
            // check main references
            var referencesAreIncorrect = (TargetScrollRect == null || LayoutListGroup == null || ScrollWindow == null);

            if (referencesAreIncorrect || IsManualScrollingAvailable)
                return;

            var selection = CurrentTargetRectTransform;

            // check if scrolling is possible
            if (selection == null || !IsInsideScrollView(selection.transform.parent))
            {
                return;
            }

            // depending on selected scroll direction move the scroll rect to selection
            switch (ScrollDirection)
            {
                case ScrollType.Vertical:
                    UpdateVerticalScrollPosition(selection);
                    break;
                case ScrollType.Horizontal:
                    UpdateHorizontalScrollPosition(selection);
                    break;
                case ScrollType.Both:
                    UpdateVerticalScrollPosition(selection);
                    UpdateHorizontalScrollPosition(selection);
                    break;
            }
        }

        private void UpdateVerticalScrollPosition(RectTransform selection)
        {
            // move the current scroll rect to correct position

            var worldPos = selection.TransformPoint(selection.anchoredPosition3D);
            var normalizedPos = LayoutListGroup.transform.InverseTransformPoint(worldPos);
            var selectionPosition = -normalizedPos.y - (selection.rect.height * (1 - selection.pivot.y));

            //var selectionPosition = -selection.anchoredPosition.y - (selection.rect.height * (1 - selection.pivot.y));

            var elementHeight = selection.rect.height;
            var maskHeight = ScrollWindow.rect.height;
            var listAnchorPosition = LayoutListGroup.anchoredPosition.y;

            // get the element offset value depending on the cursor move direction
            var offlimitsValue = GetScrollOffset(selectionPosition, listAnchorPosition, elementHeight, maskHeight);

            // move the target scroll rect
            float target = (offlimitsValue / LayoutListGroup.rect.height);
            TargetScrollRect.verticalNormalizedPosition = Mathf.Clamp01(TargetScrollRect.verticalNormalizedPosition + target * Time.unscaledDeltaTime * ScrollSpeed);
        }

        private void UpdateHorizontalScrollPosition(RectTransform selection)
        {
            // move the current scroll rect to correct position
            var selectionPosition = -selection.anchoredPosition.x - (selection.rect.width * (1 - selection.pivot.x));

            var elementWidth = selection.rect.width;
            var maskWidth = ScrollWindow.rect.width;
            var listAnchorPosition = -LayoutListGroup.anchoredPosition.x;

            // get the element offset value depending on the cursor move direction
            var offlimitsValue = -GetScrollOffset(selectionPosition, listAnchorPosition, elementWidth, maskWidth);

            if(Math.Abs(offlimitsValue) < 0.001f)
                return;

            // move the target scroll rect
            TargetScrollRect.horizontalNormalizedPosition += (offlimitsValue / LayoutListGroup.rect.width) * Time.unscaledDeltaTime * ScrollSpeed;
        }

        private static float GetScrollOffset(float position, float listAnchorPosition, float targetLength, float maskLength)
        {
            if (position < listAnchorPosition + (targetLength / 2))
            {
                return (listAnchorPosition + maskLength) - (position - targetLength);
            }
            if (position + targetLength > listAnchorPosition + maskLength)
            {
                return (listAnchorPosition + maskLength) - (position + targetLength);
            }

            return 0;
        }

        public enum ScrollType
        {
            Vertical,
            Horizontal,
            Both
        }
    }
}
