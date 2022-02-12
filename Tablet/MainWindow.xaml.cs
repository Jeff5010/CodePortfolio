using System;
using System.Collections.Generic;
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
using System.Net;
using System.Net.Sockets;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using WPFMediaKit.DirectShow.Controls;
using DirectShowLib;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;

namespace TabletPicture
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Socket sendsocket;
        string ipAddress = "0";

        string imageCount = "0";
        int imageReceiveCount = 0;

        DispatcherTimer imageCheckTimer = new DispatcherTimer();        // How often we check for a new image from the tablet
        DispatcherTimer exitTimer = new DispatcherTimer();         // How much time we wait to close the wall

        public MainWindow()
        {
            InitializeComponent();

            imageCheckTimer.Tick += new EventHandler(imageCheckTimer_Tick);
            imageCheckTimer.Interval = new TimeSpan(0, 0, 5);
            imageCheckTimer.Start();

            exitTimer.Tick += new EventHandler(exitTimer_Tick);
            exitTimer.Interval = new TimeSpan(0, 0, 2);

            LandingPage.Visibility = Visibility.Visible; 
            
            string[] inputNames = MultimediaUtil.VideoInputNames;//Get the camera
            VideoPlayer.VideoCaptureSource = inputNames[1];

            DsDevice[] captureDevices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            for(int idx = 0; idx < captureDevices.Length; idx++)
            {
                if(captureDevices[idx].Name == inputNames[0])
                {
                    GetAllAvailableResolutions(captureDevices[idx]);
                }
            }

            // Read in imageCount from text file
            imageCount = File.ReadAllText(@"C:\CameraApp\TabletPicture\imageCount.txt");
            imageReceiveCount = Int16.Parse(imageCount);

            if (imageReceiveCount > 0)
            {
                int startingImageCount = imageReceiveCount - 1;
                string samplePath = @"C:\CameraApp\TabletPicture\PicsFromServer\Sample_Image" + startingImageCount.ToString() + ".png";
                if (File.Exists(samplePath))
                {
                    var uriSource = new Uri(samplePath, UriKind.Absolute);
                    Sample.Source = new BitmapImage(uriSource);
                    Sample.Visibility = Visibility.Visible;
                    PlayMosaicVideoButton.Visibility = Visibility.Visible;
                }
            }

            string jpeg1Path = @"C:\CameraApp\TabletPicture\LibraryImages\photo1.jpg";
            string png1Path = @"C:\CameraApp\TabletPicture\LibraryImages\photo1.png";
            if (File.Exists(jpeg1Path) && !File.Exists(png1Path))
            {
                BitmapImage bitmapSource = new BitmapImage();
                bitmapSource.BeginInit();
                bitmapSource.UriSource = new Uri(jpeg1Path, UriKind.Absolute);
                bitmapSource.EndInit();

                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

                using (MemoryStream stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    Byte[] pics = stream.ToArray();
                    //save Picture 
                    File.WriteAllBytes(png1Path, pics);
                }
            }

            string jpeg2Path = @"C:\CameraApp\TabletPicture\LibraryImages\photo2.jpg";
            string png2Path = @"C:\CameraApp\TabletPicture\LibraryImages\photo2.png";
            if (File.Exists(jpeg2Path) && !File.Exists(png2Path))
            {
                BitmapImage bitmapSource = new BitmapImage();
                bitmapSource.BeginInit();
                bitmapSource.UriSource = new Uri(jpeg2Path, UriKind.Absolute);
                bitmapSource.EndInit();

                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

                using (MemoryStream stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    Byte[] pics = stream.ToArray();
                    //save Picture 
                    File.WriteAllBytes(png2Path, pics);
                }
            }

            string jpeg3Path = @"C:\CameraApp\TabletPicture\LibraryImages\photo3.jpg";
            string png3Path = @"C:\CameraApp\TabletPicture\LibraryImages\photo3.png";
            if (File.Exists(jpeg3Path) && !File.Exists(png3Path))
            {
                BitmapImage bitmapSource = new BitmapImage();
                bitmapSource.BeginInit();
                bitmapSource.UriSource = new Uri(jpeg3Path, UriKind.Absolute);
                bitmapSource.EndInit();

                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

                using (MemoryStream stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    Byte[] pics = stream.ToArray();
                    //save Picture 
                    File.WriteAllBytes(png3Path, pics);
                }
            }

            // Read in server IP Address from text file
            ipAddress = File.ReadAllText("C:\\CameraApp\\TabletPicture\\ipaddress.txt");

            byte[] data = new byte[1024];
            IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(ipAddress), 9050);

            sendsocket = new Socket(AddressFamily.InterNetwork,
                            SocketType.Stream, ProtocolType.Tcp);

            try
            {
                sendsocket.Connect(ipep);
            }
            catch (SocketException e)
            {
                Console.WriteLine("Unable to connect to server.");
                Console.WriteLine(e.ToString());
                Console.ReadLine();
            }
        }

        private static int SendVarData(Socket s, byte[] data)
        {
            int total = 0;
            int size = data.Length;
            int dataleft = size;
            int sent;

            byte[] datasize = new byte[4];
            datasize = BitConverter.GetBytes(size);
            sent = s.Send(datasize);

            while (total < size)
            {
                sent = s.Send(data, total, dataleft, SocketFlags.None);
                total += sent;
                dataleft -= sent;
            }
            return total;
        }

        private static byte[] ReceiveVarData(Socket s)
        {
            int total = 0;
            int recv;
            byte[] datasize = new byte[4];

            recv = s.Receive(datasize, 0, 4, 0);
            int size = BitConverter.ToInt32(datasize, 0);
            int dataleft = size;
            byte[] data = new byte[size];


            while (total < size)
            {
                recv = s.Receive(data, total, dataleft, 0);
                if (recv == 0)
                {
                    break;
                }
                total += recv;
                dataleft -= recv;
            }
            return data;
        }

        void imageCheckTimer_Tick(object sender, EventArgs e)
        {
            string samplePath = @"C:\CameraApp\TabletPicture\PicsFromServer\Sample_Image" + imageCount + ".png";
            if (File.Exists(samplePath))
            {
                var uriSource = new Uri(samplePath, UriKind.Absolute);
                Sample.Source = new BitmapImage(uriSource);
                Sample.Visibility = Visibility.Visible;
                PlayMosaicVideoButton.Visibility = Visibility.Visible;

                imageReceiveCount++;
                imageCount = imageReceiveCount.ToString();
                File.WriteAllText(@"C:\CameraApp\TabletPicture\imageCount.txt", imageCount);
            }
        }

        public void ExitProgram(object sender, RoutedEventArgs e)
        {
            // send a message to to the PC that the tablet has closed
            byte[] message = System.Text.Encoding.ASCII.GetBytes("Close");
            int send = SendVarData(sendsocket, message);

            exitTimer.Start();
        }


        void exitTimer_Tick(object sender, EventArgs e)
        {
            exitTimer.Stop();

            Process.GetCurrentProcess().Kill();
            //System.Windows.Application.Current.Shutdown();
        }

        public void OnClick_GoToMenu(object sender, RoutedEventArgs e)
        {
            LandingPage.Visibility = Visibility.Hidden;
            Menu.Visibility = Visibility.Visible;
        }

        public void OnClick_GoToPlayMosaic(object sender, RoutedEventArgs e)
        {
            LandingPage.Visibility = Visibility.Hidden;
            PlayMosaicVideoScreen.Visibility = Visibility.Visible;
        }

        public void OnClick_GoToCamera(object sender, RoutedEventArgs e)
        {
            //cameraCapture.StartCapture();
            Camera.Visibility = Visibility.Visible;
            Menu.Visibility = Visibility.Hidden;
            Approval.Visibility = Visibility.Hidden;
        }

        public void OnClick_GoToGallery(object sender, RoutedEventArgs e)
        {
            photoSelector_1.Visibility = Visibility.Hidden;
            photoSelector_2.Visibility = Visibility.Hidden;
            photoSelector_3.Visibility = Visibility.Hidden;

            Gallery.Visibility = Visibility.Visible;
            Menu.Visibility = Visibility.Hidden;
        }

        public void OnClick_PlayMosaic(object sender, RoutedEventArgs e)
        {
            // send a message to play the last video to the PC
            byte[] message = System.Text.Encoding.ASCII.GetBytes("Play");
            int send = SendVarData(sendsocket, message);

            FinalScreen.Visibility = Visibility.Visible;
            Menu.Visibility = Visibility.Hidden;
        }

        string buttonNumber = "";
        public void OnClick_SelectPhoto(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            buttonNumber = b.Name.Replace("StoredPhoto", "");

            int number = int.Parse(buttonNumber);

            if(number == 1)
            {
                photoSelector_1.Visibility = Visibility.Visible;
                photoSelector_2.Visibility = Visibility.Hidden;
                photoSelector_3.Visibility = Visibility.Hidden;
            }
            else if(number == 2)
            {
                photoSelector_1.Visibility = Visibility.Hidden;
                photoSelector_2.Visibility = Visibility.Visible;
                photoSelector_3.Visibility = Visibility.Hidden;
            }
            else if(number == 3)
            {
                photoSelector_1.Visibility = Visibility.Hidden;
                photoSelector_2.Visibility = Visibility.Hidden;
                photoSelector_3.Visibility = Visibility.Visible;
            }
        }

        public void OnClick_SendPhoto(object sender, RoutedEventArgs e)
        {
            // send a message with the chosen image to the PC
            byte[] imageBytes = null;
            BitmapEncoder encoder = new PngBitmapEncoder();

            BitmapImage bitmapSource = new BitmapImage();
            bitmapSource.BeginInit();
            bitmapSource.UriSource = new Uri(@"C:\CameraApp\TabletPicture\LibraryImages\photo" + buttonNumber + ".png", UriKind.RelativeOrAbsolute);
            bitmapSource.EndInit();

            if (bitmapSource != null)
            {
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

                using (var stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    imageBytes = stream.ToArray();
                }
            }

            int send = SendVarData(sendsocket, imageBytes);

            FinalScreen.Visibility = Visibility.Visible;
            Gallery.Visibility = Visibility.Hidden;
            photoSelector_1.Visibility = Visibility.Hidden;
            photoSelector_2.Visibility = Visibility.Hidden;
            photoSelector_3.Visibility = Visibility.Hidden;

            // Hide the Play Mosaic button until the render is complete
            PlayMosaicVideoButton.Visibility = Visibility.Hidden;
            Sample.Visibility = Visibility.Hidden;
        }

        public void OnClick_TakePhoto(object sender, RoutedEventArgs e)
        {
            //cameraCapture.StopCapture();

            PresentationSource source = PresentationSource.FromVisual(VideoPlayer);
            //RenderTargetBitmap bitmap = new RenderTargetBitmap(1005, 1005, 96, 96, PixelFormats.Default);
            RenderTargetBitmap bitmap = new RenderTargetBitmap((int)VideoPlayer.RenderSize.Width, (int)VideoPlayer.RenderSize.Height, 96, 96, PixelFormats.Pbgra32);

            VisualBrush sourceBrush = new VisualBrush(VideoPlayer);
            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();
            using (drawingContext)
            {
                //drawingContext.DrawRectangle(sourceBrush, null, new Rect(new System.Windows.Point(0, 0), new System.Windows.Point(1005, 1005)));
                drawingContext.DrawRectangle(sourceBrush, null, new Rect(new System.Windows.Point(0, 0), new System.Windows.Point(VideoPlayer.RenderSize.Width, VideoPlayer.RenderSize.Height)));
            }
            bitmap.Render(drawingVisual);
            CroppedBitmap cropBitmap = new CroppedBitmap(bitmap, new Int32Rect(420, 0, 1080, 1080));
            /*
            RenderTargetBitmap bitmap = new RenderTargetBitmap((int)VideoPlayer.ActualHeight, (int)VideoPlayer.ActualHeight, 96, 96, PixelFormats.Default);

            //VideoPlayer.Stretch = Stretch.Fill;
            //VideoPlayer.Measure(VideoPlayer.RenderSize);
            //VideoPlayer.Arrange(new Rect(VideoPlayer.RenderSize));


            bitmap.Render(VideoPlayer);
            */
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(cropBitmap));
            //encoder.Frames.Add(BitmapFrame.Create(bitmap));

            string fullPath = "";
            using (MemoryStream stream = new MemoryStream())
            {
                encoder.Save(stream);
                Byte[] pics = stream.ToArray();
                string fileName = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                fullPath = System.IO.Path.Combine(@"C:\CameraApp\TabletPicture\TabletPicture", fileName + "_photo.png");
                //save Picture 
                File.WriteAllBytes(fullPath, pics);
            }

            capturedImage.Source = cropBitmap;
            //capturedImage.Source = bitmap;

            Approval.Visibility = Visibility.Visible;
            Camera.Visibility = Visibility.Hidden;
        }

        public void OnClick_ApprovePhoto(object sender, RoutedEventArgs e)
        {
            //cameraCapture.SaveToPng(capturedImage, "photo.png");
            

            // send a message with the approved image to the PC

            byte[] imageBytes = null;
            BitmapEncoder encoder = new PngBitmapEncoder(); ;
            var bitmapSource = capturedImage.Source as BitmapSource;

            if (bitmapSource != null)
            {
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

                using (var stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    imageBytes = stream.ToArray();
                }
            }

            int send = SendVarData(sendsocket, imageBytes);

            // Update the screens that are visible
            FinalScreen.Visibility = Visibility.Visible;
            Approval.Visibility = Visibility.Hidden;

            // Hide the Play Mosaic button until the render is complete
            PlayMosaicVideoButton.Visibility = Visibility.Hidden;
            Sample.Visibility = Visibility.Hidden;
        }
        
        public void OnClick_BackToStart(object sender, RoutedEventArgs e)
        {
            LandingPage.Visibility = Visibility.Visible;
            FinalScreen.Visibility = Visibility.Hidden;
            PlayMosaicVideoScreen.Visibility = Visibility.Hidden;
            Gallery.Visibility = Visibility.Hidden;
            Approval.Visibility = Visibility.Hidden;
            Camera.Visibility = Visibility.Hidden;
            Menu.Visibility = Visibility.Hidden;
        }

        public void OnClick_BackToMenu(object sender, RoutedEventArgs e)
        {
            Menu.Visibility = Visibility.Visible;
            Camera.Visibility = Visibility.Hidden;
            Gallery.Visibility = Visibility.Hidden;
        }

        public static void GetAllAvailableResolutions(DsDevice vidDev)
        {
            try
            {
                int hr;
                int max = 0;
                int bitCount = 0;
                IBaseFilter sourceFilter = null;
                var m_FilterGraph2 = new FilterGraph() as IFilterGraph2;
                hr = m_FilterGraph2.AddSourceFilterForMoniker(vidDev.Mon, null, vidDev.Name, out sourceFilter);
                var pRaw2 = DsFindPin.ByCategory(sourceFilter, PinCategory.Capture, 0);
                var AvailableResolutions = new List<System.Windows.Point>();
                VideoInfoHeader v = new VideoInfoHeader();
                IEnumMediaTypes mediaTypeEnum;
                hr = pRaw2.EnumMediaTypes(out mediaTypeEnum);
                AMMediaType[] mediaTypes = new AMMediaType[1];
                IntPtr fetched = IntPtr.Zero;
                hr = mediaTypeEnum.Next(1, mediaTypes, fetched);

                while(fetched != null && mediaTypes[0] != null)
                {
                    Marshal.PtrToStructure(mediaTypes[0].formatPtr, v);
                    if (v.BmiHeader.Size != 0 && v.BmiHeader.BitCount != 0)
                    {
                        if(v.BmiHeader.BitCount > bitCount)
                        {
                            AvailableResolutions.Clear();
                            max = 0;
                            bitCount = v.BmiHeader.BitCount;
                        }
                        AvailableResolutions.Add(new System.Windows.Point(v.BmiHeader.Width, v.BmiHeader.Height));
                        Debug.WriteLine(v.BmiHeader.Width);
                        Debug.WriteLine(v.BmiHeader.Height);
                        if(v.BmiHeader.Width > max || v.BmiHeader.Height > max)
                        {
                            max = Math.Max(v.BmiHeader.Width, v.BmiHeader.Height);
                        }
                    }
                    hr = mediaTypeEnum.Next(1, mediaTypes, fetched);
                }
                return;
            }

            catch
            {
                return;
            }
        }
    }
}
