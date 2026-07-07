using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using FivePD.API;
using FivePD.API.Utils;
using CitizenFX.Core.Native;
using Microsoft.SqlServer.Server;

namespace EmergencyCallouts
{
    [CalloutProperties("Domestic Assault with Hostage", "LosAngelesi", "1.1")]
    public class DomesticAssaultHostage : Callout
    {
        private Ped suspect, victim;
        private Random random;
        private int outcome;
        private Vehicle escapeVehicle;
        private IPursuit<PursuitStateEnum> pursuit;

        public DomesticAssaultHostage()
        {
            random = new Random();
            int distance = random.Next(550, 790);
            float offsetX = random.Next(-1 * distance, distance);
            float offsetY = random.Next(-1 * distance, distance);

            InitInfo(World.GetNextPositionOnStreet(Game.PlayerPed.GetOffsetPosition(new Vector3(offsetX, offsetY, 0))));
            ShortName = "Domestic Assault with Hostage";
            CalloutDescription = "Domestic assault with a potential hostage situation. Respond Code 3.";
            ResponseCode = 3;
            StartDistance = 250f;
            outcome = random.Next(1, 16);
        }

        public override async Task OnAccept()
        {
            InitBlip();
            UpdateData();
        }

        public async override void OnStart(Ped player)
        {
            WeaponHash weapon = (WeaponHash)API.GetHashKey("WEAPON_PISTOL");
            WeaponHash weapon1 = (WeaponHash)API.GetHashKey("WEAPON_KNIFE");

            suspect = await SpawnPed(RandomUtils.GetRandomPed(), Location);
            victim = await SpawnPed(RandomUtils.GetRandomPed(), Location);
            suspect.AttachBlip();
            victim.AttachBlip();
            int scenario = new Random().Next(1, 16);
            Player player1 = Game.Player;
            Ped playerCharacter = player1.Character;
            ShowNetworkedNotification("On Scene", "CHAR_CALL911", "CHAR_CALL911", "Dispatch", "MDT UPDATE:", 10f);

            // Determine the outcome and scenario based on the random outcome variable
            switch (outcome)
            {
                case 1:
                    // Suspect surrenders peacefully upon police arrival
                    suspect.Task.HandsUp(-1);
                    API.PlayAmbientSpeech1(suspect.Handle, "GENERIC_CURSE_MED", "SPEECH_PARAMS_FORCE_NORMAL");
                    break;
                case 2:
                    // Suspect attempts to flee on foot
                    suspect.Task.FleeFrom(player);
                    break;
                case 3:
                    // Suspect attempts to flee in a vehicle
                    escapeVehicle = await SpawnVehicle(RandomUtils.GetRandomVehicle(), Location);
                    suspect.SetIntoVehicle(escapeVehicle, VehicleSeat.Driver);
                    suspect.Task.CruiseWithVehicle(escapeVehicle, 20, 447);
                    // Start a pursuit with the suspect
                    Pursuit.RegisterPursuit(suspect);
                    break;
                case 4:
                // Suspect starts a verbal argument with the hostage, but doesn't escalate
                    Player player4 = Game.Player;
                    Ped playerCharacter4 = player4.Character;
                    Vector3 playerPosition4 = playerCharacter4.Position;
                    int duration4 = 5000; // Duration in milliseconds; change this value as needed
                    suspect.Task.LookAt(playerPosition4, duration4);
                     API.PlayAmbientSpeech1(suspect.Handle, "GENERIC_CURSE_MED", "SPEECH_PARAMS_FORCE_NORMAL");
                    break;
                case 5:
                    // Suspect and victim are found arguing, but no physical violence
                    Player player5 = Game.Player;
                    Ped playerCharacter5 = player5.Character;
                    Vector3 playerPosition5 = playerCharacter5.Position;
                    int duration5 = 5000; // Duration in milliseconds; change this value as needed
                    suspect.Task.LookAt(playerPosition5, duration5);
                    API.PlayAmbientSpeech1(suspect.Handle, "GENERIC_FRIGHTENED_HIGH", "SPEECH_PARAMS_FORCE_NORMAL");
                    break;
                case 6:
                    // Suspect tries to hide when police arrive
                    suspect.Task.Cower(-1);
                    break;
                case 7:
                    // Suspect attempts to negotiate with police
                    Vector3 playerPosition7 = playerCharacter.Position;
                    int duration = 5000;
                    suspect.Task.LookAt(playerPosition7, duration);
                    API.PlayAmbientSpeech1(suspect.Handle, "GENERIC_INSULT_MED", "SPEECH_PARAMS_FORCE_NORMAL");
                    break;
                case 8:
                    // Suspect attacks the victim
                    suspect.Task.FightAgainst(victim);
                    break;
                case 9:
                    // Suspect attacks the player
                    suspect.Task.FightAgainst(player);
                    await BaseScript.Delay(5000);
                    suspect.Weapons.Give(weapon1, 1, true, true);
                    suspect.Task.FightAgainst(victim);
                    break;
                case 10:
                    // Suspect uses the victim as a human shield
                    API.TaskPlayAnim(suspect.Handle, "missminuteman_1ig_2", "handsup_base", 8.0f, 8.0f, -1, 0, 0.0f, false, false, false);
                    victim.Task.HandsUp(-1);
                    break;
                case 11:
                    // Suspect and victim are found fighting
                    suspect.Task.FightAgainst(victim);
                    victim.Task.FightAgainst(suspect);
                    break;
                case 12:
                    // Suspect attempts to flee on foot with the victim
                    suspect.Task.FleeFrom(player);
                    break;
                case 13:
                    // Suspect attempts to flee in a vehicle with the victim
                    escapeVehicle = await SpawnVehicle(RandomUtils.GetRandomVehicle(), Location);
                    suspect.SetIntoVehicle(escapeVehicle, VehicleSeat.Driver);
                    victim.SetIntoVehicle(escapeVehicle, VehicleSeat.Passenger);
                    suspect.Task.CruiseWithVehicle(escapeVehicle, 20, 447);
                    // Start a pursuit with the suspect
                    Pursuit.RegisterPursuit(suspect);
                    break;
                case 14:
                    // Suspect surrenders, but victim is found injured
                    suspect.Task.HandsUp(-1);
                    API.PlayAmbientSpeech1(suspect.Handle, "GENERIC_CURSE_MED", "SPEECH_PARAMS_FORCE_NORMAL");
                    await victim.Task.PlayAnimation("amb@medic@standing@tendtodead@idle_a", "idle_b", 8, -1, 1, AnimationFlags.None, 0);
                    await BaseScript.Delay(18000);
                    victim.Kill();
                    await BaseScript.Delay(1000);
                    suspect.Weapons.Give(weapon, 100, true, true);
                    suspect.Task.ShootAt(player);
                    break;
                case 15:
                    // Suspect and victim both surrender
                    suspect.Task.HandsUp(-1);
                    victim.Task.HandsUp(-1);
                    API.PlayAmbientSpeech1(suspect.Handle, "GENERIC_CURSE_MED", "SPEECH_PARAMS_FORCE_NORMAL");
                    API.PlayAmbientSpeech1(victim.Handle, "GENERIC_FRIGHTENED_HIGH", "SPEECH_PARAMS_FORCE_NORMAL");
                    break;
                case 16:
                    // Suspect attacks the player with a weapon
                    suspect.Weapons.Give(weapon, 100, true, true);
                    suspect.Task.ShootAt(player);
                    break;
                case 17:
                    // Suspect attacks the victim with a weapon
                    suspect.Weapons.Give(weapon1, 1, true, true);
                    suspect.Task.FightAgainst(victim);
                    break;
            }
            base.OnStart(player);
        }
        public override void OnCancelBefore()
        {
            suspect?.AttachedBlip?.Delete();
            suspect?.MarkAsNoLongerNeeded();
            suspect?.Task.WanderAround();
            suspect?.Delete();
            victim?.AttachedBlip?.Delete();
            victim?.MarkAsNoLongerNeeded();
            victim?.Task.WanderAround();
            victim?.Delete();
            escapeVehicle?.Delete();
            base.OnCancelBefore();
        }
    }
}
