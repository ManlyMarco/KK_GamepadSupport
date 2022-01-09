using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using ActionGame;
using ActionGame.Chara.Mover;
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
            [HarmonyPatch(typeof(Main), nameof(Main.Update))]
            public static IEnumerable<CodeInstruction> MainUpdateTpl(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
            {
                var lab = ilGenerator.DefineLabel();

                var m = new CodeMatcher(instructions)
                    .MatchForward(false,
                        new CodeMatch(OpCodes.Ldloca_S),
                        new CodeMatch(OpCodes.Initobj, typeof(float?)));
                var ins = m.Instruction;

                // 0 is used normally since this is an angle value, so can't be used as special value
                return m.InsertAndAdvance(
                        new CodeInstruction(ins),
                            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MainGameMap), nameof(GetMovementAngle))),
                            new CodeInstruction(OpCodes.Call, AccessTools.Constructor(typeof(float?), new[] { typeof(float) })),
                            new CodeInstruction(ins),
                            new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(float?), nameof(Nullable<float>.Value))),
                            new CodeInstruction(OpCodes.Ldc_R4, float.MaxValue),
                            new CodeInstruction(OpCodes.Ceq),
                            new CodeInstruction(OpCodes.Brfalse, lab)
                            )
                        .Advance(2)
                        .AddLabels(new[] { lab })
                        .Instructions();
            }

            public static float GetMovementAngle()
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
                return float.MaxValue;
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