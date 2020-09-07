using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ADV;
using HarmonyLib;
using SceneAssist;
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
                var hi = Harmony.CreateAndPatchAll(typeof(Hooks), Guid);

                // Fix keyboard navigation not working in chara/map lists
                var handlerPost = AccessTools.Method(typeof(Hooks), nameof(SetToggleHandlerPost));
                foreach (var methodInfo in new[]
                {
                    // Some are KK or KKP specific, hence why not using typeof
                    AccessTools.Method(Type.GetType("ActionGame.ClassRoomFileListCtrl, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", false), "SetToggleHandler", new[]{typeof(GameObject)}) ,
                    AccessTools.Method(Type.GetType("StaffRoom.StaffRoomCharaListCtrl, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", false), "SetToggleHandler", new[]{typeof(GameObject)}),
                    AccessTools.Method(Type.GetType("StaffRoom.StaffRoomMapListCtrl, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", false), "SetToggleHandler", new[]{typeof(GameObject)}),

                    AccessTools.Method(Type.GetType("ChaCustom.CustomSelectListCtrl, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", false), "SetToggleHandler", new[]{typeof(GameObject)}),
                    AccessTools.Method(Type.GetType("ExternalFile.ExternalFileListCtrl, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", false), "SetToggleHandler", new[]{typeof(GameObject)}),
                    AccessTools.Method(Type.GetType("EmblemSelectListCtrl, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", false), "SetToggleHandler", new[]{typeof(GameObject)}),

                    AccessTools.Method(Type.GetType("UGUI_AssistLibrary.UIAL_ListCtrl, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", false), "SetToggleHandler", new[]{typeof(GameObject)}),
                })
                {
                    if (methodInfo != null)
                        hi.Patch(methodInfo, null, new HarmonyMethod(handlerPost));
                }

                // KKP specific, has a MonoB argument instead of GameObj like others
                var kkpList = AccessTools.Method(Type.GetType("FileListUI.ThreadFileListCtrl`2[[ChaCustom.CustomFileInfo, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null],[ChaCustom.CustomFileInfoComponent, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]], Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", false), "SetToggleHandler");
                if (kkpList != null)
                    hi.Patch(kkpList, null, new HarmonyMethod(AccessTools.Method(typeof(Hooks), nameof(SetToggleHandlerPostForParty))));

                // Fix keyboard navigation not working in HSprite / h scene
                var hspriteTargets = AccessTools.GetDeclaredMethods(typeof(HSprite));
                var mouseKillerTpl = AccessTools.Method(typeof(Hooks), nameof(MouseCheckKillerTpl));
                foreach (var mName in _hSpriteKillList)
                {
                    foreach (var m in hspriteTargets.Where(x => x.Name == mName))
                    {
                        hi.Patch(m, null, null, new HarmonyMethod(mouseKillerTpl));
                    }
                }
            }

            private static bool _disabled;
            public static void RemoveHooks()
            {
                foreach (var fix in FindObjectsOfType<PointerActionKeyboardFix>().Cast<Object>().Concat(FindObjectsOfType<CharaListKeyboardFix>()))
                    Destroy(fix);

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

                _instance.CanvasManager.UpdateCanvases();
            }

            /// <summary>
            /// Fix pressing A not working on some UI items
            /// </summary>
            [HarmonyPostfix]
            [HarmonyPatch(typeof(HSprite), "LoadMotionList")]
            public static void FixPointerAction()
            {
                if (_disabled) return;

                foreach (var pointerAction in FindObjectsOfType<PointerAction>())
                {
                    pointerAction.GetOrAddComponent<PointerActionKeyboardFix>();
                }
            }

            /// <summary>
            /// Disable arrow keys moving the camera (now they navigate the UI)
            /// </summary>
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(BaseCameraControl_Ver2), "InputKeyProc")]
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

            public static IEnumerable<CodeInstruction> MouseCheckKillerTpl(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var instruction in instructions)
                {
                    if (instruction.operand is MethodInfo m && m.Name == "GetMouseButtonUp")
                    {
                        // Pop method argument
                        yield return new CodeInstruction(OpCodes.Pop);
                        // Push true
                        yield return new CodeInstruction(OpCodes.Ldc_I4_1);
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
