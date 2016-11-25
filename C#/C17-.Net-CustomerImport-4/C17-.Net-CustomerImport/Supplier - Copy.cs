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

        public void AddCustomer(Customer newCustomer)
        {
            throw new NotImplementedException();
        }
    }

    public interface SupplierSystem
    {
        void BeginTransaction();
        void OpenSession();
        void Commit();
        void CloseSession();
        int NumberOfSuppliers();
        Supplier SupplierIdentifiedAs(string identificationType, string identificationNumber);
        void AddSupplier(Supplier supplier);
    }

    public class TransientSupplierSystem : SupplierSystem
    {
        private IList<Supplier> _suppliers = new List<Supplier>();

        public void BeginTransaction()
        {
        }

        public void OpenSession()
        {
        }

        public void Commit()
        {
        }

        public void CloseSession()
        {
        }

        public int NumberOfSuppliers()
        {
            return _suppliers.Count;
        }

        public Supplier SupplierIdentifiedAs(string identificationType, string identificationNumber)
        {
            return _suppliers.Single(supplier => supplier.IsIdentifiedAs(identificationType, identificationNumber));
        }

        public void AddSupplier(Supplier supplier)
        {
            _suppliers.Add(supplier);
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

        public SupplierImporter(TextReader stream, SupplierSystem system)
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
            if (IsSupplierRecord())
                ImportSupplier();
            else if (IsCustomerRecord())
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
            return _record[0] == "A";
        }

        private bool IsCustomerRecord()
        {
            return _record[0] == "C";
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

        private void ImportCustomer()
        {
            if (CustomerRecordSizeIsNotCorrect()) throw new Exception(InvalidCustomerRecord);

            Customer newCustomer = new Customer
            {
                FirstName = _record[1],
                LastName = _record[2],
                IdentificationType = _record[3],
                IdentificationNumber = _record[4]
            };
            _newSupplier.AddCustomer(newCustomer);
        }

        private bool CustomerRecordSizeIsNotCorrect()
        {
            return _record.Length != 5;
        }
        private void ImportSupplier()
        {
            //if (SupplierRecordSizeIsNotCorrect()) throw new Exception(InvalidSupplierRecord);

            _newSupplier = new Supplier
            {
                Name = _record[1],
                IdentificationType = _record[2],
                IdentificationNumber = _record[3]
            };
            _system.AddSupplier(_newSupplier);        
        }
    }


    [TestClass]
    public class SupplierImportTest
    {
        private SupplierSystem _system;

        [TestInitialize]
        public void SetUp()
        {
            _system = Environment.Current().CreateSupplierSystem();
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
    }
}
