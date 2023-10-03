using Protecc.Classes;
using Protecc.Helpers;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.Security.Credentials;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Globalization;
using System.Collections;
using Windows.ApplicationModel.Core;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Protecc.Controls
{
    public sealed partial class SettingsControl : UserControl
    {
        public SettingsClass Settings = new();
        public List<Language> Languages = new(); 
        public Language SelectedLanguage { get; set; }

        public SettingsControl()
        {
            this.InitializeComponent();
            foreach (var item in ApplicationLanguages.ManifestLanguages)
            {
                Language language = new(item);
                Languages.Add(language);
                if (item == Settings.AppLanguage)
                {
                    Settings.AppLanguage = item;
                    SelectedLanguage = language;
                }
            };
            SetupSettings();
        }
        public async void SetupSettings()
        {
            if (!await KeyCredentialManager.IsSupportedAsync())
            {
                var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                WindowsHelloText.Text = resourceLoader.GetString("SettingsWindowsHelloNotAvailable/Text");
                WindowsHelloSwitch.IsEnabled = false;
            }
        }

        private async void Export_Click(object sender, RoutedEventArgs e) => await ExportHelper.Export();

        private void OOBE_Click(object sender, RoutedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            rootFrame.Navigate(typeof(OOBEPage));
        }
        
        private IAsyncOperation<bool> OpenLink(string linkUrl) => Windows.System.Launcher.LaunchUriAsync(new Uri(linkUrl));

        private async void GitHub_Click(object sender, RoutedEventArgs e) => await OpenLink("https://github.com/FireCubeStudios/Protecc");

        private async void Discord_Click(object sender, RoutedEventArgs e) => await OpenLink("https://discord.gg/3WYcKat");

        private async void Twitter_Click(object sender, RoutedEventArgs e) => await OpenLink("https://twitter.com/FireCubeStudios");

        private void LanguageComboBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            Settings.AppLanguage = SelectedLanguage.LanguageTag;
            if (SelectedLanguage.LanguageTag != ApplicationLanguages.PrimaryLanguageOverride)
            {
                ReloadButton.Visibility = Visibility.Visible;
            } else
            {
                ReloadButton.Visibility = Visibility.Collapsed;
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await CoreApplication.RequestRestartAsync("");
        }
    }
}
