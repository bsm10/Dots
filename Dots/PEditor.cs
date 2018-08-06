using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace DotsGame
{
    public class Pattern
    {
        public int PatternNumber { get; set; }
        //public int minX { get; set; }
        //public int maxX { get; set; }
        //public int minY { get; set; }
        //public int maxY { get; set; }

        public List<DotInPattern> DotsPattern { get; set; } = new List<DotInPattern>();

        public DotInPattern dXdY_ResultDot = new DotInPattern();
        public Dot FirstDot { get; set; }//Точка отсчета
        public Dot ResultDot
        {
            get
            {
                return new Dot(FirstDot.X + dXdY_ResultDot.dX, FirstDot.Y + dXdY_ResultDot.dY, FirstDot.Own);
            }
        }

        public override string ToString()
        {
            return "Pattern " + PatternNumber.ToString();
        }

    }

    public class DotInPattern
    {
        public int dX { get; set; }
        public int dY { get; set; }
        public string Owner { get; set; }
        public override string ToString()
        {
            return "dX = " + dX.ToString() + "; dY = " + dY.ToString();
        }

    }

    //public static class PEminXY
    //{
    //    public static int minX = 0;
    //    public static int maxX = 0;
    //    public static int minY = 0;
    //    public static int maxY = 0;
    //}
    public partial class GameEngine
    {
        private static bool _EditMode;
        public static bool EditMode
        {
            get
            {
                return _EditMode;
            }

            set
            {
                _EditMode = value;
                if (_EditMode) lstDotsInPattern = new List<Dot>();
            }
        }
        //Редактор паттернов
        public static List<Pattern> Patterns = new List<Pattern>();
        public static List<Dot> lstDotsInPattern;

        public static string Path_PatternData
        {
            get
            {
                return Application.StartupPath + @"\Resources\patterns.dts"; //@"d:\Proj\PointsCSharp\PointsCSharp\patterns.dts"; 
            }
        }
        public static void WritePatternToFile(List<string> lines)
        {
            // Append new text to an existing file.
            // The using statement automatically flushes AND CLOSES the stream and calls 
            // IDisposable.Dispose on the stream object.
            using (StreamWriter file =
                new StreamWriter(Path_PatternData, true))
            {

                foreach (string s in lines) file.WriteLine(s);
            }
        }

        private static int GetNumberPattern()
        {
            int number = 0;
            string line;
            // Read the file and display it line by line.
            StreamReader file = new StreamReader(Path_PatternData);
            while ((line = file.ReadLine()) != null)
            {
                if (line.Trim() == "Begin")
                {
                    line = file.ReadLine();
                    number = Convert.ToInt32(line);
                }
            }
            file.Close();
            number++;
            return number;
        }
        public static void MakePattern()//сохраняет паттерн в текстовое поле
        {
            if (lstDotsInPattern.Count == 0) return;
            List<Dot> lstPat = lstDotsInPattern;
            //rotate dots in pattern
            foreach (List<Dot> listDots in ListRotatePatterns(lstPat)) AddPatternDots(listDots);

            lstPat.Clear();
            GameDots.UnmarkAllDots();
            LoadPattern();
        }
        //----------------------------------------------------------
        private static void AddPatternDots(List<Dot> ListPatternDots)
        {
            List<string> lines = new List<string>();
            string s = string.Empty;

            int dx, dy;
            Dot firstDot = ListPatternDots.Find(d => d.PatternsFirstDot);
            Dot moveDot = ListPatternDots.Find(dt => dt.PatternsMoveDot);
            //------------------------------------------------
            lines.Add("Begin");
            lines.Add(GetNumberPattern().ToString());
            lines.Add("Dots");
            for (int i = 0; i < ListPatternDots.Count; i++)
            {
                string own = "";
                //if (firstDot.Own == ListPatternDots[i].Own) own = "owner";
                //if (firstDot.Own != ListPatternDots[i].Own) own = "enemy";
                if (ListPatternDots[i].Own == 1) own = "enemy";
                if (ListPatternDots[i].Own == 2) own = "owner";
                if (ListPatternDots[i].Own == 0 & ListPatternDots[i].PatternsAnyDot == false) own = "0";
                if (ListPatternDots[i].PatternsAnyDot) own = "!= enemy";
                dx = ListPatternDots[i].X - firstDot.X;
                dy = ListPatternDots[i].Y - firstDot.Y;
                s = dx.ToString() + ", " + dy.ToString() + ", " + own;
                lines.Add(s);
            }
            lines.Add("Result");
            lines.Add((moveDot.X - firstDot.X).ToString() + ", " +
                      (moveDot.Y - firstDot.Y).ToString());
            lines.Add("End");

            WritePatternToFile(lines);
            s = string.Empty;
            foreach (string st in lines) s = s + st + " \r\n";
            //DebugWindow.txtboxPattern.Text = s;

        }//private void AddPatternDots(List<Dot> ListPatternDots)

    }//---public partial class GameEngine--

}//--namespace DotsGame---
