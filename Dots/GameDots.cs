using DotsGame;
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
        public static string strBestMove = string.Empty;
        public static string textDBG1 = string.Empty;
        public static List<string> lstDBG1 = new List<string>();
        public static List<string> lstDBG2 = new List<string>();
        private static string s_stringMSG = string.Empty;
        public static IProgress<string> Progress { get; set; }
        public static string StringMSG
        {
            get => s_stringMSG;
            set
            {
                s_stringMSG = value;
                DebugMSGEventArgs e = new DebugMSGEventArgs();
                e.Message = s_stringMSG;
                OnRaiseDebugMSGEvent(e);
            }
        }
        public class DebugMSGEventArgs : EventArgs
        {
            public DebugMSGEventArgs()
            {
                Message = StringMSG;
            }
            public string Message { get; set; }
        }

        public static event EventHandler<DebugMSGEventArgs> RaiseDebugMSGEvent;

        public static void OnRaiseDebugMSGEvent(DebugMSGEventArgs e)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            EventHandler<DebugMSGEventArgs> handler = RaiseDebugMSGEvent;

            // Event will be null if there are no subscribers
            if (handler != null)
            {
                e.Message = s_stringMSG;
                // Use the () operator to raise the event.
                handler(StringMSG, e);
            }
        }
    }
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
            private const StateOwn COMPUTER = StateOwn.Computer;
            private const StateOwn NONE = StateOwn.Empty;
            private const StateOwn HUMAN = StateOwn.Human;

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
            /// Список ходов. Точки не участвуют в отрисовке, заносятся все ходы
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
        /// <summary>
        /// Показывает, чей ход
        /// </summary>
        public StateMove MoveState { get; set; }
        /// <summary>
        /// Получает независимую копию точки
        /// </summary>
        /// <param name="DotForCopy"></param>
        /// <returns></returns>
        public Dot GetDotCopy(Dot DotForCopy)
            {
                Dot d = new Dot(x: DotForCopy.X, 
                                y: DotForCopy.Y, 
                                Owner: DotForCopy.Own, 
                                NumberPattern: DotForCopy.NumberPattern,
                                Rating: DotForCopy.Rating,
                                Tag: DotForCopy.Tag)
                {
                    Blocked = DotForCopy.Blocked,
                    BlokingDots = DotForCopy.BlokingDots,
                    BonusDot = DotForCopy.BonusDot,
                    Fixed = DotForCopy.Fixed,
                    IndexDot = DotForCopy.IndexDot,
                    IndexRelation = DotForCopy.IndexRelation,
                };
                return d;
            }

        /// <summary>
        /// Возвращает список не занятых точек
        /// </summary>
        //private List<Dot> Board_ValidMoves => Dots.Where(d => d.Own == 0 && !d.Blocked).ToList();
        //private List<Dot> Board_NotEmptyNonBlockedDots => Dots.Where(d => d.Own != 0 && !d.Blocked).ToList();
        //private List<Dot> Board_AllNotBlockedDots => Dots.Where(d => !d.Blocked).ToList();

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
            ListLinks = new List<Link>();
            //ListMoves = new List<Dot>();
            StackMoves = new List<Dot>();
            dots_in_region = new List<Dot>();
            GoalPlayer = new Goal();
        }

        public void ListMoves_OnAdd(object sender, ListDotsEventArgs e)
            {
                UpdateDotsInList(ListMoves);
            }
            public void ListMoves_OnRemove(object sender, ListDotsEventArgs e)
            {
                //UpdateDotsInListMoves();
            }
            private void UpdateDotsInList(List<Dot>LstDots)
            {
                for (int i = 0; i < LstDots.Count; i++)
                {
                LstDots[i].Blocked = Dots.Find(d => d.IndexDot == LstDots[i].IndexDot).Blocked;
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
                //return (d1.X == d2.X & d1.Y == d2.Y & d1.Rating == d2.Rating);
                return d1.X == d2.X && d1.Y == d2.Y;
                }
            }
        class ChainsComparer : IEqualityComparer<Chain>
        {
            public bool Equals(Chain ch1, Chain ch2)
            {
                if (ch1.Dot1 == ch2.Dot1 && ch1.Dot2 == ch2.Dot2 ||
                   ch1.Dot1 == ch2.Dot2 && ch2.Dot1 == ch1.Dot2) return true;
                return false;
//                return ch1.Equals(ch2);
            }

            // If Equals() returns true for a pair of objects 
            // then GetHashCode() must return the same value for these objects.

            public int GetHashCode(Chain ch)
            {
                if (ReferenceEquals(ch, null)) return 0;
                int hashDot1 = ch.Dot1.GetHashCode();
                int hashDot2 = ch.Dot2.GetHashCode();
                //int hashDot3 = ch.DotE.GetHashCode();
                //Calculate the hash code for the product.
                return hashDot1 * hashDot2;
            }

        }
        class Chains5DotsComparer : IEqualityComparer<Chain5Dots>
        {
            public bool Equals(Chain5Dots ch1, Chain5Dots ch2)
            {
                if (ch1.Dot1 == ch2.Dot1 && ch1.Dot2 == ch2.Dot2 ||
                   ch1.Dot1 == ch2.Dot2 && ch2.Dot1 == ch1.Dot2) return true;
                return false;
                //                return ch1.Equals(ch2);
            }

            // If Equals() returns true for a pair of objects 
            // then GetHashCode() must return the same value for these objects.

            public int GetHashCode(Chain5Dots ch)
            {
                if (ReferenceEquals(ch, null)) return 0;
                int hashDot1 = ch.Dot1.GetHashCode();
                int hashDot2 = ch.Dot2.GetHashCode();
                //int hashDot3 = ch.DotE.GetHashCode();
                //Calculate the hash code for the product.
                return hashDot1 * hashDot2;
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
                    Dots[ind].NumberPattern = dot.NumberPattern;
                    if (dot.Own != 0) Dots[ind].IndexRelation = Dots[ind].IndexDot;
                    Dots[ind].Blocked = false;
                    if (dot.X == 0 | dot.X == (BoardWidth - 1) | dot.Y == 0 |
                        dot.Y == (BoardHeight - 1)) Dots[ind].Fixed = true;
                    AddNeibor(Dots[ind]);
                }
            }
        /// <summary>
        /// Добавляет точки в список NeiborDots для соседних точек и связывает их MakeIndexRelation
        /// </summary>
        /// <param name="dot"></param>
        private void AddNeibor(Dot dot)
            {
                //выбрать соседние точки, если такие есть
                IEnumerable<Dot> q = from Dot d in Dots where d.Own == dot.Own & Distance(dot, d) < 2 select d;

                foreach (Dot d in q)
                {
                    if (d != dot)
                    {
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
        /// <summary>
        /// Расстояние между точками
        /// </summary>
        /// <param name="dot1"></param>
        /// <param name="dot2"></param>
        /// <returns></returns>
        public float Distance(Dot dot1, Dot dot2)
            {
                return (float)Math.Round(Math.Sqrt(Math.Pow((dot1.X - dot2.X), 2) + Math.Pow((dot1.Y - dot2.Y), 2)), 1);
            }
            /// <summary>
            /// возвращает список соседних точек заданной точки SNWE -S -South, N -North, W -West, E -East
            /// </summary>
            /// <param name="dot"> точка Dot из массива точек типа ArrayDots </param>
            /// <returns> список точек </returns>
            private List<Dot> NeighborDotsSNWE(Dot dot) => (from d in Dots where Distance(dot, d) == 1 select d).ToList();
        /// <summary>
        /// возвращает список всех соседних точек заданной точки
        /// </summary>
        /// <param name="dot"> точка Dot из массива точек типа ArrayDots </param>
        /// <returns> список точек </returns>
        private List<Dot> NeighborDots(Dot dot) => (from d in Dots where Distance(dot, d) < 2 select d).ToList();

        /// <summary>
        /// возвращает список всех пустых соседних точек заданной точки
        /// </summary>
        /// <param name="dot"> точка Dot из массива точек типа ArrayDots </param>
        /// <returns> список точек </returns>
        private List<Dot> NeighborEmptyDots(Dot dot) => 
            (from d in Dots where Distance(dot, d) < 2 && d.Own==StateOwn.Empty select d).ToList();

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
                    d.NumberPattern = 0;
                    d.IndexRelation = d.IndexDot;
                    d.StateDot = StateDotInPattern.Normal;
                }
                ListLinks.Clear();
                ListMoves.Clear();
                StackMoves.Clear();
            }

            private int Counter = 0;
            private Dot DotChecked;
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

        /// <summary>
        /// отмена хода
        /// </summary>
        /// <param name="dot"></param>
    public void UndoMove(Dot dot, bool full = false)
    {
        if(full)//если полная отмена
            {
                ListMoves.Remove(dot);
                for (int i = 0; i < Dots.Count; i++)
                {
                    Dots[i].Restore();
                }
                for (int i = 0; i < ListMoves.Count; i++)
                {
                    MakeMove(ListMoves[i], ListMoves[i].Own);
                }
                LinkDots();
                //восстанавливаем стек
                StackMoves.Clear();
                StackMoves.AddRange(ListMoves);
                //обновляем точки на предмет блокировки
                UpdateDotsInList(ListMoves);
            }
        else //если отмена во время просчета ходов
            {
                StackMoves.Remove(dot);
                List<Dot> StackMoveCopy = new List<Dot>();//создаем рабочуюю копию стека
                foreach (Dot d in StackMoves) StackMoveCopy.Add(GetDotCopy(d));
                StackMoves.Clear();//очищаем стек, в MakeMove он заполняется заново
                //сброс игрового поля
                for (int i = 0; i < Dots.Count; i++)
                {
                    Dots[i].Restore();
                }
                //перестраиваем поле заново
                for (int i = 0; i < StackMoveCopy.Count; i++)
                {
                    MakeMove(StackMoveCopy[i], StackMoveCopy[i].Own);
                }
            }
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
        /// Ход игрока 
        /// </summary>
        /// <param name="dot"> точка куда делается ход </param>
        /// <param name="Owner"> владелец точки -целое 1-Игрок или 2 -Компьютер</param>
        /// <returns>количество окруженных точек, -1 если недопустимый ход; </returns>
        private int MakeMove(Dot dot, StateOwn Owner = 0, bool addForDraw = false)//
        {
            int Count_blocked_before_Human; int Count_blocked_after_Human;
            int Count_blocked_before_Comp; int Count_blocked_after_Comp;
            GoalPlayer.Player = 0;
            GoalPlayer.CountBlocked = 0;
            Count_blocked_before_Human = (from Dot d in this where d.Own == StateOwn.Human && d.Blocked == true select d).Count();
            Count_blocked_before_Comp = (from Dot d in this where d.Own == StateOwn.Computer && d.Blocked == true select d).Count();
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
            Count_blocked_after_Human = (from Dot d in this where d.Own == StateOwn.Human && d.Blocked == true select d).Count();
            Count_blocked_after_Comp = (from Dot d in this where d.Own == StateOwn.Computer && d.Blocked == true select d).Count();

            StackMoves.Add(Dots[IndexDot(dot.X, dot.Y)]);

            if (addForDraw)
            {
                ListMoves.Add(GetDotCopy(Dots[IndexDot(dot.X, dot.Y)]));
                LinkDots();//перестроить связи точек
            }
            int result_Human = Count_blocked_after_Human - Count_blocked_before_Human;
            int result_Comp = Count_blocked_after_Comp - Count_blocked_before_Comp;
            if (result_Human != 0)
            {
                GoalPlayer.Player = StateOwn.Computer;
                GoalPlayer.CountBlocked = result_Human;
            }
            if (result_Comp != 0)
            {
                GoalPlayer.Player = StateOwn.Human;
                GoalPlayer.CountBlocked = result_Comp;
            }

            return result_Human + result_Comp;
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
            public List<Link> ListLinks { get; private set; }
            //public List<Links> ListLinksForDrawing { get; private set; } = new List<Links>();

            /// <summary>
            /// устанавливает связь между двумя точками и возвращает массив связей 
            /// </summary>
            private void LinkDots()
            {
                ListLinks.Clear();
                ListLinks = (from Dot d1 in ListMoves
                             where d1.BlokingDots.Count > 0
                             from Dot d2 in ListMoves
                             where d2.Own == d1.Own && d1.Blocked == d2.Blocked
                             && d2.BlokingDots.Count > 0 && Distance(d1, d2) > 0 && Distance(d1, d2) < 2
                             select new Link(d1, d2)).Distinct(new LinksComparer()).ToList(); //обновляем основной массив связей - lnks              
            }

            /// <summary>
            /// функция проверяет не делается ли ход в точку, которая на следующем ходу будет окружена
            /// </summary>
            /// <param name="dot"></param>
            /// <param name="arrDots"></param>
            /// <param name="Player"></param>
            /// <returns></returns>
            private bool CheckDots(Dot dot, StateOwn Player)
            {
                int res = MakeMove(dot, Player);
                StateOwn pl = Player == StateOwn.Computer ? StateOwn.Human : StateOwn.Computer;
                if (GoalPlayer.Player == pl)// первое условие -ход в уже окруженный регион, 
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
            private Dot CommonDotSNWE(Dot d1, Dot d2)//*1d1* 
            {
                return NeighborDotsSNWE(d1).Intersect(NeighborDotsSNWE(d2), new DotEq()).FirstOrDefault();
            }
            private List<Dot> CommonDots(Dot d1, Dot d2)
            {
                return NeighborDots(d1).Intersect(NeighborDots(d2), new DotEq()).ToList();
            }
        private List<Dot> CommonEmptyDots(Dot d1, Dot d2)
        {
            return NeighborDots(d1).Intersect(NeighborDots(d2), new DotEq()).Where(d=>d.Own==0).ToList();
        }
        /// <summary>
        /// Получить все незаблокированные точки, или все заблокированные конкретного игрока
        /// </summary>
        /// <param name="Owner"></param>
        /// <param name="Blocked"></param>
        /// <returns></returns>
        private List<Dot> GetDots(StateOwn Owner, bool Blocked = false) =>
    Dots.Where(d => d.Blocked == Blocked && d.Own == Owner).ToList();
        /// <summary>
        /// Получить все незаблокированные точки, или все заблокированные занятые или нет игроками
        /// </summary>
        /// <param name="Blocked"></param>
        /// <returns></returns>
        private List<Dot> GetDots(bool NoEmpty, bool Blocked = false)
        {
            if(NoEmpty) return Dots.Where(d => d.Blocked == Blocked && d.Own != 0).ToList();
            else return Dots.Where(d => d.Blocked == Blocked).ToList();
        }

        /// <summary>
        /// Получить все незаблокированные точки, или все заблокированные
        /// </summary>
        /// <param name="Blocked"></param>
        /// <returns></returns>
        private List<Dot> GetDots(bool Blocked = false) =>
    Dots.Where(d => d.Blocked == Blocked).ToList();



        private List<Dot> CommonDot2(Dot d1, Dot d2)
        {
            return NeighborDots(d1).Intersect(NeighborDots(d2), new DotEq()).ToList();
        }

        //==============================================================================================
        /// <summary>
        /// проверяет ход в результате которого окружение.
        /// </summary>
        /// <param name="Owner">владелец проверяемых точек</param>
        /// <returns>Возвращает ход(точку) который завершает окружение</returns>
        private Dot CheckMove(StateOwn Owner, Goal _Goal=null)
        {
            GameDots GameDots_Copy = GetGameDotsCopy(ListMoves);
            List<Dot> happy_dots = new List<Dot>();
            var qry = from Dot d1 in GameDots_Copy.GetDots(Owner)
                      where d1.Own == Owner
                      from Dot d2 in GameDots_Copy.GetDots(Owner)
                      where
                            d2.IndexRelation == d1.IndexRelation
                            && Distance(d1, d2) > 2
                            && Distance(d1, d2) < 3
                            && GameDots_Copy.CommonDots(d1, d2).Where(dt => dt.Own == Owner).Count() == 0
                            ||
                            d2.IndexRelation == d1.IndexRelation
                            && Distance(d1, d2) == 2
                      from Dot d in GameDots_Copy.GetDots(StateOwn.Empty) 
                      where Distance(d, d1) < 2 && Distance(d, d2) < 2
                                && GameDots_Copy.NeighborDotsSNWE(d).Where(dt => dt.Own == Owner).Count() <= 2
                      select d;

            foreach (Dot d in qry.Distinct(new DotEq()).ToList())
            {
                //делаем ход
                GameDots_Copy.MakeMove(d, Owner);
                if (GameDots_Copy.GoalPlayer.Player == Owner)
                {
                    happy_dots.Add(new Dot(d.X, d.Y, d.Own, 777, GameDots_Copy.GoalPlayer.CountBlocked));
                }
                GameDots_Copy.UndoMove(d);

            }
            //выбрать точку, которая максимально окружит
            Dot result = happy_dots.Distinct(new DotEq()).Where(dt =>
             dt.Rating == happy_dots.Max(d => d.Rating)).FirstOrDefault();
            if (result!=null && _Goal != null)
            {
                _Goal.Player = Owner;
                _Goal.CountBlocked = result.Rating;
                result.Rating = 0;
            }
            GameDots_Copy = null;
            return result;
        }

            private Dot CheckPatternVilkaNextMove(StateOwn Owner)
            {
            GameDots GameDots_Copy = GetGameDotsCopy(ListMoves);

            IEnumerable<Dot> qry = GameDots_Copy.GetDots(Owner);//Board_NotEmptyNonBlockedDots.Where(dt => dt.Own == Owner);
                Dot dot_ptn;
                if (qry.Count() != 0)
                {
                    foreach (Dot d in qry)
                    {
                        foreach (Dot dot_move in GameDots_Copy.NeighborDots(d).Where(v=>v.Own==StateOwn.Empty))
                        {
                                //делаем ход
                                int result_last_move = GameDots_Copy.MakeMove(dot_move, Owner);
                                StateOwn pl = Owner == StateOwn.Computer ? StateOwn.Human : StateOwn.Computer;
                                Dot dt = GameDots_Copy.CheckMove(pl); // проверка чтобы не попасть в капкан
                                if (dt != null)
                                {
                                    GameDots_Copy.UndoMove(dot_move);
                                    continue;
                                }
                                dot_ptn = GameDots_Copy.CheckPattern_vilochka(d.Own);
                                //-----------------------------------
                                if (dot_ptn != null & result_last_move == 0)
                                {
                                    GameDots_Copy.UndoMove(dot_move);
                                    return dot_move;
                                }
                                GameDots_Copy.UndoMove(dot_move);
                        }
                    }
                }
            GameDots_Copy = null;
            return null;
            }

        private GameDots GetGameDotsCopy(List <Dot> LstMoves)
        {
            GameDots GD = new GameDots(BoardWidth,BoardHeight);
            for (int i = 0; i < LstMoves.Count; i++)
            {
                Dot d = new Dot(ListMoves[i]);
                GD.MovePlayer(d);
            }
            return GD;
        }

        private int iNumberPattern;

            private List<Dot> CheckPattern(StateOwn Owner)
            {
            StateOwn Enemy = Owner == StateOwn.Human ? StateOwn.Computer : StateOwn.Human;
            List<Dot> ld = new List<Dot>();
            IEnumerable<Dot> get_non_blocked = from Dot d in this where d.Blocked == false select d; //получить коллекцию незаблокированных точек
            //
            // +  m  +
            // -  -
            IEnumerable<Dot> pat = from Dot dot0 in get_non_blocked
                  where dot0.Own == Enemy
                  from Dot dot1 in get_non_blocked
                  where dot1.Own == Enemy && Distance(dot1, dot0) == 1.0f
                  from Dot dot2 in get_non_blocked
                  where dot2.Own == Owner && Distance(dot2, dot1) == 1.4f
                  from Dot dot3 in get_non_blocked
                  where dot3.Own == Owner && Distance(dot3, dot2) == 2.0f
                  from Dot dot4 in get_non_blocked
                  where dot4.Own == StateOwn.Empty && Distance(dot4, dot3) == 1.0f
                  from Dot dot5 in get_non_blocked
                  where dot5.Own == StateOwn.Empty && Distance(dot5, dot4) == 2.8f
                  from Dot dot6 in get_non_blocked
                  where dot6.Own == StateOwn.Empty && Distance(dot6, dot5) == 1.0f
                  from Dot dot7 in get_non_blocked
                  where dot7.Own == StateOwn.Empty && Distance(dot7, dot6) == 1.0f
                                                   && Distance(dot7, dot0) == 2.8f
                  from Dot move in get_non_blocked
                  where move.Own == StateOwn.Empty
                    && Distance(dot0, move) == 1.4f
                    && Distance(dot1, move) == 1.0f
                    && Distance(dot2, move) == 1.0f
                    && Distance(dot3, move) == 1.0f
                    && Distance(dot4, move) == 1.4f
                    && Distance(dot5, move) == 1.4f
                    && Distance(dot6, move) == 1.0f
                    && Distance(dot7, move) == 1.4f
                  select new Dot(move.X, move.Y, NumberPattern: 1, Rating: 5, Tag: $"CheckPattern({Owner})");
                  ld.AddRange(pat.Distinct(new DotEq()));

            //     m  +
            //  +  -  -
//            pat = from Dot dot0 in get_non_blocked
//                  where dot0.Own == Enemy
//                  from Dot dot1 in get_non_blocked
//                  where dot1.Own == Enemy && Distance(dot1, dot0) == 1.0f
//                  from Dot dot2 in get_non_blocked
//                  where dot2.Own == Owner && Distance(dot2, dot1) == 2.0f
//                  from Dot dot3 in get_non_blocked
//                  where dot3.Own == Owner && Distance(dot3, dot2) == 2.2f
//                  from Dot dot4 in get_non_blocked
//                  where dot4.Own == StateOwn.Empty && Distance(dot4, dot3) == 2.0f
//&& Distance(dot4, dot0) == 1.4f
//                  from Dot move in get_non_blocked
//                  where move.Own == StateOwn.Empty
//&& Distance(dot0, move) == 1.0f
//&& Distance(dot1, move) == 1.4f
//&& Distance(dot2, move) == 1.4f
//&& Distance(dot3, move) == 1.0f
//&& Distance(dot4, move) == 1.0f
//            select new Dot(move.X, move.Y, NumberPattern: 2, Rating: 3, Tag: $"CheckPattern({Owner})"); 
//            ld.AddRange(pat.Distinct(new DotEq()));
//    m
// +  -  +
//    -
            pat = from Dot dot0 in get_non_blocked
                  where dot0.Own == Enemy
                  from Dot dot1 in get_non_blocked
                  where dot1.Own == Enemy && Distance(dot1, dot0) == 1.0f
                  from Dot dot2 in get_non_blocked
                  where dot2.Own == Owner && Distance(dot2, dot1) == 1.0f
                  from Dot dot3 in get_non_blocked
                  where dot3.Own == Owner && Distance(dot3, dot2) == 2.0f
&& Distance(dot3, dot0) == 1.4f
                  from Dot move in get_non_blocked
                  where move.Own == StateOwn.Empty
&& Distance(dot0, move) == 2.0f
&& Distance(dot1, move) == 1.0f
&& Distance(dot2, move) == 1.4f
&& Distance(dot3, move) == 1.4f
                  select new Dot(move.X, move.Y, NumberPattern: 3, Rating: 5, Tag: $"CheckPattern({Owner})");
                 ld.AddRange(pat.Distinct(new DotEq()));
            //         +index relation
            //      -
            // ir+     +m
            pat = from Dot dot0 in get_non_blocked
                  where dot0.Own == Enemy
                  from Dot dot1 in get_non_blocked
                  where dot1.Own == Enemy && Distance(dot1, dot0) == 2.8f 
                  from Dot dot2 in get_non_blocked
                  where dot2.Own == Owner && Distance(dot2, dot1) == 1.4f && dot1.IndexRelation == dot2.IndexRelation
                  from Dot dot3 in get_non_blocked
                  where dot3.Own == StateOwn.Empty && Distance(dot3, dot2) == 1.0f
                  from Dot dot4 in get_non_blocked
                  where dot4.Own == StateOwn.Empty && Distance(dot4, dot3) == 1.4f
                  from Dot dot5 in get_non_blocked
                  where dot5.Own == StateOwn.Empty && Distance(dot5, dot4) == 1.0f
                  from Dot dot6 in get_non_blocked
                  where dot6.Own == StateOwn.Empty && Distance(dot6, dot5) == 2.8f
&& Distance(dot6, dot0) == 3.2f
                  from Dot move in get_non_blocked
                  where move.Own == StateOwn.Empty
&& Distance(dot0, move) == 2.0f
&& Distance(dot1, move) == 2.0f
&& Distance(dot2, move) == 1.4f
&& Distance(dot3, move) == 1.0f
&& Distance(dot4, move) == 1.0f
&& Distance(dot5, move) == 1.4f
&& Distance(dot6, move) == 1.4f
||    
                  dot0.Own == Enemy &&
                  dot1.Own == Owner && Distance(dot1, dot0) == 1.4f
                  && dot2.Own == Owner && Distance(dot2, dot1) == 2.8f 
                  && dot3.Own == StateOwn.Empty && Distance(dot3, dot2) == 1.0f
                  && dot4.Own == StateOwn.Empty && Distance(dot4, dot3) == 1.4f
                  && dot5.Own == StateOwn.Empty && Distance(dot5, dot4) == 1.0f
                  && dot6.Own == StateOwn.Empty && Distance(dot6, dot5) == 2.8f
                  && Distance(dot6, dot0) == 2.0f 
                  && move.Own == StateOwn.Empty
                    && Distance(dot0, move) == 1.4f
                    && Distance(dot1, move) == 2.0f
                    && Distance(dot2, move) == 2.0f
                    && Distance(dot3, move) == 1.0f
                    && Distance(dot4, move) == 1.0f
                    && Distance(dot5, move) == 1.4f
                    && Distance(dot6, move) == 1.4f
                  select new Dot(move.X, move.Y, NumberPattern: 4, Rating: 1, Tag: $"CheckPattern({ Owner})");
                  ld.AddRange(pat.Distinct(new DotEq()));
            //
            //  -   +
            //  +   m
            pat = from Dot dot0 in get_non_blocked
                  where dot0.Own == Enemy
                  from Dot dot1 in get_non_blocked
                  where dot1.Own == Owner && Distance(dot1, dot0) == 1.0f
                  from Dot dot2 in get_non_blocked
                  where dot2.Own == Owner && Distance(dot2, dot1) == 1.4f
                  from Dot dot3 in get_non_blocked
                  where dot3.Own == StateOwn.Empty && Distance(dot3, dot2) == 2.2f
                  from Dot dot4 in get_non_blocked
                  where dot4.Own == StateOwn.Empty && Distance(dot4, dot3) == 2.8f
                  from Dot dot5 in get_non_blocked
                  where dot5.Own == StateOwn.Empty && Distance(dot5, dot4) == 2.2f
                  from Dot dot6 in get_non_blocked
                  where dot6.Own == StateOwn.Empty && Distance(dot6, dot5) == 1.4f
&& Distance(dot6, dot0) == 1.0f
                  from Dot move in get_non_blocked
                  where move.Own == StateOwn.Empty
&& Distance(dot0, move) == 1.4f
&& Distance(dot1, move) == 1.0f
&& Distance(dot2, move) == 1.0f
&& Distance(dot3, move) == 2.0f
&& Distance(dot4, move) == 2.0f
&& Distance(dot5, move) == 2.2f
&& Distance(dot6, move) == 2.2f
                  select new Dot(move.X, move.Y, NumberPattern: 5, Rating: 9, Tag: $"CheckPattern({ Owner})");
ld.AddRange(pat);
            //  -   +
            // -m   +
            pat = from Dot dot0 in get_non_blocked
                  where dot0.Own == Enemy
                  from Dot dot1 in get_non_blocked
                  where dot1.Own == Owner && Distance(dot1, dot0) == 1.0f
                  from Dot dot2 in get_non_blocked
                  where dot2.Own == Owner && Distance(dot2, dot1) == 1.0f
&& Distance(dot2, dot0) == 1.4f
                  from Dot move in get_non_blocked
                  where move.Own == StateOwn.Empty
&& Distance(dot0, move) == 1.0f
&& Distance(dot1, move) == 1.4f
&& Distance(dot2, move) == 1.0f
                  select new Dot(move.X, move.Y, NumberPattern: 6, Rating: 9, Tag: $"CheckPattern({ Owner })");
                  ld.AddRange(pat.Distinct(new DotEq()));
            //  -   +
            //  -   +
            //  m    -
            pat = from Dot dot0 in get_non_blocked
                  where dot0.Own == Enemy
                  from Dot dot1 in get_non_blocked
                  where dot1.Own == Enemy && Distance(dot1, dot0) == 1.0f
                  from Dot dot2 in get_non_blocked
                  where dot2.Own == Owner && Distance(dot2, dot1) == 1.4f
                  from Dot dot3 in get_non_blocked
                  where dot3.Own == Owner && Distance(dot3, dot2) == 1.0f
                  from Dot dot4 in get_non_blocked
                  where dot4.Own == Owner && Distance(dot4, dot3) == 1.4f
&& Distance(dot4, dot0) == 2.0f
                  from Dot move in get_non_blocked
                  where move.Own == StateOwn.Empty
&& Distance(dot0, move) == 2.2f
&& Distance(dot1, move) == 1.4f
&& Distance(dot2, move) == 2.0f
&& Distance(dot3, move) == 1.0f
&& Distance(dot4, move) == 1.0f
                  select new Dot(move.X, move.Y, NumberPattern: 7, Rating: 9, Tag: $"CheckPattern({Owner})");
ld.AddRange(pat.Distinct(new DotEq()));

            //       -   +
            //  -m   +   -
            //
            pat = from Dot dot0 in get_non_blocked
                  where dot0.Own == Enemy
                  from Dot dot1 in get_non_blocked
                  where dot1.Own == Enemy && Distance(dot1, dot0) == 1.4f
                  from Dot dot2 in get_non_blocked
                  where dot2.Own == Owner && Distance(dot2, dot1) == 1.0f
                  from Dot dot3 in get_non_blocked
                  where dot3.Own == Owner && Distance(dot3, dot2) == 1.4f
                  from Dot dot4 in get_non_blocked
                  where dot4.Own == StateOwn.Empty && Distance(dot4, dot3) == 1.4f
                  from Dot dot5 in get_non_blocked
                  where dot5.Own == StateOwn.Empty && Distance(dot5, dot4) == 2.2f
&& Distance(dot5, dot0) == 2.0f
                  from Dot move in get_non_blocked
                  where move.Own == StateOwn.Empty
&& Distance(dot0, move) == 1.4f
&& Distance(dot1, move) == 2.0f
&& Distance(dot2, move) == 2.2f
&& Distance(dot3, move) == 1.0f
&& Distance(dot4, move) == 1.0f
&& Distance(dot5, move) == 1.4f
                  select new Dot(move.X, move.Y, NumberPattern: 8, Rating: 5, Tag: $"CheckPattern({Owner})");
            ld.AddRange(pat.Distinct(new DotEq()));

            //      +
            //  m+  -
            //      +
            pat = from Dot dot0 in get_non_blocked
                  where dot0.Own == Enemy
                  from Dot dot1 in get_non_blocked
                  where dot1.Own == Enemy && Distance(dot1, dot0) == 2.0f
                  from Dot dot2 in get_non_blocked
                  where dot2.Own == Owner && Distance(dot2, dot1) == 1.0f
&& Distance(dot2, dot0) == 1.0f
                  from Dot move in get_non_blocked
                  where move.Own == StateOwn.Empty
&& Distance(dot0, move) == 1.4f
&& Distance(dot1, move) == 1.4f
&& Distance(dot2, move) == 1.0f
                  select new Dot(move.X, move.Y, NumberPattern: 9, Rating: 2, Tag: $"CheckPattern({Owner})");
            ld.AddRange(pat.Distinct(new DotEq()));
            //     - 
            //  -  +  -m  +
            //      
            pat = from Dot dot0 in get_non_blocked
                  where dot0.Own == Enemy
                  from Dot dot1 in get_non_blocked
                  where dot1.Own == Enemy && Distance(dot1, dot0) == 1.4f
                  from Dot dot2 in get_non_blocked
                  where dot2.Own == Enemy && Distance(dot2, dot1) == 1.0f
                  from Dot dot3 in get_non_blocked
                  where dot3.Own == Owner && Distance(dot3, dot2) == 1.4f
                  from Dot dot4 in get_non_blocked
                  where dot4.Own == Owner && Distance(dot4, dot3) == 2.0f
                    && Distance(dot4, dot0) == 3.0f
                  from Dot move in get_non_blocked
                  where move.Own == StateOwn.Empty
&& Distance(dot0, move) == 2.0f
&& Distance(dot1, move) == 1.4f
&& Distance(dot2, move) == 1.0f
&& Distance(dot3, move) == 1.0f
&& Distance(dot4, move) == 1.0f
                  select new Dot(move.X, move.Y, NumberPattern: 10, Rating: 3, Tag: $"CheckPattern({Owner})");
            ld.AddRange(pat.Distinct(new DotEq()));

            //  -   +
            //  -   +
            // m+   -
            pat = from Dot dot0 in get_non_blocked
                  where dot0.Own == Enemy
                  from Dot dot1 in get_non_blocked
                  where dot1.Own == Enemy && Distance(dot1, dot0) == 2.2f
                  from Dot dot2 in get_non_blocked
                  where dot2.Own == Owner && Distance(dot2, dot1) == 1.4f
                  from Dot dot3 in get_non_blocked
                  where dot3.Own == Owner && Distance(dot3, dot2) == 1.0f
                  from Dot dot4 in get_non_blocked
                  where dot4.Own == Owner && Distance(dot4, dot3) == 2.2f
&& Distance(dot4, dot0) == 1.0f
                  from Dot move in get_non_blocked
                  where move.Own == StateOwn.Empty
&& Distance(dot0, move) == 1.4f
&& Distance(dot1, move) == 1.0f
&& Distance(dot2, move) == 1.0f
&& Distance(dot3, move) == 1.4f
&& Distance(dot4, move) == 1.0f
                  select new Dot(move.X, move.Y, NumberPattern: 11, Rating: 3, Tag: $"CheckPattern({Owner})");
            ld.AddRange(pat.Distinct(new DotEq()));






            return ld;
        }

            private Dot CheckPattern_vilochka(StateOwn Owner)
            {
                StateOwn Enemy = Owner == StateOwn.Human ? StateOwn.Computer : StateOwn.Human;
                IEnumerable<Dot> get_non_blocked = from Dot d in this where d.Blocked == false select d; //получить коллекцию незаблокированных точек

                //паттерн на диагональное расположение точек           *red1                 +2
                //                                                                       +1  -4
                //                                              *red2  +blue2        +0  -3  =move
                //                                                           
                //                                       *red3  +blue1  move(red) 
                iNumberPattern = 1;
                IEnumerable<Dot> pat;
            pat = from Dot dot0 in get_non_blocked
                  where dot0.Own == Enemy
                  from Dot dot1 in get_non_blocked
                  where dot1.Own == Enemy && Distance(dot1, dot0) == 1.4f
                  from Dot dot2 in get_non_blocked
                  where dot2.Own == Enemy && Distance(dot2, dot1) == 1.4f
                  from Dot dot3 in get_non_blocked
                  where dot3.Own == Owner && Distance(dot3, dot2) == 2.2f && Distance(dot3, dot1) == 1f
                  from Dot dot4 in get_non_blocked
                  where dot4.Own == Owner && Distance(dot4, dot3) == 1.4f && Distance(dot4, dot1) == 1f
                  from Dot dot5 in get_non_blocked
                  where dot5.Own == StateOwn.Empty && Distance(dot5, dot4) == 2.2f
                  from Dot dot6 in get_non_blocked
                  where dot6.Own == StateOwn.Empty && Distance(dot6, dot5) == 2.8f && Distance(dot6, dot0) == 3.2f
                  from Dot move in get_non_blocked
                  where move.Own == StateOwn.Empty
                    && Distance(dot0, move) == 2.0f
                    && Distance(dot1, move) == 1.4f
                    && Distance(dot2, move) == 2.0f
                    && Distance(dot3, move) == 1.0f
                    && Distance(dot4, move) == 1.0f
                    && Distance(dot5, move) == 1.4f
                    && Distance(dot6, move) == 1.4f
                  select new Dot(move.X, move.Y, NumberPattern: 1, Rating: 1, Tag: $"CheckPattern_vilochka({ Owner})");
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
        private List<Dot> CheckPattern2Move(StateOwn Owner)// по ошибке делал функцию Вилка2Х2
        {
            IEnumerable<Dot> qry;
            GameDots GameDots_Copy = GetGameDotsCopy(ListMoves);

            qry = from Dot d1 in GameDots_Copy.GetDots()     //.Board_AllNotBlockedDots
                  where d1.Own == Owner
                  from Dot d2 in GameDots_Copy.GetDots()
            where d2.IndexRelation == d1.IndexRelation && !d2.Blocked && Distance(d1, d2) >= 3f && Distance(d1, d2) < 3.5f

                  from Dot de1 in GameDots_Copy.NeighborDots(d1)
                  where de1.Own == 0 && !de1.Blocked

                  from Dot de2 in GameDots_Copy.NeighborDots(d2)
                  where de2.Own == 0 && !de2.Blocked

                  from Dot de3 in GameDots_Copy.CommonEmptyDots(de1, de2)

                  select de3;
            //список точек, куда делается ход в паттерне Вилка2х2
            List<Dot> ld = qry.Distinct(new DotEq()).ToList();

            IEnumerable<Link> link_empty_dots = from Dot de1 in ld
                                                 from Dot de2 in ld
                                                 where GameDots_Copy.Distance(de1, de2) >= 2
                                                 select new Link(de1, de2);
            //список точек, в которые потом делается ход для окружения
            List<Link> link = link_empty_dots.Distinct(new LinksComparer()).ToList();

            foreach (Link l in link)
            {
                //делаем ход
                int result_last_move = GameDots_Copy.MakeMove(l.Dot1, Owner);
                result_last_move = GameDots_Copy.MakeMove(l.Dot2, Owner);
                if (GameDots_Copy.GoalPlayer.Player == Owner)
                {
                    GameDots_Copy.UndoMove(l.Dot1);
                    GameDots_Copy.UndoMove(l.Dot2);
                    return ld;
                }

                //StateOwn pl = Owner == StateOwn.Computer ? StateOwn.Human : StateOwn.Computer;
                //Dot dt = GameDots_Copy.CheckMove(pl); // проверка чтобы не попасть в капкан
                //if (dt != null)
                //{
                //    GameDots_Copy.UndoMove(dot_pattern);
                //    continue;
                //}

            }
            foreach (Dot d in ld)
            {
                d.Blocked = false;
                d.Own = 0;
                d.NumberPattern = 777;
                d.Rating = 1;
                d.Tag = $"CheckPattern2Move({Owner})";
            }

            return ld;

        }
        /// <summary>
        /// Проверка хода на гарантированное окружение(когда точки находятся через 3 клетки) 
        /// Возвращает точку, в результате которой будет вилка с 2 пустыми точками
        /// </summary>
        /// <param name="Owner">Владелец точек, который проверяется</param>
        /// <param name="IndexRelation"></param>
        /// <returns></returns>
        private Dot CheckPatternVilka1x1(StateOwn Owner)
        {
            GameDots GameDots_Copy = GetGameDotsCopy(ListMoves);
            StateOwn Enemy = Owner == StateOwn.Human ? StateOwn.Computer : StateOwn.Human;
            List<Dot> ld = new List<Dot>();
            IEnumerable<Chain5Dots> qry;
            qry = from Dot d1 in GameDots_Copy.GetDots()     
                  where d1.Own == Owner
                  from Dot d2 in GameDots_Copy.GetDots()
                  where d2.IndexRelation == d1.IndexRelation && Distance(d1, d2) >= 3f && Distance(d1, d2) < 3.8f
                  from Dot de1 in GameDots_Copy.GetDots(StateOwn.Empty)
                  where Distance(d1, de1)==1f// && Distance(d2, de1) <= 2.4f
                  from Dot de2 in GameDots_Copy.GetDots(StateOwn.Empty)
                  where Distance(d1, de2) == 1.4f //&& Distance(de1, de2) == 1f && Distance(d2, de2) <= 2.4f
                  from Dot de3 in GameDots_Copy.GetDots(StateOwn.Empty)
                  //where Distance(d2, de3) <= 1.4f && Distance(d1, de3) <=2.4f

                  where Distance(d2, de3) <= 1.4f && GameDots_Copy.CommonEmptyDots(de3,d1).Contains(de1)
                  && GameDots_Copy.CommonEmptyDots(de3, d1).Contains(de2)

                  select new Chain5Dots(d1, d2, de1, de2, de3);

            List<Chain5Dots> lde3 = qry.Distinct(new Chains5DotsComparer()).ToList();

            foreach (Chain5Dots d in lde3)
            {
                //делаем 2 хода, чтобы проверить, замкнется регион или нет
                GameDots_Copy.MakeMove(d.DotE1, Owner);
                //GameDots_Copy.MakeMove(d.Dot2, Owner);
                GameDots_Copy.MakeMove(d.DotE3, Owner);
                if (GameDots_Copy.GoalPlayer.Player == Owner)
                {
                    d.DotE3.Rating = GameDots_Copy.GoalPlayer.CountBlocked;
                    ld.Add(new Dot(d.DotE3));
                }
                GameDots_Copy.UndoMove(d.DotE3);
                //GameDots_Copy.UndoMove(d.Dot2);
                GameDots_Copy.UndoMove(d.DotE1);
            }

            Dot result = ld.Where(dt => dt.Rating == ld.Max(d => d.Rating)).ElementAtOrDefault(0);
            if (result != null)
            {
                result.Blocked = false;
                result.Own = 0;
                result.NumberPattern = result.NumberPattern = Owner == StateOwn.Computer ? 777 : 666; 
                result.Rating = Owner == StateOwn.Computer ? 1 : 2;
                result.Tag = $"CheckPatternVilka1x1({Owner})";
            }
            return result;
        }

        /// <summary>
        /// Проверка хода на гарантированное окружение(когда точки находятся через 4 клетки) 
        /// Возвращает точку, в результате которой будет вилка с максимальным окружением
        /// </summary>
        /// <param name="Owner">Владелец точек, который проверяется</param>
        /// <param name="IndexRelation"></param>
        /// <returns></returns>
        private Dot CheckPatternVilka2x2(StateOwn Owner)
            {
            GameDots GameDots_Copy = GetGameDotsCopy(ListMoves);
            StateOwn Enemy = Owner == StateOwn.Human ? StateOwn.Computer : StateOwn.Human;
            List<Dot> ld = new List<Dot>();
            IEnumerable<Chain> qry;
            qry = from Dot d1 in GameDots_Copy.GetDots(NoEmpty: true)  //Board_NotEmptyNonBlockedDots
                  where d1.Own == Owner
                  from Dot d2 in GameDots_Copy.GetDots(NoEmpty: true)
                  where d2.IndexRelation == d1.IndexRelation && Distance(d1, d2) < 4.5f & Distance(d1, d2) >= 2.8f
                  from Dot de3 in GameDots_Copy.GetDots(StateOwn.Empty)
                  where GameDots_Copy.NeighborDots(de3).Where(d => d.Own == Owner).Count() == 0
                  && GameDots_Copy.CommonEmptyDots(d1, de3).Count == 2
                  && GameDots_Copy.CommonEmptyDots(d2, de3).Count == 2

                  select new Chain(d1,d2,de3);

            List<Chain> lde3 = qry.Distinct(new ChainsComparer()).ToList();
            

            foreach(Chain ch in lde3)
            {
                //делаем 3 хода, чтобы проверить, замкнется регион или нет
                Dot d1 = ch.DotE;
                Dot d2 = ch.GetConnectingEmptyDotsDot1DotE(GameDots_Copy).First();
                Dot d3 = ch.GetConnectingEmptyDotsDot2DotE(GameDots_Copy).First();
                GameDots_Copy.MakeMove(d1, Owner);
                GameDots_Copy.MakeMove(d2, Owner);
                GameDots_Copy.MakeMove(d3, Owner);
                if (GameDots_Copy.GoalPlayer.Player == Owner)
                {
                    ch.DotE.Rating = GameDots_Copy.GoalPlayer.CountBlocked;
                    ld.Add(new Dot(ch.DotE));
                }
                GameDots_Copy.UndoMove(d3);
                GameDots_Copy.UndoMove(d2);
                GameDots_Copy.UndoMove(d1);
                
            }

            Dot result =ld.Distinct(new DotEq()).Where(dt =>
            dt.Rating == ld.Max(d => d.Rating)).ElementAtOrDefault(0);

            if(result!=null)
            {
                result.Blocked = false;
                result.Own = 0;
                result.NumberPattern = Owner == StateOwn.Computer ? 777 : 666;
                result.Rating = Owner == StateOwn.Computer ? 2 : 3;
                result.Tag = $"CheckPatternVilka2x2({Owner})";
            }
            return result;
        }
        private Dot PickComputerMove(Dot enemy_move, IProgress<string> progress)
            {
                DebugInfo.Progress = progress;
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

                //foreach (Dot d in Lst_branch) DebugInfo.lstDBG2.Add(d.ToString() + " - " + d.Tag);
                Dot move = Lst_branch.Where(dt => dt.Rating == Lst_branch.Min(d => d.Rating)).ElementAtOrDefault(0);

            if (move != null)
            {
                best_move = move;
            }
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

            DebugInfo.StringMSG = "Количество ходов: " + counter_moves + "\r\n Глубина рекурсии: " + MAX_RECURSION +
            "\r\n Ход на " + best_move.ToString() +
            "\r\n время просчета " + stopWatch.ElapsedMilliseconds.ToString() + " мс";
            stopWatch.Reset();
#endif
            #endregion

            DebugInfo.lstDBG2.Add($"Ход на {best_move}");
            DebugInfo.strBestMove = $"Ход на {best_move}";
            return new Dot(best_move.X, best_move.Y); //так надо чтобы best_move не ссылался на точку в Dots
            }
        /// <summary>
        /// Поиск лучшего хода
        /// </summary>
        /// <param name="Player"></param>
        /// <returns></returns>
        private List<Dot> BestMove(StateOwn Player)
        {
            DebugInfo.lstDBG1.Clear();
            DebugInfo.lstDBG2.Clear();
            //string strDebug = string.Empty;
            List<Dot> moves = new List<Dot>();
            Dot bm;
            StateOwn Enemy = Player == StateOwn.Human ? StateOwn.Computer : StateOwn.Human;
#if DEBUG
            {
                sW2.Start();
                if (DebugInfo.Progress != null) DebugInfo.Progress.Report($"CheckMove({Player})...");
                DebugInfo.StringMSG = $"CheckMove({Player})...";
            }
#endif


            Goal GoalPlayer = new Goal();
            Goal GoalEnemy = new Goal();
            bm = CheckMove(Player, GoalPlayer);
            if (bm != null)
            {
                bm.Tag = $"CheckMove({Player})";
                bm.NumberPattern = 777; //777-ход в результате которого получается окружение - компьютер побеждает
                moves.Add(bm);
#if DEBUG
                {
                    sW2.Stop();
                    DebugInfo.lstDBG1.Add($"CheckMove {Player} - {sW2.Elapsed.Milliseconds.ToString()}");
                    if (DebugInfo.Progress != null) DebugInfo.Progress.Report($"CheckMove {Player} - {sW2.Elapsed.Milliseconds.ToString()}; CheckMove {Enemy}...");
                    sW2.Reset();
                }
#endif
            }
#if DEBUG
            {
                sW2.Start();
                DebugInfo.StringMSG = $"CheckMove({Enemy})...";
            }
#endif

            bm = CheckMove(Enemy, GoalEnemy);
            if (bm != null)
            {
                bm.Tag = $"CheckMove({Enemy})";
                bm.NumberPattern = 666; //666-ход в результате которого получается окружение -компьютер проигрывает
                moves.Add(bm);
#if DEBUG
                {
                    sW2.Stop();
                    DebugInfo.lstDBG1.Add($"CheckMove {Enemy} - {sW2.Elapsed.Milliseconds.ToString()}");
                    if (DebugInfo.Progress != null) DebugInfo.Progress.Report($"CheckMove {Enemy} - {sW2.Elapsed.Milliseconds.ToString()}; CheckPattern_vilochka...");
                    sW2.Reset();
                    //проверяем паттерны
                }
#endif
            }
            #region Проверка, кто больше окружит и будет ли угроза после окружения
            if ((GoalPlayer.CountBlocked - GoalEnemy.CountBlocked) > 0)
            {
                moves.Find(d => d.NumberPattern == 777).Rating = 0;
                if(GoalEnemy.Player != StateOwn.Empty) moves.Find(d => d.NumberPattern == 666).Rating = 1;
#if DEBUG
                {
                    foreach(Dot d in moves)
                    {
                        DebugInfo.lstDBG2.Add($"{d} - Win {d.Own}!");
                    }
                }
#endif
                return moves;
            }
            else if ((GoalPlayer.CountBlocked - GoalEnemy.CountBlocked) < 0)
            {
                if (GoalPlayer.Player != StateOwn.Empty) moves.Find(d => d.NumberPattern == 777).Rating = 1;
                moves.Find(d => d.NumberPattern == 666).Rating = 0;
#if DEBUG
                {
                    foreach (Dot d in moves)
                    {
                        DebugInfo.lstDBG2.Add($"{d} - Win {d.Own}!");
                    }
                }
#endif
                return moves;
            }
            #endregion
            #region CheckPattern_vilochka
#if DEBUG
            {
                sW2.Start();
                DebugInfo.StringMSG = "CheckPattern_vilochka проверяем ходы на два вперед...";
                if (DebugInfo.Progress != null) DebugInfo.Progress.Report(DebugInfo.StringMSG);
            }
#endif

            bm = CheckPattern_vilochka(Player);
            if (bm != null)
            {
#if DEBUG
                {
                    DebugInfo.lstDBG2.Add(bm.ToString() + " ----> CheckPattern_vilochka ");
                }
#endif
                bm.Tag = "CheckPattern_vilochka(" + Player + ")";
                bm.NumberPattern = 777; //777-ход в результате которого получается окружение -компьютер побеждает
                moves.Add(bm);
                return moves;
            }

            bm = CheckPattern_vilochka(Enemy);
            if (bm != null)
            {
                bm.Tag = "CheckPattern_vilochka(" + Enemy + ")";
                bm.NumberPattern = 666; //777-ход в результате которого получается окружение -компьютер побеждает
                moves.Add(bm);
            #region DEBUG
#if DEBUG
                {
                    DebugInfo.lstDBG2.Add(bm.ToString() + "-->CheckPattern_vilochka ");

                    sW2.Stop();
                    DebugInfo.lstDBG1.Add($"CheckPattern_vilochka - {sW2.Elapsed.Milliseconds.ToString()}");
                    if (DebugInfo.Progress != null)
                    {
                        DebugInfo.Progress.Report($"CheckPattern_vilochka - {sW2.Elapsed.Milliseconds.ToString()}; CheckPatternVilka1x1...");
                    }
                    sW2.Reset();
                    sW2.Start();
                }
#endif
                #endregion
               return moves;
            }
            #endregion
            #region CheckPatternVilka1x1 проверяем ходы на два вперед на гарантированное окружение

            bm = CheckPatternVilka1x1(Player);
            if (bm!=null)
            {
                moves.Add(bm);
                return moves;
            }

            bm = CheckPatternVilka1x1(Enemy);
            if (bm != null)
            {
                moves.Add(bm);
                return moves;
            }

            #endregion

#if DEBUG
            {
                sW2.Stop();
                DebugInfo.lstDBG1.Add($"CheckPatternVilka1x1 {Player} {Enemy} - {sW2.Elapsed.Milliseconds.ToString()}");
                if (DebugInfo.Progress != null)
                {
                    DebugInfo.Progress.Report($"CheckPatternVilka1x1 {Player} {Enemy} - {sW2.Elapsed.Milliseconds.ToString()}; CheckPatternVilka2x2...");
                }

                sW2.Reset();
                sW2.Start();
                DebugInfo.StringMSG = "CheckPatternVilka2x2...";
            }
#endif
            CheckVilka2x2(moves);
#if DEBUG
            {
                sW2.Stop();
                DebugInfo.lstDBG1.Add($"CheckPatternVilka2x2({Player}) - {sW2.Elapsed.Milliseconds.ToString()}");
                if (DebugInfo.Progress != null)
                {
                    DebugInfo.Progress.Report($"CheckPatternVilka2x2({Player}) - {sW2.Elapsed.Milliseconds.ToString()}; CheckPatterns...");
                }

                sW2.Reset();
                sW2.Start();
                DebugInfo.StringMSG = $"CheckPatterns {Player}...";
            }
#endif
            moves.AddRange(CheckPattern(Player));
#if DEBUG
            {
                sW2.Stop();
                DebugInfo.lstDBG1.Add($"CheckPatterns({Player}) - {sW2.Elapsed.Milliseconds.ToString()}");
                if (DebugInfo.Progress != null) DebugInfo.Progress.Report($"CheckPatterns({Player}) - {sW2.Elapsed.Milliseconds.ToString()}");
                sW2.Reset();
                sW2.Start();
                DebugInfo.StringMSG = $"CheckPatterns {Enemy}...";
            }
#endif
            moves.AddRange(CheckPattern(Enemy));
#if DEBUG
            {
                sW2.Stop();
                DebugInfo.lstDBG1.Add($"CheckPatterns({Enemy}) -" + sW2.Elapsed.Milliseconds.ToString());
                if (DebugInfo.Progress != null) DebugInfo.Progress.Report(DebugInfo.lstDBG1.Last());
                sW2.Reset();
                sW2.Start();
                DebugInfo.StringMSG = $"CheckPatternVilkaNextMove {StateOwn.Computer}...";
            }
#endif


            #region CheckPatternVilkaNextMove пока тормозит сильно - переработать!
            //            bm = CheckPatternVilkaNextMove(StateOwn.Computer);
            //            if (bm != null)
            //            {
            //                #region DEBUG
            //#if DEBUG
            //                {
            //                    DebugInfo.lstDBG2.Add($"{bm.ToString()} player {StateOwn.Computer} CheckPatternVilkaNextMove {iNumberPattern})");
            //                }
            //#endif
            //                #endregion
            //                moves.Add(bm); //return bm;
            //            }
            #region DEBUG

#if DEBUG
            sW2.Stop();
            DebugInfo.lstDBG1.Add("CheckPatternVilkaNextMove -" + sW2.Elapsed.Milliseconds.ToString());
            sW2.Reset();
#endif
            #endregion
            #endregion
            #region CheckPatternMove
            //moves.AddRange(CheckPatternMove(Player));
            //moves.AddRange(CheckPatternMove(Enemy));

            //#if DEBUG
            //            sW2.Stop();
            //            DebugInfo.lstDBG1.Add("CheckPatternMove(pl2) -" + sW2.Elapsed.Milliseconds.ToString());
            //            DebugInfo.textDBG1 = string.Empty;
            //            sW2.Reset();
            //#endif

            #endregion
            //moves=moves
            return moves.Where(d => d != null).Distinct(new DotEq()).ToList();
        }

        private void CheckVilka2x2(List<Dot> moves)
        {
            Dot v = CheckPatternVilka2x2(StateOwn.Human);
            if (v != null) moves.Add(v);
            v = CheckPatternVilka2x2(StateOwn.Computer);
            if (v != null) moves.Add(v);
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

            tempmove = lst_best_move.Where(dt => (dt.NumberPattern == 777 & dt.Rating == lst_best_move.Min(d => d.Rating))
                                       || (dt.NumberPattern == 666 & dt.Rating == lst_best_move.Min(d => d.Rating))).ElementAtOrDefault(0);

                //если есть паттерн на окружение противника тоже устанавливается бест мув
                if (tempmove != null)
                {
                    Lst_branch.Add(tempmove);
                    return Player;
                }
                //если есть паттерн на свое окружение устанавливается бест мув
                tempmove = lst_best_move.Where(dt => dt.NumberPattern == 666).FirstOrDefault();
                if (tempmove != null)
                {
                    Lst_branch.Add(tempmove);
                    Lst_branch.Last().Rating = tempmove.Rating + counter_moves;
                    return Enemy;
                }

                if (lst_best_move.Count > 0)
                {
                int i = 0;
                    #region Cycle
                    foreach (Dot move in lst_best_move.Where(dt => dt.Rating < 2))
                    {
                    i++;
                    if (DebugInfo.Progress != null) DebugInfo.Progress.Report("Wait..." + i*100 / lst_best_move.Count + "%");
                    #region ходим в проверяемые точки
                    if (counter_moves > MAX_COUNTMOVES) break;
                        //**************делаем ход***********************************
                        res_last_move = MakeMove(move, Player);
                        Lst_moves.Add(move);
                        counter_moves++;

                        #region проверка на окружение

                        if (GoalPlayer.Player == Player)
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
                        if (GoalPlayer.Player == Enemy)
                        {
                            UndoMove(move);

                            //Win_player = 0;
                            move.Rating = -1;
                            continue;
                        }
                        #endregion
                        #region Debug statistic
#if DEBUG
                        DebugInfo.lstDBG1.Add(move.ToString());//(move.Own + " -" + move.x + ":" + move.y);
                        DebugInfo.StringMSG = "Ходов проверено: " + counter_moves +
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
                            DebugInfo.lstDBG1.Remove(move.ToString());
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
                            DebugInfo.lstDBG1.Remove(move.ToString());
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
                            DebugInfo.lstDBG1.Remove(move.ToString());
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

            public bool IsGameOver => GetDots(StateOwn.Empty).Count == 0;

        //public IAsyncOperation<int> MoveAsync(int Player, CancellationToken? cancellationToken, Dot pl_move = null)
        //{
        //    // Use a lock to prevent the ResetAsync method from modifying the game 
        //    // state at the same time that a different thread is in this method.
        //    //lock (_lockObject)
        //    //{
        //    return MovePlayer(Player, cancellationToken);
        //    }

        //public async Task<int> MoveAsync(StateOwn player, CancellationToken? cancellationToken, Dot pl_move = null)
        //{
        //    int result = await MovePlayer(player, cancellationToken, pl_move);
        //    return result;
        //}

        public Task<int> MovePlayerAsync_old(StateOwn Player, IProgress<string> progress, Dot pl_move = null)
            {
                TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        tcs.SetResult(MovePlayer(Player, progress, pl_move));
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                });
                return tcs.Task;
            }
        //public Progress<string> Progress { get; set; } = new Progress<string>(s => DebugInfo.StringMSG = s);

        public Task<int> MovePlayerAsync(StateOwn Player, IProgress<string> progress=null, Dot pl_move = null)
        {
            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
            Task.Factory.StartNew(async () =>
            {
                try
                {
                    tcs.SetResult(await Task.Factory.StartNew(() => MovePlayer(Player, progress, pl_move ),
                                                             TaskCreationOptions.LongRunning));
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
        public int MovePlayer(StateOwn Player, IProgress<string> progress = null, Dot pl_move = null)
            {
                if (pl_move == null)
                {
                    pl_move = PickComputerMove(LastMove, progress);
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
        public Goal GoalPlayer { get; set; }

        /// <summary>
        /// Класс, который содержит информацию про игрока, который в результате своего хода окружил точки противника
        /// </summary>
        public class Goal
        {
            public StateOwn Player { get; set; }
            public int CountBlocked { get; set; }
        }
        /// <summary>
        /// Цепочка из 3 точек: 2 принадлежат игроку - 1 пустая.
        /// dot1 - e - dE - e - dot2
        /// </summary>
        public class Chain
        {
            public Chain(Dot dot1, Dot dot2, Dot dotE)
            {
                Dot1 = dot1;
                Dot2 = dot2;
                DotE = dotE;
                //list_dots = new List<Dot> { dot1, dot2, dotE };
            }
            public Dot Dot1 { get; set; }
            public Dot Dot2 { get; set; }
            public Dot DotE { get; set; }
            public List<Dot> GetConnectingEmptyDotsDot1DotE(GameDots GD)
            {
                return GD.CommonEmptyDots(Dot1, DotE);
            }
            public List<Dot> GetConnectingEmptyDotsDot2DotE(GameDots GD)
            {
                return GD.CommonEmptyDots(Dot2, DotE);
            }
            public override string ToString()
            {
                return $"{Dot1.X}:{Dot1.Y} - {DotE.X}:{DotE.Y} - {Dot2.X}:{Dot2.Y}";
            }
        }

        public class Chain5Dots
        {
            public Chain5Dots(Dot dot1, Dot dot2, Dot dotE1, Dot dotE2, Dot dotE3)
            {
                Dot1 = dot1;
                Dot2 = dot2;
                DotE1 = dotE1;
                DotE2 = dotE2;
                DotE3 = dotE3;
            }
            public Dot Dot1 { get; set; }
            public Dot Dot2 { get; set; }
            public Dot DotE1 { get; set; }
            public Dot DotE2 { get; set; }
            public Dot DotE3 { get; set; }
            public override string ToString()
            {
                return $"{Dot1.X}:{Dot1.Y} - {DotE1.X}:{DotE1.Y} - {DotE2.X}:{DotE2.Y} - {DotE3.X}:{DotE3.Y}  - {Dot2.X}:{Dot2.Y}";
            }
        }

    }
}
    


