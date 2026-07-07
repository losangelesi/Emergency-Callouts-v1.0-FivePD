using System;
using System.Threading.Tasks;
using System.Drawing;
using CitizenFX.Core;
using FivePD.API;
using FivePD.API.Utils;
using CitizenFX.Core.Native;

namespace EmergencyCallouts
{
    [CalloutProperties("Traffic Collision", "Losangelesi", "1.1")]
    public class TrafficCollision : Callout
    {
        Ped driver1, driver2;
        Vehicle vehicle1, vehicle2;
        Random rnd = new Random();

        public TrafficCollision()
        {
            int distance = rnd.Next(500, 850);
            float offsetX = rnd.Next(-1 * distance, distance);
            float offsetY = rnd.Next(-1 * distance, distance);
            InitInfo(World.GetNextPositionOnStreet(Game.PlayerPed.GetOffsetPosition(new Vector3(offsetX, offsetY, 0))));
            ShortName = "Traffic Collision";
            CalloutDescription = "A traffic collision has been reported. Respond Code 3.";
            ResponseCode = 3;
            StartDistance = 250f;
        }

        public override async Task OnAccept()
        {
            InitBlip();
            UpdateData();


            VehicleHash vehicleHash1 = acceptableVehicles[rnd.Next(acceptableVehicles.Length)];
            VehicleHash vehicleHash2 = acceptableVehicles[rnd.Next(acceptableVehicles.Length)];
            vehicle1 = await World.CreateVehicle(vehicleHash1, Location + new Vector3(0, 5f, 0));
            vehicle2 = await World.CreateVehicle(vehicleHash2, Location + new Vector3(0, -7f, 0));
            driver1 = await SpawnPed(RandomUtils.GetRandomPed(), Location + new Vector3(0, 5f, 0));
            driver2 = await SpawnPed(RandomUtils.GetRandomPed(), Location + new Vector3(0, -5f, 0));
            driver1.SetIntoVehicle(vehicle1, VehicleSeat.Driver);
            driver2.SetIntoVehicle(vehicle2, VehicleSeat.Driver);

            // Simulate a traffic collision by damaging both vehicles
            Random random = new Random();
            int engineDamage1 = random.Next(100, 800);
            int bodyDamage1 = random.Next(200, 1000);
            int engineDamage2 = random.Next(100, 800);
            int bodyDamage2 = random.Next(200, 1000);

            vehicle1.EngineHealth -= engineDamage1;
            vehicle1.BodyHealth -= bodyDamage1;
            vehicle2.EngineHealth -= engineDamage2;
            vehicle2.BodyHealth -= bodyDamage2;
        }

        public override void OnStart(Ped player)
        {
            base.OnStart(player);

            int outcome = rnd.Next(1, 12); // 1 to 11, with 11 being the vehicle fire scenario
            ShowNetworkedNotification("On Scene", "CHAR_CALL911", "CHAR_CALL911", "Dispatch", "MDT UPDATE:", 10f);

            if (outcome == 11)
            {
                vehicle1.EngineHealth = rnd.Next(-1000, -1);
                vehicle1.BodyHealth = rnd.Next(-1000, -1);
                vehicle2.EngineHealth = rnd.Next(-1000, -1);
                vehicle2.BodyHealth = rnd.Next(-1000, -1);
                Function.Call(Hash.SET_VEHICLE_ENGINE_ON, vehicle1, false, true, true);
                vehicle1.IsDriveable = false;
                driver1.Task.PlayAnimation("amb@world_human_bum_slumped@male@laying_on_right_side@base", "base", 8f, -1, -1, (AnimationFlags)49, 0);
                driver2.Task.PlayAnimation("amb@world_human_bum_slumped@male@laying_on_left_side@base", "base", 8f, -1, -1, (AnimationFlags)49, 0);
            }
            else
            {
                vehicle1.EngineHealth = rnd.Next(-1000, -1);
                vehicle1.BodyHealth = rnd.Next(-1000, -1);
                vehicle2.EngineHealth = rnd.Next(-1000, -1);
                vehicle2.BodyHealth = rnd.Next(-1000, -1);
                switch (outcome)
                {
                    // Cases 1-10 with different animations for the peds
                    case 1:
                        driver1.Task.PlayAnimation("amb@world_human_bum_slumped@male@laying_on_right_side@base", "base", 8f, -1, -1, (AnimationFlags)49, 0);
                        driver2.Task.PlayAnimation("amb@world_human_bum_slumped@male@laying_on_left_side@base", "base", 8f, -1, -1, (AnimationFlags)49, 0);
                        break;
                    case 2:
                        driver1.Task.PlayAnimation("amb@world_human_bum_slumped@male@laying_on_right_side@base", "base", 8f, -1, -1, (AnimationFlags)49, 0);
                        driver2.Task.PlayAnimation("anim@mp_player_intcelebrationmale@finger", "finger", 8f, -1, -1, (AnimationFlags)49, 0);
                        break;
                    case 3:
                        driver1.Task.PlayAnimation("misscarsteal4@actor", "actor_berating_loop", 8f, -1, -1, (AnimationFlags)49, 0);
                        driver2.Task.PlayAnimation("misscarsteal4@actor", "actor_berating_loop", 8f, -1, -1, (AnimationFlags)49, 0);
                        break;
                    case 4:
                        driver1.Task.PlayAnimation("amb@medic@standing@tendtodead@base", "base", 8f, -1, -1, (AnimationFlags)49, 0);
                        driver2.Task.PlayAnimation("amb@world_human_bum_slumped@male@laying_on_left_side@base", "base", 8f, -1, -1, (AnimationFlags)49, 0);
                        break;
                    case 5:
                        driver1.Task.PlayAnimation("amb@world_human_bum_slumped@male@laying_on_right_side@base", "base", 8f, -1, -1, (AnimationFlags)49, 0);
                        driver2.Task.PlayAnimation("amb@world_human_bum_slumped@male@laying_on_left_side@base", "base", 8f, -1, -1, (AnimationFlags)49, 0);
                        break;
                    case 6:
                        driver1.Delete();
                        driver2.Task.PlayAnimation("amb@world_human_bum_slumped@male@laying_on_left_side@base", "base", 8f, -1, -1, (AnimationFlags)49, 0);
                        break;
                    case 7:
                        driver1.Task.PlayAnimation("amb@world_human_vehicle_mechanic@male@base", "base", 8f, -1, -1, (AnimationFlags)49, 0);
                        driver2.Task.PlayAnimation("amb@world_human_vehicle_mechanic@female@base", "base", 8f, -1, -1, (AnimationFlags)49, 0);
                        break;
                    case 8:
                        driver1.Task.PlayAnimation("amb@world_human_paparazzi@male@base", "base", 8f, -1, -1, (AnimationFlags)49, 0);
                        driver2.Task.PlayAnimation("amb@world_human_standing_mobile@male@standing_mobile_base", "standing_mobile_base", 8f, -1, -1, (AnimationFlags)49, 0);
                        break;
                    case 9:
                        driver1.Task.PlayAnimation("amb@world_human_hang_out_street@male_b@idle_a", "idle_a", 8f, -1, -1, (AnimationFlags)49, 0);
                        driver2.Task.PlayAnimation("amb@world_human_hang_out_street@female_arms_crossed@idle_a", "idle_a", 8f, -1, -1, (AnimationFlags)49, 0);
                        break;
                    case 10:
                        driver1.Task.PlayAnimation("amb@world_human_mobile_film_shocking@female@base", "base", 8f, -1, -1, (AnimationFlags)49, 0);
                        driver2.Task.PlayAnimation("amb@world_human_standing_mobile@female@standing_mobile_base", "standing_mobile_base", 8f, -1, -1, (AnimationFlags)49, 0);
                        break;
                }




            }
        }
        // Spawn vehicles and drivers
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
        public override void OnCancelBefore()
        {
            driver1?.AttachedBlip?.Delete();
            driver1?.MarkAsNoLongerNeeded();
            driver1?.Task.WanderAround();
            driver1?.Delete();
            driver2?.AttachedBlip?.Delete();
            driver2?.MarkAsNoLongerNeeded();
            driver2?.Task.WanderAround();
            driver2?.Delete();
            vehicle1?.Delete();
            vehicle2?.Delete();
            base.OnCancelBefore();
        }
    }
}
