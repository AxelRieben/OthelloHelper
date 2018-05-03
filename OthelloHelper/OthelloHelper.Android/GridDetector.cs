using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables.Shapes;
using Android.Util;
using OpenCV.Android;
using OpenCV.Core;
using OpenCV.ImgCodecs;
using OpenCV.ImgProc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace OthelloHelper.Droid
{
    class GridDetector
    {
        //Constants
        private const string TAG = "GridDetector";

        private const int BLUR_SIZE = 5;
        private const int DILATATION_SIZE = 30;
        private const int EROSION_SIZE = 20;
        private const int HSV_SENSITIVITY = 20;

        private const int CANNY_THRESHOLD1 = 50;
        private const int CANNY_THRESHOLD2 = 3 * CANNY_THRESHOLD1;
        private const int CANNY_SOBEL_SIZE = 3;

        private const int BOARD_SIZE = 8;

        private const int BOX_SATURATION_THESHOLD = 100;
        private const int BOX_VALUE_THESHOLD = 150;

        private const string DIR_PROCESSING = "/OthelloHelper/";

        // Inputs
        private Bitmap bitmapSource;

        //Attributs
        private Mat initialMat;
        private Mat initialHsv;
        private Bitmap processedImage;
        private int[,] board;
        OpenCV.Core.Rect boundingRect;

        //Box
        private int boxSizeX;
        private int boxSizeY;

        //CSV file that contains hsv level of each box of the grid
        private StringBuilder csv;


        public GridDetector(Bitmap bitmap)
        {
            bitmapSource = bitmap;
            initialMat = new Mat();
            initialHsv = new Mat();
            csv = new StringBuilder();
            Board = new int[BOARD_SIZE, BOARD_SIZE];
        }

        public Bitmap ProcessedImage { get => processedImage; set => processedImage = value; }
        public int[,] Board { get => board; set => board = value; }


        /// <summary>
        /// Draw a red circle on the image
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns>Path the the file containing exported bitmap</returns>
        public string DrawBestMove(int i, int j)
        {
            // Adapt circle to mat size
            var point = IJToPoint(i, j);
            var offset = boundingRect.Height / 40;
            point.X += offset;
            point.Y += offset;

            Imgproc.Circle(initialMat, point, boundingRect.Height / 20, new Scalar(255, 0, 0, 255), offset);
            return ExportBitmap(initialMat, "step_9_final_result.png");
        }

        /// <summary>
        /// Apply image processing algorithm to analyse the current situation of the board
        /// </summary>
        /// <param name="imagePath"></param>
        public void Process()
        {
            //bitmapSource = BitmapFactory.DecodeResource(Android.App.Application.Context.Resources, Resource.Drawable.test_medium_small);

            Utils.BitmapToMat(bitmapSource, initialMat);

            if (initialMat.Empty())
            {
                Log.Info(TAG, "Image can't be loaded");
                return;
            }

            //Blur image
            Imgproc.GaussianBlur(initialMat, initialMat, new OpenCV.Core.Size(BLUR_SIZE, BLUR_SIZE), 0);

            //Convert to HSV
            Imgproc.CvtColor(initialMat, initialHsv, Imgproc.ColorBgr2hsv);
            ExportBitmap(initialMat, "step_1_hsv.png");

            //Get the green pixels and use the fact that game board is always green
            Mat green = new Mat();
            Core.InRange(initialHsv, new Scalar(60 - HSV_SENSITIVITY, 100, 100), new Scalar(60 + HSV_SENSITIVITY, 255, 255), green);
            ExportBitmap(green, "step_2_green.png");

            //Dilatation
            Mat dilated = new Mat();
            Imgproc.Dilate(green, dilated, Imgproc.GetStructuringElement(Imgproc.MorphRect, new OpenCV.Core.Size(DILATATION_SIZE, DILATATION_SIZE)));
            ExportBitmap(dilated, "step_3.1_dilated.png");

            //Erosion
            Mat eroded = new Mat();
            Imgproc.Erode(dilated, eroded, Imgproc.GetStructuringElement(Imgproc.MorphRect, new OpenCV.Core.Size(EROSION_SIZE, EROSION_SIZE)));
            ExportBitmap(eroded, "step_3.2_eroded.png");

            //Apply adaptative threshold
            Mat gray = new Mat();
            Imgproc.AdaptiveThreshold(eroded, gray, 255, Imgproc.AdaptiveThreshMeanC, Imgproc.ThreshBinary, 15, 40);
            ExportBitmap(gray, "step_4_threshold.png");

            //Apply canny
            Mat canny = new Mat();
            Imgproc.Canny(gray, canny, CANNY_THRESHOLD1, CANNY_THRESHOLD2, CANNY_SOBEL_SIZE, true);
            ExportBitmap(canny, "step_5_canny.png");

            //Find biggest contours
            IList<MatOfPoint> contours = new Android.Runtime.JavaList<MatOfPoint>();
            Mat hierarchy = new Mat();
            Imgproc.FindContours(canny, contours, hierarchy, Imgproc.RetrTree, Imgproc.ChainApproxSimple);

            double maxVal = 0;
            int maxValIdx = 0;
            for (int contourIdx = 0; contourIdx < contours.Count; contourIdx++)
            {
                double contourArea = Imgproc.ContourArea(contours[contourIdx]);
                if (maxVal < contourArea)
                {
                    maxVal = contourArea;
                    maxValIdx = contourIdx;
                }
            }

            //Get the bounding rect of the biggest contour
            boundingRect = Imgproc.BoundingRect(contours[maxValIdx]);
            Imgproc.Rectangle(initialMat, boundingRect.Tl(), boundingRect.Br(), new Scalar(255, 0, 0, 255), 1, 8, 0);
            ExportBitmap(initialMat, "step_6_bounding_rect.png");

            ProcessBoard();
            ExportBitmap(initialMat, "step_8_result.png");

            PrintLogBoard();
        }

        /// <summary>
        /// Once the bounding rect of the board is detected, all the boxes can be processed
        /// </summary>
        private void ProcessBoard()
        {
            //Size of one rectangle of the grid
            boxSizeX = boundingRect.Width / BOARD_SIZE;
            boxSizeY = boundingRect.Height / BOARD_SIZE;

            //Iterate through the board
            for (int i = 0; i < BOARD_SIZE; i++)
            {
                for (int j = 0; j < BOARD_SIZE; j++)
                {
                    //Current box
                    OpenCV.Core.Rect currentBox = new OpenCV.Core.Rect(IJToPoint(i, j), new OpenCV.Core.Size(boxSizeX / 2, boxSizeY / 2));
                    ProcessBox(currentBox, i, j);
                }
            }

            //Export CSV
            File.WriteAllText(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + DIR_PROCESSING + "data.csv", csv.ToString());
        }


        /// <summary>
        /// Convert i,j  coordinate of the board to openCV point on the image
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        private OpenCV.Core.Point IJToPoint(int i, int j)
        {
            double pointX = boundingRect.Tl().X + (i * boxSizeX) + (boxSizeX / 4);
            double pointY = boundingRect.Tl().Y + (j * boxSizeY) + (boxSizeY / 4);
            return new OpenCV.Core.Point(pointX, pointY);
        }


        /// <summary>
        /// Process one of the small square of the grid
        /// </summary>
        /// <param name="currentBox"></param>
        /// <param name="i"></param>
        /// <param name="j"></param>
        private void ProcessBox(OpenCV.Core.Rect currentBox, int i, int j)
        {
            //Get a rectangular sub area of the gray matrix and calculate the mean value
            Mat boxHsv = initialHsv.Submat(currentBox);
            Mat hue = new Mat();
            Mat saturation = new Mat();
            Mat value = new Mat();

            //Extract each channel of the hsv image
            Core.ExtractChannel(boxHsv, hue, 0);
            Core.ExtractChannel(boxHsv, saturation, 1);
            Core.ExtractChannel(boxHsv, value, 2);

            //Get the mean values of each components
            double meanBoxHue = GetMeanValue(hue);
            double meanBoxSaturation = GetMeanValue(saturation);
            double meanBoxValue = GetMeanValue(value);

            //Append CSV to make statitistics
            csv.Append("Box IJ :");
            csv.Append(",");
            csv.Append(i);
            csv.Append(",");
            csv.Append(j);
            csv.Append(",");
            csv.Append(meanBoxHue);
            csv.Append(",");
            csv.Append(meanBoxSaturation);
            csv.Append(",");
            csv.Append(meanBoxValue);
            csv.Append("\n");

            // Export image for debug
            ExportBitmap(value, "step_7_box_" + i + "_" + j + ".png");

            if (meanBoxSaturation < BOX_SATURATION_THESHOLD)
            {
                if (meanBoxValue > BOX_VALUE_THESHOLD)
                {
                    //It's a white coin
                    Board[j, i] = 0;
                    Imgproc.Rectangle(initialMat, currentBox.Tl(), currentBox.Br(), new Scalar(255, 255, 255, 255), 2, 8, 0);
                }
                else
                {
                    //It's a black coin
                    Board[j, i] = 1;
                    Imgproc.Rectangle(initialMat, currentBox.Tl(), currentBox.Br(), new Scalar(0, 0, 0, 255), 2, 8, 0);
                }
            }
            else
            {
                //It's a green box (no coin)
                Board[i, j] = -1;
                Imgproc.Rectangle(initialMat, currentBox.Tl(), currentBox.Br(), new Scalar(0, 255, 0, 255), 2, 8, 0);
            }
        }


        /// <summary>
        /// Calculate the mean value of a matrix
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        private double GetMeanValue(Mat mat)
        {
            MatOfDouble mean = new MatOfDouble();
            MatOfDouble stds = new MatOfDouble();
            Core.MeanStdDev(mat, mean, stds);
            return mean.Get(0, 0)[0];
        }


        /// <summary>
        /// Export a matrice in a bitmap file with the given filename
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="filename"></param>
        /// <returns>Path the the file containing exported bitmap</returns>
        private string ExportBitmap(Mat mat, string filename)
        {
            string filePath;

            // Work with bitmap
            {
                ProcessedImage = Bitmap.CreateBitmap(mat.Cols(), mat.Rows(), Bitmap.Config.Argb8888);
                Utils.MatToBitmap(mat, ProcessedImage);
                string directoryPath = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + DIR_PROCESSING;
                filePath = System.IO.Path.Combine(directoryPath, filename);
                Directory.CreateDirectory(directoryPath);
                FileStream fileStream = new FileStream(filePath, FileMode.Create);
                ProcessedImage.Compress(Bitmap.CompressFormat.Png, 100, fileStream);
                fileStream.Close();
            }

            // Release memory
            GC.Collect();

            return filePath;
        }


        /// <summary>
        /// Print the board in log for debug purposes
        /// </summary>
        private void PrintLogBoard()
        {
            StringBuilder text = new StringBuilder();
            for (int i = 0; i < BOARD_SIZE; i++)
            {
                for (int j = 0; j < BOARD_SIZE; j++)
                {
                    text.Append(Board[i, j]);
                    text.Append("\t");
                }
                text.Append("\n");
            }
            Log.Info(TAG, "Board : \n" + text);
        }
    }
}
