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

        public static ICondition RotateLeft = new Track.KeyboardCondition(Keys.OemComma);
        public static ICondition RotateRight = new Track.KeyboardCondition(Keys.OemPeriod);

        public static ICondition CameraDrag = new MouseCondition(MouseButton.MiddleButton);
        public static ICondition SelectionDrag = new Track.MouseCondition(MouseButton.LeftButton);

        public static ICondition Copy = new Track.KeyboardCondition(Keys.C);
        public static ICondition Paste = new Track.KeyboardCondition(Keys.V);
        public static ICondition Cut = new Track.KeyboardCondition(Keys.X);

        public static ICondition Undo =
            new AllCondition(
                new AnyCondition(
                    new Track.KeyboardCondition(Keys.LeftControl),
                    new Track.KeyboardCondition(Keys.RightControl)
                ),
                new Track.KeyboardCondition(Keys.Z)
            );
        public static ICondition Redo =
            new AllCondition(
                new AnyCondition(
                    new Track.KeyboardCondition(Keys.LeftControl),
                    new Track.KeyboardCondition(Keys.RightControl)
                ),
                new AnyCondition(
                    new Track.KeyboardCondition(Keys.LeftShift),
                    new Track.KeyboardCondition(Keys.RightShift)
                ),
                new Track.KeyboardCondition(Keys.Z)
            );

        public static ICondition Create = new Track.KeyboardCondition(Keys.Enter);
        public static ICondition Remove =
            new AnyCondition(
                new Track.KeyboardCondition(Keys.Back),
                new Track.KeyboardCondition(Keys.Delete)
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
        public static ICondition SkipEdit =
            new AnyCondition(
                new KeyboardCondition(Keys.LeftAlt),
                new KeyboardCondition(Keys.RightAlt)
            );
        public static ICondition SelectionCycle =
            new AnyCondition(
                AddToSelection,
                RemoveFromSelection,
                SkipEdit
            );
        public static ICondition ResetSortOrder =
            new KeyboardCondition(Keys.O);

        public static ICondition CreateStuff = new KeyboardCondition(Keys.F1);
        public static ICondition ResetDroppedFrames = new KeyboardCondition(Keys.F2);
    }
}
