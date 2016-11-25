using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using com.tenpines.advancetdd;
using FluentNHibernate.Automapping;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Tool.hbm2ddl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Environment = com.tenpines.advancetdd.Environment;

namespace com.tenpines.advancetdd
{

    public class StoreConfiguration : DefaultAutomappingConfiguration
    {
        public override bool ShouldMap(Type type)
        {
            return type == typeof (Customer) || type == typeof (Address) || type == typeof(Supplier);
        }
    }

    public class Supplier
    {
	    public virtual long Id { get; set; }
        public virtual string Name { get; set; }
        public virtual string IdentificationType { get; set; }
        public virtual string IdentificationNumber { get; set; }
        public virtual IList<Address> Addresses { get; set; }
        public virtual IList<Customer> Customers { get; set; }

        public Supplier()
        {
            Addresses = new List<Address>();
            Customers = new List<Customer>();
        }

        public virtual void AddAddress(Address anAddress)
        {
            Addresses.Add(anAddress);
        }

        public virtual int NumberOfAddresses()
        {
            return Addresses.Count;
        }

        public virtual int NumberOfCustomers()
        {
            return Customers.Count;
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

        public virtual void AddCustomer(Customer newCustomer)
        {
            Customers.Add(newCustomer);
        }

        public virtual bool ContainsCustomer(Customer customer)
        {
            return Customers.Contains(customer);
        }
    }

    public class SupplierImporter
    {
        private readonly TextReader _stream;
        private readonly SupplierSystem _system;
        private string[] _record;
        private Supplier _newSupplier;
        private string _readLine;

        public const string SupplierNotDefined = "Can not import Address without Supplier";
        public const string UnknowRecordType = "Unknow record type";
        public const string InvalidAddressRecord = "Invalid Address Record";
        public const string InvalidCustomerRecord = "Invalid Customer Record";
        public const string InvalidSupplierRecord = "Invalid Supplier Record";

        public SupplierImporter(TextReader stream, SupplierSystem _system)
        {
            this._stream = stream;
            this._system = _system;
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
            else if (IsNewCustomerRecord())
                ParseNewCustomer();
            else if (IsExistingCustomerRecord())
                ParseExistingCustomer();
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
            return _record[0] == "A";
        }

        public bool IsNewCustomerRecord()
        {
            return _record[0] == "NC";
        }

        public bool IsExistingCustomerRecord()
        {
            return _record[0] == "EC";
        }

        private bool IsSupplierRecord()
        {
            return _record[0] == "S";
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

        public void ParseNewCustomer()
        {
            if (CustomerRecordSizeIsNotCorrect()) throw new Exception(InvalidCustomerRecord);
            if (HasNotImportedSupplier()) throw new Exception(SupplierNotDefined);

            Customer newCustomer = new Customer
            {
                FirstName = _record[1],
                LastName = _record[2],
                IdentificationType = _record[3],
                IdentificationNumber = _record[4]
            };
            _system.CustomerSystem().AddCustomer(newCustomer);
            _newSupplier.AddCustomer(newCustomer);
        }

        public void ParseExistingCustomer()
        {
            if (InvalidExistingCustomerRecordSize()) throw new Exception(InvalidCustomerRecord);
            if (HasNotImportedSupplier()) throw new Exception(SupplierNotDefined);

            Customer newCustomer = _system.CustomerSystem().CustomerIdentifiedAs(
                    _record[1], _record[2]);
            _newSupplier.AddCustomer(newCustomer);
        }

        public bool InvalidExistingCustomerRecordSize()
        {
            return _record.Length != 3;
        }


        private bool CustomerRecordSizeIsNotCorrect()
        {
            return _record.Length != 5;
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

        private bool SupplierRecordSizeIsNotCorrect()
        {
            return _record.Length != 4;
        }
    }


    [TestClass]
    public class SupplierImportTest
    {
        private SupplierSystem _system;
        private ErpSystem _erpSystem;

        [TestInitialize]
        public void SetUp()
        {
            _erpSystem = Environment.Current().CreateErpSystem();
            _system = _erpSystem.SupplierSystem();
            _erpSystem.OpenSession();
            _erpSystem.BeginTransaction();
        }

        [TestCleanup]
        public void TearDown()
        {
            _erpSystem.Commit();
            _erpSystem.CloseSession();
        }
      
        [TestMethod]
        public void ImportsSupplierCorrectly() 
        {
		    new SupplierImporter(ValidOneSupplier(),_system).Value();

		    Assert.AreEqual(1,_system.NumberOfSuppliers());
		    AssertSanchezWasImportedCorrectly();
	    }
	
	    private void AssertSanchezWasImportedCorrectly() 
        {			
		    Supplier supplier = _system.SupplierIdentifiedAs("D", "22333444");
		    Assert.AreEqual("Sanchez",supplier.Name);
		    Assert.AreEqual("D",supplier.IdentificationType);
		    Assert.AreEqual("22333444",supplier.IdentificationNumber);
	
		    Assert.AreEqual(0,supplier.NumberOfAddresses());
		    Assert.AreEqual(0,supplier.NumberOfCustomers());
		}

	    public TextReader ValidOneSupplier() 
        {
		    return new StringReader("S,Sanchez,D,22333444\n");
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

        [TestMethod]
        public void TestDoesNotImportRecordsStartingWithSButMoreCharacters()
        {

            ImportingShouldFailAndAssert(InvalidSupplierRecordStartData(), e =>
            {
                Assert.AreEqual(SupplierImporter.UnknowRecordType, e.Message);
                Assert.AreEqual(0, _system.NumberOfSuppliers());
            });
        }
	
	    public TextReader InvalidSupplierRecordStartData()
        {
		    return new StringReader("SS,Sanchez,D,22333444\n");
	    }
	
	    [TestMethod]
	    public void TestCanNotImportSupplierRecordWithLessThanFourFields()
	    {
	        ImportingShouldFailAndAssert(SupplierRecordWithLessThanFourFields(), e =>
	        {
	            Assert.AreEqual(SupplierImporter.InvalidSupplierRecord, e.Message);
	            Assert.AreEqual(0, _system.NumberOfSuppliers());
	        });

	    }
	
	    public TextReader SupplierRecordWithLessThanFourFields() {
		    return new StringReader("S,Sanchez,D\n");
	    }	

	    [TestMethod]
	    public void TestCanNotImportSupplierRecordWithMoreThanFourFields()
	    {
	        ImportingShouldFailAndAssert(SupplierRecordWithMoreThanFourFields(), e =>
	        {
	            Assert.AreEqual(SupplierImporter.InvalidSupplierRecord, e.Message);
	            Assert.AreEqual(0, _system.NumberOfSuppliers());
	        });

	    }
	
	    public TextReader SupplierRecordWithMoreThanFourFields() {
		    return new StringReader("S,Sanchez,D,22,xx\n");
	    }	

	    [TestMethod]
	    public void CanNotImportAddressWithoutSupplier()
	    {
	        ImportingShouldFailAndAssert(AddressWithoutSupplierData(), e =>
	        {
	            Assert.AreEqual(
	                SupplierImporter.SupplierNotDefined, e.Message);
	            Assert.AreEqual(0, _system.NumberOfSuppliers());
	        });
	    }

	    public StringReader AddressWithoutSupplierData() {
		    return new StringReader("A,San Martin,3322,Olivos,1636,BsAs\n");
	    }
	
	    [TestMethod]
        public void TestDoesNotImportRecordsStartingWithAButMoreCharacters()
	    {
	        ImportingShouldFailAndAssert(InvalidAddressRecordStartData(), e =>
	        {
	            Assert.AreEqual(SupplierImporter.UnknowRecordType, e.Message);
	            Supplier supplier = _system.SupplierIdentifiedAs("D", "22333444");
	            Assert.IsTrue(supplier.AddressesIsEmpty());
	        });
	    }

	    public TextReader InvalidAddressRecordStartData() {
		    return new StringReader("S,Sanchez,D,22333444\n"+
                "AA,San Martin,3322,Olivos,1636,BsAs\n");
	    }

	    [TestMethod]
	    public void TestCanNotImportAddressRecordWithLessThanSixFields()
	    {
	        ImportingShouldFailAndAssert(AddressRecordWithLessThanSixFields(), e =>
	        {
	            Assert.AreEqual(SupplierImporter.InvalidAddressRecord, e.Message);
	            Supplier supplier = _system.SupplierIdentifiedAs("D", "22333444");
	            Assert.IsTrue(supplier.AddressesIsEmpty());
	        });

	    }

	    public StringReader AddressRecordWithLessThanSixFields() {
		    return new StringReader("S,Sanchez,D,22333444\n"+
	            "A,San Martin,3322,Olivos,1636\n");
	    }

	    [TestMethod]
	    public void TestCanNotImportAddressRecordWithMoreThanSixFields()
	    {
	        ImportingShouldFailAndAssert(AddressRecordWithMoreThanSixFields(), e =>
	        {
	            Assert.AreEqual(SupplierImporter.InvalidAddressRecord, e.Message);
	            Supplier supplier = _system.SupplierIdentifiedAs("D", "22333444");
	            Assert.IsTrue(supplier.AddressesIsEmpty());
	        });

	    }
	
	    public TextReader AddressRecordWithMoreThanSixFields() {
		    return new StringReader("S,Sanchez,D,22333444\n"
                +"A,San Martin,3322,Olivos,1636,BsAs,xx\n");
	    }	
	
	    [TestMethod]
	    public void ImportsAddressCorrectly() {
		    new SupplierImporter(ValidSupplierWithAddress(),_system).Value();

		    Assert.AreEqual(1,_system.NumberOfSuppliers());
		    AssertSanchezWithAddressWasImportedCorrectly();
	    }
	
	    private void AssertSanchezWithAddressWasImportedCorrectly() {			
		    Supplier supplier = _system.SupplierIdentifiedAs("D", "22333444");
	
		    Assert.AreEqual(1,supplier.NumberOfAddresses());
		    Address address = supplier.AddressAt("San Martin");
		    Assert.AreEqual(3322,address.StreetNumber);
		    Assert.AreEqual("Olivos", address.Town);
		    Assert.AreEqual(1636, address.ZipCode);
		    Assert.AreEqual("BsAs", address.Province);
		
		    Assert.AreEqual(0,supplier.NumberOfCustomers());
		}

	    public StringReader ValidSupplierWithAddress() {
		    return new StringReader("S,Sanchez,D,22333444\n"+
                "A,San Martin,3322,Olivos,1636,BsAs\n");
	    }	
	
	    [TestMethod]
	    public void ImportsNewCustomerCorrectly() {
		    new SupplierImporter(ValidSupplierWithAddressAndCustomer(),_system).Value();

		    Assert.AreEqual(1,_system.NumberOfSuppliers());
		    assertSanchezWithAddressAndCustomerWasImportedCorrectly();
	    }
	
	    private void assertSanchezWithAddressAndCustomerWasImportedCorrectly()
        {			
		    Supplier supplier = _system.SupplierIdentifiedAs("D", "22333444");
	
		    Assert.AreEqual(1,supplier.NumberOfAddresses());
		    Address address = supplier.AddressAt("San Martin");
		    Assert.AreEqual(3322,address.StreetNumber);
		    Assert.AreEqual("Olivos", address.Town);
		    Assert.AreEqual(1636, address.ZipCode);
		    Assert.AreEqual("BsAs", address.Province);

            Customer customer = _system.CustomerSystem().CustomerIdentifiedAs("D", "12");
		    Assert.AreEqual("Juan",customer.FirstName);
		    Assert.AreEqual("Perez",customer.LastName);
		    Assert.AreEqual("D",customer.IdentificationType);
		    Assert.AreEqual("12",customer.IdentificationNumber);
		    Assert.AreEqual(1,supplier.NumberOfCustomers());
		    Assert.IsTrue(supplier.ContainsCustomer(customer));
	    }

	    public StringReader ValidSupplierWithAddressAndCustomer() {
		    return new StringReader("S,Sanchez,D,22333444\n"+
                "A,San Martin,3322,Olivos,1636,BsAs\n"+
                "NC,Juan,Perez,D,12\n");
	    }	
	
	    [TestMethod]
	    public void ImportsExistingCustomerCorrectly()  {
		    _system.CustomerSystem().AddCustomer(new Customer
                {
                    FirstName = "Juan",
                    LastName = "Perez",
                    IdentificationType = "D",
                    IdentificationNumber = "12"
                });
		
		    new SupplierImporter(validSupplierWithAddressAndExistingCustomer(),_system).Value();

		    Assert.AreEqual(1,_system.NumberOfSuppliers());
		    assertSanchezWithAddressAndCustomerWasImportedCorrectly();
	    }

	    public StringReader validSupplierWithAddressAndExistingCustomer()
	    {
	        return new StringReader("S,Sanchez,D,22333444\n" +
	                                "A,San Martin,3322,Olivos,1636,BsAs\n" +
	                                "EC,D,12\n");
	    }	

	    [TestMethod]
	    public void InvalidExistingCustomerDoesNotImport()
	    {

	        ImportingShouldFailAndAssert(validSupplierWithAddressAndExistingCustomer(), e =>
	        {
	            Assert.AreEqual(CustomerSystem.CustomerNotFound, e.Message);
	            Assert.AreEqual(1, _system.NumberOfSuppliers());
	            Supplier supplier = _system.SupplierIdentifiedAs("D", "22333444");
	            Assert.AreEqual(0, supplier.NumberOfCustomers());
	        });

	    }

	    [TestMethod]
	    public void CanNotImportExistingCustomerWithOutSupplier()
	    {

	        ImportingShouldFailAndAssert(ExistingCustomerWithoutSupplier(), e =>
	        {
	            Assert.AreEqual(SupplierImporter.SupplierNotDefined, e.Message);
	            Assert.AreEqual(0, _system.NumberOfSuppliers());
	        });

	    }
	
	    public StringReader ExistingCustomerWithoutSupplier() {
		    return new StringReader("EC,D,12\n");
	    }	

        [TestMethod]
	    public void CanNotImportNewCustomerWithOutSupplier()
        {

            ImportingShouldFailAndAssert(newCustomerWithoutSupplier(), e =>
            {
                Assert.AreEqual(SupplierImporter.SupplierNotDefined, e.Message);
                Assert.AreEqual(0, _system.NumberOfSuppliers());
                Assert.AreEqual(0, _system.CustomerSystem().NumberOfCustomers());
            });

        }
	
	    public StringReader newCustomerWithoutSupplier() {
		    return new StringReader("NC,Juan,Perez,D,12\n");
	    }	
	
	    [TestMethod]
	    public void TestCanNotImportNewCustomerRecordWithLessThanFiveFields()
	    {
	        ImportingShouldFailAndAssert(CustomerRecordWithLessThanFiveFields(), e =>
	        {
	            Assert.AreEqual(SupplierImporter.InvalidCustomerRecord, e.Message);
	            Assert.AreEqual(0, _system.CustomerSystem().NumberOfCustomers());
	        });

	    }

	    public StringReader CustomerRecordWithLessThanFiveFields() {
		    return new StringReader("S,Sanchez,D,22333444\n"+
                "NC,Pepe,Sanchez,D\n");
	    }

	    [TestMethod]
	    public void TestCanNotImportCustomerRecordWithMoreThanFiveFields()
	    {
	        ImportingShouldFailAndAssert(customerRecordWithMoreThanFiveFields(), e =>
	        {
	            Assert.AreEqual(SupplierImporter.InvalidCustomerRecord, e.Message);
	            Assert.AreEqual(0, _system.CustomerSystem().NumberOfCustomers());
	        });

	    }

	    public StringReader customerRecordWithMoreThanFiveFields() {
		    return new StringReader("S,Sanchez,D,22333444\n"+
                "NC,Pepe,Sanchez,D,22333444,x\n");
	    }
	
	    [TestMethod]
	    public void TestCanNotImportExistingCustomerRecordWithLessThanTwoFields()
	    {
	        ImportingShouldFailAndAssert(existingCustomerRecordWithLessThanTwoFields(), e =>
	        {
	            Assert.AreEqual(SupplierImporter.InvalidCustomerRecord, e.Message);
	            Assert.AreEqual(0, _system.CustomerSystem().NumberOfCustomers());
	        });

	    }

	    public StringReader existingCustomerRecordWithLessThanTwoFields() {
		    return new StringReader("S,Sanchez,D,22333444\n" +
                "EC,D\n");
	    }

	    [TestMethod]
	    public void TestCanNotImportExistingCustomerRecordWithMoreThanTwoFields()
	    {
	        ImportingShouldFailAndAssert(ExistingCustomerRecordWithMoreThanTwoFields(), e =>
	        {
	            Assert.AreEqual(SupplierImporter.InvalidCustomerRecord, e.Message);
	            Assert.AreEqual(0, _system.CustomerSystem().NumberOfCustomers());
	        });

	    }

	    public StringReader ExistingCustomerRecordWithMoreThanTwoFields() {
		    return new StringReader("S,Sanchez,D,22333444\n" + "EC,D,12,x\n");
	    }
    }
}
