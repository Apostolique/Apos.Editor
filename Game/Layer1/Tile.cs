using MonoGame.Extended.TextureAtlases;

namespace GameProject {
    public class Tile {
        public Tile(Type type) {
            _type = type;

            switch (_type) {
                case Type.Red:
                    Texture = new TextureRegion2D(Assets.Tiles, 0, 0, 50, 50);
                    break;
                case Type.Blue:
                    Texture = new TextureRegion2D(Assets.Tiles, 51, 0, 50, 50);
                    break;
                case Type.Green:
                    Texture = new TextureRegion2D(Assets.Tiles, 0, 51, 50, 50);
                    break;
                default:
                    Texture = new TextureRegion2D(Assets.Tiles, 51, 51, 50, 50);
                    break;
            }
        }

        public TextureRegion2D Texture;

        public enum Type {
            Red,
            Blue,
            Green,
            Yellow
        }

        Type _type;
    }
}
