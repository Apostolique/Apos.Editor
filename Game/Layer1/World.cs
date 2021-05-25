using System.Collections.Generic;
using Apos.Spatial;

namespace GameProject {
    public class World {
        public World() { }

        public AABBTree<Entity> AABBTree = new AABBTree<Entity>();
        public Dictionary<uint, Entity> Entities = new Dictionary<uint, Entity>();
    }
}
