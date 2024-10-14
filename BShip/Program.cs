

using System.Runtime.InteropServices;

namespace BattleShip
{
    class Program
    {
        //set boundries of the board
        public static int boardMin = 0;
        public static int boardMax = 9;
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
                (Type: "Carrier", Coords: new List<(int, int)>(), IsHit: new bool[5], IsSunk: true),
                (Type: "Battleship", Coords: new List<(int, int)>(), IsHit: new bool[4], IsSunk: true),
                (Type: "Destroyer", Coords: new List<(int, int)>(), IsHit: new bool[3], IsSunk: true),
                (Type: "Submarine", Coords: new List<(int, int)>(), IsHit: new bool[3], IsSunk: true),
                (Type: "Gunboat", Coords: new List<(int, int)>(), IsHit: new bool[2], IsSunk: true),
            };
            var player2Ships = new[] {
                (Type: "Carrier", Coords: new List<(int, int)>(), IsHit: new bool[5], IsSunk: true),
                (Type: "Battleship", Coords: new List<(int, int)>(), IsHit: new bool[4], IsSunk: true),
                (Type: "Destroyer", Coords: new List<(int, int)>(), IsHit: new bool[3], IsSunk: true),
                (Type: "Submarine", Coords: new List<(int, int)>(), IsHit: new bool[3], IsSunk: true),
                (Type: "Gunboat", Coords: new List<(int, int)>(), IsHit: new bool[2], IsSunk: true),
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
                        Console.Write($"Would you like your {shipnames[i]} horizontal or vertical?: ");
                        hOrVResponse = Console.ReadLine().ToLower();
                        if (hOrVResponse.Length > 1 || string.IsNullOrEmpty(hOrVResponse) || !char.IsLetter(hOrVResponse[0]))
                        {
                            Console.WriteLine("Please enter either h or v.");
                            hOrVResponse = null;
                        }
                        else if (hOrVResponse != "v" && hOrVResponse != "h")
                        {
                            Console.WriteLine("Please enter either h or v.");
                            hOrVResponse = null;
                        }
                        
                    }
                    while (hOrVResponse == null);
                    do
                    {
                        //ask player where they would like their ship and check if entry is valid
                        Console.Write($"Where would you like to place your {shipnames[i]}?:");
                        playerResponse = CheckResponse(Console.ReadLine().Trim());
                        if (playerResponse != null)
                        {
                            do
                            {
                                SetShipLocation(hOrVResponse, ((int, int))playerResponse, player1Ships, i, player1Board);
                            }
                            while (player1Ships[i].IsSunk);
                        }

                    }
                    while (playerResponse == null);

                }
            }


            //method to check user feed back on squares, this will be used for set up and for attacking
            (int, int)? CheckResponse(string response)
            {
                //Console.WriteLine((response[0] - 'a', int.Parse(char.ToString(response[1])) - 1));
                //check response to see if it is a valid, checking for length of response and if its null, checking first char is letter and 2nd is a digit
                if (response.Length != 2 || string.IsNullOrEmpty(response) || !char.IsLetter(response[0]) || !char.IsDigit(response[1]))
                {
                    Console.WriteLine("Please enter a valid square in the format of [A1]");
                    return null;
                }
                else if (response[0] - 'a' > boardMax || response[0] - 'a' < boardMin || int.Parse(char.ToString(response[1])) - 1 > boardMax || int.Parse(char.ToString(response[1])) - 1 < boardMin)
                {
                    Console.WriteLine("That square is outside the bounds of the board, please try again.");
                    return null;
                }
                else
                {
                    //get index of letter given to target proper row
                    int letterIndex = response[0] - 'a';
                    var tuplePeg = (letterIndex, int.Parse(char.ToString(response[1])) - 1);
                    Console.WriteLine(tuplePeg);
                    return tuplePeg;
                }
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
                    //check if targetcoord is inside the boundries of the board(including the size of the ship)
                    if (targetCoord.Item1 <= (boardMax - shipToSetup[shipNumber].Item3.Length) && targetCoord.Item1 >= boardMin && targetCoord.Item2 <= boardMax && targetCoord.Item2 <= boardMin)
                    {
                        for (int i = 0; i < shipToSetup[shipNumber].Item3.Length; i++)
                        {
                            tempCoordsList.Add((targetCoord.Item1, targetCoord.Item2 + i));
                        }
                    }
                    //update value in array of tuples to have new coordinates, set IsSunk to false to show that the ship is now afloat
                    tempTuple[shipNumber] = (tempTuple[shipNumber].Item1, tempCoordsList, tempTuple[shipNumber].Item3, false);
                    break;
                case "v":
                    //copy h case except handle cases for vertical placement
                    if (targetCoord.Item2 <= (boardMax - shipToSetup[shipNumber].Item3.Length) && targetCoord.Item2 > boardMin && targetCoord.Item1 <= boardMax && targetCoord.Item1 <= boardMin)
                    {
                        for (int i = 0; i < shipToSetup[shipNumber].Item3.Length; i++)
                        {
                            tempCoordsList.Add((targetCoord.Item1 + i, targetCoord.Item2));
                        }
                    }
                    tempTuple[shipNumber] = (tempTuple[shipNumber].Item1, tempCoordsList, tempTuple[shipNumber].Item3, false);
                    break;
            }
            //check all other ships to see if current coordinate will intersect with any of them
            foreach (var ship in shipToSetup)
            {
                if (ship.Item2.Contains(targetCoord) && ship.Item1 != shipToSetup[shipNumber].Item1)
                {
                    return shipToSetup;
                }
            }
            for (int i = 0; i < tempCoordsList.Count; i++)
            {
                UpdateBoard(playerBoard, tempCoordsList[i].Item1, tempCoordsList[i].Item2);
            }
            return tempTuple;
        }

        static void PrintBoard(int[,] boardToDraw)
        {
            //print out the board between turns to show the progression of the game
            Console.Clear();
            Console.WriteLine(@" ____   __  ____  ____  __    ____  ____  _  _  __  ____");
            Console.WriteLine(@"(  _ \ / _\(_  _)(_  _)(  )  (  __)/ ___)/ )( \(  )(  _ \");
            Console.WriteLine(@" ) _ (/    \ )(    )(  / (_/\ ) _) \___ \) __ ( )(  ) __/");
            Console.WriteLine(@"(____/\_/\_/(__)  (__) \____/(____)(____/\_)(_/(__)(__)  ");
            Console.Write("[ ] ");
            for (int i = 0; i < 10; i++)
            {
                if (i < 9)
                {
                    Console.Write($"[{i}] ");
                }
                else { Console.WriteLine($"[{i}] "); }
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

        static void addOrUpdate(Dictionary<string, (int, int, int, bool)> dic, string key, (int, int, int, bool) newValue)
        {
            (int, int, int, bool) val;
            if (dic.TryGetValue(key, out val))
            {
                // yay, value exists!
                dic[key] = newValue;
                if (newValue.Item3 == 0)
                {
                    Console.WriteLine($"You sunk their {key}!");
                    

                }
            }
        }
        
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