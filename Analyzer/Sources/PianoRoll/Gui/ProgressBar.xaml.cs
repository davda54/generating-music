using System.Windows;

namespace PianoRoll
{
    public partial class ProgressBar : Window
    {
        public ProgressBar()
        {
            InitializeComponent();
        }
 
        public void SetValue(float value)
        {
            Progress.Value = value;
        }
    }
}