using System;
using System.Text;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using KS.Foundation;
using Pfz.Collections;

namespace KS.Foundation.ECS
{    	
	public class Entity : IEntity, IComparable<IEntity>, IEquatable<Entity>
	{
		//static readonly object SyncLock = new object();

		/***
		// Autonumber Provider for Entity.ID

		static int NextID()
		{
			lock (SyncLock) {
				unchecked {
					Interlocked.Increment (ref m_NextID);
					return (m_NextID++);
				}
			}
		}
		***/

        //readonly IDictionary<Type, IComponent> components;
		readonly ThreadSafeDictionary<Type, IComponent> components;

		static int NextID = 1;
		internal static void ResetAutoNumber()
		{
			NextID = 1;
		}

		public int ID { get; private set; }
		public override int GetHashCode ()
		{
			return ID;
		}
			
        public Entity() 
		{			
			unchecked {
				ID = Interlocked.Increment (ref NextID);					
			}
			components = new ThreadSafeDictionary<Type, IComponent>();
        }

		~Entity()
		{
			Dispose ();
		}

		public int CompareTo (IEntity other)
		{
			if (other == null)
				return 0;
			return ID.CompareTo (other.ID);
		}
			
		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as Entity);
		}

		public bool Equals (Entity other)
		{
			return ID.Equals(other.ID);
		}
		/**** ***/

		public bool BelongsTo(BaseSystem sys)
		{
			return this.ContainsAll (sys.KeyComponents);
		}
            
		internal bool AddComponent(IComponent component)
        {
			Type t = component.GetType ();
			if (components.ContainsKey (t))
				components [t] = component;
            	//return false;
			else
				components.Add(t, component);
            return true;
        }

		internal bool Remove<T>() 
		{
			return components.Remove (typeof(T));
        }			

		//public static long GetComponentCount = 0;

        public T Get<T>() where T : IComponent 
		{
			//GetComponentCount++;
            IComponent component;
			if (components.TryGetValue(typeof(T), out component))
            	return (T)component;
			return default(T);
        }

		public IComponent Get(Type t)
		{
			//GetComponentCount++;
			IComponent component;
			if (t != null && components.TryGetValue(t, out component))
				return component;
			return null;
		}

		public bool TryGet<T>(out T component) where T : IComponent
		{
			//GetComponentCount++;
			IComponent comp;
			if (components.TryGetValue (typeof(T), out comp)) {
				component = (T)comp;
				return true;
			}
			component = default(T);
			return false;
		}

        public bool Contains<T>() {
            return Contains(typeof(T));
        }

        public bool Contains(Type type) {
            return components.ContainsKey(type);
        }

        public bool ContainsAll(IEnumerable<Type> types)
        {
			return types.All (Contains);
        }

        public int ComponentCount
        {
            get {
                return components.Count;
            }
        }

		public override string ToString ()
		{
			// components which don't serialize should return String.Empty in component.ToString()

			//return "{" + String.Join (",", components.Select (c => c.Value.ToString ()).Where(s => !String.IsNullOrEmpty(s))) + "}";

			StringBuilder sb = new StringBuilder ();
			sb.Append ("{");
			bool bFlag = false;
			foreach (var kv in components) {
				string val = kv.Value.ToString ();
				if (!String.IsNullOrEmpty (val)) {
					if (bFlag)
						sb.Append (",");
					else
						bFlag = true;
					sb.Append (val);					
				}
			}
			sb.Append ("}");
			return sb.ToString();
		}

		public string ToString (string typeName)
		{
			StringBuilder sb = new StringBuilder ();
			sb.Append ("{");
			sb.Append ("\"$type:\":\"");
			sb.Append (typeName);
			sb.Append ("\"");
			foreach (var kv in components) {
				string val = kv.Value.ToString ();
				if (!String.IsNullOrEmpty (val)) {
					sb.Append (",");
					sb.Append (val);
				}
			}
			sb.Append ("}");
			return sb.ToString();
		}

        public void Dispose()
        {
            components.Clear();            
			GC.SuppressFinalize (this);
        }
    }
}
