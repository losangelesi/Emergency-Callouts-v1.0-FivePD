using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using FivePD.API;
using FivePD.API.Utils;

namespace EmergencyCallouts
{
    [CalloutProperties("Domestic Arguement", "LosAngelesi & Chris07", "1.1")]
    public class DomesticArguement : FivePD.API.Callout
    {
        private readonly Random rnd = new Random();
        private Ped suspect, victim, witness;
        private Vehicle vehicle;

        public DomesticArguement(Vehicle vehicle)
        {
            this.vehicle = vehicle;
        }
        public DomesticArguement()
        {
            int distance = rnd.Next(530, 760);
            float offsetX = rnd.Next(-1 * distance, distance);
            float offsetY = rnd.Next(-1 * distance, distance);

            InitInfo(World.GetNextPositionOnStreet(Game.PlayerPed.GetOffsetPosition(new Vector3(offsetX, offsetY, 0))));

            ShortName = "Domestic Arguement";
            CalloutDescription = "We've received a report of a Domestic Arguement. Respond code 1.";
            ResponseCode = 1;
            StartDistance = 250f;
        }

        protected void InitBlip(float circleRadius = 100f, BlipColor color = BlipColor.Blue, BlipSprite sprite = BlipSprite.BigCircle, int alpha = 100)
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
            ShowNetworkedNotification("On Scene", "CHAR_CALL911", "CHAR_CALL911", "Dispatch", "MDT UPDATE:", 10f);
            suspect = await SpawnPed(FivePD.API.Utils.RandomUtils.GetRandomPed(), Location);
            victim = await SpawnPed(FivePD.API.Utils.RandomUtils.GetRandomPed(), suspect.GetOffsetPosition(new Vector3(6f, 4f, 0f)));
            suspect.BlockPermanentEvents = true;
            victim.BlockPermanentEvents = true;
            suspect.AlwaysKeepTask = true;
            victim.AlwaysKeepTask = true;
            PedQuestion question = new PedQuestion();
            suspect.AttachBlip();
            victim.AttachBlip();
            suspect.Task.LookAt(player);
            victim.Task.LookAt(player);
            suspect.AttachedBlip.Sprite = (BlipSprite)465;
            victim.AttachedBlip.Sprite = (BlipSprite)465;

            //Sus&Ped Variables
            int chance = rnd.Next(0, 10);
            // 70% that the victim will talk to player, otherwise run away
            if (chance >= 0 && chance <= 5)
            {
                string[] startdialogue = new string[]
                {
                    "Suspect: Leave us alone!",
                    "Victim: help!"
                };
                ShowDialog(startdialogue[rnd.Next(0, startdialogue.Length)], 5000, 25f);
                chance = rnd.Next(0, 10);
                if (chance >= 0 && chance <= 2)
                {
                    suspect.Task.FleeFrom(player);
                    suspect.Weapons.Give(GetRandomWeapon(), 1, true, true);
                }
            }
            else
            {
                witness = await SpawnPed(FivePD.API.Utils.RandomUtils.GetRandomPed(), victim.GetOffsetPosition(new Vector3(3f, 4f, 0f)));
                witness.Task.LookAt(player);
                witness.AttachBlip();
                witness.AttachedBlip.Sprite = (BlipSprite)465;
                if (chance >= 0 && chance <= 10)
                    {
                        suspect.Task.ReactAndFlee(player);
                        if (chance >= 0 && chance <= 0.5) suspect.Weapons.Give(GetRandomWeapon(), 1, true, true);
                    }
                if (chance >= 0 && chance <= 2)
                    {
                        suspect.Weapons.Give(GetRandomWeapon(), 1, true, true);
                        suspect.Task.FightAgainst(player);
                    }
                if (chance >= 0 && chance <= 0.5) suspect.Kill();
                if (chance >= 0 && chance <= 1) victim.Weapons.Give(GetRandomWeapon(), 1, true, true);
                if (chance >= 0 && chance <= 1) witness.Weapons.Give(GetRandomWeapon(), 1, true, true);
                if (chance >= 0 && chance <= 1)
                {
                    vehicle = await SpawnVehicle(RandomUtils.GetRandomVehicle(), Location);
                    suspect.SetIntoVehicle(vehicle, VehicleSeat.Driver);
                    suspect.Task.CruiseWithVehicle(vehicle, 35);
                    suspect.Task.FleeFrom(player);
                }
            };
        }
        public override void OnCancelBefore()
        {
            suspect?.AttachedBlip?.Delete();
            suspect?.MarkAsNoLongerNeeded();
            suspect?.Task.WanderAround();
            suspect?.Delete();
            victim?.AttachedBlip?.Delete();
            victim?.MarkAsNoLongerNeeded();
            victim?.Task.WanderAround();
            victim?.Delete();
            base.OnCancelBefore();
            vehicle?.Delete();
        }

        //weapon list
        private WeaponHash GetRandomWeapon()
        {
            List<WeaponHash> weapons = new List<WeaponHash>
            {
                WeaponHash.Bottle,
                WeaponHash.Crowbar,
                WeaponHash.Dagger,
                WeaponHash.Knife,
                WeaponHash.Hammer,
                WeaponHash.Wrench,
                WeaponHash.APPistol,
                WeaponHash.BattleAxe,
                WeaponHash.GolfClub,
                WeaponHash.Bat,
                WeaponHash.FlareGun,
                WeaponHash.Hatchet,
                WeaponHash.Unarmed,
                WeaponHash.Machete,
                WeaponHash.KnuckleDuster
            };
            return weapons[rnd.Next(weapons.Count)];

        }
    }
}