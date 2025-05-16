using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling;

namespace Astar.MultiThreaded
{
	[NativeContainerSupportsDeallocateOnJobCompletion]
	[BurstCompile]
	public struct NativeBinaryHeap : IDisposable
	{
		public int currentItemCount;
        private NativeArray<Node> heap;
        private NativeArray<Box>.ReadOnly boxes;
        private NativeArray<Node> nodes;

        public NativeBinaryHeap(int maxSize, Allocator allocator, NativeArray<Box>.ReadOnly boxes, NativeArray<Node> nodes)
        {
            heap = new NativeArray<Node>(maxSize + 1, allocator);
            currentItemCount = 0;
            this.boxes = boxes;
            this.nodes = nodes;
        }

        public void Add(Node item)
        {
            item.heapIndex = currentItemCount;
            boxes[item.gridBoxIndex].UpdateNode(nodes, item);
            heap[currentItemCount] = item;
            HeapifyUp(currentItemCount);
            currentItemCount++;
        }
    
        public Node RemoveFirst()
        {
            Node min = heap[0];
            currentItemCount--;
            Node node = heap[currentItemCount];
            node.heapIndex = 0;
            boxes[node.gridBoxIndex].UpdateNode(nodes, node);
            heap[0] = node;
            HeapifyDown(0);
            return min;
        }

        public bool Contains(Node aStarNode)
        {
            return heap[aStarNode.heapIndex].Equals(aStarNode);
        }
    
        public void UpdateItem(Node node)
        {
            heap[node.heapIndex] = node;
            HeapifyUp(node.heapIndex);
        }
    
        private void HeapifyUp(int index)
        {
            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;
                if (heap[index].fCost > heap[parentIndex].fCost)
                    break;

                Swap(parentIndex, index);
                index = parentIndex;
            }
        }

        private void HeapifyDown(int index)
        {
            while (index * 2 + 1 < currentItemCount)
            {
                int childIndex = index * 2 + 1;
                int rightChildIndex = index * 2 + 2;

                if (rightChildIndex < currentItemCount && heap[rightChildIndex].fCost < heap[childIndex].fCost)
                    childIndex = rightChildIndex;

                if (heap[index].fCost < heap[childIndex].fCost)
                    break;

                Swap(index, childIndex);
                index = childIndex;
            }
        }

        private void Swap(int index1, int index2)
        {
            Node node1 = heap[index1];
            Node node2 = heap[index2];
            node1.heapIndex = index2;
            node2.heapIndex = index1;
            boxes[node1.gridBoxIndex].UpdateNode(nodes, node1);
            boxes[node2.gridBoxIndex].UpdateNode(nodes, node2);
            heap[index1] = node2;
            heap[index2] = node1;
        }
        
        public void Dispose()
        {
	        if (heap.IsCreated)
		        heap.Dispose();
        }
	}
}