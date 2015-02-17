using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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

            Load(System.IO.File.ReadAllBytes(filePath));
        }

        public void Dispose()
        {
            if (_levels != null)
            {
                foreach (Level level in _levels)
                {
                    level.Dispose();
                }

                _levels = null;
            }
        }

        public static LevelSet Create(string file)
        {
            return new LevelSet(file);
        }

        public string FilePath { get; set; }
        public string FileName { get { return System.IO.Path.GetFileName(FilePath); } }
        public string FileDir { get { return System.IO.Path.GetDirectoryName(FilePath); } }
        public string Name { get; set; }

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
            using (FileStream stream = new FileStream(Path.Combine(dest, "set.xml"), FileMode.Create))
            using (TextWriter writer = new StreamWriter(stream, Encoding.ASCII))
            {
                writer.WriteLine("<?xml version='1.0' ?>");
                writer.WriteLine("<Set>");
                writer.WriteLine("    <Name><![CDATA[{0}]]></Name>", Name);

                if (_levels != null && _levels.Length > 0)
                {
                    writer.WriteLine("    <LevelFiles>");

                    for (int i = 0; i < _levels.Length; i++)
                    {
                        string file = _levels[i].Save(dest, i + 1);
                        writer.WriteLine("        <LevelFile>{0}</LevelFile>", file);
                    }

                    writer.WriteLine("    </LevelFiles>");
                }

                writer.WriteLine("</Set>");
            }
        }
    }
}
