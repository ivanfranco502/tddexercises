using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentNHibernate.Automapping;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace com.tenpines.advancetdd
{

    public class StoreConfiguration : DefaultAutomappingConfiguration
    {
        public override bool ShouldMap(Type type)
        {
            return type == typeof(Customer) || type == typeof(Address) || type == typeof(Supplier);
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

        public virtual bool AddressesIsEmpty()
        {
            return Addresses.Count==0;
        }

        public virtual bool IsAt(string anIdentificationType, string anIdentificationNumber)
        {
            return IdentificationNumber.Equals(anIdentificationNumber) &&
                   IdentificationType.Equals(anIdentificationType);
        }
    }

    public class Supplier
    {
        public virtual long Id { get; set; }
        public virtual string Name { get; set; }
        public virtual string IdentificationType { get; set; }
        public virtual string IdentificationNumber { get; set; }
        public virtual IList<Customer> Customers { get; set; }
        public virtual IList<Address> Addresses { get; set; }

        public Supplier()
        {
            Customers = new List<Customer>();
            Addresses = new List<Address>();
        }

        public virtual void AddCustomer(Customer aCustomer)
        {
            Customers.Add(aCustomer);
        }

        public virtual void AddAddress(Address anAddress)
        {
            Addresses.Add(anAddress);
        }

        public virtual int NumberOfCustomers()
        {
            return Customers.Count;
        }

        public virtual int NumberOfAddress()
        {
            return Addresses.Count;
        }

        public virtual bool CustomerAt(string anIdentificationType, string anIdentificationNumber)
        {
            return Customers.Any(aCustomer => aCustomer.IsAt(anIdentificationType, anIdentificationNumber));
        }
        public virtual Address AddressAt(string aStreetName)
        {
            return Addresses.First(anAddress => anAddress.IsAt(aStreetName));
        }

        public virtual bool CustomerIsEmpty()
        {
            return Customers.Count == 0;
        }

        public virtual bool AddressesIsEmpty()
        {
            return Addresses.Count == 0;
        }
    }

    public class CustomerImporter
    {
        private readonly TextReader _stream;
        private string[] _record;
        private Customer _newCustomer;
        private string _readLine;
        private ICustomerSystem _system;

        public const string CustomerNotDefined = "Can not import Address without Customer";
        public const string UnknowRecordType = "Unknow record type";
        public const string InvalidAddressRecord = "Invalid Address Record";
        public const string InvalidCustomerRecord = "Invalid Customer Record";

        public CustomerImporter(TextReader stream, ICustomerSystem system)
        {
            _system = system;
            this._stream = stream;
        }

        public void Value()
        {
            while (NotAtEndOfFile())
            {
                ParseLine();
                ImportRecord();
            }
        }

        private void ImportRecord()
        {
            if (IsCustomerRecord())
                ImportCustomer();
            else if (IsAddressRecord())
                ImportAddress();
            else
                throw new Exception(UnknowRecordType);
        }

        private void ParseLine()
        {
            _record = _readLine.Split(',');
        }

        private bool NotAtEndOfFile()
        {
            _readLine = _stream.ReadLine();
            return _readLine != null;
        }

        private bool IsAddressRecord()
        {
            return _record[0]=="A";
        }

        private bool IsCustomerRecord()
        {
            return _record[0] == "C";
        }

        private void ImportAddress()
        {
            if (HasNotImportedCustomer()) throw new Exception(CustomerNotDefined);
            if (AddressRecordSizeIsNotCorrect()) throw new Exception(InvalidAddressRecord);

            var newAddress = new Address
                {
                    StreetName = _record[1],
                    StreetNumber = Int32.Parse(_record[2]),
                    Town = _record[3],
                    ZipCode = Int32.Parse(_record[4]),
                    Province = _record[5]
                };

            _newCustomer.AddAddress(newAddress);
        }

        private bool AddressRecordSizeIsNotCorrect()
        {
            return _record.Length!=6;
        }

        private bool HasNotImportedCustomer()
        {
            return _newCustomer == null;
        }

        private void ImportCustomer()
        {
            if (CustomerRecordSizeIsNotCorrect()) throw new Exception(InvalidCustomerRecord);

            _newCustomer = new Customer
                {
                    FirstName = _record[1],
                    LastName = _record[2],
                    IdentificationType = _record[3],
                    IdentificationNumber = _record[4]
                };
            _system.AddCustomer(_newCustomer);
        }

        private bool CustomerRecordSizeIsNotCorrect()
        {
            return _record.Length!=5;
        }
    }

    [TestClass]
    public class CustomerImportTest
    {
        private readonly ICustomerSystem _system = EnvironmentContext.Current.CreateCustomerSystem();

        [TestInitialize]
        public void SetUp()
        {
            // Bad Smell 4: Que el test conozca como conectarse!
            _system.GetSession();
            _system.BeginTransaction();
        }

        [TestCleanup]
        public void TearDown()
        {
            _system.Commit();
            _system.CloseSession();
        }

        [TestMethod]
        public void TestImportsValidDataCorrectly()
        {
            using (var inputStream = ValidDataStream())
            {
                new CustomerImporter(inputStream, _system).Value();

                Assert.AreEqual(2, _system.NumberOfCustomers());

                AssertPepeSanchezWasImportedCorrectly();
                AssertJuanPerezWasImportedCorrectly();
            }
        }

        [TestMethod]
        public void TestCanNotImportAddressWithoutCustomer()
        {
            ImportingShouldFailAndAssert(AddressWithoutCustomerData(), e =>
                {
                    Assert.AreEqual(CustomerImporter.CustomerNotDefined, e.Message);
                    AssertNoCustomerWereImported();
                });
        }

        [TestMethod]
        public void TestDoesNotImportRecordsStartingWithCButMoreCharacters()
        {
            ImportingShouldFailAndAssert(InvalidCustomerRecordStart(), e =>
                {
                    Assert.AreEqual(CustomerImporter.UnknowRecordType, e.Message);
                    AssertNoCustomerWereImported();
                });
        }

        [TestMethod]
        public void TestDoesNotImportRecordsStartingWithAButMoreCharacters()
        {
            ImportingShouldFailAndAssert(InvalidAddressRecordStart(), e =>
                {
                    Assert.AreEqual(CustomerImporter.UnknowRecordType, e.Message);
                    var pepeSanchez = _system.CustomerIdentifiedAs("D", "22333444");
                    Assert.IsTrue(pepeSanchez.AddressesIsEmpty());
                });
        }

        [TestMethod]
        public void TestLinesCanNotBeEmpty()
        {
            ImportingShouldFailAndAssert(EmptyLine(), e =>
            {
                Assert.AreEqual(CustomerImporter.UnknowRecordType, e.Message);
                AssertNoCustomerWereImported();
            });
        }

        [TestMethod]
        public void TestAddessIsEmptyReturnsFalseWhenNotEmpty()
        {
            var customerWithAddress = new Customer();
            customerWithAddress.AddAddress(new Address());
            Assert.IsFalse(customerWithAddress.AddressesIsEmpty());
        }

        [TestMethod]
        public void TestCanNotImportAddressRecordWithLessThanSixFields()
        {
            ImportingShouldFailAndAssert(AddressRecordWithLessThanSixFields(), e =>
            {
                Assert.AreEqual(CustomerImporter.InvalidAddressRecord, e.Message);
                var pepeSanchez = _system.CustomerIdentifiedAs("D", "22333444");
                Assert.IsTrue(pepeSanchez.AddressesIsEmpty());
            });
        }

        [TestMethod]
        public void TestCanNotImportAddressRecordWithMoreThanSixFields()
        {
            ImportingShouldFailAndAssert(AddressRecordWithMoreThanSixFields(), e =>
            {
                Assert.AreEqual(CustomerImporter.InvalidAddressRecord, e.Message);
                var pepeSanchez = _system.CustomerIdentifiedAs("D", "22333444");
                Assert.IsTrue(pepeSanchez.AddressesIsEmpty());
            });
        }

        [TestMethod]
        public void TestCanNotImportCustomerRecordWithLessThanFiveFields()
        {
            ImportingShouldFailAndAssert(CustomerRecordWithLessThanFiveFields(), e =>
                {
                    Assert.AreEqual(CustomerImporter.InvalidCustomerRecord, e.Message);
                    AssertNoCustomerWereImported();
                });
        }

        private void AssertNoCustomerWereImported()
        {
            Assert.AreEqual(0, _system.NumberOfCustomers());
        }

        [TestMethod]
        public void TestCanNotImportCustomerRecordWithMoreThanFiveFields()
        {
            ImportingShouldFailAndAssert(CustomerRecordWithMoreThanFiveFields(), e =>
            {
                Assert.AreEqual(CustomerImporter.InvalidCustomerRecord, e.Message);
                Assert.AreEqual(0, _system.NumberOfCustomers());
            });
        }

        private TextReader CustomerRecordWithLessThanFiveFields()
        {
            return new StringReader("C,Pepe,Sanchez,D\n");
        }

        private TextReader CustomerRecordWithMoreThanFiveFields()
        {
            return new StringReader("C,Pepe,Sanchez,D,22,x\n");
        }

        private TextReader AddressRecordWithMoreThanSixFields()
        {
            return new StringReader("C,Pepe,Sanchez,D,22333444\n" +
                                    "A,San Martin,3322,Olivos,1636,BsAs,x\n");
        }
        private TextReader AddressRecordWithLessThanSixFields()
        {
            return new StringReader("C,Pepe,Sanchez,D,22333444\n" +
                                    "A,San Martin,3322,Olivos,1636\n");
        }

        private TextReader EmptyLine()
        {
            return new StringReader("\n");
        }

        private void ImportingShouldFailAndAssert(TextReader inputStream, Action<Exception> exceptionBlock)
        {
            using (inputStream)
            {
                try
                {
                    new CustomerImporter(inputStream, _system).Value();
                    Assert.Fail();
                }
                catch (Exception e)
                {
                    exceptionBlock(e);
                }
            }
        }

        private TextReader InvalidAddressRecordStart()
        {
            return new StringReader("C,Pepe,Sanchez,D,22333444\n" +
                                    "AA,San Martin,3322,Olivos,1636,BsAs\n");
        }

        private TextReader InvalidCustomerRecordStart()
        {
            return new StringReader("CC,Pepe,Sanchez,D,22333444\n");
        }

        private TextReader AddressWithoutCustomerData()
        {
            return new StringReader("A,San Martin,3322,Olivos,1636,BsAs\n");
        }

        private void AssertJuanPerezWasImportedCorrectly()
        {
            var juanPerez = _system.CustomerIdentifiedAs("C", "23-25666777-9");
            AssertCustomer(juanPerez, "Juan", "Perez", "C", "23-25666777-9", 1);
            AssertAddressAt(juanPerez, "Alem", 1122, "CABA", 1001, "CABA");
        }

        private void AssertPepeSanchezWasImportedCorrectly()
        {
            var pepeSanchez = _system.CustomerIdentifiedAs("D", "22333444");
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

        private StringReader ValidDataStream()
        {
            var inputStream = new StringReader("C,Pepe,Sanchez,D,22333444\n" +
                                               "A,San Martin,3322,Olivos,1636,BsAs\n" +
                                               "A,Maipu,888,Florida,1122,Buenos Aires\n" +
                                               "C,Juan,Perez,C,23-25666777-9\n" +
                                               "A,Alem,1122,CABA,1001,CABA");
            return inputStream;
        }
    }


    public class SupplierImporter
    {
        private readonly TextReader _stream;
        private string[] _record;
        private Supplier _newSupplier;
        private string _readLine;
        private ISupplierSystem _system;

        public const string SupplierNotDefined = "Can not import Customer without Supplier";
        public const string UnknowRecordType = "Unknow record type";
        public const string InvalidAddressRecord = "Invalid Address Record";
        public const string InvalidCustomerRecord = "Invalid Customer Record";
        public const string InvalidSupplierRecord = "Invalid Supplier Record";
        public const string NotExistCustomerRecord = "No Exists Customer Record";

        public SupplierImporter(TextReader stream, ISupplierSystem system)
        {
            _system = system;
            this._stream = stream;
        }

        public void Value()
        {
            while (NotAtEndOfFile())
            {
                ParseLine();
                ImportRecord();
            }
        }

        private void ImportRecord()
        {
            if (IsSupplierRecord())
                ImportSupplier();
            else if (IsCustomerRecord())
                ImportCustomer();
            else if (IsExistingCustomerRecord())
                ImportExistingCustomer();
            else if (IsAddressRecord())
                ImportAddress();
            else
                throw new Exception(UnknowRecordType);
        }

        private void ImportExistingCustomer()
        {
            _newSupplier.AddCustomer(_system.CustomerIdentifiedAs(_record[1], _record[2]));
        }

        private bool IsExistingCustomerRecord()
        {
            return _record[0] == "EC";
        }

        private bool IsSupplierRecord()
        {
            return _record[0] == "S";
        }

        private void ImportSupplier()
        {
            if (SupplierRecordSizeIsNotCorrect()) throw new Exception(InvalidSupplierRecord);

            _newSupplier = new Supplier
            {
                Name = _record[1],
                IdentificationType = _record[2],
                IdentificationNumber = _record[3]
            };
            _system.AddSupplier(_newSupplier);
        }

        private void ParseLine()
        {
            _record = _readLine.Split(',');
        }

        private bool NotAtEndOfFile()
        {
            _readLine = _stream.ReadLine();
            return _readLine != null;
        }

        private bool IsAddressRecord()
        {
            return _record[0] == "A";
        }

        private bool IsCustomerRecord()
        {
            return _record[0] == "NC";
        }

        private void ImportAddress()
        {
            if (HasNotImportedSupplier()) throw new Exception(SupplierNotDefined);
            if (AddressRecordSizeIsNotCorrect()) throw new Exception(InvalidAddressRecord);

            var newAddress = new Address
            {
                StreetName = _record[1],
                StreetNumber = Int32.Parse(_record[2]),
                Town = _record[3],
                ZipCode = Int32.Parse(_record[4]),
                Province = _record[5]
            };

            _newSupplier.AddAddress(newAddress);
        }

        private bool AddressRecordSizeIsNotCorrect()
        {
            return _record.Length != 6;
        }

        private bool HasNotImportedSupplier()
        {
            return _newSupplier == null;
        }

        private void ImportCustomer()
        {
            if (HasNotImportedSupplier()) throw new Exception(SupplierNotDefined);
            if (CustomerRecordSizeIsNotCorrect()) throw new Exception(InvalidCustomerRecord);

            var newCustomer = new Customer
            {
                FirstName = _record[1],
                LastName = _record[2],
                IdentificationType = _record[3],
                IdentificationNumber = _record[4]
            };

            _newSupplier.AddCustomer(newCustomer);
        }

        private bool SupplierRecordSizeIsNotCorrect()
        {
            return _record.Length != 4;
        }

        private bool CustomerRecordSizeIsNotCorrect()
        {
            return _record.Length != 5;
        }
    }

    [TestClass]
    public class SupplierImportTest
    {
        private readonly ISupplierSystem _system = EnvironmentContext.Current.CreateSupplierSystem();

        [TestInitialize]
        public void SetUp()
        {
            // Bad Smell 4: Que el test conozca como conectarse!
            _system.GetSession();
            _system.BeginTransaction();
        }

        [TestCleanup]
        public void TearDown()
        {
            _system.Commit();
            _system.CloseSession();
        }

        [TestMethod]
        public void TestImportsValidDataCorrectly()
        {
            using (var inputStream = ValidDataStream())
            {
                new SupplierImporter(inputStream, _system).Value();

                Assert.AreEqual(1, _system.NumberOfSuppliers());

                AssertSupplier1WasImportedCorrectly();
            }
        }

        [TestMethod]
        public void TestCanNotImportAddressWithoutSupplier()
        {
            ImportingShouldFailAndAssert(AddressWithoutSupplierData(), e =>
            {
                Assert.AreEqual(SupplierImporter.SupplierNotDefined, e.Message);
                AssertNoSuppliersWhereImported();
            });
        }

        [TestMethod]
        public void TestCanNotImportCustomerWithoutSupplier()
        {
            ImportingShouldFailAndAssert(CustomerWithoutSupplierData(), e =>
            {
                Assert.AreEqual(SupplierImporter.SupplierNotDefined, e.Message);
                AssertNoSuppliersWhereImported();
            });
        }

        [TestMethod]
        public void TestDoesNotImportRecordsStartingWithSButMoreCharacters()
        {
            ImportingShouldFailAndAssert(InvalidSupplierRecordStart(), e =>
            {
                Assert.AreEqual(SupplierImporter.UnknowRecordType, e.Message);
                AssertNoSuppliersWhereImported();
            });
        }

        [TestMethod]
        public void TestDoesNotImportRecordsStartingWithNCButMoreCharacters()
        {
            ImportingShouldFailAndAssert(InvalidCustomerRecordStart(), e =>
            {
                Assert.AreEqual(SupplierImporter.UnknowRecordType, e.Message);
                var supplier = _system.SupplierIdentifiedAs("D", "123");
                Assert.IsTrue(supplier.CustomerIsEmpty());
            });
        }

        [TestMethod]
        public void TestDoesNotImportRecordsStartingWithAButMoreCharacters()
        {
            ImportingShouldFailAndAssert(InvalidAddressRecordStart(), e =>
            {
                Assert.AreEqual(SupplierImporter.UnknowRecordType, e.Message);
                var supplier = _system.SupplierIdentifiedAs("D", "123");
                Assert.IsTrue(supplier.AddressesIsEmpty());
            });
        }

        [TestMethod]
        public void TestLinesCanNotBeEmpty()
        {
            ImportingShouldFailAndAssert(EmptyLine(), e =>
            {
                Assert.AreEqual(SupplierImporter.UnknowRecordType, e.Message);
                AssertNoSuppliersWhereImported();
            });
        }

        [TestMethod]
        public void TestAddressIsEmptyReturnsFalseWhenNotEmpty()
        {
            var customerWithAddress = new Customer();
            customerWithAddress.AddAddress(new Address());
            Assert.IsFalse(customerWithAddress.AddressesIsEmpty());
        }

        [TestMethod]
        public void TestCanNotImportAddressRecordWithLessThanSixFields()
        {
            ImportingShouldFailAndAssert(AddressRecordWithLessThanSixFields(), e =>
            {
                Assert.AreEqual(SupplierImporter.InvalidAddressRecord, e.Message);
                var supplier = _system.SupplierIdentifiedAs("D", "123");
                Assert.IsTrue(supplier.AddressesIsEmpty());
            });
        }

        [TestMethod]
        public void TestCanNotImportAddressRecordWithMoreThanSixFields()
        {
            ImportingShouldFailAndAssert(AddressRecordWithMoreThanSixFields(), e =>
            {
                Assert.AreEqual(SupplierImporter.InvalidAddressRecord, e.Message);
                var supplier = _system.SupplierIdentifiedAs("D", "123");
                Assert.IsTrue(supplier.AddressesIsEmpty());
            });
        }

        [TestMethod]
        public void TestCanNotImportCustomerRecordWithLessThanFiveFields()
        {
            ImportingShouldFailAndAssert(CustomerRecordWithLessThanFiveFields(), e =>
            {
                Assert.AreEqual(SupplierImporter.InvalidCustomerRecord, e.Message);
                var supplier = _system.SupplierIdentifiedAs("D", "123");
                Assert.IsTrue(supplier.CustomerIsEmpty());
            });
        }

        private void AssertNoSuppliersWhereImported()
        {
            Assert.AreEqual(0, _system.NumberOfSuppliers());
        }

        [TestMethod]
        public void TestCanNotImportCustomerRecordWithMoreThanFiveFields()
        {
            ImportingShouldFailAndAssert(CustomerRecordWithMoreThanFiveFields(), e =>
            {
                Assert.AreEqual(SupplierImporter.InvalidCustomerRecord, e.Message);
                var supplier = _system.SupplierIdentifiedAs("D", "123");
                Assert.IsTrue(supplier.CustomerIsEmpty());
            });
        }

        [TestMethod]
        public void TestCanNotImportSupplierRecordWithLessThanFourFields()
        {
            ImportingShouldFailAndAssert(SupplierRecordWithLessThanFiveFields(), e =>
                {
                    Assert.AreEqual(SupplierImporter.InvalidSupplierRecord, e.Message);
                    Assert.AreEqual(0, _system.NumberOfSuppliers());
                });
        }

        [TestMethod]
        public void TestCanNotImportSupplierRecordWithMoreThanFourFields()
        {
            ImportingShouldFailAndAssert(SupplierRecordWithMoreThanFiveFields(), e =>
            {
                Assert.AreEqual(SupplierImporter.InvalidSupplierRecord, e.Message);
                Assert.AreEqual(0, _system.NumberOfSuppliers());
            });

        }

        [TestMethod]
        public void TestCanNotFindCustomerBecauseIsNotInSupplierSystem()
        {
            ImportingShouldFailAndAssert(SupplierRecordWithUnexistantCustomer(), e =>
            {
                Assert.AreEqual("There is not exist a Customer with the given ID", e.Message);
                Assert.AreEqual(1, _system.NumberOfSuppliers());
                var supplier = _system.SupplierIdentifiedAs("D", "123");
                Assert.IsTrue(supplier.CustomerIsEmpty());
            });
        }

        [TestMethod]
        public void TestMustFindExistingCustomerAndAddToNewSupplier()
        {
            using (var inputStream = ExistingCustomerRecord())
            {
                new SupplierImporter(inputStream, _system).Value();

                Assert.AreEqual(2, _system.NumberOfSuppliers());

                AssertSupplier1WasImportedCorrectly();
                AssertSupplier2WasImportedCorrectly();
            }

        }

        private TextReader ExistingCustomerRecord()
        {
            return new StringReader("S,Supplier1,D,123\n" +
                                    "NC,Pepe,Sanchez,D,22333444\n" +
                                    "A,San Martin,3322,Olivos,1636,BsAs\n" +
                                    "A,Maipu,888,Florida,1122,Buenos Aires\n"+
                                    "S,Supplier2,D,124\n" +
                                    "EC,D,22333444\n" +
                                    "A,San Martin,3322,Olivos,1636,BsAs\n");
        }

        private TextReader SupplierRecordWithUnexistantCustomer()
        {
            return new StringReader("S,Supplier,D,123\n"+
                                    "EC,D,5456774");
        }

        private TextReader SupplierRecordWithMoreThanFiveFields()
        {
            return new StringReader("S,Supplier,D,123,123");
        }

        private TextReader SupplierRecordWithLessThanFiveFields()
        {
            return new StringReader("S,Supplier");
        }

        private TextReader CustomerRecordWithLessThanFiveFields()
        {
            return new StringReader("S,Supplier,D,123\n"+
                                    "NC,Pepe,Sanchez,D\n");
        }

        private TextReader CustomerRecordWithMoreThanFiveFields()
        {
            return new StringReader("S,Supplier,D,123\n" +
                                    "NC,Pepe,Sanchez,D,22,x\n");
        }

        private TextReader AddressRecordWithMoreThanSixFields()
        {
            return new StringReader("S,Supplier,D,123\n" +
                                    "A,San Martin,3322,Olivos,1636,BsAs,x\n");
        }
        private TextReader AddressRecordWithLessThanSixFields()
        {
            return new StringReader("S,Supplier,D,123\n" +
                                    "A,San Martin,3322,Olivos,1636\n");
        }

        private TextReader EmptyLine()
        {
            return new StringReader("\n");
        }

        private void ImportingShouldFailAndAssert(TextReader inputStream, Action<Exception> exceptionBlock)
        {
            using (inputStream)
            {
                try
                {
                    new SupplierImporter(inputStream, _system).Value();
                    Assert.Fail();
                }
                catch (Exception e)
                {
                    exceptionBlock(e);
                }
            }
        }

        private TextReader InvalidCustomerRecordStart()
        {
            return new StringReader("S,Supplier1,D,123\n" +
                                    "NCC,Pepe,Sanchez,D,22333444\n");
        }

        private TextReader InvalidAddressRecordStart()
        {
            return new StringReader("S,Supplier1,D,123\n" +
                                    "AA,San Martin,3322,Olivos,1636,BsAs\n");
        }

        private TextReader InvalidSupplierRecordStart()
        {
            return new StringReader("SS,Supplier1,D,123\n");
        }

        private TextReader CustomerWithoutSupplierData()
        {
            return new StringReader("NC,Pepe,Sanchez,D,22333444\n");
        }

        private TextReader AddressWithoutSupplierData()
        {
            return new StringReader("A,San Martin,3322,Olivos,1636,BsAs\n");
        }

        private void AssertSupplier1WasImportedCorrectly()
        {
            var supplier1 = _system.SupplierIdentifiedAs("D", "123");
            AssertSupplier(supplier1, "Supplier1", "D", "123",1,2);
            AssertCustomer(supplier1.Customers[0], "Pepe", "Sanchez", "D", "22333444");
            AssertAddressAt(supplier1, "San Martin", 3322, "Olivos", 1636, "BsAs");
            AssertAddressAt(supplier1, "Maipu", 888, "Florida", 1122, "Buenos Aires");
        }

        private void AssertSupplier2WasImportedCorrectly()
        {
            var supplier2 = _system.SupplierIdentifiedAs("D", "124");
            AssertSupplier(supplier2, "Supplier2", "D", "124", 1, 1);
            AssertCustomer(supplier2.Customers[0], "Pepe", "Sanchez", "D", "22333444");
            AssertAddressAt(supplier2, "San Martin", 3322, "Olivos", 1636, "BsAs");
        }


        private void AssertSupplier(Supplier supplier, string supplierName, string identificationType, string identificationNumber, int numberOfCustomers, int numberOfAddresses)
        {
            Assert.AreEqual(supplierName, supplier.Name);
            Assert.AreEqual(identificationType, supplier.IdentificationType);
            Assert.AreEqual(identificationNumber, supplier.IdentificationNumber);
            Assert.AreEqual(numberOfCustomers, supplier.NumberOfCustomers());
            Assert.AreEqual(numberOfAddresses, supplier.NumberOfAddress());
        }

        private void AssertCustomer(Customer customer, string firstName, string lastName, string identificationType, string identificationNumber)
        {
            Assert.AreEqual(firstName, customer.FirstName);
            Assert.AreEqual(lastName, customer.LastName);
            Assert.AreEqual(identificationType, customer.IdentificationType);
            Assert.AreEqual(identificationNumber, customer.IdentificationNumber);
        }

        private void AssertAddressAt(Supplier supplier, string aStreetName, int streetNumber, string town, int zipCode, string province)
        {
            var address = supplier.AddressAt(aStreetName);
            Assert.AreEqual(streetNumber, address.StreetNumber);
            Assert.AreEqual(town, address.Town);
            Assert.AreEqual(zipCode, address.ZipCode);
            Assert.AreEqual(province, address.Province);
        }

        private StringReader ValidDataStream()
        {
            var inputStream = new StringReader("S,Supplier1,D,123\n" +
                                               "NC,Pepe,Sanchez,D,22333444\n" +
                                               "A,San Martin,3322,Olivos,1636,BsAs\n" +
                                               "A,Maipu,888,Florida,1122,Buenos Aires\n");
            return inputStream;
        }
    }

}
