using BarRaider.SdTools;
using System;

namespace BarRaider.GPMDP.Actions
{
    [PluginActionId("com.barraider.gpmdp.volset")]
    public class VolumeSetAction : VolumeActionBase
    {
        public VolumeSetAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
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
                    gpmdpManager.SetVolume(volume);
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
            }
        }
    }
}
