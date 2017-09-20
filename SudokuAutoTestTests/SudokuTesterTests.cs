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
    public class SudokuTesterTests
    {
        [Test()]
        public void CheckValidTest_s()
        {
            SudokuTester tester = new SudokuTester("D:\\", "14061198");
            int result = tester.CheckValid("D:\\puzzle1.txt","D:\\sudoku.txt",0);
            
            Assert.True(result == 1);
        }

    }
}