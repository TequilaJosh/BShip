

using System;
using System.ComponentModel.Design;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace BattleShip
{
    class Program
    {
        //set boundries of the board
        public static int boardMin = 0;
        public static int boardMax = 10;
        public static string LogFileName { get; set; }
        public static bool IsPlayer1Turn { get; set; }
        public static bool IsPlayer1Human { get; set; }
        public static bool IsGameOver { get; set; }
        public static bool WasLastAHit { get; set; }
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
        public static void Main(string[] args)
        {
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
            LogData($"Would you like to Play?: ", 2);
            string isPlayerPlaying = LogData(Console.ReadLine(), 3).Trim().ToLower();

            //check answer of player
            while (true)
            {
                if(isPlayerPlaying.Equals("yes"))
                {
                    ComputerSetup(shipTypes, player2Ships);
                    SetupPhase(shipTypes, player1Ships);
                    break;
                }
                else if (isPlayerPlaying.Equals("no"))
                {
                    ComputerSetup(shipTypes, player1Ships);
                    ComputerSetup(shipTypes, player2Ships);
                    break ;
                }
                else
                {
                    LogData("I didn't understand that, would you like to play???: ",1);
                    isPlayerPlaying = LogData(Console.ReadLine(),3).Trim().ToLower();
                }
            }

            
            IsPlayer1Turn = true;
            while (!IsGameOver)
            {
                
                TakeTurns(player1Ships, player2Ships);
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
        static void ComputerSetup(string[] shipNames, (string, List<(int, int)>, bool[], bool)[] shipTuple)
        {
            Random random = new Random();
            string[] computerHOrVString = ["h", "v"];
            (int, int) computerTargetCoord;
            for (int i = 0; i < shipTuple.Length; i++)
            {
                string HOrV = computerHOrVString[random.Next(computerHOrVString.Length)];
                do
                {
                    PrintBoard(player1Board);
                    computerTargetCoord = (random.Next(0, 9), random.Next(0, 9));
                    SetShipLocation(HOrV, computerTargetCoord, shipTuple, i, player2Board);
                }
                while (shipTuple[i].Item4 == true);
                LogData($"The computer has placed their  {shipNames[i]}.", 1);
                System.Threading.Thread.Sleep(2500);
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
                                    if (ship.Item2.Contains((targetCoord.Item1, targetCoord.Item2 + i)) && ship.Item1 != shipToSetup[shipNumber].Item1)
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
        /// </summary>
        static void TakeTurns((string, List<(int, int)>, bool[], bool)[] player1ShipArray, (string, List<(int, int)>, bool[], bool)[] player2ShipArray)
        {
            while (IsPlayer1Turn)
            {
                if (IsPlayer1Human)
                {
                    PrintBoard(player2Board);
                    LogData($"What square would you like to attack?: ", 2);
                    (int, int)? player1Response = CheckResponse(LogData(Console.ReadLine(), 1));
                    do
                    {
                        (bool, string) targetSpaceReturnValues = CheckTargetSquare(((int, int))player1Response, player2Board, player2ShipArray);
                        if (targetSpaceReturnValues.Item1 == true)
                        {
                            LogData($"Its a hit! you hit their {targetSpaceReturnValues.Item2}.", 1);
                            UpdateBoard(player2Board, ((int, int))player1Response);
                            PrintBoard(player2Board);
                            CheckForGameover(player2ShipArray);
                        }
                        else if (targetSpaceReturnValues.Item1 == false)
                        {
                            LogData("Swing and a miss! Player 2's turn!", 1);
                            UpdateBoard(player2Board, ((int, int))player1Response);
                            PrintBoard(player2Board);
                            IsPlayer1Turn = false;
                        }
                    }
                    while (player1Response == null);
                }
                else
                {
                    PrintBoard(player2Board);
                    (int, int)? previousTarget;
                    (int, int)? player1Response = (ComputerLogic());
                    (bool, string) targetSpaceReturnValues = CheckTargetSquare(((int, int))player1Response, player2Board, player2ShipArray);

                    LogData($"Player 1 attacks {player1Response}", 2);
                    if (targetSpaceReturnValues.Item1 == true)
                    {
                        previousTarget = player1Response;
                        LogData($"Its a hit! you hit their {targetSpaceReturnValues.Item2}.", 1);
                        UpdateBoard(player1Board, ((int, int))player1Response);
                        WasLastAHit = true;
                        Player1LastHit = ((int, int))player1Response;
                        PrintBoard(player2Board);
                        CheckForGameover(player2ShipArray);
                    }
                    else if (targetSpaceReturnValues.Item1 == false)
                    {
                        LogData("Swing and a miss! Player 2's turn!", 1);
                        UpdateBoard(player2Board, ((int, int))player1Response);
                        PrintBoard(player2Board);
                        WasLastAHit = false;
                        IsPlayer1Turn = true;
                    }
                }
            }
            while (!IsPlayer1Turn)
            {
                PrintBoard(player1Board);
                (int, int)? previousTarget;
                (int, int)? player2Response = (ComputerLogic());
                (bool, string) targetSpaceReturnValues = CheckTargetSquare(((int, int))player2Response, player1Board, player1ShipArray);

                LogData($"Player 2 attacks {player2Response}", 2);
                if (targetSpaceReturnValues.Item1 == true)
                {
                    previousTarget = player2Response;
                    LogData($"Its a hit! you hit their {targetSpaceReturnValues.Item2}.", 1);
                    UpdateBoard(player1Board, ((int, int))player2Response);
                    WasLastAHit = true;
                    Player2LastHit = ((int,int))player2Response;
                    PrintBoard(player1Board);
                    CheckForGameover(player1ShipArray);
                }
                else if (targetSpaceReturnValues.Item1 == false)
                {
                    LogData("Swing and a miss! Player 2's turn!", 1);
                    UpdateBoard(player1Board, ((int, int))player2Response);
                    PrintBoard(player1Board);
                    WasLastAHit = false;
                    IsPlayer1Turn = true;
                }
            }
        }

        static void PrintBoard(int[,] boardToDraw)
        {
            //print out the board between turns to show the progression of the game
            Console.Clear();
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
                    Console.Write($"[{leftBoardBorder[i]}] ");
                }
                else { Console.Write($"[{leftBoardBorder[i]}] "); }

                for (int j = 0; j < 10; j++)
                {
                    if (j < 9)
                    {

                        Console.Write(SymbolCheck(boardToDraw[j, i]));
                    }
                    else
                    {
                        Console.WriteLine(SymbolCheck(boardToDraw[j, i]));

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
                return ("[_] ");
            }
            else if (arrayValue == 1)
            {
                return ("[0] ");
            }
            else if (arrayValue == 2)
            {
                return ("[X] ");
            }
            else if (arrayValue == 3)
            {
                if (IsPlayer1Turn) //hide the enemies ships
                {
                    return ("[_] ");
                }
                else
                {
                    return ("[V] ");
                }
            }
            else return ("[_] ");
        }


        /// <summary>
        /// Controls computer logic, it can check the player board but can't cheat, it will only look for values thatdesignate a hit square
        /// looking for only hit spots will help not run into problems when 2 computers are fighting
        /// it will target near squares to recent hits to attempt to search out the player ships and act smart like other players would
        /// </summary>
        /// <returns></returns>
        static (int, int)? ComputerLogic()
        {
            if (WasLastAHit) //if a hit was successful, target nearby ranges
            {
                Random random = new Random();
                string[] directions = ["North", "South", "East", "West"];
                if (IsPlayer1Turn && !IsPlayer1Human)
                {   
                    (int, int) newTarget = Player1LastHit;
                    do
                    {
                        Player1Direction = directions[random.Next(directions.Length)];
                        newTarget = MoveDirection(newTarget, Player1Direction);
                    }
                    while (SymbolCheck(player2Board[newTarget.Item1, newTarget.Item2]) == "[_] " || SymbolCheck(player2Board[newTarget.Item1, newTarget.Item2]) == "[_] ");
                }
                else
                {
                    (int, int) newTarget = Player2LastHit;
                    Player2Direction = directions[random.Next(directions.Length)];
                    return (MoveDirection(newTarget, Player2Direction));
                }
            }
            else
            {
                Random random = new Random();
                return (random.Next(0, 10), random.Next(0, 10));
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
        static void CheckForGameover((string, List<(int, int)>, bool[], bool)[] playerShips)
        {
            bool[] shipIsSunkBoolArray = new bool[playerShips.Length];
            for (int i = 0; i < playerShips.Length; i++)
            {
                shipIsSunkBoolArray[i] = playerShips[i].Item4;
            }
            bool checkShipsAreAllSunk = shipIsSunkBoolArray.Any(item => item);
            if (checkShipsAreAllSunk )
            {
                IsGameOver = true;
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