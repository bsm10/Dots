using GameCore;
using System;
using System.Collections.Generic;
//using System.Drawing;
using System.Linq;
using System.Reflection;
using static DotsGame.LinksAndDots.Dot;

namespace DotsGame
{
    namespace LinksAndDots
    {
        //******************************Link********************************************
        public class Link //: IEqualityComparer<Links>
        {
            public Dot Dot1;
            public Dot Dot2;

            public float Distance
            {
                get
                {
                    return (float)Math.Round(Math.Sqrt(Math.Pow(Dot1.X - Dot2.X, 2) + Math.Pow(Dot1.Y - Dot2.Y, 2)), 1);
                }
            }

            public override string ToString()
            {
                string s = string.Empty;
                //if (Dot1.Own == StateOwn.Human & Dot2.Own == StateOwn.Human) s = " Player";
                //if (Dot1.Own == StateOwn.Computer & Dot2.Own == StateOwn.Computer) s = " Computer";
                //if (Dot1.Own == 0 | Dot2.Own == 0) s = " None";

                return $"{Dot1.X} : {Dot1.Y} - {Dot2.X} : {Dot2.Y}; {Own}  Dist - {Distance.ToString()}";
            }
            public override int GetHashCode()
            {
                //Check whether the object is null
                if (ReferenceEquals(this, null)) return 0;

                //Get hash code for the Dot1
                int hashLinkDot1 = Dot1.GetHashCode();

                //Get hash code for the Dot2
                int hashLinkDot2 = Dot2.GetHashCode();

                //Calculate the hash code for the Links
                return hashLinkDot1 * hashLinkDot2;
            }

            public bool Blocked => (Dot1.Blocked & Dot2.Blocked);
            public Player Own => Dot1.Own;

            public bool Fixed => (Dot1.Fixed | Dot2.Fixed);

            public Link(Dot dot1, Dot dot2)
            {
                Dot1 = dot1;
                Dot2 = dot2;
            }

            public bool Equals(Link otherLink)//Проверяет равенство связей по точкам
            {
                return GetHashCode().Equals(otherLink.GetHashCode());
            }

        }
        class LinksComparer : IEqualityComparer<Link>
        {
            public bool Equals(Link link1, Link link2)
            {
                return link1.Equals(link2);
            }

            // If Equals() returns true for a pair of objects 
            // then GetHashCode() must return the same value for these objects.

            public int GetHashCode(Link links)
            {
                //Check whether the object is null
                if (ReferenceEquals(links, null)) return 0;

                //Get hash code for the Name field if it is not null.
                int hashLinkDot1 = links.Dot1.GetHashCode();

                //Get hash code for the Code field.
                int hashLinkDot2 = links.Dot2.GetHashCode();

                //Calculate the hash code for the product.
                return hashLinkDot1 * hashLinkDot2;
            }

        }
        //*********************************Dot******************************************
        public class ComparerDots : IComparer<Dot>
        {
            public int Compare(Dot d1, Dot d2)
            {
                if (d1.X.CompareTo(d2.X) != 0)
                {
                    return d1.X.CompareTo(d2.X);
                }
                else if (d1.Y.CompareTo(d2.Y) != 0)
                {
                    return d1.Y.CompareTo(d2.Y);
                }
                else
                {
                    return 0;
                }
            }
        }
        public class ComparerDotsByOwn : IComparer<Dot>
        {
            public int Compare(Dot d1, Dot d2)
            {
                if (d1.X.CompareTo((int)d2.Own) != 0)
                {
                    return d1.Own.CompareTo(d2.Own);
                }
                else if (d1.Own.CompareTo(d2.Own) != 0)
                {
                    return d1.Own.CompareTo(d2.Own);
                }
                else
                {
                    return 0;
                }
            }
        }
        public class Dot : IEquatable<Dot>
        {
            private bool _Blocked;
            public bool Blocked
            {
                get => _Blocked;
                set
                {
                    _Blocked = value;
                    if (_Blocked)
                    {
                        IndexRelation = 0;
                        if (NeiborDots.Count > 0)
                        {
                            foreach (Dot d in NeiborDots)
                            {
                                if (d.Blocked == false) d.IndexRelation = d.IndexDot;
                            }
                        }
                    }
                }
            }
            /// <summary>
            /// Список точек, которые блокируются этой точкой
            /// </summary>
            public List<Dot> BlokingDots { get; set; }

            /// <summary>
            /// Точки по соседству с єтой точкой
            /// </summary>
            public List<Dot> NeiborDots { get; set; }
            public bool Fixed { get; set; }
            public int CountBlockedDots => BlokingDots.Count;
            public bool Selected { get; set; }
            public Player Own { get; set; }
            public int Rating { get; set; }
            public bool Marked { get; set; }
            public string Tag { get; set; } = string.Empty;
            public enum StateDotInPattern
            {
                Normal,
                InPattern,
                MoveDot
            }
            public enum Player
            {
                None,
                Human,
                Computer
            }

            public StateDotInPattern StateDot { get; set; }
            //public bool InPattern { get; set; }
            //public bool InPatternMoveDot { get; set; }

            private int _IndexDot;
            public int IndexDot
            {
                get
                {
                    return _IndexDot;
                }
                set
                {
                    _IndexDot = value;
                    _IndexRel = _IndexDot;
                }
            }

            public bool BonusDot { get; set; }

            public int NumberPattern { get; set; }

            public Dot(int x, int y, Player Owner = Player.None, int NumberPattern = 0, int Rating = 0, string Tag = "")
            {
                X = x;
                Y = y;
                BlokingDots = new List<Dot>();
                NeiborDots = new List<Dot>();
                Own = Owner;
                this.NumberPattern = NumberPattern;
                this.Rating = Rating;
                this.Tag = Tag;
            }
            public Dot(Dot dot)
            {
                X = dot.X;
                Y = dot.Y;
                BlokingDots = dot.BlokingDots;
                Own = dot.Own;
                NumberPattern = dot.NumberPattern;
                Rating = dot.Rating;
            }
            public Dot(Dot dot, Player Owner)
            {
                X = dot.X;
                Y = dot.Y;
                BlokingDots = dot.BlokingDots;
                Own = Owner;
                NumberPattern = dot.NumberPattern;
                Rating = dot.Rating;
            }
            /// <summary>
            /// Восстанавливаем первоначальное состояние точки
            /// </summary>
            public void Restore()
            {
                Blocked = false;
                BlokingDots.Clear();
                Own = 0;
                NumberPattern = 0;
                IndexRelation = IndexDot;
                Rating = 0;
                Tag = "";
                NeiborDots.Clear();
                Marked = false;
                NumberPattern = 0;
                StateDot = StateDotInPattern.Normal;

            }
            public void UnmarkDot()
            {
                Marked = false;
            }
            /// <summary>
            /// Удаляем метки паттернов
            /// </summary>
            public override string ToString()
            {
                string s;
                s = X + ":" + Y + "; " +
                    Own + "; " + Blocked + "; Rating: " + Rating + "; Tag: " + Tag + "; NumberPattern: " + NumberPattern;
                return s;
            }

            public bool Equals(Dot dot)//Проверяет равенство точек по координатам - это для реализации  IEquatable<Dot>
            {
                return (X == dot.X) & (Y == dot.Y);
            }
            private int _IndexRel;

            public int IndexRelation
            {
                get => _IndexRel;
                set
                {
                    _IndexRel = value;
                    if (NeiborDots.Count > 0)
                    {
                        foreach (Dot d in NeiborDots)
                        {
                            if (d.Blocked == false)
                            {
                                if (d.IndexRelation != _IndexRel & _IndexRel != 0)
                                {
                                    d.IndexRelation = _IndexRel;
                                }
                            }
                        }
                    }
                }

            }

            public int X { get; set; }
            public int Y { get; set; }
        }
    }
}
