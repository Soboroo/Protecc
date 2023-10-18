﻿using Microsoft.UI.Xaml.Controls;
using OtpNet;
using Protecc.Classes;
using Protecc.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TextBlockFX;
using TextBlockFX.Win2D.UWP;
using TextBlockFX.Win2D.UWP.Effects;
using Windows.ApplicationModel.Core;
using Windows.Security.Credentials;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using FX = TextBlockFX.Win2D.UWP;
using ProgressRing = Microsoft.UI.Xaml.Controls.ProgressRing;

namespace Protecc.Helpers
{
    public class OTPHelper : INotifyPropertyChanged, IDisposable
    {
        private ThreadPoolTimer PeriodicTimer;
        private Otp OTP;
        private int Time;
        private string _Code;
        private int OTPType;
        private int Counter;
        public string Code
        {
            get
            {
                return _Code;
            }
            set
            {
                SetField(ref _Code, value, "Code");
            }
        }
        private double _Maximum;
        public double Maximum
        {
            get
            {
                return _Maximum;
            }
            set
            {
                SetField(ref _Maximum, value, "Maximum");
            }
        }
        private double _TimeRemaining;
        public double TimeRemaining
        {
            get
            {
                return _TimeRemaining;
            }
            set
            {
                SetField(ref _TimeRemaining, value, "TimeRemaining");
            }
        }
        private int Digits;
        public OTPHelper(VaultItem vault)
        {
            try
            {
                Time = DataHelper.DecodeTime(vault.Resource);
                Maximum = Time;
                Digits = DataHelper.DecodeDigits(vault.Resource);
                Maximum = Time - 1;
                OTPType = DataHelper.OTPTypeId(vault.Resource);
                if (OTPType == 0)
                {
                    OTP = new Totp(CredentialService.GetKey(vault), step: Time, DataHelper.DecodeEncryption(vault.Resource), totpSize: Digits);
                    Code = FormatCode(((Totp)OTP).ComputeTotp(DateTime.UtcNow));
                }
                else if (OTPType == 1)
                {
                    OTP = new Hotp(CredentialService.GetKey(vault), hotpSize: Digits);
                    Counter = DataHelper.Counter(vault.Resource);
                    Code = FormatCode(((Hotp)OTP).ComputeHOTP(Counter)); // TODO: Add counter to vault
                }
                PeriodicTimer = ThreadPoolTimer.CreatePeriodicTimer(TimerElapsed, TimeSpan.FromMilliseconds(1000));
            }
            catch
            {
                //  PasswordVault Vault = new PasswordVault();
                //   Debug.WriteLine(Vault.Retrieve(vault.Resource, vault.Name).Password);
            }
        }

        public void UpdateHOTP(VaultItem vaultItem)
        {
            if (OTPType == 1)
            {
                Code = FormatCode(((Hotp)OTP).ComputeHOTP(++Counter));
                CredentialService.CounterIncrement(vaultItem);
            }
            else throw new Exception("OTP is not HOTP");
        }

        private async void TimerElapsed(ThreadPoolTimer timer)
        {
            if (OTPType == 0)
            {
                Totp OTP = (Totp)this.OTP;
                if (OTP.RemainingSeconds() == 1)
                {
                    TimeRemaining = 0;
                    TimeRemaining = Time - 1;
                    await Task.Delay(1000);
                    Code = FormatCode(OTP.ComputeTotp(DateTime.UtcNow));
                }
                else if (OTP.RemainingSeconds() <= Time - 1)
                {
                    TimeRemaining = OTP.RemainingSeconds();
                }
            }
            else if (OTPType == 1)
            {
                TimeRemaining = 0;
            }
        }

        private string FormatCode(string code)
        {
            if (Digits == 6)
            {
                return code.Substring(0, 2) + " " + code.Substring(2, 2) + " " + code.Substring(4, 2);
            }
            else // Assume 8 digits
            {
                return code.Substring(0, 2) + " " + code.Substring(2, 2) + " " + code.Substring(4, 2) + " " + code.Substring(6, 2);
            }
        }

        public void Dispose()
        {
            try
            {
                PeriodicTimer.Cancel();
            }
            catch
            {

            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected async virtual void OnPropertyChanged(string propertyName)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
            });
        }
        protected bool SetField<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
