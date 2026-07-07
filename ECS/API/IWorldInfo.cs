using System;
using System.Collections.Generic;

namespace KS.Foundation.ECS 
{

    /// <summary>
    /// Provides some debug info for the world.
    /// </summary>
    public interface IWorldInfo {

        /// <summary>
        /// The total amount of systems.
        /// </summary>
        int TotalSystemCount { get; }

        /// <summary>
        /// The total amount of draw systems.
        /// </summary>
        int DrawSystemCount { get; }

        /// <summary>
        /// The total amount of base systems.
        /// </summary>
        int BaseSystemCount { get; }

        /// <summary>
        /// The total amount of entities.
        /// </summary>
        int TotalEntityCount { get; }

        /// <summary>
        /// The total amount of Components.
        /// </summary>
        int TotalComponentCount { get; }

        /// <summary>
        /// Returns a list of the types of the base (non draw) systems, in the order they are executed.
        /// </summary>
        /// <returns>The types of the systems</returns>
        IEnumerable<Type> UpdateSystemTypes { get; }

        /// <summary>
        /// Returns a list of the types of the draw systems, in the order they are executed.
        /// </summary>
        /// <returns>The types of the systems</returns>
        IEnumerable<Type> DrawSystemTypes { get; }

        /// <summary>
        /// The amount of components in a specific entity.
        /// </summary>
        /// <param name="entity">The entity to check.</param>
        /// <returns>The amount of components.</returns>
        int ComponentCount(IEntity entity);

        /// <summary>
        /// The amount of entities in a specific system.
        /// </summary>
        /// <param name="system">The system to check.</param>
        /// <returns>The amount of entities.</returns>
        int EntityCount(BaseSystem system);

        /// <summary>
        /// Prints the list of update systems.
        /// </summary>
        void printUpdateSystemTypes();

        /// <summary>
        /// Prints the list of draw systems.
        /// </summary>
        void printDrawSystemTypes();
    }
}
