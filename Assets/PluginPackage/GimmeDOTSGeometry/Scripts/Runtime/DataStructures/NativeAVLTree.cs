using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace GimmeDOTSGeometry
{
    /// <summary>
    /// Implementation of an AVL Tree to be used in Unity's Job System.
    /// <para> </para>
    /// <para>-Insertion: O(log(n))</para>
    /// <para>-Removing: O(log(n))</para>
    /// <para>-Search: O(log(n))</para>
    /// <para> </para>
    /// Internally, the nodes are stored in a NativeArray<T>, where each node stores
    /// pointers to its children and its parent. On removing, a NativeList stores
    /// the freed position, which is then filled as soon as a new element is inserted
    /// to save space
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="U">A comparison struct used to compare the inserted elements with each other</typeparam>
    public unsafe struct NativeAVLTree<T, U> : IDisposable where T : unmanaged where U : struct, IComparer<T>
    {

        public struct TreeNode
        {
            public sbyte balance;

            public int arrIdx;

            public int parent;
            public int left;
            public int right;

            public T value;
        }

        #region Public Variables

        public U comparer;

        #endregion

        #region Private Variables

        [NoAlias]
        private NativeList<TreeNode> elements;

        //Using a list as a stack is way faster than a NativeQueue, because of the internal allocations the queue makes all the time
        [NoAlias]
        private NativeList<int> free;

        private int root;

        #endregion

        public int Length { get => this.elements.Length; set => this.elements.Length = value; }

        public TreeNode* Root => this.root >= 0 ? (TreeNode*)this.elements.GetUnsafePtr() + this.root : null;

        public int RootIdx => this.root;

        public NativeList<TreeNode> Elements => this.elements;


        public NativeAVLTree(U comparer, Allocator allocator)
        {
            this.comparer = comparer;
            this.elements = new NativeList<TreeNode>(1, allocator);
            this.free = new NativeList<int>(allocator);
            this.root = -1;
        }

        public bool IsCreated => this.elements.IsCreated || this.free.IsCreated;

        public void Dispose()
        {
            if (this.elements.IsCreated)
            {
                this.elements.Dispose();
            }

            if(this.free.IsCreated)
            {
                this.free.Dispose();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>The index of the leftmost node of the tree. If the tree is empty, -1 is returned</returns>
        public int GetLeftmostNode()
        {
            var node = this.RootIdx;
            if(node >= 0)
            {
                var elem = this.elements[node];
                while(elem.left >= 0)
                {
                    node = elem.left;
                    elem = this.elements[node];
                }
            }
            return node;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>The index of the rightmost node of the tree. If the tree is empty, -1 is returned</returns>
        public int GetRightmostNode()
        {
            var node = this.RootIdx;
            if (node >= 0)
            {
                var elem = this.elements[node];
                while (elem.right >= 0)
                {
                    node = elem.right;
                    elem = this.elements[node];
                }
            }
            return node;
        }

        private void GetAllTreeElementsRecursion(ref NativeList<int> nodeIndices, TreeTraversal traversal, int currentNode)
        {
            var elem = this.Elements[currentNode];
            switch(traversal)
            {
                case TreeTraversal.PREORDER:

                    nodeIndices.Add(currentNode);
                    if(elem.left >= 0)
                    {
                        this.GetAllTreeElementsRecursion(ref nodeIndices, traversal, elem.left);
                    }
                    if(elem.right >= 0)
                    {
                        this.GetAllTreeElementsRecursion(ref nodeIndices, traversal, elem.right);
                    }
                    break;
                case TreeTraversal.POSTORDER:
                    if(elem.left >= 0)
                    {
                        this.GetAllTreeElementsRecursion(ref nodeIndices, traversal, elem.left);
                    }
                    if(elem.right >= 0)
                    {
                        this.GetAllTreeElementsRecursion(ref nodeIndices, traversal, elem.right);
                    }
                    nodeIndices.Add(currentNode);
                    break;
                case TreeTraversal.INORDER:
                    if(elem.left >= 0)
                    {
                        this.GetAllTreeElementsRecursion(ref nodeIndices, traversal, elem.left);
                    }
                    nodeIndices.Add(currentNode);
                    if(elem.right >= 0)
                    {
                        this.GetAllTreeElementsRecursion(ref nodeIndices, traversal, elem.right);
                    }
                    break;
            }
        }

        /// <summary>
        /// Visits all nodes of the tree and stores the indices into a list, sorted depending
        /// on the tree traversal.
        /// </summary>
        /// <param name="nodeIndices">The list to store the indices into</param>
        /// <param name="traversal">The way to recursively visit each node. <see href="https://en.wikipedia.org/wiki/Tree_traversal">Wiki</see>/></param>
        public void GetAllTreeElements(ref NativeList<int> nodeIndices, TreeTraversal traversal)
        {
            if(this.root >= 0)
            {
                this.GetAllTreeElementsRecursion(ref nodeIndices, traversal, this.root);
            }
        }

        private void GetHeightRecursive(int idx, int currentHeight, ref int maxHeight)
        {
            currentHeight++;
            if(currentHeight > maxHeight)
            {
                maxHeight = currentHeight;
            }

            var element = this.Elements[idx];
            if(element.left >= 0)
            {
                this.GetHeightRecursive(element.left, currentHeight, ref maxHeight);
            }

            if(element.right >= 0)
            {
                this.GetHeightRecursive(element.right, currentHeight, ref maxHeight);
            }
        }

        //Note: This operation is O(n)
        /// <summary>
        /// 
        /// </summary>
        /// <returns>The maximum height of the tree</returns>
        public int GetHeight()
        {
            if(this.root >= 0)
            {
                int height = 0;
                this.GetHeightRecursive(this.root, height, ref height);
                return height;
            }
            return 0;
        }

        /// <summary>
        /// Return the node that is immediately to the left of the given node. In other words,
        /// the first node with a "smaller" value than the given node
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public int GetFirstLeftEntry(int nodeIdx)
        {
            if (nodeIdx < 0) return nodeIdx;

            var basePtr = (TreeNode*)this.elements.GetUnsafePtr();
            var elem = basePtr + nodeIdx;
            if (elem->left >= 0)
            {
                nodeIdx = elem->left;
                elem = basePtr + nodeIdx;
                while(elem->right >= 0)
                {
                    nodeIdx = elem->right;
                    elem = basePtr + nodeIdx;
                }
                return nodeIdx;
            } else
            {
                while(elem->parent >= 0 && (basePtr + elem->parent)->right != nodeIdx)
                {
                    nodeIdx = elem->parent;
                    elem = basePtr + nodeIdx;
                }
                return elem->parent;
            }
        }

        /// <summary>
        /// Return the node that is immediately to the left of the given node. In other words,
        /// the first node with a "greater" value than the given node
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public int GetFirstRightEntry(int nodeIdx)
        {
            if (nodeIdx < 0) return nodeIdx;

            var basePtr = (TreeNode*)this.elements.GetUnsafePtr();
            var elem = basePtr + nodeIdx;
            if (elem->right >= 0)
            {
                nodeIdx = elem->right;
                elem = basePtr + nodeIdx;
                while (elem->left >= 0)
                {
                    nodeIdx = elem->left;
                    elem = basePtr + nodeIdx;
                }
                return nodeIdx;
            }
            else
            {
                while (elem->parent >= 0 && (basePtr + elem->parent)->left != nodeIdx)
                {
                    nodeIdx = elem->parent;
                    elem = basePtr + nodeIdx;
                }
                return elem->parent;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns>The index of the node where the value is stored</returns>
        public int GetElementIdx(T value) => this.Search(value);

        private int Search(T value)
        {
            int cmp;

            var basePtr = (TreeNode*)this.elements.GetUnsafePtr();
            var nodePtr = basePtr + this.root;
            int node = this.root;

            while(node >= 0 && (cmp = this.comparer.Compare(value, nodePtr->value)) != 0)
            {
                node = cmp < 0 ? nodePtr->left : nodePtr->right;
                nodePtr = basePtr + node;
            }
            return node;
        }


        public bool IsEmpty()
        {
            return this.root < 0;
        }

        public void Clear()
        {
            this.elements.Clear();
            this.free.Clear();
            this.root = -1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns>True if the value is contained in the sorted tree (depending on the implementation of the comparer this
        /// might not always be the case)</returns>
        public bool Contains(T value)
        {
            return this.Search(value) >= 0;
        }

        private int RotateLeft(int parent, int child)
        {
            var childElem = this.elements[child];
            var parentElem = this.elements[parent];

            int leftGrandchild = childElem.left;
            parentElem.right = leftGrandchild;
            if(leftGrandchild >= 0)
            {
                var leftGrandchildElem = this.elements[leftGrandchild];
                leftGrandchildElem.parent = parent;
                this.elements[leftGrandchild] = leftGrandchildElem;
            }
            childElem.left = parent;
            parentElem.parent = child;

            if(childElem.balance == 0)
            {
                parentElem.balance = 1;
                childElem.balance = -1;
            } else
            {
                parentElem.balance = 0;
                childElem.balance = 0;
            }

            this.elements[child] = childElem;
            this.elements[parent] = parentElem;

            return child;
        }

        private int RotateRight(int parent, int child)
        {
            var childElem = this.elements[child];
            var parentElem = this.elements[parent];

            int rightGrandchild = childElem.right;
            parentElem.left = rightGrandchild;
            if(rightGrandchild >= 0)
            {
                var rightGrandchildElem = this.elements[rightGrandchild];
                rightGrandchildElem.parent = parent;
                this.elements[rightGrandchild] = rightGrandchildElem;
            }
            childElem.right = parent;
            parentElem.parent = child;

            if(childElem.balance == 0)
            {
                parentElem.balance = -1;
                childElem.balance = 1;
            } else
            {
                parentElem.balance = 0;
                childElem.balance = 0;
            }

            this.elements[child] = childElem;
            this.elements[parent] = parentElem;

            return child;
        }

        private int RotateRightLeft(int parent, int child)
        {
            var childElem = this.elements[child];
            var parentElem = this.elements[parent];

            int grandchild = childElem.left;

            var grandchildElem = this.elements[grandchild];

            int rightGrandgrandchild = grandchildElem.right;

            childElem.left = rightGrandgrandchild;
            if(rightGrandgrandchild >= 0)
            {
                var rightGrandgrandchildElem = this.elements[rightGrandgrandchild];
                rightGrandgrandchildElem.parent = child;
                this.elements[rightGrandgrandchild] = rightGrandgrandchildElem;
            }
            grandchildElem.right = child;
            childElem.parent = grandchild;

            int leftGrandgrandchild = grandchildElem.left;
            parentElem.right = leftGrandgrandchild;
            if(leftGrandgrandchild >= 0)
            {
                var leftGrandgrandchildElem = this.elements[leftGrandgrandchild];
                leftGrandgrandchildElem.parent = parent;
                this.elements[leftGrandgrandchild] = leftGrandgrandchildElem;
            }
            grandchildElem.left = parent;
            parentElem.parent = grandchild;

            if(grandchildElem.balance == 0)
            {
                parentElem.balance = 0;
                childElem.balance = 0;

            } else
            {
                if(grandchildElem.balance > 0)
                {
                    parentElem.balance = -1;
                    childElem.balance = 0;
                } else
                {
                    parentElem.balance = 0;
                    childElem.balance = 1;
                }
            }
            grandchildElem.balance = 0;

            this.elements[child] = childElem;
            this.elements[parent] = parentElem;
            this.elements[grandchild] = grandchildElem;

            return grandchild;
        }

        private int RotateLeftRight(int parent, int child)
        {
            var childElem = this.elements[child];
            var parentElem = this.elements[parent];

            int grandchild = childElem.right;

            var grandchildElem = this.elements[grandchild];

            int leftGrandgrandchild = grandchildElem.left;
            childElem.right = leftGrandgrandchild;
            if (leftGrandgrandchild >= 0)
            {
                var leftGrandgrandchildElem = this.elements[leftGrandgrandchild];
                leftGrandgrandchildElem.parent = child;
                this.elements[leftGrandgrandchild] = leftGrandgrandchildElem;
            }
            grandchildElem.left = child;
            childElem.parent = grandchild;

            int rightGrandgrandchild = grandchildElem.right;
            parentElem.left = rightGrandgrandchild;
            if (rightGrandgrandchild >= 0)
            {
                var rightGrandgrandchildElem = this.elements[rightGrandgrandchild];
                rightGrandgrandchildElem.parent = parent;
                this.elements[rightGrandgrandchild] = rightGrandgrandchildElem;
            }
            grandchildElem.right = parent;
            parentElem.parent = grandchild;

            if (grandchildElem.balance == 0)
            {
                parentElem.balance = 0;
                childElem.balance = 0;

            }
            else
            {
                if (grandchildElem.balance < 0)
                {
                    parentElem.balance = 1;
                    childElem.balance = 0;
                }
                else
                {
                    parentElem.balance = 0;
                    childElem.balance = -1;
                }
            }
            grandchildElem.balance = 0;

            this.elements[child] = childElem;
            this.elements[parent] = parentElem;
            this.elements[grandchild] = grandchildElem;

            return grandchild;
        }

        private void ShiftNodes(int parent, int child)
        {
            var parentElem = this.elements[parent];
            if (parentElem.parent < 0)
            {
                this.root = child;
            }
            else
            {
                var parentParentElem = this.elements[parentElem.parent];
                if (parent == parentParentElem.left)
                {
                    parentParentElem.left = child;
                }
                else
                {
                    parentParentElem.right = child;
                }
                this.elements[parentElem.parent] = parentParentElem;
            }

            if (child >= 0)
            {
                var childElem = this.elements[child];
                childElem.parent = parentElem.parent;
                this.elements[child] = childElem;
            }
        }

        private int MinNode(int node)
        {
            int minNodeIdx = node;
            while(this.elements[minNodeIdx].left >= 0)
            {
                minNodeIdx = this.elements[minNodeIdx].left;
            }
            return minNodeIdx;
        }

        private int GetSuccessor(int node)
        {
            var elem = this.elements[node];
            if(elem.right >= 0)
            {
                var rightNode = this.elements[node].right;
                rightNode = MinNode(rightNode);
                return rightNode;
            }
            var parent = elem.parent;
            while(parent >= 0 && node == this.elements[parent].right)
            {
                node = parent;
                parent = this.elements[parent].parent;
            }
            return parent;
        }

        private void RemoveInternal(int node)
        {
            var nodeElem = this.elements[node];
            int childIdx = node;

            bool wasLeft = nodeElem.parent >= 0 && node == this.elements[nodeElem.parent].left;
            if (nodeElem.left < 0)
            {
                this.ShiftNodes(node, nodeElem.right);
                this.free.Add(node);
            }
            else if (nodeElem.right < 0)
            {
                this.ShiftNodes(node, nodeElem.left);
                this.free.Add(node);
            }
            else
            {
                var successor = this.GetSuccessor(node);
                var successorElem = this.elements[successor];
                var rmvTreeNode = new TreeNode()
                {
                    left = successorElem.left,
                    right = successorElem.right,
                    parent = successorElem.parent
                };
                wasLeft = successorElem.parent >= 0 && this.elements[successorElem.parent].left == successor;

                if (successorElem.parent != node)
                {
                    this.ShiftNodes(successor, successorElem.right);
                    successorElem = this.elements[successor];
                    successorElem.right = nodeElem.right;
                    var successorRight = this.elements[successorElem.right];
                    successorRight.parent = successor;
                    this.elements[successorElem.right] = successorRight;
                    this.elements[successor] = successorElem;
                }
                this.ShiftNodes(node, successor);
                successorElem = this.elements[successor];
                successorElem.left = nodeElem.left;
                var successorLeft = this.elements[successorElem.left];
                successorLeft.parent = successor;
                this.elements[successorElem.left] = successorLeft;
                successorElem.balance = nodeElem.balance;
                this.elements[successor] = successorElem;

                if (rmvTreeNode.parent == node) { rmvTreeNode.parent = successor; }

                this.free.Add(node);
                nodeElem = rmvTreeNode;
                childIdx = int.MinValue;
            }

            int iteration = 0;
            int grandParent, rotatedNode;
            while (nodeElem.parent >= 0)
            {
                int nodeIdx = nodeElem.parent;
                nodeElem = this.elements[nodeIdx];
                grandParent = nodeElem.parent;

                int balance = nodeElem.balance;
                int siblingBalance = 0;

                if (nodeElem.left < 0 && nodeElem.right < 0)
                {
                    nodeElem.balance = 0;
                    this.elements[nodeIdx] = nodeElem;
                    iteration++;
                    childIdx = nodeIdx;
                    continue;
                }
                else if (childIdx == nodeElem.left || (iteration == 0 && wasLeft))
                {
                    if (balance > 0)
                    {
                        var sibling = nodeElem.right;
                        var siblingElem = this.elements[sibling];
                        siblingBalance = siblingElem.balance;
                        if (siblingBalance < 0)
                        {
                            rotatedNode = this.RotateRightLeft(nodeIdx, sibling);
                        }
                        else
                        {
                            rotatedNode = this.RotateLeft(nodeIdx, sibling);
                        }
                    }
                    else
                    {
                        if (balance == 0)
                        {
                            nodeElem.balance = 1;
                            this.elements[nodeIdx] = nodeElem;
                            break;
                        }
                        nodeElem.balance = 0;
                        this.elements[nodeIdx] = nodeElem;
                        iteration++;
                        childIdx = nodeIdx;
                        continue;
                    }
                }
                else
                {
                    if (balance < 0)
                    {
                        var sibling = nodeElem.left;
                        var siblingElem = this.elements[sibling];
                        siblingBalance = siblingElem.balance;
                        if (siblingBalance > 0)
                        {
                            rotatedNode = this.RotateLeftRight(nodeIdx, sibling);
                        }
                        else
                        {
                            rotatedNode = this.RotateRight(nodeIdx, sibling);
                        }

                    }
                    else
                    {
                        if (balance == 0)
                        {
                            nodeElem.balance = -1;
                            this.elements[nodeIdx] = nodeElem;
                            break;
                        }
                        nodeElem.balance = 0;
                        this.elements[nodeIdx] = nodeElem;
                        iteration++;
                        childIdx = nodeIdx;
                        continue;
                    }
                }

                var rotatedElem = this.elements[rotatedNode];
                rotatedElem.parent = grandParent;
                this.elements[rotatedNode] = rotatedElem;
                if (grandParent >= 0)
                {
                    var grandParentElem = this.elements[grandParent];
                    if (nodeIdx == grandParentElem.left)
                    {
                        grandParentElem.left = rotatedNode;
                    }
                    else
                    {
                        grandParentElem.right = rotatedNode;
                    }
                    this.elements[grandParent] = grandParentElem;
                }
                else
                {
                    this.root = rotatedNode;
                }
                childIdx = rotatedNode;

                if (siblingBalance == 0) break;

                iteration++;
            }
        }

        /// <summary>
        /// Removes the node with the given array index
        /// </summary>
        /// <param name="node"></param>
        /// <returns>False, if the index is outside the range of the array. True otherwise. Behaviour is undefined when removing a
        /// node that is marked as free.</returns>
        public bool RemoveNode(int node)
        {
            bool rmvSuccess = node >= 0 && node < this.elements.Length;
            if(rmvSuccess)
            {
                this.RemoveInternal(node);
            }
            return rmvSuccess;
        }

        /// <summary>
        /// Calculates a code that represents the position of the node with the value when looking at the tree in a left-right
        /// direction. The lowest code is the leftmost node, and the highest code the rightmost. 0.5 represents the root.
        /// This is useful, if you want to know from a given set of values, which one is the one most to the left in the tree etc.
        /// (which is information you sometimes need for sweepline algorithms)
        /// </summary>
        /// <param name="value"></param>
        /// <param name="idx"></param>
        /// <returns></returns>
        public float GetTreeCode(T value, out int idx)
        {
            idx = this.RootIdx;
            float val = 0.25f;

            int level = 1;

            var basePtr =(TreeNode*)this.elements.GetUnsafePtr();
            var currentElem = basePtr + idx;

            int cmp;
            while ((cmp = this.comparer.Compare(currentElem->value, value)) != 0)
            {
                if(cmp >= 0)
                {
                    val -= 1.0f / (float)(1 << (level + 1));
                    idx = currentElem->left;
                } else
                {
                    val += 1.0f / (float)(1 << (level + 1));
                    idx = currentElem->right;
                }

                if (idx < 0) break;

                currentElem = basePtr + idx; 

                level++;
            }
            return val * 2;
        }

        /// <summary>
        /// Removes the node which contains the given value from the tree.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>True, if the element was found in the tree and removed. False, if the elements was not found</returns>
        public bool Remove(T value)
        {
            var node = this.Search(value);
            bool rmvSuccess = node >= 0;

            if(rmvSuccess)
            {
                this.RemoveInternal(node);
            }
            return rmvSuccess;
        }

        /// <summary>
        /// Inserts a new node into the tree containing the given value
        /// </summary>
        /// <param name="value"></param>
        public void Insert(T value)
        {
            int node = this.root, previousNode = -1;

            int cmp = 0;
            while(node >= 0)
            {
                previousNode = node;
                var element = this.elements[node];
                cmp = this.comparer.Compare(value, element.value);
                node = cmp < 0 ? element.left : element.right;
            }

            TreeNode newNode = new TreeNode()
            {
                parent = previousNode,
                balance = 0,
                left = -1,
                right = -1,
                value = value
            };

            int newNodePtr = -1;
            //Save new element so it is not garbage collected and has a fixed adress xD
            if (this.free.Length > 0)
            {
                int freeIdx = this.free[this.free.Length - 1];
                this.free.Length--;

                newNode.arrIdx = freeIdx;
                this.elements[freeIdx] = newNode;
                newNodePtr = freeIdx;
            }
            else
            {
                newNode.arrIdx = this.elements.Length;
                this.elements.Add(newNode);
                newNodePtr = this.elements.Length - 1;
            }

            if(previousNode == -1)
            {
                this.root = newNodePtr;
            } else
            {
                var prevElem = this.elements[previousNode];
                if(cmp < 0)
                {
                    prevElem.left = newNodePtr;
                } else
                {
                    prevElem.right = newNodePtr;
                }
                this.elements[previousNode] = prevElem;
            }

            int child = -1;
            int parent = newNodePtr;

            while (this.elements[parent].parent >= 0)
            {
                child = parent;
                parent = this.elements[parent].parent;

                var childElem = this.elements[child];
                var parentElem = this.elements[parent];
                int grandParent = parentElem.parent;
                int rotatedNode;



                int balance = parentElem.balance;

                if (child == parentElem.right)
                {
                    if (balance > 0)
                    {
                        int childBalance = childElem.balance;
                        if (childBalance < 0)
                        {
                            rotatedNode = this.RotateRightLeft(parent, child);
                        }
                        else
                        {
                            rotatedNode = this.RotateLeft(parent, child);
                        }
                    }
                    else
                    {
                        if (balance < 0)
                        {
                            parentElem.balance = 0;
                            this.elements[parent] = parentElem;
                            break;
                        }
                        parentElem.balance = 1;
                        this.elements[parent] = parentElem;
                        continue;
                    }

                }
                else
                {
                    if (balance < 0)
                    {
                        int childBalance = childElem.balance;
                        if (childBalance > 0)
                        {
                            rotatedNode = this.RotateLeftRight(parent, child);

                        } else
                        {
                            rotatedNode = this.RotateRight(parent, child);
                        }

                    }
                    else
                    {
                        if (balance > 0)
                        {
                            parentElem.balance = 0;
                            this.elements[parent] = parentElem;
                            break;
                        }
                        parentElem.balance = -1;
                        this.elements[parent] = parentElem;
                        continue;
                    }
                }

                var rotatedElem = this.elements[rotatedNode];
                rotatedElem.parent = grandParent;
                this.elements[rotatedNode] = rotatedElem;

                if(grandParent >= 0)
                {
                    var grandParentElem = this.elements[grandParent];
                    if(parent == grandParentElem.left)
                    {
                        grandParentElem.left = rotatedNode;
                    } else
                    {
                        grandParentElem.right = rotatedNode;
                    }
                    this.elements[grandParent] = grandParentElem;
                } else
                {
                    this.root = rotatedNode;
                }

                break;
            }
        }
    }
}
