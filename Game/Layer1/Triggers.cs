using Apos.Input;
using Microsoft.Xna.Framework.Input;

namespace GameProject {
    public static class Triggers {
        public static ICondition RotateLeft = new KeyboardCondition(Keys.OemComma);
        public static ICondition RotateRight = new KeyboardCondition(Keys.OemPeriod);

        public static ICondition CameraDrag = new MouseCondition(MouseButton.LeftButton);
    }
}
