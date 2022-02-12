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
using System.Windows.Threading;
using System.IO;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace DisplayWall
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Socket sendsocket;
        string ipAddress = "0";

        string movieCount = "0";
        int imageReceiveCount = 0;
        int imageRenderCount = 0;

        DispatcherTimer imageCheckTimer = new DispatcherTimer();        // How often we check for a new image from the tablet
        DispatcherTimer rendertimer = new DispatcherTimer();       // How much time we wait to see if render process has completed
        DispatcherTimer moveVideoTimer = new DispatcherTimer();         // How much time we wait for the rendered video to be moved to the playback folder before going to the next step
        DispatcherTimer moveImageTimer = new DispatcherTimer();         // How much time we wait for the tablet image to be moved to the AE folder before starting the render
        DispatcherTimer videoEndTimer = new DispatcherTimer();         // How much time we wait once the video has ended before showing the screensaver
        DispatcherTimer exitTimer = new DispatcherTimer();         // How much time we wait to close the wall

        string latestVideo = "";
        string previewImage = "";
        Boolean playLatest = false;
        public MainWindow()
        {
            InitializeComponent();

            imageCheckTimer.Tick += new EventHandler(imageCheckTimer_Tick);
            imageCheckTimer.Interval = new TimeSpan(0, 0, 5);
            imageCheckTimer.Start();

            rendertimer.Tick += new EventHandler(renderTimer_Tick);
            rendertimer.Interval = new TimeSpan(0, 0, 10);

            moveVideoTimer.Tick += new EventHandler(moveVideoTimer_Tick);
            moveVideoTimer.Interval = new TimeSpan(0, 0, 10);

            moveImageTimer.Tick += new EventHandler(moveImageTimer_Tick);
            moveImageTimer.Interval = new TimeSpan(0, 0, 10);

            videoEndTimer.Tick += new EventHandler(videoEndTimer_Tick);
            videoEndTimer.Interval = new TimeSpan(0, 2, 0);

            exitTimer.Tick += new EventHandler(exitTimer_Tick);
            exitTimer.Interval = new TimeSpan(0, 0, 3);

            // Read in tablet IP Address from text file
            ipAddress = File.ReadAllText(@"C:\DisplayWall\ipaddress.txt");

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

            // Read in movieCount from text file
            movieCount = File.ReadAllText(@"C:\DisplayWall\movieCount.txt");
            if (Int16.Parse(movieCount) == 0)
            {
                // Default video if there is nothing previously rendered
                latestVideo = @"C:\DisplayWall\videoRender0.mp4";
            }
            else
            {
                latestVideo = @"C:\Videos\videoRender" + movieCount + ".mp4";
            }
            imageRenderCount = Int16.Parse(movieCount) + 1;
            imageReceiveCount = imageRenderCount;

            // Make sure that the Image in the AE folder is removed
            string targetDirectory = Path.GetDirectoryName(@"C:\MosaicBuild_v05\(Footage)\02_Images\");
            string targetFilename = "Image_Replace_02.png";
            string targetfilePath = Path.Combine(targetDirectory, targetFilename);
            if (File.Exists(targetfilePath))
            {
                File.Delete(targetfilePath);
            }

            Thread thread = new Thread(startListening);

            thread.Start();
        }

        Socket client;
        Socket newsock;
        private void startListening()
        {
            ////////////////////////////////////////////

            Console.WriteLine("Server is starting...");
            byte[] data = new byte[1024];
            IPEndPoint ipep = new IPEndPoint(IPAddress.Any, 9050);

            newsock = new Socket(AddressFamily.InterNetwork,
                            SocketType.Stream, ProtocolType.Tcp);

            newsock.Bind(ipep);
            newsock.Listen(10);
            Console.WriteLine("Waiting for a client...");

            client = newsock.Accept();
            IPEndPoint newclient = (IPEndPoint)client.RemoteEndPoint;
            Console.WriteLine("Connected with {0} at port {1}",
                            newclient.Address, newclient.Port);

            Boolean loop = true;
            while (loop)
            {
                data = ReceiveVarData(client);
                if (data.Length == 0)   // The tablet closed the connection, so stop the loop
                {
                    loop = false;
                }
                else
                {
                    MemoryStream ms = new MemoryStream(data);
                    try
                    {
                        //BitmapEncoder encoder = new PngBitmapEncoder();
                        string fullPath = "";

                        //encoder.Save(ms);
                        Byte[] pics = ms.ToArray();

                        var stringCheck = System.Text.Encoding.Default.GetString(pics);

                        if (string.Equals(stringCheck, "Play"))
                        {
                            playLatest = true;
                        }
                        else if(string.Equals(stringCheck, "Close"))
                        {
                            loop = false;
                        }
                        else
                        {
                            fullPath = System.IO.Path.Combine(@"C:\PicsFromTablet\Image_Replace_" + imageReceiveCount.ToString() + ".png");
                            //save Picture 
                            File.WriteAllBytes(fullPath, pics);
                            imageReceiveCount++;
                        }
                    }
                    catch (ArgumentException e)
                    {
                        Console.WriteLine("something broke");
                    }
                }
            }

            //Console.WriteLine("Disconnected from {0}", newclient.Address);
            client.Close();
            newsock.Close();
            /////////////////////////////////////////////
            // Start the listening process again
            startListening();
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

        public void ExitProgram(object sender, RoutedEventArgs e)
        {
            exitTimer.Start();
        }

        void exitTimer_Tick(object sender, EventArgs e)
        {
            exitTimer.Stop();

            Process.GetCurrentProcess().Kill();
            //System.Windows.Application.Current.Shutdown();
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

        void imageCheckTimer_Tick(object sender, EventArgs e)
        {
            if (playLatest)
            {
                videoLayer.Source = new Uri(latestVideo, UriKind.Absolute);
                videoLayer.Visibility = Visibility.Visible;
                idleLayer.Visibility = Visibility.Hidden;
                idleLayer.Stop();
                idleLayer.Position = new TimeSpan(0, 0, 0, 0, 1);

                playLatest = false;
            }
            else
            {
                // check the folder where the image will be sent from the tablet
                string directory = Path.GetDirectoryName(@"C:\PicsFromTablet\");
                string filename = "Image_Replace_" + imageRenderCount.ToString() + ".png";
                string filePath = Path.Combine(directory, filename);

                if (File.Exists(filePath))
                {
                    // move the image to the necessary folder
                    string targetDirectory = Path.GetDirectoryName(@"C:\MosaicBuild_v05\(Footage)\02_Images\");
                    string targetFilename = "Image_Replace_02.png";
                    string targetfilePath = Path.Combine(targetDirectory, targetFilename);

                    File.Move(filePath, targetfilePath);
                    // then start a timer to allow for the move to finish
                    moveImageTimer.Start();

                    imageCheckTimer.Stop();

                    imageRenderCount++;
                }
            }
        }

        void renderTimer_Tick(object sender, EventArgs e)
        {
            // Check every minute to see if there is a new video to play

            // Using the latest 
            string directory = Path.GetDirectoryName(@"C:\");
            string filename = "Mosaic_Build_v01.mp4";
            string filePath = Path.Combine(directory, filename);

            Process[] localAll = Process.GetProcesses();

            Process[] processName = Process.GetProcessesByName("aerender");
            if (processName.Length == 0)
            {
                if (File.Exists(filePath))
                {
                    // increase the value of movieCount, and output it to the text file
                    int count = Int16.Parse(movieCount) + 1;
                    movieCount = count.ToString();
                    File.WriteAllText(@"C:\DisplayWall\movieCount.txt", movieCount);

                    // Move the rendered video to another location, to ensure it does not become overwritten
                    latestVideo = @"C:\Videos\videoRender" + movieCount + ".mp4";
                    File.Move(filePath, latestVideo);

                    previewImage = @"C:\PreviewImages\imagePreview_" + movieCount + ".png";
                    File.Move(@"C:\Mosaic_PreviewImage_01298.png", previewImage);

                    moveVideoTimer.Start();
                    rendertimer.Stop();

                    // Remove the image to replace, so that the spot is free for the next image
                    string targetDirectory = Path.GetDirectoryName(@"C:\MosaicBuild_v05\(Footage)\02_Images\");
                    string targetFilename = "Image_Replace_02.png";
                    string targetfilePath = Path.Combine(targetDirectory, targetFilename);
                    File.Delete(targetfilePath);
                }
            }
        }

        // This timer makes sure that we allow for enough time for the finished video to be moved to the playback folder under a new name
        void moveVideoTimer_Tick(object sender, EventArgs e)
        {
            moveVideoTimer.Stop();

            // Once a video starts playing, it is now safe to look for a new image from the tablet
            imageCheckTimer.Start();
        }

        void moveImageTimer_Tick(object sender, EventArgs e)
        {
            moveImageTimer.Stop();
            //idleLayer.Visibility = Visibility.Visible;
            //videoLayer.Visibility = Visibility.Hidden;
            //idleLayer.Play();

            // Run the render process of the After Effects file
            Process.Start(@"C:\Program Files\Adobe\Adobe After Effects 2021\Support Files\aerender.exe", @"-project C:\MosaicBuild_v05\MosaicBuild_v05.aep");

            rendertimer.Start();
        }

        void OnVideoEnded(object sender, EventArgs e)
        {
            videoLayer.Stop();
            //videoLayer.Visibility = Visibility.Hidden;
            videoEndTimer.Start();
        }

        void videoEndTimer_Tick(object sender, EventArgs e)
        {
            videoEndTimer.Stop();
            idleLayer.Visibility = Visibility.Visible;
            videoLayer.Visibility = Visibility.Hidden;
        }

        private void OnScreenSaverEnded(object sender, EventArgs e)
        {
            idleLayer.Position = new TimeSpan(0, 0, 0, 0, 1);
            idleLayer.Play();
        }
    }
}
