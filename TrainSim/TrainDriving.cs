using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;
using System.Timers;

namespace TrainSim
{
    public static class TrainDriving
    {
        public static bool currentlyDriving = false;
        public static List<Train> allTrains = new List<Train>();

        public static void StartATrain(Point start, ref Train t, bool playerDriven, List<string> presetTrain) //in presetTrain, [0] is name, [1] is type
        {
            string name = "";
            if (!playerDriven) //to identify player driven and AI driven, playerDriven has no name
            {
                if (presetTrain != null)
                    name = presetTrain[0];
                while (name == "")
                {
                    name = MainForm.SimpleInputDialog("Název vlaku?");
                }
            }
            
            if(presetTrain != null)
                t = new Train(name, presetTrain[1]);
            else
                t = new Train(name, MainForm.SimpleOptionsDialog("Typ vlaku", "Osobáček", "Rychlík"));

            t.timer = new Timer();

            if (!playerDriven)
            {
                t.timer.Interval = 1000;
                allTrains.Add(t);
            }

            string direction;

            if (playerDriven)
            {
                t.currentPositionX = start.X / 20;
                t.currentPositionY = start.Y / 20;
            }
            else
            {
                t.currentPositionX = -1;
                t.currentPositionY = -1;
            }
            if (playerDriven)
            {
                if (MainForm.map[start.X / 20, start.Y / 20].Image == MainForm.horizontal)
                {
                    direction = MainForm.SimpleOptionsDialog("Směr vlaku?", "doprava", "doleva");
                }
                else
                {
                    direction = MainForm.SimpleOptionsDialog("Směr vlaku?", "nahoru", "dolu");
                }
                if(t.orientations.Count > 0)
                    t.orientations[0] = direction;
            }

            Train t_ = t;

            
            if (playerDriven)
            {
                t.timer.Enabled = true;
                t.timer.Interval = 2500;
                currentlyDriving = true;
            }

            t.timer.Elapsed += (thisSender, eventArgs) => tmrMoving_Elapsed(thisSender, eventArgs, t_);
            //return t;
        }

        static bool smallTrainPuller; //just to decrease speed of smallTrains by half


        //timer hander for setting the t.canMove tag for the PaintHandler to know if the train has to move
        public static void tmrMoving_Elapsed(object sender, EventArgs e, Train t)
        {
            
            if(t.playerDriven){
                t.canMove = true;
                MainForm.playBox.Invalidate();
            }
            else if (t.offSchedule)
            {
                if (t.currentPositionX != -1 && t.currentPositionY != -1)
                {
                    if (t.type == "smallTrain")
                    {
                        if (smallTrainPuller == false)
                        {
                            t.canMove = true;
                            smallTrainPuller = true;
                        }
                        else if (smallTrainPuller == true)
                            smallTrainPuller = false;
                    }
                    else if (MainForm.map[t.currentPositionX, t.currentPositionY].BlockInfo == 3 || MainForm.map[t.currentPositionX, t.currentPositionY].BlockInfo == 4)
                    {
                        t.currentPositionX = -1;
                        t.currentPositionY = -1;
                        t.nextPositionX = 0;
                        t.nextPositionY = 0;
                    }
                    else
                        t.canMove = true;

                    MainForm.playBox.Invalidate();
                }
            }
            else
            {
                    int correctedTime;
                    int time = MainForm.mainStopWatchInt;
                    
                    if (t.trainSchedule[time].start == true)
                    {
                        t.currentPositionX = t.trainSchedule[time].positionX;
                        t.currentPositionY = t.trainSchedule[time].positionY;
                        t.orientations[0] = t.trainSchedule[time].direction;
                        t.velocity = 1;

                        //looks 0-3 blocks ahead right from the start to set the switches right
                        for (int j = 0; j < 3; j++)
                        {
                            if (time + j > 23 * 60 + 59)
                                correctedTime = time + j - (23 * 60 + 59);
                            else
                                correctedTime = time + j;
                            try
                            {
                                if (MainForm.map[t.trainSchedule[correctedTime].positionX, t.trainSchedule[correctedTime].positionY].BlockInfo == 1 &&
                                    t.trainSchedule[correctedTime].switched)
                                {
                                    MainForm.map[t.trainSchedule[correctedTime].positionX, t.trainSchedule[correctedTime].positionY].BlockInfo = 2;
                                    MainForm.playBox.Invalidate();

                                }
                                else if (MainForm.map[t.trainSchedule[correctedTime].positionX, t.trainSchedule[correctedTime].positionY].BlockInfo == 2 &&
                                    t.trainSchedule[correctedTime].switched == false)
                                {
                                    MainForm.map[t.trainSchedule[correctedTime].positionX, t.trainSchedule[correctedTime].positionY].BlockInfo = 1;
                                    MainForm.playBox.Invalidate();
                                }
                            }
                            catch (IndexOutOfRangeException) { }
                        }

                    }
                    if (t.trainSchedule[time].stop == true)
                    {
                        t.currentPositionX = -1;
                        t.currentPositionY = -1;
                        t.velocity = 0;
                        t.orientations[0] = "none";
                    }
                    if (t.trainSchedule[time].waiting == false)
                    {
                        
                        t.canMove = true;
                    }

                    //if it is "osobáček" look two blocks ahead for the switches
                    int lookAhead = 1;
                    if (t.type == "smallTrain")
                        lookAhead = 2;
                    
                    //look a block ahead for the switches if the train is on move
                    if (time + lookAhead > 23 * 60 + 59)
                        correctedTime = time + lookAhead - (23 * 60 + 59);
                    else
                        correctedTime = time + lookAhead;
                    try
                    {
                        if (MainForm.map[t.trainSchedule[correctedTime].positionX, t.trainSchedule[correctedTime].positionY].BlockInfo == 1 &&
                            t.trainSchedule[correctedTime].switched)
                        {
                            MainForm.map[t.trainSchedule[correctedTime].positionX, t.trainSchedule[correctedTime].positionY].BlockInfo = 2;
                            MainForm.playBox.Invalidate();

                        }
                        else if (MainForm.map[t.trainSchedule[correctedTime].positionX, t.trainSchedule[correctedTime].positionY].BlockInfo == 2 &&
                            t.trainSchedule[correctedTime].switched == false)
                        {
                            MainForm.map[t.trainSchedule[correctedTime].positionX, t.trainSchedule[correctedTime].positionY].BlockInfo = 1;
                            MainForm.playBox.Invalidate();

                        }
                    }
                    catch (IndexOutOfRangeException) { }
                
            }
        }

        public static List<Train> trainsOffSchedule = new List<Train>();

        static System.Timers.Timer crashAnimationTimer;

        public static System.Windows.Forms.PictureBox exp;
        public static List<System.Windows.Forms.PictureBox> explosions = new List<System.Windows.Forms.PictureBox>();

        //handler the crash and it's animation and consequences, first overload for 1 train crashes
        public static void TrainCrash(Train t, Coordinates crash){
            exp = new System.Windows.Forms.PictureBox();
            exp.Location = new Point(crash.x * 20, crash.y * 20);
            exp.Image = Properties.Resources.explosion_g;
            exp.Size = new Size(20, 20);
            exp.Parent = MainForm.mainForm;
            explosions.Add(exp);
            exp.BringToFront();

            crashAnimationTimer = new System.Timers.Timer();
            crashAnimationTimer.Interval = 1000;
            crashAnimationTimer.Elapsed += crashAnimationTimer_elapsed;
            crashAnimationTimer.Enabled = true;


            for (int i = 0; i < TrainDriving.trainsOffSchedule.Count; i++)
            {
                if (TrainDriving.trainsOffSchedule[i] == t)
                {
                    TrainDriving.trainsOffSchedule[i].offSchedule = false;
                    TrainDriving.trainsOffSchedule.RemoveAt(i);
                }
            }
            if (MainForm.currentlyDrivenTrain != null)
            {
                MainForm.currentlyDrivenTrain = null;
            }

            t.currentPositionX = -1; t.currentPositionX = -1;
            t.nextPositionX = 0; t.nextPositionX = 0;
            t.orientations[0] = "none";
        }

        //another overload just if two trains crashes
        public static void TrainCrash(Train t1, Train t2, Coordinates crash, System.Windows.Forms.PaintEventArgs g)
        {
            exp = new System.Windows.Forms.PictureBox();
            exp.Location = new Point(crash.x * 20, crash.y * 20);
            exp.Image = Properties.Resources.explosion_g;
            exp.Parent = MainForm.mainForm;
            exp.Size = new Size(20, 20);
            explosions.Add(exp);
            exp.BringToFront();

            crashAnimationTimer = new System.Timers.Timer();
            crashAnimationTimer.Interval = 1000;
            crashAnimationTimer.Elapsed += crashAnimationTimer_elapsed;
            crashAnimationTimer.Enabled = true;


            for(int i = 0; i < TrainDriving.trainsOffSchedule.Count; i++)
			{
			    if(TrainDriving.trainsOffSchedule[i] == t1 || TrainDriving.trainsOffSchedule[i] == t2){
                    TrainDriving.trainsOffSchedule[i].offSchedule = false;
                    TrainDriving.trainsOffSchedule.RemoveAt(i);
                }
			}
            if(MainForm.currentlyDrivenTrain != null){
                MainForm.currentlyDrivenTrain = null;
            }

            t1.currentPositionX = -1; t2.currentPositionX = -1; t1.currentPositionY = -1; t2.currentPositionY = -1;
            t1.nextPositionX = 0; t2.nextPositionX = 0; t1.nextPositionY = 0; t2.nextPositionY = 0;
            t1.orientations[0] = "none"; t2.orientations[0] = "none";
        }

        static void crashAnimationTimer_elapsed(object sender, ElapsedEventArgs e)
        {
            if (explosions.Count == 0)
                crashAnimationTimer.Enabled = false;
            else
            {
                var disp = explosions[0];
                explosions.RemoveAt(0);
                disp.Invoke(new Action(() => disp.Dispose()));
                if (!(explosions.Count > 0))
                    crashAnimationTimer.Enabled = false;
            }
        }

        //redraws the train t after every redraw, stationary or onMove if t.canMove flag is ture
        public static void TrainAction(Train t, System.Windows.Forms.PaintEventArgs g, int time)
        {
            if (t.canMove)
            {
                if (t.nextPositionX > 0)
                {
                    t.orientations[0] = "doprava";
                    t.currentPositionX += t.velocity * t.nextPositionX;
                    t.nextPositionX = 0;
                }
                if (t.nextPositionX < 0)
                {
                    t.orientations[0] = "doleva";
                    t.currentPositionX += t.velocity * t.nextPositionX;
                    t.nextPositionX = 0;
                }
                if (t.nextPositionY > 0)
                {
                    t.orientations[0] = "dolu";
                    t.currentPositionY += t.velocity * t.nextPositionY;
                    t.nextPositionY = 0;
                }
                if (t.nextPositionY < 0)
                {
                    t.orientations[0] = "nahoru";
                    t.currentPositionY += t.velocity * t.nextPositionY;
                    t.nextPositionY = 0;
                }
            }
            t.canMove = false;

            t.nextVagoonPositionX = t.currentPositionX;
            t.nextVagoonPositionY = t.currentPositionY;

            t.occupiedRails.Clear();

            for (int i = 0; i < t.components.Count; i++)
            {
                try
                {
                    Block testBounds = MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY];
                }
                catch (IndexOutOfRangeException)
                {
                    if(t.nextVagoonPositionX != -1 && t.nextVagoonPositionY != -1)
                        TrainCrash(t, new Coordinates() { x = t.nextVagoonPositionX, y = t.nextVagoonPositionY});
                    return;
                }
                try //this is for drawing vagoons after the "masinka", drawing of the fist vagoon of the train is handled in the catch(ArgumentOutOfRange) branche
                {
                    t.occupiedRails.Add(new Coordinates() { x = t.nextVagoonPositionX, y = t.nextVagoonPositionY });
                    
                    if (t.orientations[i - 1] == "doleva")
                    {
                        if ((MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 3 || MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 4) && MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].ways[0,1] == true)
                        {
                            t.orientations[i] = "doleva";
                            t.nextVagoonPositionX += 1;
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 1)
                        {
                            if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vrighttop || MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vrightbottom ||
                                MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vlefttop || MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vleftbottom)
                            {
                                t.orientations[i] = "doleva";
                                g.Graphics.DrawImage(t.components[i], new Rectangle(new Point(t.nextVagoonPositionX * 20, t.nextVagoonPositionY * 20), new Size(20, 20)));
                                t.nextVagoonPositionX += 1;
                            }
                            else
                                TrainCrash(t, new Coordinates() { x = t.nextVagoonPositionX, y = t.nextVagoonPositionY});
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.horizontal || MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.horizontalOver)
                        {
                            t.orientations[i] = "doleva";
                            g.Graphics.DrawImage(t.components[i], new Rectangle(new Point(t.nextVagoonPositionX * 20, t.nextVagoonPositionY * 20), new Size(20, 20)));
                            t.nextVagoonPositionX += 1;
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.rleftbottom || (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 2 &&
                            MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vbottomleft) || (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 2 &&
                            MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vleftbottom))
                        {
                            t.orientations[i] = "nahoru";
                            g.Graphics.DrawImage(RotateImage(t.components[i], (float)45), new Rectangle(new Point(t.nextVagoonPositionX * 20 - 4, t.nextVagoonPositionY * 20 + 2), new Size(25, 25)));
                            t.nextVagoonPositionY += 1;
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.rtopleft || (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 2 &&
                            MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vlefttop) || (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 2 &&
                            MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vtopleft))
                        {
                            t.orientations[i] = "dolu";
                            g.Graphics.DrawImage(RotateImage(t.components[i], (float)315), new Rectangle(new Point(t.nextVagoonPositionX * 20 - 5, t.nextVagoonPositionY * 20 - 5), new Size(25, 25)));
                            t.nextVagoonPositionY -= 1;
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.verticalOver)
                        {
                            t.orientations[i] = "doleva";
                            t.nextVagoonPositionX += 1;
                        }
                    }
                    else if (t.orientations[i - 1] == "doprava")
                    {
                        if ((MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 3 || MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 4) && MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].ways[0, 3] == true)
                        {
                            t.orientations[i] = "doprava";
                            t.nextVagoonPositionX -= 1;
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 1)
                        {
                            if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vrighttop || MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vrightbottom ||
                                MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vlefttop || MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vleftbottom)
                            {
                                t.orientations[i] = "doprava";
                                g.Graphics.DrawImage(RotateImage(t.components[i], (float)180), new Rectangle(new Point(t.nextVagoonPositionX * 20, t.nextVagoonPositionY * 20), new Size(20, 20)));
                                t.nextVagoonPositionX -= 1;
                            }
                            else
                                TrainCrash(t, new Coordinates() { x = t.nextVagoonPositionX, y = t.nextVagoonPositionY});
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.horizontal || MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.horizontalOver)
                        {
                            t.orientations[i] = "doprava";
                            g.Graphics.DrawImage(RotateImage(t.components[i], (float)180), new Rectangle(new Point(t.nextVagoonPositionX * 20, t.nextVagoonPositionY * 20), new Size(20, 20)));
                            t.nextVagoonPositionX -= 1;
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.rrighttop || (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 2 &&
                            MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vrighttop) || (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 2 &&
                            MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vtopright))
                        {
                            t.orientations[i] = "dolu";
                            g.Graphics.DrawImage(RotateImage(t.components[i], (float)225), new Rectangle(new Point(t.nextVagoonPositionX * 20, t.nextVagoonPositionY * 20 - 6), new Size(25, 25)));
                            t.nextVagoonPositionY -= 1;
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.rbottomright || (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 2 &&
                            MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vrightbottom) || (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 2 &&
                            MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vbottomright))
                        {
                            t.orientations[i] = "nahoru";
                            g.Graphics.DrawImage(RotateImage(t.components[i], (float)135), new Rectangle(new Point(t.nextVagoonPositionX * 20, t.nextVagoonPositionY * 20), new Size(25, 25)));
                            t.nextVagoonPositionY += 1;
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.verticalOver)
                        {
                            t.orientations[i] = "doprava";
                            t.nextVagoonPositionX -= 1;
                        }
                    }
                    else if (t.orientations[i - 1] == "nahoru")
                    {
                        if ((MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 3 || MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 4) && MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].ways[0, 0] == true)
                        {
                            t.orientations[i] = "nahoru";
                            t.nextVagoonPositionY += 1;
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 1)
                        {
                            if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vbottomright || MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vbottomleft ||
                                MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vtopleft || MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vtopright)
                            {
                                t.orientations[i] = "nahoru";
                                g.Graphics.DrawImage(RotateImage(t.components[i], (float)90), new Rectangle(new Point(t.nextVagoonPositionX * 20, t.nextVagoonPositionY * 20), new Size(20, 20)));
                                t.nextVagoonPositionY += 1;
                            }
                            else
                                TrainCrash(t, new Coordinates() { x = t.nextVagoonPositionX, y = t.nextVagoonPositionY});
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vertical || MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.verticalOver)
                        {
                            t.orientations[i] = "nahoru";
                            g.Graphics.DrawImage(RotateImage(t.components[i], (float)90), new Rectangle(new Point(t.nextVagoonPositionX * 20, t.nextVagoonPositionY * 20), new Size(20, 20)));
                            t.nextVagoonPositionY += 1;
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.rtopleft || (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 2 &&
                            MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vtopleft) || (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 2 &&
                            MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vlefttop))
                        {
                            t.orientations[i] = "doprava";
                            g.Graphics.DrawImage(RotateImage(t.components[i], (float)135), new Rectangle(new Point(t.nextVagoonPositionX * 20 - 6, t.nextVagoonPositionY * 20 - 6), new Size(25, 25)));
                            t.nextVagoonPositionX -= 1;
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.rrighttop || (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 2 &&
                            MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vrighttop) || (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 2 &&
                            MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vtopright))
                        {
                            t.orientations[i] = "doleva";
                            g.Graphics.DrawImage(RotateImage(t.components[i], (float)45), new Rectangle(new Point(t.nextVagoonPositionX * 20, t.nextVagoonPositionY * 20 - 6), new Size(25, 25)));
                            t.nextVagoonPositionX += 1;
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.horizontalOver)
                        {
                            t.orientations[i] = "nahoru";
                            t.nextVagoonPositionY += 1;
                        }
                    }
                    else if (t.orientations[i - 1] == "dolu")
                    {
                        if ((MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 3 || MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 4) && MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].ways[0, 2] == true)
                        {
                            t.orientations[i] = "dolu";
                            t.nextVagoonPositionY -= 1;
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 1)
                        {
                            if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vbottomright || MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vbottomleft ||
                                MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vtopleft || MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vtopright)
                            {
                                t.orientations[i] = "dolu";
                                g.Graphics.DrawImage(RotateImage(t.components[i], (float)270), new Rectangle(new Point(t.nextVagoonPositionX * 20, t.nextVagoonPositionY * 20), new Size(20, 20)));
                                t.nextVagoonPositionY -= 1;
                            }
                            else
                                TrainCrash(t, new Coordinates() { x = t.nextVagoonPositionX, y = t.nextVagoonPositionY});
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vertical || MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.verticalOver)
                        {
                            t.orientations[i] = "dolu";
                            g.Graphics.DrawImage(RotateImage(t.components[i], (float)270), new Rectangle(new Point(t.nextVagoonPositionX * 20, t.nextVagoonPositionY * 20), new Size(20, 20)));
                            t.nextVagoonPositionY -= 1;
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.rbottomright || (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 2 &&
                            MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vbottomright) || (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 2 &&
                            MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vrightbottom))
                        {
                            t.orientations[i] = "doleva";
                            g.Graphics.DrawImage(RotateImage(t.components[i], (float)315), new Rectangle(new Point(t.nextVagoonPositionX * 20, t.nextVagoonPositionY * 20), new Size(25, 25)));
                            t.nextVagoonPositionX += 1;
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.rleftbottom || (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 2 &&
                            MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vleftbottom) || (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 2 &&
                            MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vbottomleft))
                        {
                            t.orientations[i] = "doprava";
                            g.Graphics.DrawImage(RotateImage(t.components[i], (float)225), new Rectangle(new Point(t.nextVagoonPositionX * 20 - 6, t.nextVagoonPositionY * 20), new Size(25, 25)));
                            t.nextVagoonPositionX -= 1;
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.horizontalOver)
                        {
                            t.orientations[i] = "nahoru";
                            t.nextVagoonPositionY -= 1;
                        }
                    }
                }
                //part for determining next position of the first vagoon
                catch (ArgumentOutOfRangeException)
                {
                    if (t.orientations[i] == "doleva")
                    {
                        if ((MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 3 || MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 4) && MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].ways[0, 1] == true)
                        {
                            t.nextVagoonPositionX += 1;
                            t.nextPositionX = -1;
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 1)
                        {
                            if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vrighttop || MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vrightbottom ||
                                MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vlefttop || MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vleftbottom)
                            {
                                g.Graphics.DrawImage(t.components[i], new Rectangle(new Point(t.nextVagoonPositionX * 20, t.nextVagoonPositionY * 20), new Size(20, 20)));
                                t.nextVagoonPositionX += 1;
                                t.nextPositionX = -1;
                            }
                            else if (MainForm.playingMap && t.orientations[i] != "none" && t.currentPositionX != -1 && t.currentPositionY != -1)
                                TrainCrash(t, new Coordinates() { x = t.nextVagoonPositionX, y = t.nextVagoonPositionY});
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.horizontal || MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.horizontalOver)
                        {
                            g.Graphics.DrawImage(t.components[i], new Rectangle(new Point(t.nextVagoonPositionX * 20, t.nextVagoonPositionY * 20), new Size(20, 20)));
                            t.nextVagoonPositionX += 1;
                            t.nextPositionX = -1;
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.rrighttop || (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 2 &&
                            MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vrighttop) || (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 2 &&
                            MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vtopright))
                        {
                            g.Graphics.DrawImage(RotateImage(t.components[i], (float)45), new Rectangle(new Point(t.nextVagoonPositionX * 20, t.nextVagoonPositionY * 20 - 6), new Size(25, 25)));
                            t.nextVagoonPositionX += 1;
                            t.nextPositionY = -1;
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.rbottomright || (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 2 &&
                            MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vbottomright) || (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 2 &&
                            MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vrightbottom))
                        {
                            g.Graphics.DrawImage(RotateImage(t.components[i], (float)315), new Rectangle(new Point(t.nextVagoonPositionX * 20, t.nextVagoonPositionY * 20), new Size(25, 25)));
                            t.nextVagoonPositionX += 1;
                            t.nextPositionY = +1;
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.verticalOver)
                        {
                            t.nextVagoonPositionX += 1;
                            t.nextPositionX = -1;
                        }
                        else if (MainForm.playingMap && t.orientations[i] != "none" && t.currentPositionX != -1 && t.currentPositionY != -1)
                            TrainCrash(t, new Coordinates() { x = t.nextVagoonPositionX, y = t.nextVagoonPositionY});
                    }
                    else if (t.orientations[i] == "doprava")
                    {
                        if ((MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 3 || MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 4) && MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].ways[0, 3] == true)
                        {
                            t.nextVagoonPositionX -= 1;
                            t.nextPositionX = +1;
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 1)
                        {
                            if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vrighttop || MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vrightbottom ||
                                MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vlefttop || MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vleftbottom)
                            {
                                g.Graphics.DrawImage(RotateImage(t.components[i], (float)180), new Rectangle(new Point(t.nextVagoonPositionX * 20, t.nextVagoonPositionY * 20), new Size(20, 20)));
                                t.nextVagoonPositionX -= 1;
                                t.nextPositionX = +1;
                            }
                            else if (MainForm.playingMap && t.orientations[i] != "none" && t.currentPositionX != -1 && t.currentPositionY != -1)
                                TrainCrash(t, new Coordinates() { x = t.nextVagoonPositionX, y = t.nextVagoonPositionY});
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.horizontal || MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.horizontalOver)
                        {
                            g.Graphics.DrawImage(RotateImage(t.components[i], (float)180), new Rectangle(new Point(t.nextVagoonPositionX * 20, t.nextVagoonPositionY * 20), new Size(20, 20)));
                            t.nextVagoonPositionX -= 1;
                            t.nextPositionX = +1;
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.rtopleft || (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 2 &&
                            MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vtopleft) || (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 2 &&
                            MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vlefttop))
                        {
                            g.Graphics.DrawImage(RotateImage(t.components[i], (float)135), new Rectangle(new Point(t.nextVagoonPositionX * 20 - 6, t.nextVagoonPositionY * 20 - 6), new Size(25, 25)));
                            t.nextVagoonPositionX -= 1;
                            t.nextPositionY = -1;
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.rleftbottom || (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 2 &&
                            MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vleftbottom) || (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 2 &&
                            MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vbottomleft))
                        {
                            g.Graphics.DrawImage(RotateImage(t.components[i], (float)225), new Rectangle(new Point(t.nextVagoonPositionX * 20 - 6, t.nextVagoonPositionY * 20), new Size(25, 25)));
                            t.nextVagoonPositionX -= 1;
                            t.nextPositionY = +1;
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.verticalOver)
                        {
                            t.nextVagoonPositionX -= 1;
                            t.nextPositionX = +1;
                        }
                        else if (MainForm.playingMap && t.orientations[i] != "none" && t.currentPositionX != -1 && t.currentPositionY != -1)
                            TrainCrash(t, new Coordinates() { x = t.nextVagoonPositionX, y = t.nextVagoonPositionY});
                    }
                    else if (t.orientations[i] == "nahoru")
                    {
                        if ((MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 3 || MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 4) && MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].ways[0, 2] == true)
                        {
                            t.nextVagoonPositionY += 1;
                            t.nextPositionY = -1;
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 1)
                        {
                            if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vbottomright || MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vbottomleft ||
                                MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vtopleft || MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vtopright)
                            {
                                g.Graphics.DrawImage(RotateImage(t.components[i], (float)90), new Rectangle(new Point(t.nextVagoonPositionX * 20, t.nextVagoonPositionY * 20), new Size(20, 20)));
                                t.nextVagoonPositionY += 1;
                                t.nextPositionY = -1;
                            }
                            else if (MainForm.playingMap && t.orientations[i] != "none" && t.currentPositionX != -1 && t.currentPositionY != -1)
                                TrainCrash(t, new Coordinates() { x = t.nextVagoonPositionX, y = t.nextVagoonPositionY});
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vertical || MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.verticalOver)
                        {
                            g.Graphics.DrawImage(RotateImage(t.components[i], (float)90), new Rectangle(new Point(t.nextVagoonPositionX * 20, t.nextVagoonPositionY * 20), new Size(20, 20)));
                            t.nextVagoonPositionY += 1;
                            t.nextPositionY = -1;
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.rleftbottom || (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 2 &&
                            MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vbottomleft) || (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 2 &&
                            MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vleftbottom))
                        {
                            g.Graphics.DrawImage(RotateImage(t.components[i], (float)45), new Rectangle(new Point(t.nextVagoonPositionX * 20 - 4, t.nextVagoonPositionY * 20 + 2), new Size(25, 25)));
                            t.nextVagoonPositionY += 1;
                            t.nextPositionX = -1;
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.rbottomright || (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 2 &&
                            MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vrightbottom) || (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 2 &&
                            MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vbottomright))
                        {
                            g.Graphics.DrawImage(RotateImage(t.components[i], (float)135), new Rectangle(new Point(t.nextVagoonPositionX * 20, t.nextVagoonPositionY * 20), new Size(25, 25)));
                            t.nextVagoonPositionY += 1;
                            t.nextPositionX = +1;
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.horizontalOver)
                        {
                            t.nextVagoonPositionY += 1;
                            t.nextPositionY = -1;
                        }
                        else if (MainForm.playingMap && t.orientations[i] != "none" && t.currentPositionX != -1 && t.currentPositionY != -1)
                            TrainCrash(t, new Coordinates() { x = t.nextVagoonPositionX, y = t.nextVagoonPositionY});
                    }
                    else if (t.orientations[i] == "dolu")
                    {
                        if ((MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 3 || MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 4) && MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].ways[0, 0] == true)
                        {
                            t.nextVagoonPositionY -= 1;
                            t.nextPositionY = +1;
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 1)
                        {
                            if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vbottomright || MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vbottomleft ||
                                MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vtopleft || MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vtopright)
                            {
                                g.Graphics.DrawImage(RotateImage(t.components[i], (float)270), new Rectangle(new Point(t.nextVagoonPositionX * 20, t.nextVagoonPositionY * 20), new Size(20, 20)));
                                t.nextVagoonPositionY -= 1;
                                t.nextPositionY = +1;
                            }
                            else if (MainForm.playingMap && t.orientations[i] != "none" && t.currentPositionX != -1 && t.currentPositionY != -1)
                                TrainCrash(t, new Coordinates() { x = t.nextVagoonPositionX, y = t.nextVagoonPositionY});
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vertical || MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.verticalOver)
                        {
                            g.Graphics.DrawImage(RotateImage(t.components[i], (float)270), new Rectangle(new Point(t.nextVagoonPositionX * 20, t.nextVagoonPositionY * 20), new Size(20, 20)));
                            t.nextVagoonPositionY -= 1;
                            t.nextPositionY = +1;
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.rtopleft || (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 2 &&
                            MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vlefttop) || (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 2 &&
                            MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vtopleft))
                        {
                            g.Graphics.DrawImage(RotateImage(t.components[i], (float)315), new Rectangle(new Point(t.nextVagoonPositionX * 20 - 5, t.nextVagoonPositionY * 20 - 5), new Size(25, 25)));
                            t.nextVagoonPositionY -= 1;
                            t.nextPositionX = -1;
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.rrighttop || (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 2 &&
                            MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vrighttop) || (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].BlockInfo == 2 &&
                            MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.vtopright))
                        {
                            g.Graphics.DrawImage(RotateImage(t.components[i], (float)225), new Rectangle(new Point(t.nextVagoonPositionX * 20, t.nextVagoonPositionY * 20 - 6), new Size(25, 25)));
                            t.nextVagoonPositionY -= 1;
                            t.nextPositionX = +1;
                        }
                        else if (MainForm.map[t.nextVagoonPositionX, t.nextVagoonPositionY].Image == MainForm.horizontalOver)
                        {
                            t.nextVagoonPositionY -= 1;
                            t.nextPositionY = +1;
                        }
                        else if (MainForm.playingMap && t.orientations[i] != "none" && t.currentPositionX != -1 && t.currentPositionY != -1)
                            TrainCrash(t, new Coordinates() { x = t.nextVagoonPositionX, y = t.nextVagoonPositionY});
                    }
                    else
                    {

                        //BOOOM
                    }

                    if (t.offSchedule == false && (t.trainSchedule[time].positionX != t.currentPositionX || t.trainSchedule[time].positionY != t.currentPositionY) && t.trainSchedule[time].positionX != -1 && t.trainSchedule[time].positionY != -1 &&
                        (t.trainSchedule[time].positionX != t.currentPositionX + t.nextPositionX || t.trainSchedule[time].positionY != t.currentPositionY + t.nextPositionY) &&
                        (t.trainSchedule[time].positionX != t.nextVagoonPositionX || t.trainSchedule[time].positionY != t.nextVagoonPositionY) &&
                        (t.trainSchedule[time].positionX != t.nextVagoonPositionX + t.nextPositionX || t.trainSchedule[time].positionY != t.nextVagoonPositionY+ t.nextPositionY)) //sometimes triggered due to timer inaccuracy, and possibly still can, if too many threads...
                    {
                        t.offSchedule = true;
                        trainsOffSchedule.Add(t);
                    }
                }
            }
        }

        //function found on the internet to rotate bitmap
        public static Bitmap RotateImage(Bitmap b, float Angle)
        {
            // The original bitmap needs to be drawn onto a new bitmap which will probably be bigger 
            
            float wOver2 = b.Width / 2.0f;
            float hOver2 = b.Height / 2.0f;
            float radians = -(float)(Angle / 180.0 * Math.PI);
            // Get the coordinates of the corners, taking the origin to be the centre of the bitmap.
            PointF[] corners = new PointF[]{
            new PointF(-wOver2, -hOver2),
            new PointF(+wOver2, -hOver2),
            new PointF(+wOver2, +hOver2),
            new PointF(-wOver2, +hOver2)
        };

            for (int i = 0; i < 4; i++)
            {
                PointF p = corners[i];
                PointF newP = new PointF((float)(p.X * Math.Cos(radians) - p.Y * Math.Sin(radians)), (float)(p.X * Math.Sin(radians) + p.Y * Math.Cos(radians)));
                corners[i] = newP;
            }

            // Find the min and max x and y coordinates.
            float minX = corners[0].X;
            float maxX = minX;
            float minY = corners[0].Y;
            float maxY = minY;
            for (int i = 1; i < 4; i++)
            {
                PointF p = corners[i];
                minX = Math.Min(minX, p.X);
                maxX = Math.Max(maxX, p.X);
                minY = Math.Min(minY, p.Y);
                maxY = Math.Max(maxY, p.Y);
            }
            

            // Get the size of the new bitmap.
            SizeF newSize = new SizeF(maxX - minX, maxY - minY);
            // ...and create it.
            Bitmap returnBitmap = new Bitmap((int)Math.Ceiling(newSize.Width), (int)Math.Ceiling(newSize.Height));
            returnBitmap.SetResolution(b.HorizontalResolution, b.VerticalResolution);
            // Now draw the old bitmap on it.
            using (Graphics g = Graphics.FromImage(returnBitmap))
            {
                g.TranslateTransform(newSize.Width / 2.0f, newSize.Height / 2.0f);
                g.RotateTransform(Angle);
                g.TranslateTransform(-b.Width / 2.0f, -b.Height / 2.0f);

                g.DrawImage(b, 0, 0);
            }

            return returnBitmap;
        }
    }

    public class Coordinates {
        public int x, y;
    }

    //main Train class
    public class Train
    {
        public static Bitmap smallTrain = Properties.Resources.osobak;
        static Bitmap vagoon = Properties.Resources.vagon;
        static Bitmap machine = Properties.Resources.masinka;
        static Bitmap vagoon2 = Properties.Resources.vagon2;

        public Timer timer;
        public bool canMove;
        public bool playerDriven;
        public bool offSchedule;

        public int nextPositionX;
        public int nextPositionY;

        public int nextVagoonPositionX; 
        public int nextVagoonPositionY;

        public List<Coordinates> occupiedRails;

        public int currentPositionX;
        public int currentPositionY;

        public int velocity; //velocity is just to know if u can move, the real velocity handle is timer interval of the train
        public bool waiting; //state for ai trains in stations or depos



        public class ScheduleRecord
        {
            public bool start;
            public bool stop;
            public int positionX;
            public int positionY;
            public bool available;
            public bool switched;
            public string direction;
            public bool waiting;
            public int time;

            public ScheduleRecord()
            {
                 start = false;
                 stop = false;
                 positionX = -1;
                 positionY = -1;
                 available = true;
                 switched = false;
                 direction = "none";
                 waiting = true;
                 time = -1;
            }
        }

        public List<ScheduleRecord> trainSchedule = new List<ScheduleRecord>();

         
        public string name;
        public string type;
        public List<Bitmap> components = new List<Bitmap>(); //every vagoon has a string of directionInfo after it in this list
        public List<string> orientations = new List<string>();

        public bool reversing;

        public void ReverseOrientationsAndComponents() {
            if(this.reversing){
                //reverse 
            }
            else
            {
                //front
            }
        }

        public Train(string name, string type)
        {
            this.name = name;

            if (type == "Osobáček")
            {
                components.Add(smallTrain);
                orientations.Add("none");
                this.type = "smallTrain";
            }
            else if (type == "Rychlík")
            {
                components.Add(machine);
                orientations.Add("none");
                components.Add(vagoon); //statically 2 vagooons and machine per big train
                orientations.Add("none");
                components.Add(vagoon2); //h
                orientations.Add("none");
                this.type = "bigTrain";
            }

            for (int i = 0; i < 24*60 + 3; i++) //3 is a reserve
            {
                ScheduleRecord defaut = new ScheduleRecord();
                trainSchedule.Add(defaut);
            }

            occupiedRails = new List<Coordinates>();
        }


        //Not imlemented
        /*public bool HasTime(int Time, string sourceStation, string destinationStation) //there we have to not just find if it's available, but if it can make it on time to destinated source station!
        {
            List<Train.ScheduleRecord> wayToSource;
            List<Train.ScheduleRecord> wayToDestination;
            for (int i = 0; i < TrainDriving.allTrains.Count; i++)
            {
                if (TrainDriving.allTrains[i].trainSchedule[Time % (24 * 60)].available)
                {
                    wayToSource = AITrains.CalculateDestinationTime(TrainDriving.allTrains[i], Time, new Point(TrainDriving.allTrains[i].trainSchedule[Time % (24 * 60)].positionX * 20, TrainDriving.allTrains[i].trainSchedule[Time % (23 * 60 + 59)].positionY * 20), sourceStation);
                    wayToDestination = AITrains.CalculateDestinationTime(TrainDriving.allTrains[i], (Time + wayToSource.Count) % (24 * 60), new Point(TrainDriving.allTrains[i].trainSchedule[(Time + wayToSource.Count) % (24 * 60)].positionX * 20, TrainDriving.allTrains[i].trainSchedule[(Time + wayToSource.Count) % (23 * 60 + 59)].positionY * 20), destinationStation);

                    if(TrainDriving.allTrains[i].trainSchedule[Time + .)
                }
            }
        }*/

    }
}  
