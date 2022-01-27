using System;
using Apos.Input;
using A = Apos.Camera;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using Apos.Tweens;

namespace GameProject {
    public static class Camera {
        public static void Setup(GraphicsDevice graphicsDevice, GameWindow window) {
            _camera = new A.Camera(new A.DefaultViewport(graphicsDevice, window));
        }

        public static Vector2 MouseWorld = Vector2.Zero;
        public static Matrix View => _camera.View;
        public static Matrix ViewInvert => _camera.ViewInvert;
        public static RectangleF ViewRect => _camera.ViewRect;
        public static float Rotation => _camera.Rotation;
        public static Vector2 Origin => _camera.VirtualViewport.Origin;
        public static float ScreenToWorldScale => _camera.ScreenToWorldScale();

        public static Matrix GetView(float z) => _camera.GetView(z);

        public static float FocalLength {
            get => _camera.FocalLength;
            set {
                _camera.FocalLength = value;
            }
        }
        public static float Z {
            get => _camera.Z;
            set {
                _camera.Z = value;
            }
        }

        public static void UpdateInput() {
            if (MouseCondition.Scrolled() && !Triggers.SelectionCycle.Held(false)) {
                var a = _exp.Value;
                var b = MathHelper.Clamp(_exp.B - MouseCondition.ScrollDelta * _expDistance, _maxExp, _minExp);
                _exp = new FloatTween(a, b, GetDuration(a, b, _speed, _maxDuration), Easing.ExpoOut);
            }

            if (Triggers.RotateLeft.Pressed()) {
                var a = _rotation.Value;
                var b = _rotation.B + MathHelper.PiOver4;
                _rotation = new FloatTween(a, b, GetDuration(a, b, _speed, _maxDuration), Easing.ExpoOut);
            }
            if (Triggers.RotateRight.Pressed()) {
                var a = _rotation.Value;
                var b = _rotation.B - MathHelper.PiOver4;
                _rotation = new FloatTween(a, b, GetDuration(a, b, _speed, _maxDuration), Easing.ExpoOut);
            }

            MouseWorld = _camera.ScreenToWorld(InputHelper.NewMouse.X, InputHelper.NewMouse.Y);

            if (Triggers.CameraDrag.Pressed()) {
                _dragAnchor = MouseWorld;
                _isDragging = true;
            }
            if (_isDragging && Triggers.CameraDrag.HeldOnly()) {
                _camera.XY += _dragAnchor - MouseWorld;
                MouseWorld = _dragAnchor;
            }
            if (_isDragging && Triggers.CameraDrag.Released()) {
                _isDragging = false;
            }
        }

        public static void Update() {
            _camera.Z = _camera.ScaleToZ(ExpToScale(_exp.Value), 0f);
            _camera.Rotation = _rotation.Value;
        }

        private static float ScaleToExp(float scale) {
            return -MathF.Log(scale);
        }
        private static float ExpToScale(float exp) {
            return MathF.Exp(-exp);
        }

        private static long GetDuration(float a, float b, float speed, long maxDuration) {
            return (long)MathF.Min(MathF.Abs((b - a) / speed), maxDuration);
        }

        private static A.Camera _camera = null!;
        private static Vector2 _dragAnchor = Vector2.Zero;
        private static bool _isDragging = false;

        private static ITween<float> _exp = new WaitTween<float>(0, 0);
        private static ITween<float> _rotation = new WaitTween<float>(0, 0);

        private static float _speed = 0.0007f;
        private static long _maxDuration = 3000;

        private static float _expDistance = 0.002f;
        private static float _maxExp = -2f;
        private static float _minExp = 4f;
    }
}
