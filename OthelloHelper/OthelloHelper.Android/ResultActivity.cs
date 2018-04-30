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

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Result);

            // Toolbar
            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.Title = "OthelloHelper";

            //FindViewById<Button>(Resource.Id.cameraPreview)
            //    .Click += (s, e) => StartActivity(typeof(CameraPreviewActivity));

            //Start test image processing

            image_path = Intent.Extras.GetString("image_path");
            Log.Info(TAG, $"path : {image_path}");

            textResult = FindViewById<TextView>(Resource.Id.textResult);
            imageView = FindViewById<ImageView>(Resource.Id.imageView);
            textResult.Text = $"Image path : {image_path}";

            Log.Info(TAG, $"path : {image_path}");

            try
            {
                bitmap = MediaStore.Images.Media.GetBitmap(ContentResolver, Android.Net.Uri.Parse(image_path));
                Log.Info(TAG, $"Bitmap :  {bitmap}\nByteCount : {bitmap.ByteCount}");
                imageView.SetImageBitmap(bitmap);
            }
            catch (System.Exception)
            {
                Log.Warn(TAG, $"Can't create bitmap");
            }

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

            //Finish test
        }

        public void OnManagerConnected(int loaderState)
        {
            switch (loaderState)
            {
                case LoaderCallbackInterface.Success:
                    Log.Info(TAG, "OpenCV loaded successfully");
                    GridDetector gridDetector = new GridDetector();
                    gridDetector.Process(bitmap);
                    break;
                default:
                    break;
            }
        }

        public void OnPackageInstall(int p0, IInstallCallbackInterface p1)
        {
            //Nothing
        }
    }
}

