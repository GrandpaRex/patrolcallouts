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
                    await Questions();
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
                    await BaseScript.Delay(500);
                    EndCallout();
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
        
        private async Task Questions()
        {
            var question1 = new PedQuestion
            {
                Question = "Did you call 911?",
                Answers = new List<string>
                {
                    "Yes",
                    "Yes",
                    "No",
                    "I have no idea what you're talking about",
                    "Leave me alone!"
                }
            };

            var question2 = new PedQuestion
            {
                Question = "[Denied] We traced the phone back to your name",
                Answers = new List<string>
                {
                    "Okay fine it was me",
                    "Okay fine it was me",
                    "You can't do that",
                    "That's illegal!"
                }
            };

            var question3 = new PedQuestion
            {
                Question = "Why did you hang up on our operator?",
                Answers = new List<string>
                {
                    "I got scared!",
                    "I thought he was going to come after me",
                    "*Silence*"
                }
            };

            var question4 = new PedQuestion
            {
                Question = "Is there something you want to tell me?",
                Answers = new List<string>
                {
                    "*Silence*",
                    "*Silence*",
                    "*Silence*",
                    "Just some guy, he was kinda being an ass to me. I want my lawyer",
                    "No nothing at all *looks at the ground*"
                }
            };

            PedQuestion[] pedQuestions = new PedQuestion[]
            {
                question1,
                question2,
                question3,
                question4
            };

            AddPedQuestions(_caller, pedQuestions);
        }

        private async Task LoadConfig()
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
