using System;
using System.Collections.Generic;
using System.Linq;

namespace KS.Foundation.ECS
{
    /// <summary>
    /// Provides a collection for the systems.
    /// </summary>
    class SystemManager {		
        readonly IDictionary<Type, SystemWrapper> wrappers;

		World World { get; set; }

		public SystemManager(World world) {
			World = world;
            wrappers = new Dictionary<Type, SystemWrapper>();            
        }

		~SystemManager()
		{
			World = null;
		}
			        
        public bool Add(BaseSystem system, IEnumerable<IEntity> existingEntities) {
            if (wrappers.ContainsKey(system.GetType())) {
                return false;
            }
            SystemWrapper wrapper = new SystemWrapper(system);
            wrappers.Add(system.GetType(), wrapper);

            foreach (Entity enitity in existingEntities) {
                wrapper.AddEntity(enitity);
            }
            return true;
        }

        internal void Update(double elapsedMs) {
            Update(elapsedMs, wrappers.Values.Where(system => !system.IsDraw()));
        }

        internal void Draw(double elapsedMs) {
            Update(elapsedMs, wrappers.Values.Where(system => system.IsDraw()));
        }
        
        void Update(double elapsedMs, IEnumerable<SystemWrapper> systems) {
            foreach (SystemWrapper system in systems) {
                system.Update(elapsedMs);
            }
        }

        //-- Entity Added/Removed Methods --//
        public void OnEntityAdded(Entity entity) {
            foreach (SystemWrapper system in wrappers.Values) {
                system.AddEntity(entity);
            }
        }
			
		public void OnRemovingEntity(Entity entity) {
			wrappers.Values.Where(w => w.ContainsEntity(entity)).ForEach(wrapper => wrapper.RemovingEntity(entity));
		}

		public void OnRemovingComponent<T>(Entity entity) {
			wrappers.Values.Where(wrapper => wrapper.System.KeyComponents.Contains(typeof(T)))
				.ForEach(wrapper => wrapper.RemovingEntity(entity));			
		}

		public void OnEntityRemoved(Entity entity) {
            foreach (SystemWrapper system in wrappers.Values) {
                system.RemoveEntity(entity);
            }
        }

		public void OnEntityChanged(Entity entity) {
            foreach (SystemWrapper system in wrappers.Values) {
                system.UpdateEntityValidity(entity);
            }
        }
			        
        public int GetEntityCount(BaseSystem system) {
            SystemWrapper wrapper;
            wrappers.TryGetValue(system.GetType(), out wrapper);
            if (wrapper != null)
				return wrapper.EntityCount;
			return 0;
        }			

        public int BaseSystemCount {
            get {
				return wrappers.Values.Count(system => !system.IsDraw());
            }
        }

        public int DrawSystemCount {
            get {
				return wrappers.Values.Count(system => system.IsDraw());
            }
        }

        public IEnumerable<Type> BaseSystems() {
            return wrappers.Where(pair => !pair.Value.IsDraw()).Select(pair => pair.Key);
        }

        public IEnumerable<Type> DrawSystems() {
            return wrappers.Where(pair => pair.Value.IsDraw()).Select(pair => pair.Key);
        }

		public void ClearData()
		{
			wrappers.Values.ForEach (wrapper => wrapper.ClearData ());
		}
    }
}
