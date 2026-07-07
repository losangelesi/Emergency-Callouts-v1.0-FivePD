using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using FivePD.API;
using FivePD.API.Utils;

namespace ShopLifting
{
    [CalloutProperties("Shop Lifting", "Chris07", "1.0")]

    public class ShopLifting : FivePD.API.Callout
    {
        private readonly Random rnd = new Random();
        private Ped suspect, rp;
        private ShopliftingLocation CallLocation = new ShopliftingLocation();
        private int calloutPath;
        private bool onScene = false, reportTaken = false, suspectArrested = false;
        Blip blip;

        public ShopLifting()
        {
            InitInfo(CallLocation.getRPLocation());

            this.ShortName = "Shop Lifting";
            this.CalloutDescription = "RP reporting a person has been caught shoplifting at their store.";
            this.ResponseCode = 2;
            this.StartDistance = 50f;

            BaseScript.TriggerEvent("FivePDAudio::RegisterCallout", new object[]
            {
                this.ShortName,
                @"CRIMES/CRIME_THEFT_01.ogg"
            });

            Events.OnPedArrested += OnPedArrested;
        }

        public override async Task OnAccept()
        {
            calloutPath = rnd.Next(0, 20);
            //Is selected callout path between 18-19? If so, this is an upgraded call. Make it happen.
            if (calloutPath >= 18 && calloutPath < 20)
            {
                Tick += new Func<Task>(upgradeCall);
            }
            InitBlip();
            UpdateData();
            ShowNetworkedNotification(CallLocation.getLocationComment(), "CHAR_CALL911", "CHAR_CALL911", "Dispatch", "MDT UPDATE:", 5f);
        }

        public async override void OnStart(Ped player)
        {
            base.OnStart(player);

            onScene = true;

            //Spawn RP and blip
            rp = await SpawnPed(getRPPedHash(), CallLocation.getRPLocation());
            rp.Heading = CallLocation.getRPHeading();
            rp.AttachBlip();
            rp.AttachedBlip.Sprite = (BlipSprite)465;
            rp.AttachedBlip.Color = (BlipColor)69;

            //Spawn suspect
            suspect = await SpawnPed(getSuspectPedHash(), CallLocation.getSuspectLocation());
            suspect.Heading = CallLocation.getSuspectHeading();


            PedData data = await suspect.GetData();

            data.Warrant = getRandomWarrantOrNot();
            data.Violations = getRandomViolationsOrNot();
            data.Items = getStolenItem( data.Items );
            suspect.SetData(data);


            keepTask(rp, true);
            keepTask(suspect);

            //Is the callout path for an upgraded call? If so, then make the suspect attack the RP then run.
            if (calloutPath >= 18 && calloutPath < 20)
            {
                makeSuspectViolent(player);

            }
            else if(calloutPath >= 0 && calloutPath < 4 ) // 3/20 chance for fleeing
            {
                makeSuspectFlee(player);
            }
            else //Regular boring path
            {
                Tick += new Func<Task>(greetingRangeCheck);
                
            }
            Tick += new Func<Task>(reportRangeCheck);
            Tick += new Func<Task>(checkYKey);
            Tick += new Func<Task>(rpDeathChecker);
        }

        private void makeSuspectFlee(Ped player)
        {
            //show suspect now
            suspect.AttachBlip();
            suspect.AttachedBlip.Sprite = (BlipSprite)1;
            suspect.AttachedBlip.Color = (BlipColor)1;

            TaskSequence suspectSequence = new TaskSequence();
            if (rnd.Next() % 20 != 0) //95% chance suspect will give up eventually, otherwise they'll shoot at you
            {
                suspectSequence.AddTask.FleeFrom(player, rnd.Next(5, 121) * 1000); //will stop between 5s and 2m of running
                suspectSequence.AddTask.HandsUp(-1);
                PedQuestion addOnQuestion = new PedQuestion();
            }
            else
            {
                suspectSequence.AddTask.FleeFrom(player, rnd.Next(5, 121) * 1000); //will stop between 5s and 2m of running
                suspectSequence.AddTask.HandsUp(-1);
                suspect.Weapons.Give(WeaponHash.Pistol, 50, false, true);
                suspectSequence.AddTask.FleeFrom(player, rnd.Next(5, 121) * 1000); //will stop between 5s and 2m of running
                suspectSequence.AddTask.ShootAt(player);
            }

            suspectSequence.Close();
            suspect.Task.ClearAllImmediately();
            suspect.Task.PerformSequence(suspectSequence);
            suspectSequence.Dispose();
            String[] rpShout = new string[]
            {
                "~b~RP: ~w~HEY! WHERE ARE YOU GOING!? STOP!",
                "~b~RP: ~w~STOP! WHERE DO YOU THINK YOU'RE GOING!?",
                "~b~RP: ~w~THEY'RE MAKING A RUN FOR IT! STOP THEM!",
                "~b~RP: ~w~HEY GET BACK HERE THIEF!",
                "~b~RP: ~w~GET BACK HERE YOU BASTARD!"
            };
                suspect.AttachBlip();
                suspect.AttachedBlip.Sprite = (BlipSprite)1;
                suspect.AttachedBlip.Color = (BlipColor)1;
                drawSubtitle(rpShout[rnd.Next(0, rpShout.Length)], 5000);
                Tick += new Func<Task>(reportRangeCheck);
                Tick += new Func<Task>(checkYKey);
        }

        private void makeSuspectViolent(Ped player)
        {

            //show suspect now
            suspect.AttachBlip();
            suspect.AttachedBlip.Sprite = (BlipSprite)1;
            suspect.AttachedBlip.Color = (BlipColor)1;

            TaskSequence suspectSequence = new TaskSequence();

            if (rnd.Next(0, 10) == 5) //1 in 10 chance the suspect draws a knife
            {
                suspect.Weapons.Give(WeaponHash.Knife, 1, true, true);
                suspectSequence.AddTask.FightAgainst(rp);
                suspectSequence.AddTask.FleeFrom(player);

            }
            else if (rnd.Next(0, 50) == 5) //1 in 50 chance the suspect draws a gun
            {
                suspect.Weapons.Give(WeaponHash.Pistol, 50, true, true);
                suspectSequence.AddTask.ShootAt(rp, 5000);

                if(rnd.Next(0, 5) == 0)
                {
                    suspectSequence.AddTask.ShootAt(player);
                }
                else
                {
                        suspectSequence.AddTask.FleeFrom(player, rnd.Next(5, 120) * 1000);
                        suspectSequence.AddTask.ShootAt(player);   
                }
                
                
            }
            else
            {
                suspectSequence.AddTask.FightAgainst(rp);
            }
            
            suspectSequence.Close();
            suspect.Task.ClearAllImmediately();
            suspect.Task.PerformSequence(suspectSequence);
            suspectSequence.Dispose();

            if (CallLocation.getLocationName() == "Ammunation" && rnd.Next() % 2 == 0) //RP at the ammunation has a good chance of pulling out a gun
            {
                TaskSequence rpSequence = new TaskSequence();
                rp.Weapons.Give(WeaponHash.Pistol, 15, true, true);
                rpSequence.AddTask.ShootAt(suspect);
                rpSequence.AddTask.HandsUp(-1);
                rpSequence.Close();
                rp.Task.ClearAllImmediately();
                rp.Task.PerformSequence(rpSequence);
                rpSequence.Dispose();

                PedQuestion addOnQuestion = new PedQuestion();
                addOnQuestion.Question = "Why did you open fire on the suspect?";
                addOnQuestion.Answers = new List<String>
                {
                    "He attacked me! What was I supposed to do? Retreat?",
                    "I shot him in self defense. He came at me first!",
                    "He came at me, so I shot him.",
                    "He tried to attack me, and I was in fear of my life.",
                    "He F#$%ing attacked me!"
                };
                AddPedQuestion(rp, addOnQuestion);
            }
        }

        //Make ped do only as you instruct, nothing more.
        private static void keepTask(Ped p, bool blockPE = true)
        {
            p.BlockPermanentEvents = blockPE;
            p.AlwaysKeepTask = true;
        }

        //Upgraded call path: Give radio call and MDT update to upgrade call if not already on scene
        private async Task upgradeCall()
        {
            Tick -= new Func<Task>(upgradeCall);
            await BaseScript.Delay(rnd.Next(10, 25)* 1000);
            if (onScene )
                return;

            this.ResponseCode = 3;
            this.UpdateData();

            ShowNetworkedNotification("RP reporting suspect is getting violent. ~y~Upgrade Response.", "CHAR_CALL911", "CHAR_CALL911", "Dispatch", "MDT UPDATE:", 5f);
            BaseScript.TriggerEvent("FivePDAudio::DispatchPlay", new object[] {
                @"ATTENTION_GENERIC/ATTENTION_GENERIC_01.ogg,DISPATCH_RESPOND_CODE/UNIT_RESPOND_CODE3.ogg"
            });
        }

        //Have RP randomly greet player and show suspect on map
        private async Task greetingRangeCheck()
        {
            String[] greetings = new string[]
            {
                "~b~RP: ~w~Hello officer. The person I called you about is over ~h~there",
                "~b~RP: ~w~Hello. The person is over ~h~there",
                "~b~RP: ~w~Thank God you're here. The shoplifter is over ~h~there",
                "~b~RP: ~w~Took you long enough. That low-life shoplifter is over ~h~there",
                "~b~RP: ~w~Officer, thanks for coming. The shoplifter is over ~h~there",
                "~b~RP: ~w~Hey there. That scumbag thief is over ~h~there.",
                "~b~RP: ~w~Thanks for coming. They're over ~h~there.",
                "~b~RP: ~w~Good you're here. That degenerate thief is over ~h~there."
            };
            if (Game.PlayerPed.IsInRangeOf(rp.Position, 3f))
            {
                
                suspect.AttachBlip();
                suspect.AttachedBlip.Sprite = (BlipSprite)1;
                suspect.AttachedBlip.Color = (BlipColor)1;
                drawSubtitle(greetings[rnd.Next(0, greetings.Length)], 5000);

                rp.Task.ChatTo(Game.PlayerPed);
                suspect.Task.LookAt(Game.PlayerPed);

                Tick -= new Func<Task>(greetingRangeCheck);
            }
        }

        private async Task rpDeathChecker()
        {
            if (API.IsPedDeadOrDying(rp.Handle, true))
            {
                Tick -= new Func<Task>(checkYKey);
                Tick -= new Func<Task>(reportRangeCheck);
                rp.AttachedBlip.Delete();
                Tick -= new Func<Task>(rpDeathChecker);
            }
            
        }

        private async Task reportRangeCheck()
        {
            if( Game.PlayerPed.IsInRangeOf( rp.Position, 2f) )
            {
                drawHelpText("Press ~INPUT_MP_TEXT_CHAT_TEAM~ to take a report from the RP.");
                Tick -= new Func<Task>(reportRangeCheck);
                await BaseScript.Delay(8000);
                if(! reportTaken )
                    Tick += new Func<Task>(reportRangeCheck);
            }
        }

        private async Task checkYKey()
        {
            if (Game.IsControlJustReleased(0, Control.MpTextChatTeam) && Game.PlayerPed.IsInRangeOf(rp.Position, 2f) && !API.IsPedDeadOrDying(rp.Handle, true) ) // is Y selected and are you close to RP?
            {
                int pencilHash = CitizenFX.Core.Native.API.GetHashKey("prop_pencil_01");
                Vector3 handPos = Function.Call<Vector3>(Hash.GET_PED_BONE_COORDS, Game.PlayerPed, 58866, 0f, 0f, 0f);
                API.TaskStartScenarioInPlace(Game.PlayerPed.Handle, "CODE_HUMAN_MEDIC_TIME_OF_DEATH", 0, true);
                int pencilProp = API.CreateObject(pencilHash, handPos.X, handPos.Y, handPos.Z, true, true, false);
                Function.Call(Hash.ATTACH_ENTITY_TO_ENTITY, pencilProp, Game.PlayerPed, Function.Call<int>(Hash.GET_PED_BONE_INDEX, Game.PlayerPed, 58866), 0.11, -0.02, 0.001, -120.0, 0.0, 0.0, true, true, false, true, 1, true);

                await BaseScript.Delay(25000);
                API.ClearPedTasks(Game.PlayerPed.Handle);
                API.DeleteObject(ref pencilProp);
                Tick -= new Func<Task>(checkYKey);
                Tick -= new Func<Task>(reportRangeCheck);
                reportTaken = true;
                rp.AttachedBlip.Delete();
                ShowNetworkedNotification("Witness report taken. This should help the charges stick.", "CHAR_HUMANDEFAULT", "CHAR_HUMANDEFAULT", "Callout Progress", "Objective Complete", 5f);
                Tick -= new Func<Task>(rpDeathChecker);
            }
            return;
        }

        //Add stolen items to Ped Data. Each type of business has a different value range of items
        private List<Item> getStolenItem( List<Item> list )
        {
            Item StolenItems = new Item();
            if (CallLocation.getLocationName() == "LTD" || CallLocation.getLocationName() == "24/7")
            {
                StolenItems.Name = (rnd.Next(0, 10) % 3 == 0) ? "$" + rnd.Next(5, 100) + " in liquor" : "$" + rnd.Next(2, 61) + " in food items";
                StolenItems.IsIllegal = true;
                list.Add(StolenItems);
            }
            else if (CallLocation.getLocationName() == "Robs")
            {
                StolenItems.Name = "$" + rnd.Next(6, 201) + " in liquor";
                StolenItems.IsIllegal = true;
                list.Add(StolenItems);
            }
            else if (CallLocation.getLocationName() == "Ammunation")
            {
                String[] PossibleItems = new string[] {
                    "Stolen Handgun", "Stolen Knife", "Stolen Body Armor", "Stolen Clothing", "Stolen Rifle Magazine", "Stolen Handgun Magazine", "Stolen Flashlight", "Stolen Rifle Sight", "Stolen 9mm Ammo", "Stolen .223 Ammo", "Stolen 12G Shotgun Shells",
                    "Stolen Pistol Sight", "Stolen Misc Gun Accessories"
                };

                StolenItems.Name = PossibleItems[rnd.Next(0, PossibleItems.Length)];
                StolenItems.IsIllegal = true;
                list.Add(StolenItems);
            }
            else if (CallLocation.getLocationName() == "Ponsonbys")
            {
                StolenItems.Name = "$" + rnd.Next(200, 5000) + " in designer products";
                StolenItems.IsIllegal = true;
                list.Add(StolenItems);
            }
            else
            {
                
                StolenItems.Name = "$" + rnd.Next(5, 250) + " in products";
                StolenItems.IsIllegal = true;
                list.Add(StolenItems);
            }

            return list;
        }

        //Randomly generate a violation...or not. random chance
        private List<Violation> getRandomViolationsOrNot()
        {
            String[,] PossibleViolations = new String[,] { {"Drug Possession","Misdemeanor"}, {"Public Intoxication", "Misdemeanor" }, {"Drug possesion with intent to distribute", "Felony" }, {"Petty Theft", "Misdemeanor"}, {"Grand Theft", "Felony" }, {"Resisting Arrest", "Misdemeanor" }, {"Grand Theft Auto", "Felony" }, {"Tresspassing", "Misdemeanor" }, { "Assault", "Misdemeanor" },
                { "Assault with a deadly weapon", "Felony" }, {"Possession of Pariphenalia", "Misdemeanor" }, {"Disturbing the Peace", "Misdemeanor" }, {"Embezzlement","Misdemeanor" }, {"Embezzlement","Felony" }, {"Illegal Gambling", "Misdemeanor" }, {"Possession of Controlled Substances", "Misdemeanor" } };

            int num = (rnd.Next(0, 6) == 1) ? rnd.Next(0, 5) : 0; // 1/5 chance of previous charges
            List<Violation> list = new List<Violation>();

            if ( num == 0 )
            {
                return list;
            }
            else
            {
                for( int i = 0; i < num; i++ )
                {
                    Violation violation = new Violation();
                    int index = rnd.Next(0, PossibleViolations.GetLength(0));
                    violation.Offence = PossibleViolations[ index , 0 ];
                    violation.Charge = PossibleViolations[ index, 1];
                    list.Add(violation);
                }
            }

            return list;
        }

        //Generate a random warrant for the suspect...or not. 1/7 chance
        private String getRandomWarrantOrNot()
        {
            String[] possibleWarrants = new String[]
             {
                "Arrest Warrant",
                "Bench Warrant"
             };

            //1/6 chance there is a warrant
            if (rnd.Next(0, 7) != 1)
            {
                return "-";
            }

            int i = rnd.Next(0, possibleWarrants.Length - 1);

            return possibleWarrants[i];
        }

        //Certain stores have certain peds as shopkeepers. Select the appropriate ped for the call location.
        private PedHash getRPPedHash()
        {
            Random r = new Random();

            if (CallLocation.getLocationName() == "LTD" || CallLocation.getLocationName() == "24/7")
            {
                return PedHash.ShopKeep01;
            }
            else if (CallLocation.getLocationName() == "Robs")
            {
                int num = r.Next() % 2;
                switch (num)
                {
                    case 0:
                        return PedHash.Genstreet01AMO;
                    case 1:
                        return PedHash.Strvend01SMM;
                }
            }
            else if (CallLocation.getLocationName() == "Ammunation")
            {
                int num = r.Next() % 2;
                switch (num)
                {
                    case 0:
                        return PedHash.Ammucity01SMY;
                    case 1:
                        return PedHash.AmmuCountrySMM;
                }
            }
            else if (CallLocation.getLocationName() == "DiscountClothes")
            {
                return PedHash.Sweatshop01SFM;
            }
            else if (CallLocation.getLocationName() == "Bincos")
            {
                int num = r.Next() % 2;
                switch (num)
                {
                    case 0:
                        return PedHash.ShopLowSFY;
                    case 1:
                        return PedHash.Sweatshop01SFY;
                }
            }
            else if (CallLocation.getLocationName() == "HatShop" || CallLocation.getLocationName() == "BeachShop")
            {
                int num = r.Next() % 2;
                switch (num)
                {
                    case 0:
                        return PedHash.Beachvesp01AMY;
                    case 1:
                        return PedHash.ShopMaskSMY;
                }
            }
            else if (CallLocation.getLocationName() == "Ponsonbys")
            {
                return PedHash.ShopHighSFM;
            }
            else if (CallLocation.getLocationName() == "SubUrban")
            {
                int num = r.Next() % 2;
                switch (num)
                {
                    case 0:
                        return PedHash.ShopMidSFY;
                    case 1:
                        return PedHash.Hipster01AMY;
                }
            }

            return FivePD.API.Utils.RandomUtils.GetRandomPed();

        }
        //Override initblip so I can easily change my blip colors and size
        protected void InitBlip(float circleRadius = 75f, BlipColor color = BlipColor.Blue, BlipSprite sprite = BlipSprite.BigCircle, int alpha = 100)
        {
            blip = World.CreateBlip(this.Location, circleRadius);
            this.Radius = circleRadius;
            this.Marker = blip;
            this.Marker.Sprite = sprite;
            this.Marker.Color = color;
            this.Marker.Alpha = alpha;
        }

        //Draw a subtitle on the screen
        private void drawSubtitle(string message, int duration)
        {
            CitizenFX.Core.Native.API.BeginTextCommandPrint("STRING");
            CitizenFX.Core.Native.API.AddTextComponentSubstringPlayerName(message);
            CitizenFX.Core.Native.API.EndTextCommandPrint(duration, false);
        }

        private void drawHelpText(string message)
        {
            CitizenFX.Core.Native.API.BeginTextCommandDisplayHelp("STRING");
            CitizenFX.Core.Native.API.AddTextComponentSubstringPlayerName(message);
            CitizenFX.Core.Native.API.EndTextCommandDisplayHelp(0, false, true, -1);
        }

        //Will select from a list of "acceptable" suspect peds
        private PedHash getSuspectPedHash()
        {
            Random r = new Random();

            PedHash[] suspectPeds = new PedHash[]
            {
                PedHash.Downtown01AFM,
                PedHash.Eastsa01AFM,
                PedHash.Eastsa02AFM,
                PedHash.FatBla01AFM,
                PedHash.FatWhite01AFM,
                PedHash.Salton01AFM,
                PedHash.Skidrow01AFM,
                PedHash.Soucent02AFM,
                PedHash.Soucentmc01AFM,
                PedHash.Tourist01AFM,
                PedHash.Tramp01AFM,
                PedHash.TrampBeac01AFM,
                PedHash.Salton01AFO,
                PedHash.Bevhills02AFY,
                PedHash.Eastsa01AFY,
                PedHash.Eastsa02AFY,
                PedHash.Eastsa03AFY,
                PedHash.Fitness02AFY,
                PedHash.Genhot01AFY,
                PedHash.Rurmeth01AFY,
                PedHash.Soucent03AFY,
                PedHash.Tourist02AFY,
                PedHash.Vinewood02AFY,
                PedHash.AfriAmer01AMM,
                PedHash.Eastsa01AMM,
                PedHash.Eastsa02AMM,
                PedHash.Farmer01AMM,
                PedHash.Fatlatin01AMM,
                PedHash.Genfat01AMM,
                PedHash.Genfat02AMM,
                PedHash.Hillbilly01AMM,
                PedHash.Hillbilly02AMM,
                PedHash.Indian01AMM,
                PedHash.MexCntry01AMM,
                PedHash.MexLabor01AMM,
                PedHash.Rurmeth01AMM,
                PedHash.Salton01AMM,
                PedHash.Salton02AMM,
                PedHash.Salton03AMM,
                PedHash.Salton04AMM,
                PedHash.Skidrow01AMM,
                PedHash.Socenlat01AMM,
                PedHash.Soucent01AMM,
                PedHash.Soucent03AMM,
                PedHash.Tramp01AMM,
                PedHash.TrampBeac01AMM,
                PedHash.Acult02AMO,
                PedHash.Salton01AMO,
                PedHash.Soucent02AMO,
                PedHash.Soucent03AMO,
                PedHash.Tramp01AMO,
                PedHash.Cyclist01AMY,
                PedHash.Eastsa01AMY,
                PedHash.Eastsa02AMY,
                PedHash.Epsilon01AMY,
                PedHash.Gay01AMY,
                PedHash.Hiker01AMY,
                PedHash.Hippy01AMY,
                PedHash.Ktown01AMY,
                PedHash.Ktown02AMY,
                PedHash.Methhead01AMY,
                PedHash.Polynesian01AMY,
                PedHash.Skater02AMY,
                PedHash.Soucent02AMY,
                PedHash.Soucent03AMY,
                PedHash.Stlat01AMY,
                PedHash.Vinewood03AMY,
                PedHash.OldMan1a,
                PedHash.MrsPhillips,
                PedHash.OldMan2,
                PedHash.Omega,
                PedHash.Stretch,
                PedHash.Wade,
                PedHash.ChinGoonCutscene,
                PedHash.FosRepCutscene,
                PedHash.Hao,
                PedHash.Maude,
                PedHash.Paige,
                PedHash.RampGang,
                PedHash.RampHic,
                PedHash.Ballas01GFY,
                PedHash.Families01GFY,
                PedHash.Vagos01GFY,
                PedHash.ArmLieut01GMM,
                PedHash.ChiGoon01GMM,
                PedHash.MexBoss02GMM,
                PedHash.ArmGoon02GMY,
                PedHash.BallaEast01GMY,
                PedHash.BallaOrig01GMY,
                PedHash.BallaSout01GMY,
                PedHash.Famca01GMY,
                PedHash.Famdnf01GMY,
                PedHash.Korean01GMY,
                PedHash.Lost01GMY,
                PedHash.Lost02GMY,
                PedHash.Lost03GMY,
                PedHash.MexGoon01GMY,
                PedHash.MexGoon02GMY,
                PedHash.MexGoon03GMY
            };

            return suspectPeds[r.Next(0, suspectPeds.Length)];

        }

        public override async void OnCancelBefore()
        {
            if (!reportTaken && suspectArrested && !API.IsPedDeadOrDying(rp.Handle, true))
            {
                ShowNetworkedNotification("You failed to take a report from the RP. Suspect will be released without charge.", "CHAR_BLOCKED", "CHAR_BLOCKED", "~y~Callout Complete!", "", 5f);
            }
            else if (suspectArrested && reportTaken)
            {
                ShowNetworkedNotification("Suspect has been booked and will be charged appropriately. Good work!", "CHAR_HUMANDEFAULT", "CHAR_HUMANDEFAULT", "~b~Callout Complete!", "", 5f);
            }
            else if ((reportTaken || API.IsPedDeadOrDying(rp.Handle, true)) && API.IsPedDeadOrDying(suspect.Handle, true))
            {
                ShowNetworkedNotification("Callout Completed successfully!", "CHAR_HUMANDEFAULT", "CHAR_HUMANDEFAULT", "~b~Callout Complete!", "", 5f);
            }
            else
            {
                ShowNetworkedNotification("Callout cancelled. Returning to service.", "CHAR_CALL911", "CHAR_CALL911", "Callout Cancelled", "", 5f);
            }
            rp.AttachedBlip.Delete();
            rp.MarkAsNoLongerNeeded();
            rp.Delete();
            suspect.AttachedBlip.Delete();
            suspect.MarkAsNoLongerNeeded();
            suspect.Delete();
            blip.Delete();
        }

        public async Task OnPedArrested(Ped ped)
        {
            if (ped.Handle == suspect.Handle)
            {
                ShowNetworkedNotification("Suspect Arrested.", "CHAR_HUMANDEFAULT", "CHAR_HUMANDEFAULT", "Callout Progress", "~g~Objective Complete!", 5f);
                if (!reportTaken && !rp.IsDead )
                {
                    ShowNetworkedNotification("Looks like you forgot to take a report from the RP. Better do that now!", "CHAR_ALL_PLAYERS_CONF", "CHAR_ALL_PLAYERS_CONF", "Callout Progress", "~c~Reminder:", 5f);
                }
                suspectArrested = true;
            }
        }
           
    }
}