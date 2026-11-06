using DewPad.Core;
using StardewModdingAPI;

namespace DewPad.Integration
{
    /// <summary>The part of the Generic Mod Config Menu API this mod uses. The method signatures must match the real API.</summary>
    public interface IGenericModConfigMenuApi
    {
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);

        void AddSectionTitle(IManifest mod, Func<string> text, Func<string>? tooltip = null);

        void AddParagraph(IManifest mod, Func<string> text);

        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);

        void AddNumberOption(IManifest mod, Func<float> getValue, Action<float> setValue, Func<string> name, Func<string>? tooltip = null, float? min = null, float? max = null, float? interval = null, Func<float, string>? formatValue = null, string? fieldId = null);

        void AddTextOption(IManifest mod, Func<string> getValue, Action<string> setValue, Func<string> name, Func<string>? tooltip = null, string[]? allowedValues = null, Func<string, string>? formatAllowedValue = null, string? fieldId = null);
    }

    /// <summary>Adds DewPad to the Generic Mod Config Menu, when that mod is installed.</summary>
    internal static class ConfigMenu
    {
        private const string GmcmId = "spacechase0.GenericModConfigMenu";

        public static void Register(IModHelper helper, IManifest manifest, ModConfig config, IMonitor monitor, Action onSaved)
        {
            try
            {
                var api = helper.ModRegistry.GetApi<IGenericModConfigMenuApi>(GmcmId);
                if (api is null)
                    return;

                string T(string key) => helper.Translation.Get(key);

                api.Register(manifest, reset: () => Reset(config), save: () =>
                {
                    config.Normalize();
                    helper.WriteConfig(config);
                    onSaved();
                });
                api.AddParagraph(manifest, () => T("config.note"));

                api.AddSectionTitle(manifest, () => T("config.section.general"));
                api.AddBoolOption(manifest, () => config.Enabled, v => config.Enabled = v, () => T("config.enabled.name"), () => T("config.enabled.tooltip"));
                api.AddTextOption(manifest, () => config.PromptStyle.ToString(), v => config.PromptStyle = Enum.TryParse<PromptStyle>(v, out var parsed) ? parsed : PromptStyle.Auto,
                    () => T("config.style.name"), () => T("config.style.tooltip"),
                    allowedValues: Enum.GetNames<PromptStyle>(), formatAllowedValue: v => T("config.style." + v));
                api.AddTextOption(manifest, () => config.SwitchFaceLayout.ToString(), v => config.SwitchFaceLayout = Enum.TryParse<SwitchFaceLayout>(v, out var parsed) ? parsed : SwitchFaceLayout.GameArt,
                    () => T("config.switch.name"), () => T("config.switch.tooltip"),
                    allowedValues: Enum.GetNames<SwitchFaceLayout>(), formatAllowedValue: v => T("config.switch." + v));

                api.AddSectionTitle(manifest, () => T("config.section.hints"));
                api.AddBoolOption(manifest, () => config.ShowPrompts, v => config.ShowPrompts = v, () => T("config.prompts.name"), () => T("config.prompts.tooltip"));
                api.AddNumberOption(manifest, () => config.PromptScale, v => config.PromptScale = v, () => T("config.scale.name"), () => T("config.scale.tooltip"),
                    min: 0.75f, max: 1.5f, interval: 0.25f);

                api.AddSectionTitle(manifest, () => T("config.section.icons"));
                api.AddBoolOption(manifest, () => config.ReplaceGameIcons, v => config.ReplaceGameIcons = v, () => T("config.icons.name"), () => T("config.icons.tooltip"));
            }
            catch (Exception ex)
            {
                // the menu is a convenience, so a failure here never affects the mod
                monitor.Log($"Could not add DewPad to the Generic Mod Config Menu. The settings in config.json still work. {ex.Message}", LogLevel.Warn);
            }
        }

        private static void Reset(ModConfig config)
        {
            var defaults = new ModConfig();
            config.Enabled = defaults.Enabled;
            config.ShowPrompts = defaults.ShowPrompts;
            config.PromptStyle = defaults.PromptStyle;
            config.SwitchFaceLayout = defaults.SwitchFaceLayout;
            config.PromptScale = defaults.PromptScale;
            config.ReplaceGameIcons = defaults.ReplaceGameIcons;
        }
    }
}
