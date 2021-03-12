using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace GameProject {
    public class Bleeder {
        public Texture2D Texture { get; set; }
        public Rectangle Source { get; set; }
        public RectangleF Inset { get; set; }
    }

    public class JsonBleeder {
        public string texture_name { get; set; }
        public JsonRect source { get; set; }
        public JsonRect? inset { get; set; }

        public class JsonRect {
            public int x { get; set ;}
            public int y { get; set ;}
            public int width { get; set ;}
            public int height { get; set ;}
        }
    }
}
