﻿using Protecc.Classes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Windows.UI;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;
using Protecc.Helpers;
using System.Diagnostics;
using OtpNet;
using Windows.Storage;

namespace Protecc.Services
{
    ///     Credentials are stored with three parameters. Name, Key and Resource
    ///     The Name contains the account name
    ///     The Key contains the 2FA key string
    ///     The resource contains a 9 digit identifier string with format: 
    ///     #Color in HEX format, Time in seconds (max 2 digits), Number of code digits (max 1 digit), Index representing encryptionmode enum
    ///     Encryption enums: 0 = Sha1, 1 = Sha256, 2 = Sha512
    ///     OTP type: 0 = TOTP, 1 = HOTP; In HOTP case, the time will be ignored
    ///     Example: Color white, 30 seconds, 6 digits, Sha512, TOTP will be FFFFFF30620
    ///     Example: Color black, 60 seconds, 8 digits, Sha1, HOTP will be 00000060801
    ///     Full Example: Name "Twitter", Color blue, 30 seconds, 6 digits, Sha1, TOTP will be "Twitter0000ff30600"

    public class CredentialService
    {
        private static PasswordVault Vault = new PasswordVault();
        public static ObservableCollection<VaultItem> CredentialList = new ObservableCollection<VaultItem>();

        protected internal static async void StoreNewCredential(string Name, string Key, Color Color, int TimeIndex, int DigitsIndex, int Encryptionindex, int OTPTypeIndex, StorageFile ImageFile)
        {
            string Resource = DataHelper.Encode(Color, TimeIndex, DigitsIndex, Encryptionindex, OTPTypeIndex);
            Vault.Add(new PasswordCredential(Resource, Name, Key));
            CredentialList.Add(new VaultItem(Name, Resource));

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;

            // Setting in a container
            ApplicationDataContainer container = localSettings.CreateContainer("OTPResourceContainer", ApplicationDataCreateDisposition.Always);

            if (localSettings.Containers.ContainsKey("OTPResourceContainer"))
            {
                ApplicationDataCompositeValue composite = new ApplicationDataCompositeValue();
                composite["Name"] = Name;
                composite["Counter"] = 0;

                localSettings.Containers["OTPResourceContainer"].Values[Name] = composite;
            }

            if (ImageFile != null)
            {
                Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(ImageFile);
                await ImageFile.CopyAsync(localFolder, Name + ".png", NameCollisionOption.ReplaceExisting);
            }
            else
            {
                // No image selected
                try
                {
                    await localFolder.GetFileAsync(Name + ".png").AsTask().Result.DeleteAsync();
                }
                catch (Exception)
                {
                    // No image
                }
            }
        }

        /// <summary>
        /// Edit existing credential and save it to Vault
        /// </summary>
        /// <param name="vaultItem"></param>
        /// <param name="newName"></param>
        /// <param name="newColor"></param>
        protected internal static async void EditCredential(VaultItem vaultItem, string newName, Color newColor, StorageFile imageFile)
        {
            // Retrieve required data
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            var password = Vault.Retrieve(vaultItem.Resource, vaultItem.Name).Password;
            ApplicationDataCompositeValue resource = (ApplicationDataCompositeValue)localSettings.Containers["OTPResourceContainer"].Values[vaultItem.Name];

            // Remove old item from vault and from UI
            Vault.Remove(Vault.Retrieve(vaultItem.Resource, vaultItem.Name));
            CredentialList.Remove(vaultItem);
            localSettings.Values.Remove(vaultItem.Name);

            // Create a new item and copy old properties
            string Resource = DataHelper.EncodeEdited(
                newColor, 
                DataHelper.DecodeTime(vaultItem.Resource),
                DataHelper.DecodeDigits(vaultItem.Resource),
                DataHelper.DecodeEncryptionId(vaultItem.Resource),
                DataHelper.OTPTypeId(vaultItem.Resource)
                );
            resource["Name"] = newName;

            if (imageFile != null)
            {
                Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(imageFile);
                await imageFile.CopyAsync(localFolder, newName + ".png", NameCollisionOption.ReplaceExisting);
            } 
            else
            {
                // No image selected
                try
                {
                    await localFolder.GetFileAsync(newName + ".png").AsTask().Result.DeleteAsync();
                }
                catch (Exception)
                {
                    // No image
                }
            }

            //Save to credential vault and add to UI
            Vault.Add(new PasswordCredential(Resource, newName, password));
            CredentialList.Add(new VaultItem(newName, Resource));
            localSettings.Containers["OTPResourceContainer"].Values[newName] = resource;
        }

        protected internal static void CounterIncrement(VaultItem vaultItem)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;

            // Setting in a container
            ApplicationDataContainer container = localSettings.CreateContainer("OTPResourceContainer", ApplicationDataCreateDisposition.Always);

            if (localSettings.Containers.ContainsKey("OTPResourceContainer"))
            {
                ApplicationDataCompositeValue resource = (ApplicationDataCompositeValue)localSettings.Containers["OTPResourceContainer"].Values[vaultItem.Name];
                resource["Counter"] = (int)resource["Counter"] + 1;
                localSettings.Containers["OTPResourceContainer"].Values[vaultItem.Name] = resource;
            }
        }

        protected internal static byte[] GetKey(VaultItem vaultItem)
        {
            byte[] Key;
            Key = Base32Encoding.ToBytes(Vault.Retrieve(vaultItem.Resource, vaultItem.Name).Password);
            return Key;
        }

        protected internal static void RemoveItem(VaultItem vaultItem)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            Vault.Remove(Vault.Retrieve(vaultItem.Resource, vaultItem.Name));
            CredentialList.Remove(vaultItem);
            localSettings.Values.Remove(vaultItem.Name);
        }

        protected internal async static Task RefreshListAsync()
        {
            CredentialList.Clear();
            await Task.Run(async() =>
            {
                foreach (var i in Vault.RetrieveAll())
                {
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        CredentialList.Add(new VaultItem(i.UserName, i.Resource));
                    });
                }
            });
        }

        protected internal async static Task<List<Account>> ExportAccountsAsync()
        {
            List<Account> Accounts = new();
            await Task.Run(async () =>
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    foreach (var i in Vault.RetrieveAll())
                    {
                        i.RetrievePassword();
                        Accounts.Add(new Account() { 
                            Name = i.UserName,
                            Resource = i.Resource,
                            Key = i.Password
                        });
                    }
                });
            });
            return Accounts;
        }
    }
}
