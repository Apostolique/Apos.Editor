using System;
using System.Collections.Generic;
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
            Camera.Setup();
            _selection = new Selection();

            InputHelper.Setup(this);
        }

        protected override void Update(GameTime gameTime) {
            InputHelper.UpdateSetup();

            if (Triggers.Quit.Pressed())
                Exit();

            Camera.UpdateInput();
            _selection.UpdateInput(Camera.MouseWorld);

            if (Triggers.CreateEntity.Pressed() && _selection.Rect != null) {
                _quadtree.Add(new Entity(_selection.Rect.Value));
            }
            _hoveredEntities = _quadtree.QueryPoint(Camera.MouseWorld.ToPoint());

            InputHelper.UpdateCleanup();
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(Color.Black);

            _s.Begin(transformMatrix: Camera.View);
            foreach (var e in _quadtree.QueryRect(Camera.WorldBounds, Camera.Angle, Camera.Origin))
                e.Draw(_s);
            foreach (var e in _hoveredEntities)
                e.DrawHighlight(_s);

            _selection.Draw(_s);
            _s.End();

            base.Draw(gameTime);
        }

        GraphicsDeviceManager _graphics;
        SpriteBatch _s;

        Selection _selection;
        Quadtree<Entity> _quadtree;

        HashSet<Entity> _hoveredEntities = new HashSet<Entity>();
    }
}
