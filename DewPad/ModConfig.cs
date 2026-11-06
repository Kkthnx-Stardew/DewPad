using DewPad.Core;

namespace DewPad
{
    /// <summary>Settings in config.json. Nothing here changes how the game reads input, so a wrong value can only change what is drawn.</summary>
    public sealed class ModConfig
    {
        /// <summary>Master switch. Off means the mod draws nothing.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Show the button hints along the bottom of supported menus while a controller is in use.</summary>
        public bool ShowPrompts { get; set; } = true;

        /// <summary>Which glyphs to draw. Auto is Xbox, because a PC cannot tell a PlayStation or Switch pad from an Xbox one once a driver has translated it.</summary>
        public PromptStyle PromptStyle { get; set; } = PromptStyle.Auto;

        /// <summary>GameArt follows the game's own Switch art, where Check is the Switch A. Position keeps every button where it sits on the pad, so the bottom button, the game's A, shows as the Switch B.</summary>
        public SwitchFaceLayout SwitchFaceLayout { get; set; } = SwitchFaceLayout.GameArt;

        /// <summary>Show the chosen layout in the game's own controller screens too, such as the controls tutorial, the fishing and arcade help and the button icons. The game already has art for all four layouts, and only draws the Xbox one.</summary>
        public bool ReplaceGameIcons { get; set; } = true;

        /// <summary>Size of the hints, 0.75 to 1.5.</summary>
        public float PromptScale { get; set; } = 1f;

        /// <summary>Puts every setting inside its allowed range, so a hand edited file cannot break the mod.</summary>
        public void Normalize()
        {
            if (!Enum.IsDefined(PromptStyle))
                PromptStyle = PromptStyle.Auto;
            if (!Enum.IsDefined(SwitchFaceLayout))
                SwitchFaceLayout = SwitchFaceLayout.Position;
            PromptScale = float.IsFinite(PromptScale) ? Math.Clamp(PromptScale, 0.75f, 1.5f) : 1f;
        }
    }
}
