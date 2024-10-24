﻿

using System;
using System.ComponentModel.Design;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;

namespace BattleShip
{
    class Program
    {
        //set boundries of the board
        public static int boardMin = 0;
        public static int boardMax = 10;
        public static int GameSpeed = 0;
        public static int GamesToWatch { get; set; } = 1;
        public static string LogFileName { get; set; }
        public static bool IsPlayer1Turn { get; set; } = false;
        public static bool IsPlayer1Human { get; set; } = true;
        public static bool IsGameOver { get; set; } = false;
        public static bool IsGameOn { get; set; } = false;
        public static bool WasLastAHitP1 { get; set; }
        public static bool WasLastAHitP2 { get; set; }
        public static string Player1Direction { get; set; }
        public static string Player2Direction { get; set; }
        public static (int, int) Player1LastHit { get; set; }
        public static (int,int) Player2LastHit { get; set; }

        //board borders to help player see what squares are which
        public static readonly string[] topBoardBorder = ["[1]", "[2]", "[3]", "[4]", "[5]", "[6]", "[7]", "[8]", "[9]", "[10]"];
        public static readonly string[] leftBoardBorder = ["A", "B", "C", "D", "E", "F", "G", "H", "I", "J"];

        //setup player boards
        public static int[,] player1Board { get; set; }

        public static int[,] player2Board { get; set; }
        public static  void Main(string[] args)
        {
            Thread gameThread = new Thread(NewThread);
            while (GamesToWatch > 0)
            {
                if (!gameThread.IsAlive && !IsGameOn)
                {
                    gameThread = new(new ThreadStart(NewThread));
                    IsGameOn = true;
                    gameThread.Start();
                }
            }
        }
        public static void NewThread()
        {
            if (GamesToWatch > 0)
            {
                //Set all values to base to reset the game each iteration
                IsPlayer1Turn = false;
                WasLastAHitP1 = false;
                WasLastAHitP2 = false;
                IsGameOver = false;

                LogFileName = $"Battleship_Log_{DateTime.Now.ToString("yyyyMMdd_hhmmss")}.txt";

                string[] shipTypes = { "Carrier", "Battleship", "Destroyer", "Submarine", "Gunboat" };
                //setup arrays that can be incremented over to search for coordinates and houses bools for each spot 
                //these will be created new each new game
                var player1Ships = new[] {
                (ShipType: "Carrier", Coords: new List<(int, int)>(), IsHit: new bool[5], IsSunk: true),
                (ShipType: "Battleship", Coords: new List<(int, int)>(), IsHit: new bool[4], IsSunk: true),
                (ShipType: "Destroyer", Coords: new List<(int, int)>(), IsHit: new bool[3], IsSunk: true),
                (ShipType: "Submarine", Coords: new List<(int, int)>(), IsHit: new bool[3], IsSunk: true),
                (ShipType: "Gunboat", Coords: new List<(int, int)>(), IsHit: new bool[2], IsSunk: true),
                };
                var player2Ships = new[] {
                (ShipType: "Carrier", Coords: new List<(int, int)>(), IsHit: new bool[5], IsSunk: true),
                (ShipType: "Battleship", Coords: new List<(int, int)>(), IsHit: new bool[4], IsSunk: true),
                (ShipType: "Destroyer", Coords: new List<(int, int)>(), IsHit: new bool[3], IsSunk: true),
                (ShipType: "Submarine", Coords: new List<(int, int)>(), IsHit: new bool[3], IsSunk: true),
                (ShipType: "Gunboat", Coords: new List<(int, int)>(), IsHit: new bool[2], IsSunk: true),
                };
                //setup arrays to house player turn information for storing into a file, this inclused turn count
                //amount of hits, amount of misses and player name
                var player1Stats = (
                    Name: "",
                    TurnsTaken: 0,
                    Hits: 0,
                    Misses: 0
                    );
                var player2Stats = (
                    Name: "",
                    TurnsTaken: 0,
                    Hits: 0,
                    Misses: 0
                    );

                //setup array to handle the board
                player1Board = new int[10, 10];
                player2Board = new int[10, 10];

                
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(@" ____   __  ____  ____  __    ____  ____  _  _  __  ____");
                Console.WriteLine(@"(  _ \ / _\(_  _)(_  _)(  )  (  __)/ ___)/ )( \(  )(  _ \");
                Console.WriteLine(@" ) _ (/    \ )(    )(  / (_/\ ) _) \___ \) __ ( )(  ) __/");
                Console.WriteLine(@"(____/\_/\_/(__)  (__) \____/(____)(____/\_)(_/(__)(__)  ");
                //see if player wants to play or if they just want to watch 2 computers fight it out
                

                //check answer of player
                while (true && IsPlayer1Human)
                {
                    LogData($"Would you like to Play?: ", 2);
                    string isPlayerPlaying = LogData(Console.ReadLine(), 3).Trim().ToLower();
                    
                    while (GameSpeed == 0)
                    {
                        LogData("How fast would you like the game? slow, medium or fast: ", 2);
                        string gameSpeed = LogData(Console.ReadLine(), 3).Trim().ToLower();
                        switch (gameSpeed)
                        {
                            case "slow":
                                GameSpeed = 5000; break;
                            case "medium":
                                GameSpeed = 2500; break;
                            case "fast":
                                GameSpeed = 1000; break;
                            default:
                                LogData("I didn't quite understand Please try again:", 1);
                                break;
                        }
                    }
                    if (isPlayerPlaying.Equals("yes") || isPlayerPlaying.Equals("y"))
                    {
                        
                        LogData($"What is your name human?: ", 2);
                        player1Stats.Name = LogData(Console.ReadLine().Trim(), 3);
                        player2Stats.Name = "Computer Player";
                        IsPlayer1Turn = true;
                        ComputerSetup(shipTypes, player2Ships, player2Board);
                        SetupPhase(shipTypes, player1Ships);
                        break;
                    }
                    else if (isPlayerPlaying.Equals("no") || isPlayerPlaying.Equals("n"))
                    {

                        LogData("How many games should the computers play?: ", 2);
                        int tempMatchs = 0;
                        do {
                            string matchsToPlay = LogData(Console.ReadLine().Trim(), 3);
                            
                            if (int.TryParse(matchsToPlay, out tempMatchs) && tempMatchs > 1)
                            {
                                Console.WriteLine(tempMatchs);
                                GamesToWatch = tempMatchs;
                            }
                            else if (tempMatchs <= 0)
                            {
                                LogData("You cant pick a value lower than 1",1);
                                LogData("Please try again: ", 2);
                                tempMatchs = 0;
                            }
                            else
                            {
                                tempMatchs = 0;
                                LogData("Please try again: ", 2);
                            }
                        }
                        while (tempMatchs == 0);
                        IsPlayer1Human = false;
                        break;
                    }
                    else
                    {
                        LogData("I didn't understand that, would you like to play???: ", 2);
                        isPlayerPlaying = LogData(Console.ReadLine(), 3).Trim().ToLower();
                    }
                }
                if (!IsPlayer1Human)
                {
                    IsPlayer1Turn = true;
                    player1Stats.Name = "Computer Player 1";
                    player2Stats.Name = "Computer Player 2";
                    ComputerSetup(shipTypes, player1Ships, player1Board);
                    ComputerSetup(shipTypes, player2Ships, player2Board);
                }



                while (!IsGameOver)
                {
                    TakeTurns(player1Ships, player2Ships, ref player1Stats);
                    TakeTurns(player1Ships, player2Ships, ref player2Stats);
                    //TakeTurns(player1Ships, player2Ships, player1Stats, player2Stats);
                }
                GamesToWatch = GamesToWatch - 1;
                IsGameOn = false;
            }
        }


        //Method to doo the setup with the player
        static void SetupPhase(string[] shipnames, (string, List<(int, int)>, bool[], bool)[] playerShips)
        {

            //ask players name and ask where they want their ships, this will call SetShipLocation to setup the ship
            for (int i = 0; i < shipnames.Length; i++)
            {
                (int, int)? playerResponse = null;
                string? hOrVResponse = null;
                PrintBoard(player1Board);
                //loop for picking which direction to place the ship
                do
                {
                    LogData($"Would you like your {shipnames[i]} horizontal or vertical?: ", 2);
                    hOrVResponse = LogData(Console.ReadLine().ToLower().Trim(), 3);
                    if (hOrVResponse.Length > 1 || string.IsNullOrEmpty(hOrVResponse) || !char.IsLetter(hOrVResponse[0]))
                    {
                        LogData("Please enter either h or v.", 1);
                        hOrVResponse = null;
                    }
                    else if (hOrVResponse != "v" && hOrVResponse != "h")
                    {
                        LogData("Please enter either h or v.", 1);
                        hOrVResponse = null;
                    }
                }
                while (hOrVResponse == null);
                do
                {
                    //ask player where they would like their ship and check if entry is valid
                    LogData($"Where would you like to place your {shipnames[i]}?:", 2);
                    playerResponse = CheckResponse(LogData(Console.ReadLine().Trim(), 3));
                    if (playerResponse != null)
                    {
                        SetShipLocation(hOrVResponse, ((int, int))playerResponse, playerShips, i, player1Board);
                    }
                }
                while (playerShips[i].Item4 == true);
            }
        }

        /// method to check user feed back on squares, this will be used for set up and for attacking
        static (int, int)? CheckResponse(string response)
        {
            //Console.WriteLine((response[0] - 'a', int.Parse(char.ToString(response[1])) - 1));
            //check response to see if it is a valid, checking for length of response and if its null, checking first char is letter and 2nd is a digit
            if ((response.Length > 3 || response.Length < 2))
            {
                
                LogData("Please enter a valid square in the format of [A1]", 1);
                return null;
            }
            else if (string.IsNullOrEmpty(response) || !char.IsLetter(response[0]) || !char.IsDigit(response.Substring(1)[0]))
            {
                LogData("Please enter a valid square in the format of [A1]", 1);
                return null;
            }
            else if (response[0] - 'a' > boardMax || response[0] - 'a' < boardMin || int.Parse(char.ToString(response[1])) - 1 > boardMax || int.Parse(char.ToString(response[1])) - 1 < boardMin)
            {
                LogData("That square is outside the bounds of the board, please try again.", 1);
                return null;
            }
            else if (response.Length == 3)
            {
                if (int.Parse(char.ToString(response[2])) > 0)
                {
                    LogData("That square is outside the boundry of the board", 1);
                    return null;
                }
                int letterIndex = response[0] - 'a';
                var tuplePeg = (9, letterIndex);
                Console.WriteLine(tuplePeg);
                return tuplePeg;
            }
            else if (response.Length == 2)
            {
                //get index of letter given to target proper row
                int letterIndex = response[0] - 'a';
                var tuplePeg = (int.Parse(char.ToString(response[1])) - 1, letterIndex);
                Console.WriteLine(tuplePeg);
                return tuplePeg;
            }
            else
            {
                //get index of letter given to target proper row
                int letterIndex = response[0] - 'a';
                var tuplePeg = (int.Parse(response.Substring(1)) - 1, letterIndex);
                Console.WriteLine(tuplePeg);
                return tuplePeg;
            }
        }


        /// <summary>
        /// Handle how the computer sets up their board
        /// </summary>
        static void ComputerSetup(string[] shipNames, (string, List<(int, int)>, bool[], bool)[] shipTuple, int[,] playerBoardToSetup)
        {
            Random random = new Random();
            string[] computerHOrVString = ["h", "v"];
            (int, int) computerTargetCoord;
            for (int i = 0; i < shipTuple.Length; i++)
            {
                string HOrV = computerHOrVString[random.Next(computerHOrVString.Length)];
                do
                {
                    PrintBoard(playerBoardToSetup);
                    computerTargetCoord = (random.Next(0, 9), random.Next(0, 9));
                    SetShipLocation(HOrV, computerTargetCoord, shipTuple, i, playerBoardToSetup);
                }
                while (shipTuple[i].Item4 == true);
                LogData($"The computer has placed their {shipNames[i]}.", 1);
                System.Threading.Thread.Sleep(GameSpeed);
            }
        }

        /*method that will check placement given by the user, confirm it is a valid placement
        and set the coords in the array*/
        static (string, List<(int, int)>, bool[], bool)[] SetShipLocation(string orientation, (int, int) targetCoord, (string, List<(int, int)>, bool[], bool)[] shipToSetup, int shipNumber, int[,] playerBoard)
        {
            //make tuple to hold temp values incase checks fail and placement is not valid
            //return temp is all checks pass, else pass old values back
            (string, List<(int, int)>, bool[], bool)[] tempTuple = shipToSetup;
            //create an list that will contain the coords for the ship location
            List<(int, int)> tempCoordsList = new();
            switch (orientation)
            {
                case "h":
                    int targetHorEndOfship = targetCoord.Item1 + tempTuple[shipNumber].Item3.Length;
                    if (targetHorEndOfship <= boardMax)
                    {
                        //check if targetcoord is inside the boundries of the board(including the size of the ship)
                        if (targetCoord.Item1 <= (boardMax - shipToSetup[shipNumber].Item3.Length) && targetCoord.Item1 >= boardMin && targetCoord.Item2 <= boardMax && targetCoord.Item2 >= boardMin)
                        {
                            for (int i = 0; i < shipToSetup[shipNumber].Item3.Length; i++)
                            {
                                //check all other ships to see if current coordinate will intersect with any of them
                                foreach (var ship in shipToSetup)
                                {
                                    if (ship.Item2.Contains((targetCoord.Item1 + i, targetCoord.Item2)) && ship.Item1 != shipToSetup[shipNumber].Item1)
                                    {
                                        LogData($"Your {shipToSetup[shipNumber].Item1.ToString()} can't be placed there.", 1);
                                        return shipToSetup;
                                    }
                                }
                                tempCoordsList.Add((targetCoord.Item1 + i, targetCoord.Item2));
                            }
                        }
                        //update value in array of tuples to have new coordinates, set IsSunk to false to show that the ship is now afloat
                        for (int i = 0;i < tempCoordsList.Count;i++)
                        {
                            playerBoard[tempCoordsList[i].Item1, tempCoordsList[i].Item2] = 3;
                        }
                        tempTuple[shipNumber] = (tempTuple[shipNumber].Item1, tempCoordsList, tempTuple[shipNumber].Item3, false);
                    }
                    else
                    {
                        LogData("Your ship can't be placed there, try again.", 1);
                    }
                    break;
                case "v":
                    int targetVertEndOfship = targetCoord.Item2 + tempTuple[shipNumber].Item3.Length;
                    if (targetVertEndOfship <= boardMax)
                    {
                        if ((targetCoord.Item2 + shipToSetup[shipNumber].Item3.Length) <= boardMax)
                        {
                            //copy h case except handle cases for vertical placement
                            if (targetCoord.Item2 <= (boardMax - shipToSetup[shipNumber].Item3.Length) && targetCoord.Item2 >= boardMin && targetCoord.Item1 <= boardMax && targetCoord.Item1 >= boardMin)
                            {
                                for (int i = 0; i < shipToSetup[shipNumber].Item3.Length; i++)
                                {
                                    //check all other ships to see if current coordinate will intersect with any of them
                                    foreach (var ship in shipToSetup)
                                    {
                                        if (ship.Item2.Contains((targetCoord.Item1, targetCoord.Item2 + i)) && ship.Item1 != shipToSetup[shipNumber].Item1)
                                        {
                                            LogData($"Your {shipToSetup[shipNumber].Item1.ToString()} can't be placed there.", 1);
                                            return shipToSetup;
                                        }
                                    }
                                    tempCoordsList.Add((targetCoord.Item1, targetCoord.Item2 + i));
                                }
                            }
                            for (int i = 0; i < tempCoordsList.Count; i++)
                            {
                                playerBoard[tempCoordsList[i].Item1, tempCoordsList[i].Item2] = 3;
                            }
                            tempTuple[shipNumber] = (tempTuple[shipNumber].Item1, tempCoordsList, tempTuple[shipNumber].Item3, false);
                        }
                    }
                    else
                    {
                        LogData("Your ship can't be placed there, try again.", 1);
                    }
                    break;
            }
            return tempTuple;
        }

        /// <summary>
        /// Controls how turns take place  and handles switching turns between players
        /// also logs data for turns and keeps track of turn info
        /// </summary>
        static void TakeTurns((string, List<(int, int)>, bool[], bool)[] player1ShipArray, (string, List<(int, int)>, bool[], bool)[] player2ShipArray, ref (string, int, int, int) playerStats)
        {
            
            while (IsPlayer1Turn && !IsGameOver)
            {
                if (IsPlayer1Human)
                {
                    PrintBoard(player2Board);
                    (int,int)? player1Response = null;
                    LogData($"It is {playerStats.Item1}'s turn! they are on turn {playerStats.Item2}! their hit to miss ratio is {playerStats.Item3}/{playerStats.Item4}.", 3);
                    do
                    {
                        LogData($"What square would you like to attack?: ", 2);
                        player1Response = CheckResponse(LogData(Console.ReadLine().Trim().ToLower(), 1));
                        if (player1Response != null)
                        {
                            (int, int) tempIntTuple = ((int,int))player1Response;
                            if (player2Board[tempIntTuple.Item1, tempIntTuple.Item2] == 1 || player2Board[tempIntTuple.Item1, tempIntTuple.Item2] == 2)
                            {
                                LogData("You already attacked that space, please try again.", 1);
                                player1Response = null;
                            }
                        }
                    }
                    while (player1Response == null);
                    do
                    {
                        
                        (bool, string) targetSpaceReturnValues = CheckTargetSquare(((int, int))player1Response, player2Board, player2ShipArray);
                        if (targetSpaceReturnValues.Item1 == true)
                        {
                            LogData($"Its a hit at {player1Response.ToString()}! you hit their {targetSpaceReturnValues.Item2}.", 1);
                            System.Threading.Thread.Sleep(GameSpeed);
                            UpdateBoard(player2Board, ((int, int))player1Response);
                            PrintBoard(player2Board);
                            playerStats.Item2++;
                            playerStats.Item3++;
                            LogData(playerStats.ToString(), 3);
                            CheckForGameover(player2ShipArray, playerStats);
                            
                        }
                        else if (targetSpaceReturnValues.Item1 == false)
                        {
                            LogData("Swing and a miss! Player 2's turn!", 1);
                            System.Threading.Thread.Sleep(GameSpeed);
                            UpdateBoard(player2Board, ((int, int))player1Response);
                            PrintBoard(player2Board);
                            playerStats.Item2++;
                            playerStats.Item4++;
                            LogData(playerStats.ToString(), 3);
                            IsPlayer1Turn = false;
                            
                        }
                    }
                    while (player1Response == null);
                }
                else
                {
                    LogData($"It is {playerStats.Item1}'s turn! they are on turn {playerStats.Item2}! their hit to miss ratio is {playerStats.Item3}/{playerStats.Item4}.", 3);
                    PrintBoard(player2Board);
                    (int, int)? previousTarget;
                    (int, int)? player1Response = (ComputerLogic());
                    (bool, string) targetSpaceReturnValues = CheckTargetSquare(((int, int))player1Response, player2Board, player2ShipArray);

                    
                    if (targetSpaceReturnValues.Item1 == true)
                    {
                        previousTarget = player1Response;
                        UpdateBoard(player2Board, ((int, int))player1Response);
                        PrintBoard(player2Board);
                        LogData($"Player 1 attacks {leftBoardBorder[player1Response.Value.Item2]}{player1Response.Value.Item1 + 1}", 1);
                        LogData($"Its a hit at {leftBoardBorder[player1Response.Value.Item2]}{player1Response.Value.Item1 + 1}! you hit their {targetSpaceReturnValues.Item2}.", 1);
                        System.Threading.Thread.Sleep(GameSpeed);
                        WasLastAHitP1 = true;
                        Player1LastHit = ((int, int))player1Response;
                        playerStats.Item2++;
                        playerStats.Item3++;
                        LogData(playerStats.ToString(), 3);
                        CheckForGameover(player2ShipArray, playerStats);
                        
                    }
                    else if (targetSpaceReturnValues.Item1 == false)
                    {
                        UpdateBoard(player2Board, ((int, int))player1Response);
                        PrintBoard(player2Board);
                        LogData($"Player 1 attacks {leftBoardBorder[player1Response.Value.Item2]}{player1Response.Value.Item1 + 1}", 1);
                        LogData("Swing and a miss! Player 2's turn!", 1);
                        playerStats.Item2++;
                        playerStats.Item4++;
                        LogData(playerStats.ToString(), 3);
                        System.Threading.Thread.Sleep(GameSpeed);
                        WasLastAHitP1 = false;
                        IsPlayer1Turn = false;
                        
                    }
                }
            }
            while (!IsPlayer1Turn && !IsGameOver)
            {
                LogData($"It is {playerStats.Item1}'s turn! they are on turn {playerStats.Item2}! their hit to miss ratio is {playerStats.Item3}/{playerStats.Item4}.", 3);
                PrintBoard(player1Board);
                (int, int)? previousTarget;
                (int, int)? player2Response = (ComputerLogic());
                (bool, string) targetSpaceReturnValues = CheckTargetSquare(((int, int))player2Response, player1Board, player1ShipArray);


                if (targetSpaceReturnValues.Item1 == true)
                {
                    previousTarget = player2Response;
                    UpdateBoard(player1Board, ((int, int))player2Response);
                    PrintBoard(player1Board);
                    LogData($"Player 2 attacks {leftBoardBorder[player2Response.Value.Item2]}{player2Response.Value.Item1 + 1}", 1);
                    LogData($"Its a hit at {leftBoardBorder[player2Response.Value.Item2]}{player2Response.Value.Item1 + 1}! you hit their {targetSpaceReturnValues.Item2}.", 1);
                    System.Threading.Thread.Sleep(GameSpeed);
                    WasLastAHitP2 = true;
                    Player2LastHit = ((int, int))player2Response;
                    playerStats.Item2++;
                    playerStats.Item3++;
                    LogData(playerStats.ToString(), 3);
                    CheckForGameover(player1ShipArray, playerStats);

                }
                else if (targetSpaceReturnValues.Item1 == false)
                {
                    UpdateBoard(player1Board, ((int, int))player2Response);
                    PrintBoard(player1Board);
                    LogData($"Player 2 attacks {leftBoardBorder[player2Response.Value.Item2]}{player2Response.Value.Item1 + 1}", 1);
                    LogData("Swing and a miss! Player 1's turn!", 1);
                    System.Threading.Thread.Sleep(GameSpeed);
                    playerStats.Item2++;
                    playerStats.Item4++;
                    LogData(playerStats.ToString(), 3);
                    WasLastAHitP2 = false;
                    IsPlayer1Turn = true;

                }
            }  
        }

        static void PrintBoard(int[,] boardToDraw)
        {
            //print out the board between turns to show the progression of the game
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(@" ____   __  ____  ____  __    ____  ____  _  _  __  ____");
            Console.WriteLine(@"(  _ \ / _\(_  _)(_  _)(  )  (  __)/ ___)/ )( \(  )(  _ \");
            Console.WriteLine(@" ) _ (/    \ )(    )(  / (_/\ ) _) \___ \) __ ( )(  ) __/");
            Console.WriteLine(@"(____/\_/\_/(__)  (__) \____/(____)(____/\_)(_/(__)(__)  ");
            if (IsPlayer1Turn)
            { Console.Write("[P1]");
            }
            else
            {
                Console.Write("[P2]");
            }
            for (int i = 0; i < 10; i++)
            {
                if (i < 9)
                {
                    Console.Write($"[{i + 1}] ");
                }
                else { Console.WriteLine($"[{i + 1}] "); }

            }
            for (int i = 0; i < 10; i++) //handles new row
            {
                if (i < 9)
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write($"[{leftBoardBorder[i]}] ");
                }
                else { Console.ForegroundColor = ConsoleColor.Gray; Console.Write($"[{leftBoardBorder[i]}] "); }

                for (int j = 0; j < 10; j++)
                {
                    if (j < 9)
                    {

                        Console.Write(SymbolCheck(boardToDraw[j, i]));
                    }
                    else
                    {
                        Console.WriteLine(SymbolCheck(boardToDraw[j, i]));
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                }
            }
        }

        //set values in the board to the proper value after a player selects a cell
        static void UpdateBoard(int[,] selectedBoard, (int, int) target)
        {
            //check if values are inside the bounds
            if (0 <= target.Item1 && target.Item2 <= 9 && 0 <= target.Item1 && target.Item2 <= 9)
            {
                bool viableTarget;
                if (selectedBoard[target.Item1, target.Item2] == 0 || selectedBoard[target.Item1, target.Item2] == 3)
                {
                    viableTarget = true;
                }
                else
                {
                    viableTarget = false;
                }
                if (viableTarget)
                {
                    //check if value was a hit or miss
                    if (selectedBoard[target.Item1, target.Item2] == 3)
                    {
                        selectedBoard[target.Item1, target.Item2] = 2;
                    }
                    else
                    {
                        selectedBoard[target.Item1, target.Item2] = 1;

                    }
                }
            }
        }

        //handle printing the correct symbol on the board
        static string SymbolCheck(int arrayValue)
        {
            if (arrayValue == 0)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                return ("[_] ");
            }
            else if (arrayValue == 1)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                return ("[0] ");
            }
            else if (arrayValue == 2)
            {
                Console.ForegroundColor= ConsoleColor.Red;
                return ("[X] ");
            }
            else if (arrayValue == 3)
            {
                if (IsPlayer1Turn && IsPlayer1Human) //hide the enemies ships
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return ("[_] ");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    return ("[V] ");
                }
            }
            else Console.ForegroundColor = ConsoleColor.Gray;return ("[_] ");
        }


        /// <summary>
        /// Controls computer logic, it can check the player board but can't cheat, it will only look for values thatdesignate a hit square
        /// looking for only hit spots will help not run into problems when 2 computers are fighting
        /// it will target near squares to recent hits to attempt to search out the player ships and act smart like other players would
        /// </summary>
        /// <returns></returns>
        static (int, int)? ComputerLogic()
        {
            if (WasLastAHitP1 || WasLastAHitP2) //if a hit was successful, target nearby ranges
            {
                Random random = new Random();
                string[] directions = ["North", "South", "East", "West"];
                if (IsPlayer1Turn)
                {
                    int i = 0;
                    (int, int) newTarget = Player1LastHit;
                    Player1Direction = directions[random.Next(directions.Length)];
                    
                    do
                    {
                        
                        newTarget = MoveDirection(newTarget, Player1Direction);
                        if (player2Board[newTarget.Item1, newTarget.Item2] == 1 || player2Board[newTarget.Item1, newTarget.Item2] == 2)
                        {
                            i++;
                            if (i>5)
                            {
                                break;
                            }
                            newTarget = Player1LastHit;
                        }
                        
                    }
                    while (newTarget == Player1LastHit);
                    return (newTarget);
                }
                else
                {
                    int i = 0;
                    (int, int) newTarget = Player2LastHit;
                    Player2Direction = directions[random.Next(directions.Length)];
                    do
                    {
                        
                        newTarget = (MoveDirection(newTarget, Player2Direction));
                        if (player1Board[newTarget.Item1, newTarget.Item2] == 1 || player1Board[newTarget.Item1, newTarget.Item2] == 2)
                        {
                            i++;
                            if (i > 5)
                            {
                                break;
                            }
                            newTarget = Player2LastHit;
                        }
                    }
                    while (newTarget == Player2LastHit);
                    return (newTarget);
                }
            }
            else
            {
                Random random = new Random();
                (int, int) newTarget;
                if (IsPlayer1Turn) {
                    do
                    {
                        newTarget = (random.Next(0, 10), random.Next(0, 10));
                    }
                    while (player2Board[newTarget.Item1, newTarget.Item2] == 1 || player2Board[newTarget.Item1, newTarget.Item2] == 2);
                }
                else
                {
                    do
                    {
                        newTarget = (random.Next(0, 10), random.Next(0, 10));
                    }
                    while ((player1Board[newTarget.Item1, newTarget.Item2] == 1 || player1Board[newTarget.Item1, newTarget.Item2] == 2));
                }
                return (newTarget);
            }
        }

        /// <summary>
        /// check the target square and return true if it is  ahit and what ship was hit
        /// if not a hit returns false and an empty string
        /// </summary>
        /// <param name="target"></param>
        /// <param name="targetBoard"></param>
        /// <param name="playerShips"></param>
        /// <returns></returns>
        static (bool, string) CheckTargetSquare((int, int) target, int[,] targetBoard, (string, List<(int, int)>, bool[], bool)[] playerShips)
        {

            int shipCount = 0;
            foreach (var ship in playerShips)
            {
                if (ship.Item2.Contains(target))
                {
                    for (int i = 0; i < ship.Item2.Count; i++)
                    {
                        if (ship.Item2[i] == target)
                        {
                            ship.Item3[i] = true;
                            if (ship.Item3.All(item => item))
                            {
                                playerShips[shipCount].Item4 = true;
                                LogData($"you sunk their {playerShips[shipCount].Item4}",1);
                            }
                            return (true, ship.Item1);
                        }
                    }
                }
                shipCount++;

            }
            return (false, "");
        }

        /// <summary>
        /// Logs data to the output file , this logs the setup phase and every turn of play
        /// 1 writes new line, 2 writes without new line, 3 just logs newline
        /// </summary>
        /// <param name="args"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        static string LogData(string args, int line)
        {
            using (StreamWriter logTurns = File.AppendText(LogFileName))
            {
                if (line == 1) //needs new line
                {
                    Console.WriteLine(args);
                    logTurns.WriteLine(args);
                    return args;
                }
                else if (line == 2) //does not need new line
                {
                    Console.Write(args);
                    logTurns.Write(args);
                    return args;
                }
                else //just needs value logged on same line, no output
                {
                    logTurns.WriteLine(args);
                    return args;
                }
            }
        }

        /// <summary>
        /// Check over the players ships and see if all of them are sunk, if they are announce the winner and reset the game
        /// </summary>
        /// <param name="playerShips"></param>
        static void CheckForGameover((string, List<(int, int)>, bool[], bool)[] playerShips, (string, int, int, int) playerstats)
        {
            bool[] shipIsSunkBoolArray = new bool[playerShips.Length];
            for (int i = 0; i < playerShips.Length; i++)
            {
                shipIsSunkBoolArray[i] = playerShips[i].Item4;
            }
            bool checkShipsAreAllSunk = shipIsSunkBoolArray.All(item => item);
            if (checkShipsAreAllSunk)
            {
                IsGameOver = true;
                double hitPercent = playerstats.Item3 / playerstats.Item4 * 100;
                LogData($"{playerstats.Item1} Has won! they did this in {playerstats.Item2} turns with a hit% of {hitPercent}%", 2);
            }


        }

        static (int,int) MoveDirection((int,int) oldTarget, string direction)
        {
            switch (direction)
            {
                case "North":
                    if (oldTarget.Item2 == 0)
                    { MoveDirection(oldTarget, "South"); }
                    else {oldTarget.Item2 -= 1; }
                    break;
                case "South":
                    if (oldTarget.Item2 == 9)
                    { MoveDirection(oldTarget, "North"); }
                    else { oldTarget.Item2 += 1; }
                    break;
                case "East":
                    if (oldTarget.Item1 == 9)
                    { MoveDirection(oldTarget, "West"); }
                    else { oldTarget.Item1 += 1; }
                    break;
                case "West":
                    if (oldTarget.Item1 == 0)
                    { MoveDirection(oldTarget, "East"); }
                    else { oldTarget.Item1 -= 1; }
                    break;
            }
            return (oldTarget);
        }
    }
}