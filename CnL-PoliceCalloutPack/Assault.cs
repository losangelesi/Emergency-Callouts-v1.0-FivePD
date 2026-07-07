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
    [CalloutProperties("Assault", "LosAngelesi", "1.0")]
    public class Assault : Callout
    {
        private readonly Random rnd = new Random();
        private Ped suspect, victim;
        private int calloutPath;
        private Blip searchArea, blip;
        private string pedTitle;
        private bool onScene = false, reportTaken = false, suspectArrested = false;

        public Assault()
        {
            int distance = rnd.Next(500, 780);
            float offsetX = rnd.Next(-1 * distance, distance);
            float offsetY = rnd.Next(-1 * distance, distance);
            InitInfo(World.GetNextPositionOnStreet(Game.PlayerPed.GetOffsetPosition(new Vector3(offsetX, offsetY, 0))));

            ShortName = "Assault";
            CalloutDescription = "We've received a report of a Assault. Respond Code 2.";
            ResponseCode = 2;
            StartDistance = 250f;
        }

        protected void InitBlip(float circleRadius = 75f, BlipColor color = BlipColor.Blue, BlipSprite sprite = BlipSprite.BigCircle, int alpha = 100)
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
            calloutPath = rnd.Next(0, 20);
            //Is selected callout path between 15-20? If so, this is an upgraded call. Make it happen.
            if (calloutPath >= 15 && calloutPath < 20)
            {
                Tick += new Func<Task>(upgradeCall);
            }
            InitBlip();
            UpdateData();
        }

        public async override void OnStart(Ped player)
        {
            base.OnStart(player);
            suspect = await SpawnPed(FivePD.API.Utils.RandomUtils.GetRandomPed(), Location);
            victim = await SpawnPed(FivePD.API.Utils.RandomUtils.GetRandomPed(), Location);
            suspect.BlockPermanentEvents = true;
            victim.BlockPermanentEvents = true;
            suspect.AttachBlip();
            victim.AttachBlip();
            ShowNetworkedNotification("On Scene", "CHAR_CALL911", "CHAR_CALL911", "Dispatch", "MDT UPDATE:", 10f);

            string[] startdialogue = new string[]
            {
                "Suspect: It's the police!",
                "Victim: help!"
            };
            ShowDialog(startdialogue[rnd.Next(0, startdialogue.Length)], 5000, 25f);

            ShowNetworkedNotification("On Scene", "CHAR_CALL911", "CHAR_CALL911", "Dispatch", "MDT UPDATE:", 10f);

            int chance = rnd.Next(0, 100);

            // Victim outcomes
            if (chance >= 0 && chance <= 20)
            {
                // Victim runs away
                victim.Task.ReactAndFlee(suspect);
                String[] rpShout = new string[]
                {
                    "~b~RP: ~w~HEY! WHERE ARE YOU GOING!? STOP!",
                    "~b~RP: ~w~STOP! WHERE DO YOU THINK YOU'RE GOING!?",
                    "~b~RP: ~w~THEY'RE MAKING A RUN FOR IT! STOP THEM!",
                    "~b~RP: ~w~HEY GET BACK HERE THIEF!",
                    "~b~RP: ~w~GET BACK HERE YOU BASTARD!"
                };
            }
            else if (chance > 20 && chance <= 40)
            {
                // Victim fights against the suspect
                victim.Task.FightAgainst(suspect);
                Tick += new Func<Task>(upgradeCall);
                String[] rpShout = new string[]
            {
                "~b~RP: ~w~HEY! STOP! PLEASE!",
                "~b~RP: ~w~DON'T HURT THEM!",
                "~b~RP: ~w~THEY'RE TRYING TO RUN OFFICER!",
                "~b~RP: ~w~HEY GET BACK HERE THIEF!",
                "~b~RP: ~w~YOU BASTARD!"
            };
            }
            else if (chance > 40 && chance <= 60)
            {
                // Victim tries to reason with the suspect
                victim.Task.TurnTo(suspect);
                victim.Task.HandsUp(5000);
            }
            else if (chance > 60 && chance <= 80)
            {
                // Victim is injured and cannot move
                victim.Health = 30;
                victim.Task.PlayAnimation("amb@world_human_bum_slumped@male@laying_on_left_side@idle_a", "idle_a", -1, 1, AnimationFlags.None);
            }
            else if (chance > 80 && chance <= 100)
            {
                //Take a report. Start checker for help text and Y button press
                Tick += new Func<Task>(reportRangeCheck);
                Tick += new Func<Task>(checkYKey);
                Tick += new Func<Task>(rpDeathChecker);
                Tick += new Func<Task>(greetingRangeCheck);
                // Victim surrenders
                victim.Task.TurnTo(suspect);
                victim.Task.HandsUp(-1);
            }

            chance = rnd.Next(0, 100);
            TaskSequence suspectSequence = new TaskSequence();

            // Suspect outcomes
            if (chance >= 0 && chance <= 20)
            {
                // Suspect runs away
                suspect.Task.ReactAndFlee(player);
                //Take a report. Start checker for help text and Y button press
                Tick += new Func<Task>(reportRangeCheck);
                Tick += new Func<Task>(checkYKey);
                Tick += new Func<Task>(rpDeathChecker);
                Tick += new Func<Task>(greetingRangeCheck);
            }
            else if (chance > 20 && chance <= 40)
            {
                // Suspect fights against the player
                suspectSequence.AddTask.FleeFrom(player, rnd.Next(5, 121) * 1000); //will stop between 5s and 2m of running
                suspectSequence.AddTask.HandsUp(-1);
                suspect.Weapons.Give(WeaponHash.Pistol, 50, false, true);
                suspectSequence.AddTask.FleeFrom(player, rnd.Next(5, 121) * 1000); //will stop between 5s and 2m of running
                suspectSequence.AddTask.ShootAt(player);
            }
            else if (chance > 40 && chance <= 60)
            {
                // Suspect tries to reason with the victim
                suspect.Task.TurnTo(victim);
                suspect.Task.HandsUp(5000);
            }
            else if (chance > 60 && chance <= 80)
            {
                // Suspect surrenders
                suspect.Task.TurnTo(player);
                suspect.Task.HandsUp(-1);
            }
            else
            {
                // Suspect pretends to be the victim
                suspect.Task.TurnTo(player);
                suspect.Task.PlayAnimation("amb@world_human_bum_slumped@male@laying_on_left_side@idle_a", "idle_a", -1, 1, AnimationFlags.None);
            }
        }
        //The RP greets the player when they first approach them.
        private async Task greetingRangeCheck()
        {
            String[] greetings = new string[]
            {
                "~b~RP: ~w~Hey Officer. Thank you for coming.",
                "~b~RP: ~w~Help Officer!",
                "~b~RP: ~w~I'm sorry about all this. "+pedTitle+" is really angry.",
                "~b~RP: ~w~Thank goodness, officer. "+pedTitle+" tried to kill me!"
            };
            if (Game.PlayerPed.IsInRangeOf(victim.Position, 10f))
            {
                this.ShowDialog(greetings[rnd.Next(0, greetings.Length)], 5000, 10f);

                victim.Task.ChatTo(Game.PlayerPed);

                Tick -= new Func<Task>(greetingRangeCheck);
            }
        }
        //Checks to see if the RP is dead or not before the report is taken. If so, end the callout.
        private async Task rpDeathChecker()
        {
            if (API.IsPedDeadOrDying(victim.Handle, true))
            {
                Tick -= new Func<Task>(checkYKey);
                Tick -= new Func<Task>(reportRangeCheck);
                Tick -= new Func<Task>(rpDeathChecker);
                Tick += new Func<Task>(greetingRangeCheck);
                ShowNetworkedNotification("RP is dead. Returning to service.", "CHAR_BLOCKED", "CHAR_BLOCKED", "~y~Callout Failed!", "", 5f);
                base.EndCallout();
            }
        }

        //Checks to see if we are in range of the RP to take a report. If so, display a help dialog telling the user to press Y
        private async Task reportRangeCheck()
        {
            if (Game.PlayerPed.IsInRangeOf(victim.Position, 2f))
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
            if (Game.IsControlJustReleased(0, Control.MpTextChatTeam) && Game.PlayerPed.IsInRangeOf(victim.Position, 2f) && !API.IsPedDeadOrDying(victim.Handle, true)) // is Y selected and are you close to RP?
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
                victim.AttachedBlip.Delete();
                Tick -= new Func<Task>(rpDeathChecker);
                victim.MarkAsNoLongerNeeded();
                ShowNetworkedNotification("Report taken from the victim.", "CHAR_HUMANDEFAULT", "CHAR_HUMANDEFAULT", "Callout Progress", "Objective Complete", 5f);
                startPedSearch();
            }
            return;
        }
        private void drawHelpText(string message)
        {
            CitizenFX.Core.Native.API.BeginTextCommandDisplayHelp("STRING");
            CitizenFX.Core.Native.API.AddTextComponentSubstringPlayerName(message);
            CitizenFX.Core.Native.API.EndTextCommandDisplayHelp(0, false, true, -1);
        }
        private async Task pedRangeCheck()
        {
            String[] greetings = new string[]
            {
                "~b~Subject: ~w~What do you want?",
                "~b~Subject: ~w~Hello there.",
                "~b~Subject: ~w~Hey officer..",
                "~b~Subject: ~w~What's up officer?",
                "~b~Subject: ~w~Excuse me...I'm walking here?",
            };
            String[] fleePhrase = new string[]
            {
                "~b~Subject: ~w~Oh no! Stay away from me!",
                "~b~Subject: ~w~Stay away! HELP! SOMEONE HELP!",
                "~b~Subject: ~w~I'M NOT GOING BACK TO JAIL!",
                "~b~Subject: ~w~Leave me alone!"
            };
            String[] giveupPhrase = new string[]
            {
                "~b~Subject: ~w~I give up! Please don't hurt me",
                "~b~Subject: ~y~*Cough* *wheeze* ~w~I...can't...run...anymore ~y~*cough*",
                "~b~Subject: ~y~*Out of breath* ~w~Okay, I didn't do anything."
            };

            if (Game.PlayerPed.IsInRangeOf(suspect.Position, 2f))
            {
                Tick -= new Func<Task>(pedRangeCheck);
                int chance = rnd.Next(0, 15);

                if (chance >= 0 && chance < 4) //Flee
                {
                    suspect.AttachedBlip.Sprite = (BlipSprite)1;
                    suspect.AttachedBlip.Color = (BlipColor)21;
                    suspect.Task.FleeFrom(Game.PlayerPed);
                    this.ShowDialog(fleePhrase[rnd.Next(0, fleePhrase.Length)], 5000, 5f);
                    suspect.Task.FleeFrom(Game.PlayerPed);
                    await BaseScript.Delay(rnd.Next(5, 30) * 1000);
                    suspect.Task.HandsUp(-1);
                    this.ShowDialog(giveupPhrase[rnd.Next(0, giveupPhrase.Length)], 5000, 5f);
                }
                else //Don't flee
                {
                    int actionChance = rnd.Next(5, 9);
                    if (actionChance >= 0 && actionChance <= 4) // Attack all players
                    {
                        suspect.AttachedBlip.Sprite = (BlipSprite)480;
                        suspect.AttachedBlip.Color = (BlipColor)20;
                        this.ShowDialog(greetings[rnd.Next(0, greetings.Length)], 5000, 5f);
                        suspect.Task.FightAgainst(Game.PlayerPed);
                    }
                    else // Flee then attack
                    {
                        suspect.AttachedBlip.Sprite = (BlipSprite)480;
                        suspect.AttachedBlip.Color = (BlipColor)20;
                        this.ShowDialog(fleePhrase[rnd.Next(0, fleePhrase.Length)], 5000, 5f);
                        suspect.Task.FleeFrom(Game.PlayerPed);
                        await BaseScript.Delay(20000);
                        suspect.Task.FightAgainst(Game.PlayerPed);
                    }
                    suspect.Weapons.Give(GetRandomWeapon(), 1, true, true);
                }
                ShowNetworkedNotification("Subject located. Out with one..", "CHAR_HUMANDEFAULT", "CHAR_HUMANDEFAULT", "Callout Progress", "~g~Objective Complete!", 5f);
            }
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

        private void removeBlip()
        {
            blip.Delete();
        }

        private void UpdateSearchBlip(float circleRadius = 75f, BlipColor color = BlipColor.Blue, BlipSprite sprite = BlipSprite.BigCircle, int alpha = 100)
        {
            int offset = rnd.Next(1, 30);
            float offsetX = rnd.Next(-1 * offset, offset);
            float offsetY = rnd.Next(-1 * offset, offset);
            Vector3 updatedLoc = new Vector3(suspect.Position.X + offsetX, suspect.Position.Y + offsetY, suspect.Position.Z);

            searchArea.Delete();
            searchArea = World.CreateBlip(updatedLoc, circleRadius);
            this.Radius = circleRadius;
            this.Marker = searchArea;
            this.Marker.Sprite = sprite;
            this.Marker.Color = color;
            this.Marker.Alpha = alpha;
        }
        public override void OnCancelBefore()
        {
            suspect?.AttachedBlip?.Delete();
            suspect?.MarkAsNoLongerNeeded();
            suspect?.Task.WanderAround();
            suspect?.Delete();
            victim?.AttachedBlip?.Delete();
            victim?.MarkAsNoLongerNeeded();
            victim?.Delete();
            searchArea?.Delete();
            base.OnCancelBefore();
        }
        private async void startPedSearch()
        {
            int distance = rnd.Next(600, 1000);
            float offsetX = rnd.Next(-1 * distance, distance);
            float offsetY = rnd.Next(-1 * distance, distance);

            this.Location = World.GetNextPositionOnStreet(Game.PlayerPed.GetOffsetPosition(new Vector3(offsetX, offsetY, 0)));
            UpdateData();
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
