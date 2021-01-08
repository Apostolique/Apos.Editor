using System;
using Apos.Input;
using Dcrew.Camera;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;

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

            _camera = new Camera(new Vector2(0f, 0f), 0f, new Vector2(1f));

            InputHelper.Setup(this);
        }

        protected override void Update(GameTime gameTime) {
            InputHelper.UpdateSetup();

            if (_quit.Pressed())
                Exit();

            UpdateCameraInput();

            InputHelper.UpdateCleanup();
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(Color.Black);

            _s.Begin(transformMatrix: _camera.View());
            _s.FillRectangle(new RectangleF(-50, -50, 100, 100), Color.White);
            _s.End();

            base.Draw(gameTime);
        }

        private void UpdateCameraInput() {
            int scrollDelta = InputHelper.NewMouse.ScrollWheelValue - InputHelper.OldMouse.ScrollWheelValue;
            if (scrollDelta != 0) {
                Zoom = MathF.Max(Zoom - scrollDelta * 0.0005f, 0.1f);
            }

            if (Triggers.RotateLeft.Pressed()) {
                _camera.Angle += MathHelper.PiOver4;
            }
            if (Triggers.RotateRight.Pressed()) {
                _camera.Angle -= MathHelper.PiOver4;
            }

            _mouseWorld = _camera.ScreenToWorld(InputHelper.NewMouse.X, InputHelper.NewMouse.Y);

            if (Triggers.CameraDrag.Pressed()) {
                _dragAnchor = _mouseWorld;
                _isDragging = true;
            }
            if (_isDragging && Triggers.CameraDrag.HeldOnly()) {
                _camera.XY += _dragAnchor - _mouseWorld;
                _mouseWorld = _dragAnchor;
            }
            if (_isDragging && Triggers.CameraDrag.Released()) {
                _isDragging = false;
            }
        }

        GraphicsDeviceManager _graphics;
        SpriteBatch _s;

        ICondition _quit =
            new AnyCondition(
                new KeyboardCondition(Keys.Escape),
                new GamePadCondition(GamePadButton.Back, 0)
            );

        Camera _camera;
        float Zoom {
            get => MathF.Sqrt(_camera.ZFromScale(_camera.Scale.X, 0f));
            set {
                _camera.Scale = new Vector2(_camera.ScaleFromZ(value * value, 0f));
            }
        }
        Vector2 _mouseWorld = Vector2.Zero;
        Vector2 _dragAnchor = Vector2.Zero;
        bool _isDragging = false;
    }
}
