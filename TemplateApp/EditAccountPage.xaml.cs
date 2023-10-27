using CubeKit.UI.Services;
using OtpNet;
using Protecc.Classes;
using Protecc.Helpers;
using Protecc.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Media.Imaging;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Protecc
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class EditAccountPage : Page
    {
        private VaultItem _editedVaultItem;
        private StorageFile _imageFile;

        public EditAccountPage()
        {
            this.InitializeComponent();
            WindowService.Initialize(AppTitleBar, AppTitle);
        }

        private async void Submit_Click(object sender, RoutedEventArgs e)
        {
            Ring.Visibility = Visibility.Visible;
            Content.Opacity = 0;
            await Task.Delay(100);
            if (NameBox.Text == "")
            {
                NameBox.Foreground = RedLinearGradientBrush;
                NameBox.Focus(FocusState.Programmatic);
                NameLoadAnimation.Start();
            }
            else
            {
                CredentialService.EditCredential(_editedVaultItem, NameBox.Text, ColorPicker.Color, _imageFile);
                Frame rootFrame = Window.Current.Content as Frame;
                rootFrame.Navigate(typeof(MainPage));
                return;
            }
            Ring.Visibility = Visibility.Collapsed;
            Content.Opacity = 1;
        }

        private async void Close_Click(object sender, RoutedEventArgs e)
        {
            Ring.Visibility = Visibility.Visible;
            Content.Opacity = 0;
            await Task.Delay(100);
            Frame rootFrame = Window.Current.Content as Frame;
            rootFrame.Navigate(typeof(MainPage));
        }

        private async void PictureButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            _imageFile = await picker.PickSingleFileAsync();
            if (_imageFile != null)
            {
                Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(_imageFile);
                var stream = await _imageFile.OpenAsync(FileAccessMode.Read);
                var bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(stream);
                Profile.ProfilePicture = bitmapImage;
                RemovePictureButton.Visibility = Visibility.Visible;
            }
            else
            {
                // Operation cancelled
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _editedVaultItem = e.Parameter as VaultItem;
            NameBox.Text = _editedVaultItem.Name;

            // Decode resource
            ColorPicker.Color = DataHelper.DecodeColor(_editedVaultItem.Resource).Color;

            try
            {
                Profile.ProfilePicture = DataHelper.AccountIcon(_editedVaultItem.Name);
                _imageFile = ApplicationData.Current.LocalFolder.GetFileAsync(_editedVaultItem.Name + ".png").AsTask().Result;
            }
            catch (Exception)
            {
                // No image
            }

            if (_imageFile != null)
            {
                RemovePictureButton.Visibility = Visibility.Visible;
            }
            else
            {
                RemovePictureButton.Visibility = Visibility.Collapsed;
            }
        }

        private void RemovePictureButton_Click(object sender, RoutedEventArgs e)
        {
            _imageFile = null;
            Profile.ProfilePicture = null;
            RemovePictureButton.Visibility = Visibility.Collapsed;
        }
    }
}
