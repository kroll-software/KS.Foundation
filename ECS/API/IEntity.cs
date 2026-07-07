using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KS.Foundation.ECS
{
    /// <summary>
    /// Provides the interface of an entity for the systems to work on. 
    /// </summary>
    public interface IEntity : IComparable<IEntity>
    {
		int ID { get; }
		        
        /// <summary>
        /// Returns the component of a specific type. Null if no component is available.
        /// </summary>
        /// <typeparam name="T">The type of the component to return. </typeparam>
        /// <returns>The component with the parameter type. </returns>
        T Get<T>() where T : IComponent;
		IComponent Get (Type t);
		bool TryGet<T>(out T component) where T : IComponent;

        /// <summary>
        /// Checks if the entity contains a component with a specific type.
        /// </summary>
        /// <typeparam name="T">The specific type of the component to check. </typeparam>
        /// <returns>Whether the component is present. </returns>
        bool Contains<T>();
        
        /// <summary>
        /// Returns the count of the components.
        /// </summary>
        int ComponentCount { get; }

		string ToString (string typeName);
    }
}
