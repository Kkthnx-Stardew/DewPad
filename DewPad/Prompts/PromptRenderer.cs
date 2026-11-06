using DewPad.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace DewPad.Prompts
{
    /// <summary>
    /// Draws the hint strip in the game's own look: the tooltip box, the game's own button glyphs from its controller sheet, and dark pixel pills in
    /// the same colours for the shoulder and trigger buttons. The layout is worked out once for a set of facts, a style, a size and a screen size and
    /// kept, so a frame costs a box and a few draw calls. Nothing is loaded, measured or allocated while drawing.
    /// </summary>
    internal sealed class PromptRenderer
    {
        private const int Pad = 20;          // left and right, clear of the box border
        private const int PadY = 14;
        private const int CompactPadY = 4;   // used when the room under the menu is tight, like the Steam Deck's 800 pixel screen
        private const int MinBoxHeight = 40; // the tooltip box art is 20 pixels of border at each end
        private const int ItemGap = 28;
        private const int GlyphTextGap = 6;
        private const int PairGap = 4;
        private const int EdgeMargin = 16;
        private const int MinGap = 6;
        private const float PillTextScale = 0.78f;

        // layer depths, above the menu's tooltip box
        private const float OutlineLayer = 0.948f;
        private const float BodyLayer = 0.949f;
        private const float TextLayer = 0.95f;

        // the dark of the game's own glyph discs, and the light of the letters on them
        private static readonly Color PillOutline = new(24, 24, 24);
        private static readonly Color PillBody = new(76, 76, 76);
        private static readonly Color PillText = new(214, 214, 214);

        private struct GlyphDraw
        {
            public bool IsPill;
            public Rectangle Source;
            public Rectangle Dest;
            public string Label;
            public Vector2 LabelAt;
        }

        private struct Entry
        {
            public GlyphDraw First, Second;
            public bool HasSecond;
            public string Text;
            public Vector2 TextAt;
        }

        private readonly Func<string, string> _translate;

        private Entry[] _entries = Array.Empty<Entry>();
        private Rectangle _box;
        private int _pixelScale = 1;
        private bool _hasLayout;
        private bool _drawn;

        // what the layout was made from, so a change in any of it makes a new layout
        private MenuFacts _facts;
        private IReadOnlyList<PromptItem>? _registered;
        private PromptStyle _style;
        private SwitchFaceLayout _switchLayout;
        private float _scale;
        private int _width, _height;
        private int _version;
        private int _builtVersion = -1;

        public PromptRenderer(Func<string, string> translate) => _translate = translate;

        /// <summary>Forget the layout, for a language change or any input that is not part of the key above.</summary>
        public void Invalidate() => _version++;

        /// <summary>Called at the start of every attempt to draw, so the numbers below only describe a strip that was really drawn this time.</summary>
        public void BeginFrame() => _drawn = false;

        /// <summary>The number of hints in the strip, for the diagnostics command and tests.</summary>
        public int Count => _drawn ? _entries.Length : 0;

        /// <summary>The text of each hint drawn, for tests and diagnostics. Allocates, so it is never called while drawing.</summary>
        public string[] Labels() => _drawn ? Array.ConvertAll(_entries, e => e.Text) : Array.Empty<string>();

        /// <summary>Where the strip is on screen, empty when nothing was drawn.</summary>
        public Rectangle Bounds => _drawn && _entries.Length > 0 ? _box : Rectangle.Empty;

        public void Draw(SpriteBatch batch, MenuFacts facts, PromptStyle style, SwitchFaceLayout switchLayout, float scale, IReadOnlyList<PromptItem>? registered = null)
        {
            // the sheet is read from the game each frame, because a changed setting reloads it and an old reference would be a dead texture
            var sheet = Game1.controllerMaps;
            if (sheet is null || !SheetBands.IsExpectedSize(sheet.Width, sheet.Height))
                return;

            var viewport = Game1.uiViewport;
            if (!_hasLayout || _builtVersion != _version || !ReferenceEquals(registered, _registered) || facts != _facts || style != _style || switchLayout != _switchLayout
                || scale != _scale || viewport.Width != _width || viewport.Height != _height)
                Build(facts, style, switchLayout, scale, viewport.Width, viewport.Height, registered);

            if (_entries.Length == 0)
                return;

            _drawn = true;
            IClickableMenu.drawTextureBox(batch, Game1.menuTexture, new Rectangle(0, 256, 60, 60), _box.X, _box.Y, _box.Width, _box.Height, Color.White, 1f, true);
            for (int i = 0; i < _entries.Length; i++)
            {
                ref readonly var e = ref _entries[i];
                DrawGlyph(batch, sheet, in e.First);
                if (e.HasSecond)
                    DrawGlyph(batch, sheet, in e.Second);
                Utility.drawTextWithShadow(batch, e.Text, Game1.smallFont, e.TextAt, Game1.textColor, _scale, TextLayer);
            }
        }

        private void DrawGlyph(SpriteBatch batch, Texture2D sheet, in GlyphDraw glyph)
        {
            if (!glyph.IsPill)
            {
                batch.Draw(sheet, glyph.Dest, glyph.Source, Color.White, 0f, Vector2.Zero, SpriteEffects.None, TextLayer);
                return;
            }

            // a pill with the corners cut the way the game's pixel art does it, an outline, a body and the label
            var d = glyph.Dest;
            int c = 2 * _pixelScale, line = _pixelScale;
            batch.Draw(Game1.staminaRect, new Rectangle(d.X, d.Y + c, d.Width, d.Height - 2 * c), null, PillOutline, 0f, Vector2.Zero, SpriteEffects.None, OutlineLayer);
            batch.Draw(Game1.staminaRect, new Rectangle(d.X + c, d.Y, d.Width - 2 * c, d.Height), null, PillOutline, 0f, Vector2.Zero, SpriteEffects.None, OutlineLayer);
            batch.Draw(Game1.staminaRect, new Rectangle(d.X + line, d.Y + c, d.Width - 2 * line, d.Height - 2 * c), null, PillBody, 0f, Vector2.Zero, SpriteEffects.None, BodyLayer);
            batch.Draw(Game1.staminaRect, new Rectangle(d.X + c, d.Y + line, d.Width - 2 * c, d.Height - 2 * line), null, PillBody, 0f, Vector2.Zero, SpriteEffects.None, BodyLayer);
            Utility.drawTextWithShadow(batch, glyph.Label, Game1.smallFont, glyph.LabelAt, PillText, _scale * PillTextScale, TextLayer, 0, 0);
        }

        private void Build(MenuFacts facts, PromptStyle style, SwitchFaceLayout switchLayout, float scale, int width, int height, IReadOnlyList<PromptItem>? registered, bool compact = false)
        {
            _facts = facts;
            _registered = registered;
            _style = style;
            _switchLayout = switchLayout;
            _scale = scale;
            _width = width;
            _height = height;
            _builtVersion = _version;
            _hasLayout = true;
            _entries = Array.Empty<Entry>();

            var plan = PromptPlanner.Plan(facts, registered);
            if (plan.Count == 0)
                return;

            var font = Game1.smallFont;
            // the sheet's glyphs are 28 pixel art squares, drawn whole times their size so the pixels stay square
            _pixelScale = scale >= 1.4f ? 2 : 1;
            int glyph = SheetBands.GlyphSize * _pixelScale;
            int band = SheetBands.BandFor(style);

            var widths = new float[plan.Count];
            var labels = new string[plan.Count];
            for (int i = 0; i < plan.Count; i++)
            {
                labels[i] = plan[i].IsLiteral ? plan[i].LabelKey : _translate(plan[i].LabelKey);
                float w = GlyphWidth(GlyphMap.For(style, plan[i].First, switchLayout), glyph, font, scale);
                if (plan[i].Second is { } second)
                    w += PairGap + GlyphWidth(GlyphMap.For(style, second, switchLayout), glyph, font, scale);
                widths[i] = w + GlyphTextGap + font.MeasureString(labels[i]).X * scale;
            }

            // hints that do not fit are dropped from the end, so Select and Close always stay
            int fit = PromptLayout.CountThatFit(widths, ItemGap, width - 2 * (EdgeMargin + Pad));
            if (fit == 0)
                return;

            float textHeight = font.MeasureString("Ag").Y * scale;
            float rowHeight = Math.Max(glyph, textHeight);
            int boxWidth = (int)Math.Ceiling(PromptLayout.RowWidth(widths, fit, ItemGap) + 2 * Pad);
            int boxHeight = Math.Max(MinBoxHeight, (int)Math.Ceiling(rowHeight + 2 * (compact ? CompactPadY : PadY)));

            // never over the menu: with no clear room under it the strip is not drawn at all
            int? top = PromptLayout.PlaceBelowMenu(height, boxHeight, facts.Bottom, EdgeMargin, MinGap);
            if (top is null)
            {
                // a tighter strip may still fit, and when even that does not, nothing is drawn
                if (!compact)
                    Build(facts, style, switchLayout, scale, width, height, registered, compact: true);
                return;
            }
            _box = new Rectangle((width - boxWidth) / 2, top.Value, boxWidth, boxHeight);

            var entries = new Entry[fit];
            float x = _box.X + Pad;
            float centerY = _box.Y + _box.Height / 2f;
            for (int i = 0; i < fit; i++)
            {
                var item = plan[i];
                var e = new Entry { Text = labels[i] };
                e.First = PlaceGlyph(GlyphMap.For(style, item.First, switchLayout), band, ref x, centerY, glyph, font, scale);
                if (item.Second is { } second)
                {
                    x += PairGap;
                    e.HasSecond = true;
                    e.Second = PlaceGlyph(GlyphMap.For(style, second, switchLayout), band, ref x, centerY, glyph, font, scale);
                }
                x += GlyphTextGap;
                e.TextAt = new Vector2(x, centerY - textHeight / 2f);
                x += font.MeasureString(labels[i]).X * scale + ItemGap;
                entries[i] = e;
            }
            _entries = entries;
        }

        /// <summary>A sheet glyph is one square. A pill is as wide as its label needs, and never narrower than a square.</summary>
        private float GlyphWidth(Glyph glyph, int square, SpriteFont font, float scale)
        {
            if (glyph.IsSheetGlyph)
                return square;
            return Math.Max(square, font.MeasureString(glyph.Label).X * scale * PillTextScale + 6 * _pixelScale * 2);
        }

        private GlyphDraw PlaceGlyph(Glyph glyph, int band, ref float x, float centerY, int square, SpriteFont font, float scale)
        {
            int top = (int)(centerY - square / 2f);
            if (glyph.IsSheetGlyph)
            {
                var placed = new GlyphDraw
                {
                    Source = new Rectangle(SheetBands.GlyphX(glyph.SheetColumn), SheetBands.GlyphY(band), SheetBands.GlyphSize, SheetBands.GlyphSize),
                    Dest = new Rectangle((int)x, top, square, square)
                };
                x += square;
                return placed;
            }

            int w = (int)Math.Ceiling(GlyphWidth(glyph, square, font, scale));
            var size = font.MeasureString(glyph.Label) * scale * PillTextScale;
            var pill = new GlyphDraw
            {
                IsPill = true,
                Label = glyph.Label,
                Dest = new Rectangle((int)x, top, w, square),
                LabelAt = new Vector2(x + (w - size.X) / 2f, top + (square - size.Y) / 2f)
            };
            x += w;
            return pill;
        }
    }
}
