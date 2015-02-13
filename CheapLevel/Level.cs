using System;
using System.Collections.Generic;

namespace CheapLevel
{
    internal class Level : IDisposable
    {
        private string _header;
        private int _bgColor;
        private int _width;
        private int _height;
        private int _numLems;
        private int _numSave;
        private int _releaseRate;
        private int _minutes;
        private int _seconds;
        private int _viewX;
        private int _viewY;
        private int _music;
        private string _intro;
        private string[] _hints;
        private string _style;

        private Level(string filePath)
        {
            FilePath = filePath ?? string.Empty;
            Name = string.Empty;
            Author = string.Empty;

            Load(System.IO.File.ReadAllBytes(filePath), 0);
        }

        private Level(byte[] bytes, int pos)
        {
            FilePath = string.Empty;
            Name = string.Empty;
            Author = string.Empty;

            Load(bytes, pos);
        }

        public void Dispose()
        {
        }

        public static Level Create(string file)
        {
            return new Level(file);
        }

        public static Level Create(byte[] bytes, int pos)
        {
            return new Level(bytes, pos);
        }

        public string FilePath { get; set; }
        public string FileName { get { return System.IO.Path.GetFileName(FilePath); } }
        public string FileDir { get { return System.IO.Path.GetDirectoryName(FilePath); } }
        public string Name { get; set; }
        public string Author { get; set; }

        private void Load(byte[] bytes, int pos)
        {
            Bytes stream = new Bytes(bytes, pos);
            stream.Position = stream.LoadInt() + pos;

            int posHeader = stream.LoadInt() + pos;
            int posFooter = stream.LoadInt() + pos;
            int posStats = stream.LoadInt() + pos;
            int posStyle = stream.LoadInt() + pos;
            int posTools = stream.LoadInt() + pos;
            int posSmallMap = stream.LoadInt() + pos;
            int posObjects = stream.LoadInt() + pos;
            int posImage = stream.LoadInt() + pos;

            LoadHeader(bytes, posHeader);
            LoadStats(bytes, posStats);
            LoadStyle(bytes, posStyle);
            LoadTools(bytes, posTools);
            LoadSmallMap(bytes, posSmallMap);
            LoadObjects(bytes, posObjects);
            LoadImage(bytes, posImage);
        }

        private void LoadHeader(byte[] bytes, int pos)
        {
            Bytes stream = new Bytes(bytes, pos);

            if (stream.LoadByteAsInt() != 1)
            {
                throw new Exception("Invalid level header format");
            }

            _header = stream.LoadString();

            if (_header != "Cheapo Copycat Level Editor")
            {
                throw new Exception("Invalid level header");
            }
        }

        private void LoadStats(byte[] bytes, int pos)
        {
            Bytes stream = new Bytes(bytes, pos);

            if (stream.LoadByteAsInt() != 1)
            {
                throw new Exception("Invalid level stats format");
            }

            Name = stream.LoadString();
            Author = stream.LoadString();
            _bgColor = stream.LoadColor32();
            _width = stream.LoadShortAsInt();
            _height = stream.LoadShortAsInt();
            _numLems = stream.LoadByteAsInt();
            _numSave = stream.LoadByteAsInt();
            _releaseRate = stream.LoadByteAsInt();
            _minutes = stream.LoadByteAsInt();
            _seconds = stream.LoadByteAsInt();
            _viewX = stream.LoadShortAsInt();
            _viewY = stream.LoadShortAsInt();
            _music = stream.LoadByteAsInt();
            _intro = stream.LoadString();
            _hints = stream.LoadString().Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        }

        private void LoadStyle(byte[] bytes, int pos)
        {
            Bytes stream = new Bytes(bytes, pos);

            if (stream.LoadByteAsInt() != 1)
            {
                throw new Exception("Invalid level style format");
            }

            _style = stream.LoadString();
        }

        private void LoadTools(byte[] bytes, int pos)
        {
            Bytes stream = new Bytes(bytes, pos);
        }

        private void LoadSmallMap(byte[] bytes, int pos)
        {
            Bytes stream = new Bytes(bytes, pos);
        }

        private void LoadObjects(byte[] bytes, int pos)
        {
            Bytes stream = new Bytes(bytes, pos);
        }

        private void LoadImage(byte[] bytes, int pos)
        {
            Bytes stream = new Bytes(bytes, pos);
        }

        public void Save(string dest, int index)
        {
        }
    }
}
