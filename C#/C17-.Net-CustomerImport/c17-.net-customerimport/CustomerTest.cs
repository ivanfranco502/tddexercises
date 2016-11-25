using System;
using System.IO;
using com.tenpines.advancetdd;
using FluentNHibernate.Automapping;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate;
using NHibernate.Tool.hbm2ddl;
using NHibernate.Util;

namespace C17_.Net_CustomerImport
{
    [TestClass]
    public class CustomerTest
    {
        private ISession session;

        [TestMethod]
        public void TestMethod1()
        {
            ImportCustomers(new System.IO.FileStream("input.txt", FileMode.Open));

            var customers = session.CreateCriteria<Customer>().List();

            Assert.IsTrue(customers.Count == 2);
        }

        [TestMethod]
        public void TestMethod2()
        {
            ImportCustomers(new System.IO.FileStream("input.txt", FileMode.Open));

            var addressesOfFirstCustomer = ((Customer)session.CreateCriteria<Customer>().List().First()).Addresses;

            Assert.IsTrue(addressesOfFirstCustomer.Count == 2);
        }

        [TestMethod]
        public void TestMethod3()
        {
            ImportCustomers(new System.IO.FileStream("input.txt", FileMode.Open));

            var addressesOfSecondCustomer = ((Customer)session.CreateCriteria<Customer>().List()[1]).Addresses;

            Assert.IsTrue(addressesOfSecondCustomer.Count == 1);
        }

        [TestMethod]
        public void TestMethod4()
        {
            ImportCustomers(new System.IO.FileStream("input.txt", FileMode.Open));

            var firstNameCustomer = ((Customer)session.CreateCriteria<Customer>().List()[0]).FirstName;

            Assert.AreEqual(firstNameCustomer, "Pepe");
        }

        [TestMethod]
        public void TestMethod5()
        {
            ImportCustomers(new System.IO.FileStream("input.txt", FileMode.Open));

            var lastNameCustomer = ((Customer)session.CreateCriteria<Customer>().List()[0]).LastName;

            Assert.AreEqual(lastNameCustomer, "Sanchez");
        }

        [TestMethod]
        public void TestMethod6()
        {
            ImportCustomers(new System.IO.FileStream("input.txt", FileMode.Open));

            var documentType = ((Customer)session.CreateCriteria<Customer>().List()[0]).IdentificationType;

            Assert.AreEqual(documentType, "D");
        }

        [TestMethod]
        public void TestMethod7()
        {
            ImportCustomers(new System.IO.FileStream("input.txt", FileMode.Open));

            var documentNumber = ((Customer)session.CreateCriteria<Customer>().List()[0]).IdentificationNumber;

            Assert.AreEqual(documentNumber, "22333444");
        }

        [TestMethod]
        public void TestMethod8()
        {
            ImportCustomers(new System.IO.FileStream("input.txt", FileMode.Open));

            var streetName = ((Customer)session.CreateCriteria<Customer>().List().First()).Addresses[0].StreetName;

            Assert.AreEqual(streetName, "San Martin");
        }

        [TestMethod]
        public void TestMethod9()
        {
            ImportCustomers(new System.IO.FileStream("input.txt", FileMode.Open));

            var streetNumber = ((Customer)session.CreateCriteria<Customer>().List().First()).Addresses[0].StreetNumber;

            Assert.AreEqual(streetNumber, 3322);
        }

        [TestMethod]
        public void TestMethod10()
        {
            ImportCustomers(new System.IO.FileStream("input.txt", FileMode.Open));

            var town = ((Customer)session.CreateCriteria<Customer>().List().First()).Addresses[0].Town;

            Assert.AreEqual(town, "Olivos");
        }

        [TestMethod]
        public void TestMethod11()
        {
            ImportCustomers(new System.IO.FileStream("input.txt", FileMode.Open));

            var zipCode = ((Customer)session.CreateCriteria<Customer>().List().First()).Addresses[0].ZipCode;

            Assert.AreEqual(zipCode, 1636);
        }

        [TestMethod]
        public void TestMethod12()
        {
            ImportCustomers(new System.IO.FileStream("input.txt", FileMode.Open));

            var province = ((Customer)session.CreateCriteria<Customer>().List().First()).Addresses[0].Province;

            Assert.AreEqual(province, "BsAs");
        }

        [TestMethod]
        public void TestMethod13()
        {
            ImportCustomers(new System.IO.FileStream("input.txt", FileMode.Open));

            var firstNameCustomer = ((Customer)session.CreateCriteria<Customer>().List()[1]).FirstName;

            Assert.AreEqual(firstNameCustomer, "Juan");
        }

        [TestMethod]
        public void TestMethod14()
        {
            ImportCustomers(new System.IO.FileStream("input.txt", FileMode.Open));

            var lastNameCustomer = ((Customer)session.CreateCriteria<Customer>().List()[1]).LastName;

            Assert.AreEqual(lastNameCustomer, "Perez");
        }

        [TestMethod]
        public void TestMethod15()
        {
            ImportCustomers(new System.IO.FileStream("input.txt", FileMode.Open));

            var documentType = ((Customer)session.CreateCriteria<Customer>().List()[1]).IdentificationType;

            Assert.AreEqual(documentType, "C");
        }

        [TestMethod]
        public void TestMethod16()
        {
            ImportCustomers(new System.IO.FileStream("input.txt", FileMode.Open));

            var documentNumber = ((Customer)session.CreateCriteria<Customer>().List()[1]).IdentificationNumber;

            Assert.AreEqual(documentNumber, "23-25666777-9");
        }

        [TestMethod]
        public void TestMethod17()
        {
            ImportCustomers(new System.IO.FileStream("input.txt", FileMode.Open));

            var streetName = ((Customer)session.CreateCriteria<Customer>().List().First()).Addresses[1].StreetName;

            Assert.AreEqual(streetName, "Maipu");
        }

        [TestMethod]
        public void TestMethod18()
        {
            ImportCustomers(new System.IO.FileStream("input.txt", FileMode.Open));

            var streetNumber = ((Customer)session.CreateCriteria<Customer>().List().First()).Addresses[1].StreetNumber;

            Assert.AreEqual(streetNumber, 888);
        }

        [TestMethod]
        public void TestMethod19()
        {
            ImportCustomers(new System.IO.FileStream("input.txt", FileMode.Open));

            var town = ((Customer)session.CreateCriteria<Customer>().List().First()).Addresses[1].Town;

            Assert.AreEqual(town, "Florida");
        }

        [TestMethod]
        public void TestMethod20()
        {
            ImportCustomers(new System.IO.FileStream("input.txt", FileMode.Open));

            var zipCode = ((Customer)session.CreateCriteria<Customer>().List().First()).Addresses[1].ZipCode;

            Assert.AreEqual(zipCode, 1122);
        }

        [TestMethod]
        public void TestMethod21()
        {
            ImportCustomers(new System.IO.FileStream("input.txt", FileMode.Open));

            var province = ((Customer)session.CreateCriteria<Customer>().List().First()).Addresses[1].Province;

            Assert.AreEqual(province, "Buenos Aires");
        }

        [TestMethod]
        public void TestMethod22()
        {
            ImportCustomers(new System.IO.FileStream("input.txt", FileMode.Open));

            var streetName = ((Customer)session.CreateCriteria<Customer>().List()[1]).Addresses[0].StreetName;

            Assert.AreEqual(streetName, "Alem");
        }

        [TestMethod]
        public void TestMethod23()
        {
            ImportCustomers(new System.IO.FileStream("input.txt", FileMode.Open));

            var streetNumber = ((Customer)session.CreateCriteria<Customer>().List()[1]).Addresses[0].StreetNumber;

            Assert.AreEqual(streetNumber, 1122);
        }

        [TestMethod]
        public void TestMethod24()
        {
            ImportCustomers(new System.IO.FileStream("input.txt", FileMode.Open));

            var town = ((Customer)session.CreateCriteria<Customer>().List()[1]).Addresses[0].Town;

            Assert.AreEqual(town, "CABA");
        }

        [TestMethod]
        public void TestMethod25()
        {
            ImportCustomers(new System.IO.FileStream("input.txt", FileMode.Open));

            var zipCode = ((Customer)session.CreateCriteria<Customer>().List()[1]).Addresses[0].ZipCode;

            Assert.AreEqual(zipCode, 1001);
        }

        [TestMethod]
        public void TestMethod26()
        {
            ImportCustomers(new System.IO.FileStream("input.txt", FileMode.Open));

            var province = ((Customer)session.CreateCriteria<Customer>().List()[1]).Addresses[0].Province;

            Assert.AreEqual(province, "CABA");
        }


        public void ImportCustomers(Stream sourceStream)
        {
            var lineReader = new StreamReader(sourceStream);

            var transaction = session.BeginTransaction();
            Customer newCustomer = null;
            var line = lineReader.ReadLine();
            while (line != null)
            {
                if (line.StartsWith("C"))
                {
                    var customerData = line.Split(',');
                    newCustomer = new Customer();
                    newCustomer.FirstName = customerData[1];
                    newCustomer.LastName = customerData[2];
                    newCustomer.IdentificationType = customerData[3];
                    newCustomer.IdentificationNumber = customerData[4];
                    session.Persist(newCustomer);
                }
                else if (line.StartsWith("A"))
                {
                    var addressData = line.Split(',');
                    var newAddress = new Address();

                    newCustomer.AddAddress(newAddress);
                    newAddress.StreetName = addressData[1];
                    newAddress.StreetNumber = Int32.Parse(addressData[2]);
                    newAddress.Town = addressData[3];
                    newAddress.ZipCode = Int32.Parse(addressData[4]);
                    newAddress.Province = addressData[5];
                }

                line = lineReader.ReadLine();
            }

            transaction.Commit();

            sourceStream.Close();
        }

        [TestCleanup]
        public void TearDown()
        {
            session.Close();
        }

        [TestInitialize]
        public void SetUp()
        {
            var storeConfiguration = new StoreConfiguration();
            var configuration = Fluently.Configure()
                .Database(MsSqlCeConfiguration.Standard.ShowSql().ConnectionString("Data Source=CustomerImport.sdf"))
                .Mappings(m => m.AutoMappings.Add(AutoMap
                    .AssemblyOf<Customer>(storeConfiguration)
                    .Override<Customer>(map => map.HasMany(x => x.Addresses).Cascade.All())));

            var sessionFactory = configuration.BuildSessionFactory();
            new SchemaExport(configuration.BuildConfiguration()).Execute(true, true, false);
            session = sessionFactory.OpenSession();
        }
    }
}
