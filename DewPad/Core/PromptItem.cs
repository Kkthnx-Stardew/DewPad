namespace DewPad.Core
{
    /// <summary>
    /// One hint, one or two buttons and what they do. A pair such as the two triggers shares one label. The label is a translation key, or the
    /// text itself when <see cref="IsLiteral"/> is set, which is how a hint registered by another mod arrives with its own wording.
    /// </summary>
    public readonly record struct PromptItem(GamepadButton First, GamepadButton? Second, string LabelKey, bool IsLiteral = false)
    {
        public PromptItem(GamepadButton button, string labelKey) : this(button, null, labelKey)
        {
        }
    }
}