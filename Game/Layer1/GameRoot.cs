using System;
using Apos.Input;
using Dcrew.Spatial;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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

            _quadtree = new Quadtree<Entity>();
            _camera = new CameraManager();
            _selection = new Selection(_camera.Camera);

            InputHelper.Setup(this);
        }

        protected override void Update(GameTime gameTime) {
            InputHelper.UpdateSetup();

            if (Triggers.Quit.Pressed())
                Exit();

            _camera.UpdateInput();
            _selection.UpdateInput(_camera.MouseWorld);

            if (Triggers.CreateEntity.Pressed() && _selection.Rect != null) {
                _quadtree.Add(new Entity(_selection.Rect.Value));
            }

            InputHelper.UpdateCleanup();
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(Color.Black);

            _s.Begin(transformMatrix: _camera.View);
            foreach (var n in _quadtree.QueryRect(_camera.Camera.WorldBounds(), _camera.Camera.Angle, _camera.Camera.Origin))
                n.Draw(_s);

            _selection.Draw(_s);
            _s.End();

            base.Draw(gameTime);
        }

        GraphicsDeviceManager _graphics;
        SpriteBatch _s;

        CameraManager _camera;
        Selection _selection;
        Quadtree<Entity> _quadtree;
    }
}
