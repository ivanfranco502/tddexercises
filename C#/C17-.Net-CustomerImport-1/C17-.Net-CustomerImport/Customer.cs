using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using C17_.Net_CustomerImport;
using FluentNHibernate.Automapping;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Tool.hbm2ddl;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace com.tenpines.advancetdd
{

    public class StoreConfiguration : DefaultAutomappingConfiguration
    {
        public override bool ShouldMap(Type type)
        {
            return type == typeof (Customer) || type == typeof (Address);
        }
    }

    public class Address
    {
        public virtual Guid Id { get; set; }
        public virtual string StreetName { get; set; }
        public virtual int StreetNumber { get; set; }
        public virtual string Town { get; set; }
        public virtual int ZipCode { get; set; }
        public virtual string Province { get; set; }

        public virtual bool IsAt(string aStreetName)
        {
            return StreetName.Equals(aStreetName);
        }
    }


    public class Customer
    {
	    public virtual long Id { get; set; }
        public virtual string FirstName { get; set; }
        public virtual string LastName { get; set; }
        public virtual string IdentificationType { get; set; }
        public virtual string IdentificationNumber { get; set; }
        public virtual IList<Address> Addresses { get; set; }

        public Customer()
        {
            Addresses = new List<Address>();
        }

        public virtual void AddAddress(Address anAddress)
        {
            Addresses.Add(anAddress);
        }

        public virtual int NumberOfAddress()
        {
            return Addresses.Count;
        }

        public virtual Address AddressAt(string aStreetName)
        {
            return Addresses.First(anAddress => anAddress.IsAt(aStreetName)); 
        }
    }

    // Creao abstraccion CustomerImpoter. Move ImportCustomer and Rename
    // Lo convierto en un method object.
    public class CustomerImporter
    {
        private readonly ISession _session;
        private readonly TextReader _stream;
        private string _readLine;
        private string[] _record;
        private Customer _newCustomer;

        public CustomerImporter(ISession session, TextReader stream)
        {
            this._stream = stream;
            this._session = session;
        }

        public void Value()
        {
            _newCustomer = null;
            while (HasLineToParse())
            {
                _record = _readLine.Split(',');
                if (_readLine.StartsWith("C"))
                {
                    importCustomer();
                }
                else if (_readLine.StartsWith("A"))
                {
                    importAddress();
                }
                throw new ImportException("Invalid register type");
            }
        }

        private void importAddress()
        {
            if (_record.Length == 6)
            {
                var newAddress = new Address();
                int integerParseValue;
                newAddress.StreetName = _record[1];

                if (!Int32.TryParse(_record[2], out integerParseValue))
                    throw new ImportException("Could not import Address. Value is not in correct format");
                newAddress.StreetNumber = integerParseValue;

                newAddress.Town = _record[3];
                if (!Int32.TryParse(_record[4], out integerParseValue))
                    throw new ImportException("Could not import Address. Value is not in correct format");
                newAddress.ZipCode = integerParseValue;

                newAddress.Province = _record[5];

                if (_newCustomer != null)
                    _newCustomer.AddAddress(newAddress);
                else
                    throw new ImportException("Could not import Address. Customer not exist");
            }
            else
                throw new ImportException("Could not import Address. Data is missing");
        }

        private void importCustomer()
        {
            if (_record.Length == 5)
            {
                _newCustomer = new Customer();

                _newCustomer.FirstName = _record[1];
                _newCustomer.LastName = _record[2];
                _newCustomer.IdentificationType = _record[3];
                _newCustomer.IdentificationNumber = _record[4];

                _session.Persist(_newCustomer);
            }
            else
                throw new ImportException("Could not import Customer. Data is missing");

        }

        private bool HasLineToParse()
        {
            _readLine = _stream.ReadLine();
            return _readLine != null;
        }

        public static void Main()
        {
        }
    }

    [TestClass]
    public class CustomerImportTest
    {
        private readonly PersistentCustomerSystem _persistentCustomerSystem = new PersistentCustomerSystem();

        [TestMethod]
        public void Test1()
        {
            // Bad Smell 4: Que el test conozca como conectarse!
            using (_persistentCustomerSystem._session = _persistentCustomerSystem.CreateSession())
            using (var inputStream = ValidDataStream("C,Pepe,Sanchez,D,22333444\n" +
                                                     "A,San Martin,3322,Olivos,1636,BsAs\n" +
                                                     "A,Maipu,888,Florida,1122,Buenos Aires\n" +
                                                     "C,Juan,Perez,C,23-25666777-9\n" +
                                                     "A,Alem,1122,CABA,1001,CABA"))
            {
                var transaction = _persistentCustomerSystem._session.BeginTransaction();
                new CustomerImporter(_persistentCustomerSystem._session, inputStream).Value();

                Assert.AreEqual(2, _persistentCustomerSystem._session.CreateCriteria<Customer>().List<Customer>().Count);

                AssertPepeSanchezWasImportedCorrectly();
                AssertJuanPerezWasImportedCorrectly();

                transaction.Commit();
            }
        }

        [TestMethod]
        public void Test2()
        {
            // Bad Smell 4: Que el test conozca como conectarse!
            using (_persistentCustomerSystem._session = _persistentCustomerSystem.CreateSession())
            using (var inputStream = ValidDataStream("C,Pepe,Sanchez,D\n"))
            {

                try
                {
                    var transaction = _persistentCustomerSystem._session.BeginTransaction();

                    new CustomerImporter(_persistentCustomerSystem._session, inputStream).Value();

                    transaction.Commit();

                    throw new ImportException("Test Failed");
                }
                catch (ImportException ex)
                {
                    Assert.AreEqual("Could not import Customer. Data is missing", ex.Message);
                    Assert.AreEqual(0, _persistentCustomerSystem._session.CreateCriteria<Customer>().List<Customer>().Count);
                }
            }
    }

        [TestMethod]
        public void Test3()
        {
            // Bad Smell 4: Que el test conozca como conectarse!
            using (_persistentCustomerSystem._session = _persistentCustomerSystem.CreateSession())
            using (var inputStream = ValidDataStream("C,Pepe,Sanchez,D,22333444\n" +
                                                     "A,San Martin,3322,Olivos,1636\n"))
            {
                try
                {
                    var transaction = _persistentCustomerSystem._session.BeginTransaction();
                    new CustomerImporter(_persistentCustomerSystem._session, inputStream).Value();

                    transaction.Commit();

                    throw new ImportException("Test Failed");
                }
                catch (ImportException ex)
                {
                    Assert.AreEqual("Could not import Address. Data is missing", ex.Message);
                    Assert.AreEqual(1, _persistentCustomerSystem._session.CreateCriteria<Customer>().List<Customer>().Count);
                    var customer = _persistentCustomerSystem.CustomerIdentifiedAs("D", "22333444");
                    Assert.AreEqual(0, customer.Addresses.Count);
                }
            }
        }

        [TestMethod]
        public void Test4()
        {
            // Bad Smell 4: Que el test conozca como conectarse!
            using (_persistentCustomerSystem._session = _persistentCustomerSystem.CreateSession())
            using (var inputStream = ValidDataStream("C,Pepe,Sanchez,D,22333444\n" +
                                                     "A,San Martin,S/N,Olivos,1636, BsAs\n"))
            {
                try
                {
                    var transaction = _persistentCustomerSystem._session.BeginTransaction();
                    new CustomerImporter(_persistentCustomerSystem._session, inputStream).Value();

                    transaction.Commit();

                    throw new ImportException("Test Failed");
                }
                catch (ImportException ex)
                {
                    Assert.AreEqual("Could not import Address. Value is not in correct format", ex.Message);
                    Assert.AreEqual(1, _persistentCustomerSystem._session.CreateCriteria<Customer>().List<Customer>().Count);
                    var customer = _persistentCustomerSystem.CustomerIdentifiedAs("D", "22333444");
                    Assert.AreEqual(0, customer.Addresses.Count);
                }
            }
        }

        [TestMethod]
        public void Test5()
        {
            // Bad Smell 4: Que el test conozca como conectarse!
            using (_persistentCustomerSystem._session = _persistentCustomerSystem.CreateSession())
            using (var inputStream = ValidDataStream("C,Pepe,Sanchez,Segundo Nombre,D,22333444\n"))
            {

                try
                {
                    var transaction = _persistentCustomerSystem._session.BeginTransaction();
                    new CustomerImporter(_persistentCustomerSystem._session, inputStream).Value();

                    transaction.Commit();
                    throw new ImportException("Test Failed");
                }
                catch (ImportException ex)
                {
                    Assert.AreEqual("Could not import Customer. Data is missing", ex.Message);
                    Assert.AreEqual(0, _persistentCustomerSystem._session.CreateCriteria<Customer>().List<Customer>().Count);
                }

            }
        }

        [TestMethod]
        public void Test6()
        {
            // Bad Smell 4: Que el test conozca como conectarse!
            using (_persistentCustomerSystem._session = _persistentCustomerSystem.CreateSession())
            using (var inputStream = ValidDataStream("\n" + 
                                                    "C,Pepe,Sanchez,D,22333444\n" + 
                                                    "A,San Martin,1234,Olivos,1636, BsAs\n"))
            {

                try
                {
                    var transaction = _persistentCustomerSystem._session.BeginTransaction();
                    new CustomerImporter(_persistentCustomerSystem._session, inputStream).Value();

                    transaction.Commit();
                    throw new ImportException("Test Failed");
                }
                catch (ImportException ex)
                {
                    Assert.AreEqual("Invalid register type", ex.Message);
                    Assert.AreEqual(0, _persistentCustomerSystem._session.CreateCriteria<Customer>().List<Customer>().Count);
                }

            }
        }

        [TestMethod]
        public void Test7()
        {
            // Bad Smell 4: Que el test conozca como conectarse!
            using (_persistentCustomerSystem._session = _persistentCustomerSystem.CreateSession())
            using (var inputStream = ValidDataStream("A,San Martin,1234,Olivos,1636, BsAs\n"))
            {

                try
                {
                    var transaction = _persistentCustomerSystem._session.BeginTransaction();
                    new CustomerImporter(_persistentCustomerSystem._session, inputStream).Value();

                    transaction.Commit();
                    throw new ImportException("Test Failed");
                }
                catch (ImportException ex)
                {
                    Assert.AreEqual("Could not import Address. Customer not exist", ex.Message);
                    Assert.AreEqual(0, _persistentCustomerSystem._session.CreateCriteria<Customer>().List<Customer>().Count);
                }

            }
        }

        //[TestMethod]
        //public void Test8()
        //{
        //    // Bad Smell 4: Que el test conozca como conectarse!
        //    using (_session = CreateSession())
        //    using (var inputStream = ValidDataStream("CASA,Pepe,Sanchez,D,22333444\n"))
        //    {

        //        try
        //        {
        //            var transaction = _session.BeginTransaction();
        //            new CustomerImporter(_session, inputStream).Value();

        //            transaction.Commit();
        //            throw new ImportException("Test Failed");
        //        }
        //        catch (ImportException ex)
        //        {
        //            Assert.AreEqual("Invalid register type", ex.Message);
        //            Assert.AreEqual(0, _session.CreateCriteria<Customer>().List<Customer>().Count);
        //        }

        //    }
        //}


        private void AssertJuanPerezWasImportedCorrectly()
        {
            var juanPerez = _persistentCustomerSystem.CustomerIdentifiedAs("C", "23-25666777-9");
            AssertCustomer(juanPerez, "Juan", "Perez", "C", "23-25666777-9", 1);
            AssertAddressAt(juanPerez, "Alem", 1122, "CABA", 1001, "CABA");
        }

        private void AssertPepeSanchezWasImportedCorrectly()
        {
            var pepeSanchez = _persistentCustomerSystem.CustomerIdentifiedAs("D", "22333444");
            AssertCustomer(pepeSanchez, "Pepe", "Sanchez", "D", "22333444", 2);
            AssertAddressAt(pepeSanchez, "San Martin", 3322, "Olivos", 1636, "BsAs");
            AssertAddressAt(pepeSanchez, "Maipu", 888, "Florida", 1122, "Buenos Aires");
        }

        private void AssertCustomer(Customer customer, string firstName, string lastName, 
            string identificationType, string identificationNumber, int numberOfAddresses)
        {
            Assert.AreEqual(firstName, customer.FirstName);
            Assert.AreEqual(lastName, customer.LastName);
            Assert.AreEqual(identificationType, customer.IdentificationType);
            Assert.AreEqual(identificationNumber, customer.IdentificationNumber);
            Assert.AreEqual(numberOfAddresses, customer.NumberOfAddress());
        }

        private void AssertAddressAt(Customer customer, string aStreetName, int streetNumber, 
            string town, int zipCode, string province)
        {
            var address = customer.AddressAt(aStreetName);
            Assert.AreEqual(streetNumber, address.StreetNumber);
            Assert.AreEqual(town, address.Town);
            Assert.AreEqual(zipCode, address.ZipCode);
            Assert.AreEqual(province, address.Province);
        }

        private StringReader ValidDataStream(string data)
        {
            var inputStream = new StringReader(data);
            return inputStream;
        }
    }
}
