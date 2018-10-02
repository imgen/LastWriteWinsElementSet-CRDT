using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using LastWriteWinsElementSet;
using Xunit;

namespace LastWriteWinsElementSetTests
{
    public class LastWriteWinsElementSetTests
    {
        [Fact]
        public void TestAdditionAndRemoval()
        {
            var elementSet = new LastWriteWinsElementSet<int>();
            var start = DateTime.Parse("2018-10-02");
            
            // Initially, no element is in the set
            elementSet.Lookup(3).Should().BeFalse();
            
            // Adds element should show up in the set
            elementSet.Add(3, start);
            elementSet.Lookup(3).Should().BeTrue();
            
            // Adds the same element again should not give error
            elementSet.Add(3, start.AddSeconds(1));
            elementSet.Lookup(3).Should().BeTrue();
            
            // Removes the element should make the element disappear
            elementSet.Remove(3, start.AddSeconds(2));
            elementSet.Lookup(3).Should().BeFalse();
            
            // Re-adds the element should make the element show up again 
            elementSet.Add(3, start.AddSeconds(4));
            elementSet.Lookup(3).Should().BeTrue();
            
            // Re-removes the element should make the element disappear again 
            elementSet.Remove(3, start.AddSeconds(5));
            elementSet.Lookup(3).Should().BeFalse();
            
            // Removes an element that doesn't exist should throw error
            Assert.Throws<ArgumentException>(() => elementSet.Remove(4, start.AddSeconds(6)));
        }

        private static (LastWriteWinsElementSet<int> elementSet, DateTime latestTimestamp) 
            BuildRandomLastWriteWinsElementSet(DateTime? start = null)
        {
            var elementSet = new LastWriteWinsElementSet<int>();
            start = start?? DateTime.Parse("2018-10-02");

            const int operationCount = 1000;

            var random = new Random();
            var timestamp = start.Value;
            for (int i = 0; i < operationCount; i++)
            {
                timestamp = timestamp.AddMilliseconds(random.Next(1, 10000));
                var element = random.Next(1, 100);
                if (elementSet.Lookup(element)) // Can be addition or removal
                {
                    if (random.Next(1, 10) % 2 == 0)
                    {
                        elementSet.Add(element, timestamp);
                    }
                    else
                    {
                        elementSet.Remove(element, timestamp);
                    }
                }
                else
                {
                    elementSet.Add(element, timestamp);
                }
            }
                
            return (elementSet, timestamp);
        }

        [Fact]
        public void TestComparison()
        {
            var (elementSet1, latestTimestamp) = BuildRandomLastWriteWinsElementSet();
            var elementSet2 = elementSet1.Clone();

            // An set should be the subset of itself
            elementSet1.Compare(elementSet2).Should().BeTrue();
            
            // If we add one more element to the clone, the original set should be an subset
            elementSet2.Add(500, latestTimestamp.AddSeconds(1));
            elementSet1.Compare(elementSet2).Should().BeTrue();
            
            // If we do one or more operations to the original set, the subset relationship breaks
            elementSet1.Add(600, latestTimestamp.AddSeconds(2));
            elementSet1.Compare(elementSet2).Should().BeFalse();
        }

        [Fact]
        public void TestMerge()
        {
            var now = DateTime.UtcNow;
            var (elementSet1, timestamp) = BuildRandomLastWriteWinsElementSet(now);
            var (elementSet2, _) = BuildRandomLastWriteWinsElementSet(timestamp.AddSeconds(1));
            var mergedElementSet = elementSet1.Merge(elementSet2);
            
            // The merged element set is a union of the add sets and the remove sets, 
            // so the comparison of the two original sets to the merged should all be true
            elementSet1.Compare(mergedElementSet).Should().BeTrue();
            elementSet2.Compare(mergedElementSet).Should().BeTrue();
        }
    }
}