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
                // Toggle
                if (currentVolume == 0)
                {
                    int volumeToggle;
                    if (int.TryParse(Settings.VolumeParam, out volumeToggle))
                    {
                        gpmdpManager.SetVolume(volumeToggle);
                    }
                }
                else
                {
                    Settings.VolumeParam = currentVolume.ToString();
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
                if (currentVolume == 0)
                {
                    await Connection.SetImageAsync(Properties.Settings.Default.ImgVolumeSet);
                }
                else
                {
                    await Connection.SetImageAsync(Properties.Settings.Default.ImgVolumeMute);
                }
            }
        }
    }
}
