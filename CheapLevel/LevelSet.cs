using System;
using System.Collections.Generic;

namespace CheapLevel
{
    internal class LevelSet : IDisposable
    {
        private int _posFooter;
        private Level[] _levels;

        private LevelSet(string filePath)
        {
            FilePath = filePath ?? string.Empty;
            Name = string.Empty;
            Author = string.Empty;

            Load(System.IO.File.ReadAllBytes(filePath));
        }

        public void Dispose()
        {
        }

        public static LevelSet Create(string file)
        {
            return new LevelSet(file);
        }

        public string FilePath { get; set; }
        public string FileName { get { return System.IO.Path.GetFileName(FilePath); } }
        public string FileDir { get { return System.IO.Path.GetDirectoryName(FilePath); } }
        public string Name { get; set; }
        public string Author { get; set; }

        private void Load(byte[] bytes)
        {
            Bytes stream = new Bytes(bytes);

            _posFooter = stream.LoadInt();
            stream.Position = _posFooter;

            this.Name = stream.LoadString();

            int numLevels = stream.LoadShortAsInt();
            int[] levelStarts = new int[numLevels];
            _levels = new Level[numLevels];

            for (int i = 0; i < numLevels; i++)
            {
                levelStarts[i] = stream.LoadInt();
            }

            for (int i = 0; i < numLevels; i++)
            {
                _levels[i] = Level.Create(bytes, levelStarts[i]);
            }
        }

        public void Save(string dest)
        {
            if (_levels != null)
            {
                for (int i = 0; i < _levels.Length; i++)
                {
                    _levels[i].Save(dest, i + 1);
                }
            }
        }
    }
}
