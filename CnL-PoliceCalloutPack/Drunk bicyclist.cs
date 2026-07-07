using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using FivePD.API;
using FivePD.API.Utils;
using static CitizenFX.Core.Native.API;

namespace Callouts
{
    [CalloutProperties("DUI Bike", "LosAngelesi", "1.0")]
    public class Drunk_bicyclist : FivePD.API.Callout
    {
        private readonly Random rnd = new Random();
        private Ped driver;
        private Vehicle vehicle;

        public Drunk_bicyclist()
        {
            int distance = rnd.Next(370, 860);
            float offsetX = rnd.Next(-1 * distance, distance);
            float offsetY = rnd.Next(-1 * distance, distance);

            InitInfo(World.GetNextPositionOnStreet(Game.PlayerPed.GetOffsetPosition(new Vector3(offsetX, offsetY, 0))));
            ShortName = "Poss. DUI bicycle";
            CalloutDescription = "Reports of a possible drunk bicyclist, respond Code 1";
            ResponseCode = 1;
            StartDistance = 220f;
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
            driver = await SpawnPed(RandomUtils.GetRandomPed(), Location);
            vehicle = await SpawnVehicle(VehicleHash.TriBike2, Location);
            driver.AttachBlip();
            driver.AttachedBlip.Sprite = (BlipSprite)348;
            driver.AttachedBlip.Color = (BlipColor)5;
            driver.SetIntoVehicle(vehicle, VehicleSeat.Driver);
            driver.BlockPermanentEvents = true;
            TaskVehicleDriveWander(driver.Handle, vehicle.Handle, 15f, 899);
            ShowNetworkedNotification("On Scene", "CHAR_CALL911", "CHAR_CALL911", "Dispatch", "MDT UPDATE:", 10f);
            float targetSpeed = 10f * 0.44704f; // Convert 10 mph
            driver.Task.CruiseWithVehicle(vehicle, targetSpeed, (int)DrivingStyle.Normal);


            var data = await driver.GetData();
            data.BloodAlcoholLevel = rnd.NextDouble() * (0.3 - 0.01) + 0.01;
            data.BloodAlcoholLevel = Math.Round(data.BloodAlcoholLevel, 2);
            Item Wine = new Item
            {
                Name = "Wine",
                IsIllegal = true
            };
            driver.SetData(data);
            int chance = rnd.Next(0, 10);
            if (chance >= 0 && chance <= 1)
            {
                driver.Task.ReactAndFlee(player);
                Utilities.ExcludeVehicleFromTrafficStop(vehicle.NetworkId, true);
                var pursuit = Pursuit.RegisterPursuit(driver);
                float targetSpeed2 = 15f * 0.44704f; // Convert 15 mph
                driver.Task.CruiseWithVehicle(vehicle, targetSpeed, (int)DrivingStyle.IgnorePathing);
            };
        }
        public override void OnCancelBefore()
        {
            driver?.AttachedBlip?.Delete();
            driver?.MarkAsNoLongerNeeded();
            driver?.Task.WanderAround();
            driver?.Delete();
            vehicle?.Delete();
            base.OnCancelBefore();
        }
    }
}