using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Apos.Input;
using System.Text.Json;
using System.IO;
using System;

namespace GameProject {
    public class GameRoot : Game {
        public GameRoot() {
            _graphics = new GraphicsDeviceManager(this);
            IsMouseVisible = true;
            Content.RootDirectory = "Content";
        }

        protected override void Initialize() {
            Window.AllowUserResizing = true;

            base.Initialize();
        }

        protected override void LoadContent() {
            _s = new SpriteBatch(GraphicsDevice);

            Settings settings = Utility.LoadJson<Settings>("Settings.json");

            _graphics.PreferredBackBufferWidth = settings.Width;
            _graphics.PreferredBackBufferHeight = settings.Height;
            IsFixedTimeStep = settings.IsFixedTimeStep;
            _graphics.SynchronizeWithVerticalRetrace = settings.IsVSync;
            _graphics.ApplyChanges();

            InputHelper.Setup(this);
        }

        protected override void Update(GameTime gameTime) {
            InputHelper.UpdateSetup();

            if (_quit.Pressed())
                Exit();

            InputHelper.UpdateCleanup();
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(Color.Black);

            base.Draw(gameTime);
        }

        GraphicsDeviceManager _graphics;
        SpriteBatch _s;

        ICondition _quit =
            new AnyCondition(
                new KeyboardCondition(Keys.Escape),
                new GamePadCondition(GamePadButton.Back, 0)
            );
    }
}
