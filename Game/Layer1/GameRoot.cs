using System;
using System.Collections.Generic;
using System.Linq;
using Apos.Input;
using Dcrew.Spatial;
using FontStashSharp;
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

            _quadtree = new Quadtree<Entity>();
            Camera.Setup();
            _selection = new RectEdit();
            _edit = new RectEdit();

            Assets.LoadFonts(Content, GraphicsDevice);
            Assets.Setup(Content);

            InputHelper.Setup(this);
        }

        protected override void Update(GameTime gameTime) {
            InputHelper.UpdateSetup();

            if (Triggers.Quit.Pressed())
                Exit();

            Camera.UpdateInput();
            _edit.UpdateInput(Camera.MouseWorld, false);
            var isSelectionDone = _selection.UpdateInput(Camera.MouseWorld);

            if (_selection.Rect != null) {
                var r = _selection.Rect.Value;
                _hoveredEntities = _quadtree.Query(new RotRect(r.X, r.Y, r.Width, r.Height)).ToHashSet();
            } else {
                _hoveredEntities = _quadtree.Query(Camera.MouseWorld.ToPoint()).ToHashSet();
            }

            if (Triggers.CreateEntity.Pressed()) {
                var newEntity = new Entity(new RectangleF(Camera.MouseWorld, new Vector2(100, 100)));
                _quadtree.Add(newEntity);
                isSelectionDone = true;
                _hoveredEntities.Clear();
                _hoveredEntities.Add(newEntity);
                _edit.Rect = new RectangleF(newEntity.Bounds.XY, newEntity.Bounds.Size);
            }

            if (isSelectionDone) {
                if (_hoveredEntities.Count >= 1) {
                    // TODO: Handle SHIFT or CTRL modifiers.

                    _selectedEntities.Clear();

                    _selectedEntities.AddRange(_hoveredEntities.Select(e => {
                        return new EntityOffset(e, new Vector2(0, 0));
                    }));

                    var first = _selectedEntities[0];
                    var pos1 = first.Entity.Bounds.XY;

                    float x1 = first.Entity.Bounds.X;
                    float x2 = first.Entity.Bounds.X + first.Entity.Bounds.Width;
                    float y1 = first.Entity.Bounds.Y;
                    float y2 = first.Entity.Bounds.Y + first.Entity.Bounds.Height;

                    for (int i = 1; i < _selectedEntities.Count; i++) {
                        var current = _selectedEntities[i];
                        x1 = MathF.Min(current.Entity.Bounds.X, x1);
                        x2 = MathF.Max(current.Entity.Bounds.X + current.Entity.Bounds.Width, x2);
                        y1 = MathF.Min(current.Entity.Bounds.Y, y1);
                        y2 = MathF.Max(current.Entity.Bounds.Y + current.Entity.Bounds.Height, y2);

                        var pos2 = current.Entity.Bounds.XY;

                        current.Offset = pos2 - pos1;
                    }

                    _edit.IsResizable = _selectedEntities.Count == 1;
                    _edit.Rect = new RectangleF(x1, y1, x2 - x1, y2 - y1);
                    first.Offset = pos1 - new Vector2(x1, y1);
                } else {
                    _selectedEntities.Clear();
                    _edit.Rect = null;
                }
                _selection.Rect = null;
            }

            if (_edit.Rect != null) {
                var first = _selectedEntities[0];
                var bound = first.Entity.Bounds;
                bound.XY = first.Offset + _edit.Rect.Value.Position;
                first.Entity.Bounds = bound;
                _quadtree.Update(first.Entity);

                for (int i = 1; i < _selectedEntities.Count; i++) {
                    var current = _selectedEntities[i];
                    bound = current.Entity.Bounds;
                    bound.XY = current.Offset + first.Entity.Bounds.XY;
                    current.Entity.Bounds = bound;
                    _quadtree.Update(current.Entity);
                }

                if (_selectedEntities.Count == 1) {
                    bound.Size = _edit.Rect.Value.Size;
                    first.Entity.Bounds = bound;
                }
            }

            InputHelper.UpdateCleanup();
            _quadtree.Update();
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(new Color(22, 22, 22));

            _s.Begin(transformMatrix: Camera.View);
            foreach (var e in _quadtree.Query(Camera.WorldBounds, Camera.Angle, Camera.Origin))
                e.Draw(_s);
            foreach (var e in _hoveredEntities)
                e.DrawHighlight(_s, Color.Black);
            foreach (var eo in _selectedEntities)
                eo.Entity.DrawHighlight(_s, Color.White);

            _selection.Draw(_s);
            _edit.Draw(_s);
            _s.End();

            var font = Assets.FontSystem.GetFont(30);
            _s.Begin();
            // Draw UI
            _s.End();

            base.Draw(gameTime);
        }

        GraphicsDeviceManager _graphics;
        SpriteBatch _s;

        RectEdit _selection;
        RectEdit _edit;
        Quadtree<Entity> _quadtree;

        HashSet<Entity> _hoveredEntities = new HashSet<Entity>();
        // List<Entity> _selectedEntities = new List<Entity>();
        List<EntityOffset> _selectedEntities = new List<EntityOffset>();

        class EntityOffset {
            public EntityOffset(Entity entity, Vector2 offset) {
                Entity = entity;
                Offset = offset;
            }

            public Entity Entity {
                get;
                set;
            }
            public Vector2 Offset {
                get;
                set;
            }
        }
    }
}
