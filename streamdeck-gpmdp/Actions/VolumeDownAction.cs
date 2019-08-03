using BarRaider.SdTools;
using System;

namespace BarRaider.GPMDP.Actions
{
    [PluginActionId("com.barraider.gpmdp.voldown")]
    public class VolumeDownAction : VolumeActionBase
    {
        public VolumeDownAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
        }

        public override void KeyPressed(KeyPayload payload)
        {
            baseHandledKeypress = false;
            base.KeyPressed(payload);

            if (!baseHandledKeypress)
            {
                int volume;
                if (int.TryParse(Settings.VolumeParam, out volume))
                {
                    // Spotify gets fussy if the values are out of range
                    int totalVolume = currentVolume - volume;
                    if (totalVolume < 0)
                    {
                        totalVolume = 0;
                    }
                    gpmdpManager.SetVolume(totalVolume);
                }
            }
        }

        public async override void OnTick()
        {
            baseHandledOnTick = false;
            base.OnTick();

            if (!baseHandledOnTick)
            {
                await Connection.SetImageAsync((String)null);

                if (Settings.ShowVolumeLevel)
                {
                    await Connection.SetTitleAsync(currentVolume.ToString());
                }
                else
                {
                    await Connection.SetTitleAsync(null);
                }
            }
        }
    }
}
