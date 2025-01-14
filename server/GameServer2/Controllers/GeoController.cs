﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ServerDataStructures;
using ServerStateInterfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
//using Microsoft.CodeAnalysis.CSharp.Syntax;
using UserState;

namespace GameServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GeoController : ControllerBase
    {
        private const string ADMIN_SECRET_USER_NAME =
            "REmarIdYWorYpiETerdReMnAriDaYEpOsViABLEbACRoNCeNERbAlTIveIDECoMErTiOcHonypoLosenTioClATeRIGENEGMAty";

        private const string UserId_ID = "geobanana-user-id";
        private string FriendGameId_ID = "referrer-game-id";
        private readonly ILogger<GeoController> _logger;

        private readonly
            IFullServerStateGeocontroller<WellPoint, UserData, UserEvaluation,
                LevelDescription<WellPoint, RealizationData, TrueModelState>> _stateServer;

        public GeoController(ILogger<GeoController> logger,
            IFullServerStateGeocontroller<WellPoint, UserData, UserEvaluation,
                LevelDescription<WellPoint, RealizationData, TrueModelState>> stateServer)
        {
            //Note! this is magic
            _logger = logger;
            _stateServer = stateServer;
        }

        [Route("resetallscores/iERVaNDsOrphIcATHOrSeRlabLYpoIcESTawLstenTESTENTIonosterTaKOReskICIMPLATeRnA")]
        [HttpPost]
        public void ResetAllScores()
        {
            var time = DateTime.Now;
            _logger.LogInformation(time.ToLongTimeString() + ": Resetting scores");
            var userId = GetUserId();
            if (userId == ADMIN_SECRET_USER_NAME)
            {
                _stateServer.ResetServer();
                _logger.LogInformation("Resetting scores finished in {1}ms", (DateTime.Now - time).TotalMilliseconds);
            }
            else
            {
                //throw new Exception("You are not the admin");
            }
        }

        [Route("admin/scores/iERVaNDsOrphIcATHOrSeRlabLYpoIcESTawLstenTESTENTIonosterTaKOReskICIMPLATeRnA")]
        [HttpPost]
        public LevelDescription<WellPoint, RealizationData, TrueModelState> GetScores([FromBody] int index)
        {
            var time = DateTime.Now;
            _logger.LogInformation(time.ToLongTimeString() + ": Scores requested");
            var userId = GetUserId();
            if (userId == ADMIN_SECRET_USER_NAME)
            {
                var res = _stateServer.GetScoreboard(index);
                _logger.LogInformation("Score preparation finished in {1}ms", (DateTime.Now - time).TotalMilliseconds);
                return res;
            }
            else
            {
                return null;
                //throw new Exception("You are not the admin");
            }
        }

        [Route("admin/load/iERVaNDsOrphIcATHOrSeRlabLYpoIcESTawLstenTESTENTIonosterTaKOReskICIMPLATeRnA")]
        [HttpPost]
        public LevelDescription<WellPoint, RealizationData, TrueModelState> LoadScoresFromFile(
            [FromBody] string fileName)
        {
            var time = DateTime.Now;
            _logger.LogInformation(time.ToLongTimeString() + ": Scores requested");
            foreach (var ch in Path.GetInvalidFileNameChars())
            {
                if (fileName.Contains(ch))
                {
                    return null;
                }
            }

            var userId = GetUserId();
            if (userId == ADMIN_SECRET_USER_NAME)
            {
                var fName = fileName;
                var res = _stateServer.LoadScoreboardFromFile(fName);
                return res;
            }
            else
            {
                return null;
                //throw new Exception("You are not the admin");
            }
        }





        [Route("ratings")]
        [HttpGet]
        public ContentResult GetServerRatingsFromFiles([FromQuery] string fgi = null)
        {
            var time = DateTime.Now;
            _logger.LogInformation(time.ToLongTimeString() + ": Sending ratings");

            var ratings = _stateServer.CreateScoreBoardNRoundsFromFiles();

            int i = 0;

            var dynamicText = System.IO.File.ReadAllText("wwwroot/responces/table.html2");

            var line = new StringBuilder();

            foreach (var userRating in ratings)
            {
                i++;
                var userName = userRating.Key;
                if (userName.Length > 25)
                {
                    userName = userName.Substring(0, 23) + "...";
                }
                var rating = userRating.Value;
                line.Append("<tr><td>");
                line.Append(i);
                line.Append("</td><td class=\"tduser\">");
                line.Append(userName);
                line.Append("</td><td>");
                line.Append(Math.Round(rating) + "%");
                line.Append("</td></tr>\n");
            }

            var str = line.ToString();
            dynamicText = dynamicText.Replace("{{TABLE_HERE}}", str);
            var myResult = new ContentResult()
            {
                ContentType = "text/html",
                Content = dynamicText
            };

            return myResult;
        }


        [Route("admin/nextreplaywname/iERVaNDsOrphIcATHOrSeRlabLYpoIcESTawLstenTESTENTIonosterTaKOReskICIMPLATeRnA")]
        [HttpPost]
        public UserData LoadNextUserDataFromFile(ObjectWithTextString userToLoad)
        {
            var time = DateTime.Now;
            _logger.LogInformation(time.ToLongTimeString() + ": Replay requested");

            var userId = GetUserId();
            if (userId == ADMIN_SECRET_USER_NAME)
            {
                var res = _stateServer.LoadNextUserStateFromFile(userToLoad: userToLoad.text);
                return res;
            }
            else
            {
                return null;
                //throw new Exception("You are not the admin");
            }
        }

        [Route("admin/nextreplay/iERVaNDsOrphIcATHOrSeRlabLYpoIcESTawLstenTESTENTIonosterTaKOReskICIMPLATeRnA")]
        [HttpPost]
        public UserData LoadNextUserDataFromFile(bool nextUser)
        {
            var time = DateTime.Now;
            _logger.LogInformation(time.ToLongTimeString() + ": Replay requested");

            var userId = GetUserId();
            if (userId == ADMIN_SECRET_USER_NAME)
            {
                var res = _stateServer.LoadNextUserStateFromFile(nextUser);
                return res;
            }
            else
            {
                return null;
                //throw new Exception("You are not the admin");
            }
        }

        private void DumpPrintStatistics(string fileName, string sharer)
        {
            var dirShareStatistics = "sharingStatistics";
            if (!Directory.Exists(dirShareStatistics))
            {
                Directory.CreateDirectory(dirShareStatistics);
            }

            foreach (var ch in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(ch, '-');
            }

            if (fileName == "")
            {
                return;
            }

            if (fileName.Length > 20)
            {
                fileName = fileName.Substring(0, 20);
            }

            using (StreamWriter file =
                new StreamWriter(dirShareStatistics + "/" + fileName + ".txt", true))
            {
                file.WriteLine(sharer + " / " + DateTime.Now);
            }

        }

        private string ComposeHtml(string userId=null, string fgi=null, string platform=null)
        {
            var dynamicText = System.IO.File.ReadAllText("wwwroot/responces/dynamic.html2");
            if (fgi == null)
            {
                fgi = "i";
            }
            var friendGameId = fgi;
            var challenger = _stateServer.LoadFriendUserNameFromFile(fgi);

            var challengeText = "Steer your wells and then challenge your friends to beat your score!";
            var instructionsText = "Here's how to score:";
            if (challenger != null)
            {
                challengeText = challenger + " has challenged you! See if you can beat their score?";
                //" of " + score.toString() + "!"
                instructionsText = "Here's how to beat their score:";
            }

            dynamicText = dynamicText.Replace("{{CHALLENGE_TEXT_HERE}}", challengeText);
            dynamicText = dynamicText.Replace("{{INSTRUCTIONS_CAPTION_HERE}}", instructionsText);

            if (userId == null)
            {
                var loginText = System.IO.File.ReadAllText("wwwroot/responces/login_text.html2");
                dynamicText = dynamicText.Replace("{{LOGIN_TEXT_HERE}}", loginText);
            }
            else
            {
                var loggedInText = System.IO.File.ReadAllText("wwwroot/responces/continue_text.html2");
                loggedInText = loggedInText.Replace("{{USER_NAME_HERE}}", userId);
                //TODO show the name
                dynamicText = dynamicText.Replace("{{LOGIN_TEXT_HERE}}", loggedInText);
            }

            return dynamicText;
            
        }

        [Route("redirect")]
        [HttpGet]
        public ContentResult GetMePlaces([FromQuery] string fgi=null, [FromQuery] string p=null)
        {
            var time = DateTime.Now;
            _logger.LogInformation(time.ToLongTimeString() + ": Redirecting from home");
            try
            {
                //this code is extracting the second parameter from string which is a server glitch
                if (fgi!=null && fgi.Contains('&'))
                {
                    var parts = fgi.Split('&');
                    fgi = parts[0];
                    try
                    {
                        if (p == null)
                        {
                            var pEq = parts.First(s => s.StartsWith("p="));
                            p = pEq.Substring(2);
                        }
                    }
                    catch (InvalidOperationException e)
                    {
                    }
                }

                if (p == null)
                {
                    p = "other";
                }
                if (fgi != null)
                {
                    SetFriendGameId(fgi);
                    DumpPrintStatistics(p, fgi);
                }
                var userId = GetUserId();
                var dynamicString = ComposeHtml(userId, fgi, p);
                var myResult = new ContentResult()
                {
                    ContentType = "text/html",
                    Content = dynamicString
                };
                return myResult;
                //if (userId != null)
                //{
                //    Response.Redirect("/index.html");
                //}
                //else
                //{
                //    Response.Redirect("/login.html");
                //}
            }catch (Exception e)
            {
                Response.Redirect("/login.html");
                return new ContentResult();
            }
        }

        [Route("init")]
        [HttpPost]
        public void InitNewUser([FromForm] string userName)
        {
            var time = DateTime.Now;
            _logger.LogInformation(time.ToLongTimeString() + ": Initializing user started");
            if (userName.Length < 2)
            {
                Response.Redirect("/username-taken.html");
                return;
                //throw new Exception("User ID too short");
            }
            time = DateTime.Now;
            _logger.LogInformation(time.ToLongTimeString() + ": Requested adding of a user : " + userName);
            if (userName == ADMIN_SECRET_USER_NAME)
            {
                SetUserId(userName);
                Response.Redirect("/admin.html");
            }
            else
            {
                if (_stateServer.UserExists(userName))
                {
                    Response.Redirect("/username-taken.html");
                }
                else
                {
                    SetUserId(userName);
                    _stateServer.GetUserData(userName);
                    Response.Redirect("/index.html");
                }
            }
        }

        [Route("checkUser")]
        public bool CheckUser(string userName)
        {
            var time = DateTime.Now;
            _logger.LogInformation(time.ToLongTimeString() + ": Requested checking a user : " + userName);
            var userId = GetUserId();
            return userId == userName;
        }



        [Route("newgame")]
        [HttpPost]
        public int StartNewGameForUser()
        {
            var time = DateTime.Now;
            _logger.LogInformation(time.ToLongTimeString() + ": Starting new game");

            var userId = GetUserId();
            time = DateTime.Now;
            _logger.LogInformation(time.ToLongTimeString() + ": " + userId + " wants to new game.");
            var res = _stateServer.MoveUserToNewGame(userId);
            _logger.LogInformation("User {1} moved to new game in {2}ms", userId, (DateTime.Now - time).TotalMilliseconds);
            return res;
        }

        [Route("commitstop")]
        [HttpPost]
        public MyScore CommitStop()
        {
            var time = DateTime.Now;
            _logger.LogInformation(time.ToLongTimeString() + ": Stopping a user");

            var userId = GetUserId();
            var friendGameId = GetFriendGameId();
            time = DateTime.Now;
            _logger.LogInformation(time.ToLongTimeString() + ": " + userId + " is stopping.");
            MyScore res = null;
            if (friendGameId != null)
            {
                res = _stateServer.StopUser(userId, friendGameId);
            }
            else
            {
                res = _stateServer.StopUser(userId);
            }

            _logger.LogInformation("User {1} stopped in {2}ms", userId, (DateTime.Now - time).TotalMilliseconds);
            return res;
        }


        [Route("commitpoint")]
        [HttpPost]
        public UserData Commit([FromBody] WellPoint pt)
        {
            var time = DateTime.Now;
            _logger.LogInformation(time.ToLongTimeString() + ": Commiting a user point");
            var userId = GetUserId();
            time = DateTime.Now;
            _logger.LogInformation(time.ToLongTimeString() + ": " + userId + " is submitting " + pt.X + ", " + pt.Y);
            var res = _stateServer.UpdateUser(userId, pt);
            var lossyRes = _stateServer.LossyCompress(res);
            _logger.LogInformation("User {1} updated in {2}ms", userId, (DateTime.Now - time).TotalMilliseconds);
            return lossyRes;
        }

        [Route("evaluate")]
        [HttpPost]
        public UserEvaluation GetEvaluationForTrajectory([FromBody] IList<WellPoint> trajectory)
        {
            var time = DateTime.Now;
            _logger.LogInformation(time.ToLongTimeString() + ": Evaluating user trajectory");
            var userId = GetUserId();
            time = DateTime.Now;
            _logger.LogInformation(time.ToLongTimeString() + ": " + userId + " requested evaluation.");
            var res = _stateServer.GetUserEvaluationData(userId, trajectory);
            _logger.LogInformation("User {1}, sending evaluation in {2}ms", userId, (DateTime.Now - time).TotalMilliseconds);
            return res;
        }

        //[Route("addbot/iERVaNDsOrphIcATHOrSeRlabLYpoIcESTawLstenTESTENTIonosterTaKOReskICIMPLATeRnA")]
        //[HttpPost]
        //public void AddBot()
        //{
        //    var time = DateTime.Now;
        //    _logger.LogInformation(time.ToLongTimeString() + ": " + " adding a bot.");
        //    _stateServer.AddBotUserDefault();
        //    _logger.LogInformation("Bot started in {1}ms", (DateTime.Now - time).TotalMilliseconds);
        //}

        private void SetUserId(string userId)
        {
            CookieOptions option = new CookieOptions()
            {
                Expires = DateTime.Now.AddDays(30),
                IsEssential = true
            };
            Response.Cookies.Append(UserId_ID, userId, option);
        }

        private string GetUserId()
        {
            //var userId = HttpContext.Session.GetString("userId");
            var userId = HttpContext.Request.Cookies[UserId_ID];
            return userId;
        }

        private void SetFriendGameId(string friendGameId)
        {
            CookieOptions option = new CookieOptions()
            {
                Expires = DateTime.Now.AddDays(30),
                IsEssential = true
            };
            Response.Cookies.Append(FriendGameId_ID, friendGameId, option);
        }

        private string GetFriendGameId()
        {
            //var userId = HttpContext.Session.GetString("userId");
            var friendGameId = HttpContext.Request.Cookies[FriendGameId_ID];
            return friendGameId;
        }

        [Route("userdata")]
        public UserData GetUserState()
        {
            var time = DateTime.Now;
            _logger.LogInformation(time.ToLongTimeString() + ": Requesting user data");
            var userId = GetUserId();
            time = DateTime.Now;
            _logger.LogInformation(time.ToLongTimeString() + ": " + userId + " requested userdata.");
            var res = _stateServer.GetUserData(userId);
            var lossyRes = _stateServer.LossyCompress(res);
            _logger.LogInformation("User {1}, sending userdata in {2}ms", userId, (DateTime.Now - time).TotalMilliseconds);
            return lossyRes;
        }

        [Route("userdatadefault")]
        public UserData GetDummyUserData()
        {
            var time = DateTime.Now;
            _logger.LogInformation(time.ToLongTimeString() + ": someone requested some userdata.");
            var res = _stateServer.GetUserDataDefault();
            var lossyRes = _stateServer.LossyCompress(res);
            _logger.LogInformation("Sending some userdata in {1}ms", (DateTime.Now - time).TotalMilliseconds);
            return lossyRes;
        }

    }

}
