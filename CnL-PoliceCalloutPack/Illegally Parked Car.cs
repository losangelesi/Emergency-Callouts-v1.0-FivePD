using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using FivePD.API;
using FivePD.API.Utils;
using static CitizenFX.Core.Native.API;

namespace Callouts
{
    [CalloutProperties("Illegally Parked Vehicle", "LosAngelesi", "1.1")]
    public class Illegally_Parked_Car : FivePD.API.Callout
    {
        private readonly Random rnd = new Random();
        private Vehicle vehicle;
        private Ped thief;

        public Illegally_Parked_Car()
        {
            int distance = rnd.Next(600, 850);
            float offsetX = rnd.Next(-1 * distance, distance);
            float offsetY = rnd.Next(-1 * distance, distance);

            InitInfo(World.GetNextPositionOnStreet(Game.PlayerPed.GetOffsetPosition(new Vector3(offsetX, offsetY, 0))));
            ShortName = "Illegally Parked Car";
            CalloutDescription = "Reports of a vehicle parked illegally in the road, respond Code 1";
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
            Random random = new Random();
            VehicleHash selectedVehicleHash = acceptableVehicles[random.Next(acceptableVehicles.Length)];
            vehicle = await SpawnVehicle(selectedVehicleHash, Location);
            vehicle.AttachBlip();
            vehicle.AttachedBlip.Sprite = (BlipSprite)523;
            ShowNetworkedNotification("On Scene", "CHAR_CALL911", "CHAR_CALL911", "Dispatch", "MDT UPDATE:", 10f);

            // Add chances for the vehicle to be damaged or tires popped
            if (random.Next(100) < 30) // 30% chance the vehicle will be damaged
            {
                vehicle.EngineHealth = random.Next(300, 500); // Damage the engine
            }
            if (random.Next(100) < 20) // 20% chance a tire will be popped
            {
                vehicle.Wheels[random.Next(vehicle.Wheels.Count)].Burst(); // Pop a random tire
            }

            // Add a small chance of a ped to spawn nearby and steal the vehicle
            if (random.Next(100) < 5) // 5% chance a ped will spawn and steal the vehicle
            {
                thief = await SpawnPed(RandomUtils.GetRandomPed(), Location.Around(4f));
                thief.Task.EnterVehicle(vehicle, VehicleSeat.Driver);
                thief.Task.CruiseWithVehicle(vehicle, 20f, (int)DrivingStyle.IgnoreLights);
            }
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
        public override void OnCancelBefore()
        {
            vehicle?.Delete();
            base.OnCancelBefore();
        }
    }
}
