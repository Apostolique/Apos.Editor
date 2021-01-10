using Apos.Input;
using Microsoft.Xna.Framework.Input;

namespace GameProject {
    public static class Triggers {
        public static ICondition Quit =
            new AnyCondition(
                new KeyboardCondition(Keys.Escape),
                new GamePadCondition(GamePadButton.Back, 0)
            );

        public static ICondition RotateLeft = new KeyboardCondition(Keys.OemComma);
        public static ICondition RotateRight = new KeyboardCondition(Keys.OemPeriod);

        public static ICondition CameraDrag = new MouseCondition(MouseButton.MiddleButton);
        public static ICondition SelectionDrag = new MouseCondition(MouseButton.LeftButton);

        public static ICondition CreateEntity = new KeyboardCondition(Keys.Enter);
    }
}
