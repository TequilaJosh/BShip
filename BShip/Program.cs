

using System.ComponentModel.Design;
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

        //board borders to help player see what squares are which
        public static readonly string[] topBoardBorder = ["[1]", "[2]", "[3]", "[4]", "[5]", "[6]", "[7]", "[8]", "[9]", "[10]"];
        public static readonly string[] leftBoardBorder = ["A", "B", "C", "D", "E", "F", "G", "H", "I", "J"];

        //setup player boards
        public static int[,] player1Board {  get; set; }
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

            ComputerSetup(shipTypes, player2Ships);
            SetupPhase(shipTypes, player1Ships);



            //Method to doo the setup with the player
        void SetupPhase(string[] shipnames, (string, List<(int, int)>, bool[], bool)[] playerShips)
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
                    hOrVResponse = LogData(Console.ReadLine().ToLower().Trim(),3);
                    if (hOrVResponse.Length > 1 || string.IsNullOrEmpty(hOrVResponse) || !char.IsLetter(hOrVResponse[0]))
                    {
                        LogData("Please enter either h or v.",1);
                        hOrVResponse = null;
                    }
                    else if (hOrVResponse != "v" && hOrVResponse != "h")
                    {
                        LogData("Please enter either h or v.",1);
                        hOrVResponse = null;
                    }
                }
                while (hOrVResponse == null);
                do
                {
                    //ask player where they would like their ship and check if entry is valid
                    LogData($"Where would you like to place your {shipnames[i]}?:",2);
                    playerResponse = CheckResponse(LogData(Console.ReadLine().Trim(),3));
                    if (playerResponse != null)
                    {
                        SetShipLocation(hOrVResponse, ((int, int))playerResponse, player1Ships, i, player1Board);
                    }
                }
                while (player1Ships[i].IsSunk) ;
            }
        }

        /// method to check user feed back on squares, this will be used for set up and for attacking
        (int, int)? CheckResponse(string response)
        {
            //Console.WriteLine((response[0] - 'a', int.Parse(char.ToString(response[1])) - 1));
            //check response to see if it is a valid, checking for length of response and if its null, checking first char is letter and 2nd is a digit
            if (response.Length > 3 || string.IsNullOrEmpty(response) || !char.IsLetter(response[0]) || !char.IsDigit(response.Substring(1)[0]))
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
                //Console.WriteLine(tuplePeg);
                return tuplePeg;
            }
            else 
            {
                //get index of letter given to target proper row
                int letterIndex = response[0] - 'a';
                var tuplePeg = (int.Parse(response.Substring(1)) - 1, letterIndex);
                //Console.WriteLine(tuplePeg);
                return tuplePeg;
            }
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
                    computerTargetCoord = (random.Next(0,9), random.Next(0,9));
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
                                tempCoordsList.Add((targetCoord.Item1 + i, targetCoord.Item2));
                                }
                            }
                        }
                        //update value in array of tuples to have new coordinates, set IsSunk to false to show that the ship is now afloat
                        tempTuple[shipNumber] = (tempTuple[shipNumber].Item1, tempCoordsList, tempTuple[shipNumber].Item3, false);
                    }
                    else
                    {
                        LogData("Your ship can't be placed there, try again.",1);
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
                                            LogData($"Your {shipToSetup[shipNumber].Item1.ToString()} can't be placed there.",1);
                                            return shipToSetup;
                                        }
                                    }
                                    tempCoordsList.Add((targetCoord.Item1, targetCoord.Item2 + i));
                                }
                            }
                        tempTuple[shipNumber] = (tempTuple[shipNumber].Item1, tempCoordsList, tempTuple[shipNumber].Item3, false);
                        }
                    }
                    else
                    {
                        LogData("Your ship can't be placed there, try again.",1);
                    }
                    break;
            }
            for (int i = 0; i < tempCoordsList.Count; i++)
            {
                UpdateBoard(playerBoard, tempCoordsList[i].Item1, tempCoordsList[i].Item2);
            }
            return tempTuple;
        }

        /// <summary>
        /// Controls how turns take place  and handles switching turns between players
        /// </summary>
        static void TakeTurns()
        {

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
        static void UpdateBoard(int[,] selectedBoard, int xValue, int yValue)
        {
            //check if values are inside the bounds
            if (0 <= xValue && xValue <= 9 && 0 <= yValue && yValue <= 9)
            {
                bool viableTarget;
                if (selectedBoard[xValue, yValue] == 0 || selectedBoard[xValue, yValue] == 3)
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
                    if (selectedBoard[xValue, yValue] == 3)
                    {
                        selectedBoard[xValue, yValue] = 2;
                    }
                    else
                    {
                        selectedBoard[xValue, yValue] = 1;

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

        static (int, int) ComputerLogic()
        {
            Random random = new Random();
            return (random.Next(0, 10), random.Next(0, 10));
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
    }
}