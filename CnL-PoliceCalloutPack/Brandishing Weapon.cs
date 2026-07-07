using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using FivePD.API;
using FivePD.API.Utils;

namespace Callouts
{
    [CalloutProperties("Brandishing a Weapon", "LosAngelesi", "1.1")]
    public class Brandishing_Weapon : FivePD.API.Callout
    {
        private Ped suspect;
        private readonly Random rnd = new Random();
        public Brandishing_Weapon()
        {
            int distance = rnd.Next(520, 750);
            float offsetX = rnd.Next(-1 * distance, distance);
            float offsetY = rnd.Next(-1 * distance, distance);

            ShortName = "Person Brandishing Weapon";
            CalloutDescription = "Person brandishing weapon in public, respond Code 3";
            ResponseCode = 3;
            StartDistance = 280f;
        }

        protected void InitBlip(float circleRadius = 130f, BlipColor color = BlipColor.Blue, BlipSprite sprite = BlipSprite.BigCircle, int alpha = 100)
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

        public async override void OnStart(Ped closest)
        {
            base.OnStart(closest);

            int chance = rnd.Next(0, 10);
            if (chance >= 0 && chance <= 8)
            {
                ShowNetworkedNotification("On Scene", "CHAR_CALL911", "CHAR_CALL911", "Dispatch", "MDT UPDATE:", 10f);

                suspect = await SpawnPed(RandomUtils.GetRandomPed(), Location);
                suspect.AttachBlip();
                suspect.AttachedBlip.Sprite = (BlipSprite)280;
                suspect.AttachedBlip.Color = (BlipColor)1;
                suspect.AlwaysKeepTask = true;
                suspect.BlockPermanentEvents = true;
                suspect.Weapons.Give(GetRandomWeapon(), 1, true, true);
                PedQuestion question = new PedQuestion();
                await BaseScript.Delay(15000);

                int actionChance = rnd.Next(0, 10);
                if (actionChance >= 0 && actionChance <= 4)
                {
                    suspect.Task.FightAgainst(Game.PlayerPed);
                }
                else
                {
                    suspect.Task.WanderAround();
                }
            }
            else
            {
                ShowNetworkedNotification("On Scene, no suspect, code 4", "CHAR_CALL911", "CHAR_CALL911", "Dispatch", "MDT UPDATE:", 10f);
                EndCallout();
            }
        }


        public override void OnCancelBefore()
        {
            suspect?.AttachedBlip?.Delete();
            suspect?.MarkAsNoLongerNeeded();
            suspect?.Task.WanderAround();
            suspect?.Delete();
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
                WeaponHash.Molotov,
                WeaponHash.Hatchet,
                WeaponHash.SwitchBlade,
                WeaponHash.Machete,
                WeaponHash.KnuckleDuster
            };
            return weapons[rnd.Next(weapons.Count)];
        }
    }
}