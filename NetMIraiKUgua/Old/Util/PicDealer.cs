using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMDK.Util
{
    class PicDealer
    {
        public static void setGray(Bitmap bm)
        {
         //   Bitmap bm2 = (Bitmap)bm.Clone();
            Rectangle rect = new Rectangle(0, 0, bm.Width, bm.Height);
            BitmapData bmpdata = bm.LockBits(rect, ImageLockMode.ReadWrite, bm.PixelFormat);

            int row = bmpdata.Height;
            int col = bmpdata.Width;
            //byte[][] res = new byte[row][];

            try
            {
                unsafe
                {
                    byte* ptr = (byte*)(bmpdata.Scan0);
                    for (int i = 0; i < row; i++)
                    {
                        //res[i] = new byte[col];
                        for (int j = 0; j < col; j++)
                        {
                            ptr = (byte*)(bmpdata.Scan0) + bmpdata.Stride * i + 3 * j;
                            byte ptrgray = (byte)(0.299 * ptr[2] + 0.587 * ptr[1] + 0.114 * ptr[0]);
                            ptr[0] = ptrgray;
                            ptr[1] = ptrgray;
                            ptr[2] = ptrgray;
                            //res[i][j] = (byte)(0.299 * ptr[2] + 0.587 * ptr[1] + 0.114 * ptr[0]);
                        }
                    }
                }

            }
            catch
            {

            }
            bm.UnlockBits(bmpdata);
            //return res;
        }
    }
}
