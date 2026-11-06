namespace DewPad.Core
{
    /// <summary>
    /// A button by what it is on the standard (XInput) pad the game reads, never by a printed label. <c>A</c> is the bottom face button, <c>B</c> the
    /// right one, <c>X</c> the left one and <c>Y</c> the top one. The glyph shown for each comes from the chosen style.
    /// </summary>
    public enum GamepadButton
    {
        A,
        B,
        X,
        Y,
        LeftBumper,
        RightBumper,
        LeftTrigger,
        RightTrigger,
        Start,
        Back
    }

    /// <summary>
    /// Which layout to show. <see cref="Auto"/> follows the name the pad gives itself (see <see cref="PadNames"/>), which is Xbox for a pad that Steam
    /// Input or a driver presents as an Xbox pad. The game's own controller sheet has art for the Xbox, PlayStation, Switch and PlayStation with Circle to
    /// confirm layouts, so those four are the choices.
    /// </summary>
    public enum PromptStyle
    {
        Auto = 0,
        Xbox = 1,
        PlayStation = 2,
        PlayStationCircleConfirm = 3,
        Switch = 4
    }

    /// <summary>
    /// Which Switch button each of the game's face buttons is shown as. <see cref="GameArt"/> follows the game's own Switch art, where Check is the
    /// Switch A, Cancel is B, Use Tool is Y and Crafting is X. <see cref="Position"/> keeps every button where it sits on the pad, so the bottom
    /// button shows as the Switch B and the right button as the A.
    /// </summary>
    public enum SwitchFaceLayout
    {
        GameArt = 0,
        Position = 1
    }
}
