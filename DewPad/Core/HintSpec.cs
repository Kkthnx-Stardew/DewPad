namespace DewPad.Core
{
    /// <summary>
    /// Reads the hints another mod registers, written as text so they cross from one mod to another without sharing a type. The form is the button
    /// name, or two joined by a comma, then an equals sign and the words to show, such as <c>A=Select</c> or <c>LeftTrigger,RightTrigger=Switch page</c>.
    /// </summary>
    public static class HintSpec
    {
        public const int MaxHints = 8;
        public const int MaxTextLength = 40;

        public static bool TryParse(string? spec, out PromptItem item)
        {
            item = default;
            if (string.IsNullOrWhiteSpace(spec))
                return false;

            int equals = spec.IndexOf('=');
            if (equals <= 0 || equals == spec.Length - 1)
                return false;

            string text = spec[(equals + 1)..].Trim();
            if (text.Length == 0 || text.Length > MaxTextLength)
                return false;

            var names = spec[..equals].Split(',');
            if (names.Length is < 1 or > 2 || !TryButton(names[0], out var first))
                return false;

            GamepadButton? second = null;
            if (names.Length == 2)
            {
                if (!TryButton(names[1], out var other) || other == first)
                    return false;
                second = other;
            }

            item = new PromptItem(first, second, text, IsLiteral: true);
            return true;
        }

        /// <summary>The valid hints in a list, in order and at most <see cref="MaxHints"/>, with no button used twice. A bad entry is skipped and counted.</summary>
        public static PromptItem[] ParseAll(IEnumerable<string?>? specs, out int skipped)
        {
            skipped = 0;
            var items = new List<PromptItem>();
            var used = new HashSet<GamepadButton>();
            foreach (var spec in specs ?? Array.Empty<string?>())
            {
                if (!TryParse(spec, out var item) || items.Count >= MaxHints || used.Contains(item.First) || (item.Second is { } s && used.Contains(s)))
                {
                    skipped++;
                    continue;
                }
                used.Add(item.First);
                if (item.Second is { } second)
                    used.Add(second);
                items.Add(item);
            }
            return items.ToArray();
        }

        private static bool TryButton(string name, out GamepadButton button) =>
            Enum.TryParse(name.Trim(), ignoreCase: true, out button) && Enum.IsDefined(button);
    }
}