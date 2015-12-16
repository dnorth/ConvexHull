using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace _2_convex_hull
{
    class ConvexHullSolver
    {
        //This Line class was made to facilitate the coding process
        public class Line
        {
            //leftMost- left most point in the line
            //rightMost- right most point in the line
            //lineColor- the color needed to draw the line graphically
            public PointF leftMost, rightMost;
            public Pen lineColor;

            //Constructor for new lines, initializes points(leftMost set to max value so that any other point replaces it and rightMost set to min so anything greater replaces it) and color. 
            public Line()
            {
                leftMost = new PointF(float.MaxValue, float.MaxValue);
                rightMost = new PointF(float.MinValue, float.MinValue);
                lineColor = new Pen(Color.FromArgb(0, 0, 0));
            }

            public Line(PointF left, PointF right)
            {
                leftMost = left;
                rightMost = right;
                lineColor = new Pen(Color.FromArgb(0, 0, 0));
            }
        };

        public void Solve(System.Drawing.Graphics g, List<PointF> pointList)
        {
            // Initialize new line and list
            Line line = new Line();
            List<Line> lineList = new List<Line>();

            //For each point in pointList, find the one with the lowest X value and with the highest X value
            foreach(PointF  point in pointList)
            {
                if(point.X < line.leftMost.X)
                {
                    line.leftMost = point;
                }
                if(point.X > line.rightMost.X)
                {
                    line.rightMost = point;
                }
            }

            //Initialize new lists to use in our divide and conquer method
            List<PointF> topList = new List<PointF>();
            List<PointF> bottomList = new List<PointF>(); 

            //Find out whether the point is on top of the line or below the line
            foreach(PointF point in pointList)
            {
                if(point != line.leftMost && point != line.rightMost)
                {
                    if(onTop(line, point))
                    {
                        topList.Add(point);
                    }
                    else
                    {
                        bottomList.Add(point);
                    }


                }
            }

            //Use the recursive divideAndConquer() to get the lines on the outside
            List<Line> a = divideAndConquer(line, topList);
            List<Line> b = divideAndConquer(line, bottomList);

            //Append list b to a
            a.AddRange(b);

            //Draw the lines given in the list
            drawLines(g, a);
        }

        //Draws the lines that are sent in via "lineList" 
        //Returns nothing
        public void drawLines(System.Drawing.Graphics g, List<Line> lineList)
        {
            foreach(Line line in lineList)
            {
                g.DrawLine(line.lineColor, line.leftMost, line.rightMost);
            }
        }

        //Finds what side of the line a point is on
        //Returns true if the point is above the line, false if it's below
        public bool onTop(Line line, PointF point)
        {
            int sign = ( (line.leftMost.X - line.rightMost.X) >= 0 ? 1 : -1 );

            float f = ((line.leftMost.Y - line.rightMost.Y) * (point.X - line.leftMost.X) + (line.leftMost.Y - point.Y)*(line.leftMost.X - line.rightMost.X)) * sign;
            return f > 0;
        }

        //The recursive function. We will call this until the base case happens, then return the list of lines taken from it
        public List<Line> divideAndConquer(Line line, List<PointF> pointList)
        {
            List<Line> lineList = new List<Line>();

            //This is the base case. Return the line
            if(pointList.Count == 0)
            {
                lineList.Add(line);
                return lineList;
            }

            if(pointList.Count == 1)
            {
                lineList.Add(new Line(line.leftMost, pointList[0]));
                lineList.Add(new Line(pointList[0], line.rightMost));
                return lineList;
            }

            //Get the furthest point from the line that is passed in
            PointF furthestPoint = getFurthestPoint(line, pointList);
            List<PointF> leftPointList = new List<PointF>();
            List<PointF> rightPointList = new List<PointF>();

            //for every point in pointList, check if the point is within the triangle defined by line.leftMost, line.rightMost and furthestPoint
            foreach(PointF point in pointList)
            {
                if (point != furthestPoint)
                {
                    if(!withinTriangle(line.leftMost, line.rightMost, furthestPoint, point))
                    {
                        //if it is not within the triangle, check if the point is bounded by the X values of the leftMost line point and the furthest point
                         if(point.X >= line.leftMost.X && point.X <= furthestPoint.X)
                         {
                            //if so, add that point to the list of points for that first line 
                            leftPointList.Add(point);
                         }
                         else
                         {
                             //if not, add that point to the list of points for the other line that is making the triangle
                             rightPointList.Add(point);
                         }
                    }
                }
            }

            //From the furthest point we make two lines. Then we need to recurse through those two lines.
            lineList.AddRange(divideAndConquer(new Line(line.leftMost, furthestPoint), leftPointList));
            lineList.AddRange(divideAndConquer(new Line(furthestPoint, line.rightMost), rightPointList));

            return lineList;
        }

         
        //Finds the furthest point in a list from the line given
        //Returns the furthest point in the list
        public PointF getFurthestPoint(Line line, List<PointF> pointList)
        {
            float largestDistance = float.MinValue;
            PointF furthestPoint = new PointF();
            foreach(PointF point in pointList)
            {
                float dist = getDistance(line, point);
                if(dist > largestDistance)
                {
                    largestDistance = dist;
                    furthestPoint = point;
                }
            }

            return furthestPoint;
        }

        //Gets the distance of a point from the line given. This method is the mathmatically proven way to find the distance of a point from a line. It is based on distance from the perpendicular slope of the line
        public float getDistance(Line line, PointF point)
        {
            return (float)(Math.Abs((line.rightMost.Y - line.leftMost.Y) * point.X - (line.rightMost.X - line.leftMost.X) * point.Y + line.rightMost.X * line.leftMost.Y - line.rightMost.Y * line.leftMost.X) / Math.Sqrt((Math.Pow((line.rightMost.Y - line.leftMost.Y), 2.0) + Math.Pow((line.rightMost.X - line.leftMost.X), 2.0))));
        }

        //p1- the first point
        //p2- the second point
        //p3- the third point
        //p-  the point to check whether or not it's in the triangle 
        //Returns true if the variable is within the triangle, false if it's not
        public bool withinTriangle(PointF p1, PointF p2, PointF p3, PointF p)
        {
            //If the point p is on the same side of the line as every triangle point that does not make a line with the other two points, then it is within the triangle. 
            //[i.e. if points p1 and p2 make a line, then the point p must be on the same side of that line as point p3 to be within the triangle. Do this for every combination of the line (3 in a triangle)]
            if ((onTop(new Line(p1, p2), p3) == onTop(new Line(p1, p2), p)) && (onTop(new Line(p2, p3), p1) == onTop(new Line(p2, p3), p)) && (onTop(new Line(p1, p3), p2) == onTop(new Line(p1, p3), p)))
            {
                return true;
            }

            return false;
        }
    }
}
