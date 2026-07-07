using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using FivePD.API;
using FivePD.API.Utils;
using static CitizenFX.Core.Native.API;

namespace Callouts
{
    [CalloutProperties("Public Intoxication", "LosAngelesi", "1.1")]
    public class PublicIntox : FivePD.API.Callout
    {
        private readonly Random rnd = new Random();
        private Ped ped;
        private Prop beerBottle;

        public PublicIntox()
        {
            int distance = rnd.Next(420, 750);
            float offsetX = rnd.Next(-1 * distance, distance);
            float offsetY = rnd.Next(-1 * distance, distance);

            InitInfo(World.GetNextPositionOnStreet(Game.PlayerPed.GetOffsetPosition(new Vector3(offsetX, offsetY, 0))));
            ShortName = "Poss. Intoxicated Person";
            CalloutDescription = "Possible Person under the influence in public, respond Code 1";
            ResponseCode = 1;
            StartDistance = 250f;

        }

        protected void InitBlip(float circleRadius = 150f, BlipColor color = BlipColor.Blue, BlipSprite sprite = BlipSprite.BigCircle, int alpha = 100)
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
            ped = await SpawnPed(RandomUtils.GetRandomPed(), Location);
            beerBottle = await World.CreateProp("prop_beer_bottle", ped.Position, true, true);
            ped.AttachBlip();
            ped.AttachedBlip.Sprite = (BlipSprite)280;
            ped.AttachedBlip.Color = (BlipColor)5;
            ped.BlockPermanentEvents = true;
            ped.Task.WanderAround();
            ShowNetworkedNotification("On Scene", "CHAR_CALL911", "CHAR_CALL911", "Dispatch", "MDT UPDATE:", 10f);
            ped.Task.StartScenario("WORLD_HUMAN_DRINKING", ped.Position);
            ped.Task.PlayAnimation("amb@world_human_drinking@beer@male@idle_a", "idle_a", 8f, -1, -1, (AnimationFlags)49, 0);

            var data = await ped.GetData();
            data.BloodAlcoholLevel = rnd.NextDouble() * (0.4 - 0.01) + 0.01;
            data.BloodAlcoholLevel = Math.Round(data.BloodAlcoholLevel, 2);
            ped.SetData(data);

            int chance = rnd.Next(0, 10);
            if (chance >= 0 && chance <= 0.5)
            {
                ped.Task.ReactAndFlee(player);
            };
        }
        public override void OnCancelBefore()
        {
            ped.AttachedBlip?.Delete();
            beerBottle?.Delete();
            ped?.MarkAsNoLongerNeeded();
            ped?.Task.WanderAround();
            ped?.Delete();
            base.OnCancelBefore();
        }
    }
}