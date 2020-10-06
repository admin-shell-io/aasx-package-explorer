using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using AasxIntegrationBase;
using Newtonsoft.Json;

namespace WpfMtpControl
{
    public class MtpVisuOptions
    {
        // original input

        public string Background = "#e0e0e0";

        public string StateColorActive = "#0000ff";
        public string StateColorNonActive = "#000000";

        public string StateColorForward = "#0000ff";
        public string StateColorReverse = "#00ff00";

        // prepared

        [JsonIgnore]
        public Brush BackgroundBrush = new SolidColorBrush(Color.FromRgb(224, 224, 255));

        [JsonIgnore]
        public Brush StateColorActiveBrush = Brushes.Blue;
        [JsonIgnore]
        public Brush StateColorNonActiveBrush = Brushes.Black;

        [JsonIgnore]
        public Brush StateColorForwardBrush = Brushes.Blue;
        [JsonIgnore]
        public Brush StateColorReverseBrush = Brushes.Green;

        private static void PrepareColor(string preset, ref Brush color)
        {
            if (preset.HasContent())
                try
                {
                    color = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString(preset));
                }
                catch { }
        }

        public void Prepare()
        {
            PrepareColor(this.Background, ref this.BackgroundBrush);

            PrepareColor(this.StateColorActive, ref this.StateColorActiveBrush);
            PrepareColor(this.StateColorNonActive, ref this.StateColorNonActiveBrush);

            PrepareColor(this.StateColorForward, ref this.StateColorForwardBrush);
            PrepareColor(this.StateColorReverse, ref this.StateColorReverseBrush);
        }
    }
}
