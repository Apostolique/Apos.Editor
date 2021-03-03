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
        /// <summary>Adds more elements to an exising selection group.</summary>
        public static ICondition AddModifier =
            new AnyCondition(
                new KeyboardCondition(Keys.LeftShift),
                new KeyboardCondition(Keys.RightShift)
            );
        /// <summary>Removes elements from an existing selection group.</summary>
        public static ICondition RemoveModifier =
            new AnyCondition(
                new KeyboardCondition(Keys.LeftControl),
                new KeyboardCondition(Keys.RightControl)
            );
        /// <summary>Skips editing to start a new selection group.</summary>
        public static ICondition SkipEditModifier =
            new AnyCondition(
                new KeyboardCondition(Keys.LeftAlt),
                new KeyboardCondition(Keys.RightAlt)
            );
        public static ICondition SelectionCycle =
            new AnyCondition(
                AddModifier,
                RemoveModifier,
                SkipEditModifier
            );
        public static ICondition ResetOrder =
            new KeyboardCondition(Keys.O);

        public static ICondition CreateStuff = new KeyboardCondition(Keys.F1);
        public static ICondition ResetDroppedFrames = new KeyboardCondition(Keys.F2);
    }
}
