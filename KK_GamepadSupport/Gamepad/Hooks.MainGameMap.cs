using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using ActionGame;
using ActionGame.Chara.Mover;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace KK_GamepadSupport.Gamepad
{
    internal static partial class Hooks
    {
        private static class MainGameMap
        {
            public static void InitHooks(Harmony hi)
            {
                hi.PatchAll(typeof(MainGameMap));
            }

            #region Walking

            [HarmonyTranspiler]
            [HarmonyPatch(typeof(Main), "Update")]
            public static IEnumerable<CodeInstruction> MainUpdateTpl(IEnumerable<CodeInstruction> instructions)
            {
                var newMethod = AccessTools.Method(typeof(MainGameMap), nameof(GetMovementAngle));

                var list = instructions.ToList();
                var i = list.FindIndex(instruction => instruction.opcode == OpCodes.Initobj);

                if (i > 0 && list[i - 1].opcode == OpCodes.Ldloca_S && list[i + 1].opcode == OpCodes.Ldloc_3)
                {
                    list[i - 1].opcode = OpCodes.Nop;
                    list[i - 1].operand = null;
                    list[i].opcode = OpCodes.Call;
                    list[i].operand = newMethod;
                    list[i + 1].opcode = OpCodes.Nop;
                }
                else
                {
                    Logger.Log(LogLevel.Error,
                        "Failed to patch, could not find transplier target\n" + new StackTrace());
                }

                return list;
            }

            public static float? GetMovementAngle()
            {
                if (GamepadWhisperer.CurrentState.IsConnected)
                {
                    var stickPosition = GamepadWhisperer.GetLeftStickDpadCombined();
                    if (stickPosition.magnitude > 0.01f)
                    {
                        var absAngle = Vector2.Angle(Vector2.up, stickPosition);
                        return stickPosition.x >= 0 ? absAngle : -absAngle;
                    }
                }

                return null;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(ActionInput), nameof(ActionInput.isWalk), MethodType.Getter)]
            public static bool isWalkHook(ref bool __result)
            {
                if (_disabled) return true;
                if (GamepadWhisperer.CurrentState.IsConnected)
                {
                    var movementAmount = GamepadWhisperer.GetLeftStickDpadCombined().magnitude;
                    if (movementAmount > 0.01f && movementAmount < 0.9f)
                    {
                        __result = true;
                        return false;
                    }
                }

                return true;
            }

            #endregion

            #region Buttons

            [HarmonyPrefix]
            [HarmonyPatch(typeof(ActionInput), nameof(ActionInput.isAction), MethodType.Getter)]
            public static bool isActionHook(ref bool __result)
            {
                if (_disabled) return true;
                if (GamepadWhisperer.GetButtonDown(state => state.Buttons.A))
                {
                    __result = true;
                    return false;
                }

                return true;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(ActionInput), nameof(ActionInput.isCursorLock), MethodType.Getter)]
            public static bool isCursorLockHook(ref bool __result)
            {
                if (_disabled) return true;
                if (GamepadWhisperer.GetButtonDown(state => state.Buttons.B))
                {
                    __result = true;
                    return false;
                }

                return true;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(ActionInput), nameof(ActionInput.isViewChange), MethodType.Getter)]
            public static bool isViewChangeHook(ref bool __result)
            {
                if (_disabled) return true;
                if (GamepadWhisperer.GetButtonDown(state => state.Buttons.X))
                {
                    __result = true;
                    return false;
                }

                return true;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(ActionInput), nameof(ActionInput.isViewTurn), MethodType.Getter)]
            public static bool isViewTurnHook(ref bool __result)
            {
                if (_disabled) return true;
                if (GamepadWhisperer.GetButtonDown(state => state.Buttons.LeftShoulder))
                {
                    __result = true;
                    return false;
                }

                return true;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(ActionInput), nameof(ActionInput.isCrouch), MethodType.Getter)]
            public static bool isCrouchHook(ref bool __result)
            {
                if (_disabled) return true;
                if (GamepadWhisperer.GetButton(state => state.Buttons.RightShoulder))
                {
                    __result = true;
                    return false;
                }

                return true;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(ActionInput), nameof(ActionInput.isViewPlayer), MethodType.Getter)]
            public static bool isViewPlayerHook(ref bool __result)
            {
                if (_disabled) return true;
                if (GamepadWhisperer.GetButtonDown(state => state.Buttons.RightStick) && !CursorEmulator.EmulatingCursor())
                {
                    __result = true;
                    return false;
                }

                return true;
            }

            #endregion
        }
    }
}