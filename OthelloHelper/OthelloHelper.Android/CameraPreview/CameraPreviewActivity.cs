using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using OpenCV.Android;
using OpenCV.Core;
using OpenCV.ImgProc;
using OthelloIA_G3;

namespace OthelloHelper.Droid.CameraPreview
{
    
    [Activity(Label = CameraPreview,
        ScreenOrientation = ScreenOrientation.Landscape,
        ConfigurationChanges = ConfigChanges.KeyboardHidden | ConfigChanges.Orientation
        //,Theme="@android:style/Theme.NoTitleBar.FullScreen"
        )]
    public class CameraPreviewActivity : Activity, ILoaderCallbackInterface, CameraBridgeViewBase.ICvCameraViewListener
    {
        public const string CameraPreview = "CameraPreview";
        private CameraBridgeViewBase _openCvCameraView;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Window.AddFlags(WindowManagerFlags.KeepScreenOn);
            SetContentView(Resource.Layout.CameraPreview);
            _openCvCameraView = FindViewById<CameraBridgeViewBase>(Resource.Id.surfaceView);
            _openCvCameraView.Visibility = ViewStates.Visible;
            _openCvCameraView.SetCvCameraViewListener(this);

            //SetContentView(Resource.Layout.Main);

            //StartActivity(typeof(CameraPreviewActivity));
        }

        protected override void OnPause()
        {
            base.OnPause();
            if (_openCvCameraView != null)
            {
                _openCvCameraView.DisableView();
            }
        }

        protected override void OnResume()
        {
            base.OnResume();

            if (!OpenCVLoader.InitDebug())
            {
                Log.Debug(CameraPreview, "Internal OpenCV library not found. Using OpenCV Manager for initialization");
                OpenCVLoader.InitAsync(OpenCVLoader.OpencvVersion300, this, this);
            }
            else
            {
                Log.Debug(CameraPreview, "OpenCV library found inside package. Using it!");
                OnManagerConnected(LoaderCallbackInterface.Success);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_openCvCameraView != null)
            {
                _openCvCameraView.DisableView();
            }
        }

        public void OnManagerConnected(int p0)
        {
            switch (p0)
            {
                case LoaderCallbackInterface.Success:
                    Log.Info(CameraPreview, "OpenCV loaded successfully");
                    _openCvCameraView.EnableView();
                    break;
                default:
                    break;
            }
        }

        public void OnPackageInstall(int p0, IInstallCallbackInterface p1)
        {

        }

        public void OnCameraViewStarted(int p0, int p1)
        {

        }

        public void OnCameraViewStopped()
        {

        }

        public Mat OnCameraFrame(Mat p0)
        {
            Mat p1 = new Mat();
            Imgproc.Canny(p0, p1, 255, 64);
            return p1;
        }
    }
}