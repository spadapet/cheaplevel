using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Media;

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
        private int[] _tools;
        private string _intro;
        private string[] _hints;
        private string _style;
        private Image _smallMap;
        private Image _fullMap;
        private IList<LevelObject> _objects;
        private IList<LevelBox> _boxes;

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

            if (stream.LoadByteAsInt() != 1)
            {
                throw new Exception("Invalid level tools format");
            }

            _tools = new int[8];

            int count = stream.LoadByteAsInt();
            for (int i = 0; i < count; i++)
            {
                int index = stream.LoadByteAsInt();
                int toolCount = stream.LoadByteAsInt();
                _tools[index] = toolCount;
            }
        }

        private void LoadSmallMap(byte[] bytes, int pos)
        {
            Bytes stream = new Bytes(bytes, pos);

            if (stream.LoadByteAsInt() != 1)
            {
                throw new Exception("Invalid level small map format");
            }

            _smallMap = Image.Create(stream);
        }

        private void LoadObjects(byte[] bytes, int pos)
        {
            Bytes stream = new Bytes(bytes, pos);

            if (stream.LoadByteAsInt() != 1)
            {
                throw new Exception("Invalid level object format");
            }

            _objects = new List<LevelObject>();
            _boxes = new List<LevelBox>();

            for (bool done = false; !done; )
            {
                switch (stream.LoadShortAsInt())
                {
                    case 0:
                        done = true;
                        break;

                    case 1: // style object
                        {
                            LevelObject obj = new LevelObject()
                            {
                                StyleIndex = stream.LoadShortAsInt(),
                                PosX = stream.LoadShortAsInt(),
                                PosY = stream.LoadShortAsInt(),
                                FlipX = stream.LoadByte() != 0,
                                FlipY = stream.LoadByte() != 0,
                                IsFake = stream.LoadByte() != 0,
                                InBack = stream.LoadByte() != 0,
                                Data = stream.LoadShortAsInt(),
                            };

                            _objects.Add(obj);
                        }
                        break;

                    case 2: // box
                        {
                            LevelBox obj = new LevelBox()
                            {
                                Type = (LevelBoxType)stream.LoadShortAsInt(),
                                Left = stream.LoadShortAsInt(),
                                Top = stream.LoadShortAsInt(),
                                Width = stream.LoadShortAsInt(),
                                Height = stream.LoadShortAsInt(),
                            };

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
            Bytes stream = new Bytes(bytes, pos);

            if (stream.LoadByteAsInt() != 1)
            {
                throw new Exception("Invalid level map format");
            }

            _fullMap = Image.Create(stream);
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
