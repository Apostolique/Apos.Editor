using System.Collections.Generic;
using Apos.Spatial;
using Microsoft.Xna.Framework;

namespace GameProject {
    public class World {
        public World() {
            AABBTree = new AABBTree<Entity>();
        }

        public AABBTree<Entity> AABBTree = null!;
        public Dictionary<uint, Entity> Entities = new Dictionary<uint, Entity>();
    }
}
