using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;


namespace SudokuAutoTest
{
    public class SudokuTester
    {
        private ProcessStartInfo _binaryInfo;
        public string _logFile { get; }
        public string NumberId { get; }
        public List<Tuple<string, double>> Scores { get; }

        public SudokuTester(string baseDir, string numberId)
        {
            Scores = new List<Tuple<string, double>>();
            NumberId = numberId;
            //Base dir
            _binaryInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                WorkingDirectory = Path.Combine(baseDir, NumberId)
            };
            _logFile = Path.Combine(Program.LogDir, $"{numberId}-log.txt");
        }

        //If success,return time; Else, return "error message"
        public double ExecuteTest(string arguments, int timeLimit)
        {
            if (!FindExePath(_binaryInfo))
            {
                Logger.Error("No sudoku.exe file!", _logFile);
                return (int) ErrorType.NoSudokuExe;
            }

            _binaryInfo.Arguments = arguments;
            try
            {
                Stopwatch timeWatch = new Stopwatch();
                timeWatch.Start();
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (Process exeProcess = Process.Start(_binaryInfo))
                {
                    //Start monitor
                    exeProcess.WaitForExit(timeLimit * 1000);
                    timeWatch.Stop();
                    //Release all resources
                    if (!exeProcess.HasExited)
                    {
                        //Give system sometime to release resource
                        exeProcess.Kill();
                        Thread.Sleep(1000);
                    }
                }
                //Check the sudoku file
                string checkFile = Path.Combine(_binaryInfo.WorkingDirectory, "sudoku.txt");
                if (!File.Exists(checkFile))
                {
                    Logger.Info("No sudoku.txt file!", _logFile);
                    return (int)ErrorType.NoGeneratedSudokuTxt;
                }
                //如果不出现错误的话,则退出
                int tryTimeLimit = 10;
                while (tryTimeLimit > 0)
                {
                    try
                    {
                        File.ReadAllText(checkFile);
                        break;
                    }
                    catch (Exception)
                    {
                        Thread.Sleep(100);
                        tryTimeLimit --;
                    }
                }
                if (tryTimeLimit == 0)
                {
                    Logger.Info("Exe run time out!", _logFile);
                    return (int) ErrorType.OutOfTimeCloseExe;
                }
                //获取错误代码
                //这里需要进行一些修改，根据-c-s来调用checkvalid
                var parameters = arguments.Split(' ');
                int errorNum = 0;
                //若为 - c即执行正常的CheckValid(puzzlePath = null, filePath, count)
                if(parameters.First().Equals("-c"))
                {
                    //errorNum = CheckValid(checkFile, int.Parse(Regex.Match(arguments, @"\d+").Value));
                    errorNum = CheckValid("", checkFile, int.Parse(Regex.Match(arguments, @"\d+").Value));
                }
                //若为 - s即执行CheckValid(puzzlePath, filePath, count),此时的count为0
                else
                {
                    
                    
                    errorNum = CheckValid(parameters.Last(),checkFile,0);
                }
               
                if (errorNum > 0)
                {
                    Logger.Info($"Arguments:{arguments} Normal, spend time {(double)timeWatch.ElapsedMilliseconds/1000}s", _logFile);
                    return (double)timeWatch.ElapsedMilliseconds / 1000;
                }
                else
                {
                    return errorNum;
                }
            }
            catch (Exception e)
            {
                //Log into file to record the runtime error
                Logger.Error($"Arguments:{arguments} RuntimeError:{e.Message}", _logFile);
                return (int)ErrorType.RuntimeError;
            }
        }

        //Find exe file
        public bool FindExePath(ProcessStartInfo binaryInfo)
        {
            string[] options = new[] {"BIN", "bin", "Bin"};
            foreach (var option in options)
            {
                var exePath = new FileInfo(Path.Combine(binaryInfo.WorkingDirectory, option, "sudoku.exe")).FullName;
                if (File.Exists(exePath))
                {
                    binaryInfo.FileName = exePath;
                    binaryInfo.WorkingDirectory = new FileInfo(exePath).DirectoryName;
                    return true;
                }
            }
            //Find the binaryInfo's son directory to find it
            string[] fileVariants = new[] {"sudoku.exe", "SudoKu.exe", "SUDOKU.exe"};
            foreach (var fileVariant in fileVariants)
            {
                var exePaths = Directory.GetFiles(binaryInfo.WorkingDirectory, fileVariant, SearchOption.AllDirectories);
                if (exePaths.Any())
                {
                    FileInfo info = new FileInfo(exePaths[0]);
                    binaryInfo.FileName = info.FullName;
                    binaryInfo.WorkingDirectory = info.DirectoryName;
                    return true;
                }
            }
            //Match exe file
            var anyExePaths = Directory.GetFiles(binaryInfo.WorkingDirectory, "*.exe", SearchOption.AllDirectories);
            if (anyExePaths.Any())
            {
                FileInfo info = new FileInfo(anyExePaths[0]);
                binaryInfo.FileName = info.FullName;
                binaryInfo.WorkingDirectory = info.DirectoryName;
                return true;
            }
            //No file matched
            return false;
        }

        //Overview:对可执行文件测试执行每一个测试点,得到每个点的运行时长或错误类别
        public void GetCorrectScore()
        {
            //正确性测试占分25,共5个测试点
            //其中10分为错误情况得分,在自动化测试中不进行
            //剩余15分共有5个正确性测试点
            //先测-c
            string[] argumentScoreMap_c = new string[]
            {
                "-c 1",
                "-c 5",
                "-c 100",
                "-c 500",
                "-c 1000"
            };
            foreach (var argument in argumentScoreMap_c)
            {
                Scores.Add(new Tuple<string, double>(argument, ExecuteTest(argument, 60)));
            }
            if (Scores.Where(i => i.Item2 > 0).ToList().Count >= 4)
            {
                //剩下10分,分为2组测试
                //5万+
                //100万+
                Scores.Add(new Tuple<string, double>("-c 50000", ExecuteTest("-c 50000", Program.MaxLimitTime)));
                if (Scores.Last().Item2 > 0)
                {
                    Scores.Add(new Tuple<string, double>("-c 1000000", ExecuteTest("-c 1000000", Program.MaxLimitTime)));
                }
                else
                {
                    Scores.Add(new Tuple<string, double>("-c 1000000", (int)ErrorType.OutOfTimeCloseExe));
                }
            }
            else
            {
                foreach (var argument in new[]{
                    "-c 50000",
                    "-c 1000000"})
                {
                    Scores.Add(new Tuple<string, double>(argument, (int) ErrorType.CanNotDoEfficientTest));
                }
            }
            //再测-s
            string[] argumentScoreMap_s = new string[]
            {
                "1puzzle.txt",
                "5puzzle.txt",
                "100puzzle.txt",
                "500puzzle.txt",
                "1000puzzle.txt",

            };
            foreach (var argument in argumentScoreMap_s)
            {
                Scores.Add(new Tuple<string, double>("-s "+argument, ExecuteTest("-s "+Path.GetFullPath(argument), 60)));
            }
            if (Scores.Where(i => i.Item2 > 0).ToList().Count >= 4)
            {
                //剩下10分,分为2组测试
                //5万+
                //100万+
                string _50000puzzle = "50000puzzle.txt";
                string _millionpuzzle = "1000000puzzle.txt";
                Scores.Add(new Tuple<string, double>("-s "+_50000puzzle, ExecuteTest("-s " + Path.GetFullPath(_50000puzzle), Program.MaxLimitTime)));
                if (Scores.Last().Item2 > 0)
                {
                    Scores.Add(new Tuple<string, double>("-s " + _millionpuzzle, ExecuteTest("-s " + Path.GetFullPath(_millionpuzzle), Program.MaxLimitTime)));
                }
                else
                {
                    Scores.Add(new Tuple<string, double>("-s " + _millionpuzzle, (int)ErrorType.OutOfTimeCloseExe));
                }
            }
            else
            {
                foreach (var argument in new[]{
                    "-s 50000puzzle.txt",
                    "-c 1000000puzzle.txt"})
                {
                    Scores.Add(new Tuple<string, double>(argument, (int)ErrorType.CanNotDoEfficientTest));
                }
            }
        }

        //Get Hash Code
        public static string GetHashCode(SudokuPanel obj)
        {
            return obj.ToString();
        }

        public int CheckValid(string filePath, int count)
        {
            //新申请一个数独棋盘
            var sudokuSets = new HashSet<string>();
            //从路径中读取相应内容
            var content = File.ReadAllText(filePath);
            string splitSymbol = Environment.NewLine + Environment.NewLine;
            var multipleLines = content.Split(new[] { splitSymbol }, StringSplitOptions.RemoveEmptyEntries);
            if (multipleLines.Length == 1)
            {
                multipleLines = content.Split(new[] {"\n\n"}, StringSplitOptions.RemoveEmptyEntries);
            }
            if (multipleLines.Any())
            {
                foreach (var lines in multipleLines)
                {
                    try
                    {
                        var sudokuPanel =
                            new SudokuPanel(
                                lines.Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries),
                                NumberId);
                        string hashCode = GetHashCode(sudokuPanel);
                        if (sudokuSets.Contains(hashCode))
                        {
                            Logger.Error("Sudoku.txt have repeated sudoku panels!", _logFile);
                            return (int) ErrorType.RepeatedPanels;
                        }
                        if (!sudokuPanel.Valid)
                        {
                            Logger.Error($"SudokuPanel Not Invalid:\n {sudokuPanel}", _logFile);
                            return (int) ErrorType.SudokuPanelInvalid;
                        }

                        sudokuSets.Add(hashCode);
                    }
                    catch (Exception e)
                    {
                        //the sudoku is not valid.
                        break;
                    }
                }
                if (sudokuSets.Count == count)
                {
                    return 1;
                }
            }
            Logger.Error($"Sudoku.txt doesn't have engough sudoku panels! Expect:{count} Actual:{sudokuSets.Count}", _logFile);
            return (int)ErrorType.NotEnoughCount;
        }
        //Overview:将-s和-c功能整合到一起
        //当puzzlePath为非空串时，测试-s，此时count应为0，否则测试-c
        public int CheckValid(string puzzlePath, string filePath, int count)
        {
            //新申请一个数独棋盘
            var sudokuSets = new HashSet<string>();
            //从filepath路径中读取相应内容
            var content = File.ReadAllText(filePath);
            string splitSymbol = Environment.NewLine + Environment.NewLine;
            var multipleLines = content.Split(new[] { splitSymbol }, StringSplitOptions.RemoveEmptyEntries);
            if (multipleLines.Length == 1)
            {
                multipleLines = content.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            }
            //若puzzle路径不为空，则读取其中的puzzle并进行split，此时filepath内是解得的答案
            bool hasPuzzle = string.IsNullOrEmpty(puzzlePath)^true;
            string puzzleContent;
            LinkedList<string> puzzleLines = new LinkedList<string>();
            if (hasPuzzle)
            {
                puzzleContent = File.ReadAllText(puzzlePath);
                string[] puzzleTemp = puzzleContent.Split(new[] { splitSymbol }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string puzzle in puzzleTemp)
                {
                    puzzleLines.AddLast(puzzle);  
                }
                if(puzzleLines.Count!=multipleLines.Length)
                {
                    Logger.Error($"Puzzle Number Do Not Match Answer Number!", _logFile);
                    return (int)ErrorType.NumbersDoNotMatch;
                }
            }
            //开始测试
            if (multipleLines.Any())
            {
                foreach (var lines in multipleLines)
                {
                    try
                    {
                        var sudokuPanel =
                            new SudokuPanel(
                                lines.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries),
                                NumberId);
                        
                        
                        //进行数独的有效性检查
                        if (!sudokuPanel.Valid)
                        {
                            Logger.Error($"SudokuPanel Not Invalid:\n {sudokuPanel}", _logFile);
                            return (int)ErrorType.SudokuPanelInvalid;
                        }
                        //进行判重
                        if (!hasPuzzle)
                        { 
                            string hashCode = GetHashCode(sudokuPanel);
                            if (sudokuSets.Contains(hashCode))
                            {
                                Logger.Error("Sudoku.txt have repeated sudoku panels!", _logFile);
                                return (int)ErrorType.RepeatedPanels;
                            }
                            sudokuSets.Add(hashCode);

                        }
                        //检查解与题目是否对应】
                        if (hasPuzzle)
                        {
                            //从puzzleline中取出题目
                            var puzzlePanel =
                                new SudokuPanel(
                                    puzzleLines.First.Value.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries),
                                    NumberId);
                            if (!sudokuPanel.MatchPuzzle(puzzlePanel))
                            {
                                Logger.Error($"Sudoku Answer Do Not Match The Puzzle!:\n Puzzle:\n{puzzlePanel}\n\nAnswer:\n{sudokuPanel}", _logFile);
                                return (int)ErrorType.SudokuAnswerDoNotMatch;
                            }
                            puzzleLines.RemoveFirst();
                        }
                        
                    }
                    catch (Exception e)
                    {
                        //the sudoku is not valid.
                        break;
                    }
                }
                if (hasPuzzle&&puzzleLines.Count == 0)
                {
                    return 1;
                }
                else
                {
                    if(sudokuSets.Count == count)
                    {
                        return 1;
                    }
                }
            }
            Logger.Error($"Sudoku.txt doesn't have engough sudoku panels! Expect:{count} Actual:{sudokuSets.Count}", _logFile);
            return (int)ErrorType.NotEnoughCount;
        }
        
    }

    public class SudokuPanel
    {
        public string[,] Grid { get; set; }
        public bool Valid { get; }
        private string _numberID;

        public SudokuPanel(string[] rows, string numberID)
        {
            _numberID = numberID;
            int length = rows.Length;
            Grid = new string[length, length];
            for (int rowIndex = 0; rowIndex < length; rowIndex++)
            {
                //Remove invalid whitespaces.
                var row = Regex.Replace(rows[rowIndex], @"\p{Z}", " ");
                string[] columns = row.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (columns.Length > 0)
                {
                    for (int colIndex = 0; colIndex < length; colIndex++)
                    {
                        Grid[rowIndex, colIndex] = columns[colIndex];
                    }
                }
            }
            Valid = Validiate();
        }

        public override string ToString()
        {
            StringBuilder build = new StringBuilder();
            int length = Grid.GetLength(0);
            for (int rowIndex = 0; rowIndex < length; rowIndex++)
            {
                for (int colIndex = 0; colIndex < length; colIndex++)
                {
                    build.Append(Grid[rowIndex, colIndex]);
                }
            }
            return build.ToString();
        }

        private bool Validiate()
        {
            var lastChar = _numberID[_numberID.Length - 1];
            var lastSecChar = _numberID[_numberID.Length - 2];
            var validNum = (lastSecChar - '0' + lastChar - '0') % 9 + 1;
            if(validNum != Grid[0, 0][0] - '0')
            {
                return false;
            }
            int length = Grid.GetLength(0);
            bool[,] rowCheck = new bool[length, length];
            bool[,] colCheck = new bool[length, length];
            bool[,] squareCheck = new bool[length, length];
            for (int i = 0; i < Grid.GetLength(0); i++)
            {
                for (int j = 0; j < Grid.GetLength(1); j++)
                {
                    if (!string.Equals(Grid[i, j], "0"))
                    {
                        if (Grid[i, j] == null)
                        {
                            return false;
                        }
                        int num = int.Parse(Grid[i, j]) - 1, k = i / 3 * 3 + j / 3;
                        if (rowCheck[i, num] || colCheck[j, num] || squareCheck[k, num])
                        {
                            return false;
                        }
                        rowCheck[i, num] = colCheck[j, num] = squareCheck[k, num] = true;
                    }
                }
            }
            return true;
        }

        public bool MatchPuzzle(SudokuPanel sudokuPuzzle)
        {
            
            for (int i = 0; i < this.Grid.GetLength(0); i++)
            {
                for (int j = 0; j < this.Grid.GetLength(1); j++)
                {
                    int Puzzlenum = int.Parse(sudokuPuzzle.Grid[i, j]); 
                    int Answernum = int.Parse(this.Grid[i, j]);
                    if(Puzzlenum!=0&&Puzzlenum!=Answernum)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }


    public enum ErrorType
    {
        NoSudokuExe = -1,
        NoGeneratedSudokuTxt = -2,
        RuntimeError = -3,
        OutOfTimeCloseExe = -4,
        RunOutOfTime = -5,
        RepeatedPanels = -6,
        SudokuPanelInvalid = -7,
        NotEnoughCount = -8,
        CanNotDoEfficientTest = -9,
        NumbersDoNotMatch = -10,
        SudokuAnswerDoNotMatch = -11
    }
}
