using System;
using System.ComponentModel;
using System.Drawing;

namespace KS.Foundation
{
	public interface ILayoutItem
	{
		RectangleF Bounds { get; }
		ILayoutItem Clone ();
	}

	public struct LayoutItem : ILayoutItem, IEquatable<LayoutItem>
    {
		public static readonly LayoutItem Empty = new LayoutItem(RectangleF.Empty, null);

		public readonly object Item;

		private RectangleF m_Bounds;
		public RectangleF Bounds 
		{ 
			get {
				return m_Bounds;
			}
		}

		public LayoutItem(RectangleF bounds, object item)
        {
			m_Bounds = bounds;
            Item = item;
        }

		public ILayoutItem Clone()
        {
			return new LayoutItem(this.Bounds, this.Item);
        }

		public override bool Equals (object obj)
		{
			return (obj is LayoutItem) && Equals ((LayoutItem)obj);
		}

		public bool Equals (LayoutItem other)
		{
			return m_Bounds == other.Bounds && Item == other.Item;
		}

		public override int GetHashCode ()
		{
			unchecked {
				return (Bounds.GetHashCode () + 17) ^ (Item.SafeHash () * 31);
			}
		}
    }
}
