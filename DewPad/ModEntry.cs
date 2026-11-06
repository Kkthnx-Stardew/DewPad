using DewPad.Core;
using DewPad.Integration;
using DewPad.Prompts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace DewPad
{
    /// <summary>
    /// Controller button prompts for the game's menus, and the game's own PlayStation, Switch and Circle to confirm art in place of its Xbox art.
    /// It reads menus and draws, and never handles, blocks or sends an input, so it cannot change what a button does. No Harmony and nothing saved.
    /// </summary>
    public sealed class ModEntry : Mod
    {
        private const string ControllerSheet = "LooseSprites/ControllerMaps";

        private ModConfig _config = new();
        private PerScreen<PromptRenderer?>? _renderers;
        private bool _warnedSheetSize;
        private PromptStyle _detected = PromptStyle.Xbox;
        private string _padName = "";
        private bool? _onSteamDeck;
        private readonly Dictionary<string, PromptItem[]> _registered = new();

        public override void Entry(IModHelper helper)
        {
            try
            {
                _config = helper.ReadConfig<ModConfig>() ?? new ModConfig();
            }
            catch (Exception ex)
            {
                // a config.json that cannot be read must not stop the mod, and the file is left alone
                Monitor.Log($"Could not read config.json, so the default settings are used. Fix it or delete it. ({ex.Message})", LogLevel.Warn);
            }
            _config.Normalize();

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.Display.MenuChanged += (_, _) => DetectPad();
            helper.Events.Content.LocaleChanged += (_, _) => InvalidateHints();
            helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
            helper.ConsoleCommands.Add("dewpad", "Shows what DewPad sees in the open menu, on demand.", (_, _) => PrintStatus());
        }

        private string T(string key) => Helper.Translation.Get(key);

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            _renderers = new PerScreen<PromptRenderer?>(() => new PromptRenderer(T));
            ConfigMenu.Register(Helper, ModManifest, _config, Monitor, OnSettingsSaved);
            DetectPad();
        }

        /// <summary>The style that is drawn. A fixed setting wins, and Auto follows the pad's own name, which is Xbox when the pad does not say otherwise.</summary>
        private PromptStyle EffectiveStyle => _config.PromptStyle == PromptStyle.Auto ? _detected : _config.PromptStyle;

        /// <summary>
        /// Asks Steam whether this is a Steam Deck, the same call the game makes for itself. Without Steam, or when the call fails, it is not a Deck.
        /// </summary>
        private bool IsSteamDeck()
        {
            // the answer cannot change while the game runs, so Steam is asked once, and a failing call is not repeated every time a menu opens
            if (_onSteamDeck is { } known)
                return known;

            try
            {
                _onSteamDeck = QuerySteamDeck();
            }
            catch (Exception)
            {
                // no Steam library, Steam not running or the call failing all mean the same, not a Deck
                _onSteamDeck = false;
            }
            return _onSteamDeck.Value;
        }

        // kept apart so that a missing Steam library fails here, inside the try above, and not where the caller is compiled
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool QuerySteamDeck() => Steamworks.SteamUtils.IsSteamRunningOnSteamDeck();

        /// <summary>Reads the name of the first player's pad. Cheap, and only done when a menu opens, which is when a changed pad matters.</summary>
        private void DetectPad()
        {
            try
            {
                var index = Game1.playerOneIndex;
                var capabilities = (int)index >= 0 ? GamePad.GetCapabilities(index) : default;
                string name = capabilities.IsConnected ? capabilities.DisplayName ?? "" : "";
                bool onDeck = IsSteamDeck();
                var style = PadNames.Choose(name, onDeck);
                if (name == _padName && style == _detected)
                    return;

                bool styleChanged = style != _detected;
                _padName = name;
                _detected = style;
                if (styleChanged && _config.PromptStyle == PromptStyle.Auto)
                    OnSettingsSaved();
            }
            catch (Exception ex)
            {
                // a pad that cannot be read leaves the style as it was, which is the Xbox art until something better is known
                Monitor.Log($"Could not read the pad name, so the style stays {_detected}. {ex.Message}", LogLevel.Debug);
            }
        }

        private void OnSettingsSaved()
        {
            // the game's controller sheet is edited from the settings, so it is read again and the hints are laid out again
            Helper.GameContent.InvalidateCache(ControllerSheet);
            InvalidateHints();
        }

        private void InvalidateHints() => _renderers?.GetActiveValues().ToList().ForEach(pair => pair.Value?.Invalidate());

        // ---- the game's own controller art -----------------------------------------

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (!_config.Enabled || !_config.ReplaceGameIcons || !e.NameWithoutLocale.IsEquivalentTo(ControllerSheet))
                return;
            int band = SheetBands.BandFor(EffectiveStyle);
            if (band == 0)
                return;

            e.Edit(asset =>
            {
                var image = asset.AsImage();
                if (!SheetBands.IsExpectedSize(image.Data.Width, image.Data.Height))
                {
                    // another mod changed the sheet, so it is left exactly as it is
                    if (!_warnedSheetSize)
                    {
                        _warnedSheetSize = true;
                        Monitor.Log($"The controller sheet is {image.Data.Width}x{image.Data.Height} and not the {SheetBands.SheetWidth}x{SheetBands.SheetHeight} that was read, so the game's controller icons are left alone.", LogLevel.Warn);
                    }
                    return;
                }

                // the game only ever draws the top band, which is the Xbox art. This puts the chosen layout's band there
                image.PatchImage(image.Data, new Rectangle(0, band * SheetBands.BandHeight, SheetBands.SheetWidth, SheetBands.BandHeight),
                    new Rectangle(0, 0, SheetBands.SheetWidth, SheetBands.BandHeight), PatchMode.Replace);
            });
        }

        // ---- the hint strip -----------------------------------------------------

        private void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e) => DrawHints(e.SpriteBatch);

        /// <summary>Draws the hints for the open menu, or nothing. Also what the mod's API draws, so a test sees what a player sees.</summary>
        internal void DrawHints(SpriteBatch batch)
        {
            var renderer = _renderers?.Value;
            renderer?.BeginFrame();

            // only while a controller is in use, using the game's own controller mode, so a keyboard and mouse player never sees it
            if (!_config.Enabled || !_config.ShowPrompts || !Game1.options.gamepadControls || renderer is null)
                return;

            var menu = Game1.activeClickableMenu;
            MenuFacts facts;
            PromptItem[]? registered = null;
            if (_registered.Count > 0 && !MenuFactsReader.IsBlocked(menu) && _registered.TryGetValue(menu!.GetType().FullName ?? "", out registered))
                facts = new MenuFacts(MenuKind.Registered, menu.readyToClose(), false, menu.yPositionOnScreen + menu.height);
            else
                facts = MenuFactsReader.Read(menu);
            if (facts.Kind == MenuKind.Unknown)
                return;
            renderer.Draw(batch, facts, EffectiveStyle, _config.SwitchFaceLayout, _config.PromptScale, registered);
        }

        // ---- the mod's API ----------------------------------------------------------

        // the consumer's id goes into the log lines about its hints, so a bad registration can be traced to the mod that made it
        public override object GetApi(IModInfo mod) => new DewPadApi(this, mod.Manifest.UniqueID);

        /// <summary>Registers the hints for a menu class of another mod, by the full name of the class. Replaces any earlier registration of it.</summary>
        internal bool RegisterMenu(string menuTypeFullName, IEnumerable<string?>? hints, string modId)
        {
            var items = HintSpec.ParseAll(hints, out int skipped);
            if (skipped > 0)
                Monitor.Log($"{modId} registered hints for {menuTypeFullName} and {skipped} were skipped. A hint is Button=Text, such as A=Select, with up to {HintSpec.MaxHints} hints, no button twice and text of at most {HintSpec.MaxTextLength} characters.", LogLevel.Warn);
            if (string.IsNullOrWhiteSpace(menuTypeFullName) || items.Length == 0)
            {
                _registered.Remove(menuTypeFullName ?? "");
                return false;
            }
            _registered[menuTypeFullName] = items;
            Monitor.Log($"{modId} registered {items.Length} hints for {menuTypeFullName}.", LogLevel.Trace);
            return true;
        }

        internal void UnregisterMenu(string menuTypeFullName) => _registered.Remove(menuTypeFullName ?? "");

        internal Rectangle HintBounds => _renderers?.Value?.Bounds ?? Rectangle.Empty;

        internal int HintCount => _renderers?.Value?.Count ?? 0;

        internal string[] HintLabels => _renderers?.Value?.Labels() ?? Array.Empty<string>();

        // ---- diagnostics ----------------------------------------------------------

        private void PrintStatus()
        {
            var menu = Game1.activeClickableMenu;
            var facts = MenuFactsReader.Read(menu);
            var renderer = _renderers?.Value;
            Monitor.Log(
                $"DewPad {ModManifest.Version}. Enabled {_config.Enabled}, style setting {_config.PromptStyle}, drawing {EffectiveStyle} (sheet band {SheetBands.BandFor(EffectiveStyle)}), " +
                $"pad name '{(_padName == "" ? "none connected" : _padName)}' read as {_detected}, " +
                $"controller mode {Game1.options.gamepadControls}, controller sheet {(Game1.controllerMaps is null ? "missing" : SheetBands.IsExpectedSize(Game1.controllerMaps.Width, Game1.controllerMaps.Height) ? "as expected" : "changed by another mod")}. " +
                $"Open menu {menu?.GetType().Name ?? "none"} read as {facts.Kind}, can close {facts.CanClose}, can sort {facts.CanOrganize}, hints shown {renderer?.Count ?? 0}.",
                LogLevel.Info);
        }
    }
}
