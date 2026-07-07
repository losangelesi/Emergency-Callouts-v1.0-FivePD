using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using FivePD.API;
using FivePD.API.Utils;
using static CitizenFX.Core.Native.API;

namespace EmergencyCallouts
{
    [CalloutProperties("Vehicle vs Ped", "LosAngelesi", "1.1")]
    public class Auto_vs_Ped : FivePD.API.Callout
    {
        private readonly Random rnd = new Random();
        private Ped driver, victim;
        private Vehicle vehicle;

        public Auto_vs_Ped()
        {
            int distance = rnd.Next(570, 790);
            float offsetX = rnd.Next(-1 * distance, distance);
            float offsetY = rnd.Next(-1 * distance, distance);

            InitInfo(World.GetNextPositionOnStreet(Game.PlayerPed.GetOffsetPosition(new Vector3(offsetX, offsetY, 0))));
            ShortName = "Auto vs Ped";
            CalloutDescription = "Auto vs Pedestrian, respond Code 3";
            ResponseCode = 3;
            StartDistance = 280f;
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
            driver = await SpawnPed(RandomUtils.GetRandomPed(), Location);
            victim = await SpawnPed(RandomUtils.GetRandomPed(), Location);
            Random random = new Random();
            VehicleHash selectedVehicleHash = acceptableVehicles[random.Next(acceptableVehicles.Length)];
            vehicle = await SpawnVehicle(selectedVehicleHash, Location);
            driver.AttachBlip();
            driver.AttachedBlip.Sprite = (BlipSprite)523;
            driver.AttachedBlip.Color = (BlipColor)5;
            driver.SetIntoVehicle(vehicle, VehicleSeat.Driver);
            driver.BlockPermanentEvents = true;
            victim.Kill();
            victim.AttachBlip();
            victim.AttachedBlip.Sprite = (BlipSprite)153;
            victim.AttachedBlip.Color = (BlipColor)57;
            ShowNetworkedNotification("On Scene", "CHAR_CALL911", "CHAR_CALL911", "Dispatch", "MDT UPDATE:", 10f);

            //damage for vehicle
            int engineDamage = random.Next(100, 400);
            int bodyDamage = random.Next(200, 600);

            vehicle.EngineHealth -= engineDamage;
            vehicle.BodyHealth -= bodyDamage;

            var data = await driver.GetData();
            data.BloodAlcoholLevel = (rnd.Next(1, 6) % 5 == 0) ? rnd.NextDouble() * (0.3 - 0.01) + 0.01 : 0;
            data.BloodAlcoholLevel = Math.Round(data.BloodAlcoholLevel, 2);
            driver.SetData(data);

            int chance = rnd.Next(0, 10);
            if (chance >= 0 && chance <= 1)
            {
                TaskVehicleDriveWander(driver.Handle, vehicle.Handle, 30f, 899);
                driver.Task.ReactAndFlee(player);
            }

            int outcome = rnd.Next(0, 4);
            switch (outcome)
            {
                case 0:
                await victim.Task.PlayAnimation("amb@world_human_bum_slumped@male@laying_on_left_side@idle_a", "idle_a", 8f, -8f, -1, (AnimationFlags)49, 0);
                break;
                case 1:

                await victim.Task.PlayAnimation("amb@world_human_bum_slumped@male@laying_on_left_side@idle_a", "idle_a", 8f, -8f, -1, (AnimationFlags)49, 0);
                driver.Task.LeaveVehicle(vehicle, true);
                driver.Task.ReactAndFlee(player);
                data = await driver.GetData();
                data.BloodAlcoholLevel = rnd.NextDouble() * (0.3 - 0.01) + 0.01;
                data.BloodAlcoholLevel = Math.Round(data.BloodAlcoholLevel, 2);
                Item Wine = new Item
                    {
                        Name = "Wine",
                        IsIllegal = true
                    };
                driver.SetData(data);
                // Set a random speed between 15 and 55 mph
                int randomSpeedMph = rnd.Next(15, 56);
                float targetSpeed = randomSpeedMph * 0.44704f;
                driver.Task.CruiseWithVehicle(vehicle, targetSpeed, (int)DrivingStyle.IgnorePathing);
                break;

                case 2:
                await victim.Task.PlayAnimation("amb@world_human_bum_slumped@male@laying_on_left_side@idle_a", "idle_a", 8f, -8f, -1, (AnimationFlags)49, 0);
                await BaseScript.Delay(3000);
                await victim.Task.PlayAnimation("move_strafe@injured", "idle", 8f, -8f, -1, (AnimationFlags)49, 0);
                break;

                case 3:
                await victim.Task.PlayAnimation("amb@world_human_bum_slumped@male@laying_on_left_side@idle_a", "idle_a", 8f, -8f, -1, (AnimationFlags)49, 0);
                await BaseScript.Delay(3000);
                await victim.Task.PlayAnimation("move_strafe@injured", "idle", 8f, -8f, -1, (AnimationFlags)49, 0);
                driver.Task.LeaveVehicle(vehicle, true);
                driver.Task.ReactAndFlee(player);
                Item Beer = new Item
                {
                    Name = "Beer",
                    IsIllegal = true
                };
                driver.SetData(data);
                    break;
            }
            int fireChance = rnd.Next(0, 10);
            if (fireChance == 0)
            {
                int boneIndex = GetEntityBoneIndexByName(vehicle.Handle, "engine");
                Vector3 enginePos = GetWorldPositionOfEntityBone(vehicle.Handle, boneIndex);
                StartScriptFire(enginePos.X, enginePos.Y, enginePos.Z, 32, true);
            }
        }

        public override void OnCancelBefore()
        {
            driver?.AttachedBlip?.Delete();
            driver?.MarkAsNoLongerNeeded();
            driver?.Task.WanderAround();
            driver?.Delete();
            vehicle?.Delete();
            victim?.AttachedBlip?.Delete();
            victim?.MarkAsNoLongerNeeded();
            victim?.Delete();
            base.OnCancelBefore();
        }
        VehicleHash[] acceptableVehicles =
{
            VehicleHash.Adder,
            VehicleHash.Alpha,
            VehicleHash.Ardent,
            VehicleHash.Asea2,
            VehicleHash.Asea,
            VehicleHash.Asterope,
            VehicleHash.Baller,
            VehicleHash.Baller2,
            VehicleHash.Baller3,
            VehicleHash.Baller4,
            VehicleHash.Baller5,
            VehicleHash.Baller6,
            VehicleHash.Banshee,
            VehicleHash.Banshee2,
            VehicleHash.BestiaGTS,
            VehicleHash.BfInjection,
            VehicleHash.Bison,
            VehicleHash.Bison2,
            VehicleHash.Bison3,
            VehicleHash.BJXL,
            VehicleHash.Blade,
            VehicleHash.Blista,
            VehicleHash.Blista2,
            VehicleHash.Blista3,
            VehicleHash.BobcatXL,
            VehicleHash.Bodhi2,
            VehicleHash.Boxville,
            VehicleHash.Boxville2,
            VehicleHash.Boxville3,
            VehicleHash.Boxville4,
            VehicleHash.Boxville5,
            VehicleHash.Brawler,
            VehicleHash.Brioso,
            VehicleHash.BType,
            VehicleHash.BType2,
            VehicleHash.BType3,
            VehicleHash.Buccaneer,
            VehicleHash.Buccaneer2,
            VehicleHash.Buffalo,
            VehicleHash.Buffalo2,
            VehicleHash.Buffalo3,
            VehicleHash.Bullet,
            VehicleHash.Burrito,
            VehicleHash.Burrito2,
            VehicleHash.Burrito3,
            VehicleHash.Burrito4,
            VehicleHash.Burrito5,
            VehicleHash.Camper,
            VehicleHash.Carbonizzare,
            VehicleHash.Casco,
            VehicleHash.Cavalcade,
            VehicleHash.Cavalcade2,
            VehicleHash.Cheetah,
            VehicleHash.Cheetah2,
            VehicleHash.Chino,
            VehicleHash.Chino2,
            VehicleHash.Cog55,
            VehicleHash.Cog552,
            VehicleHash.CogCabrio,
            VehicleHash.Cognoscenti,
            VehicleHash.Cognoscenti2,
            VehicleHash.Comet2,
            VehicleHash.Comet3,
            VehicleHash.Contender,
            VehicleHash.Coquette,
            VehicleHash.Coquette2,
            VehicleHash.Coquette3,
            VehicleHash.Dilettante2,
            VehicleHash.DLoader,
            VehicleHash.Dominator,
            VehicleHash.Dominator2,
            VehicleHash.Dubsta,
            VehicleHash.Dubsta2,
            VehicleHash.Dubsta3,
            VehicleHash.Dukes,
            VehicleHash.Dukes2,
            VehicleHash.Elegy,
            VehicleHash.Elegy2,
            VehicleHash.Emperor,
            VehicleHash.Emperor2,
            VehicleHash.Emperor3,
            VehicleHash.EntityXF,
            VehicleHash.Exemplar,
            VehicleHash.F620,
            VehicleHash.Faction,
            VehicleHash.Faction2,
            VehicleHash.Faction3,
            VehicleHash.Felon,
            VehicleHash.Felon2,
            VehicleHash.Feltzer2,
            VehicleHash.Feltzer3,
            VehicleHash.FMJ,
            VehicleHash.Forklift,
            VehicleHash.FQ2,
            VehicleHash.Fugitive,
            VehicleHash.Furoregt,
            VehicleHash.Futo,
            VehicleHash.Gauntlet,
            VehicleHash.Gauntlet2,
            VehicleHash.GBurrito2,
            VehicleHash.Glendale,
            VehicleHash.GP1,
            VehicleHash.Granger,
            VehicleHash.Gresley,
            VehicleHash.Habanero,
            VehicleHash.Hotknife,
            VehicleHash.Huntley,
            VehicleHash.Infernus,
            VehicleHash.Infernus2,
            VehicleHash.Ingot,
            VehicleHash.Intruder,
            VehicleHash.Issi2,
            VehicleHash.ItaliGTB,
            VehicleHash.ItaliGTB2,
            VehicleHash.Jackal,
            VehicleHash.JB700,
            VehicleHash.Jester,
            VehicleHash.Jester2,
            VehicleHash.Journey,
            VehicleHash.Kalahari,
            VehicleHash.Khamelion,
            VehicleHash.Kuruma,
            VehicleHash.Kuruma2,
            VehicleHash.Landstalker,
            VehicleHash.LE7B,
            VehicleHash.Limo2,
            VehicleHash.Lurcher,
            VehicleHash.Lynx,
            VehicleHash.Mamba,
            VehicleHash.Manana,
            VehicleHash.Marshall,
            VehicleHash.Massacro,
            VehicleHash.Massacro2,
            VehicleHash.Mesa,
            VehicleHash.Mesa2,
            VehicleHash.Mesa3,
            VehicleHash.Minivan,
            VehicleHash.Minivan2,
            VehicleHash.Monroe,
            VehicleHash.Moonbeam,
            VehicleHash.Moonbeam2,
            VehicleHash.Nero,
            VehicleHash.Nero2,
            VehicleHash.Nightshade,
            VehicleHash.Ninef,
            VehicleHash.Ninef2,
            VehicleHash.Omnis,
            VehicleHash.Oracle,
            VehicleHash.Oracle2,
            VehicleHash.Osiris,
            VehicleHash.Panto,
            VehicleHash.Paradise,
            VehicleHash.Patriot,
            VehicleHash.Penetrator,
            VehicleHash.Penumbra,
            VehicleHash.Peyote,
            VehicleHash.Pfister811,
            VehicleHash.Phoenix,
            VehicleHash.Picador,
            VehicleHash.Pigalle,
            VehicleHash.Pony,
            VehicleHash.Pony2,
            VehicleHash.Prairie,
            VehicleHash.Premier,
            VehicleHash.Primo,
            VehicleHash.Primo2,
            VehicleHash.Prototipo,
            VehicleHash.Radi,
            VehicleHash.RancherXL,
            VehicleHash.RancherXL2,
            VehicleHash.RapidGT,
            VehicleHash.RapidGT2,
            VehicleHash.Raptor,
            VehicleHash.RatLoader,
            VehicleHash.RatLoader2,
            VehicleHash.Reaper,
            VehicleHash.Rebel,
            VehicleHash.Rebel2,
            VehicleHash.Regina,
            VehicleHash.Rhapsody,
            VehicleHash.Rocoto,
            VehicleHash.Romero,
            VehicleHash.Ruiner,
            VehicleHash.Ruiner2,
            VehicleHash.Ruiner3,
            VehicleHash.Rumpo,
            VehicleHash.Rumpo2,
            VehicleHash.Rumpo3,
            VehicleHash.Ruston,
            VehicleHash.SabreGT,
            VehicleHash.SabreGT2,
            VehicleHash.Sadler,
            VehicleHash.Sadler2,
            VehicleHash.Sandking,
            VehicleHash.Sandking2,
            VehicleHash.Schafter2,
            VehicleHash.Schafter3,
            VehicleHash.Schafter4,
            VehicleHash.Schafter5,
            VehicleHash.Schafter6,
            VehicleHash.Schwarzer,
            VehicleHash.Seminole,
            VehicleHash.Sentinel,
            VehicleHash.Sentinel2,
            VehicleHash.Serrano,
            VehicleHash.Seven70,
            VehicleHash.Sheava,
            VehicleHash.SlamVan,
            VehicleHash.SlamVan2,
            VehicleHash.SlamVan3,
            VehicleHash.Specter,
            VehicleHash.Specter2,
            VehicleHash.Speedo,
            VehicleHash.Speedo2,
            VehicleHash.Stalion,
            VehicleHash.Stalion2,
            VehicleHash.Stanier,
            VehicleHash.Stinger,
            VehicleHash.StingerGT,
            VehicleHash.Stratum,
            VehicleHash.Stretch,
            VehicleHash.Sultan,
            VehicleHash.SultanRS,
            VehicleHash.Superd,
            VehicleHash.Surano,
            VehicleHash.Surfer,
            VehicleHash.Surfer2,
            VehicleHash.Surge,
            VehicleHash.T20,
            VehicleHash.Taco,
            VehicleHash.Tailgater,
            VehicleHash.Tampa,
            VehicleHash.Tampa2,
            VehicleHash.Tampa3,
            VehicleHash.Tempesta,
            VehicleHash.Torero,
            VehicleHash.Tornado,
            VehicleHash.Tornado2,
            VehicleHash.Tornado3,
            VehicleHash.Tornado4,
            VehicleHash.Tornado5,
            VehicleHash.Tornado6,
            VehicleHash.Toro,
            VehicleHash.Turismo2,
            VehicleHash.Turismor,
            VehicleHash.Tyrus,
            VehicleHash.Vacca,
            VehicleHash.Vagner,
            VehicleHash.Verlierer2,
            VehicleHash.Vigero,
            VehicleHash.Voltic,
            VehicleHash.Voltic2,
            VehicleHash.Voodoo,
            VehicleHash.Voodoo2,
            VehicleHash.Warrener,
            VehicleHash.Washington,
            VehicleHash.Windsor,
            VehicleHash.Windsor2,
            VehicleHash.XA21,
            VehicleHash.XLS,
            VehicleHash.XLS2,
            VehicleHash.Youga,
            VehicleHash.Youga2,
            VehicleHash.Zentorno,
            VehicleHash.Zion,
            VehicleHash.Zion2,
            VehicleHash.ZType
        };
    }
}
