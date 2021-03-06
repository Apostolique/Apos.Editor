﻿using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Apos.Gui;
using FontStashSharp;
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
            Window.ClientSizeChanged += WindowSizeChanged;

            base.Initialize();
        }

        protected override void LoadContent() {
            _s = new SpriteBatch(GraphicsDevice);

            // TODO: Preserve settings.
            Settings settings = Utility.LoadJson<Settings>("Settings.json");

            _graphics.PreferredBackBufferWidth = settings.Width;
            _graphics.PreferredBackBufferHeight = settings.Height;
            IsFixedTimeStep = settings.IsFixedTimeStep;
            _graphics.SynchronizeWithVerticalRetrace = settings.IsVSync;
            _graphics.ApplyChanges();

            Camera.Setup();

            Assets.LoadFonts(Content, GraphicsDevice);
            Assets.Setup(Content);

            GuiHelper.Setup(this, Assets.FontSystem);

            _world = new World();
            _editor = new Editor(_world, GraphicsDevice);
        }

        private void WindowSizeChanged(object? sender, EventArgs e) {
            int width = GraphicsDevice.Viewport.Width;
            int height = GraphicsDevice.Viewport.Height;

            _editor.WindowResize(width, height);
        }

        protected override void Update(GameTime gameTime) {
            GuiHelper.UpdateSetup();

            if (Triggers.ResetDroppedFrames.Pressed()) _fps.DroppedFrames = 0;
            _fps.Update(gameTime);

            _editor.Update(gameTime);

            GuiHelper.UpdateCleanup();
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime) {
            _fps.Draw(gameTime);

            GraphicsDevice.Clear(new Color(0, 0, 0));

            _editor.DrawBackground(_s);

            _s.Begin(transformMatrix: Camera.View, samplerState: SamplerState.PointClamp);
            foreach (var e in _world.Quadtree.Query(Camera.WorldBounds, Camera.Angle, Camera.Origin).OrderBy(e => e))
                e.Draw(_s);
            _editor.Draw(_s);
            _s.End();

            var font = Assets.FontSystem.GetFont(30);
            _s.Begin();
            // Draw UI
            _editor.DrawUI(_s, gameTime);
            _s.DrawString(font, $"fps: {_fps.FramesPerSecond} - Dropped Frames: {_fps.DroppedFrames} - Draw ms: {_fps.TimePerFrame} - Update ms: {_fps.TimePerUpdate}", new Vector2(10, 10), Color.White);
            _s.End();

            base.Draw(gameTime);
        }

        GraphicsDeviceManager _graphics = null!;
        SpriteBatch _s = null!;

        FPSCounter _fps = new FPSCounter();

        World _world;
        Editor _editor;
    }
}
