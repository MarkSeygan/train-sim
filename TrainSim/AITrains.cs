using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace TrainSim
{
    public static class AITrains
    {

        static Form inputWhereTo;

        //shows dialog to select time, sourceStation, destinationStation
        public static void SetTimeTables(TimeTableRecord ttr)
        {
            if (MainForm.Building.allFacilities.Count < 2)
            {
                MessageBox.Show("Nemáte dostatek budov pro sestavení jízdního řádu");
            }
            else
            {
                inputWhereTo = new Form();
                inputWhereTo.Width = 330;
                inputWhereTo.Height = 140;
                inputWhereTo.Controls.Add(new Label() { Text = "Odjezd z:", Top = 15, Left = 10 });
                inputWhereTo.Controls.Add(new Label() { Text = "Čas odjezdu", Top = 15, Left = 115 });
                inputWhereTo.Controls.Add(new Label() { Text = "Příjezd do:", Top = 15, Left = 220 });
                inputWhereTo.StartPosition = FormStartPosition.Manual;
                inputWhereTo.Location = new Point(MainForm.mainForm.Width / 2 - inputWhereTo.Width / 2, MainForm.mainForm.Height / 2 - inputWhereTo.Height / 2);
                ComboBox sources = new ComboBox();
                sources.Width = 80;
                sources.DropDownStyle = ComboBoxStyle.DropDownList;
                sources.Top = 40; sources.Left = 10;
                for (int i = 0; i < MainForm.Building.allFacilities.Count; i++)
                {
                    if (MainForm.Building.allFacilities[i].BlockInfo == 3 || MainForm.Building.allFacilities[i].BlockInfo == 4)
                    {
                        sources.Items.Add(MainForm.Building.allFacilities[i].facilityName);
                    }
                }
                inputWhereTo.Controls.Add(sources);


                ComboBox destinations = new ComboBox();
                destinations.Width = 80;
                destinations.Top = 40; destinations.Left = 220;
                destinations.DropDownStyle = ComboBoxStyle.DropDownList;
                for (int i = 0; i < MainForm.Building.allFacilities.Count; i++)
                {
                    if (MainForm.Building.allFacilities[i].BlockInfo == 3 || MainForm.Building.allFacilities[i].BlockInfo == 4)
                    {
                        destinations.Items.Add(MainForm.Building.allFacilities[i].facilityName);
                    }
                }
                inputWhereTo.Controls.Add(destinations);

                TextBox srcTime = new TextBox();
                srcTime.Width = 80;
                srcTime.Top = 40; srcTime.Left = 115;
                inputWhereTo.Controls.Add(srcTime);

                Button ok = new Button();
                ok.Text = "Ok";
                ok.Top = 70; ok.Left = 220;
                ok.MouseClick += (thisSender, args) => destinationDialogOk(thisSender, args, sources, destinations, srcTime, null);
                inputWhereTo.Controls.Add(ok);
                inputWhereTo.ShowDialog();

            }


        }

        //handles the input of time, sourceStation, destinationStation, and calls function to determine time when train reaches the destionation
        public static void destinationDialogOk(object sender, EventArgs e, ComboBox sources, ComboBox destinations, TextBox srcTime, List<string> presetTrain)
        {
            int time = -1;
            if (sources.Text == destinations.Text)
            {
                MessageBox.Show("Odjezdové a příjezdové město se shoduje");
            }
            else
            {
                //check if all selected
                if (srcTime.Text == "" || !System.Text.RegularExpressions.Regex.Match(srcTime.Text, "[0-2][0-9]:[0-5][0-9]").Success)
                {
                    MessageBox.Show("Zadejte čas odjezdu ve formátu hh:mm");
                }
                else if (ConvertTimeToInt(srcTime.Text) == -1)
                {
                    MessageBox.Show("Zadejte čas odjezdu ve formátu hh:mm");
                }
                else
                {
                    time = ConvertTimeToInt(srcTime.Text);
                }
            }

            //check time input
            //convert time to int 

            //then close inputWhereTo
            if (time != -1)
            {
                Point start;
                start = new Point(-1, -1);
                for (int i = 0; i < MainForm.Building.allFacilities.Count; i++)
                {
                    if (MainForm.Building.allFacilities[i].facilityName == sources.Text)
                    {
                        start = new Point(MainForm.Building.allFacilities[i].Location.X, MainForm.Building.allFacilities[i].Location.Y);
                    }
                }

                if(inputWhereTo != null)
                    inputWhereTo.Close();
                

                    Train t = null;

                    
                    //preset train is loaded from xml timetables
                    if (presetTrain != null)
                        TrainDriving.StartATrain(start, ref t, false, presetTrain);
                    else
                        TrainDriving.StartATrain(start, ref t, false, null);

                    List<Train.ScheduleRecord> theWay;
                    //gets list of blocks visited to reach the destination
                    theWay = CalculateDestinationTime(t, time, start, destinations.Text);


                    if(theWay.Count == 0)
                    {
                        MessageBox.Show("Stanice nejsou propojeny");
                    }
                    else if(theWay.Count < 24*60) {

                        if (presetTrain == null)
                        {
                            MessageBox.Show("Vlak trasu pojede " + theWay.Count + "minut");
                            if (CheckIneffectiveTrain(theWay, t.type))
                            {
                                if (MainForm.SimpleOptionsDialog("Tento vlak je velice neefektivní, doporučujeme postavit paralelní kolejnici ze stanice " + sources.Text + " do " + destinations.Text, "Postavit", "Nechat jak je") == "Postavit")
                                {
                                    //nekam returnnout
                                    MainForm.creatingMap = true;
                                    MainForm.playingMap = false;
                                    MainForm.playBox.Hide();
                                }
                            }
                        }

                        ttr.name = t.name;
                        ttr.destionationStation = destinations.Text;
                        ttr.destinationTime = (time + theWay.Count) % (24 * 60);
                        ttr.sourceStation = sources.Text;
                        ttr.sourceTime = time;
                        ttr.type = t.type;

                        //puts the schedule into Train property, the branches are for timeCorrection, when the trains go over midnight
                        for (int i = 0; i < theWay.Count; i++)
                        {
                            if(time + i > 23*60 + 59 && time + i < (23*60+59) + 3) {
                                t.trainSchedule[time + i].available = false;
                                t.trainSchedule[time + i].direction = theWay[i].direction;
                                t.trainSchedule[time + i].positionX = theWay[i].positionX;
                                t.trainSchedule[time + i].positionY = theWay[i].positionY;
                                t.trainSchedule[time + i].switched = theWay[i].switched;
                                t.trainSchedule[time + i].waiting = theWay[i].waiting;
                                t.trainSchedule[time + i].start = theWay[i].start;
                                t.trainSchedule[time + i].stop = theWay[i].stop;

                                t.trainSchedule[time + i - 24*60].available = false;
                                t.trainSchedule[time + i - 24*60].direction = theWay[i].direction;
                                t.trainSchedule[time + i - 24*60].positionX = theWay[i].positionX;
                                t.trainSchedule[time + i - 24*60].positionY = theWay[i].positionY;
                                t.trainSchedule[time + i - 24*60].switched = theWay[i].switched;
                                t.trainSchedule[time + i - 24*60].waiting = theWay[i].waiting;
                                t.trainSchedule[time + i - 24 * 60].start = theWay[i].start;
                                t.trainSchedule[time + i - 24 * 60].stop = theWay[i].stop;
                            }
                            else if (time + i > 23*60+59)
                            {
                                t.trainSchedule[time + i - 24 * 60].available = false;
                                t.trainSchedule[time + i - 24 * 60].direction = theWay[i].direction;
                                t.trainSchedule[time + i - 24 * 60].positionX = theWay[i].positionX;
                                t.trainSchedule[time + i - 24 * 60].positionY = theWay[i].positionY;
                                t.trainSchedule[time + i - 24 * 60].switched = theWay[i].switched;
                                t.trainSchedule[time + i - 24 * 60].waiting = theWay[i].waiting;
                                t.trainSchedule[time + i - 24 * 60].start = theWay[i].start;
                                t.trainSchedule[time + i - 24 * 60].stop = theWay[i].stop;
                            }
                            else
                            {
                                t.trainSchedule[time + i].available = false;
                                t.trainSchedule[time + i].direction = theWay[i].direction;
                                t.trainSchedule[time + i].positionX = theWay[i].positionX;
                                t.trainSchedule[time + i].positionY = theWay[i].positionY;
                                t.trainSchedule[time + i].switched = theWay[i].switched;
                                t.trainSchedule[time + i].waiting = theWay[i].waiting;
                                t.trainSchedule[time + i].start = theWay[i].start;
                                t.trainSchedule[time + i].stop = theWay[i].stop;
                            }
                        }
                    }
                    else {
                        MessageBox.Show("Železnice nemá pro tento spoj dostatečnou kapacitu");
                    }
                    
                    // not implemented -> even bigTrain can wait for the smallTrain
                            //if bigTrain you have to recalculate for all smallTrains behind it in allTrains
                    // not implemented 
            }
        }

        static string currentTrainTypeForDFS;

        //Tries to dfs from all exits from a station and gets the best one
        public static List<Train.ScheduleRecord> CalculateDestinationTime(Train t, int sourceTime, Point upperLeftSource, string destination) //returns destination time via ScheduleList
        {
            int timeIncrease;

            //this is for speed of the smallTrain being lower so you can count on it while estimating dest time

            currentTrainTypeForDFS = t.type;

            if (t.type == "smallTrain")
                timeIncrease = 2;
            else
                timeIncrease = 1;
            //
            
            List<List<Train.ScheduleRecord>> foundWays = new List<List<Train.ScheduleRecord>>();
            List<Train.ScheduleRecord> currentBestWay = new List<Train.ScheduleRecord>();

            int timeSpanWhileCalled = 0;

            int currentBestWayCount = 10000; // much much over than possible


            //checks all ways from the sourceStation -> it has four main branches, I commented only The first one
            for (int i = 0; i < MainForm.Building.allFacilities.Count; i++)
            {
                if(MainForm.Building.allFacilities[i].Location == upperLeftSource)
                {
                    
                    timeSpan = sourceTime;
                    visitedRails.Clear();
                    if (MainForm.Building.allFacilityPictureBoxes[i].Image == MainForm.bigStation)
                    {
                        //tries all possible outWays from station -> 4 possibilities
                        timeSpanWhileCalled = timeSpan;
                        //this loop is for the train to wait at station if it isn't safe on rails
                        while (SwitchSensitiveDFS(MainForm.Building.allFacilities[i].x + 3, MainForm.Building.allFacilities[i].y, destination, "doprava", 0, 0, 0, sourceTime, timeSpan, timeIncrease) == null)
                        {
                            visitedRails.Clear();
                            timeSpanWhileCalled = timeSpan;
                        }
                        visitedRails.Clear();
                        foundWays.Add(RepairWayRegardingToTime(SwitchSensitiveDFS(MainForm.Building.allFacilities[i].x + 3, MainForm.Building.allFacilities[i].y, destination, "doprava", 0, 0, 0, sourceTime, timeSpanWhileCalled, timeIncrease), sourceTime, t.type));
                        WorkOutTimeSpanAndVisitedRails(ref timeSpan, sourceTime, ref timeSpanWhileCalled, ref visitedRails);
                        while (SwitchSensitiveDFS(MainForm.Building.allFacilities[i].x - 1, MainForm.Building.allFacilities[i].y, destination, "doleva", 0, 0, 0, sourceTime, timeSpan , timeIncrease) == null)
                        {
                            visitedRails.Clear();
                            timeSpanWhileCalled = timeSpan;
                        }
                        visitedRails.Clear();
                        foundWays.Add(RepairWayRegardingToTime(SwitchSensitiveDFS(MainForm.Building.allFacilities[i].x - 1, MainForm.Building.allFacilities[i].y, destination, "doleva", 0, 0, 0, sourceTime, timeSpanWhileCalled , timeIncrease), sourceTime, t.type));
                        WorkOutTimeSpanAndVisitedRails(ref timeSpan, sourceTime, ref timeSpanWhileCalled, ref visitedRails);
                        while (SwitchSensitiveDFS(MainForm.Building.allFacilities[i].x + 3, MainForm.Building.allFacilities[i].y + 1, destination, "doprava", 0, 0, 0, sourceTime, timeSpan , timeIncrease) == null)
                        {
                            visitedRails.Clear();
                            timeSpanWhileCalled = timeSpan;
                        }
                        visitedRails.Clear();
                        foundWays.Add(RepairWayRegardingToTime(SwitchSensitiveDFS(MainForm.Building.allFacilities[i].x + 3, MainForm.Building.allFacilities[i].y + 1, destination, "doprava", 0, 0, 0, sourceTime, timeSpanWhileCalled, timeIncrease), sourceTime, t.type));
                        WorkOutTimeSpanAndVisitedRails(ref timeSpan, sourceTime, ref timeSpanWhileCalled, ref visitedRails);
                        while (SwitchSensitiveDFS(MainForm.Building.allFacilities[i].x - 1, MainForm.Building.allFacilities[i].y + 1, destination, "doleva", 0, 0, 0, sourceTime, timeSpan , timeIncrease) == null )
                        {
                            visitedRails.Clear();
                            timeSpanWhileCalled = timeSpan;
                        }
                        visitedRails.Clear();
                        foundWays.Add(RepairWayRegardingToTime(SwitchSensitiveDFS(MainForm.Building.allFacilities[i].x - 1, MainForm.Building.allFacilities[i].y + 1, destination, "doleva", 0, 0, 0, sourceTime, timeSpanWhileCalled, timeIncrease),sourceTime,t.type));

                        foreach (List<Train.ScheduleRecord> way in foundWays)
                        {
                            //gets the best way
                            if(way.Count != 0 && way.Count < currentBestWayCount)
                            {
                                currentBestWayCount = way.Count;
                                currentBestWay = way;
                            }
                        }
                    }
                    else if (MainForm.Building.allFacilityPictureBoxes[i].Image == MainForm.bigStationVert)
                    {
                        WorkOutTimeSpanAndVisitedRails(ref timeSpan, sourceTime, ref timeSpanWhileCalled, ref visitedRails);
                        while(SwitchSensitiveDFS(MainForm.Building.allFacilities[i].x, MainForm.Building.allFacilities[i].y - 1, destination, "nahoru", 0, 0, 0, sourceTime, timeSpan, timeIncrease) == null)
                        {
                            visitedRails.Clear();
                            timeSpanWhileCalled = timeSpan;
                        }
                        visitedRails.Clear();
                        foundWays.Add(RepairWayRegardingToTime(SwitchSensitiveDFS(MainForm.Building.allFacilities[i].x, MainForm.Building.allFacilities[i].y - 1, destination, "nahoru", 0, 0, 0, sourceTime, timeSpanWhileCalled, timeIncrease),sourceTime,t.type));
                        WorkOutTimeSpanAndVisitedRails(ref timeSpan, sourceTime, ref timeSpanWhileCalled, ref visitedRails);
                        while(SwitchSensitiveDFS(MainForm.Building.allFacilities[i].x, MainForm.Building.allFacilities[i].y + 3, destination, "dolu", 0, 0, 0, sourceTime, timeSpan, timeIncrease) == null){
                            visitedRails.Clear();
                            timeSpanWhileCalled = timeSpan;
                        }
                        visitedRails.Clear();
                        foundWays.Add(RepairWayRegardingToTime(SwitchSensitiveDFS(MainForm.Building.allFacilities[i].x, MainForm.Building.allFacilities[i].y + 3, destination, "dolu", 0, 0, 0, sourceTime, timeSpanWhileCalled, timeIncrease), sourceTime, t.type));
                        WorkOutTimeSpanAndVisitedRails(ref timeSpan, sourceTime, ref timeSpanWhileCalled, ref visitedRails);
                        while(SwitchSensitiveDFS(MainForm.Building.allFacilities[i].x + 1, MainForm.Building.allFacilities[i].y - 1, destination, "nahoru", 0, 0, 0, sourceTime, timeSpan, timeIncrease) == null) {
                            visitedRails.Clear();
                            timeSpanWhileCalled = timeSpan;
                        }
                        visitedRails.Clear();
                        foundWays.Add(RepairWayRegardingToTime(SwitchSensitiveDFS(MainForm.Building.allFacilities[i].x + 1, MainForm.Building.allFacilities[i].y - 1, destination, "nahoru", 0, 0, 0, sourceTime, timeSpanWhileCalled, timeIncrease), sourceTime, t.type));
                        WorkOutTimeSpanAndVisitedRails(ref timeSpan, sourceTime, ref timeSpanWhileCalled, ref visitedRails);
                        while (SwitchSensitiveDFS(MainForm.Building.allFacilities[i].x + 1, MainForm.Building.allFacilities[i].y + 3, destination, "dolu", 0, 0, 0, sourceTime, timeSpan, timeIncrease) == null)
                        {
                            visitedRails.Clear();
                            timeSpanWhileCalled = timeSpan;
                        }
                        visitedRails.Clear();
                        foundWays.Add(RepairWayRegardingToTime(SwitchSensitiveDFS(MainForm.Building.allFacilities[i].x + 1, MainForm.Building.allFacilities[i].y + 3, destination, "dolu", 0, 0, 0, sourceTime, timeSpanWhileCalled, timeIncrease), sourceTime, t.type));

                        foreach (List<Train.ScheduleRecord> way in foundWays)
                        {
                            if (way.Count != 0 && way.Count < currentBestWayCount)
                            {
                                currentBestWayCount = way.Count;
                                currentBestWay = way;
                            }
                        }
                    }
                    else if(MainForm.Building.allFacilityPictureBoxes[i].Image == MainForm.smallStation)
                    {
                        WorkOutTimeSpanAndVisitedRails(ref timeSpan, sourceTime, ref timeSpanWhileCalled, ref visitedRails);
                        while(SwitchSensitiveDFS(MainForm.Building.allFacilities[i].x + 2, MainForm.Building.allFacilities[i].y, destination, "doprava", 0, 0, 0, sourceTime, timeSpan, timeIncrease) == null) {
                            visitedRails.Clear();
                            timeSpanWhileCalled = timeSpan;
                        }
                        visitedRails.Clear();
                        foundWays.Add(RepairWayRegardingToTime(SwitchSensitiveDFS(MainForm.Building.allFacilities[i].x + 2, MainForm.Building.allFacilities[i].y, destination, "doprava", 0, 0, 0, sourceTime, timeSpanWhileCalled, timeIncrease), sourceTime, t.type));
                        WorkOutTimeSpanAndVisitedRails(ref timeSpan, sourceTime, ref timeSpanWhileCalled, ref visitedRails);
                        while (SwitchSensitiveDFS(MainForm.Building.allFacilities[i].x - 1, MainForm.Building.allFacilities[i].y, destination, "doleva", 0, 0, 0, sourceTime, timeSpan, timeIncrease) == null)
                        {
                            visitedRails.Clear();
                            timeSpanWhileCalled = timeSpan;
                        }
                        visitedRails.Clear();
                        foundWays.Add(RepairWayRegardingToTime(SwitchSensitiveDFS(MainForm.Building.allFacilities[i].x - 1, MainForm.Building.allFacilities[i].y, destination, "doleva", 0, 0, 0, sourceTime, timeSpanWhileCalled, timeIncrease), sourceTime, t.type));

                        foreach (List<Train.ScheduleRecord> way in foundWays)
                        {
                            if (way.Count != 0 && way.Count < currentBestWayCount)
                            {
                                currentBestWayCount = way.Count;
                                currentBestWay = way;
                            }
                        }
                    }
                    else if (MainForm.Building.allFacilityPictureBoxes[i].Image == MainForm.smallStationVert)
                    {
                        WorkOutTimeSpanAndVisitedRails(ref timeSpan, sourceTime, ref timeSpanWhileCalled, ref visitedRails);
                        while (SwitchSensitiveDFS(MainForm.Building.allFacilities[i].x, MainForm.Building.allFacilities[i].y - 1, destination, "nahoru", 0, 0, 0, sourceTime, timeSpan, timeIncrease) == null)
                        {
                            visitedRails.Clear();
                            timeSpanWhileCalled = timeSpan;
                        }
                        visitedRails.Clear();
                        foundWays.Add(RepairWayRegardingToTime(SwitchSensitiveDFS(MainForm.Building.allFacilities[i].x, MainForm.Building.allFacilities[i].y - 1, destination, "nahoru", 0, 0, 0, sourceTime, timeSpanWhileCalled, timeIncrease), sourceTime, t.type));
                        WorkOutTimeSpanAndVisitedRails(ref timeSpan, sourceTime, ref timeSpanWhileCalled, ref visitedRails);
                        while (SwitchSensitiveDFS(MainForm.Building.allFacilities[i].x, MainForm.Building.allFacilities[i].y + 2, destination, "dolu", 0, 0, 0, sourceTime, timeSpan, timeIncrease) == null) {
                            visitedRails.Clear();
                            timeSpanWhileCalled = timeSpan;
                        }
                        visitedRails.Clear();
                        foundWays.Add(RepairWayRegardingToTime(SwitchSensitiveDFS(MainForm.Building.allFacilities[i].x, MainForm.Building.allFacilities[i].y + 2, destination, "dolu", 0, 0, 0, sourceTime, timeSpanWhileCalled, timeIncrease), sourceTime, t.type));

                        foreach (List<Train.ScheduleRecord> way in foundWays)
                        {
                            if (way.Count != 0 && way.Count < currentBestWayCount)
                            {
                                currentBestWayCount = way.Count;
                                currentBestWay = way;
                            }
                        }
                    }
                    else if (MainForm.Building.allFacilityPictureBoxes[i].Image == MainForm.depo)
                    {
                        foundWays.Add(RepairWayRegardingToTime(SwitchSensitiveDFS(MainForm.Building.allFacilities[i].x, MainForm.Building.allFacilities[i].y, destination, "doprava", 0, 0, 0, sourceTime, timeSpan, timeIncrease),sourceTime,t.type));
                        foundWays.Add(RepairWayRegardingToTime(SwitchSensitiveDFS(MainForm.Building.allFacilities[i].x, MainForm.Building.allFacilities[i].y, destination, "doleva", 0, 0, 0, sourceTime, timeSpan, timeIncrease),sourceTime,t.type));

                        foreach (List<Train.ScheduleRecord> way in foundWays)
                        {
                            if (way.Count != 0 && way.Count < currentBestWayCount)
                            {
                                currentBestWayCount = way.Count;
                                currentBestWay = way;
                            }
                        }
                    }
                    else if (MainForm.Building.allFacilityPictureBoxes[i].Image == MainForm.depoVert)
                    {
                        foundWays.Add(RepairWayRegardingToTime(SwitchSensitiveDFS(MainForm.Building.allFacilities[i].x, MainForm.Building.allFacilities[i].y, destination, "nahoru", 0, 0, 0, sourceTime, timeSpan, timeIncrease),sourceTime,t.type));
                        foundWays.Add(RepairWayRegardingToTime(SwitchSensitiveDFS(MainForm.Building.allFacilities[i].x, MainForm.Building.allFacilities[i].y, destination, "dolu", 0, 0, 0, sourceTime, timeSpan, timeIncrease),sourceTime,t.type));

                        foreach (List<Train.ScheduleRecord> way in foundWays)
                        {
                            if (way.Count != 0 && way.Count < currentBestWayCount)
                            {
                                currentBestWayCount = way.Count;
                                currentBestWay = way;
                            }
                        }
                    }
                }
            }

            visitedRails.Clear();
            currentBestTime = -1;
            currentTrainTypeForDFS = "";

            if (currentBestWayCount == 10000)
                return new List<Train.ScheduleRecord>();
            return currentBestWay;
        }

        static void WorkOutTimeSpanAndVisitedRails(ref int timeSpan, int sourceTime, ref int timeSpanWhileCalled, ref List<VisitedRails> visitedRails)
        {
            timeSpan = sourceTime;
            timeSpanWhileCalled = timeSpan;
            visitedRails.Clear();
        }



        struct VisitedRails {
            public int x, y;
            public string orientation;
            public int time;
        }
        static List<VisitedRails> visitedRails = new List<VisitedRails>();
        static int currentBestTime = -1;
        static int timeSpan;
        static bool nullFlag;


        //main DFS
        static List<Train.ScheduleRecord> SwitchSensitiveDFS(int x, int y, string destination, string orientation, int nextPositionX, int nextPositionY, int currentDistance, int sourceTime, int time,int timeIncrease)
        {
            int currentPositionX = x;
            int currentPositionY = y;

            try
            {
                Block testBounds = MainForm.map[currentPositionX + nextPositionX, currentPositionY + nextPositionY];
                testBounds = MainForm.map[currentPositionX, currentPositionY];
            }
            catch (IndexOutOfRangeException)
            {
                //BOOOM
                return new List<Train.ScheduleRecord>();
            }

            int correctedTime;

            while (true)
            {
                Continue: //to get out if the train must wait for another train
               
                //to move
                if (nextPositionX > 0)
                {
                    orientation = "doprava";
                    currentPositionX += nextPositionX;
                    nextPositionX = 0;
                }
                if (nextPositionX < 0)
                {
                    orientation = "doleva";
                    currentPositionX += nextPositionX;
                    nextPositionX = 0;
                }
                if (nextPositionY > 0)
                {
                    orientation = "dolu";
                    currentPositionY += nextPositionY;
                    nextPositionY = 0;
                }
                if (nextPositionY < 0)
                {
                    orientation = "nahoru";
                    currentPositionY += nextPositionY;
                    nextPositionY = 0;
                }

                /* try
                {
                    //
                }
                catch (ArgumentOutOfRangeException)
                {
                    //to look back 24*60
                }*/

                // check if you're not in way for otherTrain
                if (time > 23 * 60 + 59)
                    time = time - 24 * 60;
                
                for (int i = 0; i < TrainDriving.allTrains.Count; i++)
                {
                    if (TrainDriving.allTrains[i].trainSchedule[time].positionX == currentPositionX && TrainDriving.allTrains[i].trainSchedule[time].positionY == currentPositionY &&
                        (MainForm.map[currentPositionX, currentPositionY].Image != MainForm.verticalOver || MainForm.map[currentPositionX, currentPositionY].Image != MainForm.horizontalOver ||
                    !IsPerpendicular(TrainDriving.allTrains[i].trainSchedule[time].direction, orientation)))
                    {
                        time = time + timeIncrease;
                        if (visitedRails.Count > 0)
                            visitedRails.RemoveAt(visitedRails.Count - 1);
                        timeSpan = time;
                        nextPositionX = 0; nextPositionY = 0;
                        return null;
                    }

                    if (time == 0 || time == 1)
                        correctedTime = 1439;
                    else
                        correctedTime = time;
                    if (TrainDriving.allTrains[i].type == "bigTrain" && ((TrainDriving.allTrains[i].trainSchedule[correctedTime].positionX == currentPositionX && TrainDriving.allTrains[i].trainSchedule[correctedTime].positionY == currentPositionY) ||
                        (TrainDriving.allTrains[i].trainSchedule[correctedTime - 1].positionX == currentPositionX && TrainDriving.allTrains[i].trainSchedule[correctedTime - 1].positionY == currentPositionY) || 
                        ((TrainDriving.allTrains[i].trainSchedule[correctedTime - 2].positionX == currentPositionX && TrainDriving.allTrains[i].trainSchedule[correctedTime - 2].positionY == currentPositionY) && correctedTime == time)) &&
                        (MainForm.map[currentPositionX, currentPositionY].Image != MainForm.verticalOver || MainForm.map[currentPositionX, currentPositionY].Image != MainForm.horizontalOver))
                    {
                        time = time + timeIncrease;
                        if (visitedRails.Count > 0)
                            visitedRails.RemoveAt(visitedRails.Count - 1);
                        nullFlag = true;
                        timeSpan = time;
                        nextPositionX = 0; nextPositionY = 0;
                        return null;
                    }
                }

                

                List<Train.ScheduleRecord> l = new List<Train.ScheduleRecord>();

                try
                {

                    // to check if the train hasn't visited the rail with better time
                    for (int i = 0; i < visitedRails.Count; i++)
                    {
                        if (visitedRails[i].x == currentPositionX && visitedRails[i].y == currentPositionY && visitedRails[i].orientation == orientation && visitedRails[i].time < time)
                        {
                            return new List<Train.ScheduleRecord>();
                        }
                    }
                    visitedRails.Add(new VisitedRails() { x = x, y = y, orientation = orientation, time = time });


                    //to check if the train isn't in destionation station
                    if (MainForm.map[currentPositionX, currentPositionY].facilityName == destination && (currentBestTime == -1 || currentBestTime >= time))
                    {
                        if (IsFacilityReachableFromThisSide(currentPositionX, currentPositionY, orientation))
                        {
                            l = new List<Train.ScheduleRecord>();
                            l.Add(new Train.ScheduleRecord() { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = false, direction = orientation, time = time });
                            return l;
                        }
                    }
                    bool canDo = false;

                    //huge branches for all the directions and possible blocks the train can encounter
                    if (orientation == "doleva")
                    {
                        if (MainForm.map[currentPositionX, currentPositionY].BlockInfo == 1 || MainForm.map[currentPositionX, currentPositionY].BlockInfo == 2)
                        {
                            if (MainForm.map[currentPositionX, currentPositionY].Image == MainForm.vlefttop || MainForm.map[currentPositionX, currentPositionY].Image == MainForm.vleftbottom)
                            {


                                nextPositionX = -1;
                                if(CheckIfHaveToWait(currentPositionX+nextPositionX, currentPositionY+nextPositionY, orientation, ref time, timeIncrease, ref nextPositionX, ref nextPositionY, ref visitedRails)) 
 								 goto Continue; 
 	 		 	 	 	 	    l = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, orientation, nextPositionX, nextPositionY, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease); 
 								if(l.Count>0)
                                {
                                    nextPositionX = 0;
                                    for (int i = 0; i < TrainDriving.allTrains.Count; i++)
                                    {
                                        for (int j = 0; j <= 3; j++)
                                        {
                                           if(time > 23*60+59) 
 												 correctedTime = time - 24*60; 
 											else 
												 correctedTime = time; 
 												if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionX == currentPositionX && TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionY == currentPositionY)
                                                canDo = true;
                                            if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].switched && canDo)
                                            {
                                                time = time + timeIncrease; 
 	 	 	 	 	 	                    	visitedRails.RemoveAt(visitedRails.Count - 1);
                                                goto Continue;
                                            }
                                        }
                                        canDo = false;
                                    }
                                    l.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = false, direction = orientation, time = time });
                                }
                                return l;
                            }
                            if (MainForm.map[currentPositionX, currentPositionY].Image == MainForm.vbottomright)
                            {
                                nextPositionY = +1;
                               if(CheckIfHaveToWait(currentPositionX+nextPositionX, currentPositionY+nextPositionY, orientation, ref time, timeIncrease, ref nextPositionX, ref nextPositionY, ref visitedRails)) 
 								 goto Continue; 
 	 		 	 	 	 	 l = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, orientation, nextPositionX, nextPositionY, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease); 
 								 if(l.Count>0)
                                {
                                    nextPositionY = 0;
                                    for (int i = 0; i < TrainDriving.allTrains.Count; i++)
                                    {
                                        for (int j = 0; j <= 3; j++)
                                        {
                                           if(time > 23*60+59) 
 												 correctedTime = time - 24*60; 
 											else 
												 correctedTime = time; 
 												if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionX == currentPositionX && TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionY == currentPositionY)
                                                canDo = true;
                                            if (!TrainDriving.allTrains[i].trainSchedule[correctedTime + j].switched && canDo)
                                            {
                                                time = time + timeIncrease; 
 	 	 	 	 	 	 	                    visitedRails.RemoveAt(visitedRails.Count - 1);
                                                goto Continue;
                                            }
                                        }
                                        canDo = false;
                                    }
                                    l.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = true, direction = orientation, time = time });
                                }
                                return l;
                            }
                            if (MainForm.map[currentPositionX, currentPositionY].Image == MainForm.vtopright)
                            {
                                nextPositionY = -1;
                               if(CheckIfHaveToWait(currentPositionX+nextPositionX, currentPositionY+nextPositionY, orientation, ref time, timeIncrease, ref nextPositionX, ref nextPositionY, ref visitedRails)) 
 								 goto Continue; 
 	 		 	 	 	 	 l = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, orientation, nextPositionX, nextPositionY, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease); 
 								 if(l.Count>0)
                                {
                                    nextPositionY = 0;
                                    for (int i = 0; i < TrainDriving.allTrains.Count; i++)
                                    {
                                        for (int j = 0; j <= 3; j++)
                                        {
                                           if(time > 23*60+59) 
 												 correctedTime = time - 24*60; 
 											else 
												 correctedTime = time; 
 												if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionX == currentPositionX && TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionY == currentPositionY)
                                                canDo = true;
                                            if (!TrainDriving.allTrains[i].trainSchedule[correctedTime + j].switched && canDo)
                                            {
                                                time = time + timeIncrease; 
 	 	 	 	 	 	 	                    visitedRails.RemoveAt(visitedRails.Count - 1);
                                                goto Continue;
                                            }
                                        }
                                        canDo = false;
                                    }
                                    l.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = true, direction = orientation, time = time });
                                }
                                return l;
                            }
                            if (MainForm.map[currentPositionX, currentPositionY].Image == MainForm.vrighttop)
                            {
                                List<Train.ScheduleRecord> straight = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, orientation, -1, 0, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease);
                                List<Train.ScheduleRecord> turned = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, orientation, 0, -1, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease);
                                if (straight != null && straight.Count > 0 && (turned == null || turned.Count == 0 || straight[0].time <= turned[0].time))
                                {
                                    for (int i = 0; i < TrainDriving.allTrains.Count; i++)
                                    {
                                        for (int j = 0; j <= 3; j++)
                                        {
                                            if (time > 23 * 60 + 59)
                                                correctedTime = time - 24 * 60;
                                            else
                                                correctedTime = time;
                                            if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionX == currentPositionX && TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionY == currentPositionY)
                                                canDo = true;
                                            if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].switched && canDo)
                                            {
                                                time = time + timeIncrease;
                                                visitedRails.RemoveAt(visitedRails.Count - 1);
                                                goto Continue;
                                            }
                                        }
                                        canDo = false;
                                    }

                                    straight.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = false, direction = orientation, time = time });
                                    return straight;
                                }
                                else if (turned != null && turned.Count > 0 && (straight == null || straight.Count == 0 || turned[0].time < straight[0].time))
                                {
                                    for (int i = 0; i < TrainDriving.allTrains.Count; i++)
                                    {
                                        for (int j = 0; j <= 3; j++)
                                        {
                                            if (time > 23 * 60 + 59)
                                                correctedTime = time - 24 * 60;
                                            else
                                                correctedTime = time;
                                            if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionX == currentPositionX && TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionY == currentPositionY)
                                                canDo = true;
                                            if (!TrainDriving.allTrains[i].trainSchedule[correctedTime + j].switched && canDo)
                                            {
                                                time = time + timeIncrease;
                                                visitedRails.RemoveAt(visitedRails.Count - 1);
                                                goto Continue;
                                            }
                                        }
                                        canDo = false;
                                    }
                                    turned.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = true, direction = orientation, time = time });
                                    return turned;
                                }
                                else if (straight == null && turned == null)
                                    throw new NullReferenceException();
                                else
                                {
                                    return new List<Train.ScheduleRecord>();
                                }
                            }
                            if (MainForm.map[currentPositionX, currentPositionY].Image == MainForm.vrightbottom)
                            {
                                List<Train.ScheduleRecord> straight = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, orientation, -1, 0, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease);
                                List<Train.ScheduleRecord> turned = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, orientation, 0, +1, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease);
                                if (straight != null && straight.Count > 0 && ( turned == null || turned.Count == 0 || straight[0].time <= turned[0].time))
                                {
                                    for (int i = 0; i < TrainDriving.allTrains.Count; i++)
                                    {
                                        for (int j = 0; j <= 3; j++)
                                        {
                                           if(time > 23*60+59) 
 												 correctedTime = time - 24*60; 
 											else 
												 correctedTime = time; 
 												if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionX == currentPositionX && TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionY == currentPositionY)
                                                canDo = true;
                                            if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].switched && canDo)
                                            {
                                                time = time + timeIncrease; 
 	 	 	 	 	 	 	                    visitedRails.RemoveAt(visitedRails.Count - 1);
                                                goto Continue;
                                            }
                                        }
                                        canDo = false;
                                    }
                                    straight.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = false, direction = orientation, time = time });
                                    return straight;
                                }
                                else if (turned != null && turned.Count > 0 && ( straight == null || straight.Count == 0 || turned[0].time < straight[0].time))
                                {
                                    for (int i = 0; i < TrainDriving.allTrains.Count; i++)
                                    {
                                        for (int j = 0; j <= 3; j++)
                                        {
                                           if(time > 23*60+59) 
 												 correctedTime = time - 24*60; 
 											else 
												 correctedTime = time; 
 												if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionX == currentPositionX && TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionY == currentPositionY)
                                                canDo = true;
                                            if (!TrainDriving.allTrains[i].trainSchedule[correctedTime + j].switched && canDo)
                                            {
                                                time = time + timeIncrease; 
 	 	 	 	 	 	 	                    visitedRails.RemoveAt(visitedRails.Count - 1);
                                                goto Continue;
                                            }
                                        }
                                        canDo = false;
                                    }
                                    turned.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = true, direction = orientation, time = time });
                                    return turned;
                                }
                                else if (straight == null && turned == null)
                                    throw new NullReferenceException();
                                else
                                {
                                    return new List<Train.ScheduleRecord>();
                                }

                            }
                        }
                        else if (MainForm.map[currentPositionX, currentPositionY].Image == MainForm.horizontal || MainForm.map[currentPositionX, currentPositionY].Image == MainForm.horizontalOver ||
                            MainForm.map[currentPositionX, currentPositionY].Image == MainForm.verticalOver || (MainForm.map[currentPositionX, currentPositionY].facilityName != null && MainForm.map[currentPositionX, currentPositionY].ways[0,1] == true))
                        {
                            nextPositionX = -1;
                            if(CheckIfHaveToWait(currentPositionX+nextPositionX, currentPositionY+nextPositionY, orientation, ref time, timeIncrease, ref nextPositionX, ref nextPositionY, ref visitedRails)) 
 								 goto Continue; 
 	 		 	 	 	 	 l = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, orientation, nextPositionX, nextPositionY, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease); 
 							if(l.Count>0)
                            {
                                l.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = false, direction = orientation, time = time });
                            }
                            return l;
                        }
                        else if (MainForm.map[currentPositionX, currentPositionY].Image == MainForm.rrighttop)
                        {
                            nextPositionY = -1;
                            if(CheckIfHaveToWait(currentPositionX+nextPositionX, currentPositionY+nextPositionY, orientation, ref time, timeIncrease, ref nextPositionX, ref nextPositionY, ref visitedRails)) 
 								 goto Continue; 
 	 		 	 	 	 	 l = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, orientation, nextPositionX, nextPositionY, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease); 
 							if(l.Count>0)
                            {
                                l.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = false, direction = orientation, time = time });
                            }
                            return l;
                        }
                        else if (MainForm.map[currentPositionX, currentPositionY].Image == MainForm.rbottomright)
                        {
                            nextPositionY = +1;
                            if(CheckIfHaveToWait(currentPositionX+nextPositionX, currentPositionY+nextPositionY, orientation, ref time, timeIncrease, ref nextPositionX, ref nextPositionY, ref visitedRails)) 
 								 goto Continue; 
 	 		 	 	 	 	 l = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, orientation, nextPositionX, nextPositionY, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease); 
 							if(l.Count>0)
                            {
                                l.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = false, direction = orientation, time = time });
                            }
                            return l;
                        }
                    }
                    else if (orientation == "doprava")
                    {
                        if (MainForm.map[currentPositionX, currentPositionY].BlockInfo == 1 || MainForm.map[currentPositionX, currentPositionY].BlockInfo == 2)
                        {
                            if (MainForm.map[currentPositionX, currentPositionY].Image == MainForm.vrighttop || MainForm.map[currentPositionX, currentPositionY].Image == MainForm.vrightbottom)
                            {
                                nextPositionX = +1;
                               if(CheckIfHaveToWait(currentPositionX+nextPositionX, currentPositionY+nextPositionY, orientation, ref time, timeIncrease, ref nextPositionX, ref nextPositionY, ref visitedRails)) 
 								 goto Continue; 
 	 		 	 	 	 	 l = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, orientation, nextPositionX, nextPositionY, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease); 
 								 if(l.Count>0)
                                {
                                    nextPositionX = 0;
                                    for (int i = 0; i < TrainDriving.allTrains.Count; i++)
                                    {
                                        for (int j = 0; j <= 3; j++)
                                        {
                                           if(time > 23*60+59) 
 												 correctedTime = time - 24*60; 
 											else 
												 correctedTime = time; 
 												if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionX == currentPositionX && TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionY == currentPositionY)
                                                canDo = true;
                                            if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].switched && canDo)
                                            {
                                                time = time + timeIncrease; 
 	 	 	 	 	 	 	                    visitedRails.RemoveAt(visitedRails.Count - 1);
                                                goto Continue;
                                            }
                                        }
                                        canDo = false;
                                    }
                                    l.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = false, direction = orientation, time = time });
                                }
                                return l;
                            }
                            if (MainForm.map[currentPositionX, currentPositionY].Image == MainForm.vbottomleft)
                            {
                                nextPositionY = +1;
                                if(CheckIfHaveToWait(currentPositionX+nextPositionX, currentPositionY+nextPositionY, orientation, ref time, timeIncrease, ref nextPositionX, ref nextPositionY, ref visitedRails)) 
 								 goto Continue; 
 	 		 	 	 	 	 l = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, orientation, nextPositionX, nextPositionY, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease); 
 								if(l.Count>0)
                                {
                                    nextPositionY = 0;
                                    for (int i = 0; i < TrainDriving.allTrains.Count; i++)
                                    {
                                        for (int j = 0; j <= 3; j++)
                                        {
                                           if(time > 23*60+59) 
 												 correctedTime = time - 24*60; 
 											else 
												 correctedTime = time; 
 												if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionX == currentPositionX && TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionY == currentPositionY)
                                                canDo = true;
                                            if (!TrainDriving.allTrains[i].trainSchedule[correctedTime + j].switched && canDo)
                                            {
                                                time = time + timeIncrease; 
 	 	 	 	 	 	 	                    visitedRails.RemoveAt(visitedRails.Count - 1);
                                                goto Continue;
                                            }
                                        }
                                        canDo = false;
                                    }
                                    l.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = true, direction = orientation, time = time });
                                }
                                return l;
                            }
                            if (MainForm.map[currentPositionX, currentPositionY].Image == MainForm.vtopleft)
                            {
                                nextPositionY = -1;
                                if(CheckIfHaveToWait(currentPositionX+nextPositionX, currentPositionY+nextPositionY, orientation, ref time, timeIncrease, ref nextPositionX, ref nextPositionY, ref visitedRails)) 
 								 goto Continue; 
 	 		 	 	 	 	 l = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, orientation, nextPositionX, nextPositionY, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease); 
 								if(l.Count>0)
                                {
                                    nextPositionY = 0;
                                    for (int i = 0; i < TrainDriving.allTrains.Count; i++)
                                    {
                                        for (int j = 0; j <= 3; j++)
                                        {
                                           if(time > 23*60+59) 
 												 correctedTime = time - 24*60; 
 											else 
												 correctedTime = time; 
 												if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionX == currentPositionX && TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionY == currentPositionY)
                                                canDo = true;
                                            if (!TrainDriving.allTrains[i].trainSchedule[correctedTime + j].switched && canDo)
                                            {
                                                time = time + timeIncrease; 
 	 	 	 	 	 	 	                    visitedRails.RemoveAt(visitedRails.Count - 1);
                                                goto Continue;
                                            }
                                        }
                                        canDo = false;
                                    }
                                    l.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = true, direction = orientation, time = time });
                                }
                                return l;
                            }
                            if (MainForm.map[currentPositionX, currentPositionY].Image == MainForm.vlefttop)
                            {
                                List<Train.ScheduleRecord> straight = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, "doprava", +1, 0, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease);
                                List<Train.ScheduleRecord> turned = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, "doprava", 0, -1, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease);
                                if (straight != null && straight.Count > 0 && ( turned == null || turned.Count == 0 || straight[0].time <= turned[0].time))
                                {
                                    for (int i = 0; i < TrainDriving.allTrains.Count; i++)
                                    {
                                        for (int j = 0; j <= 3; j++)
                                        {
                                           if(time > 23*60+59) 
 												 correctedTime = time - 24*60; 
 											else 
												 correctedTime = time; 
 												if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionX == currentPositionX && TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionY == currentPositionY)
                                                canDo = true;
                                            if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].switched && canDo)
                                            {
                                                time = time + timeIncrease; 
 	 	 	 	 	 	                    	visitedRails.RemoveAt(visitedRails.Count - 1);
                                                goto Continue;
                                            }
                                        }
                                        canDo = false;
                                    }
                                    straight.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = false, direction = orientation, time = time });
                                    return straight;
                                }
                                else if (turned != null && turned.Count > 0 && ( straight == null || straight.Count == 0 || turned[0].time < straight[0].time))
                                {
                                    for (int i = 0; i < TrainDriving.allTrains.Count; i++)
                                    {
                                        for (int j = 0; j <= 3; j++)
                                        {
                                           if(time > 23*60+59) 
 												 correctedTime = time - 24*60; 
 											else 
												 correctedTime = time; 
 												if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionX == currentPositionX && TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionY == currentPositionY)
                                                canDo = true;
                                            if (!TrainDriving.allTrains[i].trainSchedule[correctedTime + j].switched && canDo)
                                            {
                                                time = time + timeIncrease; 
 	 	 	 	 	 	                    	visitedRails.RemoveAt(visitedRails.Count - 1);
                                                goto Continue;
                                            }
                                        }
                                        canDo = false;
                                    }
                                    turned.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = true, direction = orientation, time = time });
                                    return turned;
                                }
                                else if (straight == null && turned == null)
                                    throw new NullReferenceException();
                                else
                                {
                                    return new List<Train.ScheduleRecord>();
                                }
                            }
                            if (MainForm.map[currentPositionX, currentPositionY].Image == MainForm.vleftbottom)
                            {
                                List<Train.ScheduleRecord> straight = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, "doprava", +1, 0, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease);
                                List<Train.ScheduleRecord> turned = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, "doprava", 0, +1, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease);
                                if (straight != null && straight.Count > 0 && ( turned == null || turned.Count == 0 || straight[0].time <= turned[0].time))
                                {
                                    for (int i = 0; i < TrainDriving.allTrains.Count; i++)
                                    {
                                        for (int j = 0; j <= 3; j++)
                                        {
                                           if(time > 23*60+59) 
 												 correctedTime = time - 24*60; 
 											else 
												 correctedTime = time; 
 												if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionX == currentPositionX && TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionY == currentPositionY)
                                                canDo = true;
                                            if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].switched && canDo)
                                            {
                                                time = time + timeIncrease; 
 	 	 	 	 	 	                     	visitedRails.RemoveAt(visitedRails.Count - 1);
                                                goto Continue;
                                            }
                                        }
                                        canDo = false;
                                    }
                                    straight.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = false, direction = orientation, time = time });
                                    return straight;
                                }
                                else if (turned != null && turned.Count > 0 && ( straight == null || straight.Count == 0 || turned[0].time < straight[0].time))
                                {
                                    for (int i = 0; i < TrainDriving.allTrains.Count; i++)
                                    {
                                        for (int j = 0; j <= 3; j++)
                                        {
                                           if(time > 23*60+59) 
 												 correctedTime = time - 24*60; 
 											else 
												 correctedTime = time; 
 												if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionX == currentPositionX && TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionY == currentPositionY)
                                                canDo = true;
                                            if (!TrainDriving.allTrains[i].trainSchedule[correctedTime + j].switched && canDo)
                                            {
                                                time = time + timeIncrease; 
 	 	 	 	 	 	                     	visitedRails.RemoveAt(visitedRails.Count - 1);
                                                goto Continue;
                                            }
                                        }
                                        canDo = false;
                                    }
                                    turned.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = true, direction = orientation, time = time });
                                    return turned;
                                }
                                else if (straight == null && turned == null)
                                    throw new NullReferenceException();
                                else
                                {
                                    return new List<Train.ScheduleRecord>();
                                }

                            }
                        }
                        else if (MainForm.map[currentPositionX, currentPositionY].Image == MainForm.horizontal || MainForm.map[currentPositionX, currentPositionY].Image == MainForm.horizontalOver ||
                            MainForm.map[currentPositionX, currentPositionY].Image == MainForm.verticalOver || (MainForm.map[currentPositionX, currentPositionY].facilityName != null && MainForm.map[currentPositionX, currentPositionY].ways[0,3] == true))
                        {
                            nextPositionX = +1;
                            if(CheckIfHaveToWait(currentPositionX+nextPositionX, currentPositionY+nextPositionY, orientation, ref time, timeIncrease, ref nextPositionX, ref nextPositionY, ref visitedRails)) 
 								 goto Continue; 
 	 		 	 	 	 	 l = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, orientation, nextPositionX, nextPositionY, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease); 
 							if(l.Count>0)
                            {
                                l.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = false, direction = orientation, time = time });
                            }
                            return l;
                        }
                        else if (MainForm.map[currentPositionX, currentPositionY].Image == MainForm.rtopleft)
                        {
                            nextPositionY = -1;
                            if(CheckIfHaveToWait(currentPositionX+nextPositionX, currentPositionY+nextPositionY, orientation, ref time, timeIncrease, ref nextPositionX, ref nextPositionY, ref visitedRails)) 
 								 goto Continue; 
 	 		 	 	 	 	 l = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, orientation, nextPositionX, nextPositionY, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease); 
 							if(l.Count>0)
                            {
                                l.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = false, direction = orientation, time = time });
                            }
                            return l;
                        }
                        else if (MainForm.map[currentPositionX, currentPositionY].Image == MainForm.rleftbottom)
                        {
                            nextPositionY = +1;
                            if(CheckIfHaveToWait(currentPositionX+nextPositionX, currentPositionY+nextPositionY, orientation, ref time, timeIncrease, ref nextPositionX, ref nextPositionY, ref visitedRails)) 
 								 goto Continue; 
 	 		 	 	 	 	 l = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, orientation, nextPositionX, nextPositionY, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease); 
 							if(l.Count>0)
                            {
                                l.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = false, direction = orientation, time = time });
                            }
                            return l;
                        }
                    }
                    else if (orientation == "nahoru")
                    {
                        if (MainForm.map[currentPositionX, currentPositionY].BlockInfo == 1 || MainForm.map[currentPositionX, currentPositionY].BlockInfo == 2)
                        {
                            if (MainForm.map[currentPositionX, currentPositionY].Image == MainForm.vtopleft || MainForm.map[currentPositionX, currentPositionY].Image == MainForm.vtopright)
                            {
                                nextPositionY = -1;
                                if(CheckIfHaveToWait(currentPositionX+nextPositionX, currentPositionY+nextPositionY, orientation, ref time, timeIncrease, ref nextPositionX, ref nextPositionY, ref visitedRails)) 
 								 goto Continue; 
 	 		 	 	 	 	 l = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, orientation, nextPositionX, nextPositionY, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease); 
 								if(l.Count>0)
                                {
                                    nextPositionY = 0;
                                    for (int i = 0; i < TrainDriving.allTrains.Count; i++)
                                    {
                                        for (int j = 0; j <= 3; j++)
                                        {
                                           if(time > 23*60+59) 
 												 correctedTime = time - 24*60; 
 											else 
												 correctedTime = time; 
 												if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionX == currentPositionX && TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionY == currentPositionY)
                                                canDo = true;
                                            if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].switched && canDo)
                                            {
                                                time = time + timeIncrease; 
 	 	 	 	 	 	                    	visitedRails.RemoveAt(visitedRails.Count - 1);
                                                goto Continue;
                                            }
                                        }
                                        canDo = false;
                                    }
                                    l.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = false, direction = orientation, time = time });
                                }
                                return l;
                            }
                            if (MainForm.map[currentPositionX, currentPositionY].Image == MainForm.vrightbottom)
                            {
                                nextPositionX = +1;
                                if(CheckIfHaveToWait(currentPositionX+nextPositionX, currentPositionY+nextPositionY, orientation, ref time, timeIncrease, ref nextPositionX, ref nextPositionY, ref visitedRails)) 
 								 goto Continue; 
 	 		 	 	 	 	 l = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, orientation, nextPositionX, nextPositionY, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease); 
 								if(l.Count>0)
                                {
                                    nextPositionX = 0;
                                    for (int i = 0; i < TrainDriving.allTrains.Count; i++)
                                    {
                                        for (int j = 0; j <= 3; j++)
                                        {
                                           if(time > 23*60+59) 
 												 correctedTime = time - 24*60; 
 											else 
												 correctedTime = time; 
 												if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionX == currentPositionX && TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionY == currentPositionY)
                                                canDo = true;
                                            if (!TrainDriving.allTrains[i].trainSchedule[correctedTime + j].switched && canDo)
                                            {
                                                time = time + timeIncrease; 
 	 	 	 	 	 	                    	visitedRails.RemoveAt(visitedRails.Count - 1);
                                                goto Continue;
                                            }
                                        }
                                        canDo = false;
                                    }
                                    l.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = true, direction = orientation, time = time });
                                }
                                return l;
                            }
                            if (MainForm.map[currentPositionX, currentPositionY].Image == MainForm.vleftbottom)
                            {
                                nextPositionX = -1;
                               if(CheckIfHaveToWait(currentPositionX+nextPositionX, currentPositionY+nextPositionY, orientation, ref time, timeIncrease, ref nextPositionX, ref nextPositionY, ref visitedRails)) 
 								 goto Continue; 
 	 		 	 	 	 	 l = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, orientation, nextPositionX, nextPositionY, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease); 
 								 if(l.Count>0)
                                {
                                    nextPositionX = 0;
                                    for (int i = 0; i < TrainDriving.allTrains.Count; i++)
                                    {
                                        for (int j = 0; j <= 3; j++)
                                        {
                                           if(time > 23*60+59) 
 												 correctedTime = time - 24*60; 
 											else 
												 correctedTime = time; 
 												if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionX == currentPositionX && TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionY == currentPositionY)
                                                canDo = true;
                                            if (!TrainDriving.allTrains[i].trainSchedule[correctedTime + j].switched && canDo)
                                            {
                                                time = time + timeIncrease; 
 	 	 	 	 	 	                    	visitedRails.RemoveAt(visitedRails.Count - 1);
                                                goto Continue;
                                            }
                                        }
                                        canDo = false;
                                    }
                                    l.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = true, direction = orientation, time = time });
                                }
                                return l;
                            }
                            if (MainForm.map[currentPositionX, currentPositionY].Image == MainForm.vbottomright)
                            {
                                List<Train.ScheduleRecord> straight = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, orientation, 0, -1, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease);
                                List<Train.ScheduleRecord> turned = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, orientation, +1, 0, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease);
                                if (straight != null && straight.Count > 0 && ( turned == null || turned.Count == 0 || straight[0].time <= turned[0].time))
                                {
                                    for (int i = 0; i < TrainDriving.allTrains.Count; i++)
                                    {
                                        for (int j = 0; j <= 3; j++)
                                        {
                                           if(time > 23*60+59) 
 												 correctedTime = time - 24*60; 
 											else 
												 correctedTime = time; 
 												if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionX == currentPositionX && TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionY == currentPositionY)
                                                canDo = true;
                                            if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].switched && canDo)
                                            {
                                                time = time + timeIncrease; 
 	 	 	 	 	 	                    	visitedRails.RemoveAt(visitedRails.Count - 1);
                                                goto Continue;
                                            }
                                        }
                                        canDo = false;
                                    }
                                    straight.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = false, direction = orientation, time = time });
                                    return straight;
                                }
                                else if (turned != null && turned.Count > 0 && ( straight == null || straight.Count == 0 || turned[0].time < straight[0].time))
                                {
                                    for (int i = 0; i < TrainDriving.allTrains.Count; i++)
                                    {
                                        for (int j = 0; j <= 3; j++)
                                        {
                                           if(time > 23*60+59) 
 												 correctedTime = time - 24*60; 
 											else 
												 correctedTime = time; 
 												if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionX == currentPositionX && TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionY == currentPositionY)
                                                canDo = true;
                                            if (!TrainDriving.allTrains[i].trainSchedule[correctedTime + j].switched && canDo)
                                            {
                                                time = time + timeIncrease; 
 	 	 	 	 	 	 	                    visitedRails.RemoveAt(visitedRails.Count - 1);
                                                goto Continue;
                                            }
                                        }
                                        canDo = false;
                                    }
                                    turned.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = true, direction = orientation, time = time });
                                    return turned;
                                }
                                else if (straight == null && turned == null)
                                    throw new NullReferenceException();
                                else
                                {
                                    return new List<Train.ScheduleRecord>();
                                }
                            }
                            if (MainForm.map[currentPositionX, currentPositionY].Image == MainForm.vbottomleft)
                            {
                                List<Train.ScheduleRecord> straight = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, orientation, 0, -1, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease);
                                List<Train.ScheduleRecord> turned = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, orientation, -1, 0, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease);
                                if (straight != null && straight.Count > 0 && ( turned == null || turned.Count == 0 || straight[0].time <= turned[0].time))
                                {
                                    for (int i = 0; i < TrainDriving.allTrains.Count; i++)
                                    {
                                        for (int j = 0; j <= 3; j++)
                                        {
                                           if(time > 23*60+59) 
 												 correctedTime = time - 24*60; 
 											else 
												 correctedTime = time; 
 												if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionX == currentPositionX && TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionY == currentPositionY)
                                                canDo = true;
                                            if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].switched && canDo)
                                            {
                                                time = time + timeIncrease; 
 	 	 	 	 	 	 	                    visitedRails.RemoveAt(visitedRails.Count - 1);
                                                goto Continue;
                                            }
                                        }
                                        canDo = false;
                                    }
                                    straight.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = false, direction = orientation, time = time });
                                    return straight;
                                }
                                else if (turned != null && turned.Count > 0 && ( straight == null || straight.Count == 0 || turned[0].time < straight[0].time))
                                {
                                    for (int i = 0; i < TrainDriving.allTrains.Count; i++)
                                    {
                                        for (int j = 0; j <= 3; j++)
                                        {
                                           if(time > 23*60+59) 
 												 correctedTime = time - 24*60; 
 											else 
												 correctedTime = time; 
 												if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionX == currentPositionX && TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionY == currentPositionY)
                                                canDo = true;
                                            if (!TrainDriving.allTrains[i].trainSchedule[correctedTime + j].switched && canDo)
                                            {
                                                time = time + timeIncrease; 
 	 	 	 	 	 	 	                    visitedRails.RemoveAt(visitedRails.Count - 1);
                                                goto Continue;
                                            }
                                        }
                                        canDo = false;
                                    }
                                    turned.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = true, direction = orientation, time = time });
                                    return turned;
                                }
                                else if (straight == null && turned == null)
                                    throw new NullReferenceException();
                                else
                                {
                                    return new List<Train.ScheduleRecord>();
                                }

                            }
                        }
                        else if (MainForm.map[currentPositionX, currentPositionY].Image == MainForm.vertical || MainForm.map[currentPositionX, currentPositionY].Image == MainForm.horizontalOver ||
                            MainForm.map[currentPositionX, currentPositionY].Image == MainForm.verticalOver || (MainForm.map[currentPositionX, currentPositionY].facilityName != null && MainForm.map[currentPositionX, currentPositionY].ways[0,2] == true))
                        {
                            nextPositionY = -1;
                            if(CheckIfHaveToWait(currentPositionX+nextPositionX, currentPositionY+nextPositionY, orientation, ref time, timeIncrease, ref nextPositionX, ref nextPositionY, ref visitedRails)) 
 								 goto Continue; 
 	 		 	 	 	 	 l = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, orientation, nextPositionX, nextPositionY, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease); 
 							if(l.Count>0)
                            {
                                l.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = false, direction = orientation, time = time });
                            }
                            return l;
                        }
                        else if (MainForm.map[currentPositionX, currentPositionY].Image == MainForm.rleftbottom)
                        {
                            nextPositionX = -1;
                            if(CheckIfHaveToWait(currentPositionX+nextPositionX, currentPositionY+nextPositionY, orientation, ref time, timeIncrease, ref nextPositionX, ref nextPositionY, ref visitedRails)) 
 								 goto Continue; 
 	 		 	 	 	 	 l = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, orientation, nextPositionX, nextPositionY, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease); 
 							if(l.Count>0)
                            {
                                l.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = false, direction = orientation, time = time });
                            }
                            return l;
                        }
                        else if (MainForm.map[currentPositionX, currentPositionY].Image == MainForm.rbottomright)
                        {
                            nextPositionX = +1;
                            if(CheckIfHaveToWait(currentPositionX+nextPositionX, currentPositionY+nextPositionY, orientation, ref time, timeIncrease, ref nextPositionX, ref nextPositionY, ref visitedRails)) 
 								 goto Continue; 
 	 		 	 	 	 	 l = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, orientation, nextPositionX, nextPositionY, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease);
 							if(l.Count>0)
                            {
                                l.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = false, direction = orientation, time = time });
                            }
                            return l;
                        }
                    }
                    else if (orientation == "dolu")
                    {
                        if (MainForm.map[currentPositionX, currentPositionY].BlockInfo == 1 || MainForm.map[currentPositionX, currentPositionY].BlockInfo == 2)
                        {
                            if (MainForm.map[currentPositionX, currentPositionY].Image == MainForm.vbottomleft || MainForm.map[currentPositionX, currentPositionY].Image == MainForm.vbottomright)
                            {
                                nextPositionY = +1;
                               if(CheckIfHaveToWait(currentPositionX+nextPositionX, currentPositionY+nextPositionY, orientation, ref time, timeIncrease, ref nextPositionX, ref nextPositionY, ref visitedRails)) 
 								 goto Continue; 
 	 		 	 	 	 	 l = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, orientation, nextPositionX, nextPositionY, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease); 
 								 if(l.Count>0)
                                {
                                    nextPositionY = 0;
                                    for (int i = 0; i < TrainDriving.allTrains.Count; i++)
                                    {
                                        for (int j = 0; j <= 3; j++)
                                        {
                                           if(time > 23*60+59) 
 												 correctedTime = time - 23*60; 
 											else 
												 correctedTime = time; 
 												if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionX == currentPositionX && TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionY == currentPositionY)
                                                canDo = true;
                                            if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].switched && canDo)
                                            {
                                                time = time + timeIncrease; 
 	 	 	 	 	 	 	                    visitedRails.RemoveAt(visitedRails.Count - 1);
                                                goto Continue;
                                            }
                                        }
                                        canDo = false;
                                    }
                                    l.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = false, direction = orientation, time = time });
                                }
                                return l;
                            }
                            if (MainForm.map[currentPositionX, currentPositionY].Image == MainForm.vrighttop)
                            {
                                nextPositionX = +1;
                               if(CheckIfHaveToWait(currentPositionX+nextPositionX, currentPositionY+nextPositionY, orientation, ref time, timeIncrease, ref nextPositionX, ref nextPositionY, ref visitedRails)) 
 								 goto Continue; 
 	 		 	 	 	 	 l = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, orientation, nextPositionX, nextPositionY, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease); 
 								 if(l.Count>0)
                                {
                                    nextPositionX = 0;
                                    for (int i = 0; i < TrainDriving.allTrains.Count; i++)
                                    {
                                        for (int j = 0; j <= 3; j++)
                                        {
                                           if(time > 23*60+59) 
 												 correctedTime = time - 24*60; 
 											else 
												 correctedTime = time; 
 												if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionX == currentPositionX && TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionY == currentPositionY)
                                                canDo = true;
                                            if (!TrainDriving.allTrains[i].trainSchedule[correctedTime + j].switched && canDo)
                                            {
                                                time = time + timeIncrease; 
 	 	 	 	 	 	 	                    visitedRails.RemoveAt(visitedRails.Count - 1);
                                                goto Continue;
                                            }
                                        }
                                        canDo = false;
                                    }
                                    l.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = true, direction = orientation, time = time });
                                }
                                return l;
                            }
                            if (MainForm.map[currentPositionX, currentPositionY].Image == MainForm.vlefttop)
                            {
                                nextPositionX = -1;
                               if(CheckIfHaveToWait(currentPositionX+nextPositionX, currentPositionY+nextPositionY, orientation, ref time, timeIncrease, ref nextPositionX, ref nextPositionY, ref visitedRails)) 
 								 goto Continue; 
 	 		 	 	 	 	 l = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, orientation, nextPositionX, nextPositionY, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease); 
 								 if(l.Count>0)
                                {
                                    nextPositionX = 0;
                                    for (int i = 0; i < TrainDriving.allTrains.Count; i++)
                                    {
                                        for (int j = 0; j <= 3; j++)
                                        {
                                           if(time > 23*60+59) 
 												 correctedTime = time - 24*60; 
 											else 
												 correctedTime = time; 
 												if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionX == currentPositionX && TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionY == currentPositionY)
                                                canDo = true;
                                            if (!TrainDriving.allTrains[i].trainSchedule[correctedTime + j].switched && canDo)
                                            {
                                                time = time + timeIncrease; 
 	 	 	 	 	 	 	                    visitedRails.RemoveAt(visitedRails.Count - 1);
                                                goto Continue;
                                            }
                                        }
                                        canDo = false;
                                    }
                                    l.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = true, direction = orientation, time = time });
                                }
                                return l;
                            }
                            if (MainForm.map[currentPositionX, currentPositionY].Image == MainForm.vtopright)
                            {
                                List<Train.ScheduleRecord> straight = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, orientation, 0, +1, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease);
                                List<Train.ScheduleRecord> turned = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, orientation, +1, 0, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease);
                                if (straight != null && straight.Count > 0 && ( turned == null || turned.Count == 0 || straight[0].time <= turned[0].time))
                                {
                                    for (int i = 0; i < TrainDriving.allTrains.Count; i++)
                                    {
                                        for (int j = 0; j <= 3; j++)
                                        {
                                           if(time > 23*60+59) 
 												 correctedTime = time - 24*60; 
 											else 
												 correctedTime = time; 
 												if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionX == currentPositionX && TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionY == currentPositionY)
                                                canDo = true;
                                            if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].switched && canDo)
                                            {
                                                time = time + timeIncrease; 
 	 	 	 	 	 	 	                    visitedRails.RemoveAt(visitedRails.Count - 1);
                                                goto Continue;
                                            }
                                        }
                                        canDo = false;
                                    }
                                    straight.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = false, direction = orientation, time = time });
                                    return straight;
                                }
                                else if (turned != null && turned.Count > 0 && ( straight == null || straight.Count == 0 || turned[0].time < straight[0].time))
                                {
                                    for (int i = 0; i < TrainDriving.allTrains.Count; i++)
                                    {
                                        for (int j = 0; j <= 3; j++)
                                        {
                                           if(time > 23*60+59) 
 												 correctedTime = time - 24*60; 
 											else 
												 correctedTime = time; 
 												if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionX == currentPositionX && TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionY == currentPositionY)
                                                canDo = true;
                                            if (!TrainDriving.allTrains[i].trainSchedule[correctedTime + j].switched && canDo)
                                            {
                                                time = time + timeIncrease; 
 	 	 	 	 	 	 	                    visitedRails.RemoveAt(visitedRails.Count - 1);
                                                goto Continue;
                                            }
                                        }
                                        canDo = false;
                                    }
                                    turned.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = true, direction = orientation, time = time });
                                    return turned;
                                }
                                else if (straight == null && turned == null)
                                    throw new NullReferenceException();
                                else
                                {
                                    return new List<Train.ScheduleRecord>();
                                }
                            }
                            if (MainForm.map[currentPositionX, currentPositionY].Image == MainForm.vtopleft)
                            {
                                List<Train.ScheduleRecord> straight = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, orientation, 0, +1, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease);
                                List<Train.ScheduleRecord> turned = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, orientation, -1, 0, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease);
                                if (straight != null && straight.Count > 0 && ( turned == null || turned.Count == 0 || straight[0].time <= turned[0].time))
                                {
                                    for (int i = 0; i < TrainDriving.allTrains.Count; i++)
                                    {
                                        for (int j = 0; j <= 3; j++)
                                        {
                                           if(time > 23*60+59) 
 												 correctedTime = time - 24*60; 
 											else 
												 correctedTime = time; 
 												if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionX == currentPositionX && TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionY == currentPositionY)
                                                canDo = true;
                                            if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].switched && canDo)
                                            {
                                                time = time + timeIncrease; 
 	 	 	 	 	 	 	                    visitedRails.RemoveAt(visitedRails.Count - 1);
                                                goto Continue;
                                            }
                                        }
                                        canDo = false;
                                    }
                                    straight.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = false, direction = orientation, time = time });
                                    return straight;
                                }
                                else if (turned != null && turned.Count > 0 && ( straight == null || straight.Count == 0 || turned[0].time < straight[0].time))
                                {
                                    for (int i = 0; i < TrainDriving.allTrains.Count; i++)
                                    {
                                        for (int j = 0; j <= 3; j++)
                                        {
                                           if(time > 23*60+59) 
 												 correctedTime = time - 24*60; 
 											else 
												 correctedTime = time; 
 												if (TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionX == currentPositionX && TrainDriving.allTrains[i].trainSchedule[correctedTime + j].positionY == currentPositionY)
                                                canDo = true;
                                            if (!TrainDriving.allTrains[i].trainSchedule[correctedTime + j].switched && canDo)
                                            {
                                                time = time + timeIncrease; 
 	 	 	 	 	 	 	                    visitedRails.RemoveAt(visitedRails.Count - 1);
                                                goto Continue;
                                            }
                                        }
                                        canDo = false;
                                    }
                                    turned.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = true, direction = orientation, time = time });
                                    return turned;
                                }
                                else if (straight == null && turned == null)
                                    throw new NullReferenceException();
                                else
                                {
                                    return new List<Train.ScheduleRecord>();
                                }

                            }
                        }
                        else if (MainForm.map[currentPositionX, currentPositionY].Image == MainForm.vertical || MainForm.map[currentPositionX, currentPositionY].Image == MainForm.horizontalOver ||
                            MainForm.map[currentPositionX, currentPositionY].Image == MainForm.verticalOver || (MainForm.map[currentPositionX, currentPositionY].facilityName != null && MainForm.map[currentPositionX, currentPositionY].ways[0,0] == true))
                        {
                            nextPositionY = +1;
                            if(CheckIfHaveToWait(currentPositionX+nextPositionX, currentPositionY+nextPositionY, orientation, ref time, timeIncrease, ref nextPositionX, ref nextPositionY, ref visitedRails)) 
 								 goto Continue; 
 	 		 	 	 	 	 l = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, orientation, nextPositionX, nextPositionY, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease); 
 							if(l.Count>0)
                            {
                                l.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = false, direction = orientation, time = time });
                            }
                            return l;
                        }
                        else if (MainForm.map[currentPositionX, currentPositionY].Image == MainForm.rtopleft)
                        {
                            nextPositionX = -1;
                            if(CheckIfHaveToWait(currentPositionX+nextPositionX, currentPositionY+nextPositionY, orientation, ref time, timeIncrease, ref nextPositionX, ref nextPositionY, ref visitedRails)) 
 								 goto Continue; 
 	 		 	 	 	 	 l = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, orientation, nextPositionX, nextPositionY, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease); 
 							if(l.Count>0)
                            {
                                l.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = false, direction = orientation, time = time });
                            }
                            return l;
                        }
                        else if (MainForm.map[currentPositionX, currentPositionY].Image == MainForm.rrighttop)
                        {
                            nextPositionX = +1;
                            if(CheckIfHaveToWait(currentPositionX+nextPositionX, currentPositionY+nextPositionY, orientation, ref time, timeIncrease, ref nextPositionX, ref nextPositionY, ref visitedRails)) 
 								 goto Continue; 
 	 		 	 	 	 	 l = SwitchSensitiveDFS(currentPositionX, currentPositionY, destination, orientation, nextPositionX, nextPositionY, currentDistance + 1, sourceTime, time + timeIncrease, timeIncrease); 
 							if(l.Count>0)
                            {
                                l.Add(new Train.ScheduleRecord { start = false, positionX = currentPositionX, positionY = currentPositionY, available = false, switched = false, direction = orientation, time = time });
                            }
                            return l;
                        }
                    }
                    else
                    {
                        return new List<Train.ScheduleRecord>();
                    }
                }
                //if it returns null, increase the time
                catch (NullReferenceException)
                {
                    //null flag is when a faster train than the one evaluated goes in the opposite direction, to get two blocks back in DFS
                    if(nullFlag)
                    {
                        nullFlag = false;
                        if (visitedRails.Count > 0)
                            visitedRails.RemoveAt(visitedRails.Count - 1);
                        return null;
                    }

                    time = timeSpan;
                    nextPositionX = 0; nextPositionY = 0;
                    continue;
                }


                return new List<Train.ScheduleRecord>();
            }
        }

        //function used in DFS to determine if have to wait for another train
        static bool CheckIfHaveToWait(int x, int y, string orientation, ref int time, int timeIncrease, ref int nextPositionX,ref int nextPositionY,ref List<VisitedRails> visitedRails) //gets updated x and y by nextPositions
        {
            for (int i = 0; i < TrainDriving.allTrains.Count; i++)
            {
                if (TrainDriving.allTrains[i].trainSchedule[time].positionX == x && TrainDriving.allTrains[i].trainSchedule[time].positionY == y &&
                    (MainForm.map[x,y].Image != MainForm.verticalOver || MainForm.map[x,y].Image != MainForm.horizontalOver ||
                    !IsPerpendicular(TrainDriving.allTrains[i].trainSchedule[time].direction, orientation)))
                {
                    visitedRails.RemoveAt(visitedRails.Count - 1);
                    nextPositionX = 0; nextPositionY = 0;
                    time = time + timeIncrease;
                    return true;
                }
                if (TrainDriving.allTrains[i].type == "bigTrain" && time > 1 && ((TrainDriving.allTrains[i].trainSchedule[time - 1].positionX == x && TrainDriving.allTrains[i].trainSchedule[time - 1].positionY == y)
                    || (TrainDriving.allTrains[i].trainSchedule[time - 2].positionX == x && TrainDriving.allTrains[i].trainSchedule[time - 2].positionY == y)) &&
                    (MainForm.map[x,y].Image != MainForm.verticalOver || MainForm.map[x,y].Image != MainForm.horizontalOver))
                {
                    visitedRails.RemoveAt(visitedRails.Count - 1);
                    nextPositionX = 0; nextPositionY = 0;
                    time = time + timeIncrease;
                    return true;
                }
            }
            return false;
        }
        

        //to determine if the rails to the facility are connected
        static bool IsFacilityReachableFromThisSide(int positionX, int positionY, string orientation) {
            if (MainForm.map[positionX, positionY].ways[0, 3] == true && orientation == "doprava")
            {
                return true;
            }
            if (MainForm.map[positionX, positionY].ways[0, 1] == true && orientation == "doleva")
            {
                return true;
            }
            if (MainForm.map[positionX, positionY].ways[0, 0] == true && orientation == "dolu")
            {
                return true;
            }
            if (MainForm.map[positionX, positionY].ways[0, 2] == true && orientation == "nahoru")
            {
                return true;
            }
            return false;
        }

        //enlarges to List<ScheduleRecord> by the time spent waiting in station and on rails
        static List<Train.ScheduleRecord> RepairWayRegardingToTime(List<Train.ScheduleRecord> theWay, int startTime, string trainType) {
            theWay.Reverse();
            List<Train.ScheduleRecord> updatedWay = new List<Train.ScheduleRecord>();

            if (theWay.Count > 0 && startTime != theWay[0].time)
            {
                for (int i = 0; i < theWay[0].time - startTime; i++)
                {
                    updatedWay.Add(new Train.ScheduleRecord());
                }
            }
            if(theWay.Count > 0)
                theWay[0].start = true;
            for (int i = 0; i < theWay.Count; i++)
            {
                theWay[i].waiting = false;
                updatedWay.Add(theWay[i]);
                if (i == theWay.Count - 1)
                    break;
                if (theWay[i].time + 1 < theWay[i + 1].time)
                {
                    for (int j = 0; j < theWay[i + 1].time - (theWay[i].time + 1); j++)
                    {
                        updatedWay.Add(new Train.ScheduleRecord() { positionX = theWay[i].positionX, positionY = theWay[i].positionY, waiting = true, switched = theWay[i].switched, direction = theWay[i].direction});
                    }
                }
            }
            if (theWay.Count > 0)
                updatedWay[updatedWay.Count - 1].stop = true;
            return updatedWay;
        }

        //to determine if a train must wait for another train, or if it is under the bridge or on the bridge perpendiculary
        static bool IsPerpendicular(string o1, string o2) {
            if (o1 == "doprava" && (o2 == "dolu" || o2 == "nahoru"))
                return true;
            if (o1 == "doleva" && (o2 == "dolu" || o2 == "nahoru"))
                return true;
            if (o1 == "nahoru" && (o2 == "doprava" || o2 == "doleva"))
                return true;
            if (o1 == "dolu" && (o2 == "doprava" || o2 == "doleva"))
                return true;
            return false;
        }

        //checks ineffective train and offers parallel rail construction
        static bool CheckIneffectiveTrain(List<Train.ScheduleRecord> way, string type){
            int timeWaiting = 0;
            if(type == "smallTrain")
            {
                for (int i = 0; i < way.Count - 2; i++)
                {
                    if(way[i].positionX  == -1) //means it's waiting out of map
                    {
                        timeWaiting++;
                    }
                    else if(way[i].positionX == way[i + 2].positionX && way[i].positionY == way[i + 2].positionY)
                    {
                        timeWaiting++;
                    }
                }
                if (timeWaiting > way.Count / 2)
                    return true;
                return false;
            }
            else
            {
                for (int i = 0; i < way.Count - 1; i++)
                {
                    if (way[i].positionX == -1)
                        timeWaiting++;
                    else if (way[i].positionX == way[i + 1].positionX && way[i].positionY == way[i + 1].positionY)
                        timeWaiting++;
                }
                if (timeWaiting > way.Count / 2)
                    return true;
                return false;
            }
        }
            

        static int ConvertTimeToInt(string strTime)
        {
            int output = 0;
            try
            {
                string[] ar = strTime.Split(':');
                output += int.Parse(ar[0]) * 60;
                output += int.Parse(ar[1]);
            }
            catch (FormatException) { return -1; }

            //regex wasn't protected against 24 - 29 hours so check it
            if (output > 23 * 60 + 59) //one day
                return -1;
            else
                return output;
        }

        public static string ConvertTimeToString(int intTime)
        {
            string output;
            int minuty = intTime % 60;
            int hodiny = intTime / 60;
            output = hodiny.ToString("00") + ":" + minuty.ToString("00");
            return output;
        }

        //a row to show in timetables table
        public class TimeTableRecord
        {
            public string name;
            public string sourceStation;
            public int sourceTime;
            public string destionationStation;
            public int destinationTime;
            public string type;
        }

        public static List<TimeTableRecord> trainsOverview = new List<TimeTableRecord>();

        public static Form overview;
        static TableLayoutPanel p;

        //shows a table with jízdní řády
        public static void ShowTrainsOverview(){

            overview = new Form();
            overview.StartPosition = FormStartPosition.Manual;
            overview.Location = new Point(MainForm.mainForm.Width / 2, MainForm.mainForm.Height / 2);
            overview.AutoScroll = true;
            overview.Width = 560;
            overview.Height = 300;
            overview.Text = "Přehled Jízdních řádů";
            p = new TableLayoutPanel();
            p.ColumnCount = 5;
            for (int i = 0; i < p.ColumnCount; i++)
            {
                p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60F));
            }

            p.RowCount = 2 + trainsOverview.Count;
            
            for (int i = 0; i < p.RowCount; i++)
            {
                p.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            }

            p.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;

            p.Width = 500;
            p.AutoSize = true;
            p.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            p.Controls.Add(new Label() { Text = "Název vlaku" });
            p.Controls.Add(new Label() { Text = "Odjezd" });
            p.Controls.Add(new Label() { Text = "Čas odjezdu" });
            p.Controls.Add(new Label() { Text = "Příjezd" });
            p.Controls.Add(new Label() { Text = "Čas příjezdu" });

            for (int i = 0; i < trainsOverview.Count; i++)
            {
                p.Controls.Add(new Label() { Text = "" + trainsOverview[i].name });
                p.Controls.Add(new Label() { Text = "" + trainsOverview[i].sourceStation});
                p.Controls.Add(new Label() { Text = "" + ConvertTimeToString(trainsOverview[i].sourceTime) });
                p.Controls.Add(new Label() { Text = "" + trainsOverview[i].destionationStation });
                p.Controls.Add(new Label() { Text = "" + ConvertTimeToString(trainsOverview[i].destinationTime) });
            }

            overview.FormClosed += overview_FormClosed;


            Button add = new Button();
            add.Text = "Přidat nový";
            add.MouseClick += add_MouseClick;
            add.Location = new Point(455, 10);
            overview.Controls.Add(add);

            Button close = new Button();
            close.Text = "Zavři";
            close.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            close.MouseClick += close_MouseClick;
            close.Location = new Point(455, 35);
            overview.Controls.Add(close);

            Button save = new Button();
            save.Text = "Uložit";
            save.Click += MainForm.uloz_click;
            save.Location = new Point(455, 60);
            overview.Controls.Add(save);

            Button load = new Button();
            load.Text = "Nahrát";
            load.MouseClick += MainForm.nahraj_click;
            load.Location = new Point(455, 85);
            overview.Controls.Add(load);


            for (int i = 5; i < p.Controls.Count; i++)
            {
                p.Controls[i].MouseClick += overview_rowClick;    
            }

            overview.Controls.Add(p);

            overview.Show();

        }

        static void overview_FormClosed(object sender, FormClosedEventArgs e)
        {
            MainForm.timeTabling = false;
        }

        static void overview_rowClick(object sender_, EventArgs e)
        {
            int rowIndex;
            Control sender = (Control)sender_;
            DialogResult dr = MessageBox.Show("Smazat trasu?", "Smazat trasu", MessageBoxButtons.YesNo);
            if (dr == DialogResult.Yes)
            {
                rowIndex = p.GetPositionFromControl(sender).Row;
                trainsOverview.RemoveAt(rowIndex - 1);
                TrainDriving.allTrains.RemoveAt(rowIndex - 1);
                overview.Hide();
                ShowTrainsOverview();
            }
              
        }


        static void close_MouseClick(object sender, MouseEventArgs e)
        {
            overview.Close();
            MainForm.timeTabling = false;
        }

        public static TimeTableRecord ttr;
        static void add_MouseClick(object sender, MouseEventArgs e)
        {
            overview.Hide();
            ttr = new TimeTableRecord();

            //calls the functions to determine destionationTime
            SetTimeTables(ttr);

            if ( ttr.sourceTime == -1)
            {
                MessageBox.Show("Debug: source time -1");
            }
            else if( ttr.sourceTime == ttr.destinationTime)
            {
                //just dont add it...
            }
            else
                trainsOverview.Add(ttr);
            ShowTrainsOverview();
            
        }

    }
}
