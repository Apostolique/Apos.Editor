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
        public static ICondition RectDrag = new Track.MouseCondition(MouseButton.LeftButton);

        public static ICondition AlignToGrid = new KeyboardCondition(Keys.LeftControl);

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
        /// <summary>Adds more elements to an existing selection group.</summary>
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
        public static ICondition ResetOrder = new KeyboardCondition(Keys.O);
        public static ICondition ResetResize = new KeyboardCondition(Keys.R);

        public static ICondition CreateStuff = new KeyboardCondition(Keys.F1);
        public static ICondition ResetDroppedFrames = new KeyboardCondition(Keys.F2);

        public static ICondition ClearPaths = new KeyboardCondition(Keys.L);
        public static ICondition ToggleNegativePath = new KeyboardCondition(Keys.P);
        public static ICondition ToggleVertical = new KeyboardCondition(Keys.Y);
        public static ICondition ToggleHorizontal = new KeyboardCondition(Keys.U);

        public static ICondition ToggleLilypads = new KeyboardCondition(Keys.D1);
        public static ICondition ToggleWoods = new KeyboardCondition(Keys.D2);
        public static ICondition ToggleClouds = new KeyboardCondition(Keys.D3);
    }
}
