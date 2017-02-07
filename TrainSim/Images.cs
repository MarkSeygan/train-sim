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
        public static Bitmap horizontal = Properties.Resources.horizontalRail;
        public static Bitmap vertical = Properties.Resources.verticalRail;
        public static Bitmap horizontalOver = Properties.Resources.horizontalOver;
        public static Bitmap verticalOver = Properties.Resources.verticalOver;

        public static Bitmap bigStation = Properties.Resources.bigStation;
        public static Bitmap bigStationVert = Properties.Resources.BigStationvert;
        public static Bitmap depo = Properties.Resources.depo;
        public static Bitmap depoVert = Properties.Resources.depovert;
        public static Bitmap smallStation = Properties.Resources.smallStation;
        public static Bitmap smallStationVert = Properties.Resources.smallStationvert;

        public static Bitmap rbottomright = Properties.Resources.rbottomright;
        public static Bitmap rleftbottom = Properties.Resources.rleftbottom;
        public static Bitmap rtopleft = Properties.Resources.rtopleft;
        public static Bitmap rrighttop = Properties.Resources.rrighttop;

        public static Bitmap vbottomleft = Properties.Resources.vbottomleft;
        public static Bitmap vbottomright = Properties.Resources.vbottomright;
        public static Bitmap vleftbottom = Properties.Resources.vleftbottom;
        public static Bitmap vlefttop = Properties.Resources.vlefttop;
        public static Bitmap vrightbottom = Properties.Resources.vrightbottom;
        public static Bitmap vrighttop = Properties.Resources.vrighttop;
        public static Bitmap vtopleft = Properties.Resources.vtopleft;
        public static Bitmap vtopright = Properties.Resources.vtopright;

        public static string ImageToBase64(Image image)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                System.Drawing.Imaging.ImageFormat format = System.Drawing.Imaging.ImageFormat.Png; //here it can be only used for png now
                // Convert Image to byte[]
                image.Save(ms, format);
                byte[] imageBytes = ms.ToArray();

                // Convert byte[] to Base64 String
                string base64String = Convert.ToBase64String(imageBytes);
                return base64String;
            }
        }

        public static Image FindOutTheImage(string imageFingerPrint)
        {
            if (imageFingerPrint == ImageToBase64(horizontal))
            {
                return horizontal;
            }
            if (imageFingerPrint == ImageToBase64(vertical))
            {
                return vertical;
            }
            if (imageFingerPrint == ImageToBase64(horizontalOver))
            {
                return horizontalOver;
            }
            if (imageFingerPrint == ImageToBase64(verticalOver))
            {
                return verticalOver;
            }
            if (imageFingerPrint == ImageToBase64(rbottomright))
            {
                return rbottomright;
            }
            if (imageFingerPrint == ImageToBase64(rleftbottom))
            {
                return rleftbottom;
            }
            if (imageFingerPrint == ImageToBase64(rtopleft))
            {
                return rtopleft;
            }
            if (imageFingerPrint == ImageToBase64(rrighttop))
            {
                return rrighttop;
            }
            if (imageFingerPrint == ImageToBase64(vbottomleft))
            {
                return vbottomleft;
            }
            if (imageFingerPrint == ImageToBase64(vbottomright))
            {
                return vbottomright;
            }
            if (imageFingerPrint == ImageToBase64(vleftbottom))
            {
                return vleftbottom;
            }
            if (imageFingerPrint == ImageToBase64(vlefttop))
            {
                return vlefttop;
            }
            if (imageFingerPrint == ImageToBase64(vrightbottom))
            {
                return vrightbottom;
            }
            if (imageFingerPrint == ImageToBase64(vrighttop))
            {
                return vrighttop;
            }
            if (imageFingerPrint == ImageToBase64(vtopleft))
            {
                return vtopleft;
            }
            if (imageFingerPrint == ImageToBase64(vtopright))
            {
                return vtopright;
            }
            else
                return null;
        }
    }
}
