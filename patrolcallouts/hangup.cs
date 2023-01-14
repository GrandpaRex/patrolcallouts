using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using FivePD.API;
using FivePD.API.Utils;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;

#pragma warning disable 1998
namespace patrolcallouts
{
    [CalloutProperties(name: "911 Hangup", author: "Grandpa Rex", version: "1.0")]
    public class hangup : Callout
    {
        private Ped caller;
        private readonly Random rnd = new Random();
        internal List<Vector3> calloutLocations = new List<Vector3>();

        public hangup()
        {
            _ = LoadConfig();
            InitInfo(calloutLocations.SelectRandom());

            ShortName = "911 Hangup";
            CalloutDescription = "A number that dialed 911 hung up on dispatch";
            ResponseCode = 2;
            StartDistance = 100f;
            FixedLocation = false;

        }

        public override async Task OnAccept()
        {
            try
            {
                InitBlip(30f, BlipColor.Green, BlipSprite.BigCircleOutline, 200);
                UpdateData();
            }
            catch
            {
                EndCallout();
            }
        }

        public async override void OnStart(Ped player)
        {
            base.OnStart(AssignedPlayers.FirstOrDefault());
            Ped unit = AssignedPlayers.FirstOrDefault();
            float distance = World.GetDistance(AssignedPlayers.FirstOrDefault().Position, Location);
            PlayerData udata = Utilities.GetPlayerData();

            try
            {
                int x = rnd.Next(1, 100);
                if (x < 90)
                {
                    caller = await SpawnPed(RandomUtils.GetRandomPed(), Location);
                    caller.BlockPermanentEvents = true;
                    caller.AlwaysKeepTask = true;
                    caller.Task.PlayAnimation("amb@code_human_cross_road@male@idle_a", "idle_e");
                    PedData pdata = await caller.GetData();
                    
                    

                    while (distance > 30f) { await BaseScript.Delay(250); }
                    string fname = pdata.FirstName;
                    string lname = pdata.LastName;
                    string uname = udata.DisplayName;
                    ShowNotification($"{uname} the registered owner of the phone is {fname} {lname}");
                }
                else
                {
                    while (distance > 30f) { await BaseScript.Delay(250); }
                    string uname = udata.DisplayName;
                    ShowNotification($"{uname} we were unable to trace the number, recommend returning 10-8");
                }
            }
            catch { }
        }

        public override void OnCancelBefore()
        {
            base.OnCancelBefore();

            try
            {
                if (caller.IsAlive && !caller.IsCuffed) { caller.Task.WanderAround(); caller.AlwaysKeepTask = false; caller.BlockPermanentEvents = false; }
            }
            catch { }
        }

        private void ShowNotification(string msg, string sender = "Dispatch", string subject = "Callout Update")
        {
            ShowNetworkedNotification(msg, "CHAR_CALL911", "CHAR_CALL911", sender, subject, 1f);
        }

        internal async Task LoadConfig()
        {
            try
            {
                // Coordinates
                JObject config = JObject.Parse(LoadResourceFile(GetCurrentResourceName(), "/callouts/patrolcallouts/config.json"));
                JToken coords = config["hangup"];
                if (coords["locations"] != null)
                {
                    // Get the coordinates
                    foreach (var _location in coords["locations"])
                    {
                        string[] location = ((string)_location).Split(',');
                        float.TryParse(location[0], out float locationX);
                        float.TryParse(location[1], out float locationY);
                        float.TryParse(location[2], out float locationZ);

                        calloutLocations.Add(new Vector3(locationX, locationY, locationZ));
                    }
                }
                else
                {
                    Debug.WriteLine("~r~[Patrol Callouts]~w~ Couldn't load locations!");
                }    
            }
            catch (Exception e)
            {
                Debug.WriteLine("Could not read 911 Hangup locations");
                Debug.WriteLine(e.ToString());
            }
        }
    }
}
