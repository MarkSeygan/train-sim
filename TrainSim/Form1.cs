using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Runtime.Serialization;

namespace TrainSim
{
    
    public partial class MainForm : Form
    {
        private const int mapHeight = 36;
        private const int mapWidth = 68;
        int zoomState;
        public static Block[,] map;
        static ToolTip tooltip = new ToolTip();
        

        //class for creating a map
        public class Building
        {
            static Form buildOpts;
            public static bool currentlyBuilding = false;
            private static List<Block> currentlyPrebuilt;
            
            //handler for clicking the basic block
            public static void p_MouseClick(object sender, MouseEventArgs e)
            {
                Block sender_ = (Block)sender;
               
                if (sender_.BlockInfo != 0)//means there is some structure so you can remove it
                {
                        DialogResult result = MessageBox.Show("Smazat block?", "Smazat?", MessageBoxButtons.YesNo);
                        if (result == DialogResult.Yes)
                        {
                            sender_.Image = null;
                            sender_.BlockInfo = 0;
                            sender_.facilityName = null;
                        }
                }
                else
                {
                    if (currentlyBuilding)
                    {
                        sender_ = (Block)sender;
                        BuildStraightRail(sender_, true); //true -> RealBuild -> means it is not just preshown, but you really want to build it
                    }
                    else
                    {
                        //shows a new form which offers block creation

                        buildOpts = new Form();
                        buildOpts.Size = new Size(500, 110);
                        List<Block> buildOptsControls = new List<Block>();

                        currentlyPrebuilt = new List<Block>();

                        Block straight = new Block();
                        Block turns = new Block();
                        Block switches = new Block();
                        Block facilities = new Block();

                        buildOptsControls.Add(straight); buildOptsControls.Add(turns); buildOptsControls.Add(switches); buildOptsControls.Add(facilities);

                        var theSender = (Block)sender;

                        straight.Image = horizontal;
                        straight.MouseClick += (thisSender, eventArgs) => straight_MouseClick(thisSender, eventArgs, theSender);
                        turns.Image = rbottomright;
                        turns.MouseClick += (thisSender, eventArgs) => turns_MouseClick(thisSender, eventArgs, theSender);
                        switches.Image = vbottomleft;
                        switches.MouseClick += (thisSender, eventArgs) => switches_MouseClick(thisSender, eventArgs, theSender);
                        facilities.Image = bigStation;
                        facilities.MouseClick += facilities_MouseClick;


                        buildOpts.StartPosition = FormStartPosition.Manual;
                        buildOpts.Location = new Point(theSender.Parent.Parent.Location.X + (theSender.Parent.Parent.Width / 2) - 250, theSender.Parent.Parent.Location.Y + theSender.Parent.Parent.Height / 2 - 25);

                        AlignControlsInBuildOpts(buildOptsControls);

                        facilities.SizeMode = PictureBoxSizeMode.AutoSize; //just a manual optimalization

                        buildOpts.ShowDialog();
                    }
                }
            }

            

            static void straight_MouseClick(object sender, MouseEventArgs e, Block theParentalSender)
            {
                startingPoint = theParentalSender;

                theParentalSender.Image = horizontal;

                currentlyBuilding = true;

                buildOpts.Close();

            }

            public static void HandleMouseEnterWhileBuilding(Block sender) //only while building straights
            {
                BuildStraightRail(sender, false); //false -> means preBuild
            }

            static Block startingPoint;

            //handles straight rail creation, where you can pull the rails with mouse into different directions
            static void BuildStraightRail(Block sender, bool preBuildOrRealBuild) 
            {
                if (sender.x == startingPoint.x && sender.y == startingPoint.y) //to determine which orientation on the startingpoint if only startingpoint build
                {
                    bool which = true; //0 is horizontal 1 is vertical
                    for (int i = 0; i < currentlyPrebuilt.Count; i++)
                    {
                        if (currentlyPrebuilt[i].Image == horizontal)
                        {
                            which = false;
                        }
                        currentlyPrebuilt[i].Image = null;

                    }
                    if (which == true) //keep vertical
                    {
                        startingPoint.Image = vertical;
                        if (preBuildOrRealBuild)
                        {
                            startingPoint.ways = new bool[4, 4] {   {false, false, true, false}, //from top clockwise
                                                                    {false, false, false, false}, //
                                                                    {true, false, false, false}, //
                                                                    {false, false, false, false}  //
                                                                };
                            startingPoint.BlockInfo = -1; //means it's not a switch
                        }
                    }
                    else
                    {
                        which = false;
                        startingPoint.Image = horizontal;
                        if (preBuildOrRealBuild)
                        {
                            startingPoint.ways = new bool[4, 4] {   {false, false, false, false}, //from top clockwise
                                                                    {false, false, false, true}, //
                                                                    {false, false, false, false}, //
                                                                    {false, true, false, false}  //
                                                                };
                            startingPoint.BlockInfo = -1; //means it's not a switch
                        }
                    }

                    if (preBuildOrRealBuild)
                    {
                        currentlyPrebuilt.Clear();
                        currentlyBuilding = false;
                    }
                }
                else if (sender.x > startingPoint.x && Math.Abs(sender.x - startingPoint.x) > Math.Abs(sender.y - startingPoint.y))// to the right
                {
                    if (startingPoint.Image == vertical)
                        startingPoint.Image = horizontal;
                    for (int i = 0; i < currentlyPrebuilt.Count; i++)
                    {
                        currentlyPrebuilt[i].Image = null;
                    }
                    

                    for (int i = 0; i <= sender.x - startingPoint.x; i++) //prefills all wanted blocks with rails
                    {
                        if (MainForm.map[startingPoint.x + i, startingPoint.y].BlockInfo == 0 || MainForm.map[startingPoint.x + i, startingPoint.y].BlockInfo == -1) //not switch or turn
                        {
                            if (MainForm.map[startingPoint.x + i, startingPoint.y].BlockInfo == 0)
                            {
                                currentlyPrebuilt.Add(MainForm.map[startingPoint.x + i, startingPoint.y]);
                                MainForm.map[startingPoint.x + i, startingPoint.y].Image = horizontal;
                            }
                            if (preBuildOrRealBuild)
                            {
                                if (MainForm.map[startingPoint.x + i, startingPoint.y].Image == vertical && MainForm.map[startingPoint.x + i - 1, startingPoint.y].Image == horizontal)
                                    OfferBridgeConstuction(MainForm.map[startingPoint.x + i, startingPoint.y], "vertical");
                                MainForm.map[startingPoint.x + i, startingPoint.y].ways = new bool[4, 4] {  {false, false, false, false}, //from top clockwise
                                                                                                            {false, false, false, true}, //
                                                                                                            {false, false, false, false}, //
                                                                                                            {false, true, false, false}  //
                                                                                                         };
                                MainForm.map[startingPoint.x + i, startingPoint.y].BlockInfo = -1; //means it's not a switch

                            }
                        }
                    }
                    CleanAfterRealBuild(preBuildOrRealBuild, ref currentlyPrebuilt, ref currentlyBuilding, ref startingPoint);

                }
                else if (sender.x < startingPoint.x && Math.Abs(sender.x - startingPoint.x) > Math.Abs(sender.y - startingPoint.y))// to the left
                {
                    if (startingPoint.Image == vertical)
                        startingPoint.Image = horizontal;
                    for (int i = 0; i < currentlyPrebuilt.Count; i++)
                    {
                        currentlyPrebuilt[i].Image = null;
                    }

                    for (int i = 0; i >= sender.x - startingPoint.x; i--) //prefills all wanted blocks with rails
                    {
                        if (MainForm.map[startingPoint.x + i, startingPoint.y].BlockInfo == 0 || MainForm.map[startingPoint.x + i, startingPoint.y].BlockInfo == -1) //not switch or turn
                        {
                            if (MainForm.map[startingPoint.x + i, startingPoint.y].BlockInfo == 0)
                            {
                                currentlyPrebuilt.Add(MainForm.map[startingPoint.x + i, startingPoint.y]);
                                MainForm.map[startingPoint.x + i, startingPoint.y].Image = horizontal;
                            }
                            if (preBuildOrRealBuild)
                            {
                                if (MainForm.map[startingPoint.x + i, startingPoint.y].Image == vertical && MainForm.map[startingPoint.x + i +1, startingPoint.y].Image == horizontal)
                                    OfferBridgeConstuction(MainForm.map[startingPoint.x + i, startingPoint.y], "vertical");
                                MainForm.map[startingPoint.x + i, startingPoint.y].ways = new bool[4, 4] {  {false, false, false, false}, //from top clockwise
                                                                                                            {false, false, false, true}, //
                                                                                                            {false, false, false, false}, //
                                                                                                            {false, true, false, false}  //
                                                                                                         };
                                MainForm.map[startingPoint.x + i, startingPoint.y].BlockInfo = -1; //means it's not a switch

                            }
                        }
                    }
                    CleanAfterRealBuild(preBuildOrRealBuild, ref currentlyPrebuilt, ref currentlyBuilding, ref startingPoint);
                }
                else if (sender.y < startingPoint.y && Math.Abs(sender.y - startingPoint.y) > Math.Abs(sender.x - startingPoint.x))//to up
                {
                    if (startingPoint.Image == horizontal && Math.Abs(sender.x - startingPoint.x) > 0)
                        startingPoint.Image = vertical;
                    for (int i = 0; i < currentlyPrebuilt.Count; i++)
                    {
                        
                            currentlyPrebuilt[i].Image = null;
                    }

                    for (int i = 0; i >= sender.y - startingPoint.y; i--) //prefills all wanted blocks with rails
                    {
                        if (MainForm.map[startingPoint.x, startingPoint.y + i].BlockInfo == 0 || MainForm.map[startingPoint.x, startingPoint.y + i].BlockInfo == -1) //not switch or turn
                        {
                            if (MainForm.map[startingPoint.x, startingPoint.y + i].BlockInfo == 0)
                            {
                                currentlyPrebuilt.Add(MainForm.map[startingPoint.x, startingPoint.y + i]);

                                MainForm.map[startingPoint.x, startingPoint.y + i].Image = vertical;
                            }

                            if (preBuildOrRealBuild)
                            {
                                if (MainForm.map[startingPoint.x, startingPoint.y + i].Image == horizontal && MainForm.map[startingPoint.x, startingPoint.y + i + 1].Image == vertical)
                                    OfferBridgeConstuction(MainForm.map[startingPoint.x, startingPoint.y + i], "horizontal");

                                MainForm.map[startingPoint.x, startingPoint.y + i].ways = new bool[4, 4] {  {false, false, true, false}, //from top clockwise
                                                                                                            {false, false, false, false}, //
                                                                                                            {true, false, false, false}, //
                                                                                                            {false, false, false, false}  //
                                                                                                         };
                                MainForm.map[startingPoint.x, startingPoint.y + i].BlockInfo = -1; //means it's not a switch
                            }
                        }
                    }
                    CleanAfterRealBuild(preBuildOrRealBuild, ref currentlyPrebuilt, ref currentlyBuilding, ref startingPoint);
                }
                else  // to bottom
                {
                    if (startingPoint.Image == horizontal && Math.Abs(sender.x - startingPoint.x) > 0)
                        startingPoint.Image = vertical;
                    for (int i = 0; i < currentlyPrebuilt.Count; i++)
                    {
                            currentlyPrebuilt[i].Image = null;
                    }

                    for (int i = 0; i <= sender.y - startingPoint.y; i++) //prefills all wanted blocks with rails
                    {
                        if (MainForm.map[startingPoint.x, startingPoint.y + i].BlockInfo == 0 || MainForm.map[startingPoint.x, startingPoint.y + i].BlockInfo == -1) //not switch or turn
                        {

                            if (MainForm.map[startingPoint.x, startingPoint.y + i].BlockInfo == 0) { 
                                currentlyPrebuilt.Add(MainForm.map[startingPoint.x, startingPoint.y + i]);
                                MainForm.map[startingPoint.x, startingPoint.y + i].Image = vertical;
                            }

                            if (preBuildOrRealBuild)
                            {
                                if (MainForm.map[startingPoint.x, startingPoint.y + i].Image == horizontal && MainForm.map[startingPoint.x, startingPoint.y + i -1].Image == vertical)
                                    OfferBridgeConstuction(MainForm.map[startingPoint.x, startingPoint.y + i],"horizontal");

                                MainForm.map[startingPoint.x, startingPoint.y + i].ways = new bool[4, 4] {  {false, false, true, false}, //from top clockwise
                                                                                                            {false, false, false, false}, //
                                                                                                            {true, false, false, false}, //
                                                                                                            {false, false, false, false}  //
                                                                                                         };
                                MainForm.map[startingPoint.x, startingPoint.y + i].BlockInfo = -1; //means it's not a switch
                            }
                        }
                    }
                    CleanAfterRealBuild(preBuildOrRealBuild, ref currentlyPrebuilt, ref currentlyBuilding, ref startingPoint);
                }
            }

            static void CleanAfterRealBuild(bool preBuildOrRealBuild, ref List<Block> currentlyPrebuilt, ref bool currentlyBuilding, ref Block startingPoint) {
                if (preBuildOrRealBuild)
                {
                    currentlyPrebuilt.Clear();
                    currentlyBuilding = false;
                    startingPoint = null;
                }
            }


            static void OfferBridgeConstuction(Block sender, string direction)
            {
                DialogResult result = MessageBox.Show("Postavit most?", "Most?", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    if (direction == "horizontal")
                        sender.Image = horizontalOver;
                    else
                        sender.Image = verticalOver;
                    sender.BlockInfo = -1;
                    sender.ways = new bool[4, 4] {  {false, false, true, false}, //from top clockwise
                                                    {false, false, false, true}, //
                                                    {true, false, false, false}, //
                                                    {false, true, false, false}  //
                                                 };
                }
            }

            public static void StopBuilding()
            {
                if (currentlyPrebuilt != null)
                {
                    for (int i = 0; i < currentlyPrebuilt.Count; i++)
                    {
                        currentlyPrebuilt[i].Image = null;
                    }
                    currentlyPrebuilt.Clear();
                }

                if (startingPoint != null)
                    startingPoint = null;
                
                currentlyBuilding = false;

            }

            //creation of simple truned Rails
            static void turns_MouseClick(object sender, MouseEventArgs e, Block parentalSender)
            {
                buildOpts.Controls.Clear();
                buildOpts.Width = buildOpts.Width + 100;

                Block bottomright = new Block();
                Block leftbottom = new Block();
                Block righttop = new Block();
                Block topleft = new Block();

                bottomright.Image = rbottomright;
                leftbottom.Image = rleftbottom;
                righttop.Image = rrighttop;
                topleft.Image = rtopleft;

                bottomright.ways = new bool[4, 4] {  {false, false, false, false}, //from top clockwise
                                                     {false, false, true, false}, //
                                                     {false, true, false, false}, //
                                                     {false, false, false, false}  //
                                                  };
                leftbottom.ways = new bool[4, 4] {  {false, false, false, false}, //from top clockwise
                                                    {false, false, false, false}, //
                                                    {false, false, false, true}, //
                                                    {false, false, true, false}  //
                                                };
                righttop.ways = new bool[4, 4] {  {false, true, false, false}, //from top clockwise
                                                  {true, false, false, false}, //
                                                  {false, false, false, false}, //
                                                  {false, false, false, false}  //
                                               };
                topleft.ways = new bool[4, 4] {  {false, false, false, true}, //from top clockwise
                                                 {false, false, false, false}, //
                                                 {false, false, false, false}, //
                                                 {true, false, false, false}  //
                                              };

                List<Block> buildOptsControls = new List<Block>();
                buildOptsControls.Add(bottomright); buildOptsControls.Add(leftbottom); buildOptsControls.Add(righttop); buildOptsControls.Add(topleft);

                for (int i = 0; i < buildOptsControls.Count; i++)
                {
                    buildOptsControls[i].BlockInfo = -1;
                    buildOptsControls[i].MouseClick += (thisSender, eventArgs) => ExactTurn_MouseClick(thisSender, eventArgs, parentalSender);
                }
                AlignControlsInBuildOpts(buildOptsControls);
            }

            static void ExactTurn_MouseClick(object sender, MouseEventArgs e, Block parentalSender)
            {
                Block typeOfTurn = (Block)sender;
                parentalSender.ways = typeOfTurn.ways;
                parentalSender.Image = typeOfTurn.Image;
                parentalSender.BlockInfo = typeOfTurn.BlockInfo;
                buildOpts.Close();
            }

            //creation of switches
            static void switches_MouseClick(object sender, MouseEventArgs e, Block theParentalSender)
            {
                buildOpts.Controls.Clear();
                buildOpts.Width = buildOpts.Width + 500; // to hold all the posible Blocks
                buildOpts.Left = buildOpts.Left - 100; //because it went out of the screen to the right

                Block vbottomleft = new Block();
                Block vbottomright = new Block();
                Block vleftbottom = new Block();
                Block vlefttop = new Block();
                Block vrightbottom = new Block();
                Block vrighttop = new Block();
                Block vtopleft = new Block();
                Block vtopright = new Block();

                vbottomleft.Image = MainForm.vbottomleft;
                vbottomright.Image = MainForm.vbottomright;
                vleftbottom.Image = MainForm.vleftbottom;
                vlefttop.Image = MainForm.vlefttop;
                vrightbottom.Image = MainForm.vrightbottom;
                vrighttop.Image = MainForm.vrighttop;
                vtopleft.Image = MainForm.vtopleft;
                vtopright.Image = MainForm.vtopright;

                vbottomleft.ways = new bool[4, 4] {  {false, false, true, false}, //from top clockwise
                                                     {false, false, false, false}, //
                                                     {true, false, false, true}, //
                                                     {false, false, true, false}  //
                                                  };
                vbottomright.ways = new bool[4, 4] {  {false, false, true, false}, //
                                                      {false, false, true, false}, //
                                                      {true, true, false, false}, //
                                                      {false, false, false, false}  //
                                                   };
                vleftbottom.ways = new bool[4, 4] {  {false, false, false, false}, //
                                                     {false, false, false, true}, //
                                                     {false, false, false, true}, //
                                                     {false, true, true, false}  //
                                                  };
                vlefttop.ways = new bool[4, 4] {  {false, false, false, true}, //
                                                  {false, false, false, true}, //
                                                  {false, false, false, false}, //
                                                  {true, true, false, false}  //
                                               };
                vrightbottom.ways = new bool[4, 4] {  {false, false, false, false}, //
                                                      {false, false, true, true}, //
                                                      {false, true, false, false}, //
                                                      {false, true, false, false}  //
                                                   };
                vrighttop.ways = new bool[4, 4] {  {false, true, false, false}, //
                                                   {true, false, false, true}, //
                                                   {false, false, false, false}, //
                                                   {false, true, false, false}  //
                                                };
                vtopleft.ways = new bool[4, 4] {  {false, false, true, true}, //
                                                  {false, false, false, false}, //
                                                  {true, false, false, false}, //
                                                  {true, false, false, false}  //
                                               };
                vtopright.ways = new bool[4, 4] {  {false, true, true, false}, //
                                                   {true, false, false, false}, //
                                                   {true, false, false, false}, //
                                                   {false, false, false, false}  //
                                                };

                List<Block> buildOptsControls = new List<Block>();
                buildOptsControls.Add(vbottomleft); buildOptsControls.Add(vbottomright); buildOptsControls.Add(vleftbottom); buildOptsControls.Add(vlefttop);
                buildOptsControls.Add(vrightbottom); buildOptsControls.Add(vrighttop); buildOptsControls.Add(vtopleft); buildOptsControls.Add(vtopright);

                for (int i = 0; i < buildOptsControls.Count; i++)
                {
                    buildOptsControls[i].BlockInfo = 1; //defautly 1 for straightSwitch
                    buildOptsControls[i].MouseClick += (thisSender, eventArgs) => ExactSwitch_MouseClick(thisSender, eventArgs, theParentalSender);
                }

                AlignControlsInBuildOpts(buildOptsControls);
            }

            static void ExactSwitch_MouseClick(object sender, EventArgs e, Block parentalSender)
            {
                Block typeOfSwitch = (Block)sender;
                parentalSender.ways = typeOfSwitch.ways;
                parentalSender.Image = typeOfSwitch.Image;
                parentalSender.BlockInfo = typeOfSwitch.BlockInfo;
                buildOpts.Close();
            }

            //creation of buildings
            static void facilities_MouseClick(object sender, MouseEventArgs e)
            {
                buildOpts.Controls.Clear();
                buildOpts.Width += 200; //to hold all wanted blocks

                Block bigStation = new Block();
                Block bigStationVert = new Block();
                //Block depo = new Block();  //not implemented
                //Block depoVert = new Block(); //not implemented
                Block smallStation = new Block();
                Block SmallStationVert = new Block();

                bigStation.Image = MainForm.bigStation;
                bigStationVert.Image = MainForm.bigStationVert;
                smallStation.Image = MainForm.smallStation;
                SmallStationVert.Image = MainForm.smallStationVert;
                //depo.Image = MainForm.depo;
                //depoVert.Image = MainForm.depoVert;


                List<Block> buildOptsControls = new List<Block>();
                buildOptsControls.Add(bigStation); buildOptsControls.Add(bigStationVert); buildOptsControls.Add(smallStation);
                buildOptsControls.Add(SmallStationVert); //buildOptsControls.Add(depo); buildOptsControls.Add(depoVert);

                for (int i = 0; i < buildOptsControls.Count; i++)
                {
                    buildOptsControls[i].MouseClick += ExactFacility_MouseClick;
                }

                AlignControlsInBuildOpts(buildOptsControls);
            }

            public static Block currentFacility;
            static void ExactFacility_MouseClick(object sender, MouseEventArgs e)
            {
                currentFacility = (Block)sender;
                currentFacility.SizeMode = PictureBoxSizeMode.AutoSize;
                buildOpts.Close();
            }

            static List<PictureBox> preBuiltFacil = new List<PictureBox>();

            //to preshow the building while entering places to build
            public static void HandleMouseWhileFacilityBuilding(Block sender)
            {
                if (currentFacility != null)
                {
                    currentFacility.Location = sender.Location;
                    currentFacility.x = sender.x;
                    currentFacility.y = sender.y;
                    currentFacility.MouseClick -= ExactFacility_MouseClick;
                    currentFacility.MouseClick -= (thisSender, eventArgs) => currentFacility_MouseClick(thisSender, eventArgs, sender);
                    currentFacility.MouseDown += (thisSender, eventArgs) => currentFacility_MouseClick(thisSender, eventArgs, sender);
                    mainForm.Controls.Add(currentFacility);
                    preBuiltFacil.Add(currentFacility);
                    currentFacility.BringToFront();
                }
                
            }

            public static List<PictureBox> allFacilityPictureBoxes = new List<PictureBox>();

            static void currentFacility_MouseClick(object sender, MouseEventArgs e, Block parentalSender)
            {
                if (currentFacility != null)
                {
                    Block facil = (Block)sender;
                    facil.MouseClick -= (theSender, eventArgs) => currentFacility_MouseClick(theSender, eventArgs, parentalSender);
                    facil.MouseClick += facilityClick;
                    facil.MouseHover += Facility_MouseEnter;
                    allFacilityPictureBoxes.Add(facil);
                    BuildFacility(facil, parentalSender);
                    currentFacility = null;
                }
            }

            static void facilityClick(object sender, EventArgs e) {
                DialogResult result = MessageBox.Show("Odstranit budovu?", "Smazat?", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    RemoveFacility(sender);
                }
                
            }

            static void RemoveFacility(object sender_)
            {
                PictureBox sender = (PictureBox)sender_;
                for (int i = 0; i < allFacilities.Count; i++)
                {
                    if(allFacilities[i].Location == sender.Location)
                        allFacilities[i].facilityName = "";
                }
                if (sender.Image == bigStation)
                {

                    for (int i = 0; i < 3; i++) //3 is bigStation width
                    {
                        for (int j = 0; j < 2; j++) //2 is bigStation height
                        {
                            map[sender.Location.X /20 + i, sender.Location.Y /20 + j].BlockInfo = 0; 
                            map[sender.Location.X /20 + i, sender.Location.Y /20 + j].facilityName = "";
                        }
                    }
                }
                else if (sender.Image == bigStationVert)
                {
                    for (int i = 0; i < 2; i++) //2 is bigStation width
                    {
                        for (int j = 0; j < 3; j++) //3 is bigStation height
                        {
                            map[sender.Location.X /20 + i, sender.Location.Y /20 + j].BlockInfo = 0;
                            map[sender.Location.X /20 + i, sender.Location.Y /20 + j].facilityName = "";
                        }
                    }
                }
                else if (sender.Image == smallStation)
                {
                    map[sender.Location.X /20, sender.Location.Y /20].BlockInfo = 0;
                    map[sender.Location.X /20 + 1, sender.Location.Y /20].BlockInfo = 0;

                    map[sender.Location.X /20, sender.Location.Y /20].facilityName = "";
                    map[sender.Location.X /20 + 1, sender.Location.Y /20].facilityName = "";
                }
                else if (sender.Image == smallStationVert)
                {
                    map[sender.Location.X /20, sender.Location.Y /20].BlockInfo = 0;
                    map[sender.Location.X /20, sender.Location.Y /20 + 1].BlockInfo = 0;

                    map[sender.Location.X /20, sender.Location.Y /20].facilityName = "";
                    map[sender.Location.X /20, sender.Location.Y /20 + 1].facilityName = "";
                }
                else if (sender.Image == depo)
                {
                    map[sender.Location.X /20, sender.Location.Y /20].BlockInfo = 0;
                    map[sender.Location.X /20 + 1, sender.Location.Y /20].BlockInfo = 0;
                    map[sender.Location.X /20, sender.Location.Y /20 + 1].BlockInfo = 0;
                    map[sender.Location.X /20 + 1, sender.Location.Y /20 + 1].BlockInfo = 0;

                    map[sender.Location.X /20, sender.Location.Y /20].facilityName = "";
                    map[sender.Location.X /20 + 1, sender.Location.Y /20].facilityName = "";
                    map[sender.Location.X /20, sender.Location.Y /20 + 1].facilityName = "";
                    map[sender.Location.X /20 + 1, sender.Location.Y /20 + 1].facilityName = "";
                }
                else if (sender.Image == depoVert)
                {
                    map[sender.Location.X /20, sender.Location.Y /20].BlockInfo = 5;
                    map[sender.Location.X /20 + 1, sender.Location.Y /20].BlockInfo = 5;
                    map[sender.Location.X /20, sender.Location.Y /20 + 1].BlockInfo = 5;
                    map[sender.Location.X /20 + 1, sender.Location.Y /20 + 1].BlockInfo = 5;

                    map[sender.Location.X /20, sender.Location.Y /20].facilityName = "";
                    map[sender.Location.X /20 + 1, sender.Location.Y /20].facilityName = "";
                    map[sender.Location.X /20, sender.Location.Y /20 + 1].facilityName = "";
                    map[sender.Location.X /20+ 1, sender.Location.Y /20 + 1].facilityName = "";
                }
                sender.Dispose();
            }

            public static List<Block> allFacilities = new List<Block>();

            //fills all blocks under the Facility PictureBox with information about going through it
            static void BuildFacility(Block sender, Block parentalSender)
            {
                sender.BringToFront();
                allFacilities.Add(sender);
                string name = GetTheName();
                sender.facilityName = name;

                //then to modify all blocks below it by current Facility type
                if (sender.Image == bigStation)
                {
                     
                    for (int i = 0; i < 3; i++) //3 is bigStation width
                    {
                        for (int j = 0; j < 2; j++) //2 is bigStation height
                        {
                            map[sender.x + i, sender.y + j].BlockInfo = 3; //thi is vulnerable for zooming
                            map[sender.x + i, sender.y + j].facilityName = name;
                        } 
                    }

                    map[sender.x, sender.y].ways = new bool[1, 4] { { false, true, false, true } };
                    map[sender.x + 1, sender.y].ways = new bool[1, 4] { { false, true, false, true } };
                    map[sender.x + 2, sender.y].ways = new bool[1, 4] { { false, true, false, true } };
                    map[sender.x, sender.y + 1].ways = new bool[1, 4] { { false, true, false, true } };
                    map[sender.x + 1, sender.y + 1].ways = new bool[1, 4] { { false, true, false, true } };
                    map[sender.x + 2, sender.y + 1].ways = new bool[1, 4] { { false, true, false, true } };
                }
                else if (sender.Image == bigStationVert)
                {

                    for (int i = 0; i < 2; i++) //2 is bigStation width
                    {
                        for (int j = 0; j < 3; j++) //3 is bigStation height
                        {
                            map[sender.x + i, sender.y + j].BlockInfo = 3;
                            map[sender.x + i, sender.y + j].facilityName = name;
                        }
                    }

                    map[sender.x, sender.y].ways = new bool[1, 4] { { true, false, true, false } };
                    map[sender.x + 1, sender.y].ways = new bool[1, 4] { { true, false, true, false } };
                    map[sender.x, sender.y + 1].ways = new bool[1, 4] { { true, false, true, false } };
                    map[sender.x + 1, sender.y + 1].ways = new bool[1, 4] { { true, false, true, false } };
                    map[sender.x, sender.y + 2].ways = new bool[1, 4] { { true, false, true, false } };
                    map[sender.x + 1, sender.y + 2].ways = new bool[1, 4] { { true, false, true, false } };
                }
                else if (sender.Image == smallStation)
                {
                    map[sender.x, sender.y].BlockInfo = 4;
                    map[sender.x + 1, sender.y].BlockInfo = 4;

                    map[sender.x, sender.y].facilityName = name;
                    map[sender.x + 1, sender.y].facilityName = name;

                    map[sender.x, sender.y].ways = new bool[1, 4] { { false, true, false, true } };
                    map[sender.x + 1, sender.y].ways = new bool[1, 4] { { false, true, false, true } };
                }
                else if (sender.Image == smallStationVert)
                {
                    map[sender.x, sender.y].BlockInfo = 4;
                    map[sender.x, sender.y + 1].BlockInfo = 4;

                    map[sender.x, sender.y].facilityName = name;
                    map[sender.x, sender.y + 1].facilityName = name;

                    map[sender.x, sender.y].ways = new bool[1, 4] { { true, false, true, false } };
                    map[sender.x, sender.y + 1].ways = new bool[1, 4] { { true, false, true, false } };
                }
                else if (sender.Image == depo)
                {
                    map[sender.x, sender.y].BlockInfo = 5;
                    map[sender.x + 1, sender.y].BlockInfo = 5;
                    map[sender.x, sender.y + 1].BlockInfo = 5;
                    map[sender.x + 1, sender.y + 1].BlockInfo = 5;

                    map[sender.x, sender.y].facilityName = name;
                    map[sender.x + 1, sender.y].facilityName = name;
                    map[sender.x, sender.y + 1].facilityName = name;
                    map[sender.x + 1, sender.y + 1].facilityName = name;

                    map[sender.x, sender.y].ways = new bool[1, 4] { { false, false, false, false } };
                    map[sender.x + 1, sender.y].ways = new bool[1, 4] { { false, false, false, false } };
                    map[sender.x, sender.y + 1].ways = new bool[1, 4] { { false, true, false, true } };
                    map[sender.x + 1, sender.y + 1].ways = new bool[1, 4] { { false, true, true, false } };
                }
                else if (sender.Image == depoVert)
                {
                    map[sender.x, sender.y].BlockInfo = 5;
                    map[sender.x + 1, sender.y].BlockInfo = 5;
                    map[sender.x, sender.y + 1].BlockInfo = 5;
                    map[sender.x + 1, sender.y + 1].BlockInfo = 5;

                    map[sender.x, sender.y].facilityName = name;
                    map[sender.x + 1, sender.y].facilityName = name;
                    map[sender.x, sender.y + 1].facilityName = name;
                    map[sender.x + 1, sender.y + 1].facilityName = name;

                    map[sender.x, sender.y].ways = new bool[1, 4] { { true, false, true, false } };
                    map[sender.x + 1, sender.y].ways = new bool[1, 4] { { false, false, false, false } };
                    map[sender.x, sender.y + 1].ways = new bool[1, 4] { { true, false, true, false } };
                    map[sender.x + 1, sender.y + 1].ways = new bool[1, 4] { { false, false, false, false } };
                }
            }

            public static string GetTheName(){
                string name = "";
                while (name == "")
                {
                    name = SimpleInputDialog("Zadejte název stanice");
                    for (int i = 0; i < allFacilities.Count; i++)
                    {
                        if (name == allFacilities[i].facilityName)
                        {
                            name = "";
                            MessageBox.Show("Tento název už je zabraný");
                        }

                    }
                }
                return name;
            }

            static void AlignControlsInBuildOpts(List<Block> controls)
            {
                for (int i = 0; i < controls.Count; i++)
                {
                    controls[i].Size = new Size(50 , 50);
                    controls[i].SizeMode = PictureBoxSizeMode.Zoom;
                    controls[i].Top = buildOpts.Height / 2 - 42;
                    controls[i].Left = buildOpts.Width / controls.Count  * (i+1) - buildOpts.Width / (controls.Count + 1); //formula for even positioning
                    buildOpts.Controls.Add(controls[i]);
                }
            }


            static Form loadSave;

            public static void ShowLoadSaveMenu(bool saveIt){
                loadSave = new Form();
                loadSave.Width = 150;
                loadSave.Height = 130;
                Button load = new Button();
                load.Location = new Point(10, 10);
                load.Width = 100;
                load.Text = "Nahraj mapu";
                loadSave.Controls.Add(load);
                load.Click += load_Click;
                if (saveIt)
                {
                    Button save = new Button();
                    save.Text = "Ulož mapu";
                    save.Width = 100;
                    save.Location = new Point(10, 45);
                    loadSave.Controls.Add(save);
                    save.Click += save_Click;
                }
                
                loadSave.ShowDialog();
            }


            //deserializes list of objects to recreate the map
            static void load_Click(object sender, EventArgs e)
            {
                TrainDriving.allTrains.Clear();
                loadSave.Close();
                input = new Form();
                ComboBox maps = new ComboBox();
                maps.Location = new Point(25, 10);
                maps.Width = 150;
                input.Width = 210;
                Button ok = new Button();
                ok.Location = new Point(55, 40);
                ok.Text = "Ok";
                ok.Click += ok_Click;
                input.Controls.Add(ok);
                maps.DropDownStyle = ComboBoxStyle.DropDownList;
                List<string> files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.xml")
                                     .ToList();
                foreach (var file in files) {
                    maps.Items.Add(Path.GetFileName(file));
                }
                input.Controls.Add(maps);
                saveorLoadDR = new DialogResult();
                saveorLoadDR = DialogResult.None;
                input.ShowDialog();
                if (saveorLoadDR == DialogResult.OK && maps.SelectedItem != null)
                {
                    mapSelected = files[maps.SelectedIndex];
                    for (int i = 0; i < Building.allFacilities.Count; i++)
                    {
                        Building.allFacilityPictureBoxes[i].Dispose();
                    }
                    Building.allFacilityPictureBoxes.Clear();
                    Building.allFacilities.Clear();

                    foreach (Block b in MainForm.tableLayoutPanel.Controls)
                    {
                        b.Image = null;
                        b.BlockInfo = 0;
                    }

                    string dir = @"C:\\temp";
                    string serializationFile = Path.Combine(dir, files[maps.SelectedIndex]);
                    input.Close();
                    MainForm.mainForm.Cursor = Cursors.WaitCursor;

                    //deserialize
                    using (Stream stream = File.Open(serializationFile, FileMode.Open))
                    {
                        XmlSerializer deserializer = new XmlSerializer(typeof(List<SerializableBlock>));
                        List<SerializableBlock> blocks = new List<SerializableBlock>();
                        blocks = (List<SerializableBlock>)deserializer.Deserialize(stream);

                        foreach (SerializableBlock block in blocks)
                        {
                            for (int i = 0; i < tableLayoutPanel.Controls.Count; i++)
                            {
                                if (tableLayoutPanel.Controls[i].Width == 20) //trick to get the blocks
                                {
                                    Block b = (Block)tableLayoutPanel.Controls[i];
                                    if (block.x == b.x && block.y == b.y)
                                    {
                                        if (block.ways != null)
                                            b.ways = ConvertFromJaggedToMulti(block.ways);
                                        b.BlockInfo = block.BlockInfo;
                                        b.facilityName = block.facilityName;
                                        if (block.Image != null)
                                            b.Image = FindOutTheImage(block.Image);
                                    }
                                }
                            }
                        }


                        if (creatingMap)
                            BuildFacilitiesFromXml(blocks, true);
                    }

                    allSwitches.Clear();

                    for (int i = 0; i < mapWidth; i++)
                    {
                        for (int j = 0; j < mapHeight; j++)
                        {
                            if (map[i, j].BlockInfo == 1 || map[i, j].BlockInfo == 2)
                                allSwitches.Add(map[i, j]);
                        }
                    }
                    loadCompleted = true;
                    AITrains.trainsOverview.Clear();

                }
                MainForm.mainForm.Cursor = Cursors.Default;

                
            }

            //rebuilds all facility pictureBoxes
            public static void BuildFacilitiesFromXml(List<SerializableBlock> blocks, bool passClickEvent)
            {
                for (int i = 0; i < blocks.Count; i++)
                {
                    try
                    {
                        if (blocks[i].BlockInfo == 3 && map[blocks[i].x + 1, blocks[i].y].facilityName == map[blocks[i].x + 2, blocks[i].y].facilityName &&
                            blocks[i].facilityName == map[blocks[i].x + 1, blocks[i].y].facilityName && blocks[i].facilityName == map[blocks[i].x, blocks[i].y + 1].facilityName)
                        {
                            //rebuild bigstation

                            RebuildFromImage(bigStation, map[blocks[i].x, blocks[i].y].Location, passClickEvent);

                        }
                        if (blocks[i].BlockInfo == 3 && map[blocks[i].x + 1, blocks[i].y].facilityName == map[blocks[i].x, blocks[i].y+1].facilityName &&
                            blocks[i].facilityName == map[blocks[i].x , blocks[i].y + 1].facilityName && blocks[i].facilityName == map[blocks[i].x, blocks[i].y + 2].facilityName)
                        {
                            //rebuild bigVert

                            RebuildFromImage(bigStationVert, map[blocks[i].x, blocks[i].y].Location, passClickEvent);
                            
                        }
                        if (blocks[i].BlockInfo == 4 && blocks[i].facilityName == map[blocks[i].x + 1, blocks[i].y].facilityName)
                        {
                            //rebuild smallStation

                            RebuildFromImage(smallStation, map[blocks[i].x, blocks[i].y].Location, passClickEvent);

                        }
                        if (blocks[i].BlockInfo == 4 && blocks[i].facilityName == map[blocks[i].x, blocks[i].y + 1].facilityName)
                        {
                            //rebuild smallVert

                            RebuildFromImage(smallStationVert, map[blocks[i].x, blocks[i].y].Location, passClickEvent);

                        }
                        if (blocks[i].BlockInfo == 5 && map[blocks[i].x, blocks[i].y + 1].ways[0,3] == true && map[blocks[i].x + 1, blocks[i].y ].facilityName == map[blocks[i].x, blocks[i].y].facilityName) 
                        {
                            //depo

                            RebuildFromImage(depo, map[blocks[i].x, blocks[i].y].Location, passClickEvent);
                        }
                        if (blocks[i].BlockInfo == 5 && map[blocks[i].x, blocks[i].y + 1].ways[0, 2] == true && map[blocks[i].x + 1, blocks[i].y].facilityName == map[blocks[i].x, blocks[i].y].facilityName)
                        {
                            //depoVert

                            RebuildFromImage(depoVert, map[blocks[i].x, blocks[i].y].Location, passClickEvent);
                        }
                    }
                    catch (ArgumentOutOfRangeException) { }
                    catch (IndexOutOfRangeException) { }
                }
            }

            //shows the facility image
            static void RebuildFromImage(Bitmap image, Point location, bool Click)
            {
                PictureBox p = new PictureBox();
                p.Image = image;
                p.SizeMode = PictureBoxSizeMode.AutoSize;
                p.Location = location;
                mainForm.Controls.Add(p);
                p.BringToFront();
                if (Click)
                    p.MouseClick += facilityClick;
                p.MouseMove += Facility_MouseEnter;
                allFacilities.Add(MainForm.map[location.X / 20, location.Y / 20]);
                allFacilityPictureBoxes.Add(p);
            }

            //shows tooltip with name
            static void Facility_MouseEnter(object sender_, EventArgs e)
            {
                PictureBox sender = (PictureBox)sender_;
                tooltip.SetToolTip(sender, map[sender.Location.X / currentBlockSize, sender.Location.Y / currentBlockSize].facilityName);
                //tooltip.Show("" + map[sender.Location.X/currentBlockSize, sender.Location.Y/currentBlockSize].facilityName , playBox,sender.Location.X, sender.Location.Y,1);
            }

            public static DialogResult saveorLoadDR;
            public static Form input;

            //serializes list of object into xml
            static void save_Click(object sender, EventArgs e)
            {
                loadSave.Close();
                string name = "";
                input = new Form();
                input.Width = 200;
                input.Height = 140;
                TextBox tb = new TextBox();
                tb.Location = new Point(25, 20);
                Label l = new Label();
                l.Width = 250;
                l.Text = "Zadejte unikátní název mapy";
                l.Location = new Point(15,5);
                Button ok = new Button();
                ok.Text = "Ok";
                ok.Location = new Point(25,45);
                ok.Click += ok_Click;
                input.Controls.Add(tb);input.Controls.Add(l);input.Controls.Add(ok);
                saveorLoadDR = new DialogResult();
                
                while (name == "") {
                    input.ShowDialog();
                    if (saveorLoadDR == DialogResult.OK && l.Text != ""){
                        if (File.Exists(AppDomain.CurrentDomain.BaseDirectory.ToString() + tb.Text + ".xml"))
                        {
                            MessageBox.Show("Takové jméno už nějaká mapa má");
                            continue;
                        }
                            
                        name = AppDomain.CurrentDomain.BaseDirectory.ToString() + tb.Text + ".xml";
                        break;
                    }
                    MessageBox.Show("Příště zkuste napsat jméno prosím");
                }
                string dir = @"C:\\temp";
                string serializationFIle = Path.Combine(dir, name);

                //serialize
                using (Stream stream = File.Open(serializationFIle, FileMode.Create))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(List<SerializableBlock>));
                    List<SerializableBlock> blocks = new List<SerializableBlock>();
                    foreach (Block t in tableLayoutPanel.Controls)
                    {
                        SerializableBlock b = new SerializableBlock();
                        b.x = t.x;
                        b.y = t.y;
                        b.facilityName = t.facilityName;
                        if (t.ways != null)
                            b.ways = ConvertFromMultiToJagged(t.ways);
                        b.BlockInfo = t.BlockInfo;
                        if (t.Image != null)
                            b.Image = ImageToBase64(t.Image);
                        blocks.Add(b);
                    }
                    
                    serializer.Serialize(stream, blocks);
                }
            }

            static void ok_Click(object sender, EventArgs e)
            {
                input.Close();
                saveorLoadDR = DialogResult.OK;
            }
        }

        public static bool[][] ConvertFromMultiToJagged(bool[,] my2DArray) //becasue 2D array wasn't serializable
        {
            bool[][] myJaggedArray = new bool[my2DArray.GetLength(0)][];
            for (int i = 0; i < my2DArray.GetLength(0); i++)
            {
                myJaggedArray[i] = new bool[my2DArray.GetLength(1)];
                for (int j = 0; j < my2DArray.GetLength(1); j++)
                {
                    myJaggedArray[i][j] = my2DArray[i, j];
                }
            }
            return myJaggedArray;
        }

        public static bool[,] ConvertFromJaggedToMulti(bool[][] myJaggedArray)//becasue 2D array wasn't serializable
        {
            bool[,] my2Darray = new bool[myJaggedArray.Length, myJaggedArray[0].Length];

            for (int i = 0; i < myJaggedArray.GetLength(0); i++)
            {
                for (int j = 0; j < myJaggedArray[0].Length; j++)
                {
                    my2Darray[i, j] = myJaggedArray[i][j];
                }
            }
            return my2Darray;
        }

        public static Form mainForm;
        public static TableLayoutPanel tableLayoutPanel;
        public MainForm()
        {
            InitializeComponent();


            //to smoother animations
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.AllPaintingInWmPaint | ControlStyles.SupportsTransparentBackColor,
            true);
            SetStyle(ControlStyles.DoubleBuffer, true);
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            //

            tooltip.OwnerDraw = false;
            tooltip.ShowAlways = true;

            BuildMap();

            mainForm = this;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            this.ShowIcon = false;
            this.Text = "TrainSim";
            //hide the panel
            tableLayoutPanel = tableLayoutPanel1;
            tableLayoutPanel1.Hide();
            //show the starting menu
            ShowStartMenu();
        }
        
        //main menu
        void ShowStartMenu()
        {
            this.BackgroundImage = Properties.Resources.trainScreen;
            this.BackgroundImageLayout = ImageLayout.Center;
            this.BackColor = Color.Green;

            PictureBox createMap = new PictureBox();
            createMap.Image = Properties.Resources.vytvorMapu;
            createMap.Location = new Point(100,300);
            createMap.Size = Properties.Resources.vytvorMapu.Size;
            createMap.MouseClick += createMap_MouseClick;
            createMap.Cursor = Cursors.Hand;
            this.Controls.Add(createMap);

            PictureBox playMap = new PictureBox(); //is used just for map pick
            playMap.Image = Properties.Resources.hrajMapu;
            playMap.Location = new Point(100, 360);
            playMap.Size = Properties.Resources.hrajMapu.Size;
            playMap.Cursor = Cursors.Hand;
            playMap.MouseClick += playMap_MouseClick;
            this.Controls.Add(playMap);

            PictureBox timeTables = new PictureBox();
            timeTables.Image = Properties.Resources.jizdniRady;
            timeTables.Location = new Point(950, 300);
            timeTables.Size = Properties.Resources.jizdniRady.Size;
            timeTables.Cursor = Cursors.Hand;
            timeTables.MouseClick += timeTables_MouseClick;
            this.Controls.Add(timeTables);

            PictureBox startSimulation = new PictureBox();
            startSimulation.Image = Properties.Resources.spustSimulaci;
            startSimulation.Location = new Point(950, 360);
            startSimulation.Cursor = Cursors.Hand;
            startSimulation.MouseClick += startSimulation_MouseClick;
            startSimulation.Size = Properties.Resources.spustSimulaci.Size;
            this.Controls.Add(startSimulation);

        }

        //the main StopWatch by which every train orientates
        public static Label timeLabel = new Label();
        public static System.Diagnostics.Stopwatch mainStopwatch = new System.Diagnostics.Stopwatch();

        //timer set to same interval as the mainStowatch ticks
        static System.Timers.Timer mainTimer = new System.Timers.Timer();


        //handler for starting the simulation
        public void startSimulation_MouseClick(object sender, MouseEventArgs e)
        {

            
            Building.allFacilities.Clear();
            for (int i = 0; i < Building.allFacilityPictureBoxes.Count; i++)
            {
                Building.allFacilityPictureBoxes[i].Dispose();
            }
            Building.allFacilityPictureBoxes.Clear();

            if (mapSelected != null)
            {
                playBox.BringToFront();
                playingMap = true;


                //find all facilities and rebuild them

                // this code is just to be able to reuse long function BuildFacilitiesFromXML
                List<SerializableBlock> blocks = new List<SerializableBlock>();
                foreach (Block t in tableLayoutPanel.Controls)
                {
                    SerializableBlock b = new SerializableBlock();
                    b.x = t.x;
                    b.y = t.y;
                    b.facilityName = t.facilityName;
                    if (t.ways != null)
                        b.ways = ConvertFromMultiToJagged(t.ways);
                    b.BlockInfo = t.BlockInfo;
                    if (t.Image != null)
                        b.Image = ImageToBase64(t.Image);
                    blocks.Add(b);
                }

                Building.BuildFacilitiesFromXml(blocks, false);

                playBox.Show();


                //the main invalidating timer
                mainTimer.Elapsed += mainTimer_Elapsed;
                mainTimer.Interval = 1000;
                mainTimer.Enabled = true;
                
                //just to simulate the first tick of the timer
                for (int i = 0; i < TrainDriving.allTrains.Count; i++)
                {
                    TrainDriving.tmrMoving_Elapsed(new object(), new EventArgs(), TrainDriving.allTrains[i]);
                }

                mainStopwatch.Start();
                timeLabel.Location = new Point(1200, 600);
                timeLabel.Size = new Size(100, 35);
                this.Controls.Add(timeLabel);
                timeLabel.Show();
                timeLabel.BringToFront();
            }
        }


        public static bool timeTabling;

        //handler for creating timetables for trains
        void timeTables_MouseClick(object sender, MouseEventArgs e)
        {
            if (mapSelected != null && !timeTabling)
            {
                timeTabling = true;
                Building.allFacilities.Clear();
                for (int i = 0; i < Building.allFacilityPictureBoxes.Count; i++)
                {
                    Building.allFacilityPictureBoxes[i].Dispose();
                }
                Building.allFacilityPictureBoxes.Clear();

                AITrains.ShowTrainsOverview();
                //find all facilities and rebuild them

                // this code is just to be able to reuse long function BuildFacilitiesFromXML
                List<SerializableBlock> blocks = new List<SerializableBlock>();
                foreach (Block t in tableLayoutPanel.Controls)
                {
                    SerializableBlock b = new SerializableBlock();
                    b.x = t.x;
                    b.y = t.y;
                    b.facilityName = t.facilityName;
                    if (t.ways != null)
                        b.ways = ConvertFromMultiToJagged(t.ways);
                    b.BlockInfo = t.BlockInfo;
                    if (t.Image != null)
                        b.Image = ImageToBase64(t.Image);
                    blocks.Add(b);
                }
                playBox.Show();

                Building.BuildFacilitiesFromXml(blocks, false);
            }
        }


        public static PictureBox playBox = new PictureBox();
        public static bool playingMap;
        public static string mapSelected;
        static bool loadCompleted;

        //just to select the wanted map -> it loads and hides untill you want to play it
        void playMap_MouseClick(object sender, MouseEventArgs e)
        {
            tableLayoutPanel1.Hide();
            playBox.Hide();

            for (int i = 0; i < Building.allFacilityPictureBoxes.Count; i++)
            {
                Building.allFacilityPictureBoxes[i].Dispose();
            }
            Building.allFacilities.Clear();
            Building.allFacilityPictureBoxes.Clear();


            //this builds whole map of blocks on tableLayoutPanel
            Building.ShowLoadSaveMenu(false);
            
            if (loadCompleted)
            {
                
                for (int i = 0; i < Building.allFacilityPictureBoxes.Count; i++)
                {
                    Building.allFacilityPictureBoxes[i].Dispose();
                }

                playBox = new PictureBox();
                Cursor = Cursors.WaitCursor;
                Bitmap bmp = new Bitmap(tableLayoutPanel1.Width, tableLayoutPanel1.Height);
                //draws every block into one big bitmap
                foreach (Block b in tableLayoutPanel1.Controls)
                {
                    b.DrawToBitmap(bmp, new Rectangle(b.Location, b.Size));
                }

                //creates a new picture which overlays the whole tablelaoutpanel1

                playBox.Size = new Size(tableLayoutPanel1.Width, tableLayoutPanel1.Height);
                playBox.Image = bmp;
                playBox.Location = Point.Empty;
                mainForm.Controls.Add(playBox);
                playBox.MouseDown += playBox_MouseDown; //implemented in PlayMode.cs
                //playBox.MouseMove += playBox_MouseHover;
                playBox.Paint += playBox_Paint;
                playBox.Hide();

                Cursor = Cursors.Default;
            }

            loadCompleted = false;
            
            for (int i = 0; i < Building.allFacilityPictureBoxes.Count; i++)
			{
                Building.allFacilityPictureBoxes[i].Dispose();
			}
            Building.allFacilityPictureBoxes.Clear();
            
        }

        public static int mainStopWatchInt;
        static int stopWatchBefore = -1;

        //causes reDrawing
        static void mainTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            mainStopWatchInt = (int)mainStopwatch.Elapsed.TotalSeconds;
            playBox.Invalidate();
        }


        void playBox_Paint(object sender, PaintEventArgs e)
        {
            bool hasBeenRunning = false;
            if (mainStopwatch.IsRunning)
                hasBeenRunning = true;
            
            //stops the timeMashines to avoid re-entering this code if in redraws too fast
            mainTimer.Enabled = false;
            mainStopwatch.Stop();

            RepaintSwitches(e);

            //if the time has changed
            if (stopWatchBefore != mainStopWatchInt)
            {
                stopWatchBefore = mainStopWatchInt;

                //moves all the trains
                for (int i = 0; i < TrainDriving.allTrains.Count; i++)
                {
                    TrainDriving.tmrMoving_Elapsed(new object(), new EventArgs(), TrainDriving.allTrains[i]);
                }
                
            }

            //redraws all trains
            for (int i = 0; i < TrainDriving.allTrains.Count; i++)
            {
                TrainDriving.TrainAction(TrainDriving.allTrains[i], e, mainStopWatchInt);
            }

            //redraws playerTrain
            if(currentlyDrivenTrain != null)
                TrainDriving.TrainAction(currentlyDrivenTrain, e, 0);

            try
            {
                //checks for playerDrivenTrain collisions
                if (currentlyDrivenTrain != null)
                {
                    for (int i = 0; i < TrainDriving.allTrains.Count; i++)
                    {
                        for (int j = 0; j < TrainDriving.allTrains[i].occupiedRails.Count; j++)
                        {
                            for (int k = 0; k < currentlyDrivenTrain.occupiedRails.Count; k++)
                            {
                                if (currentlyDrivenTrain.occupiedRails[k].x == TrainDriving.allTrains[i].occupiedRails[j].x && currentlyDrivenTrain.occupiedRails[k].y == TrainDriving.allTrains[i].occupiedRails[j].y)
                                {
                                    TrainDriving.TrainCrash(currentlyDrivenTrain, TrainDriving.allTrains[i], currentlyDrivenTrain.occupiedRails[k], e);
                                    goto AfterCrash;
                                }
                            }
                        }
                    }
                }

                //if one is offSchedule -> check for collision
                for (int i = 0; i < TrainDriving.allTrains.Count; i++)
                {
                    for (int j = 0; j < TrainDriving.trainsOffSchedule.Count; j++)
                    {
                        for (int k = 0; k < TrainDriving.allTrains[i].occupiedRails.Count; k++)
                        {
                            for (int l = 0; l < TrainDriving.trainsOffSchedule[j].occupiedRails.Count; l++)
                            {
                                if (TrainDriving.trainsOffSchedule[j].occupiedRails[l].x == TrainDriving.allTrains[i].occupiedRails[k].x && TrainDriving.trainsOffSchedule[j].occupiedRails[l].y == TrainDriving.allTrains[i].occupiedRails[k].y && TrainDriving.allTrains[i].offSchedule == false)
                                {
                                    TrainDriving.TrainCrash(TrainDriving.trainsOffSchedule[j], TrainDriving.allTrains[i], TrainDriving.allTrains[i].occupiedRails[k], e);
                                    goto AfterCrash;
                                }
                            }
                        }
                    }
                }

                //if both are offSchedule -> check for collision
                for (int i = 0; i < TrainDriving.trainsOffSchedule.Count - 1; i++)
                {
                    for (int k = 0; k < TrainDriving.trainsOffSchedule[i].occupiedRails.Count; k++)
                    {
                        for (int l = 0; l < TrainDriving.trainsOffSchedule[i + 1].occupiedRails.Count; l++)
                        {
                            if (TrainDriving.trainsOffSchedule[i].occupiedRails[l].x == TrainDriving.allTrains[i + 1].occupiedRails[k].x && TrainDriving.trainsOffSchedule[i].occupiedRails[l].y == TrainDriving.allTrains[i + 1].occupiedRails[k].y)
                            {
                                TrainDriving.TrainCrash(TrainDriving.trainsOffSchedule[i], TrainDriving.trainsOffSchedule[i + 1], TrainDriving.trainsOffSchedule[i].occupiedRails[k], e);
                                goto AfterCrash;
                            }
                        }
                    }
                }
            }
            catch (ArgumentOutOfRangeException) { }



            AfterCrash:

            if ((int)mainStopwatch.Elapsed.TotalMinutes == 24)
                mainStopwatch.Restart();
            timeLabel.Text = mainStopwatch.Elapsed.ToString("mm\\:ss");
            if(hasBeenRunning)
                mainStopwatch.Start();
            mainTimer.Enabled = true;
        }

        public static List<Block> allSwitches = new List<Block>();
        static SolidBrush brush = new SolidBrush(Color.Red);
        static Pen pen = new Pen(Color.Red,2);

        //shows red lines and dots to indicate switch state
        public static void RepaintSwitches(PaintEventArgs g)
        {
            for (int i = 0; i < allSwitches.Count; i++)
            {
                if (allSwitches[i].Image == vbottomleft || allSwitches[i].Image == vtopleft)
                {
                    if (allSwitches[i].BlockInfo == 1)
                        g.Graphics.DrawLine(pen, new Point(allSwitches[i].Location.X + 18, allSwitches[i].Location.Y + 2), new Point(allSwitches[i].Location.X + 18, allSwitches[i].Location.Y + 18));
                    else
                        g.Graphics.FillRectangle(brush,new Rectangle(new Point(allSwitches[i].Location.X + 16, allSwitches[i].Location.Y + 8),new Size(3,4)));
                }
                else if (allSwitches[i].Image == vbottomright || allSwitches[i].Image == vtopright)
                {
                     if (allSwitches[i].BlockInfo == 1)
                        g.Graphics.DrawLine(pen, new Point(allSwitches[i].Location.X + 1, allSwitches[i].Location.Y + 2), new Point(allSwitches[i].Location.X + 1, allSwitches[i].Location.Y + 18));
                    else
                        g.Graphics.FillRectangle(brush,new Rectangle(new Point(allSwitches[i].Location.X + 1, allSwitches[i].Location.Y + 8),new Size(3,4)));
                }
                else if (allSwitches[i].Image == vlefttop || allSwitches[i].Image == vrighttop)
                {
                     if (allSwitches[i].BlockInfo == 1)
                        g.Graphics.DrawLine(pen, new Point(allSwitches[i].Location.X + 2, allSwitches[i].Location.Y + 18), new Point(allSwitches[i].Location.X + 18, allSwitches[i].Location.Y + 18));
                    else
                        g.Graphics.FillRectangle(brush,new Rectangle(new Point(allSwitches[i].Location.X + 8, allSwitches[i].Location.Y + 16),new Size(4,3)));
                }
                else if (allSwitches[i].Image == vleftbottom || allSwitches[i].Image == vrightbottom)
                {
                     if (allSwitches[i].BlockInfo == 1)
                        g.Graphics.DrawLine(pen, new Point(allSwitches[i].Location.X + 2, allSwitches[i].Location.Y + 1), new Point(allSwitches[i].Location.X + 18, allSwitches[i].Location.Y + 1));
                     else
                        g.Graphics.FillRectangle(brush,new Rectangle(new Point(allSwitches[i].Location.X + 8, allSwitches[i].Location.Y + 1),new Size(4,3)));
                }
            }
        }

        public static bool creatingMap;
        void createMap_MouseClick(object sender, MouseEventArgs e)
        {
            creatingMap = true;
            for (int i = 0; i < Building.allFacilityPictureBoxes.Count; i++)
            {
                Building.allFacilityPictureBoxes[i].Dispose();
            }
            Building.allFacilities.Clear();
            Building.allFacilityPictureBoxes.Clear();
            foreach (Block b in tableLayoutPanel1.Controls)
            {
                b.Image = null;
                b.BlockInfo = 0;
            }
            tableLayoutPanel1.Show();
        }

        void BuildMap()
        {
            zoomState = 0;
            //this.MouseWheel += MainForm_MouseWheel; not implemented, used for zooming

            map = new Block[mapWidth, mapHeight];

            for (int i = 0; i < mapHeight; i++)
            {
                for (int j = 0; j < mapWidth; j++)
                {
                    Block p = new Block();
                    p.Margin = new Padding(0, 0, 0, 0);
                    p.x = j;
                    p.y = i;
                    map[j, i] = p;
                    p.Dock = DockStyle.Fill;
                    p.BackColor = Color.Green;

                    p.MouseEnter += new System.EventHandler(this.HandleMouseEnter);
                    p.MouseLeave += new System.EventHandler(this.HandleMouseLeave);

                    p.SizeMode = PictureBoxSizeMode.StretchImage;
                    p.MouseClick += Building.p_MouseClick;

                    tableLayoutPanel1.Controls.Add(p);
                }
            }
        }

        //determines if computer driven train can go
        static void AITimer_Elapsed(object sender, EventArgs e)
        {
            int correctedTime;
            int time;
            if (mainStopwatch.IsRunning)
            {
                time = (int)mainStopwatch.Elapsed.TotalSeconds;

                for (int i = 0; i < TrainDriving.allTrains.Count; i++)
                {
                    if (TrainDriving.allTrains[i].trainSchedule[time].start == true)
                    {
                        TrainDriving.allTrains[i].currentPositionX = TrainDriving.allTrains[i].trainSchedule[time].positionX;
                        TrainDriving.allTrains[i].currentPositionY = TrainDriving.allTrains[i].trainSchedule[time].positionY;
                        TrainDriving.allTrains[i].orientations[0] = TrainDriving.allTrains[i].trainSchedule[time].direction;
                        TrainDriving.allTrains[i].velocity = 1;

                        for (int j = 0; j < 3; j++)
                        {
                            if (time + j > 23 * 60 + 59)
                                correctedTime = time + j - (23 * 60 + 59);
                            else
                                correctedTime = time + j;
                            try
                            {
                                if (map[TrainDriving.allTrains[i].trainSchedule[correctedTime].positionX, TrainDriving.allTrains[i].trainSchedule[correctedTime].positionY].BlockInfo == 1 &&
                                    TrainDriving.allTrains[i].trainSchedule[correctedTime].switched)
                                {
                                    map[TrainDriving.allTrains[i].trainSchedule[correctedTime].positionX, TrainDriving.allTrains[i].trainSchedule[correctedTime].positionY].BlockInfo = 2;
                                }
                                else if (map[TrainDriving.allTrains[i].trainSchedule[correctedTime].positionX, TrainDriving.allTrains[i].trainSchedule[correctedTime].positionY].BlockInfo == 2 &&
                                    TrainDriving.allTrains[i].trainSchedule[correctedTime].switched == false)
                                {
                                    map[TrainDriving.allTrains[i].trainSchedule[correctedTime].positionX, TrainDriving.allTrains[i].trainSchedule[correctedTime].positionY].BlockInfo = 1;
                                }
                            }
                            catch (IndexOutOfRangeException) { }
                        }

                    }
                    if (TrainDriving.allTrains[i].trainSchedule[time].stop == true)
                    {
                        TrainDriving.allTrains[i].currentPositionX = -1;
                        TrainDriving.allTrains[i].currentPositionY = -1;
                        TrainDriving.allTrains[i].velocity = 0;
                    }
                    if (TrainDriving.allTrains[i].trainSchedule[time].waiting == false)
                    {
                        TrainDriving.allTrains[i].canMove = true;
                    }

                    //look two blocks ahead for the switches
                    if (time + 2 > 23 * 60 + 59)
                        correctedTime = time + 2 - (23 * 60 + 59);
                    else
                        correctedTime = time + 2;
                    try
                    {
                        if (map[TrainDriving.allTrains[i].trainSchedule[correctedTime].positionX, TrainDriving.allTrains[i].trainSchedule[correctedTime].positionY].BlockInfo == 1 &&
                            TrainDriving.allTrains[i].trainSchedule[correctedTime].switched)
                        {
                            map[TrainDriving.allTrains[i].trainSchedule[correctedTime].positionX, TrainDriving.allTrains[i].trainSchedule[correctedTime].positionY].BlockInfo = 2;
                        }
                        else if (map[TrainDriving.allTrains[i].trainSchedule[correctedTime].positionX, TrainDriving.allTrains[i].trainSchedule[correctedTime].positionY].BlockInfo == 2 &&
                            TrainDriving.allTrains[i].trainSchedule[correctedTime].switched == false)
                        {
                            map[TrainDriving.allTrains[i].trainSchedule[correctedTime].positionX, TrainDriving.allTrains[i].trainSchedule[correctedTime].positionY].BlockInfo = 1;
                        }
                    }
                    catch (IndexOutOfRangeException) { }


                }
            }
        }

        static int currentBlockSize = 20;
        void playBox_MouseDown(object sender, MouseEventArgs e)
        {
            BlockClickWhilePlayingMap(map[e.X / currentBlockSize, e.Y / currentBlockSize]);
        }

        public static Train currentlyDrivenTrain;

        //shows option to drive your own train
        void BlockClickWhilePlayingMap(Block sender)
        {
            if (sender.BlockInfo != 0)
            {
                if (sender.BlockInfo != -1) //means it could be a switch 
                {
                    if (sender.BlockInfo == 1) //straight switch
                    {
                        for (int i = 0; i < TrainDriving.allTrains.Count; i++)
                        {
                            try
                            {
                                if ((TrainDriving.allTrains[i].occupiedRails[0].x == sender.Location.X / 20 && TrainDriving.allTrains[i].occupiedRails[0].y == sender.Location.Y / 20) ||
                                    (TrainDriving.allTrains[i].type == "bigTrain" && (TrainDriving.allTrains[i].occupiedRails[1].x == sender.Location.X / 20 && TrainDriving.allTrains[i].occupiedRails[1].y == sender.Location.Y / 20) ||
                                    (TrainDriving.allTrains[i].type == "bigTrain" && TrainDriving.allTrains[i].occupiedRails[2].x == sender.Location.X / 20 && TrainDriving.allTrains[i].occupiedRails[2].y == sender.Location.Y / 20)))
                                {
                                    TrainDriving.TrainCrash(TrainDriving.allTrains[i], new Coordinates() { x = sender.Location.X / 20, y = sender.Location.Y / 20 });
                                }
                            }
                            catch (ArgumentOutOfRangeException) { }
                        }
                        sender.BlockInfo = 2; //turns switch to turned
                    }
                    else if (sender.BlockInfo == 2)
                    {
                        for (int i = 0; i < TrainDriving.allTrains.Count; i++)
                        {
                            try
                            {
                                if ((TrainDriving.allTrains[i].occupiedRails[0].x == sender.Location.X / 20 && TrainDriving.allTrains[i].occupiedRails[0].y == sender.Location.Y / 20) ||
                                    (TrainDriving.allTrains[i].type == "bigTrain" && (TrainDriving.allTrains[i].occupiedRails[1].x == sender.Location.X / 20 && TrainDriving.allTrains[i].occupiedRails[1].y == sender.Location.Y / 20) ||
                                    (TrainDriving.allTrains[i].type == "bigTrain" && TrainDriving.allTrains[i].occupiedRails[2].x == sender.Location.X / 20 && TrainDriving.allTrains[i].occupiedRails[2].y == sender.Location.Y / 20)))
                                {
                                    TrainDriving.TrainCrash(TrainDriving.allTrains[i], new Coordinates() { x = sender.Location.X / 20, y = sender.Location.Y / 20 });
                                }
                            }
                            catch (ArgumentOutOfRangeException) { }
                        }
                        sender.BlockInfo = 1; //turns switch to straight
                    }
                    playBox.Invalidate();
                }

                else if (MainForm.playingMap)
                {
                    if (sender.Image == horizontal || sender.Image == vertical)
                    {
                        DialogResult result = MessageBox.Show("Postavit sem vláček?", "?", MessageBoxButtons.YesNo);
                        if (result == DialogResult.Yes)
                        {
                            Train t = null;
                            TrainDriving.StartATrain(sender.Location, ref t, true, null);
                            t.playerDriven = true;
                            currentlyDrivenTrain = t;
                        }
                    }
                }
            }
        }
        //used for zooming -> performance too slow for resising such count of pictureBoxes -> not implemented

        /*void MainForm_MouseWheel(object sender, MouseEventArgs e)
        {
            if (creatingMap)
            {
                if (e.Delta > 0) //Scrolled up
                    Zoom(true);
                else            //Scrolled down
                    Zoom(false);
            }
        }*/


        /*
        void Zoom(bool dir)
        {
            float size;
            if(dir && zoomState == 1) 
            {
                size = (float)1300 / (float)1900;
                
                zoomState--;
            }
            else if (!dir && zoomState == 0)
            {
                size = (float)1900 / (float)1300;
               
                zoomState++;
            }
            else if (dir && zoomState == 2)
            {
                size = (float)1900 / (float)2500;
               
                zoomState--;
            }
            else if (!dir && zoomState == 1)
            {
                size = (float)2500 / (float)1900;
                zoomState++;
            }
            else { size = (float)1; }
           
            SizeF aSf = new SizeF(size, size);
            for (int i = 0; i < this.Controls.Count; i++)
            {
                mainForm.Controls[i].Scale(aSf);
            }
            tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
        }*/


        void HandleMouseLeave(object sender, EventArgs e)
        {
            if (!Building.currentlyBuilding)
            {
                PictureBox p = (PictureBox)sender;
                p.BorderStyle = BorderStyle.None;
            }
        }

        //fancy things when hovers over block
        void HandleMouseEnter(object sender, EventArgs e)
        {
            this.Cursor = Cursors.Hand;

            if (!Building.currentlyBuilding && Building.currentFacility == null)
            {
                PictureBox p = (PictureBox)sender;

                //Solution with MouseLeave handler had poor performance
                for (int i = 0; i < mapWidth; i++)
                {
                    for (int j = 0; j < mapHeight; j++)
                    {
                        map[i, j].BorderStyle = BorderStyle.None;
                    }
                }

                p.BorderStyle = BorderStyle.Fixed3D;
                this.Cursor = Cursors.Hand;
            }
            else if (Building.currentlyBuilding)
            {
                Block b = (Block)sender;
                Building.HandleMouseEnterWhileBuilding(b);
            }
            else if (Building.currentFacility != null)
            {
                Block b = (Block)sender;
                Building.HandleMouseWhileFacilityBuilding(b);
            }
        }

        //handles buttons as M for menu, or Esc to go to main menu, or W and S for driving the train
        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape && Building.currentlyBuilding)
            {
                Building.StopBuilding();
            }
            else if (e.KeyCode == Keys.Escape && Building.currentFacility != null)
            {
                Building.currentFacility = null;
            }
            else if (e.KeyCode == Keys.Escape && creatingMap)
            {
                creatingMap = false;
                playBox.Hide();
                tableLayoutPanel1.Hide();
                for (int i = 0; i < Building.allFacilityPictureBoxes.Count; i++)
                {
                    Building.allFacilityPictureBoxes[i].Dispose();
                }
                Building.allFacilities.Clear();
                Building.allFacilityPictureBoxes.Clear();
                foreach (Block b in tableLayoutPanel1.Controls)
                {
                    b.Image = null;
                    b.BlockInfo = 0;
                }
            }
            else if (e.KeyCode == Keys.Escape && !creatingMap && !playingMap)
            {
                timeTabling = false;
                playBox.Hide();
                tableLayoutPanel1.Hide();
                for (int i = 0; i < Building.allFacilityPictureBoxes.Count; i++)
                {
                    Building.allFacilityPictureBoxes[i].Dispose();
                }
            }
            else if (e.KeyCode == Keys.Escape && playingMap)
            {
                playingMap = false;
                tableLayoutPanel1.Hide();
                playBox.Hide();
                for (int i = 0; i < TrainDriving.trainsOffSchedule.Count; i++)
                {
                    TrainDriving.trainsOffSchedule[i].offSchedule = false;
                }
                TrainDriving.trainsOffSchedule.Clear();
                for (int i = 0; i < Building.allFacilityPictureBoxes.Count; i++)
                {
                    Building.allFacilityPictureBoxes[i].Dispose();
                }
                mainStopwatch.Stop();
                mainStopwatch.Reset();
                timeLabel.Hide();

                for (int i = 0; i < TrainDriving.allTrains.Count; i++)
                {
                    TrainDriving.allTrains[i].timer.Stop();
                    TrainDriving.allTrains[i].currentPositionX = -1;
                    TrainDriving.allTrains[i].currentPositionY = -1;
                    TrainDriving.allTrains[i].orientations[0] = "none";
                    TrainDriving.allTrains[i].nextPositionX = 0;
                    TrainDriving.allTrains[i].nextPositionY = 0;

                }


                if (currentlyDrivenTrain != null)
                {
                    currentlyDrivenTrain.timer.Stop();
                    currentlyDrivenTrain.currentPositionX = -1;
                    currentlyDrivenTrain.currentPositionY = -1;
                    currentlyDrivenTrain.orientations[0] = "none";
                    currentlyDrivenTrain.nextPositionX = 0;
                    currentlyDrivenTrain.nextPositionY = 0;
                    currentlyDrivenTrain = null;
                }
            }
            else if (e.KeyCode == Keys.M)
            {
                Building.StopBuilding();
                Building.currentFacility = null;
                bool toSave = false;
                if (creatingMap)
                     toSave = true;
                Building.ShowLoadSaveMenu(toSave);
            }
            if (e.KeyCode == Keys.W && TrainDriving.currentlyDriving)
            {
                if(currentlyDrivenTrain.reversing) {
                    currentlyDrivenTrain.reversing = false;
                    currentlyDrivenTrain.velocity = 0;
                    currentlyDrivenTrain.timer.Interval = 2500;
                }
                else if (currentlyDrivenTrain.velocity == 0)
                    currentlyDrivenTrain.velocity = 1;
                else if (currentlyDrivenTrain.timer.Interval == 2500)
                    currentlyDrivenTrain.timer.Interval = 1700;
                else if (currentlyDrivenTrain.timer.Interval == 1700)
                    currentlyDrivenTrain.timer.Interval = 1000;
            }
            if (e.KeyCode == Keys.Space && TrainDriving.currentlyDriving)
            {
                currentlyDrivenTrain.velocity = 0;
                currentlyDrivenTrain.timer.Interval = 2500;
            }
            if (e.KeyCode == Keys.S && TrainDriving.currentlyDriving)
            {
                if(currentlyDrivenTrain.timer.Interval == 2500)
                    currentlyDrivenTrain.velocity = 0;
                else if (currentlyDrivenTrain.velocity == 0) {
                    currentlyDrivenTrain.velocity = 1;
                    currentlyDrivenTrain.timer.Interval = 3000;
                    currentlyDrivenTrain.reversing = true;
                    currentlyDrivenTrain.ReverseOrientationsAndComponents();
                }
                else if (currentlyDrivenTrain.timer.Interval == 1700)
                    currentlyDrivenTrain.timer.Interval = 2500;
                else if (currentlyDrivenTrain.timer.Interval == 1000)
                    currentlyDrivenTrain.timer.Interval = 1700;

                //how to implement two list reversing and orientation reverse
            }

        }

        //this serves for loading TimeTables and differs from map loading so it  has its own function
        public static void nahraj_click(object sender, EventArgs e)
        {
            input = new Form();
            ComboBox maps = new ComboBox();
            maps.Location = new Point(25, 10);
            maps.Width = 150;
            input.Width = 210;
            Button ok = new Button();
            ok.Location = new Point(55, 40);
            ok.Text = "Ok";
            ok.Click += ok_MouseClick;
            input.Controls.Add(ok);
            maps.DropDownStyle = ComboBoxStyle.DropDownList;

            //gets the pattern to Show only the timeTables that correspond to the selected map
            string pattern = mapSelected.Replace(AppDomain.CurrentDomain.BaseDirectory, "");
            List<string> files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, pattern + "*.xm")
                                 .ToList();
            foreach (var file in files)
            {
                maps.Items.Add(Path.GetFileName(file));
            }
            input.Controls.Add(maps);
            saveorLoadDR = new DialogResult();
            saveorLoadDR = DialogResult.None;
            input.ShowDialog();
            if (saveorLoadDR == DialogResult.OK && maps.SelectedItem != null)
            {
                
                

                string dir = @"C:\\temp";
                string serializationFile = Path.Combine(dir, files[maps.SelectedIndex]);
                input.Close();
                MainForm.mainForm.Cursor = Cursors.WaitCursor;
                AITrains.trainsOverview.Clear();

                //deserialize
                using (Stream stream = File.Open(serializationFile, FileMode.Open))
                {
                    XmlSerializer deserializer = new XmlSerializer(typeof(List<AITrains.TimeTableRecord>));
                    List<AITrains.TimeTableRecord> loadedRecords = new List<AITrains.TimeTableRecord>();
                    loadedRecords = (List<AITrains.TimeTableRecord>)deserializer.Deserialize(stream);
                    TrainDriving.allTrains.Clear();

                    foreach (AITrains.TimeTableRecord rec in loadedRecords)
                    {
                        AITrains.ttr = new AITrains.TimeTableRecord();
                        ComboBox sources = new ComboBox();
                        sources.Text = rec.sourceStation;
                        ComboBox destinations = new ComboBox();
                        destinations.Text = rec.destionationStation;
                        TextBox srcTime = new TextBox();
                        srcTime.Text = AITrains.ConvertTimeToString(rec.sourceTime);
                        List<string> presetTrain = new List<string>();
                        presetTrain.Add(rec.name);
                        if (rec.type == "smallTrain")
                            presetTrain.Add("Osobáček");
                        else
                            presetTrain.Add("Rychlík");
                        AITrains.destinationDialogOk(new object(), new EventArgs(), sources, destinations, srcTime, presetTrain);
                        AITrains.trainsOverview.Add(AITrains.ttr);
                    }
                }
            }
            MainForm.mainForm.Cursor = Cursors.Default;
            AITrains.overview.Close();
            AITrains.ShowTrainsOverview();
        }

        static DialogResult saveorLoadDR;

        //for saving timetables
        public static void uloz_click(object sender, EventArgs e)
        {
            string name = "";
            input = new Form();
            input.Width = 200;
            input.Height = 140;
            TextBox tb = new TextBox();
            tb.Location = new Point(25, 20);
            Label l = new Label();
            l.Width = 250;
            l.Text = "Zadejte název řádu";
            l.Location = new Point(15, 5);
            Button ok = new Button();
            ok.Text = "Ok";
            ok.Location = new Point(25, 45);
            ok.Click += ok_MouseClick;
            input.Controls.Add(tb); input.Controls.Add(l); input.Controls.Add(ok);
            saveorLoadDR = new DialogResult();

            while (name == "")
            {
                input.ShowDialog();
                if (saveorLoadDR == DialogResult.OK && l.Text != "")
                {
                    if (File.Exists(mapSelected + " řád " + tb.Text + ".xm"))
                    {
                        MessageBox.Show("Takové jméno už nějaký řád má");
                        continue;
                    }

                    name = mapSelected + " řád " + tb.Text + ".xm";
                    break;
                }
                MessageBox.Show("Příště zkuste napsat jméno prosím");
            }
            string dir = @"C:\\temp";
            string serializationFIle = Path.Combine(dir, name);

            //serialize
            using (Stream stream = File.Open(serializationFIle, FileMode.Create))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<AITrains.TimeTableRecord>));
                serializer.Serialize(stream, AITrains.trainsOverview);
            }
        }

        static void ok_MouseClick(object sender, EventArgs e)
        {
            input.Close();
            saveorLoadDR = DialogResult.OK;
        }
    }

    [XmlInclude(typeof(SerializableBlock))]
    [Serializable()]
    public class SerializableBlock
    {
        public int x, y;
        public bool[][] ways;
        public int BlockInfo;
        public string Image;
        public string facilityName;
    }


    //block which creates the map and you can build on it
    public class Block : PictureBox
    {
        public Block() { }
        public int x, y;

        //two dimensional indicating the ways which are possible for the train to go
        public bool[,] ways; //starting from top clockwise

        public int BlockInfo; // 0 nothing , -1 rail builded, 1 straightSwitch, 2 turnedSwitch, 3 bigstation, 4 smallstation, 5 depo

        public string facilityName;

    }

    public class SwitchToolTip : PictureBox
    {
        public float rotation;
    }
}
