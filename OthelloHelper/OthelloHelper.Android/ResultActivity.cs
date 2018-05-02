using Android.App;
using Android.Widget;
using Android.OS;
using OpenCV.Android;
using Android.Util;
using OthelloHelper.Droid.CameraPreview;
using Android.Support.V7.App;
using Toolbar = Android.Support.V7.Widget.Toolbar;
using Android.Graphics;
using Java.IO;
using System.IO;
using Android.Provider;
using System.Threading.Tasks;
using OthelloIA_G3;
using System;
using System.Threading;

namespace OthelloHelper.Droid
{
    [Activity(Label = "OthelloHelper", Icon = "@drawable/icon", Theme = "@style/OthelloTheme", MainLauncher = false)]
    public class ResultActivity : AppCompatActivity, ILoaderCallbackInterface
    {
        private string image_path;
        private TextView textResult;
        private ImageView imageView;
        private Bitmap bitmap;
        private const string TAG = "ResultActivity";
        private bool isWhite;
        private string playerColor;
        private GridDetector gridDetector;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Result);

            // Toolbar
            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.Title = "OthelloHelper";

            image_path = Intent.Extras.GetString("image_path");
            Log.Info(TAG, $"path : {image_path}");
            isWhite = Intent.Extras.GetBoolean("is_white");
            playerColor = isWhite ? "white" : "black";

            textResult = FindViewById<TextView>(Resource.Id.textResult);
            textResult.Text = "Player " + playerColor + " should play on cell ...";
            imageView = FindViewById<ImageView>(Resource.Id.imageView);

            Log.Info(TAG, $"path : {image_path}");

            try
            {
                bitmap = MediaStore.Images.Media.GetBitmap(ContentResolver, Android.Net.Uri.Parse(image_path));
                Log.Info(TAG, $"Bitmap :  {bitmap}\nByteCount : {bitmap.ByteCount}");
                imageView.SetImageBitmap(bitmap);
            }
            catch (Exception)
            {
                Log.Warn(TAG, $"Can't create bitmap");
            }

            // Init OpenCV
            if (!OpenCVLoader.InitDebug())
            {
                Log.Debug(TAG, "Internal OpenCV library not found. Using OpenCV Manager for initialization");
                OpenCVLoader.InitAsync(OpenCVLoader.OpencvVersion300, this, this);
            }
            else
            {
                Log.Debug(TAG, "OpenCV library found inside package. Using it!");
                OnManagerConnected(LoaderCallbackInterface.Success);
            }
        }

        public void OnManagerConnected(int loaderState)
        {
            switch (loaderState)
            {
                case LoaderCallbackInterface.Success:
                    Log.Info(TAG, "OpenCV loaded successfully");
                    ProcessAsync();
                    break;
                default:
                    break;
            }
        }

        public void OnPackageInstall(int p0, IInstallCallbackInterface p1)
        {
            //Nothing
        }

        public void ProcessAsync()
        {
            // Image processing
            var progressDialogImageProcessing = ProgressDialog.Show(this, "Please wait...", "Processing image (1/2)", true);
            new Thread(new ThreadStart(
                async delegate
                {
                    Log.Info(TAG, "Creating task");
                    Task<int[,]> task = GridProcess();
                    Log.Info(TAG, "Awaiting task");
                    var tabBoard = await task;

                    RunOnUiThread(() =>
                    {
                        progressDialogImageProcessing.Hide();
                        WorkIA(tabBoard);
                    });
                })).Start();
        }

        void WorkIA(int[,] tabBoard)
        {
            // IA
            var progressDialogIA = ProgressDialog.Show(this, "Please wait...", "Processing IA (2/2)", true);
            new Thread(new ThreadStart(
                async delegate
                {
                    Log.Info(TAG, "Work IA");
                    var bestMove = await IAProcess(tabBoard);
                    var file = gridDetector.DrawBestMove(bestMove.Item1, bestMove.Item2);

                    RunOnUiThread(() =>
                        {
                            progressDialogIA.Hide();
                            textResult.Text = "Player " + playerColor + " should play on cell " + $"({bestMove.Item1 + 1};{bestMove.Item2 + 1})";
                            imageView.SetImageURI(Android.Net.Uri.Parse(file));
                        });
                })).Start();
        }

        public async Task<int[,]> GridProcess()
        {
            var board = await Task.Run(() =>
            {
                gridDetector = new GridDetector(bitmap);
                gridDetector.Process();
                return gridDetector.Board;
            });
            return board;
        }

        public async Task<Tuple<int, int>> IAProcess(int[,] tabBoard)
        {
            var bestMove = await Task.Run(() =>
            {
                Board board = new Board(tabBoard);
                return board.GetNextMove(tabBoard, 4, isWhite);
            });
            return bestMove;
        }
    }
}

