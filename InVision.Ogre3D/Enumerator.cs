﻿using System;
using System.Collections;
using System.Collections.Generic;
using InVision.Ogre3D.Native;

namespace InVision.Ogre3D
{
	public class Enumerator<T> : Handle, IEnumerator<T>
	{
		private Func<IntPtr, T> converter;

		/// <summary>
		/// Initializes a new instance of the <see cref="Enumerator&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="pSelf">The p self.</param>
		/// <param name="ownsHandle">if set to <c>true</c> [owns handle].</param>
		public Enumerator(IntPtr pSelf, bool ownsHandle = true)
			: base(pSelf, ownsHandle)
		{
		}

		#region IEnumerator<T> Members

		/// <summary>
		/// Advances the enumerator to the next element of the collection.
		/// </summary>
		/// <returns>
		/// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
		/// </returns>
		/// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception><filterpriority>2</filterpriority>
		public bool MoveNext()
		{
			return NativeEnumerator.MoveNext(handle);
		}

		/// <summary>
		/// Sets the enumerator to its initial position, which is before the first element in the collection.
		/// </summary>
		/// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception><filterpriority>2</filterpriority>
		public void Reset()
		{
			NativeEnumerator.Reset(handle);
		}

		/// <summary>
		/// Gets the element in the collection at the current position of the enumerator.
		/// </summary>
		/// <returns>
		/// The element in the collection at the current position of the enumerator.
		/// </returns>
		public T Current
		{
			get
			{
				IntPtr data = NativeEnumerator.GetCurrent(handle);

				return converter(data);
			}
		}

		/// <summary>
		/// Gets the current element in the collection.
		/// </summary>
		/// <returns>
		/// The current element in the collection.
		/// </returns>
		/// <exception cref="T:System.InvalidOperationException">The enumerator is positioned before the first element of the collection or after the last element.</exception><filterpriority>2</filterpriority>
		object IEnumerator.Current
		{
			get { return Current; }
		}

		#endregion

		/// <summary>
		/// Releases the specified handle.
		/// </summary>
		/// <param name="pSelf">The handle.</param>
		/// <returns></returns>
		protected override bool Release(IntPtr pSelf)
		{
			NativeEnumerator.Delete(pSelf);
			return true;
		}

		/// <summary>
		/// Sets the converter.
		/// </summary>
		/// <param name="converter">The converter.</param>
		public void SetConverter(Func<IntPtr, T> converter)
		{
			this.converter = converter;
		}
	}
}