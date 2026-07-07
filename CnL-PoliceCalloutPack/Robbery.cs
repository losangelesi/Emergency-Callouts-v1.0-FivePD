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
    [CalloutProperties("Robbery", "LosAngelesi", "1.1")]
    public class Robbery : FivePD.API.Callout
    {
        private readonly Random rnd = new Random();
        private Ped suspect, suspect2, victim;
        public Robbery()
        {
            int distance = rnd.Next(470, 840);
            float offsetX = rnd.Next(-1 * distance, distance);
            float offsetY = rnd.Next(-1 * distance, distance);

            InitInfo(World.GetNextPositionOnStreet(Game.PlayerPed.GetOffsetPosition(new Vector3(offsetX, offsetY, 0))));
            ShortName = "Robbery";
            CalloutDescription = "We've received reports of a robbery in progress, victim at the location. Respond code 3.";
            ResponseCode = 3;
            StartDistance = 40f;
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
            base.OnStart(player);
            suspect = await SpawnPed(FivePD.API.Utils.RandomUtils.GetRandomPed(), Location);
            suspect.BlockPermanentEvents = true;
            suspect.AlwaysKeepTask = true;
            suspect.AttachBlip();
            suspect.AttachedBlip.Sprite = (BlipSprite)480;
            suspect.AttachedBlip.Color = (BlipColor)1;

            victim = await SpawnPed(FivePD.API.Utils.RandomUtils.GetRandomPed(), Location);
            victim.BlockPermanentEvents = true;
            victim.AlwaysKeepTask = true;
            victim.AttachBlip();
            victim.AttachedBlip.Sprite = (BlipSprite)280;
            victim.AttachedBlip.Color = (BlipColor)69;
            Player player1 = Game.Player;
            Ped playerCharacter = player1.Character;
            var data = await suspect.GetData();

            data.Items = GetStolenItem();
            suspect.SetData(data);

            ShowNetworkedNotification("On Scene", "CHAR_CALL911", "CHAR_CALL911", "Dispatch", "MDT UPDATE:", 10f);

            PedQuestion question = new PedQuestion();



            //Sus&Ped Variables
            int chance = rnd.Next(0, 10);
            if (chance >= 0 && chance <= 2)
            {
                suspect2 = await SpawnPed(FivePD.API.Utils.RandomUtils.GetRandomPed(), Location);
                suspect2.BlockPermanentEvents = true;
                suspect2.AlwaysKeepTask = true;
                suspect2.AttachBlip();
                suspect2.AttachedBlip.Sprite = (BlipSprite)480;
                suspect2.AttachedBlip.Color = (BlipColor)1;
                suspect2.Task.FleeFrom(player);

                var pedData = await suspect2.GetData();
                data.Items = GetStolenItem();
                suspect2.SetData(data);
            }
            if (chance >= 3 && chance <= 5)
            {
                suspect.Weapons.Give(WeaponHash.Knife, 1, true, true);
                suspect.Task.FightAgainst(player);
                suspect2.Weapons.Give(WeaponHash.Crowbar, 1, true, true);
                suspect2.Task.FightAgainst(player);
            }
            if (chance >= 6 && chance <= 10)
            {
                // Suspect attempts to negotiate with police
                Vector3 playerPosition = playerCharacter.Position;
                int duration = 5000; // Duration in milliseconds; change this value as needed
                suspect.Task.LookAt(playerPosition, duration);
                API.PlayAmbientSpeech1(suspect.Handle, "GENERIC_INSULT_MED", "SPEECH_PARAMS_FORCE_NORMAL");
            }

        }
        private List<Item> GetStolenItem()
        {
            Item StolenItems = new Item();
            {
                StolenItems.Name = (rnd.Next(0, 10) % 3 == 0) ? "$" + rnd.Next(5, 100) + " from wallet" : "$" + rnd.Next(10, 101) + " from victim";
                StolenItems.IsIllegal = true;
            }
            return new List<Item> { StolenItems };
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
            base.OnCancelBefore();
        }
    }
}