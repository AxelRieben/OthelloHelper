
using Android.App;
using Android.Widget;
using Android.OS;
using OpenCV.Android;
using Android.Util;
using OthelloHelper.Droid.CameraPreview;

namespace OthelloHelper.Droid
{
    [Activity(Label = "OthelloHelper", Icon = "@drawable/icon", Theme = "@style/OthelloTheme", MainLauncher = false)]
    public class ResultActivity : Activity, ILoaderCallbackInterface
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main);

            FindViewById<Button>(Resource.Id.cameraPreview)
                .Click += (s, e) => StartActivity(typeof(CameraPreviewActivity));

            //Start test image processing

            if (!OpenCVLoader.InitDebug())
            {
                Log.Debug("ResultActivity", "Internal OpenCV library not found. Using OpenCV Manager for initialization");
                OpenCVLoader.InitAsync(OpenCVLoader.OpencvVersion300, this, this);
            }
            else
            {
                Log.Debug("ResultActivity", "OpenCV library found inside package. Using it!");
                OnManagerConnected(LoaderCallbackInterface.Success);
            }

            //Finish test
        }

        public void OnManagerConnected(int loaderState)
        {
            switch (loaderState)
            {
                case LoaderCallbackInterface.Success:
                    Log.Info("ResultActivity", "OpenCV loaded successfully");
                    GridDetector gridDetector = new GridDetector();
                    gridDetector.Process("here send the picture path");
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

