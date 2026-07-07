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
    [CalloutProperties("911 Hangup", "LosAngelesi", "1.1")]
    public class _911_Hangup : FivePD.API.Callout
    {
        private readonly Random rnd = new Random();
        private Ped ped;
        private Vehicle stolenVehicle;
        private int calloutPath;
        private bool onScene = false, reportTaken = false, suspectArrested = false;
        public _911_Hangup()
        {
            // Initialize the callout location
            int distance = rnd.Next(400, 750);
            float offsetX = rnd.Next(-1 * distance, distance);
            float offsetY = rnd.Next(-1 * distance, distance);

            InitInfo(World.GetNextPositionOnStreet(Game.PlayerPed.GetOffsetPosition(new Vector3(offsetX, offsetY, 0))));

            ShortName = "911 Hangup";
            CalloutDescription = "We've received a 911 Hangup, no answer. Respond Code 1.";
            ResponseCode = 1;
            StartDistance = 250f;
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
            if (calloutPath >= 18 && calloutPath < 20)
            {
                Tick += new Func<Task>(upgradeCall);
            }
        }
        public async override void OnStart(Ped player)
        {
            base.OnStart(player);
            ShowDialog("*Search the area*", 7000, 25f);
            ShowNetworkedNotification("On Scene", "CHAR_CALL911", "CHAR_CALL911", "Dispatch", "MDT UPDATE:", 10f);
            //Sus&Ped Variables
            int chance = rnd.Next(0, 100);

            // 80% chance that the victim will be on scene, otherwise alternate outcomes
            if (chance <= 80)
            {
                ped = await SpawnPed(FivePD.API.Utils.RandomUtils.GetRandomPed(), Location);
                ped.BlockPermanentEvents = true;
                ped.Task.WanderAround();
                ped.AttachBlip();
                ped.AttachedBlip.Sprite = (BlipSprite)280;

                // 10% chance that the ped will flee
                if (chance <= 10)
                {
                    ped.Task.ReactAndFlee(player);
                    if (chance <= 5) ped.Weapons.Give(GetRandomWeapon(), 1, true, true);
                }
                // 10% chance that the ped will fight
                else if (chance > 10 && chance <= 20)
                {
                    ped.Task.FightAgainst(player);
                    if (chance <= 15) ped.Weapons.Give(GetRandomWeapon(), 1, true, true);
                    ped.Task.ShootAt(player);
                }
                // 5% chance that the ped is dead
                else if (chance > 20 && chance <= 25)
                {
                    ped.Kill();
                }
                // 5% chance that the ped is stealing from a nearby vehicle
                else if (chance > 25 && chance <= 30)
                {
                    stolenVehicle = await SpawnVehicle(FivePD.API.Utils.RandomUtils.GetRandomVehicle(), Location + new Vector3(0, 5, 0));
                    ped.Task.EnterVehicle(stolenVehicle, VehicleSeat.Driver);

                    Vector3 targetLocation = World.GetNextPositionOnStreet(Location + new Vector3(0, 500, 0));
                    ped.Task.DriveTo(stolenVehicle, targetLocation, 0f, 20f, 786603);

                    stolenVehicle.AttachBlip();
                    stolenVehicle.AttachedBlip.Sprite = BlipSprite.PersonalVehicleCar;
                    stolenVehicle.AttachedBlip.Color = BlipColor.Red;
                    stolenVehicle.AttachedBlip.Name = "Stolen Vehicle";
                }
                // 10% chance that there's an injured person at the scene.
                if (chance > 30 && chance <= 40)
                {
                    // Spawn an injured ped at the scene.
                    Ped injuredPed = await SpawnPed(FivePD.API.Utils.RandomUtils.GetRandomPed(), Location + new Vector3(0, 5, 0));

                    // Play the injured animation.
                    Function.Call(Hash.TASK_PLAY_ANIM, injuredPed.Handle, "random@domestic", "bystander_knockout_loop", 8f, -8f, -1, 1, 0, false, false, false);

                    // Add a blip for the injured ped.
                    injuredPed.AttachBlip();
                    injuredPed.AttachedBlip.Sprite = BlipSprite.Health;
                    injuredPed.AttachedBlip.Color = BlipColor.Red;
                    injuredPed.AttachedBlip.Name = "Injured Person";

                    // Tell the player to call for an ambulance.
                    ShowNetworkedNotification("Injured person at the scene. Call for an ambulance.", "CHAR_CALL911", "CHAR_CALL911", "Dispatch", "MDT UPDATE:", 10f);
                }

                ShowNetworkedNotification("On Scene", "CHAR_CALL911", "CHAR_CALL911", "Dispatch", "MDT UPDATE:", 10f);
                string[] startdialogue = new string[]
                {
                "Subject: Sorry officer!",
                "Subject: What's going on?",
                "Subject: Can I help you, sir?"
                };
                ShowDialog(startdialogue[rnd.Next(0, startdialogue.Length)], 5000, 25f);
            }
            else
            {
                ShowNetworkedNotification("On Scene, no suspect, code 4.", "CHAR_CALL911", "CHAR_CALL911", "Dispatch", "MDT UPDATE:", 10f);
                EndCallout();
            }
        }

        public override void OnCancelBefore()
        {
            ped?.AttachedBlip?.Delete();
            ped?.MarkAsNoLongerNeeded();
            ped?.Task.WanderAround();
            ped?.Delete();
            stolenVehicle?.Delete();
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
                WeaponHash.Unarmed,
                WeaponHash.Hatchet,
                WeaponHash.SwitchBlade,
                WeaponHash.Machete,
                WeaponHash.KnuckleDuster
            };
            return weapons[rnd.Next(weapons.Count)];

        }
        private async Task upgradeCall()
        {
            Tick -= new Func<Task>(upgradeCall);
            await BaseScript.Delay(rnd.Next(10, 25) * 1000);
            if (onScene)
                return;

            this.ResponseCode = 3;
            this.UpdateData();

            ShowNetworkedNotification("RP reporting suspect is getting violent. ~y~Upgrade Response.", "CHAR_CALL911", "CHAR_CALL911", "Dispatch", "MDT UPDATE:", 5f);
            BaseScript.TriggerEvent("FivePDAudio::DispatchPlay", new object[] {
                @"ATTENTION_GENERIC/ATTENTION_GENERIC_01.ogg,DISPATCH_RESPOND_CODE/UNIT_RESPOND_CODE3.ogg"
            });
        }
    }
}