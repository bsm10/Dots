namespace DotsGame
{
    public interface IScore
    {
        /// <summary>
        /// Gets or sets the score for player one.
        /// </summary>
        int PlayerOne { get; set; }

        /// <summary>
        /// Gets or sets the score for player two.
        /// </summary>
        int PlayerTwo { get; set; }
    }
}