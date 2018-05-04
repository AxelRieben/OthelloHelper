using Android.App;
using Android.Widget;
using Android.OS;
using OpenCV.Android;
using Android.Util;
using Android.Support.V7.App;
using Toolbar = Android.Support.V7.Widget.Toolbar;
using AlertDialog = Android.Support.V7.App.AlertDialog;
using Android.Graphics;
using Android.Provider;
using System.Threading.Tasks;
using OthelloIACastellaRieben;
using System;
using System.Threading;
using Android.Content.PM;

namespace OthelloHelper.Droid
{
    [Activity(Label = "OthelloHelper", Icon = "@drawable/icon", Theme = "@style/OthelloTheme", MainLauncher = false, ScreenOrientation = ScreenOrientation.Portrait)]
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

        /// <summary>
        /// Called when this activity is created.
        /// </summary>
        /// <param name="bundle"></param>
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
            float rotationAngle = Intent.Extras.GetFloat("rotation_angle");

            textResult = FindViewById<TextView>(Resource.Id.textResult);
            textResult.Text = "Player " + playerColor + " should play on cell ...";
            imageView = FindViewById<ImageView>(Resource.Id.imageView);

            Log.Info(TAG, $"path : {image_path}");

            try
            {
                bitmap = MediaStore.Images.Media.GetBitmap(ContentResolver, Android.Net.Uri.Parse(image_path));
                if (rotationAngle != 0f)
                {
                    var matrix = new Matrix();
                    matrix.PostRotate(rotationAngle);
                    bitmap = Bitmap.CreateBitmap(bitmap, 0, 0, bitmap.Width, bitmap.Height, matrix, true);
                }
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

        /// <summary>
        /// Called when the OpenCV library has been loaded. 
        /// Start image and AI processing
        /// </summary>
        /// <param name="loaderState"></param>
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

        /// <summary>
        /// Start Image processing in separate thread, with progress dialog.
        /// </summary>
        public void ProcessAsync()
        {
            // Image processing
            var progressDialogImageProcessing = ProgressDialog.Show(this, "Please wait...", "Processing image (1/2)", true);
            new Thread(new ThreadStart(
                async delegate
                {
                    Log.Info(TAG, "Creating task");
                    Task<int[,]> task = GridProcessAsync();
                    Log.Info(TAG, "Awaiting task");
                    var tabBoard = await task;

                    RunOnUiThread(() =>
                    {
                        progressDialogImageProcessing.Hide();
                        WorkIA(tabBoard);
                    });
                })).Start();
        }

        /// <summary>
        /// Start AI processing in separate thread, with progress dialog.
        /// </summary>
        /// <param name="tabBoard"></param>
        void WorkIA(int[,] tabBoard)
        {
            // IA
            var progressDialogIA = ProgressDialog.Show(this, "Please wait...", "Processing IA (2/2)", true);
            new Thread(new ThreadStart(
                async delegate
                {
                    Log.Info(TAG, "Work IA");
                    var bestMove = await IAProcessAsync(tabBoard);
                    string file = null;
                    if (bestMove.Item1 != -1 && bestMove.Item2 != -1)
                    {
                        file = gridDetector.DrawBestMove(bestMove.Item1, bestMove.Item2);
                    }

                    RunOnUiThread(() =>
                        {
                            progressDialogIA.Hide();
                            if (file != null)
                            {
                                textResult.Text = "Player " + playerColor + " should play on cell " + $"({bestMove.Item1 + 1};{bestMove.Item2 + 1})";
                                imageView.SetImageURI(Android.Net.Uri.Parse(file));
                            }
                            else
                            {
                                ShowErrorDialog();
                            }
                        });
                })).Start();
        }

        private void ShowErrorDialog()
        {
            AlertDialog.Builder alert = new AlertDialog.Builder(this);
            alert.SetTitle("Error");
            alert.SetMessage("Application cannot find a playable move.");
            alert.SetNeutralButton("Ok", delegate { return; });
        }

        /// <summary>
        /// Async method to get the board in the picture.
        /// </summary>
        /// <returns></returns>
        public async Task<int[,]> GridProcessAsync()
        {
            var board = await Task.Run(() =>
            {
                gridDetector = new GridDetector(bitmap);
                gridDetector.Process();
                return gridDetector.Board;
            });
            return board;
        }

        /// <summary>
        /// Async method to guess the best move to play.
        /// </summary>
        /// <param name="tabBoard"></param>
        /// <returns></returns>
        public async Task<Tuple<int, int>> IAProcessAsync(int[,] tabBoard)
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

