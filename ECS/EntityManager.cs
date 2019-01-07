using System;
using System.Linq;
using System.Collections.Generic;
using Pfz.Collections;

namespace KS.Foundation.ECS
{
    /// <summary>
    /// Provides a collection for the entities.
    /// </summary>
    class EntityManager {        
        public EntityFinder EntityFinder { get; private set; }
		readonly ThreadSafeDictionary<int, Entity> entities;

		World World { get; set; }

		public EntityManager(World world) {
			World = world;
            //entities = new HashSet<Entity>();
			entities = new ThreadSafeDictionary<int, Entity>();
            EntityFinder = new EntityFinder(entities);
        }

		~EntityManager()
		{
			World = null;
		}

        public void Add(Entity entity) {
			if (entity == null)
				return;
			if (entities.TryAdd (entity.ID, entity)) {
				World.Systems.OnEntityAdded (entity);
			} else {
				this.LogWarning ("EntityManager.Add(): Entity already exists {0}", entity.ID);
			}
        }
			        
        public bool Remove(Entity entity) {
			if (entity == null)
				return false;
			if (entities.Remove(entity.ID)) {            
				World.Systems.OnEntityRemoved (entity);
                return true;
            }
            return false;
        }

        public int EntityCount {
            get {
                return entities.Count;
            }
        }

        public int ComponentCount {
            get {
				return entities.Values.Sum (e => e.ComponentCount);                
            }
        }

		public void Clear()
		{
			entities.Clear ();
		}
    }
}
