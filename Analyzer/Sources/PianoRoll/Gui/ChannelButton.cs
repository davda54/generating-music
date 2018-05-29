using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MidiModel;
using PianoRoll.Extensions;
using PianoRoll.MidiInterface;

namespace PianoRoll.Gui
{
    /// <summary>
    /// Implementation of a channel button, that:
    ///     - has a unique color
    ///     - can mute the corresponding channel
    ///     - changes appearence when a note from the corresponding channel is being played
    /// </summary>
    public class ChannelButton: Button
    {
        public ChannelState ChannelState { get; }

        private int ChannelNumber => ChannelState.ChannelNumber;

        private static readonly Style BaseStyle = Application.Current.FindResource("ChannelButtonStyle") as Style;
        private Color _baseColor;
        private Color _fadedColor;


        public ChannelButton(int channelNumber)
        {
            Click += Mute;

            ChannelState = new ChannelState(this, channelNumber);

            SetInstrument(null);
            SetContentAndColors();
        }

        

        // dependency properties

        /// <summary>
        /// true when a note from the corresponding channel is being played
        /// </summary>
        public bool IsPlaying 
        {
            get => (bool)GetValue(IsPlayingProperty);
            set => SetValue(IsPlayingProperty, value);
        }

        public static readonly DependencyProperty IsPlayingProperty =
            DependencyProperty.Register("IsPlayingProperty", typeof(bool), typeof(ChannelButton), new PropertyMetadata(false));


        /// <summary>
        /// true when the user clicked on the button and made the channel silent
        /// </summary>
        public bool IsMuted 
        {
            get => (bool)GetValue(IsMutedProperty);
            set => SetValue(IsMutedProperty, value);
        }

        public static readonly DependencyProperty IsMutedProperty =
            DependencyProperty.Register("IsMutedProperty", typeof(bool), typeof(ChannelButton), new PropertyMetadata(false));


        /// <summary>
        /// change the instrument the channel is currently playing
        /// </summary>
        public void SetInstrument(Instrument instrument)
        {
            if (ChannelNumber == Channel.PercussionChannelNumber) Content = "Percussion";
            else if (instrument == null) Content = "Channel " + ChannelNumber;
            else Content = instrument.ToString();
        }

        private void SetContentAndColors() {
            var findResource = Application.Current.FindResource("ChannelColor" + ChannelNumber);
            if (findResource == null)
                throw new FileNotFoundException($"Resource \"ChannelColor{ChannelNumber}\" was not found");

            _baseColor = (Color) findResource;
            _fadedColor = _baseColor.Fade(0.6f).Lighten(0.3f);


            // create new style

            var newStyle = new Style(typeof(ChannelButton)) { BasedOn = BaseStyle };
            
            newStyle.Setters.Add(new Setter(ForegroundProperty, new SolidColorBrush(_fadedColor)));


            Trigger mouseOverTrigger = new Trigger
            {
                Property = IsMouseOverProperty,
                Value = true
            };
            mouseOverTrigger.Setters.Add(new Setter { Property = BackgroundProperty, Value = new SolidColorBrush( _fadedColor.Darken(0.8f))});
            mouseOverTrigger.Setters.Add(new Setter { Property = BorderBrushProperty, Value = new SolidColorBrush(_baseColor.Darken(0.3f)) });
            mouseOverTrigger.Setters.Add(new Setter { Property = ForegroundProperty, Value = new SolidColorBrush(_baseColor) });
            newStyle.Triggers.Add(mouseOverTrigger);


            Trigger isMutedTrigger = new Trigger
            {
                Property = IsMutedProperty,
                Value = true
            };
            isMutedTrigger.Setters.Add(new Setter { Property = ForegroundProperty, Value = new SolidColorBrush(_baseColor.Darken(0.5f)) });
            newStyle.Triggers.Add(isMutedTrigger);
            
            
            Trigger isPlayingTrigger = new Trigger
            {
                Property = IsPlayingProperty,
                Value = true
            };
            isPlayingTrigger.Setters.Add(new Setter { Property = BorderBrushProperty, Value = new SolidColorBrush(_baseColor) });
            isPlayingTrigger.Setters.Add(new Setter { Property = ForegroundProperty, Value = new SolidColorBrush(_baseColor) });
            newStyle.Triggers.Add(isPlayingTrigger);


            Trigger isPressedTrigger = new Trigger
            {
                Property = IsPressedProperty,
                Value = true
            };
            isPressedTrigger.Setters.Add(new Setter { Property = BackgroundProperty, Value = new SolidColorBrush(Colors.Black) });
            isPressedTrigger.Setters.Add(new Setter { Property = BorderBrushProperty, Value = new SolidColorBrush(_baseColor.Darken(0.9f)) });
            isPressedTrigger.Setters.Add(new Setter { Property = ForegroundProperty, Value = new SolidColorBrush(Colors.Black) });
            newStyle.Triggers.Add(isPressedTrigger);


            // apply new style

            Style = newStyle;
        }

        private void Mute(object sender, RoutedEventArgs e)
        {
            ChannelState.Mute();
        }
    }
}
