using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using FivePD.API;
using FivePD.API.Utils;

namespace EmergencyCallouts
{
    [CalloutProperties("Unresponsive", "LosAngelesi", "1.1")]
    public class UnresponsivePerson : FivePD.API.Callout
    {
        private readonly Random rnd = new Random();
        private Ped ped;
        public UnresponsivePerson()
        {
            int distance = rnd.Next(470, 850);
            float offsetX = rnd.Next(-1 * distance, distance);
            float offsetY = rnd.Next(-1 * distance, distance);

            InitInfo(World.GetNextPositionOnStreet(Game.PlayerPed.GetOffsetPosition(new Vector3(offsetX, offsetY, 0))));

            ShortName = "Unresponsive Person";
            CalloutDescription = "We've received a report of an unresponsive person. Respond Code 3.";
            ResponseCode = 3;
            StartDistance = 100f;
        }

        protected void InitBlip(float circleRadius = 75f, BlipColor color = BlipColor.White, BlipSprite sprite = BlipSprite.BigCircle, int alpha = 100)
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
            ped.Kill();
            ped.AttachBlip();
            ped.AttachedBlip.Sprite = (BlipSprite)153;
            ped.AttachedBlip.Color = (BlipColor)57;
            ShowNetworkedNotification("On Scene", "CHAR_CALL911", "CHAR_CALL911", "Dispatch", "MDT UPDATE:", 10f);
            int chance = rnd.Next(0, 3);

            Tick += async () =>
            {
                if (ped.IsAlive)
                {
                    switch (chance)
                    {
                        case 0:
                            ped.Task.LookAt(player);
                            ShowNetworkedNotification("Thank you! Can you take me to the hospital?", "CHAR_CALL911", "CHAR_CALL911", "Dispatch", "MDT UPDATE:", 10f);
                            break;
                        case 1:
                            ped.Task.WanderAround();
                            ShowNetworkedNotification("I'm feeling a bit woozy...", "CHAR_CALL911", "CHAR_CALL911", "Dispatch", "MDT UPDATE:", 10f);
                            break;
                        case 2:
                            ped.Weapons.Give(GetRandomWeapon(), 1, true, true);
                            ped.Task.FightAgainst(player);
                            ShowNetworkedNotification("You'll never take me alive!", "CHAR_CALL911", "CHAR_CALL911", "Dispatch", "MDT UPDATE:", 10f);
                            break;
                    }
                }
            };
        }
        public override void OnCancelBefore()
        {
            ped?.AttachedBlip?.Delete();
            ped?.MarkAsNoLongerNeeded();
            ped?.Delete();
            base.OnCancelBefore();
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
