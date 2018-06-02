using System;
using System.Collections.Generic;

namespace UndoHistory
{
    /// <summary>
    /// A recorded action in the undo history.
    /// </summary>
    /// <typeparam name="T">The type of object this action was performed on.</typeparam>
    [Serializable]
    public abstract class HistoryItem<T> : IEquatable<HistoryItem<T>>
    {
        /// <summary>
        /// Returns the opposite of this action.
        /// </summary>
        /// <returns>A new <see cref="HistoryItem{T}"/> that does the opposite of this <see cref="HistoryItem{T}"/>.</returns>
        public abstract HistoryItem<T> Invert();
        /// <summary>
        /// Applies this action to the <paramref name="target"/>.
        /// </summary>
        /// <param name="target">The target of the action.</param>
        public abstract void Apply(ref T target);

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the other parameter; otherwise, false.</returns>
        public abstract bool Equals(HistoryItem<T> other);
        /// <summary>
        /// Computes a hash code for this action.
        /// </summary>
        /// <returns>A hash code for this action.</returns>
        public abstract override int GetHashCode();
        /// <summary>
        /// Returns a string representation of this action.
        /// </summary>
        /// <returns>A string representation of this action.</returns>
        public abstract override string ToString();

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if (obj is HistoryItem<T> item)
            {
                return this.Equals(item);
            }

            return false;
        }

        /// <summary>
        /// Compares two <see cref="HistoryItem{T}"/> for equality.
        /// </summary>
        /// <param name="item1">The first <see cref="HistoryItem{T}"/>.</param>
        /// <param name="item2">The second <see cref="HistoryItem{T}"/>.</param>
        /// <returns>true if <paramref name="item1"/> is equal to <paramref name="item2"/>; otherwise, false.</returns>
        public static bool operator ==(HistoryItem<T> item1, HistoryItem<T> item2) => EqualityComparer<HistoryItem<T>>.Default.Equals(item1, item2);
        /// <summary>
        /// Compares two <see cref="HistoryItem{T}"/> for equality.
        /// </summary>
        /// <param name="item1">The first <see cref="HistoryItem{T}"/>.</param>
        /// <param name="item2">The second <see cref="HistoryItem{T}"/>.</param>
        /// <returns>false if <paramref name="item1"/> is equal to <paramref name="item2"/>; otherwise, true.</returns>
        public static bool operator !=(HistoryItem<T> item1, HistoryItem<T> item2) => !(item1 == item2);
    }
}
