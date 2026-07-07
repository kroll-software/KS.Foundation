using System;
using System.Collections.Generic;

namespace KS.Foundation.ECS 
{
    /// <summary>
    /// The world in which all entities and systems exist, Create using the WorldFactory.
    /// </summary>
    public interface IWorld {

        /// <summary>
        /// Some info for this IWorld;
        /// </summary>
        IWorldInfo WorldInfo { get; }

        /// <summary>
        /// The entity finder for this world instance.
        /// </summary>
        IFinder EntityFinder { get; }

        /// <summary>
        /// Adds a system to the world.
        /// </summary>
        /// <param name="system">The system(s) to add. </param>
        IWorld AddSystem(BaseSystem system);

        /// <summary>
        /// Adds several systems to the world.
        /// </summary>
        /// <param name="systems">The systems to add. </param>
        IWorld AddSystems(params BaseSystem[] systems);

        /// <summary>
        /// Creates an entity in the ECP world.
        /// </summary>
        /// <returns>The created entity</returns>
        IEntity CreateEntity();
		IEntity CreateEntity(BaseSystem system);
		void InitEntity (IEntity entity, BaseSystem system);
        
        /// <summary>
        /// Removes an entity from the world.
        /// </summary>
        /// <param name="entity">The entity to remove. </param>
        void RemoveEntity(IEntity entity);

		T AddComponent<T> (IEntity entity, T component) where T : IComponent;
		void AddComponents (IEntity entity, params IComponent[] components);
		void RemoveComponent <T>(IEntity entity) where T : IComponent;
		//void ClearComponents (IEntity entity);

		IEntity Resolve (int id);

        /// <summary>
        /// Updates the systems, which work on the entities.
        /// </summary>
        /// <param name="elapsedMs">The elapsed milliseconds since the last update. </param>
        void Update(double elapsedMs);

        /// <summary>
        /// Updates the systems working on drawing the entities.
        /// </summary>
        /// <param name="elapsedMs">The elapsed milliseconds since the last update. </param>
        void Draw(double elapsedMs);

		void ClearData ();
    }
}
