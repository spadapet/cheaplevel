using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CheapLevel
{
    internal class Image : IDisposable
    {
        private int _width;
        private int _height;
        private ushort _bgColor;
        private ushort[] _pixels;
        private WriteableBitmap _bitmap;

        private Image(Bytes stream)
        {
            Load(stream);
        }

        public void Dispose()
        {
            CopyFrom(null);
        }

        public static Image Create(Bytes stream)
        {
            return new Image(stream);
        }

        private void Load(Bytes stream)
        {
            if (stream.LoadByte() != 1)
            {
                throw new Exception("Invalid image type");
            }

            int width = stream.LoadUshortAsInt();
            int height = stream.LoadUshortAsInt();
            ushort bgColor = stream.LoadColor16();
            ushort[] pixels = new ushort[width * height];
            byte[] imageBytes = stream.LoadCompressedBytes();
            Bytes imageStream = new Bytes(imageBytes);

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = bgColor;
            }

            for (bool done = false; !done; )
            {
                switch (imageStream.LoadByteAsInt())
                {
                    case 0:
                        done = true;
                        break;

                    case 1:
                        {
                            int y = imageStream.LoadUshortAsInt();
                            int start = imageStream.LoadUshortAsInt();
                            int end = start;
                            int count = imageStream.LoadByteAsInt();

                            while (count != 0)
                            {
                                end = start + count;
                                ushort color = imageStream.LoadColor16();

                                for (int x = start; x < end; x++)
                                {
                                    pixels[width * y + x] = color;
                                }

                                start = end;
                                count = imageStream.LoadByteAsInt();
                            }
                        }
                        break;

                    case 2:
                        {
                            int y = imageStream.LoadUshortAsInt();
                            int start = imageStream.LoadUshortAsInt();
                            int end = imageStream.LoadUshortAsInt();

                            for (int x = start; x <= end; x++)
                            {
                                ushort color = imageStream.LoadColor16();
                                pixels[width * y + x] = color;
                            }
                        }
                        break;

                    default:
                        throw new Exception("Invalid image row type");
                }
            }

            _width = width;
            _height = height;
            _bgColor = bgColor;
            _pixels = pixels;

            _bitmap = new WriteableBitmap(_width, _height, 96, 96, PixelFormats.Bgr565, null);
            _bitmap.Lock();
            _bitmap.WritePixels(new Int32Rect(0, 0, _width, _height), _pixels, _width * sizeof(ushort), 0);
            _bitmap.Unlock();
            _bitmap.Freeze();
        }

        private void CopyFrom(Image image)
        {
            _width = 0;
            _height = 0;
            _pixels = null;
            _bitmap = null;

            if (image != null)
            {
                _width = image._width;
                _height = image._height;
                _pixels = image._pixels;
                _bitmap = image._bitmap;
            }
        }

        public void SavePng(string filePath)
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Create))
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(_bitmap));
                encoder.Save(stream);
            }
        }
    }
}
