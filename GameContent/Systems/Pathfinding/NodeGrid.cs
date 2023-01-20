using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TanksRebirth.GameContent.Systems.Pathfinding;

namespace TopDownShooterPrompt
{
    public class NodeGrid
    {
        public bool ShowGrid;
        public Vector2 SlotDims, GridDims, PhysStart, TotalPhysDims, CurrentHover;

        public List<List<Node>> Slots = new();

        public NodeGrid(Vector2 slotDims, Vector2 startPos, Vector2 totalDims)
        {
            ShowGrid = false;

            this.SlotDims = slotDims;

            //Get all the dims setup
            PhysStart = new Vector2((int)startPos.X, (int)startPos.Y);
            TotalPhysDims = new Vector2((int)(totalDims.X), (int)(totalDims.Y));
            

            CurrentHover = new Vector2(0, 0);

            SetBaseGrid();
        }

        public virtual Vector2 GetPosFromLoc(Vector2 pos)
        {
            return PhysStart + new Vector2((int)pos.X * SlotDims.X, (int)pos.Y * SlotDims.Y);
        }

        public virtual Node GetSlotFromLocation(Vector2 pos)
        {
            if(pos.X >= 0 && pos.Y >= 0 && pos.X < Slots.Count && pos.Y < Slots[(int)pos.X].Count)
            {
                return Slots[(int)pos.X][(int)pos.Y];
            }

            return null;
        }


        public virtual Vector2 GetSlotFromPixel(Vector2 pos, Vector2 offset)
        {
            //This may need a -OFFSET, but meh...
            var adjustedPos = pos - PhysStart + offset;

            var tempVec = new Vector2(Math.Min(Math.Max(0, (int)(adjustedPos.X/SlotDims.X)), Slots.Count-1), Math.Min(Math.Max(0, (int)(adjustedPos.Y/SlotDims.Y)), Slots[0].Count-1));

            return tempVec;
        }



        public virtual void SetBaseGrid()
        {
            GridDims = new Vector2((int)(TotalPhysDims.X/SlotDims.X), (int)(TotalPhysDims.Y/SlotDims.Y));


            Slots.Clear();
            for(int i=0; i<GridDims.X; i++)
            {
                Slots.Add(new List<Node>());
                for(int j=0; j<GridDims.Y; j++)
                {
                    Slots[i].Add(new Node(1, false));
                }
            }
            
        }

        #region A*

        public List<Vector2> GetPath(Vector2 start, Vector2 END, bool ALLOWDIAGNALS)
        {
            List<Node> viewable = new(), used = new();

            List<List<Node>> masterGrid = new();


            bool impassable = false;
            float cost = 1;
            for(int i=0; i<Slots.Count; i++)
            {
                masterGrid.Add(new List<Node>());
                for(int j=0; j<Slots[i].Count; j++)
                {
                    impassable = Slots[i][j].impassable;

                    if(Slots[i][j].impassable || Slots[i][j].filled)
                    {
                        impassable = true;
                    }

                    cost = Slots[i][j].cost;

                    masterGrid[i].Add(new Node(new Vector2(i, j), cost, impassable, 99999999));
                }
            }

            viewable.Add(masterGrid[(int)start.X][(int)start.Y]);

            while (viewable.Count > 0 && !(viewable[0].pos.X == END.X && viewable[0].pos.Y == END.Y)) {
                TestAStarNode(masterGrid, viewable, used, END, ALLOWDIAGNALS);
            }


            List<Vector2> path = new List<Vector2>();

            if (viewable.Count > 0)
            {
                int currentViewableStart = 0;
                Node currentNode = viewable[currentViewableStart];

                path.Clear();
                Vector2 tempPos;


                while (true)
                {

                    //Add the difference between the actual grid and the custom grid back in...
                    tempPos = GetPosFromLoc(currentNode.pos) + SlotDims/2;
                    path.Add(new Vector2(tempPos.X, tempPos.Y));

                    if (currentNode.pos == start)
                        break;
                    else
                    {

                        if((int)currentNode.parent.X != -1 && (int)currentNode.parent.Y != -1)
                        {
                            if(currentNode.pos.X == masterGrid[(int)currentNode.parent.X][(int)currentNode.parent.Y].pos.X && currentNode.pos.Y == masterGrid[(int)currentNode.parent.X][(int)currentNode.parent.Y].pos.Y)
                            {
                                //Current node points to its self
                                currentNode = viewable[currentViewableStart];
                                currentViewableStart++;
                            }


                            currentNode = masterGrid[(int)currentNode.parent.X][(int)currentNode.parent.Y];
                        }
                        else {
                            //Node is off grid...
                            currentNode = viewable[currentViewableStart];
                            currentViewableStart++;
                        }
                    }
                }
                path.Reverse();
            }

            return path;
        }

        public void TestAStarNode(List<List<Node>> masterGrid, List<Node> viewable, List<Node> used, Vector2 end, bool ALLOWDIAGNALS) {
            Node currentNode;
            bool up = true, down = true, left = true, right = true;

            //Above
            if(viewable[0].pos.Y > 0 && viewable[0].pos.Y < masterGrid[0].Count && !masterGrid[(int)viewable[0].pos.X][(int)viewable[0].pos.Y-1].impassable)
            {
                currentNode = masterGrid[(int)viewable[0].pos.X][(int)viewable[0].pos.Y-1];
                up = currentNode.impassable;
                SetAStarNode(viewable, used, currentNode, new Vector2(viewable[0].pos.X, viewable[0].pos.Y), viewable[0].currentDist, end, 1);
            }

            //Below
            if(viewable[0].pos.Y >= 0 && viewable[0].pos.Y + 1 < masterGrid[0].Count && !masterGrid[(int)viewable[0].pos.X][(int)viewable[0].pos.Y+1].impassable)
            {
                currentNode = masterGrid[(int)viewable[0].pos.X][(int)viewable[0].pos.Y+1];
                down = currentNode.impassable;
                SetAStarNode(viewable, used, currentNode, new Vector2(viewable[0].pos.X, viewable[0].pos.Y), viewable[0].currentDist, end, 1);
            }

            //Left
            if(viewable[0].pos.X > 0 && viewable[0].pos.X < masterGrid.Count && !masterGrid[(int)viewable[0].pos.X-1][(int)viewable[0].pos.Y].impassable)
            {
                currentNode = masterGrid[(int)viewable[0].pos.X-1][(int)viewable[0].pos.Y];
                left = currentNode.impassable;
                SetAStarNode(viewable, used, currentNode, new Vector2(viewable[0].pos.X, viewable[0].pos.Y), viewable[0].currentDist, end, 1);
            }

            //Right
            if(viewable[0].pos.X >= 0 && viewable[0].pos.X+1 < masterGrid.Count && !masterGrid[(int)viewable[0].pos.X+1][(int)viewable[0].pos.Y].impassable)
            {
                currentNode = masterGrid[(int)viewable[0].pos.X+1][(int)viewable[0].pos.Y];
                right = currentNode.impassable;
                SetAStarNode(viewable, used, currentNode, new Vector2(viewable[0].pos.X, viewable[0].pos.Y), viewable[0].currentDist, end, 1);
            }

            if(ALLOWDIAGNALS)
            {
                // Up and Right
                if(viewable[0].pos.X >= 0 && viewable[0].pos.X+1 < masterGrid.Count && viewable[0].pos.Y > 0 && viewable[0].pos.Y < masterGrid[0].Count && !masterGrid[(int)viewable[0].pos.X + 1][(int)viewable[0].pos.Y-1].impassable && (!up || !right))
                {
                    currentNode = masterGrid[(int)viewable[0].pos.X + 1][(int)viewable[0].pos.Y-1];

                    SetAStarNode(viewable, used, currentNode, new Vector2(viewable[0].pos.X, viewable[0].pos.Y), viewable[0].currentDist, end, (float)Math.Sqrt(2));
                }

                //Down and Right
                if(viewable[0].pos.X >= 0 && viewable[0].pos.X+1 < masterGrid.Count && viewable[0].pos.Y >= 0 && viewable[0].pos.Y + 1 < masterGrid[0].Count && !masterGrid[(int)viewable[0].pos.X + 1][(int)viewable[0].pos.Y+1].impassable && (!down || !right))
                {
                    currentNode = masterGrid[(int)viewable[0].pos.X+1][(int)viewable[0].pos.Y+1];

                    SetAStarNode(viewable, used, currentNode, new Vector2(viewable[0].pos.X, viewable[0].pos.Y), viewable[0].currentDist, end, (float)Math.Sqrt(2));
                }

                //Down and Left
                if(viewable[0].pos.X > 0 && viewable[0].pos.X < masterGrid.Count && viewable[0].pos.Y >= 0 && viewable[0].pos.Y + 1 < masterGrid[0].Count && !masterGrid[(int)viewable[0].pos.X - 1][(int)viewable[0].pos.Y+1].impassable && (!down || !left))
                {
                    currentNode = masterGrid[(int)viewable[0].pos.X-1][(int)viewable[0].pos.Y+1];

                    SetAStarNode(viewable, used, currentNode, new Vector2(viewable[0].pos.X, viewable[0].pos.Y), viewable[0].currentDist, end, (float)Math.Sqrt(2));
                }

                // Up and Left
                if(viewable[0].pos.X > 0 && viewable[0].pos.X < masterGrid.Count && viewable[0].pos.Y > 0 && viewable[0].pos.Y < masterGrid[0].Count && !masterGrid[(int)viewable[0].pos.X - 1][(int)viewable[0].pos.Y-1].impassable && (!up || !left))
                {
                    currentNode = masterGrid[(int)viewable[0].pos.X-1][(int)viewable[0].pos.Y-1];

                    SetAStarNode(viewable, used, currentNode, new Vector2(viewable[0].pos.X, viewable[0].pos.Y), viewable[0].currentDist, end, (float)Math.Sqrt(2));
                }
            }


            viewable[0].hasBeenUsed = true;
            used.Add(viewable[0]);
            viewable.RemoveAt(0);



            // sort
            /*viewable.Sort(delegate(AStarNode n1, AStarNode n2)
            {
                return n1.FScore.CompareTo(n2.FScore);
            });*/
        }

        public void SetAStarNode(List<Node> viewable, List<Node> used, Node nextNode, Vector2 nextParent, float d, Vector2 target, float DISTMULT)
        {
            float f = d;
            float addedDist = nextNode.cost * DISTMULT;
           



            //Add item
            if(!nextNode.isViewable && !nextNode.hasBeenUsed)
            {
                //viewable.Add(new AStarNode(nextParent, f, new Vector2(nextNode.Pos.X, nextNode.Pos.Y), nextNode.CurrentDist + 1, nextNode.Cost, nextNode.Impassable));

                nextNode.SetNode(nextParent, f, d + addedDist);
                nextNode.isViewable = true;

                SetAStarNodeInsert(viewable, nextNode);
            }
            //Node is in viewable, so check if Fscore needs revised
            else if(nextNode.isViewable)
            {

                if(f < nextNode.fScore)
                {
                    nextNode.SetNode(nextParent, f, d + addedDist);
                }
            }
        }

        public virtual void SetAStarNodeInsert(List<Node> LIST, Node NEWNODE)
        {
            bool added = false;
            for(int i=0;i<LIST.Count;i++)
            {
                if(LIST[i].fScore > NEWNODE.fScore)
                {
                    //Cant insert at 0, because that would take up the looking at node...
                    LIST.Insert(Math.Max(1, i), NEWNODE);
                    added = true;
                    break;
                }
            }

            if(!added)
            {
                LIST.Add(NEWNODE);
            }
        }
        #endregion
    }
}
