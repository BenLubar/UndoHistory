using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace UndoHistory
{
    /// <summary>
    /// A list of <see cref="HistoryItem{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of object history is being tracked for.</typeparam>
    [Serializable]
    public sealed class HistoryList<T> : ICollection<HistoryItem<T>>, ICloneable, INotifyPropertyChanged, INotifyCollectionChanged
#if !NET40
        , IReadOnlyList<HistoryItem<T>>
#endif
    {
        /// <summary>
        /// An event that is triggered by <see cref="Index"/> or <see cref="Count"/> changing.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// An event that is triggered by <see cref="Do(ref T, HistoryItem{T})"/>, <see cref="Add(HistoryItem{T})"/>, and <see cref="Clear"/>.
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private readonly List<HistoryItem<T>> list = new List<HistoryItem<T>>();

        bool ICollection<HistoryItem<T>>.IsReadOnly => false;

        /// <summary>
        /// The current index in the list. This is between 0 and <see cref="Count"/>, inclusive.
        /// </summary>
        public int Index { get; private set; }

        /// <summary>
        /// Constructs a new empty instance of <see cref="HistoryList{T}"/>.
        /// </summary>
        public HistoryList()
        {
        }

        /// <summary>
        /// Constructs a new <see cref="HistoryList{T}"/> with a copy of the state of <paramref name="other"/>.
        /// </summary>
        /// <param name="other">The list to copy.</param>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is null.</exception>
        public HistoryList(HistoryList<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            this.list.AddRange(other);
            this.Index = other.Index;
        }

        /// <summary>
        /// Constructs a new <see cref="HistoryList{T}"/> containing <paramref name="items"/>.
        /// </summary>
        /// <remarks>The <see cref="Index"/> is set to <see cref="Count"/> by this constructor.</remarks>
        /// <param name="items">A sequence of <see cref="HistoryItem{T}"/> to add to the newly constructed <see cref="HistoryList{T}"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="items"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="items"/> contains null as an element.</exception>
        public HistoryList(IEnumerable<HistoryItem<T>> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            foreach (var item in items)
            {
                if (item == null)
                {
                    throw new ArgumentException("items contains null as an element.", nameof(items));
                }

                this.list.Add(item);
            }

            this.Index = this.Count;
        }

        /// <summary>
        /// Performs an <paramref name="action"/> on the <paramref name="target"/>. The <paramref name="action"/> is added to the history.
        /// </summary>
        /// <param name="target">The target of the action.</param>
        /// <param name="action">The action to perform.</param>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is null.</exception>
        public void Do(ref T target, HistoryItem<T> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            action.Apply(ref target);
            this.Add(action);
        }

        /// <summary>
        /// Calls <see cref="Undo(ref T)"/> or <see cref="Redo(ref T)"/> until <see cref="Index"/> is equal to <paramref name="index"/>.
        /// </summary>
        /// <param name="target">The target of the action.</param>
        /// <param name="index">The index in the history list to move to.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is greater than <see cref="Count"/>.</exception>
        public void MoveTo(ref T target, int index)
        {
            if (index < 0 || index > this.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "index is outside the bounds of this HistoryList.");
            }

            while (this.Index < index)
            {
                this.Redo(ref target);
            }

            while (this.Index > index)
            {
                this.Undo(ref target);
            }
        }

        /// <summary>
        /// Re-applies the first undone action.
        /// </summary>
        /// <param name="target">The target of the action.</param>
        /// <exception cref="InvalidOperationException"><see cref="Index"/> is equal to <see cref="Count"/>.</exception>
        public void Redo(ref T target)
        {
            if (this.Index >= this.Count)
            {
                throw new InvalidOperationException("Cannot redo past end of HistoryList.");
            }

            this.list[this.Index].Apply(ref target);
            this.Index++;
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Index)));
        }

        /// <summary>
        /// Undoes the last applied action.
        /// </summary>
        /// <param name="target">The target of the action.</param>
        /// <exception cref="InvalidOperationException"><see cref="Index"/> is 0.</exception>
        public void Undo(ref T target)
        {
            if (this.Index <= 0)
            {
                throw new InvalidOperationException("Cannot undo past start of HistoryList.");
            }

            this.list[this.Index - 1].Invert().Apply(ref target);
            this.Index--;
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Index)));
        }

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is equal to or greater than <see cref="Count"/>.</exception>
        public HistoryItem<T> this[int index] => this.list[index];
        /// <summary>
        /// Returns the number of items in this list.
        /// </summary>
        public int Count => this.list.Count;

        /// <summary>
        /// Adds an item to the end of the history.
        /// </summary>
        /// <seealso cref="Do(ref T, HistoryItem{T})"/>
        /// <remarks><see cref="Index"/> will be equal to <see cref="Count"/> when this method returns.</remarks>
        /// <param name="item">The item to add.</param>
        /// <exception cref="ArgumentNullException"><paramref name="item"/> is null.</exception>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public void Add(HistoryItem<T> item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            this.MaterializeEnd();
            this.list.Add(item);
            this.Index = this.Count;
            this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, this.Index - 1));
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Index)));
        }

        /// <summary>
        /// Removes all items from this list.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public void Clear()
        {
            if (this.Count != 0)
            {
                this.list.Clear();
                this.Index = 0;
                this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Index)));
            }
        }

        /// <summary>
        /// Moves <see cref="Index"/> to the end of the list, materializing the undo operation for any <see cref="HistoryItem{T}"/> that is currently undone.
        /// </summary>
        private void MaterializeEnd()
        {
            if (this.Index == this.Count)
            {
                return;
            }

            var originalCount = this.Count;
            var toAdd = new HistoryItem<T>[originalCount - this.Index];

            for (int i = 0; i < toAdd.Length; i++)
            {
                toAdd[i] = this.list[originalCount - 1 - i].Invert();
            }

            this.list.AddRange(toAdd);
            this.Index = this.Count;
            this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, toAdd, originalCount));
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Index)));
        }

        /// <summary>
        /// Determines whether an element is in the <see cref="HistoryList{T}"/>.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="HistoryList{T}"/>. The value can be null for reference types.</param>
        /// <returns>true if <paramref name="item"/> is found in the <see cref="HistoryList{T}"/>; otherwise, false.</returns>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public bool Contains(HistoryItem<T> item) => this.list.Contains(item);
        /// <summary>
        /// Copies the entire <see cref="HistoryList{T}"/> to a compatible one-dimensional array, starting at the specified index of the target array.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="HistoryList{T}"/>. The <see cref="Array"/> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception>
        /// <exception cref="ArgumentException">The number of elements in the source <see cref="HistoryList{T}"/> is greater
        /// than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.</exception>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public void CopyTo(HistoryItem<T>[] array, int arrayIndex) => this.list.CopyTo(array, arrayIndex);

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="HistoryList{T}"/>.
        /// </summary>
        /// <returns>A <see cref="IEnumerable{T}"/> for the <see cref="HistoryList{T}"/>.</returns>
        public IEnumerator<HistoryItem<T>> GetEnumerator() => this.list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        object ICloneable.Clone() => new HistoryList<T>(this);
        bool ICollection<HistoryItem<T>>.Remove(HistoryItem<T> item) => throw new NotSupportedException();
    }
}
