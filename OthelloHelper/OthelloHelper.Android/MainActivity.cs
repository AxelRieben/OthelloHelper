using System;

using Android.App;
using Android.Content.PM;
using Android.Widget;
using Android.OS;
using Android.Content;
using System.Collections.Generic;
using Android.Provider;
using Android.Util;
using Android.Support.V7.App;
using Toolbar = Android.Support.V7.Widget.Toolbar;
using Android.Graphics;

// Source :  https://github.com/xamarin/recipes/tree/master/Recipes/android/other_ux/camera_intent/take_a_picture_and_save_using_camera_app
namespace OthelloHelper.Droid
{
    [Activity(Label = "OthelloHelper", Icon = "@drawable/icon", Theme = "@style/OthelloTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private ImageView imageView;
        private Button btnOpenCamera;
        private Button btnRotate;
        private Button btnPickFromGallery;
        private Button btnProcess;
        private RadioGroup radioGroup;
        private float rotationAngle = 0f;

        /// <summary>
        /// Called when this activity is created. If it's recreated, reload previous image.
        /// </summary>
        /// <param name="bundle"></param>
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.Main);

            // Toolbar
            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.Title = "OthelloHelper";

            // Buttons
            btnOpenCamera = FindViewById<Button>(Resource.Id.openCamera);
            btnRotate = FindViewById<Button>(Resource.Id.rotate);
            btnRotate.Enabled = false;
            btnPickFromGallery = FindViewById<Button>(Resource.Id.pickGallery);
            btnProcess = FindViewById<Button>(Resource.Id.process);
            btnProcess.Enabled = false;
            radioGroup = FindViewById<RadioGroup>(Resource.Id.btnGroupPlayerColor);

            // Image view
            imageView = FindViewById<ImageView>(Resource.Id.imageView);

            // Button listner
            btnPickFromGallery.Click += PickFromGallery;
            btnProcess.Click += BtnProcessClicked;
            btnRotate.Click += Rotate;

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
                    btnRotate.Enabled = true;
                }
                catch (Exception e)
                {
                    Log.Info("OnCreate", $"Exception on set image uri onCreate: {e}");
                }
            }
            GC.Collect();
        }

        /// <summary>
        /// Called when the user return to the app after taking or picking a picture
        /// </summary>
        /// <param name="requestCode"></param>
        /// <param name="resultCode"></param>
        /// <param name="data"></param>
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
                btnProcess.Enabled = true;
                btnRotate.Enabled = true;
            }
        }

        /// <summary>
        /// Launch camera app to let the user take a picture
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TakeApicture(object sender, EventArgs e)
        {
            Intent intent = new Intent(MediaStore.ActionImageCapture);
            ImageProperties._file = new Java.IO.File(ImageProperties._dir, String.Format("othello_{0}.jpg", Guid.NewGuid()));
            intent.PutExtra(MediaStore.ExtraOutput, Android.Net.Uri.FromFile(ImageProperties._file));
            StartActivityForResult(intent, 0);
        }

        /// <summary>
        /// Prompt the user to pick an image from gallery app.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PickFromGallery(object sender, EventArgs e)
        {
            var imageIntent = new Intent();
            imageIntent.SetType("image/*");
            imageIntent.SetAction(Intent.ActionGetContent);
            StartActivityForResult(
                Intent.CreateChooser(imageIntent, "Select photo"), 0);
        }

        /// <summary>
        /// Rotate the bitmap used in image view by 90 degrees.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Rotate(object sender, EventArgs e)
        {
            rotationAngle = (rotationAngle + 90) % 360;
            if (ImageProperties.uri != null)
            {
                var bitmap = MediaStore.Images.Media.GetBitmap(ContentResolver, ImageProperties.uri);
                var matrix = new Matrix();
                matrix.PostRotate(rotationAngle);
                imageView.SetImageBitmap(Bitmap.CreateBitmap(bitmap, 0, 0, bitmap.Width, bitmap.Height, matrix, true));
            }
            GC.Collect();
        }

        /// <summary>
        /// Launch ResultActivity to process the given image
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnProcessClicked(object sender, EventArgs e)
        {
            var image_path = ImageProperties.uri.ToString();
            bool isWhite = true;
            switch (radioGroup.CheckedRadioButtonId)
            {
                case Resource.Id.rbtnBlack:
                    isWhite = false;
                    break;
            }
            Log.Info("MainActivity", $"path : {image_path}");
            Intent intent = new Intent(this, typeof(ResultActivity));
            intent.PutExtra("image_path", image_path);
            intent.PutExtra("is_white", isWhite);
            intent.PutExtra("rotation_angle", rotationAngle);
            StartActivity(intent);
        }

        /// <summary>
        /// Tell if the device can take a picture
        /// </summary>
        /// <returns></returns>
        private bool IsThereAnAppToTakePictures()
        {
            Intent intent = new Intent(MediaStore.ActionImageCapture);
            IList<ResolveInfo> availableActivities =
                PackageManager.QueryIntentActivities(intent, PackageInfoFlags.MatchDefaultOnly);
            return availableActivities != null && availableActivities.Count > 0;
        }

        /// <summary>
        /// Create directory on filesystem to store picture take from the application
        /// </summary>
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

