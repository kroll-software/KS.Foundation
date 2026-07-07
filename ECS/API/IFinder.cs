using System;
using System.Collections.Generic;

namespace KS.Foundation.ECS 
{

    /// <summary>
    /// Provides a class that searches the full list of entities for entities with specific components.
    /// </summary>
    public interface IFinder {

        /// <summary>
        /// Finds the entities that contain the correct components.
        /// </summary>
        /// <typeparam name="T">The component that the entities should contain. </typeparam>
        /// <returns>The entities with the correct components. </returns>
        IEnumerable<IEntity> Find<T>();

        /// <summary>
        /// Finds the entities that contain the correct components.
        /// </summary>
        /// <typeparam name="T">A component that the entities should contain. </typeparam>
        /// <typeparam name="U">A component that the entities should contain. </typeparam>
        /// <returns>The entities with the correct components. </returns>
        IEnumerable<IEntity> Find<T, U>();

        /// <summary>
        /// Finds the entities that contain the correct components.
        /// </summary>
        /// <typeparam name="T">A component that the entities should contain. </typeparam>
        /// <typeparam name="U">A component that the entities should contain. </typeparam>
        /// <typeparam name="V">A component that the entities should contain. </typeparam>
        /// <returns>The entities with the correct components. </returns>
        IEnumerable<IEntity> Find<T, U, V>();

        /// <summary>
        /// Finds the first entity that contain the correct components.
        /// </summary>
        /// <typeparam name="T">The component that the entity should contain. </typeparam>
        /// <returns>The first entity with the correct components. </returns>
        IEntity FindFirst<T>();

        /// <summary>
        /// Finds the first entity that contain the correct components.
        /// </summary>
        /// <typeparam name="T">A component that the entity should contain. </typeparam>
        /// <typeparam name="U">A component that the entity should contain. </typeparam>
        /// <returns>The first entity with the correct components. </returns>
        IEntity FindFirst<T, U>();

        /// <summary>
        /// Finds the first entity that contain the correct components.
        /// </summary>
        /// <typeparam name="T">A component that the entity should contain. </typeparam>
        /// <typeparam name="U">A component that the entity should contain. </typeparam>
        /// <typeparam name="V">A component that the entity should contain. </typeparam>
        /// <returns>The first entity with the correct components. </returns>
        IEntity FindFirst<T, U, V>();
    }
}
