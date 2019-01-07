using System;
using System.Collections.Generic;
using System.Linq;

namespace KS.Foundation.ECS
{    
    class SystemWrapper {
        public BaseSystem System { get; private set; }
        readonly HashSet<Entity> entities;
        
		public SystemWrapper(BaseSystem system) {
            System = system;
			entities = new HashSet<Entity>();
        }

        public void Update(double elapsedMs) {
            System.BeforeUpdate(elapsedMs);
            System.Update(entities, elapsedMs);
            System.AfterUpdate(elapsedMs);
        }

        public void AddEntity(Entity entity) {
			// ToDo: Double check what's required of those
			if (entities.Contains(entity) || !entity.BelongsTo(System))
                return;
            entities.Add(entity);
            System.EntityAdded(entity);
        }

        public void RemoveEntity(Entity entity) {            
			if (entities.Remove(entity))
            	System.EntityRemoved(entity);
        }

		public void RemovingEntity(Entity entity) {            
			if (entities.Contains(entity))
				System.RemovingEntity (entity);
		}

        public void UpdateEntityValidity(Entity entity) {
            if (entities.Contains(entity)) {
				if (!entity.BelongsTo(System)) {
                    RemoveEntity(entity);
                }
            }
			else if (entity.BelongsTo(System)) {
                AddEntity(entity);
            }
        }

        public bool IsDraw() {
            return System.IsDraw();
        }

		public bool ContainsEntity(Entity entity)
		{
			return entities.Contains (entity);
		}

        public int EntityCount {
            get {
                return entities.Count();
            }
        }

		public void ClearData()
		{
			System.Clear ();
		}
    }
}
