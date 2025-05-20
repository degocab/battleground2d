//using Unity.Entities;
//using Unity.Mathematics;
//using Unity.Collections;
//using System;

//public struct QuadTreeEntry
//{
//    public Entity entity;
//    public float2 position;
//}

//public struct QuadTreeNode
//{
//    public float2 center;
//    public float2 halfExtent;  // Half the width/height of the node
//    public NativeList<QuadTreeEntry> entries;
//    public QuadTreeNode? northWest;
//    public QuadTreeNode? northEast;
//    public QuadTreeNode? southWest;
//    public QuadTreeNode? southEast;
//}

//public struct QuadTree
//{
//    private QuadTreeNode root;
//    private const int maxEntriesPerNode = 4;

//    // Initialize the root node
//    public void Initialize(float2 center, float2 halfExtent)
//    {
//        root.center = center;
//        root.halfExtent = halfExtent;
//        root.entries = new NativeList<QuadTreeEntry>(Allocator.Persistent);
//        root.northWest = null;
//        root.northEast = null;
//        root.southWest = null;
//        root.southEast = null;
//    }

//    // Insert an entity into the QuadTree
//    public void Insert(Entity entity, float2 position)
//    {
//        InsertEntity(ref root, entity, position);
//    }

//    private void InsertEntity(ref QuadTreeNode node, Entity entity, float2 position)
//    {
//        // If the node's bounding box doesn't contain the point, return early
//        if (!IsWithinBounds(node, position))
//            return;

//        // If this node has space, add the entity here
//        if (node.entries.Length < maxEntriesPerNode)
//        {
//            node.entries.Add(new QuadTreeEntry { entity = entity, position = position });
//        }
//        else
//        {
//            // Otherwise, split the node into 4 children if it hasn't been done yet
//            if (!node.northWest.HasValue)
//                SplitNode(ref node);

//            // Recursively insert into the appropriate quadrant
//            InsertEntityIntoChildNodes(ref node, entity, position);
//        }
//    }

//    // Split a node into 4 child nodes
//    private void SplitNode(ref QuadTreeNode node)
//    {
//        float2 offset = node.halfExtent / 2f;

//        node.northWest = new QuadTreeNode
//        {
//            center = node.center + new float2(-offset.x, offset.y),
//            halfExtent = offset,
//            entries = new NativeList<QuadTreeEntry>(Allocator.Persistent)
//        };

//        node.northEast = new QuadTreeNode
//        {
//            center = node.center + new float2(offset.x, offset.y),
//            halfExtent = offset,
//            entries = new NativeList<QuadTreeEntry>(Allocator.Persistent)
//        };

//        node.southWest = new QuadTreeNode
//        {
//            center = node.center + new float2(-offset.x, -offset.y),
//            halfExtent = offset,
//            entries = new NativeList<QuadTreeEntry>(Allocator.Persistent)
//        };

//        node.southEast = new QuadTreeNode
//        {
//            center = node.center + new float2(offset.x, -offset.y),
//            halfExtent = offset,
//            entries = new NativeList<QuadTreeEntry>(Allocator.Persistent)
//        };

//        // Now redistribute entries from the current node into the children
//        foreach (var entry in node.entries)
//        {
//            InsertEntityIntoChildNodes(ref node, entry.entity, entry.position);
//        }

//        node.entries.Clear(); // Clear the entries in the parent node since they were redistributed
//    }

//    private void InsertEntityIntoChildNodes(ref QuadTreeNode node, Entity entity, float2 position)
//    {
//        // Insert into northWest
//        if (!node.northWest.HasValue)
//        {
//            node.northWest = CreateChildNode(node, -1, 1);
//        }
//        InsertEntityIntoNode(node.northWest.Value, entity, position);

//        // Insert into northEast
//        if (!node.northEast.HasValue)
//        {
//            node.northEast = CreateChildNode(node, 1, 1);
//        }
//        InsertEntityIntoNode(node.northEast.Value, entity, position);

//        // Insert into southWest
//        if (!node.southWest.HasValue)
//        {
//            node.southWest = CreateChildNode(node, -1, -1);
//        }
//        InsertEntityIntoNode(node.southWest.Value, entity, position);

//        // Insert into southEast
//        if (!node.southEast.HasValue)
//        {
//            node.southEast = CreateChildNode(node, 1, -1);
//        }
//        InsertEntityIntoNode(node.southEast.Value, entity, position);
//    }

//    // This function will insert the entity into a node. This does not modify child nodes, only the node's entries.
//    private void InsertEntityIntoNode(QuadTreeNode node, Entity entity, float2 position)
//    {
//        // If the node's bounding box doesn't contain the point, return early
//        if (!IsWithinBounds(node, position))
//            return;

//        // If this node has space, add the entity here
//        if (node.entries.Length < maxEntriesPerNode)
//        {
//            node.entries.Add(new QuadTreeEntry { entity = entity, position = position });
//        }
//        else
//        {
//            // Otherwise, split the node into 4 children if it hasn't been done yet
//            if (!node.northWest.HasValue)
//                SplitNode(ref node);

//            // Recursively insert into the appropriate quadrant
//            InsertEntityIntoChildNodes(ref node, entity, position);
//        }
//    }

//    // Helper function to create a new child node
//    private QuadTreeNode CreateChildNode(QuadTreeNode parentNode, int xOffset, int yOffset)
//    {
//        float2 offset = parentNode.halfExtent / 2f;
//        float2 newCenter = parentNode.center + new float2(xOffset * offset.x, yOffset * offset.y);

//        return new QuadTreeNode
//        {
//            center = newCenter,
//            halfExtent = offset,
//            entries = new NativeList<QuadTreeEntry>(Allocator.Persistent)
//        };
//    }


//    // Check if a point is within the bounds of a node
//    private bool IsWithinBounds(QuadTreeNode node, float2 position)
//    {
//        return position.x >= node.center.x - node.halfExtent.x &&
//               position.x <= node.center.x + node.halfExtent.x &&
//               position.y >= node.center.y - node.halfExtent.y &&
//               position.y <= node.center.y + node.halfExtent.y;
//    }

//    // Query for entities within a circular range
//    public void Query(float2 center, float radius, NativeList<Entity> results)
//    {
//        QueryNode(root, center, radius, results);
//    }

//    private void QueryNode(QuadTreeNode node, float2 center, float radius, NativeList<Entity> results)
//    {
//        // If the node doesn't overlap with the query circle, return
//        if (!IsOverlapping(node, center, radius))
//            return;

//        // Check all entries within the node
//        foreach (var entry in node.entries)
//        {
//            if (math.distance(entry.position, center) <= radius)
//            {
//                results.Add(entry.entity);
//            }
//        }

//        // Recursively query child nodes
//        if (node.northWest.HasValue) QueryNode(node.northWest.Value, center, radius, results);
//        if (node.northEast.HasValue) QueryNode(node.northEast.Value, center, radius, results);
//        if (node.southWest.HasValue) QueryNode(node.southWest.Value, center, radius, results);
//        if (node.southEast.HasValue) QueryNode(node.southEast.Value, center, radius, results);
//    }

//    // Check if the node overlaps with a query circle
//    private bool IsOverlapping(QuadTreeNode node, float2 center, float radius)
//    {
//        float2 closestPoint = math.clamp(center, node.center - node.halfExtent, node.center + node.halfExtent);
//        return math.distance(closestPoint, center) <= radius;
//    }

//    // Clear all data
//    public void Clear()
//    {
//        root.entries.Clear();
//        root.entries.Dispose();
//        root.northWest = null;
//        root.northEast = null;
//        root.southWest = null;
//        root.southEast = null;
//    }

//    public QuadTreeNode GetRoot()
//    {
//        return root;
//    }
//}
