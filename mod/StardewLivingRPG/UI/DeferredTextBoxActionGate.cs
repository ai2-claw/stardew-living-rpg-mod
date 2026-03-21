using StardewValley;
using System;

namespace StardewLivingRPG.UI;

internal enum DeferredTextAction
{
    None,
    Submit,
    Close
}

internal sealed class DeferredTextBoxActionGate
{
    private DeferredTextAction _pendingAction;
    private int _armedAtTick = -1;
    private string _armedText = string.Empty;

    public void ArmSubmit(string? currentText)
    {
        Arm(DeferredTextAction.Submit, currentText);
    }

    public void ArmClose(string? currentText)
    {
        Arm(DeferredTextAction.Close, currentText);
    }

    public DeferredTextAction Update(string? currentText)
    {
        if (_pendingAction == DeferredTextAction.None)
            return DeferredTextAction.None;

        if (Game1.ticks <= _armedAtTick)
            return DeferredTextAction.None;

        var action = _pendingAction;
        var armedText = _armedText;
        Clear();

        return string.Equals(currentText ?? string.Empty, armedText, StringComparison.Ordinal)
            ? action
            : DeferredTextAction.None;
    }

    public void Clear()
    {
        _pendingAction = DeferredTextAction.None;
        _armedAtTick = -1;
        _armedText = string.Empty;
    }

    private void Arm(DeferredTextAction action, string? currentText)
    {
        _pendingAction = action;
        _armedAtTick = Game1.ticks;
        _armedText = currentText ?? string.Empty;
    }
}
