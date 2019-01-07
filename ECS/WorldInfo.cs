using System;
using System.Collections.Generic;

namespace KS.Foundation.ECS {
    /// <summary>
    /// Provides an implementation of the IDebugInfo interface.
    /// </summary>
    class WorldInfo : IWorldInfo {
        private EntityManager entities;
        private SystemManager systems;

        public WorldInfo(EntityManager entities, SystemManager systems) {
            this.entities = entities;
            this.systems = systems;
        }

        public int TotalSystemCount {
            get {
                return DrawSystemCount + BaseSystemCount;
            }
        }

        public int BaseSystemCount {
            get {
                return systems.BaseSystemCount;
            }
        }

        public int DrawSystemCount {
            get {
                return systems.DrawSystemCount;
            }
        }

        public int TotalEntityCount {
            get {
                return entities.EntityCount;
            }
        }

        public int TotalComponentCount {
            get {
                return entities.ComponentCount;
            }
        }

        public IEnumerable<Type> UpdateSystemTypes {
            get {
                return systems.BaseSystems();
            }
        }

        public IEnumerable<Type> DrawSystemTypes {
            get {
                return systems.DrawSystems();
            }
        }

        public int ComponentCount(IEntity entity) {
            return entity.ComponentCount;
        }

        public int EntityCount(BaseSystem system) {
            return systems.GetEntityCount(system);
        }

        public void printUpdateSystemTypes() {
            foreach (Type type in UpdateSystemTypes) {
                System.Diagnostics.Debug.WriteLine("System type: " + type);
            }
        }

        public void printDrawSystemTypes() {
            foreach (Type type in DrawSystemTypes) {
                System.Diagnostics.Debug.WriteLine("System type: " + type);
            }
        }
    }
}
