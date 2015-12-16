using System;
using System.Collections.Generic;
using System.Collections; 
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace _2_convex_hull
{
    public partial class FormMain : Form
    {

        abstract class PointGenerator
        {
            /// <summary>
            /// Generates a random point in a rectangle defined with the given width x height.
            /// Sub classes should implement a specific algorithm to determine point distribution.
            /// </summary>
            /// <param name="width">Width of the bounding rectangle</param>
            /// <param name="height">Height of the bounding rectangle</param>
            /// <returns>The generated random point</returns>
            public abstract PointF generatePointIn(int width, int height);
        }
        
        class GaussianPointGenerator : PointGenerator {

            // Uses this to generate the Guassian distribution
            // from http://msdn.microsoft.com/msdnmag/issues/06/09/TestRun/default.aspx
            // (http://msdn.microsoft.com/msdnmag/issues/06/09/TestRun/default.aspx?loc=&fig=true#fig9)
            class Gaussian
            {
                private Random r = new Random();
                private bool use_last_result = false; // flag for NextGaussian3()
                private double y2 = 0.0;  // secondary result for NextGaussian3()

                public double NextGaussian(double mean, double sd) // most efficient
                {
                    double x1, x2, w, y1 = 0.0;

                    if (use_last_result) // use answer from previous call?
                    {
                        y1 = y2;
                        use_last_result = false;
                    }
                    else
                    {
                        do
                        {
                            x1 = 2.0 * r.NextDouble() - 1.0;
                            x2 = 2.0 * r.NextDouble() - 1.0;
                            w = (x1 * x1) + (x2 * x2);
                        }
                        while (w >= 1.0); // are x1 and x2 inside unit circle?

                        w = Math.Sqrt((-2.0 * Math.Log(w)) / w);
                        y1 = x1 * w;
                        y2 = x2 * w;
                        use_last_result = true;
                    }

                    return mean + y1 * sd;
                }
            }
            Gaussian m_rand;

            public GaussianPointGenerator() {
                m_rand = new Gaussian();
            }

            public override PointF generatePointIn(int width, int height) {
                // mdj 1/8/07 generate random points with floats instead of ints to avoid having so many 
                // duplicates as per Dr. Ringger's comments from Fall 06.  Doubles would be better, but there's 
                // no built-in point data structure for doubles.  
                return new PointF(width / 2 + (float)m_rand.NextGaussian(0, width / 6),
                                 height / 2 + (float)m_rand.NextGaussian(0, height / 6));
            }
        }

        class UniformPointGenerator : PointGenerator
        {
            private Random rand = new Random();

            /// <summary>
            /// Generates points that are uniformly distributed inside of the oval
            /// defined by the bounding rectangle passed in.
            /// </summary>
            /// <param name="width">Width of the bounding rectangle</param>
            /// <param name="height">Height of the bounding rectangle</param>
            /// <returns>Random point in an oval bound by the rectangle</returns>
            public override PointF generatePointIn(int width, int height) {
                double r, x, y;

                do {
                    //First generate points inside a circle
                    x = 2.0 * rand.NextDouble() - 1.0;
                    y = 2.0 * rand.NextDouble() - 1.0;

                    //Check radius
                    r = Math.Sqrt(x * x + y * y);
                } while( r > 1.0 );

                //Now convert to possibly-oval, larger bounds
                x *= width / 2 - 10;    //giving 5px border on each side
                y *= width / 2 - 10;
                //Translate to screen coords
                x += width / 2;
                y += height / 2;

                //Using float gives fewer duplicates than using int.
                //Double would be better but there is no Point-Double class.
                return new PointF((float)x, (float)y);
            }
        }
        
        Graphics m_g;
        PointGenerator pointGenerator;
        List<PointF> m_pointList;
        private Hashtable UniquePoints; 
        //bool m_imageScaled;

        public FormMain()
        {
            InitializeComponent();
            pictureBoxView.Image = new Bitmap(pictureBoxView.Width, pictureBoxView.Height);
            m_g = Graphics.FromImage(pictureBoxView.Image);
            pointGenerator = new UniformPointGenerator();
            radioUniform.Checked = true;    //start with this as the default
            m_pointList = new List<PointF>();
            UniquePoints = new Hashtable(); 
        }

        private PointF getRandomPoint()
        {
            //eam, 1/17/08 -- changed to use a Strategy Pattern for generating pts w/ different distributions
            return pointGenerator.generatePointIn(pictureBoxView.Width, pictureBoxView.Height);
        }

        private void generatePoints()
        {
            // create point list
            int numPoints = int.Parse(textBoxNumPoints.Text);
            m_pointList.Clear();
            UniquePoints.Clear(); 
            PointF NewlyCreatedPoint; 
            pbProgress.Value = pbProgress.Minimum;
            pbProgress.Maximum = 100; 

            // make sure X value are unique.  Y values may contain duplicates by the way. 
            while (UniquePoints.Count < numPoints) 
            {
                pbProgress.Value = (int) (100f * ((float) UniquePoints.Count / ((float) numPoints))); 
                NewlyCreatedPoint = getRandomPoint();   //get the next point to add
                if (!UniquePoints.Contains(NewlyCreatedPoint.X))
                    UniquePoints.Add(NewlyCreatedPoint.X,NewlyCreatedPoint); 
            };

            // more convenient from here on out to use list. 
            foreach (PointF point in UniquePoints.Values)
            {
                m_pointList.Add(point);
            }

            // find the max and min
            float maxX = pictureBoxView.Image.Width, maxY = pictureBoxView.Image.Height;
            float minX = 0, minY = 0;
            foreach (PointF point in m_pointList)
            {
                if(maxX < point.X) maxX = point.X;
                if(maxY < point.Y) maxY = point.Y;
                if(minX > point.X) minX = point.X;
                if(minY > point.Y) minY = point.Y;
            }

            // find translation factors
            float transX = (minX < 0F) ? -minX : 0F;
            float transY = (minY < 0F) ? -minY : 0F;

            // find the point range
            float rangeX = transX + (maxX - minX);
            float rangeY = transY + (maxY - minY);

            // find scaling factors
            const int padding = 20;
            float scaleX = (pictureBoxView.Image.Width - padding) / rangeX;
            float scaleY = (pictureBoxView.Image.Height - padding) / rangeY;

            // only shrink points , not enlarge them
            if (scaleX > 1.0) scaleX = 1F;
            if (scaleY > 1.0) scaleY = 1F;

            // scale points
            for(int i = 0; i < m_pointList.Count; ++i)
            {
                PointF p = m_pointList[i];
                p.X = transX + (p.X * scaleX);
                p.Y = transY + (p.Y * scaleY);
                m_pointList[i] = p;
            }

            /*if (scaleX != 1.0 || scaleY != 1.0)
                m_imageScaled = true;*/

            drawPoints();
            statusLabel.Text = "" + numPoints + " points Generated. Scale factor: " +
                 ((scaleX >= scaleY) ? scaleX : scaleY);
        }

        private void drawPoints()
        {
            // disable this as it is still too slow
            
            /*if (m_imageScaled);
            {
                using (DirectBitmapWriter dbw = new DirectBitmapWriter((Bitmap)pictureBoxView.Image))
                {
                    dbw.Clear(Color.White);
                    foreach (Point point in m_pointList)
                    {
                        //if (callback != null) callback(i * 100 / (Bitmap.Height - 2));
                        dbw.SetPixel(point, Color.Black);
                    }
                }
            }
            else*/
            {
                m_g.Clear(Color.White);
                m_g.Flush();
                //pictureBoxView.Refresh(); // just in case there are not any points*/
                Pen pointColor = new Pen(Color.FromArgb(0, 0, 0));
                foreach (PointF point in m_pointList)
                {
                    m_g.DrawEllipse(pointColor, new RectangleF(point, new Size(2, 2)));
                    //pictureBoxView.Refresh();
                }
            }
            pictureBoxView.Refresh(); // show our results
        }

        #region GUI Control
        private void buttonGenerate_Click(object sender, EventArgs e)
        {
            generatePoints();
        }

        private void textBoxNumPoints_Validating(object sender, CancelEventArgs e)
        {
            int result;
            if(!(int.TryParse(textBoxNumPoints.Text, out result)))
            {
                e.Cancel = true;
            }
        }

        private void buttonSolve_Click(object sender, EventArgs e)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
                ConvexHullSolver convexHullSolver = new ConvexHullSolver();
                convexHullSolver.Solve(m_g, m_pointList);
            timer.Stop();
            pictureBoxView.Refresh();
            statusLabel.Text = "Done.  Time taken: " + timer.Elapsed;

        }

        private void buttonClearToPoints_Click(object sender, EventArgs e)
        {
            drawPoints();
            statusLabel.Text = "Cleared to the original points.";
        }
        #endregion

        private void radioUniform_CheckedChanged(object sender, EventArgs e) {
            if( !(pointGenerator is UniformPointGenerator) ) {
                pointGenerator = new UniformPointGenerator();
            }
        }

        private void radioGaussian_CheckedChanged(object sender, EventArgs e) {
            if( !(pointGenerator is GaussianPointGenerator) ) {
                pointGenerator = new GaussianPointGenerator();
            }
        }

        private void pbProgress_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            Stopwatch timer = new Stopwatch();
            TimeSpan avg = new TimeSpan();
            for(int i=0; i < 10; i++)
            {
                timer.Start();
                ConvexHullSolver convexHullSolver = new ConvexHullSolver();
                convexHullSolver.Solve(m_g, m_pointList);
                timer.Stop();
                avg += timer.Elapsed;
                timer.Reset();
            }
            pictureBoxView.Refresh();
            statusLabel.Text = "Done.  Avg Time taken: " + (avg.TotalSeconds / 10);
        }
    }
}