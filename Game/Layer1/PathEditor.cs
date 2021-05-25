using System;
using System.Collections.Generic;
using System.Linq;
using Apos.Spatial;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameProject {
    public class PathEditor {
        public PathEditor(AABBTree<Entity> paths) {
            _paths = paths;
        }

        public void UpdateInput() {
            if (Triggers.ClearPaths.Pressed()) {
                _vertical.Clear();
                _horizontal.Clear();
            }
            // if (Triggers.ToggleNegativePath.Pressed()) {
            //     foreach (var e in _selectedEntities) {
            //         e.IsNegative = !e.IsNegative;
            //     }
            // }
            if (Triggers.ToggleVertical.Pressed()) {
                _showVertical = !_showVertical;
            }
            if (Triggers.ToggleHorizontal.Pressed()) {
                _showHorizontal = !_showHorizontal;
            }
        }
        public void Draw(SpriteBatch s) {
            if (_showHorizontal)
                foreach (var v in _horizontal.Query(Camera.ViewRect))
                    v.DrawHorizontal(s, !_showVertical);
            if (_showVertical)
                foreach (var v in _vertical.Query(Camera.ViewRect))
                    v.DrawVertical(s, !_showHorizontal);
        }

        public void RemoveFromPath(Rectangle r) {
            AddAposPath(r, true, isVertical: true);
            AddAposPath(r, true, isVertical: false);

            foreach (var e in _paths.Query(r).OrderBy(e => e.IsNegative ? 1 : 0)) {
                Rectangle er = (Rectangle)e.Inset;
                Rectangle result = Intersection(ref r, ref er);
                AddAposPath(result, e.IsNegative, isVertical: true);
                AddAposPath(result, e.IsNegative, isVertical: false);
            }
        }
        public void AddToPath(Rectangle r, bool isNegative) {
            AddAposPath(r, isNegative, isVertical: true);
            AddAposPath(r, isNegative, isVertical: false);

            if (!isNegative) {
                foreach (var e in _paths.Query(r).Where(e => e.IsNegative)) {
                    Rectangle er = (Rectangle)e.Inset;
                    Rectangle result = Intersection(ref r, ref er);
                    AddAposPath(result, true, isVertical: true);
                    AddAposPath(result, true, isVertical: false);
                }
            }
        }
        public void UpdatePath(Rectangle or, Rectangle nr, bool isNegative) {
            RemoveFromPath(or);
            AddToPath(nr, isNegative);
        }

        private void AddAposPath(Rectangle r, bool isNegative, bool isVertical) {
            if (r.Width == 0 || r.Height == 0) {
                // No need to add this rectangle.
                return;
            }

            // TODO: Use more appropriate data structures.
            var container = isVertical ? _vertical : _horizontal;
            var overlaps = container.Query(r).ToList();

            List<AposPath> pending = new List<AposPath>();
            SortedDictionary<int, List<Range>> slabs = new SortedDictionary<int, List<Range>>();

            int nextId = 0;

            slabs.Add(CreateRangeKey(r, true, isVertical), new List<Range>() { CreateRange(nextId, r, true, isNegative, isVertical) });
            slabs.Add(CreateRangeKey(r, false, isVertical), new List<Range>() { CreateRange(nextId++, r, false, isNegative, isVertical) });

            foreach (var o in overlaps) {
                Rectangle rect = o.Rect;
                int keyA = CreateRangeKey(rect, true, isVertical);
                int keyB = CreateRangeKey(rect, false, isVertical);
                Range rangeA = CreateRange(nextId, rect, true, false, isVertical);
                Range rangeB = CreateRange(nextId++, rect, false, false, isVertical);
                if (slabs.TryGetValue(keyA, out List<Range>? l1)) {
                    l1.Add(rangeA);
                } else {
                    slabs.Add(keyA, new List<Range>() { rangeA});
                }

                if (slabs.TryGetValue(keyB, out List<Range>? l2)) {
                    l2.Add(rangeB);
                } else {
                    slabs.Add(keyB, new List<Range>() { rangeB });
                }

                o.Leaf = container.Remove(o.Leaf);
            }

            // Generate overlaps. Preserve active overlaps while going through the slabs.
            List<Range> existingRanges = new List<Range>();
            List<Range.Overlap> oldOverlaps;
            List<Range.Overlap> newOverlaps = new List<Range.Overlap>();
            foreach (var kvp in slabs) {
                oldOverlaps = newOverlaps;
                newOverlaps = new List<Range.Overlap>();

                // Update existing
                foreach (var s in kvp.Value) {
                    if (existingRanges.RemoveAll(e => e.Id == s.Id) == 0) {
                        existingRanges.Add(s);
                    }
                }

                existingRanges.Sort((a, b) => {
                    if (a.IsNegative == b.IsNegative) {
                        if (a.IsNegative) {
                            return a.O.B.CompareTo(b.O.B);
                        } else {
                            return a.O.A.CompareTo(b.O.A);
                        }
                    } else {
                        if (a.IsNegative) {
                            return a.O.B.CompareTo(b.O.A) <= 0 ? -1 : 1;
                        } else {
                            return a.O.A.CompareTo(b.O.B) >= 0 ? 1 : -1;
                        }
                    }
                });

                // Merge existing into new overlaps
                for (int i = 0; i < existingRanges.Count; i++) {
                    var range = existingRanges[i];
                    if (range.IsNegative) {
                        int count = newOverlaps.Count;
                        int limit = count;
                        for (int j = count - 1; j >= 0; j--) {
                            var positiveOverlap = newOverlaps[j];
                            if (!range.O.CheapNegativeOverlap(positiveOverlap)) {
                                break;
                            }

                            limit = j;
                        }
                        for (int j = limit; j < count; j++) {
                            var positiveOverlap = newOverlaps[j];

                            var o = positiveOverlap.Split(range.O, out bool isRemoved);
                            if (isRemoved) {
                                newOverlaps.RemoveAt(j);
                                --j;
                                --count;
                            } else {
                                if (o != null) {
                                    newOverlaps.Add(o);
                                }
                            }
                        }
                    } else {
                        if (newOverlaps.Count == 0 || !newOverlaps.Last().CheapOverlap(range.O)) {
                            newOverlaps.Add(range.O.Copy(kvp.Key));
                        } else {
                            newOverlaps.Last().Merge(range.O);
                        }
                    }
                }

                // Figure out the difference and create rectangles.
                foreach (var oo in oldOverlaps) {
                    bool createRect = true;

                    foreach (var no in newOverlaps) {
                        if (oo.ExactMatch(no)) {
                            createRect = false;
                            no.C = oo.C; // Bring C forward
                        }
                    }

                    if (createRect) {
                        var p = new AposPath(OverlapToRect(oo.A, oo.B, oo.C, kvp.Key, isVertical));
                        p.Leaf = container.Add(p.Rect, p);
                    }
                }
            }
        }
        private int CreateRangeKey(Rectangle r, bool isStart, bool isVertical) {
            if (isVertical) {
                if (isStart) {
                    return r.Left;
                } else {
                    return r.Right;
                }
            } else {
                if (isStart) {
                    return r.Top;
                } else {
                    return r.Bottom;
                }
            }
        }
        private Range CreateRange(int id, Rectangle r, bool isStart, bool isNegative, bool isVertical) {
            if (isVertical) {
                if (isStart) {
                    return new Range(r.Top, r.Bottom, r.Left, id, isNegative);
                } else {
                    return new Range(r.Top, r.Bottom, r.Right, id, isNegative);
                }
            } else {
                if (isStart) {
                    return new Range(r.Left, r.Right, r.Top, id, isNegative);
                } else {
                    return new Range(r.Left, r.Right, r.Bottom, id, isNegative);
                }
            }
        }
        private Rectangle OverlapToRect(int a, int b, int c, int key, bool isVertical) {
            if (isVertical) {
                return new Rectangle(c, a, key - c, b - a);
            } else {
                return new Rectangle(a, c, b - a, key - c);
            }
        }

        private Rectangle Intersection(ref Rectangle r1, ref Rectangle r2) {
            Rectangle result;

            var left = Math.Max(r1.Left, r2.Left);
            var top = Math.Max(r1.Top, r2.Top);
            var right = Math.Min(r1.Right, r2.Right);
            var bottom = Math.Min(r1.Bottom, r2.Bottom);

            if (right < left || bottom < top) {
                result = new Rectangle();
            } else {
                result = new Rectangle(left, top, right - left, bottom - top);
            }

            return result;
        }

        bool _showVertical = true;
        bool _showHorizontal = true;

        AABBTree<Entity> _paths;
        AABBTree<AposPath> _vertical = new AABBTree<AposPath>();
        AABBTree<AposPath> _horizontal = new AABBTree<AposPath>();
    }
}
