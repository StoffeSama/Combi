using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using Microsoft.ProjectOxford.Face;

namespace MyFirstApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        private readonly IFaceServiceClient faceServiceClient = new FaceServiceClient("bcba6dd40af64f31b200339e2322bf1c");
        private string personGroupId = "haxxorgroup";
        private Stream s;

        public MainWindow()
        {
            InitializeComponent();



        }


        //20 api calls per minut grej
        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {


            var openDlg = new Microsoft.Win32.OpenFileDialog();

            openDlg.Filter = "JPEG images (*.jpeg)|*.jpeg|JPG images (*.jpg)|*.jpg|PNG images (*.png)|*.png|BMP images (*.bmp)|*.bmp"
                    + "|All Files (*.*)|*.*";
            bool? result = openDlg.ShowDialog(this);

            if (!(bool)result)
            {
                return;
            }

            string filePath = openDlg.FileName;

            Uri fileUri = new Uri(filePath);
            BitmapImage bitmapSource = new BitmapImage();

            bitmapSource.BeginInit();
            bitmapSource.CacheOption = BitmapCacheOption.None;
            bitmapSource.UriSource = fileUri;
            bitmapSource.EndInit();

            FacePhoto.Source = bitmapSource;
            Title = "Detecting...";
            FaceRectangle[] faceRects = await UploadAndDetectFaces(filePath);
            Title = String.Format("Detection Finished. {0} face(s) detected", faceRects.Length);


            using (s = File.OpenRead(filePath))
            {
                var faces = await faceServiceClient.DetectAsync(s);
                var faceIds = faces.Select(face => face.FaceId).ToArray();

          

                var results = await faceServiceClient.IdentifyAsync(personGroupId, faceIds);
 

                foreach (var identifyResult in results)
                {
                    //    Console.WriteLine("Result of face: {0}", identifyResult.FaceId);


                    if (identifyResult.Candidates.Length == 0)
                    {
                        Console.WriteLine("No one identified");
                        outputBox.Text = "No one identified";
                        confidenceBox.Text = "Confidence: " + 0;
                    }
                    else
                    {
                        // Get top 1 among all candidates returned
                        var candidateId = identifyResult.Candidates[0].PersonId;
                        var ress = identifyResult.Candidates[0].Confidence;
                        var person = await faceServiceClient.GetPersonAsync(personGroupId, candidateId);
                       

                        Console.WriteLine(ress);
                        confidenceBox.Text= "Confidence: " + ress;
                        Console.WriteLine("Identified as {0}", person.Name);
                        outputBox.Text = "Identified as " + person.Name;
                    }
                    if (faceRects.Length > 0)
                    {
                        DrawingVisual visual = new DrawingVisual();
                        DrawingContext drawingContext = visual.RenderOpen();
                        drawingContext.DrawImage(bitmapSource,
                            new Rect(0, 0, bitmapSource.Width, bitmapSource.Height));
                        double dpi = bitmapSource.DpiX;
                        double resizeFactor = 96 / dpi;

                        foreach (var faceRect in faceRects)
                        {
                            drawingContext.DrawRectangle(
                                Brushes.Transparent,
                                new Pen(Brushes.Red, 2),
                                new Rect(
                                    faceRect.Left * resizeFactor,
                                    faceRect.Top * resizeFactor,
                                    faceRect.Width * resizeFactor,
                                    faceRect.Height * resizeFactor
                                    )
                            );
                        }

                        drawingContext.Close();
                        RenderTargetBitmap faceWithRectBitmap = new RenderTargetBitmap(
                            (int)(bitmapSource.PixelWidth * resizeFactor),
                            (int)(bitmapSource.PixelHeight * resizeFactor),
                            96,
                            96,
                            PixelFormats.Pbgra32);

                        faceWithRectBitmap.Render(visual);
                        FacePhoto.Source = faceWithRectBitmap;
                    }
                }
            }

        }

        private async Task<FaceRectangle[]> UploadAndDetectFaces(string imageFilePath)
        {
            try
            {
                using (Stream imageFileStream = File.OpenRead(imageFilePath))
                {
                    var faces = await faceServiceClient.DetectAsync(imageFileStream);
                    var faceRects = faces.Select(face => face.FaceRectangle);
                    return faceRects.ToArray();
                }
            }
            catch (Exception)
            {
                return new FaceRectangle[0];
            }
        }



        private async void CreatePerson_Click(object sender, RoutedEventArgs e)
        {
            string name = textBox.Text;
            Console.WriteLine(name + " " + "added");

            // Define Anna
            CreatePersonResult friend1 = await faceServiceClient.CreatePersonAsync(
                // Id of the person group that the person belonged to
                personGroupId,
                // Name of the person
                name

            );
            var openDlg = new Microsoft.Win32.OpenFileDialog();

            openDlg.Filter = "JPEG Image(*.jpeg)|*.jpeg";
            bool? result = openDlg.ShowDialog(this);

            if (!(bool)result)
            {
                return;
            }

            string filePath = openDlg.FileName;



            using (Stream s = File.OpenRead(filePath))
            {
                // Detect faces in the image and add to Anna
                await faceServiceClient.AddPersonFaceAsync(
                    personGroupId, friend1.PersonId, s);
            }

            trainGroup();

        }

        private async void trainGroup()
        {

            await faceServiceClient.TrainPersonGroupAsync(personGroupId);

            TrainingStatus trainingStatus = null;
            while (true)
            {
                trainingStatus = await faceServiceClient.GetPersonGroupTrainingStatusAsync(personGroupId);

                if (trainingStatus.Status != Status.Running)
                {
                    break;
                }

                await Task.Delay(1000);
            }

            Console.WriteLine("Group trained");
        }

        private async void deletion_Click(object sender, RoutedEventArgs e)
        {

            await faceServiceClient.DeletePersonGroupAsync(personGroupId);
            Console.WriteLine("Group deleted");
        }

        private async void Create_Click(object sender, RoutedEventArgs e)
        {

            await faceServiceClient.CreatePersonGroupAsync(personGroupId, "My Friends");

            Console.WriteLine("Group created");


        }

    }
}
    


    
