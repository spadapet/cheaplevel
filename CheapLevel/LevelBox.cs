namespace CheapLevel
{
    internal enum LevelBoxType
    {
        Metal = 20,
        LeftArrows = 21,
        RightArrows = 22,
    }

    internal class LevelBox
    {
        public LevelBoxType Type { get; set; }
        public int Left { get; set; }
        public int Top { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public LevelBox()
        {
        }
    }
}
