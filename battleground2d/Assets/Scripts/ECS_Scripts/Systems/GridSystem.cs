using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using Unity.Physics;
using static DebugStream;
using System;
using UnityEngine.Experimental.XR;
using Unity.Collections;
using Unity.Entities.UniversalDelegates;
using System.Linq;
using static UnityEngine.ParticleSystem;
using UnityEditor.U2D.Path;
using System.Collections.Generic;

[UpdateInGroup(typeof(Unity.Entities.SimulationSystemGroup))]
[UpdateBefore(typeof(QuadrantSystem))]
[BurstCompile]
public class GridSystem : SystemBase
{
    private static readonly float2 mapSize = new float2(1000f, 1000f); // Example size of the map
    private static readonly int gridSize = 1000;  // 100x100 grid
    private static readonly float2 divisionSize = mapSize / gridSize;

    protected override void OnUpdate()
    {
        if (GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
            return;
        //draw rectangle and store first qaudtree
        var boundary = new Rectangle(500f, 500f, 1000f, 1000f);
        var qtree = new QuadTree(boundary, 4);


        EntityQuery query = GetEntityQuery(typeof(Translation), typeof(GridID));
        //NativeArray<GridID> gridIds = query.ToComponentDataArray<GridID>(Allocator.TempJob);
        //NativeArray<Translation> translations = query.ToComponentDataArray<Translation>(Allocator.TempJob);
        //Particle[] particles = new Particle[translations.Count()];

        ////loop through units(particles)
        ////  insert unit into quadtree
        ////foreach (var p in translations)
        //for (var i = 0; i < translations.Count(); i++)
        //{
        //    if (i == 49)
        //    {
        //        var lol = 49;
        //    }
        //    var p = translations[i];
        //    var particle = new Particle { x = p.Value.x, y = p.Value.y };
        //    particles[i] = particle;
        //    var point = new Point(p.Value.x, p.Value.y, particle);
        //    qtree.insert(point);
        //}

        ////loop through units in physics?
        ////foreach (var p in translations)
        //for (var i = 0; i < particles.Count(); i++)
        //{

        //    var p = particles[i];
        //    var range = new Circle(p.x, p.y, 0.125f * 2);
        //    var points = qtree.query(range, null);
        //    foreach(var point in points)
        //    {
        //        var other = point.userData;
        //        // for (let other of particles) {
        //        if (!p.Equals(other) && p.Intersects(other))
        //        {
        //            //p.setHighlight(true);

        //        }
        //    }
        //}

        // Iterate through all entities with Translation and GridID components
        Dependency = Entities.ForEach((ref Translation translation, ref GridID grid) =>
        {
            // Find the grid cell index based on the entity's position
            int gridX = Mathf.FloorToInt(translation.Value.x / divisionSize.x);
            int gridY = Mathf.FloorToInt(translation.Value.y / divisionSize.y);

            // Ensure the grid values are clamped to prevent index out-of-range errors
            gridX = math.clamp(gridX, 0, gridSize - 1);
            gridY = math.clamp(gridY, 0, gridSize - 1);

            // Calculate the grid ID (1-based index for better readability, optional)
            int gridId = gridY * gridSize + gridX;

            // Update the GridID component with the calculated grid ID
            grid.Value = gridId;


        }).WithBurst().ScheduleParallel(Dependency);

        
    }
}

// Create a simple GridID component
public struct GridID : IComponentData
{
    public int Value;

    public Entity otherEntity;
}

//based on my html/js collision quad tree example
//need rectangle to store our quadtree
//points are my units
//

public interface IShape
{
    bool Contains(Point point);
    bool Intersects(Rectangle range);
    bool Intersects(Circle range);
}

public struct Rectangle : IShape
{
    public float x;
    public float y;
    public float w;
    public float h;

    public Rectangle(float x, float y, float w, float h)
    {
        this.x = x;
        this.y = y;
        this.w = w;
        this.h = h;
    }

    public bool Contains(Point point)
    {
        return (point.x >= this.x - this.w &&
            point.x <= this.x + this.w &&
            point.y >= this.y - this.h &&
            point.y <= this.y + this.h
        );
    }

    public bool Intersects(Rectangle range)
    {
        return !(range.x - range.w > this.x + this.w ||
            range.x + range.w < this.x - this.w ||
            range.y - range.h > this.y + this.h ||
            range.y + range.h < this.y - this.h);
    }

    public bool Intersects(Circle range)
    {
        throw new NotImplementedException();
    }
}

public struct Circle : IShape
{
    public float x;
    public float y;
    public float r;
    public float rSquared;

    public Circle(float x, float y, float r)
    {
        this.x = x;
        this.y = y;
        this.r = r;
        this.rSquared = this.r * this.r;
    }

    public bool Contains(Point point)
    {
        float d = Mathf.Pow((point.x - this.x), 2) + Mathf.Pow((point.y - this.y), 2);
        return d <= this.rSquared;
    }

    public bool Intersects(Rectangle range)
    {
        float xDist = Mathf.Abs(range.y - this.x);
        float yDist = Mathf.Abs(range.y - this.y);

        float r = this.r;

        float w = range.w / 2;
        float h = range.h / 2;

        float edges = Mathf.Pow((xDist - w), 2) + Mathf.Pow((yDist - h), 2);

        //no intersection
        if (xDist > (r + w) || yDist > (r + h))
            return false;
        // intersection within the circle 
        if (xDist <= w || yDist <= h)
            return true;
        // intersection on the edge of the circle
        return edges <= this.rSquared;
    }

    public bool Intersects(Circle range)
    {
        throw new NotImplementedException();
    }
}


/// <summary>
/// this is my unit data
/// </summary>
public struct Particle
{
    public float x;
    public float y;
    public int r;
    public bool highlight;

    public Particle(float x, float y)
    {
        this.x = x;
        this.y = y;
        this.r = 4;
        this.highlight = false;
    }

    public bool Intersects(Particle other)
    {
        float dx = this.x - other.x;
        float dy = this.y - other.y;
        float distance = Mathf.Sqrt(dx * dx + dy * dy);
        return (distance < this.r + other.r);
    }
}

public struct Point
{
    public float x;
    public float y;
    public Particle userData;

    public Point(float x, float y, Particle userData)
    {
        this.x = x;
        this.y = y;
        this.userData = userData;

    }
}

public class QuadTree
{
    public Rectangle boundary;
    public int capacity;
    public Point[] points;
    public bool divided;
    public QuadTree northeast;
    public QuadTree northwest;
    public QuadTree southeast;
    public QuadTree southwest;


    public QuadTree(Rectangle boundary, int n)
    {
        this.boundary = boundary;
        this.capacity = n;
        this.points = new Point[4];
        this.divided = false;
    }

    public void subdivide()
    {
        float x = this.boundary.x;
        float y = this.boundary.y;
        float w = this.boundary.w;
        float h = this.boundary.h;


        Rectangle ne = new Rectangle(x + w / 2, y - h / 2, w / 2, h / 2);
        this.northeast = new QuadTree(ne, this.capacity);

        Rectangle nw = new Rectangle(x - w / 2, y - h / 2, w / 2, h / 2);
        this.northwest = new QuadTree(nw, this.capacity);

        Rectangle se = new Rectangle(x + w / 2, y + h / 2, w / 2, h / 2);
        this.southeast = new QuadTree(se, this.capacity);

        Rectangle sw = new Rectangle(x - w / 2, y + h / 2, w / 2, h / 2);
        this.southwest = new QuadTree(sw, this.capacity);
        this.divided = true;
    }

    public bool insert(Point point)
    {
        if (!this.boundary.Contains(point))
        {
            return false;
        }

        if (this.points.Count() < this.capacity)
        {
            this.points[this.points.Count() - 1] = point;
            return true;
        }
        else
        {
            if (!this.divided)
            {
                this.subdivide();
            }

            if (this.northeast.insert(point))
            {
                return true;
            }
            else if (this.northwest.insert(point))
            {
                return true;
            }
            else if (this.southeast.insert(point))
            {
                return true;
            }
            else if (this.southwest.insert(point))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public List<Point> query(Rectangle range, List<Point> found)
    {
        if (!(found == null))
        {
            found = new List<Point>();
        }
        if (!this.boundary.Intersects(range))
        {
            return null;
        }
        else
        {
            foreach (var p in this.points)
            {
                if (range.Contains(p))
                {
                    found.Add(p);
                }
            }
        }
        if (this.divided)
        {
            this.northwest.query(range, found);
            this.northeast.query(range, found);
            this.southwest.query(range, found);
            this.southeast.query(range, found);
        }
        return found;
    }


    public List<Point> query(Circle range, List<Point> found)
    {
        if (!(found == null))
        {
            found = new List<Point>();
        }
        if (!this.boundary.Intersects(range))
        {
            return null;
        }
        else
        {
            foreach (var p in this.points)
            {
                if (range.Contains(p))
                {
                    found.Add(p);
                }
            }
        }
        if (this.divided)
        {
            this.northwest.query(range, found);
            this.northeast.query(range, found);
            this.southwest.query(range, found);
            this.southeast.query(range, found);
        }
        return found;
    }

}