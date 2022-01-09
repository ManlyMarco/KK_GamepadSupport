using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ADV;
using ChaCustom;
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
            private static readonly string[] _hSpriteKillList = new[]{
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
                "OnClickConfig",
                "OnClickCoordinateChange",
                "OnClickHelp",
                "OnClickHSceneEnd",
                "OnClickInitMoveAxis",
                "OnClickLightColorInit",
                "OnClickLightDirInit",
                "OnClickMoveAxisDraw",
                "OnClickMoveAxisInitScale",
                "OnClickMoveAxisInitSpeed",
                "OnClickPeepingRestart",
                "OnClickTrespassing",
                "OnClothChange",
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
                "OnVomitClick"};

            public static void InitHooks()
            {
                MethodInfo TryMethod(Type type, string name, Type[] parameters = null, Type[] generics = null)
                {
                    return type == null ? null : AccessTools.Method(type, name, parameters, generics);
                }

                _hi = Harmony.CreateAndPatchAll(typeof(Hooks), GamepadSupportPlugin.Guid + ".CanvasCharmer");

                // Fix keyboard navigation not working in chara/map lists
                var handlerPost = AccessTools.Method(typeof(Hooks), nameof(SetToggleHandlerPost));
                foreach (var methodInfo in new[]
                {
                    // Some are KK or KKP specific, hence why not using typeof
                    TryMethod(Type.GetType("ActionGame.ClassRoomFileListCtrl, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", false), "SetToggleHandler", new[]{typeof(GameObject)}) ,
                    TryMethod(Type.GetType("StaffRoom.StaffRoomCharaListCtrl, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", false), "SetToggleHandler", new[]{typeof(GameObject)}),
                    TryMethod(Type.GetType("StaffRoom.StaffRoomMapListCtrl, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", false), "SetToggleHandler", new[]{typeof(GameObject)}),

                    TryMethod(Type.GetType("ChaCustom.CustomSelectListCtrl, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", false), "SetToggleHandler", new[]{typeof(GameObject)}),
                    TryMethod(Type.GetType("ExternalFile.ExternalFileListCtrl, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", false), "SetToggleHandler", new[]{typeof(GameObject)}),
                    TryMethod(Type.GetType("EmblemSelectListCtrl, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", false), "SetToggleHandler", new[]{typeof(GameObject)}),

                    TryMethod(Type.GetType("UGUI_AssistLibrary.UIAL_ListCtrl, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", false), "SetToggleHandler", new[]{typeof(GameObject)}),
                })
                {
                    if (methodInfo != null)
                        _hi.Patch(original: methodInfo, postfix: new HarmonyMethod(handlerPost));
                }

                // KKP specific, has a MonoB argument instead of GameObj like others
                var listType = Type.GetType("FileListUI.ThreadFileListCtrl`2[[ChaCustom.CustomFileInfo, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null],[ChaCustom.CustomFileInfoComponent, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]], Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", false);
                if (listType != null)
                {
                    var kkpList = AccessTools.Method(listType, "SetToggleHandler");
                    if (kkpList != null)
                        _hi.Patch(kkpList, postfix: new HarmonyMethod(AccessTools.Method(typeof(Hooks), nameof(SetToggleHandlerPostForParty))));
                }

                // Fix keyboard navigation not working in HSprite / h scene
                var hspriteTargets = AccessTools.GetDeclaredMethods(typeof(HSprite));
                var mouseKillerTpl = AccessTools.Method(typeof(Hooks), nameof(MouseCheckKillerTpl));
                foreach (var mName in _hSpriteKillList)
                {
                    foreach (var m in hspriteTargets.Where(x => x.Name == mName))
                    {
                        _hi.Patch(original: m, transpiler: new HarmonyMethod(mouseKillerTpl));
                    }
                }
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

            public static IEnumerable<CodeInstruction> MouseCheckKillerTpl(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
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

            public static void SetToggleHandlerPost(GameObject obj)
            {
                obj.GetOrAddComponent<CharaListKeyboardFix>();
            }

            public static void SetToggleHandlerPostForParty(MonoBehaviour fic)
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
