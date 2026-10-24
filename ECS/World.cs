using System;
using System.Collections.Generic;

namespace KS.Foundation.ECS
{
    /// <summary>
    /// Implementation of the IWorld interface.
    /// </summary>
    class World : IWorld {
        public IWorldInfo WorldInfo { get; private set; }

		public readonly EntityManager Entities;
		public readonly SystemManager Systems;
		public readonly ComponentManager Components;

        public World() {
            Entities = new EntityManager(this);
            Systems = new SystemManager(this);
			Components = new ComponentManager (this);
            WorldInfo = new WorldInfo(Entities, Systems);
        }

        public IWorld AddSystems(BaseSystem[] inputSystems) 
		{
            foreach (BaseSystem system in inputSystems) {
                AddSystem(system);
            }
            return this;
        }

        public IWorld AddSystem(BaseSystem system) 
		{
            system.Finder = this.EntityFinder;
            Systems.Add(system, Entities.EntityFinder.Find(system.KeyComponents));
            return this;
        }

        public IEntity CreateEntity() 
		{
            Entity entity = new Entity();
            Entities.Add(entity);
            return entity;
        }

		public IEntity CreateEntity(BaseSystem system) 
		{
			Entity entity = new Entity();
			Entities.Add(entity);
			InitEntity (entity, system);
			return entity;
		}

		public void InitEntity (IEntity entity, BaseSystem system)
		{
			Components.InitEntity (entity as Entity, system);
		}

        public void RemoveEntity(IEntity entity) {
            Entity converted_entity = entity as Entity;
            if (converted_entity != null) {
				Systems.OnRemovingEntity (converted_entity);
                converted_entity.Dispose();
                Entities.Remove(converted_entity);
            }
        }
			
		public T AddComponent<T> (IEntity entity, T component) where T : IComponent
		{
			if (component.Equals(default(T)))
				return default(T);
			Entity converted_entity = entity as Entity;
			if (converted_entity == null)
				return default(T);
			return (T)Components.AddComponent (converted_entity, component);
		}

		public void AddComponents (IEntity entity, params IComponent[] components)
		{
			if (components == null || components.Length == 0)
				return;
			Entity converted_entity = entity as Entity;
			if (converted_entity != null) {
				components.ForEach (c => Components.AddComponent (converted_entity, c));
			}
		}			

		public void RemoveComponent <T>(IEntity entity) where T : IComponent
		{
			Entity converted_entity = entity as Entity;
			if (converted_entity != null) {
				Components.RemoveComponent <T>(converted_entity);
			}
		}

		/***
		public void ClearComponents (IEntity entity)
		{
			Entity converted_entity = entity as Entity;
			if (converted_entity != null) {
				Components.ClearComponents (converted_entity);
			}
		}
		***/

        public void Update(double elapsedMs) 
		{
            Systems.Update(elapsedMs);
        }

        public void Draw(double elapsedMs) 
		{
            Systems.Draw(elapsedMs);
        }

        public IFinder EntityFinder 
		{
            get {
                return Entities.EntityFinder;
            }
        }

		public IEntity Resolve(int id)
		{
			return Entities.EntityFinder.EntityByID (id);
		}

		public void ClearData ()
		{
			Systems.ClearData ();
			Entities.Clear ();
			// ★48 (24.10.2026): Keine ID-ResetAutoNumber mehr — Entity.ID läuft
			// monoton (SummerGUI-Widget-Pattern: Interlocked.Increment, nie zurückgesetzt).
			// Begründung: Ein Reset würde neue Entities mit ALTEN IDs belegen →
			// ID-basierte Sets/Dicts (SystemWrapper.entities, EntityManager,
			// GanttViewSystem-Caches) könnten eine neue Entity als „bekannt“
			// klassifizieren (★47-Incident) oder ein Consumer mit gespeicherter
			// Alt-ID erhält stillschweigend eine komplette andere Entity.
			// Monotone IDs machen Aliasing strukturell unmöglich.
		}
    }
}
