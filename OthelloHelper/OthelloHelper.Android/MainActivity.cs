using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using OpenCV.Core;
using OpenCV.Android;
using OthelloHelper.Droid.CameraPreview;
using Android.Graphics;
using Java.IO;
using Android.Content;
using System.Collections.Generic;
using Android.Provider;
using Android.Net;
using System.IO;

namespace OthelloHelper.Droid
{
    [Activity(Label = "OthelloHelper", Icon = "@drawable/icon", Theme = "@style/MainTheme", MainLauncher = true)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity //Activity
    {
        private ImageView imageView;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            //TabLayoutResource = Resource.Layout.Tabbar;
            //ToolbarResource = Resource.Layout.Toolbar;

            //global::Xamarin.Forms.Forms.Init(this, bundle);
            //LoadApplication(new App());

            SetContentView(Resource.Layout.Main);
            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetActionBar(toolbar);

            if (IsThereAnAppToTakePictures())
            {
                CreateDirectoryForPictures();

                Button button = FindViewById<Button>(Resource.Id.openCamera);
                imageView = FindViewById<ImageView>(Resource.Id.imageView);
                button.Click += TakeApicture;
            }
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            Intent mediaScanIntent = new Intent(Intent.ActionMediaScannerScanFile);
            Android.Net.Uri contentUri = Android.Net.Uri.FromFile(AppFile._file);
            mediaScanIntent.SetData(contentUri);
            SendBroadcast(mediaScanIntent);

            int height = Resources.DisplayMetrics.HeightPixels;
            int width = imageView.Height;
            AppFile.bitmap = AppFile._file.Path.LoadAndResizeBitmap(width, height);
            if (AppFile.bitmap != null)
            {
                imageView.SetImageBitmap(AppFile.bitmap);
                AppFile.bitmap = null;
            }

            // Dispose of the Java side bitmap.
            GC.Collect();
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            try
            {
                MemoryStream stream = new MemoryStream();
                AppFile.bitmap.Compress(Bitmap.CompressFormat.Png, 0, stream);
                byte[] bitmapData = stream.ToArray();
                outState.PutByteArray("bitmap", bitmapData);
            }
            catch (Exception e)
            {

            }

            base.OnSaveInstanceState(outState);
        }

        protected override void OnRestoreInstanceState(Bundle savedInstanceState)
        {
            base.OnRestoreInstanceState(savedInstanceState);

            try
            {
                var byteArray = savedInstanceState.GetByteArray("bitmap");
                if (byteArray != null)
                {
                    Toast.MakeText(this, $"Length : {byteArray.Length}", ToastLength.Short);
                    AppFile.bitmap = BitmapFactory.DecodeByteArray(byteArray, 0, byteArray.Length);
                    imageView.SetImageBitmap(AppFile.bitmap);
                }
                Toast.MakeText(this, $"Byte array null", ToastLength.Short);
            }
            catch
            {
                Toast.MakeText(this, $"Catch", ToastLength.Short);
            }
        }

        private void TakeApicture(object sender, EventArgs e)
        {
            Intent intent = new Intent(MediaStore.ActionImageCapture);
            AppFile._file = new Java.IO.File(AppFile._dir, String.Format("othello_{0}.jpg", Guid.NewGuid()));
            intent.PutExtra(MediaStore.ExtraOutput, Android.Net.Uri.FromFile(AppFile._file));
            StartActivityForResult(intent, 0);
        }

        private bool IsThereAnAppToTakePictures()
        {
            Intent intent = new Intent(MediaStore.ActionImageCapture);
            IList<ResolveInfo> availableActivities =
                PackageManager.QueryIntentActivities(intent, PackageInfoFlags.MatchDefaultOnly);
            return availableActivities != null && availableActivities.Count > 0;
        }

        private void CreateDirectoryForPictures()
        {
            AppFile._dir = new Java.IO.File(
                Android.OS.Environment.GetExternalStoragePublicDirectory(
                    Android.OS.Environment.DirectoryPictures), "OthelloHelper");
            if (!AppFile._dir.Exists())
            {
                AppFile._dir.Mkdirs();
            }
        }
    }

    public static class AppFile
    {
        public static Java.IO.File _file;
        public static Java.IO.File _dir;
        public static Bitmap bitmap;
    }
}

