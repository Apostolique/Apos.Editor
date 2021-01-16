using Apos.Input;
using Track = Apos.Input.Track;
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
        public static ICondition SelectionDrag = new Track.MouseCondition(MouseButton.LeftButton);

        public static ICondition CreateEntity = new KeyboardCondition(Keys.Enter);
        public static ICondition RemoveEntity =
            new AnyCondition(
                new KeyboardCondition(Keys.Back),
                new KeyboardCondition(Keys.Delete)
            );
        public static ICondition AddToSelection =
            new AnyCondition(
                new KeyboardCondition(Keys.LeftShift),
                new KeyboardCondition(Keys.RightShift)
            );
        public static ICondition RemoveFromSelection =
            new AnyCondition(
                new KeyboardCondition(Keys.LeftControl),
                new KeyboardCondition(Keys.RightControl)
            );
        public static ICondition SelectionCycle =
            new AnyCondition(
                AddToSelection,
                RemoveFromSelection
            );

        public static ICondition SpawnStuff = new KeyboardCondition(Keys.F1);
    }
}
