using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using FivePD.API;
using FivePD.API.Utils;
using Newtonsoft.Json.Linq;

#pragma warning disable 1998
namespace patrolcallouts
{
    [CalloutProperties(name: "911 Hangup", author: "Grandpa Rex", version: "1.0")]
    public class Hangup : Callout
    {
        private Ped _caller;
        private readonly Random _rnd = new Random();
        private List<Vector3> _calloutLocations = new List<Vector3>();

        public Hangup()
        {
            _ = LoadConfig();
            InitInfo(_calloutLocations.SelectRandom());

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

        public override async void OnStart(Ped player)
        {
            base.OnStart(AssignedPlayers.FirstOrDefault());
            var unit = AssignedPlayers.FirstOrDefault();
            var badge = Utilities.GetPlayerData().Callsign;

            try
            {
                var x = _rnd.Next(1, 100);
                if (x < 90)
                {
                    var heading = _rnd.Next(1, 360);
                    _caller = await SpawnPed(RandomUtils.GetRandomPed(), Location);
                    _caller.BlockPermanentEvents = true;
                    _caller.AlwaysKeepTask = true;
                    _caller.Heading = heading;
                    _caller.Task.PlayAnimation("amb@code_human_cross_road@male@idle_a", "idle_e");
                    var pdata = await _caller.GetData();
                    
                    

                    while (World.GetDistance(unit.Position, Location) > 30f) { await BaseScript.Delay(250); }
                    var fname = pdata.FirstName;
                    var lname = pdata.LastName;
                    ShowNotification($"{badge} the registered owner of the phone is {fname} {lname}");
                }
                else
                {
                    while (World.GetDistance(unit.Position, Location) > 30f) { await BaseScript.Delay(250); }
                    ShowNotification($"{badge} we were unable to trace the number, recommend returning 10-8");
                }
            }
            catch { EndCallout(); }
        }

        public override void OnCancelBefore()
        {
            base.OnCancelBefore();

            try
            {
                if (!_caller.IsAlive || _caller.IsCuffed) return;
                _caller.Task.WanderAround(); _caller.AlwaysKeepTask = false; _caller.BlockPermanentEvents = false;
            }
            catch { EndCallout(); }
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
                var config = JObject.Parse(LoadResourceFile(GetCurrentResourceName(), "/callouts/patrolcallouts/config.json"));
                var coords = config["hangup"];
                if (coords["locations"] != null)
                {
                    // Get the coordinates
                    foreach (var _location in coords["locations"])
                    {
                        string[] location = ((string)_location).Split(',');
                        float.TryParse(location[0], out var locationX);
                        float.TryParse(location[1], out var locationY);
                        float.TryParse(location[2], out var locationZ);

                        _calloutLocations.Add(new Vector3(locationX, locationY, locationZ));
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
