﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public CustomerImporter(ISession session, TextReader stream)
        {
            this._stream = stream;
            this._session = session;
        }

        public void Value()
        {
            Customer newCustomer = null;
            var line = _stream.ReadLine();
            while (line != null)
            {
                // 1: Extract variable, reemplazando dos ocurrencias
                var record = line.Split(',');
                if (line.StartsWith("C"))
                {
                    // 2: Inline de customerData
                    newCustomer = new Customer();
                    newCustomer.FirstName = record[1];
                    newCustomer.LastName = record[2];
                    newCustomer.IdentificationType = record[3];
                    newCustomer.IdentificationNumber = record[4];
                    _session.Persist(newCustomer);
                }
                else if (line.StartsWith("A"))
                {
                    // 2: Inline de addressData
                    var newAddress = new Address();

                    newCustomer.AddAddress(newAddress);
                    newAddress.StreetName = record[1];
                    newAddress.StreetNumber = Int32.Parse(record[2]);
                    newAddress.Town = record[3];
                    newAddress.ZipCode = Int32.Parse(record[4]);
                    newAddress.Province = record[5];
                }

                line = _stream.ReadLine();
            }
        }
    }

    [TestClass]
    public class CustomerImportTest
    {
        private ISession _session;

        [TestMethod]
        public void TestImportsValidDataCorrectly()
        {
            // Bad Smell 4: Que el test conozca como conectarse!
            using (_session = CreateSession())
            using (var inputStream = ValidDataStream())
            {
                var transaction = _session.BeginTransaction();
                new CustomerImporter(_session, inputStream).Value();

                Assert.AreEqual(2, _session.CreateCriteria<Customer>().List<Customer>().Count);

                AssertPepeSanchezWasImportedCorrectly();
                AssertJuanPerezWasImportedCorrectly();

                transaction.Commit();
            }
        }

        private void AssertJuanPerezWasImportedCorrectly()
        {
            var juanPerez = CustomerIdentifiedAs("C", "23-25666777-9");
            AssertCustomer(juanPerez, "Juan", "Perez", "C", "23-25666777-9", 1);
            AssertAddressAt(juanPerez, "Alem", 1122, "CABA", 1001, "CABA");
        }

        private void AssertPepeSanchezWasImportedCorrectly()
        {
            var pepeSanchez = CustomerIdentifiedAs("D", "22333444");
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

        private Customer CustomerIdentifiedAs(string identificationType, string identificationNumber)
        {
            var customers = _session.CreateCriteria<Customer>().
                                       Add(Restrictions.Eq("IdentificationType", identificationType)).
                                       Add(Restrictions.Eq("IdentificationNumber", identificationNumber)).List<Customer>();
            Assert.AreEqual(1, customers.Count);

            return customers[0];
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

        public ISession CreateSession()
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
            var session = sessionFactory.OpenSession();
            return session;
        }
    }
}
