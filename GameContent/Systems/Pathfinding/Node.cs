using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.GameContent.Systems.Pathfinding;

public class Node
{
    public bool filled, impassable, unPathable, hasBeenUsed, isViewable;
    public float fScore, cost, currentDist;
    public Vector2 parent, pos;

    public Node()
    {
        filled = false;
        impassable = false;
        unPathable = false;
        hasBeenUsed = false;
        isViewable = false;
        cost = 1.0f;
    }
    public Node(float cost, bool filled)
    {
        this.cost = cost;
        this.filled = filled;
    }

    public Node(Vector2 pos, float cost, bool filled, float fScore)
    {
        this.cost = cost;
        this.filled = filled;
        unPathable = false;
        hasBeenUsed = false;
        isViewable = false;

        this.pos = pos;

        this.fScore = fScore;
    }

    public void SetNode(Vector2 parent, float fScore, float currentDist)
    {
        this.parent = parent;
        this.fScore = fScore;
        this.currentDist = currentDist;
    }

    public virtual void SetToFilled(bool impassible)
    {
        filled = true;
        this.impassable = impassible;
    }
}
