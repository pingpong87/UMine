using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Data;

namespace ProbabilisticMine
{
    class PointToPicture
    {
        public static void DrawPicture(Panel pan, DataTable dt)
        {
            cleanPanel(pan);
            DrawXY(pan);
            DrawXLine(pan, 2000, 10);
            DrawYLine(pan, 2000, 10);
            DrawTips(pan, 2000, 2000,6);
            SolidBrush myBrush;
            if(dt != null)
            {
                foreach (DataRow dr in dt.Rows)
                {

                    myBrush = new SolidBrush(Util.pointColor(Convert.ToInt32(dr["fea"].ToString())));
                    DrawPoint(pan, 2000, 2000, Convert.ToDouble(dr["x"].ToString()), Convert.ToDouble(dr["y"].ToString()), myBrush);
                }
            }
        }

        public static void DrawImage(Panel pan, float[] times)
        {
            Graphics g = pan.CreateGraphics();
            float move = 50f; //整体内缩move像素  

            cleanPanel(pan);
            DrawXY(pan);
            g.DrawString("time/s", new Font("宋体 ", 10f), Brushes.Black, new PointF(move / 3, move / 2f));
           

            string[] algs= { "ExpAlg", "ProAlg", "ApprAlg"};
            DrawXAlg(pan, 2000, algs);
            DrawRect(pan, 2000, times);
            //g.DrawString("ExpAlg", new Font("宋体 ", 10f), Brushes.Black, new PointF(move - 15f, pan.Height - move / 1.5f));
            //g.DrawString("ProAlg", new Font("宋体 ", 10f), Brushes.Black, new PointF(pan.Width - move - 15f, pan.Height - move / 1.5f));
            //g.DrawString("ApprAlg", new Font("宋体 ", 10f), Brushes.Black, new PointF(pan.Width - move - 15f, pan.Height - move / 1.5f));


        }

        public static void DrawXY(Panel pan)
        {
            Graphics g = pan.CreateGraphics();
            //整体内缩move像素  
            float move = 50f; //距离panel像素
            float newX = pan.Width - move;
            float newY = pan.Height - move;

            //绘制X轴,  
            PointF px1 = new PointF(move, newY);
            PointF px2 = new PointF(newX, newY);
            g.DrawLine(new Pen(Brushes.Black, 2), px1, px2);
            //绘制Y轴  
            PointF py1 = new PointF(move, move);
            PointF py2 = new PointF(move, newY);

            g.DrawLine(new Pen(Brushes.Black, 2), py1, py2);
            g.Save();
        }


        public static void DrawPoint(Panel pan,float maxX,float maxY, double x, double y,SolidBrush myBrush)
        {
            float move = 50f;
            float LenX = pan.Width - 2 * move;
            float LenY = pan.Height - 2 * move;
            //该点对应panel的位置
            x = x*LenX/maxX + move;
            y = (LenY - y*LenY/maxY) + move;

            //SolidBrush myBrush = new SolidBrush(Color.Red);
            Graphics g = pan.CreateGraphics();
            g.FillRectangle(myBrush, new Rectangle((int)x, (int)y, 5, 5));
            g.Save();
        }

        public static void DrawTips(Panel pan,float maxX,float maxY,int colorNum)
        {
            float move = 50f;
            float LenX = pan.Width - 2 * move;
            float LenY = pan.Height - 2 * move;

            Graphics g = pan.CreateGraphics();
            for (int i=0; i< colorNum; i++)
            {
                SolidBrush myBrush = new SolidBrush(Util.pointColor(i));
                g.FillRectangle(myBrush, new Rectangle((int)(LenX+move), (int)(move+(i*10)+2f), 5, 5));
                string tips = i.ToString();
                if (i == 5)
                {
                    tips = "others";
                }
                g.DrawString(tips, new Font("宋体 ", 6f), Brushes.Black, new PointF((LenX + move+8f), (move + i * 10)));
            }
            g.Save();
        }

        public static void cleanPanel(Panel pan)
        {
            Graphics g = pan.CreateGraphics();
            g.Clear(Color.White);
        }
        

        /// <summary>  
        /// 画出Y轴上的分值线，从零开始   
        #region   画出Y轴上的分值线，从零开始  
        public static void DrawYLine(Panel pan, float maxY, int len)
        {
            float move = 50f;
            float LenX = pan.Width - 2 * move;
            float LenY = pan.Height - 2 * move;
            Graphics g = pan.CreateGraphics();
            for (int i = 0; i <= len; i++)    //len等份Y轴  
            {
                PointF px1 = new PointF(move, LenY * i / len + move);
                PointF px2 = new PointF(move + 4, LenY * i / len + move);
                string sx = (maxY - maxY * i / len).ToString();
                g.DrawLine(new Pen(Brushes.Black, 2), px1, px2);
                StringFormat drawFormat = new StringFormat();
                drawFormat.Alignment = StringAlignment.Far;
                drawFormat.LineAlignment = StringAlignment.Center;
                g.DrawString(sx, new Font("宋体", 8f), Brushes.Black, new PointF(move / 1.2f, LenY * i / len + move * 1.1f), drawFormat);
            }
            Pen pen = new Pen(Color.Black, 1);
            g.DrawString("Y", new Font("宋体 ", 10f), Brushes.Black, new PointF(move / 3, move / 2f));
            g.Save();
        }
        #endregion

        /// <summary>  
        /// 画出X轴上的分值线，从零开始  
        #region   画出X轴上的分值线，从零开始  
        public static void DrawXLine(Panel pan, float maxX, int len)
        {
            float move = 50f;
            float LenX = pan.Width - 2 * move;
            float LenY = pan.Height - 2 * move;
            Graphics g = pan.CreateGraphics();
            for (int i = 1; i <= len; i++)
            {
                PointF py1 = new PointF(LenX * i / len + move, pan.Height - move - 4);
                PointF py2 = new PointF(LenX * i / len + move, pan.Height - move);
                string sy = (maxX * i / len).ToString();
                g.DrawLine(new Pen(Brushes.Black, 2), py1, py2);
                g.DrawString(sy, new Font("宋体", 8f), Brushes.Black, new PointF(LenX * i / len + move-10f, pan.Height - move / 1.1f));
            }
            Pen pen = new Pen(Color.Black, 1);
            g.DrawString("X", new Font("宋体 ", 10f), Brushes.Black, new PointF(pan.Width - move / 1.5f, pan.Height - move / 1.5f));
            g.Save();
        }
        #endregion

        public static void DrawXAlg(Panel pan, float maxX, String[] algs)
        {
            float move = 50f;
            float LenX = pan.Width - 2 * move;
            float LenY = pan.Height - 2 * move;
            Graphics g = pan.CreateGraphics();
             
            for (int i = 0; i < algs.Length; i++)
            {

                g.DrawString(algs[i], new Font("宋体", 10f), Brushes.Black, new PointF(LenX * i / algs.Length + move+20f, pan.Height - move / 1.1f));
            }
            Pen pen = new Pen(Color.Black, 1);
            g.DrawString("algorithms", new Font("宋体 ", 10f), Brushes.Black, new PointF(pan.Width - move - 15f, pan.Height - move / 1.5f));
            g.Save();
        }

        public static void DrawRect(Panel pan, float maxX,float[] times)
        {
            float move = 50f;
            float LenX = pan.Width - 2 * move;
            float LenY = pan.Height - 2 * move;
            Graphics g = pan.CreateGraphics();

            Font font2 = new System.Drawing.Font("Arial", 10, FontStyle.Bold);
            SolidBrush mybrush = new SolidBrush(Color.Green);

            if(times != null)
            {
                for (int i = 0; i < times.Length; i++)
                {
                    g.FillRectangle(mybrush, LenX * i / times.Length + move + 25f, pan.Height - move - times[i], 20, times[i]);
                    g.DrawString(times[i].ToString(), font2, Brushes.Red, LenX * i / times.Length + move + 25f, pan.Height - move - times[i] - 15);
                }
            }
            
            
        }

    }
}
