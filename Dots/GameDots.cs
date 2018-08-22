using DotsGame.LinksAndDots;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static DotsGame.LinksAndDots.Dot;

namespace DotsGame
{
    public static class DebugInfo
    {
        public static string textDBG = string.Empty;
        public static string textDBG1 = string.Empty; 
        public static List<Dot> lstDBG1 = new List<Dot>();
        public static List<string> lstDBG2 = new List<string>();
        
    }
    namespace GameCore
    {
        public class ListDots : List<Dot>
        {
            public event EventHandler<ListDotsEventArgs> OnAdd;
            public event EventHandler<ListDotsEventArgs> OnRemove;

            public new void Add(Dot dot)
            {
                ListDotsEventArgs args = new ListDotsEventArgs();
                args.Dot = dot;
                base.Add(dot);
                OnAdd?.Invoke(this, args);
            }
            public new void Remove(Dot dot)
            {
                ListDotsEventArgs args = new ListDotsEventArgs();
                args.Dot = dot;
                base.Remove(dot);
                OnRemove?.Invoke(this, args);
            }

        }
        public class ListDotsEventArgs : EventArgs
        {
            public Dot Dot { get; set; }
        }
        public class GameDots : IEnumerator, IEnumerable//, IGame
        {
            //private const int PLAYER_DRAW = -1;
            //private const int StateOwn.Empty = 0;
            //private const int StateOwner.Human = 1;
            public StateMove MoveState;//переменная хранит значение игрока который делает ход
            public enum StateMove
            {
                Human,
                Computer
            }
            /// <summary>
            /// Cписок ходов - где точки не ссылаются на точки списка Dot, а дублируют
            /// </summary>
            //public List<Dot> ListMoves { get; set; }
            public ListDots ListMoves { get; set; }


            /// <summary>
            /// Список ходов. рабочий стек
            /// </summary>
            private List<Dot> StackMoves { get; set; }

        public Dot LastMove
            {
                get
                {
                    if (ListMoves.Count == 0)//когда выбирается первая точка для хода
                    {
                        var random = new Random(DateTime.Now.Millisecond);
                        var q = from Dot d in Dots
                                where d.X <= BoardWidth / 2 & d.X > BoardWidth / 3
                                                        & d.Y <= BoardHeight / 2 & d.Y > BoardHeight / 3
                                orderby (random.Next())
                                select d;
                        return q.First();

                    }
                    else
                    {
                        return ListMoves.Last();
                    }
                }
            }
            //public List<Dot> ListDotsForDrawing
            //{// главная коллекция для отрисовки партии
            //    get
            //    {
            //        return ListMoves.ToList();
            //    }
            //}
            private Dot best_move; //ход который должен сделать комп
            private List<Dot> dots_in_region;//записывает сюда точки, которые окружают точку противника
            /// <summary>
            /// Основной список - вся доска
            /// </summary>
            public List<Dot> Dots { get; set; }

            public Dot GetDotCopy(Dot DotForCopy)
            {
                Dot d = new Dot(DotForCopy.X, DotForCopy.Y, DotForCopy.Own)
                {
                    Blocked = DotForCopy.Blocked,
                    BlokingDots = DotForCopy.BlokingDots,
                    BonusDot = DotForCopy.BonusDot,
                    Fixed = DotForCopy.Fixed,
                    IndexDot = DotForCopy.IndexDot,
                    IndexRelation = DotForCopy.IndexRelation,
                    iNumberPattern = DotForCopy.iNumberPattern,
                    Rating = DotForCopy.Rating,
                    Tag = DotForCopy.Tag
                };
                return d;
            }

            /// <summary>
            /// Возвращает список не занятых точек
            /// </summary>
            private List<Dot> Board_ValidMoves
            {
                get
                {
                    return (from Dot d in Dots where CheckValidMove(d) select d).ToList();
                }
            }
            private List<Dot> Board_NotEmptyNonBlockedDots
            {
                get
                {
                    return (from Dot d in Dots where d.Own != 0 && d.Blocked == false select d).ToList();
                }
            }
            private List<Dot> Board_AllNotBlockedDots
            {
                get
                {
                    return Dots.Where(d => !d.Blocked).ToList();
                }
            }
            private GameDots CopyDots
            {
                get
                {
                    GameDots ad = new GameDots(BoardWidth, BoardHeight);
                    for (int i = 0; i < Dots.Count; i++)
                    {
                        ad.Dots[i].Blocked = Dots[i].Blocked;
                        ad.Dots[i].Fixed = Dots[i].Fixed;
                        ad.Dots[i].IndexDot = Dots[i].IndexDot;
                        ad.Dots[i].Own = Dots[i].Own;
                        ad.Dots[i].X = Dots[i].X;
                        ad.Dots[i].Y = Dots[i].Y;
                    }
                    return ad;
                }
            }


#if DEBUG
            Stopwatch stopWatch = new Stopwatch();//для диагностики времени выполнения
            Stopwatch sW_BM = new Stopwatch();
            Stopwatch sW2 = new Stopwatch();

#endif
            int position = -1;

            public int BoardWidth { get; set; }
            public int BoardHeight { get; set; }
            private int BoardSize => BoardWidth * BoardHeight;
            public GameDots(int boardwidth, int boardheight)
            {
                int counter = 0;
                int ind;
                BoardHeight = boardheight;
                BoardWidth = boardwidth;
                Dots = new List<Dot>(boardwidth * boardheight); // главная коллекция точек
                for (int i = 0; i < boardwidth; i++)
                {
                    for (int j = 0; j < boardheight; j++)
                    {
                        ind = IndexDot(i, j);
                        Dots.Add(new Dot(i, j));
                        Dots[ind].IndexDot = ind;
                        if (i == 0 | i == (boardwidth - 1) | j == 0 | j == (boardheight - 1)) Dots[ind].Fixed = true;
                        counter += 1;
                    }
                }
                ListMoves = new ListDots();
                ListMoves.OnAdd += new EventHandler<ListDotsEventArgs>(ListMoves_OnAdd);
                ListMoves.OnRemove += new EventHandler<ListDotsEventArgs>(ListMoves_OnRemove);
                ListLinks = new List<Links>();
                //ListMoves = new List<Dot>();
                StackMoves = new List<Dot>();
                dots_in_region = new List<Dot>();

            }
            public void ListMoves_OnAdd(object sender, ListDotsEventArgs e)
            {
                UpdateDotsInListMoves();
            }
            public void ListMoves_OnRemove(object sender, ListDotsEventArgs e)
            {
                //UpdateDotsInListMoves();
            }
            private void UpdateDotsInListMoves()
            {
                for (int i = 0; i < ListMoves.Count; i++)
                {
                    ListMoves[i].Blocked = Dots.Find(d => d.IndexDot == ListMoves[i].IndexDot).Blocked;
                }
                return;
            }

            public void NewGame()
            {
                ClearDoard();
                dots_in_region.Clear();
            }
            public class DotEq : EqualityComparer<Dot>
            {
                public override int GetHashCode(Dot dot)
                {
                    int hCode = dot.X ^ dot.Y;
                    return hCode.GetHashCode();
                }

                public override bool Equals(Dot d1, Dot d2)
                {
                    return (d1.X == d2.X & d1.Y == d2.Y & d1.Rating == d2.Rating);
                }
            }
            public int Count
            {
                get
                {
                    return Dots.Count;
                }
            }
            public Dot this[int i, int j]//Индексатор возвращает элемент из массива по его индексу
            {
                get
                {
                    if (i >= BoardWidth) i = BoardWidth - 1;
                    if (j >= BoardHeight) j = BoardHeight - 1;
                    if (i < 0) i = 0;
                    if (j < 0) j = 0;
                    return Dots[IndexDot(i, j)];
                }
            }
            public string GetDotStatistic(Dot d)
            {
                string dotstatistic = string.Empty;
                dotstatistic = "Blocked: " + this[d.X, d.Y].Blocked.ToString() + "\r\n" +
                                   "BlokingDots: " + this[d.X, d.Y].BlokingDots.Count.ToString() + "\r\n" +
                                   "CountBlockedDots: " + this[d.X, d.Y].CountBlockedDots.ToString() + "\r\n" +
                                   "Fixed: " + this[d.X, d.Y].Fixed.ToString() + "\r\n" +
                                   "IndexDot: " + this[d.X, d.Y].IndexDot.ToString() + "\r\n" +
                                   "IndexRelation: " + this[d.X, d.Y].IndexRelation.ToString() + "\r\n" +
                                   "Marked: " + this[d.X, d.Y].Marked.ToString() + "\r\n" +
                                   "NeiborDots: " + this[d.X, d.Y].NeiborDots.Count.ToString() + "\r\n" +
                                   "Own: " + this[d.X, d.Y].Own.ToString() + "\r\n" +
                                   "Rating: " + this[d.X, d.Y].Rating.ToString() + "\r\n" +
                                   "StateDot: " + this[d.X, d.Y].StateDot.ToString() + "\r\n" +
                                   "Tag: " + this[d.X, d.Y].Tag.ToString() + "\r\n" +
                                   "X = " + this[d.X, d.Y].X.ToString() + "; Y = " + this[d.X, d.Y].Y.ToString();
                return dotstatistic;
            }

            private void Move(Dot dot)//добавляет точку в массив
            {
                int ind = IndexDot(dot.X, dot.Y);
                if (DotIndexCheck(dot.X, dot.Y))
                {
                    Dots[ind].Own = dot.Own;
                    Dots[ind].Rating = dot.Rating;
                    Dots[ind].Tag = dot.Tag;
                    Dots[ind].iNumberPattern = dot.iNumberPattern;
                    if (dot.Own != 0) Dots[ind].IndexRelation = Dots[ind].IndexDot;
                    Dots[ind].Blocked = false;
                    if (dot.X == 0 | dot.X == (BoardWidth - 1) | dot.Y == 0 |
                        dot.Y == (BoardHeight - 1)) Dots[ind].Fixed = true;
                    AddNeibor(Dots[ind]);
                    StackMoves.Add(Dots[ind]);
                }
            }
            private void AddNeibor(Dot dot)
            {
                //выбрать соседние точки, если такие есть
                IEnumerable<Dot> q = from Dot d in Dots where d.Own == dot.Own & Distance(dot, d) < 2 select d;

                foreach (Dot d in q)
                {
                    if (d != dot)
                    {
                        //if (dot.Rating > d.Rating) dot.Rating = d.Rating;
                        if (dot.NeiborDots.Contains(d) == false) dot.NeiborDots.Add(d);
                        if (d.NeiborDots.Contains(dot) == false) d.NeiborDots.Add(dot);
                    }
                }
                MakeIndexRelation(dot);
            }
            private void RemoveNeibor(Dot dot)
            {
                foreach (Dot d in Dots)
                {
                    if (d.NeiborDots.Contains(dot)) d.NeiborDots.Remove(dot);
                }
            }
            //private void Remove(Dot dot)//удаляет точку из массива
            //{
            //int ind = IndexDot(dot.X, dot.Y);
            //if (DotIndexCheck(dot.X, dot.Y))
            //{
            //    int i = Dots[ind].IndexDot;
            //    RemoveNeibor(dot);
            //    Dots[ind] = new Dot(dot.X, dot.Y);
            //    Dots[ind].IndexDot = i;
            //    Dots[ind].IndexRelation = i;
            //    ListMoves.Remove(dot);
            //    stackMoves.Remove(dot);
            //}
            //}
            public float Distance(Dot dot1, Dot dot2)//расстояние между точками
            {
                return (float)Math.Round(Math.Sqrt(Math.Pow((dot1.X - dot2.X), 2) + Math.Pow((dot1.Y - dot2.Y), 2)), 1);
            }
            /// <summary>
            /// возвращает список соседних точек заданной точки
            /// </summary>
            /// <param name="dot"> точка Dot из массива точек типа ArrayDots </param>
            /// <returns> список точек </returns>
            private List<Dot> NeighborDotsSNWE(Dot dot)//SNWE -S -South, N -North, W -West, E -East
            {
                Dot[] dts = new Dot[4] {
                                    this[dot.X + 1, dot.Y],
                                    this[dot.X - 1, dot.Y],
                                    this[dot.X, dot.Y + 1],
                                    this[dot.X, dot.Y - 1]
                                    };
                return dts.ToList();
            }
            private List<Dot> NeighborDots(Dot dot)
            {
                //List<Dot> l = new List<Dot>();
                //foreach (Dot d in (from neibordots in Dots
                //                   from dt in Dots
                //                   where Distance(dt,neibordots)<2
                //                   select neibordots))
                //{
                //    l.Add(d);
                //}

                Dot[] dts = new Dot[8] {
                                    this[dot.X + 1, dot.Y],
                                    this[dot.X - 1, dot.Y],
                                    this[dot.X, dot.Y + 1],
                                    this[dot.X, dot.Y - 1],
                                    this[dot.X + 1, dot.Y + 1],
                                    this[dot.X - 1, dot.Y - 1],
                                    this[dot.X - 1, dot.Y + 1],
                                    this[dot.X + 1, dot.Y - 1]
                                    };
                return dts.ToList();
            }
            public void UnmarkAllDots()
            {
                Counter = 0;
                foreach (Dot d in Dots)
                {
                    d.UnmarkDot();
                }
            }
            private int MinX()
            {
                var q = from Dot d in Dots where d.Own != 0 & d.Blocked == false select d;
                int minX = BoardWidth;
                foreach (Dot d in q)
                {
                    if (minX > d.X) minX = d.X;
                }
                return minX;
            }
            private int MaxX()
            {
                var q = from Dot d in Dots where d.Own != 0 & d.Blocked == false select d;
                int maxX = 0;
                foreach (Dot d in q)
                {
                    if (maxX < d.X) maxX = d.X;
                }
                return maxX;
            }
            private int MaxY()
            {
                var q = from Dot d in Dots where d.Own != 0 & d.Blocked == false select d;
                int maxY = 0;
                foreach (Dot d in q)
                {
                    if (maxY < d.Y) maxY = d.Y;
                }
                return maxY;
            }
            private int MinY()
            {
                var q = from Dot d in Dots where d.Own != 0 & d.Blocked == false select d;
                int minY = BoardHeight;
                foreach (Dot d in q)
                {
                    if (minY > d.Y) minY = d.Y;
                }
                return minY;
            }
            private int CountNeibourDots(StateOwn Owner)//количество точек определенного цвета возле пустой точки
            {
                var q = from Dot d in Dots
                        where d.Blocked == false & d.Own == 0 &
                        Dots[IndexDot(d.X + 1, d.Y - 1)].Blocked == false & Dots[IndexDot(d.X + 1, d.Y - 1)].Own == Owner &
                        Dots[IndexDot(d.X + 1, d.Y + 1)].Blocked == false & Dots[IndexDot(d.X + 1, d.Y + 1)].Own == Owner
                        | d.Own == 0 & Dots[IndexDot(d.X, d.Y - 1)].Blocked == false & Dots[IndexDot(d.X, d.Y - 1)].Own == Owner & Dots[IndexDot(d.X, d.Y + 1)].Blocked == false & Dots[IndexDot(d.X, d.Y + 1)].Own == Owner
                        | d.Own == 0 & Dots[IndexDot(d.X - 1, d.Y - 1)].Blocked == false & Dots[IndexDot(d.X - 1, d.Y - 1)].Own == Owner & Dots[IndexDot(d.X - 1, d.Y + 1)].Blocked == false & Dots[IndexDot(d.X - 1, d.Y + 1)].Own == Owner
                        | d.Own == 0 & Dots[IndexDot(d.X - 1, d.Y - 1)].Blocked == false & Dots[IndexDot(d.X - 1, d.Y - 1)].Own == Owner & Dots[IndexDot(d.X + 1, d.Y + 1)].Blocked == false & Dots[IndexDot(d.X + 1, d.Y + 1)].Own == Owner
                        | d.Own == 0 & Dots[IndexDot(d.X - 1, d.Y + 1)].Blocked == false & Dots[IndexDot(d.X - 1, d.Y + 1)].Own == Owner & Dots[IndexDot(d.X + 1, d.Y - 1)].Blocked == false & Dots[IndexDot(d.X + 1, d.Y - 1)].Own == Owner
                        select d;
                return q.Count();
            }
            /// <summary>
            /// Вычисляет индекс точки в списке по ее координатам
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <returns></returns>
            private int IndexDot(int x, int y)
            {

                int index = x * BoardHeight + y;

                return index;
            }
            /// <summary>
            /// Проверка, находится ли точка на игровой доске
            /// </summary>
            /// <returns></returns>
            private bool DotIndexCheck(int x, int y)
            {
                return (x >= 0 && x < BoardWidth &&
                        y >= 0 && y < BoardHeight);
            }

            /// <summary>
            /// список не занятых точек возле всех точек
            /// </summary>
            /// <param name="Owner"></param>
            /// <returns></returns>
            private List<Dot> EmptyNeibourDots(StateOwn Owner)
            {
                List<Dot> ld = new List<Dot>();
                foreach (Dot d in Dots)
                {
                    if (d.Own == Owner)
                    {
                        var q = from Dot dot in Dots
                                where dot.Blocked == false & dot.Own == 0 & Distance(dot, d) < 2
                                select dot;
                        foreach (Dot empty_d in q)
                        {
                            if (ld.Contains(empty_d) == false) ld.Add(empty_d);
                        }
                    }
                }
                return ld;
            }

            private int MakeIndexRelation(Dot dot)
            {
                if (dot.NeiborDots.Count > 0)
                {
                    foreach (Dot d in dot.NeiborDots)
                    {
                        if (dot.Blocked == false & dot.Own == d.Own) d.IndexRelation = dot.IndexRelation;
                    }
                }
                else
                {
                }
                return dot.IndexRelation;
            }
            /// <summary>
            /// Не очищает список Dots, а сбрасывает статусы точки
            /// </summary>
            public void ClearDoard()
            {
                foreach (Dot d in Dots)
                {
                    d.Own = 0;
                    d.Marked = false;
                    d.Blocked = false;
                    d.BlokingDots.Clear();
                    d.Rating = 0;
                    d.Tag = "";
                    d.iNumberPattern = 0;
                    d.IndexRelation = d.IndexDot;
                    d.StateDot = StateDotInPattern.Normal;
                }
                ListLinks.Clear();
                ListMoves.Clear();
                StackMoves.Clear();
            }

            ///// <summary>
            ///// проверяет заблокирована ли точка. Перед использованием функции надо установить flg_own
            ///// </summary>
            ///// <param name="dot">поверяемая точка</param>
            ///// <param name="flg_own">владелец проверяемой точки, этот параметр нужен для рекурсии</param>
            ///// <returns></returns>
            //private bool DotIsFree(Dot dot, StateOwn flg_own)
            //{
            //    dot.Marked = true;
            //    if (dot.X == 0 | dot.Y == 0 | dot.X == BoardWidth - 1 | dot.Y == BoardHeight - 1)
            //    {
            //        return true;
            //    }
            //    Dot[] d = new Dot[4] { this[dot.X + 1, dot.Y], this[dot.X - 1, dot.Y], this[dot.X, dot.Y + 1], this[dot.X, dot.Y - 1] };
            //    //--------------------------------------------------------------------------------
            //    if (flg_own == 0)// если точка не принадлежит никому и рядом есть незаблокированные точки -эта точка считается свободной(незаблокированной)
            //    {
            //        var q = from Dot fd in d where fd.Blocked == false select fd;
            //        if (q.Count() > 0)
            //        {
            //            return true;
            //        }
            //        else return false;

            //    }
            //    //----------------------------------------------------------------------------------
            //    for (int i = 0; i < 4; i++)
            //    {
            //        if (d[i].Marked == false)
            //        {
            //            if (d[i].Own == 0 | d[i].Own == flg_own | d[i].Own != flg_own
            //              & d[i].Blocked & d[i].BlokingDots.Contains(dot) == false)
            //            {
            //                if (DotIsFree(d[i], flg_own))
            //                {
            //                    return true;
            //                }
            //            }
            //        }
            //    }
            //    return false;
            //}
            //********************************************************************************************
            private int Counter = 0;
            private Dot DotChecked;
            //private bool IsFreeflag;
            private bool DotIsBlocked(Dot dot)
            {
                if (Counter == 0)
                {
                    DotChecked = dot;
                }

                dot.Marked = true;
                List<Dot> lst = NeighborDotsSNWE(dot);
                if (dot.Fixed | (from d in lst
                                 where d.Fixed & d.Own == DotChecked.Own |
                                       d.Fixed & d.Own == 0
                                 select d).Count() > 0)

                {
                    DotChecked.Blocked = false;
                    return false;
                }
                Counter++;
                foreach (Dot d in lst)
                {
                    if (!d.Marked && d.Own == DotChecked.Own & !d.Blocked |
                                    d.Own == 0 |
                                    d.Own != DotChecked.Own & d.Blocked)
                    {
                        if (!DotIsBlocked(d))
                        {
                            Counter--;
                            goto ext;
                        }
                    }
                }
                return true;
                ext:
                return false;
            }

            //private void RebuildDots1()
            //{
            //    GameDots _Dots = new GameDots(BoardWidth, BoardHeight);
            //    foreach (Dot dot in StackMoves)
            //    {
            //        if (ListMoves.Contains(dot)) _Dots.MakeMove(dot, dot.Own, addForDraw: true);
            //        else _Dots.MakeMove(dot, dot.Own);
            //    }
            //    _Dots.RescanBlockedDots();
            //    Dots = _Dots.Dots;
            //    ListMoves = _Dots.ListMoves;
            //    LinkDots();
            //}
            public void UndoMove(Dot dot)//поле отмена хода
            {
                ListMoves.Remove(dot);
                StackMoves.Remove(dot);
                //ListLinks.Clear();
                //Dots = new List<Dot>(BoardSize);
                foreach (Dot d in Dots)
                {
                    d.Restore();
                }
                for (int i = 0; i < ListMoves.Count; i++)
                {
                    MakeMove(ListMoves[i], ListMoves[i].Own);
                }
                LinkDots();
                StackMoves.Clear();
                UpdateDotsInListMoves();
            }
            public bool CheckValidMove(Dot CheckDotForMove)
            {
                if (CheckDotForMove is null) return false;

                Dot d = Dots.Find(x => x.X == CheckDotForMove.X && x.Y == CheckDotForMove.Y);
                if (d is null)
                {
                    return false;
                }
                return d.Blocked == false && d.Own == 0;
            }
            private int count_in_region;
            private int count_blocked_dots;

            /// <summary>
            /// Функция делает ход игрока 
            /// </summary>
            /// <param name="dot">точка куда делается ход</param>
            /// <param name="Owner">владелец точки -целое 1-Игрок или 2 -Компьютер</param>
            /// <returns>количество окруженных точек, -1 если недопустимый ход; </returns>
            private int MakeMove(Dot dot, StateOwn Owner = 0, bool addForDraw = false)//
            {
                int Count_blocked_before1; int Count_blocked_after1;
                int Count_blocked_before2; int Count_blocked_after2;
                Win_player = 0;
                Count_blocked_before1 = (from Dot d in this where d.Own == StateOwn.Human && d.Blocked == true select d).Count();
                Count_blocked_before2 = (from Dot d in this where d.Own == StateOwn.Computer && d.Blocked == true select d).Count();
                if (CheckValidMove(this[dot.X, dot.Y]))//если точка не занята
                {
                    if (Owner != 0)
                    {
                        dot.Own = Owner;
                    }
                    Move(dot);
                }
                else return -1;//в случае невозможного хода
                               //--------------------------------
                CheckBlocked(dot.Own);
                //Count_blocked_after1 = CheckBlocked(dot.Own);
                Count_blocked_after1 = (from Dot d in this where d.Own == StateOwn.Human && d.Blocked == true select d).Count();
                Count_blocked_after2 = (from Dot d in this where d.Own == StateOwn.Computer && d.Blocked == true select d).Count();
                if (addForDraw)
                {
                    //ListMoves.Add(Dots[IndexDot(dot.X, dot.Y)]);
                    //ListMoves.Add(new Dot (dot.X, dot.Y, dot.Own));
                    ListMoves.Add(GetDotCopy(Dots[IndexDot(dot.X, dot.Y)]));
                    LinkDots();//перестроить связи точек
                    ListLinksForDrawing = ListLinks;
                }
                int result1 = Count_blocked_after1 - Count_blocked_before1;
                int result2 = Count_blocked_after2 - Count_blocked_before2;
                if (result1 != 0) Win_player = StateOwn.Computer;
                if (result2 != 0) Win_player = StateOwn.Human;

                return result1 + result2;//res;
            }

            /// <summary>
            /// проверяет блокировку точек, маркирует точки которые блокируют, возвращает количество окруженных точек
            /// </summary>
            /// <param name="arrDots"></param>
            /// <param name="last_moveOwner"></param>
            /// <returns>количество окруженных точек</returns>
            private int CheckBlocked(StateOwn last_moveOwner = StateOwn.Empty)
            {
                int counter = 0;
                IOrderedEnumerable<Dot> checkdots = from Dot dots in this
                                where dots.Own != 0 | dots.Own == 0 & dots.Blocked
                                orderby dots.Own == last_moveOwner
                                select dots;
                Lst_blocked_dots.Clear(); Lst_in_region_dots.Clear();
                foreach (Dot d in checkdots)
                {
                    UnmarkAllDots();
                    if (d.Blocked | DotIsBlocked(d) == true)
                    {
                        d.Blocked = true;
                        d.IndexRelation = 0;
                        IEnumerable<Dot> q1 = from Dot dots in this where dots.BlokingDots.Contains(d) select dots;
                        if (q1.Count() == 0)
                        {
                            UnmarkAllDots();
                            MarkDotsInRegion(d, d.Own);
                            for (int i = 0; i < Lst_in_region_dots.Count; i++)
                            {
                                Dot dr = Lst_in_region_dots[i];
                                //Win_player = dr.Own;
                                count_in_region++;
                                for (int j = 0; j < Lst_blocked_dots.Count; j++)
                                {
                                    Dot bd = Lst_blocked_dots[j];
                                    if (bd.Own != 0) counter += 1;
                                    if (dr.BlokingDots.Contains(bd) == false & bd.Own != 0 & dr.Own != bd.Own)
                                    {
                                        dr.BlokingDots.Add(bd);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        d.Blocked = false;
                    }
                }
                //if (Lst_blocked_dots.Count == 0) win_player = 0;
                //return Lst_blocked_dots.Count;
                return (from Dot d in this where d.Blocked == true select d).Count();

            }

            //private int CheckBlocked2(StateOwn Owner)
            //{
            //    var checkdots = from Dot d in this
            //                    where d.Own == Owner && DotIsFree(d, Owner) == false
            //                    select d;
            //    if (checkdots.Count() > 0) foreach (Dot d in checkdots) d.Blocked = true;

            //    var blocking_dots = from Dot d1 in this
            //                        where d1.Blocked
            //                        from Dot d2 in this
            //                        where d2.Blocked == false & Distance(d1, d2) == 1
            //                        select d2;


            //    Lst_blocked_dots = checkdots.ToList();


            //    return Lst_blocked_dots.Count;
            //    //if (blocking_dots.Count() > 0) foreach (Dot d in blocking_dots) d.Blocked = true;
            //}

            /// <summary>
            /// Определяет блокирующие точки и устанавливает этим точкам поле InRegion=true 
            /// </summary>
            /// <param name="blocked_dot">точка, которая блокируется</param>
            /// <param name="flg_own">владелец точки</param>
            private void MarkDotsInRegion(Dot blocked_dot, StateOwn flg_own)
            {
                blocked_dot.Marked = true;
                //добавим точки которые попали в окружение
                if (Lst_blocked_dots.Contains(blocked_dot) == false)
                {
                    Lst_blocked_dots.Add(blocked_dot);
                }
                //foreach (Dot _d in dts)
                foreach (Dot _d in NeighborDotsSNWE(blocked_dot))
                {
                    if (_d.Own != 0 & _d.Blocked == false & _d.Own != flg_own)//_d-точка которая окружает
                    {
                        //добавим в коллекцию точки которые окружают
                        if (Lst_in_region_dots.Contains(_d) == false) Lst_in_region_dots.Add(_d);
                    }
                    else
                    {
                        if (_d.Marked == false & _d.Fixed == false)
                        {
                            _d.Blocked = true;
                            MarkDotsInRegion(_d, flg_own);
                        }
                    }
                }
            }
            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            private void MakeRating()//возвращает массив вражеских точек вокруг заданной точки
            {
                int res;
                var qd = from Dot dt in this where dt.Own != 0 & dt.Blocked == false select dt;
                foreach (Dot dot in qd)
                {
                    //if (dot.x > 0 & dot.y > 0 & dot.x < iMapSize -1 & dot.y < iMapSize -1)
                    if (dot.X > 0 & dot.Y > 0 & dot.X < BoardWidth - 1 & dot.Y < BoardHeight - 1)
                    {
                        Dot[] dts = new Dot[4] {this[dot.X + 1, dot.Y],
                                            this[dot.X -1, dot.Y],
                                            this[dot.X, dot.Y + 1],
                                            this[dot.X, dot.Y -1]};
                        res = 0;
                        foreach (Dot item in dts)
                        {
                            if (item.Own != 0 & item.Own != dot.Own) res++;
                            else if (item.Own == dot.Own & item.Rating == 0)
                            {
                                res = -1;
                                break;
                            }
                        }
                        dot.Rating = res + 1;//точка без связей получает рейт 1
                    }
                }
            }
            public List<Links> ListLinks { get; private set; }
            public List<Links> ListLinksForDrawing { get; private set; } = new List<Links>();

            /// <summary>
            /// устанавливает связь между двумя точками и возвращает массив связей 
            /// </summary>
            private void LinkDots()
            {
                ListLinks.Clear();
                ListLinks = (from Dot d1 in this
                             where d1.BlokingDots.Count > 0
                             from Dot d2 in this
                             where d2.Own == d1.Own && d1.Blocked == d2.Blocked
                             && d2.BlokingDots.Count > 0 && Distance(d1, d2) > 0 && Distance(d1, d2) < 2
                             select new Links(d1, d2)).Distinct(new LinksComparer()).ToList(); //обновляем основной массив связей - lnks              
            }

            /// <summary>
            /// функция проверяет не делается ли ход в точку, которая на следующем ходу будет окружена
            /// </summary>
            /// <param name="dot"></param>
            /// <param name="arrDots"></param>
            /// <param name="Player"></param>
            /// <returns></returns>
            private bool CheckDot(Dot dot, StateOwn Player)
            {
                int res = MakeMove(dot, Player);
                StateOwn pl = Player == StateOwn.Computer ? StateOwn.Human : StateOwn.Computer;
                if (Win_player == pl)// первое условие -ход в уже окруженный регион, 
                {
                    UndoMove(dot);
                    return true; // да будет окружена
                }
                //будет ли окружена на следующем ходу
                Dot dotEnemy = CheckMove(pl);
                if (dotEnemy != null)
                {
                    res = MakeMove(dotEnemy, pl);
                    UndoMove(dotEnemy);
                    UndoMove(dot);
                    return true; // да будет окружена
                }
                //нет не будет
                UndoMove(dot);
                return false;
            }
            //===============================================================================================================

            //private List<Dot> RebuildLinks()
            //{
            //List<Dot> lstDots = new List<Dot>();

            //var q = from Dot dot in this
            //        where dot.IndexDot == index & dot.NeiborDots.Count == 1
            //        select dot;

            //    return null;
            //}
            //==============================================================================================
            /// <summary>
            /// проверяет ход в результате которого окружение.
            /// </summary>
            /// <param name="Owner">владелец проверянмых точек</param>
            /// <returns>Возвращает ход(точку) который завершает окружение</returns>
            private Dot CheckMove1(StateOwn Owner)
            {
                List<Dot> happy_dots = new List<Dot>();
                var qry = Board_ValidMoves.Where(
                #region Query patterns Check

                             d =>
                              //     d.x + 1 < BoardWidth && d.x - 1 > -1 &&
                              //     d.y + 1 < BoardHeight && d.y - 1 > -1 &&
                              //       + 
                              //    d  
                              //       +
                              this[d.X + 1, d.Y - 1].Blocked == false & this[d.X + 1, d.Y + 1].Blocked == false &
                              this[d.X + 1, d.Y - 1].Own == Owner & this[d.X + 1, d.Y + 1].Own == Owner &
                              this[d.X + 1, d.Y].Own != Owner
                              //+        
                              //   d     
                              //+       
                              | this[d.X - 1, d.Y - 1].Blocked == false & this[d.X - 1, d.Y + 1].Blocked == false &
                              this[d.X - 1, d.Y - 1].Own == Owner & this[d.X - 1, d.Y + 1].Own == Owner &
                              this[d.X - 1, d.Y].Own != Owner
                            //      
                            //   d          
                            //+     +
                            | this[d.X + 1, d.Y + 1].Blocked == false & this[d.X - 1, d.Y + 1].Blocked == false &
                            this[d.X + 1, d.Y + 1].Own == Owner & this[d.X - 1, d.Y + 1].Own == Owner &
                            this[d.X, d.Y + 1].Own != Owner
                            //+     + 
                            //   d          
                            //     
                            | this[d.X - 1, d.Y - 1].Blocked == false & this[d.X + 1, d.Y - 1].Blocked == false &
                             this[d.X - 1, d.Y - 1].Own == Owner & this[d.X + 1, d.Y - 1].Own == Owner &
                             this[d.X, d.Y - 1].Own != Owner

                              //    +    
                              //    d   
                              //    +   
                              | this[d.X, d.Y + 1].Blocked == false & this[d.X, d.Y - 1].Blocked == false &
                                  this[d.X, d.Y - 1].Own == Owner & this[d.X, d.Y + 1].Own == Owner
                              //&
                              //this[d.x + 1, d.y].Own != Owner |
                              //this[d.x - 1, d.y].Own != Owner
                              //        
                              //+   d   +  
                              //       
                              | this[d.X - 1, d.Y].Blocked == false & this[d.X + 1, d.Y].Blocked == false &
                                this[d.X - 1, d.Y].Own == Owner & this[d.X + 1, d.Y].Own == Owner
                              //&
                              //this[d.x, d.y + 1].Own != Owner &
                              //this[d.x, d.y - 1].Own != Owner

                              //+        
                              //   d     
                              //      +   
                              | this[d.X - 1, d.Y - 1].Blocked == false & this[d.X + 1, d.Y + 1].Blocked == false &
                              this[d.X - 1, d.Y - 1].Own == Owner & this[d.X + 1, d.Y + 1].Own == Owner
                              //&
                              //this[d.x, d.y - 1].Own != Owner &
                              //this[d.x, d.y + 1].Own != Owner &
                              //this[d.x + 1, d.y].Own != Owner &
                              //this[d.x - 1, d.y].Own != Owner
                              //      +  
                              //   d     
                              //+        
                              | this[d.X - 1, d.Y + 1].Blocked == false & this[d.X + 1, d.Y - 1].Blocked == false &
                              this[d.X - 1, d.Y + 1].Own == Owner & this[d.X + 1, d.Y - 1].Own == Owner
                            //&
                            //this[d.x, d.y - 1].Own != Owner &
                            //this[d.x, d.y + 1].Own != Owner &
                            //this[d.x + 1, d.y].Own != Owner &
                            //this[d.x - 1, d.y].Own != Owner

                            //      +
                            //+  d
                            | this[d.X - 1, d.Y].Blocked == false & this[d.X + 1, d.Y - 1].Blocked == false &
                              this[d.X - 1, d.Y].Own == Owner & this[d.X + 1, d.Y - 1].Own == Owner
                              &
                             this[d.X, d.Y - 1].Own != Owner

                            //& this[d.x + 1, d.y].Own != Owner &
                            //this[d.x - 1, d.y - 1].Own != Owner &
                            //this[d.x + 1, d.y + 1].Own != Owner &
                            // this[d.x, d.y + 1].Own != Owner

                            // +  d
                            //       +
                            | this[d.X - 1, d.Y].Blocked == false & this[d.X + 1, d.Y + 1].Blocked == false &
                              this[d.X - 1, d.Y].Own == Owner & this[d.X + 1, d.Y + 1].Own == Owner
                              &
                              this[d.X, d.Y + 1].Own != Owner
                            //& this[d.x + 1, d.y].Own != Owner &
                            //this[d.x - 1, d.y + 1].Own != Owner &
                            //this[d.x + 1, d.y - 1].Own != Owner &
                            //this[d.x, d.y - 1].Own != Owner

                            //+
                            //   d  +       
                            | this[d.X + 1, d.Y].Blocked == false & this[d.X - 1, d.Y - 1].Blocked == false &
                             this[d.X + 1, d.Y].Own == Owner & this[d.X - 1, d.Y - 1].Own == Owner &
                             this[d.X, d.Y - 1].Own != Owner

                            //& this[d.x - 1, d.y + 1].Own != Owner &
                            //this[d.x - 1, d.y].Own != Owner &
                            //this[d.x + 1, d.y - 1].Own != Owner 
                            //& this[d.x, d.y + 1].Own != Owner

                            //   d  +       
                            //+
                            | this[d.X + 1, d.Y].Blocked == false & this[d.X - 1, d.Y + 1].Blocked == false &
                             this[d.X + 1, d.Y].Own == Owner & this[d.X - 1, d.Y + 1].Own == Owner &
                             this[d.X, d.Y + 1].Own != Owner
                            //&
                            //this[d.x + 1, d.y + 1].Own != Owner &
                            //this[d.x - 1, d.y].Own != Owner &
                            //this[d.x - 1, d.y - 1].Own != Owner &
                            // this[d.x, d.y - 1].Own != Owner

                            //+   
                            //   d          
                            //   +
                            | this[d.X, d.Y + 1].Blocked == false & this[d.X - 1, d.Y - 1].Blocked == false &
                             this[d.X, d.Y + 1].Own == Owner & this[d.X - 1, d.Y - 1].Own == Owner &
                             this[d.X - 1, d.Y].Own != Owner
                            //&
                            //this[d.x - 1, d.y + 1].Own != Owner &
                            //this[d.x, d.y - 1].Own != Owner &
                            //this[d.x + 1, d.y - 1].Own != Owner 
                            //& this[d.x + 1, d.y].Own != Owner

                            //   +
                            //   d          
                            //+   
                            | this[d.X, d.Y - 1].Blocked == false & this[d.X - 1, d.Y + 1].Blocked == false &
                            this[d.X, d.Y - 1].Own == Owner & this[d.X - 1, d.Y + 1].Own == Owner &
                            this[d.X - 1, d.Y].Own != Owner
                            //&
                            //this[d.x - 1, d.y - 1].Own != Owner &
                            //this[d.x + 1, d.y].Own != Owner &
                            //this[d.x + 1, d.y + 1].Own != Owner &
                            //this[d.x, d.y + 1].Own != Owner

                            //   +
                            //   d          
                            //      +
                            | this[d.X, d.Y - 1].Blocked == false & this[d.X + 1, d.Y + 1].Blocked == false &
                            this[d.X, d.Y - 1].Own == Owner & this[d.X + 1, d.Y + 1].Own == Owner &
                            this[d.X + 1, d.Y].Own != Owner
                            //&
                            //this[d.x, d.y + 1].Own != Owner &
                            //this[d.x - 1, d.y + 1].Own != Owner &
                            //this[d.x - 1, d.y].Own != Owner &
                            //this[d.x + 1, d.y - 1].Own != Owner

                            //      +
                            //   d          
                            //   +   
                            | this[d.X, d.Y + 1].Blocked == false & this[d.X + 1, d.Y - 1].Blocked == false &
                            this[d.X, d.Y + 1].Own == Owner & this[d.X + 1, d.Y - 1].Own == Owner &
                            this[d.X + 1, d.Y].Own != Owner
//&
//this[d.x - 1, d.y - 1].Own != Owner &
//this[d.x, d.y - 1].Own != Owner &
//this[d.x + 1, d.y + 1].Own != Owner &
//this[d.x - 1, d.y].Own != Owner
                             #endregion
);
#if DEBUG
                Dot[] ad = qry.ToArray();
#endif
                foreach (Dot d in qry)
                {
                    //делаем ход
                    int result_last_move = MakeMove(d, Owner);
#if DEBUG
                    //if (f.chkMove.Checked) Pause();
#endif
                    //-----------------------------------
                    if (result_last_move != 0 & this[d.X, d.Y].Blocked == false)
                    {
                        UndoMove(d);
                        //d.CountBlockedDots = result_last_move;
                        happy_dots.Add(d);
                        //return d;
                    }
                    UndoMove(d);
                }

                //выбрать точку, которая максимально окружит
                var x = happy_dots.Where(dd =>
                        dd.CountBlockedDots == happy_dots.Max(dt => dt.CountBlockedDots));

                return x.Count() > 0 ? x.First() : null;

            }

            private Dot ОбщаяТочкаSNWE(Dot d1, Dot d2)//*1d1* 
            {
                return NeighborDotsSNWE(d1).Intersect(NeighborDotsSNWE(d2), new DotEq()).FirstOrDefault();
            }
            private List<Dot> ОбщаяТочка(Dot d1, Dot d2)
            {
                return NeighborDots(d1).Intersect(NeighborDots(d2), new DotEq()).ToList();
            }

            //==============================================================================================
            /// <summary>
            /// проверяет ход в результате которого окружение.
            /// </summary>
            /// <param name="Owner">владелец проверяемых точек</param>
            /// <returns>Возвращает ход(точку) который завершает окружение</returns>
            private Dot CheckMove(StateOwn Owner)
            {
                List<Dot> happy_dots = new List<Dot>();
                var qry = from Dot d1 in this
                          where d1.Own == Owner
                          from Dot d2 in this
                          where
                                d2.IndexRelation == d1.IndexRelation
                                && Distance(d1, d2) > 2
                                && Distance(d1, d2) < 3
                                && ОбщаяТочка(d1, d2).Where(dt => dt.Own == Owner).Count() == 0
                                ||
                                d2.IndexRelation == d1.IndexRelation
                                && Distance(d1, d2) == 2
                          from Dot d in this
                          where CheckValidMove(d) && Distance(d, d1) < 2 && Distance(d, d2) < 2
                                    && NeighborDotsSNWE(d).Where(dt => dt.Own == Owner).Count() <= 2
                          select d;

                foreach (Dot d in qry.Distinct(new DotEq()).ToList())
                {
                    //делаем ход
                    if (MakeMove(d, Owner) != 0 & this[d.X, d.Y].Blocked == false)
                    {
                        happy_dots.Add(new Dot(d.X, d.Y, d.Own));
                    }
                    UndoMove(d);
                }

                //выбрать точку, которая максимально окружит
                var x = happy_dots.Where(dd =>
                        dd.CountBlockedDots == happy_dots.Max(dt => dt.CountBlockedDots));

                return x.Count() > 0 ? x.First() : null;

            }

            private Dot CheckPatternVilkaNextMove(StateOwn Owner)
            {
                var qry = Board_NotEmptyNonBlockedDots.Where(dt => dt.Own == Owner);
                Dot dot_ptn;
                if (qry.Count() != 0)
                {
                    foreach (Dot d in qry)
                    {
                        foreach (Dot dot_move in NeighborDots(d))
                        {
                            if (CheckValidMove(dot_move))
                            {
                                //делаем ход
                                int result_last_move = MakeMove(dot_move, Owner);
                                StateOwn pl = Owner == StateOwn.Computer ? StateOwn.Human : StateOwn.Computer;
                                Dot dt = CheckMove(pl); // проверка чтобы не попасть в капкан
                                if (dt != null)
                                {
                                    UndoMove(dot_move);
                                    continue;
                                }
                                dot_ptn = CheckPattern_vilochka(d.Own);
                                //-----------------------------------
                                if (dot_ptn != null & result_last_move == 0)
                                {
                                    UndoMove(dot_move);
                                    return dot_move;
                                }
                                UndoMove(dot_move);
                            }
                        }
                    }
                }
                return null;
            }
            private int iNumberPattern;
            // * *
            // + m +
            private List<Dot> Проверка1(StateOwn Owner)
            {
                IEnumerable<Dot> qry = from Dot d1 in Board_NotEmptyNonBlockedDots.Where(dt => dt.Own == Owner)
                                       where d1.Own == Owner && !d1.Blocked
                                       from Dot d2 in Board_NotEmptyNonBlockedDots.Where(dt => dt.Own == Owner)
                                       where d2.Own == Owner && !d2.Blocked
                                               && Distance(d1, d2) == 1
                                       from Dot de1 in Board_NotEmptyNonBlockedDots.Where(dt => dt.Own != Owner)
                                       where de1.Own != Owner && Distance(de1, d1) == 1
                                             && Distance(de1, d2) == 1.4f
                                       from Dot de2 in Board_NotEmptyNonBlockedDots.Where(dt => dt.Own != Owner)
                                       where de1.Own != Owner && Distance(de2, d2) == 1.4f
                                             && Distance(de2, de1) == 2
                                       from Dot dm in EmptyNeibourDots(Owner)
                                       where CheckValidMove(dm) && Distance(dm, d1) == 1.4f
                                             && Distance(dm, d2) == 1
                                             && Distance(dm, de1) == 1
                                             && Distance(dm, de2) == 1
                                       select dm;
                List<Dot> ld = qry.Distinct(new DotEq()).ToList();
                foreach (Dot d in ld) d.Tag = "Проверка1(" + Owner + ")";
                return ld;
            }

            private Dot CheckPattern_vilochka(StateOwn Owner)
            {
                StateOwn Enemy = Owner == StateOwn.Human ? StateOwn.Computer : StateOwn.Human;
                IEnumerable<Dot> get_non_blocked = from Dot d in this where d.Blocked == false select d; //получить коллекцию незаблокированных точек

                //паттерн на диагональное расположение точек           *red1               +
                //                                                                      +  -
                //                                              *red2  +blue2        +  -  =
                //                                                           
                //                                       *red3  +blue1  move(red) 
                iNumberPattern = 1;
                IEnumerable<Dot> pat;
                pat = from Dot dot0 in get_non_blocked
                      where dot0.Own == StateOwn.Empty
                      from Dot dot1 in get_non_blocked
                      where dot1.Own == StateOwn.Empty && Distance(dot1, dot0) == 2.8f
                      from Dot dot2 in get_non_blocked
                      where dot2.Own == Owner && Distance(dot2, dot1) == 1.4f
                      from Dot dot3 in get_non_blocked
                      where dot3.Own == Owner && Distance(dot3, dot2) == 1.4f
                      from Dot dot4 in get_non_blocked
                      where dot4.Own == Owner && Distance(dot4, dot3) == 1.4f
                      from Dot dot5 in get_non_blocked
                      where dot5.Own == Enemy && Distance(dot5, dot4) == 1.0f
                      from Dot dot6 in get_non_blocked
                      where dot6.Own == Enemy && Distance(dot6, dot5) == 1.4f && Distance(dot6, dot0) == 2.2f
                      from Dot move in get_non_blocked
                      where move.Own == StateOwn.Empty
&& Distance(dot0, move) == 1.4f
&& Distance(dot1, move) == 1.4f
&& Distance(dot2, move) == 2.0f
&& Distance(dot3, move) == 1.4f
&& Distance(dot4, move) == 2.0f
&& Distance(dot5, move) == 1.0f
&& Distance(dot6, move) == 1.0f
                      select move;
                if (pat.Count() > 0) return pat.FirstOrDefault();
                //**********************************************************************************        
                //     *     *
                //  *  +  *  +   
                //  *        m
                //     * 
                iNumberPattern = 2;
                pat = from Dot dot0 in get_non_blocked
                      where dot0.Own != Owner
                      from Dot dot1 in get_non_blocked
                      where dot1.Own == StateOwn.Empty && Distance(dot1, dot0) == 1.4f
                      from Dot dot2 in get_non_blocked
                      where dot2.Own == StateOwn.Empty && Distance(dot2, dot1) == 2.8f
                      from Dot dot3 in get_non_blocked
                      where dot3.Own == Owner && Distance(dot3, dot2) == 3.6f
                      from Dot dot4 in get_non_blocked
                      where dot4.Own == Owner && Distance(dot4, dot3) == 1.4f
                      from Dot dot5 in get_non_blocked
                      where dot5.Own == Owner && Distance(dot5, dot4) == 1.4f
                      from Dot dot6 in get_non_blocked
                      where dot6.Own == Owner && Distance(dot6, dot5) == 1.0f
                      from Dot dot7 in get_non_blocked
                      where dot7.Own == Owner && Distance(dot7, dot6) == 1.4f
                      from Dot dot8 in get_non_blocked
                      where dot8.Own != Owner && Distance(dot8, dot7) == 2.2f
                      from Dot dot9 in get_non_blocked
                      where dot9.Own == Enemy && Distance(dot9, dot8) == 1.4f
&& Distance(dot9, dot0) == 2.2f
                      from Dot move in get_non_blocked
                      where move.Own == StateOwn.Empty
&& Distance(dot0, move) == 2.0f
&& Distance(dot1, move) == 1.4f
&& Distance(dot2, move) == 1.4f
&& Distance(dot3, move) == 2.2f
&& Distance(dot4, move) == 3.0f
&& Distance(dot5, move) == 2.2f
&& Distance(dot6, move) == 1.4f
&& Distance(dot7, move) == 2.0f
&& Distance(dot8, move) == 1.0f
&& Distance(dot9, move) == 1.0f
                      select move;
                if (pat.Count() > 0) return pat.FirstOrDefault();
                //===========ВИЛОЧКА=================================================================================================== 
                //     +   
                //  m  -  
                //  -  +
                //  +
                iNumberPattern = 3;
                pat = from Dot dot0 in get_non_blocked
                      where dot0.Own == StateOwn.Empty
                      from Dot dot1 in get_non_blocked
                      where dot1.Own == StateOwn.Empty && Distance(dot1, dot0) == 1.4f
                      from Dot dot2 in get_non_blocked
                      where dot2.Own == StateOwn.Empty && Distance(dot2, dot1) == 1.0f
                      from Dot dot3 in get_non_blocked
                      where dot3.Own == StateOwn.Empty && Distance(dot3, dot2) == 3.2f
                      from Dot dot4 in get_non_blocked
                      where dot4.Own == Owner && Distance(dot4, dot3) == 2.8f
                      from Dot dot5 in get_non_blocked
                      where dot5.Own == Owner && Distance(dot5, dot4) == 1.4f
                      from Dot dot6 in get_non_blocked
                      where dot6.Own == Owner && Distance(dot6, dot5) == 2.0f
                      from Dot dot7 in get_non_blocked
                      where dot7.Own == Enemy && Distance(dot7, dot6) == 2.2f
                      from Dot dot8 in get_non_blocked
                      where dot8.Own == Enemy && Distance(dot8, dot7) == 1.4f
                      from Dot move in get_non_blocked
                      where move.Own == StateOwn.Empty
                        && Distance(dot0, move) == 1.0f
                        && Distance(dot1, move) == 1.0f
                        && Distance(dot2, move) == 1.4f
                        && Distance(dot3, move) == 2.0f
                        && Distance(dot4, move) == 2.0f
                        && Distance(dot5, move) == 1.4f
                        && Distance(dot6, move) == 1.4f
                        && Distance(dot7, move) == 1.0f
                        && Distance(dot8, move) == 1.0f
                      select move;
                if (pat.Count() > 0) return pat.FirstOrDefault();


                //   +   +
                // + - m - +
                //
                iNumberPattern = 4;
                pat = from Dot dot0 in get_non_blocked
                      where dot0.Own == Enemy
                      from Dot dot1 in get_non_blocked
                      where dot1.Own == Enemy && Distance(dot1, dot0) == 2.0f
                      from Dot dot2 in get_non_blocked
                      where dot2.Own == Owner && Distance(dot2, dot1) == 3.0f
                      from Dot dot3 in get_non_blocked
                      where dot3.Own == Owner && Distance(dot3, dot2) == 1.4f
                      from Dot dot4 in get_non_blocked
                      where dot4.Own == Owner && Distance(dot4, dot3) == 2.0f
                      from Dot dot5 in get_non_blocked
                      where dot5.Own == Owner && Distance(dot5, dot4) == 1.4f
                      from Dot dot6 in get_non_blocked
                      where dot6.Own == StateOwn.Empty && Distance(dot6, dot5) == 3.2f
                      from Dot dot7 in get_non_blocked
                      where dot7.Own == StateOwn.Empty && Distance(dot7, dot6) == 1.0f
                      from Dot dot8 in get_non_blocked
                      where dot8.Own == StateOwn.Empty && Distance(dot8, dot7) == 1.0f
&& Distance(dot8, dot0) == 2.2f
                      from Dot move in get_non_blocked
                      where move.Own == StateOwn.Empty
&& Distance(dot0, move) == 1.0f
&& Distance(dot1, move) == 1.0f
&& Distance(dot2, move) == 2.0f
&& Distance(dot3, move) == 1.4f
&& Distance(dot4, move) == 1.4f
&& Distance(dot5, move) == 2.0f
&& Distance(dot6, move) == 1.4f
&& Distance(dot7, move) == 1.0f
&& Distance(dot8, move) == 1.4f
                      select move;
                if (pat.Count() > 0) return pat.FirstOrDefault();
                //=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*
                return null;//если никаких паттернов не найдено возвращаем нуль

            }
            private Dot CheckPatternMove1(StateOwn Owner)//паттерны без вражеской точки
            {
                iNumberPattern = 1;
                var pat1 = from Dot d in this
                           where d.Own == 0
                               && this[d.X - 1, d.Y + 1].Own == Owner && this[d.X - 1, d.Y + 1].Blocked == false
                               && this[d.X + 1, d.Y - 1].Own == Owner && this[d.X + 1, d.Y - 1].Blocked == false
                               && this[d.X - 1, d.Y].Own == 0 && this[d.X - 1, d.Y].Blocked == false
                               && this[d.X - 1, d.Y - 1].Own == 0 && this[d.X - 1, d.Y - 1].Blocked == false
                               && this[d.X, d.Y - 1].Own == 0 && this[d.X, d.Y - 1].Blocked == false
                               && this[d.X + 1, d.Y].Own == 0 && this[d.X + 1, d.Y].Blocked == false
                               && this[d.X + 1, d.Y + 1].Own == 0 && this[d.X + 1, d.Y + 1].Blocked == false
                               && this[d.X, d.Y + 1].Own == 0 && this[d.X, d.Y + 1].Blocked == false
                               && this[d.X, d.Y - 2].Own == 0 && this[d.X, d.Y - 2].Blocked == false
                               && this[d.X + 2, d.Y].Own == 0 && this[d.X + 2, d.Y].Blocked == false
                               && this[d.X, d.Y + 2].Own == 0 && this[d.X, d.Y + 2].Blocked == false
                               && this[d.X - 2, d.Y].Own == 0 && this[d.X - 2, d.Y].Blocked == false
                           select d;
                if (pat1.Count() > 0) return new Dot(pat1.First().X, pat1.First().Y);
                //--------------Rotate on 90-----------------------------------
                var pat1_2_3_4 = from Dot d in this
                                 where d.Own == 0
                                     && this[d.X + 1, d.Y + 1].Own == Owner && this[d.X + 1, d.Y - 1].Blocked == false
                                     && this[d.X - 1, d.Y - 1].Own == Owner && this[d.X - 1, d.Y + 1].Blocked == false
                                     && this[d.X, d.Y + 1].Own == 0 && this[d.X, d.Y - 1].Blocked == false
                                     && this[d.X - 1, d.Y + 1].Own == 0 && this[d.X - 1, d.Y - 1].Blocked == false
                                     && this[d.X - 1, d.Y].Own == 0 && this[d.X - 1, d.Y].Blocked == false
                                     && this[d.X, d.Y - 1].Own == 0 && this[d.X, d.Y + 1].Blocked == false
                                     && this[d.X + 1, d.Y - 1].Own == 0 && this[d.X + 1, d.Y + 1].Blocked == false
                                     && this[d.X + 1, d.Y].Own == 0 && this[d.X + 1, d.Y].Blocked == false
                                     && this[d.X - 2, d.Y].Own == 0 && this[d.X - 2, d.Y].Blocked == false
                                     && this[d.X, d.Y - 2].Own == 0 && this[d.X, d.Y + 2].Blocked == false
                                     && this[d.X + 2, d.Y].Own == 0 && this[d.X + 2, d.Y].Blocked == false
                                     && this[d.X, d.Y + 2].Own == 0 && this[d.X, d.Y - 2].Blocked == false
                                 select d;
                if (pat1_2_3_4.Count() > 0) return new Dot(pat1_2_3_4.First().X, pat1_2_3_4.First().Y);
                //============================================================================================================== 
                iNumberPattern = 883;
                var pat883 = from Dot d in this
                             where d.Own == Owner
                                 && this[d.X + 1, d.Y].Own == 0 && this[d.X + 1, d.Y].Blocked == false
                                 && this[d.X + 2, d.Y].Own == 0 && this[d.X + 2, d.Y].Blocked == false
                                 && this[d.X + 3, d.Y].Own == 0 && this[d.X + 3, d.Y].Blocked == false
                                 && this[d.X + 3, d.Y - 1].Own == Owner && this[d.X + 3, d.Y - 1].Blocked == false
                                 && this[d.X + 2, d.Y - 1].Own == 0 && this[d.X + 2, d.Y - 1].Blocked == false
                                 && this[d.X + 1, d.Y - 1].Own == 0 && this[d.X + 1, d.Y - 1].Blocked == false
                                 && this[d.X, d.Y - 1].Own == 0 && this[d.X, d.Y - 1].Blocked == false
                                 && this[d.X + 2, d.Y - 2].Own == 0 && this[d.X + 2, d.Y - 2].Blocked == false
                                 && this[d.X + 1, d.Y - 2].Own == 0 && this[d.X + 1, d.Y - 2].Blocked == false
                                 && this[d.X + 3, d.Y - 2].Own == 0 && this[d.X + 3, d.Y - 2].Blocked == false
                                 && this[d.X, d.Y - 2].Own == 0 && this[d.X, d.Y - 2].Blocked == false
                             select d;
                if (pat883.Count() > 0) return new Dot(pat883.First().X + 1, pat883.First().Y - 1);
                //--------------Rotate on 90-----------------------------------
                var pat883_2_3 = from Dot d in this
                                 where d.Own == Owner
                                     && this[d.X, d.Y - 1].Own == 0 && this[d.X, d.Y - 1].Blocked == false
                                     && this[d.X, d.Y - 2].Own == 0 && this[d.X, d.Y - 2].Blocked == false
                                     && this[d.X, d.Y - 3].Own == 0 && this[d.X, d.Y - 3].Blocked == false
                                     && this[d.X + 1, d.Y - 3].Own == Owner && this[d.X + 1, d.Y - 3].Blocked == false
                                     && this[d.X + 1, d.Y - 2].Own == 0 && this[d.X + 1, d.Y - 2].Blocked == false
                                     && this[d.X + 1, d.Y - 1].Own == 0 && this[d.X + 1, d.Y - 1].Blocked == false
                                     && this[d.X + 1, d.Y].Own == 0 && this[d.X + 1, d.Y].Blocked == false
                                     && this[d.X + 2, d.Y - 2].Own == 0 && this[d.X + 2, d.Y - 2].Blocked == false
                                     && this[d.X + 2, d.Y - 1].Own == 0 && this[d.X + 2, d.Y - 1].Blocked == false
                                     && this[d.X + 2, d.Y - 3].Own == 0 && this[d.X + 2, d.Y - 3].Blocked == false
                                     && this[d.X + 2, d.Y].Own == 0 && this[d.X + 2, d.Y].Blocked == false
                                 select d;
                if (pat883_2_3.Count() > 0) return new Dot(pat883_2_3.First().X + 1, pat883_2_3.First().Y - 1);
                //=================================================================================
                // 0d край доски
                // m   *
                iNumberPattern = 2;
                var pat2 = from Dot d in this
                           where d.Own == Owner && d.Y == 0 && d.X > 0 && d.X < BoardWidth
                               && this[d.X + 1, d.Y + 1].Own == Owner && this[d.X + 1, d.Y + 1].Blocked == false
                               && this[d.X, d.Y + 1].Own == 0 && this[d.X, d.Y + 1].Blocked == false
                           select d;
                if (pat2.Count() > 0) return new Dot(pat2.First().X, pat2.First().Y + 1);
                var pat2_2 = from Dot d in this
                             where d.Own == Owner && d.Y > 1 && d.Y < BoardHeight && d.X == 0
                                   && this[d.X + 1, d.Y + 1].Own == Owner && this[d.X + 1, d.Y + 1].Blocked == false
                                   && this[d.X + 1, d.Y].Own == 0 && this[d.X + 1, d.Y].Blocked == false
                             select d;
                if (pat2_2.Count() > 0) return new Dot(pat2_2.First().X + 1, pat2_2.First().Y);
                var pat2_2_3 = from Dot d in this
                               where d.Own == Owner && d.X == BoardWidth - 1 && d.Y > 0 && d.Y < BoardHeight
                                     && this[d.X - 1, d.Y - 1].Own == Owner && this[d.X - 1, d.Y - 1].Blocked == false
                                     && this[d.X - 1, d.Y].Own == 0 && this[d.X - 1, d.Y].Blocked == false
                               select d;
                if (pat2_2_3.Count() > 0) return new Dot(pat2_2_3.First().X - 1, pat2_2_3.First().Y);
                var pat2_2_3_4 = from Dot d in this
                                 where d.Own == Owner && d.Y == BoardHeight - 1 && d.X > 0 && d.X < BoardWidth
                                       && this[d.X - 1, d.Y - 1].Own == Owner && this[d.X - 1, d.Y - 1].Blocked == false
                                       && this[d.X, d.Y - 1].Own == 0 && this[d.X, d.Y - 1].Blocked == false
                                 select d;
                if (pat2_2_3_4.Count() > 0) return new Dot(pat2_2_3_4.First().X, pat2_2_3_4.First().Y - 1);
                iNumberPattern = 4;
                var pat4 = from Dot d in this
                           where d.Own == Owner
                               && this[d.X, d.Y - 1].Own == 0 && this[d.X, d.Y - 1].Blocked == false
                               && this[d.X + 1, d.Y - 2].Own == 0 && this[d.X + 1, d.Y - 2].Blocked == false
                               && this[d.X + 2, d.Y - 2].Own == Owner && this[d.X + 2, d.Y - 2].Blocked == false
                               && this[d.X + 2, d.Y - 1].Own == 0 && this[d.X + 2, d.Y - 1].Blocked == false
                               && this[d.X + 1, d.Y].Own == 0 && this[d.X + 1, d.Y].Blocked == false
                               && this[d.X + 1, d.Y - 1].Own == 0 && this[d.X + 1, d.Y - 1].Blocked == false
                           select d;
                if (pat4.Count() > 0) return new Dot(pat4.First().X + 1, pat4.First().Y - 1);
                //180 Rotate=========================================================================================================== 
                //  *
                //     m
                //        d* 
                var pat4_2 = from Dot d in this
                             where d.Own == Owner
                                 && this[d.X, d.Y - 1].Own == 0 && this[d.X, d.Y - 1].Blocked == false
                                 && this[d.X - 1, d.Y - 2].Own == 0 && this[d.X - 1, d.Y - 2].Blocked == false
                                 && this[d.X - 2, d.Y - 2].Own == Owner && this[d.X - 2, d.Y - 2].Blocked == false
                                 && this[d.X - 2, d.Y - 1].Own == 0 && this[d.X - 2, d.Y - 1].Blocked == false
                                 && this[d.X - 1, d.Y].Own == 0 && this[d.X - 1, d.Y].Blocked == false
                                 && this[d.X - 1, d.Y - 1].Own == 0 && this[d.X - 1, d.Y - 1].Blocked == false
                             select d;
                if (pat4_2.Count() > 0) return new Dot(pat4_2.First().X - 1, pat4_2.First().Y - 1);
                //============================================================================================================== 
                //d*  m  *
                iNumberPattern = 7;
                var pat7 = from Dot d in this
                           where d.Own == Owner
                               && this[d.X + 1, d.Y].Own == 0 && this[d.X + 1, d.Y].Blocked == false
                               && this[d.X + 1, d.Y - 1].Own == 0 && this[d.X + 1, d.Y - 1].Blocked == false
                               && this[d.X + 1, d.Y + 1].Own == 0 && this[d.X + 1, d.Y + 1].Blocked == false
                               && this[d.X + 2, d.Y].Own == Owner && this[d.X + 2, d.Y].Blocked == false
                           select d;
                if (pat7.Count() > 0) return new Dot(pat7.First().X + 1, pat7.First().Y);
                //--------------Rotate on 90-----------------------------------
                //   *
                //   m
                //   d*
                var pat7_2 = from Dot d in this
                             where d.Own == Owner
                                 && this[d.X, d.Y - 1].Own == 0 && this[d.X, d.Y - 1].Blocked == false
                                 && this[d.X - 1, d.Y - 1].Own == 0 && this[d.X - 1, d.Y - 1].Blocked == false
                                 && this[d.X + 1, d.Y - 1].Own == 0 && this[d.X + 1, d.Y - 1].Blocked == false
                                 && this[d.X, d.Y - 2].Own == Owner && this[d.X, d.Y - 2].Blocked == false
                             select d;
                if (pat7_2.Count() > 0) return new Dot(pat7_2.First().X, pat7_2.First().Y - 1);
                //============================================================================================================== 
                //    *
                // m 
                //
                // d*
                iNumberPattern = 8;
                var pat8 = from Dot d in this
                           where d.Own == Owner
                               && this[d.X + 1, d.Y - 3].Own == Owner && this[d.X + 1, d.Y - 3].Blocked == false
                               && this[d.X, d.Y - 2].Own == 0 && this[d.X, d.Y - 2].Blocked == false
                               && this[d.X + 1, d.Y - 2].Own == 0 && this[d.X + 1, d.Y - 2].Blocked == false
                               && this[d.X + 1, d.Y - 1].Own == 0 && this[d.X + 1, d.Y - 1].Blocked == false
                               && this[d.X, d.Y - 1].Own == 0 && this[d.X, d.Y - 1].Blocked == false
                               && this[d.X + 1, d.Y].Own == 0 && this[d.X + 1, d.Y].Blocked == false
                               && this[d.X, d.Y - 3].Own == 0 && this[d.X, d.Y - 3].Blocked == false
                           select d;
                if (pat8.Count() > 0) return new Dot(pat8.First().X, pat8.First().Y - 2);
                //180 Rotate=========================================================================================================== 
                var pat8_2 = from Dot d in this
                             where d.Own == Owner
                                 && this[d.X - 1, d.Y + 3].Own == Owner && this[d.X - 1, d.Y + 3].Blocked == false
                                 && this[d.X, d.Y + 2].Own == 0 && this[d.X, d.Y + 2].Blocked == false
                                 && this[d.X - 1, d.Y + 2].Own == 0 && this[d.X - 1, d.Y + 2].Blocked == false
                                 && this[d.X - 1, d.Y + 1].Own == 0 && this[d.X - 1, d.Y + 1].Blocked == false
                                 && this[d.X, d.Y + 1].Own == 0 && this[d.X, d.Y + 1].Blocked == false
                                 && this[d.X - 1, d.Y].Own == 0 && this[d.X - 1, d.Y].Blocked == false
                                 && this[d.X, d.Y + 3].Own == 0 && this[d.X, d.Y + 3].Blocked == false
                             select d;
                if (pat8_2.Count() > 0) return new Dot(pat8_2.First().X, pat8_2.First().Y + 2);
                //--------------Rotate on 90-----------------------------------
                var pat8_2_3 = from Dot d in this
                               where d.Own == Owner
                                   && this[d.X + 3, d.Y - 1].Own == Owner && this[d.X + 3, d.Y - 1].Blocked == false
                                   && this[d.X + 2, d.Y].Own == 0 && this[d.X + 2, d.Y].Blocked == false
                                   && this[d.X + 2, d.Y - 1].Own == 0 && this[d.X + 2, d.Y - 1].Blocked == false
                                   && this[d.X + 1, d.Y - 1].Own == 0 && this[d.X + 1, d.Y - 1].Blocked == false
                                   && this[d.X + 1, d.Y].Own == 0 && this[d.X + 1, d.Y].Blocked == false
                                   && this[d.X, d.Y - 1].Own == 0 && this[d.X, d.Y - 1].Blocked == false
                                   && this[d.X + 3, d.Y].Own == 0 && this[d.X + 3, d.Y].Blocked == false
                               select d;
                if (pat8_2_3.Count() > 0) return new Dot(pat8_2_3.First().X + 2, pat8_2_3.First().Y);
                //--------------Rotate on 90 -2-----------------------------------
                var pat8_2_3_4 = from Dot d in this
                                 where d.Own == Owner
                                     && this[d.X - 3, d.Y + 1].Own == Owner && this[d.X - 3, d.Y + 1].Blocked == false
                                     && this[d.X - 2, d.Y].Own == 0 && this[d.X - 2, d.Y].Blocked == false
                                     && this[d.X - 2, d.Y + 1].Own == 0 && this[d.X - 2, d.Y + 1].Blocked == false
                                     && this[d.X - 1, d.Y + 1].Own == 0 && this[d.X - 1, d.Y + 1].Blocked == false
                                     && this[d.X - 1, d.Y].Own == 0 && this[d.X - 1, d.Y].Blocked == false
                                     && this[d.X, d.Y + 1].Own == 0 && this[d.X, d.Y + 1].Blocked == false
                                     && this[d.X - 3, d.Y].Own == 0 && this[d.X - 3, d.Y].Blocked == false
                                 select d;
                if (pat8_2_3_4.Count() > 0) return new Dot(pat8_2_3_4.First().X - 2, pat8_2_3_4.First().Y);
                //============================================================================================================== 
                //     *
                //        d*  
                //     m
                //============================================================================================================== 
                iNumberPattern = 9;
                var pat9 = from Dot d in this
                           where d.Own == Owner && d.X >= 2
                           && this[d.X - 1, d.Y - 1].Own == Owner && this[d.X - 1, d.Y - 1].Blocked == false
                           && this[d.X - 1, d.Y + 1].Own == 0 && this[d.X - 1, d.Y + 1].Blocked == false
                           && this[d.X - 1, d.Y].Own == 0 && this[d.X - 1, d.Y].Blocked == false
                           && this[d.X - 2, d.Y].Own == 0 && this[d.X - 2, d.Y].Blocked == false
                           select d;
                if (pat9.Count() > 0) return new Dot(pat9.First().X - 1, pat9.First().Y + 1);
                //180 Rotate=========================================================================================================== 
                //     m  
                // d*  
                //     *
                var pat9_2 = from Dot d in this
                             where d.Own == Owner && d.X <= BoardWidth - 2
                                   && this[d.X + 1, d.Y + 1].Own == Owner && this[d.X + 1, d.Y + 1].Blocked == false
                                   && this[d.X + 1, d.Y - 1].Own == 0 && this[d.X + 1, d.Y - 1].Blocked == false
                                   && this[d.X + 1, d.Y].Own == 0 && this[d.X + 1, d.Y].Blocked == false
                                   && this[d.X + 2, d.Y].Own == 0 && this[d.X + 2, d.Y].Blocked == false
                             select d;
                if (pat9_2.Count() > 0) return new Dot(pat9_2.First().X + 1, pat9_2.First().Y - 1);
                //--------------Rotate on 90-----------------------------------
                //         
                //     d*
                //  m       *
                var pat9_2_3 = from Dot d in this
                               where d.Own == Owner && d.Y <= BoardHeight - 2
                                     && this[d.X + 1, d.Y + 1].Own == Owner && this[d.X + 1, d.Y + 1].Blocked == false
                                     && this[d.X - 1, d.Y + 1].Own == 0 && this[d.X - 1, d.Y + 1].Blocked == false
                                     && this[d.X, d.Y + 1].Own == 0 && this[d.X, d.Y + 1].Blocked == false
                                     && this[d.X, d.Y + 2].Own == 0 && this[d.X, d.Y + 2].Blocked == false
                               select d;
                if (pat9_2_3.Count() > 0) return new Dot(pat9_2_3.First().X - 1, pat9_2_3.First().Y + 1);
                //--------------Rotate on 90 -2-----------------------------------
                // *      m
                //    d*   
                //
                var pat9_2_3_4 = from Dot d in this
                                 where d.Own == Owner && d.Y >= 2
                                       && this[d.X - 1, d.Y - 1].Own == Owner && this[d.X - 1, d.Y - 1].Blocked == false
                                       && this[d.X + 1, d.Y - 1].Own == 0 && this[d.X + 1, d.Y - 1].Blocked == false
                                       && this[d.X, d.Y - 1].Own == 0 && this[d.X, d.Y - 1].Blocked == false
                                       && this[d.X, d.Y - 2].Own == 0 && this[d.X, d.Y - 2].Blocked == false
                                 select d;
                if (pat9_2_3_4.Count() > 0) return new Dot(pat9_2_3_4.First().X + 1, pat9_2_3_4.First().Y - 1);
                //============================================================================================================== 

                //=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*
                return null;//если никаких паттернов не найдено возвращаем нуль
            }
            /// <summary>
            /// Поиск паттерна, где точка может соединить разные цепочки (с разным IndexRelation)
            /// </summary>
            /// <param name="Owner"></param>
            /// <returns>Список точек, которые могут замкнуть регион</returns>
            private List<Dot> CheckPatternMove(StateOwn Owner)
            {
                var qry = from Dot d1 in this
                          where d1.Own == Owner && !d1.Blocked
                          from Dot d2 in this
                          where d2.Own == Owner && !d2.Blocked
                                  && d2.IndexRelation != d1.IndexRelation
                                  && Distance(d1, d2) >= 2
                                  && Distance(d1, d2) < 3
                          from Dot dm in this
                          where CheckValidMove(dm) & Distance(dm, d1) < 2
                          && CheckValidMove(dm) & Distance(dm, d2) < 2
                          select dm;
                List<Dot> ld = qry.Distinct(new DotEq()).ToList();
                foreach (Dot d in ld) d.Tag = "CheckPatternMove(" + Owner + ")";
                return ld;
            }

            /// <summary>
            /// проверка хода на гарантированное окружение(когда точки находятся через две клетки) 
            /// </summary>
            /// <param name="Owner"></param>
            /// <returns></returns>
            private List<Dot> CheckPattern2Move(StateOwn Owner, bool IndexRelation)
            {
                IEnumerable<Dot> qry;
                if (IndexRelation)
                {
                    qry = from Dot d1 in this
                          where d1.Own == Owner && !d1.Blocked
                          from Dot d2 in this
                          where d2.IndexRelation == d1.IndexRelation && !d2.Blocked && Distance(d1, d2) >= 3f && Distance(d1, d2) < 3.5f

                          from Dot de1 in NeighborDots(d1)
                          where de1.Own == 0 && !de1.Blocked

                          from Dot de2 in NeighborDots(d2)
                          where de2.Own == 0 && !de2.Blocked

                          from Dot de3 in NeighborDotsSNWE(de1).Intersect(NeighborDotsSNWE(de2), new DotEq())
                          where CheckValidMove(de3)

                          select de3;//new Dot(de3.X, de3.Y, NumberPattern: 777, Rating: 1);

                }
                else
                {
                    qry = from Dot d1 in this
                          where d1.Own == Owner && !d1.Blocked
                          from Dot d2 in this
                          where d2.Own == Owner && !d2.Blocked && Distance(d1, d2) < 3.5f & Distance(d1, d2) >= 3
                          from Dot de1 in this
                          where CheckValidMove(de1) & Distance(d1, de1) == 1
                          from Dot de2 in this
                          where CheckValidMove(de2) & Distance(de1, de2) == 1 & Distance(d1, de2) < 2
                          from Dot de3 in this
                          where CheckValidMove(de3) & Distance(d2, de3) < 2 & Distance(de2, de3) == 1
                             || CheckValidMove(de3) & Distance(d2, de3) < 2 & Distance(de1, de3) == 1
                          select de3;

                }
                List<Dot> ld = qry.Distinct(new DotEq()).ToList();
                foreach (Dot d in ld) d.Tag = "CheckPattern2Move(" + Owner + ")";
                return ld;
            }
            /// <summary>
            /// Проверка хода на гарантированное окружение(когда точки находятся через 4 клетки) 
            /// </summary>
            /// <param name="Owner"></param>
            /// <param name="IndexRelation"></param>
            /// <returns></returns>
            private List<Dot> CheckPatternVilka2x2(StateOwn Owner, bool IndexRelation)
            {
                IEnumerable<Dot> qry;
                //int rating = IndexRelation == true ? 3 : 4;
                if (IndexRelation)
                {
                    qry = from Dot d1 in this
                          where d1.Own == Owner && !d1.Blocked

                          from Dot d2 in this
                          where d2.IndexRelation == d1.IndexRelation && !d2.Blocked && Distance(d1, d2) < 4.5f & Distance(d1, d2) >= 2.5f

                          from Dot de1 in this
                          where CheckValidMove(de1) & Distance(d1, de1) == 1

                          from Dot de2 in this
                          where CheckValidMove(de2) & Distance(d2, de2) == 1

                          from Dot de1_1 in this
                          where CheckValidMove(de1_1) & Distance(de1_1, de1) == 1 & Distance(de1_1, d1) < 2

                          from Dot de2_1 in this
                          where CheckValidMove(de2_1) & Distance(de2_1, de2) == 1 & Distance(de2_1, d2) < 2


                          from Dot de3 in this
                          where CheckValidMove(de3) & Distance(de1, de3) < 2
                                              & Distance(de2, de3) < 2
                                              & Distance(de1_1, de3) < 2
                                              & Distance(de2_1, de3) < 2
                                              & Distance(d1, de3) >= 2
                                              & Distance(d2, de3) >= 2

                          select new Dot(de3.X, de3.Y, NumberPattern: 777, Rating: 2);
                }
                else
                {
                    qry = from Dot d1 in this
                          where d1.Own == Owner && !d1.Blocked

                          from Dot d2 in this
                          where d2.Own == d1.Own && !d2.Blocked && Distance(d1, d2) < 4.5f & Distance(d1, d2) >= 2.5f

                          from Dot de1 in this
                          where CheckValidMove(de1) & Distance(d1, de1) == 1

                          from Dot de2 in this
                          where CheckValidMove(de2) & Distance(d2, de2) == 1

                          from Dot de1_1 in this
                          where CheckValidMove(de1_1) & Distance(de1_1, de1) == 1 & Distance(de1_1, d1) < 2

                          from Dot de2_1 in this
                          where CheckValidMove(de2_1) & Distance(de2_1, de2) == 1 & Distance(de2_1, d2) < 2


                          from Dot de3 in this
                          where CheckValidMove(de3) & Distance(de1, de3) < 2
                                              & Distance(de2, de3) < 2
                                              & Distance(de1_1, de3) < 2
                                              & Distance(de2_1, de3) < 2
                                              & Distance(d1, de3) >= 2
                                              & Distance(d2, de3) >= 2

                          select new Dot(de3.X, de3.Y, NumberPattern: 777, Rating: 3);
                }

                List<Dot> ld = qry.Distinct(new DotEq()).ToList();
                foreach (Dot d in ld) d.Tag = "CheckPatternVilka2x2(" + Owner + ")";
                return ld;
            }
            private Dot PickComputerMove(Dot enemy_move, CancellationToken? ct)
            {

                #region если первый ход выбираем произвольную соседнюю точку
                if (ListMoves.Count < 2)
                {
                    var random = new Random(DateTime.Now.Millisecond);
                    var fm = from Dot d in Dots
                             where d.Own == 0 & Math.Sqrt(Math.Pow(Math.Abs(d.X - enemy_move.X), 2) + Math.Pow(Math.Abs(d.Y - enemy_move.Y), 2)) < 2
                             orderby random.Next()
                             select d;
                    return new Dot(fm.First().X, fm.First().Y); //так надо чтобы best_move не ссылался на точку в Dots;
                }
                #endregion
                #region  Если ситуация проигрышная -сдаемся
                //var q1 = from Dot d in Dots
                //         where d.Own == StateOwn.Computer && (d.Blocked == false)
                //         select d;
                //var q2 = from Dot d in Dots
                //         where d.Own == StateOwner.Human && (d.Blocked == false)
                //         select d;
                //float res1 = q2.Count();
                //float res2 = q1.Count();
                //if (res1 / res2 > 2.0)
                //{
                //    return null;
                //}

                #endregion

                best_move = null;
                var t1 = DateTime.Now.Millisecond;
                #region StopWatch
#if DEBUG
                stopWatch.Start();
#endif
                #endregion
                counter_moves = 0;
                Lst_branch.Clear();

                //Проигрываем разные комбинации
                recursion_depth = 0;
                Play(StateOwn.Computer);

                foreach (Dot d in Lst_branch) DebugInfo.lstDBG2.Add(d.ToString() + " - " + d.Tag);
                Dot move = Lst_branch.Where(dt => dt.Rating == Lst_branch.Min(d => d.Rating)).ElementAtOrDefault(0);

                if (move != null) best_move = move;
                #region Если не найдено лучшего хода, берем любую точку
                if (best_move == null)
                {
                    var random = new Random(DateTime.Now.Millisecond);
                    var q = from Dot d in Dots//любая точка
                            where d.Blocked == false & d.Own == StateOwn.Empty
                            orderby random.Next()
                            select d;

                    //if (q.Count() > 0) best_move = q.Where(dt => Distance(dt, LastMove) < 3).FirstOrDefault();
                    if (q.Count() > 0)
                    {
                        best_move = q.FirstOrDefault();//q.Where(dt => Distance(dt, LastMove) < 3).FirstOrDefault();
                        best_move.Tag = "Random";
                    }
                    else
                    {
                        best_move = null;
                    }
                }
                #endregion

                #region Statistic
#if DEBUG
                stopWatch.Stop();

                DebugInfo.textDBG = "Количество ходов: " + counter_moves + "\r\n Глубина рекурсии: " + MAX_RECURSION +
                "\r\n Ход на " + best_move.ToString() +
                "\r\n время просчета " + stopWatch.ElapsedMilliseconds.ToString() + " мс";
                stopWatch.Reset();
#endif
                #endregion
                DebugInfo.lstDBG2.Add("Ход на " + best_move.ToString() + " - " + best_move.Tag);
                return new Dot(best_move.X, best_move.Y); //так надо чтобы best_move не ссылался на точку в Dots
            }
            //===============================================================================================
            //-----------------------------------Поиск лучшего хода------------------------------------------
            //===============================================================================================
            private List<Dot> BestMove(StateOwn Player)
            {
                DebugInfo.lstDBG1.Clear();
                DebugInfo.lstDBG2.Clear();
                string strDebug = string.Empty;
                List<Dot> moves = new List<Dot>();
                Dot bm;
                StateOwn Enemy = Player == StateOwn.Human ? StateOwn.Computer : StateOwn.Human;
#if DEBUG
                {
                    sW2.Start();
                    DebugInfo.textDBG = "CheckMove(pl2,pl1)...";
                }
#endif
                bm = CheckMove(Player);
                if (bm != null)
                {
                    bm.Tag = "CheckMove(" + Player + ")";
                    bm.iNumberPattern = 777; //777-ход в результате которого получается окружение - компьютер побеждает
                    moves.Add(bm);
#if DEBUG
                    {
                        DebugInfo.lstDBG2.Add(bm.ToString() + " - Win Comp! ");
                    }
#endif
                }

                bm = CheckMove(Enemy);
                if (bm != null)
                {
                    bm.Tag = "CheckMove(" + Enemy + ")";
                    bm.iNumberPattern = 666; //666-ход в результате которого получается окружение -компьютер проигрывает
                    moves.Add(bm);
                    #region DEBUG
#if DEBUG
                    {
                        DebugInfo.lstDBG2.Add(bm.ToString() + " - Win Human! ");
                        sW2.Stop();
                        strDebug = "CheckMove pl1,pl2 -" + sW2.Elapsed.Milliseconds.ToString();
                        sW2.Reset();
                        //проверяем паттерны
                        sW2.Start();
                        DebugInfo.textDBG = "CheckPattern_vilochka проверяем ходы на два вперед...";
                    }
#endif
                    #endregion
                }
                #region CheckPattern_vilochka
                bm = CheckPattern_vilochka(Player);
                if (bm != null)
                {
#if DEBUG
                    {
                        DebugInfo.lstDBG2.Add(bm.ToString() + " ----> CheckPattern_vilochka ");
                    }
#endif
                    bm.Tag = "CheckPattern_vilochka(" + Player + ")";
                    bm.iNumberPattern = 777; //777-ход в результате которого получается окружение -компьютер побеждает
                    moves.Add(bm);
                }
                #endregion

                bm = CheckPattern_vilochka(Enemy);
                if (bm != null)
                {
                    bm.Tag = "CheckPattern_vilochka(" + Enemy + ")";
                    bm.iNumberPattern = 666; //777-ход в результате которого получается окружение -компьютер побеждает
                    moves.Add(bm);//return bm;
                    #region DEBUG
#if DEBUG
                    {
                        DebugInfo.lstDBG2.Add(bm.ToString() + "-->CheckPattern_vilochka ");

                        sW2.Stop();
                        strDebug = string.Empty + "\r\nCheckPattern_vilochka -" + sW2.Elapsed.Milliseconds.ToString();

                        sW2.Reset();
                        sW2.Start();
                        DebugInfo.textDBG = "CheckPattern2Move...";
                    }
#endif
                    #endregion
                }
                #region CheckPattern2Move проверяем ходы на два вперед на гарантированное окружение

                //List<Dot> ld_bm = CheckPattern2Move(pl2, true);
                //ld_bm.AddRange(CheckPatternVilka2x2(pl2, true));
                //ld_bm.AddRange(CheckPatternVilka2x2(pl2, false));
                //ld_bm.AddRange(CheckPattern2Move(pl1, true));
                ////ld_bm.AddRange(CheckPatternVilka2x2(pl1, true));
                ////ld_bm.AddRange(CheckPatternVilka2x2(pl1, false));
                //if (ld_bm.Count > 0)moves.AddRange(ld_bm);

                moves.AddRange(CheckPattern2Move(Player, true));
                moves.AddRange(CheckPatternVilka2x2(Player, true));
                moves.AddRange(CheckPatternVilka2x2(Player, false));
                moves.AddRange(CheckPattern2Move(Enemy, true));
                #endregion
                #region DEBUG
#if DEBUG
                sW2.Stop();
                strDebug = string.Empty + "\r\nCheckPattern2Move(pl2) -" + sW2.Elapsed.Milliseconds.ToString();
                sW2.Reset();
                sW2.Start();
                DebugInfo.textDBG = "CheckPatternVilkaNextMove...";
#endif
                #endregion
                #region CheckPatternVilkaNextMove
                //            bm = CheckPatternVilkaNextMove(pl2);
                //            if (DotIndexCheck(bm))
                //            {
                //                #region DEBUG
                //#if DEBUG
                //                {
                //                    lstDBG1.Add(bm.x + ":" + bm.y + " player" + pl2 + "CheckPatternVilkaNextMove " + iNumberPattern);
                //                }
                //#endif
                //                #endregion
                //                moves.Add(bm); //return bm;
                //            }
                #region DEBUG

#if DEBUG
                sW2.Stop();
                strDebug = string.Empty + "\r\nCheckPatternVilkaNextMove -" + sW2.Elapsed.Milliseconds.ToString();

                sW2.Reset();
                sW2.Start();
                DebugInfo.textDBG = "CheckPattern(pl2)...";
#endif
                #endregion
                #endregion
                #region CheckPattern
                //ld_bm = Проверка1(pl2);
                //if (ld_bm.Count > 0) moves.AddRange(ld_bm);
                #endregion
                #region CheckPatternMove
                moves.AddRange(CheckPatternMove(Player));
                moves.AddRange(CheckPatternMove(Enemy));

                //foreach (Dot dt in CheckPatternMove(pl2))
                //{
                //    moves.Add(dt);
                //    //if (CheckValidMove(dt) && (CheckDot(dt, pl2) == false)) moves.Add(dt);
                //}
                //foreach (Dot dt in CheckPatternMove(pl1))
                //{
                //    moves.Add(dt);
                //    //if (CheckValidMove(dt) && (CheckDot(dt, pl1) == false)) moves.Add(dt);
                //}

#if DEBUG
                sW2.Stop();
                strDebug = string.Empty + "/r/nCheckPatternMove(pl2) -" + sW2.Elapsed.Milliseconds.ToString();
                DebugInfo.textDBG1 = string.Empty;
                sW2.Reset();
#endif

                #endregion
                return moves.Distinct(new DotEq()).ToList();
            }

            //
            int counter_moves = 0;
            int res_last_move; //хранит результат хода
                               //int recursion_depth;
            const int MAX_RECURSION = 3;
            const int MAX_COUNTMOVES = 3;
            int recursion_depth;
            Dot tempmove;

            //===================================================================================================================
            /// <summary>
            /// возвращает Owner кто побеждает в результате хода
            /// </summary>
            /// <param name="Player">Human</param>
            /// <param name="player2">Computer</param>
            /// <returns></returns>
            private StateOwn Play(StateOwn Player)
            {
                StateOwn Enemy = Player == StateOwn.Human ? StateOwn.Computer : StateOwn.Human;

                List<Dot> lst_best_move = new List<Dot>();//сюда заносим лучшие ходы
                if (recursion_depth == 1) counter_moves = 1;
                recursion_depth++;
                counter_moves++;

                if (recursion_depth > MAX_RECURSION) return StateOwn.Empty;

                lst_best_move = BestMove(Player);
                foreach (Dot d in lst_best_move)
                {
                    d.Rating += counter_moves;
                }

                //tempmove = lst_best_move.Where(dt => (dt.iNumberPattern == 777 & dt.Rating == 1)
                //                                   || (dt.iNumberPattern == 666 & dt.Rating == lst_best_move.Min(d => d.Rating))).ElementAtOrDefault(0);
                tempmove = lst_best_move.Where(dt => (dt.iNumberPattern == 777)
                                       || (dt.iNumberPattern == 666 & dt.Rating == lst_best_move.Min(d => d.Rating))).ElementAtOrDefault(0);


                //если есть паттерн на окружение противника тоже устанавливается бест мув
                if (tempmove != null)
                {
                    Lst_branch.Add(tempmove);
                    return Player;
                }
                //если есть паттерн на свое окружение устанавливается бест мув
                tempmove = lst_best_move.Where(dt => dt.iNumberPattern == 666).FirstOrDefault();
                if (tempmove != null)
                {
                    Lst_branch.Add(tempmove);
                    Lst_branch.Last().Rating = tempmove.Rating + counter_moves;
                    return Enemy;
                }

                if (lst_best_move.Count > 0)
                {
                    #region Cycle
                    foreach (Dot move in lst_best_move.Where(dt => dt.Rating < 2))
                    {
                        #region ходим в проверяемые точки
                        if (counter_moves > MAX_COUNTMOVES) break;
                        //**************делаем ход***********************************
                        res_last_move = MakeMove(move, Player);
                        Lst_moves.Add(move);
                        counter_moves++;

                        #region проверка на окружение

                        if (Win_player == Player)
                        {
                            Dot dt_move = Lst_moves.First();
                            dt_move.Rating = counter_moves;
                            Lst_branch.Add(dt_move);
                            UndoMove(move);
                            //Win_player = 0;
                            continue;
                            //return StateOwn.Computer;
                        }
                        //если ход в заведомо окруженный регион - пропускаем такой ход
                        if (Win_player == Enemy)
                        {
                            UndoMove(move);

                            //Win_player = 0;
                            move.Rating = -1;
                            continue;
                        }
                        #endregion
                        #region Debug statistic
#if DEBUG
                        DebugInfo.lstDBG1.Add(move);//(move.Own + " -" + move.x + ":" + move.y);
                        DebugInfo.textDBG = "Ходов проверено: " + counter_moves +
                                           "\r\n проверка вокруг точки " + LastMove +
                                           "\r\n время поиска " + stopWatch.ElapsedMilliseconds;
#endif
                        #endregion
                        //теперь ходит другой игрок ===========================================================================

                        StateOwn result = Play(Enemy);

                        recursion_depth--;

                        if (result == 0)
                        {
                            Lst_moves.Remove(move);
#if DEBUG
                            DebugInfo.lstDBG1.Remove(move);
#endif
                            UndoMove(move);
                            continue;
                        }
                        else if (result == Player)
                        {
                            if (recursion_depth == 1)
                            {
                                Lst_moves[0].Rating = counter_moves;
                                Lst_branch.Add(Lst_moves[0]);
                            }

                            Lst_moves.Remove(move);
#if DEBUG
                            DebugInfo.lstDBG1.Remove(move);
#endif
                            UndoMove(move);
                            return result;
                        }
                        else if (result == Enemy)
                        {
                            if (recursion_depth == 2)
                            {
                                Lst_moves[0].Rating = counter_moves;//такой рейтинг устанавливается если выигрывает человек
                                Lst_branch.Add(Lst_moves[0]);
                            }
                            Lst_moves.Remove(move);
#if DEBUG
                            DebugInfo.lstDBG1.Remove(move);
#endif

                            UndoMove(move);
                            return result;
                        }


                        #region Debug
#if DEBUG
                        //remove from list
                        if (DebugInfo.lstDBG1.Count > 0) DebugInfo.lstDBG1.RemoveAt(DebugInfo.lstDBG1.Count - 1);
#endif
                        #endregion
                    }
                    #endregion
                }
                #endregion
                best_move = lst_best_move.Where(dt => dt.Rating == lst_best_move.Min(d => d.Rating)).ElementAtOrDefault(0);

                return StateOwn.Empty;
            }//----------------------------Play-----------------------------------------------------




            private float SquarePolygon(int nBlockedDots, int nRegionDots)
            {
                return nBlockedDots + nRegionDots / 2.0f - 1;//Формула Пика
            }
            public int pause { get; set; } = 10;

            private static void AddToList(List<Dot> ld, IEnumerable<Dot> pattern, int dx, int dy)
            {
                foreach (Dot dot in pattern)
                {
                    Dot d = new Dot(dot.X + dx, dot.Y + dy);
                    if (ld.Contains(d) == false) ld.Add(d);
                }
            }

            public class Pattern
            {
                public int PatternNumber { get; set; }

                public List<DotInPattern> DotsPattern { get; set; } = new List<DotInPattern>();

                public DotInPattern dXdY_ResultDot = new DotInPattern();
                public Dot FirstDot { get; set; }//Точка отсчета
                public Dot ResultDot => new Dot(FirstDot.X + dXdY_ResultDot.dX, FirstDot.Y + dXdY_ResultDot.dY, FirstDot.Own);

                public override string ToString() => "Pattern " + PatternNumber.ToString();

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



            public static class GameMessages
            {
                public static string Message { get; set; }

            }
            //IEnumerator and IEnumerable require these methods.
            public IEnumerator GetEnumerator()
            {
                position = -1;
                //return this;
                return Dots.GetEnumerator();
            }
            //IEnumerator
            public bool MoveNext()
            {
                position++;
                return (position < Dots.Count);
            }
            //IEnumerable
            public void Reset()
            { position = 0; }

            //public IScore GetScore()
            //{
            //    throw new NotImplementedException();
            //}

            //public State GetSpaceState(int row, int column)
            //{
            //    throw new NotImplementedException();
            //}

            //public bool IsValidMove(Dot move)
            //{
            //    return move.ValidMove; 
            //}

            //public bool IsValidMove(int row, int column)
            //{
            //    return this[row,column].ValidMove; ;
            //}

            public bool IsGameOver => Board_ValidMoves.Count == 0;

            //public IAsyncOperation<int> MoveAsync(int Player, CancellationToken? cancellationToken, Dot pl_move = null)
            //{
            //    // Use a lock to prevent the ResetAsync method from modifying the game 
            //    // state at the same time that a different thread is in this method.
            //    //lock (_lockObject)
            //    //{
            //    return Move(Player, cancellationToken)
            //}

            //public async Task<int> MoveAsync(int player, CancellationToken? cancellationToken, Dot pl_move = null)
            //{
            //    int result = await Move(player, cancellationToken, pl_move);
            //    return result;
            //}

            public Task<int> MovePlayerAsync(StateOwn Player, CancellationToken? cancellationToken, Dot pl_move = null)
            {
                var tcs = new TaskCompletionSource<int>();
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        tcs.SetResult(MovePlayer(Player, cancellationToken, pl_move));
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                });
                return tcs.Task;
            }


            //CancellationToken ct;

            /// <summary>
            /// Ход игрока. Возвращает 0 - успешно 
            /// 1 - игра окончена
            /// -1 - ошибка
            /// </summary>
            /// <param name="Player"></param>
            /// <param name="cancellationToken"></param>
            /// <param name="pl_move"></param>
            /// <returns>0 - успешно 
            /// 1 - игра окончена
            /// -1 - ошибка
            /// </returns>
            public int MovePlayer(StateOwn Player, CancellationToken? cancellationToken, Dot pl_move = null)
            {
                if (pl_move == null)
                {
                    pl_move = PickComputerMove(LastMove, cancellationToken);
                }
                if (MakeMove(pl_move, Player, addForDraw: true) == -1)
                {
                    return -1;
                }

                if (IsGameOver)
                {
                    return 1;
                }
                return 0;
            }
            public string Statistic()
            {
                var q5 = from Dot d in Dots where d.Own == StateOwn.Human select d;
                var q6 = from Dot d in Dots where d.Own == StateOwn.Computer select d;
                var q7 = from Dot d in Dots where d.Own == StateOwn.Human & d.Blocked select d;
                var q8 = from Dot d in Dots where d.Own == StateOwn.Computer & d.Blocked select d;
                return q8.Count().ToString() + ":" + q7.Count().ToString();
            }

            public int MovePlayer(Dot pl_move)
            {
                if (MakeMove(pl_move, addForDraw: true) == -1)
                {
                    return -1;
                }

                if (IsGameOver)
                {
                    return 1;
                }
                return 0;
            }

            public object Current => Dots[position];

            public List<Dot> Lst_blocked_dots { get; set; } = new List<Dot>();
            public List<Dot> Lst_in_region_dots { get; set; } = new List<Dot>();
            public List<Dot> Lst_moves { get; set; } = new List<Dot>();
            public List<Dot> Lst_branch { get; set; } = new List<Dot>();
            /// <summary>
            /// Устанавливается в функции CheckBlocked, во время проверки окружения точек
            /// </summary>
            public StateOwn Win_player { get; set; }
        }
    }
    

}
