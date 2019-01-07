using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pfz.Collections;

namespace KS.Foundation.ECS {
    /// <summary>
    /// Implementation of the IFinder interface.
    /// </summary>
    class EntityFinder : IFinder {
		ThreadSafeDictionary<int, Entity> entities;

		public EntityFinder(ThreadSafeDictionary<int, Entity> entities) {
            this.entities = entities;
        }
			
		public IEntity EntityByID(int id)
		{					
			Entity entity;
			if (entities.TryGetValue (id, out entity))
				return entity;
			return null;
		}

        public IEnumerable<IEntity> Find(params Type[] components) {
            return entities.Values.Where (e => e.ContainsAll(components));
        }

        public IEnumerable<IEntity> Find(IEnumerable<Type> components) {
            return Find(components.ToArray());
        }

        public IEnumerable<IEntity> Find<T>() {
            return Find(typeof(T));
        }

        public IEnumerable<IEntity> Find<T, U>() {
            return Find(typeof(T), typeof(U));
        }

        public IEnumerable<IEntity> Find<T, U, V>() {
            return Find(typeof(T), typeof(U), typeof(V));
        }

        public IEntity FindFirst(params Type[] components) {
            return Find(components).FirstOrDefault();
        }

        public IEntity FindFirst<T>() {
            return FindFirst(typeof(T));
        }

        public IEntity FindFirst<T, U>() {
            return FindFirst(typeof(T), typeof(U));
        }

        public IEntity FindFirst<T, U, V>() {
            return FindFirst(typeof(T), typeof(U), typeof(V));
        }
    }
}
