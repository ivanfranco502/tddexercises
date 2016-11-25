using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using FluentNHibernate.Automapping;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Tool.hbm2ddl;

namespace com.tenpines.advancetdd
{
    public abstract class ErpSystem
    {
        public abstract void BeginTransaction();
        public abstract void OpenSession();
        public abstract void Commit();
        public abstract void CloseSession();
        public abstract CustomerSystem CustumerSystem();
        public abstract SupplierSystem SupplierSystem();
    }

    public class TransientErpSystem : ErpSystem
    {
        private TransientCustomerSystem _customerSystem;
        private TransientSupplierSystem _supplierSystem;

        public TransientErpSystem()
        {
            _customerSystem = new TransientCustomerSystem(this);
            _supplierSystem = new TransientSupplierSystem(this);
        }

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

        public override CustomerSystem CustumerSystem()
        {
            return _customerSystem;
        }

        public override SupplierSystem SupplierSystem()
        {
            return _supplierSystem;
        }
    }

    public class PersistentErpSystem : ErpSystem
    {
        private PersistentCustomerSystem _customerSystem;
        private PersistentSupplierSystem _supplierSystem;
        private ISession _session;
        private ITransaction _transaction;

        public PersistentErpSystem()
        {
            _customerSystem = new PersistentCustomerSystem(this);
            _supplierSystem = new PersistentSupplierSystem(this);
        }

        public ISession GetSession()
        {
            return _session;
        }

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

        public override CustomerSystem CustumerSystem()
        {
            return _customerSystem;
        }

        public override SupplierSystem SupplierSystem()
        {
            return _supplierSystem;
        }
    }

    public abstract class CustomerSystem
    {
        public const string CustomerNotFound = "Customer not found";
        public abstract int NumberOfCustomers();
        public abstract Customer CustomerIdentifiedAs(string identificationType, string identificationNumber);
        public abstract void AddCustomer(Customer customer);
    }

    public class TransientCustomerSystem : CustomerSystem
    {
        private readonly TransientErpSystem _erpSystem;
        private IList<Customer> _customers = new List<Customer>();

        public TransientCustomerSystem(TransientErpSystem erpSystem)
        {
            _erpSystem = erpSystem;
        }

        public override int NumberOfCustomers()
        {
            return _customers.Count;
        }

        public override Customer CustomerIdentifiedAs(string identificationType, string identificationNumber)
        {
            Customer foundCustomer = _customers.SingleOrDefault(
                customer => customer.IsIdentifiedAs(identificationType, identificationNumber));
            if (foundCustomer == null) throw new Exception(CustomerNotFound);
            return foundCustomer;
        }

        public override void AddCustomer(Customer customer)
        {
            _customers.Add(customer);
        }
    }

    public class PersistentCustomerSystem : CustomerSystem
    {
        private PersistentErpSystem _erpSystem;

        public PersistentCustomerSystem(PersistentErpSystem erpSystem)
        {
            _erpSystem = erpSystem;
        }
        public override int NumberOfCustomers()
        {
            return _erpSystem.GetSession().CreateCriteria<Customer>().List<Customer>().Count;
        }

        public override Customer CustomerIdentifiedAs(string identificationType, string identificationNumber)
        {
            var customers = _erpSystem.GetSession().CreateCriteria<Customer>().
                Add(Restrictions.Eq("IdentificationType", identificationType)).
                Add(Restrictions.Eq("IdentificationNumber", identificationNumber)).List<Customer>();
            Assert.AreEqual(1, customers.Count);

            return customers[0];
        }

        public override void AddCustomer(Customer customer)
        {
            _erpSystem.GetSession().Persist(customer);
        }
    }

    public abstract class SupplierSystem
    {
        private const String SupplierNotFound = "Supplier not found";
        public abstract int NumberOfSuppliers();
        public abstract Supplier SupplierIdentifiedAs(string identificationType, string identificationNumber);
        public abstract void AddSupplier(Supplier supplier);
        public abstract CustomerSystem CustomerSystem();
    }

    public class TransientSupplierSystem : SupplierSystem
    {
        private readonly TransientErpSystem _erpSystem;
        private IList<Supplier> _suppliers = new List<Supplier>();

        public TransientSupplierSystem(TransientErpSystem erpSystem)
        {
            _erpSystem = erpSystem;
        }

        public override int NumberOfSuppliers()
        {
            return _suppliers.Count;
        }

        public override Supplier SupplierIdentifiedAs(string identificationType, string identificationNumber)
        {
            return _suppliers.Single(supplier => supplier.IsIdentifiedAs(identificationType, identificationNumber));
        }

        public override void AddSupplier(Supplier supplier)
        {
            _suppliers.Add(supplier);
        }

        public override CustomerSystem CustomerSystem()
        {
            return _erpSystem.CustumerSystem();
        }
    }

    public class PersistentSupplierSystem : SupplierSystem
    {
        private PersistentErpSystem _erpSystem;

        public PersistentSupplierSystem(PersistentErpSystem erpSystem)
        {
            _erpSystem = erpSystem;
        }
        public override int NumberOfSuppliers()
        {
            return _erpSystem.GetSession().CreateCriteria<Supplier>().List<Supplier>().Count;
        }

        public override Supplier SupplierIdentifiedAs(string identificationType, string identificationNumber)
        {
            var suppliers = _erpSystem.GetSession().CreateCriteria<Supplier>().
                Add(Restrictions.Eq("IdentificationType", identificationType)).
                Add(Restrictions.Eq("IdentificationNumber", identificationNumber)).List<Supplier>();
            Assert.AreEqual(1, suppliers.Count);

            return suppliers[0];
        }

        public override void AddSupplier(Supplier supplier)
        {
            _erpSystem.GetSession().Persist(supplier);
        }

        public override CustomerSystem CustomerSystem()
        {
            return _erpSystem.CustumerSystem();
        }
    }
}
