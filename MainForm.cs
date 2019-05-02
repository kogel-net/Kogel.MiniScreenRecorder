namespace MiniScreenRecorder
{
    using System;
    using System.Drawing;
    using System.Diagnostics;
    using System.Windows.Forms;
    using AForge.Video;
    using AForge.Video.FFMPEG;
    using OperatingAvi.Record;
    using AviFile;
    using System.Threading;
    using System.Runtime.InteropServices;
    using web.App_Code;
    using System.IO;
    using AForge;
    using AForge.Video.DirectShow;
    /// <summary>
    /// MiniScreenRecorder is recording the screen in video file.
    /// </summary>
    public partial class MiniScreenRecorder : Form
    {
        #region Fields
        private const int DEFAULT_FRAME_RATE = 10;
        private int screenWidth;
        private int screenHight;
        private int bitRate;
        private int frameRate;
        private bool isRecording;
        private int framesCount;
        private string fileName;
        private Stopwatch stopWatch;
        private Rectangle screenArea;
        private VideoFileWriter videoWriter;
        private ScreenCaptureStream videoStreamer;
        private FolderBrowserDialog folderBrowser;
        private VideoCodec videoCodec;
        #endregion
        static string AviFileUrl = AppDomain.CurrentDomain.BaseDirectory + System.Configuration.ConfigurationManager.AppSettings["AviFileUrl"];
        static string WavFileUrl = AppDomain.CurrentDomain.BaseDirectory + System.Configuration.ConfigurationManager.AppSettings["WavFileUrl"];
        public MiniScreenRecorder()
        {
            InitializeComponent();
            this.screenWidth = SystemInformation.VirtualScreen.Width;
            this.screenHight = SystemInformation.VirtualScreen.Height;
            this.frameRate = DEFAULT_FRAME_RATE;
            this.isRecording = false;
            this.framesCount = default(int);
            this.stopWatch = new Stopwatch();
            this.screenArea = Rectangle.Empty;
            this.videoWriter = new VideoFileWriter();
            this.folderBrowser = new FolderBrowserDialog();

           
            InitializeDropDownMenus();

            //this.WindowState = FormWindowState.Minimized;
            //this.ShowInTaskbar = false;
            //SetVisibleCore(false);
        }
        //SaveFileDialog sd = new SaveFileDialog();
        //录音
        private static RecordSound recordSound = new RecordSound();
        //用来操作摄像头
        private VideoCaptureDevice Camera = null;
        //用来把每一帧图像编码到视频文件
        private VideoFileWriter VideoOutPut = new VideoFileWriter();
        private void startButton_Click(object sender, EventArgs e)
        {
            try
            {
                //创建目录
                if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\MV"))
                {
                    Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "\\MV");
                }
                FileSave();
                recordSound.StartRecordSound();
                //摄像头
                try
                {
                    //获取摄像头列表
                    var devs = new FilterInfoCollection(FilterCategory.VideoInputDevice);

                    //实例化设备控制类(我选了第1个)
                    if (devs.Count != 0)
                    {
                        Camera = new VideoCaptureDevice(devs[0].MonikerString);
                        //配置录像参数(宽,高,帧率,比特率等参数)VideoCapabilities这个属性会返回摄像头支持哪些配置,从这里面选一个赋值接即可,我选了第1个
                        Camera.VideoResolution = Camera.VideoCapabilities[0];

                        //设置回调,aforge会不断从这个回调推出图像数据
                        Camera.NewFrame += Camera_NewFrame;

                        //打开摄像头
                        Camera.Start();

                        //打开录像文件(如果没有则创建,如果有也会清空),这里还有关于
                        VideoOutPut.Open(AppDomain.CurrentDomain.BaseDirectory + "\\MV\\vedios.MP4",
                           Camera.VideoResolution.FrameSize.Width,
                           Camera.VideoResolution.FrameSize.Height,
                           Camera.VideoResolution.AverageFrameRate,
                           VideoCodec.MPEG4,
                           Camera.VideoResolution.BitCount);
                    }
                }
                catch { }
            }
            catch (Exception ex)
            {

            }
        }
        //图像缓存
        private Bitmap bmp = new Bitmap(1, 1);

        //摄像头输出回调
        private void Camera_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            //写到文件
            VideoOutPut.WriteVideoFrame(eventArgs.Frame);
            lock (bmp)
            {
                //释放上一个缓存
                bmp.Dispose();
                //保存一份缓存
                bmp = eventArgs.Frame.Clone() as Bitmap;
                this.pictureBox1.Image = bmp;
            }
        }
        /// <summary>
        /// 停止
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void stopButton_Click(object sender, EventArgs e)
        {
            try
            {
                this.isRecording = false;
                stopWatch.Reset();
                videoStreamer.Stop();
                videoWriter.Close();
                recordSound.EndRecordSound(WavFileUrl);

                try
                {
                    //停摄像头
                    Camera.Stop();

                    //关闭录像文件,如果忘了不关闭,将会得到一个损坏的文件,无法播放
                    VideoOutPut.Close();
                }
                catch { }

                //获取和保存音频流到文件(桌面)
              /*  AviManager aviManager = new AviManager(AviFileUrl, true);
                aviManager.AddAudioStream(WavFileUrl, 0);
                aviManager.Close();
                //获取和保存音频到摄像头视频文件
                AviManager avim = new AviManager(AppDomain.CurrentDomain.BaseDirectory + "\\MV\\vedios.avi", true);
                avim.AddAudioStream(WavFileUrl, 0);
                avim.Close();

                */

            }
            catch (Exception ex)
            {

            }
            finally
            {
                Application.Exit();
            }
        }

        private void MiniScreenRecorder_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isRecording)
            {
                if (MessageBox.Show("Do you want to exit? Video will be saved!", "Mini Screen Recorder",
                   MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    stopWatch.Reset();
                    videoStreamer.Stop();
                    videoWriter.Close();
                    e.Cancel = false;
                }
                else
                {
                    e.Cancel = true;
                }
                Application.Exit();
            }
        }
        /// <summary>
        /// 录制桌面回调
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void video_NewFrame(object sender, NewFrameEventArgs e)
        {
            if (this.isRecording)
            {
                this.framesCount++;

                this.videoWriter.WriteVideoFrame(e.Frame);
                this.stopWatchLabel.Invoke(new Action
                    (() => this.stopWatchLabel.Text = string.Format
                        (@"Duration: {0}", this.stopWatch.Elapsed.ToString("hh\\:mm\\:ss"))));

                this.framesCountLabel.Invoke(new Action
                    (() => this.framesCountLabel.Text = string.Format
                        (@"Frames: {0}", this.framesCount)));

            }
            else
            {
                stopWatch.Reset();
                videoStreamer.Stop();
                videoWriter.Close();
            }
        }

        /// <summary>
        /// Setting the required parameters for starting video recording and opens
        /// video writer.
        /// </summary>
        private void InitializeRecordingParameters(string filename)
        {
            if (!this.isRecording)
            {
                this.isRecording = true;
                fileName = filename;
                SetScreenArea();
                this.videoWriter.Open
                    (this.fileName, this.screenWidth, this.screenHight,
                    this.frameRate, this.videoCodec, this.bitRate);
                //sd.FileName = null;

            }
        }

        /// <summary>
        /// Defining the screen area that will be recorded.
        /// </summary>
        private void SetScreenArea()
        {
            foreach (Screen screen in Screen.AllScreens)
            {
                this.screenArea = Rectangle.Union(this.screenArea, screen.Bounds);
            }

            //if (this.screenArea == Rectangle.Empty)
            //{
            //    throw new InvalidOperationException("Screan area can not be set");
            //}
        }
        /// <summary>
        /// Opens video streamer and starts recordnig.
        /// </summary>
        private void StartRecording()
        {
            //SaveFileDialog Savefile = new SaveFileDialog() { Filter = "AVI视频文件|*.avi" };
            //if (Savefile.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            //{
            //    fileName = Savefile.FileName;
            //}
            //this.fileName = saveFile(sd.FileName);
            //InitializeRecordingParameters();
            this.videoStreamer = new ScreenCaptureStream(this.screenArea);
            this.videoStreamer.NewFrame += new NewFrameEventHandler(video_NewFrame);
            this.videoStreamer.Start();

            this.stopWatch.Start();
        }

        /// <summary>
        /// Initializing the MainForm combo boxes content.
        /// </summary>
        private void InitializeDropDownMenus()
        {
            this.videoCodec = (VideoCodec)3;
            this.bitRate = 3000000;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text.Equals("rec"))
            {
                videoStreamer.SignalToStop();
                stopWatch.Stop();
                //videoStreamer.WaitForStop();
                button1.Text = "sto";
            }
            else
            {
                videoStreamer.Start();
                stopWatch.Start();
                button1.Text = "rec";
            }

        }
        public void FileSave()
        {
            if (!string.IsNullOrEmpty(AviFileUrl))
            {
                InitializeRecordingParameters(AviFileUrl);
                StartRecording();
            }
        }

        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case (int)Record.Start:
                    setControl("startButton_Click", null);
                    break;
                case (int)Record.Stop:
                    setControl("stopButton_Click", null);
                    break;
            }
            base.DefWndProc(ref m);
        }
        static int index = 0;
        private void MiniScreenRecorder_Load(object sender, EventArgs e)
        {
            //this.Opacity = 0;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            // this.Hide();
            //this.Size = new Size() { Width = 1, Height = 1 };
        }


        private delegate void SetControlCallback(string method, object obj);

        private void setControl(string method, object obj)
        {
            if (this.InvokeRequired)
            {
                SetControlCallback d = new SetControlCallback(setControl);
                this.Invoke(d, new object[] { method, obj });
            }
            else
            {
                if (method.Equals("startButton_Click"))
                {
                    startButton_Click(null, null);
                }
                else if (method.Equals("stopButton_Click"))
                {
                    stopButton_Click(null, null);
                }
            }
        }
    }
}
