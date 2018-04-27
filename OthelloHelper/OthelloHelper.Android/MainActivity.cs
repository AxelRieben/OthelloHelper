using System;

using Android.App;
using Android.Content.PM;
using Android.Widget;
using Android.OS;
using Android.Content;
using System.Collections.Generic;
using Android.Provider;
using Android.Util;

// Source :  https://github.com/xamarin/recipes/tree/master/Recipes/android/other_ux/camera_intent/take_a_picture_and_save_using_camera_app
namespace OthelloHelper.Droid
{
    [Activity(Label = "OthelloHelper", Icon = "@drawable/icon", Theme = "@style/OthelloTheme", MainLauncher = true)]
    public class MainActivity : Activity
    {
        private ImageView imageView;
        private Button btnOpenCamera;
        private Button btnPickFromGallery;
        private Button btnProcess;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.Main);

            // Toolbar
            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetActionBar(toolbar);
            ActionBar.Title = "OthelloHelper";

            // Buttons
            btnOpenCamera = FindViewById<Button>(Resource.Id.openCamera);
            btnPickFromGallery = FindViewById<Button>(Resource.Id.pickGallery);
            btnProcess = FindViewById<Button>(Resource.Id.process);
            btnProcess.Enabled = true;

            // Image view
            imageView = FindViewById<ImageView>(Resource.Id.imageView);

            // Button listner
            btnPickFromGallery.Click += PickFromGallery;
            btnProcess.Click += BtnProcessClicked;
            if (IsThereAnAppToTakePictures())
            {
                CreateDirectoryForPictures();
                btnOpenCamera.Click += TakeApicture;
            }

            // Image view content
            if (ImageProperties.uri != null)
            {
                Log.Info("OnCreate", $"Restore image from uri {ImageProperties.uri.Path}");
                try
                {
                    imageView.SetImageURI(ImageProperties.uri);
                    btnProcess.Enabled = true;
                }
                catch (Exception e)
                {
                    Log.Info("OnCreate", $"Exception on set image uri onCreate: {e}");
                }
            }
            GC.Collect();
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            Android.Net.Uri uri;
            if (resultCode == Result.Ok)
            {
                if (ImageProperties._file != null)
                {
                    uri = Android.Net.Uri.FromFile(ImageProperties._file);
                    ImageProperties._file = null;
                }
                else
                {
                    uri = data.Data;
                }
                ImageProperties.uri = uri;
                imageView.SetImageURI(uri);
            }
            GC.Collect();
        }

        private void TakeApicture(object sender, EventArgs e)
        {
            Intent intent = new Intent(MediaStore.ActionImageCapture);
            ImageProperties._file = new Java.IO.File(ImageProperties._dir, String.Format("othello_{0}.jpg", Guid.NewGuid()));
            intent.PutExtra(MediaStore.ExtraOutput, Android.Net.Uri.FromFile(ImageProperties._file));
            StartActivityForResult(intent, 0);
        }
        private void PickFromGallery(object sender, EventArgs e)
        {
            var imageIntent = new Intent();
            imageIntent.SetType("image/*");
            imageIntent.SetAction(Intent.ActionGetContent);
            StartActivityForResult(
                Intent.CreateChooser(imageIntent, "Select photo"), 0);
        }

        private void BtnProcessClicked(object sender, EventArgs e)
        {
            Toast.MakeText(this, "Process started", ToastLength.Short).Show();
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
            ImageProperties._dir = new Java.IO.File(
                Android.OS.Environment.GetExternalStoragePublicDirectory(
                    Android.OS.Environment.DirectoryPictures), "OthelloHelper");
            if (!ImageProperties._dir.Exists())
            {
                ImageProperties._dir.Mkdirs();
            }
        }
    }

    public static class ImageProperties
    {
        public static Java.IO.File _file;
        public static Java.IO.File _dir;
        public static Android.Net.Uri uri;
    }
}

