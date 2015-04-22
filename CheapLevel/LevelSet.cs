using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CheapLevel
{
    /// <summary>
    /// This class holds all data loaded from a *.set file. Basically it's just the
    /// name of the set and the array of levels in the set.
    /// Look for the Load(byte[] bytes) function below to see how the set file gets loaded
    /// from raw file bytes.
    /// </summary>
    internal class LevelSet : IDisposable
    {
        // The position in the set file where all information about the set is stored
        // (like name and number of levels). It's at the end of the file so it's called a footer.
        private int _posFooter;

        // As each level gets loaded, it gets added to this array
        private Level[] _levels;

        private LevelSet(string filePath)
        {
            FilePath = filePath ?? string.Empty;
            Name = string.Empty;

            Load(System.IO.File.ReadAllBytes(filePath));
        }

        // Cleans up memory used by this set
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

        /// <summary>
        /// This is where all the magic starts when loading a *.set file
        /// </summary>
        /// <param name="bytes">The raw bytes loaded from disk</param>
        private void Load(byte[] bytes)
        {
            Bytes stream = new Bytes(bytes);

            // The first 4 bytes are the position in the file where the footer is
            _posFooter = stream.LoadInt();
            // Move to the footer to start loading new bytes
            stream.Position = _posFooter;

            // A saved string always 4 bytes for the length integer. That tells you how
            // many bytes to load for the string (in ASCII format). The last byte is
            // always a terminating zero character to make it a valid C-string.
            this.Name = stream.LoadString();

            // Then 2 bytes for the number of levels in the set
            int numLevels = stream.LoadShortAsInt();
            int[] levelStarts = new int[numLevels];
            _levels = new Level[numLevels];

            // Then there are 4 byte integers that tell you the position of every one of
            // the levels, starting from the beginning of the file.
            for (int i = 0; i < numLevels; i++)
            {
                levelStarts[i] = stream.LoadInt();
            }

            // Now that we know the starting offset of every level, load them all
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
