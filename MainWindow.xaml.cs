//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.BodyBasics
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Timers;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Shapes;
    using Microsoft.Kinect;

    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// Radius of drawn hand circles
        /// </summary>
        private const double HandSize = 30;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Constant for clamping Z values of camera space points from being negative
        /// </summary>
        private const float InferredZPositionClamp = 0.1f;

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as closed
        /// </summary>
        private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as opened
        /// </summary>
        private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as in lasso (pointer) position
        /// </summary>
        private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Drawing group for body rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// Reader for body frames
        /// </summary>
        private BodyFrameReader bodyFrameReader = null;

        /// <summary>
        /// Array for the bodies
        /// </summary>
        private Body[] bodies = null;

        /// <summary>
        /// definition of bones
        /// </summary>
        private List<Tuple<JointType, JointType>> bones;

        /// <summary>
        /// Width of display (depth space)
        /// </summary>
        private int displayWidth;

        /// <summary>
        /// Height of display (depth space)
        /// </summary>
        private int displayHeight;

        /// <summary>
        /// List of colors for each body tracked
        /// </summary>
        private List<Pen> bodyColors;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;

        int licznikLewa = 0;
        int licznikPrawa = 0;

        Point point2 = new Point(100, 100);
        Point point3 = new Point(300, 300);
        Timer time;
        Timer timeSekundy;

        int sekundy = 20;

        bool flaga = false;
        private bool flagaZatrzymania = false;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            // one sensor is currently supported
            this.kinectSensor = KinectSensor.GetDefault();

            // get the coordinate mapper
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            // get the depth (display) extents
            FrameDescription frameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            // get size of joint space
            this.displayWidth = frameDescription.Width;
            this.displayHeight = frameDescription.Height;

            // open the reader for the body frames
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

            // a bone defined as a line between two joints
            this.bones = new List<Tuple<JointType, JointType>>();

            // Torso
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // Right Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // Left Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Right Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

            // Left Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

            // populate body colors, one for each BodyIndex
            this.bodyColors = new List<Pen>();

            this.bodyColors.Add(new Pen(Brushes.Red, 6));
            this.bodyColors.Add(new Pen(Brushes.Orange, 6));
            this.bodyColors.Add(new Pen(Brushes.Green, 6));
            this.bodyColors.Add(new Pen(Brushes.Blue, 6));
            this.bodyColors.Add(new Pen(Brushes.Indigo, 6));
            this.bodyColors.Add(new Pen(Brushes.Violet, 6));

            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this.kinectSensor.Open();

            // set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // use the window object as the view model in this simple example
            this.DataContext = this;





            // initialize the components (controls) of the window
            this.InitializeComponent();
        }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.imageSource;
            }
        }

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        /// <summary>
        /// Execute start up tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.FrameArrived += this.Reader_FrameArrived;
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                // BodyFrameReader is IDisposable
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        /// <summary>
        /// Handles the body frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    //if(flagaZatrzymania == false)
                        bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }

            if (dataReceived)
            {
                using (DrawingContext dc = this.drawingGroup.Open())
                {
                    // Draw a transparent background to set the render size
                    dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));

                    int penIndex = 0;
                    //foreach (Body body in this.bodies)

                    Pen drawPen = this.bodyColors[penIndex++];

                    if (this.bodies[0].IsTracked)
                    {
                        this.DrawClippedEdges(this.bodies[0], dc);

                        IReadOnlyDictionary<JointType, Joint> joints = this.bodies[0].Joints;

                        // convert the joint points to depth (display) space
                        Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                        foreach (JointType jointType in joints.Keys)
                        {
                            // sometimes the depth(Z) of an inferred joint may show as negative
                            // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                            CameraSpacePoint position = joints[jointType].Position;
                            if (position.Z < 0)
                            {
                                position.Z = InferredZPositionClamp;
                            }

                            DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                            jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                        }

                        this.DrawBody(joints, jointPoints, dc, drawPen);

                        this.DrawHand(this.bodies[0].HandLeftState, jointPoints[JointType.HandLeft], dc, 0);
                        this.DrawHand(this.bodies[0].HandRightState, jointPoints[JointType.HandRight], dc, 1);



                        SolidColorBrush mySolidColorBrush = new SolidColorBrush();
                        mySolidColorBrush.Color = Color.FromArgb(255, 255, 255, 0);

                        Pen blackBluePen = new Pen();
                        blackBluePen.Thickness = 5;
                        blackBluePen.LineJoin = PenLineJoin.Bevel;
                        blackBluePen.StartLineCap = PenLineCap.Triangle;
                        blackBluePen.EndLineCap = PenLineCap.Round;
                        LinearGradientBrush blueBlackLGB = new LinearGradientBrush();
                        blueBlackLGB.StartPoint = new Point(0, 0);
                        blueBlackLGB.EndPoint = new Point(1, 1);
                        blackBluePen.Brush = blueBlackLGB;
                        double radius = 30;
                        if (!flagaZatrzymania)
                        {
                            if (jointPoints[JointType.HandLeft].X > point2.X - radius && jointPoints[JointType.HandLeft].X < point2.X + radius && jointPoints[JointType.HandLeft].Y > point2.Y - radius && jointPoints[JointType.HandLeft].Y < point2.Y + radius)
                            {
                                Random rnd = new Random();
                                point2.X = rnd.Next(100, 400);
                                point2.Y = rnd.Next(100, 400);
                                licznikLewa++;
                                if (flaga == false)
                                {
                                    time = new System.Timers.Timer(20000);

                                    time.Elapsed += new ElapsedEventHandler(OnTimedEvent);
                                    timeSekundy = new System.Timers.Timer(1000);
                                    timeSekundy.Elapsed += new ElapsedEventHandler(OnTimedSecEvent);
                                    timeSekundy.Start();
                                    time.Start();
                                    flaga = true;
                                }
                            }
                            else
                            {
                                dc.DrawEllipse(mySolidColorBrush, blackBluePen, point2, radius, radius);
                            }


                            SolidColorBrush mySolidColorBrush2 = new SolidColorBrush();
                            mySolidColorBrush2.Color = Color.FromArgb(255, 255, 100, 0);

                            if (jointPoints[JointType.HandRight].X > point3.X - radius && jointPoints[JointType.HandRight].X < point3.X + radius && jointPoints[JointType.HandRight].Y > point3.Y - radius && jointPoints[JointType.HandRight].Y < point3.Y + radius)
                            {
                                Random rnd = new Random();
                                point3.X = rnd.Next(100, 400);
                                point3.Y = rnd.Next(100, 400);
                                licznikPrawa++;
                                if (flaga == false)
                                {
                                    time = new System.Timers.Timer(10000);
                                    time.Elapsed += new ElapsedEventHandler(OnTimedEvent);

                                    timeSekundy = new System.Timers.Timer(1000);
                                    timeSekundy.Elapsed += new ElapsedEventHandler(OnTimedSecEvent);
                                    timeSekundy.Start();

                                    time.Start();
                                    flaga = true;
                                }
                            }
                            else
                            {
                                dc.DrawEllipse(mySolidColorBrush2, blackBluePen, point3, radius, radius);
                            }
                        }
                        else
                        {
                            if (jointPoints[JointType.HandRight].X > 300 && jointPoints[JointType.HandRight].X < 400 && jointPoints[JointType.HandRight].Y > 5 && jointPoints[JointType.HandRight].Y < 55)
                            {
                                WynikLabel.Visibility = Visibility.Hidden;
                                obraz.Visibility = Visibility.Visible;
                                licznikLewa = 0;
                                licznikPrawa = 0;
                                ResetPrzycisk.Visibility = Visibility.Hidden;
                                flagaZatrzymania = false;
                                flaga = false;
                                Czas.Visibility = Visibility.Visible;
                                ZresetowanieLabel.Visibility = Visibility.Hidden;
                            }

                            if (jointPoints[JointType.HandLeft].X > 300 && jointPoints[JointType.HandLeft].X < 400 && jointPoints[JointType.HandLeft].Y > 5 && jointPoints[JointType.HandLeft].Y < 55)
                            {
                                WynikLabel.Visibility = Visibility.Hidden;
                                obraz.Visibility = Visibility.Visible;
                                licznikLewa = 0;
                                licznikPrawa = 0;
                                ResetPrzycisk.Visibility = Visibility.Hidden;
                                flagaZatrzymania = false;
                                flaga = false;
                                Czas.Visibility = Visibility.Visible;
                                ZresetowanieLabel.Visibility = Visibility.Hidden;
                            }
                        }
                        FormattedText formattedText = new FormattedText(
                        licznikLewa.ToString(),
                        CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight,
                        new Typeface("Verdana"),
                        32,
                        Brushes.White);
                        dc.DrawText(formattedText,new Point(100,350));

                        FormattedText formattedText2 = new FormattedText(
                        licznikPrawa.ToString(),
                        CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight,
                        new Typeface("Verdana"),
                        32,
                        Brushes.White);
                        dc.DrawText(formattedText2, new Point(400, 350));

                        FormattedText formattedTextCzas = new FormattedText(
                        "Pozostało: " + sekundy.ToString(),
                        CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight,
                        new Typeface("Verdana"),
                        15,
                        Brushes.White);
                        dc.DrawText(formattedTextCzas, new Point(10,10));

                    }

                    if (flagaZatrzymania == true)
                    {
                        WynikLabel.FontSize = 50;

                        int wynik = licznikPrawa + licznikLewa;
                        WynikLabel.Content = "Wynik = " + wynik;
                        WynikLabel.Visibility = Visibility.Visible;
                        ResetPrzycisk.Visibility = Visibility.Visible;
                        Czas.Visibility = Visibility.Hidden;
                        timeSekundy.Stop();


                        Pen blackBluePen4 = new Pen();
                        blackBluePen4.Thickness = 5;
                        blackBluePen4.LineJoin = PenLineJoin.Bevel;
                        blackBluePen4.StartLineCap = PenLineCap.Triangle;
                        blackBluePen4.EndLineCap = PenLineCap.Round;
                        SolidColorBrush mySolidColorBrush4 = new SolidColorBrush();
                        mySolidColorBrush4.Color = Color.FromArgb(255, 0, 255, 0);


                        Rect blueRectangle = new Rect();
                        blueRectangle.Height = 50;
                        blueRectangle.Width = 100;
                        blueRectangle.X = 300;
                        blueRectangle.Y = 5;
                        ZresetowanieLabel.Visibility = Visibility.Visible;
                        dc.DrawRectangle(mySolidColorBrush4, blackBluePen4, blueRectangle);

                        
                    }
                        // prevent drawing outside of our render area
                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                }
            }
        }
        private void PrzyciskEvent(object sender, RoutedEventArgs e)
        {
            WynikLabel.Visibility = Visibility.Hidden;
            obraz.Visibility = Visibility.Visible;
            licznikLewa = 0;
            licznikPrawa = 0;
            ResetPrzycisk.Visibility = Visibility.Hidden;
            flagaZatrzymania = false;
            flaga = false;
            Czas.Visibility = Visibility.Visible;
            ZresetowanieLabel.Visibility = Visibility.Hidden;
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            flagaZatrzymania = true;
            time.Enabled = false;
            sekundy = 20;

        }

        private void OnTimedSecEvent(Object source, ElapsedEventArgs e)
        {
            sekundy--;
        }

        /// <summary>
        /// Draws a body
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="drawingPen">specifies color to draw a specific body</param>
        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext, Pen drawingPen)
        {
            // Draw the bones
            foreach (var bone in this.bones)
            {
                this.DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen);
            }

            // Draw the joints
            foreach (JointType jointType in joints.Keys)
            {
                Brush drawBrush = null;

                TrackingState trackingState = joints[jointType].TrackingState;

                if (trackingState == TrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (trackingState == TrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Draws one bone of a body (joint to joint)
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="jointType0">first joint of bone to draw</param>
        /// <param name="jointType1">second joint of bone to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// /// <param name="drawingPen">specifies color to draw a specific bone</param>
        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, DrawingContext drawingContext, Pen drawingPen)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            {
                drawPen = drawingPen;
            }

            drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
        }

        /// <summary>
        /// Draws a hand symbol if the hand is tracked: red circle = closed, green circle = opened; blue circle = lasso
        /// </summary>
        /// <param name="handState">state of the hand</param>
        /// <param name="handPosition">position of the hand</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext, int reka)
        {
            if (reka == 0)
            {
                SolidColorBrush mySolidColorBrush = new SolidColorBrush();
                mySolidColorBrush.Color = Color.FromArgb(255, 255, 255, 0);
                drawingContext.DrawEllipse(mySolidColorBrush, null, handPosition, HandSize, HandSize);
            }
            else if (reka == 1)
            {
                SolidColorBrush mySolidColorBrush = new SolidColorBrush();
                mySolidColorBrush.Color = Color.FromArgb(255, 255, 100, 0);
          
                drawingContext.DrawEllipse(mySolidColorBrush, null, handPosition, HandSize, HandSize);
            }

        }

        /// <summary>
        /// Draws indicators to show which edges are clipping body data
        /// </summary>
        /// <param name="body">body to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawClippedEdges(Body body, DrawingContext drawingContext)
        {
            FrameEdges clippedEdges = body.ClippedEdges;

            if (clippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, this.displayHeight - ClipBoundsThickness, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, this.displayHeight));
            }

            if (clippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(this.displayWidth - ClipBoundsThickness, 0, ClipBoundsThickness, this.displayHeight));
            }
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }
    }
}
