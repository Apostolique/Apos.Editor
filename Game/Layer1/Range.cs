using System;

namespace GameProject {
    public class Range {
        public Range(int a, int b, int c, int id, bool isNegative) {
            O = new Range.Overlap(a, b, c);
            Id = id;
            IsNegative = isNegative;
        }

        public Overlap O { get; set; }
        public int Id { get; set; }
        public bool IsNegative { get; set; }

        public class Overlap {
            public Overlap(int a, int b, int c) {
                A = a;
                B = b;
                C = c;
            }

            public int A { get; set; }
            public int B { get; set; }
            public int C { get; set; }

            public bool CheapOverlap(Overlap o) {
                return B >= o.A;
            }
            public bool CheapNegativeOverlap(Overlap o) {
                return B > o.A;
            }
            public bool Overlaps(Overlap o) {
                return
                    A <= o.A && B >= o.A || A <= o.B && B >= o.B ||
                    o.A <= A && o.B >= A || o.A <= B && o.B >= B;
            }
            public bool ExactMatch(Overlap o) {
                return A == o.A && B == o.B;
            }
            public void Merge(Overlap o) {
                A = Math.Min(A, o.A);
                B = Math.Max(B, o.B);
            }
            public Overlap? Split(Overlap o, out bool isRemoved) {
                if (o.A <= A && o.B >= B) {
                    isRemoved = true;
                    return null;
                }

                isRemoved = false;
                if (A < o.A && B > o.B) {
                    var temp = B;
                    B = o.A;

                    return new Overlap(o.B, temp, C);
                } else if (A < o.A) {
                    B = o.A;
                } else if (B > o.B) {
                    A = o.B;
                }

                return null;
            }

            public Overlap Copy(int c) {
                return new Overlap(A, B, c);
            }
        }
    }
}
