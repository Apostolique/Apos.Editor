using Apos.Gui;
using Apos.Input;
using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace GameProject.UI {
    public class Sidebar : Panel {
        public Sidebar(string name) : base(name) { }

        public override void UpdatePrefSize(GameTime gameTime) {
            base.UpdatePrefSize(gameTime);

            PrefWidth = 200;
            PrefHeight = InputHelper.WindowHeight / GuiHelper.Scale;
        }

        public override void UpdateSetup(GameTime gameTime) {
            // Sidebar is a root component so it can set it's own position.
            X = (InputHelper.WindowWidth - 200) / GuiHelper.Scale;
            Y = 0;

            float currentY = 0;
            foreach (var c in _children) {
                c.X = X + OffsetX;
                c.Y = currentY + Y + OffsetY;
                c.Width = MathHelper.Min(c.PrefWidth, Width);
                c.Height = c.PrefHeight;

                c.Clip = c.Bounds.Intersection(Clip);

                c.UpdateSetup(gameTime);

                currentY += c.Height;
            }

            FullWidth = Width;
            FullHeight = MathHelper.Max(currentY, Height);
        }
        public override void UpdateInput(GameTime gameTime) {
            base.UpdateInput(gameTime);

            if (Clip.Contains(GuiHelper.Mouse) && Default.MouseInteraction.Pressed()) {
                _pressed = true;
            }
            if (IsFocused) {
                if (_pressed) {
                    if (Default.MouseInteraction.Released()) {
                        _pressed = false;
                    } else {
                        Default.MouseInteraction.Consume();
                    }
                }
            }
        }
        public override void Draw(GameTime gameTime) {
            GuiHelper.SetScissor(Clip);
            GuiHelper.SpriteBatch.FillRectangle(Bounds, Color.Black * 0.5f);
            GuiHelper.SpriteBatch.DrawRectangle(Bounds, Color.Black, 2f);
            GuiHelper.ResetScissor();
            foreach (var c in _children) {
                if (Bounds.Intersects(c.Bounds)) {
                    c.Draw(gameTime);
                }
            }
        }
        public static new Sidebar Put(int id = 0) {
            // 1. Check if panel with id already exists.
            //      a. If already exists. Get it.
            //      b  If not, create it.
            // 3. Push it on the stack.
            // 4. Ping it.
            IParent? parent = GuiHelper.CurrentIMGUI.CurrentParent;
            var fullName = GuiHelper.GenerateName(parent, "sidebar", id);

            GuiHelper.CurrentIMGUI.TryGetValue(fullName, out IComponent c);

            Sidebar a;
            if (c is Sidebar) {
                a = (Sidebar)c;
            } else {
                a = new Sidebar(fullName);
                GuiHelper.CurrentIMGUI.Add(fullName, a);
            }

            if (a.LastPing != InputHelper.CurrentFrame) {
                a.Reset();
                a.LastPing = InputHelper.CurrentFrame;
                if (parent != null) {
                    a.Index = parent.NextIndex();
                }
            }

            GuiHelper.CurrentIMGUI.Push(a);

            return a;
        }

        protected bool _pressed = false;
    }
}
