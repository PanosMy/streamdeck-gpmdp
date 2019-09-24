﻿using BarRaider.SdTools;
using System;
using GPMDP_Api.Playback;
using GPMDP_Api.Volume;
using GPMDP_Api.Enums;
using System.Threading.Tasks;

namespace BarRaider.GPMDP.Communication
{
    public class GpmdpManager : IDisposable
    {

        #region Public Methods
        public event EventHandler<EventArgs> TokenStatusChanged;

        public bool TokenExists
        {
            get
            {
                return TokenManager.Instance.TokenExists;
            }
        }

        public bool IsClientConnected
        {
            get
            {
                return GpmdpClient.Instance.IsClientConnected;
            }
        }

        public bool IsReadyForCommand
        {
            get
            {
                return TokenExists && GpmdpClient.Instance.IsClientConnected;
            }
        }

        public bool IsPlaying
        {
            get
            {
                return GpmdpClient.Instance.IsPlaying;
            }
        }


        public GpmdpManager()
        {
            TokenManager.Instance.TokensChanged += Instance_TokensChanged;
        }

        public void Dispose()
        {
            TokenManager.Instance.TokensChanged -= Instance_TokensChanged;
        }

        public void SubmitApprovalCode(string approvalCode)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Submitting Approval Code: {approvalCode}");
            GpmdpClient.Instance.Connect();
            GpmdpClient.Instance.Authenticate(approvalCode);
        }

        public void ForceConnect()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "ForceConnect called");
            GpmdpClient.Instance.Connect();
            TokenManager.Instance.Authenticate();
        }

        public void PlayPause()
        {
            GpmdpClient.Instance.GetClient().PlayPause();
        }

        public void NextSong()
        {
            GpmdpClient.Instance.GetClient().Next();
        }

        public void PreviousSong()
        {
            GpmdpClient.Instance.GetClient().Previous();
        }

        public async Task<ShuffleType> GetShuffle()
        {
            try
            {
                return await GpmdpClient.Instance.GetClient().GetShuffleAsync();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"GetShuffle failed: {ex}");
                return ShuffleType.Unknown;
            }
        }

        public async void ShuffleToggle()
        {
            await GpmdpClient.Instance.GetClient().ToggleShuffleAsync();
        }

        public void SetShuffle(ShuffleType type)
        {
            GpmdpClient.Instance.GetClient().SetShuffle(type);
        }

        public async Task<RepeatType> GetRepeat()
        {
            try
            {
                return await GpmdpClient.Instance.GetClient().GetRepeatAsync();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"GetRepeat failed: {ex}");
                return RepeatType.Unknown;
            }
        }

        public async void RepeatToggle()
        {
            await GpmdpClient.Instance.GetClient().ToggleRepeatAsync();
        }

        public int GetVolume()
        {
            return GpmdpClient.Instance.GetClient().GetVolume();
        }

        public void SetVolume(int volume)
        {
            GpmdpClient.Instance.GetClient().SetVolume(volume);
        }


        public void IncreaseVolume(int volume)
        {
            GpmdpClient.Instance.GetClient().IncreaseVolume(volume);
        }

        public void DecreaseVolume(int volume)
        {
            GpmdpClient.Instance.GetClient().DecreaseVolume(volume);
        }

        #endregion

        #region Private Methods

        private void Instance_TokensChanged(object sender, GpmdpTokenEventArgs e)
        {
            TokenStatusChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}
