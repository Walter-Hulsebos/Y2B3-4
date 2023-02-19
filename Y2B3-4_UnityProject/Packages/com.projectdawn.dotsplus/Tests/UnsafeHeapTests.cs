using System;
using NUnit.Framework;
using Unity.Collections;
using ProjectDawn.Collections.LowLevel.Unsafe;

namespace ProjectDawn.Collections.Tests
{
    internal class UnsafeHeapTests
    {
        [Test]
        public unsafe void UnsafeHeapTests_Int_Push_Pop()
        {
            var heap = new UnsafeHeap<int, int>(1, Allocator.Temp);

            heap.Push(5, 2);
            heap.Push(2, 1);
            heap.Push(0, 0);
            heap.Push(10, 3);

            Assert.AreEqual(0, heap.Pop());
            Assert.AreEqual(1, heap.Pop());
            Assert.AreEqual(2, heap.Pop());
            Assert.AreEqual(3, heap.Pop());

            heap.Dispose();
        }

        [Test]
        public unsafe void UnsafeHeapTests_Int_Peek()
        {
            var heap = new UnsafeHeap<int, int>(1, Allocator.Temp);

            heap.Push(5, 2);
            heap.Push(2, 1);
            heap.Push(0, 0);
            heap.Push(10, 3);

            Assert.AreEqual(0, heap.Pop());
            Assert.AreEqual(1, heap.Peek());
            Assert.AreEqual(1, heap.Pop());
            Assert.AreEqual(2, heap.Pop());
            Assert.AreEqual(3, heap.Pop());

            heap.Dispose();
        }

        [Test]
        public unsafe void UnsafeHeapTests_Int_TryPop()
        {
            var heap = new UnsafeHeap<int, int>(1, Allocator.Temp);

            heap.Push(5, 2);
            heap.Push(2, 1);
            heap.Push(0, 0);
            heap.Push(10, 3);

            if (heap.TryPop(out int value))
                Assert.AreEqual(0, value);
            if (heap.TryPop(out value))
                Assert.AreEqual(1, value);
            if (heap.TryPop(out value))
                Assert.AreEqual(2, value);
            if (heap.TryPop(out value))
                Assert.AreEqual(3, value);
            if (heap.TryPop(out value))
                Assert.AreEqual(3, value);

            heap.Dispose();
        }

        [Test]
        public unsafe void UnsafeHeapTests_Int_TrimExcess()
        {
            var heap = new UnsafeHeap<int, int>(1, Allocator.Temp);

            heap.Push(5, 2);
            heap.Push(2, 1);
            heap.Push(0, 0);
            heap.Push(10, 3);

            Assert.AreNotEqual(heap.Length, heap.Capacity);

            heap.TrimExcess();

            Assert.AreEqual(heap.Length, heap.Capacity);

            heap.Dispose();
        }

        [Test]
        public unsafe void UnsafeHeapTests_Int_Clear()
        {
            var heap = new UnsafeHeap<int, int>(1, Allocator.Temp);

            heap.Push(5, 2);
            heap.Push(2, 1);
            heap.Push(0, 0);
            heap.Push(10, 3);

            heap.Clear();

            Assert.IsTrue(heap.IsEmpty);

            heap.Dispose();
        }

        /*[Test]
        public unsafe void UnsafeHeapTests_Int_GetArray()
        {
            var heap = new UnsafeHeap<int, int>(1, Allocator.Temp);

            heap.Push(5, 2);
            heap.Push(2, 1);
            heap.Push(0, 0);
            heap.Push(10, 3);

            var keys = heap.GetKeyArray(Allocator.TempJob);
            Assert.AreEqual(0, keys[0]);
            Assert.AreEqual(2, keys[1]);
            Assert.AreEqual(5, keys[2]);
            Assert.AreEqual(10, keys[3]);
            keys.Dispose();

            var values = heap.GetValueArray(Allocator.TempJob);
            Assert.AreEqual(0, values[0]);
            Assert.AreEqual(1, values[1]);
            Assert.AreEqual(2, values[2]);
            Assert.AreEqual(3, values[3]);
            values.Dispose();

            heap.Dispose();
        }*/
    }
}
