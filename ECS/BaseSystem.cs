using System;
using System.Collections.Generic;

namespace KS.Foundation.ECS {

    /// <summary>
    /// Provides a system that contains the logic of the world.
    /// Systems (only) act on entities with the correct components.
    /// </summary>
    public abstract class BaseSystem 
	{

        /// <summary>
        /// The entity finder used to find entities.
        /// </summary>
        private IFinder entityFinder;

        /// <summary>
        /// Reference the entity finder of the world.
        /// </summary>
        protected internal IFinder Finder {
            protected get {
                return entityFinder;
            }
            set {
                entityFinder = value;
            }
        }

        /// <summary>
        /// Collection of the components an entity needs for the system to act on it.
        /// </summary>
		internal ISet<Type> KeyComponents { get; set; }

        /// <summary>
        /// Constructor of the base system.
        /// </summary>
        protected BaseSystem() {
            KeyComponents = new HashSet<Type>();
            AddKeyComponents();
        }			

        /// <summary>
        /// Adds a component to the list of key components for the system.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        protected internal void AddKeyComponent<T>() where T : IComponent {
            KeyComponents.Add(typeof(T));
        }

        /// <summary>
        /// The update methods for the system can be overridden seperately, to allow for different behaviour.
        /// </summary>
        /// <param name="entities">the list of entities</param>
        /// <param name="elapsedMs">the elapsed time</param>
        public virtual void Update(IEnumerable<IEntity> entities, double elapsedMs) {
            foreach (Entity entity in entities) {
                UpdateEntity(entity, elapsedMs);
            }
        }

        /// <summary>
        /// Determines whether a system is a draw system or not.
        /// </summary>
        protected internal abstract bool IsDraw();

        /// <summary>
        /// Abstract method that needs to be implemented by the child classes, giving them an 
        /// opportunity to add the key components.
        /// </summary>
        protected internal abstract void AddKeyComponents();

        /// <summary>
        /// An update for a specific entity.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        /// <param name="elapsedMs">he elapsed time since the last update.</param>
        protected internal virtual void UpdateEntity(IEntity entity, double elapsedMs)
		{
		}
        
        /// <summary>
        /// Before a specific update.
        /// </summary>
        /// <param name="elapsedMs">The elapsed time since the last update.</param>
        protected internal virtual void BeforeUpdate(double elapsedMs) {}

        /// <summary>
        /// After a specific update.
        /// </summary>
        /// <param name="elapsedMs">The elapsed time since the last update.</param>
        protected internal virtual void AfterUpdate(double elapsedMs) {}

        /// <summary>
        /// Methods that gets called when the system adds an entity.
        /// </summary>
        /// <param name="entity"> The entity that got added. </param>
        protected internal virtual void EntityAdded(IEntity entity) {}

		protected internal virtual void RemovingEntity(IEntity entity) {}

        /// <summary>
        /// Method that gets called when the system removes an entity.
        /// </summary>
        /// <param name="entity">The entity that got removed.</param>
        protected internal virtual void EntityRemoved(IEntity entity) {}
		protected internal abstract void Clear ();
    }
}
