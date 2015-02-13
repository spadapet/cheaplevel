namespace CheapLevel
{
    internal class LevelObject
    {
        public int StyleIndex { get; set; }
        public int PosX { get; set; }
        public int PosY { get; set; }
        public bool FlipX { get; set; }
        public bool FlipY { get; set; }
        public bool IsFake { get; set; }
        public bool InBack { get; set; }
        public int Data { get; set; }

        public LevelObject()
        {
        }
    }
}
