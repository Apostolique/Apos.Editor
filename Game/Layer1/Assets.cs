using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace GameProject {
    public static class Assets {
        public static void Setup(ContentManager content) {
        }

        public static void LoadFonts(ContentManager content, GraphicsDevice graphicsDevice) {
            FontSystem = FontSystemFactory.Create(graphicsDevice, 2048, 2048);
            FontSystem.AddFont(TitleContainer.OpenStream($"{content.RootDirectory}/SourceCodePro-Medium.ttf"));
        }

        public static FontSystem FontSystem = null!;
    }
}
