using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace GameProject {
    public class AposPath {
        public AposPath(Rectangle rect) {
            Rect = rect;

            _bt = 16f;
            _t = 2f;
            _rt = _bt - _t * 2f;
        }

        public Rectangle Rect { get; set; }
        public int Leaf { get; set; } = -1;

        public void DrawVertical(SpriteBatch s, bool isFull) {
            var thickness = _t * Camera.ScreenToWorldScale;
            var border = Rect;

            if (border.Width <= 0f || border.Height <= 0f) {
                return;
            }

            var rect = Utility.ExpandRect(border, -thickness);

            var borderThickness = _bt * Camera.ScreenToWorldScale;
            var rectThickness = _rt * Camera.ScreenToWorldScale;

            if (rectThickness * 2f >= rect.Width || rectThickness * 2f >= rect.Height) {
                s.FillRectangle(border, Color.Black);
                s.FillRectangle(rect , new Color(220, 221, 206));
            } else if (isFull) {
                s.DrawRectangle(border, Color.Black, borderThickness);
                s.DrawRectangle(rect, new Color(220, 221, 206), rectThickness);
            } else {
                s.DrawParallelVertical(border, Color.Black, borderThickness);
                s.DrawParallelVertical(rect, new Color(220, 221, 206), rectThickness);
            }
        }
        public void DrawHorizontal(SpriteBatch s, bool isFull) {
            var thickness = _t * Camera.ScreenToWorldScale;
            var border = Rect;

            if (border.Width <= 0f || border.Height <= 0f) {
                return;
            }

            var rect = Utility.ExpandRect(border, -thickness);

            var borderThickness = _bt * Camera.ScreenToWorldScale;
            var rectThickness = _rt * Camera.ScreenToWorldScale;

            if (rectThickness * 2f >= rect.Width || rectThickness * 2f >= rect.Height) {
                s.FillRectangle(border, Color.Black);
                s.FillRectangle(rect , new Color(162, 180, 164));
            } else if (isFull) {
                s.DrawRectangle(border, Color.Black, borderThickness);
                s.DrawRectangle(rect, new Color(162, 180, 164), rectThickness);
            } else {
                s.DrawParallelHorizontal(border, Color.Black, borderThickness);
                s.DrawParallelHorizontal(rect, new Color(162, 180, 164), rectThickness);
            }
        }

        private float _bt;
        private float _rt;
        private float _t;
    }
}
