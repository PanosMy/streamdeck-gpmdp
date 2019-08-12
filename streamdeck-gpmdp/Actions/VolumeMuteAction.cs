using BarRaider.SdTools;

namespace BarRaider.GPMDP.Actions
{
    [PluginActionId("com.barraider.gpmdp.voltoggle")]
    public class VolumeMuteAction : VolumeActionBase
    {
        public VolumeMuteAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
        }

        public async override void KeyPressed(KeyPayload payload)
        {
            baseHandledKeypress = false;
            base.KeyPressed(payload);

            if (!baseHandledKeypress)
            {
                if (!gpmdpManager.IsConnected)
                {
                    await Connection.ShowAlert();
                    return;
                }

                // Toggle
                int volume = gpmdpManager.GetVolume();              
                if (volume == 0)
                {
                    if (int.TryParse(Settings.VolumeParam, out int volumeToggle))
                    {
                        gpmdpManager.SetVolume(volumeToggle);
                    }
                }
                else
                {
                    Settings.VolumeParam = volume.ToString();
                    gpmdpManager.SetVolume(0);
                    await SaveSettings();
                }
            }
        }

        public async override void OnTick()
        {
            baseHandledOnTick = false;
            base.OnTick();

            if (!baseHandledOnTick)
            {
                if (!gpmdpManager.IsConnected)
                {
                    return;
                }

                int volume = gpmdpManager.GetVolume();
                if (volume == 0)
                {
                    await Connection.SetImageAsync(Properties.Settings.Default.ImgVolumeSet);
                }
                else
                {
                    await Connection.SetImageAsync(Properties.Settings.Default.ImgVolumeMute);
                }

                if (Settings.ShowVolumeLevel)
                {
                    await Connection.SetTitleAsync(volume.ToString());
                }
                else
                {
                    await Connection.SetTitleAsync(null);
                }
            }
        }
    }
}
