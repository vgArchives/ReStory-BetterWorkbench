using System.Collections.Generic;
using Rewired;

namespace ReStoryBetterWorkbench;

internal static class GamepadHotkeys
{
    private const int MissingActionId = -1;
    private const double HoldThresholdSeconds = 0.5;

    private static readonly Dictionary<BenchGamepadButton, (string GameActionName, string Label)> Buttons = new()
    {
        { BenchGamepadButton.LeftStickClick, ("UILeftStickButton", "L3") },
        { BenchGamepadButton.RightStickClick, ("UIRightStickButton", "R3") }
    };

    private static readonly Dictionary<BenchGamepadButton, int> ActionIdsByButton = new();
    private static readonly HashSet<BenchGamepadButton> ButtonsHandledThisPress = new();

    internal static string Label(BenchGamepadButton button)
    {
        return Buttons.TryGetValue(button, out (string GameActionName, string Label) info) ? info.Label : "";
    }

    internal static bool WasPressed(BenchGamepadButton button)
    {
        Player player = GamePlayer();
        int actionId = ActionIdFor(button);

        return player != null && actionId != MissingActionId && player.GetButtonDown(actionId);
    }

    internal static GamepadButtonPress ReadTapOrHold(BenchGamepadButton button)
    {
        Player player = GamePlayer();
        int actionId = ActionIdFor(button);

        if (player == null || actionId == MissingActionId)
            return GamepadButtonPress.None;

        if (player.GetButtonDown(actionId))
        {
            ButtonsHandledThisPress.Remove(button);
        }

        if (ButtonsHandledThisPress.Contains(button))
            return GamepadButtonPress.None;

        bool isHeldPastThreshold = player.GetButton(actionId)
                                   && player.GetButtonTimePressed(actionId) >= HoldThresholdSeconds;

        if (isHeldPastThreshold)
        {
            ButtonsHandledThisPress.Add(button);
            return GamepadButtonPress.Hold;
        }

        return player.GetButtonUp(actionId) ? GamepadButtonPress.Tap : GamepadButtonPress.None;
    }

    private static Player GamePlayer()
    {
        return ReInput.isReady ? ReInput.players.GetPlayer(RewiredConsts.Player.Player0) : null;
    }

    private static int ActionIdFor(BenchGamepadButton button)
    {
        if (ActionIdsByButton.TryGetValue(button, out int cachedActionId))
            return cachedActionId;

        bool isBoundToGameAction = Buttons.TryGetValue(button, out (string GameActionName, string Label) info);

        if (!isBoundToGameAction || !ReInput.isReady)
            return MissingActionId;

        InputAction action = ReInput.mapping.GetAction(info.GameActionName);
        int actionId = action == null ? MissingActionId : action.id;

        if (action == null)
        {
            Log.Warning($"Gamepad button {button} is disabled: the game has no \"{info.GameActionName}\" "
                + "input action anymore, most likely renamed by a game update.");
        }

        ActionIdsByButton[button] = actionId;
        return actionId;
    }
}
