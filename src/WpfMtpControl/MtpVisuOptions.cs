using AasxIntegrationBase;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace WpfMtpControl
{
    public class MtpVisuOptions
    {
        // original input

        public string Background = "#e0e0e0";
        public string StateColorActive = "#0000ff";
        public string StateColorNonActive = "#000000";

        // prepared

        [JsonIgnore]
        public Brush BackgroundBrush = new SolidColorBrush(Color.FromRgb(224, 224, 255));
        [JsonIgnore]
        public Brush StateColorActiveBrush = Brushes.Blue;
        [JsonIgnore]
        public Brush StateColorNonActiveBrush = Brushes.Black;

        public void Prepare()
        {
            if (this.Background.HasContent())
                this.BackgroundBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(this.Background));

            if (this.StateColorActive.HasContent())
                this.StateColorActiveBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(this.StateColorActive));

            if (this.StateColorNonActive.HasContent())
                this.StateColorNonActiveBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(this.StateColorNonActive));
        }
    }
}
