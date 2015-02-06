using System;
using System.Collections.Generic;

namespace CheapLevel
{
    /// <summary>
    /// All images, sounds, etc. loaded from a style file. All styles are
    /// cached until they are disposed.
    /// </summary>
    internal class LevelSet : IDisposable
    {
        private static List<LevelSet> _styleCache;
        private static object _lock;

        private int _posHeader;
        private int _posStyle;
        private int _posAuthor;
        private int _posStandards;
        private int _posSketches;
        private int _posErasers;
        private int _posCharacters;
        private int _posObjects;
        private int _posGraphics;
        private int _posFooter;
        private int _posMusic;
        private int _posSounds;
        private int _posImages;
        private int _posFiles;

        private IList<Image> _imageOthers;
        private IList<Image> _allImages;

        static LevelSet()
        {
            _styleCache = new List<LevelSet>();
            _lock = new object();
        }

        private LevelSet(string filePath)
        {
            FilePath = filePath ?? string.Empty;
            Header = string.Empty;
            Name = string.Empty;
            Author = string.Empty;

            _imageOthers = new List<Image>();
            _allImages = new List<Image>();
            _styleCache.Add(this);

            Load(System.IO.File.ReadAllBytes(filePath));
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _styleCache.Remove(this);

                foreach (Image image in AllImages)
                {
                    image.Dispose();
                }
            }
        }

        public static LevelSet Create(string file)
        {
            // Only one style can be loaded at a time
            lock (_lock)
            {
                string name = System.IO.Path.GetFileName(file);

                // See if the style was already loaded
                foreach (LevelSet existingStyle in _styleCache)
                {
                    if (name.Equals(existingStyle.FileName, StringComparison.OrdinalIgnoreCase))
                    {
                        return existingStyle;
                    }
                }

                // See if the file is within the directory of a loaded style
                if (!System.IO.File.Exists(file) && !System.IO.Path.IsPathRooted(file))
                {
                    foreach (LevelSet existingStyle in _styleCache)
                    {
                        string tryPath = System.IO.Path.Combine(existingStyle.FileDir, file);
                        if (System.IO.File.Exists(tryPath))
                        {
                            file = tryPath;
                            break;
                        }
                    }
                }

                return new LevelSet(file);
            }
        }

        public string FilePath { get; set; }
        public string FileName { get { return System.IO.Path.GetFileName(FilePath); } }
        public string FileDir { get { return System.IO.Path.GetDirectoryName(FilePath); } }
        public string Header { get; set; }
        public string Name { get; set; }
        public string Author { get; set; }

        public ICollection<Image> OtherImages
        {
            get
            {
                return _imageOthers;
            }
        }

        public IEnumerable<Image> AllImages
        {
            get
            {
                return _allImages;
            }
        }

        private void Load(byte[] bytes)
        {
            Bytes stream = new Bytes(bytes);

            _posFooter = stream.LoadInt();
            stream.Position = _posFooter;

            _posHeader = stream.LoadInt();
            _posStyle = stream.LoadInt();
            _posAuthor = stream.LoadInt();
            _posStandards = stream.LoadInt();
            _posSketches = stream.LoadInt();
            _posErasers = stream.LoadInt();
            _posCharacters = stream.LoadInt();
            _posObjects = stream.LoadInt();
            _posGraphics = stream.LoadInt();
            _posFooter = stream.LoadInt();
            _posMusic = stream.LoadInt();
            _posSounds = stream.LoadInt();
            _posImages = stream.LoadInt();
            _posFiles = stream.LoadInt();

            LoadHeader(bytes);
            LoadStandardImages(bytes);
            LoadOtherImages(bytes);
        }

        private void LoadHeader(byte[] bytes)
        {
            Bytes stream = new Bytes(bytes);

            if (_posHeader != 0)
            {
                stream.Position = _posHeader;
                Header = stream.LoadString();
            }

            if (Header != "Cheapo Copycat Level Editor")
            {
                throw new Exception("Invalid style file");
            }

            if (_posStyle != 0)
            {
                stream.Position = _posStyle;
                Name = stream.LoadString();
            }

            if (_posAuthor != 0)
            {
                stream.Position = _posAuthor;
                Author = stream.LoadString();
            }
        }

        private void LoadStandardImages(byte[] bytes)
        {
        }

        private void LoadOtherImages(byte[] bytes)
        {
            if (_posImages != 0)
            {
                Bytes stream = new Bytes(bytes, _posImages);
                if (stream.LoadByte() != 1)
                {
                    throw new Exception("Invalid image type in style");
                }

                int count = stream.LoadByteAsInt();
                for (int i = 0; i < count; i++)
                {
                    int index = stream.LoadByteAsInt();
                    Image image = Image.CreateOther(stream, index);
                    _imageOthers.Add(image);
                    _allImages.Add(image);
                }
            }
        }

        public void Save(string dest)
        {
            foreach (Image image in AllImages)
            {
                string file = System.IO.Path.Combine(dest, image.Name + ".png");
                string fileSprites = System.IO.Path.Combine(dest, image.Name + ".sprites.xml");
                string fileObjects = System.IO.Path.Combine(dest, image.Name + ".objects.xml");

                image.SavePng(file);
            }
        }
    }
}
