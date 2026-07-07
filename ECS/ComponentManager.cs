using System;
using System.Linq;
using System.Collections.Generic;
using KS.Foundation;

namespace KS.Foundation.ECS
{
	class ComponentManager
	{
		World World { get; set; }

		public ComponentManager (World world)
		{
			World = world;
		}

		~ComponentManager()
		{
			World = null;
		}

		public IComponent AddComponent (Entity entity, IComponent component)
		{
			if (entity == null || component == null)
				return null;
			
			if (entity.AddComponent (component)) {
				World.Systems.OnEntityChanged (entity);
				return component;
			} else {				
				return entity.Get(component.GetType ());
			}				
		}

		public void RemoveComponent <T>(Entity entity) where T : IComponent
		{
			if (entity == null)
				return;
			if (entity.Contains<T> ()) {
				World.Systems.OnRemovingComponent <T>(entity);
				if (entity.Remove<T> ())
					World.Systems.OnEntityChanged (entity);
			}
		}

		public void InitEntity(Entity entity, BaseSystem system)
		{
			if (entity == null || system == null)
				return;
			if (entity.ComponentCount == 0)
				system.KeyComponents.ForEach (type => AddComponent (entity, Activator.CreateInstance (type) as IComponent));
			else
				system.KeyComponents.Where(c => !entity.Contains(c)).ForEach (type => AddComponent (entity, Activator.CreateInstance (type) as IComponent));
		}

		/***
		public void ClearComponents(Entity entity)
		{			
		}
		***/
	}
}

