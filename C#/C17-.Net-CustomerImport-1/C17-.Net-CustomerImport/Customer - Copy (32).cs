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
        [TestMethod]
        public void Test1()
        {
            // 1: Agrego el test y saco el main
            // 8: Introduce Variable
            // 28: Muevo CreateSession aca
            // Bad Smell 4: Que el test conozca como conectarse!
            ISession session = CreateSession();
            var transaction = session.BeginTransaction();
            // 24: No tiene sentido stream() en Customer, lo muevo aca
            // 25: Introduce variable
            // 26: Saco archivo. Inline de stream. Inline de lineReader. Generalize Type en ImportCustomer.
            // rename stream1 a inputStream
            // 30: Doy semantica con extract method
            var inputStream = ValidDataStream();
            Customer.ImportCustomers(session, inputStream);
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

            // 17: Me aseguro que las direcciones sean correctas
            var address = customer.AddressAt("San Martin"); // No rompo encapsulamiento!
            Assert.AreEqual(3322, address.StreetNumber);
            Assert.AreEqual("Olivos", address.Town);
            Assert.AreEqual(1636, address.ZipCode);
            Assert.AreEqual("BsAs", address.Province);

            // 18: Me aseguro sobre la otra direccion
            // Bad Smell 1: Copy & Paste
            address = customer.AddressAt("Maipu"); 
            Assert.AreEqual(888, address.StreetNumber);
            Assert.AreEqual("Florida", address.Town);
            Assert.AreEqual(1122, address.ZipCode);
            Assert.AreEqual("Buenos Aires", address.Province);

            // 19: Me aseguro que Juan Perez haya sido importando bien
            // Bad Smell 2: Otro copy & paste
            // Bad Smell 3: Como se busca esta en el test!
            customers = session.CreateCriteria<Customer>().
                Add(Restrictions.Eq("IdentificationType", "C")).
                Add(Restrictions.Eq("IdentificationNumber", "23-25666777-9")).List<Customer>();
            Assert.AreEqual(1, customers.Count);
            
            customer = customers[0];
            Assert.AreEqual("Juan", customer.FirstName);
            Assert.AreEqual("Perez", customer.LastName);
            Assert.AreEqual("C", customer.IdentificationType);
            Assert.AreEqual("23-25666777-9", customer.IdentificationNumber);
            Assert.AreEqual(1, customer.NumberOfAddress()); 

            address = customer.AddressAt("Alem"); 
            Assert.AreEqual(1122, address.StreetNumber);
            Assert.AreEqual("CABA", address.Town);
            Assert.AreEqual(1001, address.ZipCode);
            Assert.AreEqual("CABA", address.Province);

            transaction.Commit();
            session.Close();
            inputStream.Close();
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
