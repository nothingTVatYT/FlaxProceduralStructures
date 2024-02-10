using System;
using System.Collections.Generic;
using FlaxEngine;

namespace Game.ProceduralStructures {
    [Serializable]
    public class FrameDefinition {
        [Serializable]
        public class Edge {
            public readonly int A;
            public readonly int B;
            public Edge(int a, int b) {
                A = a;
                B = b;
            }
            public override int GetHashCode() {
                if (A < B) return A + 16383*B;
                return B + 16383*A;
            }
            public override bool Equals(object obj) {
                if (obj is Edge other) {
                    return (A == other.A && B == other.B) || (B == other.A && A == other.B);
                }
                return false;
            }
            public override string ToString()
            {
                return string.Format("E[" + A + "," + B + "]");
            }
        }
        public List<Vector3> Points;
        public List<Edge> Edges;
    }
}