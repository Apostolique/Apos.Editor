using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace GameProject {
    public static class Assets {
        public static void Setup(ContentManager content) {
            LoadTextures(content);
            LoadShaders(content);
        }

        public static void LoadFonts(ContentManager content, GraphicsDevice graphicsDevice) {
            FontSystem = FontSystemFactory.Create(graphicsDevice, 2048, 2048);
            FontSystem.AddFont(TitleContainer.OpenStream($"{content.RootDirectory}/SourceCodePro-Medium.ttf"));
        }
        public static void LoadTextures(ContentManager content) {
            Tiles = content.Load<Texture2D>("tiles");
            Pixel = content.Load<Texture2D>("pixel");
        }
        public static void LoadShaders(ContentManager content) {
            Grid = content.Load<Effect>("grid");
            Grid.Parameters["foreground_color"]?.SetValue(new Color(30, 30, 30).ToVector4());
        }

        public static FontSystem FontSystem = null!;
        public static Texture2D Tiles = null!;
        public static Texture2D Pixel = null!;
        public static Effect Grid = null!;
    }
}
