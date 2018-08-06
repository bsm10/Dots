using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace DotsGame
{
    //public static class GameMessages
    //{
    //    public static string Message {get;set;}

    //}
    public static partial class GameEngine
    {

        //-------------------------------------------------
        public static int iScaleCoef = 1;//-коэффициент масштаба

        public static float startX = -0.5f, startY = -0.5f;
        //===========================================================================================================
        public static GameDots GameDots { get; set; }

        public static Dot DOT(int x, int y)
        {
            return GameDots[x,y];
        }
        public static Dot DOT(Dot d)
        {
            return GameDots[d.X, d.Y];
        }
        public static string Status { get; set; } = string.Empty;

        public static bool Autoplay
        {
            get; set;
        }


        //=========== цвета, шрифты ===================================================
        public static Color colorGamer1 = Color.FromArgb(200, 100, 50),
                           colorGamer2 = Color.FromArgb(80, 100, 200),
                           colorCursor = Color.FromArgb(128, 64, 0);
        private static float PointWidth = 0.18f;
        public static Pen boardPen = new Pen(Color.FromArgb(150,200,200), 0.08f);//(Color.DarkSlateBlue, 0.05f);
        private static SolidBrush drawBrush = new SolidBrush(Color.MediumPurple);
        public static Font drawFont = new Font("Arial", 0.22f);
        public static bool Redraw {get; set;}
        //===============================================================================
        public static Graphics _gr;
        public static Graphics GraphicsGame => _gr;
        public static Dot MousePos;

        public static Dot LastMove
        {
            get
            {
                return GameDots.LastMove;
            }
        }

        public static PictureBox CanvasGame { get; set; }
        //public static PictureBox PbxBoard { get; set; }

        static GameEngine()
        {
            NewGame(10, 15);
            LoadPattern();
        }
        //  ************************************************
        public static bool GameOver()
        {
            return GameDots.IsGameOver;
        }

        public static void DotStatistic(Dot dotst)
        {
#if DEBUG
            // DebugWindow.txtDotStatus.Text = "Blocked: " + GameDots[x, y].Blocked + "\r\n" +
            //"BlokingDots.Count: " + GameDots[x, y].BlokingDots.Count + "\r\n" +
            //"NeiborDots.Count: " + GameDots[x, y].NeiborDots.Count + "\r\n" +
            //"Rating: " + GameDots[x, y].Rating + "\r\n" +
            //"IndexDot: " + GameDots[x, y].IndexDot + "\r\n" +
            //"IndexRelation: " + GameDots[x, y].IndexRelation + "\r\n" +
            //"Own: " + GameDots[x, y].Own + "\r\n" +
            //"X: " + GameDots[x, y].x + "; Y: " + GameDots[x, y].y;


           // DebugWindow.txtDotStatus.Text = dotst.DotStatistic;
#endif

        }

        public static void NewGame(int boardWidth, int boardHeigth)
        {
            GameDots = new GameDots(boardWidth,boardHeigth); 
            
            lstDotsInPattern = new List<Dot>();
            startX = -0.5f;
            startY = -0.5f;
            Redraw=true;
        }
        //------------------------------------------------------------------------------------

        public static void ResizeBoard(int newSizeWidth, int newSizeHeight)//изменение размера доски
        {
            if (newSizeWidth < 5) newSizeWidth = 5;
            else if (newSizeWidth > 40) newSizeWidth = 40;
            if (newSizeHeight < 5) newSizeHeight = 5;
            else if (newSizeHeight > 40) newSizeHeight = 40;

            GameDots.BoardHeight = newSizeHeight;
            GameDots.BoardWidth = newSizeWidth;
            NewGame(newSizeWidth,newSizeHeight);
            CanvasGame.Invalidate();
        }


        private static List<List<Dot>> ListRotatePatterns(List<Dot> listPat)
        {
            List<List<Dot>> lstlstPat = new List<List<Dot>>();

            return lstlstPat;
        }

        public static void LoadPattern()
        {
            int counter_line = 0;
            try
            {
                string line;
                // Read the file and display it line by line.
                StreamReader file = new StreamReader(Path_PatternData);
                Pattern ptrn=new Pattern();
                Patterns.Clear();
                while ((line = file.ReadLine()) != null)
                {
                    counter_line++;
                    switch (line.Trim())
                    {
                        case "Begin":
                            ptrn = new Pattern();
                            //number pattern
                            line = file.ReadLine();
                            counter_line++;
                            int x;
                            if (int.TryParse(line, out x)) ptrn.PatternNumber = x; //Convert.ToInt32(line);
                            break;
                        case "Dots": //точки паттерна
                            while ((line = file.ReadLine().Replace(" ", string.Empty)) != "Result")
                            {
                                counter_line++;
                                string[] ss = line.Split(new char[] { ',' });
                                DotInPattern dtp = new DotInPattern();
                                dtp.dX = Convert.ToInt32(ss[0]);
                                dtp.dY = Convert.ToInt32(ss[1]);
                                dtp.Owner = ss[2];
                                ptrn.DotsPattern.Add(dtp);
                            }
                            counter_line++;
                            line = file.ReadLine().Replace(" ", string.Empty);
                            counter_line++;
                            string[] sss = line.Split(new char[] { ',' });
                            ptrn.dXdY_ResultDot.dX = Convert.ToInt32(sss[0]);
                            ptrn.dXdY_ResultDot.dY = Convert.ToInt32(sss[1]);
                            break;
                        case "End":
                           Patterns.Add(ptrn);
                           break;
                        default:
                           break;

                    }
            }
            file.Close();
            }
            catch (Exception e)
            {
                MessageBox.Show("LoadPattern \r\n" + e.Message);
            }

        }

        #region SAVE_LOAD Game
        public static string path_savegame = Application.CommonAppDataPath + @"\dots.dts";
        public static void SaveGame()
        {
            try
            {
                // создаем объект BinaryWriter
                using (BinaryWriter writer = new BinaryWriter(File.Open(path_savegame, FileMode.Create)))
                {

        		for (int i = 0; i < GameDots.ListMoves.Count; i++)
           			{
                        writer.Write((byte)GameDots.ListMoves[i].X);
                        writer.Write((byte)GameDots.ListMoves[i].Y);
                        writer.Write((byte)GameDots.ListMoves[i].Own);
                	}
            	}
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        public static void LoadGame()
        {
            GameDots.Clear();
            Dot d=null;
            try
            {
                // создаем объект BinaryReader
                BinaryReader reader = new BinaryReader(File.Open(path_savegame, FileMode.Open));
                // пока не достигнут конец файла считываем каждое значение из файла
                while (reader.PeekChar() > -1)
                {
                    d = new Dot((int)reader.ReadByte(), (int)reader.ReadByte(), (int)reader.ReadByte());
                    //GameDots.MakeMove(d, addForDraw: true);
                    GameDots.MovePlayer(d);
                }
                reader.Close();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
            
        }
        #endregion
        #region RENDER
        public static Dot TranslateCoordinates(MouseEventArgs MousePos)
        {
            Matrix transform = _transform.Clone();
            transform.Invert();
            Point p = new Point(MousePos.X, MousePos.Y);
            Point[] points = new[] { p };
            transform.TransformPoints(points);
            return (Dot)points[0];
        }
        public static void DrawGame(Graphics gr)//отрисовка хода игры
        {
            _gr = gr;
            gr.SmoothingMode = SmoothingMode.AntiAlias;
            //Устанавливаем масштаб
            SetScale(gr, CanvasGame.ClientSize.Width, CanvasGame.ClientSize.Height,
                startX, startX + GameDots.BoardWidth, startY, GameDots.BoardHeight + startY);
            //Рисуем доску
            DrawBoard(gr);
            //Рисуем точки
            DrawPoints(gr);
            //Отрисовка курсора
            if (MousePos is null == false)
            {
                gr.FillEllipse(new SolidBrush(Color.FromArgb(30, colorCursor)), MousePos.X - PointWidth, MousePos.Y - PointWidth, PointWidth * 2, PointWidth * 2);
                gr.FillEllipse(new SolidBrush(Color.FromArgb(130, Color.WhiteSmoke)), MousePos.X - PointWidth / 2, MousePos.Y - PointWidth / 2, PointWidth, PointWidth);
                gr.DrawEllipse(new Pen(Color.FromArgb(50, colorCursor), 0.05f), MousePos.X - PointWidth, MousePos.Y - PointWidth, PointWidth * 2, PointWidth * 2);

            }
            //Отрисовка замкнутого региона игрока1
            DrawLinks(gr);

        }
        public static void DrawBoard(Graphics gr)//рисуем доску из клеток
        {
            Pen pen = new Pen(new SolidBrush(Color.MediumSeaGreen), 0.15f);// 0
            for (float i = 0; i <= GameDots.BoardWidth; i++)
            {
                SolidBrush drB = i == 0 ? new SolidBrush(Color.MediumSeaGreen) : drawBrush;
#if DEBUG
                //gr.DrawString("y" + (i + startY + 0.5f).ToString(), drawFont, drB, startX, i + startY + 0.5f - 0.2f);
                gr.DrawString("x" + (i + startX + 0.5f).ToString(), drawFont, drB, i + startX + 0.5f - 0.2f, startY);
#endif
                gr.DrawLine(boardPen, i + startX + 0.5f, startY + 0.5f, i + startX + 0.5f, GameDots.BoardHeight + startY - 0.5f);
                //gr.DrawLine(boardPen, startX + 0.5f, i + startY + 0.5f, gameDots.BoardWidth + startX - 0.5f, i + startY + 0.5f);
            }
            for (float i = 0; i <= GameDots.BoardHeight; i++)
            {
                SolidBrush drB = i == 0 ? new SolidBrush(Color.MediumSeaGreen) : drawBrush;
#if DEBUG
                gr.DrawString("y" + (i + startY + 0.5f).ToString(), drawFont, drB, startX, i + startY + 0.5f - 0.2f);
                //gr.DrawString("x" + (i + startX + 0.5f).ToString(), drawFont, drB, i + startX + 0.5f - 0.2f, startY);
#endif
                //gr.DrawLine(boardPen, i + startX + 0.5f, startY + 0.5f, i + startX + 0.5f, gameDots.BoardHeight + startY - 0.5f);
                gr.DrawLine(boardPen, startX + 0.5f, i + startY + 0.5f, GameDots.BoardWidth + startX - 0.5f, i + startY + 0.5f);
            }

        }
        public static void DrawLinks(Graphics gr)//отрисовка связей
        {
            List<Links> lnks = GameDots.ListLinks;
            if (lnks != null)
            {
                Pen PenGamer;
                for (int i = 0; i < lnks.Count; i++)
                {
                    if (lnks[i].Blocked)
                    {
                        PenGamer = lnks[i].Dot1.Own == 1 ? new Pen(Color.FromArgb(130, colorGamer1), 0.1f) :
                                                           new Pen(Color.FromArgb(130, colorGamer2), 0.1f);

                        gr.DrawLine(PenGamer, lnks[i].Dot1.X, lnks[i].Dot1.Y, lnks[i].Dot2.X, lnks[i].Dot2.Y);
                    }
                    else
                    {
                        PenGamer = lnks[i].Dot1.Own == 1 ? new Pen(colorGamer1, 0.1f) : new Pen(colorGamer2, 0.1f);
                        gr.DrawLine(PenGamer, lnks[i].Dot1.X, lnks[i].Dot1.Y, lnks[i].Dot2.X, lnks[i].Dot2.Y);
                    }
                }
            }
        }
        public static void DrawPoints(Graphics gr)//рисуем поставленные точки
        {
            IList<Dot> lstDotsForDraw = EditMode ? GameDots.Dots : GameDots.ListMoves;
            //отрисовываем поставленные точки
            foreach (Dot p in lstDotsForDraw)
            {
                switch (p.Own)
                {
                    case 1:
                        SetColorAndDrawDots(gr, p, colorGamer1);
                        break;
                    case 2:
                        SetColorAndDrawDots(gr, p, colorGamer2);
                        break;
                    case 0:
                        if (EditMode) SetColorAndDrawDots(gr, p, Color.FromArgb(150, Color.WhiteSmoke));
                        break;
                }
            }
        }
        private static void SetColorAndDrawDots(Graphics gr, Dot p, Color colorGamer) //Вспомогательная функция для DrawPoints. Выбор цвета точки в зависимости от ее состояния и рисование элипса
        {
            Dot last_move =  GameDots.LastMove;
            Color c;
            if (last_move != null && p.X == last_move.X & p.Y == last_move.Y)//точка последнего хода должна для удовства выделяться
            {
                gr.FillEllipse(new SolidBrush(Color.FromArgb(140, colorGamer)), p.X - PointWidth, p.Y - PointWidth, PointWidth * 2, PointWidth * 2);
                gr.DrawEllipse(new Pen(Color.FromArgb(140, Color.WhiteSmoke), 0.08f), p.X - PointWidth / 2, p.Y - PointWidth / 2, PointWidth, PointWidth);
                gr.DrawEllipse(new Pen(colorGamer, 0.08f), p.X - PointWidth, p.Y - PointWidth, PointWidth + PointWidth, PointWidth + PointWidth);
            }
            else
            {
                c = p.Blocked ? Color.FromArgb(130, colorGamer) : Color.FromArgb(255, colorGamer);
                gr.FillEllipse(new SolidBrush(c), p.X - PointWidth, p.Y - PointWidth, PointWidth * 2, PointWidth * 2);
            }
            if (p.PatternsEmptyDot)
            {
                gr.DrawEllipse(new Pen(Color.DarkOliveGreen, 0.05f), p.X - PointWidth * 1.3f, p.Y - PointWidth * 1.3f, PointWidth * 1.3f * 2, PointWidth * 1.3f * 2);
            }
            if (p.PatternsMoveDot)
            {
                gr.DrawEllipse(new Pen(Color.LimeGreen, 0.08f), p.X - PointWidth, p.Y - PointWidth, PointWidth * 2, PointWidth * 2);
                gr.DrawEllipse(new Pen(Color.DarkOliveGreen, 0.05f), p.X - PointWidth * 1.3f, p.Y - PointWidth * 1.3f, PointWidth * 1.3f * 2, PointWidth * 1.3f * 2);
            }
            if (p.PatternsFirstDot)
            {
                gr.DrawEllipse(new Pen(Color.DarkSeaGreen, 0.08f), p.X - PointWidth, p.Y - PointWidth, PointWidth * 2, PointWidth * 2);
                gr.DrawEllipse(new Pen(Color.DarkOliveGreen, 0.05f), p.X - PointWidth * 1.3f, p.Y - PointWidth * 1.3f, PointWidth * 1.3f * 2, PointWidth * 1.3f * 2);
            }
            if (p.PatternsAnyDot)
            {
                gr.FillEllipse(new SolidBrush(Color.Yellow), p.X - PointWidth, p.Y - PointWidth, PointWidth * 2, PointWidth * 2);
                gr.DrawEllipse(new Pen(Color.Orange, 0.08f), p.X - PointWidth, p.Y - PointWidth, PointWidth * 2, PointWidth * 2);
                gr.DrawEllipse(new Pen(Color.DarkOliveGreen, 0.05f), p.X - PointWidth * 1.3f, p.Y - PointWidth * 1.3f, PointWidth * 1.3f * 2, PointWidth * 1.3f * 2);
            }

        }
        static Matrix _transform = new Matrix();//матрица для преобразования координат точек в заданном масштабе
        private static void SetScale(Graphics gr, int gr_width, int gr_height, float left_x, float right_x, float top_y, float bottom_y)
        {
            //функция масштабирования, устанавливает массштаб
            gr.ResetTransform();
            gr.ScaleTransform(gr_width / (right_x - left_x), gr_height / (bottom_y - top_y));
            gr.TranslateTransform(-left_x, -top_y);
            var transform = new Matrix();
            _transform = gr.Transform;
        }

        #endregion
        public static void UndoDot(Dot dot_move)
        {
            GameDots.UndoMove(dot_move);
        }
        //=========================================================================
    }
}
