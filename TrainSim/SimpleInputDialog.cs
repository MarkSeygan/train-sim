using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.IO;

namespace TrainSim
{
    public partial class MainForm : Form
    {
        static Form input;
        static string theInput;

        public static string SimpleOptionsDialog(string question, string option1, string option2)
        {
            theInput = "";
            input = new Form();
            input.Width = question.Length * 6;
            if (input.Width < 240)
                input.Width = 240;
            Label l = new Label();
            Button b1 = new Button();
            Button b2 = new Button();
            l.Text = question;
            b1.Text = option1;
            b2.Text = option2;
            b1.Width = b1.Text.Length * 10;
            b2.Width = b2.Text.Length * 10;
            l.Width = question.Length * 10;
            l.Location = new Point(10, 10);
            b1.Location = new Point(10, 35);
            b2.Location = new Point(95, 35);
            b1.Click += b1_Click;
            b2.Click += b2_Click;
            input.Controls.Add(l); input.Controls.Add(b1); input.Controls.Add(b2);
            input.Height = 130;
            input.StartPosition = FormStartPosition.Manual;
            input.Location = new Point(MainForm.mainForm.Width / 2 - input.Width / 2, MainForm.mainForm.Height / 2 - input.Height / 2);
            input.ShowDialog();
            return theInput;
        }

        static void b2_Click(object sender, EventArgs e)
        {
            Button s = (Button)sender;
            input.Close();
            theInput = s.Text;
        }

        static void b1_Click(object sender, EventArgs e)
        {
            Button s = (Button)sender;
            input.Close();
            theInput = s.Text;
        }
        public static string SimpleInputDialog(string question)
        {
            theInput = "";
            input = new Form();
            Label l = new Label();
            TextBox tb = new TextBox();
            Button ok = new Button();
            ok.Text = "Ok";
            l.Location = new Point(10, 10);
            l.AutoSize = true;
            l.Text = question;
            tb.Location = new Point(10, 35);
            ok.Location = new Point(10, 60);
            ok.Click += (thisSender, eventArgs) => simpleInputok_Click(thisSender,eventArgs,ref theInput, tb);
            input.Controls.Add(l); input.Controls.Add(tb); input.Controls.Add(ok);
            input.Height = 150;
            input.Width = 190;
            input.StartPosition = FormStartPosition.Manual;
            input.Location = new Point(MainForm.mainForm.Width / 2 - input.Width / 2, MainForm.mainForm.Height / 2 - input.Height / 2);
            input.ShowDialog();
            if (theInput != "")
            {
                input.Close();
                return theInput;
                
            }
            return "";
        }

        static void simpleInputok_Click(object sender, EventArgs e, ref string theInput, TextBox tb)
        {
            if (tb.Text != "")
            {
                theInput = tb.Text;
                input.Close();
            }
        }
    }
}