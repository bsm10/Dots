using System.Collections.Generic;
using System.Threading;
//using Windows.Foundation;

namespace DotsGame
{
    public interface IGame
    {
        IList<Dot> Dots { get; set; }
        int BoardWidth { get; set; }
        int BoardHeight { get; set; }
        IList<Dot> ListMoves { get; set; }
        State CurrentPlayer { get; }
        State CurrentOpponent { get; }
        State Winner { get; }
        IScore GetScore();

        State GetSpaceState(int row, int column);
        bool IsValidMove(Dot move);
        bool IsValidMove(int row, int column);
        bool IsGameOver { get; }
        void Move(int player, CancellationToken? cancellationToken, Dot pl_move = null);



        string ToString();

    }
    public enum State
    {
        None,
        One,
        Two
    }

}