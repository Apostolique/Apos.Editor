using System.Collections.Generic;
using Dcrew.Spatial;

namespace GameProject {
    public class World {
        public World() {
            Quadtree = new Quadtree<Entity>();
        }

        public Quadtree<Entity> Quadtree = null!;
        public Dictionary<uint, Entity> Entities = new Dictionary<uint, Entity>();
    }
}
