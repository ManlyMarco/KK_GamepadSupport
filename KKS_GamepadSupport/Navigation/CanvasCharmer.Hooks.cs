using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ADV;
using ChaCustom;
using ExternalFile;
using FileListUI;
using HarmonyLib;
using SceneAssist;
using StrayTech;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace KK_GamepadSupport.Navigation
{
    public partial class CanvasCharmer
    {
        private static class Hooks
        {
            private static readonly HashSet<string> _hSpriteKillList = new HashSet<string> {
                "OnActionHoushiMenuBreast",
                "OnActionHoushiMenuHand",
                "OnActionHoushiMenuMouth",
                "OnActionMenu",
                "OnAutoFinish",
                "OnBackDislikesClick",
                "OnChangeMotionClick",
                "OnClickAccessory",
                "OnClickAllAccessory",
                "OnClickAllAccessoryGroup1",
                "OnClickAllAccessoryGroup2",
                "OnClickAllCloth",
                "OnClickChangeMoveAxis",
                "OnClickCloth",
                "OnClickClothMale",
                "OnClickConfig",
                "OnClickCoordinateChange",
                "OnClickHelp",
                "OnClickHSceneEnd",
                "OnClickInitMoveAxis",
                "OnClickLightColorInit",
                "OnClickLightDirInit",
                "OnClickMaleAccessoryGroup1",
                "OnClickMaleAccessoryGroup2",
                "OnClickMaleCoordinateChange",
                "OnClickMoveAxisDraw",
                "OnClickMoveAxisInitScale",
                "OnClickMoveAxisInitSpeed",
                "OnClickPeepingRestart",
                "OnClickTrespassing",
                "OnClothChange",
                "OnClothCharaClick",
                "OnCondomClick",
                "OnDetachItemClick",
                "OnDrinkClick",
                "OnFemaleClick",
                "OnFemaleDressSubMenu",
                "OnFrontDislikesClick",
                "OnIdleClick",
                "OnImmediatelyFinishFemale",
                "OnImmediatelyFinishMale",
                "OnInsertAnalClick",
                "OnInsertAnalNoVoiceClick",
                "OnInsertClick",
                "OnInsertNoVoiceClick",
                "OnInsideClick",
                "OnLoopClick",
                "OnMainMenu",
                "OnMaleClick",
                "OnOLoopClick",
                "OnOrgSClick",
                "OnOrgWClick",
                "OnOutsideClick",
                "OnPullClick",
                "OnRePlayClick",
                "OnSameSClick",
                "OnSameWClick",
                "OnSpeedMouseUp",
                "OnStopIdleClick",
                "OnSubMenu",
                "OnSubMenuMale",
                "OnSubMenuMultiMale",
                "OnVomitClick"
            };

            public static void InitHooks()
            {
                _hi = Harmony.CreateAndPatchAll(typeof(Hooks), GamepadSupportPlugin.Guid + ".CanvasCharmer");

                // Fix keyboard navigation not working in HSprite / h scene
                var hspriteTargets = AccessTools.GetDeclaredMethods(typeof(HSprite));
                var mouseKillerTpl = AccessTools.Method(typeof(Hooks), nameof(MouseCheckKillerTpl));
                foreach (var m in hspriteTargets.Where(x => _hSpriteKillList.Contains(x.Name)))
                    _hi.Patch(original: m, transpiler: new HarmonyMethod(mouseKillerTpl));
            }

            private static Harmony _hi;
            private static bool _disabled;
            public static void RemoveHooks()
            {
                if (_disabled) return;

                foreach (var fix in FindObjectsOfType<PointerActionKeyboardFix>().Cast<Object>().Concat(FindObjectsOfType<CharaListKeyboardFix>()))
                    Destroy(fix);

                _hi.UnpatchSelf();
                _hi = null;
                _disabled = true;
            }

            /// <summary>
            /// Fix opening dropdowns not updating navigation
            /// </summary>
            [HarmonyPostfix]
            [HarmonyPatch(typeof(TMP_Dropdown), nameof(TMP_Dropdown.Show))]
            public static void TMP_DropdownShowPost()
            {
                if (_disabled) return;

                _instance.CanvasManager.NeedsCanvasesRefresh = true;
            }

            /// <summary>
            /// Fix opening yes/no windows in maker not updating navigation
            /// </summary>
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CustomCheckWindow), nameof(CustomCheckWindow.Setup))]
            public static void CustomCheckWindow_Setup_Post()
            {
                if (_disabled) return;

                _instance.CanvasManager.NeedsCanvasesRefresh = true;
            }

            /// <summary>
            /// Fix pressing A not working on some UI items
            /// </summary>
            [HarmonyPostfix]
            [HarmonyPatch(typeof(HSprite), nameof(HSprite.LoadMotionList))]
            public static void FixPointerAction()
            {
                if (_disabled) return;

                foreach (var pointerAction in FindObjectsOfType<PointerAction>())
                {
                    pointerAction.GetOrAddComponent<PointerActionKeyboardFix>();
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(CanvasGroup), nameof(CanvasGroup.alpha), MethodType.Setter)]
            public static void CanvasGroupAlphaChanged(CanvasGroup __instance, float value)
            {
                if (_disabled) return;

                if (value > 0.99f && __instance.alpha <= 0.99f || value < 0.01f && __instance.alpha >= 0.01f)
                {
                    if (GamepadSupportPlugin.CanvasDebug.Value) GamepadSupportPlugin.Logger.LogDebug(
                        $"CanvasGroupAlphaChanged triggered for value={value} oldValue={__instance.alpha} name={__instance.FullPath()}");
                    _instance.CanvasManager.NeedsCanvasesRefresh = true;
                }
            }

            /// <summary>
            /// Disable arrow keys moving the camera (now they navigate the UI)
            /// </summary>
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(BaseCameraControl_Ver2), nameof(BaseCameraControl_Ver2.InputKeyProc))]
            public static IEnumerable<CodeInstruction> CameraControlDisableArrows(IEnumerable<CodeInstruction> instructions)
            {
                var keys = new[] { KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.UpArrow, KeyCode.DownArrow };
                foreach (var instruction in instructions)
                {
                    if ((instruction.opcode == OpCodes.Ldc_I4 || instruction.opcode == OpCodes.Ldc_I4_S) && keys.Any(x => ((int)x).Equals(instruction.operand)))
                    {
                        // Input.GetKey is always false for KeyCode.None, reuse to keep labels
                        instruction.opcode = OpCodes.Ldc_I4;
                        instruction.operand = (int)KeyCode.None;
                        yield return instruction;
                    }
                    else
                    {
                        yield return instruction;
                    }
                }
            }

            public static IEnumerable<CodeInstruction> MouseCheckKillerTpl(IEnumerable<CodeInstruction> instructions)//, MethodBase __originalMethod)
            {
                foreach (var instruction in instructions)
                {
                    if (instruction.operand is MethodInfo m && m.Name == "GetMouseButtonUp")
                    {
                        // Pop method argument
                        yield return new CodeInstruction(OpCodes.Pop);
                        // Push true
                        yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                        //Console.WriteLine(__originalMethod.Name);
                    }
                    else
                    {
                        yield return instruction;
                    }
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(CustomSelectListCtrl), nameof(CustomSelectListCtrl.SetToggleHandler))]
            [HarmonyPatch(typeof(ExternalFileListCtrl), nameof(ExternalFileListCtrl.SetToggleHandler))]
            [HarmonyPatch(typeof(EmblemSelectListCtrl), nameof(EmblemSelectListCtrl.SetToggleHandler))]
            [HarmonyPatch(typeof(UGUI_AssistLibrary.UIAL_ListCtrl), nameof(UGUI_AssistLibrary.UIAL_ListCtrl.SetToggleHandler))]
            public static void SetToggleHandlerPost(GameObject obj)
            {
                obj.GetOrAddComponent<CharaListKeyboardFix>();
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(ThreadFileListCtrl<CustomFileInfo, CustomFileInfoComponent>), nameof(ThreadFileListCtrl<CustomFileInfo, CustomFileInfoComponent>.SetToggleHandler))]
            public static void SetToggleHandlerPostMb(MonoBehaviour fic)
            {
                fic.GetOrAddComponent<CharaListKeyboardFix>();
            }

            /// <summary>
            /// Disable up arrow hotkey for ADV backlog
            /// </summary>
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(KeyInput), nameof(KeyInput.BackLogButton))]
            public static IEnumerable<CodeInstruction> BackLogButtonTpl(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var instruction in instructions)
                {
                    if (((int)KeyCode.UpArrow).Equals(instruction.operand))
                        instruction.operand = (int)KeyCode.None;

                    yield return instruction;
                }
            }

            /// <summary>
            /// Disable down arrow hotkey for ADV next
            /// </summary>
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(KeyInput), nameof(KeyInput.TextNext))]
            public static IEnumerable<CodeInstruction> TextNextTpl(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var instruction in instructions)
                {
                    if (((int)KeyCode.DownArrow).Equals(instruction.operand))
                        instruction.operand = (int)KeyCode.None;

                    yield return instruction;
                }
            }
        }
    }
}
