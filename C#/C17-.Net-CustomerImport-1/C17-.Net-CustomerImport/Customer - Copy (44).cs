using System;
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
        
        // 7: Rename de session a session
        public static void ImportCustomers(ISession session, TextReader stream)
        {

            // 4: Extract method para CreateSession
            // 5: Introduce parameter de CreateSession
            // 6: Inline de session

            // 20: Tengo que poder importar de cualquier stream! Lo debo parametrizar
            // 21: No puedo hacer extract method facilmente a menos que 
            // cambie el close para que se haga sobre lineReader
            // 22: Extract Method
            // 23: Inline

            // 27: Saco manejo de transaccion. Extract Method. Move Method. Sacar colaboracion de aca
            // Hacer inline en test

            Customer newCustomer = null;
            var line = stream.ReadLine();
            while (line != null)
            {
                if (line.StartsWith("C"))
                {
                    var customerData = line.Split(',');
                    newCustomer = new Customer();
                    newCustomer.FirstName = customerData[1];
                    newCustomer.LastName = customerData[2];
                    newCustomer.IdentificationType = customerData[3];
                    // 16: Arreglo error
                    newCustomer.IdentificationNumber = customerData[4];
                    session.Persist(newCustomer);
                }
                else if (line.StartsWith("A"))
                {
                    var addressData = line.Split(',');
                    var newAddress = new Address();

                    newCustomer.AddAddress(newAddress);
                    newAddress.StreetName = addressData[1];
                    newAddress.StreetNumber = int.Parse(addressData[2]);
                    newAddress.Town = addressData[3];
                    newAddress.ZipCode = int.Parse(addressData[4]);
                    // 18: Arreglo error
                    newAddress.Province = addressData[5];
                }

                line = stream.ReadLine();
            }

            // 10: Extract method para poder controlar el close
            // 11: Hago el close en el test

            // 25: Extract method, move y sacarlo de aca
        }

        public virtual int NumberOfAddress()
        {
            return Addresses.Count;
        }

        public virtual Address AddressAt(string aStreetName)
        {
            return Addresses.First(anAddress => anAddress.IsAt(aStreetName)); // No rompo encapsulamiento!
        }
    }

    [TestClass]
    public class CustomerImportTest
    {
        private ISession _session;

        [TestMethod]
        public void Test1()
        {
            // Bad Smell 4: Que el test conozca como conectarse!
            // 31: Uso idiom using
            // 32: Vuelvo para atras, introduce field
            using (_session = CreateSession())
            using (var inputStream = ValidDataStream())
            {
                var transaction = _session.BeginTransaction();
                Customer.ImportCustomers(_session, inputStream);

                // 33: Vuelvo a hacer los refactors
                Assert.AreEqual(2, _session.CreateCriteria<Customer>().List<Customer>().Count);

                AssertPepeSanchezWasImportedCorrectly();
                AssertJuanPerezWasImportedCorrectly();

                transaction.Commit();
            }
        }

        private void AssertJuanPerezWasImportedCorrectly()
        {
            // 35: Reutilizo customer identified as
            var customer = CustomerIdentifiedAs("C", "23-25666777-9");
            AssertCustomer(customer, "Juan", "Perez", "C", "23-25666777-9", 1);

            AssertAddressAt(customer, "Alem", 1122, "CABA", 1001, "CABA");
        }

        private void AssertPepeSanchezWasImportedCorrectly()
        {
            // 33: Empiezo a sacar bad smell 3
            var customer = CustomerIdentifiedAs("D", "22333444");
            // 38: Empiezo a sacar bad smell 2
            AssertCustomer(customer, "Pepe", "Sanchez", "D", "22333444", 2);

            // 36: empiezo a sacar bad smell 1
            AssertAddressAt(customer, "San Martin", 3322, "Olivos", 1636, "BsAs");
            AssertAddressAt(customer, "Maipu", 888, "Florida", 1122, "Buenos Aires");
        }

        private static void AssertCustomer(Customer customer, string firstName, string lastName, 
            string identificationType, string identificationNumber, int numberOfAddresses)
        {
            Assert.AreEqual(firstName, customer.FirstName);
            Assert.AreEqual(lastName, customer.LastName);
            Assert.AreEqual(identificationType, customer.IdentificationType);
            Assert.AreEqual(identificationNumber, customer.IdentificationNumber);
            Assert.AreEqual(numberOfAddresses, customer.NumberOfAddress());
        }

        // 37: Parametrizo lo que cambia
        private static void AssertAddressAt(Customer customer, string aStreetName, int streetNumber, 
            string town, int zipCode, string province)
        {
            var address = customer.AddressAt(aStreetName);
            Assert.AreEqual(streetNumber, address.StreetNumber);
            Assert.AreEqual(town, address.Town);
            Assert.AreEqual(zipCode, address.ZipCode);
            Assert.AreEqual(province, address.Province);
        }

        // 34: Parametrizo lo que cambia con Introduce Parameter. Inline de customer
        private Customer CustomerIdentifiedAs(string identificationType, string identificationNumber)
        {
            var customers = _session.CreateCriteria<Customer>().
                                       Add(Restrictions.Eq("IdentificationType", identificationType)).
                                       Add(Restrictions.Eq("IdentificationNumber", identificationNumber)).List<Customer>();
            Assert.AreEqual(1, customers.Count);

            return customers[0];
        }

        private static StringReader ValidDataStream()
        {
            var inputStream = new StringReader("C,Pepe,Sanchez,D,22333444\n" +
                                               "A,San Martin,3322,Olivos,1636,BsAs\n" +
                                               "A,Maipu,888,Florida,1122,Buenos Aires\n" +
                                               "C,Juan,Perez,C,23-25666777-9\n" +
                                               "A,Alem,1122,CABA,1001,CABA");
            return inputStream;
        }

        public static ISession CreateSession()
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
