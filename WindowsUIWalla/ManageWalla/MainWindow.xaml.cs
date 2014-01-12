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
using System.Timers;
using System.Windows.Media.Animation; 

namespace MeBlendTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Timer timer = new Timer(100);
        DateTime startTime = DateTime.Now;
        DateTime stopTime = DateTime.Now;

        public enum MessageType
        {
            Busy = 0,
            Info = 1,
            Warning = 2,
            Error = 3
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            paneBusy.Visibility = System.Windows.Visibility.Collapsed;
            gridAlertDialog.Visibility = System.Windows.Visibility.Collapsed;
            gridInfoAlert.Visibility = System.Windows.Visibility.Collapsed;
        }

        #region Interface


        /*
        public void ShowBusy()
        {
            Dispatcher.Invoke(new Action(() => { UpdateDialog(MessageType.Busy, ""); }));
            Dispatcher.Invoke(new Action(() => { ShowBusyApply(false); }));
        }

        public void FinishBusy()
        {
            Dispatcher.Invoke(FinishBusyApply);
        }
        */

        public void CancelProcess()
        {
            
            Dispatcher.Invoke(FinishBusyApply);
        }

        public void ShowMessage(MessageType messageType, string message)
        {
            
            Dispatcher.Invoke(new Action(() => { UpdateDialogsAndShow(messageType, message); }));


            //cmdAlertDialogResponse.Click += cmdDialogResponse_Click;
            /*
            if (messageType == MessageType.Info)
            {
                
            }
            else
            {
                Dispatcher.Invoke(new Action(() => { UpdateAlertDialog(messageType, message); }));
            }
            Dispatcher.Invoke(new Action(() => { ShowBusyApply(messageType); }));
             */
        }
        #endregion

        #region Implementation
        private void UpdateDialogsAndShow(MessageType messageType, string message)
        {
            switch (messageType)
            {
                case MessageType.Info:
                    if (gridInfoAlert.Visibility == Visibility.Visible) {return;}
                    lblInfoDialogMessage.Text = message;

                    gridInfoAlert.BeginAnimation(Grid.OpacityProperty, null);
                    gridInfoAlert.BeginAnimation(Grid.VisibilityProperty, null);
                    gridInfoAlert.Opacity = 0.0;
                    gridInfoAlert.Visibility = Visibility.Collapsed;

                    DoubleAnimationUsingKeyFrames opacityFrameAnimInfo = new DoubleAnimationUsingKeyFrames();
                    opacityFrameAnimInfo.FillBehavior = FillBehavior.HoldEnd;
                    opacityFrameAnimInfo.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, TimeSpan.FromSeconds(2.0)));
                    opacityFrameAnimInfo.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, TimeSpan.FromSeconds(6.0)));
                    opacityFrameAnimInfo.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, TimeSpan.FromSeconds(7.0)));
                    gridInfoAlert.BeginAnimation(Border.OpacityProperty, opacityFrameAnimInfo);

                    ObjectAnimationUsingKeyFrames visibilityAnimInfo = new ObjectAnimationUsingKeyFrames();
                    visibilityAnimInfo.KeyFrames.Add(new DiscreteObjectKeyFrame(Visibility.Visible, TimeSpan.FromSeconds(0.1)));
                    visibilityAnimInfo.KeyFrames.Add(new DiscreteObjectKeyFrame(Visibility.Collapsed,TimeSpan.FromSeconds(7.0)));
                    gridInfoAlert.BeginAnimation(Grid.VisibilityProperty, visibilityAnimInfo);

                    break;
                case MessageType.Busy:
                case MessageType.Warning:
                case MessageType.Error:
                    if (gridAlertDialog.Visibility == Visibility.Visible) { return; }
                    lblAlertDialogMessage.Text = message;

                    if (messageType == MessageType.Busy)
                    {
                        lblAlertDialogHeader.Text = "..loading";
                        cmdAlertDialogResponse.Content = "Cancel";
                    }
                    else if (messageType == MessageType.Warning)
                    {
                        lblAlertDialogHeader.Text = "Warning";
                        cmdAlertDialogResponse.Content = "OK";
                    }
                    else
                    {
                        lblAlertDialogHeader.Text = "Error";
                        cmdAlertDialogResponse.Content = "OK";
                    }

                    paneBusy.BeginAnimation(Border.OpacityProperty, null);
                    paneBusy.Opacity = 0.0;
                    paneBusy.Visibility = Visibility.Visible;

                    gridAlertDialog.BeginAnimation(Grid.OpacityProperty, null);
                    gridAlertDialog.Opacity = 0.0;
                    gridAlertDialog.Visibility = Visibility.Visible;

                    if (messageType == MessageType.Busy)
                    {
                        DoubleAnimationUsingKeyFrames opacityFrameAnim = new DoubleAnimationUsingKeyFrames();
                        opacityFrameAnim.FillBehavior = FillBehavior.HoldEnd;
                        opacityFrameAnim.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, TimeSpan.FromSeconds(2.0)));
                        opacityFrameAnim.KeyFrames.Add(new LinearDoubleKeyFrame(0.5, TimeSpan.FromSeconds(4.0)));
                        paneBusy.BeginAnimation(Border.OpacityProperty, opacityFrameAnim);

                        DoubleAnimationUsingKeyFrames opacityActionsAnim = new DoubleAnimationUsingKeyFrames();
                        //opacityActionsAnim.Duration = TimeSpan.FromSeconds(2.0);
                        opacityActionsAnim.FillBehavior = FillBehavior.HoldEnd;
                        opacityActionsAnim.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, TimeSpan.FromSeconds(2.0)));
                        opacityActionsAnim.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, TimeSpan.FromSeconds(2.5)));
                        gridAlertDialog.BeginAnimation(Grid.OpacityProperty, opacityActionsAnim);
                    }
                    else
                    {
                        DoubleAnimationUsingKeyFrames opacityFrameAnim = new DoubleAnimationUsingKeyFrames();
                        opacityFrameAnim.FillBehavior = FillBehavior.HoldEnd;
                        opacityFrameAnim.KeyFrames.Add(new LinearDoubleKeyFrame(0.5, TimeSpan.FromSeconds(0.5)));
                        paneBusy.BeginAnimation(Border.OpacityProperty, opacityFrameAnim);

                        DoubleAnimationUsingKeyFrames opacityActionsAnim = new DoubleAnimationUsingKeyFrames();
                        opacityActionsAnim.FillBehavior = FillBehavior.HoldEnd;
                        opacityActionsAnim.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, TimeSpan.FromSeconds(0.5)));
                        gridAlertDialog.BeginAnimation(Grid.OpacityProperty, opacityActionsAnim);
                    }
                    break;
            }
        }

        /*
        private void ShowBusyApply(MessageType messageType)
        {
            if (messageType == MessageType.Info)
            {
                gridInfoAlert.BeginAnimation(Grid.OpacityProperty, null);
                gridInfoAlert.Opacity = 0.0;
                gridInfoAlert.Visibility = Visibility.Visible;

                DoubleAnimationUsingKeyFrames opacityFrameAnim = new DoubleAnimationUsingKeyFrames();
                opacityFrameAnim.FillBehavior = FillBehavior.HoldEnd;
                opacityFrameAnim.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, TimeSpan.FromSeconds(1.0)));
                gridInfoAlert.BeginAnimation(Border.OpacityProperty, opacityFrameAnim);
            }
            else
            {


                if (messageType == MessageType.Busy)
                {
                    DoubleAnimationUsingKeyFrames opacityFrameAnim = new DoubleAnimationUsingKeyFrames();
                    opacityFrameAnim.FillBehavior = FillBehavior.HoldEnd;
                    opacityFrameAnim.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, TimeSpan.FromSeconds(2.0)));
                    opacityFrameAnim.KeyFrames.Add(new LinearDoubleKeyFrame(0.5, TimeSpan.FromSeconds(4.0)));
                    paneBusy.BeginAnimation(Border.OpacityProperty, opacityFrameAnim);

                    DoubleAnimationUsingKeyFrames opacityActionsAnim = new DoubleAnimationUsingKeyFrames();
                    //opacityActionsAnim.Duration = TimeSpan.FromSeconds(2.0);
                    opacityActionsAnim.FillBehavior = FillBehavior.HoldEnd;
                    opacityActionsAnim.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, TimeSpan.FromSeconds(2.0)));
                    opacityActionsAnim.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, TimeSpan.FromSeconds(2.5)));
                    gridAlertDialog.BeginAnimation(Grid.OpacityProperty, opacityActionsAnim);
                }
                else
                {
                    DoubleAnimationUsingKeyFrames opacityFrameAnim = new DoubleAnimationUsingKeyFrames();
                    opacityFrameAnim.FillBehavior = FillBehavior.HoldEnd;
                    opacityFrameAnim.KeyFrames.Add(new LinearDoubleKeyFrame(0.5, TimeSpan.FromSeconds(0.5)));
                    paneBusy.BeginAnimation(Border.OpacityProperty, opacityFrameAnim);

                    DoubleAnimationUsingKeyFrames opacityActionsAnim = new DoubleAnimationUsingKeyFrames();
                    opacityActionsAnim.FillBehavior = FillBehavior.HoldEnd;
                    opacityActionsAnim.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, TimeSpan.FromSeconds(0.5)));
                    gridAlertDialog.BeginAnimation(Grid.OpacityProperty, opacityActionsAnim);
                }
            }
        }
        */
          
        private void FinishBusyApply()
        {
            //Call cancel on async tokens.

            gridAlertDialog.Visibility = Visibility.Collapsed;
            paneBusy.Visibility = Visibility.Collapsed;
        }

        private void cmdAlertDialogResponse_Click(object sender, RoutedEventArgs e)
        {
            CancelProcess();
        }
        #endregion

        private void cmdShowIt_Click(object sender, RoutedEventArgs e)
        {
            startTime = DateTime.Now;
            stopTime = startTime.AddMilliseconds(sldLength.Value);

            //ShowBusy();

            timer.Elapsed += timer_Elapsed;
            timer.Start();
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (stopTime < DateTime.Now)
            {
                timer.Stop();
                //FinishBusy();
            }
            else
            {
                //Dispatcher.Invoke(simon2);
            }
        }



        private void ButtonInfo_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage(MessageType.Info, "hello info, the time is: " + DateTime.Now.ToLongTimeString());
        }

        private void ButtonWarning_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage(MessageType.Warning, "hello warning, the time is: " + DateTime.Now.ToLongTimeString());
        }

        private void ButtonError_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage(MessageType.Error, "hello error, the time is: " + DateTime.Now.ToLongTimeString());
        }

    }
}
