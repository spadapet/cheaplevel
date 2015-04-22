using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Media;

namespace CheapLevel
{
    /// <summary>
    /// This class holds all of the data for a single level: The name, author, number
    /// of tools, number of lemmings, map, etc.
    /// Look for the Load(byte[] bytes, int pos) function to see how a level gets loaded
    /// from raw file bytes.
    /// </summary>
    internal class Level : IDisposable
    {
        private string _header; // always "Cheapo Copycat Level Editor" for valid level files
        private int _bgColor; // RGB color that is used to clear the background before drawing the level
        private int _width; // of level
        private int _height; // of level
        private int _numLems; // number of lemmings in the level
        private int _numSave; // number to save
        private int _releaseRate;
        private int _minutes; // time limit minutes
        private int _seconds; // time limit seconds (it was stupid to save minutes and seconds separately, oh well)
        private int _viewX; // X and Y: top left corner of the initial view of the level
        private int _viewY;
        private int _music; // which background music is preferred
        private int[] _tools; // count of the 8 standard tools (the file format can support more than 8 types of tools, this loader only supports 8)
        private string _intro; // text to show before playing the level
        private string[] _hints; // a list of hint strings
        private string _style; // name of the style file to load
        private LevelImage _smallMap;
        private LevelImage _fullMap; // the full level image
        private IList<LevelObject> _objects; // windows, traps, etc...
        private IList<LevelBox> _boxes; // one way arrows and metal boxes

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

        // Cleans up memory used by this level
        public void Dispose()
        {
            if (_smallMap != null)
            {
                _smallMap.Dispose();
                _smallMap = null;
            }

            if (_fullMap != null)
            {
                _fullMap.Dispose();
                _fullMap = null;
            }
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

        /// <summary>
        /// This is where all the magic starts when loading a level from a file.
        /// Either a *.lev file or somewhere within a *.set file.
        /// When loading from a *.set, the "pos" parameter should be set to the position
        /// within the set file where the level bytes start.
        /// "pos" should be zero for a *.lev file.
        /// </summary>
        private void Load(byte[] bytes, int pos)
        {
            Bytes stream = new Bytes(bytes, pos);

            // The first 4 bytes are an integer that tell you where the rest
            // of the data is stored. This position and every other position
            // must be offset by the "pos" parameter. This allows multiple
            // level files to be combined into one set file, without having to
            // update any position info in the level.
            stream.Position = stream.LoadInt() + pos;

            // All of these positions are 4 byte integers
            int posHeader = stream.LoadInt() + pos; // Position of header data that validates the level file
            int posFooter = stream.LoadInt() + pos; // Repeat of the footer position (ignore)
            int posStats = stream.LoadInt() + pos; // Position of level statistics
            int posStyle = stream.LoadInt() + pos; // Position of the style name
            int posTools = stream.LoadInt() + pos; // Position of the tool counts
            int posSmallMap = stream.LoadInt() + pos; // Position of the small map image
            int posObjects = stream.LoadInt() + pos; // Position of object information
            int posImage = stream.LoadInt() + pos; // Position of the full map image

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
            // This starts reading the data at offset "pos" within the byte array
            Bytes stream = new Bytes(bytes, pos);

            // The header is only here to validate the file format.
            // It must be 1 byte equal to 1, and a specific string.
            // Remember, a string is always 2 bytes for the length,
            // then a bunch of bytes of that given length. The last
            // byte is always 0.

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
            // This starts reading the data at offset "pos" within the byte array
            Bytes stream = new Bytes(bytes, pos);

            // Like usual, the first byte must be 1. The real purpose of that is to allow
            // other data formats (like 2, 3, etc) if someone ever wants to extend this file format.
            // My parser only knows how to deal with data format 1.
            if (stream.LoadByteAsInt() != 1)
            {
                throw new Exception("Invalid level stats format");
            }

            Name = stream.LoadString(); // Name of the level
            Author = stream.LoadString(); // Name of the author
            _bgColor = stream.LoadColor32(); // Lowest byte is Red, then Green, then Blue
            _width = stream.LoadShortAsInt(); // 2 bytes for width
            _height = stream.LoadShortAsInt(); // 2 bytes for height
            _numLems = stream.LoadByteAsInt(); // 1 byte for lemming count
            _numSave = stream.LoadByteAsInt(); // 1 byte for number to save
            _releaseRate = stream.LoadByteAsInt(); // 1 byte for release rate
            _minutes = stream.LoadByteAsInt(); // 1 byte for minutes
            _seconds = stream.LoadByteAsInt(); // 1 byte for seconds
            _viewX = stream.LoadShortAsInt(); // 2 bytes for initial view X
            _viewY = stream.LoadShortAsInt(); // 2 bytes for initial view Y
            _music = stream.LoadByteAsInt(); // 1 byte for music index
            _intro = stream.LoadString();
            // The hints are stored in a single string, with line breaks separating each hint
            _hints = stream.LoadString().Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        }

        private void LoadStyle(byte[] bytes, int pos)
        {
            // This starts reading the data at offset "pos" within the byte array
            Bytes stream = new Bytes(bytes, pos);

            if (stream.LoadByteAsInt() != 1)
            {
                throw new Exception("Invalid level style format");
            }

            _style = stream.LoadString();
        }

        private void LoadTools(byte[] bytes, int pos)
        {
            // This starts reading the data at offset "pos" within the byte array
            Bytes stream = new Bytes(bytes, pos);

            if (stream.LoadByteAsInt() != 1)
            {
                throw new Exception("Invalid level tools format");
            }

            // Assume that the standard 8 tools are used
            _tools = new int[8];

            // 1 byte for the number of tools used in the level
            int count = stream.LoadByteAsInt();
            for (int i = 0; i < count; i++)
            {
                // 1 byte for the type of tool, then 1 byte for the tool usage count
                int index = stream.LoadByteAsInt();
                int toolCount = stream.LoadByteAsInt();
                // This assumes that the index is less than 8, but if custom tools are used, it could be higher
                _tools[index] = toolCount;
            }
        }

        private void LoadSmallMap(byte[] bytes, int pos)
        {
            // This starts reading the data at offset "pos" within the byte array
            Bytes stream = new Bytes(bytes, pos);

            if (stream.LoadByteAsInt() != 1)
            {
                throw new Exception("Invalid level small map format");
            }

            _smallMap = LevelImage.Create(stream);
        }

        /// <summary>
        /// This loads both objects from styles, and boxes like arrows and metal
        /// </summary>
        private void LoadObjects(byte[] bytes, int pos)
        {
            // This starts reading the data at offset "pos" within the byte array
            Bytes stream = new Bytes(bytes, pos);

            if (stream.LoadByteAsInt() != 1)
            {
                throw new Exception("Invalid level object format");
            }

            _objects = new List<LevelObject>();
            _boxes = new List<LevelBox>();

            // A two byte integer defines the type of object:
            // 0 = No more objects, stop looping
            // 1 = A normal object from a style
            // 2 = A box (arrows or metal)

            for (bool done = false; !done; )
            {
                switch (stream.LoadShortAsInt()) // 2 byte object type
                {
                    case 0:
                        done = true;
                        break;

                    case 1: // style object
                        {
                            LevelObject obj = new LevelObject()
                            {
                                StyleIndex = stream.LoadShortAsInt(), // 2 bytes to tell you which object in the style is being used
                                PosX = stream.LoadShortAsInt(), // 2 bytes for X position
                                PosY = stream.LoadShortAsInt(), // 2 bytes for Y position
                                FlipX = stream.LoadByte() != 0, // 1 byte: Nonzero to flip the image horizontally
                                FlipY = stream.LoadByte() != 0, // 1 byte: Nonzero to flip the image vertically
                                IsFake = stream.LoadByte() != 0, // 1 byte: Nonzero if the object is fake
                                InBack = stream.LoadByte() != 0, // 1 byte: Nonzero if it's drawn behind the terrain
                                Data = stream.LoadShortAsInt(), // 2 bytes for either window or teleporter index
                            };

                            _objects.Add(obj);
                        }
                        break;

                    case 2: // box
                        {
                            LevelBox obj = new LevelBox()
                            {
                                Type = (LevelBoxType)stream.LoadShortAsInt(), // 2 bytes for the type of box
                                Left = stream.LoadShortAsInt(),
                                Top = stream.LoadShortAsInt(),
                                Width = stream.LoadShortAsInt(),
                                Height = stream.LoadShortAsInt(),
                            };

                            // The loaded values are actually the right and bottom of the box.
                            // This converts them to width and height.
                            obj.Width -= obj.Left - 1;
                            obj.Height -= obj.Top - 1;

                            _boxes.Add(obj);
                        }
                        break;

                    default:
                        throw new Exception("Invalid object type in level");
                }
            }
        }

        private void LoadImage(byte[] bytes, int pos)
        {
            // This starts reading the data at offset "pos" within the byte array
            Bytes stream = new Bytes(bytes, pos);

            if (stream.LoadByteAsInt() != 1)
            {
                throw new Exception("Invalid level map format");
            }

            _fullMap = LevelImage.Create(stream);
        }

        public string Save(string dest, int index)
        {
            string smallFormat = index != 0
                ? "level-{0}-small.png"
                : "level-small.png";

            string fullFormat = index != 0
                ? "level-{0}.png"
                : "level.png";

            string xmlFormat = index != 0
                ? "level-{0}.xml"
                : "level.xml";

            string smallFile = string.Format(smallFormat, index);
            string fullFile = string.Format(fullFormat, index);
            string xmlFile = string.Format(xmlFormat, index);

            if (_smallMap != null)
            {
                _smallMap.SavePng(Path.Combine(dest, smallFile));
            }

            if (_fullMap != null)
            {
                _fullMap.SavePng(Path.Combine(dest, fullFile));
            }

            using (FileStream stream = new FileStream(Path.Combine(dest, xmlFile), FileMode.Create))
            using (TextWriter writer = new StreamWriter(stream, Encoding.ASCII))
            {
                writer.WriteLine("<?xml version='1.0' ?>");
                writer.WriteLine("<Level>");

                writer.WriteLine("    <Name><![CDATA[{0}]]></Name>", Name);
                writer.WriteLine("    <Author><![CDATA[{0}]]></Author>", Author);
                writer.WriteLine("    <Style>{0}</Style>", _style);
                writer.WriteLine("    <MapFile>{0}</MapFile>", fullFile);
                writer.WriteLine("    <SmallMapFile>{0}</SmallMapFile>", smallFile);
                writer.WriteLine("    <BackgroundColor R='{0}' G='{1}' B='{2}' />", _bgColor & 0xFF, (_bgColor >> 8) & 0xff, (_bgColor >> 16) & 0xff);
                writer.WriteLine("    <Width>{0}</Width>", _width);
                writer.WriteLine("    <Height>{0}</Height>", _height);
                writer.WriteLine("    <LemCount>{0}</LemCount>", _numLems);
                writer.WriteLine("    <LemsToSave>{0}</LemsToSave>", _numSave);
                writer.WriteLine("    <ReleaseRate>{0}</ReleaseRate>", _releaseRate);
                writer.WriteLine("    <Minutes>{0}</Minutes>", _minutes);
                writer.WriteLine("    <Seconds>{0}</Seconds>", _seconds);
                writer.WriteLine("    <StartView X='{0}' Y='{1}' />", _viewX, _viewY);
                writer.WriteLine("    <Music>{0}</Music>", _music);
                writer.WriteLine("    <Tools>{0},{1},{2},{3},{4},{5},{6},{7}</Tools>", _tools[0], _tools[1], _tools[2], _tools[3], _tools[4], _tools[5], _tools[6], _tools[7]);
                writer.WriteLine("    <Intro><![CDATA[{0}]]></Intro>", _intro);

                if (_hints != null && _hints.Length > 0)
                {
                    writer.WriteLine("    <Hints>");

                    foreach (string hint in _hints)
                    {
                        writer.WriteLine("        <Hint><![CDATA[{0}]]></Hint>", hint);
                    }

                    writer.WriteLine("    </Hints>");
                }

                if (_objects != null && _objects.Count > 0)
                {
                    writer.WriteLine("    <Objects>");

                    foreach (LevelObject obj in _objects)
                    {
                        writer.WriteLine("        <Object StyleIndex='{0}' X='{1}' Y='{2}' FlipX='{3}' FlipY='{4}' IsFake='{5}' InBack='{6}' ExtraData='{7}' />",
                            obj.StyleIndex,
                            obj.PosX,
                            obj.PosY,
                            obj.FlipX,
                            obj.FlipY,
                            obj.IsFake,
                            obj.InBack,
                            obj.Data);
                    }

                    writer.WriteLine("    </Objects>");
                }

                if (_boxes != null && _boxes.Count > 0)
                {
                    writer.WriteLine("    <Boxes>");

                    foreach (LevelBox box in _boxes)
                    {
                        writer.WriteLine("        <Box Type='{0}' X='{1}' Y='{2}' Width='{3}' Height='{4}' />",
                            box.Type,
                            box.Left,
                            box.Top,
                            box.Width,
                            box.Height);
                    }

                    writer.WriteLine("    </Boxes>");
                }

                writer.WriteLine("</Level>");
            }

            return xmlFile;
        }
    }
}
