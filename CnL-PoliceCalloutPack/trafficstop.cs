using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FivePD.API;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace EmergencyCallouts
{
    [CalloutProperties("Traffic Stop", "LosAngelesi", "1.1")]
    public class TrafficStopScenario : Callout
    {
        private Vehicle vehicle;
        private Ped driver;
        private int calloutPath;
        private string pedTitle;
        private bool onScene = false, reportTaken = false, suspectArrested = false;
        private readonly Random rnd = new Random();

        public TrafficStopScenario()
        {
            int distance = rnd.Next(300, 750);
            float offsetX = rnd.Next(-1 * distance, distance);
            float offsetY = rnd.Next(-1 * distance, distance);
            InitInfo(World.GetNextPositionOnStreet(Game.PlayerPed.GetOffsetPosition(new Vector3(offsetX, offsetY, 0))));
            ShortName = "Vehicle speeding";
            CalloutDescription = "Possible vehicle speeding, respond Code 1.";
            ResponseCode = 1;
            StartDistance = 200f;
        }

        public override async Task OnAccept()
        {
            InitBlip();
            UpdateData();
        }

        public override async void OnStart(Ped closest)
        {
            Random random = new Random();
            VehicleHash selectedVehicleHash = acceptableVehicles[random.Next(acceptableVehicles.Length)];
            vehicle = await SpawnVehicle(selectedVehicleHash, Location);
            driver = await SpawnPed(PedHash.Business01AFY, Location);
            driver.SetIntoVehicle(vehicle, VehicleSeat.Driver);
            ShowNetworkedNotification("On Scene", "CHAR_CALL911", "CHAR_CALL911", "Dispatch", "MDT UPDATE:", 10f);

            List<TrafficStopOutcome> outcomes = new List<TrafficStopOutcome>
            {
                new TrafficStopOutcome
                {
                    Description = "Speeding 10-15 mph over the limit",
                    Fine = 100,
                    Points = 2,
                    Chance = 40,
                    SpeedOverLimit = 15,
                },
                new TrafficStopOutcome
                {
                    Description = "Speeding 16-25 mph over the limit",
                    Fine = 200,
                    Points = 3,
                    Chance = 30,
                    SpeedOverLimit = 25,
                },
                // Add more outcomes
            };
            int randomNumber = random.Next(1, 101);

            TrafficStopOutcome selectedOutcome = null;

            int currentChance = 0;
            foreach (TrafficStopOutcome outcome in outcomes)
            {
                currentChance += outcome.Chance;
                if (randomNumber <= currentChance)
                {
                    selectedOutcome = outcome;
                    break;
                }
            }

            if (selectedOutcome != null)
            {
                int speedLimit = GetSpeedLimit(driver);
                driver.Task.CruiseWithVehicle(vehicle, speedLimit + selectedOutcome.SpeedOverLimit, (int)DrivingStyle.Rushed);

                var args = new Dictionary<string, object>
                {
                    ["color"] = new[] { 255, 255, 0 },
                    ["multiline"] = true,
                    ["args"] = new[] { "[Traffic Stop]", "Outcome: " + selectedOutcome.Description }
                };
                BaseScript.TriggerEvent("chat:addMessage", args);
            }
            else
            {
                var args = new Dictionary<string, object>
                {
                    ["color"] = new[] { 255, 0, 0 },
                    ["multiline"] = true,
                    ["args"] = new[] { "[Traffic Stop]", "No outcome selected." }
                };
                BaseScript.TriggerEvent("chat:addMessage", args);
            }
        }

        public class TrafficStopOutcome
        {
            public string Description { get; set; }
            public int Fine { get; set; }
            public int Points { get; set; }
            public int Chance { get; set; }
            public int SpeedOverLimit { get; set; }
        }



        private int GetSpeedLimit(Ped driver)
        {
            // Get the driver's position
            Vector3 driverPosition = driver.Position;

            // Get the street name based on the driver's position
            string streetName = GetStreetName(driverPosition);

            int speedLimit;

            // Check if the street name contains "Joshua" and set the speed limit accordingly
            if (streetName.ToLower().Contains("joshua"))
            {
                speedLimit = 50; // Set the speed limit for Joshua Road
            }
            if (streetName.ToLower().Contains("route"))
            {
                speedLimit = 65; // Set the speed limit for route 68
            }
            if (streetName.ToLower().Contains("freeway"))
            {
                speedLimit = 70; // Set the speed limit for route 68
            }
            else
            {
                speedLimit = 55; // Default speed limit for other roads
            }

            return speedLimit;
        }

        public override void OnCancelBefore()
        {
            base.OnCancelBefore();
            if (vehicle.Exists()) vehicle.Delete();
            if (driver.Exists()) driver.Delete();
        }
        //The RP greets the player when they first approach them.
        private async Task greetingRangeCheck()
        {
            String[] greetings = new string[]
            {
                "~b~RP: ~w~Hello officer..",
                "~b~RP: ~w~Whats up officer",
            };
            if (Game.PlayerPed.IsInRangeOf(driver.Position, 10f))
            {
                this.ShowDialog(greetings[rnd.Next(0, greetings.Length)], 5000, 10f);

                driver.Task.ChatTo(Game.PlayerPed);

                Tick -= new Func<Task>(greetingRangeCheck);
            }
        }
        //Checks to see if the RP is dead or not before the report is taken. If so, end the callout.
        private async Task rpDeathChecker()
        {
            if (API.IsPedDeadOrDying(driver.Handle, true))
            {
                Tick -= new Func<Task>(checkYKey);
                Tick -= new Func<Task>(reportRangeCheck);
                Tick -= new Func<Task>(rpDeathChecker);
                ShowNetworkedNotification("RP is dead. Returning to service.", "CHAR_BLOCKED", "CHAR_BLOCKED", "~y~Callout Failed!", "", 5f);
                base.EndCallout();
            }
        }
        public class Report
        {
            public string OfficerName { get; set; }
            public string SuspectName { get; set; }
            public string VehicleDescription { get; set; }
            public string IncidentDetails { get; set; }
            public DateTime TimeOfReport { get; set; }
            // You can add more properties depending on what information you want to include in the report
        }
        public async Task<Report> TakeReport()
        {
            // You may want to replace these values with actual data from the game
            string officerName = "Officer Name"; // replace with actual officer name
            string vehicleModelName = Function.Call<string>(Hash.GET_DISPLAY_NAME_FROM_VEHICLE_MODEL, vehicle.Model.Hash);
            string incidentDetails = "Speeding 10-15 mph over the limit"; // replace with actual incident details

            Report report = new Report()
            {
                OfficerName = officerName,
                VehicleDescription = vehicleModelName,
                IncidentDetails = incidentDetails,
                TimeOfReport = DateTime.Now
            };

            string reportNotification = $"Report taken by {officerName}.\nVehicle: {vehicleModelName}\nDetails: {incidentDetails}";
            ShowNetworkedNotification(reportNotification, "CHAR_HUMANDEFAULT", "CHAR_HUMANDEFAULT", "Callout Progress", "Objective Complete", 5f);

            return report;
        }

        //Checks to see if we are in range of the RP to take a report. If so, display a help dialog telling the user to press Y
        private async Task reportRangeCheck()
        {
            if (Game.PlayerPed.IsInRangeOf(driver.Position, 2f))
            {
                drawHelpText("Press ~INPUT_MP_TEXT_CHAT_TEAM~ to take a report from the RP.");
                Tick -= new Func<Task>(reportRangeCheck);
                await BaseScript.Delay(30000);
                if (!reportTaken)
                    Tick += new Func<Task>(reportRangeCheck);
            }
        }
        //Check for Y key to be pressed while within close range of the RP. Take the report and move on with the callout
        private async Task checkYKey()
        {
            if (Game.IsControlJustReleased(0, Control.MpTextChatTeam) && Game.PlayerPed.IsInRangeOf(driver.Position, 2f) && !API.IsPedDeadOrDying(driver.Handle, true)) // is Y selected and are you close to RP?
            {
                Tick -= new Func<Task>(checkYKey);
                Tick -= new Func<Task>(reportRangeCheck);

                // Take the report
                Report report = await TakeReport();

                reportTaken = true;
                driver.AttachedBlip.Delete();
                Tick -= new Func<Task>(rpDeathChecker);
            }
            return;
        }
        private async void startPedSearch()
        {
            int distance = rnd.Next(600, 1000);
            float offsetX = rnd.Next(-1 * distance, distance);
            float offsetY = rnd.Next(-1 * distance, distance);

            this.Location = World.GetNextPositionOnStreet(Game.PlayerPed.GetOffsetPosition(new Vector3(offsetX, offsetY, 0)));
            UpdateData();
        }
        private void drawHelpText(string message)
        {
            CitizenFX.Core.Native.API.BeginTextCommandDisplayHelp("STRING");
            CitizenFX.Core.Native.API.AddTextComponentSubstringPlayerName(message);
            CitizenFX.Core.Native.API.EndTextCommandDisplayHelp(0, false, true, -1);
        }
        private string GetStreetName(Vector3 position)
        {
            var streetHash = World.GetStreetName(position);
            return Function.Call<string>(Hash.GET_STREET_NAME_FROM_HASH_KEY, streetHash);
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