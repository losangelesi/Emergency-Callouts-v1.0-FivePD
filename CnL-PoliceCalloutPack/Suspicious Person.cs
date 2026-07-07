using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using FivePD.API;
using FivePD.API.Utils;

namespace EmergencyCallouts
{
    [CalloutProperties("Suspicious person", "LosAngelesi", "1.1")]
    public class SuspiciousPerson : FivePD.API.Callout
    {
        private readonly Random rnd = new Random();
        private Ped ped;

        public SuspiciousPerson()
        {
            int distance = rnd.Next(470, 740);
            float offsetX = rnd.Next(-1 * distance, distance);
            float offsetY = rnd.Next(-1 * distance, distance);

            InitInfo(World.GetNextPositionOnStreet(Game.PlayerPed.GetOffsetPosition(new Vector3(offsetX, offsetY, 0))));

            ShortName = "Suspicious Person";
            CalloutDescription = "We've received a report of a suspicious person. Respond code 2.";
            ResponseCode = 2;
            StartDistance = 240f;
        }

        protected void InitBlip(float circleRadius = 125f, BlipColor color = BlipColor.Blue, BlipSprite sprite = BlipSprite.BigCircle, int alpha = 100)
        {
            Blip blip = World.CreateBlip(this.Location, circleRadius);
            this.Radius = circleRadius;
            this.Marker = blip;
            this.Marker.Sprite = sprite;
            this.Marker.Color = color;
            this.Marker.Alpha = alpha;
        }

        public override async Task OnAccept()
        {
            InitBlip();
            UpdateData();
        }
        public async override void OnStart(Ped player)
        {
            base.OnStart(player);
            ped = await SpawnPed(FivePD.API.Utils.RandomUtils.GetRandomPed(), Location);
            ped.BlockPermanentEvents = true;
            ped.AlwaysKeepTask = true;
            PedQuestion question = new PedQuestion();
            ped.AttachBlip();
            ped.AttachedBlip.Sprite = (BlipSprite)465;
            ShowNetworkedNotification("On Scene", "CHAR_CALL911", "CHAR_CALL911", "Dispatch", "MDT UPDATE:", 10f);

            //Sus&Ped Variables
            int chance = rnd.Next(0, 10);
            // 70% that the victim will talk to player, otherwise run away
            if (chance >= 0 && chance <= 7)
            {
                ped.AttachedBlip.Color = (BlipColor)46;
                ped.Task.WanderAround();
                ShowDialog("*Observing for suspicious people*", 7000, 15f);

                if (chance > 0 && chance <= 2)
                {
                    ped.Weapons.Give(WeaponHash.Crowbar, 1, true, true);
                    ped.Task.ReactAndFlee(player);
                }
            }
            else
            {
                ped.AttachedBlip.Color = (BlipColor)3;
                ped.Task.ReactAndFlee(player);
                ShowDialog("LATER BRO!", 7000, 15f);
            }

        }
        public override void OnCancelBefore()
        {
            ped?.AttachedBlip?.Delete();
            ped?.MarkAsNoLongerNeeded();
            ped?.Task.WanderAround();
            ped?.Delete();
            base.OnCancelBefore();
        }
    }
}
