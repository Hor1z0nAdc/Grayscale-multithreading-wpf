using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Threshold
{
    public class Algorithms
    {
        private BitmapImage bitmapImage;
        private Bitmap bitmap;
        private BitmapData bitmapData;
        private Func<int[], int> selectedMethod;
        private int bytesPerPixel, heightInPixels, widthInBytes;
        private int error;

        public void prepareProcess(BitmapImage image)
        {
            bitmapImage = image;
            bitmap = BitmapImage2Bitmap(bitmapImage);
            bytesPerPixel = Bitmap.GetPixelFormatSize(bitmap.PixelFormat) / 8;
            error = 0;
        }

        public static BitmapImage ToBitmapImage(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }


        private static Bitmap BitmapImage2Bitmap(BitmapImage bitmapImage)
        {
            // BitmapImage bitmapImage = new BitmapImage(new Uri("../Images/test.png", UriKind.Relative));

            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);

                return new Bitmap(bitmap);
            }
        }


        public BitmapImage startProcessingImage(string selectedAlgorithm, int numOfThreads = 1)
        {
            //puts the image pixel bytes into memory
            bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width,
                bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);

            //metadata about image
            heightInPixels = bitmapData.Height;
            widthInBytes = bitmapData.Width * bytesPerPixel;

            //determine the needed gray scale algoritm based on user selection
            switch (selectedAlgorithm)
            {
                case "Average gray scale":
                    selectedMethod = averageGrayScale;
                    break;
                case "Luma gray scale":
                    selectedMethod = lumaGrayScale;
                    break;
                case "4 shade gray scale":
                    selectedMethod = fourShadeGrayScale;
                    break;
                case "8 shade gray scale":
                    selectedMethod = eightShadeGrayScale;
                    break;
                case "Dithering":
                    selectedMethod = Dithering;
                    break;
            }

            if (numOfThreads > 1)
            {
                parallelProcessImage(numOfThreads);
            }
            else
            {
                //sequential processing
                changeAllPixels(0, heightInPixels);
            }

            bitmap.UnlockBits(bitmapData);
            return ToBitmapImage(bitmap);
        }

      
        public void parallelProcessImage(int numOfThreads)
        {
            //pointer part
            unsafe
            {
                List<Task> tasks = new List<Task>();
                int linesPerTask = heightInPixels / numOfThreads;

                for (int i = 0; i < numOfThreads; i++)
                {
                    int startingYIndex = linesPerTask * i;
                    int endYIndex = linesPerTask * (i + 1);

                    Task task = Task.Factory.StartNew(() =>
                    {
                        changeAllPixels(startingYIndex, endYIndex);
                    });
                    tasks.Add(task);
                }

                Task.WaitAll(tasks.ToArray());
            }
        }


        private int lumaGrayScale(int[] BGRvalues)
        {
            int newGrayValue = (int)(BGRvalues[0] * 0.11 + BGRvalues[1] * 0.59 + BGRvalues[2] * 0.3);
            return newGrayValue;
        }


        private int averageGrayScale(int[] BGRvalues)
        {
            int newGrayValue = (BGRvalues[0] + BGRvalues[0] + BGRvalues[0]) / 3;
            return newGrayValue;
        }


        private int limitedShadeGrayScale(int[] BGRvalues, int numOfShades)
        {
            int conversionFactor = 255 / (numOfShades - 1);
            double averageValue = (BGRvalues[0] + BGRvalues[0] + BGRvalues[0]) / 3;
            int newGrayValue = (int)((averageValue / conversionFactor) + 0.5) * conversionFactor;
          
            return newGrayValue;
        }


        private int fourShadeGrayScale(int[] BGRvalues)
        {
            return limitedShadeGrayScale(BGRvalues, 4);
        }


        private int eightShadeGrayScale(int[] BGRvalues)
        {
            return limitedShadeGrayScale(BGRvalues, 8);
        }


        private int Dithering(int[] BGRvalues)
        {
            int average = averageGrayScale(BGRvalues);
            int temporalColor = average + error;
            int newColor;

            if (temporalColor < 123)
            {
                newColor = 0;
                error = average;
            } 
            else
            {
                newColor = 255;
                error = temporalColor - 255;
            }

            return newColor;
        }
        

        private void changeAllPixels(int startingYIndex, int endYIndex)
        {
            unsafe
            {
                byte* ptrFirstPixel = (byte*)bitmapData.Scan0;

                for (int y = startingYIndex; y < endYIndex; y++)
                {
                    byte* currentLine = ptrFirstPixel + (y * bitmapData.Stride);
                    error = 0;

                    for (int x = 0; x < widthInBytes; x = x + bytesPerPixel)
                    {
                        int[] oldBGRvalues = { currentLine[x], currentLine[x + 1], currentLine[x + 2] };
                        int grayValue = selectedMethod(oldBGRvalues);
                                   
                        currentLine[x] = (byte)grayValue;
                        currentLine[x + 1] = (byte)grayValue;
                        currentLine[x + 2] = (byte)grayValue;
                    }
                }
            }
        }
    }
}
