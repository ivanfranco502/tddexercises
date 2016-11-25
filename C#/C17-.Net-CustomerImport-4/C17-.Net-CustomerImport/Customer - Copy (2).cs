using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using FluentNHibernate.Automapping;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Tool.hbm2ddl;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace com.tenpines.advancetdd
{
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

        public virtual bool IsIdentifiedAs(string identificationType, string identificationNumber)
        {
            return this.IdentificationType.Equals(identificationType) &&
                   this.IdentificationNumber.Equals(identificationNumber);
        }
    }

    public class CustomerImporter
    {
        private readonly TextReader _stream;
        private readonly CustomerSystem _system;
        private string[] _record;
        private Customer _newCustomer;
        private string _readLine;

        public const string CustomerNotDefined = "Can not import Address without Customer";
        public const string UnknowRecordType = "Unknow record type";
        public const string InvalidAddressRecord = "Invalid Address Record";
        public const string InvalidCustomerRecord = "Invalid Customer Record";

        public CustomerImporter(TextReader stream, CustomerSystem system)
        {
            this._stream = stream;
            this._system = system;
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

    public abstract class CustomerSystem
    {
        public const string CustomerNotFound = "Customer not found";
        public abstract void BeginTransaction();
        public abstract void OpenSession();
        public abstract void Commit();
        public abstract void CloseSession();
        public abstract int NumberOfCustomers();
        public abstract Customer CustomerIdentifiedAs(string identificationType, string identificationNumber);
        public abstract void AddCustomer(Customer customer);
    }

    public class TransientCustomerSystem : CustomerSystem
    {
        private IList<Customer> _customers = new List<Customer>();

        public override void BeginTransaction()
        {
        }

        public override void OpenSession()
        {
        }

        public override void Commit()
        {
        }

        public override void CloseSession()
        {
        }

        public override int NumberOfCustomers()
        {
            return _customers.Count;
        }

        public override Customer CustomerIdentifiedAs(string identificationType, string identificationNumber)
        {
            Customer foundCustomer = _customers.SingleOrDefault(
                customer => customer.IsIdentifiedAs(identificationType, identificationNumber));
            if (foundCustomer==null) throw new Exception(CustomerNotFound);
            return foundCustomer;
        }

        public override void AddCustomer(Customer customer)
        {
            _customers.Add(customer);
        }
    }

    public class PersistentCustomerSystem : CustomerSystem
    {
        public ISession _session;
        public ITransaction _transaction;

        public override void BeginTransaction()
        {
            _transaction = _session.BeginTransaction();
        }

        public override void OpenSession()
        {
            var storeConfiguration = new StoreConfiguration();
            var configuration = Fluently.Configure()
                                        .Database(
                                            MsSqlCeConfiguration.Standard.ShowSql()
                                                                .ConnectionString("Data Source=CustomerImport.sdf"))
                                        .Mappings(m => m.AutoMappings.Add(AutoMap.AssemblyOf<Customer>(storeConfiguration)
                                                                              .Override<Customer>(
                                                                                  map =>
                                                                                  map.HasMany(x => x.Addresses).Cascade.All())));

            var sessionFactory = configuration.BuildSessionFactory();
            new SchemaExport(configuration.BuildConfiguration()).Execute(true, true, false);
            _session = sessionFactory.OpenSession();
        }

        public override void Commit()
        {
            _transaction.Commit();
        }

        public override void CloseSession()
        {
            _session.Close();
        }

        public override int NumberOfCustomers()
        {
            return _session.CreateCriteria<Customer>().List<Customer>().Count;
        }

        public override Customer CustomerIdentifiedAs(string identificationType, string identificationNumber)
        {
            var customers = _session.CreateCriteria<Customer>().
                Add(Restrictions.Eq("IdentificationType", identificationType)).
                Add(Restrictions.Eq("IdentificationNumber", identificationNumber)).List<Customer>();
            Assert.AreEqual(1, customers.Count);

            return customers[0];
        }

        public override void AddCustomer(Customer customer)
        {
            _session.Persist(customer);
        }
    }

    public abstract class Environment
    {
        public abstract CustomerSystem CreateCustomerSystem();

        public static Environment Current()
        {
            if (DevelopmentEnvironment.IsCurrent())
                return new DevelopmentEnvironment ();
            else if (IntegrationEnvironment.IsCurrent())
                return new IntegrationEnvironment();
            else 
                throw new Exception("Invalid Environment");

        }

        public abstract SupplierSystem CreateSupplierSystem();
    }

    public class IntegrationEnvironment: Environment
    {
        public static bool IsCurrent()
        {
            return !DevelopmentEnvironment.IsCurrent();
        }

        public override CustomerSystem CreateCustomerSystem()
        {
            return new PersistentCustomerSystem();
        }

        public override SupplierSystem CreateSupplierSystem()
        {
            throw new NotImplementedException();
        }
    }

    public class DevelopmentEnvironment: Environment
    {
        public static bool IsCurrent()
        {
            return true;
        }
        
        public override CustomerSystem CreateCustomerSystem()
        {
            return new TransientCustomerSystem();
        }

        public override SupplierSystem CreateSupplierSystem()
        {
            return new TransientSupplierSystem(new TransientCustomerSystem());
        }
    }

    [TestClass]
    public class CustomerImportTest
    {
        private CustomerSystem _system;

        [TestInitialize]
        public void SetUp()
        {
            _system = Environment.Current().CreateCustomerSystem();
            _system.OpenSession();
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
}
