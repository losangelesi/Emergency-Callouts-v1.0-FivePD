using System;
using System.Threading.Tasks;
using FivePD.API;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace EmergencyCallouts
{
    [CalloutProperties("Trespassing", "LosAngelesi", "1.1")]
    public class Trespassing : Callout
    {
        private Ped suspect;
        private bool callCanceled;
        private readonly Random rnd = new Random();
        private Blip searchAreaBlip;
        private Vector3 searchArea;

        public Trespassing()
        {
            int distance = rnd.Next(300, 750);
            float offsetX = rnd.Next(-1 * distance, distance);
            float offsetY = rnd.Next(-1 * distance, distance);

            InitInfo(World.GetNextPositionOnStreet(Game.PlayerPed.GetOffsetPosition(new Vector3(offsetX, offsetY, 0))));
            ShortName = "Trespassing";
            CalloutDescription = "A report of a trespasser has been received. Respond Code 1";
            ResponseCode = 1;
            StartDistance = 250f;
        }

        public override async Task OnAccept()
        {
            InitBlip();
            UpdateData();
            searchArea = Location + new Vector3(rnd.Next(-100, 100), rnd.Next(-100, 100), 0);
            searchAreaBlip = World.CreateBlip(searchArea);
            searchAreaBlip.Color = BlipColor.Yellow;
            searchAreaBlip.Sprite = (BlipSprite)1;
            searchAreaBlip.Name = "Search Area";
        }

        public async override void OnStart(Ped player)
        {
            InitBlip();
            Random random = new Random();
            callCanceled = random.Next(1, 101) <= 10; // 10% chance for the call to be canceled
            ShowNetworkedNotification("On Scene", "CHAR_CALL911", "CHAR_CALL911", "Dispatch", "MDT UPDATE:", 10f);

            if (callCanceled)
            {
                Notify("~r~The call has been canceled.");
                EndCallout();
                return;
            }

            PedHash[] pedHashes = { PedHash.Methhead01AMY, PedHash.Salton01AFM };
            PedHash randomPedHash = pedHashes[random.Next(pedHashes.Length)];
            suspect = await SpawnPed(randomPedHash, Location); // Replace with actual coordinates
            suspect.AlwaysKeepTask = true;
            suspect.BlockPermanentEvents = true;

            // Determine if the suspect flees
            bool suspectFlees = random.Next(1, 101) <= 30; // 30% chance for the suspect to flee

            if (suspectFlees)
            {
                // Make the suspect flee
                suspect.Task.FleeFrom(player);
            }
            else
            {
                // Make the suspect wander around
                suspect.Task.WanderAround();
            }
        }
        public override void OnCancelBefore()
        {
            suspect?.AttachedBlip?.Delete();
            suspect?.MarkAsNoLongerNeeded();
            suspect?.Task.WanderAround();
            suspect?.Delete();
            searchAreaBlip?.Delete();
            base.OnCancelBefore();
        }
        [Tick]
        public async Task OnTick()
        {
            // Check if the suspect has been arrested
            if (!callCanceled && suspect.IsCuffed)
            {
                // End the callout
                Notify("~g~Suspect arrested. Good job!");
                EndCallout();
            }

            // Check if the player is in the search area
            if (Game.PlayerPed.Position.DistanceToSquared(searchArea) < 10000)
            {
                // Remove the search area blip
                searchAreaBlip?.Delete();
            }

            await BaseScript.Delay(1000); // Control the tick rate (1 second)
        }

        private void Notify(string message)
        {
            BaseScript.TriggerEvent("chat:addMessage", new
            {
                color = new[] { 255, 255, 255 },
                args = new[] { "", message }
            });
        }
    }
}
