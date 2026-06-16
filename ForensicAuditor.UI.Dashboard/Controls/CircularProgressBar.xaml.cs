using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ForensicAuditor.UI.Dashboard.Controls
{
    public partial class CircularProgressBar : UserControl
    {
        // Daftarkan DependencyProperty agar properti "Value" dapat kita bind secara dinamis di XAML
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double), typeof(CircularProgressBar),
                new PropertyMetadata(0.0, OnValueChanged));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public CircularProgressBar()
        {
            InitializeComponent();
            // Inisialisasi awal lingkaran kosong
            UpdateIndicator(0);
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CircularProgressBar bar)
            {
                bar.UpdateIndicator((double)e.NewValue);
            }
        }

      
        private void UpdateIndicator(double value)
        {
            if (ProgressIndicator == null) return;

            // Batasi nilai agar tetap di antara 0 dan 100
            value = Math.Max(0, Math.Min(100, value));

            // r (radius efektif garis tengah stroke) = (Lebar 130 - Ketebalan 10) / 2 = 60
            double r = 60;
            double circumference = 2 * Math.PI * r; 

            // StrokeDashArray WPF dihitung relatif terhadap StrokeThickness (10)
            double thickness = ProgressIndicator.StrokeThickness;
            double totalUnits = circumference / thickness; // Total unit relatif ~37.7

            // Hitung rasio panjang busur aktif vs sisa kekosongan
            double dashLength = (value / 100.0) * totalUnits;
            double gapLength = totalUnits - dashLength;

            // Update bentuk visual busur lingkaran
            ProgressIndicator.StrokeDashArray = new DoubleCollection(new[] { dashLength, gapLength });

            // Ganti warna busur lingkaran agar adaptif terhadap tingkat bahaya
            if (value >= 80)
            {
                // Status Merah Bahaya (Critical)
                ProgressIndicator.Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
            }
            else if (value >= 40)
            {
                // Status Oranye Waspada (Medium/High)
                ProgressIndicator.Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E67E22"));
            }
            else
            {
                // Status Hijau Aman (Informational/Low)
                ProgressIndicator.Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71"));
            }
        }
    }
}