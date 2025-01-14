using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Xml.Schema;
using Newtonsoft.Json;
using ServerDataStructures;
using static System.IO.Path;


namespace ServerStateInterfaces
{
    public abstract class ServerStateBase<
        TWellPoint, TUserDataModel, TUserModel,
        TSecretState, TUserResult, TRealizationData> :
        IFullServerStateGeocontroller<
            TWellPoint, TUserDataModel, TUserResult, LevelDescription<TWellPoint, TRealizationData, TSecretState>>
        where TUserModel : IUserImplementaion<
            TUserDataModel, TWellPoint, TSecretState, TUserResult, TRealizationData>, new()

    {
        protected ConcurrentDictionary<string, UserScorePairLockedGeneric<TUserModel, TUserDataModel, TSecretState,
                TWellPoint, TUserResult, TRealizationData>>
            _users =
                new ConcurrentDictionary<string, UserScorePairLockedGeneric<TUserModel, TUserDataModel, TSecretState,
                    TWellPoint, TUserResult, TRealizationData>>();

        //protected IList<LevelDescription<TWellPoint, TRealizationData>> _scoreDataAll;
        //protected ConcurrentDictionary<UserResultId, UserResultFinal<TWellPoint>>
        //    _resultingTrajectories =
        //        new ConcurrentDictionary<UserResultId, UserResultFinal<TWellPoint>>();

        //protected ConcurrentDictionary<UserResultId, UserResultFinal<TWellPoint>> 
        //    _newResultingTrajectories = 
        //        new ConcurrentDictionary<UserResultId, UserResultFinal<TWellPoint>>();
        /// <summary>
        /// We never write to this one
        /// </summary>
        protected readonly LevelDescription<TWellPoint, TRealizationData, TSecretState>[] _levelDescriptions =
            new LevelDescription<TWellPoint, TRealizationData, TSecretState>[TOTAL_LEVELS];

        /// <summary>
        /// 
        /// </summary>
        protected readonly TSecretState[] _secrets = new TSecretState[TOTAL_LEVELS];

        protected TUserDataModel _dummyUserData;


        protected const string BotUserName = "Alyaev et al.[2019]";

        protected const int TOTAL_LEVELS = 4;
        //protected TSecretState _secret = default;

        //TODO Generate secret states
        //TODO update code for many secrets
        //TODO make a funciton that fatches secret for a user given their game number
        private int _seedInd = 0;


        protected abstract ObjectiveEvaluationDelegateUser<TUserDataModel, TWellPoint, TUserResult>.
            ObjectiveEvaluationFunction
            EvaluatorUser { get; }

        protected abstract ObjectiveEvaluatorDelegateTruth<TRealizationData, TWellPoint>.ObjectiveEvaluationFunction
            EvaluatorTruth { get; }



        private Random rnd = new Random();
        //4 is really bad 91 is bad
        //good seeds in 100: 101, 102?, 103!, 105
        //good seeds in 200: 201, 202, 203, 204, 205, 206, 207, 208, 209
        //202 bottom good bot 1948/3000
        //105 top bot gets 1739 2200
        //101 bottom bot top 963 / 1727
        //!!!!
        //103 bottom bit top 3100 / 4400
        //!!!!
        //201 bottom only 725 / 1500
        //205 bottom bot 1168 / 2568
        //206 nope
        //207 top bot 3327 / 4318







        protected int[] seeds =
        {
            503,
            401,
            //402,
            //403,
            //404,
            //405,
            //406,
            //407,
            //408,
            //409,
            410,
            //411,
            412,
            //end of 4
            600,
            //500,
            //501,
            //502,
            503,
            //504,
            505,
            //506,
            507,
            508,
            //509,
            //510,
            //511,
            //512,
            513,
            //514,
            515,
            //516,
            //517,
            518,
            519,
            //409,
            401,
            //402,
            //403,
            //404,
            //405,
            //406,
            //407,
            //408,
            //409,
            410,
            //411,
            412,
            413,
            414,
            415,
            416,
            417,
            418,
            419,
            420,
            //original NFES
            //0,
            //202,
            //105, //105,
            //end of original NFES
            //103, //103,
            214, //214,
            209,
            //214,
            213,
            215,
            227, 228, 229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 240, 241
        };
        //private int[] seeds = {0, 91, 91, 10, 100};
        //private int[] seeds = { 0, 1, 91, 91, 10, 100, 3, 1, 4, 4, 5, 6, 7, 7, 8, 8, 8 };



        private int NextSeed()
        {
            var res = rnd.Next();
            _seedInd++;
            if (_seedInd < seeds.Length)
            {
                res = seeds[_seedInd];
            }


            return res;
        }

        public ServerStateBase()
        {
            //TODO check if works without
            InitializeNewSyntheticTruths();
            AddBotUserDefault();
        }

        public void DumpScoreBoardToFile(LevelDescription<TWellPoint, TRealizationData, TSecretState> scoreBoard,
            string dirIdHighScore = "scoreLog/",
            string fileName = "")
        {
            if (!Directory.Exists(dirIdHighScore))
            {
                Directory.CreateDirectory(dirIdHighScore);
            }

            if (fileName == "")
            {
                fileName += DateTime.Now.Ticks;
            }

            var jsonStr = JsonConvert.SerializeObject(scoreBoard);
            System.IO.File.WriteAllText(dirIdHighScore + "/" + fileName, jsonStr);
        }

        //public void DumpAllScoreBoardToFile(
        //    string dirIdHighScore = "scoreLog/")
        //{
        //    ;
        //    if (!Directory.Exists(dirIdHighScore))
        //    {
        //        Directory.CreateDirectory(dirIdHighScore);
        //    }

        //    DumpAllScoresToFile(GetUserResultsForAllGames(), dirIdHighScore);

        //    for (int i = 0; i < TOTAL_LEVELS; ++i)
        //    {
        //        DumpScoreBoardToFile(GetScoreboard(i),
        //            dirIdHighScore,
        //            "board_" + seeds[i] + ".json");
        //    }

        //}


        public string GetUserNameDirId(string userId)
        {
            var hashString = string.Format("{0:X}",
                UserScorePairLockedGeneric<TUserModel, TUserDataModel, TSecretState, TWellPoint, TUserResult,
                        TRealizationData>
                    .CalculateHashInt(userId));
            var strMaxLen = 15;
            var userDirName = userId.Trim();
            if (userDirName.Length > strMaxLen)
            {
                userDirName = userDirName.Remove(strMaxLen);
            }

            //foreach (var ch in GetInvalidFileNameChars())
            //{
            //    userDirName = userDirName.Replace(ch, '-');
            //}
            userDirName = Regex.Replace(userDirName, @"[^A-Za-z0-9]+", "-");
            userDirName = userDirName + "-" + hashString;

            return userDirName;
        }


        public string DumpUserResultToFileOnStop(KeyValuePair<UserResultId, UserResultFinal<TWellPoint>> resultPair)
        {
            var userId = resultPair.Key.UserName;
            var serverGameSeed = seeds[resultPair.Key.GameId];

            var userDirName = GetUserNameDirId(userId);


            var dirPrefix = "resultLog/";
            var userDirId = DateTime.Now.Ticks + "-" + userDirName;
            var dirId = dirPrefix + userDirId;
            if (!Directory.Exists(dirId))
            {
                Directory.CreateDirectory(dirId);
            }

            var newPair = new KeyValuePair<UserResultId, UserResultFinal<TWellPoint>>(
                new UserResultId(userId, resultPair.Key.GameNumberForUser, serverGameSeed),
                resultPair.Value);

            var jsonStr = JsonConvert.SerializeObject(newPair);
            File.WriteAllText(dirId + "/" + "userResultPair.json", jsonStr);

            //FIXME see how much logging do we get
            //DumpAllScoreBoardToFile(dirId);
            return userDirId;
        }

        private static double _GetRatingPercent(IList<UserResultFinal<TWellPoint>> results,
            UserResultFinal<TWellPoint> singleResult)
        {
            var myScore = GetFinalScore(singleResult);
            double lower = results.Select(GetFinalScore).Count(value => value < myScore);
            var total = results.Count - 1;
            if (total == 0)
            {
                return 100;
            }
            return lower / total * 100.0;
        }

        private static IList<double> ComputeRatingsForUser(Dictionary<int, LevelDescription<TWellPoint, TRealizationData, TSecretState>> boards,
            IList<KeyValuePair<UserResultId, UserResultFinal<TWellPoint>>> resultsForUser)
        {
            
            var resCount = resultsForUser.Count;

            var oneGameRatings = new double[resCount];
            var curTotal = 0.0;
            var sumRating = new double[resCount];

            var maxRating = new double[resCount];

            for (var index = 0; index<resCount; index++)
            {
                var userResultPair = resultsForUser[index];
                var key = userResultPair.Key;
                var curBoard = boards[key.GameId];

                var curRating = _GetRatingPercent(curBoard.UserResults, userResultPair.Value);
                oneGameRatings[index] = curRating;
                
                
                //update averages
                for (var j = 0; j < index; ++j)
                {
                    //update total
                    sumRating[j] += - oneGameRatings[index - j - 1] + curRating;
                    if (sumRating[j] / (j + 1) > maxRating[j])
                    {
                        maxRating[j] = sumRating[j] / (j + 1);
                    }
                }
                //for index
                sumRating[index] = curTotal + curRating;
                maxRating[index] = sumRating[index] / (index + 1);
                //for next
                curTotal = sumRating[index];
            }

            return maxRating;
        }

        public IList<KeyValuePair<string, double>> CreateScoreBoardNRoundsFromFiles()
        {
            var pair = LoadAndCreateScoreBoardUserPair();
            var list = CreateScoreBoardNRounds(pair.Item1, pair.Item2, 3);
            return list;
        }

        private IList<KeyValuePair<string, double>> CreateScoreBoardNRounds(Dictionary<int, LevelDescription<TWellPoint, TRealizationData, TSecretState>> boards,
        Dictionary<string, IList<KeyValuePair<UserResultId, UserResultFinal<TWellPoint>>>> userScores, int numGames = 3)
        {
            var score3List = new List<KeyValuePair<string, double>>(userScores.Count);
            var ratingsDict = new Dictionary<string, IList<double>>(userScores.Count);
            //var numGames = 3;
            foreach (var userPair in userScores)
            {
                var userId = userPair.Key;
                var ratings = ComputeRatingsForUser(boards, userPair.Value);
                ratingsDict.Add(userId, ratings);

                if (ratings.Count >= numGames)
                {
                    score3List.Add(new KeyValuePair<string, double>(userId, ratings[numGames - 1]));
                }
            }

            var scoreBoard = score3List.OrderByDescending(x => x.Value).ToList();

            return scoreBoard;
        }

        /// <summary>
        /// This gets the dictionary containing all standings
        /// </summary>
        /// <returns></returns>
        public Tuple<Dictionary<int, LevelDescription<TWellPoint, TRealizationData, TSecretState>>, 
            Dictionary<string, IList<KeyValuePair<UserResultId, UserResultFinal<TWellPoint>>>>> 
            LoadAndCreateScoreBoardUserPair()
        {
            var dirId = "resultLog/";
            if (!Directory.Exists(dirId))
            {
                Directory.CreateDirectory(dirId);
            }

            var allDirectories = Directory.GetDirectories(dirId);
            Array.Sort(allDirectories);
            var boards = new Dictionary<int, LevelDescription<TWellPoint, TRealizationData, TSecretState>>();
            var userScores = new Dictionary<string, 
                IList<KeyValuePair<UserResultId,UserResultFinal<TWellPoint>>>>();
            for (var i = 0; i < TOTAL_LEVELS; ++i)
            {
                boards.Add(seeds[i], new LevelDescription<TWellPoint, TRealizationData, TSecretState>());
                boards[seeds[i]].UserResults = new List<UserResultFinal<TWellPoint>>();
            }

            var totalGames = 0;

            //this gives a list of rounds and all boards
            foreach (var directory in allDirectories)
            {
                try
                {
                    var dir = directory.Substring(directory.LastIndexOf('/') + 1);
                    var pair = LoadUserResultPairFromFile(dir);
                    var key = pair.Key;

                    //user
                    if (!userScores.ContainsKey(key.UserName))
                    {
                        userScores.Add(key.UserName, new List<KeyValuePair<UserResultId, UserResultFinal<TWellPoint>>>());
                    }
                    var curList = userScores[key.UserName];
                    //if (curList.Count >= 10)
                    //{
                    //    continue;
                    //}


                    curList.Add(pair);
                    totalGames++;

                    //board
                    var curBoard = boards[key.GameId];
                    curBoard.UserResults.Add(pair.Value);
                }
                catch (Exception e)
                {
                    System.Console.Write("Probably no needed file in a dir: " + e);
                }
            }

            System.Console.WriteLine("total " + totalGames);
            System.Console.WriteLine("total users " + userScores.Count);
            System.Console.WriteLine("average games per user " + totalGames * 1.0 / userScores.Count);

            //foreach (var board in boards.Values)
            //{
            //    board.UserResults = board.UserResults.OrderBy(x => GetFinalScore(x)).ToList();
            //}
            return new Tuple<Dictionary<int, LevelDescription<TWellPoint, TRealizationData, TSecretState>>, 
                Dictionary<string, IList<KeyValuePair<UserResultId, UserResultFinal<TWellPoint>>>>>(
                boards, userScores);
        }

        public MyScore GetMyFullScore(UserResultId resultId, UserResultFinal<TWellPoint> result, bool getRating = true, bool getBotResult = true, string friendsSaveId = "")
        {
            var boardsAndResults = LoadAndCreateScoreBoardUserPair();
            var boards = boardsAndResults.Item1;
            var resultsAllUsers = boardsAndResults.Item2;
            var gameSeed = seeds[resultId.GameId];
            var serverGameIndex = resultId.GameId;
            var valueScore = GetFinalScore(result);
            var userName = resultId.UserName;
            var results = boards[gameSeed].UserResults;

            var score = new MyScore()
            {
                ScorePercent = GetScorePercentForGame(serverGameIndex, valueScore),
                ScoreValue = valueScore,
                YouDidBetterThan = _GetRatingPercent(results, result),
                UserName = userName,
            };
            if (getBotResult)
            {
                if (resultsAllUsers.ContainsKey(BotUserName))
                {
                    var botsResults = resultsAllUsers[BotUserName];
                    try
                    {
                        var botsResultCurGame = botsResults.Last(
                            x => x.Key.GameId == gameSeed);
                        var botsFinalScore = GetFinalScore(botsResultCurGame.Value);
                        score.AiScore = new MyScore()
                        {
                            ScorePercent = GetScorePercentForGame(serverGameIndex, botsFinalScore),
                            ScoreValue = botsFinalScore,
                            UserName = BotUserName
                        };

                    }
                    catch (InvalidOperationException e)
                    {
                        System.Console.WriteLine("Warning, no bot result for the game " + resultId + "\n" + e);
                    }
                }
            }

            if (friendsSaveId != "")
            {
                try
                {
                    var friendsName = LoadFriendUserNameFromFile(friendsSaveId);
                    if (resultsAllUsers.ContainsKey(friendsName))
                    {
                        var friendsResults = resultsAllUsers[friendsName];
                        try
                        {
                            var friendsResultCurGame
                                = friendsResults.Last(
                                    x => x.Key.GameId == gameSeed);
                            var friendsFinalScore = GetFinalScore(friendsResultCurGame.Value);
                            score.FriendsScore = new MyScore()
                            {
                                ScorePercent = GetScorePercentForGame(serverGameIndex, friendsFinalScore),
                                ScoreValue = friendsFinalScore,
                                UserName = friendsName
                            };

                        }
                        catch (InvalidOperationException e)
                        {
                            System.Console.WriteLine("Warning, no friends result for the game " + resultId + "\n" + e);
                        }
                    }
                }
                catch (Exception e)
                {
                    System.Console.WriteLine("Error while loading friends score " + friendsSaveId + "\n" + e);
                }
            }
            if (getRating)
            {
                var resultsForUser = resultsAllUsers[userName];
                score.Rating = ComputeRatingsForUser(boards, resultsForUser);
                //GetRating(userName, GetResultsForUser(GetUserResultsForAllGames(), userName));
            }
            return score;
        }

        private static double GetFinalScore(UserResultFinal<TWellPoint> userResult)
        {
            return userResult.TrajectoryWithScore[userResult.TrajectoryWithScore.Count - 1].Score;
        }

        /// <summary>
        /// Load user results from file useful
        /// </summary>
        /// <param name="folderId"></param>
        /// <returns></returns>
        private static KeyValuePair<UserResultId, UserResultFinal<TWellPoint>> LoadUserResultPairFromFile(string folderId)
        {
            var dirId = "resultLog/" ;
            var fileSuffix = "/userResultPair.json";

            var userResultPair = ReadFromFileSafe<KeyValuePair<UserResultId, UserResultFinal<TWellPoint>>>(dirId, folderId, fileSuffix);
            return userResultPair;
        }

        private static T ReadFromFileSafe<T>(string prefix, string unsafeName, string suffix="") {
            foreach (var ch in GetInvalidFileNameChars())
            {
                if (unsafeName.Contains(ch))
                {
                    return default;
                }
            }

            try
            {
                var fileString = File.ReadAllText(prefix+unsafeName+suffix);
                var data = JsonConvert.DeserializeObject<T>(fileString);
                return data;
            }
            catch (Exception e)
            {
                return default;
            }
        }


        /// <summary>
        /// Load scoreboard from file useful
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public LevelDescription<TWellPoint, TRealizationData, TSecretState> LoadScoreboardFromFile(string fileName)
        {
            var dirId = "scoreLog";

            var scoreBoard = ReadFromFileSafe<LevelDescription<TWellPoint, TRealizationData, TSecretState>>(dirId, fileName);
            return scoreBoard;
        }

        //TODO remove
        public void DumpAllScoresToFile(IList<KeyValuePair<UserResultId, UserResultFinal<TWellPoint>>> scoreBoard,
            string folderName)
        {
            var dirId = "";
            var dirIdHighScore = dirId + folderName;
            if (!Directory.Exists(dirIdHighScore))
            {
                Directory.CreateDirectory(dirIdHighScore);
            }

            var fileName = "allScores.json";

            var jsonStr = JsonConvert.SerializeObject(scoreBoard);
            System.IO.File.WriteAllText(dirIdHighScore + "/" + fileName, jsonStr);
        }

        public IList<KeyValuePair<UserResultId, UserResultFinal<TWellPoint>>> LoadAllScoresFromFile(string folderName)
        {
            var dirId = "resultLog/";
            var fileId = "/allScores.json";

            var allScores = ReadFromFileSafe<List<KeyValuePair<UserResultId, UserResultFinal<TWellPoint>>>>(dirId, folderName, fileId);
            return allScores;
        }

        //TODO remove
        /// <summary>
        /// Not to be used
        /// </summary>
        /// <param name="folderName"></param>
        /// <param name="fileInd"></param>
        /// <returns></returns>
        public IList<UserResultFinal<TWellPoint>> LoadScoresForGameFromFile(string folderName, int fileInd)
        {
            var dirId = "resultLog/";
            var fileId = "/board_"+fileInd+".json";

            var allScores = ReadFromFileSafe<ScoreBoard<TWellPoint, TRealizationData, TSecretState>>(dirId, folderName, fileId);
            return allScores.UserResults;
        }

        private string lastLoadedUser = "";
        private string lastLoadedUserFile = "";
        //private IFullServerStateGeocontroller<TWellPoint, TUserDataModel, TUserResult, LevelDescription<TWellPoint, TRealizationData, TSecretState>>
        //    _fullServerStateGeocontrollerImplementation;

        private string _GetNextDir()
        {
            var dirId = "userLog/";
            var allDirs = Directory.GetDirectories(dirId);
            Array.Sort(allDirs);
            if (allDirs.Length == 0)
            {
                throw new InvalidOperationException("No folders to choose from");
            }
            try
            {
                var nextDir = allDirs.First(str => str.CompareTo(lastLoadedUser) > 0);
                lastLoadedUser = nextDir;
                lastLoadedUserFile = "";
                return nextDir;
            }
            catch (InvalidOperationException e)
            {
                lastLoadedUser = "";
                return _GetNextDir();
            }
        }

        private string _GetNextFile(string dirId)
        {
            var files = Directory.GetFiles(dirId);
            Array.Sort(files);
            try
            {
                var getNextFile = files.First(str => str.CompareTo(lastLoadedUserFile) > 0);
                lastLoadedUserFile = getNextFile;
                return getNextFile;
            }
            catch (InvalidOperationException e)
            {
                dirId = _GetNextDir();
                return _GetNextFile(dirId);
            }
        }


        public TUserDataModel LoadNextUserStateFromFile(bool nextUser = false, string userToLoad = "")
        {
            if (userToLoad != "")
            {
                //TODO make this into a const
                lastLoadedUser = "userLog/" + userToLoad;
                lastLoadedUserFile = "";
                lastLoadedUser = _GetNextDir();
            }
            if (nextUser || lastLoadedUser == "")
            {
                lastLoadedUser = _GetNextDir();
            }

            lastLoadedUserFile = _GetNextFile(lastLoadedUser);

            var fileName = lastLoadedUserFile;

            var userState = ReadFromFileSafe<TUserDataModel>(fileName, "");
            return userState;
        }

        //public ManyWells<TWellPoint> GetScreenFull()
        //{
        //    return _fullServerStateGeocontrollerImplementation.GetScreenFull();
        //}


        /// <summary>
        /// this should call dump secret stateGeocontroller to file
        /// </summary>
        /// <param name="seed"></param>
        protected abstract TSecretState[] InitializeNewSyntheticTruths();
        //{
        //    DumpSectetStateToFile(seed);
        //    //Console.WriteLine("Initialized synthetic truth with seed: " + seed);
        //    //_syntheticTruth = new TrueModelState(seed);
        //}

        protected abstract IList<TRealizationData> GetTruthsForEvaluation();

        protected UserScorePairLockedGeneric<TUserModel, TUserDataModel, TSecretState, TWellPoint,
            TUserResult, TRealizationData> GetNewDefaultUserPair(string userKey)
        {
            //TODO here we need to create synthetic truth?
            return new UserScorePairLockedGeneric<TUserModel, TUserDataModel, TSecretState, TWellPoint,
                TUserResult, TRealizationData>(
                userKey,
                EvaluatorUser, EvaluatorTruth, GetTruthsForEvaluation());
        }

        public void ResetServer(int seed = -1)
        {
            //TODO implement the seed
            _users = new ConcurrentDictionary<string, 
                UserScorePairLockedGeneric<TUserModel, TUserDataModel, TSecretState, 
                    TWellPoint, TUserResult, TRealizationData>>();
        }



        //public ManyWells<TWellPoint> GetScreenFull()
        //{
        //    var wells = new ManyWells<TWellPoint>();
        //    var allTrajectoryKeys = _resultingTrajectories.Keys;
        //    foreach (var userResultId in allTrajectoryKeys)
        //    {
        //        UserResultFinal<TWellPoint> curTrajectory;
        //        var result = _resultingTrajectories.TryGetValue(userResultId, out curTrajectory);
        //        if (result)
        //        {
        //            wells.UserResults.Add(TrajectoryOutputSingle<TWellPoint>.FromUserResult(curTrajectory));
        //        }
        //    }

        //    return wells;
        //}

        //public IList<KeyValuePair<UserResultId, UserResultFinal<TWellPoint>>> GetUserResultsForAllGames()
        //{
        //    var allTrajectoryKeys = _resultingTrajectories.Keys;
        //    var currentScores = new List<KeyValuePair<UserResultId, UserResultFinal<TWellPoint>>>();
        //    foreach (var userResultId in allTrajectoryKeys)
        //    {
        //        UserResultFinal<TWellPoint> curTrajectory;
        //        var result = _resultingTrajectories.TryGetValue(userResultId, out curTrajectory);
        //        if (result)
        //        {
        //            var serverGameSeed = seeds[userResultId.GameId];
        //            var newPair = new KeyValuePair<UserResultId, UserResultFinal<TWellPoint>>(
        //                new UserResultId(userResultId.UserName, userResultId.GameNumberForUser, serverGameSeed),
        //                curTrajectory);
        //            currentScores.Add(newPair);
        //        }
        //    }

        //    return currentScores;
        //}

        private static List<KeyValuePair<UserResultId, UserResultFinal<TWellPoint>>> GetResultsForUser(IList<KeyValuePair<UserResultId, UserResultFinal<TWellPoint>>> current, string userId)
        {
            //var allTrajectoryKeys = current.Select(x => x.Key);
            var selectedPairs = current.Where(x => x.Key.UserName == userId).ToList();

            selectedPairs = selectedPairs.OrderBy(x => x.Key.GameNumberForUser).ToList();
            return selectedPairs;
        }

        //public IList<UserResultFinal<TWellPoint>> GetUserResultsForGame(int serverGameIndex)
        //{
        //    serverGameIndex %= _levelDescriptions.Length;
        //    var allTrajectoryKeys = _resultingTrajectories.Keys;
        //    var selectedKeys = allTrajectoryKeys.Where(key => key.GameId == serverGameIndex);
        //    var currentScores = new List<UserResultFinal<TWellPoint>>();
        //    foreach (var userResultId in selectedKeys)
        //    {
        //        UserResultFinal<TWellPoint> curTrajectory;
        //        var result = _resultingTrajectories.TryGetValue(userResultId, out curTrajectory);
        //        if (result)
        //        {
        //            currentScores.Add(curTrajectory);
        //        }
        //    }

        //    return currentScores;
        //}

        //public UserResultFinal<TWellPoint> GetBotResultForGame(int serverGameIndex)
        //{
        //    UserResultFinal<TWellPoint> botResult = null;
        //    var key = new UserResultId(BotUserName, serverGameIndex, serverGameIndex);
        //    //var getResult = _resultingTrajectories.TryGetValue(key, out botResult);
        //    return botResult;
        //}

        //private IList<double> GetRating(string userName, 
        //    IList<KeyValuePair<UserResultId, UserResultFinal<TWellPoint>>> current, 
        //    IList<KeyValuePair<UserResultId, UserResultFinal<TWellPoint>>> original = null,
        //    string folderName = "")
        //{
        //    List<KeyValuePair<UserResultId, UserResultFinal<TWellPoint>>> resultsForUser = null;
        //    var exclude = 1;
        //    if (original == null)
        //    {
        //        resultsForUser = GetResultsForUser(current, userName);
        //        original = current;
        //    }
        //    else
        //    {
        //        resultsForUser = GetResultsForUser(original, userName);
        //        exclude = 0;
        //    }
            
            

        //    var resCount = resultsForUser.Count;

        //    var oneGameRatings = new double[resCount];
        //    var curTotal = 0.0;
        //    var totalRatingUpTo = new double[resCount];
        //    var maxRating = new double[resCount];

        //    for (var index = 0; index < resCount; index++)
        //    {
        //        var keyValuePair = resultsForUser[index];
        //        //if (keyValuePair.Key.GameNumberForUser != index)
        //        //{
        //        //    throw new InvalidOperationException("User has not done all games");
        //        //}

        //        var serverIndex = seeds.ToList().FindIndex(x => x == keyValuePair.Key.GameId);
                

        //        double curRating = 0;
        //        //the game is active on the server
        //        if (serverIndex != -1 && serverIndex<TOTAL_LEVELS)
        //        {
        //            curRating = GetPercentile100ForGame(serverIndex, GetFinalScore(keyValuePair.Value), exclude);
        //        }
        //        else
        //        {
        //            //this requests from a board
        //            curRating = LoadPercentile100ForGame(GetFinalScore(keyValuePair.Value), folderName, keyValuePair.Key.GameId);
        //        }

        //        oneGameRatings[index] = curRating;

        //        //update averages
        //        for (var j = 0; j < index; ++j)
        //        {
        //            totalRatingUpTo[j] = totalRatingUpTo[j] - oneGameRatings[index-j-1] + curRating;
        //            if (totalRatingUpTo[j] / (j+1) > maxRating[j])
        //            {
        //                maxRating[j] = totalRatingUpTo[j] / (j+1);
        //            }
        //        }

        //        totalRatingUpTo[index] = curTotal + curRating;
        //        maxRating[index] = totalRatingUpTo[index] / (index + 1);
        //        curTotal = totalRatingUpTo[index];

        //    }


        //    return maxRating;
        //}

        //public double GetPercentile100ForGame(int serverGameIndex, double myScore, int exclude = 1)
        //{
        //    serverGameIndex %= _levelDescriptions.Length;
        //    var results = GetUserResultsForGame(serverGameIndex);
        //    double lower = results.Select(GetFinalScore).Count(value => value < myScore);
        //    var total = results.Count - exclude;
        //    if (total == 0)
        //    {
        //        return 100;
        //    }
        //    return lower / total * 100.0;
        //}

        public double LoadPercentile100ForGame(double myScore, string folderName, int gameId)
        {
            var results = LoadScoresForGameFromFile(folderName, gameId);
            double lower = results.Select(GetFinalScore).Count(value => value < myScore);
            var total = results.Count - 1;
            if (total == 0)
            {
                return 100;
            }
            return lower / total * 100.0;
        }



        //public LevelDescription<TWellPoint, TRealizationData, TSecretState> GetScoreboard(int serverGameIndex)
        //{
        //    serverGameIndex %= _levelDescriptions.Length;
        //    var results = GetUserResultsForGame(serverGameIndex);
        //    var level = _levelDescriptions[serverGameIndex];
        //    level.UserResults = results;
        //    level.BotResult = GetBotResultForGame(serverGameIndex);
        //    return level;
        //}

        public double GetScorePercentForGame(int serverGameIndex, double myScore)
        {
            serverGameIndex %= _levelDescriptions.Length;
            var best = GetFinalScore(_levelDescriptions[serverGameIndex].BestPossible);
            return myScore / best * 100.0;
        }

        //public Dictionary<string, UserRating> GetAllRatings()
        //{
        //    var allResults = GetUserResultsForAllGames();
        //    var result = new Dictionary<string, UserRating>();
        //    foreach (var keyValuePair in allResults)
        //    {
        //        var userName = keyValuePair.Key.UserName;
        //        if (result.ContainsKey(userName))
        //        {
        //            continue;
        //        }

        //        var userResults = GetResultsForUser(allResults, userName).OrderBy(x => -x.Key.GameNumberForUser).ToList();
        //        var lastResultTicks = userResults.First().Value.TimeTicks;
        //        //var time0 = new DateTime(userResults[0].Value.TimeTicks);
        //        //var time1 = new DateTime(userResults[1].Value.TimeTicks);
        //        //var time2 = new DateTime(userResults[2].Value.TimeTicks);
        //        //var ourTime = new DateTime(lastResultTicks);
        //        var ratings = GetRating(userName, userResults);
        //        var rating = new UserRating()
        //        {
        //            Rating = ratings,
        //            TimeTicks = lastResultTicks,
        //            UserName = userName
        //        };
        //        result.Add(userName, rating);
        //    }

        //    return result;
        //}



        public string LoadFriendUserNameFromFile(string folderId)
        {
            var pair = LoadUserResultPairFromFile(folderId);
            if (pair.Value == null)
            {
                return null;
            }

            return pair.Key.UserName;
        }

        //public Dictionary<string, UserRating> GetAllRatings()
        //{
        //    return _fullServerStateGeocontrollerImplementation.GetAllRatings();
        //}

        private int GetServerGameIndex(int gameSeed)
        {
            var serverGameIndex = seeds.ToList().FindIndex(x => x == gameSeed);
            return serverGameIndex;
        }

        //TODO remove
        private MyScore LoadUserResultFromFileForGame(string folderId, int gameSeed)
        {
            var pair1 = LoadUserResultPairFromFile(folderId);
            var fgi = pair1.Key;
            var allScores = LoadAllScoresFromFile(folderId);
            var myScores = allScores.Where(x => (x.Key.UserName == fgi.UserName && x.Key.GameId == gameSeed)).ToList();
            if (myScores.Count <= 0)
            {
                return null;
            }
            var orderedScores = myScores.OrderBy(x => x.Key.GameNumberForUser);
            var trajValue = orderedScores.First();
            //TODO try loading correct game do not use value
            //try load score from current scores
            var serverGameIndex = seeds.ToList().FindIndex(x => x == gameSeed);
            if (serverGameIndex != -1 && serverGameIndex < TOTAL_LEVELS)
            {
                var valueScore = GetFinalScore(trajValue.Value);
                var score = new MyScore()
                {
                    ScorePercent = GetScorePercentForGame(serverGameIndex, valueScore),
                    ScoreValue = valueScore,
                    //TODO fix
                    //YouDidBetterThan = GetPercentile100ForGame(serverGameIndex, valueScore),
                    UserName = pair1.Key.UserName,
                };
                return score;
            }

            return null;


        }


        //protected void PushToResultingTrajectories(KeyValuePair<UserResultId, UserResultFinal<TWellPoint>> pair)
        //{
        //    _resultingTrajectories.AddOrUpdate(pair.Key, pair.Value,
        //        (key, value) => pair.Value);
        //    //_newResultingTrajectories.AddOrUpdate(pair.Key, pair.Value,
        //    //    (key, value) => pair.Value);

        //}


        public virtual TUserDataModel LossyCompress(TUserDataModel data)
        {
            return data;
        }

        public LevelDescription<TWellPoint, TRealizationData, TSecretState> GetScoreboard(int serverGameIndex)
        {
            //var onlineBoard = _fullServerStateGeocontrollerImplementation.GetScoreboard(serverGameIndex);
            var onlineBoard = _levelDescriptions[serverGameIndex];
            var pair = LoadAndCreateScoreBoardUserPair();
            var levelDict = pair.Item1;
            var offlineBoard = levelDict[seeds[serverGameIndex]];
            onlineBoard.UserResults = offlineBoard.UserResults;
            var botUser = pair.Item2[BotUserName];
            var botUserResultPair = botUser.First(x => x.Key.GameId == seeds[serverGameIndex]);
            onlineBoard.BotResult = botUserResultPair.Value;
            

            return onlineBoard;
        }

        public abstract void AddBotUserDefault();


        public bool UserExists(string userId)
        {
            var dirId = "resultLog/";
            if (!Directory.Exists(dirId))
            {
                Directory.CreateDirectory(dirId);
            }
            var allDirectories = Directory.GetDirectories(dirId);
            var userDirId = GetUserNameDirId(userId);
            if (allDirectories.FirstOrDefault(x => x.Contains(userDirId)) != null)
            {
                return true;
            }
            return false;
            //return _users.ContainsKey(userId);
        }

        public TUserDataModel GetUserDataDefault()
        {
            return _dummyUserData;
        }

        public TUserDataModel GetUserData(string userId)
        {
            //GetNewDefaultUserPair does not lock
            //seems to be locked only if update is locked
            var userData = _users.GetOrAdd(userId, GetNewDefaultUserPair)
                .UserDataLocked;
            return userData;
        }

        public TUserResult GetUserEvaluationData(string userId, IList<TWellPoint> trajectory)
        { 
            //this method does NOT lock
            var userPair = _users.GetOrAdd(userId, GetNewDefaultUserPair);
            var resultDistribution = userPair.GetEvalautionLocked(trajectory);
            //var scorePair = userPair.GetUserResultScorePairLocked(_levelDescriptions.Length);
            //PushToResultingTrajectories(scorePair);
            return resultDistribution;
        }


        //TODO update
        /// <summary>
        /// stopping
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="friendSaveId"></param>
        /// <returns></returns>
        public MyScore StopUser(string userId, string friendSaveId = null)
        {
            //this method does NOT lock
            var userPair = _users.GetOrAdd(userId, GetNewDefaultUserPair);
            var updatedUser = userPair.StopUserLocked(EvaluatorTruth, GetTruthsForEvaluation());
            KeyValuePair<UserResultId, UserResultFinal<TWellPoint>> pair;
            pair = userPair.GetUserResultScorePairLocked(_levelDescriptions.Length);
            var shareId = DumpUserResultToFileOnStop(pair);
            var serverGameIndex = pair.Key.GameId;
            var gameResultId = pair.Key;
            var userResult = pair.Value;

            MyScore myScore;
            //myScore = GetMyFullScore(pair.Key.GameId, GetFinalScore(pair.Value), userId, true);
            myScore = GetMyFullScore(gameResultId, userResult, friendsSaveId: friendSaveId);
            myScore.SharingId = shareId;
            //myScore.FriendsScore = friendsScore;
            //myScore.AiScore = aiScore;
            return myScore;
        }

        public int MoveUserToNewGame(string userId)
        {
            //this method does not lock
            var gameInd = _users.GetOrAdd(userId, GetNewDefaultUserPair)
                .MoveUserToNewGameLocked(EvaluatorTruth, GetTruthsForEvaluation());
            return gameInd;
        }

        public TUserDataModel UpdateUser(string userId, TWellPoint load = default)
        {
            //this method locks
            var userPair = _users.GetOrAdd(userId, GetNewDefaultUserPair);
            var updatedUser = userPair.UpdateUserLocked(load, _secrets, EvaluatorTruth, GetTruthsForEvaluation());
            KeyValuePair<UserResultId, UserResultFinal<TWellPoint>> pair;
            //pair = userPair.GetUserResultScorePairLocked(_levelDescriptions.Length);
            //PushToResultingTrajectories(pair);
            return updatedUser;
        }

    }
}
