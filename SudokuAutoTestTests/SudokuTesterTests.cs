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
            SudokuTester tester = new SudokuTester("D:\\", "14061195");
            int result = tester.CheckValid("D:\\puzzle1.txt","D:\\result.txt",0);
            Assert.True(result == 1);
        }
        [Test()]
        public void CheckValidTest_c()
        {
            SudokuTester tester = new SudokuTester("D:\\", "14061195");
            int result = tester.CheckValid("", "D:\\result.txt", 1);
            Assert.True(result == 1);
        }
    }
}