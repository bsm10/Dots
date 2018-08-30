using DotsGame.LinksAndDots;
using GameCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotsGame
{
    namespace Chains
    {
        /// <summary>
        /// Цепочка из 3 точек: 2 принадлежат игроку - 1 пустая.
        /// dot1 - e - dE - e - dot2
        /// </summary>
        public class Chain7Dots
        {
            public Chain7Dots(Dot dot1, Dot dot2, Dot dotE)
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
        public class Chain4Dots
        {
            private string s = string.Empty;
            public Chain4Dots(Dot dot1, Dot dot2, List<Dot> dotE)
            {
                Dot1 = dot1;
                Dot2 = dot2;
                DotE = dotE;
            }
            public Dot Dot1 { get; set; }
            public Dot Dot2 { get; set; }
            public List<Dot> DotE { get; set; } = new List<Dot>();
            public override string ToString()
            {
                foreach (Dot d in DotE)
                {
                    s += $" - DotE {d.X}:{d.Y} - ";
                }
                return $"Dot1 {Dot1.X}:{Dot1.Y}{s}Dot2 {Dot2.X}:{Dot2.Y}";
            }
        }
        public class Chain3Dots
        {
            public Chain3Dots(Dot dot1, Dot dotE, Dot dot2)
            {
                Dot1 = dot1;
                Dot2 = dot2;
                DotE = dotE;
            }
            public Dot Dot1 { get; set; }
            public Dot Dot2 { get; set; }
            public Dot DotE { get; set; }
            public override string ToString()
            {
                return $"Dot1 {Dot1.X}:{Dot1.Y} - DotE {DotE.X}:{DotE.Y} - Dot2 {Dot2.X}:{Dot2.Y}";
            }
        }
        public class Chain: IEqualityComparer<Chain>
        {
            public Chain(Dot dot1, Dot dot2)
            {
                Dot1 = dot1;
                Dot2 = dot2;
                if(!Dot2.Blocked)
                {
                    Dot2.IndexRelation = Dot1.IndexRelation;
                }
                
            }
            public Dot Dot1 { get; set; }
            public Dot Dot2 { get; set; }

            public override string ToString()
            {
                return $"Dot1 {Dot1.X}:{Dot1.Y} - Dot2 {Dot2.X}:{Dot2.Y}";
            }

            public bool Equals(Chain ch1, Chain ch2)
            {
                if (ch1.Dot1 == ch2.Dot1 && ch1.Dot2 == ch2.Dot2 ||
                   ch1.Dot1 == ch2.Dot2 && ch2.Dot1 == ch1.Dot2) return true;
                return false;
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

    }
}
