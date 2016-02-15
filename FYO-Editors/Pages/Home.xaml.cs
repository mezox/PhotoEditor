using System;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

using System.ComponentModel;
using System.IO;
using System.Net;
using AForge.Controls;
using AForge.Imaging;
using AForge.Imaging.Filters;
using FirstFloor.ModernUI.Windows.Controls;

using System.Runtime.InteropServices;

namespace FYO_Editors.Pages
{
    /// <summary>
    /// Interaction logic for Home.xaml
    /// </summary>
    public partial class Home : UserControl, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Private variables


        private ImageSource             mainPhotoIS = null;
        private System.Drawing.Bitmap   mainPhoto;

        private ContrastCorrection cc = null;
        private BrightnessCorrection bc = null;
        private HueModifier hm = null;
        private SaturationCorrection sc = null;
        private ChannelFiltering chf = null;

        private int brightness = 0;
        private int contrast = 0;
        private int hue = 0;
        private float saturation = 0.0f;

        private double dpiX;
        private double dpiY;

        private String localImagePath = null;
        private PointCollection luminanceHistogramPoints = null;
        private PointCollection redColorHistogramPoints = null;
        private PointCollection greenColorHistogramPoints = null;
        private PointCollection blueColorHistogramPoints = null;

        private BitmapImage redChannelbmp = null;
        private BitmapImage greenChannelbmp = null;
        private BitmapImage blueChannelbmp = null;

        #endregion

        #region Public Properties

        public String ImageURL { get; set; }

        public String LocalImagePath
        {
            get
            {
                return this.localImagePath;
            }
            set
            {
                if (this.localImagePath != value)
                {
                    this.localImagePath = value;
                    if (this.PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("LocalImagePath"));
                    }
                }
            }
        }

        public BitmapImage RedChannelbmp
        {
            get
            {
                return this.redChannelbmp;
            }
            set
            {
                if (this.redChannelbmp != value)
                {
                    this.redChannelbmp = value;
                    if (this.PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("RedChannelbmp"));
                    }
                }
            }
        }
        public BitmapImage GreenChannelbmp
        {
            get
            {
                return this.greenChannelbmp;
            }
            set
            {
                if (this.greenChannelbmp != value)
                {
                    this.greenChannelbmp = value;
                    if (this.PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("GreenChannelbmp"));
                    }
                }
            }
        }
        public BitmapImage BlueChannelbmp
        {
            get
            {
                return this.blueChannelbmp;
            }
            set
            {
                if (this.blueChannelbmp != value)
                {
                    this.blueChannelbmp = value;
                    if (this.PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("BlueChannelbmp"));
                    }
                }
            }
        }

        public bool PerformHistogramSmoothing { get; set; }

        public PointCollection LuminanceHistogramPoints
        {
            get
            {
                return this.luminanceHistogramPoints;
            }
            set
            {
                if (this.luminanceHistogramPoints != value)
                {
                    this.luminanceHistogramPoints = value;
                    if (this.PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("LuminanceHistogramPoints"));
                    }
                }
            }
        }

        public PointCollection RedColorHistogramPoints
        {
            get
            {
                return this.redColorHistogramPoints;
            }
            set
            {
                if (this.redColorHistogramPoints != value)
                {
                    this.redColorHistogramPoints = value;
                    if (this.PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("RedColorHistogramPoints"));
                    }
                }
            }
        }

        public PointCollection GreenColorHistogramPoints
        {
            get
            {
                return this.greenColorHistogramPoints;
            }
            set
            {
                if (this.greenColorHistogramPoints != value)
                {
                    this.greenColorHistogramPoints = value;
                    if (this.PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("GreenColorHistogramPoints"));
                    }
                }
            }
        }

        public PointCollection BlueColorHistogramPoints
        {
            get
            {
                return this.blueColorHistogramPoints;
            }
            set
            {
                if (this.blueColorHistogramPoints != value)
                {
                    this.blueColorHistogramPoints = value;
                    if (this.PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("BlueColorHistogramPoints"));
                    }
                }
            }
        }

        #endregion

        #region Constructor

        public Home()
        {
            try
            {
                this.ImageURL = new Uri(Path.Combine(Environment.CurrentDirectory, "Sample.jpg"), UriKind.Absolute).AbsolutePath;

                if (!String.IsNullOrWhiteSpace(this.ImageURL))
                {
                    this.mainPhoto = (System.Drawing.Bitmap)System.Drawing.Image.FromFile(ImageURL);
                    this.mainPhotoIS = ToBitmapImage(mainPhoto);
                    this.LocalImagePath = ImageURL;

                    this.dpiX = 84;
                    this.dpiY = 84;


                    UpdateHistograms(null);
                    UpdateChannelPreviews(null);
                }
            }
            catch
            {
                // do nothing, user must enter a URL manualy
            }

            this.DataContext = this;

            InitializeComponent();
        }

        #endregion

        #region Private Methods

        private PointCollection ConvertToPointCollection(int[] values)
        {
            if (this.PerformHistogramSmoothing)
            {
                values = SmoothHistogram(values);
            }

            int max = values.Max();

            PointCollection points = new PointCollection();
            // first point (lower-left corner)
            points.Add(new Point(0, max));
            // middle points
            for (int i = 0; i < values.Length; i++)
            {
                points.Add(new Point(i, max - values[i]));
            }
            // last point (lower-right corner)
            points.Add(new Point(values.Length - 1, max));

            return points;
        }

        private int[] SmoothHistogram(int[] originalValues)
        {
            int[] smoothedValues = new int[originalValues.Length];

            double[] mask = new double[] { 0.25, 0.5, 0.25 };

            for (int bin = 1; bin < originalValues.Length - 1; bin++)
            {
                double smoothedValue = 0;
                for (int i = 0; i < mask.Length; i++)
                {
                    smoothedValue += originalValues[bin - 1 + i] * mask[i];
                }
                smoothedValues[bin] = (int)smoothedValue;
            }

            return smoothedValues;
        }

        #endregion

        private void onOpenFileMenuClick(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".jpg"; // Default file extension
            dlg.Filter = "JPEG files (.jpg)|*.jpg"; // Filter files by extension 

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results 
            if (result == true)
            {
                try
                {
                    this.ImageURL = new Uri(dlg.FileName, UriKind.Absolute).AbsolutePath;

                    if (!String.IsNullOrWhiteSpace(this.ImageURL))
                    {
                        UpdatePicture(ImageURL);
                        UpdateHistograms(null);
                        UpdateChannelPreviews(null);
                    }
                }
                catch
                {

                }
            }
        }
        private void UpdatePicture(String imagePath)
        {
            try
            {
                this.mainPhoto = (System.Drawing.Bitmap)System.Drawing.Image.FromFile(imagePath);
                this.mainPhotoIS = ToBitmapImage(mainPhoto);
                this.LocalImagePath = imagePath;
                Photography.Source = mainPhotoIS;
            }
            catch(Exception)
            {
                MessageBox.Show("Invalid image URL. Image could not be retrieved", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        private void UpdateChannelPreviews(System.Drawing.Bitmap pic)
        {
            ExtractChannel r = new ExtractChannel(RGB.R);
            ExtractChannel g = new ExtractChannel(RGB.G);
            ExtractChannel b = new ExtractChannel(RGB.B);

            System.Drawing.Bitmap rImage = null;
            System.Drawing.Bitmap gImage = null;
            System.Drawing.Bitmap bImage = null;

            // apply the filter
            if(pic != null)
            {
                rImage = r.Apply(pic);
                gImage = g.Apply(pic);
                bImage = b.Apply(pic);
            }
            else
            {
                rImage = r.Apply(mainPhoto);
                gImage = g.Apply(mainPhoto);
                bImage = b.Apply(mainPhoto);
            }
            
            RedChannelbmp = ToBitmapImage(rImage);
            GreenChannelbmp = ToBitmapImage(gImage);
            BlueChannelbmp = ToBitmapImage(bImage);
        }

        private void UpdateHistograms(System.Drawing.Bitmap pic)
        {
            ImageStatisticsHSL hslStatistics = null;
            ImageStatistics rgbStatistics = null;

            if(pic == null)
            {
                hslStatistics = new ImageStatisticsHSL(mainPhoto);
                rgbStatistics = new ImageStatistics(mainPhoto);
            }
            else
            {
                hslStatistics = new ImageStatisticsHSL(pic);
                rgbStatistics = new ImageStatistics(pic);
            }

            this.LuminanceHistogramPoints = ConvertToPointCollection(hslStatistics.Luminance.Values);
            this.RedColorHistogramPoints = ConvertToPointCollection(rgbStatistics.Red.Values);
            this.GreenColorHistogramPoints = ConvertToPointCollection(rgbStatistics.Green.Values);
            this.BlueColorHistogramPoints = ConvertToPointCollection(rgbStatistics.Blue.Values);
        }

        /// <summary>
        /// Converts Bitmap to BitmapImage which is required Source type for Image control
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public static BitmapImage ToBitmapImage(System.Drawing.Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }

        private void eqHistogram_Click(object sender, RoutedEventArgs e)
        {
            ClearTextBoxes();

            brightness = 0;
            Brightness.Value = 0;

            if (eqHistogram.IsChecked == true)
            {
                HistogramEqualization filter = new HistogramEqualization();
                filter.ApplyInPlace(mainPhoto);
                Photography.Source = ToBitmapImage(mainPhoto);
            }
            else
            {
                UpdatePicture(ImageURL);
                Photography.Source = ToBitmapImage(mainPhoto);
            }
            
            mainPhotoIS = Photography.Source;
            UpdateHistograms(null);
            UpdateChannelPreviews(null);
        }

        private void RedChannelEnterEvent(object sender, MouseEventArgs e)
        {
            RedChannelGrid.Opacity = 0.5;
            mainPhotoIS = Photography.Source;
            Photography.Source = RedChannel.Source;
        }

        private void RedChannelLeaveEvent(object sender, MouseEventArgs e)
        {
            RedChannelGrid.Opacity = 0.0;

            if (Photography.Source != mainPhotoIS)
            {
                Photography.Source = mainPhotoIS;
            }
        }

        private void GreenChannelEnterEvent(object sender, MouseEventArgs e)
        {
            GreenChannelGrid.Opacity = 0.5;
            mainPhotoIS = Photography.Source;
            Photography.Source = GreenChannel.Source;
        }

        private void GreenChannelLeaveEvent(object sender, MouseEventArgs e)
        {
            GreenChannelGrid.Opacity = 0.0;

            if (Photography.Source != mainPhotoIS)
            {
                Photography.Source = mainPhotoIS;
            }
        }

        private void BlueChannelEnterEvent(object sender, MouseEventArgs e)
        {
            BlueChannelGrid.Opacity = 0.5;
            mainPhotoIS = Photography.Source;
            Photography.Source = BlueChannel.Source;
        }

        private void BlueChannelLeaveEvent(object sender, MouseEventArgs e)
        {
            BlueChannelGrid.Opacity = 0.0;

            if (Photography.Source != mainPhotoIS)
            {
                Photography.Source = mainPhotoIS;
            }
        }

        private void Brightness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            BrightnessLabel.Content = Brightness.Value;
            brightness = (int)Brightness.Value;

            if (mainPhoto != null)
            {
                bc = new BrightnessCorrection(brightness);
                System.Drawing.Bitmap tmp = bc.Apply((System.Drawing.Bitmap)mainPhoto.Clone());
                //BitmapImage tmpBmpIs = ToBitmapImage(tmp);

                Photography.Source = ToBitmapImage(tmp);

                UpdateHistograms(tmp);
                UpdateChannelPreviews(tmp);
            }
        }

        private void Contrast_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ContrastLabel.Content = Contrast.Value;
            contrast = (int)Contrast.Value;

            if (mainPhoto != null)
            {
                cc = new ContrastCorrection(contrast);
                System.Drawing.Bitmap tmp = cc.Apply((System.Drawing.Bitmap)mainPhoto.Clone());
                BitmapImage tmpBmpIs = ToBitmapImage(tmp);

                Photography.Source = ToBitmapImage(tmp);

                UpdateHistograms(tmp);
                UpdateChannelPreviews(tmp);
            }
        }

        private void contrastStretch_Click(object sender, RoutedEventArgs e)
        {
            if (contrastStretch.IsChecked == true)
            {
                ContrastStretch filter = new ContrastStretch();
                filter.ApplyInPlace(mainPhoto);
                Photography.Source = ToBitmapImage(mainPhoto);
            }
            else
            {
                UpdatePicture(ImageURL);
                Photography.Source = ToBitmapImage(mainPhoto);
            }

            mainPhotoIS = Photography.Source;
            UpdateHistograms(null);
            UpdateChannelPreviews(null);
        }

        private void Hue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            HueLabel.Content = Hue.Value;
            hue = (int)Hue.Value;

            if (mainPhoto != null)
            {
                hm = new HueModifier(hue);
                System.Drawing.Bitmap tmp = hm.Apply((System.Drawing.Bitmap)mainPhoto.Clone());
                //BitmapImage tmpBmpIs = ToBitmapImage(tmp);

                Photography.Source = ToBitmapImage(tmp);

                UpdateHistograms(tmp);
                UpdateChannelPreviews(tmp);
            }
        }

        private void Saturation_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SaturationLabel.Content = (int)(Saturation.Value * 100);
            saturation = (float)Saturation.Value;

            if (mainPhoto != null)
            {
                sc = new SaturationCorrection(saturation);
                System.Drawing.Bitmap tmp = sc.Apply((System.Drawing.Bitmap)mainPhoto.Clone());
                //BitmapImage tmpBmpIs = ToBitmapImage(tmp);

                Photography.Source = ToBitmapImage(tmp);

                UpdateHistograms(tmp);
                UpdateChannelPreviews(tmp);
            }
        }

        private void RGBFilter_Clicked(object sender, RoutedEventArgs e)
        {
            if (mainPhoto != null)
            {
                chf = new ChannelFiltering();

                // set channels' ranges to keep
                chf.Red = new AForge.IntRange(int.Parse(RedIn.Text), int.Parse(RedOut.Text));
                chf.Green = new AForge.IntRange(int.Parse(GreenIn.Text), int.Parse(GreenOut.Text));
                chf.Blue = new AForge.IntRange(int.Parse(BlueIn.Text), int.Parse(BlueOut.Text));

                // apply the filter
                System.Drawing.Bitmap tmp = chf.Apply((System.Drawing.Bitmap)mainPhoto.Clone());
                //BitmapImage tmpBmpIs = ToBitmapImage(tmp);

                /*RenderTargetBitmap rtb = new RenderTargetBitmap((int)Photography.ActualWidth, (int)Photography.ActualHeight, mainPhoto.HorizontalResolution, mainPhoto, PixelFormats.Pbgra32);
                rtb.Render(Photography);

                PngBitmapEncoder png = new PngBitmapEncoder();
                png.Frames.Add(BitmapFrame.Create(rtb));
                MemoryStream stream = new MemoryStream();
                png.Save(stream);
                System.Drawing.Bitmap image = (System.Drawing.Bitmap)System.Drawing.Image.FromStream(stream);*/

                Photography.Source = ToBitmapImage(tmp);

                UpdateHistograms(tmp);
                UpdateChannelPreviews(tmp);
            }
        }

        public static System.Drawing.Bitmap ConvertToBitmap(BitmapSource bitmapSource)
        {
            var width = bitmapSource.PixelWidth;
            var height = bitmapSource.PixelHeight;
            var stride = width * ((bitmapSource.Format.BitsPerPixel + 7) / 8);
            var memoryBlockPointer = Marshal.AllocHGlobal(height * stride);
            bitmapSource.CopyPixels(new Int32Rect(0, 0, width, height), memoryBlockPointer, height * stride, stride);
            var bitmap = new System.Drawing.Bitmap(width, height, stride, System.Drawing.Imaging.PixelFormat.Format32bppPArgb, memoryBlockPointer);
            return bitmap;
        }

        private void ClearTextBoxes()
        {
            RedIn.Text = "1";
            RedOut.Text = "256";
            GreenIn.Text = "1";
            GreenOut.Text = "256";
            BlueIn.Text = "1";
            BlueOut.Text = "256";
        }

        public static System.Drawing.Bitmap BitmapSourceToBitmap2(BitmapSource srs)
        {
            int width = srs.PixelWidth;
            int height = srs.PixelHeight;
            int stride = width * ((srs.Format.BitsPerPixel + 7) / 8);
            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(height * stride);
                srs.CopyPixels(new Int32Rect(0, 0, width, height), ptr, height * stride, stride);
                using (var btm = new System.Drawing.Bitmap(width, height, stride, System.Drawing.Imaging.PixelFormat.Format1bppIndexed, ptr))
                {
                    // Clone the bitmap so that we can dispose it and
                    // release the unmanaged memory at ptr
                    return new System.Drawing.Bitmap(btm);
                }
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }
        }
    }
}