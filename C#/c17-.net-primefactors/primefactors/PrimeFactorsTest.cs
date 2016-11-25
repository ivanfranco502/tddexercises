using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PrimeFactors
{
    public static class IntExtentions
    {
        public static Boolean isDivisibleBy(this int self, int aDivisor)
        {
            return self%aDivisor == 0;
        }

        public static List<int> primeFactors(this int self)
        {
            return new PrimeFactors(self).value();
        }
    }

    public class PrimeFactors
    {
        private int _numberToFactorize;
        private List<int> _result;
        private int _divisor;

        public PrimeFactors(int numberToFactorize)
        {
            assertIsFactorizable(numberToFactorize);
            _numberToFactorize = numberToFactorize;
        }

        public List<int> value()
        {
            initialize();

            while (isFactorizable())
                factorizeByDivisor();

            return _result;
        }

        private void assertIsFactorizable(int numberToFactorize)
        {
            if (numberToFactorize < 1)
                throw new Exception("Numero no factorizable");
        }

        private void initialize()
        {
            _result = new List<int>();
            _divisor = 2;
        }

        private void factorizeByDivisor()
        {
            while (_numberToFactorize.isDivisibleBy(_divisor))
            {
                _result.Add(_divisor);
                _numberToFactorize = _numberToFactorize/_divisor;
            }
            _divisor++;
        }

        private bool isFactorizable()
        {
            return _numberToFactorize != 1;
        }
    }

    [TestClass]
    public class PrimeFactorsTest
    {
        [TestMethod]
        public void TestOneHasNoPrimeFactors()
        {
            var primeFactors = new PrimeFactors(1).value();
            Assert.AreEqual(0, primeFactors.Count);
        }

        [TestMethod]
        public void TestPrimeNumbersHaveThemselvesAsPrimeFactors()
        {
            var primeFactors = new PrimeFactors(2).value();
            Assert.IsTrue(primeFactors.SequenceEqual(new List<int> { 2 }));
        }

        [TestMethod]
        public void TestNumbersWithSamePrimeFactorAreFactorizeCorrectly()
        {
            var primeFactors = new PrimeFactors(4).value();
            Assert.IsTrue(primeFactors.SequenceEqual(
                new List<int> { 2, 2 }));
        }

        [TestMethod]
        public void TestNumersWithDifferentPrimeFactorsAreFactorizeCorrectly()
        {
            var primeFactors = new PrimeFactors(6).value();
            Assert.IsTrue(primeFactors.SequenceEqual(
                new List<int> { 2, 3 }));
        }

        [TestMethod]
        public void TestBigNumbersAreFactorizeCorrectly()
        {
            var primeFactors = new PrimeFactors(2*2*3*5*7*11).value();
            Assert.IsTrue(primeFactors.SequenceEqual(
                new List<int> { 2,2,3,5,7,11 }));
        }
        
        [TestMethod]
        public void TestCanNotFactorizeNumbersLessThanOne()
        {
            try
            {
                new PrimeFactors(0).value();
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual("Numero no factorizable",e.Message);
            }
        }
    }
}
