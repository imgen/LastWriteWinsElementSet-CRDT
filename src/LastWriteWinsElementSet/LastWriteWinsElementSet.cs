using System;
using System.Collections.Generic;
using System.Linq;

namespace LastWriteWinsElementSet
{
    public class LastWriteWinsElementSet<T>
    {
        private readonly IEqualityComparer<T> _comparer;
        private IDictionary<T, List<LastWriteWinsElement<T>>> _addSet, _removeSet;

        /// <summary>
        /// Gets a clone of _addSet so original set won't be modified
        /// </summary>
        public IDictionary<T, List<LastWriteWinsElement<T>>> AddSet =>
            _addSet.ToDictionary(pair => pair.Key, pair => pair.Value.ToList());
        
        /// <summary>
        /// Gets a clone of _removeSet so original set won't be modified
        /// </summary>
        public IDictionary<T, List<LastWriteWinsElement<T>>> RemoveSet =>
            _removeSet.ToDictionary(pair => pair.Key, pair => pair.Value.ToList());

        public LastWriteWinsElementSet(IEqualityComparer<T> comparer = null, 
            IDictionary<T, List<LastWriteWinsElement<T>>> addSet = null,
            IDictionary<T, List<LastWriteWinsElement<T>>> removeSet = null)
        {
            comparer = comparer?? EqualityComparer<T>.Default;
            _comparer = comparer;
            _addSet = addSet?? new Dictionary<T, List<LastWriteWinsElement<T>>>(comparer);
            _removeSet = removeSet?? new Dictionary<T, List<LastWriteWinsElement<T>>>(comparer);
        }

        public bool Lookup(T element)
        {
            if (!_addSet.ContainsKey(element))
                return false;

            if (!_removeSet.ContainsKey(element))
                return true;
            
            var additions = _addSet[element];
            var removals = _removeSet[element];

            var latestAdditionTimestamp = additions.Max(x => x.Timestamp);
            var latestRemovalTimestamp = removals.Max(x => x.Timestamp);

            return latestAdditionTimestamp > latestRemovalTimestamp;
        }

        public void Add(T element, DateTime? timestamp = null)
        {
            AddToSet(_addSet, element, timestamp);
        }
        
        public void Remove(T element, DateTime? timestamp = null)
        {
            if (!Lookup(element))
            {
                throw new ArgumentException("The element is not in the set yet, thus cannot be removed");
            }
            AddToSet(_removeSet, element, timestamp);
        }

        private static void AddToSet<T>(
            IDictionary<T, List<LastWriteWinsElement<T>>> set, 
            T element,
            DateTime? timestamp = null)
        {
            timestamp = timestamp ?? DateTime.UtcNow;
            var elementWithTimestamp = new LastWriteWinsElement<T>(element, timestamp.Value);
            if (set.ContainsKey(element))
            {
                set[element].Add(elementWithTimestamp);
            }
            else
            {
                set[element] = new List<LastWriteWinsElement<T>>
                {
                    elementWithTimestamp
                };
            }
        }

        /// <summary>
        /// Determines if this element set is less than or equal to another element set
        /// </summary>
        /// <param name="elementSet">The other element set</param>
        /// <returns>A boolean to indicate that whether this element set is less or equal to another element set</returns>
        public bool Compare(LastWriteWinsElementSet<T> elementSet) => 
            IsSubset(_addSet, elementSet._addSet) && 
            IsSubset(_removeSet, elementSet._removeSet);

        private bool IsSubset(IDictionary<T, List<LastWriteWinsElement<T>>> set1,
            IDictionary<T, List<LastWriteWinsElement<T>>> set2) => 
            set1.Keys.All(
                    element =>
                    {
                        if (!set2.ContainsKey(element))
                        {
                            return false;
                        }
                
                        var additionsOrRemovals1 = set1[element];
                        var additionsOrRemovals2 = set2[element];
                        return 
                            !additionsOrRemovals1.Except(additionsOrRemovals2,
                                    new LastWriteWinsElementEqualityComparer<T>(_comparer))
                                .Any();
                    }
                );

        public LastWriteWinsElementSet<T> Merge(LastWriteWinsElementSet<T> elementSet)
        {
            var mergedElementSet = new LastWriteWinsElementSet<T>(_comparer);
            UnionSet(mergedElementSet._addSet, _addSet);
            UnionSet(mergedElementSet._addSet, elementSet._addSet);
            UnionSet(mergedElementSet._removeSet, _removeSet);
            UnionSet(mergedElementSet._removeSet, elementSet._removeSet);
            elementSet.ResolveConflicts();
            return mergedElementSet;
        }

        /// <summary>
        /// Removes any conflicts by biasing toward addition
        /// </summary>
        private void ResolveConflicts()
        {
            foreach (var element in _addSet.Keys)
            {
                if (!_removeSet.ContainsKey(element))
                {
                    continue;
                }

                var additions = _addSet[element];
                var removals = _removeSet[element];
                foreach (var addition in additions)
                {
                    var removalsWithTheSameTimestamp = removals.Where(x => x.Timestamp == addition.Timestamp);
                    removals = removals.Except(removalsWithTheSameTimestamp).ToList();
                }

                if (removals.Any())
                {
                    _removeSet[element] = removals;
                }
                else
                {
                    _removeSet.Remove(element);
                }
            }
        }

        private static void UnionSet(IDictionary<T, List<LastWriteWinsElement<T>>> targetSet,
            IDictionary<T, List<LastWriteWinsElement<T>>> sourceSet)
        {
            foreach (var pair in sourceSet)
            {
                foreach (var element in pair.Value)
                {
                    AddToSet(targetSet, element.Element, element.Timestamp);
                }
            }
        }

        public LastWriteWinsElementSet<T> Clone() => 
            new LastWriteWinsElementSet<T>(_comparer, AddSet, RemoveSet);
    }
}