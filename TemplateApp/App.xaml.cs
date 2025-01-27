﻿using Microsoft.Toolkit.Uwp.Helpers;
using Protecc.Classes;
using Protecc.Helpers;
using Protecc.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Globalization;

namespace Protecc
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedException;
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                Windows.ApplicationModel.Core.CoreApplication.EnablePrelaunch(true);
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    SettingsClass Settings = new();
                    if (SystemInformation.Instance.IsFirstRun)
                    {
                        ApplicationData.Current.LocalSettings.Values["LaunchCount"] = 1;
                        Settings.Setup();
                        ApplicationLanguages.PrimaryLanguageOverride = Settings.AppLanguage;
                        rootFrame.Navigate(typeof(OOBEPage), e.Arguments);
                    }
                    else if (SystemInformation.Instance.IsAppUpdated)
                    {
                        Settings.Update();
                        ApplicationLanguages.PrimaryLanguageOverride = Settings.AppLanguage;
                        rootFrame.Navigate(typeof(WhatsNewPage), e.Arguments);
                    }
                    else if (new SettingsClass().LaunchCount == 4)
                    {
                        ApplicationLanguages.PrimaryLanguageOverride = Settings.AppLanguage;
                        rootFrame.Navigate(typeof(RatingsPage), e.Arguments);
                    }
                    else if (new SettingsClass().WindowsHello)
                    {
                        ApplicationLanguages.PrimaryLanguageOverride = Settings.AppLanguage;
                        rootFrame.Navigate(typeof(WindowsHelloPage), e.Arguments);
                    }
                    else
                    {
                        ApplicationLanguages.PrimaryLanguageOverride = Settings.AppLanguage;
                        rootFrame.Navigate(typeof(MainPage), e.Arguments);
                    }
                    Settings.LaunchCount++;
                    ApplicationView View = ApplicationView.GetForCurrentView();
                    View.IsScreenCaptureEnabled = Settings.CanRecord;
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        #region Error handling

        private static void OnUnobservedException(object sender, UnobservedTaskExceptionEventArgs e) => e.SetObserved();

        private static void OnUnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e) => e.Handled = true;

        private void CurrentDomain_FirstChanceException(object sender, FirstChanceExceptionEventArgs e)
        {
        }
        #endregion

    }
}
