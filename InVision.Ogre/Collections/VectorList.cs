﻿using System;
using System.Collections;
using System.Collections.Generic;
using InVision.Native;
using InVision.Ogre.Native;

namespace InVision.Ogre.Collections
{
	public abstract class VectorList<T> : Handle,  IList<T>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="VectorList&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="pSelf">The p self.</param>
		/// <param name="ownsHandle">if set to <c>true</c> [owns handle].</param>
		protected VectorList(IntPtr pSelf, bool ownsHandle)
			: base(pSelf, ownsHandle)
		{
		}

		#region IList<T> Members

		/// <summary>
		/// 	Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>
		/// 	A <see cref = "T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
		/// </returns>
		/// <filterpriority>1</filterpriority>
		public abstract IEnumerator<T> GetEnumerator();

		/// <summary>
		/// 	Returns an enumerator that iterates through a collection.
		/// </summary>
		/// <returns>
		/// 	An <see cref = "T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
		/// </returns>
		/// <filterpriority>2</filterpriority>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <summary>
		/// 	Adds an item to the <see cref = "T:System.Collections.Generic.ICollection`1" />.
		/// </summary>
		/// <param name = "item">The object to add to the <see cref = "T:System.Collections.Generic.ICollection`1" />.</param>
		/// <exception cref = "T:System.NotSupportedException">The <see cref = "T:System.Collections.Generic.ICollection`1" /> is read-only.</exception>
		public abstract void Add(T item);

		/// <summary>
		/// 	Removes all items from the <see cref = "T:System.Collections.Generic.ICollection`1" />.
		/// </summary>
		/// <exception cref = "T:System.NotSupportedException">The <see cref = "T:System.Collections.Generic.ICollection`1" /> is read-only. </exception>
		public void Clear()
		{
			NativeVectorList.Clear(handle);
		}

		/// <summary>
		/// 	Determines whether the <see cref = "T:System.Collections.Generic.ICollection`1" /> contains a specific value.
		/// </summary>
		/// <returns>
		/// 	true if <paramref name = "item" /> is found in the <see cref = "T:System.Collections.Generic.ICollection`1" />; otherwise, false.
		/// </returns>
		/// <param name = "item">The object to locate in the <see cref = "T:System.Collections.Generic.ICollection`1" />.</param>
		public abstract bool Contains(T item);

		/// <summary>
		/// 	Copies the elements of the <see cref = "T:System.Collections.Generic.ICollection`1" /> to an <see cref = "T:System.Array" />, starting at a particular <see cref = "T:System.Array" /> index.
		/// </summary>
		/// <param name = "array">The one-dimensional <see cref = "T:System.Array" /> that is the destination of the elements copied from <see cref = "T:System.Collections.Generic.ICollection`1" />. The <see cref = "T:System.Array" /> must have zero-based indexing.</param>
		/// <param name = "arrayIndex">The zero-based index in <paramref name = "array" /> at which copying begins.</param>
		/// <exception cref = "T:System.ArgumentNullException"><paramref name = "array" /> is null.</exception>
		/// <exception cref = "T:System.ArgumentOutOfRangeException"><paramref name = "arrayIndex" /> is less than 0.</exception>
		/// <exception cref = "T:System.ArgumentException"><paramref name = "array" /> is multidimensional.-or-The number of elements in the source <see cref = "T:System.Collections.Generic.ICollection`1" /> is greater than the available space from <paramref name = "arrayIndex" /> to the end of the destination <paramref name = "array" />.-or-Type <paramref name = "T" /> cannot be cast automatically to the type of the destination <paramref name = "array" />.</exception>
		public abstract void CopyTo(T[] array, int arrayIndex);

		/// <summary>
		/// 	Removes the first occurrence of a specific object from the <see cref = "T:System.Collections.Generic.ICollection`1" />.
		/// </summary>
		/// <returns>
		/// 	true if <paramref name = "item" /> was successfully removed from the <see cref = "T:System.Collections.Generic.ICollection`1" />; otherwise, false. This method also returns false if <paramref name = "item" /> is not found in the original <see cref = "T:System.Collections.Generic.ICollection`1" />.
		/// </returns>
		/// <param name = "item">The object to remove from the <see cref = "T:System.Collections.Generic.ICollection`1" />.</param>
		/// <exception cref = "T:System.NotSupportedException">The <see cref = "T:System.Collections.Generic.ICollection`1" /> is read-only.</exception>
		public abstract bool Remove(T item);

		/// <summary>
		/// 	Gets the number of elements contained in the <see cref = "T:System.Collections.Generic.ICollection`1" />.
		/// </summary>
		/// <returns>
		/// 	The number of elements contained in the <see cref = "T:System.Collections.Generic.ICollection`1" />.
		/// </returns>
		public int Count
		{
			get { return NativeVectorList.GetCount(handle); }
		}

		/// <summary>
		/// 	Gets a value indicating whether the <see cref = "T:System.Collections.Generic.ICollection`1" /> is read-only.
		/// </summary>
		/// <returns>
		/// 	true if the <see cref = "T:System.Collections.Generic.ICollection`1" /> is read-only; otherwise, false.
		/// </returns>
		public bool IsReadOnly
		{
			get { return false; }
		}

		/// <summary>
		/// 	Determines the index of a specific item in the <see cref = "T:System.Collections.Generic.IList`1" />.
		/// </summary>
		/// <returns>
		/// 	The index of <paramref name = "item" /> if found in the list; otherwise, -1.
		/// </returns>
		/// <param name = "item">The object to locate in the <see cref = "T:System.Collections.Generic.IList`1" />.</param>
		public abstract int IndexOf(T item);

		/// <summary>
		/// 	Inserts an item to the <see cref = "T:System.Collections.Generic.IList`1" /> at the specified index.
		/// </summary>
		/// <param name = "index">The zero-based index at which <paramref name = "item" /> should be inserted.</param>
		/// <param name = "item">The object to insert into the <see cref = "T:System.Collections.Generic.IList`1" />.</param>
		/// <exception cref = "T:System.ArgumentOutOfRangeException"><paramref name = "index" /> is not a valid index in the <see cref = "T:System.Collections.Generic.IList`1" />.</exception>
		/// <exception cref = "T:System.NotSupportedException">The <see cref = "T:System.Collections.Generic.IList`1" /> is read-only.</exception>
		public abstract void Insert(int index, T item);

		/// <summary>
		/// 	Removes the <see cref = "T:System.Collections.Generic.IList`1" /> item at the specified index.
		/// </summary>
		/// <param name = "index">The zero-based index of the item to remove.</param>
		/// <exception cref = "T:System.ArgumentOutOfRangeException"><paramref name = "index" /> is not a valid index in the <see cref = "T:System.Collections.Generic.IList`1" />.</exception>
		/// <exception cref = "T:System.NotSupportedException">The <see cref = "T:System.Collections.Generic.IList`1" /> is read-only.</exception>
		public abstract void RemoveAt(int index);

		/// <summary>
		/// 	Gets or sets the element at the specified index.
		/// </summary>
		/// <returns>
		/// 	The element at the specified index.
		/// </returns>
		/// <param name = "index">The zero-based index of the element to get or set.</param>
		/// <exception cref = "T:System.ArgumentOutOfRangeException"><paramref name = "index" /> is not a valid index in the <see cref = "T:System.Collections.Generic.IList`1" />.</exception>
		/// <exception cref = "T:System.NotSupportedException">The property is set and the <see cref = "T:System.Collections.Generic.IList`1" /> is read-only.</exception>
		public abstract T this[int index] { get; set; }

		#endregion

		/// <summary>
		/// 	Releases the specified pointer to the unmanaged object.
		/// </summary>
		/// <returns></returns>
		protected override void ReleaseValidHandle()
		{
			NativeVectorList.Delete(handle);
		}
	}
}