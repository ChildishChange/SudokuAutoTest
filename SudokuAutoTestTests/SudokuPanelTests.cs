using NUnit.Framework;
using SudokuAutoTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SudokuAutoTest.Tests
{
    [TestFixture()]
    public class SudokuPanelTests
    {
        [Test()]
        public void MatchPuzzleSuccessTest()
        {
            string[] _sudoku =
            {
                 "9 5 8 3 6 7 1 2 4",
                 "2 3 7 4 5 1 9 6 8",
                 "1 4 6 9 2 8 3 5 7",
                 "6 1 2 8 7 4 5 9 3",
                 "5 7 3 6 1 9 4 8 2",
                 "4 8 9 2 3 5 6 7 1",
                 "7 2 4 5 9 3 8 1 6",
                 "8 9 1 7 4 6 2 3 5",
                 "3 6 5 1 8 2 7 4 9"
            };
            string[] _puzzle =
            {
                "9 0 8 0 6 0 1 2 4",
                "2 3 7 4 5 1 9 6 8",
                "1 4 6 0 2 0 3 5 7",
                "0 1 2 0 7 0 5 9 3",
                "0 7 3 0 1 0 4 8 2",
                "4 8 0 0 0 5 6 0 1",
                "7 0 4 5 9 0 8 1 6",
                "8 9 0 7 4 6 2 0 0",
                "3 0 5 0 8 0 7 0 9"
            };
            var sudoku = new SudokuPanel(_sudoku, "14061195");
            var puzzle = new SudokuPanel(_puzzle, "14061195");
            bool result = sudoku.MatchPuzzle(puzzle);
            Assert.True(result);
        }

        [Test()]
        public void MatchPuzzleFailTest()
        {
            string[] _sudoku =
            {
                 "9 5 8 3 6 7 1 2 4",
                 "2 3 7 4 5 1 9 6 8",
                 "1 4 6 2 5 7 3 5 7",
                 "6 1 2 8 7 4 5 9 3",
                 "5 7 3 6 1 9 4 8 2",
                 "4 8 9 2 3 5 6 7 1",
                 "7 2 4 5 9 3 8 1 6",
                 "8 9 1 7 4 6 2 3 5",
                 "3 6 5 1 8 2 7 4 9"
            };
            string[] _puzzle =
            {
                "9 0 8 0 6 0 1 2 4",
                "2 3 7 4 5 1 9 6 8",
                "1 4 6 0 2 0 3 5 7",
                "0 1 2 0 7 0 5 9 3",
                "0 7 3 0 1 0 4 8 2",
                "4 8 0 0 0 5 6 0 1",
                "7 0 4 5 9 0 8 1 6",
                "8 9 0 7 4 6 2 0 0",
                "3 0 5 0 8 0 7 0 9"
            };
            var sudoku = new SudokuPanel(_sudoku, "14061195");
            var puzzle = new SudokuPanel(_puzzle, "14061195");
            bool result = sudoku.MatchPuzzle(puzzle);
            Assert.False(result);
        }
    }
}