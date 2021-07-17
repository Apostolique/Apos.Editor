using System.Collections.Generic;
using System.IO;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace GameProject {
    public static class Assets {
        public static void Setup(ContentManager content) {
            LoadTextures(content);
            LoadShaders(content);
            LoadAtlas(content);
        }

        public static FontSystem FontSystem = null!;
        public static Texture2D Tiles = null!;
        public static Texture2D Pixel = null!;
        public static Texture2D Background = null!;
        public static Effect Grid = null!;
        public static Effect Infinite = null!;
        public static Dictionary<int, Bleeder> Bleeders = new Dictionary<int, Bleeder>();

        public static void LoadFonts(ContentManager content, GraphicsDevice graphicsDevice) {
            FontSystem = FontSystemFactory.Create(graphicsDevice, 2048, 2048);
            FontSystem.AddFont(TitleContainer.OpenStream($"{content.RootDirectory}/source-code-pro-medium.ttf"));
        }
        public static void LoadTextures(ContentManager content) {
            Tiles = content.Load<Texture2D>("tiles");
            Pixel = content.Load<Texture2D>("pixel");
            Background = content.Load<Texture2D>("background");
        }
        public static void LoadShaders(ContentManager content) {
            Grid = content.Load<Effect>("grid");
            Grid.Parameters["foreground_color"]?.SetValue(new Color(30, 30, 30).ToVector4());
            Infinite = content.Load<Effect>("infinite");
        }
        public static void LoadAtlas(ContentManager content) {
            LoadMeta(content, "lilypads-meta.json", World.LayerType.Lilypads);
            LoadMeta(content, "woods-meta.json", World.LayerType.Woods);
            LoadMeta(content, "clouds-meta.json", World.LayerType.Clouds);
        }

        private static void LoadMeta(ContentManager content, string name, World.LayerType layer) {
            var meta = Utility.LoadJson<List<JsonBleeder>>(Path.Combine(content.RootDirectory, name));
            foreach (var e in meta) {
                var b = new Bleeder {
                    Texture = content.Load<Texture2D>(e.texture_name),
                    Source = new Rectangle(e.source.x, e.source.y, e.source.width, e.source.height),
                    Layer = layer
                };

                if (e.inset != null) {
                    b.Inset = new RectangleF(
                        (e.inset.x - e.source.x) / (float)e.source.width,
                        (e.inset.y - e.source.y) / (float)e.source.height,
                        e.inset.width / (float)e.source.width,
                        e.inset.height / (float)e.source.height
                    );
                } else {
                    b.Inset = new RectangleF(0, 0, 1, 1);
                }

                Bleeders.Add(Bleeders.Count, b);
            }
        }
    }
}
