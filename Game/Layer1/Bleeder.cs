using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameProject {
    public class Bleeder {
        public Texture2D Texture { get; set; }
        public Rectangle Source { get; set; }

        public string texture_name { get; set; }
        public int x { get; set ;}
        public int y { get; set ;}
        public int width { get; set ;}
        public int height { get; set ;}
    }
}
