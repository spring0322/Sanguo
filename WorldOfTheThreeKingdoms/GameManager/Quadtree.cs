using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GameObjects;
using GameObjects.TroopDetail;

namespace GameManager
{
    /// <summary>
    /// Quadtree spatial partitioning system for efficient object culling and collision detection
    /// </summary>
    public class Quadtree
    {
        // Use simple constants for now to avoid dependency issues
        private const int MAX_OBJECTS = 10; // Maximum objects per node before splitting
        private const int MAX_LEVELS = 5;   // Maximum tree depth

        private int _level;
        private List<Troop> _objects;
        private Rectangle _bounds;
        private Quadtree[] _nodes;

        public Quadtree(int level, Rectangle bounds)
        {
            _level = level;
            _bounds = bounds;
            _objects = new List<Troop>();
            _nodes = new Quadtree[4];
        }

        /// <summary>
        /// Clear the quadtree (used for frame reset with dynamic objects)
        /// </summary>
        public void Clear()
        {
            _objects.Clear();
            for (int i = 0; i < _nodes.Length; i++)
            {
                if (_nodes[i] != null)
                {
                    _nodes[i].Clear();
                    _nodes[i] = null;
                }
            }
        }

        /// <summary>
        /// Split the node into four quadrants
        /// </summary>
        private void Split()
        {
            int subWidth = _bounds.Width / 2;
            int subHeight = _bounds.Height / 2;
            int x = _bounds.X;
            int y = _bounds.Y;

            // Create four child nodes: NE, NW, SW, SE
            _nodes[0] = new Quadtree(_level + 1, new Rectangle(x + subWidth, y, subWidth, subHeight)); // NE
            _nodes[1] = new Quadtree(_level + 1, new Rectangle(x, y, subWidth, subHeight));             // NW
            _nodes[2] = new Quadtree(_level + 1, new Rectangle(x, y + subHeight, subWidth, subHeight)); // SW
            _nodes[3] = new Quadtree(_level + 1, new Rectangle(x + subWidth, y + subHeight, subWidth, subHeight)); // SE
        }

        /// <summary>
        /// Determine which quadrant an object belongs to
        /// </summary>
        /// <param name="pRect">Object bounds</param>
        /// <returns>Quadrant index, or -1 if object spans multiple quadrants</returns>
        private int GetIndex(Rectangle pRect)
        {
            int index = -1;
            double verticalMidpoint = _bounds.X + (_bounds.Width / 2.0);
            double horizontalMidpoint = _bounds.Y + (_bounds.Height / 2.0);

            bool topQuadrant = (pRect.Y < horizontalMidpoint && pRect.Y + pRect.Height < horizontalMidpoint);
            bool bottomQuadrant = (pRect.Y > horizontalMidpoint);

            if (pRect.X < verticalMidpoint && pRect.X + pRect.Width < verticalMidpoint)
            {
                if (topQuadrant) index = 1;      // NW
                else if (bottomQuadrant) index = 2; // SW
            }
            else if (pRect.X > verticalMidpoint)
            {
                if (topQuadrant) index = 0;      // NE
                else if (bottomQuadrant) index = 3; // SE
            }

            return index;
        }

        /// <summary>
        /// Insert a troop into the quadtree
        /// </summary>
        /// <param name="troop">Troop to insert</param>
        public void Insert(Troop troop)
        {
            // Get troop bounds for spatial partitioning
            Rectangle troopBounds = GetTroopBounds(troop);

            // If we have child nodes, try to insert into appropriate child
            if (_nodes[0] != null)
            {
                int index = GetIndex(troopBounds);
                if (index != -1)
                {
                    _nodes[index].Insert(troop);
                    return;
                }
            }

            // Add to current node
            _objects.Add(troop);

            // If we have too many objects and haven't reached max depth, split
            if (_objects.Count > MAX_OBJECTS && _level < MAX_LEVELS)
            {
                if (_nodes[0] == null) Split();

                int i = 0;
                while (i < _objects.Count)
                {
                    Rectangle objBounds = GetTroopBounds(_objects[i]);
                    int index = GetIndex(objBounds);
                    if (index != -1)
                    {
                        Troop t = _objects[i];
                        _objects.RemoveAt(i);
                        _nodes[index].Insert(t);
                    }
                    else
                    {
                        i++;
                    }
                }
            }
        }

        /// <summary>
        /// Retrieve all objects that could potentially intersect with the given rectangle
        /// </summary>
        /// <param name="returnObjects">List to populate with results</param>
        /// <param name="rect">Query rectangle (usually camera viewport)</param>
        public void Retrieve(List<Troop> returnObjects, Rectangle rect)
        {
            int index = GetIndex(rect);
            
            if (index != -1 && _nodes[0] != null)
            {
                // Query fits entirely in one quadrant
                _nodes[index].Retrieve(returnObjects, rect);
            }
            else if (_nodes[0] != null)
            {
                // Query spans multiple quadrants, check all children
                for (int i = 0; i < 4; i++)
                {
                    _nodes[i].Retrieve(returnObjects, rect);
                }
            }

            // Add objects from current node
            returnObjects.AddRange(_objects);
        }

        /// <summary>
        /// Get bounds rectangle for a troop based on its position and size
        /// </summary>
        /// <param name="troop">Troop to get bounds for</param>
        /// <returns>Rectangle representing troop's spatial bounds</returns>
        private Rectangle GetTroopBounds(Troop troop)
        {
            // Convert troop tile position to world coordinates
            // Using standard tile size (60x40 pixels)
            const int tileWidth = 60;
            const int tileHeight = 40;
            
            int worldX = troop.Position.X * tileWidth;
            int worldY = troop.Position.Y * tileHeight;
            
            // Use troop's actual size if available, otherwise use tile size
            int width = tileWidth;
            int height = tileHeight;
            
            return new Rectangle(worldX, worldY, width, height);
        }

        /// <summary>
        /// Get debug information about the quadtree
        /// </summary>
        /// <returns>String containing tree statistics</returns>
        public string GetDebugInfo()
        {
            int totalNodes = CountNodes();
            int totalObjects = CountObjects();
            return $"Quadtree - Nodes: {totalNodes}, Objects: {totalObjects}, Max Level: {_level}";
        }

        private int CountNodes()
        {
            int count = 1; // Current node
            if (_nodes[0] != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    count += _nodes[i].CountNodes();
                }
            }
            return count;
        }

        private int CountObjects()
        {
            int count = _objects.Count;
            if (_nodes[0] != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    count += _nodes[i].CountObjects();
                }
            }
            return count;
        }
    }
}