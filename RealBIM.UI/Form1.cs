using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Expression.Encoder.Devices;
using Microsoft.Expression.Encoder.Live;
using ZXing;

namespace RealBIM.UI
{
    public partial class Form1 : Form
    {
        const int WIDTH = 640;
        const int HEIGHT = 480;
        const int FRAME_DURATION = 15;

        private SpeechRecognitionEngine _recognizer;
        private SpeechSynthesizer _synthesizer;
        private readonly Worker _worker = new Worker();
        private Guid _guidResult;

        const string CMD_BEST = "I am the best";
        const string CMD_NEW = "Scan";
        const string CMD_VIEW = "View";
        const string CMD_WHERE = "Where";
        const string CMD_SHOW = "Show";
        const string CMD_ZOOM_IN = "Zoom in";
        const string CMD_ZOOM_OUT = "Zoom out";
        const string CMD_ANIMATE = "Sequence full";
        const string CMD_ANIMATE_PART = "Sequence part";
        const string CMD_LOVE = "I love you";
        const string STATUS_DONE = "Status done";
        const string STATUS_SKIP = "Status fail";

        const string CMD_HIRE = "What";
        const string CMD_STOP = "Stop";
        private bool stopped = false;
        private IBarcodeReader _reader = new BarcodeReader();
        private EncoderDevice _encoderDevice;
        private LiveJob _liveJob;
        private LiveDeviceSource _liveDeviceSource;
        private SourceProperties _sourceProperties;
        private Form _previewForm;

        public Form1()
        {
            InitializeComponent();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            InitRecognition();
            InitSynthesizer();
            InitWebcam();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            _liveJob.StopEncoding();
            _liveJob.RemoveDeviceSource(_liveDeviceSource);
            _liveJob.Dispose();
            _liveDeviceSource.Dispose();
            _encoderDevice.Dispose();

            _recognizer.Dispose();
            _synthesizer.Dispose();
        }

        private void InitWebcam()
        {
            var devices = EncoderDevices.FindDevices(EncoderDeviceType.Video).ToArray();
            _encoderDevice = devices.FirstOrDefault(d => d.Name.Contains("Camera"));
            
            Debug.Print($"Using {_encoderDevice?.Name}");

            _liveJob = new LiveJob();
            _liveDeviceSource = _liveJob.AddDeviceSource(_encoderDevice, null);
            _liveDeviceSource.PickBestVideoFormat(new Size(WIDTH, HEIGHT), FRAME_DURATION);
            _sourceProperties = _liveDeviceSource.SourcePropertiesSnapshot();
            _liveJob.OutputFormat.VideoProfile.Size = new Size(_sourceProperties.Size.Width, _sourceProperties.Size.Height);

            //_previewForm = new Form
            //{
            //    Owner = this,
            //    FormBorderStyle = FormBorderStyle.FixedToolWindow,
            //    Size = new Size(_sourceProperties.Size.Width, _sourceProperties.Size.Height)
            //};
            //_previewForm.Show();
            _liveDeviceSource.PreviewWindow = new PreviewWindow(new HandleRef(videoPreview, videoPreview.Handle));
            
            //_liveJob.OutputPreviewWindow = new PreviewWindow(new HandleRef(this, Handle));
            _liveJob.ActivateSource(_liveDeviceSource);
            //_liveJob.StartEncoding();
        }

        private void InitSynthesizer()
        {
            _synthesizer = new SpeechSynthesizer();
            _synthesizer.SelectVoice("Microsoft Zira Desktop");
        }

        private void InitRecognition()
        {
            _recognizer = new SpeechRecognitionEngine();
            _recognizer.LoadGrammar(new Grammar(new GrammarBuilder(CMD_NEW)));
            _recognizer.LoadGrammar(new Grammar(new GrammarBuilder(CMD_VIEW)));
            _recognizer.LoadGrammar(new Grammar(new GrammarBuilder(CMD_WHERE)));
            _recognizer.LoadGrammar(new Grammar(new GrammarBuilder(CMD_SHOW)));
            _recognizer.LoadGrammar(new Grammar(new GrammarBuilder(CMD_BEST)));
            _recognizer.LoadGrammar(new Grammar(new GrammarBuilder(CMD_ZOOM_OUT)));
            _recognizer.LoadGrammar(new Grammar(new GrammarBuilder(CMD_ZOOM_IN)));
            _recognizer.LoadGrammar(new Grammar(new GrammarBuilder(STATUS_DONE)));
            _recognizer.LoadGrammar(new Grammar(new GrammarBuilder(STATUS_SKIP)));
            _recognizer.LoadGrammar(new Grammar(new GrammarBuilder(CMD_ANIMATE_PART)));
            _recognizer.LoadGrammar(new Grammar(new GrammarBuilder(CMD_LOVE)));
            _recognizer.LoadGrammar(new Grammar(new GrammarBuilder(CMD_STOP)));
            _recognizer.LoadGrammar(new Grammar(new GrammarBuilder(CMD_ANIMATE))); 
            //_recognizer.LoadGrammar(new Grammar(new GrammarBuilder(CMD_HIRE)));

            _recognizer.SpeechRecognized += RecognizerOnSpeechRecognized;
            _recognizer.SpeechRecognitionRejected += RecognizerOnSpeechRecognitionRejected;
            _recognizer.RecognizeCompleted += RecognizerOnRecognizeCompleted;


            _recognizer.SetInputToDefaultAudioDevice();
        }

        private void RecognizerOnRecognizeCompleted(object sender, RecognizeCompletedEventArgs recognizeCompletedEventArgs)
        {
            if (!stopped)
                _recognizer.RecognizeAsync(RecognizeMode.Single); // recognize speech asynchronous
        }

        private void RecognizerOnSpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs speechRecognitionRejectedEventArgs)
        {
           
        }

        private void RecognizerOnSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            listBox1.Items.Add(e.Result.Text);


            switch (e.Result.Text)
            {
                //case CMD_HIRE:
                //_zira.Speak("Hey you guys! Do you like this kind of fun and technical work? Sweco is hiring");

                //break;

                case CMD_BEST:
                    _synthesizer.Speak("Sure you are");

                    break;

                case CMD_NEW:
                    cmdNew();
                    break;

                case CMD_SHOW:
                    cmdWhere();
                    break;

                case CMD_STOP:
                    cmdStop();
                    break;

                case STATUS_DONE:
                    _synthesizer.Speak("Part Done, Status updated and sent");
                    break;

                case STATUS_SKIP:
                    _synthesizer.Speak("Part Failed, Status updated and sent");
                    break;

                case CMD_LOVE:
                    _synthesizer.Speak("I love you too darling");
                    break;

                case CMD_WHERE:
                    cmdWhere();
                    break;

                case CMD_ZOOM_IN:
                    cmdZoom();
                    break;

                case CMD_ZOOM_OUT:
                    cmdOverview();
                    break;

                case CMD_ANIMATE:
                    cmdAnimate();
                    break;

                case CMD_ANIMATE_PART:
                    cmdAnimatePart();
                    break;
            }
        }

        private void cmdAnimate()
        {
            //if (!_checkGuid()) return;

            _synthesizer.SpeakAsync("Showing build order");
            _worker.AnimateBuild();
        }


        private void cmdAnimatePart()
        {
            if (!_checkGuid()) return;

            _synthesizer.SpeakAsync("Showing build order for part");
            _worker.HighlightPartWithNeigbours(_guidResult);
        }

        private void cmdStop()
        {
            stopped = true;
            _recognizer.RecognizeAsyncStop();
            _synthesizer.Speak("Not listening to you anymore");
        }

        private void cmdWhere()
        {
            if (!_checkGuid()) return;

            _synthesizer.SpeakAsync("Showing part");
            _worker.TurnToPart(_guidResult, 20);
            _worker.HighlightPartWithInstalled(_guidResult, 5);
        }

        private void cmdOverview()
        {

            _synthesizer.SpeakAsync("Zoom out");
            _worker.ZoomOverview();
        }

        private void cmdZoom()
        {
            if (!_checkGuid()) return;

            _synthesizer.SpeakAsync("Zoom in");
            _worker.ZoomToPart(_guidResult, 10);
        }

        private void cmdShow()
        {
            if (!_checkGuid()) return;

            _synthesizer.SpeakAsync("Flashing selected part");
            _worker.HighlightPartWithInstalled(_guidResult, 5);
        }

        private bool _checkGuid()
        {
            if (_guidResult == Guid.Empty)
            {
                _synthesizer.Speak("No part selected");
                return false;
            }

            return true;
        }
        private void cmdNew()
        {
            Bitmap photoTakenWithWebCam = null;
            DateTime start = DateTime.Now;

            //_guidResult = Guid.Empty;

            Guid oldGuid = _guidResult;

            _synthesizer.Speak("Scan code");
            do
            {
                if (photoTakenWithWebCam != null)
                    photoTakenWithWebCam.Dispose();

                photoTakenWithWebCam = CreatePicture();

                //photoTakenWithWebCam.Save(@"C:\Temp\HACKATHON\PHOTOS\WebCam" + DateTime.Now.Ticks + ".png");
            } while (photoTakenWithWebCam != null && !ReadCode(photoTakenWithWebCam) &&
                     (DateTime.Now - start).Seconds <= 5);

            if (photoTakenWithWebCam == null)
            {
                _synthesizer.Speak("What did you do, FAILED");
            }

            if ((DateTime.Now - start).Seconds > 5 && _guidResult == Guid.Empty)
            {
                _synthesizer.Speak("You were too slow");
            }
            else if (_guidResult == oldGuid)
            {
                _synthesizer.Speak("Part not changed");
                return;
            }

            photoTakenWithWebCam?.Dispose();

            if (_guidResult != Guid.Empty)
                _synthesizer.Speak("Good job");
        }

        private Bitmap CreatePicture()
        {

            try
            {
                Bitmap image = new Bitmap(videoPreview.Width, videoPreview.Height);

                using (Graphics g = Graphics.FromImage(image))
                {
                    Point sourcePoints =
                        videoPreview.PointToScreen(new Point(videoPreview.ClientRectangle.X,
                            videoPreview.ClientRectangle.Y));

                    g.CopyFromScreen(sourcePoints, Point.Empty, videoPreview.Bounds.Size);
                }

                return image;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private bool ReadCode(Bitmap barCodeBitmap)
        {
            //_reader = new BarcodeReader();

            try
            {
                var result = _reader.Decode(barCodeBitmap);

                if (result != null)
                {
                    var message =
                        "DecoderType: " + result.BarcodeFormat.ToString() + "\n" +
                        "DecoderContent: " + result.Text;

                    Guid value;

                    if (Guid.TryParse(result.Text, out value))
                        _guidResult = value;
                    else
                    {
                        _guidResult = Guid.Empty;
                    }

                    //MessageBox.Show(message);
                }
                else
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private void StartSpeech_Click(object sender, EventArgs e)
        {
            _synthesizer.Speak("Ok I'll listen to you once again");
            stopped = false;
            _recognizer.RecognizeAsync(RecognizeMode.Single); // recognize speech asynchronous
        }

        private void button1_Click(object sender, EventArgs e)
        {
            CreatePicture()?.Dispose();
        }
    }
}
