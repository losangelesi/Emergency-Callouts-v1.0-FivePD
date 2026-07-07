using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using FivePD.API;
using FivePD.API.Utils;

namespace SilverAlert
{
    [CalloutProperties("Silver Alert", "Chris07, losangelesi", "1.0")]
    public class SilverAlert : FivePD.API.Callout
    {
        private readonly Random rnd = new Random();
        private Ped ped, rp;
        private bool pedSpawned = false, pedFound = false, reportTaken = false;
        private Blip searchArea, blip;
        private string pedTitle;
        private SilverAlertLocation CalloutLocation = new SilverAlertLocation();


        public SilverAlert()
        {

            InitInfo(CalloutLocation.getRPLocation());

            ShortName = "Silver Alert";
            CalloutDescription = "We recieved report of an Elderly person missing. Respond Code 2.";
            ResponseCode = 2;
            StartDistance = 65f;

            BaseScript.TriggerEvent("FivePDAudio::RegisterCallout", new object[]
           {
                this.ShortName,
                @"CRIMES/CRIME_CIVILIAN_NEEDING_ASSISTANCE_01.ogg"
           });
        }

        public override async Task OnAccept()
        {
            pedTitle = (rnd.Next() % 2 == 0) ? "Grandma" : "Grandpa";
            InitBlip();
            UpdateData();
            ShowNetworkedNotification("Elderly person missing, Please see RP at the address provided.", "CHAR_CALL911", "CHAR_CALL911", "Dispatch", "MDT UPDATE:", 10f);
        }
        public async override void OnStart(Ped player)
        {
            base.OnStart(player);

            rp = await SpawnPed(getAcceptableCaretakerPed(), CalloutLocation.getRPLocation());
            rp.Heading = CalloutLocation.getRPHeading();
            rp.AttachBlip();
            rp.AttachedBlip.Sprite = (BlipSprite)465;
            rp.AttachedBlip.Color = (BlipColor)69;
            rp.BlockPermanentEvents = true;
            rp.AlwaysKeepTask = true;
            rp.CanBeTargetted = false;

            ShowNetworkedNotification("On Scene.", "CHAR_CALL911", "CHAR_CALL911", "Dispatch", "MDT UPDATE:", 5f);

            //Take a report. Start checker for help text and Y button press
            Tick += new Func<Task>(reportRangeCheck);
            Tick += new Func<Task>(checkYKey);
            Tick += new Func<Task>(rpDeathChecker);
            Tick += new Func<Task>(greetingRangeCheck); 
        }

        //Function for the actual search for the elderly ped
        private async void startPedSearch()
        {
            int distance = rnd.Next(600, 1000);
            float offsetX = rnd.Next(-1 * distance, distance);
            float offsetY = rnd.Next(-1 * distance, distance);

            this.Location = World.GetNextPositionOnStreet(Game.PlayerPed.GetOffsetPosition(new Vector3(offsetX, offsetY, 0)));
            UpdateData();
            removeBlip();
            InitSearchBlip();
            API.SetBlipRoute(searchArea.Handle, true);
            Tick += new Func<Task>(spawnPedChecker);

        }

        //Function for the drop off portion of the callout
        private void startDropOff()
        {
            ShowNetworkedNotification("Enroute to return subject back to RP.", "CHAR_CALL911", "CHAR_CALL911", "Dispatch", "MDT UPDATE:", 10f);
            this.Location = CalloutLocation.getDropOffLocation();
            UpdateData();
            InitBlip();
            API.SetBlipRoute(blip.Handle, true);
            Tick += new Func<Task>(dropOffRangeCheck);
        }

        //Check if within search area and then spawn ped.
        private async Task spawnPedChecker()
        {
            if (Game.PlayerPed.IsInRangeOf(this.Location, 200f))
            {
                if(!pedSpawned)
                {
                    Tick -= new Func<Task>(spawnPedChecker);
                    pedSpawned = true;
                    ped = await SpawnPed(getAcceptableElderlyPed(), World.GetNextPositionOnSidewalk(Location));
                    ped.BlockPermanentEvents = true;
                    ped.AlwaysKeepTask = true;
                    ped.Task.WanderAround();
                    API.SetBlipRoute(blip.Handle, false);
                    await BaseScript.Delay(rnd.Next(15, 20) * 1000); // Narrow search range after set time period
                    if (!pedFound)
                    {
                        ShowNetworkedNotification("Subject spotted. Updated location information sent to MDT.", "CHAR_CALL911", "CHAR_CALL911", "Dispatch", "MDT UPDATE:", 10f);
                        UpdateSearchBlip();
                    }
                    ShowNetworkedNotification("Leaving RP and heading to search area.", "CHAR_CALL911", "CHAR_CALL911", "Dispatch", "MDT UPDATE:", 10f);
                    Tick += new Func<Task>(spotPedRangeCheck);                    
                }
            }
            
        }

        //The RP greets the player when they first approach them.
        private async Task greetingRangeCheck()
        {
            String[] greetings = new string[]
            {
                "~b~RP: ~w~Hello officer. Thank you for coming.",
                "~b~RP: ~w~Thank you so much for coming officer!",
                "~b~RP: ~w~I'm sorry about all this. "+pedTitle+" ran off again.",
                "~b~RP: ~w~Thank goodness, officer. "+pedTitle+" is missing!"
            };
            if (Game.PlayerPed.IsInRangeOf(rp.Position, 10f))
            {
                this.ShowDialog(greetings[rnd.Next(0, greetings.Length)], 5000, 10f);

                rp.Task.ChatTo(Game.PlayerPed);

                Tick -= new Func<Task>(greetingRangeCheck);
            }
        }

        //Checks to see if the RP is dead or not before the report is taken. If so, end the callout.
        private async Task rpDeathChecker()
        {
            if (API.IsPedDeadOrDying(rp.Handle, true))
            {
                Tick -= new Func<Task>(checkYKey);
                Tick -= new Func<Task>(reportRangeCheck);
                Tick -= new Func<Task>(rpDeathChecker);
                ShowNetworkedNotification("RP is dead. Returning to service.", "CHAR_BLOCKED", "CHAR_BLOCKED", "~y~Callout Failed!", "", 5f);
                base.EndCallout();
            }
        }

        //Checks to see if we are in range of the RP to take a report. If so, display a help dialog telling the user to press Y
        private async Task reportRangeCheck()
        {
            if (Game.PlayerPed.IsInRangeOf(rp.Position, 2f))
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
            if (Game.IsControlJustReleased(0, Control.MpTextChatTeam) && Game.PlayerPed.IsInRangeOf(rp.Position, 2f) && !API.IsPedDeadOrDying(rp.Handle, true)) // is Y selected and are you close to RP?
            {
                Tick -= new Func<Task>(checkYKey);
                Tick -= new Func<Task>(reportRangeCheck);
                int pencilHash = CitizenFX.Core.Native.API.GetHashKey("prop_pencil_01");
                Vector3 handPos = Function.Call<Vector3>(Hash.GET_PED_BONE_COORDS, Game.PlayerPed, 58866, 0f, 0f, 0f);
                API.TaskStartScenarioInPlace(Game.PlayerPed.Handle, "CODE_HUMAN_MEDIC_TIME_OF_DEATH", 0, true);
                int pencilProp = API.CreateObject(pencilHash, handPos.X, handPos.Y, handPos.Z, true, true, false);
                Function.Call(Hash.ATTACH_ENTITY_TO_ENTITY, pencilProp, Game.PlayerPed, Function.Call<int>(Hash.GET_PED_BONE_INDEX, Game.PlayerPed, 58866), 0.11, -0.02, 0.001, -120.0, 0.0, 0.0, true, true, false, true, 1, true);

                await BaseScript.Delay(20000);
                API.ClearPedTasks(Game.PlayerPed.Handle);
                API.DeleteObject(ref pencilProp);
                
                reportTaken = true;
                rp.AttachedBlip.Delete();
                Tick -= new Func<Task>(rpDeathChecker);
                rp.MarkAsNoLongerNeeded();
                ShowNetworkedNotification("Report taken. RP gives you a good area to start searching.", "CHAR_HUMANDEFAULT", "CHAR_HUMANDEFAULT", "Callout Progress", "Objective Complete", 5f);
                startPedSearch();
            }
            return;
        }

        private async Task spotPedRangeCheck()
        {
            if (Game.PlayerPed.IsInRangeOf(ped.Position, 30f))
            {
                Tick -= new Func<Task>(spotPedRangeCheck);
                pedFound = true;
                searchArea.Delete();
                ped.AttachBlip();
                ped.AttachedBlip.Sprite = (BlipSprite)66;
                ped.AttachedBlip.Color = (BlipColor)20;
                this.ShowDialog("~y~*You see someone who fits the person's description*", 5000, 5f);
                Tick += new Func<Task>(pedRangeCheck);
            }
        }

        private async Task pedRangeCheck()
        {
            String[] greetings = new string[]
            {
                "~b~Subject: ~w~Hello officer. Lovely day isn't it?",
                "~b~Subject: ~w~Hello there.",
                "~b~Subject: ~w~Thank goodness! I need help. I'm lost.",
                "~b~Subject: ~w~Excuse me, do you know the way to the Ambassador Theater?",
                "~b~Subject: ~w~Excuse me...I'm a bit lost, can you help me?",
            };
            String[] fleePhrase = new string[]
            {
                "~b~Subject: ~w~Oh no! Stay away from me!",
                "~b~Subject: ~w~Stay away! HELP! SOMEONE HELP!",
                "~b~Subject: ~w~I'M NOT GOING BACK TO THE NURSING HOME!",
                "~b~Subject: ~w~Leave me alone!"
            };
            String[] giveupPhrase = new string[]
            {
                "~b~Subject: ~w~I give up! Please don't hurt me",
                "~b~Subject: ~y~*Cough* *wheeze* ~w~I...can't...run...anymore ~y~*cough*",
                "~b~Subject: ~y~*Out of breath* ~w~Okay, Okay, you win copper."
            };

            if (Game.PlayerPed.IsInRangeOf(ped.Position, 2f))
            {
                Tick -= new Func<Task>(pedRangeCheck);
                int chance = rnd.Next(0,15);

                if ( chance >= 0 && chance < 2 ) //Flee
                { 
                    ped.AttachedBlip.Sprite = (BlipSprite)1;
                    ped.AttachedBlip.Color = (BlipColor)21;
                    ped.Task.FleeFrom(Game.PlayerPed);
                    this.ShowDialog(fleePhrase[rnd.Next(0, fleePhrase.Length)], 5000, 5f); 
                    ped.Task.FleeFrom(Game.PlayerPed);
                    await BaseScript.Delay(rnd.Next(5, 30) * 1000);
                    ped.Task.HandsUp(-1);
                    this.ShowDialog(giveupPhrase[rnd.Next(0, giveupPhrase.Length)], 5000, 5f); 
                }
                else //Don't flee
                {
                    ped.AttachedBlip.Sprite = (BlipSprite)480; 
                    ped.AttachedBlip.Color = (BlipColor)20; 
                    this.ShowDialog(greetings[rnd.Next(0, greetings.Length)], 5000, 5f); 

                    ped.Task.ChatTo(Game.PlayerPed); 
                }
                Tick += new Func<Task>(pedInCarCheck); 
                ShowNetworkedNotification("Subject located. Now take them back home.", "CHAR_HUMANDEFAULT", "CHAR_HUMANDEFAULT", "Callout Progress", "~g~Objective Complete!", 5f); 
            }
        }

        private async Task pedInCarCheck()
        {
            if( API.IsPedSittingInAnyVehicle(Game.PlayerPed.Handle) ) //Is the player in a car?
            {
                int seats = API.GetVehicleMaxNumberOfPassengers(Game.PlayerPed.CurrentVehicle.Handle);
                int vehicle = Game.PlayerPed.CurrentVehicle.Handle;

                for(int i = 0; i < seats; i++ ) //Check all the seats in the player's vehicle and check for the elderly ped
                {
                    if( API.GetPedInVehicleSeat(vehicle, i) == ped.Handle )
                    {
                        Tick -= new Func<Task>(pedInCarCheck);
                        startDropOff();
                    }
                }

            }
        }

        private async Task dropOffRangeCheck()
        {
            if (Game.PlayerPed.IsInRangeOf(this.Location, 50f))
            {
                Tick -= new Func<Task>(dropOffRangeCheck);
                Tick += new Func<Task>(drawMarker);
                Tick += new Func<Task>(dropOffCheck);
            }
        }

        private async Task drawMarker()
        {
            API.DrawMarker(1, CalloutLocation.getDropOffLocation().X, CalloutLocation.getDropOffLocation().Y, CalloutLocation.getDropOffLocation().Z-2, 0, 0, 0, 0, 0, 0, 2, 2, 2, 0, 255, 255, 180, false, true, 3, false, null, null, false);
        }

        private async Task dropOffCheck()
        {
            if (Game.PlayerPed.IsInRangeOf(CalloutLocation.getDropOffLocation(), 3f))
            {
                Tick -= new Func<Task>(dropOffCheck);
                drawHelpText("Press ~INPUT_REPLAY_SCREENSHOT~ to drop off the subject.");
                Tick += new Func<Task>(checkUKey);
            }
        }

        private async Task checkUKey()
        {
            if (Game.IsControlJustReleased(0, Control.ReplayScreenshot) && Game.PlayerPed.IsInRangeOf(CalloutLocation.getDropOffLocation(),3f)) // is U pressed while near the drop point? If so, delete the ped and end the callout.
            {
                Tick -= new Func<Task>(checkUKey);
                ped.Delete();
                this.ShowDialog("RP: Thank you for your help, officer!", 5000, 10f);
                ShowNetworkedNotification("Callout Completed successfully!", "CHAR_HUMANDEFAULT", "CHAR_HUMANDEFAULT", "~b~Callout Complete!", "", 5f);
                base.EndCallout();
            }
        }

        public override void OnCancelBefore()
        {
            rp?.AttachedBlip?.Delete();
            rp?.MarkAsNoLongerNeeded();
            rp?.Task.WanderAround();
            rp?.Delete();
            ped?.AttachedBlip?.Delete();
            ped?.MarkAsNoLongerNeeded();
            ped?.Delete();
            blip?.Delete();
            searchArea?.Delete();
        }

        protected void InitSearchBlip(float circleRadius = 200f, BlipColor color = BlipColor.Blue, BlipSprite sprite = BlipSprite.BigCircle, int alpha = 100)
        {
            int offset = rnd.Next(1, 80);
            float offsetX = rnd.Next(-1 * offset, offset);
            float offsetY = rnd.Next(-1 * offset, offset);
            Vector3 loc = new Vector3(Location.X + offsetX, Location.Y + offsetY, Location.Z);

            searchArea = World.CreateBlip(loc, circleRadius);
            this.Radius = circleRadius;
            this.Marker = searchArea;
            this.Marker.Sprite = sprite;
            this.Marker.Color = color;
            this.Marker.Alpha = alpha;
        }

        protected void InitBlip(float circleRadius = 35f, BlipColor color = BlipColor.Blue, BlipSprite sprite = BlipSprite.BigCircle, int alpha = 100)
        {
            blip = World.CreateBlip(this.Location, circleRadius);
            this.Radius = circleRadius;
            this.Marker = blip;
            this.Marker.Sprite = sprite;
            this.Marker.Color = color;
            this.Marker.Alpha = alpha;
        }

        private void removeBlip()
        {
            blip.Delete();
        }

        private void UpdateSearchBlip(float circleRadius = 75f, BlipColor color = BlipColor.Blue, BlipSprite sprite = BlipSprite.BigCircle, int alpha = 100)
        {
            int offset = rnd.Next(1, 30);
            float offsetX = rnd.Next(-1 * offset, offset);
            float offsetY = rnd.Next(-1 * offset, offset);
            Vector3 updatedLoc = new Vector3(ped.Position.X + offsetX, ped.Position.Y + offsetY, ped.Position.Z);

            searchArea.Delete();
            searchArea = World.CreateBlip(updatedLoc, circleRadius);
            this.Radius = circleRadius;
            this.Marker = searchArea;
            this.Marker.Sprite = sprite;
            this.Marker.Color = color;
            this.Marker.Alpha = alpha;
        }

        private void drawHelpText(string message)
        {
            CitizenFX.Core.Native.API.BeginTextCommandDisplayHelp("STRING");
            CitizenFX.Core.Native.API.AddTextComponentSubstringPlayerName(message);
            CitizenFX.Core.Native.API.EndTextCommandDisplayHelp(0, false, true, -1);
        }

        private PedHash getAcceptableElderlyPed()
        {
            Random r = new Random();

            PedHash[] acceptableMPed = new PedHash[]
            {
                PedHash.Acult01AMO,
                PedHash.Acult02AMO,
                PedHash.Acult01AMY,
                PedHash.Genstreet01AMO,
                PedHash.Soucent03AMO,
                PedHash.OldMan1a,
                PedHash.FilmDirector
            };
            PedHash[] acceptableFPed = new PedHash[]
            {
                PedHash.Ktown01AFO,
                PedHash.MrsThornhill,
                PedHash.MovieStar,
                PedHash.Ktown02AFM,
                PedHash.Genstreet01AFO,
            };

            if(pedTitle == "Grandpa")
                return acceptableMPed[r.Next(0, acceptableMPed.Length)];
            else
                return acceptableFPed[r.Next(0, acceptableFPed.Length)];
        }

        private PedHash getAcceptableCaretakerPed()
        {
            Random r = new Random();

            PedHash[] acceptablePed = new PedHash[]
            {
                PedHash.Ktown01AFM,
                PedHash.Downtown01AFM,
                PedHash.Salton01AFM,
                PedHash.Soucent01AFM,
                PedHash.Soucent02AFM,
                PedHash.Soucentmc01AFM,
                PedHash.Salton01AFO,
                PedHash.Bevhills01AFY,
                PedHash.Eastsa01AFY,
                PedHash.Genhot01AFY,
                PedHash.Hipster04AFY,
                PedHash.Soucent02AFY,
                PedHash.Soucent01AFY,
                PedHash.Soucent03AFY,
                PedHash.Bevhills02AMM,
                PedHash.Eastsa02AMM,
                PedHash.MexCntry01AMM,
                PedHash.Salton02AMM,
                PedHash.Salton03AMM,
                PedHash.Salton04AMM,
                PedHash.Soucent01AMM,
                PedHash.Soucent02AMM,
                PedHash.Soucent03AMM,
                PedHash.Soucent04AMM,
                PedHash.Bevhills01AMY,
                PedHash.Eastsa02AMY,
                PedHash.Eastsa01AMY,
                PedHash.Genstreet01AMY
            };


            return acceptablePed[r.Next(0, acceptablePed.Length)];
        }

    }
}