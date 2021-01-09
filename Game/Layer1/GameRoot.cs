using System;
using Apos.Input;
using Dcrew.Camera;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

            if (Triggers.Quit.Pressed())
                Exit();

            UpdateCameraInput();
            UpdateSelectionInput();

            InputHelper.UpdateCleanup();
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(Color.Black);

            _s.Begin(transformMatrix: _camera.View());
            if (_selection != null) {
                _s.FillRectangle(Utility.ExpandRect(_selection.Value, _handleDistanceWorld), Color.White * 0.1f);
                _s.DrawRectangle(Utility.ExpandRect(_selection.Value, _handleDistanceWorld), Color.White * 0.5f, 1f / _camera.ScreenToWorldScale());
                _s.FillRectangle(_selection.Value, Color.White * 0.2f);
                _s.DrawRectangle(_selection.Value, Color.White * 0.5f, 1f / _camera.ScreenToWorldScale());
            }
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

        public void UpdateSelectionInput(bool canCreate = true) {
            // TODO: Might be nice to do a minimum selection size threshold for it's area. (width * height > 900)

            // Use the current selection or create a new one.
            RectangleF r = _selection ?? new RectangleF(_mouseWorld.X, _mouseWorld.Y, 0, 0);
            bool shouldCreate = false;

            float x1 = r.Left;
            float x2 = r.Right;
            float y1 = r.Top;
            float y2 = r.Bottom;

            // Drag start
            if (Triggers.SelectionDrag.Pressed()) {
                if (_selection == null) {
                    shouldCreate = canCreate;
                }

                float dx = 0;
                float dy = 0;

                // Move selection
                if (r.Contains(_mouseWorld)) {
                    dx = x1 - _mouseWorld.X;
                    dy = y1 - _mouseWorld.Y;
                    _dragHandle = DragHandle.Center;
                } else {
                    // Resize selection
                    if (y1 - _handleDistanceWorld <= _mouseWorld.Y && _mouseWorld.Y <= y2 + _handleDistanceWorld) {
                        float diff1 = x1 - _mouseWorld.X;
                        float diff2 = _mouseWorld.X - x2;

                        if (0 <= diff1 && diff1 <= _handleDistanceWorld) {
                            _dragHandle |= DragHandle.Left;
                            dx = x1 - _mouseWorld.X;
                        } else if (0 <= diff2 && diff2 <= _handleDistanceWorld) {
                            _dragHandle |= DragHandle.Right;
                            dx = x2 - _mouseWorld.X;
                        }
                    }
                    // Resize selection
                    if (x1 - _handleDistanceWorld <= _mouseWorld.X && _mouseWorld.X <= x2 + _handleDistanceWorld) {
                        float diff1 = y1 - _mouseWorld.Y;
                        float diff2 = _mouseWorld.Y - y2;

                        if (0 <= diff1 && diff1 <= _handleDistanceWorld) {
                            _dragHandle |= DragHandle.Top;
                            dy = y1 - _mouseWorld.Y;
                        } else if (0 <= diff2 && diff2 <= _handleDistanceWorld) {
                            _dragHandle |= DragHandle.Bottom;
                            dy = y2 - _mouseWorld.Y;
                        }
                    }

                    // We're outside the selection, create a new one.
                    if (canCreate && _dragHandle == DragHandle.None) {
                        r = new RectangleF(_mouseWorld.X, _mouseWorld.Y, 0, 0);
                        x1 = r.Left;
                        x2 = r.Right;
                        y1 = r.Top;
                        y2 = r.Bottom;

                        _dragHandle = DragHandle.Left | DragHandle.Top;
                        dx = x1 - _mouseWorld.X;
                        dy = y1 - _mouseWorld.Y;
                    }
                }

                if (_dragHandle != DragHandle.None) {
                    _dragDiff = new Vector2(dx, dy);
                }
            }
            // Drag ongoing
            if (_dragHandle != DragHandle.None && Triggers.SelectionDrag.HeldOnly()) {
                // Move selection
                if (_dragHandle.HasFlag(DragHandle.Center)) {
                    r.X = _mouseWorld.X + _dragDiff.X;
                    r.Y = _mouseWorld.Y + _dragDiff.Y;
                } else {
                    // Resize selection
                    float width = r.Width;
                    float height = r.Height;

                    // Do a first pass in case we're dragging a side and crossing over causing negative width or height.
                    if (_dragHandle.HasFlag(DragHandle.Left)) {
                        float left = _mouseWorld.X + _dragDiff.X;
                        width = x2 - r.X;
                    } else if (_dragHandle.HasFlag(DragHandle.Right)) {
                        width = _mouseWorld.X + _dragDiff.X - x1;
                    }
                    if (_dragHandle.HasFlag(DragHandle.Top)) {
                        float right = _mouseWorld.Y + _dragDiff.Y;
                        height = y2 - r.Y;
                    } else if (_dragHandle.HasFlag(DragHandle.Bottom)) {
                        height = _mouseWorld.Y + _dragDiff.Y - y1;
                    }

                    // If we cross, preserve the static side.
                    if (width < 0) {
                        _dragHandle ^= DragHandle.Left | DragHandle.Right;
                        float temp = x1;
                        x1 = x2;
                        x2 = temp;
                    }
                    if (height < 0) {
                        _dragHandle ^= DragHandle.Top | DragHandle.Bottom;
                        float temp = y1;
                        y1 = y2;
                        y2 = temp;
                    }

                    // Compute the final rectangle.
                    if (_dragHandle.HasFlag(DragHandle.Left)) {
                        r.X = _mouseWorld.X + _dragDiff.X;
                        r.Width = x2 - r.X;
                    } else if (_dragHandle.HasFlag(DragHandle.Right)) {
                        r.X = x1;
                        r.Width = _mouseWorld.X + _dragDiff.X - x1;
                    }
                    if (_dragHandle.HasFlag(DragHandle.Top)) {
                        r.Y = _mouseWorld.Y + _dragDiff.Y;
                        r.Height = y2 - r.Y;
                    } else if (_dragHandle.HasFlag(DragHandle.Bottom)) {
                        r.Y = y1;
                        r.Height = _mouseWorld.Y + _dragDiff.Y - y1;
                    }
                }
            }
            // Drag end
            if (Triggers.SelectionDrag.Released()) {
                _dragHandle = DragHandle.None;
            }

            if (_selection != null || shouldCreate) {
                _selection = r;
            }
        }

        GraphicsDeviceManager _graphics;
        SpriteBatch _s;

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

        [Flags]
        enum DragHandle {
            None = 0,
            Left = 1,
            Top = 2,
            Right = 4,
            Bottom = 8,
            Center = 16,
        }
        RectangleF? _selection = null;
        DragHandle _dragHandle = DragHandle.None;
        float _handleDistance = 50f;
        float _handleDistanceWorld {
            get => _handleDistance * Vector2.Distance(_camera.ScreenToWorld(0, 0, 0), _camera.ScreenToWorld(1, 0, 0));
        }
        Vector2 _dragDiff = new Vector2();
    }
}
