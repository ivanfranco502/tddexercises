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
        public static void ImportCustomers(ISession session)
        {
            var fileStream = new System.IO.FileStream("input.txt",FileMode.Open);

            // 4: Extract method para CreateSession
            // 5: Introduce parameter de CreateSession
            // 6: Inline de session

            var lineReader = new StreamReader(fileStream);

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
                    newAddress.Province = addressData[3];
                }

                line = lineReader.ReadLine();
            }

            transaction.Commit();
            // 10: Extract method para poder controlar el close
            // 11: Hago el close en el test

            fileStream.Close();
        }

        public static ISession CreateSession()
        {
            var storeConfiguration = new StoreConfiguration();
            var configuration = Fluently.Configure()
                                        .Database(
                                            MsSqlCeConfiguration.Standard.ShowSql()
                                                                .ConnectionString("Data Source=CustomerImport.sdf"))
                                        .Mappings(m => m.AutoMappings.Add(AutoMap
                                                                              .AssemblyOf<Customer>(storeConfiguration)
                                                                              .Override<Customer>(
                                                                                  map =>
                                                                                  map.HasMany(x => x.Addresses).Cascade.All())));

            var sessionFactory = configuration.BuildSessionFactory();
            new SchemaExport(configuration.BuildConfiguration()).Execute(true, true, false);
            var session = sessionFactory.OpenSession();
            return session;
        }

        public int NumberOfAddress()
        {
            return Addresses.Count;
        }
    }

    [TestClass]
    public class CustomerImportTest
    {
        [TestMethod]
        public void Test1()
        {
            // 1: Agrego el test y saco el main
            // 8: Introduce Variable
            ISession session = Customer.CreateSession();
            Customer.ImportCustomers(session);
            // 2: Agrego asserts sobre cantidad de customers... pero no puedo
            // 3: Comento la linea para que el test corra
            // 9: Ahora si puedo usar session, pero el test falla porque la session esta cerrada!   
            // 12: ahora si asserto
            var customers = session.CreateCriteria<Customer>().List<Customer>();
            Assert.AreEqual(2,customers.Count);
            // 13: No tiene sentido CloseSession en Customer, lo muevo aca
            // 14: No tiene sentido el CloseSession! hago Inline

            // 15: Me aseguro que uno de ellos sea Pepe Sanchez
            customers = session.CreateCriteria<Customer>().
                Add(Restrictions.Eq("IdentificationType", "D")).
				Add(Restrictions.Eq("IdentificationNumber","22333444")).List<Customer>();
		    Assert.AreEqual(1,customers.Count);
            // 16: Asserto sobre los datos de Pepe Sanchez
            var customer = customers[0];
            Assert.AreEqual("Pepe", customer.FirstName);
            Assert.AreEqual("Sanchez", customer.LastName);
            Assert.AreEqual("D", customer.IdentificationType);
            Assert.AreEqual("22333444", customer.IdentificationNumber);
            Assert.AreEqual(2, customer.NumberOfAddress()); // No rompo encapsulamiento!
				
            session.Close();
        }
    }
}
