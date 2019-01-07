using System;

namespace KS.Foundation.ECS 
{

    /// <summary>
    /// Provides a factory for creating the World in which all objects exist.
    /// </summary>
    public class WorldFactory {

        /// <summary>
        /// Creates the world.
        /// </summary>
        /// <param name="systems">Optional systems that get added to the world.</param>
        /// <returns>The world that got created. </returns>
        public static IWorld Create(params BaseSystem[] systems) {
            return new World().AddSystems(systems);
        }
    }
}
